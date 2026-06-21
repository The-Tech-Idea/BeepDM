using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// Field validation constraints for Oracle Forms-style validation
    /// </summary>
    public class FieldConstraints
    {
        /// <summary>Gets or sets whether the field is required</summary>
        public bool Required { get; set; }
        
        /// <summary>Gets or sets the maximum length for string fields</summary>
        public int MaxLength { get; set; }
        
        /// <summary>Gets or sets the minimum value for numeric fields</summary>
        public double? MinValue { get; set; }
        
        /// <summary>Gets or sets the maximum value for numeric fields</summary>
        public double? MaxValue { get; set; }
        
        /// <summary>Gets or sets a custom validation function</summary>
        public Func<object, ValidationResult> CustomValidator { get; set; }
        
        /// <summary>Gets or sets additional validation rules</summary>
        public Dictionary<string, object> CustomRules { get; set; } = new Dictionary<string, object>();
        
        /// <summary>Gets or sets whether null values are allowed</summary>
        public bool AllowNull { get; set; } = true;
        
        /// <summary>Gets or sets a regex pattern for string validation</summary>
        public string Pattern { get; set; }
        
        /// <summary>Gets or sets a list of valid values</summary>
        public List<object> ValidValues { get; set; } = new List<object>();
        
        /// <summary>Gets or sets whether case-sensitive validation should be used</summary>
        public bool CaseSensitive { get; set; } = false;
    }
}