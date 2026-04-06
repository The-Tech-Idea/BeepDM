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

        public void SetQueryOperator(string blockName, string fieldName, QueryOperator op)
        {
            var dict = _operators.GetOrAdd(blockName, _ => new Dictionary<string, QueryOperator>(StringComparer.OrdinalIgnoreCase));
            dict[fieldName] = op;
        }

        public QueryOperator GetQueryOperator(string blockName, string fieldName)
        {
            if (_operators.TryGetValue(blockName, out var dict) &&
                dict.TryGetValue(fieldName, out var op))
                return op;
            return QueryOperator.Equals; // default
        }

        public void ClearQueryOperators(string blockName)
        {
            _operators.TryRemove(blockName, out _);
        }

        #endregion

        #region Filter Building

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

        public List<AppFilter> ParseWhereClause(string whereClause)
        {
            var filters = new List<AppFilter>();
            if (string.IsNullOrWhiteSpace(whereClause)) return filters;

            // Simple parser: splits on " AND " (case-insensitive)
            var parts = whereClause.Split(new[] { " AND ", " and " }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var filter = ParseCondition(part.Trim());
                if (filter != null)
                    filters.Add(filter);
            }

            return filters;
        }

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

        public List<AppFilter> CombineFiltersAnd(List<AppFilter> a, List<AppFilter> b)
        {
            var combined = new List<AppFilter>();
            if (a != null) combined.AddRange(a);
            if (b != null) combined.AddRange(b);
            return combined;
        }

        #endregion

        #region Query Templates

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

        public QueryTemplateInfo LoadQueryTemplate(string blockName, string templateName)
        {
            if (_templates.TryGetValue(blockName, out var dict) &&
                dict.TryGetValue(templateName, out var template))
                return template;
            return null;
        }

        public IReadOnlyList<QueryTemplateInfo> GetQueryTemplates(string blockName)
        {
            if (_templates.TryGetValue(blockName, out var dict))
                return dict.Values.ToList().AsReadOnly();
            return Array.Empty<QueryTemplateInfo>();
        }

        public bool DeleteQueryTemplate(string blockName, string templateName)
        {
            if (_templates.TryGetValue(blockName, out var dict))
                return dict.Remove(templateName);
            return false;
        }

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
            // Try common operators in order from longest to shortest
            string[] operators = { ">=", "<=", "<>", "!=", "=", ">", "<",
                                    "LIKE", "like", "NOT LIKE", "not like",
                                    "IS NOT NULL", "is not null", "IS NULL", "is null",
                                    "IN", "in", "NOT IN", "not in", "BETWEEN", "between" };

            foreach (var op in operators)
            {
                var idx = condition.IndexOf(op, StringComparison.OrdinalIgnoreCase);
                if (idx > 0)
                {
                    var fieldName = condition.Substring(0, idx).Trim();
                    var filterValue = condition.Substring(idx + op.Length).Trim().Trim('\'', '"');

                    var normalizedOp = op.Trim().ToLowerInvariant();
                    if (normalizedOp == "!=") normalizedOp = "<>";

                    return new AppFilter
                    {
                        FieldName = fieldName,
                        Operator = normalizedOp,
                        FilterValue = filterValue
                    };
                }
            }

            return null;
        }

        #endregion
    }
}
