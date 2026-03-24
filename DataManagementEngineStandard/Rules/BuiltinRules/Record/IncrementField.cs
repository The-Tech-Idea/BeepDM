using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Rules.BuiltinRules.Record
{
    /// <summary>
    /// Adds a numeric <c>Step</c> to a field value, with optional min/max clamping.
    /// Parameters:
    ///   <c>Record</c>    — <see cref="IDictionary{TKey,TValue}"/> or <c>"field=val;…"</c> string (optional).
    ///   <c>FieldName</c> — numeric field to increment.
    ///   <c>Step</c>      — amount to add; can be negative for decrement (default 1).
    ///   <c>Clamp</c>     — (optional) <c>"min:max"</c> string to clamp the result.
    /// Outputs: <c>Record</c> (updated), <c>OldValue</c>, <c>NewValue</c>.
    /// Returns the new numeric value.
    /// </summary>
    [Rule(ruleKey: "Record.IncrementField", ParserKey = "RulesParser", RuleName = "IncrementField")]
    public sealed class IncrementField : IRule
    {
        public string RuleText { get; set; } = "Record.IncrementField";
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

            parameters.TryGetValue("Step",   out var stepRaw);
            parameters.TryGetValue("Record", out var recRaw);
            parameters.TryGetValue("Clamp",  out var clampRaw);

            var fieldName = fnRaw.ToString();
            double step   = 1.0;
            if (stepRaw != null) double.TryParse(stepRaw.ToString(), out step);

            var record = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (recRaw is IDictionary<string, object> d)
                foreach (var kv in d) record[kv.Key] = kv.Value;
            else if (recRaw is string s)
                foreach (var pair in s.Split(';', StringSplitOptions.RemoveEmptyEntries))
                { var idx = pair.IndexOf('='); if (idx > 0) record[pair[..idx].Trim()] = pair[(idx + 1)..].Trim(); }

            record.TryGetValue(fieldName, out var oldRaw);
            double.TryParse(oldRaw?.ToString(), out double oldVal);
            double newVal = oldVal + step;

            // Optional clamping
            if (clampRaw != null)
            {
                var parts = clampRaw.ToString().Split(':');
                if (parts.Length == 2)
                {
                    if (double.TryParse(parts[0], out double min)) newVal = Math.Max(min, newVal);
                    if (double.TryParse(parts[1], out double max)) newVal = Math.Min(max, newVal);
                }
            }

            // Store as long when the value has no fractional part
            object stored = (newVal == Math.Floor(newVal) && !double.IsInfinity(newVal))
                          ? (object)(long)newVal : newVal;
            record[fieldName] = stored;

            output["Record"]   = record;
            output["OldValue"] = oldVal;
            output["NewValue"] = stored;
            return (output, stored);
        }
    }
}
