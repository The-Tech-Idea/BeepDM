using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Services.Audit.Models;
using TheTechIdea.Beep.Services.Logging;

namespace TheTechIdea.Beep.Services.Telemetry
{
    /// <summary>
    /// Single envelope type carried from producer (BeepLog / BeepAudit) all
    /// the way through the queue, enrichers, redactors, samplers and sinks.
    /// One envelope class lets logs and audit share a single bounded queue
    /// without re-ordering relative to each other.
    /// </summary>
    /// <remarks>
    /// Mutable on purpose: enrichers (Phase 06) and redactors (Phase 05) edit
    /// the envelope in-place to avoid per-event allocations on the hot path.
    /// Sinks must treat the envelope as read-only once they receive it.
    /// </remarks>
    public sealed partial class TelemetryEnvelope
    {
        /// <summary>Discriminator: log, audit, or self-observability.</summary>
        public TelemetryKind Kind { get; set; }

        /// <summary>UTC timestamp captured by the producer.</summary>
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

        /// <summary>Logical category (component / feature). Optional for audit.</summary>
        public string Category { get; set; }

        /// <summary>Severity. Audit envelopes always use <see cref="BeepLogLevel.Information"/>.</summary>
        public BeepLogLevel Level { get; set; }

        /// <summary>Human-readable message; may be a template.</summary>
        public string Message { get; set; }

        /// <summary>Attached exception, when applicable.</summary>
        public Exception Exception { get; set; }

        /// <summary>Free-form structured property bag.</summary>
        public IDictionary<string, object> Properties { get; set; }

        /// <summary>
        /// Audit payload. Non-null exactly when <see cref="Kind"/> is
        /// <see cref="TelemetryKind.Audit"/>.
        /// </summary>
        public AuditEvent Audit { get; set; }

        /// <summary>OpenTelemetry-compatible trace id (Phase 06 enriches).</summary>
        public string TraceId { get; set; }

        /// <summary>Cross-component correlation id (Phase 06 enriches).</summary>
        public string CorrelationId { get; set; }
    }
}
