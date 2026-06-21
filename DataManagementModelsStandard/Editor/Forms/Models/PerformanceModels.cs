using System;

namespace TheTechIdea.Beep.Editor.Forms.Models
{
    /// <summary>Lazy-load strategy for a data block.</summary>
    public enum LazyLoadMode
    {
        /// <summary>All records loaded eagerly at query time (default).</summary>
        None,

        /// <summary>First page loaded; subsequent pages fetched on navigation.</summary>
        Deferred,

        /// <summary>Individual BLOB/CLOB/large fields fetched only when explicitly accessed.</summary>
        OnDemand
    }

    /// <summary>Describes the current paging state of a data block result set.</summary>
    public class PageInfo
    {
        /// <summary>1-based page number currently loaded.</summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>Number of records per page.</summary>
        public int PageSize { get; set; } = 50;

        /// <summary>Total records available in the data source for the current query.</summary>
        public long TotalRecords { get; set; }

        /// <summary>Total number of pages (computed).</summary>
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalRecords / PageSize) : 0;

        /// <summary>Whether a previous page exists.</summary>
        public bool HasPrevious => PageNumber > 1;

        /// <summary>Whether a next page exists.</summary>
        public bool HasNext => PageNumber < TotalPages;

        /// <summary>0-based index of the first record on this page (useful for skip/take).</summary>
        public int Skip => (PageNumber - 1) * PageSize;
    }

    /// <summary>Cache hit/miss statistics snapshot.</summary>
    public class CacheStats
    {
        /// <summary>Number of cache hits since last reset.</summary>
        public long Hits { get; set; }

        /// <summary>Number of cache misses since last reset.</summary>
        public long Misses { get; set; }

        /// <summary>Number of evictions (TTL expiry + LRU eviction) since last reset.</summary>
        public long Evictions { get; set; }

        /// <summary>Current number of entries in the cache.</summary>
        public int CurrentSize { get; set; }

        /// <summary>Ratio of hits to total requests (0–1).</summary>
        public double HitRate => (Hits + Misses) > 0 ? (double)Hits / (Hits + Misses) : 0.0;

        /// <summary>Rough memory estimate (entries × 256 bytes).</summary>
        public long EstimatedMemoryBytes { get; set; }

        /// <summary>When this snapshot was taken.</summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
