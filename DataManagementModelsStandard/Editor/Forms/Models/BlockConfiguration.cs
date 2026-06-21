using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// Configuration settings for individual data blocks
    /// </summary>
    public class BlockConfiguration
    {
        /// <summary>Gets or sets whether caching is enabled for this block</summary>
        public bool EnableCaching { get; set; } = true;
        
        /// <summary>Gets or sets whether validation is enabled for this block</summary>
        public bool EnableValidation { get; set; } = true;
        
        /// <summary>Gets or sets whether audit trail is enabled for this block</summary>
        public bool EnableAuditTrail { get; set; } = false;
        
        /// <summary>Gets or sets the query timeout in seconds</summary>
        public int QueryTimeout { get; set; } = 30;
        
        /// <summary>Gets or sets the maximum number of records to load</summary>
        public int MaxRecords { get; set; } = 1000;
        
        /// <summary>Gets or sets custom block settings</summary>
        public Dictionary<string, object> CustomSettings { get; set; } = new Dictionary<string, object>();
        
        /// <summary>Gets or sets whether to enable optimistic locking</summary>
        public bool EnableOptimisticLocking { get; set; } = false;
        
        /// <summary>Gets or sets whether to enable batch operations</summary>
        public bool EnableBatchOperations { get; set; } = true;
        
        /// <summary>Gets or sets the batch size for operations</summary>
        public int BatchSize { get; set; } = 100;
        
        /// <summary>Gets or sets whether to enable change tracking</summary>
        public bool EnableChangeTracking { get; set; } = true;

        // Phase 7 — paging & lazy-load

        /// <summary>Records per page (0 = paging disabled, load all).</summary>
        public int PageSize { get; set; } = 0;

        /// <summary>Number of pages to pre-fetch ahead of the current page (0 = disabled).</summary>
        public int FetchAheadDepth { get; set; } = 1;

        /// <summary>Maximum records to load per fetch when lazy/deferred loading is active.</summary>
        public int MaxRecordsPerFetch { get; set; } = 100;

        /// <summary>Whether lazy loading is enabled for this block.</summary>
        public bool EnableLazyLoad { get; set; } = false;

        /// <summary>Cache TTL in minutes (0 = use global default).</summary>
        public int CacheTtlMinutes { get; set; } = 0;
    }
}