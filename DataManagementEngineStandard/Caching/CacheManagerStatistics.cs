using System;

namespace TheTechIdea.Beep.Caching
{
    /// <summary>
    /// Comprehensive statistics for the cache manager including all providers.
    /// </summary>
    public class CacheManagerStatistics
    {
        /// <summary>Gets or sets the primary provider statistics.</summary>
        public CacheStatistics PrimaryProvider { get; set; }

        /// <summary>Gets or sets the fallback provider statistics.</summary>
        public CacheStatistics FallbackProvider { get; set; }

        /// <summary>Gets or sets the primary provider name.</summary>
        public string PrimaryProviderName { get; set; }

        /// <summary>Gets or sets the fallback provider name.</summary>
        public string FallbackProviderName { get; set; }

        /// <summary>Gets or sets whether the primary provider is available.</summary>
        public bool PrimaryProviderAvailable { get; set; }

        /// <summary>Gets or sets whether the fallback provider is available.</summary>
        public bool FallbackProviderAvailable { get; set; }

        /// <summary>Gets or sets the cache configuration.</summary>
        public CacheConfiguration Configuration { get; set; }

        /// <summary>Gets the combined hit ratio across all providers.</summary>
        public double CombinedHitRatio
        {
            get
            {
                var totalHits = (PrimaryProvider?.Hits ?? 0) + (FallbackProvider?.Hits ?? 0);
                var totalMisses = (PrimaryProvider?.Misses ?? 0) + (FallbackProvider?.Misses ?? 0);
                var total = totalHits + totalMisses;
                
                return total > 0 ? (double)totalHits / total * 100 : 0;
            }
        }

        /// <summary>Gets the total item count across all providers.</summary>
        public long TotalItemCount => (PrimaryProvider?.ItemCount ?? 0) + (FallbackProvider?.ItemCount ?? 0);

        /// <summary>Gets the total memory usage across all providers.</summary>
        public long TotalMemoryUsage => (PrimaryProvider?.MemoryUsage ?? 0) + (FallbackProvider?.MemoryUsage ?? 0);

        /// <summary>Gets the total expired items across all providers.</summary>
        public long TotalExpiredItems => (PrimaryProvider?.ExpiredItems ?? 0) + (FallbackProvider?.ExpiredItems ?? 0);

        /// <summary>Gets the total evicted items across all providers.</summary>
        public long TotalEvictedItems => (PrimaryProvider?.EvictedItems ?? 0) + (FallbackProvider?.EvictedItems ?? 0);
    }
}