using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Helpers.UniversalDataSourceHelpers.Core;
using TheTechIdea.Beep.Helpers.DataTypesHelpers;

namespace TheTechIdea.Beep.Helpers.UniversalDataSourceHelpers.RedisHelpers
{
    /// <summary>
    /// Redis-specific helper for query generation and operations.
    /// Redis is an in-memory key-value store with support for various data structures.
    /// This helper generates Redis commands and Lua scripts for operations.
    ///
    /// Note: Redis doesn't have traditional schemas. Key patterns and data structures are used.
    /// Lua scripts provide atomic multi-command operations.
    /// TTL (Time-To-Live) is a first-class feature.
    /// </summary>
    public class RedisHelper : IDataSourceHelper
    {
        private readonly IDMEEditor _dmeEditor;

        /// <summary>
        /// Initializes a new instance of the RedisHelper class.
        /// </summary>
        /// <param name="dmeEditor">The IDMEEditor instance</param>
        public RedisHelper(IDMEEditor dmeEditor)
        {
            _dmeEditor = dmeEditor ?? throw new ArgumentNullException(nameof(dmeEditor));
        }

        public DataSourceType SupportedType { get; set; } = DataSourceType.Redis;
        public string Name => "Redis";

        public DataSourceCapabilities Capabilities =>
            DataSourceCapabilityMatrix.GetCapabilities(SupportedType);

        #region Schema Operations

        /// <summary>
        /// Gets a command to retrieve all keys matching a pattern.
        /// </summary>
        public (string Query, bool Success) GetSchemaQuery(string userName = null)
        {
            try
            {
                // Redis command to list all keys
                const string query = "KEYS *";
                return (query, true);
            }
            catch
            {
                return ("", false);
            }
        }

        /// <summary>
        /// Gets a command to check if a key exists.
        /// </summary>
        public (string Query, bool Success) GetTableExistsQuery(string keyPattern)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyPattern))
                    return ("", false);

                string query = $"EXISTS {keyPattern}";
                return (query, true);
            }
            catch
            {
                return ("", false);
            }
        }

        /// <summary>
        /// Gets a command to retrieve information about a key's type and TTL.
        /// </summary>
        public (string Query, bool Success) GetColumnInfoQuery(string keyName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyName))
                    return ("", false);

                // Redis pipeline to get type and TTL
                string query = $"TYPE {keyName}\nTTL {keyName}";
                return (query, true);
            }
            catch
            {
                return ("", false);
            }
        }

        #endregion

        #region DDL Operations (Create, Alter, Drop) - Level 1 Schema Operations

        /// <summary>
        /// Redis doesn't require explicit table/collection creation.
        /// Keys are created implicitly when set.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateCreateTableSql(
            EntityStructure entity,
            string schemaName = null,
            DataSourceType? dataSourceType = null)
        {
            return ("", true, "Redis keys are created implicitly - no DDL needed");
        }

        /// <summary>
        /// Generates Redis command to delete keys matching a pattern.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateDropTableSql(string keyPattern, string schemaName = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyPattern))
                    return ("", false, "Key pattern cannot be empty");

                // Use Lua script for atomic deletion of multiple keys
                string script = $@"
local keys = redis.call('KEYS', '{keyPattern}')
for i, key in ipairs(keys) do
    redis.call('DEL', key)
