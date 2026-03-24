using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Rules.BuiltinRules.Validate
{
    /// <summary>
    /// Validates an absolute URL using <see cref="Uri.TryCreate"/>.
    /// Parameters:
    ///   <c>Value</c>          — URL string to validate.
    ///   <c>AllowedSchemes</c> — comma-separated list of allowed URI schemes
    ///                          (default <c>http,https</c>).
    /// Outputs: <c>Result</c> (bool), <c>Host</c> (when valid).
    /// </summary>
    [Rule(ruleKey: "Validate.ValidUrl", ParserKey = "RulesParser", RuleName = "ValidUrl")]
    public sealed class ValidUrl : IRule
    {
        public string RuleText { get; set; } = "Validate.ValidUrl";
        public IRuleStructure Structure { get; set; } = new RuleStructure();

        public (Dictionary<string, object> outputs, object result) SolveRule(
            Dictionary<string, object> parameters = null)
        {
            var output = new Dictionary<string, object>();
            object valRaw = null;
            object schemesRaw = null;
            parameters?.TryGetValue("Value",          out valRaw);
            parameters?.TryGetValue("AllowedSchemes", out schemesRaw);

            var url     = valRaw?.ToString()?.Trim() ?? string.Empty;
            var schemes = (schemesRaw?.ToString() ?? "http,https")
                              .Split(',', StringSplitOptions.RemoveEmptyEntries);

            Uri uri = null;
            bool valid = Uri.TryCreate(url, UriKind.Absolute, out uri)
                      && Array.Exists(schemes, s => string.Equals(uri.Scheme, s.Trim(), StringComparison.OrdinalIgnoreCase));

            output["Result"] = valid;
            output["Value"]  = url;
            if (valid) output["Host"] = uri!.Host;
            return (output, valid);
        }
    }
}
