using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Core;

namespace TheTechIdea.Beep.Editor
{
    /// <summary>
    /// Defines the contract for all datasource-specific helper implementations.
    /// Each datasource type (RDBMS, MongoDB, Redis, etc.) should have an implementation
    /// of this interface to provide database-agnostic operations.
    /// 
    /// This abstraction enables:
    /// - Unified API for different datasource types
    /// - Query generation for each datasource's native language/syntax
    /// - Feature detection for graceful degradation
    /// - Type mapping across different database systems
    /// </summary>
    public interface IDataSourceHelper
    {
        /// <summary>
        /// Gets or sets the datasource type this helper is designed for.
        /// Setting this allows dynamic switching between different datasource types at runtime.
        /// </summary>
        DataSourceType SupportedType { get; set; }

        /// <summary>
        /// Gets the human-readable name of the datasource type.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the capabilities of this datasource type.
        /// </summary>
        DataSourceCapabilities Capabilities { get; }

        #region Schema Operations

        /// <summary>
        /// Gets a query to retrieve schemas or databases accessible to the specified user.
        /// </summary>
        /// <param name="userName">The username to check privileges for (can be null for some datasources)</param>
        /// <returns>Query string and success indicator</returns>
        (string Query, bool Success) GetSchemaQuery(string userName);

        /// <summary>
        /// Gets a query to check if a table/collection exists.
        /// </summary>
        /// <param name="tableName">The name of the table/collection</param>
        /// <returns>Query string and success indicator</returns>
        (string Query, bool Success) GetTableExistsQuery(string tableName);

        /// <summary>
        /// Gets a query to retrieve column/field information for a table/collection.
        /// </summary>
        /// <param name="tableName">The name of the table/collection</param>
        /// <returns>Query string and success indicator</returns>
        (string Query, bool Success) GetColumnInfoQuery(string tableName);

        #endregion

        #region Ddl Operations (Create, Alter, Drop) - Level 1 Schema Operations

        /// <summary>
        /// Generates SQL/query to create a table/collection from an entity structure.
        /// Supports optional schema name and datasource type for multi-dialect generation.
        /// </summary>
        /// <param name="entity">The entity definition</param>
        /// <param name="schemaName">Optional schema name (for SQL Server, PostgreSQL, Oracle). Default uses default schema.</param>
        /// <param name="dataSourceType">Optional datasource type for context-aware generation. If null, uses SupportedType.</param>
        /// <returns>DDL statement, success indicator, and error message if failed</returns>
        (string Sql, bool Success, string ErrorMessage) GenerateCreateTableSql(
            EntityStructure entity,
            string schemaName = null,
            DataSourceType? dataSourceType = null);

        /// <summary>
        /// Generates SQL/query to drop a table/collection.
        /// </summary>
        /// <param name="tableName">The name of the table/collection to drop</param>
        /// <param name="schemaName">Optional schema name for multi-schema datasources</param>
        /// <returns>DDL statement, success indicator, and error message if failed</returns>
        (string Sql, bool Success, string ErrorMessage) GenerateDropTableSql(string tableName, string schemaName = null);

        /// <summary>
        /// Generates SQL/query to truncate a table/collection (remove all data).
        /// </summary>
        /// <param name="tableName">The name of the table/collection</param>
        /// <param name="schemaName">Optional schema name for multi-schema datasources</param>
        /// <returns>DML statement, success indicator, and error message if failed</returns>
        (string Sql, bool Success, string ErrorMessage) GenerateTruncateTableSql(string tableName, string schemaName = null);

        /// <summary>
        /// Generates SQL/query to create an index on specified columns.
        /// </summary>
        /// <param name="tableName">The table/collection name</param>
        /// <param name="indexName">The name for the index</param>
        /// <param name="columns">Column names to index</param>
        /// <param name="options">Optional configuration (unique, clustered, etc.)</param>
        /// <returns>DDL statement, success indicator, and error message if failed</returns>
        (string Sql, bool Success, string ErrorMessage) GenerateCreateIndexSql(
            string tableName,
            string indexName,
            string[] columns,
            Dictionary<string, object> options = null);

        /// <summary>
        /// Generates SQL/query to add a column to an existing table/collection.
        /// Unsupported by schema-free datasources.
        /// </summary>
        /// <param name="tableName">The table/collection name</param>
        /// <param name="column">The EntityField to add</param>
        /// <returns>DDL statement, success indicator, and error message if failed</returns>
        (string Sql, bool Success, string ErrorMessage) GenerateAddColumnSql(string tableName, EntityField column);

        /// <summary>
        /// Generates SQL/query to modify an existing column definition.
        /// Unsupported by schema-free datasources and has limited support in some RDBMS.
        /// </summary>
        /// <param name="tableName">The table/collection name</param>
        /// <param name="columnName">Name of the column to modify</param>
        /// <param name="newColumn">New column definition</param>
        /// <returns>DDL statement, success indicator, and error message if failed</returns>
        (string Sql, bool Success, string ErrorMessage) GenerateAlterColumnSql(string tableName, string columnName, EntityField newColumn);

