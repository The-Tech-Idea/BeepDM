namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// LOV search mode - determines how search text is matched against LOV data
    /// </summary>
    public enum LOVSearchMode
    {
        /// <summary>Search anywhere in the string (default)</summary>
        Contains,
        
        /// <summary>Search at start of string</summary>
        StartsWith,
        
        /// <summary>Search at end of string</summary>
        EndsWith,
        
        /// <summary>Exact match only</summary>
        Exact
    }
    
    /// <summary>
    /// LOV validation type - Oracle Forms compatible
    /// Determines how user input is validated against LOV
    /// </summary>
    public enum LOVValidationType
    {
        /// <summary>
        /// Oracle Forms: Validate From List = Yes
        /// User MUST select from LOV, cannot type custom value
        /// </summary>
        ListOnly,
        
        /// <summary>
        /// Oracle Forms: Validate From List = No
        /// User can type any value, LOV is optional helper
        /// </summary>
        Unrestricted,
        
        /// <summary>
        /// User can type value, but it must match a value in the LOV
        /// </summary>
        Validated
    }
    
    /// <summary>
    /// LOV column alignment
    /// </summary>
    public enum LOVColumnAlignment
    {
        /// <summary>Left align (default)</summary>
        Left,
        
        /// <summary>Center align</summary>
        Center,
        
        /// <summary>Right align</summary>
        Right
    }
}
