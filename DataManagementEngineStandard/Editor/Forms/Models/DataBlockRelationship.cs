using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// Extended data block relationship with additional metadata
    /// </summary>
    public class DataBlockRelationship
    {
        /// <summary>Gets or sets the name of the master block</summary>
        public string MasterBlockName { get; set; }
        
        /// <summary>Gets or sets the name of the detail block</summary>
        public string DetailBlockName { get; set; }
        
        /// <summary>Gets or sets the key field in the master block</summary>
        public string MasterKeyField { get; set; }
        
        /// <summary>Gets or sets the foreign key field in the detail block</summary>
        public string DetailForeignKeyField { get; set; }
        
        /// <summary>Gets or sets the type of relationship</summary>
        public RelationshipType RelationshipType { get; set; } = RelationshipType.OneToMany;
        
        /// <summary>Gets or sets whether the relationship is active</summary>
        public bool IsActive { get; set; } = true;
        
        /// <summary>Gets or sets when the relationship was created</summary>
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        /// <summary>Gets or sets when the relationship was last modified</summary>
        public DateTime? ModifiedDate { get; set; }
        
        /// <summary>Gets or sets a description of the relationship</summary>
        public string Description { get; set; }
        
        /// <summary>Gets or sets extended properties for the relationship</summary>
        public Dictionary<string, object> ExtendedProperties { get; set; } = new Dictionary<string, object>();
        
        /// <summary>Gets or sets whether to cascade deletes</summary>
        public bool CascadeDelete { get; set; } = false;
        
        /// <summary>Gets or sets whether to cascade updates</summary>
        public bool CascadeUpdate { get; set; } = true;
        
        /// <summary>Gets or sets the relationship strength (weak, strong)</summary>
        public RelationshipStrength Strength { get; set; } = RelationshipStrength.Strong;
        
        /// <summary>Gets or sets custom synchronization logic</summary>
        public string CustomSyncLogic { get; set; }
        
        /// <summary>Gets or sets performance metrics for the relationship</summary>
        public RelationshipMetrics Metrics { get; set; } = new RelationshipMetrics();
    }
    
    /// <summary>
    /// Relationship strength types
    /// </summary>
    public enum RelationshipStrength
    {
        /// <summary>Weak relationship - detail records can exist without master</summary>
        Weak,
        
        /// <summary>Strong relationship - detail records require master record</summary>
        Strong
    }
    
    /// <summary>
    /// Performance metrics for relationships
    /// </summary>
    public class RelationshipMetrics
    {
        /// <summary>Gets or sets the total number of synchronizations</summary>
        public long SynchronizationCount { get; set; }
        
        /// <summary>Gets or sets the average synchronization time in milliseconds</summary>
        public double AverageSyncTimeMs { get; set; }
        
        /// <summary>Gets or sets the last synchronization time in milliseconds</summary>
        public double LastSyncTimeMs { get; set; }
        
        /// <summary>Gets or sets the total number of records synchronized</summary>
        public long TotalRecordsSynchronized { get; set; }
        
        /// <summary>Gets or sets the number of synchronization errors</summary>
        public long ErrorCount { get; set; }
        
        /// <summary>Gets or sets the last synchronization timestamp</summary>
        public DateTime? LastSyncTimestamp { get; set; }
    }
}