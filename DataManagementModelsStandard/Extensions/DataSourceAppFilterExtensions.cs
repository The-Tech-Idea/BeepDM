using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Extensions
{
    /// <summary>
    /// Query text and parameter payload produced from AppFilter values.
    /// </summary>
    public sealed class AppFilterQueryDefinition
    {
        public string QueryText { get; set; } = string.Empty;
        public string WhereClause { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        public List<AppFilter> NormalizedFilters { get; set; } = new List<AppFilter>();
    }

    /// <summary>
    /// IDataSource extensions for normalized AppFilter-based entity and query operations.
    /// </summary>
    public static class DataSourceAppFilterExtensions
    {
        private static readonly Dictionary<string, string> OperatorAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["="] = "=",
            ["=="] = "=",
            ["eq"] = "=",
            ["equal"] = "=",
            ["equals"] = "=",
            ["is"] = "=",

            ["!="] = "!=",
            ["<>"] = "!=",
            ["ne"] = "!=",
            ["neq"] = "!=",
            ["notequal"] = "!=",
            ["not equal"] = "!=",
            ["is not"] = "!=",

            [">"] = ">",
            ["gt"] = ">",
            ["greater"] = ">",
            ["greaterthan"] = ">",

            [">="] = ">=",
            ["gte"] = ">=",
            ["greaterorequal"] = ">=",
            ["greater than or equal"] = ">=",

            ["<"] = "<",
            ["lt"] = "<",
            ["less"] = "<",
            ["lessthan"] = "<",

            ["<="] = "<=",
            ["lte"] = "<=",
            ["lessorequal"] = "<=",
            ["less than or equal"] = "<=",

            ["like"] = "like",
            ["contains"] = "contains",
            ["startswith"] = "startswith",
            ["startwith"] = "startswith",
            ["endswith"] = "endswith",
            ["endwith"] = "endswith",

            ["in"] = "in",
            ["notin"] = "not in",
            ["not in"] = "not in",

            ["between"] = "between",
            ["notbetween"] = "not between",
            ["not between"] = "not between",

            ["isnull"] = "is null",
            ["null"] = "is null",
            ["isempty"] = "is null",
            ["is null"] = "is null",

            ["isnotnull"] = "is not null",
            ["notnull"] = "is not null",
            ["is not null"] = "is not null"
        };

        /// <summary>
        /// Normalizes operator aliases to canonical values and returns a cleaned filter list.
        /// </summary>
        public static List<AppFilter> NormalizeFilters(this IDataSource dataSource, IEnumerable<AppFilter> filters)
        {
            if (filters == null)
            {
                return new List<AppFilter>();
            }

            var normalized = new List<AppFilter>();
            foreach (var filter in filters)
            {
                if (filter == null || string.IsNullOrWhiteSpace(filter.FieldName))
                {
                    continue;
                }

                var clone = CloneFilter(filter);
                clone.Operator = NormalizeOperator(clone.Operator);

                if (string.IsNullOrWhiteSpace(clone.Operator))
                {
                    continue;
                }

                normalized.Add(clone);
            }

            return normalized;
        }

        /// <summary>
        /// Fetches entity rows with normalized filters.
        /// </summary>
        public static IEnumerable<object> GetEntityByFilters(this IDataSource dataSource, string entityName, IEnumerable<AppFilter> filters)
        {
            if (dataSource == null)
            {
                throw new ArgumentNullException(nameof(dataSource));
            }

            var normalized = dataSource.NormalizeFilters(filters);
            return dataSource.GetEntity(entityName, normalized);
        }

        /// <summary>
        /// Fetches a page of entity rows with normalized filters.
        /// </summary>
        public static PagedResult GetEntityByFilters(this IDataSource dataSource, string entityName, IEnumerable<AppFilter> filters, int pageNumber, int pageSize)
        {
            if (dataSource == null)
            {
                throw new ArgumentNullException(nameof(dataSource));
            }

            var normalized = dataSource.NormalizeFilters(filters);
            return dataSource.GetEntity(entityName, normalized, pageNumber, pageSize);
        }

        /// <summary>
        /// Asynchronously fetches entity rows with normalized filters.
        /// </summary>
        public static Task<IEnumerable<object>> GetEntityByFiltersAsync(this IDataSource dataSource, string entityName, IEnumerable<AppFilter> filters)
        {
            if (dataSource == null)
            {
                throw new ArgumentNullException(nameof(dataSource));
            }

            var normalized = dataSource.NormalizeFilters(filters);
            return dataSource.GetEntityAsync(entityName, normalized);
        }

        /// <summary>
        /// Builds a parameterized SELECT query and parameter map using datasource-aware delimiters.
        /// </summary>
        public static AppFilterQueryDefinition BuildSelectQueryDefinition(
            this IDataSource dataSource,
            string entityNameOrSelect,
            IEnumerable<AppFilter> filters,
            IEnumerable<string> selectedColumns = null)
        {
            if (dataSource == null)
            {
                throw new ArgumentNullException(nameof(dataSource));
            }

            if (string.IsNullOrWhiteSpace(entityNameOrSelect))
            {
                throw new ArgumentException("Entity name or select statement is required.", nameof(entityNameOrSelect));
            }

            var normalized = dataSource.NormalizeFilters(filters);
            var whereDefinition = BuildWhereClause(dataSource, normalized);
            var queryText = BuildSelectQueryText(entityNameOrSelect, selectedColumns, whereDefinition.WhereClause);

            return new AppFilterQueryDefinition
            {
                QueryText = queryText,
                WhereClause = whereDefinition.WhereClause,
                Parameters = whereDefinition.Parameters,
                NormalizedFilters = normalized
            };
        }

        /// <summary>
        /// Builds a parameterized SELECT query and parameter map without requiring an IDataSource instance.
        /// Use this in helper layers that only know datasource type/delimiter.
        /// </summary>
        public static AppFilterQueryDefinition BuildSelectQueryDefinition(
            string entityNameOrSelect,
            IEnumerable<AppFilter> filters,
            DataSourceType dataSourceType,
            string parameterDelimiter = "@",
            IEnumerable<string> selectedColumns = null)
        {
            if (string.IsNullOrWhiteSpace(entityNameOrSelect))
            {
                throw new ArgumentException("Entity name or select statement is required.", nameof(entityNameOrSelect));
            }

            var normalized = NormalizeFiltersCore(filters);
            var parameterPrefix = string.IsNullOrWhiteSpace(parameterDelimiter)
                ? (dataSourceType == DataSourceType.Oracle ? ":" : "@")
                : parameterDelimiter.Trim();

            var whereDefinition = BuildWhereClause(dataSourceType, parameterPrefix, normalized);
            var queryText = BuildSelectQueryText(entityNameOrSelect, selectedColumns, whereDefinition.WhereClause);

            return new AppFilterQueryDefinition
            {
                QueryText = queryText,
                WhereClause = whereDefinition.WhereClause,
                Parameters = whereDefinition.Parameters,
                NormalizedFilters = normalized
            };
        }

        /// <summary>
        /// Builds only the parameter dictionary from filters (keys without prefix).
        /// </summary>
        public static Dictionary<string, object> BuildFilterParameterDictionary(
            IEnumerable<AppFilter> filters,
            DataSourceType dataSourceType,
            string parameterDelimiter = "@")
        {
            var parameterPrefix = string.IsNullOrWhiteSpace(parameterDelimiter)
                ? (dataSourceType == DataSourceType.Oracle ? ":" : "@")
                : parameterDelimiter.Trim();

            var normalized = NormalizeFiltersCore(filters);
            var result = BuildWhereClause(dataSourceType, parameterPrefix, normalized);
            return result.Parameters;
        }

        /// <summary>
        /// Returns a datasource-aware parameter token prefix.
        /// </summary>
        public static string GetParameterPrefix(this IDataSource dataSource)
        {
            if (dataSource == null)
            {
                return "@";
            }

            if (!string.IsNullOrWhiteSpace(dataSource.ParameterDelimiter))
            {
                return dataSource.ParameterDelimiter.Trim();
            }

            return dataSource.DatasourceType switch
            {
                DataSourceType.Oracle => ":",
                _ => "@"
            };
        }

        private static (string WhereClause, Dictionary<string, object> Parameters) BuildWhereClause(IDataSource dataSource, IReadOnlyList<AppFilter> normalizedFilters)
        {
            var clauses = new List<string>();
            var parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var parameterPrefix = dataSource.GetParameterPrefix();
            var index = 0;

            foreach (var filter in normalizedFilters)
            {
                var quotedField = QuoteIdentifier(filter.FieldName, dataSource.DatasourceType);
                var parameterBase = $"p_{SanitizeName(filter.FieldName)}_{index}";
                var op = filter.Operator;

                switch (op)
                {
                    case "is null":
                    case "is not null":
                        clauses.Add($"{quotedField} {op}");
                        break;

                    case "between":
                    case "not between":
                    {
                        var p0 = parameterBase;
                        var p1 = parameterBase + "_1";
                        clauses.Add($"{quotedField} {(op == "between" ? "BETWEEN" : "NOT BETWEEN")} {parameterPrefix}{p0} AND {parameterPrefix}{p1}");
                        parameters[p0] = ConvertFilterValue(filter.FilterValue, filter.valueType, filter.FieldType);
                        parameters[p1] = ConvertFilterValue(filter.FilterValue1, filter.valueType, filter.FieldType);
                        break;
                    }

                    case "in":
                    case "not in":
                    {
                        var values = SplitCollectionValues(filter.FilterValue).ToList();
                        if (values.Count == 0)
                        {
                            continue;
                        }

                        var placeholders = new List<string>();
                        for (var i = 0; i < values.Count; i++)
                        {
                            var p = $"{parameterBase}_{i}";
                            placeholders.Add($"{parameterPrefix}{p}");
                            parameters[p] = ConvertFilterValue(values[i], filter.valueType, filter.FieldType);
                        }

                        clauses.Add($"{quotedField} {op.ToUpperInvariant()} ({string.Join(", ", placeholders)})");
                        break;
                    }

                    case "contains":
                    case "startswith":
                    case "endswith":
                    case "like":
                    {
                        var p = parameterBase;
                        clauses.Add($"{quotedField} LIKE {parameterPrefix}{p}");
                        parameters[p] = op switch
                        {
                            "contains" => $"%{filter.FilterValue}%",
                            "startswith" => $"{filter.FilterValue}%",
                            "endswith" => $"%{filter.FilterValue}",
                            _ => filter.FilterValue
                        };
                        break;
                    }

                    default:
                    {
                        var p = parameterBase;
                        clauses.Add($"{quotedField} {op} {parameterPrefix}{p}");
                        parameters[p] = ConvertFilterValue(filter.FilterValue, filter.valueType, filter.FieldType);
                        break;
                    }
                }

                index++;
            }

            return (string.Join(" AND ", clauses), parameters);
        }

        private static (string WhereClause, Dictionary<string, object> Parameters) BuildWhereClause(
            DataSourceType dataSourceType,
            string parameterPrefix,
            IReadOnlyList<AppFilter> normalizedFilters)
        {
            var clauses = new List<string>();
            var parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var index = 0;

            foreach (var filter in normalizedFilters)
            {
                var quotedField = QuoteIdentifier(filter.FieldName, dataSourceType);
                var parameterBase = $"p_{SanitizeName(filter.FieldName)}_{index}";
                var op = filter.Operator;

                switch (op)
                {
                    case "is null":
                    case "is not null":
                        clauses.Add($"{quotedField} {op}");
                        break;

                    case "between":
                    case "not between":
                    {
                        var p0 = parameterBase;
                        var p1 = parameterBase + "_1";
                        clauses.Add($"{quotedField} {(op == "between" ? "BETWEEN" : "NOT BETWEEN")} {parameterPrefix}{p0} AND {parameterPrefix}{p1}");
                        parameters[p0] = ConvertFilterValue(filter.FilterValue, filter.valueType, filter.FieldType);
                        parameters[p1] = ConvertFilterValue(filter.FilterValue1, filter.valueType, filter.FieldType);
                        break;
                    }

                    case "in":
                    case "not in":
                    {
                        var values = SplitCollectionValues(filter.FilterValue).ToList();
                        if (values.Count == 0)
                        {
                            continue;
                        }

                        var placeholders = new List<string>();
                        for (var i = 0; i < values.Count; i++)
                        {
                            var p = $"{parameterBase}_{i}";
                            placeholders.Add($"{parameterPrefix}{p}");
                            parameters[p] = ConvertFilterValue(values[i], filter.valueType, filter.FieldType);
                        }

                        clauses.Add($"{quotedField} {op.ToUpperInvariant()} ({string.Join(", ", placeholders)})");
                        break;
                    }

                    case "contains":
                    case "startswith":
                    case "endswith":
                    case "like":
                    {
                        var p = parameterBase;
                        clauses.Add($"{quotedField} LIKE {parameterPrefix}{p}");
                        parameters[p] = op switch
                        {
                            "contains" => $"%{filter.FilterValue}%",
                            "startswith" => $"{filter.FilterValue}%",
                            "endswith" => $"%{filter.FilterValue}",
                            _ => filter.FilterValue
                        };
                        break;
                    }

                    default:
                    {
                        var p = parameterBase;
                        clauses.Add($"{quotedField} {op} {parameterPrefix}{p}");
                        parameters[p] = ConvertFilterValue(filter.FilterValue, filter.valueType, filter.FieldType);
                        break;
                    }
                }

                index++;
            }

            return (string.Join(" AND ", clauses), parameters);
        }

        private static List<AppFilter> NormalizeFiltersCore(IEnumerable<AppFilter> filters)
        {
            if (filters == null)
            {
                return new List<AppFilter>();
            }

            var normalized = new List<AppFilter>();
            foreach (var filter in filters)
            {
                if (filter == null || string.IsNullOrWhiteSpace(filter.FieldName))
                {
                    continue;
                }

                var clone = CloneFilter(filter);
                clone.Operator = NormalizeOperator(clone.Operator);

                if (string.IsNullOrWhiteSpace(clone.Operator))
                {
                    continue;
                }

                normalized.Add(clone);
            }

            return normalized;
        }

        private static string BuildSelectQueryText(string entityNameOrSelect, IEnumerable<string> selectedColumns, string whereClause)
        {
            var working = entityNameOrSelect.Trim();
            var isSelect = Regex.IsMatch(working, @"^\s*select\b", RegexOptions.IgnoreCase);
            var projection = selectedColumns == null || !selectedColumns.Any() ? "*" : string.Join(", ", selectedColumns);

            if (!isSelect)
            {
                if (string.IsNullOrWhiteSpace(whereClause))
                {
                    return $"SELECT {projection} FROM {working}";
                }

                return $"SELECT {projection} FROM {working} WHERE {whereClause}";
            }

            if (string.IsNullOrWhiteSpace(whereClause))
            {
                return working;
            }

            var lower = working.ToLowerInvariant();
            var insertIndex = FindTrailingClauseIndex(lower);
            var head = insertIndex > -1 ? working.Substring(0, insertIndex).TrimEnd() : working;
            var tail = insertIndex > -1 ? working.Substring(insertIndex) : string.Empty;

            if (Regex.IsMatch(head, @"\bwhere\b", RegexOptions.IgnoreCase))
            {
                return $"{head} AND {whereClause}{(string.IsNullOrWhiteSpace(tail) ? string.Empty : " " + tail.TrimStart())}";
            }

            return $"{head} WHERE {whereClause}{(string.IsNullOrWhiteSpace(tail) ? string.Empty : " " + tail.TrimStart())}";
        }

        private static int FindTrailingClauseIndex(string lowerSql)
        {
            var orderBy = lowerSql.IndexOf(" order by ", StringComparison.Ordinal);
            var groupBy = lowerSql.IndexOf(" group by ", StringComparison.Ordinal);
            var having = lowerSql.IndexOf(" having ", StringComparison.Ordinal);

            var indexes = new[] { orderBy, groupBy, having }.Where(i => i >= 0).ToList();
            return indexes.Count == 0 ? -1 : indexes.Min();
        }

        private static AppFilter CloneFilter(AppFilter filter)
        {
            return new AppFilter
            {
                ID = filter.ID,
                GuidID = string.IsNullOrWhiteSpace(filter.GuidID) ? Guid.NewGuid().ToString() : filter.GuidID,
                FieldName = filter.FieldName?.Trim(),
                FilterValue = filter.FilterValue?.Trim(),
                FilterValue1 = filter.FilterValue1?.Trim(),
                Operator = filter.Operator?.Trim(),
                valueType = filter.valueType?.Trim(),
                FieldType = filter.FieldType
            };
        }

        private static string NormalizeOperator(string op)
        {
            if (string.IsNullOrWhiteSpace(op))
            {
                return string.Empty;
            }

            var token = Regex.Replace(op.Trim(), @"\s+", " ");
            if (OperatorAliases.TryGetValue(token, out var normalized))
            {
                return normalized;
            }

            return token.ToLowerInvariant();
        }

        private static string QuoteIdentifier(string identifier, DataSourceType type)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return identifier;
            }

            var clean = identifier.Trim();
            if (clean.Contains(" ") || clean.Contains(".") || clean.Contains("[") || clean.Contains("\"") || clean.Contains("`"))
            {
                return clean;
            }

            return type switch
            {
                DataSourceType.SqlServer or DataSourceType.AzureSQL => $"[{clean}]",
                DataSourceType.Mysql or DataSourceType.MariaDB => $"`{clean}`",
                _ => $"\"{clean}\""
            };
        }

        private static string SanitizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "field";
            }

            var token = Regex.Replace(name, @"\W+", "_");
            if (string.IsNullOrWhiteSpace(token))
            {
                return "field";
            }
            if (char.IsDigit(token[0]))
            {
                token = "_" + token;
            }

            return token;
        }

        private static IEnumerable<string> SplitCollectionValues(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Enumerable.Empty<string>();
            }

            return value
                .Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(v => v.Trim())
                .Where(v => !string.IsNullOrWhiteSpace(v));
        }

        private static object ConvertFilterValue(string rawValue, string valueTypeName, Type fieldType)
        {
            if (rawValue == null)
            {
                return DBNull.Value;
            }

            var targetType = ResolveTargetType(valueTypeName, fieldType);
            if (targetType == typeof(string))
            {
                return rawValue;
            }

            try
            {
                if (targetType == typeof(int) && int.TryParse(rawValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var i))
                {
                    return i;
                }

                if (targetType == typeof(long) && long.TryParse(rawValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var l))
                {
                    return l;
                }

                if (targetType == typeof(short) && short.TryParse(rawValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var s))
                {
                    return s;
                }

                if (targetType == typeof(decimal) && decimal.TryParse(rawValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                {
                    return d;
                }

                if (targetType == typeof(double) && double.TryParse(rawValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var db))
                {
                    return db;
                }

                if (targetType == typeof(float) && float.TryParse(rawValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var f))
                {
                    return f;
                }

                if (targetType == typeof(bool))
                {
                    if (bool.TryParse(rawValue, out var b))
                    {
                        return b;
                    }

                    if (rawValue == "1" || rawValue.Equals("yes", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    if (rawValue == "0" || rawValue.Equals("no", StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }

                if (targetType == typeof(DateTime) && DateTime.TryParse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
                {
                    return dt;
                }

                if (targetType == typeof(Guid) && Guid.TryParse(rawValue, out var g))
                {
                    return g;
                }

                return Convert.ChangeType(rawValue, targetType, CultureInfo.InvariantCulture);
            }
            catch
            {
                return rawValue;
            }
        }

        private static Type ResolveTargetType(string valueTypeName, Type fieldType)
        {
            var selected = fieldType;
            if (selected == null && !string.IsNullOrWhiteSpace(valueTypeName))
            {
                selected = Type.GetType(valueTypeName, throwOnError: false, ignoreCase: true);
                if (selected == null)
                {
                    selected = Type.GetType($"System.{valueTypeName}", throwOnError: false, ignoreCase: true);
                }
            }

            return Nullable.GetUnderlyingType(selected ?? typeof(string)) ?? (selected ?? typeof(string));
        }
    }
}
