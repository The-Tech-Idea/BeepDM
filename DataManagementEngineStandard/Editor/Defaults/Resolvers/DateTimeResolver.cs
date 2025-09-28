using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Editor.Defaults.Resolvers
{
    /// <summary>
    /// Resolver for date/time related default values with enhanced functionality
    /// </summary>
    public class DateTimeResolver : BaseDefaultValueResolver
    {
        public DateTimeResolver(IDMEEditor editor) : base(editor) { }

        public override string ResolverName => "DateTime";

        public override IEnumerable<string> SupportedRuleTypes => new[]
        {
            "NOW", "TODAY", "YESTERDAY", "TOMORROW", 
            "CURRENTDATE", "CURRENTTIME", "CURRENTDATETIME",
            "ADDDAYS", "ADDHOURS", "ADDMINUTES", "ADDMONTHS", "ADDYEARS",
            "FORMAT", "DATEFORMAT", "STARTOFMONTH", "ENDOFMONTH",
            "STARTOFYEAR", "ENDOFYEAR", "STARTOFWEEK", "ENDOFWEEK"
        };

        public override object ResolveValue(string rule, IPassedArgs parameters)
        {
            if (string.IsNullOrWhiteSpace(rule))
                return DateTime.Now;

            var upperRule = rule.ToUpperInvariant().Trim();
            
            try
            {
                return upperRule switch
                {
                    "NOW" or "CURRENTDATETIME" => DateTime.Now,
                    "TODAY" or "CURRENTDATE" => DateTime.Today,
                    "YESTERDAY" => DateTime.Today.AddDays(-1),
                    "TOMORROW" => DateTime.Today.AddDays(1),
                    "CURRENTTIME" => DateTime.Now.TimeOfDay,
                    "STARTOFMONTH" => new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1),
                    "ENDOFMONTH" => new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(1).AddDays(-1),
                    "STARTOFYEAR" => new DateTime(DateTime.Today.Year, 1, 1),
                    "ENDOFYEAR" => new DateTime(DateTime.Today.Year, 12, 31),
                    "STARTOFWEEK" => GetStartOfWeek(DateTime.Today),
                    "ENDOFWEEK" => GetStartOfWeek(DateTime.Today).AddDays(6),
                    _ when upperRule.StartsWith("ADDDAYS(") => ParseAddDays(rule),
                    _ when upperRule.StartsWith("ADDHOURS(") => ParseAddHours(rule),
                    _ when upperRule.StartsWith("ADDMINUTES(") => ParseAddMinutes(rule),
                    _ when upperRule.StartsWith("ADDMONTHS(") => ParseAddMonths(rule),
                    _ when upperRule.StartsWith("ADDYEARS(") => ParseAddYears(rule),
                    _ when upperRule.StartsWith("FORMAT(") || upperRule.StartsWith("DATEFORMAT(") => ParseDateFormat(rule),
                    _ => DateTime.Now
                };
            }
            catch (Exception ex)
            {
                LogError($"Error resolving date/time rule '{rule}'", ex);
                return DateTime.Now;
            }
        }

        public override bool CanHandle(string rule)
        {
            if (string.IsNullOrWhiteSpace(rule))
                return false;

            var upperRule = rule.ToUpperInvariant().Trim();
            
            return SupportedRuleTypes.Any(type => upperRule.Contains(type)) ||
                   upperRule.StartsWith("ADDDAYS(") ||
                   upperRule.StartsWith("ADDHOURS(") ||
                   upperRule.StartsWith("ADDMINUTES(") ||
                   upperRule.StartsWith("ADDMONTHS(") ||
                   upperRule.StartsWith("ADDYEARS(") ||
                   upperRule.StartsWith("FORMAT(") ||
                   upperRule.StartsWith("DATEFORMAT(");
        }

        public override IEnumerable<string> GetExamples()
        {
            return new[]
            {
                "NOW - Current date and time",
                "TODAY - Current date (midnight)",
                "YESTERDAY - Yesterday's date",
                "TOMORROW - Tomorrow's date",
                "CURRENTTIME - Current time only",
                "STARTOFMONTH - First day of current month",
                "ENDOFMONTH - Last day of current month",
                "STARTOFYEAR - January 1st of current year",
                "ENDOFYEAR - December 31st of current year",
                "STARTOFWEEK - Monday of current week",
                "ENDOFWEEK - Sunday of current week",
                "ADDDAYS(TODAY, 7) - Add 7 days to today",
                "ADDDAYS(NOW, -30) - Subtract 30 days from now",
                "ADDHOURS(NOW, 2) - Add 2 hours to current time",
                "ADDMINUTES(NOW, 30) - Add 30 minutes to current time",
                "ADDMONTHS(TODAY, 1) - Add 1 month to today",
                "ADDYEARS(TODAY, -1) - Subtract 1 year from today",
                "FORMAT(NOW, 'yyyy-MM-dd') - Format current date as ISO date",
                "FORMAT(NOW, 'HH:mm:ss') - Format current time as time string",
                "DATEFORMAT(TODAY, 'dd/MM/yyyy') - Format today as DD/MM/YYYY"
            };
        }

        #region Private Helper Methods

        private DateTime GetStartOfWeek(DateTime date)
        {
            var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-1 * diff).Date;
        }

        private object ParseAddDays(string rule)
        {
            try
            {
                var content = ExtractParenthesesContent(rule);
                var parts = SplitParameters(content);
                
                if (parts.Length == 2)
                {
                    var baseValue = ResolveBaseDateTime(parts[0].Trim());
                    if (TryConvert<int>(parts[1].Trim(), out int days))
                    {
                        return baseValue.AddDays(days);
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Error parsing ADDDAYS rule '{rule}'", ex);
            }
            return DateTime.Now;
        }

        private object ParseAddHours(string rule)
        {
            try
            {
                var content = ExtractParenthesesContent(rule);
                var parts = SplitParameters(content);
                
                if (parts.Length == 2)
                {
                    var baseValue = ResolveBaseDateTime(parts[0].Trim());
                    if (TryConvert<int>(parts[1].Trim(), out int hours))
                    {
                        return baseValue.AddHours(hours);
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Error parsing ADDHOURS rule '{rule}'", ex);
            }
            return DateTime.Now;
        }

        private object ParseAddMinutes(string rule)
        {
            try
            {
                var content = ExtractParenthesesContent(rule);
                var parts = SplitParameters(content);
                
                if (parts.Length == 2)
                {
                    var baseValue = ResolveBaseDateTime(parts[0].Trim());
                    if (TryConvert<int>(parts[1].Trim(), out int minutes))
                    {
                        return baseValue.AddMinutes(minutes);
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Error parsing ADDMINUTES rule '{rule}'", ex);
            }
            return DateTime.Now;
        }

        private object ParseAddMonths(string rule)
        {
            try
            {
                var content = ExtractParenthesesContent(rule);
                var parts = SplitParameters(content);
                
                if (parts.Length == 2)
                {
                    var baseValue = ResolveBaseDateTime(parts[0].Trim());
                    if (TryConvert<int>(parts[1].Trim(), out int months))
                    {
                        return baseValue.AddMonths(months);
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Error parsing ADDMONTHS rule '{rule}'", ex);
            }
            return DateTime.Now;
        }

        private object ParseAddYears(string rule)
        {
            try
            {
                var content = ExtractParenthesesContent(rule);
                var parts = SplitParameters(content);
                
                if (parts.Length == 2)
                {
                    var baseValue = ResolveBaseDateTime(parts[0].Trim());
                    if (TryConvert<int>(parts[1].Trim(), out int years))
                    {
                        return baseValue.AddYears(years);
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Error parsing ADDYEARS rule '{rule}'", ex);
            }
            return DateTime.Now;
        }

        private object ParseDateFormat(string rule)
        {
            try
            {
                var content = ExtractParenthesesContent(rule);
                var parts = SplitParameters(content);
                
                if (parts.Length == 2)
                {
                    var dateValue = ResolveBaseDateTime(parts[0].Trim());
                    var format = RemoveQuotes(parts[1].Trim());
                    
                    if (!string.IsNullOrWhiteSpace(format))
                    {
                        return dateValue.ToString(format);
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Error parsing FORMAT rule '{rule}'", ex);
            }
            return DateTime.Now.ToString("yyyy-MM-dd");
        }

        private DateTime ResolveBaseDateTime(string baseRule)
        {
            if (string.IsNullOrWhiteSpace(baseRule))
                return DateTime.Now;

            // Try to resolve as another date rule first
            var resolved = ResolveValue(baseRule, null);
            if (resolved is DateTime dt)
                return dt;

            // Try to parse as direct date string
            if (DateTime.TryParse(baseRule, out DateTime parsedDate))
                return parsedDate;

            // Default to now
            return DateTime.Now;
        }

        #endregion
    }
}