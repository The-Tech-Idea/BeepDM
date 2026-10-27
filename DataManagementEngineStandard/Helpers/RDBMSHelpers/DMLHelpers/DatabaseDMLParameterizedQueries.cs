using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Extensions;

namespace TheTechIdea.Beep.Helpers.RDBMSHelpers.DMLHelpers
{
    /// <summary>
    /// Helper class for generating parameterized queries with proper parameter placeholders for each database type.
    /// </summary>
    public static class DatabaseDMLParameterizedQueries
    {
        private static string ConditionalUtcClock(DataSourceType provider)
        {
            if (provider == DataSourceType.SqlLite) return "strftime('%Y-%m-%d %H:%M:%f','now')";
            if (provider == DataSourceType.Postgre) return "timezone('UTC', clock_timestamp())";
            if (provider is DataSourceType.SqlServer or DataSourceType.AzureSQL) return "SYSUTCDATETIME()";
            throw new NotSupportedException("UTC expiration guards are not qualified for this provider.");
        }

        private static string ConditionalWindow(DataSourceType provider, ConditionalRowGuard guard, Func<string, string, string> field)
        {
            var terms = new List<string>();
            if (guard.NotExpiredUtcField != null)
            {
                var column = field(guard.EntityName, guard.NotExpiredUtcField);
                terms.Add("(" + column + " IS NULL OR " + column + " > " + ConditionalUtcClock(provider) + ")");
            }
            string Day(string value) => provider == DataSourceType.SqlLite ? "date(" + value + ")" : "CAST(" + value + " AS date)";
            foreach (var endpoint in new[] { (Name: guard.CalendarFromField, Relation: " <= "), (Name: guard.CalendarThroughField, Relation: " >= ") })
            {
                if (endpoint.Name == null) continue;
                var column = field(guard.EntityName, endpoint.Name);
                terms.Add("(" + column + " IS NULL OR " + Day(column) + endpoint.Relation + Day(ConditionalUtcClock(provider)) + ")");
            }
            return terms.Count == 0 ? string.Empty : " AND " + string.Join(" AND ", terms);
        }

        /// <summary>Rechecks complete alternative groups containing calendar windows after a single mutation, before commit.</summary>
        public static AppFilterQueryDefinition GenerateConditionalCalendarCommitQuery(DataSourceType provider, ConditionalMutationRequest request,
            Func<string, string> resolveEntity, Func<string, string, string> resolveField)
        {
            ArgumentNullException.ThrowIfNull(request);
            return GenerateConditionalGuardQuery(provider,
                request.Guards.Where(group => group.Alternatives.Any(guard => guard.HasCalendarWindow)),
                Array.Empty<ConditionalRowSetGuard>(), resolveEntity, resolveField);
        }

        /// <summary>Parameterized preflight observations, under the provider's owned serializable transaction.
        /// Complete groups containing calendar windows are also rechecked before commit.</summary>
        public static AppFilterQueryDefinition GenerateConditionalBatchGuardQuery(DataSourceType provider, ConditionalMutationBatchRequest request,
            Func<string, string> resolveEntity, Func<string, string, string> resolveField, bool beforeCommit = false)
        {
            ArgumentNullException.ThrowIfNull(request);
            var selected = beforeCommit
                ? request.CommitGuards.Concat(request.Guards.Concat(request.Mutations.SelectMany(op => op.Guards))
                    .Where(group => group.Alternatives.Any(guard => guard.HasCalendarWindow))).Distinct()
                : request.Guards;
            return GenerateConditionalGuardQuery(provider, selected,
                beforeCommit ? Array.Empty<ConditionalRowSetGuard>() : request.Sets, resolveEntity, resolveField);
        }

