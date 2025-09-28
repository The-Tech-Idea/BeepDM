using System;
using System.Collections.Generic;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// Information about a registered data block
    /// </summary>
    public class DataBlockInfo
    {
        /// <summary>Gets or sets the name of the block</summary>
        public string BlockName { get; set; }
        
        /// <summary>Gets or sets the unit of work for this block</summary>
        public IUnitofWork UnitOfWork { get; set; }
        
        /// <summary>Gets or sets the entity structure</summary>
        public IEntityStructure EntityStructure { get; set; }
        
        /// <summary>Gets or sets the data source name</summary>
        public string DataSourceName { get; set; }
        
        /// <summary>Gets or sets whether this is a master block</summary>
        public bool IsMasterBlock { get; set; }
        
        /// <summary>Gets or sets the current mode of the block</summary>
        public DataBlockMode Mode { get; set; } = DataBlockMode.Query;
        
        /// <summary>Gets or sets the master block name (if this is a detail block)</summary>
        public string MasterBlockName { get; set; }
        
        /// <summary>Gets or sets the master key field name</summary>
        public string MasterKeyField { get; set; }
        
        /// <summary>Gets or sets the foreign key field name</summary>
        public string ForeignKeyField { get; set; }
        
        /// <summary>Gets or sets whether the block is registered</summary>
        public bool IsRegistered { get; set; }
        
        /// <summary>Gets or sets when the block was registered</summary>
        public DateTime RegisteredAt { get; set; } = DateTime.Now;
        
        /// <summary>Gets or sets the block configuration</summary>
        public BlockConfiguration Configuration { get; set; } = new BlockConfiguration();
        
        /// <summary>Gets or sets extended properties for the block</summary>
        public Dictionary<string, object> ExtendedProperties { get; set; } = new Dictionary<string, object>();
        public  DateTime LastModeChange { get; set; } = DateTime.Now;
    }
    
    /// <summary>
    /// Data block modes similar to Oracle Forms
    /// </summary>
    public enum DataBlockMode
    {
        /// <summary>Query mode - for querying data</summary>
        Query,
        
        /// <summary>CRUD mode - for create, read, update, delete operations</summary>
        CRUD,
        
        /// <summary>Read-only mode</summary>
        ReadOnly,
        
        /// <summary>Insert mode - for adding new records</summary>
        Insert
    }
    
    /// <summary>
    /// Types of relationships between blocks
    /// </summary>
    public enum RelationshipType
    {
        /// <summary>One-to-one relationship</summary>
        OneToOne,
        
        /// <summary>One-to-many relationship</summary>
        OneToMany,
        
        /// <summary>Many-to-one relationship</summary>
        ManyToOne,
        
        /// <summary>Many-to-many relationship</summary>
        ManyToMany
    }
}