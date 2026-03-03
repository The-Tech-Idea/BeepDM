namespace TheTechIdea.Beep.Editor.Importing.Quality
{
    /// <summary>What to do when a quality rule fires against a record.</summary>
    public enum DataQualityAction
    {
        /// <summary>Reject the record entirely — it is not written to the destination.</summary>
        Block,
        /// <summary>Move the record to the error store for later analysis.</summary>
        Quarantine,
        /// <summary>Log the issue but still write the record.</summary>
        Warn
    }

    /// <summary>
    /// Contract for a single data-quality rule evaluated per-record during import.
    /// Implementations live in <see cref="BuiltInRules"/> or can be supplied by callers.
    /// </summary>
    public interface IDataQualityRule
    {
        /// <summary>Human-readable rule identifier (e.g. "not_null:CustomerID").</summary>
        string RuleName { get; }

        /// <summary>The field this rule is scoped to.</summary>
        string FieldName { get; }

        /// <summary>Action taken when <see cref="Evaluate"/> returns <c>false</c>.</summary>
        DataQualityAction OnFailure { get; }

        /// <summary>
        /// Returns <c>true</c> when the record passes the rule, <c>false</c> when it fails.
        /// </summary>
        /// <param name="fieldValue">Extracted value of <see cref="FieldName"/> from <paramref name="record"/>.</param>
        /// <param name="record">The full source record (for cross-field rules).</param>
        bool Evaluate(object? fieldValue, object record);

        /// <summary>Builds a human-readable failure description for error logging.</summary>
        string FailureMessage(object? fieldValue);
    }
}
