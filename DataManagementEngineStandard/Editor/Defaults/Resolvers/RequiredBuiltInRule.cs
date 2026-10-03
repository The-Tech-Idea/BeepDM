using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Editor.Defaults.Interfaces;
using TheTechIdea.Beep.Editor.Importing;

namespace TheTechIdea.Beep.Editor.Defaults.Resolvers
{
    // Required-only grammar for shipped resolvers; custom resolvers own their contracts.
    internal static class RequiredBuiltInRule
    {
        internal static bool MatchesOperator(string rule, IEnumerable<string> tokens)
        {
            var token = GetOperator(rule);
            return tokens.Any(value => string.Equals(value, token, StringComparison.OrdinalIgnoreCase));
        }

        internal static bool IsQuotedLiteral(string text) => text.Length >= 2 &&
            text[0] is '\'' or '"' && text.IndexOf(text[0], 1) == text.Length - 1;

        private static string GetOperator(string rule)
        {
            var text = rule.Trim();
            var end = text.IndexOfAny(new[] { '(', ':' });
            return (end < 0 ? text : text.Substring(0, end)).ToUpperInvariant();
        }

        internal static void Validate(IDefaultValueResolver resolver, string rule)
        {
            var type = resolver.GetType();
            // Do not impose shipped semantics on a plugin subclass/override.
            if (type != typeof(DateTimeResolver) && type != typeof(GuidResolver) &&
                type != typeof(UserContextResolver) && type != typeof(SystemInfoResolver) &&
                type != typeof(EnvironmentResolver) && type != typeof(ConfigurationResolver) &&
                type != typeof(ObjectPropertyResolver) && type != typeof(FormulaResolver) &&
                type != typeof(ExpressionResolver)) return;

            var text = rule.Trim();
            var op = GetOperator(text);
            if (!MatchesOperator(text, resolver.SupportedRuleTypes)) Deny();
            var open = text.IndexOf('(');
            var colon = text.IndexOf(':');
            if (colon >= 0 && (open < 0 || colon < open))
            {
                if (type != typeof(EnvironmentResolver) && type != typeof(ConfigurationResolver)) Deny();
                RequireAtom(text.Substring(colon + 1), nonempty: true);
                return;
            }
            if (open < 0)
            {
                if (type == typeof(DateTimeResolver) && IsDateFunction(op) ||
                    type == typeof(EnvironmentResolver) && op is not ("TEMP" or "TEMPPATH" or "SYSTEMPATH" or "USERPATH") ||
                    type == typeof(ConfigurationResolver) || type == typeof(ObjectPropertyResolver) ||
                    type == typeof(FormulaResolver) || type == typeof(ExpressionResolver)) Deny();
                return;
            }

            if (!text.EndsWith(")", StringComparison.Ordinal) || !RequiredDefaultResolution.HasValidEnvelope(text)) Deny();
            var args = SplitArguments(text.Substring(open + 1, text.Length - open - 2));
            if (type == typeof(DateTimeResolver))
            {
                if (!IsDateFunction(op)) Deny();
                RequireCount(args, 2, 2);
                RequireAtom(args[1], nonempty: true);
            }
            else if (type == typeof(GuidResolver))
            {
                RequireCount(args, 0, op is "GUID" or "UUID" ? 1 : 0);
                if (args.Length == 1)
                {
                    var format = RequireAtom(args[0], nonempty: false);
                    if (format.Length > 0 && (format.Length != 1 || !"NDBPX".Contains(format.ToUpperInvariant()))) Deny();
                }
            }
            else if (type == typeof(SystemInfoResolver)) Deny();
            else if (type == typeof(UserContextResolver))
            {
                if (op is not ("USERPROFILE" or "USERROLE")) Deny();
                RequireCount(args, 1, 1);
                RequireAtom(args[0], nonempty: true);
            }
            else if (type == typeof(EnvironmentResolver))
            {
                if (op is not ("ENV" or "ENVIRONMENT" or "ENVIRONMENTVARIABLE" or "ENVVAR")) Deny();
                RequireCount(args, 1, 2);
                RequireAtom(args[0], nonempty: true);
                if (args.Length == 2 && RequireAtom(args[1], nonempty: true).ToUpperInvariant()
                    is not ("PROCESS" or "USER" or "MACHINE" or "SYSTEM")) Deny();
            }
            else if (type == typeof(ConfigurationResolver))
            {
                RequireCount(args, 1, 1);
                RequireAtom(args[0], nonempty: true);
            }
            else if (type == typeof(ObjectPropertyResolver))
            {
                RequireCount(args, op is "ARRAYITEM" or "DICTVALUE" ? 2 : 1, op is "ARRAYITEM" or "DICTVALUE" ? 2 : 1);
                foreach (var arg in args) RequireAtom(arg, nonempty: true);
            }
            else if (type == typeof(FormulaResolver))
            {
                // Existing sequence/increment handlers are time/hash demonstrations, not durable allocation.
                if (op is "SEQUENCE" or "INCREMENT" or "AUTOINCREMENT") Deny();
                if (op is "CALCULATE" or "COMPUTE") RequireCount(args, 1, 1);
                else if (op is "RANDOM" or "RANDOMVALUE") RequireCount(args, 0, 2);
                else if (op == "ROUND") RequireCount(args, 1, 2);
                else if (op == "MATH")
                {
                    RequireCount(args, 1, 2);
                    var function = RequireAtom(args[0], nonempty: true).ToUpperInvariant();
                    RequireCount(args, function is "PI" or "E" ? 1 : 2, function is "PI" or "E" ? 1 : 2);
                }
                else RequireCount(args, 2, 2);
            }
            else if (type == typeof(ExpressionResolver))
            {
                if (op is "IF" or "CONDITIONAL") RequireCount(args, 2, 3);
                else if (op == "TERNARY") RequireCount(args, 3, 3);
                else if (op == "CASE") RequireCount(args, 3, int.MaxValue);
                else if (op is "COALESCE" or "AND" or "OR") RequireCount(args, 2, int.MaxValue);
                else if (op is "NOT" or "EXPRESSION" or "EVAL") RequireCount(args, 1, 1);
                else RequireCount(args, 2, 2);
            }
        }

