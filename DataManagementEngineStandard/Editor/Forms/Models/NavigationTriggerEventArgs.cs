using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// Navigation trigger event arguments for Oracle Forms simulation
    /// </summary>
    public class NavigationTriggerEventArgs : EventArgs
    {
        /// <summary>Gets the name of the block being navigated</summary>
        public string BlockName { get; }
        
        /// <summary>Gets the name of the form containing the block</summary>
        public string FormName { get; }
        
        /// <summary>Gets the type of navigation being performed</summary>
        public NavigationType NavigationType { get; }
        
        /// <summary>Gets or sets the target index for ToRecord navigation</summary>
        public int? TargetIndex { get; set; }
        
        /// <summary>Gets or sets the target key for ToKey navigation</summary>
        public object TargetKey { get; set; }
        
        /// <summary>Gets or sets the search criteria for ToSearch navigation</summary>
        public Dictionary<string, object> SearchCriteria { get; set; }
        
        /// <summary>Gets or sets a message associated with the navigation</summary>
        public string Message { get; set; }
        
        /// <summary>Gets or sets whether the navigation should be cancelled</summary>
        public bool Cancel { get; set; }
        
        /// <summary>Gets the timestamp when the event was created</summary>
        public DateTime Timestamp { get; } = DateTime.Now;
        
        /// <summary>Gets or sets the source record (before navigation)</summary>
        public object SourceRecord { get; set; }
        
        /// <summary>Gets or sets the target record (after navigation)</summary>
        public object TargetRecord { get; set; }
        
        /// <summary>Gets or sets additional context information</summary>
        public Dictionary<string, object> Context { get; set; } = new Dictionary<string, object>();
        
        /// <summary>Initializes a new instance of the NavigationTriggerEventArgs class</summary>
        /// <param name="blockName">The name of the block being navigated</param>
        /// <param name="formName">The name of the form containing the block</param>
        /// <param name="navigationType">The type of navigation being performed</param>
        public NavigationTriggerEventArgs(string blockName, string formName, NavigationType navigationType)
        {
            BlockName = blockName;
            FormName = formName;
            NavigationType = navigationType;
        }
        
        /// <summary>
        /// Adds contextual information to the navigation event
        /// </summary>
        /// <param name="key">The context key</param>
        /// <param name="value">The context value</param>
        public void AddContext(string key, object value)
        {
            Context[key] = value;
        }
    }
}