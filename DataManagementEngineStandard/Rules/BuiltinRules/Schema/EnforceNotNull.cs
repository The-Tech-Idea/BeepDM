using System;
using System.Collections.Generic;
using System.Linq;

namespace TheTechIdea.Beep.Rules.BuiltinRules.Schema
{
    /// <summary>
    /// Enforces that a set of named fields are non-null and non-empty in the input record.
    /// Parameters:
    ///   <c>Record</c>  — <c>IDictionary&lt;string,object&gt;</c>
    ///                    OR a <c>"field=value;…"</c> semicolon-separated string.
    ///   <c>Fields</c>  — comma-separated list of field names to validate.
    /// Returns <c>true</c> when every specified field is present and non-empty.
    /// Outputs: <c>NullFields</c> (comma-separated list of offending fields).
    /// </summary>
    [Rule(ruleKey: "Schema.EnforceNotNull", ParserKey = "RulesParser", RuleName = "EnforceNotNull")]
    public sealed class EnforceNotNull : IRule
    {
        public string RuleText { get; set; } = "Schema.EnforceNotNull";
        public IRuleStructure Structure { get; set; } = new RuleStructure();

        public (Dictionary<string, object> outputs, object result) SolveRule(
            Dictionary<string, object> parameters = null)
        {
            var output = new Dictionary<string, object>();

            if (parameters == null)
            {
                output["Error"] = "No parameters supplied.";
                return (output, false);
            }

            // --- resolve record ---
            IDictionary<string, object> record = null;
            if (parameters.TryGetValue("Record", out var recRaw))
            {
                if (recRaw is IDictionary<string, object> dict)
                    record = dict;
                else if (recRaw is string s)
                    record = ParseKvPairs(s);
            }

            if (record == null)
            {
                output["Error"] = "Record is not provided.";
                return (output, false);
            }

            // --- fields to check ---
            if (!parameters.TryGetValue("Fields", out var fieldsRaw) || fieldsRaw == null)
            {
                output["Error"] = "Missing required parameter: Fields";
                return (output, false);
            }

            var fields = fieldsRaw.ToString()
                                   .Split(',', StringSplitOptions.RemoveEmptyEntries)
                                   .Select(f => f.Trim());

            var nullFields = new List<string>();
            foreach (var field in fields)
            {
                if (!record.TryGetValue(field, out var val))
                {
                    nullFields.Add(field); // missing counts as null
                    continue;
                }

                bool isNull = val is null
                           || val is DBNull
                           || string.IsNullOrWhiteSpace(val.ToString());
                if (isNull) nullFields.Add(field);
            }

            bool passed = nullFields.Count == 0;
            output["Result"]     = passed;
            output["NullFields"] = string.Join(", ", nullFields);
            return (output, passed);
        }

        private static Dictionary<string, object> ParseKvPairs(string raw)
        {
            var d = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in raw.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var idx = pair.IndexOf('=');
                if (idx <= 0) continue;
                d[pair[..idx].Trim()] = pair[(idx + 1)..].Trim();
            }
            return d;
        }
    }
}
