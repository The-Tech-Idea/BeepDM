using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Helpers.UniversalDataSourceHelpers.Core
{
    /// <summary>
    /// General implementation of IDataSourceHelper that uses DataSourceHelperFactory
    /// to delegate operations to the appropriate specific helper based on datasource type.
    ///
    /// This provides a unified interface that can handle any datasource type dynamically,
    /// making it useful for scenarios where the datasource type is determined at runtime.
    /// </summary>
    public class GeneralDataSourceHelper : IDataSourceHelper
    {
        private DataSourceType _dataSourceType;
        private IDataSourceHelper _currentHelper;

        /// <summary>
        /// Initializes a new instance of GeneralDataSourceHelper for the specified datasource type.
        /// </summary>
        /// <param name="dataSourceType">The datasource type to handle</param>
        public GeneralDataSourceHelper(DataSourceType dataSourceType)
        {
            SetDataSourceType(dataSourceType);
        }

        /// <summary>
        /// Gets or sets the datasource type this helper is designed for.
        /// Setting this property will automatically switch to the appropriate helper for the new type.
        /// </summary>
        public DataSourceType SupportedType
        {
            get => _dataSourceType;
            set => SetDataSourceType(value);
        }

        /// <summary>
        /// Sets the datasource type and updates the internal helper instance.
        /// </summary>
        /// <param name="dataSourceType">The new datasource type to switch to</param>
        /// <exception cref="ArgumentException">Thrown if no helper is available for the specified type</exception>
        private void SetDataSourceType(DataSourceType dataSourceType)
        {
            var newHelper = DataSourceHelperFactory.CreateHelper(dataSourceType);

            if (newHelper == null)
            {
                throw new ArgumentException($"No helper available for datasource type: {dataSourceType}", nameof(dataSourceType));
            }

            _dataSourceType = dataSourceType;
            _currentHelper = newHelper;
        }

        /// <summary>
        /// Gets the human-readable name of the datasource type.
        /// </summary>
        public string Name => _currentHelper.Name;

        /// <summary>
        /// Gets the capabilities of this datasource type.
        /// </summary>
        public DataSourceCapabilities Capabilities => _currentHelper.Capabilities;

        #region Schema Operations

        /// <summary>
        /// Gets a query to retrieve schemas or databases accessible to the specified user.
        /// </summary>
        /// <param name="userName">The username to check privileges for (can be null for some datasources)</param>
        /// <returns>Query string and success indicator</returns>
        public (string Query, bool Success) GetSchemaQuery(string userName)
        {
            return _currentHelper.GetSchemaQuery(userName);
        }

        /// <summary>
        /// Gets a query to check if a table/collection exists.
        /// </summary>
        /// <param name="tableName">The name of the table/collection</param>
        /// <returns>Query string and success indicator</returns>
        public (string Query, bool Success) GetTableExistsQuery(string tableName)
        {
            return _currentHelper.GetTableExistsQuery(tableName);
        }

        /// <summary>
        /// Gets a query to retrieve column/field information for a table/collection.
        /// </summary>
        /// <param name="tableName">The name of the table/collection</param>
        /// <returns>Query string and success indicator</returns>
        public (string Query, bool Success) GetColumnInfoQuery(string tableName)
        {
            return _currentHelper.GetColumnInfoQuery(tableName);
        }

        #endregion

        #region DDL Operations (Create, Alter, Drop) - Level 1 Schema Operations

        /// <summary>
        /// Generates SQL/query to create a table/collection from an entity structure.
        /// Supports optional schema name and datasource type for multi-dialect generation.
        /// </summary>
        /// <param name="entity">The entity definition</param>
        /// <param name="schemaName">Optional schema name (for SQL Server, PostgreSQL, Oracle). Default uses default schema.</param>
        /// <param name="dataSourceType">Optional datasource type for context-aware generation. If null, uses SupportedType.</param>
        /// <returns>DDL statement, success indicator, and error message if failed</returns>
        public (string Sql, bool Success, string ErrorMessage) GenerateCreateTableSql(
            EntityStructure entity,
            string schemaName = null,
            DataSourceType? dataSourceType = null)
        {
            return _currentHelper.GenerateCreateTableSql(entity, schemaName, dataSourceType);
        }

        /// <summary>
        /// Generates SQL/query to drop a table/collection.
        /// </summary>
        /// <param name="tableName">The name of the table/collection to drop</param>
        /// <param name="schemaName">Optional schema name for multi-schema datasources</param>
        /// <returns>DDL statement, success indicator, and error message if failed</returns>
        public (string Sql, bool Success, string ErrorMessage) GenerateDropTableSql(string tableName, string schemaName = null)
        {
            return _currentHelper.GenerateDropTableSql(tableName, schemaName);
        }

        /// <summary>
        /// Generates SQL/query to truncate a table/collection (remove all data).
        /// </summary>
        /// <param name="tableName">The name of the table/collection</param>
        /// <param name="schemaName">Optional schema name for multi-schema datasources</param>
        /// <returns>DML statement, success indicator, and error message if failed</returns>
        public (string Sql, bool Success, string ErrorMessage) GenerateTruncateTableSql(string tableName, string schemaName = null)
        {
            return _currentHelper.GenerateTruncateTableSql(tableName, schemaName);
        }

        /// <summary>
        /// Generates SQL/query to create an index on specified columns.
        /// </summary>
        /// <param name="tableName">The table/collection name</param>
        /// <param name="indexName">The name for the index</param>
        /// <param name="columns">Column names to index</param>
        /// <param name="options">Optional configuration (unique, clustered, etc.)</param>
        /// <returns>DDL statement, success indicator, and error message if failed</returns>
        public (string Sql, bool Success, string ErrorMessage) GenerateCreateIndexSql(
            string tableName,
            string indexName,
            string[] columns,
            Dictionary<string, object> options = null)
        {
            return _currentHelper.GenerateCreateIndexSql(tableName, indexName, columns, options);
        }

        /// <summary>
        /// Generates SQL/query to add a column to an existing table/collection.
        /// Unsupported by schema-free datasources.
        /// </summary>
        /// <param name="tableName">The table/collection name</param>
        /// <param name="column">The EntityField to add</param>
        /// <returns>DDL statement, success indicator, and error message if failed</returns>
        public (string Sql, bool Success, string ErrorMessage) GenerateAddColumnSql(string tableName, EntityField column)
        {
            return _currentHelper.GenerateAddColumnSql(tableName, column);
        }

        /// <summary>
        /// Generates SQL/query to modify an existing column definition.
        /// Unsupported by schema-free datasources and has limited support in some RDBMS.
        /// </summary>
        /// <param name="tableName">The table/collection name</param>
        /// <param name="columnName">Name of the column to modify</param>
        /// <param name="newColumn">New column definition</param>
        /// <returns>DDL statement, success indicator, and error message if failed</returns>
        public (string Sql, bool Success, string ErrorMessage) GenerateAlterColumnSql(string tableName, string columnName, EntityField newColumn)
        {
            return _currentHelper.GenerateAlterColumnSql(tableName, columnName, newColumn);
        }

        /// <summary>
        /// Generates SQL/query to drop a column from a table/collection.
        /// Unsupported by schema-free datasources.
        /// </summary>
        /// <param name="tableName">The table/collection name</param>
        /// <param name="columnName">Name of the column to drop</param>
        /// <returns>DDL statement, success indicator, and error message if failed</returns>
        public (string Sql, bool Success, string ErrorMessage) GenerateDropColumnSql(string tableName, string columnName)
        {
            return _currentHelper.GenerateDropColumnSql(tableName, columnName);
        }

        /// <summary>
        /// Generates SQL/query to rename a table/collection.
        /// </summary>
        /// <param name="oldTableName">Current table/collection name</param>
        /// <param name="newTableName">New table/collection name</param>
        /// <returns>DDL statement, success indicator, and error message if failed</returns>
        public (string Sql, bool Success, string ErrorMessage) GenerateRenameTableSql(string oldTableName, string newTableName)
        {
            return _currentHelper.GenerateRenameTableSql(oldTableName, newTableName);
        }

        /// <summary>
        /// Generates SQL/query to rename a column in a table/collection.
        /// </summary>
        /// <param name="tableName">The table/collection name</param>
        /// <param name="oldColumnName">Current column name</param>
        /// <param name="newColumnName">New column name</param>
        /// <returns>DDL statement, success indicator, and error message if failed</returns>
        public (string Sql, bool Success, string ErrorMessage) GenerateRenameColumnSql(string tableName, string oldColumnName, string newColumnName)
        {
            return _currentHelper.GenerateRenameColumnSql(tableName, oldColumnName, newColumnName);
        }

        #endregion

        #region Constraint Operations - Level 2 Schema Integrity

        /// <summary>
        /// Generates SQL/query to add a primary key constraint to a table.
        /// Unsupported by most NoSQL datasources except Cassandra.
        /// </summary>
        /// <param name="tableName">The table/collection name</param>
        /// <param name="columnNames">Columns that form the primary key</param>
        /// <returns>DDL statement, success indicator, and error message if failed</returns>
        public (string Sql, bool Success, string ErrorMessage) GenerateAddPrimaryKeySql(string tableName, params string[] columnNames)
        {
            return _currentHelper.GenerateAddPrimaryKeySql(tableName, columnNames);
        }

        /// <summary>
        /// Generates SQL/query to add a foreign key constraint between two tables.
        /// Unsupported by NoSQL datasources (MongoDB, Redis, etc.).
        /// </summary>
        /// <param name="tableName">The table containing the foreign key</param>
        /// <param name="columnNames">Columns that form the foreign key</param>
        /// <param name="referencedTableName">Referenced table name</param>
        /// <param name="referencedColumnNames">Columns in referenced table</param>
        /// <returns>DDL statement, success indicator, and error message if failed</returns>
        public (string Sql, bool Success, string ErrorMessage) GenerateAddForeignKeySql(
            string tableName,
            string[] columnNames,
            string referencedTableName,
            string[] referencedColumnNames)
        {
            return _currentHelper.GenerateAddForeignKeySql(tableName, columnNames, referencedTableName, referencedColumnNames);
        }

        /// <summary>
        /// Generates SQL/query to add a generic constraint (UNIQUE, CHECK, etc.).
        /// </summary>
        /// <param name="tableName">The table/collection name</param>
        /// <param name="constraintName">Name for the constraint</param>
        /// <param name="constraintDefinition">Constraint definition (e.g., "UNIQUE (col1, col2)" or "CHECK (col1 > 0)")</param>
        /// <returns>DDL statement, success indicator, and error message if failed</returns>
        public (string Sql, bool Success, string ErrorMessage) GenerateAddConstraintSql(string tableName, string constraintName, string constraintDefinition)
        {
            return _currentHelper.GenerateAddConstraintSql(tableName, constraintName, constraintDefinition);
        }

        /// <summary>
        /// Generates a query to retrieve primary key information for a table.
        /// </summary>
        /// <param name="tableName">The table/collection name</param>
        /// <returns>Query statement, success indicator, and error message if failed</returns>
        public (string Query, bool Success, string ErrorMessage) GetPrimaryKeyQuery(string tableName)
        {
            return _currentHelper.GetPrimaryKeyQuery(tableName);
        }

        /// <summary>
        /// Generates a query to retrieve foreign key information for a table.
        /// </summary>
        /// <param name="tableName">The table/collection name</param>
        /// <returns>Query statement, success indicator, and error message if failed</returns>
        public (string Query, bool Success, string ErrorMessage) GetForeignKeysQuery(string tableName)
        {
            return _currentHelper.GetForeignKeysQuery(tableName);
        }

        /// <summary>
        /// Generates a query to retrieve all constraints (PRIMARY, FOREIGN, UNIQUE, CHECK) for a table.
        /// </summary>
        /// <param name="tableName">The table/collection name</param>
        /// <returns>Query statement, success indicator, and error message if failed</returns>
        public (string Query, bool Success, string ErrorMessage) GetConstraintsQuery(string tableName)
        {
            return _currentHelper.GetConstraintsQuery(tableName);
        }

        #endregion

        #region Transaction Control - Level 3 ACID Support

        /// <summary>
        /// Gets the SQL command to begin a transaction.
        /// Returns empty string for datasources that don't support transactions.
        /// </summary>
        public string GetBeginTransactionCommand()
        {
            return _currentHelper.GetBeginTransactionCommand();
        }

        /// <summary>
        /// Gets the SQL command to commit a transaction.
        /// Returns empty string for datasources that don't support transactions.
        /// </summary>
        public string GetCommitTransactionCommand()
        {
            return _currentHelper.GetCommitTransactionCommand();
        }

        /// <summary>
        /// Gets the SQL command to rollback a transaction.
        /// Returns empty string for datasources that don't support transactions.
        /// </summary>
        public string GetRollbackTransactionCommand()
        {
            return _currentHelper.GetRollbackTransactionCommand();
        }

        /// <summary>
        /// Gets the SQL command to set a savepoint in a transaction.
        /// Returns empty string for datasources that don't support savepoints.
        /// </summary>
        /// <param name="savepointName">Name of the savepoint</param>
        public string GetSavepointCommand(string savepointName)
        {
            return _currentHelper.GetSavepointCommand(savepointName);
        }

        /// <summary>
        /// Gets the SQL command to rollback to a savepoint.
        /// Returns empty string for datasources that don't support savepoints.
        /// </summary>
        /// <param name="savepointName">Name of the savepoint to rollback to</param>
        public string GetRollbackToSavepointCommand(string savepointName)
        {
            return _currentHelper.GetRollbackToSavepointCommand(savepointName);
        }

        #endregion

        #region DML Operations (Insert, Update, Delete, Select)

        /// <summary>
        /// Generates SQL/query to insert a single record into a table/collection.
        /// </summary>
        /// <param name="tableName">The table/collection name</param>
        /// <param name="fields">Dictionary of field names and values to insert</param>
        /// <returns>DML statement, success indicator, and error message if failed</returns>
        public (string Sql, bool Success, string ErrorMessage) GenerateInsertSql(string tableName, Dictionary<string, object> fields)
        {
            return _currentHelper.GenerateInsertSql(tableName, fields);
        }

        /// <summary>
        /// Generates SQL/query to insert multiple records into a table/collection.
        /// </summary>
        /// <param name="tableName">The table/collection name</param>
        /// <param name="records">List of dictionaries containing field names and values</param>
        /// <returns>DML statement, success indicator, and error message if failed</returns>
        public (string Sql, bool Success, string ErrorMessage) GenerateBulkInsertSql(string tableName, List<Dictionary<string, object>> records)
        {
            return _currentHelper.GenerateBulkInsertSql(tableName, records);
        }

        /// <summary>
        /// Generates SQL/query to update records in a table/collection.
        /// </summary>
        /// <param name="tableName">The table/collection name</param>
        /// <param name="setFields">Dictionary of field names and values to update</param>
        /// <param name="whereClause">WHERE condition for the update</param>
        /// <returns>DML statement, success indicator, and error message if failed</returns>
        public (string Sql, bool Success, string ErrorMessage) GenerateUpdateSql(
            string tableName,
            Dictionary<string, object> setFields,
            string whereClause)
        {
            return _currentHelper.GenerateUpdateSql(tableName, setFields, whereClause);
        }

        /// <summary>
        /// Generates SQL/query to delete records from a table/collection.
        /// </summary>
        /// <param name="tableName">The table/collection name</param>
        /// <param name="whereClause">WHERE condition for the delete</param>
        /// <returns>DML statement, success indicator, and error message if failed</returns>
        public (string Sql, bool Success, string ErrorMessage) GenerateDeleteSql(string tableName, string whereClause)
        {
            return _currentHelper.GenerateDeleteSql(tableName, whereClause);
        }

        /// <summary>
        /// Generates SQL/query to select records from a table/collection.
        /// </summary>
        /// <param name="tableName">The table/collection name</param>
        /// <param name="columns">Columns to select (null for all columns)</param>
        /// <param name="whereClause">WHERE condition (null for no filter)</param>
        /// <param name="orderBy">ORDER BY clause (null for no ordering)</param>
        /// <param name="limit">Maximum number of records to return (null for no limit)</param>
        /// <param name="offset">Number of records to skip (null for no offset)</param>
        /// <returns>SELECT statement, success indicator, and error message if failed</returns>
        public (string Sql, bool Success, string ErrorMessage) GenerateSelectSql(
            string tableName,
            string[] columns = null,
            string whereClause = null,
            string orderBy = null,
            int? limit = null,
            int? offset = null)
        {
            return _currentHelper.GenerateSelectSql(tableName, columns, whereClause, orderBy, limit, offset);
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Escapes identifiers (table names, column names) according to datasource rules.
        /// </summary>
        /// <param name="identifier">The identifier to escape</param>
        /// <returns>Escaped identifier</returns>
        public string EscapeIdentifier(string identifier)
        {
            return _currentHelper.EscapeIdentifier(identifier);
        }

        /// <summary>
        /// Gets the parameter placeholder format for this datasource.
        /// Examples: "?" for MySQL, "@param" for SQL Server, "$1" for PostgreSQL.
        /// </summary>
        /// <param name="parameterName">The parameter name</param>
        /// <returns>Parameter placeholder string</returns>
        public string GetParameterPlaceholder(string parameterName)
        {
            return _currentHelper.GetParameterPlaceholder(parameterName);
        }

        /// <summary>
        /// Converts a .NET type to the equivalent datasource type name.
        /// </summary>
        /// <param name="netType">The .NET type</param>
        /// <param name="maxLength">Maximum length for string types (optional)</param>
        /// <param name="precision">Precision for decimal/numeric types (optional)</param>
        /// <param name="scale">Scale for decimal/numeric types (optional)</param>
        /// <returns>Datasource type name</returns>
        public string GetDataSourceTypeName(Type netType, int? maxLength = null, int? precision = null, int? scale = null)
        {
            return _currentHelper.GetDataSourceTypeName(netType, maxLength, precision, scale);
        }

        /// <summary>
        /// Gets the maximum identifier length supported by this datasource.
        /// </summary>
        /// <returns>Maximum identifier length in characters</returns>
        public int GetMaxIdentifierLength()
        {
            return _currentHelper.GetMaxIdentifierLength();
        }

        /// <summary>
        /// Checks if the given identifier is valid for this datasource.
        /// </summary>
        /// <param name="identifier">The identifier to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public bool IsValidIdentifier(string identifier)
        {
            return _currentHelper.IsValidIdentifier(identifier);
        }

        /// <summary>
        /// Gets the SQL wildcard character used by this datasource.
        /// Usually "%" for most SQL databases, "*" for some others.
        /// </summary>
        public string GetWildcardCharacter()
        {
            return _currentHelper.GetWildcardCharacter();
        }

        /// <summary>
        /// Gets the concatenation operator used by this datasource.
        /// Examples: "||" for Oracle/SQLite, "+" for SQL Server, "CONCAT()" for MySQL.
        /// </summary>
        public string GetConcatenationOperator()
        {
            return _currentHelper.GetConcatenationOperator();
        }

        #endregion
    }
}