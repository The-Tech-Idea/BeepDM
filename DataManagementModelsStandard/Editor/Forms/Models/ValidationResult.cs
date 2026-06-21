using System;
using System.Collections.Generic;
using System.Linq;

namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// Result of a single validation rule execution
    /// </summary>
    public class ValidationRuleResult
    {
        /// <summary>
        /// Name of the rule that was executed
        /// </summary>
        public string RuleName { get; set; }
        
        /// <summary>
        /// Whether validation passed
        /// </summary>
        public bool IsValid { get; set; }
        
        /// <summary>
        /// Error message if validation failed
        /// </summary>
        public string ErrorMessage { get; set; }
        
        /// <summary>
        /// Severity of the validation failure
        /// </summary>
        public ValidationSeverity Severity { get; set; }
        
        /// <summary>
        /// The field/item that was validated
        /// </summary>
        public string ItemName { get; set; }
        
        /// <summary>
        /// The value that was validated
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// Exception captured while executing the validation rule, when applicable.
        /// </summary>
        public Exception Exception { get; internal set; }

        /// <summary>
        /// Factory method for successful validation
        /// </summary>
        public static ValidationRuleResult Success(string ruleName, string itemName)
        {
            return new ValidationRuleResult
            {
                RuleName = ruleName,
                ItemName = itemName,
                IsValid = true
            };
        }
        
        /// <summary>
        /// Factory method for failed validation
        /// </summary>
        public static ValidationRuleResult Failure(string ruleName, string itemName, 
            string errorMessage, ValidationSeverity severity = ValidationSeverity.Error)
        {
            return new ValidationRuleResult
            {
                RuleName = ruleName,
                ItemName = itemName,
                IsValid = false,
                ErrorMessage = errorMessage,
                Severity = severity
            };
        }
    }

    /// <summary>
    /// Result of validating an item/field (may have multiple rules)
    /// </summary>
    public class ItemValidationResult
    {
        /// <summary>
        /// Block name containing the item
        /// </summary>
        public string BlockName { get; set; }
        
        /// <summary>
        /// Name of the item/field validated
        /// </summary>
        public string ItemName { get; set; }
        
        /// <summary>
        /// The value that was validated
        /// </summary>
        public object Value { get; set; }
        
        /// <summary>
        /// Whether all validations passed
        /// </summary>
        public bool IsValid => !RuleResults.Any(r => !r.IsValid && r.Severity >= ValidationSeverity.Error);
        
        /// <summary>
        /// Whether there are any warnings (even if valid)
        /// </summary>
        public bool HasWarnings => RuleResults.Any(r => !r.IsValid && r.Severity == ValidationSeverity.Warning);
        
        /// <summary>
        /// Results from individual validation rules
        /// </summary>
        public List<ValidationRuleResult> RuleResults { get; set; } = new List<ValidationRuleResult>();
        
        /// <summary>
        /// Get all error messages
        /// </summary>
        public IEnumerable<string> ErrorMessages => 
            RuleResults.Where(r => !r.IsValid && r.Severity >= ValidationSeverity.Error)
                      .Select(r => r.ErrorMessage);
        
        /// <summary>
        /// Get all warning messages
        /// </summary>
        public IEnumerable<string> WarningMessages => 
            RuleResults.Where(r => !r.IsValid && r.Severity == ValidationSeverity.Warning)
                      .Select(r => r.ErrorMessage);
        
        /// <summary>
        /// Get first error message (most common case)
        /// </summary>
        public string FirstError => ErrorMessages.FirstOrDefault();
        
        /// <summary>
        /// Timestamp when validation was performed
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Result of validating a record (all items)
    /// </summary>
    public class RecordValidationResult
    {
        /// <summary>
        /// Block name containing the record
        /// </summary>
        public string BlockName { get; set; }
        
        /// <summary>
        /// Record index that was validated
        /// </summary>
        public int RecordIndex { get; set; }
        
        /// <summary>
        /// Reference to the record object
        /// </summary>
        public object Record { get; set; }
        
        /// <summary>
        /// Whether all item validations passed
        /// </summary>
        public bool IsValid => ItemResults.All(r => r.Value.IsValid);
        
        /// <summary>
        /// Whether there are any warnings
        /// </summary>
        public bool HasWarnings => ItemResults.Any(r => r.Value.HasWarnings);
        
        /// <summary>
        /// Validation results for each item
        /// </summary>
        public Dictionary<string, ItemValidationResult> ItemResults { get; set; } 
            = new Dictionary<string, ItemValidationResult>(StringComparer.OrdinalIgnoreCase);
        
        /// <summary>
        /// Get items that failed validation
        /// </summary>
        public IEnumerable<string> InvalidItems => 
            ItemResults.Where(kvp => !kvp.Value.IsValid).Select(kvp => kvp.Key);
        
        /// <summary>
        /// Get all error messages grouped by item
        /// </summary>
        public Dictionary<string, IEnumerable<string>> ErrorsByItem =>
            ItemResults.Where(kvp => !kvp.Value.IsValid)
                      .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ErrorMessages);
        
        /// <summary>
        /// Timestamp when validation was performed
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Result of validating a block (all records)
    /// </summary>
    public class BlockValidationResult
    {
        /// <summary>
        /// Block name that was validated
        /// </summary>
        public string BlockName { get; set; }
        
        /// <summary>
        /// Whether all record validations passed
        /// </summary>
        public bool IsValid => RecordResults.All(r => r.IsValid);
        
        /// <summary>
        /// Whether there are any warnings
        /// </summary>
        public bool HasWarnings => RecordResults.Any(r => r.HasWarnings);
        
        /// <summary>
        /// Total number of records validated
        /// </summary>
        public int RecordCount => RecordResults.Count;
        
        /// <summary>
        /// Number of records with errors
        /// </summary>
        public int ErrorCount => RecordResults.Count(r => !r.IsValid);
        
        /// <summary>
        /// Validation results for each record
        /// </summary>
        public List<RecordValidationResult> RecordResults { get; set; } = new List<RecordValidationResult>();
        
        /// <summary>
        /// Timestamp when validation was performed
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Result of validating a form (all blocks)
    /// </summary>
    public class FormValidationResult
    {
        /// <summary>
        /// Form name that was validated
        /// </summary>
        public string FormName { get; set; }
        
        /// <summary>
        /// Whether all block validations passed
        /// </summary>
        public bool IsValid => BlockResults.All(r => r.Value.IsValid);
        
        /// <summary>
        /// Whether there are any warnings
        /// </summary>
        public bool HasWarnings => BlockResults.Any(r => r.Value.HasWarnings);
        
        /// <summary>
        /// Total number of blocks validated
        /// </summary>
        public int BlockCount => BlockResults.Count;
        
        /// <summary>
        /// Number of blocks with errors
        /// </summary>
        public int ErrorBlockCount => BlockResults.Count(r => !r.Value.IsValid);
        
        /// <summary>
        /// Validation results for each block
        /// </summary>
        public Dictionary<string, BlockValidationResult> BlockResults { get; set; } 
            = new Dictionary<string, BlockValidationResult>(StringComparer.OrdinalIgnoreCase);
        
        /// <summary>
        /// Timestamp when validation was performed
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
