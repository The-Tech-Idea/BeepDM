using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Rules.BuiltinRules.Compute
{
    /// <summary>
    /// Maps an input value to an output value via a lookup table — equivalent to a SWITCH/CASE.
    /// Parameters:
    ///   <c>Value</c>   — input value to look up (case-insensitive match).
    ///   <c>Map</c>     — semicolon-separated <c>"input=output"</c> pairs.
    ///                    Use <c>"*=default"</c> for a catch-all entry.
    ///                    Example: <c>"A=Active;I=Inactive;*=Unknown"</c>.
    ///   <c>Default</c> — fallback string when no entry matches (overrides <c>*</c> entry).
    /// Outputs: <c>Result</c> (mapped value or default), <c>Matched</c> (bool — true when an exact key matched).
    /// </summary>
    [Rule(ruleKey: "Compute.MapValueTable", ParserKey = "RulesParser", RuleName = "MapValueTable")]
    public sealed class MapValueTable : IRule
    {
        public string RuleText { get; set; } = "Compute.MapValueTable";
        public IRuleStructure Structure { get; set; } = new RuleStructure();

        public (Dictionary<string, object> outputs, object result) SolveRule(
            Dictionary<string, object> parameters = null)
        {
            var output = new Dictionary<string, object>();
            if (parameters == null) { output["Error"] = "No parameters."; return (output, null); }

            parameters.TryGetValue("Value",   out var valRaw);
            parameters.TryGetValue("Map",     out var mapRaw);
            parameters.TryGetValue("Default", out var defRaw);

            var input    = valRaw?.ToString()?.Trim() ?? string.Empty;
            var mapStr   = mapRaw?.ToString()         ?? string.Empty;
            string catchAll = defRaw?.ToString();
            string matched  = null;

            foreach (var pair in mapStr.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var idx = pair.IndexOf('=');
                if (idx <= 0) continue;
                var key = pair[..idx].Trim();
                var val = pair[(idx + 1)..].Trim();
                if (key == "*") { catchAll ??= val; continue; }
                if (string.Equals(key, input, StringComparison.OrdinalIgnoreCase))
                { matched = val; break; }
            }

            bool found      = matched != null;
            object resultVal = found ? matched : catchAll;

            output["Result"]  = resultVal;
            output["Matched"] = found;
            return (output, resultVal);
        }
    }
}
