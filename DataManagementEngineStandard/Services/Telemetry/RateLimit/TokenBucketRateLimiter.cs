using System;
using System.Collections.Generic;
using System.Threading;
using TheTechIdea.Beep.Services.Logging;

namespace TheTechIdea.Beep.Services.Telemetry.RateLimit
{
    /// <summary>
    /// Per-key token-bucket rate limiter. Refills at
    /// <see cref="RefillPerSecond"/>, capped at <see cref="Burst"/>; once
    /// the bucket for a key empties, subsequent envelopes are rejected
    /// until enough time has passed for a fresh token.
    /// </summary>
    /// <remarks>
    /// Tokens are tracked as <see cref="double"/> so the refill math stays
    /// exact for sub-second arrivals. A periodic summary envelope is
    /// emitted (default every 30s) describing the per-key drops accrued
    /// since the previous summary, so operators can see "category X has
    /// been throttled" without having to parse every dropped event.
    /// </remarks>
    public sealed class TokenBucketRateLimiter : IRateLimiter
    {
        /// <summary>Default summary cadence if none is supplied.</summary>
        public static readonly TimeSpan DefaultSummaryInterval = TimeSpan.FromSeconds(30);

        /// <summary>Property key carrying the per-summary drop count.</summary>
        public const string DropCountProperty = "rateLimit.drops";

        /// <summary>Property key carrying the rate-limit key (e.g. category).</summary>
        public const string DropKeyProperty = "rateLimit.key";

        private readonly object _gate = new object();
        private readonly Dictionary<string, BucketState> _buckets;
        private readonly Func<TelemetryEnvelope, string> _keyBy;
        private readonly Func<DateTime> _utcNow;
        private readonly double _refillPerSecond;
        private readonly double _burst;
        private readonly TimeSpan _summaryInterval;

        private Action<TelemetryEnvelope> _emitSummary;
        private DateTime _nextSummaryUtc;
        private long _droppedCount;

