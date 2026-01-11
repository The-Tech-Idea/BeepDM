using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Helpers.UniversalDataSourceHelpers.Core;
using TheTechIdea.Beep.Helpers.RDBMSHelpers;
using TheTechIdea.Beep.Helpers.RDBMSHelpers.DMLHelpers;
using TheTechIdea.Beep.Helpers.RDBMSHelpers.EntityHelpers;
using TheTechIdea.Beep.Helpers.DataTypesHelpers;

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
        private readonly IDMEEditor _dmeEditor;

        /// <summary>
        /// Initializes a new instance of the RdbmsHelper class.
        /// </summary>
        /// <param name="dmeEditor">The IDMEEditor instance</param>
        public RdbmsHelper(IDMEEditor dmeEditor)
        {
            _dmeEditor = dmeEditor ?? throw new ArgumentNullException(nameof(dmeEditor));
        }

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
                var query = DatabaseSchemaQueryHelper.GetSchemasorDatabases(SupportedType, userName);
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
                var query = DatabaseSchemaQueryHelper.GetTableExistsQuery(
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
                var query = DatabaseSchemaQueryHelper.GetColumnInfoQuery(
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
                if (entity == null)
                    return (string.Empty, false, "Entity structure cannot be null");

                // Set DatabaseType on entity if not already set
                if (entity.DatabaseType == DataSourceType.NONE || entity.DatabaseType == DataSourceType.Unknown)
                {
                    entity.DatabaseType = dataSourceType ?? SupportedType;
                }

                var (sql, success, errorMessage) = DatabaseObjectCreationHelper.GenerateCreateTableSQL(entity);
                return (sql ?? string.Empty, success, errorMessage ?? string.Empty);
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
                var query = RDBMSHelpers.DatabaseObjectCreationHelper.GenerateCreateIndexQuery(
                    SupportedType,
                    tableName,
                    indexName,
                    columns,
                    options
                );
                return (query ?? string.Empty, !string.IsNullOrEmpty(query), string.Empty);
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
                if (string.IsNullOrWhiteSpace(tableName))
                    return (string.Empty, new Dictionary<string, object>(), false, "Table name cannot be null or empty");
                
                if (data == null || data.Count == 0)
                    return (string.Empty, new Dictionary<string, object>(), false, "Data cannot be null or empty");

                var sql = DatabaseDMLHelper.GenerateInsertQuery(SupportedType, tableName, data);
                var parameters = new Dictionary<string, object>(data);
                return (sql ?? string.Empty, parameters, true, string.Empty);
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
                if (string.IsNullOrWhiteSpace(tableName))
                    return (string.Empty, new Dictionary<string, object>(), false, "Table name cannot be null or empty");
                
                if (data == null || data.Count == 0)
                    return (string.Empty, new Dictionary<string, object>(), false, "Data cannot be null or empty");
                
                if (conditions == null || conditions.Count == 0)
                    return (string.Empty, new Dictionary<string, object>(), false, "Conditions cannot be null or empty");

                var sql = DatabaseDMLHelper.GenerateUpdateQuery(SupportedType, tableName, data, conditions);
                var parameters = new Dictionary<string, object>(data);
                foreach (var kvp in conditions)
                    parameters[kvp.Key] = kvp.Value;
                
                return (sql ?? string.Empty, parameters, true, string.Empty);
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
                if (string.IsNullOrWhiteSpace(tableName))
                    return (string.Empty, new Dictionary<string, object>(), false, "Table name cannot be null or empty");
                
                if (conditions == null || conditions.Count == 0)
                    return (string.Empty, new Dictionary<string, object>(), false, "Conditions cannot be null or empty");

                var sql = DatabaseDMLHelper.GenerateDeleteQuery(SupportedType, tableName, conditions);
                var parameters = new Dictionary<string, object>(conditions);
                
                return (sql ?? string.Empty, parameters, true, string.Empty);
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
                if (string.IsNullOrWhiteSpace(tableName))
                    return (string.Empty, new Dictionary<string, object>(), false, "Table name cannot be null or empty");

                // Build WHERE clause from conditions
                string whereClause = null;
                var parameters = new Dictionary<string, object>();
                if (conditions != null && conditions.Count > 0)
                {
                    var whereParts = new List<string>();
                    foreach (var kvp in conditions)
                    {
                        whereParts.Add($"{QuoteIdentifier(kvp.Key)} = @{kvp.Key}");
                        parameters[kvp.Key] = kvp.Value;
                    }
                    whereClause = string.Join(" AND ", whereParts);
                }

                // Calculate page number from skip/take for paging
                int? pageNumber = null;
                if (skip.HasValue && take.HasValue && take.Value > 0)
                {
                    pageNumber = (skip.Value / take.Value) + 1;
                }

                var sql = DatabaseDMLHelper.GenerateSelectQuery(
                    SupportedType,
                    tableName,
                    columns,
                    whereClause,
                    orderBy,
                    pageNumber,
                    take
                );

                return (sql ?? string.Empty, parameters, !string.IsNullOrEmpty(sql), string.Empty);
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
                return DatabaseDMLUtilities.QuoteIdentifierIfNeeded(identifier, SupportedType);
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
        /// Uses DataTypeMappingRepository for mapping.
        /// </summary>
        public string MapClrTypeToDatasourceType(Type clrType, int? size = null, int? precision = null, int? scale = null)
        {
            try
            {
                if (clrType == null)
                    return "VARCHAR(255)";

                var netTypeName = clrType.FullName ?? clrType.Name;
                
                // Get mappings for this datasource type
                var mappings = DataTypeMappingRepository.GetDataTypes(SupportedType, _dmeEditor);
                if (mappings != null && mappings.Any())
                {
                    // Look for exact .NET type match (preferred first)
                    var exactMatch = mappings.FirstOrDefault(m => 
                        m.NetDataType.Equals(netTypeName, StringComparison.OrdinalIgnoreCase) && m.Fav);
                    
                    if (exactMatch == null)
                    {
                        exactMatch = mappings.FirstOrDefault(m => 
                            m.NetDataType.Equals(netTypeName, StringComparison.OrdinalIgnoreCase));
                    }

                    if (exactMatch != null)
                    {
                        var dataType = exactMatch.DataType;
                        
                        // Apply size/precision/scale if provided
                        if (clrType == typeof(string) && size.HasValue && dataType.Contains("(N)"))
                        {
                            dataType = dataType.Replace("(N)", $"({size.Value})");
                        }
                        else if ((clrType == typeof(decimal) || clrType == typeof(float) || clrType == typeof(double)) 
                                 && precision.HasValue && scale.HasValue && dataType.Contains("(P,S)"))
                        {
                            dataType = dataType.Replace("(P,S)", $"({precision.Value},{scale.Value})");
                        }
                        
                        return dataType;
                    }
                }

                // Fallback to basic mapping logic
                return MapClrTypeToDatabaseTypeFallback(clrType, size, precision, scale);
            }
            catch
            {
                return "VARCHAR(255)";  // Fallback for unknown types
            }
        }

        /// <summary>
        /// Maps a datasource type string to a CLR type.
        /// Implementation of IDataSourceHelper database to CLR type mapping.
        /// Uses DataTypeMappingRepository for reverse mapping.
        /// </summary>
        public Type MapDatasourceTypeToClrType(string datasourceType)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(datasourceType))
                    return typeof(string);

                // Clean the datasource type (remove size/precision)
                var cleanType = CleanDatabaseType(datasourceType);

                // Get mappings for this datasource type
                var mappings = DataTypeMappingRepository.GetDataTypes(SupportedType, _dmeEditor);
                if (mappings != null && mappings.Any())
                {
                    var mapping = mappings.FirstOrDefault(m => 
                        m.DataType.Equals(cleanType, StringComparison.OrdinalIgnoreCase) && m.Fav);
                    
                    if (mapping == null)
                    {
                        mapping = mappings.FirstOrDefault(m => 
                            m.DataType.StartsWith(cleanType, StringComparison.OrdinalIgnoreCase));
                    }

                    if (mapping != null && !string.IsNullOrWhiteSpace(mapping.NetDataType))
                    {
                        var type = Type.GetType(mapping.NetDataType);
                        if (type != null)
                            return type;
                    }
                }

                // Fallback to basic mapping
                return MapDatabaseTypeToClrTypeFallback(cleanType);
            }
            catch
            {
                return typeof(string);  // Fallback to string for unknown types
            }
        }

        /// <summary>
        /// Fallback mapping for CLR type to database type when repository doesn't have the mapping.
        /// </summary>
        private string MapClrTypeToDatabaseTypeFallback(Type clrType, int? size, int? precision, int? scale)
        {
            var baseType = clrType.Name.ToUpper();
            if (clrType == typeof(string))
            {
                return size.HasValue ? $"VARCHAR({size.Value})" : "VARCHAR(MAX)";
            }
            if (clrType == typeof(int))
                return "INT";
            if (clrType == typeof(long))
                return "BIGINT";
            if (clrType == typeof(short))
                return "SMALLINT";
            if (clrType == typeof(byte))
                return "TINYINT";
            if (clrType == typeof(decimal))
                return precision.HasValue && scale.HasValue ? $"DECIMAL({precision.Value},{scale.Value})" : "DECIMAL(18,2)";
            if (clrType == typeof(double) || clrType == typeof(float))
                return "FLOAT";
            if (clrType == typeof(DateTime))
                return "DATETIME";
            if (clrType == typeof(bool))
                return "BIT";
            if (clrType == typeof(Guid))
                return "UNIQUEIDENTIFIER";
            if (clrType == typeof(byte[]))
                return "VARBINARY(MAX)";
            
            return "VARCHAR(255)";
        }

        /// <summary>
        /// Fallback mapping for database type to CLR type when repository doesn't have the mapping.
        /// </summary>
        private Type MapDatabaseTypeToClrTypeFallback(string databaseType)
        {
            var normalized = databaseType.ToUpper();
            
            if (normalized.Contains("VARCHAR") || normalized.Contains("CHAR") || normalized.Contains("TEXT") || normalized.Contains("NVARCHAR"))
                return typeof(string);
            if (normalized.Contains("INT") && normalized.Contains("BIG"))
                return typeof(long);
            if (normalized.Contains("INT"))
                return typeof(int);
            if (normalized.Contains("DECIMAL") || normalized.Contains("NUMERIC") || normalized.Contains("MONEY"))
                return typeof(decimal);
            if (normalized.Contains("FLOAT") || normalized.Contains("DOUBLE") || normalized.Contains("REAL"))
                return typeof(double);
            if (normalized.Contains("DATE") || normalized.Contains("TIME") || normalized.Contains("TIMESTAMP"))
                return typeof(DateTime);
            if (normalized.Contains("BIT") || normalized.Contains("BOOL"))
                return typeof(bool);
            if (normalized.Contains("GUID") || normalized.Contains("UNIQUEIDENTIFIER") || normalized.Contains("UUID"))
                return typeof(Guid);
            if (normalized.Contains("BINARY") || normalized.Contains("BLOB") || normalized.Contains("IMAGE") || normalized.Contains("BYTEA"))
                return typeof(byte[]);
            
            return typeof(string);
        }

        /// <summary>
        /// Cleans database type string by removing size/precision/scale parameters.
        /// </summary>
        private string CleanDatabaseType(string databaseType)
        {
            if (string.IsNullOrWhiteSpace(databaseType))
                return databaseType;

            // Remove parentheses and everything inside (e.g., VARCHAR(255) -> VARCHAR)
            var index = databaseType.IndexOf('(');
            if (index > 0)
                return databaseType.Substring(0, index).Trim();
            
            return databaseType.Trim();
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

                if (entity.Fields == null || !entity.Fields.Any())
                {
                    errors.Add("Entity must have at least one field");
                }
                else
                {
                    // Check for reserved keywords (static method, no instance needed)
                    if (DatabaseEntityReservedKeywordChecker.IsReservedKeyword(entity.EntityName, SupportedType))
                    {
                        errors.Add($"Entity name '{entity.EntityName}' is a reserved keyword in {SupportedType}");
                    }

                    // Validate fields
                    foreach (var field in entity.Fields)
                    {
                        if (string.IsNullOrWhiteSpace(field.fieldname))
                        {
                            errors.Add("All fields must have a name");
                        }

                        if (DatabaseEntityReservedKeywordChecker.IsReservedKeyword(field.fieldname, SupportedType))
                        {
                            errors.Add($"Field name '{field.fieldname}' is a reserved keyword in {SupportedType}");
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
                var dataType = column.fieldtype;
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
                var dataType = newColumn.fieldtype;
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
