using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// Result of field validation operations
    /// </summary>
    public class ValidationResult
    {
        /// <summary>Gets or sets whether the validation passed</summary>
        public bool IsValid { get; set; }
        
        /// <summary>Gets or sets the name of the field being validated</summary>
        public string FieldName { get; set; }
        
        /// <summary>Gets or sets the primary error message</summary>
        public string ErrorMessage { get; set; }
        
        /// <summary>Gets or sets the name of the block containing the field</summary>
        public string BlockName { get; set; }
        
        /// <summary>Gets or sets a collection of validation error messages</summary>
        public List<string> ValidationMessages { get; set; } = new List<string>();
        
        /// <summary>Gets or sets the severity level of the validation result</summary>
        public ValidationSeverity Severity { get; set; } = ValidationSeverity.Error;
        
        /// <summary>Gets or sets additional context information</summary>
        public Dictionary<string, object> Context { get; set; } = new Dictionary<string, object>();
        
        /// <summary>Gets or sets the timestamp when validation was performed</summary>
        public DateTime ValidationTimestamp { get; set; } = DateTime.Now;
        
        /// <summary>Gets or sets the value that was validated</summary>
        public object ValidatedValue { get; set; }
        
        /// <summary>Adds a validation error message</summary>
        /// <param name="message">The error message to add</param>
        public void AddError(string message)
        {
            ValidationMessages.Add(message);
            if (string.IsNullOrEmpty(ErrorMessage))
            {
                ErrorMessage = message;
            }
            IsValid = false;
        }
        
        /// <summary>Adds a validation warning message</summary>
        /// <param name="message">The warning message to add</param>
        public void AddWarning(string message)
        {
            ValidationMessages.Add($"WARNING: {message}");
            if (Severity == ValidationSeverity.Info)
            {
                Severity = ValidationSeverity.Warning;
            }
        }
        
        /// <summary>Adds contextual information</summary>
        /// <param name="key">The context key</param>
        /// <param name="value">The context value</param>
        public void AddContext(string key, object value)
        {
            Context[key] = value;
        }
    }
    
    /// <summary>
    /// Validation severity levels
    /// </summary>
    public enum ValidationSeverity
    {
        /// <summary>Informational message</summary>
        Info,
        
        /// <summary>Warning message</summary>
        Warning,
        
        /// <summary>Error message</summary>
        Error,
        
        /// <summary>Critical error message</summary>
        Critical
    }
}