        private static bool IsDateFunction(string op) => op is
            "ADDDAYS" or "ADDHOURS" or "ADDMINUTES" or "ADDMONTHS" or "ADDYEARS" or "FORMAT" or "DATEFORMAT";

        private static void RequireCount(string[] args, int min, int max)
        {
            if (args.Length < min || args.Length > max) Deny();
        }

        internal static string RequireAtom(string argument, bool nonempty)
        {
            var text = argument.Trim();
            if (text.Length > 0 && text[0] is '\'' or '"')
            {
                if (text.Length < 2 || text[text.Length - 1] != text[0] || text.Substring(1, text.Length - 2).Contains(text[0])) Deny();
                text = text.Substring(1, text.Length - 2);
            }
            else if (text.IndexOfAny(new[] { '\'', '"', '(', ')', ',' }) >= 0) Deny();
            if (nonempty && string.IsNullOrWhiteSpace(text) || text.Any(char.IsControl)) Deny();
            return text;
        }

        internal static string[] SplitArguments(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return Array.Empty<string>();
            var args = new List<string>();
            var start = 0;
            var depth = 0;
            var quote = '\0';
            for (var i = 0; i < content.Length; i++)
            {
                var ch = content[i];
                if (quote != '\0')
                {
                    if (ch == quote) quote = '\0';
                    continue;
                }
                if (ch is '\'' or '"') quote = ch;
                else if (ch == '(') { if (++depth > 32) Deny(); }
                else if (ch == ')') { if (--depth < 0) Deny(); }
                else if (ch == ',' && depth == 0)
                {
                    AddArgument(args, content.Substring(start, i - start));
                    start = i + 1;
                }
            }
            if (quote != '\0' || depth != 0) Deny();
            AddArgument(args, content.Substring(start));
            return args.ToArray();
        }

        private static void AddArgument(List<string> args, string argument)
        {
            var text = argument.Trim();
            if (text.Length == 0) Deny();
            if (text[0] is '\'' or '"')
            {
                var end = text.IndexOf(text[0], 1);
                if (end < 0) Deny();
                // Comparisons/arithmetic may start with a quoted operand; adjacent literals may not.
                var remainder = text.Substring(end + 1).TrimStart();
                if (remainder.Length > 0 && "=<>!+-*/".IndexOf(remainder[0]) < 0) Deny();
            }
            args.Add(text);
        }

        private static void Deny() => throw new ImportTransformationException(ImportTransformationStage.Defaults);
    }
}
