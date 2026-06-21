using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// Represents a validation rule that can be applied to items/fields
    /// UI-agnostic validation rule definition
    /// </summary>
    public class ValidationRule
    {
        #region Identification
        
        /// <summary>
        /// Unique rule name/identifier
        /// </summary>
        public string RuleName { get; set; }
        
        /// <summary>
        /// Block this rule belongs to (null = form-level)
        /// </summary>
        public string BlockName { get; set; }
        
        /// <summary>
        /// Item/field this rule applies to (null = record-level rule)
        /// </summary>
        public string ItemName { get; set; }
        
        #endregion
        
        #region Rule Definition
        
        /// <summary>
        /// Type of validation
        /// </summary>
        public ValidationType ValidationType { get; set; }
        
        /// <summary>
        /// Severity level for validation failure
        /// </summary>
        public ValidationSeverity Severity { get; set; } = ValidationSeverity.Error;
        
        /// <summary>
        /// When to apply this validation
        /// </summary>
        public ValidationTiming Timing { get; set; } = ValidationTiming.OnBlur;
        
        /// <summary>
        /// Whether this rule is currently enabled
        /// </summary>
        public bool IsEnabled { get; set; } = true;
        
        #endregion
        
        #region Error Messages
        
        /// <summary>
        /// Error message to display on validation failure
        /// Supports placeholders: {FieldName}, {Value}, {Min}, {Max}, {Pattern}
        /// </summary>
        public string ErrorMessage { get; set; }
        
        /// <summary>
        /// Default error message based on validation type
        /// </summary>
        public string DefaultErrorMessage => ValidationType switch
        {
            ValidationType.Required => "{FieldName} is required",
            ValidationType.Range => "{FieldName} must be between {Min} and {Max}",
            ValidationType.Pattern => "{FieldName} must match the required format",
            ValidationType.MaxLength => "{FieldName} cannot exceed {Max} characters",
            ValidationType.MinLength => "{FieldName} must be at least {Min} characters",
            ValidationType.Lookup => "{FieldName} value not found in list",
            ValidationType.Unique => "{FieldName} value must be unique",
            ValidationType.Email => "{FieldName} must be a valid email address",
            ValidationType.Url => "{FieldName} must be a valid URL",
            ValidationType.Date => "{FieldName} must be a valid date",
            ValidationType.Numeric => "{FieldName} must be a number",
            ValidationType.GreaterThan => "{FieldName} must be greater than {CompareField}",
            ValidationType.LessThan => "{FieldName} must be less than {CompareField}",
            ValidationType.EqualTo => "{FieldName} must equal {CompareField}",
            _ => "{FieldName} is invalid"
        };
        
        /// <summary>
        /// Get the effective error message with placeholders replaced
        /// </summary>
        public string GetFormattedMessage(string fieldName, object value, Dictionary<string, object> context = null)
        {
            var message = !string.IsNullOrEmpty(ErrorMessage) ? ErrorMessage : DefaultErrorMessage;
            
            message = message.Replace("{FieldName}", fieldName ?? ItemName ?? "Field");
            message = message.Replace("{Value}", value?.ToString() ?? "null");
            
            if (context != null)
            {
                foreach (var kvp in context)
                {
                    message = message.Replace($"{{{kvp.Key}}}", kvp.Value?.ToString() ?? "");
                }
            }
            
            // Replace rule-specific placeholders
            if (MinValue != null)
                message = message.Replace("{Min}", MinValue.ToString());
            if (MaxValue != null)
                message = message.Replace("{Max}", MaxValue.ToString());
            if (!string.IsNullOrEmpty(Pattern))
                message = message.Replace("{Pattern}", Pattern);
            if (!string.IsNullOrEmpty(CompareFieldName))
                message = message.Replace("{CompareField}", CompareFieldName);
            
            return message;
        }
        
        #endregion
        
        #region Rule Parameters
        
        /// <summary>
        /// Minimum value for Range/MinLength validation
        /// </summary>
        public object MinValue { get; set; }
        
        /// <summary>
        /// Maximum value for Range/MaxLength validation
        /// </summary>
        public object MaxValue { get; set; }
        
        /// <summary>
        /// Regular expression pattern for Pattern validation
        /// </summary>
        public string Pattern { get; set; }
        
        /// <summary>
        /// Lookup source for Lookup validation (LOV name or query)
        /// </summary>
        public string LookupSource { get; set; }
        
        /// <summary>
        /// Field name for comparison validations (GreaterThan, LessThan, EqualTo)
        /// </summary>
        public string CompareFieldName { get; set; }
        
        /// <summary>
        /// Custom validation function for Custom validation type
        /// Parameters: (object value, object record, Dictionary[string,object] context)
        /// Returns: (bool isValid, string errorMessage)
        /// </summary>
        public Func<object, object, Dictionary<string, object>, Task<(bool isValid, string errorMessage)>> CustomValidator { get; set; }
        
        /// <summary>
        /// Condition expression - rule only applies if this evaluates to true
        /// </summary>
        public string Condition { get; set; }
        
        /// <summary>
        /// Condition function for complex condition logic
        /// </summary>
        public Func<object, object, bool> ConditionFunction { get; set; }
        
        #endregion
        
        #region Execution Order
        
        /// <summary>
        /// Order in which rules are executed (lower = first)
        /// </summary>
        public int ExecutionOrder { get; set; }
        
        /// <summary>
        /// Whether to stop validation on first failure
        /// </summary>
        public bool StopOnFailure { get; set; } = true;
        
        #endregion
        
        #region Metadata
        
        /// <summary>
        /// Description of what this rule validates
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// When the rule was registered
        /// </summary>
        public DateTime RegisteredDate { get; set; } = DateTime.Now;
        
        /// <summary>
        /// Who registered this rule
        /// </summary>
        public string RegisteredBy { get; set; }
        
        #endregion
        
        #region Factory Methods
        
        /// <summary>
        /// Create a Required validation rule
        /// </summary>
        public static ValidationRule Required(string itemName, string errorMessage = null)
        {
            return new ValidationRule
            {
                ItemName = itemName,
                ValidationType = ValidationType.Required,
                ErrorMessage = errorMessage
            };
        }
        
        /// <summary>
        /// Create a Range validation rule
        /// </summary>
        public static ValidationRule Range(string itemName, object min, object max, string errorMessage = null)
        {
            return new ValidationRule
            {
                ItemName = itemName,
                ValidationType = ValidationType.Range,
                MinValue = min,
                MaxValue = max,
                ErrorMessage = errorMessage
            };
        }
        
        /// <summary>
        /// Create a Pattern (regex) validation rule
        /// </summary>
        public static ValidationRule ForPattern(string itemName, string pattern, string errorMessage = null)
        {
            return new ValidationRule
            {
                ItemName = itemName,
                ValidationType = ValidationType.Pattern,
                Pattern = pattern,
                ErrorMessage = errorMessage
            };
        }
        
        /// <summary>
        /// Create a MaxLength validation rule
        /// </summary>
        public static ValidationRule MaxLength(string itemName, int maxLength, string errorMessage = null)
        {
            return new ValidationRule
            {
                ItemName = itemName,
                ValidationType = ValidationType.MaxLength,
                MaxValue = maxLength,
                ErrorMessage = errorMessage
            };
        }
        
        /// <summary>
        /// Create a custom validation rule
        /// </summary>
        public static ValidationRule Custom(string itemName, 
            Func<object, object, Dictionary<string, object>, Task<(bool isValid, string errorMessage)>> validator,
            string errorMessage = null)
        {
            return new ValidationRule
            {
                ItemName = itemName,
                ValidationType = ValidationType.Custom,
                CustomValidator = validator,
                ErrorMessage = errorMessage
            };
        }
        
        #endregion
    }
}
