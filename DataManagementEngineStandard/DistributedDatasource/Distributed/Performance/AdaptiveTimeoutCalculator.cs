using System;
using TheTechIdea.Beep.Distributed.Observability;

namespace TheTechIdea.Beep.Distributed.Performance
{
    /// <summary>
    /// Produces a per-call deadline from a shard's observed p95
    /// latency so a slow shard gets its timeout shortened (fail
    /// fast) while a healthy shard keeps its full budget.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Formula: <c>deadline = clamp(factor * p95, minDeadline, maxDeadline)</c>
    /// where <c>factor</c> / <c>min</c> / <c>max</c> come from
    /// <see cref="PerformanceOptions"/>. When no metrics are
    /// available the calculator returns the supplied fallback so
    /// callers keep their pre-Phase-14 behaviour.
    /// </para>
    /// <para>
    /// The calculator is intentionally stateless apart from the
    /// metrics aggregator reference; it re-reads p95 on every call
    /// so plan swaps / aggregator replacements take effect
    /// immediately.
    /// </para>
    /// </remarks>
    public sealed class AdaptiveTimeoutCalculator
    {
        private readonly Func<IDistributedMetricsAggregator> _aggregatorAccessor;
        private readonly PerformanceOptions                  _options;

        /// <summary>Creates a new calculator.</summary>
        /// <param name="aggregatorAccessor">Delegate that returns the live aggregator (never cached so hot-swaps take effect).</param>
        /// <param name="options">Performance options; must not be <c>null</c>.</param>
        public AdaptiveTimeoutCalculator(
            Func<IDistributedMetricsAggregator> aggregatorAccessor,
            PerformanceOptions                  options)
        {
            _aggregatorAccessor = aggregatorAccessor ?? throw new ArgumentNullException(nameof(aggregatorAccessor));
            _options            = options            ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Computes the deadline for a single-shard call against
        /// <paramref name="shardId"/>. Falls back to
        /// <paramref name="fallbackMs"/> when the shard has no p95
        /// sample yet.
        /// </summary>
        public int ComputeDeadlineMs(string shardId, int fallbackMs)
        {
            if (string.IsNullOrWhiteSpace(shardId) || fallbackMs <= 0)
            {
                return fallbackMs <= 0 ? _options.MaxAdaptiveDeadlineMs : fallbackMs;
            }

            double p95 = TryGetShardP95(shardId);
            if (p95 <= 0)
            {
                return Clamp(fallbackMs);
            }

            double adaptive = p95 * _options.AdaptiveDeadlineFactor;
            // Blend the adaptive deadline with the caller's fallback
            // so we never shorten below what the caller explicitly
            // requested (fallback acts as a floor).
            int floor = Math.Min(fallbackMs, _options.MaxAdaptiveDeadlineMs);
            return Clamp((int)Math.Round(Math.Max(floor, adaptive)));
        }

        private int Clamp(int ms)
        {
            if (ms < _options.MinAdaptiveDeadlineMs) ms = _options.MinAdaptiveDeadlineMs;
            if (ms > _options.MaxAdaptiveDeadlineMs) ms = _options.MaxAdaptiveDeadlineMs;
            return ms;
        }

        private double TryGetShardP95(string shardId)
        {
            try
            {
                var aggregator = _aggregatorAccessor();
                var snapshot   = aggregator?.Snapshot();
                if (snapshot == null) return 0;
                if (!snapshot.PerShard.TryGetValue(shardId, out var shard) || shard == null) return 0;
                return shard.P95LatencyMs;
            }
            catch
            {
                return 0;
            }
        }
    }
}
