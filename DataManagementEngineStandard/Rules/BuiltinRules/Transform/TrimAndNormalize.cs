using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Rules.BuiltinRules.Transform
{
    /// <summary>
    /// Trims whitespace from <c>Value</c> and optionally changes casing.
    /// Parameters: <c>Value</c> (string), optional <c>CaseMode</c> ("upper"|"lower"|"title"|"none", default "none").
    /// </summary>
    [Rule(ruleKey: "Transform.TrimAndNormalize", ParserKey = "RulesParser", RuleName = "TrimAndNormalize")]
    public sealed class TrimAndNormalize : IRule
    {
        public string RuleText { get; set; } = "Transform.TrimAndNormalize";
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

            string value = rawValue?.ToString()?.Trim() ?? string.Empty;

            if (parameters.TryGetValue("CaseMode", out var modeRaw))
            {
                value = (modeRaw?.ToString()?.ToLowerInvariant()) switch
                {
                    "upper" => value.ToUpperInvariant(),
                    "lower" => value.ToLowerInvariant(),
                    "title" => System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value.ToLowerInvariant()),
                    _       => value
                };
            }

            output["Result"] = value;
            return (output, value);
        }
    }
}
