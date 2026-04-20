using System;

namespace TheTechIdea.Beep.Services.Telemetry.RateLimit
{
    /// <summary>
    /// Pre-queue stage that enforces a per-key throughput cap on log
    /// envelopes. Audit envelopes always bypass this stage by pipeline
    /// contract.
    /// </summary>
    /// <remarks>
    /// The rate limiter is the final pre-queue gate. Implementations are
    /// expected to be thread-safe and to update internal counters under a
    /// lock no broader than per-key.
    /// </remarks>
    public interface IRateLimiter
    {
        /// <summary>Stable name for diagnostics.</summary>
        string Name { get; }

        /// <summary>Total envelopes rejected since startup.</summary>
        long DroppedCount { get; }

        /// <summary>
        /// Wires the synthetic-summary emitter so the limiter can publish
        /// periodic <c>"[rate-limited]"</c> envelopes describing recent
        /// drops. Called once by the pipeline at construction.
        /// </summary>
        void Bind(Action<TelemetryEnvelope> emitSummary);

        /// <summary>
        /// Returns <c>true</c> when the envelope fits inside the bucket
        /// for its key (and consumes one token); returns <c>false</c> when
        /// the bucket is empty.
        /// </summary>
        bool TryAcquire(TelemetryEnvelope envelope);

        /// <summary>
        /// Forces emission of any pending rate-limit summary. Called by
        /// the pipeline drain loop and on shutdown.
        /// </summary>
        void DrainSummaries();
    }
}
