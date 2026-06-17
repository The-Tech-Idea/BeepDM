using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Editor.UOWManager.Helpers
{
    /// <summary>
    /// Pure-logic query builder: builds AppFilter lists from field-value dictionaries,
    /// manages per-block per-field operator registrations, and persists/loads query templates.
    /// No UI or WinForms references.
    /// </summary>
    public class QueryBuilderManager : IQueryBuilderManager
    {
        // blockName -> (fieldName -> operator)
        private readonly ConcurrentDictionary<string, Dictionary<string, QueryOperator>> _operators = new(StringComparer.OrdinalIgnoreCase);

        // blockName -> (templateName -> template)
        private readonly ConcurrentDictionary<string, Dictionary<string, QueryTemplateInfo>> _templates = new(StringComparer.OrdinalIgnoreCase);

        #region Operator Registry

        /// <summary>
        /// Registers the comparison operator that should be used for a block field when building filters.
        /// </summary>
        public void SetQueryOperator(string blockName, string fieldName, QueryOperator op)
        {
            var dict = _operators.GetOrAdd(blockName, _ => new Dictionary<string, QueryOperator>(StringComparer.OrdinalIgnoreCase));
            dict[fieldName] = op;
        }

        /// <summary>
        /// Returns the configured comparison operator for a block field.
        /// </summary>
        public QueryOperator GetQueryOperator(string blockName, string fieldName)
        {
            if (_operators.TryGetValue(blockName, out var dict) &&
                dict.TryGetValue(fieldName, out var op))
                return op;
            return QueryOperator.Equals; // default
        }

        /// <summary>
        /// Clears all operator registrations for a block.
        /// </summary>
        public void ClearQueryOperators(string blockName)
        {
            _operators.TryRemove(blockName, out _);
        }

        #endregion

        #region Filter Building

        /// <summary>
        /// Builds an <see cref="AppFilter"/> list from a block-name and field-value dictionary.
        /// </summary>
        public List<AppFilter> BuildFilters(string blockName, Dictionary<string, object> fieldValues)
        {
            var filters = new List<AppFilter>();
            if (fieldValues == null) return filters;

            foreach (var kvp in fieldValues)
            {
                if (kvp.Value == null) continue;

                var op = GetQueryOperator(blockName, kvp.Key);
                var valueStr = Convert.ToString(kvp.Value, CultureInfo.InvariantCulture) ?? string.Empty;

                var filter = new AppFilter
                {
                    FieldName = kvp.Key,
                    Operator = OperatorToString(op),
                    FilterValue = FormatFilterValue(op, valueStr)
                };

                filters.Add(filter);
            }

            return filters;
        }

        /// <summary>
        /// Parses a simple SQL-like WHERE clause into <see cref="AppFilter"/> instances.
        /// Supports =, &lt;&gt;, &gt;, &lt;, &gt;=, &lt;=, LIKE, NOT LIKE, IS NULL, IS NOT NULL,
        /// IN (value1, value2, ...), BETWEEN val1 AND val2, and parameterized placeholders (:1, :name).
        /// </summary>
        public List<AppFilter> ParseWhereClause(string whereClause)
        {
            var filters = new List<AppFilter>();
            if (string.IsNullOrWhiteSpace(whereClause)) return filters;

            // Remove leading WHERE keyword if present
            var clause = whereClause.Trim();
            if (clause.StartsWith("WHERE ", StringComparison.OrdinalIgnoreCase))
                clause = clause.Substring(6).Trim();

            var parts = SplitWhereConditions(clause);
            foreach (var part in parts)
            {
                var filter = ParseCondition(part.Trim());
                if (filter != null)
                    filters.Add(filter);
            }

            return filters;
        }

        /// <summary>
        /// Splits a WHERE clause on AND while respecting parenthesized groups and quoted strings.
        /// </summary>
        private static List<string> SplitWhereConditions(string clause)
        {
            var parts = new List<string>();
            int start = 0, depth = 0;
            bool inSingleQuote = false, inDoubleQuote = false;

            for (int i = 0; i <= clause.Length; i++)
            {
                var c = i < clause.Length ? clause[i] : '\0';
                if (c == '\'' && !inDoubleQuote) inSingleQuote = !inSingleQuote;
                else if (c == '"' && !inSingleQuote) inDoubleQuote = !inDoubleQuote;
                else if (!inSingleQuote && !inDoubleQuote)
                {
                    if (c == '(') depth++;
                    else if (c == ')') depth--;
                }

                var isEnd = i == clause.Length;
                var isAnd = !isEnd && !inSingleQuote && !inDoubleQuote && depth == 0 &&
                            i + 4 <= clause.Length &&
                            clause.Substring(i, 4).Equals(" AND", StringComparison.OrdinalIgnoreCase)
                            && (i + 4 == clause.Length || !char.IsLetterOrDigit(clause[i + 4]));

                if (isEnd || isAnd)
                {
                    var part = clause.Substring(start, i - start).Trim();
                    if (!string.IsNullOrEmpty(part))
                        parts.Add(part);
                    if (isAnd)
                    {
                        start = i + 4;
                        i += 3;
                    }
                }
            }
            return parts;
        }

        /// <summary>
        /// Parses a simple ORDER BY clause into order-related <see cref="AppFilter"/> instances.
        /// </summary>
        public List<AppFilter> ParseOrderByClause(string orderByClause)
        {
            var filters = new List<AppFilter>();
            if (string.IsNullOrWhiteSpace(orderByClause)) return filters;

            var parts = orderByClause.Split(',');
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                var tokens = trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var fieldName = tokens[0];
                var direction = tokens.Length > 1 && tokens[1].Equals("DESC", StringComparison.OrdinalIgnoreCase)
                    ? "DESC" : "ASC";

                filters.Add(new AppFilter
                {
                    FieldName = fieldName,
                    Operator = "OrderBy",
                    FilterValue = direction
                });
            }

            return filters;
        }

        /// <summary>
        /// Concatenates two filter lists using AND semantics.
        /// </summary>
        public List<AppFilter> CombineFiltersAnd(List<AppFilter> a, List<AppFilter> b)
        {
            var combined = new List<AppFilter>();
            if (a != null) combined.AddRange(a);
            if (b != null) combined.AddRange(b);
            return combined;
        }

        #endregion

        #region Query Templates

        /// <summary>
        /// Saves a named query template for a block, including its current operator map.
        /// </summary>
        public void SaveQueryTemplate(string blockName, string templateName, List<AppFilter> filters)
        {
            var dict = _templates.GetOrAdd(blockName, _ => new Dictionary<string, QueryTemplateInfo>(StringComparer.OrdinalIgnoreCase));

            // Capture current operator map for this block
            Dictionary<string, QueryOperator> operatorMap = null;
            if (_operators.TryGetValue(blockName, out var ops))
                operatorMap = new Dictionary<string, QueryOperator>(ops, StringComparer.OrdinalIgnoreCase);

            dict[templateName] = new QueryTemplateInfo
            {
                Name = templateName,
                BlockName = blockName,
                CreatedDate = DateTime.UtcNow,
                Filters = filters != null ? new List<AppFilter>(filters) : new List<AppFilter>(),
                OperatorMap = operatorMap ?? new Dictionary<string, QueryOperator>(StringComparer.OrdinalIgnoreCase)
            };
        }

        /// <summary>
        /// Loads a saved query template by block and template name.
        /// </summary>
        public QueryTemplateInfo LoadQueryTemplate(string blockName, string templateName)
        {
            if (_templates.TryGetValue(blockName, out var dict) &&
                dict.TryGetValue(templateName, out var template))
                return template;
            return null;
        }

        /// <summary>
        /// Returns all saved query templates for a block.
        /// </summary>
        public IReadOnlyList<QueryTemplateInfo> GetQueryTemplates(string blockName)
        {
            if (_templates.TryGetValue(blockName, out var dict))
                return dict.Values.ToList().AsReadOnly();
            return Array.Empty<QueryTemplateInfo>();
        }

        /// <summary>
        /// Deletes a saved query template for a block.
        /// </summary>
        public bool DeleteQueryTemplate(string blockName, string templateName)
        {
            if (_templates.TryGetValue(blockName, out var dict))
                return dict.Remove(templateName);
            return false;
        }

        /// <summary>
        /// Deletes all saved query templates for a block.
        /// </summary>
        public void ClearAllTemplates(string blockName)
        {
            _templates.TryRemove(blockName, out _);
        }

        #endregion

        #region Private Helpers

        private static string OperatorToString(QueryOperator op) => op switch
        {
            QueryOperator.Equals => "=",
            QueryOperator.NotEquals => "<>",
            QueryOperator.GreaterThan => ">",
            QueryOperator.LessThan => "<",
            QueryOperator.GreaterThanOrEqual => ">=",
            QueryOperator.LessThanOrEqual => "<=",
            QueryOperator.Like => "like",
            QueryOperator.NotLike => "not like",
            QueryOperator.In => "in",
            QueryOperator.NotIn => "not in",
            QueryOperator.Between => "between",
            QueryOperator.IsNull => "is null",
            QueryOperator.IsNotNull => "is not null",
            QueryOperator.StartsWith => "like",
            QueryOperator.EndsWith => "like",
            QueryOperator.Contains => "like",
            _ => "="
        };

        private static string FormatFilterValue(QueryOperator op, string value)
        {
            return op switch
            {
                QueryOperator.StartsWith => $"{value}%",
                QueryOperator.EndsWith => $"%{value}",
                QueryOperator.Contains => $"%{value}%",
                _ => value
            };
        }

        private static AppFilter ParseCondition(string condition)
        {
            // Handle IS NULL / IS NOT NULL first
            if (condition.EndsWith("IS NULL", StringComparison.OrdinalIgnoreCase))
            {
                var field = condition.Substring(0, condition.Length - 7).Trim();
                return new AppFilter { FieldName = field, Operator = "is null", FilterValue = "" };
            }
            if (condition.EndsWith("IS NOT NULL", StringComparison.OrdinalIgnoreCase))
            {
                var field = condition.Substring(0, condition.Length - 11).Trim();
                return new AppFilter { FieldName = field, Operator = "is not null", FilterValue = "" };
            }

            // Handle IN (value1, value2, ...) 
            var inIdx = IndexOfOperator(condition, " IN ");
            if (inIdx > 0)
            {
                var fieldName = condition.Substring(0, inIdx).Trim();
                var rest = condition.Substring(inIdx + 4).Trim();
                if (rest.StartsWith("(") && rest.EndsWith(")"))
                {
                    var values = rest.Substring(1, rest.Length - 2);
                    return new AppFilter { FieldName = fieldName, Operator = "in", FilterValue = values.Trim() };
                }
            }

            // Handle BETWEEN val1 AND val2
            var betIdx = IndexOfOperator(condition, " BETWEEN ");
            if (betIdx > 0)
            {
                var fieldName = condition.Substring(0, betIdx).Trim();
                var rest = condition.Substring(betIdx + 8).Trim();
                var andIdx = IndexOfOperator(rest, " AND ");
                if (andIdx > 0)
                {
                    var val1 = rest.Substring(0, andIdx).Trim();
                    var val2 = rest.Substring(andIdx + 5).Trim();
                    return new AppFilter { FieldName = fieldName, Operator = "between", FilterValue = $"{val1},{val2}" };
                }
            }

            // Handle NOT IN
            var notInIdx = IndexOfOperator(condition, " NOT IN ");
            if (notInIdx > 0)
            {
                var fieldName = condition.Substring(0, notInIdx).Trim();
                var rest = condition.Substring(notInIdx + 7).Trim();
                if (rest.StartsWith("(") && rest.EndsWith(")"))
                {
                    var values = rest.Substring(1, rest.Length - 2);
                    return new AppFilter { FieldName = fieldName, Operator = "not in", FilterValue = values.Trim() };
                }
            }

            // Handle NOT LIKE
            var notLikeIdx = IndexOfOperator(condition, " NOT LIKE ");
            if (notLikeIdx > 0)
            {
                var fieldName = condition.Substring(0, notLikeIdx).Trim();
                var filterValue = condition.Substring(notLikeIdx + 10).Trim().Trim('\'', '"');
                return new AppFilter { FieldName = fieldName, Operator = "not like", FilterValue = filterValue };
            }

            // Handle LIKE
            var likeIdx = IndexOfOperator(condition, " LIKE ");
            if (likeIdx > 0)
            {
                var fieldName = condition.Substring(0, likeIdx).Trim();
                var filterValue = condition.Substring(likeIdx + 6).Trim().Trim('\'', '"');
                return new AppFilter { FieldName = fieldName, Operator = "like", FilterValue = filterValue };
            }

            // Try common operators in order from longest to shortest
            string[] operators = { ">=", "<=", "<>", "!=", "=", ">", "<" };

            foreach (var op in operators)
            {
                var idx = IndexOfOperator(condition, $" {op} ");
                if (idx <= 0)
                {
                    // Try without spaces: "Field>=Value"
                    idx = condition.IndexOf(op, StringComparison.Ordinal);
                    if (idx > 0)
                    {
                        var fieldName = condition.Substring(0, idx).Trim();
                        var filterValue = condition.Substring(idx + op.Length).Trim().Trim('\'', '"').TrimEnd(';');
                        var normalizedOp = op == "!=" ? "<>" : op;
                        return new AppFilter { FieldName = fieldName, Operator = normalizedOp, FilterValue = filterValue };
                    }
                    continue;
                }
                var fieldName2 = condition.Substring(0, idx).Trim();
                var filterValue2 = condition.Substring(idx + op.Length + 2).Trim().Trim('\'', '"').TrimEnd(';');
                var normalizedOp2 = op == "!=" ? "<>" : op;
                return new AppFilter { FieldName = fieldName2, Operator = normalizedOp2, FilterValue = filterValue2 };
            }

            // Handle parameterized placeholders: Field = :1 or Field = :name
            // These are preserved as-is so the caller can resolve them
            foreach (var op in operators)
            {
                var idx = condition.IndexOf(op, StringComparison.Ordinal);
                if (idx > 0)
                {
                    var fieldName = condition.Substring(0, idx).Trim();
                    var filterValue = condition.Substring(idx + op.Length).Trim().Trim('\'', '"').TrimEnd(';');
                    return new AppFilter { FieldName = fieldName, Operator = op == "!=" ? "<>" : op, FilterValue = filterValue };
                }
            }

            return null;
        }

        private static int IndexOfOperator(string text, string op)
        {
            var idx = text.IndexOf(op, StringComparison.OrdinalIgnoreCase);
            return idx;
        }

        #endregion
    }
}
