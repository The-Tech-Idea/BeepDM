using System;
using System.Threading;
using TheTechIdea.Beep.Services.Telemetry.Diagnostics;

namespace TheTechIdea.Beep.Services.Telemetry.Sinks
{
    /// <summary>
    /// Phase 11 health-probe surface for <see cref="SqliteSink"/>. Pulls
    /// last-success / last-error timestamps from the existing write path
    /// without holding the write gate so the probe can run from any
    /// thread.
    /// </summary>
    public sealed partial class SqliteSink : ISinkHealthProbe
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

        private void RecordWriteSuccess()
        {
            Interlocked.Exchange(ref _lastSuccessTicks, DateTime.UtcNow.Ticks);
            Interlocked.Exchange(ref _consecutiveFailures, 0);
        }

        private void RecordWriteError()
        {
            Interlocked.Exchange(ref _lastErrorTicks, DateTime.UtcNow.Ticks);
            Interlocked.Increment(ref _consecutiveFailures);
        }
    }
}
