using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// Information about current navigation state of a data block
    /// </summary>
    public class NavigationInfo
    {
        /// <summary>Gets or sets the name of the block</summary>
        public string BlockName { get; set; }
        
        /// <summary>Gets or sets the current record index (0-based)</summary>
        public int CurrentIndex { get; set; }
        
        /// <summary>Gets or sets the total number of records</summary>
        public int TotalRecords { get; set; }
        
        /// <summary>Gets or sets whether there is a previous record</summary>
        public bool HasPrevious { get; set; }
        
        /// <summary>Gets or sets whether there is a next record</summary>
        public bool HasNext { get; set; }
        
        /// <summary>Gets or sets the current record object</summary>
        public object CurrentRecord { get; set; }
        
        /// <summary>Gets whether the current position is at the first record</summary>
        public bool IsAtFirst => CurrentIndex == 0;
        
        /// <summary>Gets whether the current position is at the last record</summary>
        public bool IsAtLast => CurrentIndex == TotalRecords - 1;
        
        /// <summary>Gets whether the block has any records</summary>
        public bool HasRecords => TotalRecords > 0;
        
        /// <summary>Gets whether the block is empty</summary>
        public bool IsEmpty => TotalRecords == 0;
        
        /// <summary>Gets the record number (1-based for display purposes)</summary>
        public int RecordNumber => CurrentIndex + 1;
        
        /// <summary>Gets or sets additional navigation metadata</summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        
        /// <summary>Gets or sets the block mode</summary>
        public DataBlockMode BlockMode { get; set; }
        
        /// <summary>Gets or sets whether the current record is dirty</summary>
        public bool IsCurrentRecordDirty { get; set; }
        
        /// <summary>Gets or sets navigation performance metrics</summary>
        public NavigationMetrics Metrics { get; set; } = new NavigationMetrics();
    }
    
    /// <summary>
    /// Performance metrics for navigation operations
    /// </summary>
    public class NavigationMetrics
    {
        /// <summary>Gets or sets the total number of navigation operations</summary>
        public long NavigationCount { get; set; }
        
        /// <summary>Gets or sets the average navigation time in milliseconds</summary>
        public double AverageNavigationTimeMs { get; set; }
        
        /// <summary>Gets or sets the last navigation time in milliseconds</summary>
        public double LastNavigationTimeMs { get; set; }
        
        /// <summary>Gets or sets the total synchronization time for detail blocks</summary>
        public double TotalSyncTimeMs { get; set; }
    }
}