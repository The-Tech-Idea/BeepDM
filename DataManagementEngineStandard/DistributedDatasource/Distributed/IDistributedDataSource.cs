using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Distributed.Events;
using TheTechIdea.Beep.Proxy;

namespace TheTechIdea.Beep.Distributed
{
    /// <summary>
    /// Distributed-tier datasource contract that exposes a single
    /// <see cref="IDataSource"/> surface over a set of physical shards.
    /// Each shard is itself an HA pool implemented by an existing
    /// <see cref="IProxyCluster"/>, so per-shard failover, circuit
    /// breaking, and load balancing are reused from the
    /// <c>Proxy/</c> tier rather than reinvented.
    ///
    /// Phase 01 only defines the contract and the partial-class
    /// skeleton; routing, execution, transactions, and observability
    /// are filled in by later phases (see
    /// <c>DistributedDatasource/DistributedPlans/MASTER-TODO-TRACKER.md</c>).
    /// </summary>
    /// <remarks>
    /// Implementations MUST remain additive against
    /// <see cref="IDataSource"/> consumers — callers that only need an
    /// <see cref="IDataSource"/> never need to cast to
    /// <see cref="IDistributedDataSource"/>.
    /// </remarks>
    public interface IDistributedDataSource : IDataSource, IDisposable
    {
        // ── Plan / topology snapshot ──────────────────────────────────────

        /// <summary>
        /// Read-only snapshot of the distribution plan currently in
        /// effect. Replace with a new plan via
        /// <see cref="ApplyDistributionPlan(DistributionPlan)"/>.
        /// </summary>
        DistributionPlan DistributionPlan { get; }

        /// <summary>
        /// Read-only snapshot of the shard map (shardId →
        /// <see cref="IProxyCluster"/>). The dictionary returned MUST be
        /// safe to enumerate concurrently with active operations.
        /// </summary>
        IReadOnlyDictionary<string, IProxyCluster> Shards { get; }

        /// <summary>
        /// Atomically replaces the active distribution plan. Phase 02
        /// validates the plan against the current shard map; Phase 11
        /// extends this method with reshard-on-apply behaviour. Phase 01
        /// only validates non-null and stores the reference.
        /// </summary>
        /// <param name="plan">New plan to install. Must not be <c>null</c>.</param>
        void ApplyDistributionPlan(DistributionPlan plan);

        // ── Events (declared in Phase 01, raised in later phases) ────────

        /// <summary>Raised after the router resolves an entity / key pair to a shard. Phase 05+.</summary>
        event EventHandler<ShardSelectedEventArgs> OnShardSelected;

        /// <summary>Raised when a routing or plan-application step finds a placement violation. Phase 02+.</summary>
        event EventHandler<PlacementViolationEventArgs> OnPlacementViolation;

        /// <summary>Raised when a reshard operation begins. Phase 11+.</summary>
        event EventHandler<ReshardEventArgs> OnReshardStarted;

        /// <summary>Raised when a reshard operation completes (successfully or otherwise). Phase 11+.</summary>
        event EventHandler<ReshardEventArgs> OnReshardCompleted;

        /// <summary>Raised as a reshard copy loop makes progress. Phase 11+.</summary>
        event EventHandler<ReshardProgressEventArgs> OnReshardProgress;

        /// <summary>
        /// Raised when a replicated or broadcast write satisfied its
        /// quorum but at least one target shard still failed. Phase 07+.
        /// Full quorum failures surface via the normal
        /// <see cref="IErrorsInfo"/> return path and do not raise this
        /// event.
        /// </summary>
        event EventHandler<PartialReplicationFailureEventArgs> OnPartialReplicationFailure;

        /// <summary>
        /// Raised when a two-phase commit scope reaches
        /// <see cref="Transactions.DistributedTransactionStatus.InDoubt"/>:
        /// every shard voted <c>PrepareOk</c> but at least one shard
        /// failed the commit round. Operators (or the Phase 13
        /// reconciler) must decide how to resolve the orphaned state.
        /// Phase 09+.
        /// </summary>
        event EventHandler<TransactionInDoubtEventArgs> OnTransactionInDoubt;

        /// <summary>
        /// Raised when the <see cref="Resilience.IShardHealthMonitor"/>
        /// classifies a shard as unhealthy for the first time since
        /// the last recovery. Phase 10+.
        /// </summary>
        event EventHandler<ShardDownEventArgs> OnShardDown;

        /// <summary>
        /// Raised when a previously unhealthy shard recovers.
        /// Phase 10+.
        /// </summary>
        event EventHandler<ShardRestoredEventArgs> OnShardRestored;

        /// <summary>
        /// Raised when the healthy-shard ratio falls below
        /// <see cref="DistributedDataSourceOptions.MinimumHealthyShardRatio"/>
        /// and a scatter / broadcast call had to be rejected or
        /// degraded. Phase 10+.
        /// </summary>
        event EventHandler<DegradedModeEventArgs> OnDegradedMode;

        /// <summary>
        /// Raised when a broadcast write excluded one or more
        /// unhealthy shards but still satisfied its quorum, so the
        /// call succeeded with a missing-replica gap that operators
        /// (or a Phase 13 reconciler) must replay. Phase 10+.
        /// </summary>
        event EventHandler<PartialBroadcastEventArgs> OnPartialBroadcast;
    }
}
