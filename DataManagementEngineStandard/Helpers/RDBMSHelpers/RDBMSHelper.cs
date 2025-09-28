using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using System.Linq;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Helpers.DataTypesHelpers;
using TheTechIdea.Beep.Helpers.RDBMSHelpers.EntityHelpers;
using TheTechIdea.Beep.Helpers.RDBMSHelpers.DMLHelpers;

namespace TheTechIdea.Beep.Helpers.RDBMSHelpers
{
    /// <summary>
    /// Core facade helper class for interacting with Relational Database Management Systems (RDBMS).
    /// Delegates operations to specialized helper classes for better maintainability.
    /// </summary>
    public static partial class RDBMSHelper
    {
        #region Schema and Metadata Operations (Delegated to DatabaseSchemaQueryHelper)

        /// <summary>
        /// Gets the query for fetching schemas or databases that the specified user has access to.
        /// </summary>
        /// <param name="rdbms">The type of database system.</param>
        /// <param name="userName">The username to check privileges for (can be null for some database systems).</param>
        /// <returns>A SQL query string to retrieve accessible schemas or databases, or empty string if not supported.</returns>
        public static string GetSchemasorDatabases(DataSourceType rdbms, string userName)
        {
            return DatabaseSchemaQueryHelper.GetSchemasorDatabases(rdbms, userName);
        }

        /// <summary>
        /// Gets the query for fetching schemas or databases with built-in error handling.
        /// </summary>
        /// <param name="rdbms">The type of database system.</param>
        /// <param name="userName">The username to check privileges for.</param>
        /// <param name="throwOnError">Whether to throw exceptions for errors (default: false).</param>
        /// <returns>A tuple containing the query string and a success indicator.</returns>
        public static (string Query, bool Success, string ErrorMessage) GetSchemasorDatabasesSafe(
            DataSourceType rdbms,
            string userName,
            bool throwOnError = false)
        {
            return DatabaseSchemaQueryHelper.GetSchemasorDatabasesSafe(rdbms, userName, throwOnError);
        }

        /// <summary>
        /// Validates a generated database schema query and provides error information.
        /// </summary>
        /// <param name="rdbms">Database type for the query</param>
        /// <param name="userName">Username used in the query</param>
        /// <param name="query">The generated query string (if already created)</param>
        /// <returns>A QueryValidationResult containing validation status and details</returns>
        public static QueryValidationResult ValidateSchemaQuery(DataSourceType rdbms, string userName, string query = null)
        {
            return DatabaseSchemaQueryHelper.ValidateSchemaQuery(rdbms, userName, query);
        }

        /// <summary>
        /// Generates SQL to check if a table exists
        /// </summary>
        /// <param name="dataSourceType">Database type</param>
        /// <param name="tableName">Name of the table to check</param>
        /// <param name="schemaName">Schema name (optional)</param>
        /// <returns>SQL statement to check table existence</returns>
        public static string GetTableExistsQuery(DataSourceType dataSourceType, string tableName, string schemaName = null)
        {
            return DatabaseSchemaQueryHelper.GetTableExistsQuery(dataSourceType, tableName, schemaName);
        }

        /// <summary>
        /// Generates SQL to get column information for a table
        /// </summary>
        /// <param name="dataSourceType">Database type</param>
        /// <param name="tableName">Name of the table</param>
        /// <param name="schemaName">Schema name (optional)</param>
        /// <returns>SQL statement to get column information</returns>
        public static string GetColumnInfoQuery(DataSourceType dataSourceType, string tableName, string schemaName = null)
        {
            return DatabaseSchemaQueryHelper.GetColumnInfoQuery(dataSourceType, tableName, schemaName);
        }

        #endregion

        #region Database Object Creation Operations (Delegated to DatabaseObjectCreationHelper)

