using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Helpers.UniversalDataSourceHelpers.Core;
using TheTechIdea.Beep.Helpers.DataTypesHelpers;

namespace TheTechIdea.Beep.Helpers.UniversalDataSourceHelpers.CassandraHelpers
{
    /// <summary>
    /// Apache Cassandra-specific helper for query generation and operations.
    /// Cassandra is a distributed, NoSQL database designed for massive scalability.
    /// Uses CQL (Cassandra Query Language) which is SQL-like but with differences.
    ///
    /// Note: Cassandra doesn't support JOINs; denormalization is required.
    /// No transactions for multi-partition operations.
    /// Token-based pagination for efficient distributed queries.
    /// </summary>
    public class CassandraHelper : IDataSourceHelper
    {
        private readonly IDMEEditor _dmeEditor;

        /// <summary>
        /// Initializes a new instance of the CassandraHelper class.
        /// </summary>
        /// <param name="dmeEditor">The IDMEEditor instance</param>
        public CassandraHelper(IDMEEditor dmeEditor)
        {
            _dmeEditor = dmeEditor ?? throw new ArgumentNullException(nameof(dmeEditor));
        }

        public DataSourceType SupportedType { get; set; } = DataSourceType.Cassandra;
        public string Name => "Apache Cassandra";

        public DataSourceCapabilities Capabilities =>
            DataSourceCapabilityMatrix.GetCapabilities(SupportedType);

        #region Schema Operations

        /// <summary>
        /// Gets a CQL query to retrieve keyspaces (schemas in Cassandra).
        /// </summary>
        public (string Query, bool Success) GetSchemaQuery(string userName = null)
        {
            try
            {
                const string query = "SELECT keyspace_name FROM system_schema.keyspaces;";
                return (query, true);
            }
            catch
            {
                return ("", false);
            }
        }

        /// <summary>
        /// Gets a CQL query to check if a table (column family) exists.
        /// </summary>
        public (string Query, bool Success) GetTableExistsQuery(string tableName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tableName))
                    return ("", false);

