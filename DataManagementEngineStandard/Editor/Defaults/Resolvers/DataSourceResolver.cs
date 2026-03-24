using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Defaults.Attributes;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Editor.Defaults.Resolvers
{
    /// <summary>
    /// Resolver for getting default values from data sources.
    ///
    /// All operations use <see cref="IDataSource.GetEntity"/> + <see cref="AppFilter"/> exclusively
    /// so they work across every IDataSource implementation — not just RDBMS.
    /// Aggregates (COUNT/MAX/MIN/SUM/AVG) are computed in memory via LINQ on the result set.
    /// Filter values prefixed with '@' are bound from runtime context parameters.
    /// </summary>
    [DefaultResolver("DataSource", "Data Source Resolver",
        Description = "Resolves values by querying data sources using IDataSource.GetEntity. Supports lookups, aggregates, and scalar queries.",
        SupportedTokens = "GETENTITY,LOOKUP,ENTITYLOOKUP,QUERY,COUNT,MAX,MIN,SUM,AVG")]
    public class DataSourceResolver : BaseDefaultValueResolver
    {
        public DataSourceResolver(IDMEEditor editor) : base(editor) { }

        public override string ResolverName => "DataSource";

        public override IEnumerable<string> SupportedRuleTypes => new[]
        {
            "GETENTITY", "LOOKUP", "ENTITYLOOKUP",
            "QUERY",
            "COUNT", "MAX", "MIN", "SUM", "AVG"
        };

        public override object ResolveValue(string rule, IPassedArgs parameters)
        {
            if (string.IsNullOrWhiteSpace(rule))
                return null;

            var upperRule = rule.ToUpperInvariant().Trim();

            try
            {
                return upperRule switch
                {
                    _ when upperRule.StartsWith("GETENTITY(")    => HandleGetEntity(rule, parameters),
                    _ when upperRule.StartsWith("LOOKUP(")
                        || upperRule.StartsWith("ENTITYLOOKUP(") => HandleLookup(rule, parameters),
                    _ when upperRule.StartsWith("QUERY(")        => HandleQuery(rule, parameters),
                    _ when upperRule.StartsWith("COUNT(")        => HandleAggregate(rule, parameters, "COUNT"),
                    _ when upperRule.StartsWith("MAX(")          => HandleAggregate(rule, parameters, "MAX"),
                    _ when upperRule.StartsWith("MIN(")          => HandleAggregate(rule, parameters, "MIN"),
                    _ when upperRule.StartsWith("SUM(")          => HandleAggregate(rule, parameters, "SUM"),
                    _ when upperRule.StartsWith("AVG(")          => HandleAggregate(rule, parameters, "AVG"),
                    _ => null
                };
            }
            catch (Exception ex)
            {
                LogError($"Error resolving data source rule '{rule}'", ex);
                return null;
            }
        }

        public override bool CanHandle(string rule)
        {
            if (string.IsNullOrWhiteSpace(rule))
                return false;

            var u = rule.ToUpperInvariant().Trim();
            return u.StartsWith("GETENTITY(")   ||
                   u.StartsWith("LOOKUP(")       ||
                   u.StartsWith("ENTITYLOOKUP(") ||
                   u.StartsWith("QUERY(")        ||
                   u.StartsWith("COUNT(")        ||
                   u.StartsWith("MAX(")          ||
                   u.StartsWith("MIN(")          ||
                   u.StartsWith("SUM(")          ||
                   u.StartsWith("AVG(");
        }

        public override IEnumerable<string> GetExamples()
        {
            return new[]
            {
                "GETENTITY(Users, IsActive=true) - First entity matching filters",
                "LOOKUP(Users, Email, ID=@UserID) - Field value from first match; @Param bound from context",
                "ENTITYLOOKUP(Products, Name, CategoryID=5) - Same as LOOKUP",
                "QUERY(scalar, Users, Email, IsActive=true) - Single field from first match",
                "QUERY(first, Orders, CustomerID=@CustomerID) - First matching entity",
                "QUERY(exists, Users, IsActive=true) - true/false if any record matches",
                "QUERY(count, Orders, Status=Pending) - Count of matching records",
                "QUERY(aggregate, MAX, Orders, OrderDate, CustomerID=@CustomerID) - Aggregate a field",
                "COUNT(Orders, CustomerID=123) - Count matching records",
                "MAX(Orders, OrderDate, CustomerID=123) - Maximum field value across matches",
                "MIN(Products, Price, CategoryID=1) - Minimum field value across matches",
                "SUM(OrderItems, Quantity, OrderID=456) - Sum field values across matches",
                "AVG(Products, Rating, CategoryID=2) - Average field value across matches"
            };
        }

        // ---------------------------------------------------------------
        // Handlers
        // ---------------------------------------------------------------

        /// <summary>
        /// GETENTITY(entityName[, field=value ...])
        /// Returns the first matching entity object.
        /// </summary>
        private object HandleGetEntity(string rule, IPassedArgs parameters)
        {
            var parts = SplitParameters(ExtractParenthesesContent(rule));
            if (parts.Length < 1) { LogError("GETENTITY requires at least: entityName"); return null; }

            var entityName = RemoveQuotes(parts[0].Trim());
            var filters    = BuildFilters(parts, 1, parameters);
            var ds         = GetDataSourceFromParameters(parameters);
            if (ds == null) { LogError("No data source available for GETENTITY"); return null; }

            return ds.GetEntity(entityName, filters)?.FirstOrDefault();
        }

        /// <summary>
        /// LOOKUP(entityName, fieldToReturn[, field=value ...])
        /// ENTITYLOOKUP(entityName, fieldToReturn[, field=value ...])
        /// Returns the value of a specific field from the first matching entity.
        /// </summary>
        private object HandleLookup(string rule, IPassedArgs parameters)
        {
            var parts = SplitParameters(ExtractParenthesesContent(rule));
            if (parts.Length < 2) { LogError("LOOKUP requires at least: entityName, fieldToReturn"); return null; }

            var entityName   = RemoveQuotes(parts[0].Trim());
            var fieldToReturn = RemoveQuotes(parts[1].Trim());
            var filters      = BuildFilters(parts, 2, parameters);

            var ds = GetDataSourceFromParameters(parameters);
            if (ds == null) { LogError("No data source available for LOOKUP"); return null; }

            var first = ds.GetEntity(entityName, filters)?.FirstOrDefault();
            return first == null ? null : ExtractFieldValue(first, fieldToReturn);
        }

        /// <summary>
        /// QUERY(mode, ...)  — universal entity-based query using AppFilter.
        ///
        /// Modes:
        ///   scalar    QUERY(scalar, entityName, fieldName[, filter...])
        ///             → value of fieldName from the first matching record
        ///   first     QUERY(first, entityName[, filter...])
        ///             → first matching entity object
        ///   exists    QUERY(exists, entityName[, filter...])
        ///             → true when at least one record matches, false otherwise
        ///   count     QUERY(count, entityName[, filter...])
        ///             → number of matching records
        ///   aggregate QUERY(aggregate, func, entityName, fieldName[, filter...])
        ///             → MAX / MIN / SUM / AVG over fieldName across matching records
        /// </summary>
        private object HandleQuery(string rule, IPassedArgs parameters)
        {
            var parts = SplitParameters(ExtractParenthesesContent(rule));
            if (parts.Length < 2) { LogError("QUERY requires at least: mode, entityName"); return null; }

            var mode = parts[0].Trim().ToUpperInvariant();

            switch (mode)
            {
                case "SCALAR":
                {
                    if (parts.Length < 3) { LogError("QUERY scalar: mode, entityName, fieldName required"); return null; }
                    var entity  = RemoveQuotes(parts[1].Trim());
                    var field   = RemoveQuotes(parts[2].Trim());
                    var filters = BuildFilters(parts, 3, parameters);
                    var ds = GetDataSourceFromParameters(parameters);
                    if (ds == null) { LogError("No data source available"); return null; }
                    var first = ds.GetEntity(entity, filters)?.FirstOrDefault();
                    return first == null ? null : ExtractFieldValue(first, field);
                }

                case "FIRST":
                {
                    var entity  = RemoveQuotes(parts[1].Trim());
                    var filters = BuildFilters(parts, 2, parameters);
                    var ds = GetDataSourceFromParameters(parameters);
                    if (ds == null) { LogError("No data source available"); return null; }
                    return ds.GetEntity(entity, filters)?.FirstOrDefault();
                }

                case "EXISTS":
                {
                    var entity  = RemoveQuotes(parts[1].Trim());
                    var filters = BuildFilters(parts, 2, parameters);
                    var ds = GetDataSourceFromParameters(parameters);
                    if (ds == null) { LogError("No data source available"); return null; }
                    return ds.GetEntity(entity, filters)?.Any() == true;
                }

                case "COUNT":
                {
                    var entity  = RemoveQuotes(parts[1].Trim());
                    var filters = BuildFilters(parts, 2, parameters);
                    var ds = GetDataSourceFromParameters(parameters);
                    if (ds == null) { LogError("No data source available"); return null; }
                    return ds.GetEntity(entity, filters)?.Count() ?? 0;
                }

                case "AGGREGATE":
                {
                    if (parts.Length < 4) { LogError("QUERY aggregate: mode, func, entityName, fieldName required"); return null; }
                    var func    = parts[1].Trim().ToUpperInvariant();
                    var entity  = RemoveQuotes(parts[2].Trim());
                    var field   = RemoveQuotes(parts[3].Trim());
                    var filters = BuildFilters(parts, 4, parameters);
                    return ComputeAggregate(func, entity, field, filters, parameters);
                }

                default:
                    LogError($"Unknown QUERY mode '{mode}'. Expected: scalar, first, exists, count, aggregate");
                    return null;
            }
        }

        /// <summary>
        /// COUNT(entityName[, filter...])
        /// MAX / MIN / SUM / AVG (entityName, fieldName[, filter...])
        /// </summary>
        private object HandleAggregate(string rule, IPassedArgs parameters, string function)
        {
            var parts = SplitParameters(ExtractParenthesesContent(rule));
            if (parts.Length < 1) { LogError($"{function} requires at least: entityName"); return null; }

            var entityName = RemoveQuotes(parts[0].Trim());
            string fieldName = null;
            int filterStart = 1;

            if (function != "COUNT" && parts.Length > 1)
            {
                fieldName   = RemoveQuotes(parts[1].Trim());
                filterStart = 2;
            }

            var filters = BuildFilters(parts, filterStart, parameters);
            return ComputeAggregate(function, entityName, fieldName, filters, parameters);
        }

        // ---------------------------------------------------------------
        // Aggregate core — always via GetEntity + LINQ
        // ---------------------------------------------------------------

        private object ComputeAggregate(string function, string entityName, string fieldName,
            List<AppFilter> filters, IPassedArgs parameters)
        {
            var ds = GetDataSourceFromParameters(parameters);
            if (ds == null) { LogError("No data source available"); return null; }

            var rows = ds.GetEntity(entityName, filters)?.ToList();
            if (rows == null || rows.Count == 0) return null;

            if (function == "COUNT")
                return rows.Count;

            if (string.IsNullOrWhiteSpace(fieldName))
            {
                LogError($"{function} requires a field name");
                return null;
            }

            var values = rows
                .Select(r => ExtractFieldValue(r, fieldName))
                .Where(v => v != null)
                .Select(v => TryToDouble(v))
                .Where(v => v.HasValue)
                .Select(v => v.Value)
                .ToList();

            if (!values.Any()) return null;

            return function switch
            {
                "MAX" => (object)values.Max(),
                "MIN" => (object)values.Min(),
                "SUM" => (object)values.Sum(),
                "AVG" => (object)values.Average(),
                _     => null
            };
        }

        // ---------------------------------------------------------------
        // Filter building
        // ---------------------------------------------------------------

        private List<AppFilter> BuildFilters(string[] parts, int startIndex, IPassedArgs parameters)
        {
            var filters = new List<AppFilter>();
            for (int i = startIndex; i < parts.Length; i++)
            {
                var f = ParseFilterCondition(parts[i].Trim(), parameters);
                if (f != null) filters.Add(f);
            }
            return filters;
        }

        /// <summary>
        /// Parses "Field=Value", "Field!=Value", etc.
        /// Values prefixed with '@' are resolved from runtime context.
        /// </summary>
        private AppFilter ParseFilterCondition(string condition, IPassedArgs parameters)
        {
            if (string.IsNullOrWhiteSpace(condition)) return null;

            // Longest-match first to avoid '=' swallowing '!=' or '>='
            var operators = new[] { "!=", "<=", ">=", "=", "<", ">" };

            foreach (var op in operators)
            {
                var idx = condition.IndexOf(op, StringComparison.Ordinal);
                if (idx <= 0) continue;

                var fieldName = condition.Substring(0, idx).Trim();
                var rawValue  = RemoveQuotes(condition.Substring(idx + op.Length).Trim());
                var bound     = BindParameter(rawValue, parameters);

                return new AppFilter { FieldName = fieldName, Operator = op, FilterValue = bound };
            }

            return null;
        }

        /// <summary>
        /// Resolves '@Param' tokens against runtime context.
        /// Checks (in order): named Objects list → record fields → IPassedArgs string slots.
        /// Returns the original string when no binding is found.
        /// </summary>
        private string BindParameter(string value, IPassedArgs parameters)
        {
            if (string.IsNullOrWhiteSpace(value) || value[0] != '@' || parameters == null)
                return value;

            var paramName = value.Substring(1); // strip '@'

            // 1. Named item in Objects list
            if (parameters.Objects != null)
            {
                var match = parameters.Objects.FirstOrDefault(o =>
                    string.Equals(o.Name, paramName, StringComparison.OrdinalIgnoreCase));
                if (match?.obj != null)
                    return match.obj.ToString();
            }

            // 2. Field on the record object (first Object entry)
            if (parameters.Objects?.Count > 0)
            {
                var record = parameters.Objects[0]?.obj;
                if (record != null)
                {
                    var fieldVal = ExtractFieldValue(record, paramName);
                    if (fieldVal != null) return fieldVal.ToString();
                }
            }

            // 3. Conventional string slots
            if (string.Equals(paramName, "FieldName", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(parameters.ParameterString1))
                return parameters.ParameterString1;

            LogWarning($"Parameter '@{paramName}' not found in context — used as literal.");
            return value;
        }

        // ---------------------------------------------------------------
        // Field extraction — handles POCO, Dictionary<string,object>, IDictionary
        // ---------------------------------------------------------------

        private object ExtractFieldValue(object entity, string fieldName)
        {
            if (entity == null || string.IsNullOrWhiteSpace(fieldName)) return null;

            // Schema-less: Dictionary<string, object>
            if (entity is IDictionary<string, object> typed)
            {
                var key = typed.Keys.FirstOrDefault(k =>
                    string.Equals(k, fieldName, StringComparison.OrdinalIgnoreCase));
                return key != null ? typed[key] : null;
            }

            // Non-generic IDictionary
            if (entity is IDictionary dict)
            {
                foreach (var k in dict.Keys)
                    if (string.Equals(k?.ToString(), fieldName, StringComparison.OrdinalIgnoreCase))
                        return dict[k];
                return null;
            }

            // Typed POCO — reflection
            var prop = entity.GetType().GetProperty(fieldName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            return prop?.GetValue(entity);
        }

        // ---------------------------------------------------------------
        // Utilities
        // ---------------------------------------------------------------

        private IDataSource GetDataSourceFromParameters(IPassedArgs parameters)
        {
            var ds = GetParameterValue<IDataSource>(parameters, "DataSource");
            if (ds != null) return ds;

            var name = GetParameterValue<string>(parameters, "DatasourceName");
            if (!string.IsNullOrWhiteSpace(name))
                return GetDataSource(parameters, name);

            return null;
        }

        private static double? TryToDouble(object value)
        {
            try { return Convert.ToDouble(value); }
            catch { return null; }
        }
    }
}