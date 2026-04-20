namespace TheTechIdea.Beep.Distributed.Execution
{
    /// <summary>
    /// How the Phase 07 write executor decides whether a multi-shard
    /// write succeeded. Applied to <see cref="DistributionMode.Replicated"/>
    /// and <see cref="DistributionMode.Broadcast"/> writes; single-shard
    /// sharded writes always require <c>All</c> (the one shard must ack).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The effective quorum numbers are derived from the
    /// <see cref="Plan.EntityPlacement.WriteQuorum"/> in the active
    /// plan, but callers may override per-call via
    /// <see cref="DistributedWriteOptions.QuorumOverride"/>.
    /// </para>
    /// <para>
    /// Quorum checks the count of shards that succeeded; partial
    /// failures are reported via
    /// <see cref="Events.PartialReplicationFailureEventArgs"/>
    /// so operators can reconcile the divergent replicas.
    /// </para>
    /// </remarks>
    public enum QuorumPolicy
    {
        /// <summary>
        /// Every target shard must ack the write. The safest / default
        /// policy when the plan does not explicitly set a quorum.
        /// </summary>
        All = 0,

        /// <summary>
        /// A simple majority (<c>&gt; N/2</c>) of target shards must ack.
        /// Suitable for odd replication factors (3, 5, 7).
        /// </summary>
        Majority = 1,

        /// <summary>
        /// At least <see cref="DistributedWriteOptions.AtLeastN"/>
        /// target shards must ack. Defaults to
        /// <see cref="Plan.EntityPlacement.WriteQuorum"/> when not
        /// overridden. Use when the placement explicitly encodes the
        /// required ack count (e.g. <c>2</c> of <c>3</c>).
        /// </summary>
        AtLeastN = 2,
    }
}
