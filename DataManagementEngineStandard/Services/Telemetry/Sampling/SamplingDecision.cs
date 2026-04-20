using System;
using System.Threading;

namespace TheTechIdea.Beep.Services.Telemetry.Sampling
{
    /// <summary>
    /// Shared rate-comparison primitive used by every built-in sampler.
    /// Deterministic when an envelope carries a non-empty correlation/trace
    /// id (a single trace stays whole or wholly dropped); falls back to a
    /// thread-local <see cref="Random"/> otherwise.
    /// </summary>
    /// <remarks>
    /// The deterministic path uses FNV-1a over the correlation id rather
    /// than <see cref="string.GetHashCode"/> because the latter is salted
    /// per-process on modern .NET — that would make sampling decisions
    /// non-reproducible across host restarts and break replay-style
    /// debugging.
    /// </remarks>
    internal static class SamplingDecision
    {
        private const uint FnvOffset = 2166136261u;
        private const uint FnvPrime = 16777619u;

        private static readonly ThreadLocal<Random> RandomLocal =
            new ThreadLocal<Random>(() => new Random(unchecked(Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId)));

        /// <summary>
        /// Returns <c>true</c> for envelopes that should be kept given the
        /// supplied <paramref name="rate"/> (clamped to <c>[0, 1]</c>).
        /// </summary>
        public static bool Keep(TelemetryEnvelope envelope, double rate)
        {
            if (rate >= 1.0)
            {
                return true;
            }
            if (rate <= 0.0)
            {
                return false;
            }

            string seed = ResolveSeed(envelope);
            double score = seed is null
                ? RandomLocal.Value.NextDouble()
                : ToUnitInterval(Fnv1a(seed));
            return score < rate;
        }

        private static string ResolveSeed(TelemetryEnvelope envelope)
        {
            if (envelope is null)
            {
                return null;
            }
            if (!string.IsNullOrEmpty(envelope.CorrelationId))
            {
                return envelope.CorrelationId;
            }
            if (!string.IsNullOrEmpty(envelope.TraceId))
            {
                return envelope.TraceId;
            }
            return null;
        }

        private static uint Fnv1a(string value)
        {
            uint hash = FnvOffset;
            for (int i = 0; i < value.Length; i++)
            {
                hash ^= value[i];
                hash *= FnvPrime;
            }
            return hash;
        }

        private static double ToUnitInterval(uint hash)
        {
            // Map to [0, 1) with 24 bits of precision, plenty for sampling.
            const double scale = 1.0 / (1u << 24);
            return (hash >> 8) * scale;
        }
    }
}
