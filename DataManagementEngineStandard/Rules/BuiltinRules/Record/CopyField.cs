using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Rules.BuiltinRules.Record
{
    /// <summary>
    /// Copies the value of <c>SourceField</c> to <c>TargetField</c> within a record.
    /// Parameters:
    ///   <c>Record</c>      — <see cref="IDictionary{TKey,TValue}"/> or <c>"field=val;…"</c> string.
    ///   <c>SourceField</c> — field name to read from.
    ///   <c>TargetField</c> — field name to write to.
    /// Outputs: <c>Record</c> (updated copy), <c>CopiedValue</c>, <c>TargetField</c>.
    /// </summary>
    [Rule(ruleKey: "Record.CopyField", ParserKey = "RulesParser", RuleName = "CopyField")]
    public sealed class CopyField : IRule
    {
        public string RuleText { get; set; } = "Record.CopyField";
        public IRuleStructure Structure { get; set; } = new RuleStructure();

        public (Dictionary<string, object> outputs, object result) SolveRule(
            Dictionary<string, object> parameters = null)
        {
            var output = new Dictionary<string, object>();
            if (parameters == null) { output["Error"] = "No parameters."; return (output, false); }

            parameters.TryGetValue("SourceField", out var sfRaw);
            parameters.TryGetValue("TargetField", out var tfRaw);
            parameters.TryGetValue("Record",      out var recRaw);

            var src = sfRaw?.ToString() ?? string.Empty;
            var tgt = tfRaw?.ToString() ?? string.Empty;

            var record = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (recRaw is IDictionary<string, object> d)
                foreach (var kv in d) record[kv.Key] = kv.Value;
            else if (recRaw is string s)
                foreach (var pair in s.Split(';', StringSplitOptions.RemoveEmptyEntries))
                { var idx = pair.IndexOf('='); if (idx > 0) record[pair[..idx].Trim()] = pair[(idx + 1)..].Trim(); }

            record.TryGetValue(src, out var val);
            record[tgt] = val;

            output["Record"]      = record;
            output["CopiedValue"] = val;
            output["TargetField"] = tgt;
            return (output, val);
        }
    }
}
