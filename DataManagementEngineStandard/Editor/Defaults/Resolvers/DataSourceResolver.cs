using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Editor.Defaults.Resolvers
{
    /// <summary>
    /// Resolver for getting values from data sources using GetEntity and GetScalar operations
    /// </summary>
    public class DataSourceResolver : BaseDefaultValueResolver
    {
        public DataSourceResolver(IDMEEditor editor) : base(editor) { }

        public override string ResolverName => "DataSource";

        public override IEnumerable<string> SupportedRuleTypes => new[]
        {
            "GETENTITY", "GETSCALAR", "LOOKUP", "ENTITYLOOKUP", 
            "QUERY", "SQLQUERY", "COUNT", "MAX", "MIN", "SUM", "AVG"
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
                    _ when upperRule.StartsWith("GETENTITY(") => HandleGetEntity(rule, parameters),
                    _ when upperRule.StartsWith("GETSCALAR(") => HandleGetScalar(rule, parameters),
                    _ when upperRule.StartsWith("LOOKUP(") || upperRule.StartsWith("ENTITYLOOKUP(") => HandleLookup(rule, parameters),
                    _ when upperRule.StartsWith("QUERY(") || upperRule.StartsWith("SQLQUERY(") => HandleQuery(rule, parameters),
                    _ when upperRule.StartsWith("COUNT(") => HandleAggregateFunction(rule, parameters, "COUNT"),
                    _ when upperRule.StartsWith("MAX(") => HandleAggregateFunction(rule, parameters, "MAX"),
                    _ when upperRule.StartsWith("MIN(") => HandleAggregateFunction(rule, parameters, "MIN"),
                    _ when upperRule.StartsWith("SUM(") => HandleAggregateFunction(rule, parameters, "SUM"),
                    _ when upperRule.StartsWith("AVG(") => HandleAggregateFunction(rule, parameters, "AVG"),
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

            var upperRule = rule.ToUpperInvariant().Trim();
            
            return upperRule.StartsWith("GETENTITY(") ||
                   upperRule.StartsWith("GETSCALAR(") ||
                   upperRule.StartsWith("LOOKUP(") ||
                   upperRule.StartsWith("ENTITYLOOKUP(") ||
                   upperRule.StartsWith("QUERY(") ||
                   upperRule.StartsWith("SQLQUERY(") ||
                   upperRule.StartsWith("COUNT(") ||
                   upperRule.StartsWith("MAX(") ||
                   upperRule.StartsWith("MIN(") ||
                   upperRule.StartsWith("SUM(") ||
                   upperRule.StartsWith("AVG(");
        }

        public override IEnumerable<string> GetExamples()
        {
            return new[]
            {
                "GETENTITY(Users, FirstName='John') - Get first matching entity",
                "GETSCALAR(SELECT COUNT(*) FROM Users) - Execute scalar query",
                "LOOKUP(Users, FirstName, ID=123) - Lookup field value by ID",
                "ENTITYLOOKUP(Products, Name, CategoryID=5) - Lookup product name by category",
                "QUERY(SELECT TOP 1 Email FROM Users WHERE Active=1) - Execute custom query",
                "COUNT(Orders, CustomerID=123) - Count records with condition",
                "MAX(Orders, OrderDate, CustomerID=123) - Get maximum date for customer",
                "MIN(Products, Price, CategoryID=1) - Get minimum price in category",
                "SUM(OrderItems, Quantity, OrderID=456) - Sum quantities for order",
                "AVG(Products, Rating, CategoryID=2) - Average rating in category"
            };
        }

        #region Private Handler Methods

        private object HandleGetEntity(string rule, IPassedArgs parameters)
        {
            try
            {
                var content = ExtractParenthesesContent(rule);
                var parts = SplitParameters(content);

                if (parts.Length < 1)
                {
                    LogError("GETENTITY requires at least entity name parameter");
                    return null;
                }

                var entityName = RemoveQuotes(parts[0].Trim());
                var filters = new List<AppFilter>();

                // Parse filter conditions (format: Field=Value or Field='Value')
                for (int i = 1; i < parts.Length; i++)
                {
                    var filter = ParseFilterCondition(parts[i].Trim());
                    if (filter != null)
                        filters.Add(filter);
                }

                var dataSource = GetDataSourceFromParameters(parameters);
                if (dataSource == null)
                {
                    LogError("No data source available for GETENTITY operation");
                    return null;
                }

                var entities = dataSource.GetEntity(entityName, filters);
                return entities?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                LogError($"Error in HandleGetEntity", ex);
                return null;
            }
        }

        private object HandleGetScalar(string rule, IPassedArgs parameters)
        {
            try
            {
                var content = ExtractParenthesesContent(rule);
                var query = RemoveQuotes(content.Trim());

                if (string.IsNullOrWhiteSpace(query))
                {
                    LogError("GETSCALAR requires a query parameter");
                    return null;
                }

                var dataSource = GetDataSourceFromParameters(parameters);
                if (dataSource == null)
                {
                    LogError("No data source available for GETSCALAR operation");
                    return null;
                }

                return dataSource.GetScalar(query);
            }
            catch (Exception ex)
            {
                LogError($"Error in HandleGetScalar", ex);
                return null;
            }
        }

        private object HandleLookup(string rule, IPassedArgs parameters)
        {
            try
            {
                var content = ExtractParenthesesContent(rule);
                var parts = SplitParameters(content);

                if (parts.Length < 3)
                {
                    LogError("LOOKUP requires at least 3 parameters: entityName, fieldToReturn, filterCondition");
                    return null;
                }

                var entityName = RemoveQuotes(parts[0].Trim());
                var fieldToReturn = RemoveQuotes(parts[1].Trim());
                var filters = new List<AppFilter>();

                // Parse filter conditions
                for (int i = 2; i < parts.Length; i++)
                {
                    var filter = ParseFilterCondition(parts[i].Trim());
                    if (filter != null)
                        filters.Add(filter);
                }

                var dataSource = GetDataSourceFromParameters(parameters);
                if (dataSource == null)
                {
                    LogError("No data source available for LOOKUP operation");
                    return null;
                }

                var entities = dataSource.GetEntity(entityName, filters);
                var firstEntity = entities?.FirstOrDefault();

                if (firstEntity != null)
                {
                    // Try to get the field value using reflection
                    var entityType = firstEntity.GetType();
                    var property = entityType.GetProperty(fieldToReturn);
                    if (property != null && property.CanRead)
                    {
                        return property.GetValue(firstEntity);
                    }

                    // Try dictionary access if it's a dictionary
                    if (firstEntity is Dictionary<string, object> dict && dict.ContainsKey(fieldToReturn))
                    {
                        return dict[fieldToReturn];
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                LogError($"Error in HandleLookup", ex);
                return null;
            }
        }

        private object HandleQuery(string rule, IPassedArgs parameters)
        {
            try
            {
                var content = ExtractParenthesesContent(rule);
                var query = RemoveQuotes(content.Trim());

                if (string.IsNullOrWhiteSpace(query))
                {
                    LogError("QUERY requires a query parameter");
                    return null;
                }

                var dataSource = GetDataSourceFromParameters(parameters);
                if (dataSource == null)
                {
                    LogError("No data source available for QUERY operation");
                    return null;
                }

                var results = dataSource.RunQuery(query);
                return results?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                LogError($"Error in HandleQuery", ex);
                return null;
            }
        }

        private object HandleAggregateFunction(string rule, IPassedArgs parameters, string function)
        {
            try
            {
                var content = ExtractParenthesesContent(rule);
                var parts = SplitParameters(content);

                if (parts.Length < 1)
                {
                    LogError($"{function} requires at least entity name parameter");
                    return null;
                }

                var entityName = RemoveQuotes(parts[0].Trim());
                string fieldName = null;
                var filters = new List<AppFilter>();

                // For functions that need a field name (MAX, MIN, SUM, AVG)
                int startIndex = 1;
                if (function != "COUNT" && parts.Length > 1)
                {
                    fieldName = RemoveQuotes(parts[1].Trim());
                    startIndex = 2;
                }

                // Parse filter conditions
                for (int i = startIndex; i < parts.Length; i++)
                {
                    var filter = ParseFilterCondition(parts[i].Trim());
                    if (filter != null)
                        filters.Add(filter);
                }

                var dataSource = GetDataSourceFromParameters(parameters);
                if (dataSource == null)
                {
                    LogError($"No data source available for {function} operation");
                    return null;
                }

                // Build and execute aggregate query
                var whereClause = BuildWhereClause(filters);
                var query = BuildAggregateQuery(function, entityName, fieldName, whereClause);

                return dataSource.GetScalar(query);
            }
            catch (Exception ex)
            {
                LogError($"Error in Handle{function}", ex);
                return null;
            }
        }

        #endregion

        #region Helper Methods

        private IDataSource GetDataSourceFromParameters(IPassedArgs parameters)
        {
            // Try to get data source from parameters
            var dataSource = GetParameterValue<IDataSource>(parameters, "DataSource");
            if (dataSource != null)
                return dataSource;

            // Try to get by name
            var dataSourceName = GetParameterValue<string>(parameters, "DatasourceName");
            if (!string.IsNullOrWhiteSpace(dataSourceName))
            {
                return GetDataSource(parameters, dataSourceName);
            }

            return null;
        }

        private AppFilter ParseFilterCondition(string condition)
        {
            if (string.IsNullOrWhiteSpace(condition))
                return null;

            try
            {
                // Handle different operators (=, !=, <, >, <=, >=)
                var operators = new[] { "!=", "<=", ">=", "=", "<", ">" };
                
                foreach (var op in operators)
                {
                    var index = condition.IndexOf(op);
                    if (index > 0)
                    {
                        var fieldName = condition.Substring(0, index).Trim();
                        var value = condition.Substring(index + op.Length).Trim();
                        value = RemoveQuotes(value);

                        return new AppFilter
                        {
                            FieldName = fieldName,
                            Operator = op,
                            FilterValue = value
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                LogWarning($"Could not parse filter condition '{condition}': {ex.Message}");
            }

            return null;
        }

        private string BuildWhereClause(List<AppFilter> filters)
        {
            if (filters == null || !filters.Any())
                return string.Empty;

            var conditions = filters.Select(f => $"{f.FieldName} {f.Operator} '{f.FilterValue}'");
            return "WHERE " + string.Join(" AND ", conditions);
        }

        private string BuildAggregateQuery(string function, string entityName, string fieldName, string whereClause)
        {
            var field = string.IsNullOrWhiteSpace(fieldName) ? "*" : fieldName;
            var query = $"SELECT {function}({field}) FROM {entityName}";
            
            if (!string.IsNullOrWhiteSpace(whereClause))
            {
                query += " " + whereClause;
            }

            return query;
        }

        #endregion
    }
}