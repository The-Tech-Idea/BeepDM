namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// Options for save operations in dirty state management
    /// </summary>
    public class SaveOptions
    {
        /// <summary>Gets the default save options</summary>
        public static SaveOptions Default => new SaveOptions();
        
        /// <summary>Gets or sets whether to validate before saving</summary>
        public bool ValidateBeforeSave { get; set; } = true;
        
        /// <summary>Gets or sets whether to stop on the first error</summary>
        public bool StopOnFirstError { get; set; } = false;
        
        /// <summary>Gets or sets the maximum number of retries</summary>
        public int MaxRetries { get; set; } = 3;
        
        /// <summary>Gets or sets the delay between retries in milliseconds</summary>
        public int RetryDelayMs { get; set; } = 1000;
        
        /// <summary>Gets or sets whether to use transactions</summary>
        public bool UseTransaction { get; set; } = true;
        
        /// <summary>Gets or sets whether to backup data before saving</summary>
        public bool BackupBeforeSave { get; set; } = false;
        
        /// <summary>Gets or sets the timeout for save operations in seconds</summary>
        public int TimeoutSeconds { get; set; } = 30;
        
        /// <summary>Gets or sets whether to log progress</summary>
        public bool LogProgress { get; set; } = true;
    }
}