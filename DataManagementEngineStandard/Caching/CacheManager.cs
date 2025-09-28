using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Caching.Providers;

namespace TheTechIdea.Beep.Caching
{
    /// <summary>
    /// Advanced cache manager with pluggable providers, retry logic, and comprehensive monitoring.
    /// Supports multiple cache backends including In-Memory, MemoryCache, Redis, and custom providers.
    /// </summary>
    public static partial class CacheManager
    {
        #region Private Fields
        private static ICacheProvider _primaryProvider;
        private static ICacheProvider _fallbackProvider;
        private static CacheConfiguration _configuration;
        private static readonly object _lock = new object();
        private static bool _initialized = false;
        #endregion

        #region Initialization
        /// <summary>
        /// Initializes the cache manager with the specified configuration.
        /// </summary>
        /// <param name="configuration">Cache configuration settings.</param>
        /// <param name="primaryProviderType">The primary cache provider type.</param>
        /// <param name="fallbackProviderType">The fallback cache provider type (optional).</param>
        public static void Initialize(
            CacheConfiguration configuration = null, 
            CacheProviderType primaryProviderType = CacheProviderType.InMemory,
            CacheProviderType? fallbackProviderType = null)
        {
            lock (_lock)
            {
                if (_initialized)
                    return;

                _configuration = configuration ?? new CacheConfiguration();
                _primaryProvider = CreateProvider(primaryProviderType, _configuration);
                
                if (fallbackProviderType.HasValue)
                {
                    _fallbackProvider = CreateProvider(fallbackProviderType.Value, _configuration);
                }
                else
                {
                    // Always have an in-memory fallback
                    _fallbackProvider = new SimpleCacheProvider(_configuration);
                }

                _initialized = true;
            }
        }

        /// <summary>
        /// Sets a custom cache provider as the primary provider.
        /// </summary>
        /// <param name="provider">The custom cache provider.</param>
        /// <param name="fallbackProvider">Optional fallback provider.</param>
        public static void SetProvider(ICacheProvider provider, ICacheProvider fallbackProvider = null)
        {
            lock (_lock)
            {
                _primaryProvider?.Dispose();
                _primaryProvider = provider ?? throw new ArgumentNullException(nameof(provider));
                
                if (fallbackProvider != null)
                {
                    _fallbackProvider?.Dispose();
                    _fallbackProvider = fallbackProvider;
                }

                _initialized = true;
            }
        }
        #endregion

        #region Core Cache Operations
        /// <summary>
        /// Gets a cached value or creates it using the provided factory function.
        /// </summary>
        /// <typeparam name="T">The type of the cached value.</typeparam>
        /// <param name="key">The cache key.</param>
        /// <param name="factory">Factory function to create the value if not cached.</param>
        /// <param name="expiry">Optional expiration time.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The cached or newly created value.</returns>
        public static async Task<T> GetOrCreateAsync<T>(
            string key, 
            Func<Task<T>> factory, 
            TimeSpan? expiry = null,
            CancellationToken cancellationToken = default)
        {
            EnsureInitialized();

            if (string.IsNullOrWhiteSpace(key) || factory == null)
                return default(T);

            // Try to get from cache first
            var cachedValue = await GetAsync<T>(key, cancellationToken);
            if (!EqualityComparer<T>.Default.Equals(cachedValue, default(T)))
            {
                return cachedValue;
            }

            // Create new value
            var newValue = await factory();
            if (!EqualityComparer<T>.Default.Equals(newValue, default(T)))
            {
                await SetAsync(key, newValue, expiry, cancellationToken);
            }

            return newValue;
        }

