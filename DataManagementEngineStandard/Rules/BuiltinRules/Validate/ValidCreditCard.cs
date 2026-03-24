using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace TheTechIdea.Beep.Rules.BuiltinRules.Validate
{
    /// <summary>
    /// Validates a credit card number using the Luhn algorithm and detects card type.
    /// Accepts numbers with or without spaces / dashes.
    /// Parameters: <c>Value</c> — card number string.
    /// Outputs: <c>Result</c> (bool), <c>CardType</c> (Visa|Mastercard|Amex|Discover|Unknown).
    /// </summary>
    [Rule(ruleKey: "Validate.ValidCreditCard", ParserKey = "RulesParser", RuleName = "ValidCreditCard")]
    public sealed class ValidCreditCard : IRule
    {
        private static readonly Regex _strip = new(@"[\s\-]", RegexOptions.Compiled);

        public string RuleText { get; set; } = "Validate.ValidCreditCard";
        public IRuleStructure Structure { get; set; } = new RuleStructure();

        public (Dictionary<string, object> outputs, object result) SolveRule(
            Dictionary<string, object> parameters = null)
        {
            var output = new Dictionary<string, object>();
            object raw = null;
            parameters?.TryGetValue("Value", out raw);
            var number   = _strip.Replace(raw?.ToString() ?? string.Empty, "");
            bool valid   = LuhnCheck(number);
            string ctype = number.Length >= 2 ? DetectType(number) : "Unknown";

            output["Result"]   = valid;
            output["CardType"] = ctype;
            return (output, valid);
        }

        private static bool LuhnCheck(string number)
        {
            var digits = number.Where(char.IsDigit).Select(c => c - '0').ToArray();
            if (digits.Length < 13) return false;
            int sum = 0; bool doubleIt = false;
            for (int i = digits.Length - 1; i >= 0; i--)
            {
                int d = digits[i];
                if (doubleIt) { d *= 2; if (d > 9) d -= 9; }
                sum += d; doubleIt = !doubleIt;
            }
            return sum % 10 == 0;
        }

        private static string DetectType(string n) => n[0] switch
        {
            '4'                                                          => "Visa",
            '5' when n.Length >= 2 && n[1] >= '1' && n[1] <= '5'       => "Mastercard",
            '3' when n.Length >= 2 && (n[1] == '4' || n[1] == '7')     => "Amex",
            '6' when n.StartsWith("6011")                               => "Discover",
            _                                                            => "Unknown"
        };
    }
}
