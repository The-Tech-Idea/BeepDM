namespace TheTechIdea.Beep.Distributed.Execution
{
    /// <summary>
    /// How the Phase 06 read executor treats per-shard failures while
    /// it is fanning a single read out across multiple shards.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Chosen via <see cref="DistributedDataSourceOptions.ScatterFailurePolicy"/>.
    /// The policy only affects scatter reads (reads whose
    /// <see cref="Routing.RoutingDecision.IsScatter"/> or
    /// <see cref="Routing.RoutingDecision.IsFanOut"/> is <c>true</c>);
    /// single-shard reads always propagate the originating exception.
    /// </para>
    /// <para>
    /// Writes use the separate <see cref="Plan.EntityPlacement.WriteQuorum"/>
    /// / Phase 07 quorum policy and do not consult this enum.
    /// </para>
    /// </remarks>
    public enum ScatterFailurePolicy
    {
        /// <summary>
        /// Return whatever the healthy shards produced. Failed shards
        /// are logged via <see cref="IDataSource.PassEvent"/> but do
        /// not propagate. Default — safest for read-heavy workloads.
        /// </summary>
        BestEffort = 0,

        /// <summary>
        /// The first failure cancels the remaining shard calls and
        /// rethrows the originating exception. Use when a partial
        /// result is worse than no result.
        /// </summary>
        FailFast = 1,

        /// <summary>
        /// Every shard must succeed. A single failure aggregates
        /// every error into an <see cref="System.AggregateException"/>
        /// and throws. Use for reconciliation / audit reads.
        /// </summary>
        RequireAll = 2,
    }
}
