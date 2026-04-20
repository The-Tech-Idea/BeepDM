using System;

namespace TheTechIdea.Beep.Services.Telemetry.Sampling
{
    /// <summary>
    /// Applies a per-category keep rate. Envelopes whose
    /// <see cref="TelemetryEnvelope.Category"/> matches the configured
    /// category (case-insensitive Ordinal) are sampled; all others pass
    /// through.
    /// </summary>
    /// <remarks>
    /// Use to throttle a single noisy component (e.g. a tight inner loop
    /// tagged <c>"HotLoop"</c>) without affecting the rest of the
    /// application's logging volume.
    /// </remarks>
    public sealed class CategorySampler : ISampler
    {
        private readonly string _category;
        private readonly double _rate;

        /// <summary>Creates a category sampler.</summary>
        /// <param name="category">Category name to throttle (must be non-empty).</param>
        /// <param name="rate">Keep rate in <c>[0, 1]</c>. Clamped if outside.</param>
        public CategorySampler(string category, double rate)
        {
            if (string.IsNullOrEmpty(category))
            {
                throw new ArgumentException("Category must be non-empty.", nameof(category));
            }
            _category = category;
            _rate = rate < 0.0 ? 0.0 : (rate > 1.0 ? 1.0 : rate);
        }

        /// <summary>The throttled category.</summary>
        public string Category => _category;

        /// <summary>The keep rate applied to matching envelopes.</summary>
        public double Rate => _rate;

        /// <inheritdoc/>
        public string Name => "category";

        /// <inheritdoc/>
        public bool ShouldSample(TelemetryEnvelope envelope)
        {
            if (envelope is null)
            {
                return false;
            }
            if (!string.Equals(envelope.Category, _category, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            return SamplingDecision.Keep(envelope, _rate);
        }
    }
}
