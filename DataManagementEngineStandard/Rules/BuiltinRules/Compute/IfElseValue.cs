using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Rules.BuiltinRules.Compute
{
    /// <summary>
    /// Returns <c>TrueValue</c> or <c>FalseValue</c> based on a single-field comparison.
    /// Parameters:
    ///   <c>ConditionField</c> — field name to look up in <c>Record</c> (or directly in parameters).
    ///   <c>Operator</c>       — =, !=, &gt;, &lt;, &gt;=, &lt;=, contains, startswith, endswith (default =).
    ///   <c>CompareValue</c>   — value to compare against.
    ///   <c>TrueValue</c>      — value returned when condition holds.
    ///   <c>FalseValue</c>     — value returned when condition does not hold (default empty string).
    ///   <c>Record</c>         — <see cref="IDictionary{TKey,TValue}"/> or <c>"field=val;…"</c> string.
    /// Outputs: <c>Result</c> (chosen value), <c>Condition</c> (bool).
    /// </summary>
    [Rule(ruleKey: "Compute.IfElseValue", ParserKey = "RulesParser", RuleName = "IfElseValue")]
    public sealed class IfElseValue : IRule
    {
        public string RuleText { get; set; } = "Compute.IfElseValue";
        public IRuleStructure Structure { get; set; } = new RuleStructure();

        public (Dictionary<string, object> outputs, object result) SolveRule(
            Dictionary<string, object> parameters = null)
        {
            var output = new Dictionary<string, object>();
            if (parameters == null) { output["Error"] = "No parameters."; return (output, null); }

            parameters.TryGetValue("ConditionField", out var cfRaw);
            parameters.TryGetValue("Operator",       out var opRaw);
            parameters.TryGetValue("CompareValue",   out var cvRaw);
            parameters.TryGetValue("TrueValue",      out var tvRaw);
            parameters.TryGetValue("FalseValue",     out var fvRaw);

            var fieldName    = cfRaw?.ToString() ?? string.Empty;
            var op           = opRaw?.ToString()?.ToLowerInvariant() ?? "=";
            var compareValue = cvRaw?.ToString() ?? string.Empty;

            // Resolve the field value from Record or direct parameter
            string fieldValue = string.Empty;
            if (parameters.TryGetValue("Record", out var recRaw))
            {
                if (recRaw is IDictionary<string, object> d && d.TryGetValue(fieldName, out var fv))
                    fieldValue = fv?.ToString() ?? string.Empty;
                else if (recRaw is string s)
                    foreach (var pair in s.Split(';', StringSplitOptions.RemoveEmptyEntries))
                    {
                        var idx = pair.IndexOf('=');
                        if (idx > 0 && string.Equals(pair[..idx].Trim(), fieldName, StringComparison.OrdinalIgnoreCase))
                        { fieldValue = pair[(idx + 1)..].Trim(); break; }
                    }
            }
            else if (parameters.TryGetValue(fieldName, out var fvDirect))
                fieldValue = fvDirect?.ToString() ?? string.Empty;

            bool condition  = Evaluate(fieldValue, op, compareValue);
            object chosen   = condition ? tvRaw : (fvRaw ?? string.Empty);

            output["Result"]    = chosen;
            output["Condition"] = condition;
            return (output, chosen);
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
                "contains"   => left.Contains(right,      StringComparison.OrdinalIgnoreCase),
                "startswith" => left.StartsWith(right,    StringComparison.OrdinalIgnoreCase),
                "endswith"   => left.EndsWith(right,      StringComparison.OrdinalIgnoreCase),
                _            => strEq
            };
        }
    }
}
