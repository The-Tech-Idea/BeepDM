using System;

namespace TheTechIdea.Beep.Services.Telemetry.Dedup
{
    /// <summary>
    /// Pre-queue stage that collapses near-identical log envelopes into a
    /// single representative envelope plus a periodic summary. Audit
    /// envelopes always bypass this stage by pipeline contract.
    /// </summary>
    /// <remarks>
    /// The pipeline supplies an emitter delegate via <see cref="Bind"/>;
    /// the deduper invokes it whenever a window expires and a synthetic
    /// summary needs to enter the queue. This keeps the deduper free of
    /// any pipeline coupling — making it trivially testable.
    /// </remarks>
    public interface IMessageDeduper
    {
        /// <summary>Stable name for diagnostics.</summary>
        string Name { get; }

        /// <summary>Total envelopes suppressed since startup.</summary>
        long SuppressedCount { get; }

        /// <summary>
        /// Wires the synthetic-summary emitter. Called once by the pipeline
        /// at construction. Implementations may store the delegate or wrap
        /// it in a no-op when null is supplied.
        /// </summary>
        void Bind(Action<TelemetryEnvelope> emitSummary);

        /// <summary>
        /// Returns <c>true</c> when the envelope should continue downstream
        /// or <c>false</c> when it has been folded into an existing window.
        /// May synchronously call the bound emitter to flush expired
        /// summaries before deciding on the current envelope.
        /// </summary>
        bool TryAccept(TelemetryEnvelope envelope);

        /// <summary>
        /// Forces emission of every expired window summary. Called by the
        /// pipeline drain loop and on shutdown so no suppressed envelope is
        /// silently lost.
        /// </summary>
        void DrainExpired();
    }
}
