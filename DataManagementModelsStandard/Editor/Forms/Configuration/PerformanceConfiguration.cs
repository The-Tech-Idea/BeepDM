namespace TheTechIdea.Beep.Editor.UOWManager.Configuration
{
    /// <summary>
    /// Performance-related configuration settings
    /// </summary>
    public class PerformanceConfiguration
    {
        /// <summary>Gets or sets whether caching is enabled</summary>
        public bool EnableCaching { get; set; } = true;
        
        /// <summary>Gets or sets the maximum cache size</summary>
        public int MaxCacheSize { get; set; } = 1000;
        
        /// <summary>Gets or sets cache expiration time in minutes</summary>
        public int CacheExpirationMinutes { get; set; } = 30;
        
        /// <summary>Gets or sets whether performance metrics are enabled</summary>
        public bool EnableMetrics { get; set; } = true;
        
        /// <summary>Gets or sets metrics retention period in days</summary>
        public int MetricsRetentionDays { get; set; } = 7;
        
        /// <summary>Gets or sets whether to enable performance optimization</summary>
        public bool EnableOptimization { get; set; } = true;
        
        /// <summary>Gets or sets the optimization interval in minutes</summary>
        public int OptimizationIntervalMinutes { get; set; } = 60;
    }
}