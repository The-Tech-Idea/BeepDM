using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Rules.BuiltinRules.DQ
{
    /// <summary>
    /// Returns <c>true</c> when the input value is null, DBNull, empty string, or whitespace.
    /// Parameters: <c>Value</c> (object).
    /// </summary>
    [Rule(ruleKey: "DQ.IsNullOrEmpty", ParserKey = "RulesParser", RuleName = "IsNullOrEmpty")]
    public sealed class IsNullOrEmpty : IRule
    {
        public string RuleText { get; set; } = "DQ.IsNullOrEmpty";
        public IRuleStructure Structure { get; set; } = new RuleStructure();

        public (Dictionary<string, object> outputs, object result) SolveRule(
            Dictionary<string, object> parameters = null)
        {
            var output = new Dictionary<string, object>();
            if (parameters == null || !parameters.TryGetValue("Value", out var raw))
            {
                output["Error"] = "Missing required parameter: Value";
                return (output, false);
            }

            bool isEmpty = raw == null
                || raw is DBNull
                || (raw is string s && string.IsNullOrWhiteSpace(s));

            output["Result"] = isEmpty;
            return (output, isEmpty);
        }
    }
}
