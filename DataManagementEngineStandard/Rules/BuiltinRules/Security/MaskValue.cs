using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Rules.BuiltinRules.Security
{
    /// <summary>
    /// Masks <c>Value</c> to hide sensitive characters.
    /// Parameters: <c>Value</c> (string).
    /// Optional <c>Mode</c> ("last4"|"first4"|"middle"|"all", default "last4"),
    /// <c>MaskChar</c> (single char, default '*').
    /// Examples: "1234567890" → "last4" = "******7890" / "first4" = "1234******" / "middle" = "12****90".
    /// </summary>
    [Rule(ruleKey: "Security.MaskValue", ParserKey = "RulesParser", RuleName = "MaskValue")]
    public sealed class MaskValue : IRule
    {
        public string RuleText { get; set; } = "Security.MaskValue";
        public IRuleStructure Structure { get; set; } = new RuleStructure();

        public (Dictionary<string, object> outputs, object result) SolveRule(
            Dictionary<string, object> parameters = null)
        {
            var output = new Dictionary<string, object>();
            if (parameters == null || !parameters.TryGetValue("Value", out var rawValue))
            {
                output["Error"] = "Missing required parameter: Value";
                return (output, null);
            }

            string src = rawValue?.ToString() ?? string.Empty;

            char maskChar = '*';
            if (parameters.TryGetValue("MaskChar", out var mcRaw) &&
                mcRaw?.ToString()?.Length > 0)
                maskChar = mcRaw.ToString()![0];

            string mode = "last4";
            if (parameters.TryGetValue("Mode", out var modeRaw))
                mode = modeRaw?.ToString()?.ToLowerInvariant() ?? "last4";

            if (src.Length == 0)
            {
                output["Result"] = src;
                return (output, src);
            }

            string res = mode switch
            {
                "last4"  => MaskExcept(src, src.Length - Math.Min(4, src.Length), maskChar, false),
                "first4" => MaskExcept(src, Math.Min(4, src.Length), maskChar, true),
                "middle" => MaskMiddle(src, maskChar),
                "all"    => new string(maskChar, src.Length),
                _        => MaskExcept(src, src.Length - Math.Min(4, src.Length), maskChar, false)
            };

            output["Result"] = res;
            return (output, res);
        }

        // Keep `keepCount` characters at the start (keepFromStart=true) or end (false), mask the rest.
        private static string MaskExcept(string src, int keepCount, char mask, bool keepFromStart)
        {
            if (keepFromStart)
                return src[..keepCount] + new string(mask, src.Length - keepCount);
            int maskLen = src.Length - keepCount;
            return new string(mask, maskLen) + src[maskLen..];
        }

        // Keep first 2 and last 2 characters; mask everything in between.
        private static string MaskMiddle(string src, char mask)
        {
            if (src.Length <= 4) return new string(mask, src.Length);
            int keep = 2;
            return src[..keep] + new string(mask, src.Length - 2 * keep) + src[^keep..];
        }
    }
}
