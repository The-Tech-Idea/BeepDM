using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;

namespace TheTechIdea.Beep.Rules.BuiltinRules.Compute
{
    /// <summary>
    /// Evaluates a mathematical or string expression with <c>{FieldName}</c> token substitution.
    /// Uses <see cref="DataTable.Compute"/> for safe, eval-free arithmetic.
    /// Parameters:
    ///   <c>Expression</c> — e.g. <c>{Price} * {Qty} * (1 - {Discount})</c>.
    ///   <c>Record</c>     — <c>IDictionary&lt;string,object&gt;</c> or <c>"field=val;…"</c> string.
    ///   Individual field values may also be passed directly as keyed parameters.
    /// Outputs: <c>Result</c> (computed value), <c>Expression</c> (token-expanded expression string).
    /// </summary>
    [Rule(ruleKey: "Compute.EvalExpression", ParserKey = "RulesParser", RuleName = "EvalExpression")]
    public sealed class EvalExpression : IRule
    {
        private static readonly Regex _token = new(@"\{(\w+)\}", RegexOptions.Compiled);

        public string RuleText { get; set; } = "Compute.EvalExpression";
        public IRuleStructure Structure { get; set; } = new RuleStructure();

        public (Dictionary<string, object> outputs, object result) SolveRule(
            Dictionary<string, object> parameters = null)
        {
            var output = new Dictionary<string, object>();
            if (parameters == null || !parameters.TryGetValue("Expression", out var exprRaw))
            {
                output["Error"] = "Missing parameter: Expression";
                return (output, null);
            }

            // Build flat key→value lookup from Record + individual params
            var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (parameters.TryGetValue("Record", out var recRaw))
            {
                if (recRaw is IDictionary<string, object> d)
                    foreach (var kv in d) values[kv.Key] = kv.Value;
                else if (recRaw is string s)
                    foreach (var pair in s.Split(';', StringSplitOptions.RemoveEmptyEntries))
                    {
                        var idx = pair.IndexOf('=');
                        if (idx > 0) values[pair[..idx].Trim()] = pair[(idx + 1)..].Trim();
                    }
            }
            foreach (var kv in parameters)
                if (kv.Key != "Expression" && kv.Key != "Record")
                    values.TryAdd(kv.Key, kv.Value);

            // Substitute {FieldName} tokens
            string expr = _token.Replace(exprRaw.ToString()!, m =>
            {
                var key = m.Groups[1].Value;
                return values.TryGetValue(key, out var v) ? (v?.ToString() ?? "0") : "0";
            });

            try
            {
                var computed = new DataTable().Compute(expr, null);
                output["Result"]     = computed;
                output["Expression"] = expr;
                return (output, computed);
            }
            catch (Exception ex)
            {
                output["Error"]      = ex.Message;
                output["Expression"] = expr;
                return (output, null);
            }
        }
    }
}
