using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Caching
{
    /// <summary>
    /// Extension methods for cache operations to provide additional functionality.
    /// </summary>
    public static partial class CacheManager
    {
        #region Tagged Cache Operations
        /// <summary>
        /// Sets a value in the cache with associated tags for grouped operations.
        /// </summary>
        /// <typeparam name="T">The type of the value to cache.</typeparam>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The value to cache.</param>
        /// <param name="tags">Tags associated with this cache entry.</param>
        /// <param name="expiry">Optional expiration time.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the value was cached successfully.</returns>
        public static async Task<bool> SetWithTagsAsync<T>(
            string key, 
            T value, 
            IEnumerable<string> tags, 
            TimeSpan? expiry = null,
            CancellationToken cancellationToken = default)
        {
            if (tags == null)
                return await SetAsync(key, value, expiry, cancellationToken);

            // Store the main value
            var success = await SetAsync(key, value, expiry, cancellationToken);
            
            if (success)
            {
                // Store tag associations
                foreach (var tag in tags)
                {
                    var tagKey = $"tag:{tag}";
                    var taggedKeys = await GetAsync<HashSet<string>>(tagKey, cancellationToken) ?? new HashSet<string>();
                    taggedKeys.Add(key);
                    await SetAsync(tagKey, taggedKeys, TimeSpan.FromDays(30), cancellationToken); // Long expiry for tags
                }
            }

            return success;
        }

        /// <summary>
        /// Removes all cache entries associated with the specified tag.
        /// </summary>
        /// <param name="tag">The tag to remove.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The number of entries removed.</returns>
        public static async Task<long> RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return 0;

            var tagKey = $"tag:{tag}";
            var taggedKeys = await GetAsync<HashSet<string>>(tagKey, cancellationToken);
            
            if (taggedKeys == null || taggedKeys.Count == 0)
                return 0;

            long removedCount = 0;
            foreach (var key in taggedKeys)
            {
                if (await RemoveAsync(key, cancellationToken))
                    removedCount++;
            }

            // Remove the tag itself
            await RemoveAsync(tagKey, cancellationToken);
            
            return removedCount;
        }
        #endregion

        #region Conditional Cache Operations
        /// <summary>
        /// Sets a value in the cache only if the key doesn't already exist.
        /// </summary>
        /// <typeparam name="T">The type of the value to cache.</typeparam>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The value to cache.</param>
        /// <param name="expiry">Optional expiration time.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the value was cached (key didn't exist).</returns>
        public static async Task<bool> SetIfNotExistsAsync<T>(
            string key, 
            T value, 
            TimeSpan? expiry = null,
            CancellationToken cancellationToken = default)
        {
            if (await ExistsAsync(key, cancellationToken))
                return false;

            return await SetAsync(key, value, expiry, cancellationToken);
        }

        /// <summary>
        /// Gets a value from cache and removes it atomically.
        /// </summary>
        /// <typeparam name="T">The type of the cached value.</typeparam>
        /// <param name="key">The cache key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The cached value or default(T) if not found.</returns>
        public static async Task<T> GetAndRemoveAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            var value = await GetAsync<T>(key, cancellationToken);
            if (!EqualityComparer<T>.Default.Equals(value, default(T)))
            {
                await RemoveAsync(key, cancellationToken);
            }
            return value;
        }

        /// <summary>
        /// Refreshes the expiration time of a cached item.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="expiry">The new expiration time.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the expiration was refreshed.</returns>
        public static async Task<bool> RefreshAsync(string key, TimeSpan expiry, CancellationToken cancellationToken = default)
        {
            EnsureInitialized();

            if (string.IsNullOrWhiteSpace(key))
                return false;

            bool refreshed = false;

            // Try to refresh in both providers
            if (_primaryProvider?.IsAvailable == true)
            {
                try
                {
                    refreshed = await _primaryProvider.RefreshAsync(key, expiry, cancellationToken) || refreshed;
                }
                catch
                {
                    // Ignore refresh errors
                }
            }

            if (_fallbackProvider?.IsAvailable == true)
            {
                try
                {
                    refreshed = await _fallbackProvider.RefreshAsync(key, expiry, cancellationToken) || refreshed;
                }
                catch
                {
                    // Ignore refresh errors
                }
            }

            return refreshed;
        }
        #endregion

        #region Cache Warming
        /// <summary>
        /// Warms the cache by pre-loading values using the provided factory functions.
        /// </summary>
        /// <typeparam name="T">The type of the values to cache.</typeparam>
        /// <param name="warmupItems">Dictionary of key-factory pairs for warming up the cache.</param>
        /// <param name="expiry">Optional expiration time for warmed items.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The number of items successfully warmed.</returns>
        public static async Task<long> WarmCacheAsync<T>(
            Dictionary<string, Func<Task<T>>> warmupItems, 
            TimeSpan? expiry = null,
            CancellationToken cancellationToken = default)
        {
            if (warmupItems == null || warmupItems.Count == 0)
                return 0;

            long warmedCount = 0;
            var tasks = new List<Task>();

            foreach (var kvp in warmupItems)
            {
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        if (!await ExistsAsync(kvp.Key, cancellationToken))
                        {
                            var value = await kvp.Value();
                            if (await SetAsync(kvp.Key, value, expiry, cancellationToken))
                            {
                                Interlocked.Increment(ref warmedCount);
                            }
                        }
                    }
                    catch
                    {
                        // Ignore individual warming failures
                    }
                }, cancellationToken));
            }

            await Task.WhenAll(tasks);
            return warmedCount;
        }
        #endregion

        #region Distributed Lock Operations (for coordination across instances)
        /// <summary>
        /// Attempts to acquire a distributed lock using the cache.
        /// </summary>
        /// <param name="lockKey">The lock key.</param>
        /// <param name="lockValue">The lock value (usually a unique identifier).</param>
        /// <param name="expiry">Lock expiration time.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the lock was acquired.</returns>
        public static async Task<bool> TryAcquireLockAsync(
            string lockKey, 
            string lockValue, 
            TimeSpan expiry,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(lockKey) || string.IsNullOrWhiteSpace(lockValue))
                return false;

            var fullLockKey = $"lock:{lockKey}";
            return await SetIfNotExistsAsync(fullLockKey, lockValue, expiry, cancellationToken);
        }

        /// <summary>
        /// Releases a distributed lock.
        /// </summary>
        /// <param name="lockKey">The lock key.</param>
        /// <param name="lockValue">The lock value to verify ownership.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the lock was released.</returns>
        public static async Task<bool> ReleaseLockAsync(
            string lockKey, 
            string lockValue, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(lockKey) || string.IsNullOrWhiteSpace(lockValue))
                return false;

            var fullLockKey = $"lock:{lockKey}";
            var currentValue = await GetAsync<string>(fullLockKey, cancellationToken);
            
            if (currentValue == lockValue)
            {
                return await RemoveAsync(fullLockKey, cancellationToken);
            }

            return false;
        }

        /// <summary>
        /// Executes an action while holding a distributed lock.
        /// </summary>
        /// <param name="lockKey">The lock key.</param>
        /// <param name="action">The action to execute.</param>
        /// <param name="lockTimeout">Lock timeout.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the action was executed successfully.</returns>
        public static async Task<bool> ExecuteWithLockAsync(
            string lockKey, 
            Func<Task> action, 
            TimeSpan lockTimeout,
            CancellationToken cancellationToken = default)
        {
            var lockValue = Guid.NewGuid().ToString();
            
            if (await TryAcquireLockAsync(lockKey, lockValue, lockTimeout, cancellationToken))
            {
                try
                {
                    await action();
                    return true;
                }
                finally
                {
                    await ReleaseLockAsync(lockKey, lockValue, cancellationToken);
                }
            }

            return false;
        }
        #endregion

        #region Cache Health Monitoring
        /// <summary>
        /// Performs a health check on all cache providers.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Health check results.</returns>
        public static async Task<CacheHealthStatus> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            EnsureInitialized();

            var result = new CacheHealthStatus
            {
                CheckTime = DateTimeOffset.UtcNow
            };

            // Test primary provider
            if (_primaryProvider != null)
            {
                result.PrimaryProviderHealth = await TestProviderHealthAsync(_primaryProvider, cancellationToken);
            }

            // Test fallback provider
            if (_fallbackProvider != null)
            {
                result.FallbackProviderHealth = await TestProviderHealthAsync(_fallbackProvider, cancellationToken);
            }

            result.OverallHealth = (result.PrimaryProviderHealth?.IsHealthy ?? false) || 
                                 (result.FallbackProviderHealth?.IsHealthy ?? false);

            return result;
        }

        private static async Task<ProviderHealthStatus> TestProviderHealthAsync(ICacheProvider provider, CancellationToken cancellationToken)
        {
            var health = new ProviderHealthStatus
            {
                ProviderName = provider.Name,
                IsAvailable = provider.IsAvailable
            };

            if (!provider.IsAvailable)
            {
                health.IsHealthy = false;
                health.ErrorMessage = "Provider is not available";
                return health;
            }

            try
            {
                // Perform a simple round-trip test
                var testKey = $"health-check-{Guid.NewGuid()}";
                var testValue = "health-test";
                var startTime = DateTimeOffset.UtcNow;

                var setResult = await provider.SetAsync(testKey, testValue, TimeSpan.FromMinutes(1), cancellationToken);
                var getValue = await provider.GetAsync<string>(testKey, cancellationToken);
                var removeResult = await provider.RemoveAsync(testKey, cancellationToken);

                health.ResponseTime = DateTimeOffset.UtcNow - startTime;
                health.IsHealthy = setResult && getValue == testValue && removeResult;

                if (!health.IsHealthy)
                {
                    health.ErrorMessage = "Round-trip test failed";
                }
            }
            catch (Exception ex)
            {
                health.IsHealthy = false;
                health.ErrorMessage = ex.Message;
            }

            return health;
        }
        #endregion
    }

    /// <summary>
    /// Cache health status information.
    /// </summary>
    public class CacheHealthStatus
    {
        /// <summary>Gets or sets the time when the health check was performed.</summary>
        public DateTimeOffset CheckTime { get; set; }

        /// <summary>Gets or sets the overall health status.</summary>
        public bool OverallHealth { get; set; }

        /// <summary>Gets or sets the primary provider health status.</summary>
        public ProviderHealthStatus PrimaryProviderHealth { get; set; }

        /// <summary>Gets or sets the fallback provider health status.</summary>
        public ProviderHealthStatus FallbackProviderHealth { get; set; }
    }

    /// <summary>
    /// Individual provider health status.
    /// </summary>
    public class ProviderHealthStatus
    {
        /// <summary>Gets or sets the provider name.</summary>
        public string ProviderName { get; set; }

        /// <summary>Gets or sets whether the provider is available.</summary>
        public bool IsAvailable { get; set; }

        /// <summary>Gets or sets whether the provider is healthy.</summary>
        public bool IsHealthy { get; set; }

        /// <summary>Gets or sets the response time for health check operations.</summary>
        public TimeSpan ResponseTime { get; set; }

        /// <summary>Gets or sets any error message from health check.</summary>
        public string ErrorMessage { get; set; }
    }
}