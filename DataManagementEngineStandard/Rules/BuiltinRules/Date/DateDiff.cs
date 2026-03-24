using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Rules.BuiltinRules.Date
{
    /// <summary>
    /// Computes the difference between <c>Start</c> and <c>End</c> dates.
    /// Parameters: <c>Start</c> (DateTime/string), <c>End</c> (DateTime/string),
    /// <c>Part</c> ("days"|"hours"|"minutes"|"seconds"|"totaldays").
    /// Returns the difference as a double (fractional for sub-day parts).
    /// </summary>
    [Rule(ruleKey: "Date.DateDiff", ParserKey = "RulesParser", RuleName = "DateDiff")]
    public sealed class DateDiff : IRule
    {
        public string RuleText { get; set; } = "Date.DateDiff";
        public IRuleStructure Structure { get; set; } = new RuleStructure();

        public (Dictionary<string, object> outputs, object result) SolveRule(
            Dictionary<string, object> parameters = null)
        {
            var output = new Dictionary<string, object>();
            if (parameters == null ||
                !parameters.TryGetValue("Start", out var rawStart) ||
                !parameters.TryGetValue("End",   out var rawEnd)   ||
                !parameters.TryGetValue("Part",  out var rawPart))
            {
                output["Error"] = "Missing required parameters: Start, End, Part";
                return (output, null);
            }

            if (!TryParseDate(rawStart, out DateTime start) ||
                !TryParseDate(rawEnd,   out DateTime end))
            {
                output["Error"] = "Start and End must be valid DateTimes";
                return (output, null);
            }

            TimeSpan span = end - start;
            double res;
            switch (rawPart?.ToString()?.ToLowerInvariant())
            {
                case "days":      res = Math.Floor(span.TotalDays);    break;
                case "totaldays": res = span.TotalDays;                break;
                case "hours":     res = span.TotalHours;               break;
                case "minutes":   res = span.TotalMinutes;             break;
                case "seconds":   res = span.TotalSeconds;             break;
                default:
                    output["Error"] = "Part must be one of: days, totaldays, hours, minutes, seconds";
                    return (output, null);
            }

            output["Result"] = res;
            output["Span"]   = span.ToString();
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
