using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace TheTechIdea.Beep.Rules.BuiltinRules.Validate
{
    /// <summary>
    /// Validates a phone number.
    /// Parameters:
    ///   <c>Value</c>  — phone string to validate.
    ///   <c>Format</c> — <c>E164</c> | <c>US</c> | <c>Any</c> (default Any).
    /// Returns <c>true</c> when the number matches the requested format.
    /// </summary>
    [Rule(ruleKey: "Validate.ValidPhone", ParserKey = "RulesParser", RuleName = "ValidPhone")]
    public sealed class ValidPhone : IRule
    {
        private static readonly Regex _e164   = new(@"^\+[1-9]\d{6,14}$",                                                    RegexOptions.Compiled);
        private static readonly Regex _us     = new(@"^(\+1[-.\s]?)?\(?\d{3}\)?[-.\s]?\d{3}[-.\s]?\d{4}$",                  RegexOptions.Compiled);
        private static readonly Regex _digits = new(@"\d",                                                                    RegexOptions.Compiled);

        public string RuleText { get; set; } = "Validate.ValidPhone";
        public IRuleStructure Structure { get; set; } = new RuleStructure();

        public (Dictionary<string, object> outputs, object result) SolveRule(
            Dictionary<string, object> parameters = null)
        {
            var output = new Dictionary<string, object>();
            object valRaw = null;
            object fmtRaw = null;
            parameters?.TryGetValue("Value",  out valRaw);
            parameters?.TryGetValue("Format", out fmtRaw);

            var phone  = valRaw?.ToString()?.Trim() ?? string.Empty;
            var format = fmtRaw?.ToString()?.Trim().ToUpperInvariant() ?? "ANY";

            bool valid = format switch
            {
                "E164" => _e164.IsMatch(phone),
                "US"   => _us.IsMatch(phone),
                _      => _e164.IsMatch(phone)
                       || _us.IsMatch(phone)
                       || (_digits.Matches(phone).Cast<Match>().Select(m => m.Value)
                               is var ds && string.Concat(ds).Length is >= 7 and <= 15)
            };

            output["Result"] = valid;
            output["Value"]  = phone;
            return (output, valid);
        }
    }
}
