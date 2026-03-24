using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Rules.BuiltinRules.DQ
{
    /// <summary>
    /// Tries to parse <c>Value</c> as <c>ExpectedType</c> and returns true on success.
    /// Parameters: <c>Value</c> (object), <c>ExpectedType</c> (string: "int", "double",
    /// "decimal", "date", "datetime", "guid", "bool", "string").
    /// Output dict also contains <c>ParsedValue</c> on success.
    /// </summary>
    [Rule(ruleKey: "DQ.MatchesExpectedType", ParserKey = "RulesParser", RuleName = "MatchesExpectedType")]
    public sealed class MatchesExpectedType : IRule
    {
        public string RuleText { get; set; } = "DQ.MatchesExpectedType";
        public IRuleStructure Structure { get; set; } = new RuleStructure();

        public (Dictionary<string, object> outputs, object result) SolveRule(
            Dictionary<string, object> parameters = null)
        {
            var output = new Dictionary<string, object>();
            if (parameters == null ||
                !parameters.TryGetValue("Value",        out var rawValue) ||
                !parameters.TryGetValue("ExpectedType", out var rawType))
            {
                output["Error"] = "Missing required parameters: Value, ExpectedType";
                return (output, false);
            }

            string valueStr = rawValue?.ToString() ?? string.Empty;
            string typeStr  = rawType?.ToString()?.ToLowerInvariant()?.Trim() ?? string.Empty;
            bool   success  = false;
            object parsed   = null;

            switch (typeStr)
            {
                case "int":
                    if (int.TryParse(valueStr, out var i)) { success = true; parsed = i; } break;
                case "double":
                case "float":
                    if (double.TryParse(valueStr, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out var d))
                    { success = true; parsed = d; } break;
                case "decimal":
                    if (decimal.TryParse(valueStr, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out var dec))
                    { success = true; parsed = dec; } break;
                case "date":
                    if (DateTime.TryParse(valueStr, out var dt))
                    { success = true; parsed = dt.Date; } break;
                case "datetime":
                    if (DateTime.TryParse(valueStr, out var dtt))
                    { success = true; parsed = dtt; } break;
                case "guid":
                    if (Guid.TryParse(valueStr, out var g)) { success = true; parsed = g; } break;
                case "bool":
                    if (bool.TryParse(valueStr, out var b)) { success = true; parsed = b; } break;
                case "string":
                    success = true; parsed = valueStr; break;
                default:
                    output["Error"] = $"Unknown ExpectedType: '{typeStr}'. "
                        + "Supported: int, double, decimal, date, datetime, guid, bool, string";
                    return (output, false);
            }

            output["Result"]      = success;
            if (parsed != null) output["ParsedValue"] = parsed;
            return (output, success);
        }
    }
}
