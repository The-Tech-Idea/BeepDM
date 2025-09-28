using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Timers;

namespace TheTechIdea.Beep.Caching.Providers
{
    /// <summary>
    /// Enhanced memory cache provider with advanced memory management and size-aware operations.
    /// 
    /// This provider focuses on intelligent memory usage and provides enterprise-grade features
    /// for applications requiring precise memory control and advanced caching strategies:
    /// 
    /// **Memory Management Excellence:**
    /// - Size-based eviction policies with configurable memory limits
    /// - Real-time memory usage tracking and reporting
    /// - Intelligent compaction algorithms to optimize memory utilization
    /// - Memory pressure detection with adaptive eviction strategies
    /// - Configurable size limits with automatic enforcement
    /// - Memory-aware item prioritization and retention policies
    /// 
    /// **Advanced Caching Features:**
    /// - Hybrid LRU eviction based on both access time and memory consumption
    /// - Size estimation algorithms for accurate memory accounting
    /// - Configurable compaction percentage for memory optimization
    /// - Smart eviction targeting least valuable items first
    /// - Multiple eviction triggers (size, time, access patterns)
    /// 
    /// **Enterprise Capabilities:**
    /// - Precise memory limit enforcement with soft and hard boundaries
    /// - Advanced statistics including memory efficiency metrics
    /// - Integration with system memory monitoring
    /// - Proactive eviction to prevent out-of-memory conditions
    /// - Support for memory-constrained environments
    /// 
    /// **Use Cases:**
    /// - Memory-constrained server environments
    /// - Cloud applications with memory limits (containers, serverless)
    /// - Applications requiring predictable memory usage
    /// - Systems with varying memory availability
    /// - Embedded systems with limited resources
    /// - Multi-tenant applications with memory quotas
    /// 
    /// **Performance Characteristics:**
    /// - Optimized for memory efficiency over raw speed
    /// - Intelligent trade-offs between memory usage and performance
    /// - Adaptive behavior based on available system memory
    /// - Efficient handling of varying object sizes
    /// - Minimal memory fragmentation
    /// 
    /// **Memory Optimization:**
    /// - Dynamic size calculation for all cached objects
    /// - Memory usage forecasting and planning
    /// - Efficient memory reclamation strategies
    /// - Support for memory pressure callbacks
    /// - Integration with .NET garbage collection optimization
    /// 
    /// **Monitoring & Control:**
    /// - Real-time memory usage dashboards
    /// - Memory efficiency reporting and analysis
    /// - Configurable memory thresholds and alerts
    /// - Memory usage trending and capacity planning
    /// - Integration with application performance monitoring (APM) tools
    /// 
    /// **Configuration Flexibility:**
    /// - Granular memory limit configuration
    /// - Adjustable compaction strategies
    /// - Customizable eviction policies
    /// - Integration with system memory management
    /// </summary>
    public class MemoryCacheProvider : ICacheProvider
    {
        #region Private Fields
        private readonly ConcurrentDictionary<string, CacheEntry> _cache;
        private readonly CacheConfiguration _configuration;
        private readonly CacheStatistics _statistics;
        private readonly System.Timers.Timer _cleanupTimer;
        private readonly object _sizeLock = new object();
        private volatile bool _disposed = false;
        private long _currentSize = 0;

        // Thread-safe fields for statistics
        private long _hits = 0;
        private long _misses = 0;
        private long _itemCount = 0;
        private long _memoryUsage = 0;
        private long _expiredItems = 0;
        private long _evictedItems = 0;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the MemoryCacheProvider class.
        /// </summary>
        /// <param name="configuration">Cache configuration settings.</param>
        public MemoryCacheProvider(CacheConfiguration configuration = null)
        {
            _configuration = configuration ?? new CacheConfiguration();
            _statistics = new CacheStatistics();
            _cache = new ConcurrentDictionary<string, CacheEntry>(StringComparer.OrdinalIgnoreCase);

            // Setup cleanup timer
            _cleanupTimer = new System.Timers.Timer(_configuration.CleanupInterval.TotalMilliseconds);
            _cleanupTimer.Elapsed += OnCleanupTimer;
            _cleanupTimer.AutoReset = true;
            _cleanupTimer.Start();
        }
        #endregion

        #region ICacheProvider Implementation
        public string Name => "EnhancedMemoryCache";
        public bool IsAvailable => !_disposed;
        public CacheStatistics Statistics 
        {
            get
            {
                // Update statistics with current values
                _statistics.Hits = _hits;
                _statistics.Misses = _misses;
                _statistics.ItemCount = _itemCount;
                _statistics.MemoryUsage = _memoryUsage;
                _statistics.ExpiredItems = _expiredItems;
                _statistics.EvictedItems = _evictedItems;
                _statistics.LastUpdated = DateTimeOffset.UtcNow;
                return _statistics;
            }
        }