        /// <summary>
        /// Generates SQL to create a table based on an EntityStructure.
        /// </summary>
        /// <param name="entity">The EntityStructure containing entity definition</param>
        /// <returns>A tuple containing the SQL statement, success flag, and any error message</returns>
        public static (string Sql, bool Success, string ErrorMessage) GenerateCreateTableSQL(EntityStructure entity)
        {
            return DatabaseObjectCreationHelper.GenerateCreateTableSQL(entity);
        }

        /// <summary>
        /// Generates a SQL query to add a primary key to a table in a specific RDBMS.
        /// </summary>
        /// <param name="rdbms">The type of RDBMS.</param>
        /// <param name="tableName">The name of the table.</param>
        /// <param name="primaryKey">The name of the primary key column.</param>
        /// <param name="type">The data type of the primary key column.</param>
        /// <returns>A SQL query to add a primary key to the specified table in the specified RDBMS.</returns>
        public static string GeneratePrimaryKeyQuery(DataSourceType rdbms, string tableName, string primaryKey, string type)
        {
            return DatabaseObjectCreationHelper.GeneratePrimaryKeyQuery(rdbms, tableName, primaryKey, type);
        }

        /// <summary>
        /// Generates SQL to add a primary key to a table based on its entity structure.
        /// </summary>
        /// <param name="entity">The entity structure containing table and primary key information.</param>
        /// <returns>A tuple containing the SQL statement, success flag, and error message (if any).</returns>
        public static (string Sql, bool Success, string ErrorMessage) GeneratePrimaryKeyFromEntity(EntityStructure entity)
        {
            return DatabaseObjectCreationHelper.GeneratePrimaryKeyFromEntity(entity);
        }

        /// <summary>
        /// Generates a query to create an index
        /// </summary>
        /// <param name="databaseType">Database type</param>
        /// <param name="tableName">Name of the table</param>
        /// <param name="indexName">Name of the index</param>
        /// <param name="columns">Array of column names</param>
        /// <param name="options">Optional index creation options</param>
        /// <returns>SQL statement to create the index</returns>
        public static string GenerateCreateIndexQuery(DataSourceType databaseType, string tableName, string indexName, 
            string[] columns, Dictionary<string, object> options = null)
        {
            return DatabaseObjectCreationHelper.GenerateCreateIndexQuery(databaseType, tableName, indexName, columns, options);
        }

        /// <summary>
        /// Generates SQL to create a unique index on a table based on its entity structure.
        /// </summary>
        /// <param name="entity">The entity structure containing table and index information.</param>
        /// <returns>A tuple containing the SQL statement, success flag, and error message (if any).</returns>
        public static (string Sql, bool Success, string ErrorMessage) GenerateUniqueIndexFromEntity(EntityStructure entity)
        {
            return DatabaseObjectCreationHelper.GenerateUniqueIndexFromEntity(entity);
        }

        /// <summary>
        /// Generates SQL to drop an entity
        /// </summary>
        /// <param name="dataSourceType">Database type</param>
        /// <param name="entityName">Name of the entity to drop</param>
        /// <returns>SQL statement to drop the entity</returns>
        public static string GetDropEntity(DataSourceType dataSourceType, string entityName)
        {
            return DatabaseObjectCreationHelper.GetDropEntity(dataSourceType, entityName);
        }

        /// <summary>
        /// Generates SQL to truncate a table
        /// </summary>
        /// <param name="dataSourceType">Database type</param>
        /// <param name="tableName">Name of the table to truncate</param>
        /// <param name="schemaName">Schema name (optional)</param>
        /// <returns>SQL statement to truncate the table</returns>
        public static string GetTruncateTableQuery(DataSourceType dataSourceType, string tableName, string schemaName = null)
        {
            return DatabaseObjectCreationHelper.GetTruncateTableQuery(dataSourceType, tableName, schemaName);
        }

        #endregion

        #region DML Operations (Delegated to DatabaseDMLHelper)