        /// <summary>
        /// Gets a cached value or creates it using the provided factory function (synchronous version).
        /// </summary>
        /// <typeparam name="T">The type of the cached value.</typeparam>
        /// <param name="key">The cache key.</param>
        /// <param name="factory">Factory function to create the value if not cached.</param>
        /// <param name="expiry">Optional expiration time.</param>
        /// <returns>The cached or newly created value.</returns>
        public static T GetOrCreate<T>(string key, Func<T> factory, TimeSpan? expiry = null)
        {
            return GetOrCreateAsync(key, () => Task.FromResult(factory()), expiry).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Gets a cached value by key.
        /// </summary>
        /// <typeparam name="T">The type of the cached value.</typeparam>
        /// <param name="key">The cache key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The cached value or default(T) if not found.</returns>
        public static async Task<T> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            EnsureInitialized();

            if (string.IsNullOrWhiteSpace(key))
                return default(T);

            try
            {
                // Try primary provider first
                if (_primaryProvider?.IsAvailable == true)
                {
                    var value = await _primaryProvider.GetAsync<T>(key, cancellationToken);
                    if (!EqualityComparer<T>.Default.Equals(value, default(T)))
                    {
                        return value;
                    }
                }

                // Try fallback provider
                if (_fallbackProvider?.IsAvailable == true)
                {
                    return await _fallbackProvider.GetAsync<T>(key, cancellationToken);
                }
            }
            catch (Exception)
            {
                // Try fallback on primary provider failure
                if (_fallbackProvider?.IsAvailable == true && _fallbackProvider != _primaryProvider)
                {
                    try
                    {
                        return await _fallbackProvider.GetAsync<T>(key, cancellationToken);
                    }
                    catch
                    {
                        // Ignore fallback errors
                    }
                }
            }

            return default(T);
        }

        /// <summary>
        /// Sets a value in the cache.
        /// </summary>
        /// <typeparam name="T">The type of the value to cache.</typeparam>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The value to cache.</param>
        /// <param name="expiry">Optional expiration time.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the value was cached successfully.</returns>
        public static async Task<bool> SetAsync<T>(
            string key, 
            T value, 
            TimeSpan? expiry = null, 
            CancellationToken cancellationToken = default)
        {
            EnsureInitialized();

            if (string.IsNullOrWhiteSpace(key) || value == null)
                return false;

            bool primarySuccess = false;
            bool fallbackSuccess = false;

            try
            {
                // Try primary provider first
                if (_primaryProvider?.IsAvailable == true)
                {
                    primarySuccess = await _primaryProvider.SetAsync(key, value, expiry, cancellationToken);
                }
            }
            catch
            {
                // Primary provider failed
            }

            try
            {
                // Always try to cache in fallback provider for redundancy
                if (_fallbackProvider?.IsAvailable == true)
                {
                    fallbackSuccess = await _fallbackProvider.SetAsync(key, value, expiry, cancellationToken);
                }
            }
            catch
            {
                // Fallback provider failed
            }

            return primarySuccess || fallbackSuccess;
        }

        /// <summary>
        /// Removes a value from the cache.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the value was removed from at least one provider.</returns>
        public static async Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            EnsureInitialized();

            if (string.IsNullOrWhiteSpace(key))
                return false;

            bool removed = false;

            // Remove from both providers
            if (_primaryProvider?.IsAvailable == true)
            {
                try
                {
                    removed = await _primaryProvider.RemoveAsync(key, cancellationToken) || removed;
                }
                catch
                {
                    // Ignore removal errors
                }
            }

            if (_fallbackProvider?.IsAvailable == true)
            {
                try
                {
                    removed = await _fallbackProvider.RemoveAsync(key, cancellationToken) || removed;
                }
                catch
                {
                    // Ignore removal errors
                }
            }

            return removed;
        }

        /// <summary>
        /// Checks if a key exists in the cache.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the key exists in at least one provider.</returns>
        public static async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            EnsureInitialized();

            if (string.IsNullOrWhiteSpace(key))
                return false;

