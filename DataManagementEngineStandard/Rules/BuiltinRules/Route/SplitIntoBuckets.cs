using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Rules.BuiltinRules.Route
{
    /// <summary>
    /// Splits a numeric <c>Value</c> into buckets and returns the bucket label.
    /// Parameters:
    ///   <c>Value</c> (numeric), <c>Buckets</c> (semicolon-separated "min-max=label" or "max=label"),
    ///   <c>DefaultBucket</c> (label when no bucket matches, default "Other").
    /// Example Buckets: "0-25=Low;26-75=Medium;76-100=High"
    /// </summary>
    [Rule(ruleKey: "Route.SplitIntoBuckets", ParserKey = "RulesParser", RuleName = "SplitIntoBuckets")]
    public sealed class SplitIntoBuckets : IRule
    {
        public string RuleText { get; set; } = "Route.SplitIntoBuckets";
        public IRuleStructure Structure { get; set; } = new RuleStructure();

        public (Dictionary<string, object> outputs, object result) SolveRule(
            Dictionary<string, object> parameters = null)
        {
            var output = new Dictionary<string, object>();
            if (parameters == null ||
                !parameters.TryGetValue("Value",   out var rawValue)   ||
                !parameters.TryGetValue("Buckets", out var rawBuckets))
            {
                output["Error"] = "Missing required parameters: Value, Buckets";
                return (output, null);
            }

            var ic = System.Globalization.CultureInfo.InvariantCulture;
            if (!double.TryParse(rawValue?.ToString(),
                    System.Globalization.NumberStyles.Any, ic, out double val))
            {
                output["Error"] = "Value must be a valid numeric";
                return (output, null);
            }

            string defaultBucket = "Other";
            if (parameters.TryGetValue("DefaultBucket", out var defRaw))
                defaultBucket = defRaw?.ToString() ?? "Other";

            string? matched = null;
            foreach (string entry in (rawBuckets?.ToString() ?? string.Empty).Split(';',
                         StringSplitOptions.RemoveEmptyEntries))
            {
                int eq = entry.IndexOf('=', StringComparison.Ordinal);
                if (eq < 0) continue;
                string rangeStr = entry[..eq].Trim();
                string label    = entry[(eq + 1)..].Trim();

                int dash = rangeStr.IndexOf('-', 1); // skip leading minus
                if (dash > 0 &&
                    double.TryParse(rangeStr[..dash], System.Globalization.NumberStyles.Any, ic, out double lo) &&
                    double.TryParse(rangeStr[(dash + 1)..], System.Globalization.NumberStyles.Any, ic, out double hi))
                {
                    if (val >= lo && val <= hi) { matched = label; break; }
                }
                else if (double.TryParse(rangeStr, System.Globalization.NumberStyles.Any, ic, out double max))
                {
                    if (val <= max) { matched = label; break; }
                }
            }

            string res = matched ?? defaultBucket;
            output["Result"] = res;
            output["Bucket"] = res;
            return (output, res);
        }
    }
}