        /// <summary>
        /// Generates SQL/query to drop a column from a table/collection.
        /// Unsupported by schema-free datasources.
        /// </summary>
        /// <param name="tableName">The table/collection name</param>
        /// <param name="columnName">Name of the column to drop</param>
        /// <returns>DDL statement, success indicator, and error message if failed</returns>
        (string Sql, bool Success, string ErrorMessage) GenerateDropColumnSql(string tableName, string columnName);

        /// <summary>
        /// Generates SQL/query to rename a table/collection.
        /// </summary>
        /// <param name="oldTableName">Current table/collection name</param>
        /// <param name="newTableName">New table/collection name</param>
        /// <returns>DDL statement, success indicator, and error message if failed</returns>
        (string Sql, bool Success, string ErrorMessage) GenerateRenameTableSql(string oldTableName, string newTableName);

        /// <summary>
        /// Generates SQL/query to rename a column in a table/collection.
        /// </summary>
        /// <param name="tableName">The table/collection name</param>
        /// <param name="oldColumnName">Current column name</param>
        /// <param name="newColumnName">New column name</param>
        /// <returns>DDL statement, success indicator, and error message if failed</returns>
        (string Sql, bool Success, string ErrorMessage) GenerateRenameColumnSql(string tableName, string oldColumnName, string newColumnName);

        #endregion

        #region Constraint Operations - Level 2 Schema Integrity

        /// <summary>
        /// Generates SQL/query to add a primary key constraint to a table.
        /// Unsupported by most NoSQL datasources except Cassandra.
        /// </summary>
        /// <param name="tableName">The table/collection name</param>
        /// <param name="columnNames">Columns that form the primary key</param>
        /// <returns>DDL statement, success indicator, and error message if failed</returns>
        (string Sql, bool Success, string ErrorMessage) GenerateAddPrimaryKeySql(string tableName, params string[] columnNames);

        /// <summary>
        /// Generates SQL/query to add a foreign key constraint between two tables.
        /// Unsupported by NoSQL datasources (MongoDB, Redis, etc.).
        /// </summary>
        /// <param name="tableName">The table containing the foreign key</param>
        /// <param name="columnNames">Columns that form the foreign key</param>
        /// <param name="referencedTableName">Referenced table name</param>
        /// <param name="referencedColumnNames">Columns in referenced table</param>
        /// <returns>DDL statement, success indicator, and error message if failed</returns>
        (string Sql, bool Success, string ErrorMessage) GenerateAddForeignKeySql(
            string tableName,
            string[] columnNames,
            string referencedTableName,
            string[] referencedColumnNames);

        /// <summary>
        /// Generates SQL/query to add a generic constraint (UNIQUE, CHECK, etc.).
        /// </summary>
        /// <param name="tableName">The table/collection name</param>
        /// <param name="constraintName">Name for the constraint</param>
        /// <param name="constraintDefinition">Constraint definition (e.g., "UNIQUE (col1, col2)" or "CHECK (col1 > 0)")</param>
        /// <returns>DDL statement, success indicator, and error message if failed</returns>
        (string Sql, bool Success, string ErrorMessage) GenerateAddConstraintSql(string tableName, string constraintName, string constraintDefinition);

        /// <summary>
        /// Generates a query to retrieve primary key information for a table.
        /// </summary>
        /// <param name="tableName">The table/collection name</param>
        /// <returns>Query statement, success indicator, and error message if failed</returns>
        (string Query, bool Success, string ErrorMessage) GetPrimaryKeyQuery(string tableName);

        /// <summary>
        /// Generates a query to retrieve foreign key information for a table.
        /// </summary>
        /// <param name="tableName">The table/collection name</param>
        /// <returns>Query statement, success indicator, and error message if failed</returns>
        (string Query, bool Success, string ErrorMessage) GetForeignKeysQuery(string tableName);

        /// <summary>
        /// Generates a query to retrieve all constraints (PRIMARY, FOREIGN, UNIQUE, CHECK) for a table.
        /// </summary>
        /// <param name="tableName">The table/collection name</param>
        /// <returns>Query statement, success indicator, and error message if failed</returns>
        (string Query, bool Success, string ErrorMessage) GetConstraintsQuery(string tableName);

        #endregion

        #region Transaction Control - Level 3 ACID Support

        /// <summary>
        /// Generates SQL/command to begin a transaction.
        /// Unsupported by datasources without ACID (MongoDB pre-v4.0, Redis with limited scope, etc.).
        /// </summary>
        /// <returns>SQL command, success indicator, and error message if failed</returns>
        (string Sql, bool Success, string ErrorMessage) GenerateBeginTransactionSql();

        /// <summary>
        /// Generates SQL/command to commit a transaction.
        /// Unsupported by datasources without ACID support.
        /// </summary>
        /// <returns>SQL command, success indicator, and error message if failed</returns>
        (string Sql, bool Success, string ErrorMessage) GenerateCommitSql();

