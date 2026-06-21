namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// DML operation types for database operations
    /// </summary>
    public enum DMLOperation
    {
        /// <summary>Query operation to retrieve data</summary>
        Query,
        
        /// <summary>Insert operation to add new records</summary>
        Insert,
        
        /// <summary>Update operation to modify existing records</summary>
        Update,
        
        /// <summary>Delete operation to remove records</summary>
        Delete,
        
        /// <summary>Commit operation to save changes</summary>
        Commit,
        
        /// <summary>Rollback operation to undo changes</summary>
        Rollback,
        
        /// <summary>Bulk insert operation for multiple records</summary>
        BulkInsert,
        
        /// <summary>Bulk update operation for multiple records</summary>
        BulkUpdate,
        
        /// <summary>Bulk delete operation for multiple records</summary>
        BulkDelete,
        
        /// <summary>Merge operation (upsert)</summary>
        Merge
    }
}