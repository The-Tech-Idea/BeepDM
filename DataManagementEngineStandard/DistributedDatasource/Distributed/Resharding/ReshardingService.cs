using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Distributed.Events;
using TheTechIdea.Beep.Distributed.Plan;
using TheTechIdea.Beep.Proxy;

namespace TheTechIdea.Beep.Distributed.Resharding
{
    /// <summary>
    /// Default <see cref="IReshardingService"/> — orchestrates every
    /// topology change (add/remove shard, move/repartition entity,
    /// apply a new plan) through the Shadow → DualWrite → Cutover
    /// window protocol.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The service is decoupled from <see cref="DistributedDataSource"/>
    /// through a small set of delegates supplied at construction time.
    /// This keeps the partial class focused on orchestration while
    /// making every side effect (plan swap, shard catalog mutation,
    /// event raise) testable in isolation.
    /// </para>
    /// <para>
    /// Implementation is split across partials:
    /// </para>
    /// <list type="bullet">
    ///   <item><c>ReshardingService.cs</c> — construction, shared helpers, <c>ApplyPlanAsync</c>.</item>
    ///   <item><c>ReshardingService.AddShard.cs</c> — <c>AddShardAsync</c>.</item>
    ///   <item><c>ReshardingService.RemoveShard.cs</c> — <c>RemoveShardAsync</c>.</item>
    ///   <item><c>ReshardingService.MoveEntity.cs</c> — <c>MoveEntityAsync</c>.</item>
    ///   <item><c>ReshardingService.Repartition.cs</c> — <c>RepartitionEntityAsync</c>.</item>
    /// </list>
    /// </remarks>
    public sealed partial class ReshardingService : IReshardingService
    {
        private readonly Func<DistributionPlan>                         _getCurrentPlan;
        private readonly Action<DistributionPlan>                       _applyPlan;
        private readonly Func<string, IProxyCluster>                    _resolveShard;
        private readonly Action<string, IProxyCluster>                  _registerShard;
        private readonly Action<string>                                 _unregisterShard;
        private readonly Action<string, string, int?>                   _raiseStarted;
        private readonly Action<string, string, int?, Exception>        _raiseCompleted;
        private readonly Action<ReshardProgressEventArgs>               _raiseProgress;

        /// <summary>Initialises a new resharding service.</summary>
        /// <param name="dualWrites">Shared dual-write coordinator. Required.</param>
        /// <param name="copyService">Shared copy service. Required.</param>
        /// <param name="getCurrentPlan">Delegate that returns the currently-active plan.</param>
        /// <param name="applyPlan">Delegate that atomically replaces the active plan.</param>
        /// <param name="resolveShard">Delegate that returns the <see cref="IProxyCluster"/> for a shard id (or <c>null</c>).</param>
        /// <param name="registerShard">Delegate that adds a new shard to the catalog.</param>
        /// <param name="unregisterShard">Delegate that removes a shard from the catalog.</param>
        /// <param name="raiseStarted">Delegate used to raise <c>OnReshardStarted</c> (reshardId, reason, affected).</param>
        /// <param name="raiseCompleted">Delegate used to raise <c>OnReshardCompleted</c> (reshardId, reason, affected, error).</param>
        /// <param name="raiseProgress">Delegate used to raise <c>OnReshardProgress</c>.</param>
        public ReshardingService(
            IDualWriteCoordinator                              dualWrites,
            IEntityCopyService                                 copyService,
            Func<DistributionPlan>                             getCurrentPlan,
            Action<DistributionPlan>                           applyPlan,
            Func<string, IProxyCluster>                        resolveShard,
            Action<string, IProxyCluster>                      registerShard,
            Action<string>                                     unregisterShard,
            Action<string, string, int?>                       raiseStarted,
            Action<string, string, int?, Exception>            raiseCompleted,
            Action<ReshardProgressEventArgs>                   raiseProgress)
        {
            DualWrites       = dualWrites       ?? throw new ArgumentNullException(nameof(dualWrites));
            CopyService      = copyService      ?? throw new ArgumentNullException(nameof(copyService));
            _getCurrentPlan  = getCurrentPlan   ?? throw new ArgumentNullException(nameof(getCurrentPlan));
            _applyPlan       = applyPlan        ?? throw new ArgumentNullException(nameof(applyPlan));
            _resolveShard    = resolveShard     ?? throw new ArgumentNullException(nameof(resolveShard));
            _registerShard   = registerShard    ?? throw new ArgumentNullException(nameof(registerShard));
            _unregisterShard = unregisterShard  ?? throw new ArgumentNullException(nameof(unregisterShard));
            _raiseStarted    = raiseStarted    ?? ((_, __, ___) => { });
            _raiseCompleted  = raiseCompleted  ?? ((_, __, ___, ____) => { });
            _raiseProgress   = raiseProgress   ?? (_ => { });
        }

