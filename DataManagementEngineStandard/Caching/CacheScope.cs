using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Caching.Providers;

namespace TheTechIdea.Beep.Caching
{
    /// <summary>
    /// An isolated, instance-based cache backed by a dedicated <see cref="ICacheProvider"/>.
    /// <para>
    /// Use <see cref="CacheManager.CreateScope"/> to obtain a scope.  Each scope has its own
    /// provider (and therefore its own statistics, key namespace, and eviction policy), so
    /// components like <c>ProxyDataSource</c> can have per-datasource caches without sharing
    /// the global <see cref="CacheManager"/> instance.
    /// </para>
    /// </summary>
    public sealed class CacheScope : IDisposable
    {
        private readonly ICacheProvider _provider;
        private bool _disposed;

        internal CacheScope(ICacheProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <summary>Live statistics exposed by the underlying provider.</summary>
        public CacheStatistics Statistics => _provider.Statistics;

        /// <summary>Whether the underlying provider is currently available.</summary>
        public bool IsAvailable => _provider.IsAvailable && !_disposed;

        // ── Core async API ─────────────────────────────────────────────────────────

        public Task<T> GetAsync<T>(string key, CancellationToken ct = default)
        {
            ThrowIfDisposed();
            return _provider.GetAsync<T>(key, ct);
        }

        public Task<bool> ExistsAsync(string key, CancellationToken ct = default)
        {
            ThrowIfDisposed();
            return _provider.ExistsAsync(key, ct);
        }

        public Task<bool> SetAsync<T>(
            string key, T value,
            TimeSpan? expiry = null,
            CancellationToken ct = default)
        {
            ThrowIfDisposed();
            return _provider.SetAsync(key, value, expiry, ct);
        }

        public Task<bool> RemoveAsync(string key, CancellationToken ct = default)
        {
            ThrowIfDisposed();
            return _provider.RemoveAsync(key, ct);
        }

        public Task<long> ClearAsync(string pattern = null, CancellationToken ct = default)
        {
            ThrowIfDisposed();
            return _provider.ClearAsync(pattern, ct);
        }

        // ── GetOrCreate with stale-while-revalidate ────────────────────────────────

        /// <summary>
        /// Fetch-or-create with stale-while-revalidate semantics.
        /// <list type="bullet">
        ///   <item>If the key exists and is within <paramref name="softTtl"/>, return it.</item>
        ///   <item>If the key is older than <paramref name="softTtl"/> but the hard TTL
        ///         (<paramref name="hardTtl"/>) has not expired, return the stale value and
        ///         trigger a background refresh.</item>
        ///   <item>On a hard miss, invoke <paramref name="factory"/> synchronously.</item>
        /// </list>
        /// </summary>
        public async Task<T> GetOrCreateAsync<T>(
            string key,
            Func<Task<T>> factory,
            TimeSpan softTtl,
            TimeSpan hardTtl,
            CancellationToken ct = default)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(key) || factory == null)
                return default;

            if (await _provider.ExistsAsync(key, ct).ConfigureAwait(false))
            {
                var metaKey = $"{key}:__swr_ts";
                bool stale = false;

                if (await _provider.ExistsAsync(metaKey, ct).ConfigureAwait(false))
                {
                    var cachedAt = await _provider.GetAsync<DateTime>(metaKey, ct).ConfigureAwait(false);
                    stale = DateTime.UtcNow - cachedAt > softTtl;
                }

                var current = await _provider.GetAsync<T>(key, ct).ConfigureAwait(false);

                if (!stale)
                    return current;

                // Stale: return current value and trigger background refresh
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var fresh = await factory().ConfigureAwait(false);
                        await _provider.SetAsync(key, fresh, hardTtl).ConfigureAwait(false);
                        await _provider.SetAsync(metaKey, DateTime.UtcNow, hardTtl).ConfigureAwait(false);
                    }
                    catch { /* swallow — background refresh; do not crash caller */ }
                });

                return current;
            }

            // Hard miss — block on factory
            var value = await factory().ConfigureAwait(false);
            var metaKeyNew = $"{key}:__swr_ts";
            await _provider.SetAsync(key, value, hardTtl, ct).ConfigureAwait(false);
            await _provider.SetAsync(metaKeyNew, DateTime.UtcNow, hardTtl, ct).ConfigureAwait(false);
            return value;
        }

        /// <summary>Simple get-or-create without stale semantics.</summary>
        public async Task<T> GetOrCreateAsync<T>(
            string key,
            Func<Task<T>> factory,
            TimeSpan? expiry = null,
            CancellationToken ct = default)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(key) || factory == null)
                return default;

            if (await _provider.ExistsAsync(key, ct).ConfigureAwait(false))
                return await _provider.GetAsync<T>(key, ct).ConfigureAwait(false);

            var value = await factory().ConfigureAwait(false);
            await _provider.SetAsync(key, value, expiry, ct).ConfigureAwait(false);
            return value;
        }

        // ── Batch ─────────────────────────────────────────────────────────────────

        public Task<Dictionary<string, T>> GetManyAsync<T>(IEnumerable<string> keys, CancellationToken ct = default)
        {
            ThrowIfDisposed();
            return _provider.GetManyAsync<T>(keys, ct);
        }

        public Task<long> SetManyAsync<T>(Dictionary<string, T> values, TimeSpan? expiry = null, CancellationToken ct = default)
        {
            ThrowIfDisposed();
            return _provider.SetManyAsync(values, expiry, ct);
        }

        // ── Dispose ───────────────────────────────────────────────────────────────

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _provider?.Dispose();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(CacheScope));
        }
    }
}
