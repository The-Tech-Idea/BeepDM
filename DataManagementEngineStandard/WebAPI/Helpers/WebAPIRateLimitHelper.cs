using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Logger;

namespace TheTechIdea.Beep.WebAPI.Helpers
{
    /// <summary>
    /// Rate limiting helper to manage API request throttling and quota compliance
    /// Implements token bucket algorithm for smooth rate limiting
    /// </summary>
    public class WebAPIRateLimitHelper : IDisposable
    {
        #region Private Fields
        
        private readonly IDMLogger _logger;
        private readonly string _datasourceName;
        private readonly ConcurrentDictionary<string, TokenBucket> _buckets;
        private readonly Timer _cleanupTimer;
        private readonly object _lockObject = new object();
        private bool _disposed;

        #endregion

        #region Properties

        /// <summary>Default requests per second if not configured</summary>
        public int DefaultRequestsPerSecond { get; set; } = 10;

        /// <summary>Default burst capacity</summary>
        public int DefaultBurstCapacity { get; set; } = 20;

        /// <summary>Bucket cleanup interval in minutes</summary>
        public int CleanupIntervalMinutes { get; set; } = 5;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes the rate limit helper
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="datasourceName">Data source name for logging</param>
        public WebAPIRateLimitHelper(IDMLogger logger, string datasourceName)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _datasourceName = datasourceName ?? throw new ArgumentNullException(nameof(datasourceName));
            _buckets = new ConcurrentDictionary<string, TokenBucket>();
            
            // Setup cleanup timer
            _cleanupTimer = new Timer(CleanupExpiredBuckets, null, 
                TimeSpan.FromMinutes(CleanupIntervalMinutes), 
                TimeSpan.FromMinutes(CleanupIntervalMinutes));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Waits for rate limit permission before proceeding
        /// </summary>
        /// <param name="endpoint">API endpoint identifier</param>
        /// <param name="requestsPerSecond">Requests per second limit</param>
        /// <param name="burstCapacity">Burst capacity</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task that completes when permission is granted</returns>
        public async Task WaitForPermissionAsync(string endpoint, 
            int requestsPerSecond = 0, 
            int burstCapacity = 0, 
            CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(WebAPIRateLimitHelper));

            if (string.IsNullOrEmpty(endpoint))
                endpoint = "default";

            var rps = requestsPerSecond > 0 ? requestsPerSecond : DefaultRequestsPerSecond;
            var capacity = burstCapacity > 0 ? burstCapacity : DefaultBurstCapacity;

            var bucket = _buckets.GetOrAdd(endpoint, _ => new TokenBucket(rps, capacity, _logger));

            var waitTime = bucket.TryConsume();
            if (waitTime > TimeSpan.Zero)
            {
                _logger?.WriteLog($"Rate limit reached for {endpoint}. Waiting {waitTime.TotalMilliseconds}ms");
                await Task.Delay(waitTime, cancellationToken);
            }
        }

        /// <summary>
        /// Checks if a request can be made immediately
        /// </summary>
        /// <param name="endpoint">API endpoint identifier</param>
        /// <param name="requestsPerSecond">Requests per second limit</param>
        /// <param name="burstCapacity">Burst capacity</param>
        /// <returns>True if request can be made immediately</returns>
        public bool CanMakeRequest(string endpoint, int requestsPerSecond = 0, int burstCapacity = 0)
        {
            if (_disposed)
                return false;

            if (string.IsNullOrEmpty(endpoint))
                endpoint = "default";

            var rps = requestsPerSecond > 0 ? requestsPerSecond : DefaultRequestsPerSecond;
            var capacity = burstCapacity > 0 ? burstCapacity : DefaultBurstCapacity;

            var bucket = _buckets.GetOrAdd(endpoint, _ => new TokenBucket(rps, capacity, _logger));
            return bucket.TryConsume() == TimeSpan.Zero;
        }

