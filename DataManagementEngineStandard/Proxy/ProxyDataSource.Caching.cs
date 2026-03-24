using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TheTechIdea.Beep.Caching;
using TheTechIdea.Beep.Caching.Providers;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Proxy
{
    public partial class ProxyDataSource
    {
        // ── Per-instance cache scope (replaces _entityCache dict) ─────
        private CacheScope _cacheScope;

        // ── Convenience accessor ──────────────────────────────────────
        private ProxyCacheProfile CacheProfile => _policy.Cache;

        // ─────────────────────────────────────────────────────────────
        //  Initialization
        //  Called from InitializeFromPolicy() in the main partial class.
        // ─────────────────────────────────────────────────────────────

        internal void InitializeCacheProvider()
        {
            _cacheScope?.Dispose();

            var profile = CacheProfile;
            var config  = new CacheConfiguration
            {
                MaxItems         = profile.MaxItems,
                DefaultExpiry    = profile.DefaultExpiration,
                EnableStatistics = true,
                KeyPrefix        = $"proxy:{DatasourceName}:"
            };

            var providerType = profile.Tier switch
            {
                ProxyCacheTier.EntityProfile => CacheProviderType.InMemory,
                _                            => CacheProviderType.InMemory
            };

            _cacheScope = CacheManager.CreateScope(providerType, config);
            _dmeEditor.AddLogMessage($"[Proxy:{DatasourceName}] Cache scope initialised ({providerType}).");
        }

        // ─────────────────────────────────────────────────────────────
        //  GetEntityWithCache  (stale-while-revalidate aware)
        // ─────────────────────────────────────────────────────────────

        public object GetEntityWithCache(string entityName, List<AppFilter> filter, TimeSpan? expiration = null)
        {
            if (!CacheProfile.Enabled || _cacheScope == null)
                return GetEntity(entityName, filter);

            string   key     = GenerateCacheKey(entityName, filter);
            TimeSpan softTtl = expiration ?? CacheProfile.DefaultExpiration;
            TimeSpan hardTtl = softTtl.Add(CacheProfile.StaleWindow);

            return _cacheScope
                .GetOrCreateAsync<object>(
                    key,
                    factory:  () => System.Threading.Tasks.Task.FromResult<object>(GetEntity(entityName, filter)),
                    softTtl:  softTtl,
                    hardTtl:  hardTtl)
                .GetAwaiter()
                .GetResult();
        }

        // ─────────────────────────────────────────────────────────────
        //  Write-through invalidation
        // ─────────────────────────────────────────────────────────────

        private void InvalidateCacheOnWrite(string entityName)
        {
            if (CacheProfile.Consistency == ProxyCacheConsistency.WriteThrough)
                InvalidateCache(entityName);
        }

        // ─────────────────────────────────────────────────────────────
        //  Public cache management
        // ─────────────────────────────────────────────────────────────

        public void InvalidateCache(string entityName = null)
        {
            if (_cacheScope == null) return;

            if (string.IsNullOrEmpty(entityName))
            {
                _cacheScope.ClearAsync().GetAwaiter().GetResult();
                _dmeEditor.AddLogMessage($"[Proxy:{DatasourceName}] Cache cleared (all entities).");
            }
            else
            {
                _cacheScope.ClearAsync($"{entityName}~").GetAwaiter().GetResult();
                _cacheScope.RemoveAsync(entityName).GetAwaiter().GetResult();
                _dmeEditor.AddLogMessage($"[Proxy:{DatasourceName}] Cache cleared for entity: {entityName}.");
            }
        }

        public IDictionary<string, DataSourceMetrics> GetMetrics()
            => new Dictionary<string, DataSourceMetrics>(_metrics);

        // ─────────────────────────────────────────────────────────────
        //  Cache key generation  (collision-safe)
        // ─────────────────────────────────────────────────────────────

        private string GenerateCacheKey(string entityName, List<AppFilter> filter)
        {
            if (filter == null || filter.Count == 0)
                return entityName;

            string filterPart = string.Join("|",
                filter
                    .OrderBy(f => f.FieldName, StringComparer.OrdinalIgnoreCase)
                    .Select(f => $"{f.FieldName}:{f.Operator}:{f.FilterValue}:{f.FilterValue1}"));

            return $"{entityName}~{filterPart}";
        }
    }
}
