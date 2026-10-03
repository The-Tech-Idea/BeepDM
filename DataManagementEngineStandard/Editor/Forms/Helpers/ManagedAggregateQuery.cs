using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Extensions;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.Forms.Helpers
{
    internal static class ManagedAggregateQuery
    {
        internal static AppFilterQueryDefinition Build(string entity, IEnumerable<EntityField> fields,
            List<AppFilter> filters, string expression, DataSourceType type, bool bind)
        {
            if (expression == null || expression.Length > 1024) throw new FormatException("Invalid managed aggregate.");
            var match = Regex.Match(expression, @"\A\s*(COUNT|SUM|AVG|MIN|MAX)\s*\(\s*(\*|[A-Za-z_][A-Za-z0-9_]*)\s*\)\s*\z",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
            if (!match.Success) throw new FormatException("Expected a single declared-field aggregate, not SQL source text.");
            var function = match.Groups[1].Value.ToUpperInvariant();
            var field = match.Groups[2].Value;
            if (field == "*" && function != "COUNT") throw new FormatException("Only COUNT supports '*'.");
            if (field != "*")
            {
                var fieldType = new ManagedFilterCompiler(fields).FieldType(field);
                if (function != "COUNT" && (fieldType == typeof(string) || fieldType == typeof(bool) ||
                    fieldType == typeof(DateTime) || fieldType == typeof(DateTimeOffset) || fieldType == typeof(Guid)))
                    throw new FormatException("This aggregate requires a numeric field.");
                field = ManagedCountQuery.Quote(field, type);
            }
            var definition = ManagedCountQuery.Build(entity, filters, type, bind);
            definition.QueryText = "SELECT " + function + "(" + field + ")" + definition.QueryText.Substring("SELECT COUNT(*)".Length);
            return definition;
        }
    }
}
