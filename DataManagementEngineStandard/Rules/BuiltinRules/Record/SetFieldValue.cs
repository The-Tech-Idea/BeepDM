using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TheTechIdea.Beep.Rules.BuiltinRules.Record
{
    /// <summary>
    /// Sets a named field in a record to a literal value or a <c>{Token}</c>-interpolated string.
    /// Tokens are resolved from the <c>Record</c> dictionary or individual parameters.
    /// Parameters:
    ///   <c>FieldName</c> — name of the field to set.
    ///   <c>Value</c>     — new value; may contain <c>{OtherField}</c> tokens.
    ///   <c>Record</c>    — <see cref="IDictionary{TKey,TValue}"/> or <c>"field=val;…"</c> string (optional).
    /// Outputs: <c>FieldName</c>, <c>Value</c> (resolved), <c>Record</c> (updated copy).
    /// </summary>
    [Rule(ruleKey: "Record.SetFieldValue", ParserKey = "RulesParser", RuleName = "SetFieldValue")]
    public sealed class SetFieldValue : IRule
    {
        private static readonly Regex _token = new(@"\{(\w+)\}", RegexOptions.Compiled);

        public string RuleText { get; set; } = "Record.SetFieldValue";
        public IRuleStructure Structure { get; set; } = new RuleStructure();

        public (Dictionary<string, object> outputs, object result) SolveRule(
            Dictionary<string, object> parameters = null)
        {
            var output = new Dictionary<string, object>();
            if (parameters == null || !parameters.TryGetValue("FieldName", out var fnRaw))
            {
                output["Error"] = "Missing parameter: FieldName";
                return (output, false);
            }

            parameters.TryGetValue("Value",  out var valRaw);
            var fieldName = fnRaw.ToString();
            var rawValue  = valRaw?.ToString() ?? string.Empty;

            // Build flat lookup from Record + direct params
            var lookup = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (parameters.TryGetValue("Record", out var recRaw))
            {
                if (recRaw is IDictionary<string, object> d)
                    foreach (var kv in d) lookup[kv.Key] = kv.Value;
                else if (recRaw is string s)
                    foreach (var pair in s.Split(';', StringSplitOptions.RemoveEmptyEntries))
                    { var idx = pair.IndexOf('='); if (idx > 0) lookup[pair[..idx].Trim()] = pair[(idx + 1)..].Trim(); }
            }
            foreach (var kv in parameters)
                if (kv.Key != "FieldName" && kv.Key != "Value" && kv.Key != "Record")
                    lookup.TryAdd(kv.Key, kv.Value);

            // Interpolate {Token} placeholders
            string resolved = _token.Replace(rawValue, m =>
            {
                var key = m.Groups[1].Value;
                return lookup.TryGetValue(key, out var v) ? v?.ToString() ?? string.Empty : string.Empty;
            });

            lookup[fieldName] = resolved;

            output["FieldName"] = fieldName;
            output["Value"]     = resolved;
            output["Record"]    = lookup;
            return (output, resolved);
        }
    }
}