end
return #keys";

                return (script, true, "");
            }
            catch (Exception ex)
            {
                return ("", false, ex.Message);
            }
        }

        /// <summary>
        /// Redis doesn't support truncate operations.
        /// Use DEL command for individual keys.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateTruncateTableSql(string keyPattern, string schemaName = null)
        {
            return ("", false, "Redis does not support truncate operations - use DEL for individual keys");
        }

        /// <summary>
        /// Redis doesn't support traditional indexes.
        /// Secondary indexes would need to be implemented as separate keys.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateCreateIndexSql(
            string tableName,
            string indexName,
            string[] columns,
            Dictionary<string, object> options = null)
        {
            return ("", false, "Redis does not support traditional indexes - implement as separate keys");
        }

        /// <summary>
        /// Redis is schema-less, so adding fields is not applicable.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateAddColumnSql(string tableName, EntityField column)
        {
            if (string.IsNullOrWhiteSpace(tableName) || column == null)
                return ("", false, "Table name or column is missing");

            switch (SupportedType)
            {
                case DataSourceType.ApacheIgnite:
                case DataSourceType.GridGain:
                case DataSourceType.Hazelcast:
                    return GenerateSqlAddColumn(tableName, column);
                case DataSourceType.Redis:
                case DataSourceType.Memcached:
                case DataSourceType.ChronicleMap:
                case DataSourceType.InMemoryCache:
                case DataSourceType.CachedMemory:
                case DataSourceType.RealIM:
                case DataSourceType.Petastorm:
                case DataSourceType.RocketSet:
                    return ("", true, $"{SupportedType} is schema-less - no DDL required");
                default:
                    return ("", true, "Schema is flexible - no DDL required");
            }
        }

        /// <summary>
        /// Redis is schema-less, so altering fields is not applicable.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateAlterColumnSql(string tableName, string columnName, EntityField newColumn)
        {
            return ("", true, "Redis is schema-less - field types are dynamic");
        }

        /// <summary>
        /// Redis is schema-less, so dropping fields is not applicable.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateDropColumnSql(string tableName, string columnName)
        {
            return ("", true, "Redis is schema-less - fields cannot be dropped individually");
        }

        /// <summary>
        /// Redis doesn't support renaming keys directly.
        /// Would require copying data and deleting old key.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateRenameTableSql(string oldKeyName, string newKeyName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(oldKeyName) || string.IsNullOrWhiteSpace(newKeyName))
                    return ("", false, "Key names cannot be empty");

                // Lua script to rename key atomically
                string script = $@"
local value = redis.call('DUMP', '{oldKeyName}')
if value then
    redis.call('RESTORE', '{newKeyName}', 0, value)
    redis.call('DEL', '{oldKeyName}')
    return 1
else
    return 0
