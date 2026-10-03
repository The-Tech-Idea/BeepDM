using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using TheTechIdea.Beep.Extensions;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.Forms.Helpers
{
    internal static class ManagedCountQuery
    {
        internal static AppFilterQueryDefinition Build(string entity, List<AppFilter> filters, DataSourceType type, bool bind)
        {
            var parameters = new Dictionary<string, object>();
            var clauses = new List<string>();
            var prefix = type == DataSourceType.Oracle ? ":" : "@";
            string Value(string raw, Type fieldType)
            {
                var value = ManagedFilterCompiler.TypedValue(raw, fieldType);
                if (!bind) return Literal(value, type);
                var name = "p" + parameters.Count.ToString(CultureInfo.InvariantCulture);
                parameters.Add(name, value);
                return prefix + name;
            }
            foreach (var filter in filters)
            {
                if (filter.Operator == "OrderBy") continue;
                var column = Quote(filter.FieldName, type);
                var op = filter.Operator;
                switch (op)
                {
                    case "is null": case "is not null": clauses.Add(column + " " + op.ToUpperInvariant()); break;
                    case "in": case "not in":
                        var values = DataSourceAppFilterExtensions.ParseCollectionFilterValues(filter.FilterValue);
                        clauses.Add(column + " " + op.ToUpperInvariant() + " (" + string.Join(",", values.Select(v => Value(v, filter.FieldType))) + ")"); break;
                    case "between": case "not between":
                        clauses.Add(column + " " + op.ToUpperInvariant() + " " + Value(filter.FilterValue, filter.FieldType) + " AND " + Value(filter.FilterValue1, filter.FieldType)); break;
                    case "like": case "not like": case "contains": case "startswith": case "endswith":
                        var text = op == "contains" ? "%" + filter.FilterValue + "%" : op == "startswith" ? filter.FilterValue + "%" :
                            op == "endswith" ? "%" + filter.FilterValue : filter.FilterValue;
                        clauses.Add(column + (op == "not like" ? " NOT LIKE " : " LIKE ") + Value(text, typeof(string))); break;
                    case "=": case "!=": case "<": case ">": case "<=": case ">=":
                        clauses.Add(column + " " + op + " " + Value(filter.FilterValue, filter.FieldType)); break;
                    default: throw new FormatException("Unsupported managed count predicate.");
                }
            }
            var where = string.Join(" AND ", clauses.Select(c => "(" + c + ")"));
            return new AppFilterQueryDefinition
            {
                QueryText = "SELECT COUNT(*) FROM " + QuoteQualified(entity, type) + (where.Length == 0 ? "" : " WHERE " + where),
                WhereClause = where, Parameters = parameters, NormalizedFilters = filters
            };
        }

        private static string QuoteQualified(string name, DataSourceType type)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length > 1024 || name.Any(c => char.IsControl(c)))
                throw new FormatException("Invalid managed count entity identity.");
            // Metadata is an entity identity, never arbitrary SELECT/WHERE source text.
            var segments = name.Split('.');
            if (segments.Length > 3 || segments.Any(string.IsNullOrWhiteSpace)) throw new FormatException("Invalid qualified entity identity.");
            return string.Join(".", segments.Select(s => Quote(s, type)));
        }

        internal static string Quote(string name, DataSourceType type)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length > 1024 || name.Any(char.IsControl))
                throw new FormatException("Invalid managed column identity.");
            return type switch
            {
                DataSourceType.SqlServer => "[" + name.Replace("]", "]]") + "]",
                DataSourceType.Mysql or DataSourceType.MariaDB => "`" + name.Replace("`", "``") + "`",
                _ => "\"" + name.Replace("\"", "\"\"") + "\""
            };
        }

        private static string Literal(object value, DataSourceType type)
        {
            if (value == DBNull.Value) return "NULL";
            if (value is bool boolean) return type == DataSourceType.Postgre ? boolean ? "TRUE" : "FALSE" : boolean ? "1" : "0";
            if (value is string || value is Guid || value is DateTime || value is DateTimeOffset)
            {
                var text = value switch
                {
                    DateTime date => date.ToString("O", CultureInfo.InvariantCulture),
                    DateTimeOffset date => date.ToString("O", CultureInfo.InvariantCulture),
                    Guid guid => guid.ToString("D"), _ => (string)value
                };
                // Hex avoids SQL modes/backslash escaping and keeps caller text out of SQL syntax.
                var utf8 = Convert.ToHexString(new UTF8Encoding(false, true).GetBytes(text));
                return type switch
                {
                    DataSourceType.SqlLite => "CAST(X'" + utf8 + "' AS TEXT)",
                    DataSourceType.SqlServer => "CONVERT(nvarchar(max),0x" + Convert.ToHexString(Encoding.Unicode.GetBytes(text)) + ")",
                    DataSourceType.Mysql or DataSourceType.MariaDB => "CONVERT(X'" + utf8 + "' USING utf8mb4)",
                    DataSourceType.Postgre => "convert_from(decode('" + utf8 + "','hex'),'UTF8')",
                    _ => throw new NotSupportedException("This provider requires parameterized managed scalar execution for text/date/GUID filters.")
                };
            }
            if (value is double number) return number.ToString("R", CultureInfo.InvariantCulture);
            if (value is float single) return single.ToString("R", CultureInfo.InvariantCulture);
            return ((IFormattable)value).ToString(null, CultureInfo.InvariantCulture);
        }
    }
}
