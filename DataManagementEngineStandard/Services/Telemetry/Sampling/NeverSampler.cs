namespace TheTechIdea.Beep.Services.Telemetry.Sampling
{
    /// <summary>
    /// Pinning helper that always returns <c>false</c>. Useful for
    /// blackholing a category temporarily without removing the rest of the
    /// pipeline configuration.
    /// </summary>
    public sealed class NeverSampler : ISampler
    {
        /// <summary>Shared singleton instance.</summary>
        public static readonly NeverSampler Instance = new NeverSampler();

        /// <inheritdoc/>
        public string Name => "never";

        /// <inheritdoc/>
        public bool ShouldSample(TelemetryEnvelope envelope) => false;
    }
}
