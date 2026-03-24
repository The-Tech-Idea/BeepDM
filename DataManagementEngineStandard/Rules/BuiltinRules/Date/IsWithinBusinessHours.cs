using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Rules.BuiltinRules.Date
{
    /// <summary>
    /// Returns <c>true</c> if <c>Value</c> falls within business hours on a weekday.
    /// Parameters: <c>Value</c> (DateTime/string), <c>StartHour</c> (int 0-23, default 9),
    /// <c>EndHour</c> (int 0-23, default 17).
    /// Optional <c>TimeZone</c> (IANA/Windows timezone id, default = local).
    /// Optional <c>IncludeWeekends</c> (bool string, default "false").
    /// </summary>
    [Rule(ruleKey: "Date.IsWithinBusinessHours", ParserKey = "RulesParser", RuleName = "IsWithinBusinessHours")]
    public sealed class IsWithinBusinessHours : IRule
    {
        public string RuleText { get; set; } = "Date.IsWithinBusinessHours";
        public IRuleStructure Structure { get; set; } = new RuleStructure();

        public (Dictionary<string, object> outputs, object result) SolveRule(
            Dictionary<string, object> parameters = null)
        {
            var output = new Dictionary<string, object>();
            if (parameters == null || !parameters.TryGetValue("Value", out var rawValue))
            {
                output["Error"] = "Missing required parameter: Value";
                return (output, false);
            }

            if (!TryParseDate(rawValue, out DateTime dtUtc))
            {
                output["Error"] = $"Cannot parse '{rawValue}' as a DateTime";
                return (output, false);
            }

            // Convert to target timezone when provided
            if (parameters.TryGetValue("TimeZone", out var tzRaw) &&
                !string.IsNullOrWhiteSpace(tzRaw?.ToString()))
            {
                try
                {
                    var tzi = TimeZoneInfo.FindSystemTimeZoneById(tzRaw.ToString()!);
                    dtUtc = TimeZoneInfo.ConvertTime(dtUtc, tzi);
                }
                catch (TimeZoneNotFoundException)
                {
                    output["Warning"] = $"TimeZone '{tzRaw}' not found; using local time";
                }
            }

            int startHour = 9;
            int endHour   = 17;
            if (parameters.TryGetValue("StartHour", out var shRaw)) int.TryParse(shRaw?.ToString(), out startHour);
            if (parameters.TryGetValue("EndHour",   out var ehRaw)) int.TryParse(ehRaw?.ToString(), out endHour);

            bool includeWeekends = false;
            if (parameters.TryGetValue("IncludeWeekends", out var iwRaw))
                bool.TryParse(iwRaw?.ToString(), out includeWeekends);

            bool isWeekday = dtUtc.DayOfWeek != DayOfWeek.Saturday && dtUtc.DayOfWeek != DayOfWeek.Sunday;
            bool inHours   = dtUtc.Hour >= startHour && dtUtc.Hour < endHour;
            bool res       = (includeWeekends || isWeekday) && inHours;

            output["Result"]          = res;
            output["DayOfWeek"]       = dtUtc.DayOfWeek.ToString();
            output["HourOfDay"]       = dtUtc.Hour;
            output["IsWeekday"]       = isWeekday;
            output["IsWithinHours"]   = inHours;
            return (output, res);
        }

        private static bool TryParseDate(object raw, out DateTime result)
        {
            if (raw is DateTime dt) { result = dt; return true; }
            return DateTime.TryParse(raw?.ToString(),
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out result);
        }
    }
}