        /// <summary>Creates a token-bucket limiter.</summary>
        /// <param name="refillPerSecond">Sustained rate per second per key (positive).</param>
        /// <param name="burst">Maximum bucket size per key (positive).</param>
        /// <param name="keyBy">Key extractor; defaults to <c>envelope.Category</c>.</param>
        /// <param name="summaryInterval">Cadence of the summary envelope (positive).</param>
        /// <param name="utcNow">UTC clock seam for tests.</param>
        public TokenBucketRateLimiter(
            double refillPerSecond,
            double burst,
            Func<TelemetryEnvelope, string> keyBy = null,
            TimeSpan? summaryInterval = null,
            Func<DateTime> utcNow = null)
        {
            if (refillPerSecond <= 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(refillPerSecond), "Refill must be positive.");
            }
            if (burst <= 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(burst), "Burst must be positive.");
            }
            _refillPerSecond = refillPerSecond;
            _burst = burst;
            _keyBy = keyBy ?? DefaultKeyBy;
            _summaryInterval = summaryInterval ?? DefaultSummaryInterval;
            if (_summaryInterval <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(summaryInterval), "Summary interval must be positive.");
            }
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
            _buckets = new Dictionary<string, BucketState>(StringComparer.Ordinal);
            _nextSummaryUtc = _utcNow() + _summaryInterval;
        }

        /// <inheritdoc/>
        public string Name => "tokenBucket";

        /// <inheritdoc/>
        public long DroppedCount => Interlocked.Read(ref _droppedCount);

        /// <summary>Configured refill rate per second per key.</summary>
        public double RefillPerSecond => _refillPerSecond;

        /// <summary>Configured burst capacity per key.</summary>
        public double Burst => _burst;

        /// <inheritdoc/>
        public void Bind(Action<TelemetryEnvelope> emitSummary)
        {
            _emitSummary = emitSummary;
        }

        /// <inheritdoc/>
        public bool TryAcquire(TelemetryEnvelope envelope)
        {
            if (envelope is null)
            {
                return false;
            }

            string key = _keyBy(envelope) ?? string.Empty;
            DateTime now = _utcNow();
            bool allowed;

            List<TelemetryEnvelope> summaries = null;
            lock (_gate)
            {
                if (!_buckets.TryGetValue(key, out BucketState state))
                {
                    state = new BucketState
                    {
                        Tokens = _burst - 1.0,
                        LastRefillUtc = now
                    };
                    _buckets[key] = state;
                    allowed = true;
                }
                else
                {
                    Refill_NoLock(state, now);
                    if (state.Tokens >= 1.0)
                    {
                        state.Tokens -= 1.0;
                        allowed = true;
                    }
                    else
                    {
                        state.Drops++;
                        Interlocked.Increment(ref _droppedCount);
                        allowed = false;
                    }
                }

                if (now >= _nextSummaryUtc)
                {
                    summaries = BuildSummariesAndResetCounters_NoLock(now);
                    _nextSummaryUtc = now + _summaryInterval;
                }
            }

            FlushSummaries(summaries);
            return allowed;
        }

        /// <inheritdoc/>
        public void DrainSummaries()
        {
            DateTime now = _utcNow();
            List<TelemetryEnvelope> summaries;
            lock (_gate)
            {
                summaries = BuildSummariesAndResetCounters_NoLock(now);
                _nextSummaryUtc = now + _summaryInterval;
            }
            FlushSummaries(summaries);
        }

        private void Refill_NoLock(BucketState state, DateTime now)
        {
            double elapsedSeconds = (now - state.LastRefillUtc).TotalSeconds;
            if (elapsedSeconds <= 0.0)
            {
                return;
            }
            double newTokens = state.Tokens + (elapsedSeconds * _refillPerSecond);
            if (newTokens > _burst)
            {
                newTokens = _burst;
            }
            state.Tokens = newTokens;
            state.LastRefillUtc = now;
        }

        private List<TelemetryEnvelope> BuildSummariesAndResetCounters_NoLock(DateTime now)
        {
            List<TelemetryEnvelope> result = null;
            foreach (KeyValuePair<string, BucketState> entry in _buckets)
            {
                if (entry.Value.Drops <= 0)
                {
                    continue;
                }
                long drops = entry.Value.Drops;
                entry.Value.Drops = 0;

                result ??= new List<TelemetryEnvelope>(4);
                result.Add(BuildSummary(entry.Key, drops, _summaryInterval));
            }
            return result;
        }

        private void FlushSummaries(List<TelemetryEnvelope> summaries)
        {
            if (summaries is null || summaries.Count == 0 || _emitSummary is null)
            {
                return;
            }
            for (int i = 0; i < summaries.Count; i++)
            {
                try
                {
                    _emitSummary(summaries[i]);
                }
                catch
                {
                    // Summary emission must never throw past the limiter.
                }
            }
        }

        private static TelemetryEnvelope BuildSummary(string key, long drops, TimeSpan window)
        {
            string message = string.Concat(
                "[rate-limited] key='",
                key,
                "' dropped=",
                drops.ToString(),
                " window=",
                ((int)window.TotalSeconds).ToString(),
                "s");

            var properties = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                [DropKeyProperty] = key,
                [DropCountProperty] = drops
            };

            return new TelemetryEnvelope
            {
                Kind = TelemetryKind.Log,
                TimestampUtc = DateTime.UtcNow,
                Level = BeepLogLevel.Warning,
                Category = "Beep.RateLimit",
                Message = message,
                Properties = properties
            };
        }

        private static string DefaultKeyBy(TelemetryEnvelope envelope)
        {
            if (envelope is null)
            {
                return string.Empty;
            }
            return envelope.Category ?? string.Empty;
        }

        private sealed class BucketState
        {
            public double Tokens;
            public DateTime LastRefillUtc;
            public long Drops;
        }
    }
}
