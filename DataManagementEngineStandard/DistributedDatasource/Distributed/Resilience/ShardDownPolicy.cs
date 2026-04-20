namespace TheTechIdea.Beep.Distributed.Resilience
{
    /// <summary>
    /// Strategy the distribution tier applies when a routed call lands
    /// on a shard that is currently classified as unhealthy
    /// (Phase 10). Each
    /// <see cref="Plan.DistributionMode"/> can map to a different
    /// policy through
    /// <see cref="ShardDownPolicyOptions"/>.
    /// </summary>
    public enum ShardDownPolicy
    {
        /// <summary>
        /// Throw a <see cref="Routing.ShardRoutingException"/> (or the
        /// mode-appropriate error) immediately. Preferred for routed
        /// reads / writes where the data only lives on the target
        /// shard and no replica exists.
        /// </summary>
        FailFast = 0,

        /// <summary>
        /// Drop the unhealthy shard from the candidate set and
        /// continue. Used by replicated reads (pick another replica)
        /// and broadcast writes (raise
        /// <see cref="Events.PartialBroadcastEventArgs"/>, carry on
        /// with the survivors).
        /// </summary>
        SkipShard = 1,

        /// <summary>
        /// Route the call to the designated failover shard configured
        /// on <see cref="ShardDownPolicyOptions"/>. Typically used for
        /// routed reads against a read-through cache or warm replica
        /// set. Falls back to <see cref="FailFast"/> when no failover
        /// shard is configured or the failover shard is also
        /// unhealthy.
        /// </summary>
        UseFailover = 2,

        /// <summary>
        /// Only for sharded scatter reads — execute against the known
        /// healthy subset and accept the degraded view. The scatter
        /// entry gate still enforces
        /// <see cref="DistributedDataSourceOptions.MinimumHealthyShardRatio"/>.
        /// </summary>
        DegradeScatter = 3,
    }
}