        /// <inheritdoc/>
        public IDualWriteCoordinator DualWrites { get; }

        /// <inheritdoc/>
        public IEntityCopyService CopyService { get; }

        /// <inheritdoc/>
        public async Task<ReshardOutcome> ApplyPlanAsync(
            DistributionPlan  targetPlan,
            CancellationToken cancellationToken = default)
        {
            if (targetPlan == null) throw new ArgumentNullException(nameof(targetPlan));

            var reshardId = NewReshardId("ApplyPlan");
            var currentPlan = _getCurrentPlan();
            var diff        = PlanDiff.Compute(currentPlan, targetPlan);

            _raiseStarted(reshardId, $"ApplyPlan '{targetPlan.Name}' v{targetPlan.Version}", diff.Count);

            if (diff.Count == 0)
            {
                // Pure version bump — swap and return.
                _applyPlan(targetPlan);
                _raiseCompleted(reshardId, "ApplyPlan completed: plan unchanged", 0, null);
                return SuccessOutcome(reshardId, "ApplyPlan", targetPlan.Version, copyResults: Array.Empty<CopyResult>());
            }

            var copyResults = new List<CopyResult>();
            try
            {
                foreach (var step in diff)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var legResults = await ExecuteDiffStepAsync(reshardId, step, cancellationToken).ConfigureAwait(false);
                    copyResults.AddRange(legResults);
                }

                _applyPlan(targetPlan);
                _raiseCompleted(reshardId, $"ApplyPlan completed ({diff.Count} step(s))", diff.Count, null);
                return SuccessOutcome(reshardId, "ApplyPlan", targetPlan.Version, copyResults);
            }
            catch (OperationCanceledException)
            {
                _raiseCompleted(reshardId, "ApplyPlan cancelled", diff.Count, null);
                return CancelledOutcome(reshardId, "ApplyPlan", currentPlan?.Version ?? 0, copyResults);
            }
            catch (Exception ex)
            {
                _raiseCompleted(reshardId, $"ApplyPlan failed: {ex.Message}", diff.Count, ex);
                return FailureOutcome(reshardId, "ApplyPlan", currentPlan?.Version ?? 0, copyResults, ex);
            }
        }

        // ── Shared helpers consumed by partials ──────────────────────────

        private async Task<IReadOnlyList<CopyResult>> ExecuteDiffStepAsync(
            string            reshardId,
            PlanDiffEntry     step,
            CancellationToken cancellationToken)
        {
            switch (step.Kind)
            {
                case PlanDiffKind.AddEntity:
                    // New placement — no rows to copy; the plan swap seeds the placement
                    // on the next apply. Record no-op copy results for traceability.
                    return Array.Empty<CopyResult>();

                case PlanDiffKind.RemoveEntity:
                    return Array.Empty<CopyResult>();

                case PlanDiffKind.MoveEntity:
                    var oldShard = step.OldPlacement.ShardIds[0];
                    var newShard = step.NewPlacement.ShardIds[0];
                    var moveOutcome = await MoveEntityInternalAsync(
                        reshardId, step.EntityName, oldShard, newShard, cancellationToken).ConfigureAwait(false);
                    return moveOutcome;

                case PlanDiffKind.Repartition:
                case PlanDiffKind.ModeChange:
                    var repartOutcome = await RepartitionEntityInternalAsync(
                        reshardId, step.EntityName,
                        step.OldPlacement.ShardIds, step.NewPlacement.ShardIds,
                        cancellationToken).ConfigureAwait(false);
                    return repartOutcome;

                default:
                    return Array.Empty<CopyResult>();
            }
        }

