using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Editor.Forms.Models;
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
        
        /// <summary>Gets or sets the CLR entity type for generic block operations</summary>
        public Type EntityType { get; set; }
        
        /// <summary>Gets or sets the current mode of the block</summary>
        public DataBlockMode Mode { get; set; } = DataBlockMode.Query;
        
        /// <summary>Gets or sets the master block name (if this is a detail block)</summary>
        public string MasterBlockName { get; set; }
        
        /// <summary>Gets or sets the master key field name</summary>
        public string MasterKeyField { get; set; }
        
        /// <summary>Gets or sets the foreign key field name</summary>
        public string ForeignKeyField { get; set; }
        
        /// <summary>Gets or sets whether insert operations are allowed (Oracle Forms: INSERT_ALLOWED)</summary>
        public bool InsertAllowed { get; set; } = true;
        
        /// <summary>Gets or sets whether update operations are allowed (Oracle Forms: UPDATE_ALLOWED)</summary>
        public bool UpdateAllowed { get; set; } = true;
        
        /// <summary>Gets or sets whether delete operations are allowed (Oracle Forms: DELETE_ALLOWED)</summary>
        public bool DeleteAllowed { get; set; } = true;
        
        /// <summary>Gets or sets whether query operations are allowed (Oracle Forms: QUERY_ALLOWED)</summary>
        public bool QueryAllowed { get; set; } = true;
        
        /// <summary>Gets or sets a default WHERE clause appended to every query</summary>
        public string DefaultWhereClause { get; set; } = string.Empty;
        
        /// <summary>Gets or sets a default ORDER BY clause appended to every query</summary>
        public string DefaultOrderByClause { get; set; } = string.Empty;
        
        /// <summary>Gets or sets per-field metadata (visibility, ordering, queryability)</summary>
        public List<BlockFieldMetadata> FieldMetadata { get; set; } = new List<BlockFieldMetadata>();

        /// <summary>Gets or sets whether the block is registered</summary>
        public bool IsRegistered { get; set; }
        
        /// <summary>Gets or sets when the block was registered</summary>
        public DateTime RegisteredAt { get; set; } = DateTime.Now;
        
        /// <summary>Gets or sets the block configuration</summary>
        public BlockConfiguration Configuration { get; set; } = new BlockConfiguration();

        // Phase 7 — paging & lazy-load

        /// <summary>Lazy-load strategy for this block.</summary>
        public LazyLoadMode LazyLoadMode { get; set; } = LazyLoadMode.None;

        /// <summary>Current 1-based page number (used when paging is active).</summary>
        public int CurrentPage { get; set; } = 1;
        
        /// <summary>Gets or sets extended properties for the block</summary>
        public Dictionary<string, object> ExtendedProperties { get; set; } = new Dictionary<string, object>();

        /// <summary>Gets or sets when the block last changed modes.</summary>
        public  DateTime LastModeChange { get; set; } = DateTime.Now;
    }
    
    /// <summary>
    /// Data block modes similar to Oracle Forms
    /// </summary>
    public enum DataBlockMode
    {
        /// <summary>Normal mode — reserved for future use. CRUD (3) is the current canonical normal-mode value.</summary>
        Normal = 0,

        /// <summary>Enter-Query mode - the user is typing example criteria to filter the next query (Oracle Forms ENTER_QUERY).</summary>
        EnterQuery = 1,

        /// <summary>Query mode - results are loaded and editable per the block's UpdateAllowed/InsertAllowed flags.</summary>
        Query = 2,

        /// <summary>CRUD mode - explicit alias for Normal kept for source compatibility with existing call-sites.</summary>
        CRUD = 3,

        /// <summary>Read-only mode — reserved for future use. Currently enforced via DataBlockInfo.InsertAllowed/UpdateAllowed/DeleteAllowed flags.</summary>
        ReadOnly = 4,

        /// <summary>Insert mode — reserved for future use. Insert operations currently run in CRUD (3) mode.</summary>
        Insert = 5
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