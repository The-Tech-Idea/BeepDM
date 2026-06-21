using System;

namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// Result of a save operation
    /// </summary>
    public class SaveResult
    {
        /// <summary>Gets or sets the name of the block that was saved</summary>
        public string BlockName { get; set; }
        
        /// <summary>Gets or sets whether the save operation was successful</summary>
        public bool Success { get; set; }
        
        /// <summary>Gets or sets the error message if the operation failed</summary>
        public string ErrorMessage { get; set; }
        
        /// <summary>Gets or sets the exception that occurred during the operation</summary>
        public Exception Exception { get; set; }
        
        /// <summary>Gets or sets the number of retry attempts made</summary>
        public int RetryCount { get; set; }
        
        /// <summary>Gets or sets the duration of the save operation</summary>
        public TimeSpan Duration { get; set; }
        
        /// <summary>Gets or sets the number of records affected</summary>
        public int RecordsAffected { get; set; }
        
        /// <summary>Gets or sets the timestamp when the operation completed</summary>
        public DateTime CompletedAt { get; set; } = DateTime.Now;
        
        /// <summary>Gets or sets additional result data</summary>
        public object ResultData { get; set; }
    }
}