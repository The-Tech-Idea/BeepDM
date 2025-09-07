using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using TheTechIdea.Beep.Logger;

namespace TheTechIdea.Beep.WebAPI.Helpers
{
    /// <summary>
    /// Caching helper for Web API responses with TTL support
    /// </summary>
    public class WebAPICacheHelper : IDisposable
    {
        private readonly ConcurrentDictionary<string, CacheEntry> _cache = new ConcurrentDictionary<string, CacheEntry>();
        private readonly System.Threading.Timer _cleanupTimer;
        private readonly int _defaultTtlMinutes;
        private readonly IDMLogger _logger;
        private readonly string _dataSourceName;

        public WebAPICacheHelper(IDMLogger logger, string dataSourceName, int defaultTtlMinutes = 15)
        {
            _logger = logger;
            _dataSourceName = dataSourceName ?? "WebAPI";
            _defaultTtlMinutes = defaultTtlMinutes;
            
            // Setup cleanup timer (runs every 5 minutes)
            _cleanupTimer = new System.Threading.Timer(CleanupExpiredEntries, null, 
                TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        public string GenerateCacheKey(string operation, params string[] parameters)
        {
            var key = $"{_dataSourceName}:{operation}";
            if (parameters?.Length > 0)
            {
                key += ":" + string.Join(":", parameters);
            }
            return key;
        }

        public T Get<T>(string cacheKey) where T : class
        {
            if (_cache.TryGetValue(cacheKey, out var entry))
            {
                if (DateTime.UtcNow <= entry.ExpiryTime)
                {
                    _logger?.WriteLog($"Cache hit for key: {cacheKey}");
                    return entry.Value as T;
                }
                
                // Expired entry - remove it
                _cache.TryRemove(cacheKey, out _);
                _logger?.WriteLog($"Cache entry expired and removed: {cacheKey}");
            }
            
            return null;
        }

        public void Set<T>(string cacheKey, T value, TimeSpan? ttl = null)
        {
            var expiryTime = DateTime.UtcNow.Add(ttl ?? TimeSpan.FromMinutes(_defaultTtlMinutes));
            var cacheEntry = new CacheEntry
            {
                Value = value,
                ExpiryTime = expiryTime,
                CreatedTime = DateTime.UtcNow
            };

            _cache.AddOrUpdate(cacheKey, cacheEntry, (key, oldValue) => cacheEntry);
            _logger?.WriteLog($"Cache entry added/updated: {cacheKey}, expires at {expiryTime}");
        }

        public void Remove(string cacheKey)
        {
            if (_cache.TryRemove(cacheKey, out _))
            {
                _logger?.WriteLog($"Cache entry removed: {cacheKey}");
            }
        }

        public void RemoveByPattern(string pattern)
        {
            var keysToRemove = new List<string>();
            
            foreach (var kvp in _cache)
            {
                if (kvp.Key.Contains(pattern))
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                _cache.TryRemove(key, out _);
            }

            if (keysToRemove.Count > 0)
            {
                _logger?.WriteLog($"Removed {keysToRemove.Count} cache entries matching pattern: {pattern}");
            }
        }

        public void Clear()
        {
            var count = _cache.Count;
            _cache.Clear();
            _logger?.WriteLog($"Cache cleared, removed {count} entries");
        }

        public CacheStatistics GetStatistics()
        {
            var now = DateTime.UtcNow;
            var totalEntries = _cache.Count;
            var expiredEntries = 0;
            var validEntries = 0;

            foreach (var kvp in _cache)
            {
                if (now > kvp.Value.ExpiryTime)
                    expiredEntries++;
                else
                    validEntries++;
            }

            return new CacheStatistics
            {
                TotalEntries = totalEntries,
                ValidEntries = validEntries,
                ExpiredEntries = expiredEntries,
                CacheEfficiencyPercent = totalEntries > 0 ? (double)validEntries / totalEntries * 100 : 0
            };
        }

        public async Task<T> GetOrSetAsync<T>(string cacheKey, Func<Task<T>> factory, TimeSpan? ttl = null) where T : class
        {
            var cached = Get<T>(cacheKey);
            if (cached != null)
                return cached;

            var value = await factory();
            if (value != null)
            {
                Set(cacheKey, value, ttl);
            }

            return value;
        }

        private void CleanupExpiredEntries(object state)
        {
            try
            {
                var expiredKeys = new List<string>();
                var now = DateTime.UtcNow;
                
                foreach (var kvp in _cache)
                {
                    if (now > kvp.Value.ExpiryTime)
                    {
                        expiredKeys.Add(kvp.Key);
                    }
                }
                
                foreach (var key in expiredKeys)
                {
                    _cache.TryRemove(key, out _);
                }
                
                if (expiredKeys.Count > 0)
                {
                    _logger?.WriteLog($"Cache cleanup: removed {expiredKeys.Count} expired entries");
                }
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error during cache cleanup: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _cleanupTimer?.Dispose();
            _cache?.Clear();
        }

        private class CacheEntry
        {
            public object Value { get; set; }
            public DateTime ExpiryTime { get; set; }
            public DateTime CreatedTime { get; set; }
        }

        public class CacheStatistics
        {
            public int TotalEntries { get; set; }
            public int ValidEntries { get; set; }
            public int ExpiredEntries { get; set; }
            public double CacheEfficiencyPercent { get; set; }
        }
    }
}
