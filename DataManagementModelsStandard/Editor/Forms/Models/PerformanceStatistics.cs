using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// Performance statistics for the manager
    /// </summary>
    public class PerformanceStatistics
    {
        /// <summary>Gets or sets the number of cache hits</summary>
        public long CacheHits { get; set; }
        
        /// <summary>Gets or sets the number of cache misses</summary>
        public long CacheMisses { get; set; }
        
        /// <summary>Gets or sets the number of cache writes</summary>
        public long CacheWrites { get; set; }
        
        /// <summary>Gets or sets the number of expired cache entries</summary>
        public long CacheExpired { get; set; }
        
        /// <summary>Gets or sets the number of cache clears</summary>
        public long CacheClears { get; set; }
        
        /// <summary>Gets or sets the optimization count</summary>
        public long OptimizationCount { get; set; }
        
        /// <summary>Gets or sets the current cache size</summary>
        public int CurrentCacheSize { get; set; }
        
        /// <summary>Gets or sets the cache hit ratio</summary>
        public double CacheHitRatio { get; set; }
        
        /// <summary>Gets or sets the average optimization time</summary>
        public TimeSpan AverageOptimizationTime { get; set; }
        
        /// <summary>Gets or sets the last optimization time</summary>
        public DateTime? LastOptimizationTime { get; set; }
        
        /// <summary>Gets or sets the last cache clear time</summary>
        public DateTime? LastCacheClearTime { get; set; }
        
        /// <summary>Gets or sets the top performance metrics</summary>
        public List<PerformanceMetric> TopPerformanceMetrics { get; set; } = new();
    }
    
    
}