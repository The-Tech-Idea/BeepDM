using System;
using System.Collections.Generic;
using System.Linq;

namespace TheTechIdea.Beep.Editor
{
    /// <summary>
    /// Severity level for a validation error.
    /// </summary>
    public enum ValidationSeverity
    {
        /// <summary>Blocks commit — item cannot be saved.</summary>
        Error,
        /// <summary>Advisory — item can still be saved.</summary>
        Warning,
        /// <summary>Informational only.</summary>
        Info
    }

    /// <summary>
    /// Represents a single validation error on a property.
    /// </summary>
    public class ValidationError
    {
        /// <summary>Name of the property that failed validation (null for object-level errors).</summary>
        public string PropertyName { get; set; }

        /// <summary>Human-readable error message.</summary>
        public string ErrorMessage { get; set; }

        /// <summary>Severity of the error.</summary>
        public ValidationSeverity Severity { get; set; }

        public ValidationError() { }

        public ValidationError(string propertyName, string errorMessage, ValidationSeverity severity = ValidationSeverity.Error)
        {
            PropertyName = propertyName;
            ErrorMessage = errorMessage;
            Severity = severity;
        }

        public override string ToString() => $"[{Severity}] {PropertyName}: {ErrorMessage}";
    }

    /// <summary>
    /// Aggregate validation result for a single item.
    /// </summary>
    public class ValidationResult
    {
        /// <summary>All validation errors (including warnings and info).</summary>
        public List<ValidationError> Errors { get; set; } = new List<ValidationError>();

        /// <summary>True when there are no Error-severity entries.</summary>
        public bool IsValid => !Errors.Any(e => e.Severity == ValidationSeverity.Error);

        /// <summary>True when there are no entries at all.</summary>
        public bool HasNoIssues => Errors.Count == 0;

        /// <summary>Number of Error-severity entries.</summary>
        public int ErrorCount => Errors.Count(e => e.Severity == ValidationSeverity.Error);

        /// <summary>Number of Warning-severity entries.</summary>
        public int WarningCount => Errors.Count(e => e.Severity == ValidationSeverity.Warning);

        /// <summary>Merges another ValidationResult into this one.</summary>
        public void Merge(ValidationResult other)
        {
            if (other?.Errors != null)
                Errors.AddRange(other.Errors);
        }

        /// <summary>Gets errors for a specific property.</summary>
        public List<ValidationError> GetPropertyErrors(string propertyName)
        {
            return Errors.Where(e => e.PropertyName == propertyName).ToList();
        }
    }

    /// <summary>
    /// EventArgs raised when validation fails for an item.
    /// </summary>
    public class ValidationEventArgs<T> : EventArgs
    {
        /// <summary>The item that failed validation.</summary>
        public T Item { get; }

        /// <summary>The validation result with all errors.</summary>
        public ValidationResult Result { get; }

        /// <summary>The property that triggered validation (null for full-item validation).</summary>
        public string TriggerProperty { get; }

        public ValidationEventArgs(T item, ValidationResult result, string triggerProperty = null)
        {
            Item = item;
            Result = result;
            TriggerProperty = triggerProperty;
        }
    }
}
