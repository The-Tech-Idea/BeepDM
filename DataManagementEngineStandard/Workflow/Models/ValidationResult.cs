using System.Collections.Generic;

namespace TheTechIdea.Beep.Workflow.Models
{
    /// <summary>
    /// Common validation result class used across the workflow system
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// List of validation errors
        /// </summary>
        public List<string> Errors { get; } = new List<string>();

        /// <summary>
        /// List of validation warnings
        /// </summary>
        public List<string> Warnings { get; } = new List<string>();

        /// <summary>
        /// Indicates if the validation passed (no errors)
        /// </summary>
        public bool IsValid => Errors.Count == 0;

        /// <summary>
        /// Indicates if there are any warnings
        /// </summary>
        public bool HasWarnings => Warnings.Count > 0;

        /// <summary>
        /// Adds an error to the result
        /// </summary>
        public void AddError(string error)
        {
            if (!string.IsNullOrWhiteSpace(error))
            {
                Errors.Add(error);
            }
        }

        /// <summary>
        /// Adds a warning to the result
        /// </summary>
        public void AddWarning(string warning)
        {
            if (!string.IsNullOrWhiteSpace(warning))
            {
                Warnings.Add(warning);
            }
        }

        /// <summary>
        /// Clears all errors and warnings
        /// </summary>
        public void Clear()
        {
            Errors.Clear();
            Warnings.Clear();
        }

        /// <summary>
        /// Merges another validation result into this one
        /// </summary>
        public void Merge(ValidationResult other)
        {
            if (other != null)
            {
                Errors.AddRange(other.Errors);
                Warnings.AddRange(other.Warnings);
            }
        }

        /// <summary>
        /// Gets a summary of the validation result
        /// </summary>
        public string GetSummary()
        {
            if (IsValid)
            {
                if (HasWarnings)
                {
                    return $"Validation passed with {Warnings.Count} warning(s)";
                }
                return "Validation passed";
            }
            else
            {
                return $"Validation failed with {Errors.Count} error(s)" +
                       (HasWarnings ? $" and {Warnings.Count} warning(s)" : "");
            }
        }
    }
}
