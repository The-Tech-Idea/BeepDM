using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TheTechIdea.Beep.Rules.BuiltinRules.DQ
{
    /// <summary>
    /// Returns <c>true</c> when <c>Value</c> matches the regex <c>Pattern</c>.
    /// Parameters: <c>Value</c> (string), <c>Pattern</c> (string),
    /// optional <c>IgnoreCase</c> (bool string, default "false").
    /// </summary>
    [Rule(ruleKey: "DQ.MatchesRegex", ParserKey = "RulesParser", RuleName = "MatchesRegex")]
    public sealed class MatchesRegex : IRule
    {
        public string RuleText { get; set; } = "DQ.MatchesRegex";
        public IRuleStructure Structure { get; set; } = new RuleStructure();

        public (Dictionary<string, object> outputs, object result) SolveRule(
            Dictionary<string, object> parameters = null)
        {
            var output = new Dictionary<string, object>();
            if (parameters == null ||
                !parameters.TryGetValue("Value",   out var rawValue) ||
                !parameters.TryGetValue("Pattern", out var rawPattern))
            {
                output["Error"] = "Missing required parameters: Value, Pattern";
                return (output, false);
            }

            bool ignoreCase = false;
            if (parameters.TryGetValue("IgnoreCase", out var icRaw))
                bool.TryParse(icRaw?.ToString(), out ignoreCase);

            try
            {
                var opts    = ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
                bool matched = Regex.IsMatch(rawValue?.ToString() ?? string.Empty,
                                             rawPattern?.ToString() ?? string.Empty, opts);
                output["Result"] = matched;
                return (output, matched);
            }
            catch (Exception ex)
            {
                output["Error"] = $"Invalid regex pattern: {ex.Message}";
                return (output, false);
            }
        }
    }
}
