using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TheTechIdea.Beep.Editor.Defaults.RuleParsing
{
    /// <summary>
    /// Parses rule strings that use the dot-segment DSL syntax (DSL v1).
    ///
    /// General form:  Operator.arg1.arg2...argN
    ///
    /// Segmentation rules:
    ///  - The first dot-delimited token is the operator (case-insensitive).
    ///  - Subsequent tokens are arguments in order.
    ///  - A segment may be quoted with single or double quotes to protect embedded dots:
    ///      QUERY.scalar.'SELECT a.b FROM t'
    ///  - Decimal numbers are kept intact: ADD.10.5 → args ["10","5"]
    ///  - Comparison expressions that include dots (e.g. 1.5) must be quoted.
    ///
    /// The parser sets <see cref="RuleSyntaxVersion.V1Dot"/> on success.
    /// </summary>
    public sealed class DotStyleRuleParser
    {
        // Known operators for the dot-style DSL v1.
        private static readonly HashSet<string> _knownOperators = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // DateTime
            "NOW", "TODAY", "YESTERDAY", "TOMORROW",
            "CURRENTDATE", "CURRENTTIME", "CURRENTDATETIME",
            "ADDDAYS", "ADDHOURS", "ADDMINUTES", "ADDMONTHS", "ADDYEARS",
            "FORMAT", "DATEFORMAT", "STARTOFMONTH", "ENDOFMONTH",
            "STARTOFYEAR", "ENDOFYEAR", "STARTOFWEEK", "ENDOFWEEK",
            // Formulas/math
            "ADD", "SUBTRACT", "MULTIPLY", "DIVIDE",
            "SEQUENCE", "INCREMENT", "RANDOM", "CALCULATE",
            // Expressions/conditionals
            "IF", "CASE", "COALESCE", "ISNULL",
            // Query/datasource
            "QUERY", "LOOKUP", "COUNT", "MAX", "MIN", "SUM", "AVG",
            // Fallback
            "ONERROR", "ONEMPTY",
            // User/system
            "CURRENTUSER", "ENV", "CONFIG", "GUID",
            // Object property
            "PROPERTY", "RECORD", "FIELD"
        };

        /// <summary>
        /// Heuristically decides whether a rule string uses the dot-style DSL.
        /// The rule must start with a known operator followed immediately by a dot,
        /// OR be a known no-argument operator token (like "NOW", "TODAY", "GUID").
        /// </summary>
        public static bool IsDotStyleRule(string rule)
        {
            if (string.IsNullOrWhiteSpace(rule))
                return false;

            var trimmed = rule.Trim();
            var dotIndex = trimmed.IndexOf('.');
            var parenIndex = trimmed.IndexOf('(');

            // If there is a paren before (or instead of) any dot, treat as legacy.
            if (parenIndex >= 0 && (dotIndex < 0 || parenIndex < dotIndex))
                return false;

            if (dotIndex > 0)
            {
                var candidate = trimmed.Substring(0, dotIndex).ToUpperInvariant();
                return _knownOperators.Contains(candidate);
            }

            // No dot at all — match only if it is a known zero-arg operator.
            return _knownOperators.Contains(trimmed.ToUpperInvariant());
        }

        /// <summary>
        /// Parses the given dot-style rule string into a <see cref="ParsedRule"/>.
        /// </summary>
        /// <param name="rule">The rule text to parse.</param>
        /// <returns>A <see cref="ParsedRule"/> — check <see cref="ParsedRule.IsValid"/> before use.</returns>
        public ParsedRule Parse(string rule)
        {
            var diagnostics = new List<RuleDiagnostic>();

            if (string.IsNullOrWhiteSpace(rule))
            {
                diagnostics.Add(new RuleDiagnostic(RuleDiagnosticSeverity.Error, "DSL001", "Rule string is null or empty."));
                return MakeInvalid(rule, diagnostics);
            }

            var trimmed = rule.Trim();
            var segments = SplitDotSegments(trimmed, diagnostics);

            if (segments.Count == 0)
            {
                diagnostics.Add(new RuleDiagnostic(RuleDiagnosticSeverity.Error, "DSL002", "No segments could be extracted from the rule."));
                return MakeInvalid(rule, diagnostics);
            }

            var @operator = segments[0].ToUpperInvariant();

            if (!_knownOperators.Contains(@operator))
            {
                diagnostics.Add(new RuleDiagnostic(RuleDiagnosticSeverity.Warning, "DSL010",
                    $"Operator '{@operator}' is not in the known-operator set. Rule may be unresolvable."));
            }

            var args = segments.Skip(1).ToList();
            ValidateArgCount(@operator, args, diagnostics);

            var normalized = BuildNormalized(@operator, args);

            return new ParsedRule(
                @operator,
                args.AsReadOnly(),
                rule,
                normalized,
                RuleSyntaxVersion.V1Dot,
                diagnostics.AsReadOnly());
        }

        // ---------------------------------------------------------------
        // Segment splitting — handles quoted segments and numeric decimals
        // ---------------------------------------------------------------

        private static List<string> SplitDotSegments(string text, List<RuleDiagnostic> diagnostics)
        {
            var segments = new List<string>();
            var current = new StringBuilder();
            var inQuotes = false;
            var quoteChar = '\0';

            for (int i = 0; i < text.Length; i++)
            {
                var ch = text[i];

                if (!inQuotes && (ch == '\'' || ch == '"'))
                {
                    inQuotes = true;
                    quoteChar = ch;
                    // Don't include the quote character in the segment text.
                    continue;
                }

                if (inQuotes && ch == quoteChar)
                {
                    inQuotes = false;
                    quoteChar = '\0';
                    continue;
                }

                if (!inQuotes && ch == '.')
                {
                    // Protect decimal numbers: if the current buffer ends with a digit
                    // and the next char is also a digit, keep the dot as part of the number.
                    if (IsDecimalDot(current, text, i))
                    {
                        current.Append(ch);
                        continue;
                    }

                    var segment = current.ToString().Trim();
                    if (segment.Length > 0)
                        segments.Add(segment);
                    current.Clear();
                    continue;
                }

                current.Append(ch);
            }

            if (inQuotes)
            {
                diagnostics.Add(new RuleDiagnostic(RuleDiagnosticSeverity.Error, "DSL003",
                    "Unterminated quoted string in rule."));
            }

            var last = current.ToString().Trim();
            if (last.Length > 0)
                segments.Add(last);

            return segments;
        }

        /// <summary>
        /// Returns true when the dot at <paramref name="dotIndex"/> is a decimal-number separator,
        /// e.g. the buffer ends with a digit and the next character is a digit.
        /// </summary>
        private static bool IsDecimalDot(StringBuilder current, string text, int dotIndex)
        {
            if (current.Length == 0)
                return false;

            var lastChar = current[current.Length - 1];
            if (!char.IsDigit(lastChar))
                return false;

            var nextIndex = dotIndex + 1;
            return nextIndex < text.Length && char.IsDigit(text[nextIndex]);
        }

        // ---------------------------------------------------------------
        // Argument-count validation per operator
        // ---------------------------------------------------------------

        private static void ValidateArgCount(string @operator, List<string> args, List<RuleDiagnostic> diagnostics)
        {
            int count = args.Count;

            switch (@operator)
            {
                case "IF":
                    if (count < 2)
                        diagnostics.Add(new RuleDiagnostic(RuleDiagnosticSeverity.Error, "DSL020",
                            "IF operator requires at least 2 arguments: IF.condition.trueValue[.falseValue]"));
                    break;

                case "COALESCE":
                    if (count < 1)
                        diagnostics.Add(new RuleDiagnostic(RuleDiagnosticSeverity.Error, "DSL021",
                            "COALESCE requires at least 1 argument."));
                    break;

                case "QUERY":
                    if (count < 2)
                        diagnostics.Add(new RuleDiagnostic(RuleDiagnosticSeverity.Error, "DSL022",
                            "QUERY requires at least 2 arguments: QUERY.mode.queryOrEntity[.field][.predicate]"));
                    else
                    {
                        var mode = args[0].ToUpperInvariant();
                        var validModes = new[] { "SCALAR", "FIRST", "EXISTS", "AGGREGATE" };
                        if (!validModes.Contains(mode))
                            diagnostics.Add(new RuleDiagnostic(RuleDiagnosticSeverity.Warning, "DSL023",
                                $"Unknown QUERY mode '{mode}'. Expected one of: {string.Join(", ", validModes)}."));
                    }
                    break;

                case "ADD":
                case "SUBTRACT":
                case "MULTIPLY":
                case "DIVIDE":
                    if (count < 2)
                        diagnostics.Add(new RuleDiagnostic(RuleDiagnosticSeverity.Error, "DSL024",
                            $"{@operator} requires exactly 2 arguments: {{{@operator}}}.value1.value2"));
                    break;

                case "ONERROR":
                case "ONEMPTY":
                    if (count < 2)
                        diagnostics.Add(new RuleDiagnostic(RuleDiagnosticSeverity.Error, "DSL025",
                            $"{@operator} requires 2 arguments: {{{@operator}}}.type.value"));
                    break;
            }
        }

        // ---------------------------------------------------------------
        // Normalization → legacy function-style
        // ---------------------------------------------------------------

        /// <summary>
        /// Converts the operator + args back to a function-style string so existing resolvers
        /// can process it without modification.
        /// e.g. IF.Age>=18.Adult.Minor  →  IF(Age>=18,Adult,Minor)
        /// </summary>
        private static string BuildNormalized(string @operator, List<string> args)
        {
            if (args.Count == 0)
                return @operator;

            // QUERY mode: QUERY.scalar.sql → QUERY(scalar,sql)
            // ONERROR/ONEMPTY pass through as is.
            var quotedArgs = args.Select(EscapeArg);
            return $"{@operator}({string.Join(",", quotedArgs)})";
        }

        /// <summary>
        /// Adds quotes around an argument if it contains a comma or parenthesis
        /// so the normalized form does not confuse the legacy SplitParameters helper.
        /// </summary>
        private static string EscapeArg(string arg)
        {
            if (arg.Contains(',') || arg.Contains('(') || arg.Contains(')'))
                return $"'{arg}'";
            return arg;
        }

        private static ParsedRule MakeInvalid(string rule, List<RuleDiagnostic> diagnostics) =>
            new ParsedRule(string.Empty, Array.Empty<string>(), rule ?? string.Empty,
                string.Empty, RuleSyntaxVersion.Unknown, diagnostics.AsReadOnly());
    }
}