        /// <summary>
        /// Generates a SQL query to insert data into a table in a specific RDBMS.
        /// </summary>
        /// <param name="rdbms">The type of RDBMS.</param>
        /// <param name="tableName">The name of the table.</param>
        /// <param name="data">The data to insert, in key-value pair format.</param>
        /// <returns>A SQL query to insert the data into the specified table in the specified RDBMS.</returns>
        public static string GenerateInsertQuery(DataSourceType rdbms, string tableName, Dictionary<string, object> data)
        {
            return DatabaseDMLHelper.GenerateInsertQuery(rdbms, tableName, data);
        }

        /// <summary>
        /// Generates a SQL query to update data in a table in a specific RDBMS.
        /// </summary>
        /// <param name="rdbms">The type of RDBMS.</param>
        /// <param name="tableName">The name of the table.</param>
        /// <param name="data">The data to update, in key-value pair format.</param>
        /// <param name="conditions">The conditions for the update, in key-value pair format.</param>
        /// <returns>A SQL query to update the data in the specified table in the specified RDBMS.</returns>
        public static string GenerateUpdateQuery(DataSourceType rdbms, string tableName, Dictionary<string, object> data, Dictionary<string, object> conditions)
        {
            return DatabaseDMLHelper.GenerateUpdateQuery(rdbms, tableName, data, conditions);
        }

        /// <summary>
        /// Generates a SQL query to delete data from a table in a specific RDBMS.
        /// </summary>
        /// <param name="rdbms">The type of RDBMS.</param>
        /// <param name="tableName">The name of the table.</param>
        /// <param name="conditions">The conditions for the deletion, in key-value pair format.</param>
        /// <returns>A SQL query to delete the data from the specified table in the specified RDBMS.</returns>
        public static string GenerateDeleteQuery(DataSourceType rdbms, string tableName, Dictionary<string, object> conditions)
        {
            return DatabaseDMLHelper.GenerateDeleteQuery(rdbms, tableName, conditions);
        }

        /// <summary>
        /// Gets the SQL syntax for paging results
        /// </summary>
        /// <param name="dataSourceType">Database type</param>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Number of records per page</param>
        /// <returns>SQL paging syntax</returns>
        public static string GetPagingSyntax(DataSourceType dataSourceType, int pageNumber, int pageSize)
        {
            return DatabaseDMLHelper.GetPagingSyntax(dataSourceType, pageNumber, pageSize);
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
            return DatabaseDMLHelper.GetRecordCountQuery(dataSourceType, tableName, schemaName, whereClause);
        }

        /// <summary>
        /// Safely quotes a string value for SQL to prevent injection attacks.
        /// </summary>
        /// <param name="value">The value to quote</param>
        /// <param name="dataSourceType">The database type</param>
        /// <returns>Safely quoted string</returns>
        public static string SafeQuote(string value, DataSourceType dataSourceType)
        {
            return DatabaseDMLHelper.SafeQuote(value, dataSourceType);
        }

        #endregion

        #region Database Features and Sequences (Delegated to DatabaseFeatureHelper)

        /// <summary>
        /// Generates a query to fetch the next value from a sequence in a specific database.
        /// </summary>
        /// <param name="rdbms">The type of the database.</param>
        /// <param name="sequenceName">The name of the sequence.</param>
        /// <returns>A query string to fetch the next value from the specified sequence in the given database.</returns>
        public static string GenerateFetchNextSequenceValueQuery(DataSourceType rdbms, string sequenceName)
        {
            return DatabaseFeatureHelper.GenerateFetchNextSequenceValueQuery(rdbms, sequenceName);
        }

        /// <summary>
        /// Generates a query to fetch the last inserted identity value based on the specified RDBMS.
        /// </summary>
        /// <param name="rdbms">The type of RDBMS.</param>
        /// <param name="sequenceName">The name of the sequence or generator (optional for some RDBMS).</param>
        /// <returns>A query string to fetch the last inserted identity value.</returns>
        public static string GenerateFetchLastIdentityQuery(DataSourceType rdbms, string sequenceName = "")
        {
            return DatabaseFeatureHelper.GenerateFetchLastIdentityQuery(rdbms, sequenceName);
        }

