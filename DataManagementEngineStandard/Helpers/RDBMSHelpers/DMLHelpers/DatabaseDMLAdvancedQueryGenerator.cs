using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Helpers.RDBMSHelpers.DMLHelpers
{
    /// <summary>
    /// Helper class for advanced query generation including JOINs, aggregations, and window functions.
    /// </summary>
    public static class DatabaseDMLAdvancedQueryGenerator
    {
        /// <summary>
        /// Generates a SELECT query with optional filtering, ordering, and paging.
        /// </summary>
        /// <param name="dataSourceType">Database type</param>
        /// <param name="tableName">Name of the table</param>
        /// <param name="columns">Columns to select (null for all)</param>
        /// <param name="whereClause">Optional WHERE clause</param>
        /// <param name="orderBy">Optional ORDER BY clause</param>
        /// <param name="pageNumber">Optional page number (1-based)</param>
        /// <param name="pageSize">Optional page size</param>
        /// <returns>Generated SELECT query</returns>
        public static string GenerateSelectQuery(DataSourceType dataSourceType, string tableName, 
            IEnumerable<string> columns = null, string whereClause = null, string orderBy = null, 
            int? pageNumber = null, int? pageSize = null)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Table name cannot be null or empty", nameof(tableName));

            var columnList = columns != null && columns.Any() ? string.Join(", ", columns) : "*";
            var whereClauseText = string.IsNullOrEmpty(whereClause) ? "" : $" WHERE {whereClause}";
            var orderByText = string.IsNullOrEmpty(orderBy) ? "" : $" ORDER BY {orderBy}";
            
            var baseQuery = $"SELECT {columnList} FROM {tableName}{whereClauseText}{orderByText}";
            
            if (pageNumber.HasValue && pageSize.HasValue)
            {
                var pagingClause = DatabaseDMLUtilities.GetPagingSyntax(dataSourceType, pageNumber.Value, pageSize.Value);
                baseQuery += $" {pagingClause}";
            }
            
            return baseQuery;
        }

        /// <summary>
        /// Generates a complex JOIN query with multiple tables.
        /// </summary>
        /// <param name="dataSourceType">Database type</param>
        /// <param name="joinSpec">Join specification containing tables and conditions</param>
        /// <returns>Generated JOIN query</returns>
        public static string GenerateJoinQuery(DataSourceType dataSourceType, JoinSpecification joinSpec)
        {
            if (joinSpec == null)
                throw new ArgumentException("Join specification cannot be null", nameof(joinSpec));

            var query = new StringBuilder();
            query.Append($"SELECT {string.Join(", ", joinSpec.SelectColumns)} FROM {joinSpec.MainTable}");

            foreach (var join in joinSpec.Joins)
            {
                query.Append($" {join.JoinType} JOIN {join.TableName} ON {join.OnCondition}");
            }

            if (!string.IsNullOrEmpty(joinSpec.WhereClause))
                query.Append($" WHERE {joinSpec.WhereClause}");

            if (!string.IsNullOrEmpty(joinSpec.OrderBy))
                query.Append($" ORDER BY {joinSpec.OrderBy}");

            return query.ToString();
        }

        /// <summary>
        /// Generates aggregation queries with GROUP BY and HAVING clauses.
        /// </summary>
        /// <param name="dataSourceType">Database type</param>
        /// <param name="tableName">Name of the table</param>
        /// <param name="selectColumns">Columns to select (including aggregates)</param>
        /// <param name="groupByColumns">Columns to group by</param>
        /// <param name="havingClause">Optional HAVING clause</param>
        /// <param name="whereClause">Optional WHERE clause</param>
        /// <returns>Generated aggregation query</returns>
        public static string GenerateAggregationQuery(DataSourceType dataSourceType, string tableName,
            IEnumerable<string> selectColumns, IEnumerable<string> groupByColumns,
            string havingClause = null, string whereClause = null)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Table name cannot be null or empty", nameof(tableName));

            var selects = selectColumns?.ToList() ?? throw new ArgumentException("Select columns cannot be null", nameof(selectColumns));
            var groups = groupByColumns?.ToList() ?? throw new ArgumentException("Group by columns cannot be null", nameof(groupByColumns));

            var query = new StringBuilder();
            query.Append($"SELECT {string.Join(", ", selects)} FROM {tableName}");

            if (!string.IsNullOrEmpty(whereClause))
                query.Append($" WHERE {whereClause}");

            query.Append($" GROUP BY {string.Join(", ", groups)}");

            if (!string.IsNullOrEmpty(havingClause))
                query.Append($" HAVING {havingClause}");

            return query.ToString();
        }

        /// <summary>
        /// Generates window function queries for advanced analytics.
        /// </summary>
        /// <param name="dataSourceType">Database type</param>
        /// <param name="tableName">Name of the table</param>
        /// <param name="windowSpec">Window function specification</param>
        /// <returns>Generated window function query</returns>
        public static string GenerateWindowFunctionQuery(DataSourceType dataSourceType, string tableName, WindowFunctionSpec windowSpec)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Table name cannot be null or empty", nameof(tableName));

            if (windowSpec == null)
                throw new ArgumentException("Window specification cannot be null", nameof(windowSpec));

            if (!DatabaseFeatureHelper.SupportsFeature(dataSourceType, DatabaseFeature.WindowFunctions))
                throw new NotSupportedException($"Window functions not supported by {dataSourceType}");

            var query = new StringBuilder();
            query.Append($"SELECT {string.Join(", ", windowSpec.SelectColumns)}, ");
            query.Append($"{windowSpec.WindowFunction} OVER (");

            if (windowSpec.PartitionBy != null && windowSpec.PartitionBy.Any())
                query.Append($"PARTITION BY {string.Join(", ", windowSpec.PartitionBy)} ");

            if (windowSpec.OrderBy != null && windowSpec.OrderBy.Any())
                query.Append($"ORDER BY {string.Join(", ", windowSpec.OrderBy)} ");

            if (!string.IsNullOrEmpty(windowSpec.WindowFrame))
                query.Append(windowSpec.WindowFrame);

            query.Append($") AS {windowSpec.Alias} FROM {tableName}");

            if (!string.IsNullOrEmpty(windowSpec.WhereClause))
                query.Append($" WHERE {windowSpec.WhereClause}");

            return query.ToString();
        }

        /// <summary>
        /// Generates conditional INSERT (INSERT IF NOT EXISTS) for databases that support it.
        /// </summary>
        /// <param name="dataSourceType">Database type</param>
        /// <param name="tableName">Name of the table</param>
        /// <param name="columns">Columns to insert</param>
        /// <param name="values">Values to insert</param>
        /// <param name="conflictColumns">Columns to check for conflicts</param>
        /// <returns>Conditional insert query</returns>
        public static string GenerateConditionalInsertQuery(DataSourceType dataSourceType, string tableName,
            IEnumerable<string> columns, IEnumerable<string> values, IEnumerable<string> conflictColumns)
        {
            var columnList = string.Join(", ", columns);
            var valueList = string.Join(", ", values);
            var conflictList = string.Join(" AND ", conflictColumns.Select(c => $"{c} = ?"));

            return dataSourceType switch
            {
                DataSourceType.Mysql => $"INSERT IGNORE INTO {tableName} ({columnList}) VALUES ({valueList})",
                DataSourceType.Postgre => $"INSERT INTO {tableName} ({columnList}) VALUES ({valueList}) ON CONFLICT DO NOTHING",
                DataSourceType.SqlLite => $"INSERT OR IGNORE INTO {tableName} ({columnList}) VALUES ({valueList})",
                DataSourceType.SqlServer => $"IF NOT EXISTS (SELECT 1 FROM {tableName} WHERE {conflictList}) INSERT INTO {tableName} ({columnList}) VALUES ({valueList})",
                _ => throw new NotSupportedException($"Conditional insert not supported for {dataSourceType}")
            };
        }

        /// <summary>
        /// Generates optimized EXISTS clause for better performance than IN.
        /// </summary>
        /// <param name="dataSourceType">Database type</param>
        /// <param name="mainTable">Main table name</param>
        /// <param name="subqueryTable">Subquery table name</param>
        /// <param name="joinCondition">Join condition between tables</param>
        /// <returns>EXISTS clause query</returns>
        public static string GenerateExistsQuery(DataSourceType dataSourceType, string mainTable, 
            string subqueryTable, string joinCondition)
        {
            return $"SELECT * FROM {mainTable} WHERE EXISTS (SELECT 1 FROM {subqueryTable} WHERE {joinCondition})";
        }
    }
}