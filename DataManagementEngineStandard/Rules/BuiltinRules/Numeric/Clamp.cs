using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Rules.BuiltinRules.Numeric
{
    /// <summary>
    /// Clamps <c>Value</c> between <c>Min</c> and <c>Max</c>.
    /// Parameters: <c>Value</c> (numeric), <c>Min</c> (numeric), <c>Max</c> (numeric).
    /// Returns the clamped double value.
    /// </summary>
    [Rule(ruleKey: "Numeric.Clamp", ParserKey = "RulesParser", RuleName = "Clamp")]
    public sealed class Clamp : IRule
    {
        public string RuleText { get; set; } = "Numeric.Clamp";
        public IRuleStructure Structure { get; set; } = new RuleStructure();

        public (Dictionary<string, object> outputs, object result) SolveRule(
            Dictionary<string, object> parameters = null)
        {
            var output = new Dictionary<string, object>();
            if (parameters == null ||
                !parameters.TryGetValue("Value", out var rawValue) ||
                !parameters.TryGetValue("Min",   out var rawMin)   ||
                !parameters.TryGetValue("Max",   out var rawMax))
            {
                output["Error"] = "Missing required parameters: Value, Min, Max";
                return (output, null);
            }

            var ic = System.Globalization.CultureInfo.InvariantCulture;
            var ns = System.Globalization.NumberStyles.Any;
            if (!double.TryParse(rawValue?.ToString(), ns, ic, out double val) ||
                !double.TryParse(rawMin?.ToString(),   ns, ic, out double min) ||
                !double.TryParse(rawMax?.ToString(),   ns, ic, out double max))
            {
                output["Error"] = "Value, Min, and Max must all be valid numerics";
                return (output, null);
            }

            if (min > max)
            {
                output["Error"] = "Min must not be greater than Max";
                return (output, null);
            }

            double res = Math.Max(min, Math.Min(max, val));
            output["Result"]  = res;
            output["Clamped"] = res != val;
            return (output, res);
        }
    }
}
