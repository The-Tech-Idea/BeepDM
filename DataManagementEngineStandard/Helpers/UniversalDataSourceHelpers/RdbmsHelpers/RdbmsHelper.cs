using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Helpers.UniversalDataSourceHelpers.Core;

namespace TheTechIdea.Beep.Helpers.UniversalDataSourceHelpers.RdbmsHelpers
{
    /// <summary>
    /// Unified RDBMS helper implementing IDataSourceHelper interface for all RDBMS databases.
    /// Provides a single entry point for all RDBMS query generation and operations.
    /// 
    /// Supported Databases (9):
    /// - SQL Server, MySQL, PostgreSQL, Oracle, SQLite, DB2, Firebird
    /// - Azure SQL, AWS RDS
    /// 
    /// This implementation delegates to the legacy helper classes:
    /// - DatabaseSchemaQueryHelper - Schema and metadata queries
    /// - DatabaseObjectCreationHelper - DDL operations
    /// - DatabaseDMLHelper - DML operations (INSERT, UPDATE, DELETE, SELECT)
    /// - DatabaseEntityHelper - Entity analysis and validation
    /// </summary>
    public partial class RdbmsHelper : IDataSourceHelper
    {
        #region Properties

        /// <summary>
        /// Gets or sets the datasource type this helper is designed for.
        /// </summary>
        public DataSourceType SupportedType { get; set; } = DataSourceType.SqlServer;

        /// <summary>
        /// Gets the human-readable name of the datasource type.
        /// </summary>
        public string Name => $"RDBMS ({SupportedType})";

        /// <summary>
        /// Gets the capabilities of this datasource type.
        /// </summary>
        public DataSourceCapabilities Capabilities => DataSourceCapabilityMatrix.GetCapabilities(SupportedType);

        #endregion

        #region Schema Operations

        /// <summary>
        /// Gets the schema or databases accessible to a user.
        /// Implementation of IDataSourceHelper schema query method.
        /// </summary>
        public (string Query, bool Success) GetSchemaQuery(string userName)
        {
            try
            {
                var query = RDBMSHelpers.DatabaseSchemaQueryHelper.GetSchemasorDatabases(SupportedType, userName);
                return (query, !string.IsNullOrEmpty(query));
            }
            catch
            {
                return (string.Empty, false);
            }
        }

        /// <summary>
        /// Checks if a table/entity exists.
        /// Implementation of IDataSourceHelper table existence check.
        /// </summary>
        public (string Query, bool Success) GetTableExistsQuery(string tableName)
        {
            try
            {
                var query = RDBMSHelpers.DatabaseSchemaQueryHelper.GetTableExistsQuery(
                    SupportedType,
                    tableName,
                    null
                );
                return (query, !string.IsNullOrEmpty(query));
            }
            catch
            {
                return (string.Empty, false);
            }
        }

        /// <summary>
        /// Gets column information for a table.
        /// Implementation of IDataSourceHelper column info retrieval.
        /// </summary>
        public (string Query, bool Success) GetColumnInfoQuery(string tableName)
        {
            try
            {
                var query = RDBMSHelpers.DatabaseSchemaQueryHelper.GetColumnInfoQuery(
                    SupportedType,
                    tableName,
                    null
                );
                return (query, !string.IsNullOrEmpty(query));
            }
            catch
            {
                return (string.Empty, false);
            }
        }

        #endregion

        #region DDL Operations

        /// <summary>
        /// Generates CREATE TABLE SQL from an EntityStructure.
        /// Implementation of IDataSourceHelper DDL create table.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateCreateTableSql(
            EntityStructure entity,
            string schemaName = null,
            DataSourceType? dataSourceType = null)
        {
            try
            {
                var query = RDBMSHelpers.DatabaseObjectCreationHelper.GenerateCreateTableSQL(
                    entity, 
                    dataSourceType ?? SupportedType, 
                    schemaName);
                return (query, !string.IsNullOrEmpty(query), string.Empty);
            }
            catch (Exception ex)
            {
                return (string.Empty, false, ex.Message);
            }
        }

