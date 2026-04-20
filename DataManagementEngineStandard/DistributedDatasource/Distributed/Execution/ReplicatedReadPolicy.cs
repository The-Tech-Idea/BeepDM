namespace TheTechIdea.Beep.Distributed.Execution
{
    /// <summary>
    /// How the Phase 06 read executor picks a single shard for a
    /// <see cref="DistributionMode.Replicated"/> read when the
    /// placement resolves to multiple live shards.
    /// </summary>
    /// <remarks>
    /// Load-aware / latency-aware variants are reserved for later
    /// phases; Phase 06 ships the two deterministic options below.
    /// Chosen via <see cref="DistributedDataSourceOptions.ReplicatedReadPolicy"/>.
    /// </remarks>
    public enum ReplicatedReadPolicy
    {
        /// <summary>
        /// Always pick the first live shard in the placement order.
        /// Deterministic — easiest to trace. Default.
        /// </summary>
        First = 0,

        /// <summary>
        /// Pick a live shard uniformly at random on every call.
        /// Distributes read load across replicas at the cost of
        /// trace determinism.
        /// </summary>
        Random = 1,
    }
}