        /// <summary>Bounded physical observations, all executed under the caller's one serializable transaction.
        /// Guard alternatives remain intact; this never splits mutations or commits.</summary>
        public static IReadOnlyList<AppFilterQueryDefinition> GenerateConditionalBatchGuardQueries(DataSourceType provider, ConditionalMutationBatchRequest request,
            Func<string, string> resolveEntity, Func<string, string, string> resolveField, bool beforeCommit = false)
        {
            ArgumentNullException.ThrowIfNull(request);
            var groups = beforeCommit
                ? request.CommitGuards.Concat(request.Guards.Concat(request.Mutations.SelectMany(op => op.Guards))
                    .Where(group => group.Alternatives.Any(guard => guard.HasCalendarWindow))).Distinct()
                : request.Guards;
            var result = new List<AppFilterQueryDefinition>();
            foreach (var chunk in groups.Chunk(32))
                result.Add(GenerateConditionalGuardQuery(provider, chunk, Array.Empty<ConditionalRowSetGuard>(), resolveEntity, resolveField));
            if (!beforeCommit) foreach (var chunk in request.Sets.Chunk(32))
                result.Add(GenerateConditionalGuardQuery(provider, Array.Empty<ConditionalGuardGroup>(), chunk, resolveEntity, resolveField));
            if (result.Count == 0) result.Add(GenerateConditionalGuardQuery(provider, Array.Empty<ConditionalGuardGroup>(), Array.Empty<ConditionalRowSetGuard>(), resolveEntity, resolveField));
            return result.AsReadOnly();
        }

        private static AppFilterQueryDefinition GenerateConditionalGuardQuery(DataSourceType provider,
            IEnumerable<ConditionalGuardGroup> selected, IEnumerable<ConditionalRowSetGuard> sets,
            Func<string, string> resolveEntity, Func<string, string, string> resolveField)
        {
            if (provider is not (DataSourceType.SqlLite or DataSourceType.Postgre or DataSourceType.SqlServer or DataSourceType.AzureSQL))
                throw new NotSupportedException("Conditional guards are not qualified for this provider.");
            var result = new AppFilterQueryDefinition(); var prefix = provider == DataSourceType.SqlLite ? "$" : "@";
            string Entity(string entity) => DataSourceAppFilterExtensions.QuoteQueryIdentifier(resolveEntity(entity), provider);
            string Field(string entity, string field) => DataSourceAppFilterExtensions.QuoteQueryIdentifier(resolveField(entity, field), provider);
            string Parameter(object value) { var key = "guard_" + result.Parameters.Count; result.Parameters.Add(key, value == DBNull.Value ? null : value is DateTime date ? DataSourceAppFilterExtensions.DateTimeComparisonValue(date, provider) : value); return prefix + key; }
            string DateField(string entity, string field) => DataSourceAppFilterExtensions.DateTimeComparisonExpression(Field(entity, field), provider);
            var textParameters = new Dictionary<int, string>();
            string TextParameter(int value) { if (!textParameters.TryGetValue(value, out var name)) textParameters.Add(value, name = Parameter(value)); return name; }
            string Predicates(string entity, IReadOnlyDictionary<string, object> values) => string.Join(" AND ", values.Select(pair =>
                pair.Value == null || pair.Value == DBNull.Value ? Field(entity, pair.Key) + " IS NULL" :
                    (pair.Value is DateTime ? DateField(entity, pair.Key) : Field(entity, pair.Key)) + " = " + Parameter(pair.Value)));
            string DateRanges(ConditionalRowSetGuard set) => string.Concat(set.DateRanges.Select(range =>
                " AND " + DateField(set.EntityName, range.FieldName) + (range.IncludeFrom ? " >= " : " > ") + Parameter(range.From) +
                " AND " + DateField(set.EntityName, range.FieldName) + (range.IncludeThrough ? " <= " : " < ") + Parameter(range.Through)));
            var locks = provider == DataSourceType.Postgre ? " FOR SHARE" : string.Empty;
            var groups = selected.Select(group => "(" + string.Join(" OR ", group.Alternatives.Select(guard =>
                "EXISTS (SELECT 1 FROM " + Entity(guard.EntityName) + " WHERE " + Predicates(guard.EntityName, guard.Match) +
                ConditionalWindow(provider, guard, Field) + locks + ")")) + ")").ToList();
            groups.AddRange(sets.Select(set => "(SELECT COUNT(*) FROM (SELECT 1 AS guarded_value FROM " + Entity(set.EntityName) + " WHERE " +
                Predicates(set.EntityName, set.Match) + DateRanges(set) + string.Concat(set.TextEqualities.Select(text => " AND " +
                    OrdinalIgnoreCaseTextQuery.Build(provider, Field(set.EntityName, text.FieldName), text, TextParameter))) + locks + ") AS guarded_set) = " + Parameter(set.ExpectedCount)));
            if (provider is DataSourceType.SqlServer or DataSourceType.AzureSQL && result.Parameters.Count > 2100)
                throw new NotSupportedException("The native guard graph exceeds the SQL Server parameter limit.");
            result.QueryText = groups.Count == 0 ? "SELECT 1" : "SELECT 1 WHERE " + string.Join(" AND ", groups);
            return result;
        }

