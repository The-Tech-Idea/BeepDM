using System;
using System.Collections.Generic;
using System.Linq;

namespace TheTechIdea.Beep.Rules.BuiltinRules.DQ
{
    /// <summary>
    /// Returns <c>true</c> when <c>Value</c> is contained in <c>AllowedValues</c>.
    /// Parameters: <c>Value</c> (object), <c>AllowedValues</c> (comma-separated string
    /// OR <c>IEnumerable&lt;object&gt;</c>).
    /// Optional <c>IgnoreCase</c> (bool string, default "false") — applies to strings.
    /// </summary>
    [Rule(ruleKey: "DQ.IsInList", ParserKey = "RulesParser", RuleName = "IsInList")]
    public sealed class IsInList : IRule
    {
        public string RuleText { get; set; } = "DQ.IsInList";
        public IRuleStructure Structure { get; set; } = new RuleStructure();

        public (Dictionary<string, object> outputs, object result) SolveRule(
            Dictionary<string, object> parameters = null)
        {
            var output = new Dictionary<string, object>();
            if (parameters == null ||
                !parameters.TryGetValue("Value",         out var rawValue) ||
                !parameters.TryGetValue("AllowedValues", out var rawList))
            {
                output["Error"] = "Missing required parameters: Value, AllowedValues";
                return (output, false);
            }

            bool ignoreCase = false;
            if (parameters.TryGetValue("IgnoreCase", out var icRaw))
                bool.TryParse(icRaw?.ToString(), out ignoreCase);

            IEnumerable<string> allowed;
            if (rawList is IEnumerable<object> collection)
                allowed = collection.Select(x => x?.ToString() ?? string.Empty);
            else
                allowed = (rawList?.ToString() ?? string.Empty).Split(',')
                           .Select(v => v.Trim());

            var comparison = ignoreCase
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

            bool found = allowed.Any(v =>
                string.Equals(v, rawValue?.ToString() ?? string.Empty, comparison));

            output["Result"] = found;
            return (output, found);
        }
    }
}
