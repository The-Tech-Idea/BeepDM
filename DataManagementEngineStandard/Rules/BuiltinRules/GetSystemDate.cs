using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Rules.BuiltinRules
{
    [Rule(ruleKey: "GetSystemDate", ParserKey = "RulesParser", RuleName = "GetSystemDate")]
    public class GetSystemDate : IRule
    {
        public string RuleText { get; set; } = "GetSystemDate";
        public IRuleStructure Structure { get; set; } = new RuleStructure();

        public GetSystemDate() { }
        public GetSystemDate(string ruleText) { RuleText = ruleText; }

        public (Dictionary<string, object> outputs, object result) SolveRule(
            Dictionary<string, object> parameters = null)
        {
            return (parameters ?? new Dictionary<string, object>(), DateTime.UtcNow);
        }
    }
}

