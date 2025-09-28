using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// Event arguments for error handling triggers
    /// </summary>
    public class ErrorTriggerEventArgs : EventArgs
    {
        /// <summary>Gets the name of the block where the error occurred</summary>
        public string BlockName { get; }
        
        /// <summary>Gets the error message</summary>
        public string ErrorMessage { get; }
        
        /// <summary>Gets the exception that caused the error</summary>
        public Exception Exception { get; }
        
        /// <summary>Gets the timestamp when the error occurred</summary>
        public DateTime Timestamp { get; } = DateTime.Now;
        
        /// <summary>Gets or sets additional context information</summary>
        public Dictionary<string, object> Context { get; set; } = new Dictionary<string, object>();
        
        /// <summary>Gets or sets the error severity level</summary>
        public ErrorSeverity Severity { get; set; } = ErrorSeverity.Error;
        
        /// <summary>Gets or sets whether the error has been handled</summary>
        public bool Handled { get; set; }
        
        /// <summary>Gets or sets the error code if applicable</summary>
        public string ErrorCode { get; set; }
        
        /// <summary>Gets or sets the operation that was being performed when the error occurred</summary>
        public string Operation { get; set; }
        
        /// <summary>Initializes a new instance of the ErrorTriggerEventArgs class</summary>
        /// <param name="blockName">The name of the block where the error occurred</param>
        /// <param name="errorMessage">The error message</param>
        /// <param name="exception">Optional exception that caused the error</param>
        public ErrorTriggerEventArgs(string blockName, string errorMessage, Exception exception = null)
        {
            BlockName = blockName;
            ErrorMessage = errorMessage;
            Exception = exception;
            
            // Extract error code from exception if available
            if (exception != null)
            {
                ErrorCode = exception.HResult.ToString();
                Operation = exception.TargetSite?.Name;
            }
        }
        
        /// <summary>
        /// Adds contextual information about the error
        /// </summary>
        /// <param name="key">The context key</param>
        /// <param name="value">The context value</param>
        public void AddContext(string key, object value)
        {
            Context[key] = value;
        }
    }
    
    /// <summary>
    /// Error severity levels
    /// </summary>
    public enum ErrorSeverity
    {
        /// <summary>Informational message</summary>
        Info,
        
        /// <summary>Warning message</summary>
        Warning,
        
        /// <summary>Error message</summary>
        Error,
        
        /// <summary>Critical error message</summary>
        Critical,
        
        /// <summary>Fatal error message</summary>
        Fatal
    }
}