end";

                return (script, true, "");
            }
            catch (Exception ex)
            {
                return ("", false, ex.Message);
            }
        }

        /// <summary>
        /// Redis doesn't support renaming fields in data structures.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateRenameColumnSql(string tableName, string oldColumnName, string newColumnName)
        {
            return ("", false, "Redis does not support renaming fields - requires manual data migration");
        }

        #endregion

        private (string Sql, bool Success, string ErrorMessage) GenerateSqlAddColumn(string tableName, EntityField column)
        {
            var dataType = ResolveFieldType(column);
            var sql = $"ALTER TABLE {tableName} ADD {column.fieldname} {dataType}";
            return (sql, true, "SQL add-column for in-memory grid");
        }

        private string ResolveFieldType(EntityField column)
        {
            if (column == null || string.IsNullOrWhiteSpace(column.fieldtype))
                return "VARCHAR(255)";

            var clrType = ResolveClrType(column.fieldtype);
            if (clrType != null)
            {
                int? size = column.Size > 0 ? column.Size : null;
                int? precision = column.NumericPrecision > 0 ? column.NumericPrecision : null;
                int? scale = column.NumericScale > 0 ? column.NumericScale : null;
                return MapClrTypeToDatasourceType(clrType, size, precision, scale);
            }

            return column.fieldtype;
        }

        private static Type ResolveClrType(string fieldType)
        {
            var type = Type.GetType(fieldType);
            if (type != null)
                return type;

            type = Type.GetType($"System.{fieldType}");
            if (type != null)
                return type;

            return fieldType.ToLowerInvariant() switch
            {
                "string" => typeof(string),
                "int" or "int32" => typeof(int),
                "long" or "int64" => typeof(long),
                "short" or "int16" => typeof(short),
                "byte" => typeof(byte),
                "decimal" => typeof(decimal),
                "double" => typeof(double),
                "float" => typeof(float),
                "single" => typeof(float),
                "bool" or "boolean" => typeof(bool),
                "datetime" => typeof(DateTime),
                "guid" => typeof(Guid),
                _ => null
            };
        }

        #region Constraint Operations - Level 2 Schema Integrity

        /// <summary>
        /// Redis doesn't support primary key constraints.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateAddPrimaryKeySql(string tableName, params string[] columnNames)
        {
            return ("", false, "Redis does not support primary key constraints");
        }

        /// <summary>
        /// Redis doesn't support foreign key constraints.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateAddForeignKeySql(
            string tableName,
            string[] columnNames,
            string referencedTableName,
            string[] referencedColumnNames)
        {
            return ("", false, "Redis does not support foreign key constraints");
        }

        /// <summary>
        /// Redis doesn't support traditional constraints.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateAddConstraintSql(string tableName, string constraintName, string constraintDefinition)
        {
            return ("", false, "Redis does not support traditional constraints");
        }

        /// <summary>
        /// Redis doesn't have primary key queries.
        /// </summary>
        public (string Query, bool Success, string ErrorMessage) GetPrimaryKeyQuery(string tableName)
        {
            return ("", false, "Redis does not support primary key queries");
        }

        /// <summary>
        /// Redis doesn't support foreign key constraints.
        /// </summary>
        public (string Query, bool Success, string ErrorMessage) GetForeignKeysQuery(string tableName)
        {
            return ("", false, "Redis does not support foreign key constraints");
        }

        /// <summary>
        /// Redis doesn't support traditional constraints.
        /// </summary>
        public (string Query, bool Success, string ErrorMessage) GetConstraintsQuery(string tableName)
        {
            return ("", false, "Redis does not support traditional constraints");
        }

        #endregion

        #region Transaction Control - Level 3 ACID Support

        /// <summary>
        /// Generates Redis MULTI command to start a transaction.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateBeginTransactionSql()
        {
            try
            {
                const string command = "MULTI";
                return (command, true, "");
            }
            catch (Exception ex)
            {
                return ("", false, ex.Message);
            }
        }

        /// <summary>
        /// Generates Redis EXEC command to commit a transaction.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateCommitSql()
        {
            try
            {
                const string command = "EXEC";
                return (command, true, "");
            }
            catch (Exception ex)
            {
                return ("", false, ex.Message);
            }
        }

        /// <summary>
        /// Generates Redis DISCARD command to rollback a transaction.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateRollbackSql()
        {
            try
            {
                const string command = "DISCARD";
                return (command, true, "");
            }
            catch (Exception ex)
            {
                return ("", false, ex.Message);
            }
        }

        #endregion

        #region DML Operations (Insert, Update, Delete, Select)

        /// <summary>
        /// Generates Redis command to set a key-value pair.
        /// </summary>
        public (string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage) GenerateInsertSql(
            string keyName,
            Dictionary<string, object> data)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyName) || data == null || !data.Any())
                    return ("", null, false, "Invalid key name or data");

                // For simple key-value, use SET
                if (data.Count == 1 && data.ContainsKey("value"))
                {
                    string insertCommand = $"SET {keyName} {data["value"]}";
                    return (insertCommand, data, true, "");
                }

                // For complex data structures, use appropriate Redis type
                // This is a simplified implementation
                string jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                string complexCommand = $"SET {keyName} {jsonData}";

                return (complexCommand, data, true, "");
            }
            catch (Exception ex)
            {
                return ("", null, false, ex.Message);
            }
        }

        /// <summary>
        /// Generates Redis command to update a key's value or TTL.
        /// </summary>
        public (string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage) GenerateUpdateSql(
            string keyName,
            Dictionary<string, object> data,
            Dictionary<string, object> conditions)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyName) || data == null || !data.Any())
                    return ("", null, false, "Invalid key name or data");

                // Redis doesn't have conditional updates like SQL
                // This is a simplified implementation
                string jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                string command = $"SET {keyName} {jsonData}";

                var parameters = new Dictionary<string, object>();
                foreach (var kvp in data)
                    parameters[kvp.Key] = kvp.Value;
                if (conditions != null)
                    foreach (var kvp in conditions)
                        parameters[kvp.Key] = kvp.Value;

                return (command, parameters, true, "");
            }
            catch (Exception ex)
            {
                return ("", null, false, ex.Message);
            }
        }

        /// <summary>
        /// Generates Redis DEL command to delete keys.
        /// </summary>
        public (string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage) GenerateDeleteSql(
            string keyPattern,
            Dictionary<string, object> conditions)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyPattern))
                    return ("", null, false, "Invalid key pattern");

                string command = $"DEL {keyPattern}";
                return (command, conditions, true, "");
            }
            catch (Exception ex)
            {
                return ("", null, false, ex.Message);
            }
        }

        /// <summary>
        /// Generates Redis GET command to retrieve key values.
        /// </summary>
        public (string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage) GenerateSelectSql(
            string keyPattern,
            IEnumerable<string> columns = null,
            Dictionary<string, object> conditions = null,
            string orderBy = null,
            int? skip = null,
            int? take = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyPattern))
                    return ("", null, false, "Invalid key pattern");

                // For simple key-value retrieval
                string command = $"GET {keyPattern}";

                // Redis doesn't support complex queries like SQL
                // This is a simplified implementation
                var parameters = new Dictionary<string, object>();
                if (conditions != null)
                    foreach (var kvp in conditions)
                        parameters[kvp.Key] = kvp.Value;

                return (command, parameters, true, "");
            }
            catch (Exception ex)
            {
                return ("", null, false, ex.Message);
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Redis doesn't require identifier quoting for key names.
        /// Returns the identifier as-is.
        /// </summary>
        public string QuoteIdentifier(string identifier)
        {
            return identifier;
        }

        /// <summary>
        /// Maps C# types to Redis string representations.
        /// </summary>
        /// <summary>
        /// Maps C# types to Redis types.
        /// Uses DataTypeMappingRepository for mapping.
        /// </summary>
        public string MapClrTypeToDatasourceType(Type clrType, int? size = null, int? precision = null, int? scale = null)
        {
            try
            {
                if (clrType == null)
                    return "string";

                var netTypeName = clrType.FullName ?? clrType.Name;
                
                var mappings = DataTypeMappingRepository.GetDataTypes(SupportedType, _dmeEditor);
                if (mappings != null && mappings.Any())
                {
                    var exactMatch = mappings.FirstOrDefault(m => 
                        m.NetDataType.Equals(netTypeName, StringComparison.OrdinalIgnoreCase) && m.Fav)
                        ?? mappings.FirstOrDefault(m => m.NetDataType.Equals(netTypeName, StringComparison.OrdinalIgnoreCase));
                    
                    if (exactMatch != null)
                        return exactMatch.DataType;
                }

                return "string"; // Minimal fallback
            }
            catch
            {
                return "string"; // Minimal fallback
            }
        }

        /// <summary>
        /// Maps Redis types back to C# types.
        /// Uses DataTypeMappingRepository for mapping.
        /// </summary>
        public Type MapDatasourceTypeToClrType(string datasourceType)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(datasourceType))
                    return typeof(string);

                var cleanType = datasourceType.Trim();
                var mappings = DataTypeMappingRepository.GetDataTypes(SupportedType, _dmeEditor);
                if (mappings != null && mappings.Any())
                {
                    var mapping = mappings.FirstOrDefault(m => 
                        m.DataType.Equals(cleanType, StringComparison.OrdinalIgnoreCase) && m.Fav)
                        ?? mappings.FirstOrDefault(m => m.DataType.StartsWith(cleanType, StringComparison.OrdinalIgnoreCase));
                    
                    if (mapping != null && !string.IsNullOrWhiteSpace(mapping.NetDataType))
                    {
                        var type = Type.GetType(mapping.NetDataType);
                        if (type != null)
                            return type;
                    }
                }

                return typeof(string); // Minimal fallback
            }
            catch
            {
                return typeof(string); // Minimal fallback
            }
        }

        /// <summary>
        /// Validates entity structure for Redis compatibility.
        /// Redis is very flexible with key naming and data types.
        /// </summary>
        public (bool IsValid, List<string> Errors) ValidateEntity(EntityStructure entity)
        {
            var errors = new List<string>();

            if (entity == null)
            {
                errors.Add("Entity cannot be null");
                return (false, errors);
            }

            if (string.IsNullOrWhiteSpace(entity.EntityName))
            {
                errors.Add("Entity name (key pattern) cannot be empty");
            }

            // Redis has some restrictions on key names
            if (entity.EntityName?.Length > 512)
            {
                errors.Add("Key name cannot exceed 512 bytes");
            }

            return (errors.Count == 0, errors);
        }

        /// <summary>
        /// Checks if Redis supports a specific capability.
        /// </summary>
        public bool SupportsCapability(CapabilityType capability)
        {
            return Capabilities.IsCapable(capability);
        }

        /// <summary>
        /// Redis doesn't have string size limits.
        /// Returns -1 to indicate unlimited.
        /// </summary>
        public int GetMaxStringSize()
        {
            return -1; // Unlimited
        }

        /// <summary>
        /// Redis uses 64-bit integers, so precision is limited.
        /// Returns 18 (max digits in 64-bit signed integer).
        /// </summary>
        public int GetMaxNumericPrecision()
        {
            return 18; // 64-bit signed integer max digits
        }

        #endregion
    }
}
