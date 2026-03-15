using System;

namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// Types of validation that can be applied to items/fields
    /// </summary>
    public enum ValidationType
    {
        /// <summary>Field must have a value (not null/empty)</summary>
        Required = 1,
        
        /// <summary>Value must be within a numeric range</summary>
        Range = 2,
        
        /// <summary>Value must match a regular expression pattern</summary>
        Pattern = 3,
        
        /// <summary>String/Array must not exceed maximum length</summary>
        MaxLength = 4,
        
        /// <summary>String/Array must meet minimum length</summary>
        MinLength = 5,
        
        /// <summary>Value must exist in a lookup table/LOV</summary>
        Lookup = 6,
        
        /// <summary>Value must be unique within the block/dataset</summary>
        Unique = 7,
        
        /// <summary>Custom validation with user-defined function</summary>
        Custom = 8,
        
        /// <summary>Value must be a valid email format</summary>
        Email = 9,
        
        /// <summary>Value must be a valid URL format</summary>
        Url = 10,
        
        /// <summary>Value must be a valid date</summary>
        Date = 11,
        
        /// <summary>Value must be a valid numeric value</summary>
        Numeric = 12,
        
        /// <summary>Value must be greater than another field</summary>
        GreaterThan = 13,
        
        /// <summary>Value must be less than another field</summary>
        LessThan = 14,
        
        /// <summary>Value must equal another field</summary>
        EqualTo = 15,
        
        /// <summary>Cross-field validation based on multiple fields</summary>
        CrossField = 16,
        
        /// <summary>Database-level validation (check constraint)</summary>
        Database = 17
    }

    /// <summary>
    /// Severity level for validation results
    /// </summary>
    public enum ValidationSeverity
    {
        /// <summary>Informational message, operation continues</summary>
        Info = 0,
        
        /// <summary>Warning message, operation continues with notice</summary>
        Warning = 1,
        
        /// <summary>Error - operation is blocked</summary>
        Error = 2,
        
        /// <summary>Critical error - immediate stop required</summary>
        Critical = 3
    }

    /// <summary>
    /// When to apply validation
    /// </summary>
    public enum ValidationTiming
    {
        /// <summary>Validate when item loses focus</summary>
        OnBlur = 1,
        
        /// <summary>Validate on each keystroke/change</summary>
        OnChange = 2,
        
        /// <summary>Validate before record navigation</summary>
        OnRecordChange = 3,
        
        /// <summary>Validate before block commit</summary>
        OnCommit = 4,
        
        /// <summary>Validate on explicit request only</summary>
        Manual = 5
    }
}