        private IProxyCluster ResolveOrThrow(string shardId, string roleForError)
        {
            var cluster = _resolveShard(shardId);
            if (cluster == null)
            {
                throw new InvalidOperationException(
                    $"Unable to resolve {roleForError} shard '{shardId}' — not registered in the live catalog.");
            }
            return cluster;
        }

        private void EnsureNoActiveWindow(string entityName)
        {
            var existing = DualWrites.TryGetWindow(entityName);
            if (existing != null)
            {
                throw new InvalidOperationException(
                    $"A reshard is already in progress for entity '{entityName}' " +
                    $"(state={existing.State}, reshard={existing.ReshardId}).");
            }
        }

        private static string NewReshardId(string operation)
            => $"reshard-{operation}-{Guid.NewGuid():N}";

        private static ReshardOutcome SuccessOutcome(
            string                    reshardId,
            string                    operation,
            int                       planVersion,
            IReadOnlyList<CopyResult> copyResults)
            => new ReshardOutcome(reshardId, operation, success: true, cancelled: false, error: null, copyResults, planVersion);

        private static ReshardOutcome CancelledOutcome(
            string                    reshardId,
            string                    operation,
            int                       planVersion,
            IReadOnlyList<CopyResult> copyResults)
            => new ReshardOutcome(reshardId, operation, success: false, cancelled: true, error: null, copyResults, planVersion);

        private static ReshardOutcome FailureOutcome(
            string                    reshardId,
            string                    operation,
            int                       planVersion,
            IReadOnlyList<CopyResult> copyResults,
            Exception                 ex)
            => new ReshardOutcome(reshardId, operation, success: false, cancelled: false, error: ex, copyResults, planVersion);

        private void RaiseProgressSafe(
            string                    reshardId,
            string                    entityName,
            string                    fromShardId,
            string                    toShardId,
            long                      rowsCopied,
            long?                     totalRows,
            DualWriteState            dualWriteState)
        {
            try
            {
                _raiseProgress(new ReshardProgressEventArgs(
                    reshardId:      reshardId,
                    entityName:     entityName,
                    fromShardId:    fromShardId,
                    toShardId:      toShardId,
                    rowsCopied:     rowsCopied,
                    totalRows:      totalRows,
                    dualWriteState: dualWriteState));
            }
            catch
            {
                // never propagate from progress notification
            }
        }

        private async Task<CopyResult> RunLegAsync(
            string            reshardId,
            string            entityName,
            string            fromShard,
            string            toShard,
            CancellationToken cancellationToken)
        {
            var source = ResolveOrThrow(fromShard, "source");
            var target = ResolveOrThrow(toShard,   "target");

            var progress = new Progress<CopyProgress>(p => RaiseProgressSafe(
                reshardId:      reshardId,
                entityName:     entityName,
                fromShardId:    fromShard,
                toShardId:      toShard,
                rowsCopied:     p.RowsCopied,
                totalRows:      p.TotalRows,
                dualWriteState: DualWrites.TryGetWindow(entityName)?.State ?? DualWriteState.Off));

            return await CopyService.CopyAsync(
                reshardId:         reshardId,
                entityName:        entityName,
                fromShardId:       fromShard,
                toShardId:         toShard,
                source:            source,
                target:            target,
                filter:            null,
                progress:          progress,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }
}
