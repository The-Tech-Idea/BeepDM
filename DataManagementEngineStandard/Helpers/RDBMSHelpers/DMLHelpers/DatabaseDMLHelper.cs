using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Helpers.RDBMSHelpers.DMLHelpers
{
    /// <summary>
    /// Main helper class for generating SQL DML (Data Manipulation Language) queries for different database types.
    /// This class serves as a facade that delegates to specialized helper classes.
    /// </summary>
    public static partial class DatabaseDMLHelper
    {
        #region Basic DML Operations (Delegated to DatabaseDMLBasicOperations)

        /// <summary>
        /// Generates a SQL query to insert data into a table in a specific RDBMS.
        /// </summary>
        /// <param name="rdbms">The type of RDBMS.</param>
        /// <param name="tableName">The name of the table.</param>
        /// <param name="data">The data to insert, in key-value pair format.</param>
        /// <returns>A SQL query to insert the data into the specified table in the specified RDBMS.</returns>
        /// <exception cref="ArgumentException">Thrown when the specified RDBMS or table name is not supported.</exception>
        public static string GenerateInsertQuery(DataSourceType rdbms, string tableName, Dictionary<string, object> data)
        {
            return DatabaseDMLBasicOperations.GenerateInsertQuery(rdbms, tableName, data);
        }

        /// <summary>
        /// Generates a SQL query to update data in a table in a specific RDBMS.
        /// </summary>
        /// <param name="rdbms">The type of RDBMS.</param>
        /// <param name="tableName">The name of the table.</param>
        /// <param name="data">The data to update, in key-value pair format.</param>
        /// <param name="conditions">The conditions for the update, in key-value pair format.</param>
        /// <returns>A SQL query to update the data in the specified table in the specified RDBMS.</returns>
        /// <exception cref="ArgumentException">Thrown when the specified RDBMS or table name is not supported.</exception>
        public static string GenerateUpdateQuery(DataSourceType rdbms, string tableName, Dictionary<string, object> data, Dictionary<string, object> conditions)
        {
            return DatabaseDMLBasicOperations.GenerateUpdateQuery(rdbms, tableName, data, conditions);
        }

        /// <summary>
        /// Generates a SQL query to delete data from a table in a specific RDBMS.
        /// </summary>
        /// <param name="rdbms">The type of RDBMS.</param>
        /// <param name="tableName">The name of the table.</param>
        /// <param name="conditions">The conditions for the deletion, in key-value pair format.</param>
        /// <returns>A SQL query to delete the data from the specified table in the specified RDBMS.</returns>
        /// <exception cref="ArgumentException">Thrown when the specified RDBMS or table name is not supported.</exception>
        public static string GenerateDeleteQuery(DataSourceType rdbms, string tableName, Dictionary<string, object> conditions)
        {
            return DatabaseDMLBasicOperations.GenerateDeleteQuery(rdbms, tableName, conditions);
        }

        #endregion

        #region Bulk Operations (Delegated to DatabaseDMLBulkOperations)

        /// <summary>
        /// Generates bulk insert SQL for multiple records with optimized performance.
        /// </summary>
        /// <param name="dataSourceType">Database type</param>
        /// <param name="tableName">Name of the table</param>
        /// <param name="columns">Column names</param>
        /// <param name="batchSize">Number of records per batch (default: 1000)</param>
        /// <returns>Optimized bulk insert SQL</returns>
        public static string GenerateBulkInsertQuery(DataSourceType dataSourceType, string tableName, 
            IEnumerable<string> columns, int batchSize = 1000)
        {
            return DatabaseDMLBulkOperations.GenerateBulkInsertQuery(dataSourceType, tableName, columns, batchSize);
        }

        /// <summary>
        /// Generates an UPSERT (INSERT or UPDATE) query for database-specific merge operations.
        /// </summary>
        /// <param name="dataSourceType">Database type</param>
        /// <param name="tableName">Name of the table</param>
        /// <param name="keyColumns">Key columns for matching</param>
        /// <param name="updateColumns">Columns to update</param>
        /// <param name="insertColumns">Columns for insert</param>
        /// <returns>Database-specific UPSERT query</returns>
        public static string GenerateUpsertQuery(DataSourceType dataSourceType, string tableName,
            IEnumerable<string> keyColumns, IEnumerable<string> updateColumns, IEnumerable<string> insertColumns)
        {
            return DatabaseDMLBulkOperations.GenerateUpsertQuery(dataSourceType, tableName, keyColumns, updateColumns, insertColumns);
        }

        /// <summary>
        /// Generates bulk delete query with IN clause optimization.
        /// </summary>
        /// <param name="dataSourceType">Database type</param>
        /// <param name="tableName">Name of the table</param>
        /// <param name="keyColumn">Key column for deletion</param>
        /// <param name="batchSize">Number of IDs per batch</param>
        /// <returns>Optimized bulk delete query</returns>
        public static string GenerateBulkDeleteQuery(DataSourceType dataSourceType, string tableName, 
            string keyColumn, int batchSize = 1000)
        {
            return DatabaseDMLBulkOperations.GenerateBulkDeleteQuery(dataSourceType, tableName, keyColumn, batchSize);
        }

        #endregion

        #region Advanced Query Generation (Delegated to DatabaseDMLAdvancedQueryGenerator)

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
            return DatabaseDMLAdvancedQueryGenerator.GenerateSelectQuery(dataSourceType, tableName, columns, whereClause, orderBy, pageNumber, pageSize);
        }

        /// <summary>
        /// Generates a complex JOIN query with multiple tables.
        /// </summary>
        /// <param name="dataSourceType">Database type</param>
        /// <param name="joinSpec">Join specification containing tables and conditions</param>
        /// <returns>Generated JOIN query</returns>
        public static string GenerateJoinQuery(DataSourceType dataSourceType, JoinSpecification joinSpec)
        {
            return DatabaseDMLAdvancedQueryGenerator.GenerateJoinQuery(dataSourceType, joinSpec);
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
            return DatabaseDMLAdvancedQueryGenerator.GenerateAggregationQuery(dataSourceType, tableName, selectColumns, groupByColumns, havingClause, whereClause);
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
            return DatabaseDMLAdvancedQueryGenerator.GenerateWindowFunctionQuery(dataSourceType, tableName, windowSpec);
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
            return DatabaseDMLAdvancedQueryGenerator.GenerateConditionalInsertQuery(dataSourceType, tableName, columns, values, conflictColumns);
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
            return DatabaseDMLAdvancedQueryGenerator.GenerateExistsQuery(dataSourceType, mainTable, subqueryTable, joinCondition);
        }

        #endregion

        #region Parameterized Queries (Delegated to DatabaseDMLParameterizedQueries)

        /// <summary>
        /// Generates a parameterized INSERT query with proper parameter placeholders for each database type.
        /// </summary>
        /// <param name="rdbms">The type of RDBMS.</param>
        /// <param name="tableName">The name of the table.</param>
        /// <param name="columns">The column names for the insert.</param>
        /// <returns>A parameterized SQL INSERT query.</returns>
        public static string GenerateParameterizedInsertQuery(DataSourceType rdbms, string tableName, IEnumerable<string> columns)
        {
            return DatabaseDMLParameterizedQueries.GenerateParameterizedInsertQuery(rdbms, tableName, columns);
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
            return DatabaseDMLParameterizedQueries.GenerateParameterizedUpdateQuery(rdbms, tableName, updateColumns, whereColumns);
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
            return DatabaseDMLParameterizedQueries.GenerateParameterizedDeleteQuery(rdbms, tableName, whereColumns);
        }

        #endregion

        #region Utility Methods (Delegated to DatabaseDMLUtilities)

        /// <summary>
        /// Gets the SQL syntax for paging results
        /// </summary>
        /// <param name="dataSourceType">Database type</param>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Number of records per page</param>
        /// <returns>SQL paging syntax</returns>
        public static string GetPagingSyntax(DataSourceType dataSourceType, int pageNumber, int pageSize)
        {
            return DatabaseDMLUtilities.GetPagingSyntax(dataSourceType, pageNumber, pageSize);
        }

        /// <summary>
        /// Generates SQL to get the count of records in a table
        /// </summary>
        /// <param name="dataSourceType">Database type</param>
        /// <param name="tableName">Name of the table</param>
        /// <param name="schemaName">Schema name (optional)</param>
        /// <param name="whereClause">Optional WHERE clause</param>
        /// <returns>SQL statement to count records</returns>
        public static string GetRecordCountQuery(DataSourceType dataSourceType, string tableName, string schemaName = null, string whereClause = null)
        {
            return DatabaseDMLUtilities.GetRecordCountQuery(dataSourceType, tableName, schemaName, whereClause);
        }

        /// <summary>
        /// Safely quotes a string value for SQL to prevent injection attacks.
        /// </summary>
        /// <param name="value">The value to quote</param>
        /// <param name="dataSourceType">The database type</param>
        /// <returns>Safely quoted string</returns>
        public static string SafeQuote(string value, DataSourceType dataSourceType)
        {
            return DatabaseDMLUtilities.SafeQuote(value, dataSourceType);
        }

        #endregion
    }
}