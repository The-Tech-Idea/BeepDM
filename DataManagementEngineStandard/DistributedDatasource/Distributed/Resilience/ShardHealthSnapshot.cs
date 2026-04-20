using System;

namespace TheTechIdea.Beep.Distributed.Resilience
{
    /// <summary>
    /// Immutable point-in-time view of a single shard's health as seen
    /// by the distribution tier. Produced by
    /// <see cref="IShardHealthMonitor"/> and consumed by the read /
    /// write executors plus the resilience event pipeline (Phase 10).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Per-node HA (connection retries, replica fail-over, per-node
    /// circuits) belongs to <see cref="Proxy.IProxyCluster"/> and is
    /// never duplicated here. This snapshot describes the cluster as a
    /// whole — if the cluster itself is unreachable or every node has
    /// tripped its circuit, the snapshot is marked unhealthy so the
    /// distributed tier can apply a
    /// <see cref="ShardDownPolicy"/> decision for the affected shard.
    /// </para>
    /// <para>
    /// All values are captured at the moment the snapshot was built;
    /// subscribers must re-fetch via
    /// <see cref="IShardHealthMonitor.GetSnapshot(string)"/> rather
    /// than caching the instance.
    /// </para>
    /// </remarks>
    public sealed class ShardHealthSnapshot
    {
        /// <summary>Creates a new immutable health snapshot.</summary>
        /// <param name="shardId">Shard id this snapshot describes. Required.</param>
        /// <param name="isHealthy">True when the shard is considered usable.</param>
        /// <param name="consecutiveFailures">Number of consecutive failures recorded since the last observed success.</param>
        /// <param name="averageLatencyMs">Weighted average latency (milliseconds) reported by the underlying cluster. 0 when unknown.</param>
        /// <param name="lastCheckedUtc">UTC timestamp when the health check last ran.</param>
        /// <param name="lastSuccessUtc">UTC timestamp of the last successful observation. <c>null</c> when never observed.</param>
        /// <param name="reason">Optional human-readable reason (e.g. "cluster report unhealthy", "circuit open").</param>
        public ShardHealthSnapshot(
            string    shardId,
            bool      isHealthy,
            int       consecutiveFailures,
            double    averageLatencyMs,
            DateTime  lastCheckedUtc,
            DateTime? lastSuccessUtc,
            string    reason)
        {
            if (string.IsNullOrWhiteSpace(shardId))
                throw new ArgumentException("Shard id cannot be null or whitespace.", nameof(shardId));

            ShardId             = shardId;
            IsHealthy           = isHealthy;
            ConsecutiveFailures = Math.Max(0, consecutiveFailures);
            AverageLatencyMs    = averageLatencyMs < 0 ? 0 : averageLatencyMs;
            LastCheckedUtc      = lastCheckedUtc;
            LastSuccessUtc      = lastSuccessUtc;
            Reason              = reason ?? string.Empty;
        }

        /// <summary>Shard id this snapshot describes.</summary>
        public string   ShardId             { get; }

        /// <summary>True when the shard is considered usable by the distribution tier.</summary>
        public bool     IsHealthy           { get; }

        /// <summary>Consecutive failure count since the last observed success.</summary>
        public int      ConsecutiveFailures { get; }

        /// <summary>Weighted average latency (milliseconds); <c>0</c> when not reported.</summary>
        public double   AverageLatencyMs    { get; }

        /// <summary>UTC timestamp when the health check last ran.</summary>
        public DateTime LastCheckedUtc      { get; }

        /// <summary>UTC timestamp of the last successful observation. <c>null</c> when never observed.</summary>
        public DateTime? LastSuccessUtc     { get; }

        /// <summary>Optional human-readable reason attached to the snapshot.</summary>
        public string   Reason              { get; }

        /// <summary>
        /// Builds a canonical "unknown" snapshot for a freshly
        /// registered shard. The shard is treated as healthy (fail-open)
        /// so the first call is allowed through — subsequent failures
        /// trip the monitor.
        /// </summary>
        /// <param name="shardId">Shard id. Required.</param>
        /// <param name="nowUtc">Anchor timestamp; typically <see cref="DateTime.UtcNow"/>.</param>
        public static ShardHealthSnapshot NewlyRegistered(string shardId, DateTime nowUtc)
            => new ShardHealthSnapshot(
                shardId:             shardId,
                isHealthy:           true,
                consecutiveFailures: 0,
                averageLatencyMs:    0,
                lastCheckedUtc:      nowUtc,
                lastSuccessUtc:      null,
                reason:              "newly-registered");

        /// <inheritdoc/>
        public override string ToString()
            => $"{ShardId}: healthy={IsHealthy}, fails={ConsecutiveFailures}, " +
               $"avg={AverageLatencyMs:F1}ms, checked={LastCheckedUtc:O}, reason='{Reason}'";
    }
}
