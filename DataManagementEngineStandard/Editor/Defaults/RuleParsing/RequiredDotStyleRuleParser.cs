using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Editor.Defaults.Resolvers;
using TheTechIdea.Beep.Editor.Importing;

namespace TheTechIdea.Beep.Editor.Defaults.RuleParsing
{
    // Unlike the public legacy parser, required execution cannot erase an argument or its literal type.
    internal static class RequiredDotStyleRuleParser
    {
        private static readonly HashSet<string> Operators = new(StringComparer.OrdinalIgnoreCase)
        {
            "NOW", "TODAY", "YESTERDAY", "TOMORROW", "CURRENTDATE", "CURRENTTIME", "CURRENTDATETIME",
            "UTCNOW", "UTCTODAY", "CURRENTUTCDATETIME", "ADDDAYS", "ADDHOURS", "ADDMINUTES", "ADDMONTHS",
            "ADDYEARS", "FORMAT", "DATEFORMAT", "STARTOFMONTH", "ENDOFMONTH", "STARTOFYEAR", "ENDOFYEAR",
            "STARTOFWEEK", "ENDOFWEEK", "ADD", "SUBTRACT", "SUB", "MULTIPLY", "MUL", "DIVIDE", "DIV",
            "ROUND", "SEQUENCE", "INCREMENT", "AUTOINCREMENT", "RANDOM", "RANDOMVALUE", "CALCULATE",
            "COMPUTE", "MATH", "EXPRESSION", "EVAL", "IF", "CASE", "CONDITIONAL", "TERNARY", "ISNULL",
            "COALESCE", "AND", "OR", "NOT", "EQ", "NE", "GT", "GTE", "LT", "LTE", "GETENTITY",
            "LOOKUP", "ENTITYLOOKUP", "QUERY", "COUNT", "MAX", "MIN", "SUM", "AVG", "ONERROR", "ONEMPTY",
            "USERNAME", "USERID", "USEREMAIL", "USERLOGIN", "CURRENTUSER", "USERDOMAIN", "USERPROFILE",
            "USERGROUP", "USERROLE", "USERPRINCIPAL", "NEWGUID", "GUID", "UUID", "GENERATEUNIQUEID",
            "MACHINENAME", "HOSTNAME", "VERSION", "APPVERSION", "OSVERSION", "PLATFORM", "PROCESSORCOUNT",
            "WORKINGSET", "TIMESTAMP", "TICKS", "ENVIRONMENTVARIABLE", "ENV", "ENVIRONMENT", "ENVVAR",
            "SYSTEMPATH", "USERPATH", "TEMP", "TEMPPATH", "CONFIGURATIONVALUE", "CONFIG", "APPSETTING",
            "SETTING", "CONNECTIONSTRING", "APPCONFIG", "WEBCONFIG", "PROPERTY", "FIELD", "OBJECTVALUE",
            "PARENTVALUE", "CHILDVALUE", "GETPROPERTY", "GETFIELD", "NESTED", "ARRAYITEM", "DICTVALUE", "RECORD"
        };

        internal static bool TryParse(string expression, string original, out ParsedRule parsed)
        {
            parsed = null;
            var dot = expression.IndexOf('.');
            var paren = expression.IndexOf('(');
            if (dot <= 0 || paren >= 0 && paren < dot) return false;
            var op = expression.Substring(0, dot).Trim().ToUpperInvariant();
            // A plugin's unknown dot dialect is not silently rewritten to shipped semantics.
            if (!Operators.Contains(op)) return false;
            var args = Split(expression.Substring(dot + 1));
            var normalizedArgs = args.Select((arg, index) => NormalizeArgument(op, args, arg, index)).ToArray();
            parsed = new ParsedRule(op, args.AsReadOnly(), original,
                $"{op}({string.Join(",", normalizedArgs)})", RuleSyntaxVersion.V1Dot);
            return true;
        }

        private static List<string> Split(string text)
        {
            var args = new List<string>();
            var start = 0;
            var depth = 0;
            var quote = '\0';
            for (var i = 0; i < text.Length; i++)
            {
                var ch = text[i];
                if (quote != '\0')
                {
                    if (ch == quote) quote = '\0';
                    continue;
                }
                if (ch is '\'' or '"') quote = ch;
                else if (ch == '(') { if (++depth > 32) Deny(); }
                else if (ch == ')') { if (--depth < 0) Deny(); }
                else if (ch == ',' && depth == 0) Deny();
                else if (ch == '.' && depth == 0)
                {
                    Add(args, text.Substring(start, i - start));
                    start = i + 1;
                }
            }
            if (quote != '\0' || depth != 0) Deny();
            Add(args, text.Substring(start));
            return args;
        }

        private static void Add(List<string> args, string argument)
        {
            var text = argument.Trim();
            if (text.Length == 0) Deny();
            if (text[0] is '\'' or '"' && !RequiredBuiltInRule.IsQuotedLiteral(text)) Deny();
            args.Add(text);
        }

        private static string NormalizeArgument(string op, List<string> args, string arg, int index)
        {
            if (!IsGroupedExpression(op, args, index) || !RequiredBuiltInRule.IsQuotedLiteral(arg)) return arg;
            var body = arg.Substring(1, arg.Length - 2);
            // Grouping quotes are syntax only in documented expression/mode/filter positions.
            if (RequiredBuiltInRule.SplitArguments(body).Length != 1) Deny();
            return body;
        }

        private static bool IsGroupedExpression(string op, List<string> args, int index)
        {
            if (op is "AND" or "OR" or "NOT") return true;
            if (index == 0 && op is "IF" or "CONDITIONAL" or "TERNARY" or "CALCULATE" or "COMPUTE" or
                "ADDDAYS" or "ADDHOURS" or "ADDMINUTES" or "ADDMONTHS" or "ADDYEARS" or "FORMAT" or "DATEFORMAT" or "MATH") return true;
            if (op is "COUNT" or "GETENTITY") return index >= 1;
            if (op is "LOOKUP" or "ENTITYLOOKUP" or "MAX" or "MIN" or "SUM" or "AVG") return index >= 2;
            if (op != "QUERY") return false;
            if (index == 0) return true;
            var mode = args[0].Trim('\'', '"').ToUpperInvariant();
            return mode switch
            {
                "SCALAR" => index >= 3,
                "FIRST" or "EXISTS" or "COUNT" => index >= 2,
                "AGGREGATE" => index == 1 || index >= 4,
                _ => false
            };
        }

        private static void Deny() => throw new ImportTransformationException(ImportTransformationStage.Defaults);
    }
}
