namespace TheTechIdea.Beep.Services.Telemetry
{
    /// <summary>
    /// Pipeline stage that decides whether a log envelope is forwarded or
    /// dropped. Audit envelopes always bypass sampling. Forward-declared in
    /// Phase 02; concrete samplers ship in Phase 07.
    /// </summary>
    /// <remarks>
    /// Samplers run after enrichment and redaction but before enqueue so a
    /// dropped envelope never consumes queue capacity or sink work. Returning
    /// <c>false</c> drops the envelope; returning <c>true</c> keeps it.
    /// </remarks>
    public interface ISampler
    {
        /// <summary>Stable sampler name for diagnostics and ordering.</summary>
        string Name { get; }

        /// <summary>
        /// Returns <c>true</c> when the envelope should continue through the
        /// pipeline. Implementations must be deterministic across the same
        /// envelope to keep sampling reproducible.
        /// </summary>
        bool ShouldSample(TelemetryEnvelope envelope);
    }
}
