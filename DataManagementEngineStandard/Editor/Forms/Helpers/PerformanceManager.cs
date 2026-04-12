using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.UOWManager.Helpers
{
    /// <summary>
    /// Helper class for performance optimization and caching in UnitofWorksManager
    /// </summary>
    public class PerformanceManager : IPerformanceManager, IDisposable
    {
        #region Fields
        private readonly IDMEEditor _dmeEditor;
        private readonly ConcurrentDictionary<string, CachedBlockInfo> _blockCache = new();
        private readonly ConcurrentDictionary<string, PerformanceMetric> _performanceMetrics = new();
        private readonly ConcurrentDictionary<string, TimeSpan> _blockTtl = new();  // Phase 7: per-block TTL
        private long _evictionCount;                                                  // Phase 7: LRU eviction counter
        private readonly Timer _cacheCleanupTimer;
        private readonly ReaderWriterLockSlim _cacheLock = new(LockRecursionPolicy.SupportsRecursion);
        private readonly PerformanceStatistics _statistics = new();
        private bool _disposed;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the current number of cached blocks.
        /// </summary>
        public int CacheSize => _blockCache.Count;

        /// <summary>
        /// Gets or sets the default cache expiration window applied to cached block entries.
        /// </summary>
        public TimeSpan CacheExpirationTime { get; set; } = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Gets or sets the maximum number of cached blocks retained before eviction runs.
        /// </summary>
        public int MaxCacheSize { get; set; } = 1000;

        /// <summary>
        /// Gets or sets whether detailed performance metrics should be collected.
        /// </summary>
        public bool EnableDetailedMetrics { get; set; } = true;
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a performance manager for FormsManager caching and runtime metrics.
        /// </summary>
        /// <param name="dmeEditor">Editor used for logging.</param>
        public PerformanceManager(IDMEEditor dmeEditor)
        {
            _dmeEditor = dmeEditor ?? throw new ArgumentNullException(nameof(dmeEditor));
            
            // Setup cache cleanup timer to run every 5 minutes
            _cacheCleanupTimer = new Timer(CleanupExpiredCache, null, 
                TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
                
            LogOperation("PerformanceManager initialized");
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Optimizes block access by implementing various performance strategies
        /// </summary>
        public void OptimizeBlockAccess()
        {
            var sw = Stopwatch.StartNew();
            
            try
            {
                _cacheLock.EnterWriteLock();
                
                // Remove expired entries
                CleanupExpiredCacheInternal();
                
                // Optimize cache layout by moving frequently accessed items
                OptimizeCacheLayout();
                
                // Update statistics
                _statistics.LastOptimizationTime = DateTime.UtcNow;
                _statistics.OptimizationCount++;
                
                sw.Stop();
                _statistics.AverageOptimizationTime = UpdateAverageTime(_statistics.AverageOptimizationTime, sw.Elapsed, _statistics.OptimizationCount);
                
                LogOperation($"Block access optimization completed in {sw.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                LogError("Error during block access optimization", ex);
            }
            finally
            {
                if (_cacheLock.IsWriteLockHeld)
                    _cacheLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Caches block information for faster access
        /// </summary>
        public void CacheBlockInfo(string blockName, DataBlockInfo blockInfo)
        {
            if (string.IsNullOrWhiteSpace(blockName) || blockInfo == null)
                return;

            var sw = Stopwatch.StartNew();
            
            try
            {
                // Check cache size limit
                if (_blockCache.Count >= MaxCacheSize)
                {
                    RemoveOldestCacheEntries();
                }

                var cachedInfo = new CachedBlockInfo
                {
                    BlockInfo = blockInfo, // This is the original DataBlockInfo, not the Models one
                    CacheTime = DateTime.UtcNow,
                    AccessCount = 1,
                    LastAccessed = DateTime.UtcNow
                };

                _blockCache.AddOrUpdate(blockName, cachedInfo, (key, existing) =>
                {
                    existing.BlockInfo = blockInfo;
                    existing.CacheTime = DateTime.UtcNow;
                    existing.AccessCount++;
                    existing.LastAccessed = DateTime.UtcNow;
                    return existing;
                });

                _statistics.CacheWrites++;
                
                if (EnableDetailedMetrics)
                {
                    RecordPerformanceMetric($"Cache_Write_{blockName}", sw.Elapsed);
                }

                LogOperation($"Block '{blockName}' cached successfully");
            }
            catch (Exception ex)
            {
                LogError($"Error caching block info for '{blockName}'", ex);
            }
        }

        /// <summary>
        /// Gets cached block information
        /// </summary>
        public DataBlockInfo GetCachedBlockInfo(string blockName)
        {
            if (string.IsNullOrWhiteSpace(blockName))
                return null;

            var sw = Stopwatch.StartNew();
            
            try
            {
                if (_blockCache.TryGetValue(blockName, out var cachedInfo))
                {
                    // Phase 7: use per-block TTL if configured, otherwise global
                    var ttl = _blockTtl.TryGetValue(blockName, out var perBlock)
                        ? perBlock
                        : CacheExpirationTime;

                    // Check if cache entry is still valid
                    if (DateTime.UtcNow.Subtract(cachedInfo.CacheTime) <= ttl)
                    {
                        // Update access statistics
                        cachedInfo.AccessCount++;
                        cachedInfo.LastAccessed = DateTime.UtcNow;
                        
                        _statistics.CacheHits++;
                        
                        if (EnableDetailedMetrics)
                        {
                            RecordPerformanceMetric($"Cache_Hit_{blockName}", sw.Elapsed);
                        }

                        return cachedInfo.BlockInfo; // Return the original DataBlockInfo
                    }
                    else
                    {
                        // Remove expired entry
                        _blockCache.TryRemove(blockName, out _);
                        _statistics.CacheExpired++;
                        Interlocked.Increment(ref _evictionCount);
                    }
                }

                _statistics.CacheMisses++;
                
                if (EnableDetailedMetrics)
                {
                    RecordPerformanceMetric($"Cache_Miss_{blockName}", sw.Elapsed);
                }

                return null;
            }
            catch (Exception ex)
            {
                LogError($"Error getting cached block info for '{blockName}'", ex);
                return null;
            }
        }

        /// <summary>
        /// Clears all cached data
        /// </summary>
        public void ClearCache()
        {
            try
            {
                _cacheLock.EnterWriteLock();
                
                var cacheSize = _blockCache.Count;
                _blockCache.Clear();
                _performanceMetrics.Clear();
                
                _statistics.CacheClears++;
                _statistics.LastCacheClearTime = DateTime.UtcNow;
                
                LogOperation($"Cache cleared. Removed {cacheSize} entries");
            }
            catch (Exception ex)
            {
                LogError("Error clearing cache", ex);
            }
            finally
            {
                if (_cacheLock.IsWriteLockHeld)
                    _cacheLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Gets performance statistics
        /// </summary>
        public PerformanceStatistics GetPerformanceStatistics()
        {
            try
            {
                _cacheLock.EnterReadLock();
                
                var stats = new PerformanceStatistics
                {
                    CacheHits = _statistics.CacheHits,
                    CacheMisses = _statistics.CacheMisses,
                    CacheWrites = _statistics.CacheWrites,
                    CacheExpired = _statistics.CacheExpired,
                    CacheClears = _statistics.CacheClears,
                    OptimizationCount = _statistics.OptimizationCount,
                    CurrentCacheSize = _blockCache.Count,
                    AverageOptimizationTime = _statistics.AverageOptimizationTime,
                    LastOptimizationTime = _statistics.LastOptimizationTime,
                    LastCacheClearTime = _statistics.LastCacheClearTime
                };

                // Calculate hit ratio
                stats.CacheHitRatio = stats.CacheHits + stats.CacheMisses > 0
                    ? (double)stats.CacheHits / (stats.CacheHits + stats.CacheMisses)
                    : 0.0;

                // Get top performance metrics
                stats.TopPerformanceMetrics = GetTopPerformanceMetrics(10);

                return stats;
            }
            finally
            {
                if (_cacheLock.IsReadLockHeld)
                    _cacheLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Preloads frequently used blocks into cache
        /// </summary>
        public void PreloadFrequentBlocks(IEnumerable<string> blockNames)
        {
            if (blockNames == null) return;

            var sw = Stopwatch.StartNew();
            var preloadedCount = 0;

            try
            {
                foreach (var blockName in blockNames.Take(MaxCacheSize / 2)) // Limit preload size
                {
                    if (!_blockCache.ContainsKey(blockName))
                    {
                        // This would typically load from a data source
                        // For now, we just mark it as preloaded
                        var placeholderInfo = new CachedBlockInfo
                        {
                            BlockInfo = null, // Would be loaded from source
                            CacheTime = DateTime.UtcNow,
                            AccessCount = 0,
                            LastAccessed = DateTime.UtcNow,
                            IsPreloaded = true
                        };

                        _blockCache.TryAdd(blockName, placeholderInfo);
                        preloadedCount++;
                    }
                }

                sw.Stop();
                LogOperation($"Preloaded {preloadedCount} blocks in {sw.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                LogError("Error preloading frequent blocks", ex);
            }
        }

        /// <summary>
        /// Gets cache efficiency metrics
        /// </summary>
        public CacheEfficiencyMetrics GetCacheEfficiencyMetrics()
        {
            try
            {
                _cacheLock.EnterReadLock();

                var totalRequests = _statistics.CacheHits + _statistics.CacheMisses;
                var metrics = new CacheEfficiencyMetrics
                {
                    TotalRequests = totalRequests,
                    HitRate = totalRequests > 0 ? (double)_statistics.CacheHits / totalRequests : 0.0,
                    MissRate = totalRequests > 0 ? (double)_statistics.CacheMisses / totalRequests : 0.0,
                    CacheUtilization = MaxCacheSize > 0 ? (double)_blockCache.Count / MaxCacheSize : 0.0,
                    AverageAccessCount = _blockCache.Values.Any() ? _blockCache.Values.Average(c => c.AccessCount) : 0.0,
                    ExpiredEntries = _statistics.CacheExpired,
                    PreloadedEntries = _blockCache.Values.Count(c => c.IsPreloaded)
                };

                return metrics;
            }
            finally
            {
                if (_cacheLock.IsReadLockHeld)
                    _cacheLock.ExitReadLock();
            }
        }

        #endregion

        #region Phase 7 — Cache Improvements

        /// <summary>
        /// Removes a single block from the cache (external-change invalidation).
        /// </summary>
        public void InvalidateBlockCache(string blockName)
        {
            if (string.IsNullOrWhiteSpace(blockName)) return;
            if (_blockCache.TryRemove(blockName, out _))
            {
                Interlocked.Increment(ref _evictionCount);
                LogOperation($"Cache invalidated for block '{blockName}'");
            }
        }

        /// <summary>
        /// Sets a per-block TTL override. Use <see cref="TimeSpan.Zero"/> to remove the override.
        /// </summary>
        public void SetBlockCacheTtl(string blockName, TimeSpan ttl)
        {
            if (string.IsNullOrWhiteSpace(blockName)) return;
            if (ttl == TimeSpan.Zero)
                _blockTtl.TryRemove(blockName, out _);
            else
                _blockTtl[blockName] = ttl;
        }

        /// <summary>
        /// Returns a lightweight cache stats snapshot (hit/miss/eviction/size).
        /// </summary>
        public CacheStats GetCacheStats()
        {
            var stats = GetPerformanceStatistics();
            return new CacheStats
            {
                Hits               = stats.CacheHits,
                Misses             = stats.CacheMisses,
                Evictions          = Interlocked.Read(ref _evictionCount) + stats.CacheExpired,
                CurrentSize        = _blockCache.Count,
                EstimatedMemoryBytes = _blockCache.Count * 256L
            };
        }

        /// <summary>
        /// Checks estimated managed memory against <paramref name="thresholdBytes"/>.
        /// If exceeded, evicts the least-recently-used half of the cache.
        /// </summary>
        public void CheckMemoryPressure(long thresholdBytes = 256 * 1024 * 1024)
        {
            var usedBytes = GC.GetTotalMemory(false);
            if (usedBytes < thresholdBytes) return;

            LogOperation($"Memory pressure detected ({usedBytes / (1024 * 1024)} MB). Evicting LRU cache entries.");

            if (!_cacheLock.TryEnterWriteLock(TimeSpan.FromSeconds(3))) return;
            try
            {
                var half = _blockCache.Count / 2;
                if (half <= 0) return;

                var lruKeys = _blockCache
                    .OrderBy(kvp => kvp.Value.LastAccessed)
                    .Take(half)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in lruKeys)
                {
                    _blockCache.TryRemove(key, out _);
                    Interlocked.Increment(ref _evictionCount);
                }

                LogOperation($"LRU eviction removed {lruKeys.Count} entries due to memory pressure.");
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }

        #endregion

        #region Private Helper Methods

        private void CleanupExpiredCache(object state)
        {
            try
            {
                CleanupExpiredCacheInternal();
            }
            catch (Exception ex)
            {
                LogError("Error during scheduled cache cleanup", ex);
            }
        }

        private void CleanupExpiredCacheInternal()
        {
            if (!_cacheLock.TryEnterWriteLock(TimeSpan.FromSeconds(5)))
                return;

            try
            {
                var expiredKeys = _blockCache
                    .Where(kvp => DateTime.UtcNow.Subtract(kvp.Value.CacheTime) > CacheExpirationTime)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in expiredKeys)
                {
                    _blockCache.TryRemove(key, out _);
                    _statistics.CacheExpired++;
                }

                if (expiredKeys.Count > 0)
                {
                    LogOperation($"Cleaned up {expiredKeys.Count} expired cache entries");
                }

                // Phase 7: log hit/miss ratio periodically
                var total = _statistics.CacheHits + _statistics.CacheMisses;
                if (total > 0)
                {
                    var hitRate = (double)_statistics.CacheHits / total * 100.0;
                    LogOperation($"Cache stats — hits: {_statistics.CacheHits}, misses: {_statistics.CacheMisses}, hit rate: {hitRate:F1}%, size: {_blockCache.Count}");
                }
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }

        private void OptimizeCacheLayout()
        {
            // Move frequently accessed items to optimize retrieval
            var frequentlyAccessed = _blockCache
                .Where(kvp => kvp.Value.AccessCount > 10)
                .OrderByDescending(kvp => kvp.Value.AccessCount)
                .Take(MaxCacheSize / 4) // Top 25% most accessed
                .ToList();

            // Update their cache time to keep them longer
            foreach (var item in frequentlyAccessed)
            {
                item.Value.CacheTime = DateTime.UtcNow;
            }
        }

        private void RemoveOldestCacheEntries()
        {
            var entriesToRemove = _blockCache.Count - MaxCacheSize + 100; // Remove extra to avoid frequent cleanup
            if (entriesToRemove <= 0) return;

            var oldestEntries = _blockCache
                .OrderBy(kvp => kvp.Value.LastAccessed)
                .Take(entriesToRemove)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in oldestEntries)
            {
                _blockCache.TryRemove(key, out _);
                Interlocked.Increment(ref _evictionCount);
            }

            LogOperation($"LRU: removed {oldestEntries.Count} oldest cache entries to maintain size limit");
        }

        private void RecordPerformanceMetric(string operationName, TimeSpan duration)
        {
            if (!EnableDetailedMetrics) return;

            _performanceMetrics.AddOrUpdate(operationName, 
                new PerformanceMetric { OperationName = operationName, Duration = duration, Count = 1 },
                (key, existing) =>
                {
                    existing.Duration = TimeSpan.FromTicks((existing.Duration.Ticks * existing.Count + duration.Ticks) / (existing.Count + 1));
                    existing.Count++;
                    return existing;
                });
        }

        private List<PerformanceMetric> GetTopPerformanceMetrics(int count)
        {
            return _performanceMetrics.Values
                .OrderByDescending(pm => pm.Count)
                .Take(count)
                .ToList();
        }

        private TimeSpan UpdateAverageTime(TimeSpan currentAverage, TimeSpan newTime, long count)
        {
            if (count <= 1) return newTime;
            
            var totalTicks = currentAverage.Ticks * (count - 1) + newTime.Ticks;
            return TimeSpan.FromTicks(totalTicks / count);
        }

        private void LogOperation(string message)
        {
            _dmeEditor?.AddLogMessage("PerformanceManager", message, DateTime.Now, 0, null, Errors.Ok);
        }

        private void LogError(string message, Exception ex)
        {
            _dmeEditor?.AddLogMessage("PerformanceManager", $"{message}: {ex?.Message}", DateTime.Now, -1, null, Errors.Failed);
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Releases cache cleanup resources and clears cached state.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                _cacheCleanupTimer?.Dispose();
                _cacheLock?.Dispose();
                _blockCache.Clear();
                _performanceMetrics.Clear();
            }
            catch (Exception ex)
            {
                LogError("Error during PerformanceManager disposal", ex);
            }
            finally
            {
                _disposed = true;
            }
        }

        #endregion
    }
}