        /// <summary>
        /// Generates SQL statements for transaction operations
        /// </summary>
        /// <param name="databaseType">Database type</param>
        /// <param name="operation">Transaction operation (Begin, Commit, Rollback)</param>
        /// <returns>SQL statement for the transaction operation</returns>
        public static string GetTransactionStatement(DataSourceType databaseType, TransactionOperation operation)
        {
            return DatabaseFeatureHelper.GetTransactionStatement(databaseType, operation);
        }

        /// <summary>
        /// Determines if the database type supports specific features
        /// </summary>
        /// <param name="dataSourceType">Database type</param>
        /// <param name="feature">Feature to check</param>
        /// <returns>True if the feature is supported</returns>
        public static bool SupportsFeature(DataSourceType dataSourceType, DatabaseFeature feature)
        {
            return DatabaseFeatureHelper.SupportsFeature(dataSourceType, feature);
        }

        /// <summary>
        /// Gets all supported features for a given database type.
        /// </summary>
        /// <param name="dataSourceType">Database type to check</param>
        /// <returns>List of supported database features</returns>
        public static List<DatabaseFeature> GetSupportedFeatures(DataSourceType dataSourceType)
        {
            return DatabaseFeatureHelper.GetSupportedFeatures(dataSourceType);
        }

        /// <summary>
        /// Checks if a database supports sequences (auto-incrementing values).
        /// </summary>
        /// <param name="dataSourceType">Database type to check</param>
        /// <returns>True if sequences are supported</returns>
        public static bool SupportsSequences(DataSourceType dataSourceType)
        {
            return DatabaseFeatureHelper.SupportsSequences(dataSourceType);
        }

        /// <summary>
        /// Checks if a database supports auto-increment/identity columns.
        /// </summary>
        /// <param name="dataSourceType">Database type to check</param>
        /// <returns>True if auto-increment is supported</returns>
        public static bool SupportsAutoIncrement(DataSourceType dataSourceType)
        {
            return DatabaseFeatureHelper.SupportsAutoIncrement(dataSourceType);
        }

        /// <summary>
        /// Gets the maximum identifier length for a given database type.
        /// </summary>
        /// <param name="dataSourceType">Database type</param>
        /// <returns>Maximum identifier length in characters</returns>
        public static int GetMaxIdentifierLength(DataSourceType dataSourceType)
        {
            return DatabaseFeatureHelper.GetMaxIdentifierLength(dataSourceType);
        }

        #endregion

        #region Query Repository Operations (Delegated to DatabaseQueryRepositoryHelper)

        /// <summary>
        /// Creates a comprehensive list of QuerySqlRepo objects for different database types and operations.
        /// </summary>
        /// <returns>A list of QuerySqlRepo objects representing different query configurations.</returns>
        public static List<QuerySqlRepo> CreateQuerySqlRepos()
        {
            return DatabaseQueryRepositoryHelper.CreateQuerySqlRepos();
        }

        /// <summary>
        /// Gets a predefined query for the specified database type and query type.
        /// </summary>
        /// <param name="dataSourceType">The database type</param>
        /// <param name="queryType">The type of query needed</param>
        /// <returns>The SQL query string, or empty string if not found</returns>
        public static string GetQuery(DataSourceType dataSourceType, Sqlcommandtype queryType)
        {
            return DatabaseQueryRepositoryHelper.GetQuery(dataSourceType, queryType);
        }

        /// <summary>
        /// Checks if a given SQL statement is valid by looking for common SQL keywords.
        /// </summary>
        /// <param name="sqlString">The SQL statement to be validated.</param>
        /// <returns>True if the SQL statement is valid, false otherwise.</returns>
        public static bool IsSqlStatementValid(string sqlString)
        {
            return DatabaseQueryRepositoryHelper.IsSqlStatementValid(sqlString);
        }

