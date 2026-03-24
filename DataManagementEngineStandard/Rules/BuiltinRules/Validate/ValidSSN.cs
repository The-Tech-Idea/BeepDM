using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TheTechIdea.Beep.Rules.BuiltinRules.Validate
{
    /// <summary>
    /// Validates a US Social Security Number in XXX-XX-XXXX format.
    /// The pattern rejects all-zero groups, the 666 prefix, and
    /// the 900-999 range (ITIN prefixes).
    /// Parameters: <c>Value</c> — the SSN string to validate.
    /// Returns <c>true</c> when the SSN is structurally valid.
    /// </summary>
    [Rule(ruleKey: "Validate.ValidSSN", ParserKey = "RulesParser", RuleName = "ValidSSN")]
    public sealed class ValidSSN : IRule
    {
        // Rejects 000-XX-XXXX, 666-XX-XXXX, 9XX-XX-XXXX, XXX-00-XXXX, XXX-XX-0000
        private static readonly Regex _pattern = new(
            @"^(?!000|666|9\d\d)\d{3}-(?!00)\d{2}-(?!0000)\d{4}$",
            RegexOptions.Compiled);

        public string RuleText { get; set; } = "Validate.ValidSSN";
        public IRuleStructure Structure { get; set; } = new RuleStructure();

        public (Dictionary<string, object> outputs, object result) SolveRule(
            Dictionary<string, object> parameters = null)
        {
            var output = new Dictionary<string, object>();
            object raw = null;
            parameters?.TryGetValue("Value", out raw);
            var ssn   = raw?.ToString()?.Trim() ?? string.Empty;
            bool valid = _pattern.IsMatch(ssn);

            output["Result"] = valid;
            output["Value"]  = ssn;
            return (output, valid);
        }
    }
}