        /// <summary>Parameterized insert/update with expected values and required foreign-row guards.
        /// Callers resolve every table/field through declared metadata before generation.</summary>
        public static AppFilterQueryDefinition GenerateConditionalMutationQuery(DataSourceType rdbms,
            ConditionalMutationRequest request, Func<string, string> resolveEntity, Func<string, string, string> resolveField)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (rdbms is not (DataSourceType.SqlServer or DataSourceType.AzureSQL or DataSourceType.Postgre or DataSourceType.SqlLite))
                throw new NotSupportedException("Conditional mutations are not qualified for this provider.");
            var result = new AppFilterQueryDefinition();
            var prefix = rdbms == DataSourceType.SqlLite ? "$" : "@";
            string Entity(string entity) => DataSourceAppFilterExtensions.QuoteQueryIdentifier(resolveEntity(entity), rdbms);
            string Field(string entity, string field) => DataSourceAppFilterExtensions.QuoteQueryIdentifier(resolveField(entity, field), rdbms);
            string Parameter(object value)
            {
                var key = "mutation_" + result.Parameters.Count;
                result.Parameters.Add(key, value == DBNull.Value ? null : value);
                return prefix + key;
            }
            string Predicates(string entity, IReadOnlyDictionary<string, object> values) => string.Join(" AND ",
                values.Select(pair => pair.Value == null || pair.Value == DBNull.Value
                    ? Field(entity, pair.Key) + " IS NULL" : pair.Value is DateTime date
                        ? DataSourceAppFilterExtensions.DateTimeComparisonExpression(Field(entity, pair.Key), rdbms) + " = " +
                            Parameter(DataSourceAppFilterExtensions.DateTimeComparisonValue(date, rdbms))
                        : Field(entity, pair.Key) + " = " + Parameter(pair.Value)));
            // PostgreSQL MVCC guards need shared row locks until commit so deactivation/erasure cannot pass an in-flight save.
            var guardLock = rdbms == DataSourceType.Postgre ? " FOR SHARE" : string.Empty;
            var guards = request.Guards.Select(group => "(" + string.Join(" OR ", group.Alternatives.Select(guard =>
                "EXISTS (SELECT 1 FROM " + Entity(guard.EntityName) + " WHERE " + Predicates(guard.EntityName, guard.Match) + ConditionalWindow(rdbms, guard, Field) + guardLock + ")")) + ")").ToArray();
            var guardSql = string.Join(" AND ", guards);
            // The request constructor rejects unnamed values; keep named members exhaustive.
#pragma warning disable CS8524
            result.QueryText = request.Kind switch
            {
                ConditionalMutationKind.Insert => "INSERT INTO " + Entity(request.EntityName) + " (" +
                    string.Join(", ", request.Values.Keys.Select(field => Field(request.EntityName, field))) + ") SELECT " +
                    string.Join(", ", request.Values.Values.Select(Parameter)) + " WHERE " + guardSql,
                ConditionalMutationKind.Update => "UPDATE " + Entity(request.EntityName) + " SET " +
                    string.Join(", ", request.Values.Select(pair => Field(request.EntityName, pair.Key) + " = " + Parameter(pair.Value))) +
                    " WHERE " + Predicates(request.EntityName, request.Match) + " AND " + guardSql,
                ConditionalMutationKind.Delete => "DELETE FROM " + Entity(request.EntityName) + " WHERE " +
                    Predicates(request.EntityName, request.Match) + " AND " + guardSql
            };
            return result;
#pragma warning restore CS8524
        }

        /// <summary>
        /// Generates a parameterized INSERT query with proper parameter placeholders for each database type.
        /// </summary>
        /// <param name="rdbms">The type of RDBMS.</param>
        /// <param name="tableName">The name of the table.</param>
        /// <param name="columns">The column names for the insert.</param>
        /// <returns>A parameterized SQL INSERT query.</returns>
        public static string GenerateParameterizedInsertQuery(DataSourceType rdbms, string tableName, IEnumerable<string> columns)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Table name cannot be null or empty", nameof(tableName));

            if (columns == null || !columns.Any())
                throw new ArgumentException("No columns provided for insertion", nameof(columns));

