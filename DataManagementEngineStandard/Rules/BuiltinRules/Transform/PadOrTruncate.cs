using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Rules.BuiltinRules.Transform
{
    /// <summary>
    /// Pads or truncates <c>Value</c> to exactly <c>Length</c> characters.
    /// Parameters: <c>Value</c> (string), <c>Length</c> (int string).
    /// Optional <c>Align</c> ("left"|"right", default "right"), <c>PadChar</c> (single char string, default " ").
    /// If the value exceeds <c>Length</c> it is truncated from the right.
    /// </summary>
    [Rule(ruleKey: "Transform.PadOrTruncate", ParserKey = "RulesParser", RuleName = "PadOrTruncate")]
    public sealed class PadOrTruncate : IRule
    {
        public string RuleText { get; set; } = "Transform.PadOrTruncate";
        public IRuleStructure Structure { get; set; } = new RuleStructure();

        public (Dictionary<string, object> outputs, object result) SolveRule(
            Dictionary<string, object> parameters = null)
        {
            var output = new Dictionary<string, object>();
            if (parameters == null ||
                !parameters.TryGetValue("Value",  out var rawValue)  ||
                !parameters.TryGetValue("Length", out var rawLength))
            {
                output["Error"] = "Missing required parameters: Value, Length";
                return (output, null);
            }

            if (!int.TryParse(rawLength?.ToString(), out int length) || length < 0)
            {
                output["Error"] = "Length must be a non-negative integer";
                return (output, null);
            }

            char padChar = ' ';
            if (parameters.TryGetValue("PadChar", out var pcRaw) &&
                pcRaw?.ToString()?.Length > 0)
                padChar = pcRaw.ToString()[0];

            string align = "right";
            if (parameters.TryGetValue("Align", out var alRaw) &&
                alRaw?.ToString()?.ToLowerInvariant() == "left")
                align = "left";

            string src = rawValue?.ToString() ?? string.Empty;
            string res;
            if (src.Length >= length)
            {
                res = src[..length];
            }
            else
            {
                res = align == "left"
                    ? src.PadRight(length, padChar)
                    : src.PadLeft(length, padChar);
            }

            output["Result"] = res;
            return (output, res);
        }
    }
}