        /// <summary>
        /// Gets current rate limit status for an endpoint
        /// </summary>
        /// <param name="endpoint">API endpoint identifier</param>
        /// <returns>Rate limit status information</returns>
        public RateLimitStatus GetStatus(string endpoint)
        {
            if (string.IsNullOrEmpty(endpoint))
                endpoint = "default";

            if (_buckets.TryGetValue(endpoint, out var bucket))
            {
                return bucket.GetStatus();
            }

            return new RateLimitStatus
            {
                Endpoint = endpoint,
                TokensAvailable = DefaultBurstCapacity,
                RefillRate = DefaultRequestsPerSecond,
                NextRefillTime = DateTime.UtcNow
            };
        }

        #endregion

        #region Private Methods

        private void CleanupExpiredBuckets(object state)
        {
            if (_disposed)
                return;

            lock (_lockObject)
            {
                var cutoffTime = DateTime.UtcNow.AddMinutes(-CleanupIntervalMinutes * 2);
                var expiredKeys = new List<string>();

                foreach (var kvp in _buckets)
                {
                    if (kvp.Value.LastAccessed < cutoffTime)
                    {
                        expiredKeys.Add(kvp.Key);
                    }
                }

                foreach (var key in expiredKeys)
                {
                    if (_buckets.TryRemove(key, out var bucket))
                    {
                        bucket.Dispose();
                        _logger?.WriteLog($"Cleaned up expired rate limit bucket for {key}");
                    }
                }
            }
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _cleanupTimer?.Dispose();
                
                foreach (var bucket in _buckets.Values)
                {
                    bucket.Dispose();
                }
                _buckets.Clear();
                
                _disposed = true;
            }
        }

        #endregion

        #region Inner Classes

        /// <summary>
        /// Token bucket implementation for rate limiting
        /// </summary>
        private class TokenBucket : IDisposable
        {
            private readonly int _capacity;
            private readonly double _refillRate;
            private readonly IDMLogger _logger;
            private readonly object _lock = new object();
            
            private double _tokens;
            private DateTime _lastRefill;
            
            public DateTime LastAccessed { get; private set; }

            public TokenBucket(int requestsPerSecond, int capacity, IDMLogger logger)
            {
                _refillRate = requestsPerSecond;
                _capacity = capacity;
                _logger = logger;
                _tokens = capacity;
                _lastRefill = DateTime.UtcNow;
                LastAccessed = DateTime.UtcNow;
            }

            public TimeSpan TryConsume()
            {
                lock (_lock)
                {
                    LastAccessed = DateTime.UtcNow;
                    Refill();

                    if (_tokens >= 1.0)
                    {
                        _tokens -= 1.0;
                        return TimeSpan.Zero;
                    }

                    // Calculate wait time
                    var tokensNeeded = 1.0 - _tokens;
                    var waitTimeSeconds = tokensNeeded / _refillRate;
                    return TimeSpan.FromSeconds(waitTimeSeconds);
                }
            }

            public RateLimitStatus GetStatus()
            {
                lock (_lock)
                {
                    Refill();
                    return new RateLimitStatus
                    {
                        TokensAvailable = (int)_tokens,
                        RefillRate = (int)_refillRate,
                        NextRefillTime = _lastRefill.AddSeconds(1.0 / _refillRate)
                    };
                }
            }

            private void Refill()
            {
                var now = DateTime.UtcNow;
                var elapsed = (now - _lastRefill).TotalSeconds;
                var tokensToAdd = elapsed * _refillRate;
                
                _tokens = Math.Min(_capacity, _tokens + tokensToAdd);
                _lastRefill = now;
            }

            public void Dispose()
            {
                // No managed resources to dispose
            }
        }

        /// <summary>
        /// Rate limit status information
        /// </summary>
        public class RateLimitStatus
        {
            public string Endpoint { get; set; }
            public int TokensAvailable { get; set; }
            public int RefillRate { get; set; }
            public DateTime NextRefillTime { get; set; }
        }

        #endregion
    }
}
