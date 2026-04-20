using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Services.Telemetry
{
    /// <summary>
    /// Output stage for the unified telemetry pipeline. A sink receives a
    /// pre-batched, fully-enriched, fully-redacted slice of envelopes and
    /// is responsible for persisting or forwarding them.
    /// </summary>
    /// <remarks>
    /// Implementations must be safe to call from a single dedicated worker
    /// thread (the pipeline drains serially per sink) and must surface
    /// failure through <see cref="IsHealthy"/> so the pipeline can react.
    /// Per-call exceptions are caught by the pipeline; sinks should not rely
    /// on the pipeline to retry.
    /// </remarks>
    public interface ITelemetrySink : IAsyncDisposable
    {
        /// <summary>Stable, human-readable sink identifier (e.g. <c>file</c>, <c>sqlite</c>).</summary>
        string Name { get; }

        /// <summary>
        /// Snapshot of sink health. The pipeline reads this between batches
        /// and may emit a self-observability event (Phase 11) when it flips
        /// from <c>true</c> to <c>false</c>.
        /// </summary>
        bool IsHealthy { get; }

        /// <summary>
        /// Persists or forwards a batch of envelopes. Implementations may
        /// throw; the pipeline isolates the failure to this sink.
        /// </summary>
        Task WriteBatchAsync(
            IReadOnlyList<TelemetryEnvelope> batch,
            CancellationToken cancellationToken);

        /// <summary>
        /// Flushes any sink-side buffer (e.g. fsync the underlying writer).
        /// Called from <c>FlushAsync</c> on the pipeline.
        /// </summary>
        Task FlushAsync(CancellationToken cancellationToken);
    }
}
