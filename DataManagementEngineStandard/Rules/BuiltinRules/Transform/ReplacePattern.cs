using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TheTechIdea.Beep.Rules.BuiltinRules.Transform
{
    /// <summary>
    /// Replaces all occurrences of <c>Pattern</c> in <c>Value</c> with <c>Replacement</c>.
    /// Parameters: <c>Value</c> (string), <c>Pattern</c> (regex string), <c>Replacement</c> (string).
    /// Optional <c>IgnoreCase</c> (bool string, default "false").
    /// </summary>
    [Rule(ruleKey: "Transform.ReplacePattern", ParserKey = "RulesParser", RuleName = "ReplacePattern")]
    public sealed class ReplacePattern : IRule
    {
        public string RuleText { get; set; } = "Transform.ReplacePattern";
        public IRuleStructure Structure { get; set; } = new RuleStructure();

        public (Dictionary<string, object> outputs, object result) SolveRule(
            Dictionary<string, object> parameters = null)
        {
            var output = new Dictionary<string, object>();
            if (parameters == null ||
                !parameters.TryGetValue("Value",       out var rawValue)   ||
                !parameters.TryGetValue("Pattern",     out var rawPattern) ||
                !parameters.TryGetValue("Replacement", out var rawReplace))
            {
                output["Error"] = "Missing required parameters: Value, Pattern, Replacement";
                return (output, null);
            }

            bool ignoreCase = false;
            if (parameters.TryGetValue("IgnoreCase", out var icRaw))
                bool.TryParse(icRaw?.ToString(), out ignoreCase);

            try
            {
                var opts   = ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
                string res = Regex.Replace(rawValue?.ToString() ?? string.Empty,
                                           rawPattern?.ToString() ?? string.Empty,
                                           rawReplace?.ToString() ?? string.Empty, opts);
                output["Result"] = res;
                return (output, res);
            }
            catch (Exception ex)
            {
                output["Error"] = $"Regex error: {ex.Message}";
                return (output, null);
            }
        }
    }
}
