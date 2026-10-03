using System;
using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor.Importing;

namespace TheTechIdea.Beep.Editor.Defaults.Resolvers
{
    // Validate the shipped date tree before invoking any nested resolver callbacks.
    internal static class RequiredDateTimeRule
    {
        private sealed record Plan(string Source, string Operator, Plan Base = null,
            object Literal = null, long Ticks = 0, int CalendarOffset = 0, string Format = null);

        private const string IsoPattern = @"\A[0-9]{4}-[0-9]{2}-[0-9]{2}(?:T[0-9]{2}:[0-9]{2}:[0-9]{2}(?:\.[0-9]{1,7})?(?:Z|[+-][0-9]{2}:[0-9]{2})?)?\z";
        private static readonly string[] LocalFormats = { "yyyy-MM-dd", "yyyy-MM-dd'T'HH:mm:ss", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF" };
        private static readonly string[] UtcFormats = { "yyyy-MM-dd'T'HH:mm:ss'Z'", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'" };
        private static readonly string[] OffsetFormats = { "yyyy-MM-dd'T'HH:mm:sszzz", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz" };

        private static void Check()
        {
            RequiredDefaultResolution.Current?.Token.ThrowIfCancellationRequested();
            if (RequiredDefaultResolution.Current?.Failed == true) Deny();
        }

        private static void Deny() => throw new ImportTransformationException(ImportTransformationStage.Defaults);
        private static bool Matches(string text, string pattern) => Regex.IsMatch(text, pattern,
            RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);
        private static bool IsFunction(string op) => op is "ADDDAYS" or "ADDHOURS" or "ADDMINUTES" or "ADDMONTHS" or "ADDYEARS" or "FORMAT" or "DATEFORMAT";
        private static bool IsClock(string op) => op is "NOW" or "CURRENTDATETIME" or "UTCNOW" or "CURRENTUTCDATETIME" or
            "UTCTODAY" or "TODAY" or "CURRENTDATE" or "YESTERDAY" or "TOMORROW" or "CURRENTTIME" or
            "STARTOFMONTH" or "ENDOFMONTH" or "STARTOFYEAR" or "ENDOFYEAR" or "STARTOFWEEK" or "ENDOFWEEK";

        internal static object Resolve(string rule, IPassedArgs parameters)
        {
            var plan = Parse(rule, 1);
            return Evaluate(plan, parameters, root: true);
        }

        private static Plan Parse(string source, int depth)
        {
            Check();
            var text = source.Trim();
            if (depth > 32 || text.Length == 0 || text.Length > 16384) Deny();
            if (text[0] is '\'' or '"')
                return new Plan(text, null, Literal: ParseIso(RequiredBuiltInRule.RequireAtom(text, nonempty: true)));
            if (Matches(text, IsoPattern)) return new Plan(text, null, Literal: ParseIso(text));
            if (!RequiredDefaultResolution.HasValidEnvelope(text)) Deny();
            var open = text.IndexOf('(');
            var op = (open < 0 ? text : text.Substring(0, open)).ToUpperInvariant();
            if (!Matches(op, @"\A[A-Z_][A-Z0-9_]*\z")) Deny();
            if (!IsFunction(op))
            {
                if (IsClock(op) && open >= 0) Deny();
                return new Plan(text, op);
            }
            if (open < 0 || !text.EndsWith(")", StringComparison.Ordinal)) Deny();
            var args = RequiredBuiltInRule.SplitArguments(text.Substring(open + 1, text.Length - open - 2));
            if (args.Length != 2) Deny();
            var basePlan = Parse(args[0], depth + 1);
            var argument = RequiredBuiltInRule.RequireAtom(args[1], nonempty: true);
            if (op is "FORMAT" or "DATEFORMAT")
            {
                if (argument.Length > 1024) Deny();
                // The invariant formatter checks unmatched quotes, escapes and standard specifiers.
                if (DateTime.MinValue.ToString(argument, CultureInfo.InvariantCulture).Length > 16384) Deny();
                return new Plan(text, op, basePlan, Format: argument);
            }
            if (op is "ADDMONTHS" or "ADDYEARS")
            {
                if (!Matches(argument, @"\A[+-]?[0-9]+\z") || !int.TryParse(argument,
                    NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var count))
                    throw new ImportTransformationException(ImportTransformationStage.Defaults);
                return new Plan(text, op, basePlan, CalendarOffset: count);
            }
            if (argument.Length > 64 || !Matches(argument, @"\A[+-]?[0-9]+(?:\.[0-9]+)?\z")) Deny();
            var digits = 0;
            foreach (var ch in argument) if (ch is >= '0' and <= '9') digits++;
            // No decimal parser rounding or sub-tick truncation is admitted.
            if (digits > 28) Deny();
            var factor = op == "ADDDAYS" ? TimeSpan.TicksPerDay : op == "ADDHOURS" ? TimeSpan.TicksPerHour : TimeSpan.TicksPerMinute;
            var dot = argument.IndexOf('.');
            var numerator = BigInteger.Parse(argument.Replace(".", ""), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture) * factor;
            var denominator = BigInteger.Pow(10, dot < 0 ? 0 : argument.Length - dot - 1);
            var ticks = BigInteger.DivRem(numerator, denominator, out var remainder);
            if (!remainder.IsZero || ticks < long.MinValue || ticks > long.MaxValue) Deny();
            return new Plan(text, op, basePlan, Ticks: (long)ticks);
        }

        private static object ParseIso(string text)
        {
            if (text.Length > 33 || !Matches(text, IsoPattern)) Deny();
            if (text.EndsWith("Z", StringComparison.Ordinal))
            {
                if (DateTime.TryParseExact(text, UtcFormats, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var utc)) return utc;
            }
            else if (text.Length > 19 && text[text.Length - 6] is '+' or '-')
            {
                if (DateTimeOffset.TryParseExact(text, OffsetFormats, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var offset)) return offset;
            }
            else if (DateTime.TryParseExact(text, LocalFormats, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var local)) return local;
            Deny();
            return null;
        }

        private static object Evaluate(Plan plan, IPassedArgs parameters, bool root)
        {
            Check();
            if (plan.Literal != null) return plan.Literal;
            if (!root)
            {
                // Even shipped token/function overrides retain pinned registry selection and context.
                var scope = RequiredDefaultResolution.Current;
                var value = scope.Registry.Resolve(":" + plan.Source, parameters, scope.Token);
                Check();
                if (value is DateTime or DateTimeOffset) return value;
                Deny();
            }
            if (plan.Base == null) return Clock(plan.Operator);
            var date = Evaluate(plan.Base, parameters, root: false);
            Check();
            object result;
            if (date is DateTime dt)
                result = plan.Operator switch
                {
                    "ADDMONTHS" => dt.AddMonths(plan.CalendarOffset),
                    "ADDYEARS" => dt.AddYears(plan.CalendarOffset),
                    "FORMAT" or "DATEFORMAT" => dt.ToString(plan.Format, CultureInfo.InvariantCulture),
                    _ => dt.AddTicks(plan.Ticks)
                };
            else if (date is DateTimeOffset offset)
                result = plan.Operator switch
                {
                    "ADDMONTHS" => offset.AddMonths(plan.CalendarOffset),
                    "ADDYEARS" => offset.AddYears(plan.CalendarOffset),
                    "FORMAT" or "DATEFORMAT" => offset.ToString(plan.Format, CultureInfo.InvariantCulture),
                    _ => offset.AddTicks(plan.Ticks)
                };
            else throw new ImportTransformationException(ImportTransformationStage.Defaults);
            if (result is string formatted && formatted.Length > 16384) Deny();
            Check();
            return result;
        }

        private static object Clock(string op)
        {
            Check();
            var instant = DateTimeOffset.UtcNow;
            var now = instant.LocalDateTime;
            var today = now.Date;
            object result = op switch
            {
                "NOW" or "CURRENTDATETIME" => now,
                "UTCNOW" or "CURRENTUTCDATETIME" => instant.UtcDateTime,
                "UTCTODAY" => instant.UtcDateTime.Date,
                "TODAY" or "CURRENTDATE" => today,
                "YESTERDAY" => today.AddDays(-1),
                "TOMORROW" => today.AddDays(1),
                "CURRENTTIME" => now.TimeOfDay,
                "STARTOFMONTH" => new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Local),
                "ENDOFMONTH" => new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month), 0, 0, 0, DateTimeKind.Local),
                "STARTOFYEAR" => new DateTime(today.Year, 1, 1, 0, 0, 0, DateTimeKind.Local),
                "ENDOFYEAR" => new DateTime(today.Year, 12, 31, 0, 0, 0, DateTimeKind.Local),
                "STARTOFWEEK" => today.AddDays(-((7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7)),
                "ENDOFWEEK" => today.AddDays(6 - ((7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7)),
                _ => throw new ImportTransformationException(ImportTransformationStage.Defaults)
            };
            Check();
            return result;
        }
    }
}
