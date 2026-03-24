using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Rules.BuiltinRules.Compute
{
    /// <summary>
    /// Computes the elapsed time between a source date value and today (or a reference date).
    /// Parameters:
    ///   <c>Value</c>         — the date to measure from (parseable date string or DateTime).
    ///   <c>Unit</c>          — <c>Years</c> | <c>Months</c> | <c>Days</c> | <c>Hours</c> (default Years).
    ///   <c>ReferenceDate</c> — date to measure to (default UTC now).
    /// Outputs: <c>Result</c> (numeric in requested unit), <c>Years</c>, <c>Months</c>, <c>Days</c>.
    /// </summary>
    [Rule(ruleKey: "Compute.AgeFromDate", ParserKey = "RulesParser", RuleName = "AgeFromDate")]
    public sealed class AgeFromDate : IRule
    {
        public string RuleText { get; set; } = "Compute.AgeFromDate";
        public IRuleStructure Structure { get; set; } = new RuleStructure();

        public (Dictionary<string, object> outputs, object result) SolveRule(
            Dictionary<string, object> parameters = null)
        {
            var output = new Dictionary<string, object>();
            if (parameters == null || !parameters.TryGetValue("Value", out var valRaw))
            {
                output["Error"] = "Missing parameter: Value";
                return (output, null);
            }

            if (!DateTime.TryParse(valRaw?.ToString(), out var from))
            {
                output["Error"] = $"Cannot parse date: {valRaw}";
                return (output, null);
            }

            DateTime to = DateTime.UtcNow;
            if (parameters.TryGetValue("ReferenceDate", out var refRaw) && refRaw != null)
                DateTime.TryParse(refRaw.ToString(), out to);

            parameters.TryGetValue("Unit", out var unitRaw);
            var unit = unitRaw?.ToString()?.Trim().ToUpperInvariant() ?? "YEARS";

            var ts     = to - from;
            int years  = (int)(ts.TotalDays / 365.25);
            int months = years * 12 + (to.Month - from.Month) + (to.Day >= from.Day ? 0 : -1);
            if (months < 0) months = 0;

            object result = unit switch
            {
                "YEARS"  => (object)years,
                "MONTHS" => months,
                "DAYS"   => (int)ts.TotalDays,
                "HOURS"  => (long)ts.TotalHours,
                _        => years
            };

            output["Result"] = result;
            output["Years"]  = years;
            output["Months"] = months;
            output["Days"]   = (int)ts.TotalDays;
            return (output, result);
        }
    }
}
