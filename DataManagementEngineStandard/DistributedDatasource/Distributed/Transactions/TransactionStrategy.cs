namespace TheTechIdea.Beep.Distributed.Transactions
{
    /// <summary>
    /// Execution strategy the Phase 09
    /// <see cref="IDistributedTransactionCoordinator"/> selects for a
    /// given scope. Picked by
    /// <see cref="TransactionDecisionResolver"/> based on the enlisted
    /// shards' capabilities and the active
    /// <see cref="DistributedDataSourceOptions"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The strategy is captured on the
    /// <see cref="DistributedTransactionScope"/> at begin time and
    /// cannot change afterwards — callers that need a different
    /// policy must open a fresh scope. This avoids accidental
    /// promotion from a cheap fast-path to 2PC mid-flight.
    /// </para>
    /// </remarks>
    public enum TransactionStrategy
    {
        /// <summary>
        /// Every enlisted entity resolves to the same shard. The
        /// coordinator forwards <c>BeginTransaction</c> /
        /// <c>Commit</c> / <c>EndTransaction</c> to that shard's
        /// cluster with no added round-trips.
        /// </summary>
        SingleShardFastPath = 0,

        /// <summary>
        /// Explicit two-phase commit: a prepare round (every shard
        /// votes) is followed by a commit round only when every vote
        /// is <c>PrepareOk</c>. Any prepare failure triggers a
        /// rollback round. Requires every involved cluster to report
        /// <see cref="Proxy.IProxyCluster.SupportsTwoPhaseCommit"/>
        /// as <c>true</c>.
        /// </summary>
        TwoPhaseCommit = 1,

        /// <summary>
        /// Caller-driven saga: a sequence of
        /// <see cref="SagaStep"/> objects, each with a forward
        /// delegate and an idempotent compensation. The coordinator
        /// runs forwards in order, then runs compensations in
        /// reverse on failure. Used when at least one shard cannot
        /// prepare or when the caller explicitly opts into eventual
        /// consistency.
        /// </summary>
        Saga = 2,
    }
}
