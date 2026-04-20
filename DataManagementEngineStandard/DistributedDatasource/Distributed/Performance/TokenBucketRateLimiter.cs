using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Distributed.Performance
{
    /// <summary>
    /// Per-shard token-bucket implementation of
    /// <see cref="IDistributedRateLimiter"/>. Each shard gets its own
    /// bucket so a bursty shard cannot drain tokens from neighbours.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Buckets refill continuously at
    /// <see cref="TokenBucketRateLimiter.RatePerSecond"/> tokens/sec
    /// up to <see cref="TokenBucketRateLimiter.Burst"/>. The bucket
    /// state is maintained via a simple spin-lock on each bucket so
    /// the hot path stays allocation-free.
    /// </para>
    /// <para>
    /// Pass <c>ratePerSecond &lt;= 0</c> to effectively disable the
    /// limiter — every <c>TryAcquire</c> succeeds and
    /// <c>AcquireAsync</c> returns immediately.
    /// </para>
    /// </remarks>
    public sealed class TokenBucketRateLimiter : IDistributedRateLimiter
    {
        private readonly ConcurrentDictionary<string, Bucket> _buckets;
        private double _ratePerSecond;
        private int    _burst;

        /// <summary>Creates a new limiter with the supplied defaults.</summary>
        public TokenBucketRateLimiter(double ratePerSecond, int burst)
        {
            _ratePerSecond = ratePerSecond;
            _burst         = burst > 0 ? burst : 1;
            _buckets       = new ConcurrentDictionary<string, Bucket>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>Current steady-state rate (calls/sec).</summary>
        public double RatePerSecond => _ratePerSecond;

        /// <summary>Current bucket capacity.</summary>
        public int Burst => _burst;

        /// <inheritdoc/>
        public void Configure(double ratePerSecond, int burst)
        {
            _ratePerSecond = ratePerSecond;
            _burst         = burst > 0 ? burst : 1;
        }

        /// <inheritdoc/>
        public bool TryAcquire(string shardId)
        {
            if (_ratePerSecond <= 0 || string.IsNullOrWhiteSpace(shardId)) return true;
            return GetBucket(shardId).TryTake(_ratePerSecond, _burst);
        }

        /// <inheritdoc/>
        public async Task AcquireAsync(
            string             shardId,
            TimeSpan           wait,
            CancellationToken  cancellationToken = default)
        {
            if (_ratePerSecond <= 0 || string.IsNullOrWhiteSpace(shardId)) return;

            var bucket   = GetBucket(shardId);
            var deadline = wait <= TimeSpan.Zero ? DateTime.UtcNow : DateTime.UtcNow.Add(wait);

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (bucket.TryTake(_ratePerSecond, _burst)) return;

                if (DateTime.UtcNow >= deadline)
                {
                    var retryAfter = bucket.EstimateRefillDelay(_ratePerSecond);
                    throw new BackpressureException(
                        gateName:   "Shard:" + shardId,
                        retryAfter: retryAfter,
                        message:    $"Rate limit exceeded for shard '{shardId}' (rate={_ratePerSecond:F1}/s, burst={_burst}).");
                }

                var delay = bucket.EstimateRefillDelay(_ratePerSecond);
                if (delay > TimeSpan.FromMilliseconds(100)) delay = TimeSpan.FromMilliseconds(100);
                if (delay <= TimeSpan.Zero)                 delay = TimeSpan.FromMilliseconds(1);
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        private Bucket GetBucket(string shardId)
        {
            return _buckets.GetOrAdd(shardId, _ => new Bucket(_burst));
        }

        /// <summary>
        /// Per-shard bucket state. Uses a spin-lock instead of a
        /// <see cref="SemaphoreSlim"/> because every call is lock-
        /// release-bounded and allocation-free.
        /// </summary>
        private sealed class Bucket
        {
            private double    _tokens;
            private DateTime  _lastRefillUtc;
            private int       _gate; // 0 = free, 1 = locked

            internal Bucket(int initialTokens)
            {
                _tokens         = initialTokens;
                _lastRefillUtc  = DateTime.UtcNow;
            }

            internal bool TryTake(double rate, int burst)
            {
                while (Interlocked.CompareExchange(ref _gate, 1, 0) != 0)
                {
                    Thread.SpinWait(1);
                }
                try
                {
                    Refill(rate, burst);
                    if (_tokens >= 1.0)
                    {
                        _tokens -= 1.0;
                        return true;
                    }
                    return false;
                }
                finally
                {
                    Volatile.Write(ref _gate, 0);
                }
            }

            internal TimeSpan EstimateRefillDelay(double rate)
            {
                if (rate <= 0) return TimeSpan.Zero;
                double deficit = 1.0 - Volatile.Read(ref _tokens);
                if (deficit <= 0) return TimeSpan.Zero;
                double seconds = deficit / rate;
                if (seconds < 0) seconds = 0;
                return TimeSpan.FromSeconds(seconds);
            }

            private void Refill(double rate, int burst)
            {
                if (rate <= 0) return;
                var now     = DateTime.UtcNow;
                var elapsed = (now - _lastRefillUtc).TotalSeconds;
                if (elapsed <= 0) return;

                _tokens        = Math.Min(burst, _tokens + elapsed * rate);
                _lastRefillUtc = now;
            }
        }
    }
}
