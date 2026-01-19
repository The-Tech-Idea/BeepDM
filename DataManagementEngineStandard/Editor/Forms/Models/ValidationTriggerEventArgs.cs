using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// Event arguments for validation triggers in Oracle Forms simulation
    /// </summary>
    public class ValidationTriggerEventArgs : EventArgs
    {
        /// <summary>Gets the name of the block</summary>
        public string BlockName { get; }
        
        /// <summary>Gets or sets the name of the field being validated</summary>
        public string FieldName { get; set; }
        
        /// <summary>Gets or sets the value being validated</summary>
        public object Value { get; set; }
        
        /// <summary>Gets or sets the validation message</summary>
        public string ValidationMessage { get; set; }
        
        /// <summary>Gets or sets whether the validation passed</summary>
        public bool IsValid { get; set; } = true;
        
        /// <summary>Gets or sets whether the operation should be cancelled</summary>
        public bool Cancel { get; set; }
        
        /// <summary>Gets or sets the list of validation errors</summary>
        public List<string> ValidationErrors { get; set; } = new List<string>();
        
        /// <summary>Gets the timestamp when the event was created</summary>
        public DateTime Timestamp { get; } = DateTime.Now;
        
        /// <summary>Gets or sets the validation severity</summary>
        public ValidationSeverity Severity { get; set; } = ValidationSeverity.Error;
        
        /// <summary>Gets or sets the validation context</summary>
        public Dictionary<string, object> Context { get; set; } = new Dictionary<string, object>();
        
        /// <summary>Initializes a new instance of the ValidationTriggerEventArgs class</summary>
        /// <param name="blockName">The name of the block</param>
        /// <param name="FieldName">Optional field name being validated</param>
        /// <param name="value">Optional value being validated</param>
        public ValidationTriggerEventArgs(string blockName, string FieldName = null, object value = null)
        {
            BlockName = blockName;
           FieldName = FieldName;
            Value = value;
        }

        /// <summary>
        /// Adds a validation error and marks the validation as failed
        /// </summary>
        /// <param name="error">The error message to add</param>
        public void AddValidationError(string error)
        {
            ValidationErrors.Add(error);
            IsValid = false;
            
            if (string.IsNullOrEmpty(ValidationMessage))
            {
                ValidationMessage = error;
            }
        }
        
        /// <summary>
        /// Adds a validation warning
        /// </summary>
        /// <param name="warning">The warning message to add</param>
        public void AddValidationWarning(string warning)
        {
            ValidationErrors.Add($"WARNING: {warning}");
            if (Severity == ValidationSeverity.Info)
            {
                Severity = ValidationSeverity.Warning;
            }
        }
        
        /// <summary>
        /// Adds contextual information
        /// </summary>
        /// <param name="key">The context key</param>
        /// <param name="value">The context value</param>
        public void AddContext(string key, object value)
        {
            Context[key] = value;
        }
    }
}