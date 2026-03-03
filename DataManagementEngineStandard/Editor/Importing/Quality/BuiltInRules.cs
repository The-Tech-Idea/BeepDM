using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TheTechIdea.Beep.Editor.Importing.Quality
{
    /// <summary>Records must have a non-null value for <see cref="IDataQualityRule.FieldName"/>.</summary>
    public sealed class NotNullRule : IDataQualityRule
    {
        public string FieldName  { get; }
        public string RuleName   => $"not_null:{FieldName}";
        public DataQualityAction OnFailure { get; }

        public NotNullRule(string fieldName, DataQualityAction onFailure = DataQualityAction.Block)
        {
            FieldName  = fieldName ?? throw new ArgumentNullException(nameof(fieldName));
            OnFailure  = onFailure;
        }

        public bool Evaluate(object? fieldValue, object record) =>
            fieldValue != null && fieldValue != DBNull.Value && !string.IsNullOrWhiteSpace(fieldValue.ToString());

        public string FailureMessage(object? fieldValue) =>
            $"Field '{FieldName}' must not be null or empty.";
    }

    /// <summary>All values seen in <see cref="IDataQualityRule.FieldName"/> within a batch must be distinct.</summary>
    public sealed class UniqueRule : IDataQualityRule
    {
        private readonly HashSet<string> _seen = new(StringComparer.OrdinalIgnoreCase);

        public string FieldName  { get; }
        public string RuleName   => $"unique:{FieldName}";
        public DataQualityAction OnFailure { get; }

        public UniqueRule(string fieldName, DataQualityAction onFailure = DataQualityAction.Block)
        {
            FieldName = fieldName ?? throw new ArgumentNullException(nameof(fieldName));
            OnFailure = onFailure;
        }

        public bool Evaluate(object? fieldValue, object record)
        {
            var key = fieldValue?.ToString() ?? string.Empty;
            return _seen.Add(key);
        }

        public string FailureMessage(object? fieldValue) =>
            $"Field '{FieldName}' contains duplicate value '{fieldValue}'.";
    }

    /// <summary>Numeric or date value must fall within [<see cref="Min"/>, <see cref="Max"/>].</summary>
    public sealed class RangeRule : IDataQualityRule
    {
        public string  FieldName  { get; }
        public string  RuleName   => $"range:{FieldName}[{Min},{Max}]";
        public DataQualityAction OnFailure { get; }
        public IComparable Min { get; }
        public IComparable Max { get; }

        public RangeRule(string fieldName, IComparable min, IComparable max,
            DataQualityAction onFailure = DataQualityAction.Block)
        {
            FieldName = fieldName ?? throw new ArgumentNullException(nameof(fieldName));
            Min = min;  Max = max;  OnFailure = onFailure;
        }

        public bool Evaluate(object? fieldValue, object record)
        {
            if (fieldValue == null || fieldValue == DBNull.Value) return false;
            try
            {
                var converted = Convert.ChangeType(fieldValue, Min.GetType());
                return Min.CompareTo(converted) <= 0 && Max.CompareTo(converted) >= 0;
            }
            catch { return false; }
        }

        public string FailureMessage(object? fieldValue) =>
            $"Field '{FieldName}' value '{fieldValue}' is outside the accepted range [{Min}, {Max}].";
    }

    /// <summary>String value must match a regular expression pattern.</summary>
    public sealed class RegexRule : IDataQualityRule
    {
        private readonly Regex _regex;

        public string FieldName  { get; }
        public string RuleName   => $"regex:{FieldName}";
        public DataQualityAction OnFailure { get; }

        public RegexRule(string fieldName, string pattern,
            DataQualityAction onFailure = DataQualityAction.Block,
            RegexOptions options = RegexOptions.None)
        {
            FieldName  = fieldName ?? throw new ArgumentNullException(nameof(fieldName));
            _regex     = new Regex(pattern ?? throw new ArgumentNullException(nameof(pattern)), options);
            OnFailure  = onFailure;
        }

        public bool Evaluate(object? fieldValue, object record)
        {
            if (fieldValue == null || fieldValue == DBNull.Value) return false;
            return _regex.IsMatch(fieldValue.ToString()!);
        }

        public string FailureMessage(object? fieldValue) =>
            $"Field '{FieldName}' value '{fieldValue}' does not match the required pattern.";
    }

    /// <summary>Value must belong to a declared set of accepted values.</summary>
    public sealed class AcceptedValuesRule : IDataQualityRule
    {
        private readonly HashSet<string> _accepted;

        public string FieldName  { get; }
        public string RuleName   => $"accepted_values:{FieldName}";
        public DataQualityAction OnFailure { get; }

        public AcceptedValuesRule(string fieldName, IEnumerable<string> acceptedValues,
            DataQualityAction onFailure = DataQualityAction.Block)
        {
            FieldName  = fieldName ?? throw new ArgumentNullException(nameof(fieldName));
            _accepted  = new HashSet<string>(acceptedValues, StringComparer.OrdinalIgnoreCase);
            OnFailure  = onFailure;
        }

        public bool Evaluate(object? fieldValue, object record) =>
            fieldValue != null && _accepted.Contains(fieldValue.ToString()!);

        public string FailureMessage(object? fieldValue) =>
            $"Field '{FieldName}' value '{fieldValue}' is not in the accepted value set.";
    }

    /// <summary>
    /// Value must exist as a key in a lookup data source.
    /// Requires the lookup datasource to be open; evaluation is done in-memory against a
    /// pre-loaded key set supplied at construction time to avoid per-record round trips.
    /// </summary>
    public sealed class ReferentialIntegrityRule : IDataQualityRule
    {
        private readonly HashSet<string> _lookupKeys;

        public string FieldName  { get; }
        public string RuleName   => $"referential_integrity:{FieldName}";
        public DataQualityAction OnFailure { get; }

        public ReferentialIntegrityRule(string fieldName, IEnumerable<string> lookupKeys,
            DataQualityAction onFailure = DataQualityAction.Block)
        {
            FieldName   = fieldName ?? throw new ArgumentNullException(nameof(fieldName));
            _lookupKeys = new HashSet<string>(lookupKeys, StringComparer.OrdinalIgnoreCase);
            OnFailure   = onFailure;
        }

        public bool Evaluate(object? fieldValue, object record) =>
            fieldValue != null && _lookupKeys.Contains(fieldValue.ToString()!);

        public string FailureMessage(object? fieldValue) =>
            $"Field '{FieldName}' value '{fieldValue}' does not exist in the referenced data source.";
    }
}
