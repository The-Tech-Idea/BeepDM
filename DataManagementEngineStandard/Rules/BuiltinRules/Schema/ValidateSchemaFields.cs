using System;
using System.Collections.Generic;
using System.Linq;

namespace TheTechIdea.Beep.Rules.BuiltinRules.Schema
{
    /// <summary>
    /// Validates that an input record contains all required fields and, optionally,
    /// that each field value can be parsed as the declared type.
    /// Parameters:
    ///   <c>Record</c>         — <c>IDictionary&lt;string,object&gt;</c>
    ///                           OR a semicolon-separated <c>"field=value;…"</c> string.
    ///   <c>RequiredFields</c> — comma-separated list of field names that must be present.
    ///   <c>FieldTypes</c>     — (optional) semicolon-separated <c>"field:type"</c> pairs.
    ///                           Supported types: int, double, decimal, bool, datetime, guid, string.
    /// Returns <c>true</c> when the record passes all checks.
    /// Outputs: <c>MissingFields</c>, <c>InvalidFields</c>, <c>Errors</c> (list).
    /// </summary>
    [Rule(ruleKey: "Schema.ValidateSchemaFields", ParserKey = "RulesParser", RuleName = "ValidateSchemaFields")]
    public sealed class ValidateSchemaFields : IRule
    {
        public string RuleText { get; set; } = "Schema.ValidateSchemaFields";
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

            if (record == null || record.Count == 0)
            {
                output["Error"] = "Record is empty or not provided.";
                return (output, false);
            }

            // --- required fields ---
            var missing = new List<string>();
            if (parameters.TryGetValue("RequiredFields", out var rfRaw))
            {
                var fields = rfRaw?.ToString()
                                   .Split(',', StringSplitOptions.RemoveEmptyEntries)
                                   .Select(f => f.Trim())
                             ?? Enumerable.Empty<string>();

                foreach (var f in fields)
                    if (!record.ContainsKey(f))
                        missing.Add(f);
            }

            // --- field type checks ---
            var invalid = new List<string>();
            if (parameters.TryGetValue("FieldTypes", out var ftRaw) && ftRaw != null)
            {
                foreach (var pair in ftRaw.ToString().Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    var parts = pair.Split(':', 2);
                    if (parts.Length != 2) continue;
                    var field = parts[0].Trim();
                    var type  = parts[1].Trim().ToLowerInvariant();

                    if (!record.TryGetValue(field, out var val)) continue; // handled by missing
                    var raw = val?.ToString() ?? string.Empty;

                    bool ok = type switch
                    {
                        "int"      => int.TryParse(raw, out _),
                        "double"   => double.TryParse(raw, out _),
                        "decimal"  => decimal.TryParse(raw, out _),
                        "bool"     => bool.TryParse(raw, out _),
                        "datetime" => DateTime.TryParse(raw, out _),
                        "guid"     => Guid.TryParse(raw, out _),
                        "string"   => true,
                        _          => true
                    };

                    if (!ok) invalid.Add($"{field}({type})");
                }
            }

            bool passed = missing.Count == 0 && invalid.Count == 0;
            output["Result"]        = passed;
            output["MissingFields"] = string.Join(", ", missing);
            output["InvalidFields"] = string.Join(", ", invalid);
            output["Errors"]        = missing.Select(f => $"Missing: {f}")
                                             .Concat(invalid.Select(f => $"InvalidType: {f}"))
                                             .ToList();
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
