using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Caching.Providers;

namespace TheTechIdea.Beep.Caching
{
    /// <summary>
    /// Advanced cache manager with pluggable providers, stale-while-revalidate support,
    /// scoped provider access, and SLO-ready statistics.
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

        // SWR: tracks "soft" expiry (stale window) per key — key → soft-expiry time
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, DateTime>
            _swrExpiry = new System.Collections.Concurrent.ConcurrentDictionary<string, DateTime>(StringComparer.Ordinal);

        // SWR: guards against stampede — only one background refresh per key at a time
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, int>
            _swrInFlight = new System.Collections.Concurrent.ConcurrentDictionary<string, int>(StringComparer.Ordinal);
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
        /// Uses an explicit hit/miss contract — null, 0, false, and empty collections are valid
        /// cached values and are NOT treated as cache misses.
        /// </summary>
        public static async Task<T> GetOrCreateAsync<T>(
            string key,
            Func<Task<T>> factory,
            TimeSpan? expiry = null,
            CancellationToken cancellationToken = default)
        {
            EnsureInitialized();

            if (string.IsNullOrWhiteSpace(key) || factory == null)
                return default;

            // Use presence check (ExistsAsync) rather than value equality so that
            // valid cached values of null / 0 / false / empty are honoured as hits.
            if (await ExistsAsync(key, cancellationToken).ConfigureAwait(false))
                return await GetAsync<T>(key, cancellationToken).ConfigureAwait(false);

            var newValue = await factory().ConfigureAwait(false);
            await SetAsync(key, newValue, expiry, cancellationToken).ConfigureAwait(false);
            return newValue;
        }

        /// <summary>
        /// Gets a cached value or creates it using stale-while-revalidate semantics.
        /// Returns a (possibly stale) cached value immediately; if the entry is older than
        /// <paramref name="softTtl"/>, a background refresh is triggered so the next caller
        /// will receive fresh data. The hard TTL (<paramref name="hardTtl"/>) is when the
        /// item is fully evicted and the next call will block on the factory.
        /// </summary>
        /// <param name="key">Cache key.</param>
        /// <param name="factory">Async factory invoked on misses and background refreshes.</param>
        /// <param name="softTtl">How long a value is considered fresh (serve without refresh).</param>
        /// <param name="hardTtl">How long the value stays in cache after softTtl passes.</param>
        /// <param name="cancellationToken">Cancellation token (only used for blocking path).</param>
        public static async Task<T> GetOrCreateWithStaleAsync<T>(
            string key,
            Func<Task<T>> factory,
            TimeSpan softTtl,
            TimeSpan hardTtl,
            CancellationToken cancellationToken = default)
        {
            EnsureInitialized();

            if (string.IsNullOrWhiteSpace(key) || factory == null)
                return default;

            bool exists = await ExistsAsync(key, cancellationToken).ConfigureAwait(false);

            if (exists)
            {
                // Check whether soft TTL has expired
                bool stale = _swrExpiry.TryGetValue(key, out var softExpiry)
                          && DateTime.UtcNow > softExpiry;

                if (!stale)
                    return await GetAsync<T>(key, cancellationToken).ConfigureAwait(false);

                // Stale but still in hard-TTL window — serve stale + background refresh
                var staleValue = await GetAsync<T>(key, cancellationToken).ConfigureAwait(false);

                // Only launch one background refresh per key
                if (_swrInFlight.TryAdd(key, 1))
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var fresh = await factory().ConfigureAwait(false);
                            await SetAsync(key, fresh, hardTtl).ConfigureAwait(false);
                            _swrExpiry[key] = DateTime.UtcNow.Add(softTtl);
                        }
                        finally
                        {
                            _swrInFlight.TryRemove(key, out _);
                        }
                    });
                }

                return staleValue;
            }

            // Hard miss — blocking factory call
            var value = await factory().ConfigureAwait(false);
            await SetAsync(key, value, hardTtl, cancellationToken).ConfigureAwait(false);
            _swrExpiry[key] = DateTime.UtcNow.Add(softTtl);
            return value;
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
        /// Returns default(T) only when the key is genuinely absent — a cached null/0/false/empty
        /// is returned as-is and is NOT treated as a miss.
        /// </summary>
        public static async Task<T> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            EnsureInitialized();

            if (string.IsNullOrWhiteSpace(key))
                return default;

            try
            {
                if (_primaryProvider?.IsAvailable == true)
                {
                    // Existence check guards against default(T) false-miss
                    if (await _primaryProvider.ExistsAsync(key, cancellationToken).ConfigureAwait(false))
                        return await _primaryProvider.GetAsync<T>(key, cancellationToken).ConfigureAwait(false);
                }

                if (_fallbackProvider?.IsAvailable == true)
                {
                    if (await _fallbackProvider.ExistsAsync(key, cancellationToken).ConfigureAwait(false))
                        return await _fallbackProvider.GetAsync<T>(key, cancellationToken).ConfigureAwait(false);
                }
            }
            catch
            {
                if (_fallbackProvider?.IsAvailable == true && !ReferenceEquals(_fallbackProvider, _primaryProvider))
                {
                    try
                    {
                        if (await _fallbackProvider.ExistsAsync(key, cancellationToken).ConfigureAwait(false))
                            return await _fallbackProvider.GetAsync<T>(key, cancellationToken).ConfigureAwait(false);
                    }
                    catch { /* swallow — return default below */ }
                }
            }

            return default;
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
        /// Resets the CacheManager to an un-initialized state, disposing current providers.
        /// Primarily useful for testing or hot-provider-swap scenarios.
        /// </summary>
        public static void Reset()
        {
            lock (_lock)
            {
                _primaryProvider?.Dispose();
                _fallbackProvider?.Dispose();
                _primaryProvider  = null;
                _fallbackProvider = null;
                _configuration    = null;
                _swrExpiry.Clear();
                _swrInFlight.Clear();
                _initialized = false;
            }
        }

        /// <summary>
        /// Creates an isolated <see cref="CacheScope"/> backed by a dedicated provider instance.
        /// Use this to give each <see cref="TheTechIdea.Beep.Proxy.ProxyDataSource"/> its own
        /// cache namespace and statistics without sharing global state.
        /// </summary>
        /// <param name="providerType">Provider type for the scope (default: InMemory).</param>
        /// <param name="configuration">Optional configuration override for the scope.</param>
        public static CacheScope CreateScope(
            CacheProviderType providerType = CacheProviderType.InMemory,
            CacheConfiguration configuration = null)
        {
            var cfg      = configuration ?? _configuration ?? new CacheConfiguration();
            var provider = CreateProvider(providerType, cfg);
            return new CacheScope(provider);
        }

        /// <summary>
        /// Creates an isolated <see cref="CacheScope"/> backed by an externally supplied provider.
        /// </summary>
        public static CacheScope CreateScope(ICacheProvider provider)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            return new CacheScope(provider);
        }

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
                CacheProviderType.InMemory        => new InMemoryCacheProvider(configuration),  // high-perf LRU
                CacheProviderType.Simple          => new SimpleCacheProvider(configuration),    // lightweight fallback
                CacheProviderType.MemoryCache     => new MemoryCacheProvider(configuration),
                CacheProviderType.Redis           => new RedisCacheProvider(configuration),
                CacheProviderType.Hybrid          => new HybridCacheProvider(
                                                        new InMemoryCacheProvider(configuration),
                                                        new SimpleCacheProvider(configuration),
                                                        configuration),
                _                                 => new InMemoryCacheProvider(configuration)
            };
        }
        #endregion
    }
}
