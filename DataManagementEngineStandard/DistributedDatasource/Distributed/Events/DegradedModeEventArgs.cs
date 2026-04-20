using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Distributed.Events
{
    /// <summary>
    /// Raised when the distribution tier detects that the healthy
    /// shard ratio has fallen below
    /// <see cref="DistributedDataSourceOptions.MinimumHealthyShardRatio"/>
    /// (Phase 10). The datasource continues to honour single-shard
    /// routed calls that target a healthy shard, but scatter reads
    /// and broadcast writes are throttled or rejected until the ratio
    /// recovers.
    /// </summary>
    public sealed class DegradedModeEventArgs : EventArgs
    {
        /// <summary>Creates a new degraded-mode event.</summary>
        /// <param name="healthyShardIds">Snapshot of currently healthy shard ids. Must not be <c>null</c>.</param>
        /// <param name="unhealthyShardIds">Snapshot of currently unhealthy shard ids. Must not be <c>null</c>.</param>
        /// <param name="healthyRatio">Healthy ratio observed when the event was raised, in [0.0, 1.0].</param>
        /// <param name="threshold">Configured <see cref="DistributedDataSourceOptions.MinimumHealthyShardRatio"/>.</param>
        /// <param name="reason">Optional human-readable reason (e.g. "scatter aborted", "broadcast write throttled").</param>
        public DegradedModeEventArgs(
            IReadOnlyList<string> healthyShardIds,
            IReadOnlyList<string> unhealthyShardIds,
            double                healthyRatio,
            double                threshold,
            string                reason)
        {
            HealthyShardIds   = healthyShardIds   ?? throw new ArgumentNullException(nameof(healthyShardIds));
            UnhealthyShardIds = unhealthyShardIds ?? throw new ArgumentNullException(nameof(unhealthyShardIds));
            HealthyRatio      = ClampRatio(healthyRatio);
            Threshold         = ClampRatio(threshold);
            Reason            = reason ?? string.Empty;
            TimestampUtc      = DateTime.UtcNow;
        }

        /// <summary>Shards classified as healthy at the moment of the event.</summary>
        public IReadOnlyList<string> HealthyShardIds   { get; }

        /// <summary>Shards classified as unhealthy at the moment of the event.</summary>
        public IReadOnlyList<string> UnhealthyShardIds { get; }

        /// <summary>Healthy ratio observed (0.0 to 1.0 inclusive).</summary>
        public double HealthyRatio { get; }

        /// <summary>Configured minimum healthy ratio.</summary>
        public double Threshold    { get; }

        /// <summary>Optional reason captured by the raiser.</summary>
        public string Reason       { get; }

        /// <summary>UTC timestamp the event was raised.</summary>
        public DateTime TimestampUtc { get; }

        /// <inheritdoc/>
        public override string ToString()
            => $"DegradedMode(healthy={HealthyShardIds.Count}, unhealthy={UnhealthyShardIds.Count}, " +
               $"ratio={HealthyRatio:F2}, threshold={Threshold:F2}, reason='{Reason}', at={TimestampUtc:O})";

        private static double ClampRatio(double value)
        {
            if (double.IsNaN(value) || value < 0d) return 0d;
            if (value > 1d) return 1d;
            return value;
        }
    }
}
