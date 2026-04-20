using System;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Services.Telemetry
{
    /// <summary>
    /// Lifetime / flush half of <see cref="TelemetryPipeline"/>. Owns the
    /// cooperative shutdown contract callers see through
    /// <see cref="TheTechIdea.Beep.Services.Logging.IBeepLog.FlushAsync"/> and
    /// <see cref="TheTechIdea.Beep.Services.Audit.IBeepAudit.FlushAsync"/>.
    /// </summary>
    public sealed partial class TelemetryPipeline
    {
        private int _disposed;

        /// <summary>
        /// Drains the queue and asks every sink to flush within the supplied
        /// <paramref name="timeout"/>. Safe to call repeatedly.
        /// </summary>
        public Task FlushAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                return Task.CompletedTask;
            }
            DrainPreQueueStages();
            return _writer.FlushAsync(timeout, cancellationToken);
        }

        /// <summary>
        /// Disposes the pipeline. Completes the queue, cancels the drain
        /// loop, awaits its shutdown, then disposes every sink.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }
            DrainPreQueueStages();
            await _writer.DisposeAsync().ConfigureAwait(false);
        }

        private void DrainPreQueueStages()
        {
            try
            {
                _deduper?.DrainExpired();
            }
            catch
            {
                // Synthetic emission must not break shutdown.
            }
            try
            {
                _rateLimiter?.DrainSummaries();
            }
            catch
            {
                // Same isolation rule as the deduper drain.
            }
        }
    }
}
