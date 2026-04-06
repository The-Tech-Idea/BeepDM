using System;
using TheTechIdea.Beep.Editor.UOWManager.Models;

namespace TheTechIdea.Beep.Editor.UOWManager.Helpers
{
    /// <summary>
    /// Pre-built ValidationRule factories for common business scenarios.
    /// Ported from WinForms ValidationRuleHelpers into a platform-neutral BeepDM helper.
    /// All methods return a fully configured ValidationRule ready to register with IValidationManager.
    /// </summary>
    public static class ValidationRuleLibrary
    {
        // ---------------------------------------------------------------
        // Format rules
        // ---------------------------------------------------------------

        public static ValidationRule EmailRule(string blockName, string fieldName) => new()
        {
            RuleName = $"{blockName}_{fieldName}_Email",
            BlockName = blockName,
            ItemName = fieldName,
            ValidationType = ValidationType.Email,
            ErrorMessage = $"{fieldName} must be a valid email address"
        };

        public static ValidationRule PhoneRule(string blockName, string fieldName) => new()
        {
            RuleName = $"{blockName}_{fieldName}_Phone",
            BlockName = blockName,
            ItemName = fieldName,
            ValidationType = ValidationType.Pattern,
            Pattern = @"^\(?\d{3}\)?[-.\s]?\d{3}[-.\s]?\d{4}$",
            ErrorMessage = $"{fieldName} must be a valid phone number (e.g., (555) 123-4567)"
        };

        public static ValidationRule URLRule(string blockName, string fieldName) => new()
        {
            RuleName = $"{blockName}_{fieldName}_URL",
            BlockName = blockName,
            ItemName = fieldName,
            ValidationType = ValidationType.Url,
            ErrorMessage = $"{fieldName} must be a valid URL"
        };

        public static ValidationRule PostalCodeRule(string blockName, string fieldName) => new()
        {
            RuleName = $"{blockName}_{fieldName}_PostalCode",
            BlockName = blockName,
            ItemName = fieldName,
            ValidationType = ValidationType.Pattern,
            Pattern = @"^\d{5}(-\d{4})?$",
            ErrorMessage = $"{fieldName} must be a valid postal code (e.g., 12345 or 12345-6789)"
        };

        public static ValidationRule IpAddressRule(string blockName, string fieldName) => new()
        {
            RuleName = $"{blockName}_{fieldName}_IpAddress",
            BlockName = blockName,
            ItemName = fieldName,
            ValidationType = ValidationType.Pattern,
            Pattern = @"^(\d{1,3}\.){3}\d{1,3}$",
            ErrorMessage = $"{fieldName} must be a valid IP address"
        };

        // ---------------------------------------------------------------
        // Required / length rules
        // ---------------------------------------------------------------

        public static ValidationRule NonEmptyRule(string blockName, string fieldName) => new()
        {
            RuleName = $"{blockName}_{fieldName}_NonEmpty",
            BlockName = blockName,
            ItemName = fieldName,
            ValidationType = ValidationType.Required,
            ErrorMessage = $"{fieldName} is required"
        };

        public static ValidationRule MinLengthRule(string blockName, string fieldName, int minLength) => new()
        {
            RuleName = $"{blockName}_{fieldName}_MinLength_{minLength}",
            BlockName = blockName,
            ItemName = fieldName,
            ValidationType = ValidationType.MinLength,
            MinValue = minLength,
            ErrorMessage = $"{fieldName} must be at least {minLength} characters"
        };

        public static ValidationRule MaxLengthRule(string blockName, string fieldName, int maxLength) => new()
        {
            RuleName = $"{blockName}_{fieldName}_MaxLength_{maxLength}",
            BlockName = blockName,
            ItemName = fieldName,
            ValidationType = ValidationType.MaxLength,
            MaxValue = maxLength,
            ErrorMessage = $"{fieldName} must be at most {maxLength} characters"
        };

        // ---------------------------------------------------------------
        // Numeric / range rules
        // ---------------------------------------------------------------

        public static ValidationRule PositiveNumberRule(string blockName, string fieldName) => new()
        {
            RuleName = $"{blockName}_{fieldName}_Positive",
            BlockName = blockName,
            ItemName = fieldName,
            ValidationType = ValidationType.Range,
            MinValue = 0,
            ErrorMessage = $"{fieldName} must be a positive number"
        };

        public static ValidationRule PercentageRule(string blockName, string fieldName) => new()
        {
            RuleName = $"{blockName}_{fieldName}_Percentage",
            BlockName = blockName,
            ItemName = fieldName,
            ValidationType = ValidationType.Range,
            MinValue = 0,
            MaxValue = 100,
            ErrorMessage = $"{fieldName} must be between 0 and 100"
        };

        public static ValidationRule RangeRule(string blockName, string fieldName, object min, object max) => new()
        {
            RuleName = $"{blockName}_{fieldName}_Range_{min}_{max}",
            BlockName = blockName,
            ItemName = fieldName,
            ValidationType = ValidationType.Range,
            MinValue = min,
            MaxValue = max,
            ErrorMessage = $"{fieldName} must be between {min} and {max}"
        };

        // ---------------------------------------------------------------
        // Date rules
        // ---------------------------------------------------------------

        public static ValidationRule FutureDateRule(string blockName, string fieldName) => new()
        {
            RuleName = $"{blockName}_{fieldName}_Future",
            BlockName = blockName,
            ItemName = fieldName,
            ValidationType = ValidationType.Date,
            MinValue = DateTime.Today,
            ErrorMessage = $"{fieldName} must be today or in the future"
        };

        public static ValidationRule PastDateRule(string blockName, string fieldName) => new()
        {
            RuleName = $"{blockName}_{fieldName}_Past",
            BlockName = blockName,
            ItemName = fieldName,
            ValidationType = ValidationType.Date,
            MaxValue = DateTime.Today,
            ErrorMessage = $"{fieldName} must be today or in the past"
        };

        // ---------------------------------------------------------------
        // Pattern rule (generic)
        // ---------------------------------------------------------------

        public static ValidationRule PatternRule(string blockName, string fieldName, string pattern, string errorMessage = null) => new()
        {
            RuleName = $"{blockName}_{fieldName}_Pattern",
            BlockName = blockName,
            ItemName = fieldName,
            ValidationType = ValidationType.Pattern,
            Pattern = pattern,
            ErrorMessage = errorMessage ?? $"{fieldName} has invalid format"
        };
    }
}
