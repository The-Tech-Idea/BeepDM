using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Rules.BuiltinRules.Transform
{
    /// <summary>
    /// Splits <c>Value</c> by <c>Delimiter</c> and returns the element at <c>Index</c> (0-based).
    /// Parameters: <c>Value</c> (string), <c>Delimiter</c> (string), <c>Index</c> (int string).
    /// Optional <c>TrimParts</c> (bool string, default "true").
    /// </summary>
    [Rule(ruleKey: "Transform.SplitAndTake", ParserKey = "RulesParser", RuleName = "SplitAndTake")]
    public sealed class SplitAndTake : IRule
    {
        public string RuleText { get; set; } = "Transform.SplitAndTake";
        public IRuleStructure Structure { get; set; } = new RuleStructure();

        public (Dictionary<string, object> outputs, object result) SolveRule(
            Dictionary<string, object> parameters = null)
        {
            var output = new Dictionary<string, object>();
            if (parameters == null ||
                !parameters.TryGetValue("Value",     out var rawValue) ||
                !parameters.TryGetValue("Delimiter", out var rawDelim) ||
                !parameters.TryGetValue("Index",     out var rawIndex))
            {
                output["Error"] = "Missing required parameters: Value, Delimiter, Index";
                return (output, null);
            }

            if (!int.TryParse(rawIndex?.ToString(), out int idx))
            {
                output["Error"] = "Index must be a valid integer";
                return (output, null);
            }

            bool trimParts = true;
            if (parameters.TryGetValue("TrimParts", out var tpRaw))
                bool.TryParse(tpRaw?.ToString(), out trimParts);

            string   src   = rawValue?.ToString() ?? string.Empty;
            string   delim = rawDelim?.ToString()  ?? string.Empty;
            string[] parts = src.Split(new[] { delim }, StringSplitOptions.None);

            if (trimParts)
                for (int i = 0; i < parts.Length; i++)
                    parts[i] = parts[i].Trim();

            if (idx < 0 || idx >= parts.Length)
            {
                output["Error"]  = $"Index {idx} is out of range (parts count: {parts.Length})";
                output["Parts"]  = parts;
                output["Result"] = null;
                return (output, null);
            }

            string res = parts[idx];
            output["Result"] = res;
            output["Parts"]  = parts;
            return (output, res);
        }
    }
}
