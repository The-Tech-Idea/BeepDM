namespace TheTechIdea.Beep.Services.Telemetry
{
    /// <summary>
    /// Discriminator carried on every <see cref="TelemetryEnvelope"/> so the
    /// pipeline (and individual sinks) can tell logs from audit and apply
    /// the correct policy. Audit envelopes bypass samplers and never use
    /// <c>DropOldest</c> backpressure.
    /// </summary>
    public enum TelemetryKind
    {
        /// <summary>Diagnostic log entry (lossy by default).</summary>
        Log = 0,

        /// <summary>Audit-trail event (lossless by policy).</summary>
        Audit = 1,

        /// <summary>
        /// Pipeline self-observability event (e.g. drop counter, sink error).
        /// Reserved for Phase 11; Phase 02 does not emit these.
        /// </summary>
        Self = 2
    }
}
