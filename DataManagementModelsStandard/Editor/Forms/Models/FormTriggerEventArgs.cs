using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// Event arguments for form-level triggers in Oracle Forms simulation
    /// </summary>
    public class FormTriggerEventArgs : EventArgs
    {
        /// <summary>Gets the name of the form</summary>
        public string FormName { get; }
        
        /// <summary>Gets or sets the trigger message</summary>
        public string Message { get; set; }
        
        /// <summary>Gets or sets whether the operation should be cancelled</summary>
        public bool Cancel { get; set; }
        
        /// <summary>Gets or sets additional data associated with the trigger</summary>
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
        
        /// <summary>Gets the timestamp when the event was created</summary>
        public DateTime Timestamp { get; } = DateTime.Now;
        
        /// <summary>Gets or sets the source of the trigger</summary>
        public string TriggerSource { get; set; }
        
        /// <summary>Gets or sets the form operation type</summary>
        public FormOperationType OperationType { get; set; }
        
        /// <summary>Gets or sets additional context information</summary>
        public Dictionary<string, object> Context { get; set; } = new Dictionary<string, object>();
        
        /// <summary>Initializes a new instance of the FormTriggerEventArgs class</summary>
        /// <param name="formName">The name of the form</param>
        /// <param name="message">Optional message</param>
        public FormTriggerEventArgs(string formName, string message = null)
        {
            FormName = formName;
            Message = message;
        }
        
        /// <summary>
        /// Adds contextual information to the event
        /// </summary>
        /// <param name="key">The context key</param>
        /// <param name="value">The context value</param>
        public void AddContext(string key, object value)
        {
            Context[key] = value;
        }
    }
    
    /// <summary>
    /// Types of form operations
    /// </summary>
    public enum FormOperationType
    {
        /// <summary>Form is being opened</summary>
        Open,
        
        /// <summary>Form is being closed</summary>
        Close,
        
        /// <summary>Form data is being committed</summary>
        Commit,
        
        /// <summary>Form data is being rolled back</summary>
        Rollback,
        
        /// <summary>Form is being validated</summary>
        Validate,
        
        /// <summary>Form is being cleared</summary>
        Clear,
        
        /// <summary>Form is being refreshed</summary>
        Refresh
    }
}