        /// <summary>
        /// Generates SQL/command to rollback a transaction.
        /// Unsupported by datasources without ACID support.
        /// </summary>
        /// <returns>SQL command, success indicator, and error message if failed</returns>
        (string Sql, bool Success, string ErrorMessage) GenerateRollbackSql();

        #endregion

        #region DML Operations (Insert, Update, Delete, Select)

        /// <summary>
        /// Generates SQL/query to insert a single record.
        /// </summary>
        /// <param name="tableName">The table/collection name</param>
        /// <param name="data">Column→Value mappings</param>
        /// <returns>DML statement, parameters dict, success indicator, and error message if failed</returns>
        (string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage) GenerateInsertSql(
            string tableName,
            Dictionary<string, object> data);

        /// <summary>
        /// Generates SQL/query to update records matching conditions.
        /// </summary>
        /// <param name="tableName">The table/collection name</param>
        /// <param name="data">Column→Value mappings to update</param>
        /// <param name="conditions">Condition column→Value mappings for WHERE clause</param>
        /// <returns>DML statement, parameters dict, success indicator, and error message if failed</returns>
        (string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage) GenerateUpdateSql(
            string tableName,
            Dictionary<string, object> data,
            Dictionary<string, object> conditions);

        /// <summary>
        /// Generates SQL/query to delete records matching conditions.
        /// </summary>
        /// <param name="tableName">The table/collection name</param>
        /// <param name="conditions">Condition column→Value mappings for WHERE clause</param>
        /// <returns>DML statement, parameters dict, success indicator, and error message if failed</returns>
        (string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage) GenerateDeleteSql(
            string tableName,
            Dictionary<string, object> conditions);

        /// <summary>
        /// Generates SQL/query to select records with optional filtering and paging.
        /// </summary>
        /// <param name="tableName">The table/collection name</param>
        /// <param name="columns">Columns to select (null = all)</param>
        /// <param name="conditions">Optional WHERE conditions</param>
        /// <param name="orderBy">Optional ORDER BY clause</param>
        /// <param name="skip">Records to skip (for paging)</param>
        /// <param name="take">Records to take (for paging)</param>
        /// <returns>Query statement, parameters dict, success indicator, and error message if failed</returns>
        (string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage) GenerateSelectSql(
            string tableName,
            IEnumerable<string> columns = null,
            Dictionary<string, object> conditions = null,
            string orderBy = null,
            int? skip = null,
            int? take = null);

        #endregion

        #region Utility Methods

        /// <summary>
        /// Safely quotes/escapes identifiers (table names, column names) according to datasource rules.
        /// </summary>
        /// <param name="identifier">The identifier to quote</param>
        /// <returns>Properly quoted identifier</returns>
        string QuoteIdentifier(string identifier);

        /// <summary>
        /// Maps a C# type to the datasource's native type with optional size/precision constraints.
        /// </summary>
        /// <param name="clrType">The C# type (e.g., typeof(int))</param>
        /// <param name="size">Optional size constraint for string types (e.g., 255 for VARCHAR)</param>
        /// <param name="precision">Optional precision for numeric types (e.g., 18 for DECIMAL)</param>
        /// <param name="scale">Optional scale for numeric types (e.g., 2 for DECIMAL)</param>
        /// <returns>Datasource-specific type name (e.g., "INT", "VARCHAR(255)", "DECIMAL(18,2)")</returns>
        string MapClrTypeToDatasourceType(Type clrType, int? size = null, int? precision = null, int? scale = null);

        /// <summary>
        /// Maps a datasource type to the corresponding C# type.
        /// </summary>
        /// <param name="datasourceType">The datasource type name</param>
        /// <returns>Corresponding C# Type</returns>
        Type MapDatasourceTypeToClrType(string datasourceType);

        /// <summary>
        /// Validates whether an entity structure is compatible with this datasource.
        /// </summary>
        /// <param name="entity">The entity to validate</param>
        /// <returns>Validation result with any error messages</returns>
        (bool IsValid, List<string> Errors) ValidateEntity(EntityStructure entity);

        /// <summary>
        /// Checks whether this datasource supports a specific operation capability.
        /// Used for graceful degradation when features aren't available.
        /// </summary>
        /// <param name="capability">The capability to check</param>
        /// <returns>True if supported, false otherwise</returns>
        bool SupportsCapability(CapabilityType capability);

        /// <summary>
        /// Gets the maximum size allowed for string/varchar columns in this datasource.
        /// Returns -1 for unlimited, 0 for unsupported.
        /// </summary>
        /// <returns>Maximum character length</returns>
        int GetMaxStringSize();

        /// <summary>
        /// Gets the maximum numeric precision supported by this datasource.
        /// For DECIMAL/NUMERIC types, returns total digits. Returns 0 for unlimited.
        /// </summary>
        /// <returns>Maximum precision</returns>
        int GetMaxNumericPrecision();

        #endregion
    }
}
