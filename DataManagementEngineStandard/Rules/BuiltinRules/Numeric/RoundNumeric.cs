using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Rules.BuiltinRules.Numeric
{
    /// <summary>
    /// Rounds <c>Value</c> to <c>Decimals</c> decimal places.
    /// Parameters: <c>Value</c> (numeric string), <c>Decimals</c> (int string, default "2").
    /// Optional <c>MidpointRounding</c> ("AwayFromZero"|"ToEven", default "AwayFromZero").
    /// </summary>
    [Rule(ruleKey: "Numeric.RoundNumeric", ParserKey = "RulesParser", RuleName = "RoundNumeric")]
    public sealed class RoundNumeric : IRule
    {
        public string RuleText { get; set; } = "Numeric.RoundNumeric";
        public IRuleStructure Structure { get; set; } = new RuleStructure();

        public (Dictionary<string, object> outputs, object result) SolveRule(
            Dictionary<string, object> parameters = null)
        {
            var output = new Dictionary<string, object>();
            if (parameters == null || !parameters.TryGetValue("Value", out var rawValue))
            {
                output["Error"] = "Missing required parameter: Value";
                return (output, null);
            }

            if (!double.TryParse(rawValue?.ToString(),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out double value))
            {
                output["Error"] = "Value must be a valid numeric";
                return (output, null);
            }

            int decimals = 2;
            if (parameters.TryGetValue("Decimals", out var decRaw))
                int.TryParse(decRaw?.ToString(), out decimals);

            var midpoint = MidpointRounding.AwayFromZero;
            if (parameters.TryGetValue("MidpointRounding", out var mpRaw) &&
                string.Equals(mpRaw?.ToString(), "ToEven", StringComparison.OrdinalIgnoreCase))
                midpoint = MidpointRounding.ToEven;

            double res = Math.Round(value, decimals, midpoint);
            output["Result"] = res;
            return (output, res);
        }
    }
}