        #endregion

        #region Entity Operations (Delegated to DatabaseEntityHelper)

        /// <summary>
        /// Generates SQL to delete records from an entity with provided values.
        /// </summary>
        /// <param name="entity">The EntityStructure containing entity information</param>
        /// <param name="values">Dictionary containing values for the WHERE clause</param>
        /// <returns>A tuple containing the SQL statement, success flag, and any error message</returns>
        public static (string Sql, bool Success, string ErrorMessage) GenerateDeleteEntityWithValues(EntityStructure entity, Dictionary<string, object> values)
        {
            return DatabaseEntityHelper.GenerateDeleteEntityWithValues(entity, values);
        }

        /// <summary>
        /// Generates SQL to insert records into an entity with provided values.
        /// </summary>
        /// <param name="entity">The EntityStructure containing entity information</param>
        /// <param name="values">Dictionary containing field values to insert</param>
        /// <returns>A tuple containing the SQL statement, parameters, success flag, and any error message</returns>
        public static (string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage) GenerateInsertWithValues(EntityStructure entity, Dictionary<string, object> values)
        {
            return DatabaseEntityHelper.GenerateInsertWithValues(entity, values);
        }

        /// <summary>
        /// Generates SQL to update records in an entity with provided values and conditions.
        /// </summary>
        /// <param name="entity">The EntityStructure containing entity information</param>
        /// <param name="values">Dictionary containing field values to update</param>
        /// <param name="conditions">Dictionary containing values for the WHERE clause</param>
        /// <returns>A tuple containing the SQL statement, success flag, and any error message</returns>
        public static (string Sql, bool Success, string ErrorMessage) GenerateUpdateEntityWithValues(EntityStructure entity, Dictionary<string, object> values, Dictionary<string, object> conditions)
        {
            return DatabaseEntityHelper.GenerateUpdateEntityWithValues(entity, values, conditions);
        }

        /// <summary>
        /// Validates an entity structure and returns errors if any.
        /// </summary>
        /// <param name="entity">The EntityStructure to validate</param>
        /// <returns>Tuple with validation result and error list</returns>
        public static (bool IsValid, List<string> ValidationErrors) ValidateEntityStructure(EntityStructure entity)
        {
            return DatabaseEntityHelper.ValidateEntityStructure(entity);
        }

        /// <summary>
        /// Gets entity compatibility information for different database types.
        /// </summary>
        /// <param name="entity">The entity to analyze</param>
        /// <returns>Dictionary containing compatibility information</returns>
        public static Dictionary<string, object> GetEntityCompatibilityInfo(EntityStructure entity)
        {
            return DatabaseEntityHelper.GetEntityCompatibilityInfo(entity);
        }

        /// <summary>
        /// Suggests improvements for an entity structure.
        /// </summary>
        /// <param name="entity">The entity to analyze</param>
        /// <returns>List of improvement suggestions</returns>
        public static List<string> SuggestEntityImprovements(EntityStructure entity)
        {
            return DatabaseEntityHelper.SuggestEntityImprovements(entity);
        }

        #endregion

        //#region Backward Compatibility (Deprecated - Use specialized helpers directly)

        //[Obsolete("Use DatabaseEntityHelper.GenerateUpdateEntityWithValues instead")]
        //public static (bool Success, string ErrorMessage) GenerateUpdateEntityWithValues(EntityStructure entity, Dictionary<string, object> values, Dictionary<string, object> conditions)
        //{
        //    var result = DatabaseEntityHelper.GenerateUpdateEntityWithValues(entity, values, conditions);
        //    return (result.Success, result.Success ? result.Sql : result.ErrorMessage);
        //}

        //#endregion
    }
}
