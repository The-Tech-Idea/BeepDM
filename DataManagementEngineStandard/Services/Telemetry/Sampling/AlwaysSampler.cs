namespace TheTechIdea.Beep.Services.Telemetry.Sampling
{
    /// <summary>
    /// Pinning helper that always returns <c>true</c>. Useful in tests and
    /// as an explicit "I deliberately disabled sampling" marker in
    /// configuration code.
    /// </summary>
    public sealed class AlwaysSampler : ISampler
    {
        /// <summary>Shared singleton instance.</summary>
        public static readonly AlwaysSampler Instance = new AlwaysSampler();

        /// <inheritdoc/>
        public string Name => "always";

        /// <inheritdoc/>
        public bool ShouldSample(TelemetryEnvelope envelope) => true;
    }
}
