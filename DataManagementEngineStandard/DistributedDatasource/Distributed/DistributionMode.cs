namespace TheTechIdea.Beep.Distributed
{
    /// <summary>
    /// Per-entity placement / replication strategy used by
    /// <see cref="DistributedDataSource"/>. The mode is recorded inside the
    /// <c>EntityPlacement</c> attached to a <c>DistributionPlan</c> in
    /// Phase 02; the router and executor partials introduced in later
    /// phases consult the mode to decide whether to route to a single
    /// shard, scatter, fan-out, or broadcast.
    /// </summary>
    /// <remarks>
    /// New modes must be appended to keep persisted plans backward
    /// compatible. Numeric values are explicit so a stored plan from an
    /// earlier version always resolves to the same mode after upgrade.
    /// </remarks>
    public enum DistributionMode
    {
        /// <summary>
        /// Entity-level placement: the entire entity lives on exactly one
        /// shard. Reads and writes are routed to that shard; no row-level
        /// partitioning is performed.
        /// </summary>
        Routed = 0,

        /// <summary>
        /// Row-level sharding: rows of the entity are split across N shards
        /// via the entity's partition function (Phase 04). Point reads use
        /// the partition key; range/scatter reads visit the candidate
        /// shards reported by the partition function.
        /// </summary>
        Sharded = 1,

        /// <summary>
        /// Replicated: the entity exists on every shard. Writes fan out to
        /// all shards (Phase 07 quorum policy), reads can be served by any
        /// healthy shard.
        /// </summary>
        Replicated = 2,

        /// <summary>
        /// Broadcast (reference data): reads can be served by any shard,
        /// writes go to every shard. Distinguished from
        /// <see cref="Replicated"/> only by the read policy: broadcast
        /// reads pick the closest healthy shard rather than respecting
        /// any consistency window.
        /// </summary>
        Broadcast = 3
    }
}
