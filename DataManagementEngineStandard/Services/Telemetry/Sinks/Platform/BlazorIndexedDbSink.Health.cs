using System;
using System.Threading;
using TheTechIdea.Beep.Services.Telemetry.Diagnostics;

namespace TheTechIdea.Beep.Services.Telemetry.Sinks.Platform
{
    /// <summary>
    /// <see cref="ISinkHealthProbe"/> surface for
    /// <see cref="BlazorIndexedDbSink"/>. Tracks last success / error
    /// timestamps and consecutive failures so the
    /// <see cref="HealthAggregator"/> reports a meaningful status when
    /// IndexedDB throws (typically <c>QuotaExceededError</c> on tight
    /// browsers).
    /// </summary>
    public sealed partial class BlazorIndexedDbSink : ISinkHealthProbe
    {
        private long _lastSuccessTicks;
        private long _lastErrorTicks;
        private int _consecutiveFailures;

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