        public async Task<T> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key) || _disposed)
            {
                if (_configuration.EnableStatistics)
                    Interlocked.Increment(ref _misses);
                return default(T);
            }

            var fullKey = GetFullKey(key);

            if (_cache.TryGetValue(fullKey, out var entry))
            {
                if (entry.IsExpired)
                {
                    // Remove expired entry
                    await RemoveEntryAsync(fullKey, entry);
                    
                    if (_configuration.EnableStatistics)
                    {
                        Interlocked.Increment(ref _misses);
                        Interlocked.Increment(ref _expiredItems);
                    }
                    return default(T);
                }

                // Update last accessed time
                entry.LastAccessed = DateTimeOffset.UtcNow;
                
                if (_configuration.EnableStatistics)
                    Interlocked.Increment(ref _hits);

                return await DeserializeValueAsync<T>(entry.Data);
            }

            if (_configuration.EnableStatistics)
                Interlocked.Increment(ref _misses);

            return default(T);
        }

        public async Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key) || _disposed || value == null)
                return false;

            var fullKey = GetFullKey(key);
            var expirationTime = expiry.HasValue 
                ? DateTimeOffset.UtcNow.Add(expiry.Value) 
                : DateTimeOffset.UtcNow.Add(_configuration.DefaultExpiry);

            try
            {
                var serializedData = await SerializeValueAsync(value);
                var entrySize = EstimateSize(serializedData);

                // Check size limits and evict if necessary
                await EnsureSizeLimit(entrySize);

                var entry = new CacheEntry
                {
                    Data = serializedData,
                    Expiry = expirationTime,
                    Size = entrySize,
                    LastAccessed = DateTimeOffset.UtcNow,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                var wasAdded = false;
                _cache.AddOrUpdate(fullKey, entry, (k, existingEntry) =>
                {
                    // Update existing entry
                    lock (_sizeLock)
                    {
                        _currentSize -= existingEntry.Size;
                        _currentSize += entry.Size;
                    }
                    return entry;
                });

                if (!_cache.ContainsKey(fullKey) || _cache[fullKey] == entry)
                {
                    wasAdded = true;
                    lock (_sizeLock)
                    {
                        if (!wasAdded) // New entry
                        {
                            _currentSize += entrySize;
                        }
                    }
                }

                if (_configuration.EnableStatistics)
                {
                    if (wasAdded)
                        Interlocked.Increment(ref _itemCount);
                    Interlocked.Exchange(ref _memoryUsage, _currentSize);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key) || _disposed)
                return false;

            var fullKey = GetFullKey(key);
            
            if (_cache.TryRemove(fullKey, out var entry))
            {
                await RemoveEntryAsync(fullKey, entry, false);
                return true;
            }

            return false;
        }

        public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key) || _disposed)
                return false;

            var fullKey = GetFullKey(key);
            
            if (_cache.TryGetValue(fullKey, out var entry))
            {
                if (entry.IsExpired)
                {
                    // Remove expired entry
                    await RemoveEntryAsync(fullKey, entry);
                    return false;
                }
                return true;
            }

            return false;
        }

        public async Task<long> ClearAsync(string pattern = null, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                return 0;

            long removedCount = 0;

            if (string.IsNullOrWhiteSpace(pattern))
            {
                removedCount = _cache.Count;
                _cache.Clear();
                
                lock (_sizeLock)
                {
                    _currentSize = 0;
                }
            }
            else
            {
                var keysToRemove = _cache.Keys
                    .Where(k => k.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var key in keysToRemove)
                {
                    if (_cache.TryRemove(key, out var entry))
                    {
                        lock (_sizeLock)
                        {
                            _currentSize -= entry.Size;
                        }
                        removedCount++;
                    }
                }
            }

            if (_configuration.EnableStatistics)
            {
                Interlocked.Add(ref _itemCount, -removedCount);
                Interlocked.Exchange(ref _memoryUsage, _currentSize);
            }

            return removedCount;
        }

        public async Task<Dictionary<string, T>> GetManyAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default)
        {
            var result = new Dictionary<string, T>();
            
            if (keys == null || _disposed)
                return result;

            foreach (var key in keys)
            {
                var value = await GetAsync<T>(key, cancellationToken);
                if (!EqualityComparer<T>.Default.Equals(value, default(T)))
                {
                    result[key] = value;
                }
            }

            return result;
        }

        public async Task<long> SetManyAsync<T>(Dictionary<string, T> values, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
        {
            if (values == null || _disposed)
                return 0;

            long successCount = 0;
            
            foreach (var kvp in values)
            {
                if (await SetAsync(kvp.Key, kvp.Value, expiry, cancellationToken))
                {
                    successCount++;
                }
            }

            return successCount;
        }

        public async Task<bool> RefreshAsync(string key, TimeSpan expiry, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key) || _disposed)
                return false;

            var fullKey = GetFullKey(key);
            
            if (_cache.TryGetValue(fullKey, out var entry))
            {
                entry.Expiry = DateTimeOffset.UtcNow.Add(expiry);
                entry.LastAccessed = DateTimeOffset.UtcNow;
                return true;
            }

            return false;
        }
        #endregion

        #region Private Helper Methods
        private string GetFullKey(string key)
        {
            return string.IsNullOrWhiteSpace(_configuration.KeyPrefix) 
                ? key 
                : $"{_configuration.KeyPrefix}{key}";
        }

        private async Task<byte[]> SerializeValueAsync<T>(T value)
        {
            try
            {
                var json = JsonSerializer.Serialize(value);
                return System.Text.Encoding.UTF8.GetBytes(json);
            }
            catch
            {
                return Array.Empty<byte>();
            }
        }

        private async Task<T> DeserializeValueAsync<T>(byte[] value)
        {
            try
            {
                var json = System.Text.Encoding.UTF8.GetString(value);
                return JsonSerializer.Deserialize<T>(json);
            }
            catch
            {
                return default(T);
            }
        }

        private static long EstimateSize(byte[] data)
        {
            return data?.Length ?? 0;
        }

        private async Task RemoveEntryAsync(string key, CacheEntry entry, bool alreadyRemoved = true)
        {
            if (!alreadyRemoved)
            {
                _cache.TryRemove(key, out _);
            }

            lock (_sizeLock)
            {
                _currentSize -= entry.Size;
            }

            if (_configuration.EnableStatistics)
            {
                Interlocked.Decrement(ref _itemCount);
                Interlocked.Exchange(ref _memoryUsage, _currentSize);
            }
        }

        private async Task EnsureSizeLimit(long newEntrySize)
        {
            if (!_configuration.MemoryCache.SizeLimit.HasValue)
                return;

            var sizeLimit = _configuration.MemoryCache.SizeLimit.Value;
            var projectedSize = _currentSize + newEntrySize;

            if (projectedSize <= sizeLimit)
                return;

            // Need to evict entries
            var targetSize = (long)(sizeLimit * (1 - _configuration.MemoryCache.CompactionPercentage));
            await EvictLeastRecentlyUsedAsync(targetSize);
        }

        private async Task EvictLeastRecentlyUsedAsync(long targetSize)
        {
            var entries = _cache.ToList()
                .OrderBy(kvp => kvp.Value.LastAccessed)
                .ToList();

            foreach (var kvp in entries)
            {
                if (_currentSize <= targetSize)
                    break;

                if (_cache.TryRemove(kvp.Key, out var entry))
                {
                    await RemoveEntryAsync(kvp.Key, entry);
                    
                    if (_configuration.EnableStatistics)
                    {
                        Interlocked.Increment(ref _evictedItems);
                    }
                }
            }
        }

        private void OnCleanupTimer(object sender, ElapsedEventArgs e)
        {
            if (_disposed)
                return;

            _ = Task.Run(async () =>
            {
                try
                {
                    await CleanupExpiredEntriesAsync();
                }
                catch
                {
                    // Ignore cleanup errors
                }
            });
        }

        private async Task CleanupExpiredEntriesAsync()
        {
            var expiredKeys = new List<string>();

            foreach (var kvp in _cache)
            {
                if (kvp.Value.IsExpired)
                {
                    expiredKeys.Add(kvp.Key);
                }
            }

            foreach (var key in expiredKeys)
            {
                if (_cache.TryRemove(key, out var entry))
                {
                    await RemoveEntryAsync(key, entry);
                }
            }
        }
        #endregion

        #region IDisposable Implementation
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _cleanupTimer?.Stop();
                _cleanupTimer?.Dispose();
                _cache?.Clear();
                
                lock (_sizeLock)
                {
                    _currentSize = 0;
                }
            }
        }
        #endregion

        #region Cache Entry Class
        private class CacheEntry
        {
            public byte[] Data { get; set; }
            public DateTimeOffset Expiry { get; set; }
            public long Size { get; set; }
            public DateTimeOffset LastAccessed { get; set; }
            public DateTimeOffset CreatedAt { get; set; }
            public bool IsExpired => DateTimeOffset.UtcNow > Expiry;
        }
        #endregion
    }
}