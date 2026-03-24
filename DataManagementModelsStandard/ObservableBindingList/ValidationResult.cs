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
        Info,
        /// <summary>Critical error message.</summary>
        Critical
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
    /// Aggregate validation result for a single item or field.
    /// Supports both aggregate error collection and single-field validation scenarios.
    /// </summary>
    public class ValidationResult
    {
        private bool? _isValidOverride;

        /// <summary>All validation errors (including warnings and info).</summary>
        public List<ValidationError> Errors { get; set; } = new();

        /// <summary>
        /// Gets or sets whether the validation passed.
        /// When set explicitly, uses the set value; otherwise computes from Errors.
        /// </summary>
        public bool IsValid
        {
            get => _isValidOverride ?? !Errors.Any(e => e.Severity == ValidationSeverity.Error);
            set => _isValidOverride = value;
        }

        /// <summary>Gets or sets the name of the field being validated.</summary>
        public string FieldName { get; set; }

        /// <summary>Gets or sets the primary error message.</summary>
        public string ErrorMessage { get; set; }

        /// <summary>Gets or sets the name of the block containing the field.</summary>
        public string BlockName { get; set; }

        /// <summary>Gets or sets a collection of validation error messages.</summary>
        public List<string> ValidationMessages { get; set; } = new();

        /// <summary>Gets or sets the severity level of the validation result.</summary>
        public ValidationSeverity Severity { get; set; } = ValidationSeverity.Error;

        /// <summary>Gets or sets additional context information.</summary>
        public Dictionary<string, object> Context { get; set; } = new();

        /// <summary>Gets or sets the timestamp when validation was performed.</summary>
        public DateTime ValidationTimestamp { get; set; } = DateTime.Now;

        /// <summary>Gets or sets the value that was validated.</summary>
        public object ValidatedValue { get; set; }

        /// <summary>True when there are no entries at all.</summary>
        public bool HasNoIssues => Errors.Count == 0 && ValidationMessages.Count == 0;

        /// <summary>Number of Error-severity entries.</summary>
        public int ErrorCount => Errors.Count(e => e.Severity == ValidationSeverity.Error);

        /// <summary>Number of Warning-severity entries.</summary>
        public int WarningCount => Errors.Count(e => e.Severity == ValidationSeverity.Warning);

        /// <summary>Merges another ValidationResult into this one.</summary>
        public void Merge(ValidationResult other)
        {
            if (other?.Errors != null)
                Errors.AddRange(other.Errors);
            if (other?.ValidationMessages != null)
                ValidationMessages.AddRange(other.ValidationMessages);
        }

        /// <summary>Gets errors for a specific property.</summary>
        public List<ValidationError> GetPropertyErrors(string propertyName)
        {
            return Errors.Where(e => e.PropertyName == propertyName).ToList();
        }

        /// <summary>Adds a validation error message.</summary>
        /// <param name="message">The error message to add.</param>
        public void AddError(string message)
        {
            ValidationMessages.Add(message);
            if (string.IsNullOrEmpty(ErrorMessage))
            {
                ErrorMessage = message;
            }
            _isValidOverride = false;
        }

        /// <summary>Adds a validation warning message.</summary>
        /// <param name="message">The warning message to add.</param>
        public void AddWarning(string message)
        {
            ValidationMessages.Add($"WARNING: {message}");
            if (Severity == ValidationSeverity.Info)
            {
                Severity = ValidationSeverity.Warning;
            }
        }

        /// <summary>Adds contextual information.</summary>
        /// <param name="key">The context key.</param>
        /// <param name="value">The context value.</param>
        public void AddContext(string key, object value)
        {
            Context[key] = value;
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
