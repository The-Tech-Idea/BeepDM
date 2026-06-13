using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.Schema
{
    /// <summary>
    /// Utilities for parsing rule engine output dictionaries and return values.
    /// Extracted from <see cref="SchemaFingerprinter"/> to keep that class
    /// single-responsibility.
    ///
    /// <para>
    /// The rule engine returns (IDictionary{string, object}, object). Callers
    /// typically pull a named key out of the dictionary and treat the result
    /// as a string, bool, or a specific value. These helpers consolidate the
    /// parsing pattern.
    /// </para>
    /// </summary>
    public static class RuleOutputParser
    {
        /// <summary>
        /// Read a string value from a rule output dictionary. Returns
        /// <paramref name="fallback"/> when the key is missing or the value is null.
        /// </summary>
        public static string ReadString(IReadOnlyDictionary<string, object>? outputs, string key, string fallback)
        {
            if (outputs == null || string.IsNullOrEmpty(key)) return fallback;
            return outputs.TryGetValue(key, out var v) && v != null ? v.ToString() ?? fallback : fallback;
        }

        /// <summary>
        /// Read a boolean value from a rule output dictionary. The value is
        /// considered <c>true</c> if it's a bool <c>true</c>, or if its string
        /// form is anything other than the literal <c>"false"</c>.
        /// </summary>
        public static bool ReadBoolean(IReadOnlyDictionary<string, object>? outputs, string key, bool fallback = false)
        {
            if (outputs == null || string.IsNullOrEmpty(key)) return fallback;
            if (!outputs.TryGetValue(key, out var v) || v == null) return fallback;
            return v is bool b ? b : v.ToString() != "false";
        }

        /// <summary>
        /// Read a boolean value from a rule return value (the second tuple
        /// element from <c>IRuleEngine.SolveRule</c>). Treats <c>null</c> as
        /// <paramref name="fallback"/>.
        /// </summary>
        public static bool ReadBoolean(object? result, bool fallback = false)
        {
            if (result == null) return fallback;
            return result is bool b ? b : result.ToString() != "false";
        }

        /// <summary>
        /// Build a <see cref="TheTechIdea.Beep.Rules.RuleExecutionPolicy"/> from a
        /// policy-shape with sensible defaults. Shared by BeepSync's rule-evaluation
        /// sites and the schema manager's preflight, so they all share the same depth
        /// and timeout interpretation.
        /// </summary>
        /// <param name="maxDepth">Requested MaxDepth; falls back to <paramref name="defaultMaxDepth"/> when &lt;= 0.</param>
        /// <param name="maxExecutionMs">Requested MaxExecutionMs; falls back to <paramref name="defaultMaxMs"/> when &lt;= 0.</param>
        /// <param name="defaultMaxDepth">Default MaxDepth when caller did not specify one.</param>
        /// <param name="defaultMaxMs">Default MaxExecutionMs when caller did not specify one.</param>
        public static TheTechIdea.Beep.Rules.RuleExecutionPolicy BuildRulePolicy(
            int maxDepth, int maxExecutionMs,
            int defaultMaxDepth = 10, int defaultMaxMs = 5000) =>
            new TheTechIdea.Beep.Rules.RuleExecutionPolicy
            {
                MaxDepth       = maxDepth       > 0 ? maxDepth       : defaultMaxDepth,
                MaxExecutionMs = maxExecutionMs > 0 ? maxExecutionMs : defaultMaxMs
            };
    }
}
