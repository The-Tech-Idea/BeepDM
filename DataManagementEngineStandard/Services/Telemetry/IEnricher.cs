namespace TheTechIdea.Beep.Services.Telemetry
{
    /// <summary>
    /// Pipeline stage that augments each <see cref="TelemetryEnvelope"/>
    /// with cross-cutting context (correlation ids, machine name, user id,
    /// activity span info, etc.). Forward-declared in Phase 02; concrete
    /// enrichers ship in Phase 06.
    /// </summary>
    /// <remarks>
    /// Enrichers run on the producer thread (before enqueue) so they must
    /// be cheap, allocation-light, and never block on IO. Implementations
    /// mutate the supplied envelope in place rather than returning a copy.
    /// </remarks>
    public interface IEnricher
    {
        /// <summary>Stable enricher name for diagnostics and ordering.</summary>
        string Name { get; }

        /// <summary>
        /// Augments the envelope. Must not throw on null
        /// <c>envelope.Properties</c> — initialize when needed.
        /// </summary>
        void Enrich(TelemetryEnvelope envelope);
    }
}
