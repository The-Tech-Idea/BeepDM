using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// Detailed information about a dirty block
    /// </summary>
    public class DirtyBlockInfo
    {
        /// <summary>Gets or sets the name of the block</summary>
        public string BlockName { get; set; }
        
        /// <summary>Gets or sets the name of the entity</summary>
        public string EntityName { get; set; }
        
        /// <summary>Gets or sets the number of dirty records in the block</summary>
        public int DirtyRecordCount { get; set; }
        
        /// <summary>Gets or sets when the block was last modified</summary>
        public DateTime? LastModified { get; set; }
        
        /// <summary>Gets or sets whether the block has validation errors</summary>
        public bool HasErrors { get; set; }
        
        /// <summary>Gets or sets whether this is a master block</summary>
        public bool IsMasterBlock { get; set; }
        
        /// <summary>Gets or sets the data source name</summary>
        public string DataSourceName { get; set; }
        
        /// <summary>Gets or sets additional metadata</summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        
        /// <summary>Gets or sets the validation error messages</summary>
        public List<string> ValidationErrors { get; set; } = new List<string>();
        
        /// <summary>Gets or sets the size of the block in bytes (approximate)</summary>
        public long EstimatedSize { get; set; }
    }
}