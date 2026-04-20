namespace TheTechIdea.Beep.Services.Telemetry
{
    /// <summary>
    /// Pipeline stage that removes or masks sensitive content
    /// (PII, secrets, connection strings) from each
    /// <see cref="TelemetryEnvelope"/>. Forward-declared in Phase 02;
    /// concrete redactors ship in Phase 05.
    /// </summary>
    /// <remarks>
    /// Redactors run after enrichers and before sampling/enqueue so all
    /// downstream stages and sinks see redacted content only. Implementations
    /// must edit the envelope in place to keep the hot path allocation-free.
    /// </remarks>
    public interface IRedactor
    {
        /// <summary>Stable redactor name for diagnostics and ordering.</summary>
        string Name { get; }

        /// <summary>Redacts sensitive data from the supplied envelope.</summary>
        void Redact(TelemetryEnvelope envelope);
    }
}
