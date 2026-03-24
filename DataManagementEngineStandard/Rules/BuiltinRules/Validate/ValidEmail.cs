using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Rules.BuiltinRules.Validate
{
    /// <summary>
    /// Validates an e-mail address using <see cref="System.Net.Mail.MailAddress"/>,
    /// which covers RFC 5321/5322 syntax checking.
    /// Parameters: <c>Value</c> — the address to validate.
    /// Returns <c>true</c> when the address is well-formed.
    /// </summary>
    [Rule(ruleKey: "Validate.ValidEmail", ParserKey = "RulesParser", RuleName = "ValidEmail")]
    public sealed class ValidEmail : IRule
    {
        public string RuleText { get; set; } = "Validate.ValidEmail";
        public IRuleStructure Structure { get; set; } = new RuleStructure();

        public (Dictionary<string, object> outputs, object result) SolveRule(
            Dictionary<string, object> parameters = null)
        {
            var output = new Dictionary<string, object>();
            object raw = null;
            parameters?.TryGetValue("Value", out raw);
            var email = raw?.ToString()?.Trim() ?? string.Empty;

            bool valid = false;
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                valid = string.Equals(addr.Address, email, StringComparison.OrdinalIgnoreCase);
            }
            catch { }

            output["Result"] = valid;
            output["Value"]  = email;
            return (output, valid);
        }
    }
}
