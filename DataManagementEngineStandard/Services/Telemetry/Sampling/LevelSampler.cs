using TheTechIdea.Beep.Services.Logging;

namespace TheTechIdea.Beep.Services.Telemetry.Sampling
{
    /// <summary>
    /// Drops a configurable fraction of envelopes <strong>at or below</strong>
    /// a threshold level; envelopes more severe than the threshold are
    /// always kept. Use to thin out chatty <c>Trace</c>/<c>Debug</c> traffic
    /// without ever dropping warnings or errors.
    /// </summary>
    /// <remarks>
    /// Decision is deterministic per <c>correlationId</c>/<c>traceId</c> so
    /// either every envelope of a given trace is sampled in or none is
    /// (avoids sparse, half-correlated traces). Audit envelopes never reach
    /// any sampler — see the pipeline guard.
    /// </remarks>
    public sealed class LevelSampler : ISampler
    {
        private readonly BeepLogLevel _threshold;
        private readonly double _rate;

        /// <summary>
        /// Creates a level sampler. <paramref name="rate"/> is clamped to
        /// <c>[0, 1]</c>; <c>1.0</c> is a no-op (everything kept) and
        /// <c>0.0</c> drops all envelopes at/below the threshold.
        /// </summary>
        public LevelSampler(BeepLogLevel threshold, double rate)
        {
            _threshold = threshold;
            _rate = rate < 0.0 ? 0.0 : (rate > 1.0 ? 1.0 : rate);
        }

        /// <summary>The level at or below which sampling applies.</summary>
        public BeepLogLevel Threshold => _threshold;

        /// <summary>The keep rate applied to envelopes at/below the threshold.</summary>
        public double Rate => _rate;

        /// <inheritdoc/>
        public string Name => "level";

        /// <inheritdoc/>
        public bool ShouldSample(TelemetryEnvelope envelope)
        {
            if (envelope is null)
            {
                return false;
            }
            if (envelope.Level > _threshold)
            {
                return true;
            }
            return SamplingDecision.Keep(envelope, _rate);
        }
    }
}
