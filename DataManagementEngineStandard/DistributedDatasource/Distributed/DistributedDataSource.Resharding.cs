using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Distributed.Events;
using TheTechIdea.Beep.Distributed.Plan;
using TheTechIdea.Beep.Distributed.Resharding;
using TheTechIdea.Beep.Proxy;

namespace TheTechIdea.Beep.Distributed
{
    /// <summary>
    /// <see cref="DistributedDataSource"/> partial — Phase 11 resharding
    /// surface. Exposes the <see cref="IReshardingService"/>, the
    /// in-memory <see cref="IDualWriteCoordinator"/>, and async
    /// wrappers for add-shard, remove-shard, move-entity, repartition,
    /// and apply-plan calls. Every operation runs through the same
    /// Shadow → DualWrite → Cutover → Off window protocol so the
    /// router and write executors stay in lockstep while data moves.
    /// </summary>
    public partial class DistributedDataSource
    {
        private readonly object                 _reshardLock = new object();
        private IDualWriteCoordinator           _dualWriteCoordinator;
        private IEntityCopyService              _entityCopyService;
        private IReshardingService              _reshardingService;

        /// <summary>
        /// Active dual-write coordinator (shared with
        /// <see cref="Router"/> during resharding). Lazily initialised
        /// on first use.
        /// </summary>
        public IDualWriteCoordinator DualWriteCoordinator
        {
            get
            {
                EnsureReshardingInitialized();
                return _dualWriteCoordinator;
            }
        }

        /// <summary>
        /// Active entity copy service used for bulk-copy legs between
        /// shards during a reshard operation. Lazily initialised.
        /// </summary>
        public IEntityCopyService EntityCopyService
        {
            get
            {
                EnsureReshardingInitialized();
                return _entityCopyService;
            }
        }

        /// <summary>
        /// Phase 11 resharding orchestrator. Lazily initialised so
        /// deployments that never reshape their topology pay no cost.
        /// </summary>
        public IReshardingService Resharder
        {
            get
            {
                EnsureReshardingInitialized();
                return _reshardingService;
            }
        }

        /// <inheritdoc cref="IReshardingService.AddShardAsync"/>
        public Task<ReshardOutcome> AddShardAsync(
            ShardSpec         spec,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return Resharder.AddShardAsync(spec, cancellationToken);
        }

        /// <inheritdoc cref="IReshardingService.RemoveShardAsync"/>
        public Task<ReshardOutcome> RemoveShardAsync(
            RemoveShardPlan   plan,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return Resharder.RemoveShardAsync(plan, cancellationToken);
        }

        /// <inheritdoc cref="IReshardingService.MoveEntityAsync"/>
        public Task<ReshardOutcome> MoveEntityAsync(
            string            entityName,
            string            fromShard,
            string            toShard,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return Resharder.MoveEntityAsync(entityName, fromShard, toShard, cancellationToken);
        }

        /// <inheritdoc cref="IReshardingService.RepartitionEntityAsync"/>
        public Task<ReshardOutcome> RepartitionEntityAsync(
            string                entityName,
            PartitionFunctionRef  newFunction,
            IReadOnlyList<string> newShardIds,
            CancellationToken     cancellationToken = default)
        {
            ThrowIfDisposed();
            return Resharder.RepartitionEntityAsync(entityName, newFunction, newShardIds, cancellationToken);
        }

        /// <summary>
        /// Applies a new distribution plan, running the Phase 11
        /// reshard pipeline (copy + dual-write + cutover) for every
        /// placement delta detected by <see cref="PlanDiff.Compute"/>.
        /// Pure version bumps (no placement deltas) short-circuit and
        /// just swap the plan reference.
        /// </summary>
        /// <param name="plan">Target plan. Must not be <c>null</c>.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public Task<ReshardOutcome> ApplyDistributionPlanAsync(
            DistributionPlan  plan,
            CancellationToken cancellationToken = default)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            ThrowIfDisposed();
            ValidatePlanAgainstShards(plan);
            return Resharder.ApplyPlanAsync(plan, cancellationToken);
        }

        // ── Internal catalog hooks used by ReshardingService ─────────────

        private void EnsureReshardingInitialized()
        {
            if (_reshardingService != null) return;
            lock (_reshardLock)
            {
                if (_reshardingService != null) return;

                _dualWriteCoordinator ??= new DualWriteCoordinator();
                _entityCopyService    ??= new EntityCopyService();

                _reshardingService = new ReshardingService(
                    dualWrites:      _dualWriteCoordinator,
                    copyService:     _entityCopyService,
                    getCurrentPlan:  () => DistributionPlan,
                    applyPlan:       ApplyDistributionPlanInternal,
                    resolveShard:    ResolveShardInternal,
                    registerShard:   RegisterShardInternal,
                    unregisterShard: UnregisterShardInternal,
                    raiseStarted:    RaiseReshardStarted,
                    raiseCompleted:  RaiseReshardCompleted,
                    raiseProgress:   RaiseReshardProgress);

                AttachDualWriteCoordinatorToRouter();
            }
        }

        private void ApplyDistributionPlanInternal(DistributionPlan plan)
        {
            if (plan == null) return;

            lock (_planSwapLock)
            {
                _plan = plan;
                RebuildPlacementResolver(plan);
            }
            AttachDualWriteCoordinatorToRouter();
        }

        private IProxyCluster ResolveShardInternal(string shardId)
        {
            if (string.IsNullOrWhiteSpace(shardId)) return null;
            return _shards.TryGetValue(shardId, out var cluster) ? cluster : null;
        }

        private void RegisterShardInternal(string shardId, IProxyCluster cluster)
        {
            if (string.IsNullOrWhiteSpace(shardId))
                throw new ArgumentException("Shard id required.", nameof(shardId));
            if (cluster == null)
                throw new ArgumentNullException(nameof(cluster));

            _shards[shardId] = cluster;
            lock (_planSwapLock)
            {
                RebuildPlacementResolver(_plan);
            }
            AttachDualWriteCoordinatorToRouter();
        }

        private void UnregisterShardInternal(string shardId)
        {
            if (string.IsNullOrWhiteSpace(shardId)) return;
            _shards.TryRemove(shardId, out _);
            lock (_planSwapLock)
            {
                RebuildPlacementResolver(_plan);
            }
            AttachDualWriteCoordinatorToRouter();
        }

        private void AttachDualWriteCoordinatorToRouter()
        {
            if (_dualWriteCoordinator == null) return;
            lock (_planSwapLock)
            {
                if (_router is Routing.ShardRouter concrete)
                {
                    concrete.DualWrites = _dualWriteCoordinator;
                }
            }
        }
    }
}
