using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Caching
{
    /// <summary>
    /// Interface for cache providers that can be used with the CacheManager.
    /// Supports various caching backends like In-Memory, Redis, MemoryCache, etc.
    /// </summary>
    public interface ICacheProvider : IDisposable
    {
        /// <summary>Gets the name of the cache provider.</summary>
        string Name { get; }

        /// <summary>Gets a value indicating whether the provider is available.</summary>
        bool IsAvailable { get; }

        /// <summary>Gets cache statistics.</summary>
        CacheStatistics Statistics { get; }

        /// <summary>Gets a cached value by key.</summary>
        /// <typeparam name="T">The type of the cached value.</typeparam>
        /// <param name="key">The cache key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The cached value or default(T) if not found.</returns>
        Task<T> GetAsync<T>(string key, CancellationToken cancellationToken = default);

        /// <summary>Sets a value in the cache.</summary>
        /// <typeparam name="T">The type of the value to cache.</typeparam>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The value to cache.</param>
        /// <param name="expiry">The expiration time.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the value was cached successfully.</returns>
        Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default);

        /// <summary>Removes a value from the cache.</summary>
        /// <param name="key">The cache key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the value was removed.</returns>
        Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>Checks if a key exists in the cache.</summary>
        /// <param name="key">The cache key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the key exists and is not expired.</returns>
        Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>Clears the entire cache or keys matching a pattern.</summary>
        /// <param name="pattern">The pattern to match keys (optional).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The number of keys removed.</returns>
        Task<long> ClearAsync(string pattern = null, CancellationToken cancellationToken = default);

        /// <summary>Gets multiple values from the cache.</summary>
        /// <typeparam name="T">The type of the cached values.</typeparam>
        /// <param name="keys">The cache keys.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Dictionary with found values.</returns>
        Task<Dictionary<string, T>> GetManyAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default);

        /// <summary>Sets multiple values in the cache.</summary>
        /// <typeparam name="T">The type of the values to cache.</typeparam>
        /// <param name="values">Dictionary of key-value pairs to cache.</param>
        /// <param name="expiry">The expiration time for all values.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The number of values successfully cached.</returns>
        Task<long> SetManyAsync<T>(Dictionary<string, T> values, TimeSpan? expiry = null, CancellationToken cancellationToken = default);

        /// <summary>Refreshes the expiration time of a cached item.</summary>
        /// <param name="key">The cache key.</param>
        /// <param name="expiry">The new expiration time.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the expiration was refreshed.</returns>
        Task<bool> RefreshAsync(string key, TimeSpan expiry, CancellationToken cancellationToken = default);
    }
}