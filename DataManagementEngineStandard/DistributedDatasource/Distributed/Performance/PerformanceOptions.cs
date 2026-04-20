using System;

namespace TheTechIdea.Beep.Distributed.Performance
{
    /// <summary>
    /// Per-instance tuning record that controls Phase 14 capacity
    /// engineering: cross-shard parallelism caps, per-shard rate
    /// limits, adaptive timeouts, and hot-shard read-shedding.
    /// </summary>
    /// <remarks>
    /// <para>
    /// All caps default to safe, generous values so existing Phase 06
    /// / 07 / 13 deployments keep running unchanged; operators opt in
    /// to stricter limits as they collect production telemetry.
    /// </para>
    /// <para>
    /// Layering with <see cref="Proxy.ProxyCluster"/>: the
    /// distributed gate runs <em>before</em> any cluster-tier
    /// semaphore. Both must be satisfied for a call to proceed.
    /// </para>
    /// </remarks>
    public sealed class PerformanceOptions
    {
        /// <summary>
        /// Maximum simultaneous distribution-tier calls in flight
        /// across every entity and every caller. Acts as a global
        /// circuit breaker against runaway parallelism. Defaults to
        /// <c>256</c>; set to <c>0</c> to disable the gate entirely.
        /// </summary>
        public int MaxConcurrentDistributedCalls { get; set; } = 256;

        /// <summary>
        /// Maximum time a caller will wait to acquire the global
        /// distributed permit before <see cref="BackpressureException"/>
        /// is thrown. Defaults to 2 seconds.
        /// </summary>
        public TimeSpan DistributedPermitWait { get; set; } = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Maximum simultaneous calls routed to a single shard from
        /// the distribution tier. Defaults to <c>32</c>; set to
        /// <c>0</c> to disable the per-shard gate.
        /// </summary>
        public int MaxConcurrentCallsPerShard { get; set; } = 32;

        /// <summary>
        /// Maximum time a caller will wait to acquire a per-shard
        /// permit. Defaults to 1 second.
        /// </summary>
        public TimeSpan ShardPermitWait { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Token-bucket steady-state rate per shard (calls/sec).
        /// Defaults to <c>0</c> = rate-limiting disabled.
        /// </summary>
        public double ShardRateLimitPerSecond { get; set; } = 0;

        /// <summary>
        /// Token-bucket burst capacity per shard. Defaults to
        /// <c>32</c>; ignored when <see cref="ShardRateLimitPerSecond"/>
        /// is <c>0</c>.
        /// </summary>
        public int ShardRateLimitBurst { get; set; } = 32;

        /// <summary>
        /// Multiplier applied to a shard's observed p95 latency to
        /// produce an adaptive deadline (<c>DeadlineFactor * p95</c>).
        /// Defaults to <c>4.0</c>.
        /// </summary>
        public double AdaptiveDeadlineFactor { get; set; } = 4.0;

        /// <summary>
        /// Hard ceiling for adaptive deadlines, in milliseconds.
        /// Defaults to <c>60_000</c> (1 minute).
        /// </summary>
        public int MaxAdaptiveDeadlineMs { get; set; } = 60_000;

        /// <summary>
        /// Minimum adaptive deadline in milliseconds; applied even
        /// when p95 is zero or negligible. Defaults to <c>250</c> ms.
        /// </summary>
        public int MinAdaptiveDeadlineMs { get; set; } = 250;

        /// <summary>
        /// When <c>true</c> (default), reads against entities with
        /// replicated / broadcast placements skip shards flagged hot
        /// by the Phase 13 detector; single-shard reads are never
        /// shed (to avoid orphaning partitioned data).
        /// </summary>
        public bool EnableHotShardReadShedding { get; set; } = true;

        /// <summary>
        /// When <c>true</c> (default), executors call
        /// <see cref="Execution.IShardInvoker.AcquireDistributedCallPermit"/>
        /// and the per-shard acquire before dispatching. Set to
        /// <c>false</c> for tests that want to observe pre-Phase-14
        /// behaviour.
        /// </summary>
        public bool EnableCapacityGates { get; set; } = true;

        /// <summary>
        /// Returns a deep copy so runtime tweaks by one caller do
        /// not leak into other datasources.
        /// </summary>
        public PerformanceOptions Clone()
        {
            return new PerformanceOptions
            {
                MaxConcurrentDistributedCalls = MaxConcurrentDistributedCalls,
                DistributedPermitWait         = DistributedPermitWait,
                MaxConcurrentCallsPerShard    = MaxConcurrentCallsPerShard,
                ShardPermitWait               = ShardPermitWait,
                ShardRateLimitPerSecond       = ShardRateLimitPerSecond,
                ShardRateLimitBurst           = ShardRateLimitBurst,
                AdaptiveDeadlineFactor        = AdaptiveDeadlineFactor,
                MaxAdaptiveDeadlineMs         = MaxAdaptiveDeadlineMs,
                MinAdaptiveDeadlineMs         = MinAdaptiveDeadlineMs,
                EnableHotShardReadShedding    = EnableHotShardReadShedding,
                EnableCapacityGates           = EnableCapacityGates,
            };
        }
    }
}
