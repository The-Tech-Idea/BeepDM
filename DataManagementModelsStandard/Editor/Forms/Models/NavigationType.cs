namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// Navigation types for triggers and operations
    /// </summary>
    public enum NavigationType
    {
        /// <summary>Navigate to first record</summary>
        First,
        
        /// <summary>Navigate to next record</summary>
        Next,
        
        /// <summary>Navigate to previous record</summary>
        Previous,
        
        /// <summary>Navigate to last record</summary>
        Last,
        
        /// <summary>Current record changed event</summary>
        CurrentChanged,
        
        /// <summary>Navigate to specific record by index</summary>
        ToRecord,
        
        /// <summary>Navigate to record by key</summary>
        ToKey,
        
        /// <summary>Navigate to record by search criteria</summary>
        ToSearch
    }
}