            var columnList = string.Join(", ", columns);
            
            return rdbms switch
            {
                DataSourceType.SqlServer => $"INSERT INTO {tableName} ({columnList}) VALUES ({string.Join(", ", columns.Select(c => "@" + c))})",
                DataSourceType.Oracle => $"INSERT INTO {tableName} ({columnList}) VALUES ({string.Join(", ", columns.Select(c => ":" + c))})",
                DataSourceType.Mysql or DataSourceType.Postgre or DataSourceType.SqlLite => 
                    $"INSERT INTO {tableName} ({columnList}) VALUES ({string.Join(", ", columns.Select(c => "?"))})",
                _ => $"INSERT INTO {tableName} ({columnList}) VALUES ({string.Join(", ", columns.Select(c => "?"))})"
            };
        }

        /// <summary>
        /// Generates a parameterized UPDATE query with proper parameter placeholders for each database type.
        /// </summary>
        /// <param name="rdbms">The type of RDBMS.</param>
        /// <param name="tableName">The name of the table.</param>
        /// <param name="updateColumns">The columns to update.</param>
        /// <param name="whereColumns">The columns for the WHERE clause.</param>
        /// <returns>A parameterized SQL UPDATE query.</returns>
        public static string GenerateParameterizedUpdateQuery(DataSourceType rdbms, string tableName, 
            IEnumerable<string> updateColumns, IEnumerable<string> whereColumns)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Table name cannot be null or empty", nameof(tableName));

            if (updateColumns == null || !updateColumns.Any())
                throw new ArgumentException("No update columns provided", nameof(updateColumns));

            if (whereColumns == null || !whereColumns.Any())
                throw new ArgumentException("No WHERE columns provided", nameof(whereColumns));

            return rdbms switch
            {
                DataSourceType.SqlServer => 
                    $"UPDATE {tableName} SET {string.Join(", ", updateColumns.Select(c => $"{c} = @{c}"))} WHERE {string.Join(" AND ", whereColumns.Select(c => $"{c} = @{c}"))}",
                DataSourceType.Oracle => 
                    $"UPDATE {tableName} SET {string.Join(", ", updateColumns.Select(c => $"{c} = :{c}"))} WHERE {string.Join(" AND ", whereColumns.Select(c => $"{c} = :{c}"))}",
                DataSourceType.Mysql or DataSourceType.Postgre or DataSourceType.SqlLite => 
                    $"UPDATE {tableName} SET {string.Join(", ", updateColumns.Select(c => $"{c} = ?"))} WHERE {string.Join(" AND ", whereColumns.Select(c => $"{c} = ?"))}",
                _ => 
                    $"UPDATE {tableName} SET {string.Join(", ", updateColumns.Select(c => $"{c} = ?"))} WHERE {string.Join(" AND ", whereColumns.Select(c => $"{c} = ?"))}"
            };
        }

        /// <summary>
        /// Generates a parameterized DELETE query with proper parameter placeholders for each database type.
        /// </summary>
        /// <param name="rdbms">The type of RDBMS.</param>
        /// <param name="tableName">The name of the table.</param>
        /// <param name="whereColumns">The columns for the WHERE clause.</param>
        /// <returns>A parameterized SQL DELETE query.</returns>
        public static string GenerateParameterizedDeleteQuery(DataSourceType rdbms, string tableName, 
            IEnumerable<string> whereColumns)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Table name cannot be null or empty", nameof(tableName));

            if (whereColumns == null || !whereColumns.Any())
                throw new ArgumentException("No WHERE columns provided", nameof(whereColumns));

            return rdbms switch
            {
                DataSourceType.SqlServer => 
                    $"DELETE FROM {tableName} WHERE {string.Join(" AND ", whereColumns.Select(c => $"{c} = @{c}"))}",
                DataSourceType.Oracle => 
                    $"DELETE FROM {tableName} WHERE {string.Join(" AND ", whereColumns.Select(c => $"{c} = :{c}"))}",
                DataSourceType.Mysql or DataSourceType.Postgre or DataSourceType.SqlLite => 
                    $"DELETE FROM {tableName} WHERE {string.Join(" AND ", whereColumns.Select(c => $"{c} = ?"))}",
                _ => 
                    $"DELETE FROM {tableName} WHERE {string.Join(" AND ", whereColumns.Select(c => $"{c} = ?"))}"
            };
        }
    }
}
