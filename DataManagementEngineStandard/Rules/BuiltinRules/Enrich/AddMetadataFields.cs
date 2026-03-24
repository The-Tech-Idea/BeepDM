using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Rules.BuiltinRules.Enrich
{
    /// <summary>
    /// Enriches a record with computed metadata fields: <c>__timestamp</c>, <c>__ruleKey</c>,
    /// <c>__version</c>, and any user-defined tags.
    /// Parameters (all optional):
    ///   <c>RuleKey</c>  — source rule identifier to stamp (default: this rule's key).
    ///   <c>Version</c>  — schema/pipeline version string.
    ///   <c>Tags</c>     — semicolon-separated "key=value" tag string.
    ///   <c>TimestampField</c> — output field name for timestamp (default "__timestamp").
    /// </summary>
    [Rule(ruleKey: "Enrich.AddMetadataFields", ParserKey = "RulesParser", RuleName = "AddMetadataFields")]
    public sealed class AddMetadataFields : IRule
    {
        public string RuleText { get; set; } = "Enrich.AddMetadataFields";
        public IRuleStructure Structure { get; set; } = new RuleStructure();

        public (Dictionary<string, object> outputs, object result) SolveRule(
            Dictionary<string, object> parameters = null)
        {
            parameters ??= new Dictionary<string, object>();
            var output = new Dictionary<string, object>();

            string tsField = "__timestamp";
            if (parameters.TryGetValue("TimestampField", out var tfRaw) && tfRaw != null)
                tsField = tfRaw.ToString()!;

            output[tsField]     = DateTime.UtcNow.ToString("o");
            output["__ruleKey"] = parameters.TryGetValue("RuleKey", out var rk)
                                    ? rk?.ToString() ?? RuleText
                                    : RuleText;

            if (parameters.TryGetValue("Version", out var verRaw) && verRaw != null)
                output["__version"] = verRaw.ToString()!;

            if (parameters.TryGetValue("Tags", out var tagsRaw))
            {
                foreach (string entry in (tagsRaw?.ToString() ?? string.Empty).Split(';',
                             StringSplitOptions.RemoveEmptyEntries))
                {
                    int eq = entry.IndexOf('=', StringComparison.Ordinal);
                    if (eq < 0) { output["__tag_" + entry.Trim()] = true; continue; }
                    output["__tag_" + entry[..eq].Trim()] = entry[(eq + 1)..].Trim();
                }
            }

            output["Result"] = true;
            return (output, true);
        }
    }
}