        /// <summary>
        /// Generates DROP TABLE SQL.
        /// Implementation of IDataSourceHelper DDL drop table.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateDropTableSql(string tableName, string schemaName = null)
        {
            try
            {
                var query = RDBMSHelpers.DatabaseObjectCreationHelper.GetDropEntity(SupportedType, tableName);
                return (query, !string.IsNullOrEmpty(query), string.Empty);
            }
            catch (Exception ex)
            {
                return (string.Empty, false, ex.Message);
            }
        }

        /// <summary>
        /// Generates TRUNCATE TABLE SQL.
        /// Implementation of IDataSourceHelper DDL truncate table.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateTruncateTableSql(string tableName, string schemaName = null)
        {
            try
            {
                var query = RDBMSHelpers.DatabaseObjectCreationHelper.GetTruncateTableQuery(SupportedType, tableName, schemaName);
                return (query, !string.IsNullOrEmpty(query), string.Empty);
            }
            catch (Exception ex)
            {
                return (string.Empty, false, ex.Message);
            }
        }

        /// <summary>
        /// Generates CREATE INDEX SQL.
        /// Implementation of IDataSourceHelper DDL create index.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateCreateIndexSql(
            string tableName,
            string indexName,
            string[] columns,
            Dictionary<string, object> options = null)
        {
            try
            {
                // Convert Dictionary<string, object> to Dictionary<string, string> for legacy helper
                var stringOptions = options?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? string.Empty) 
                    ?? new Dictionary<string, string>();
                    
                var query = RDBMSHelpers.DatabaseObjectCreationHelper.GenerateCreateIndexQuery(
                    SupportedType,
                    tableName,
                    indexName,
                    columns,
                    stringOptions
                );
                return (query, !string.IsNullOrEmpty(query), string.Empty);
            }
            catch (Exception ex)
            {
                return (string.Empty, false, ex.Message);
            }
        }

        #endregion

        #region DML Operations

        /// <summary>
        /// Generates INSERT SQL from entity data.
        /// Implementation of IDataSourceHelper DML insert.
        /// </summary>
        public (string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage) GenerateInsertSql(
            string tableName,
            Dictionary<string, object> data)
        {
            try
            {
                // Create minimal entity structure for legacy helper
                var entity = new EntityStructure { EntityName = tableName };
                var (query, parameters, success, error) = RDBMSHelpers.DatabaseDMLHelper.GenerateInsertQuerySafe(
                    entity,
                    data,
                    SupportedType
                );
                return (query, parameters ?? new Dictionary<string, object>(), success, error);
            }
            catch (Exception ex)
            {
                return (string.Empty, new Dictionary<string, object>(), false, ex.Message);
            }
        }

        /// <summary>
        /// Generates UPDATE SQL from entity data and where conditions.
        /// Implementation of IDataSourceHelper DML update.
        /// </summary>
        public (string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage) GenerateUpdateSql(
            string tableName,
            Dictionary<string, object> data,
            Dictionary<string, object> conditions)
        {
            try
            {
                var entity = new EntityStructure { EntityName = tableName };
                var (query, parameters, success, error) = RDBMSHelpers.DatabaseDMLHelper.GenerateUpdateQuerySafe(
                    entity,
                    data,
                    conditions,
                    SupportedType
                );
                return (query, parameters ?? new Dictionary<string, object>(), success, error);
            }
            catch (Exception ex)
            {
                return (string.Empty, new Dictionary<string, object>(), false, ex.Message);
            }
        }

        /// <summary>
        /// Generates DELETE SQL from where conditions.
        /// Implementation of IDataSourceHelper DML delete.
        /// </summary>
        public (string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage) GenerateDeleteSql(
            string tableName,
            Dictionary<string, object> conditions)
        {
            try
            {
                var entity = new EntityStructure { EntityName = tableName };
                var (query, parameters, success, error) = RDBMSHelpers.DatabaseDMLHelper.GenerateDeleteQuerySafe(
                    entity,
                    conditions,
                    SupportedType
                );
                return (query, parameters ?? new Dictionary<string, object>(), success, error);
            }
            catch (Exception ex)
            {
                return (string.Empty, new Dictionary<string, object>(), false, ex.Message);
            }
        }

        /// <summary>
        /// Generates SELECT SQL from entity and optional where conditions.
        /// Implementation of IDataSourceHelper DML select.
        /// </summary>
        public (string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage) GenerateSelectSql(
            string tableName,
            IEnumerable<string> columns = null,
            Dictionary<string, object> conditions = null,
            string orderBy = null,
            int? skip = null,
            int? take = null)
        {
            try
            {
                var entity = new EntityStructure { EntityName = tableName };
                var (query, parameters, success, error) = RDBMSHelpers.DatabaseDMLHelper.GenerateSelectQuerySafe(
                    entity,
                    conditions,
                    SupportedType,
                    take,  // limit
                    skip   // offset
                );
                return (query, parameters ?? new Dictionary<string, object>(), success, error);
            }
            catch (Exception ex)
            {
                return (string.Empty, new Dictionary<string, object>(), false, ex.Message);
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Quotes/escapes an identifier for the specified datasource.
        /// Implementation of IDataSourceHelper identifier quoting.
        /// </summary>
        public string QuoteIdentifier(string identifier)
        {
            try
            {
                return RDBMSHelpers.RDBMSHelper.QuoteIdentifier(identifier, SupportedType);
            }
            catch
            {
                // Fallback to double quotes if method fails
                return $"\"{identifier}\"";
            }
        }

        /// <summary>
        /// Maps a CLR type to the appropriate datasource type.
        /// Implementation of IDataSourceHelper CLR to database type mapping.
        /// </summary>
        public string MapClrTypeToDatasourceType(Type clrType, int? size = null, int? precision = null, int? scale = null)
        {
            try
            {
                return RDBMSHelpers.DatabaseEntityTypeHelper.MapClrTypeToDatabase(clrType, SupportedType);
            }
            catch
            {
                return "VARCHAR(MAX)";  // Fallback for unknown types
            }
        }

        /// <summary>
        /// Maps a datasource type string to a CLR type.
        /// Implementation of IDataSourceHelper database to CLR type mapping.
        /// </summary>
        public Type MapDatasourceTypeToClrType(string datasourceType)
        {
            try
            {
                return RDBMSHelpers.DatabaseEntityTypeHelper.MapDatabaseTypeToClr(datasourceType, SupportedType);
            }
            catch
            {
                return typeof(string);  // Fallback to string for unknown types
            }
        }

        /// <summary>
        /// Validates an entity structure for use with the datasource.
        /// Implementation of IDataSourceHelper entity validation.
        /// </summary>
        public (bool IsValid, List<string> Errors) ValidateEntity(EntityStructure entity)
        {
            var errors = new List<string>();

            try
            {
                if (entity == null)
                {
                    errors.Add("Entity structure cannot be null");
                    return (false, errors);
                }

                if (string.IsNullOrWhiteSpace(entity.EntityName))
                {
                    errors.Add("Entity name is required");
                }

                if (entity.EntityFields == null || !entity.EntityFields.Any())
                {
                    errors.Add("Entity must have at least one field");
                }
                else
                {
                    // Check for reserved keywords
                    var keywordChecker = new RDBMSHelpers.DatabaseEntityReservedKeywordChecker();
                    if (keywordChecker.IsReservedKeyword(entity.EntityName, SupportedType))
                    {
                        errors.Add($"Entity name '{entity.EntityName}' is a reserved keyword in {SupportedType}");
                    }

                    // Validate fields
                    foreach (var field in entity.EntityFields)
                    {
                        if (string.IsNullOrWhiteSpace(field.FieldName))
                        {
                            errors.Add("All fields must have a name");
                        }

                        if (keywordChecker.IsReservedKeyword(field.FieldName, SupportedType))
                        {
                            errors.Add($"Field name '{field.FieldName}' is a reserved keyword in {SupportedType}");
                        }
                    }
                }

                return (errors.Count == 0, errors);
            }
            catch (Exception ex)
            {
                errors.Add($"Validation error: {ex.Message}");
                return (false, errors);
            }
        }

        /// <summary>
        /// Checks whether this datasource supports a specific operation capability.
        /// </summary>
        public bool SupportsCapability(CapabilityType capability)
        {
            var caps = Capabilities;
            return capability switch
            {
                CapabilityType.SupportsTransactions => caps.SupportsTransactions,
                CapabilityType.SupportsJoins => caps.SupportsJoins,
                CapabilityType.SupportsAggregations => caps.SupportsAggregations,
                CapabilityType.SupportsIndexes => caps.SupportsIndexes,
                CapabilityType.SupportsParameterization => caps.SupportsParameterization,
                CapabilityType.SupportsIdentity => caps.SupportsIdentity,
                CapabilityType.SupportsTTL => caps.SupportsTTL,
                CapabilityType.SupportsTemporalTables => caps.SupportsTemporalTables,
                CapabilityType.SupportsWindowFunctions => caps.SupportsWindowFunctions,
                CapabilityType.SupportsStoredProcedures => caps.SupportsStoredProcedures,
                CapabilityType.SupportsBulkOperations => caps.SupportsBulkOperations,
                CapabilityType.SupportsFullTextSearch => caps.SupportsFullTextSearch,
                CapabilityType.SupportsNativeJson => caps.SupportsNativeJson,
                CapabilityType.SupportsPartitioning => caps.SupportsPartitioning,
                CapabilityType.SupportsReplication => caps.SupportsReplication,
                CapabilityType.SupportsViews => caps.SupportsViews,
                CapabilityType.SupportsSchemaEvolution => caps.SupportsSchemaEvolution,
                CapabilityType.IsSchemaEnforced => caps.IsSchemaEnforced,
                _ => false
            };
        }

        /// <summary>
        /// Gets the maximum size allowed for string/varchar columns in this datasource.
        /// Returns -1 for unlimited, 0 for unsupported.
        /// </summary>
        public int GetMaxStringSize()
        {
            return SupportedType switch
            {
                DataSourceType.SqlServer => 8000,
                DataSourceType.Mysql => 65535,
                DataSourceType.Postgre => -1, // Unlimited
                DataSourceType.Oracle => 4000,
                DataSourceType.SqlLite => -1,
                DataSourceType.DB2 => 32704,
                DataSourceType.FireBird => 32765,
                _ => 255 // Safe default
            };
        }

        /// <summary>
        /// Gets the maximum numeric precision supported by this datasource.
        /// For DECIMAL/NUMERIC types, returns total digits. Returns 0 for unlimited.
        /// </summary>
        public int GetMaxNumericPrecision()
        {
            return SupportedType switch
            {
                DataSourceType.SqlServer => 38,
                DataSourceType.Mysql => 65,
                DataSourceType.Postgre => 1000,
                DataSourceType.Oracle => 38,
                DataSourceType.SqlLite => 0, // No fixed precision
                DataSourceType.DB2 => 31,
                DataSourceType.FireBird => 18,
                _ => 18 // Safe default
            };
        }

        /// <summary>
        /// Generates SQL to add a column to an existing table.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateAddColumnSql(string tableName, EntityField column)
        {
            try
            {
                var dataType = MapClrTypeToDatasourceType(column.FieldType);
                var quotedTable = QuoteIdentifier(tableName);
                var quotedColumn = QuoteIdentifier(column.fieldname);
                var sql = $"ALTER TABLE {quotedTable} ADD {quotedColumn} {dataType}";
                return (sql, true, string.Empty);
            }
            catch (Exception ex)
            {
                return (string.Empty, false, ex.Message);
            }
        }

        /// <summary>
        /// Generates SQL to modify an existing column definition.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateAlterColumnSql(string tableName, string columnName, EntityField newColumn)
        {
            try
            {
                var dataType = MapClrTypeToDatasourceType(newColumn.FieldType);
                var quotedTable = QuoteIdentifier(tableName);
                var quotedColumn = QuoteIdentifier(columnName);
                var sql = SupportedType switch
                {
                    DataSourceType.SqlServer => $"ALTER TABLE {quotedTable} ALTER COLUMN {quotedColumn} {dataType}",
                    DataSourceType.Mysql => $"ALTER TABLE {quotedTable} MODIFY COLUMN {quotedColumn} {dataType}",
                    DataSourceType.Postgre => $"ALTER TABLE {quotedTable} ALTER COLUMN {quotedColumn} TYPE {dataType}",
                    DataSourceType.Oracle => $"ALTER TABLE {quotedTable} MODIFY {quotedColumn} {dataType}",
                    _ => $"ALTER TABLE {quotedTable} ALTER COLUMN {quotedColumn} {dataType}"
                };
                return (sql, true, string.Empty);
            }
            catch (Exception ex)
            {
                return (string.Empty, false, ex.Message);
            }
        }

        /// <summary>
        /// Generates SQL to drop a column from a table.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateDropColumnSql(string tableName, string columnName)
        {
            try
            {
                var quotedTable = QuoteIdentifier(tableName);
                var quotedColumn = QuoteIdentifier(columnName);
                var sql = $"ALTER TABLE {quotedTable} DROP COLUMN {quotedColumn}";
                return (sql, true, string.Empty);
            }
            catch (Exception ex)
            {
                return (string.Empty, false, ex.Message);
            }
        }

        /// <summary>
        /// Generates SQL to rename a table.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateRenameTableSql(string oldTableName, string newTableName)
        {
            try
            {
                var quotedOld = QuoteIdentifier(oldTableName);
                var quotedNew = QuoteIdentifier(newTableName);
                var sql = SupportedType switch
                {
                    DataSourceType.SqlServer => $"EXEC sp_rename '{oldTableName}', '{newTableName}'",
                    DataSourceType.Mysql => $"RENAME TABLE {quotedOld} TO {quotedNew}",
                    DataSourceType.Postgre => $"ALTER TABLE {quotedOld} RENAME TO {quotedNew}",
                    DataSourceType.Oracle => $"RENAME {quotedOld} TO {quotedNew}",
                    _ => $"ALTER TABLE {quotedOld} RENAME TO {quotedNew}"
                };
                return (sql, true, string.Empty);
            }
            catch (Exception ex)
            {
                return (string.Empty, false, ex.Message);
            }
        }

        /// <summary>
        /// Generates SQL to rename a column in a table.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateRenameColumnSql(string tableName, string oldColumnName, string newColumnName)
        {
            try
            {
                var quotedTable = QuoteIdentifier(tableName);
                var quotedOld = QuoteIdentifier(oldColumnName);
                var quotedNew = QuoteIdentifier(newColumnName);
                var sql = SupportedType switch
                {
                    DataSourceType.SqlServer => $"EXEC sp_rename '{tableName}.{oldColumnName}', '{newColumnName}', 'COLUMN'",
                    DataSourceType.Mysql => $"ALTER TABLE {quotedTable} RENAME COLUMN {quotedOld} TO {quotedNew}",
                    DataSourceType.Postgre => $"ALTER TABLE {quotedTable} RENAME COLUMN {quotedOld} TO {quotedNew}",
                    DataSourceType.Oracle => $"ALTER TABLE {quotedTable} RENAME COLUMN {quotedOld} TO {quotedNew}",
                    _ => $"ALTER TABLE {quotedTable} RENAME COLUMN {quotedOld} TO {quotedNew}"
                };
                return (sql, true, string.Empty);
            }
            catch (Exception ex)
            {
                return (string.Empty, false, ex.Message);
            }
        }

        #endregion
    }
}
                        {
                            errors.Add($"Field name '{field.FieldName}' is a reserved keyword in {datasourceType}");
                        }
                    }
                }

                return (errors.Count == 0, errors);
            }
            catch (Exception ex)
            {
                errors.Add($"Validation error: {ex.Message}");
                return (false, errors);
            }
        }
    }
}