                // Parse table name to extract keyspace.table
                var parts = tableName.Split('.');
                if (parts.Length == 2)
                {
                    string keyspace = parts[0];
                    string table = parts[1];
                    string query = $"SELECT table_name FROM system_schema.tables WHERE keyspace_name = '{keyspace}' AND table_name = '{table}' ALLOW FILTERING;";
                    return (query, true);
                }
                else
                {
                    return ("", false);
                }
            }
            catch
            {
                return ("", false);
            }
        }

        /// <summary>
        /// Gets a CQL query to retrieve column information for a table.
        /// </summary>
        public (string Query, bool Success) GetColumnInfoQuery(string tableName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tableName))
                    return ("", false);

                // Parse table name to extract keyspace.table
                var parts = tableName.Split('.');
                if (parts.Length == 2)
                {
                    string keyspace = parts[0];
                    string table = parts[1];
                    string query = $"SELECT column_name, type, kind FROM system_schema.columns WHERE keyspace_name = '{keyspace}' AND table_name = '{table}';";
                    return (query, true);
                }
                else
                {
                    return ("", false);
                }
            }
            catch
            {
                return ("", false);
            }
        }

        #endregion

        #region Ddl Operations (Create, Alter, Drop) - Level 1 Schema Operations

        /// <summary>
        /// Generates CQL to create a table from an entity structure.
        /// Cassandra requires partition keys and clustering columns to be specified.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateCreateTableSql(
            EntityStructure entity,
            string schemaName = null,
            DataSourceType? dataSourceType = null)
        {
            try
            {
                if (entity == null || string.IsNullOrWhiteSpace(entity.EntityName))
                    return ("", false, "Entity structure is invalid");

                var columns = new List<string>();
                var partitionKeys = new List<string>();
                var clusteringKeys = new List<string>();

                foreach (var field in entity.Fields)
                {
                    // Convert Fieldtype string to Type
                    Type fieldClrType = Type.GetType(field.Fieldtype) ?? typeof(string);
                    string cassandraType = MapClrTypeToDatasourceType(fieldClrType);
                    columns.Add($"{QuoteIdentifier(field.FieldName)} {cassandraType}");

                    // Assume first field is partition key if not specified
                    if (partitionKeys.Count == 0)
                        partitionKeys.Add(field.FieldName);

                    // Add other fields as clustering keys if needed
                    if (fieldClrType == typeof(DateTime) && clusteringKeys.Count == 0)
                        clusteringKeys.Add(field.FieldName);
                }

                string keyspace = schemaName ?? "default_keyspace";
                string tableName = entity.EntityName;

                string partitionKeyClause = string.Join(", ", partitionKeys.Select(QuoteIdentifier));
                string clusteringKeyClause = clusteringKeys.Any() ?
                    $", CLUSTERING ORDER BY ({string.Join(", ", clusteringKeys.Select(k => $"{QuoteIdentifier(k)} ASC"))})" : "";

                string cql = $@"
CREATE TABLE IF NOT EXISTS {QuoteIdentifier(keyspace)}.{QuoteIdentifier(tableName)} (
    {string.Join(",\n    ", columns)}
    PRIMARY KEY ({partitionKeyClause}){clusteringKeyClause}
);";

                return (cql.Trim(), true, "");
            }
            catch (Exception ex)
            {
                return ("", false, ex.Message);
            }
        }

        /// <summary>
        /// Generates CQL to drop a table.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateDropTableSql(string tableName, string schemaName = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tableName))
                    return ("", false, "Table name cannot be empty");

                string keyspace = schemaName ?? "default_keyspace";
                string cql = $"DROP TABLE IF EXISTS {QuoteIdentifier(keyspace)}.{QuoteIdentifier(tableName)};";
                return (cql, true, "");
            }
            catch (Exception ex)
            {
                return ("", false, ex.Message);
            }
        }

        /// <summary>
        /// Generates CQL to truncate a table.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateTruncateTableSql(string tableName, string schemaName = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tableName))
                    return ("", false, "Table name cannot be empty");

                string keyspace = schemaName ?? "default_keyspace";
                string cql = $"TRUNCATE TABLE {QuoteIdentifier(keyspace)}.{QuoteIdentifier(tableName)};";
                return (cql, true, "");
            }
            catch (Exception ex)
            {
                return ("", false, ex.Message);
            }
        }

        /// <summary>
        /// Cassandra doesn't support traditional secondary indexes like RDBMS.
        /// Use materialized views or application-side indexing instead.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateCreateIndexSql(
            string tableName,
            string indexName,
            string[] columns,
            Dictionary<string, object> options = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tableName) || columns == null || !columns.Any())
                    return ("", false, "Invalid table name or columns");

                // Cassandra secondary index (limited use in production)
                string keyspace = "default_keyspace"; // Would need to extract from tableName
                string columnName = columns[0]; // Simple case
                string cql = $"CREATE INDEX IF NOT EXISTS {QuoteIdentifier(indexName)} ON {QuoteIdentifier(keyspace)}.{QuoteIdentifier(tableName)} ({QuoteIdentifier(columnName)});";

                return (cql, true, "");
            }
            catch (Exception ex)
            {
                return ("", false, ex.Message);
            }
        }

        /// <summary>
        /// Generates CQL to add a column to an existing table.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateAddColumnSql(string tableName, EntityField column)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tableName) || column == null)
                    return ("", false, "Invalid table name or column");

                string keyspace = "default_keyspace"; // Would need to extract from tableName
                Type columnClrType = Type.GetType(column.Fieldtype) ?? typeof(string);
                string cassandraType = MapClrTypeToDatasourceType(columnClrType);
                string cql = $"ALTER TABLE {QuoteIdentifier(keyspace)}.{QuoteIdentifier(tableName)} ADD {QuoteIdentifier(column.FieldName)} {cassandraType};";

                return (cql, true, "");
            }
            catch (Exception ex)
            {
                return ("", false, ex.Message);
            }
        }

        /// <summary>
        /// Cassandra doesn't support altering column types.
        /// You need to create a new table and migrate data.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateAlterColumnSql(string tableName, string columnName, EntityField newColumn)
        {
            return ("", false, "Cassandra does not support altering column types - create new table and migrate data");
        }

        /// <summary>
        /// Cassandra doesn't support dropping columns.
        /// You need to create a new table without the column and migrate data.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateDropColumnSql(string tableName, string columnName)
        {
            return ("", false, "Cassandra does not support dropping columns - create new table and migrate data");
        }

        /// <summary>
        /// Cassandra doesn't support renaming tables.
        /// You need to create a new table and migrate data.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateRenameTableSql(string oldTableName, string newTableName)
        {
            return ("", false, "Cassandra does not support renaming tables - create new table and migrate data");
        }

        /// <summary>
        /// Cassandra doesn't support renaming columns.
        /// You need to create a new table and migrate data.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateRenameColumnSql(string tableName, string oldColumnName, string newColumnName)
        {
            return ("", false, "Cassandra does not support renaming columns - create new table and migrate data");
        }

        #endregion

        #region Constraint Operations - Level 2 Schema Integrity

        /// <summary>
        /// Cassandra doesn't support adding primary keys to existing tables.
        /// Primary keys must be defined at table creation.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateAddPrimaryKeySql(string tableName, params string[] columnNames)
        {
            return ("", false, "Cassandra does not support adding primary keys to existing tables");
        }

        /// <summary>
        /// Cassandra doesn't support foreign key constraints.
        /// Referential integrity must be handled application-side.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateAddForeignKeySql(
            string tableName,
            string[] columnNames,
            string referencedTableName,
            string[] referencedColumnNames)
        {
            return ("", false, "Cassandra does not support foreign key constraints");
        }

        /// <summary>
        /// Cassandra doesn't support traditional constraints.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateAddConstraintSql(string tableName, string constraintName, string constraintDefinition)
        {
            return ("", false, "Cassandra does not support traditional constraints");
        }

        /// <summary>
        /// Gets a CQL query to retrieve primary key information.
        /// </summary>
        public (string Query, bool Success, string ErrorMessage) GetPrimaryKeyQuery(string tableName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tableName))
                    return ("", false, "Table name cannot be empty");

                // Parse table name to extract keyspace.table
                var parts = tableName.Split('.');
                if (parts.Length == 2)
                {
                    string keyspace = parts[0];
                    string table = parts[1];
                    string query = $"SELECT keyspace_name, table_name, column_name, kind, position FROM system_schema.columns WHERE keyspace_name = '{keyspace}' AND table_name = '{table}' AND kind IN ('partition_key', 'clustering');";
                    return (query, true, "");
                }
                else
                {
                    return ("", false, "Table name must be in format keyspace.table");
                }
            }
            catch (Exception ex)
            {
                return ("", false, ex.Message);
            }
        }

        /// <summary>
        /// Cassandra doesn't support foreign key constraints.
        /// </summary>
        public (string Query, bool Success, string ErrorMessage) GetForeignKeysQuery(string tableName)
        {
            return ("", false, "Cassandra does not support foreign key constraints");
        }

        /// <summary>
        /// Cassandra doesn't support traditional constraints.
        /// </summary>
        public (string Query, bool Success, string ErrorMessage) GetConstraintsQuery(string tableName)
        {
            return ("", false, "Cassandra does not support traditional constraints");
        }

        #endregion

        #region Transaction Control - Level 3 ACID Support

        /// <summary>
        /// Cassandra doesn't support traditional transactions.
        /// Light-weight transactions (LWT) are available for single partition operations.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateBeginTransactionSql()
        {
            return ("", false, "Cassandra does not support traditional transactions - use light-weight transactions for single partition operations");
        }

        /// <summary>
        /// Cassandra doesn't support traditional transaction commit.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateCommitSql()
        {
            return ("", false, "Cassandra does not support traditional transactions");
        }

        /// <summary>
        /// Cassandra doesn't support traditional transaction rollback.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateRollbackSql()
        {
            return ("", false, "Cassandra does not support traditional transactions");
        }

        #endregion

        #region DML Operations (Insert, Update, Delete, Select)

        /// <summary>
        /// Generates CQL INSERT statement.
        /// </summary>
        public (string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage) GenerateInsertSql(
            string tableName,
            Dictionary<string, object> data)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tableName) || data == null || !data.Any())
                    return ("", null, false, "Invalid table name or data");

                string keyspace = "default_keyspace"; // Would need to extract from tableName
                var columns = data.Keys.Select(QuoteIdentifier);
                var values = data.Keys.Select(k => "?");

                string cql = $"INSERT INTO {QuoteIdentifier(keyspace)}.{QuoteIdentifier(tableName)} ({string.Join(", ", columns)}) VALUES ({string.Join(", ", values)});";

                return (cql, data, true, "");
            }
            catch (Exception ex)
            {
                return ("", null, false, ex.Message);
            }
        }

        /// <summary>
        /// Generates CQL UPDATE statement.
        /// </summary>
        public (string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage) GenerateUpdateSql(
            string tableName,
            Dictionary<string, object> data,
            Dictionary<string, object> conditions)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tableName) || data == null || !data.Any())
                    return ("", null, false, "Invalid table name or data");

                string keyspace = "default_keyspace"; // Would need to extract from tableName
                var setClause = data.Select(kvp => $"{QuoteIdentifier(kvp.Key)} = ?");
                var whereClause = conditions?.Select(kvp => $"{QuoteIdentifier(kvp.Key)} = ?") ?? new string[0];

                string cql = $"UPDATE {QuoteIdentifier(keyspace)}.{QuoteIdentifier(tableName)} SET {string.Join(", ", setClause)} WHERE {string.Join(" AND ", whereClause)};";

                var parameters = new Dictionary<string, object>();
                foreach (var kvp in data)
                    parameters[kvp.Key] = kvp.Value;
                if (conditions != null)
                    foreach (var kvp in conditions)
                        parameters[kvp.Key] = kvp.Value;

                return (cql, parameters, true, "");
            }
            catch (Exception ex)
            {
                return ("", null, false, ex.Message);
            }
        }

        /// <summary>
        /// Generates CQL DELETE statement.
        /// </summary>
        public (string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage) GenerateDeleteSql(
            string tableName,
            Dictionary<string, object> conditions)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tableName))
                    return ("", null, false, "Invalid table name");

                string keyspace = "default_keyspace"; // Would need to extract from tableName
                var whereClause = conditions?.Select(kvp => $"{QuoteIdentifier(kvp.Key)} = ?") ?? new string[0];

                string cql = $"DELETE FROM {QuoteIdentifier(keyspace)}.{QuoteIdentifier(tableName)} WHERE {string.Join(" AND ", whereClause)};";

                return (cql, conditions, true, "");
            }
            catch (Exception ex)
            {
                return ("", null, false, ex.Message);
            }
        }

        /// <summary>
        /// Generates CQL SELECT statement.
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
                    return ("", null, false, "Invalid table name");

                string keyspace = "default_keyspace"; // Would need to extract from tableName
                string columnClause = columns != null && columns.Any() ?
                    string.Join(", ", columns.Select(QuoteIdentifier)) : "*";

                string whereClause = "";
                if (conditions != null && conditions.Any())
                {
                    whereClause = $" WHERE {string.Join(" AND ", conditions.Select(kvp => $"{QuoteIdentifier(kvp.Key)} = ?"))}";
                }

                string orderByClause = "";
                if (!string.IsNullOrWhiteSpace(orderBy))
                {
                    // Simple parsing - assumes format "col1 asc, col2 desc"
                    var orderParts = orderBy.Split(',').Select(p => p.Trim());
                    var orderSpecs = new List<string>();
                    foreach (var part in orderParts)
                    {
                        if (part.EndsWith(" desc", StringComparison.OrdinalIgnoreCase))
                        {
                            var col = part.Substring(0, part.Length - 5).Trim();
                            orderSpecs.Add($"{QuoteIdentifier(col)} DESC");
                        }
                        else if (part.EndsWith(" asc", StringComparison.OrdinalIgnoreCase))
                        {
                            var col = part.Substring(0, part.Length - 4).Trim();
                            orderSpecs.Add($"{QuoteIdentifier(col)} ASC");
                        }
                        else
                        {
                            orderSpecs.Add($"{QuoteIdentifier(part)} ASC");
                        }
                    }
                    orderByClause = $" ORDER BY {string.Join(", ", orderSpecs)}";
                }

                string limitClause = take.HasValue ? $" LIMIT {take.Value}" : "";

                string cql = $"SELECT {columnClause} FROM {QuoteIdentifier(keyspace)}.{QuoteIdentifier(tableName)}{whereClause}{orderByClause}{limitClause};";

                var parameters = new Dictionary<string, object>();
                if (conditions != null)
                    foreach (var kvp in conditions)
                        parameters[kvp.Key] = kvp.Value;

                return (cql, parameters, true, "");
            }
            catch (Exception ex)
            {
                return ("", null, false, ex.Message);
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Quotes identifiers using double quotes for Cassandra CQL.
        /// </summary>
        public string QuoteIdentifier(string identifier)
        {
            return $"\"{identifier.Replace("\"", "\"\"")}\"";
        }

        /// <summary>
        /// Maps C# types to Cassandra CQL types.
        /// </summary>
        /// <summary>
        /// Maps C# types to Cassandra CQL types.
        /// Uses DataTypeMappingRepository for mapping.
        /// </summary>
        public string MapClrTypeToDatasourceType(Type clrType, int? size = null, int? precision = null, int? scale = null)
        {
            try
            {
                if (clrType == null)
                    return "text";

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

                return "text"; // Minimal fallback
            }
            catch
            {
                return "text"; // Minimal fallback
            }
        }

        /// <summary>
        /// Maps Cassandra CQL types back to C# types.
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
        /// Validates entity structure for Cassandra compatibility.
        /// Cassandra has specific requirements for primary keys and clustering columns.
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
                errors.Add("Entity name cannot be empty");
            }

            if (entity.Fields == null || !entity.Fields.Any())
            {
                errors.Add("Entity must have at least one field");
            }
            else
            {
                // Cassandra requires at least one partition key
                bool hasPartitionKey = entity.Fields.Any(f =>
                {
                    Type fType = Type.GetType(f.Fieldtype) ?? typeof(string);
                    return f.IsKey == true || f.IsAutoIncrement == true ||
                        fType == typeof(string) || fType == typeof(int) || fType == typeof(long);
                });

                if (!hasPartitionKey)
                {
                    errors.Add("Entity must have at least one field suitable for partition key (string, int, long, or marked as key)");
                }
            }

            return (errors.Count == 0, errors);
        }

        /// <summary>
        /// Checks if Cassandra supports a specific capability.
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
        /// Cassandra text columns can be up to 2GB.
        /// Returns -1 to indicate very large limit.
        /// </summary>
        public int GetMaxStringSize()
        {
            return -1; // Very large limit (2GB)
        }

        /// <summary>
        /// Cassandra decimal type has high precision.
        /// Returns 0 to indicate unlimited.
        /// </summary>
        public int GetMaxNumericPrecision()
        {
            return 0; // Unlimited precision for decimal
        }

        #endregion
    }
}
