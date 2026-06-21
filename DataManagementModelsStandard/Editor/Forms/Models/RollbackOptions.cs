namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// Options for rollback operations in dirty state management
    /// </summary>
    public class RollbackOptions
    {
        /// <summary>Gets the default rollback options</summary>
        public static RollbackOptions Default => new RollbackOptions();
        
        /// <summary>Gets or sets whether to stop on the first error</summary>
        public bool StopOnFirstError { get; set; } = false;
        
        /// <summary>Gets or sets whether to clear data after rollback</summary>
        public bool ClearAfterRollback { get; set; } = false;
        
        /// <summary>Gets or sets whether to confirm before rollback</summary>
        public bool ConfirmBeforeRollback { get; set; } = true;
        
        /// <summary>Gets or sets whether to backup data before rollback</summary>
        public bool BackupBeforeRollback { get; set; } = false;
        
        /// <summary>Gets or sets the timeout for rollback operations in seconds</summary>
        public int TimeoutSeconds { get; set; } = 30;
        
        /// <summary>Gets or sets whether to log progress</summary>
        public bool LogProgress { get; set; } = true;
    }
}