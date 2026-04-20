using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TheTechIdea.Beep.Distributed.Resilience
{
    /// <summary>
    /// Thrown by the distribution tier when a scatter read, broadcast
    /// write, or cross-shard query would run below the configured
    /// <see cref="DistributedDataSourceOptions.MinimumHealthyShardRatio"/>
    /// (Phase 10). The caller receives this exception instead of
    /// partial results so the degraded view is never silently returned.
    /// </summary>
    [Serializable]
    public sealed class DegradedShardSetException : Exception
    {
        /// <summary>Creates a new degraded-shard-set exception.</summary>
        /// <param name="message">Human-readable explanation.</param>
        /// <param name="healthyShardIds">Shards classified as healthy when the exception was raised.</param>
        /// <param name="unhealthyShardIds">Shards classified as unhealthy when the exception was raised.</param>
        /// <param name="healthyRatio">Observed healthy ratio (0.0 to 1.0).</param>
        /// <param name="threshold">Configured healthy-ratio threshold.</param>
        public DegradedShardSetException(
            string                message,
            IReadOnlyList<string> healthyShardIds,
            IReadOnlyList<string> unhealthyShardIds,
            double                healthyRatio,
            double                threshold)
            : base(message ?? "Healthy shard ratio fell below the configured threshold.")
        {
            HealthyShardIds   = healthyShardIds   ?? Array.Empty<string>();
            UnhealthyShardIds = unhealthyShardIds ?? Array.Empty<string>();
            HealthyRatio      = Clamp(healthyRatio);
            Threshold         = Clamp(threshold);
        }

        /// <summary>Serialization ctor; preserved so callers can cross
        /// AppDomain boundaries without losing payload.</summary>
        private DegradedShardSetException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            HealthyShardIds   = Array.Empty<string>();
            UnhealthyShardIds = Array.Empty<string>();
            HealthyRatio      = info.GetDouble(nameof(HealthyRatio));
            Threshold         = info.GetDouble(nameof(Threshold));
        }

        /// <summary>Shards classified as healthy when the exception was raised.</summary>
        public IReadOnlyList<string> HealthyShardIds   { get; }

        /// <summary>Shards classified as unhealthy when the exception was raised.</summary>
        public IReadOnlyList<string> UnhealthyShardIds { get; }

        /// <summary>Observed healthy ratio.</summary>
        public double HealthyRatio { get; }

        /// <summary>Configured minimum healthy ratio.</summary>
        public double Threshold    { get; }

        /// <inheritdoc/>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            base.GetObjectData(info, context);
            info.AddValue(nameof(HealthyRatio), HealthyRatio);
            info.AddValue(nameof(Threshold),    Threshold);
        }

        private static double Clamp(double value)
        {
            if (double.IsNaN(value) || value < 0d) return 0d;
            if (value > 1d) return 1d;
            return value;
        }
    }
}
