using System;

namespace TheTechIdea.Beep.Distributed.Observability
{
    /// <summary>
    /// Aggregates distribution-tier counters, latency samples, and
    /// shard-cluster metrics into a single
    /// <see cref="DistributionMetricsSnapshot"/>. Instances are
    /// thread-safe and designed to live for the full lifetime of
    /// a <see cref="DistributedDataSource"/>.
    /// </summary>
    /// <remarks>
    /// Implementations are free to retain rolling windows for
    /// latency percentiles and hot-shard detection; concrete
    /// <see cref="DistributedMetricsAggregator"/> keeps a bounded
    /// token-bucket-style window to keep memory flat even under
    /// sustained load.
    /// </remarks>
    public interface IDistributedMetricsAggregator
    {
        /// <summary>
        /// Records a completed request. Called by the read/write/DDL
        /// executors on the hot path; implementations MUST be
        /// allocation-cheap and MUST NOT throw.
        /// </summary>
        /// <param name="shardId">Shard that served the request.</param>
        /// <param name="entityName">Entity the request targeted.</param>
        /// <param name="mode">
        /// Distribution mode (<see cref="Plan.DistributionMode"/>)
        /// encoded as a string so callers can pass the enum's
        /// <c>ToString()</c> without referencing its numeric value.
        /// </param>
        /// <param name="latencyMs">
        /// Wall-clock latency observed at the distribution tier
        /// (includes routing, wait, and shard round-trip).
        /// </param>
        /// <param name="succeeded">
        /// <c>true</c> when the request completed successfully;
        /// <c>false</c> when the shard threw, timed out, or the
        /// scatter reducer raised a failure.
        /// </param>
        void Record(
            string shardId,
            string entityName,
            string mode,
            double latencyMs,
            bool   succeeded);

        /// <summary>
        /// Returns an immutable snapshot of current metrics.
        /// Callers commonly invoke this on a polling loop and
        /// diff successive snapshots to derive rates.
        /// </summary>
        DistributionMetricsSnapshot Snapshot();

        /// <summary>
        /// Raised by the aggregator (or its paired
        /// <see cref="HotShardDetector"/>) when a shard's p95
        /// latency exceeds the configured threshold for the
        /// configured number of consecutive windows. Subscribers
        /// typically quarantine the shard or page an operator.
        /// Handlers <strong>must not</strong> throw.
        /// </summary>
        event EventHandler<HotShardEventArgs> OnHotShardDetected;

        /// <summary>
        /// Raised when an entity's request rate spikes above the
        /// configured rate threshold — useful for catching sudden
        /// hot keys without needing shard-level access.
        /// </summary>
        event EventHandler<HotEntityEventArgs> OnHotEntityDetected;
    }

    /// <summary>
    /// Arguments describing a shard that breached its hot-shard
    /// latency threshold.
    /// </summary>
    public sealed class HotShardEventArgs : EventArgs
    {
        /// <summary>Creates a new hot-shard event-args instance.</summary>
        public HotShardEventArgs(
            string   shardId,
            double   p95LatencyMs,
            double   thresholdMs,
            int      consecutiveWindows,
            DateTime observedAtUtc)
        {
            ShardId            = shardId ?? string.Empty;
            P95LatencyMs       = p95LatencyMs;
            ThresholdMs        = thresholdMs;
            ConsecutiveWindows = consecutiveWindows;
            ObservedAtUtc      = observedAtUtc;
        }

        /// <summary>Shard whose p95 latency breached the threshold.</summary>
        public string   ShardId            { get; }
        /// <summary>Observed p95 latency (ms) for the latest window.</summary>
        public double   P95LatencyMs       { get; }
        /// <summary>Configured threshold (ms).</summary>
        public double   ThresholdMs        { get; }
        /// <summary>Number of consecutive windows that breached the threshold.</summary>
        public int      ConsecutiveWindows { get; }
        /// <summary>Detection timestamp (UTC).</summary>
        public DateTime ObservedAtUtc      { get; }
    }

    /// <summary>
    /// Arguments describing an entity whose request rate breached
    /// the hot-entity threshold.
    /// </summary>
    public sealed class HotEntityEventArgs : EventArgs
    {
        /// <summary>Creates a new hot-entity event-args instance.</summary>
        public HotEntityEventArgs(
            string   entityName,
            double   requestsPerSecond,
            double   thresholdRps,
            DateTime observedAtUtc)
        {
            EntityName        = entityName ?? string.Empty;
            RequestsPerSecond = requestsPerSecond;
            ThresholdRps      = thresholdRps;
            ObservedAtUtc     = observedAtUtc;
        }

        /// <summary>Entity whose request rate breached the threshold.</summary>
        public string   EntityName        { get; }
        /// <summary>Observed requests per second for the latest window.</summary>
        public double   RequestsPerSecond { get; }
        /// <summary>Configured threshold (RPS).</summary>
        public double   ThresholdRps      { get; }
        /// <summary>Detection timestamp (UTC).</summary>
        public DateTime ObservedAtUtc     { get; }
    }
}
