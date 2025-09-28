using System;

namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// Cached block information with metadata
    /// </summary>
    public class CachedBlockInfo
    {
        /// <summary>Gets or sets the block information</summary>
        public DataBlockInfo BlockInfo { get; set; }
        
        /// <summary>Gets or sets when the entry was cached</summary>
        public DateTime CacheTime { get; set; }
        
        /// <summary>Gets or sets the number of times this entry has been accessed</summary>
        public int AccessCount { get; set; }
        
        /// <summary>Gets or sets when the entry was last accessed</summary>
        public DateTime LastAccessed { get; set; }
        
        /// <summary>Gets or sets whether this entry was preloaded</summary>
        public bool IsPreloaded { get; set; }
        
        /// <summary>Gets or sets the priority of this cache entry</summary>
        public CachePriority Priority { get; set; } = CachePriority.Normal;
        
        /// <summary>Gets or sets the size of the cached data in bytes (approximate)</summary>
        public long Size { get; set; }
        
        /// <summary>Gets or sets whether this entry is locked (cannot be evicted)</summary>
        public bool IsLocked { get; set; }
    }
    
    /// <summary>
    /// Cache priority levels
    /// </summary>
    public enum CachePriority
    {
        /// <summary>Low priority - first to be evicted</summary>
        Low,
        
        /// <summary>Normal priority</summary>
        Normal,
        
        /// <summary>High priority - kept longer</summary>
        High,
        
        /// <summary>Critical priority - never evicted unless explicitly removed</summary>
        Critical
    }
}