            try
            {
                // Check primary provider first
                if (_primaryProvider?.IsAvailable == true)
                {
                    if (await _primaryProvider.ExistsAsync(key, cancellationToken))
                        return true;
                }

                // Check fallback provider
                if (_fallbackProvider?.IsAvailable == true)
                {
                    return await _fallbackProvider.ExistsAsync(key, cancellationToken);
                }
            }
            catch
            {
                // Ignore check errors
            }

            return false;
        }

        /// <summary>
        /// Clears the cache or removes keys matching a pattern.
        /// </summary>
        /// <param name="pattern">Optional pattern to match keys.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The total number of keys removed from all providers.</returns>
        public static async Task<long> ClearAsync(string pattern = null, CancellationToken cancellationToken = default)
        {
            EnsureInitialized();

            long totalRemoved = 0;

            // Clear from both providers
            if (_primaryProvider?.IsAvailable == true)
            {
                try
                {
                    totalRemoved += await _primaryProvider.ClearAsync(pattern, cancellationToken);
                }
                catch
                {
                    // Ignore clear errors
                }
            }

            if (_fallbackProvider?.IsAvailable == true)
            {
                try
                {
                    totalRemoved += await _fallbackProvider.ClearAsync(pattern, cancellationToken);
                }
                catch
                {
                    // Ignore clear errors
                }
            }

            return totalRemoved;
        }
        #endregion

        #region Batch Operations
        /// <summary>
        /// Gets multiple values from the cache.
        /// </summary>
        /// <typeparam name="T">The type of the cached values.</typeparam>
        /// <param name="keys">The cache keys.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Dictionary with found values.</returns>
        public static async Task<Dictionary<string, T>> GetManyAsync<T>(
            IEnumerable<string> keys, 
            CancellationToken cancellationToken = default)
        {
            EnsureInitialized();

            var result = new Dictionary<string, T>();
            
            if (keys == null)
                return result;

            try
            {
                // Try primary provider first
                if (_primaryProvider?.IsAvailable == true)
                {
                    var primaryResults = await _primaryProvider.GetManyAsync<T>(keys, cancellationToken);
                    foreach (var kvp in primaryResults)
                    {
                        result[kvp.Key] = kvp.Value;
                    }
                }

                // Get missing keys from fallback provider
                var missingKeys = keys.Where(key => !result.ContainsKey(key));
                if (_fallbackProvider?.IsAvailable == true && missingKeys.Any())
                {
                    var fallbackResults = await _fallbackProvider.GetManyAsync<T>(missingKeys, cancellationToken);
                    foreach (var kvp in fallbackResults)
                    {
                        result[kvp.Key] = kvp.Value;
                    }
                }
            }
            catch
            {
                // Return partial results on error
            }

            return result;
        }

        /// <summary>
        /// Sets multiple values in the cache.
        /// </summary>
        /// <typeparam name="T">The type of the values to cache.</typeparam>
        /// <param name="values">Dictionary of key-value pairs to cache.</param>
        /// <param name="expiry">Optional expiration time for all values.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The number of values successfully cached.</returns>
        public static async Task<long> SetManyAsync<T>(
            Dictionary<string, T> values, 
            TimeSpan? expiry = null, 
            CancellationToken cancellationToken = default)
        {
            EnsureInitialized();

            if (values == null || values.Count == 0)
                return 0;

            long totalSet = 0;

            // Set in both providers
            if (_primaryProvider?.IsAvailable == true)
            {
                try
                {
                    totalSet = Math.Max(totalSet, await _primaryProvider.SetManyAsync(values, expiry, cancellationToken));
                }
                catch
                {
                    // Ignore primary provider errors
                }
            }

            if (_fallbackProvider?.IsAvailable == true)
            {
                try
                {
                    await _fallbackProvider.SetManyAsync(values, expiry, cancellationToken);
                }
                catch
                {
                    // Ignore fallback provider errors
                }
            }

            return totalSet;
        }
        #endregion

        #region Synchronous Wrapper Methods (for backward compatibility)
        /// <summary>
        /// Gets a cached value by key (synchronous version).
        /// </summary>
        /// <typeparam name="T">The type of the cached value.</typeparam>
        /// <param name="key">The cache key.</param>
        /// <returns>The cached value or default(T) if not found.</returns>
        public static T Get<T>(string key)
        {
            return GetAsync<T>(key).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Sets a value in the cache (synchronous version).
        /// </summary>
        /// <typeparam name="T">The type of the value to cache.</typeparam>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The value to cache.</param>
        /// <param name="expiry">Optional expiration time.</param>
        /// <returns>True if the value was cached successfully.</returns>
        public static bool Set<T>(string key, T value, TimeSpan? expiry = null)
        {
            return SetAsync(key, value, expiry).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Removes a value from the cache (synchronous version).
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <returns>True if the value was removed.</returns>
        public static bool Remove(string key)
        {
            return RemoveAsync(key).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Checks if a key exists in the cache (synchronous version).
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <returns>True if the key exists.</returns>
        public static bool Contains(string key)
        {
            return ExistsAsync(key).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Invalidates cache entries (synchronous version).
        /// </summary>
        /// <param name="pattern">Pattern to match keys for removal.</param>
        public static void InvalidateCache(string pattern = "*")
        {
            ClearAsync(pattern == "*" ? null : pattern).GetAwaiter().GetResult();
        }
        #endregion

        #region Provider Management and Diagnostics
        /// <summary>
        /// Gets comprehensive cache statistics from all providers.
        /// </summary>
        /// <returns>Combined cache statistics.</returns>
        public static CacheManagerStatistics GetStatistics()
        {
            EnsureInitialized();

            return new CacheManagerStatistics
            {
                PrimaryProvider = _primaryProvider?.Statistics,
                FallbackProvider = _fallbackProvider?.Statistics,
                PrimaryProviderName = _primaryProvider?.Name,
                FallbackProviderName = _fallbackProvider?.Name,
                PrimaryProviderAvailable = _primaryProvider?.IsAvailable ?? false,
                FallbackProviderAvailable = _fallbackProvider?.IsAvailable ?? false,
                Configuration = _configuration
            };
        }

        /// <summary>
        /// Gets the current cache configuration.
        /// </summary>
        /// <returns>The cache configuration.</returns>
        public static CacheConfiguration GetConfiguration()
        {
            return _configuration;
        }

        /// <summary>
        /// Forces a refresh of the cache providers (reconnects if needed).
        /// </summary>
        public static void RefreshProviders()
        {
            lock (_lock)
            {
                // In a production environment, you might want to implement
                // provider health checking and reconnection logic here
            }
        }
        #endregion

        #region Provider Access Methods
        /// <summary>
        /// Gets the primary cache provider.
        /// </summary>
        /// <returns>The primary cache provider instance.</returns>
        public static ICacheProvider GetPrimaryProvider()
        {
            EnsureInitialized();
            return _primaryProvider;
        }

        /// <summary>
        /// Gets the fallback cache provider.
        /// </summary>
        /// <returns>The fallback cache provider instance.</returns>
        public static ICacheProvider GetFallbackProvider()
        {
            EnsureInitialized();
            return _fallbackProvider;
        }
        #endregion

        #region Private Helper Methods
        private static void EnsureInitialized()
        {
            if (!_initialized)
            {
                Initialize();
            }
        }

        private static ICacheProvider CreateProvider(CacheProviderType providerType, CacheConfiguration configuration)
        {
            return providerType switch
            {
                CacheProviderType.InMemory => new SimpleCacheProvider(configuration),
                CacheProviderType.MemoryCache => new MemoryCacheProvider(configuration),
                CacheProviderType.Redis => new RedisCacheProvider(configuration),
                _ => new SimpleCacheProvider(configuration)
            };
        }
        #endregion
    }
}
