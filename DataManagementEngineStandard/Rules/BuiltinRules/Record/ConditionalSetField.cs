using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Rules.BuiltinRules.Record
{
    /// <summary>
    /// Sets <c>TargetField</c> to <c>TrueValue</c> when a field condition is met,
    /// or to <c>FalseValue</c> when it is not (if supplied).
    /// Parameters:
    ///   <c>Record</c>         — <see cref="IDictionary{TKey,TValue}"/> or <c>"field=val;…"</c> string.
    ///   <c>ConditionField</c> — field name to evaluate.
    ///   <c>Operator</c>       — =, !=, &gt;, &lt;, &gt;=, &lt;=, contains, startswith, endswith,
    ///                           isnull, isnotnull (default =).
    ///   <c>CompareValue</c>   — value to compare against (not used by isnull/isnotnull).
    ///   <c>TargetField</c>    — field name to set.
    ///   <c>TrueValue</c>      — value written when condition is true.
    ///   <c>FalseValue</c>     — value written when condition is false (optional; field left unchanged when absent).
    /// Outputs: <c>Record</c> (updated copy), <c>Applied</c> (bool), <c>TargetField</c>, <c>NewValue</c>.
    /// </summary>
    [Rule(ruleKey: "Record.ConditionalSetField", ParserKey = "RulesParser", RuleName = "ConditionalSetField")]
    public sealed class ConditionalSetField : IRule
    {
        public string RuleText { get; set; } = "Record.ConditionalSetField";
        public IRuleStructure Structure { get; set; } = new RuleStructure();

        public (Dictionary<string, object> outputs, object result) SolveRule(
            Dictionary<string, object> parameters = null)
        {
            var output = new Dictionary<string, object>();
            if (parameters == null) { output["Error"] = "No parameters."; return (output, false); }

            parameters.TryGetValue("ConditionField", out var cfRaw);
            parameters.TryGetValue("Operator",       out var opRaw);
            parameters.TryGetValue("CompareValue",   out var cvRaw);
            parameters.TryGetValue("TargetField",    out var tfRaw);
            parameters.TryGetValue("TrueValue",      out var tvRaw);
            parameters.TryGetValue("FalseValue",     out var fvRaw);
            parameters.TryGetValue("Record",         out var recRaw);

            var condField   = cfRaw?.ToString() ?? string.Empty;
            var op          = opRaw?.ToString()?.ToLowerInvariant() ?? "=";
            var compareVal  = cvRaw?.ToString() ?? string.Empty;
            var targetField = tfRaw?.ToString() ?? string.Empty;

            var record = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (recRaw is IDictionary<string, object> d)
                foreach (var kv in d) record[kv.Key] = kv.Value;
            else if (recRaw is string s)
                foreach (var pair in s.Split(';', StringSplitOptions.RemoveEmptyEntries))
                { var idx = pair.IndexOf('='); if (idx > 0) record[pair[..idx].Trim()] = pair[(idx + 1)..].Trim(); }

            record.TryGetValue(condField, out var fieldVal);
            var fieldStr = fieldVal?.ToString() ?? string.Empty;

            bool condition = op switch
            {
                "isnull"    => fieldVal is null || fieldVal is DBNull,
                "isnotnull" => fieldVal is not null && fieldVal is not DBNull,
                _           => Evaluate(fieldStr, op, compareVal)
            };

            bool applied  = false;
            object newVal = null;
            if (condition)
            { record[targetField] = tvRaw; newVal = tvRaw; applied = true; }
            else if (fvRaw != null)
            { record[targetField] = fvRaw; newVal = fvRaw; applied = true; }

            output["Record"]      = record;
            output["Applied"]     = applied;
            output["TargetField"] = targetField;
            output["NewValue"]    = newVal;
            return (output, applied);
        }

        private static bool Evaluate(string left, string op, string right)
        {
            bool strEq = string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
            if (double.TryParse(left, out double l) && double.TryParse(right, out double r))
                return op switch
                {
                    "=" or "==" => l == r, "!=" => l != r,
                    ">"  => l > r,  "<"  => l < r,
                    ">=" => l >= r, "<=" => l <= r,
                    _    => strEq
                };
            return op switch
            {
                "=" or "=="  => strEq,
                "!="         => !strEq,
                "contains"   => left.Contains(right,   StringComparison.OrdinalIgnoreCase),
                "startswith" => left.StartsWith(right, StringComparison.OrdinalIgnoreCase),
                "endswith"   => left.EndsWith(right,   StringComparison.OrdinalIgnoreCase),
                _            => strEq
            };
        }
    }
}
