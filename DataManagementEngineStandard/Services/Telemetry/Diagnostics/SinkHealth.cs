using System;

namespace TheTechIdea.Beep.Services.Telemetry.Diagnostics
{
    /// <summary>
    /// Frozen point-in-time view of a single sink's health. Returned by
    /// <see cref="ISinkHealthProbe.Probe"/> and aggregated by
    /// <see cref="HealthAggregator"/>.
    /// </summary>
    /// <remarks>
    /// The struct surface is intentionally tiny: just enough to drive a
    /// dashboard or an exporter. Every property is set during construction
    /// so the snapshot is safe to share across threads without locking.
    /// </remarks>
    public sealed class SinkHealth
    {
        /// <summary>Stable sink name (mirrors <see cref="ITelemetrySink.Name"/>).</summary>
        public string Name { get; init; }

        /// <summary>True when the sink reports itself healthy.</summary>
        public bool IsHealthy { get; init; }

        /// <summary>UTC timestamp of the most recent successful write.</summary>
        public DateTime? LastSuccessUtc { get; init; }

        /// <summary>UTC timestamp of the most recent error.</summary>
        public DateTime? LastErrorUtc { get; init; }

        /// <summary>Human-readable description of the most recent error.</summary>
        public string LastError { get; init; }

        /// <summary>Total writes succeeded since startup.</summary>
        public long WrittenCount { get; init; }

        /// <summary>Consecutive failures without an intervening success.</summary>
        public int ConsecutiveFailures { get; init; }

        /// <summary>
        /// Builds a probe view from a bare <see cref="ITelemetrySink"/>.
        /// Used by <see cref="HealthAggregator"/> when a sink does not
        /// implement <see cref="ISinkHealthProbe"/> directly.
        /// </summary>
        public static SinkHealth FromBareSink(ITelemetrySink sink)
        {
            if (sink is null)
            {
                return new SinkHealth { Name = "(null)", IsHealthy = false };
            }
            return new SinkHealth
            {
                Name = sink.Name,
                IsHealthy = sink.IsHealthy
            };
        }
    }
}
