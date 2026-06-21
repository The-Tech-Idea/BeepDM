namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// Cache efficiency metrics for performance analysis
    /// </summary>
    public class CacheEfficiencyMetrics
    {
        /// <summary>Gets or sets the total number of cache requests</summary>
        public long TotalRequests { get; set; }
        
        /// <summary>Gets or sets the cache hit rate (0.0 to 1.0)</summary>
        public double HitRate { get; set; }
        
        /// <summary>Gets or sets the cache miss rate (0.0 to 1.0)</summary>
        public double MissRate { get; set; }
        
        /// <summary>Gets or sets the cache utilization percentage (0.0 to 1.0)</summary>
        public double CacheUtilization { get; set; }
        
        /// <summary>Gets or sets the average access count per cached item</summary>
        public double AverageAccessCount { get; set; }
        
        /// <summary>Gets or sets the number of expired entries removed</summary>
        public long ExpiredEntries { get; set; }
        
        /// <summary>Gets or sets the number of preloaded entries</summary>
        public long PreloadedEntries { get; set; }
        
        /// <summary>Gets or sets the memory usage of the cache in bytes</summary>
        public long MemoryUsageBytes { get; set; }
        
        /// <summary>Gets or sets the number of evicted entries due to size limits</summary>
        public long EvictedEntries { get; set; }
        
        /// <summary>Gets or sets the average time to retrieve from cache in milliseconds</summary>
        public double AverageRetrievalTimeMs { get; set; }
    }
}