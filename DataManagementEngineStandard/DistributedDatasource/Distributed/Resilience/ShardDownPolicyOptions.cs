using System;

namespace TheTechIdea.Beep.Distributed.Resilience
{
    /// <summary>
    /// Per-mode <see cref="ShardDownPolicy"/> configuration plus the
    /// distributed circuit-breaker tuning knobs (Phase 10). Owned by
    /// <see cref="DistributedDataSourceOptions"/>; may be swapped at
    /// runtime via
    /// <see cref="DistributedDataSource.ResilienceOptions"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Defaults are chosen to match the scope in the phase plan:
    /// </para>
    /// <list type="bullet">
    ///   <item><see cref="DistributionMode.Routed"/> → <see cref="ShardDownPolicy.FailFast"/>.</item>
    ///   <item><see cref="DistributionMode.Replicated"/> → <see cref="ShardDownPolicy.SkipShard"/>.</item>
    ///   <item><see cref="DistributionMode.Broadcast"/> → <see cref="ShardDownPolicy.SkipShard"/> (raises <see cref="Events.PartialBroadcastEventArgs"/>).</item>
    ///   <item><see cref="DistributionMode.Sharded"/> → <see cref="ShardDownPolicy.DegradeScatter"/>.</item>
    /// </list>
    /// <para>
    /// All values are plain POCO properties so the options instance can
    /// be serialized alongside the rest of
    /// <see cref="DistributedDataSourceOptions"/> without custom
    /// converters.
    /// </para>
    /// </remarks>
    public sealed class ShardDownPolicyOptions
    {
        /// <summary>Policy applied when a routed entity lands on an unhealthy shard.</summary>
        public ShardDownPolicy RoutedPolicy { get; set; } = ShardDownPolicy.FailFast;

        /// <summary>Policy applied when a replicated entity lands on an unhealthy shard.</summary>
        public ShardDownPolicy ReplicatedPolicy { get; set; } = ShardDownPolicy.SkipShard;

        /// <summary>Policy applied when a broadcast write targets an unhealthy shard.</summary>
        public ShardDownPolicy BroadcastPolicy { get; set; } = ShardDownPolicy.SkipShard;

        /// <summary>Policy applied when a sharded scatter hits unhealthy shards.</summary>
        public ShardDownPolicy ShardedPolicy { get; set; } = ShardDownPolicy.DegradeScatter;

        /// <summary>
        /// Shard id used by <see cref="ShardDownPolicy.UseFailover"/>.
        /// Ignored when the current policy is not
        /// <see cref="ShardDownPolicy.UseFailover"/> or when the
        /// failover shard itself is unhealthy (the resolver then falls
        /// back to <see cref="ShardDownPolicy.FailFast"/>).
        /// </summary>
        public string FailoverShardId { get; set; }

        /// <summary>
        /// Consecutive failures that trip the distributed circuit
        /// breaker for a shard. Defaults to 5; set to 0 to disable the
        /// distributed circuit (only the per-cluster breakers remain).
        /// </summary>
        public int CircuitFailureThreshold { get; set; } = 5;

        /// <summary>
        /// Time a tripped distributed circuit stays open before
        /// transitioning to half-open. Defaults to 30 seconds.
        /// </summary>
        public TimeSpan CircuitResetTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Consecutive successes required in half-open state before
        /// the distributed circuit closes again. Defaults to 2.
        /// </summary>
        public int CircuitSuccessThreshold { get; set; } = 2;

        /// <summary>
        /// Creates a deep copy of this options instance so consumers
        /// can hot-swap settings without affecting in-flight calls.
        /// </summary>
        public ShardDownPolicyOptions Clone()
            => new ShardDownPolicyOptions
            {
                RoutedPolicy            = RoutedPolicy,
                ReplicatedPolicy        = ReplicatedPolicy,
                BroadcastPolicy         = BroadcastPolicy,
                ShardedPolicy           = ShardedPolicy,
                FailoverShardId         = FailoverShardId,
                CircuitFailureThreshold = CircuitFailureThreshold,
                CircuitResetTimeout     = CircuitResetTimeout,
                CircuitSuccessThreshold = CircuitSuccessThreshold,
            };

        /// <summary>
        /// Resolves the policy to apply for a given
        /// <see cref="DistributionMode"/>.
        /// </summary>
        public ShardDownPolicy Resolve(DistributionMode mode)
        {
            switch (mode)
            {
                case DistributionMode.Routed:     return RoutedPolicy;
                case DistributionMode.Replicated: return ReplicatedPolicy;
                case DistributionMode.Broadcast:  return BroadcastPolicy;
                case DistributionMode.Sharded:    return ShardedPolicy;
                default:                          return ShardDownPolicy.FailFast;
            }
        }
    }
}
