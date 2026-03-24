using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Rules.BuiltinRules.Enrich
{
    /// <summary>
    /// Merges all key-value pairs from <c>SourceFields</c> into the output dictionary,
    /// optionally applying a prefix to each key.
    /// Parameters: <c>SourceFields</c> (semicolon-separated "Key=Value" string OR any
    /// <c>IDictionary&lt;string,object&gt;</c> via the <c>SourceFields</c> parameter).
    /// Optional <c>Prefix</c> (string prepended to each key).
    /// </summary>
    [Rule(ruleKey: "Enrich.MergeFields", ParserKey = "RulesParser", RuleName = "MergeFields")]
    public sealed class MergeFields : IRule
    {
        public string RuleText { get; set; } = "Enrich.MergeFields";
        public IRuleStructure Structure { get; set; } = new RuleStructure();

        public (Dictionary<string, object> outputs, object result) SolveRule(
            Dictionary<string, object> parameters = null)
        {
            var output = new Dictionary<string, object>();
            if (parameters == null || !parameters.TryGetValue("SourceFields", out var srcRaw))
            {
                output["Error"] = "Missing required parameter: SourceFields";
                return (output, null);
            }

            string prefix = string.Empty;
            if (parameters.TryGetValue("Prefix", out var pfxRaw))
                prefix = pfxRaw?.ToString() ?? string.Empty;

            int merged = 0;
            if (srcRaw is IDictionary<string, object> dict)
            {
                foreach (var kv in dict)
                {
                    output[prefix + kv.Key] = kv.Value;
                    merged++;
                }
            }
            else
            {
                // Parse "key1=val1;key2=val2" string
                foreach (string entry in (srcRaw?.ToString() ?? string.Empty).Split(';',
                             StringSplitOptions.RemoveEmptyEntries))
                {
                    int eq = entry.IndexOf('=', StringComparison.Ordinal);
                    if (eq < 0) continue;
                    string key = prefix + entry[..eq].Trim();
                    string val = entry[(eq + 1)..].Trim();
                    output[key] = val;
                    merged++;
                }
            }

            output["Result"]      = merged;
            output["MergedCount"] = merged;
            return (output, merged);
        }
    }
}
