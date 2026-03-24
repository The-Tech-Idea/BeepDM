using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Rules.BuiltinRules.DQ
{
    /// <summary>
    /// Returns <c>true</c> when <c>Value</c> lies between <c>Min</c> and <c>Max</c>.
    /// Parameters: <c>Value</c> (IComparable), <c>Min</c>, <c>Max</c>,
    /// optional <c>Inclusive</c> (bool string "true"/"false", default true).
    /// </summary>
    [Rule(ruleKey: "DQ.IsInRange", ParserKey = "RulesParser", RuleName = "IsInRange")]
    public sealed class IsInRange : IRule
    {
        public string RuleText { get; set; } = "DQ.IsInRange";
        public IRuleStructure Structure { get; set; } = new RuleStructure();

        public (Dictionary<string, object> outputs, object result) SolveRule(
            Dictionary<string, object> parameters = null)
        {
            var output = new Dictionary<string, object>();
            if (parameters == null)
            {
                output["Error"] = "Parameters cannot be null";
                return (output, false);
            }

            if (!parameters.TryGetValue("Value", out var rawValue) ||
                !parameters.TryGetValue("Min",   out var rawMin)   ||
                !parameters.TryGetValue("Max",   out var rawMax))
            {
                output["Error"] = "Missing required parameters: Value, Min, Max";
                return (output, false);
            }

            bool inclusive = true;
            if (parameters.TryGetValue("Inclusive", out var inclRaw))
                bool.TryParse(inclRaw?.ToString(), out inclusive);

            try
            {
                double value = Convert.ToDouble(rawValue);
                double min   = Convert.ToDouble(rawMin);
                double max   = Convert.ToDouble(rawMax);

                bool inRange = inclusive
                    ? value >= min && value <= max
                    : value >  min && value <  max;

                output["Result"] = inRange;
                return (output, inRange);
            }
            catch (Exception ex)
            {
                output["Error"] = $"Type conversion failed: {ex.Message}";
                return (output, false);
            }
        }
    }
}
