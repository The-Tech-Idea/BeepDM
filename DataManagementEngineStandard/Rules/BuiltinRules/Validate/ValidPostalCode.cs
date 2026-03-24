using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TheTechIdea.Beep.Rules.BuiltinRules.Validate
{
    /// <summary>
    /// Validates postal/ZIP codes.
    /// Parameters:
    ///   <c>Value</c>   — the postal code string.
    ///   <c>Country</c> — <c>US</c> | <c>CA</c> | <c>UK</c> | <c>DE</c> | <c>FR</c> | generic (default <c>US</c>).
    /// </summary>
    [Rule(ruleKey: "Validate.ValidPostalCode", ParserKey = "RulesParser", RuleName = "ValidPostalCode")]
    public sealed class ValidPostalCode : IRule
    {
        private static readonly Dictionary<string, Regex> _patterns = new(System.StringComparer.OrdinalIgnoreCase)
        {
            ["US"] = new Regex(@"^\d{5}(-\d{4})?$",                      RegexOptions.Compiled),
            ["CA"] = new Regex(@"^[A-Z]\d[A-Z]\s?\d[A-Z]\d$",            RegexOptions.Compiled | RegexOptions.IgnoreCase),
            ["UK"] = new Regex(@"^[A-Z]{1,2}\d[A-Z\d]?\s?\d[A-Z]{2}$",  RegexOptions.Compiled | RegexOptions.IgnoreCase),
            ["DE"] = new Regex(@"^\d{5}$",                                RegexOptions.Compiled),
            ["FR"] = new Regex(@"^\d{5}$",                                RegexOptions.Compiled),
        };
        private static readonly Regex _generic = new(@"^[A-Z0-9\s\-]{3,10}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public string RuleText { get; set; } = "Validate.ValidPostalCode";
        public IRuleStructure Structure { get; set; } = new RuleStructure();

        public (Dictionary<string, object> outputs, object result) SolveRule(
            Dictionary<string, object> parameters = null)
        {
            var output = new Dictionary<string, object>();
            object valRaw = null;
            object cntRaw = null;
            parameters?.TryGetValue("Value",   out valRaw);
            parameters?.TryGetValue("Country", out cntRaw);

            var code    = valRaw?.ToString()?.Trim() ?? string.Empty;
            var country = cntRaw?.ToString()?.Trim().ToUpperInvariant() ?? "US";
            var regex   = _patterns.TryGetValue(country, out var r) ? r : _generic;
            bool valid  = regex.IsMatch(code);

            output["Result"]  = valid;
            output["Value"]   = code;
            output["Country"] = country;
            return (output, valid);
        }
    }
}
