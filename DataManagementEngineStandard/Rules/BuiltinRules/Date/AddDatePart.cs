using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Rules.BuiltinRules.Date
{
    /// <summary>
    /// Adds a specified amount of a date part to <c>Value</c>.
    /// Parameters: <c>Value</c> (DateTime or parseable string), <c>Amount</c> (int),
    /// <c>Part</c> ("years"|"months"|"days"|"hours"|"minutes"|"seconds").
    /// Returns the resulting <see cref="DateTime"/>.
    /// </summary>
    [Rule(ruleKey: "Date.AddDatePart", ParserKey = "RulesParser", RuleName = "AddDatePart")]
    public sealed class AddDatePart : IRule
    {
        public string RuleText { get; set; } = "Date.AddDatePart";
        public IRuleStructure Structure { get; set; } = new RuleStructure();

        public (Dictionary<string, object> outputs, object result) SolveRule(
            Dictionary<string, object> parameters = null)
        {
            var output = new Dictionary<string, object>();
            if (parameters == null ||
                !parameters.TryGetValue("Value",  out var rawValue)  ||
                !parameters.TryGetValue("Amount", out var rawAmount) ||
                !parameters.TryGetValue("Part",   out var rawPart))
            {
                output["Error"] = "Missing required parameters: Value, Amount, Part";
                return (output, null);
            }

            if (!TryParseDate(rawValue, out DateTime dt))
            {
                output["Error"] = $"Cannot parse '{rawValue}' as a DateTime";
                return (output, null);
            }

            if (!int.TryParse(rawAmount?.ToString(), out int amount))
            {
                output["Error"] = "Amount must be a valid integer";
                return (output, null);
            }

            DateTime res;
            switch (rawPart?.ToString()?.ToLowerInvariant())
            {
                case "years":   res = dt.AddYears(amount);   break;
                case "months":  res = dt.AddMonths(amount);  break;
                case "days":    res = dt.AddDays(amount);    break;
                case "hours":   res = dt.AddHours(amount);   break;
                case "minutes": res = dt.AddMinutes(amount); break;
                case "seconds": res = dt.AddSeconds(amount); break;
                default:
                    output["Error"] = "Part must be one of: years, months, days, hours, minutes, seconds";
                    return (output, null);
            }

            output["Result"] = res;
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
