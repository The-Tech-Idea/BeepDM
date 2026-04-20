using System;
using System.Threading;
using TheTechIdea.Beep.Services.Telemetry.Diagnostics;

namespace TheTechIdea.Beep.Services.Telemetry.Sinks
{
    /// <summary>
    /// Phase 11 health-probe surface for <see cref="FileRollingSink"/>.
    /// Exposes the existing healthy / last-error / written-count fields
    /// plus per-write timestamps so <see cref="HealthAggregator"/> can
    /// report richer self-observability data without changing the core
    /// write path.
    /// </summary>
    public sealed partial class FileRollingSink : ISinkHealthProbe
    {
        private long _lastSuccessTicks;
        private long _lastErrorTicks;
        private int _consecutiveFailures;

        /// <inheritdoc />
        SinkHealth ISinkHealthProbe.Probe()
        {
            long successTicks = Interlocked.Read(ref _lastSuccessTicks);
            long errorTicks = Interlocked.Read(ref _lastErrorTicks);
            return new SinkHealth
            {
                Name = Name,
                IsHealthy = IsHealthy,
                LastSuccessUtc = successTicks > 0 ? new DateTime(successTicks, DateTimeKind.Utc) : null,
                LastErrorUtc = errorTicks > 0 ? new DateTime(errorTicks, DateTimeKind.Utc) : null,
                LastError = LastError,
                WrittenCount = WrittenCount,
                ConsecutiveFailures = Volatile.Read(ref _consecutiveFailures)
            };
        }

        private void RecordSuccess()
        {
            Interlocked.Exchange(ref _lastSuccessTicks, DateTime.UtcNow.Ticks);
            Interlocked.Exchange(ref _consecutiveFailures, 0);
        }

        private void RecordError()
        {
            Interlocked.Exchange(ref _lastErrorTicks, DateTime.UtcNow.Ticks);
            Interlocked.Increment(ref _consecutiveFailures);
        }
    }
}
