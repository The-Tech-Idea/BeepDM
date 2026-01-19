using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Helpers.UniversalDataSourceHelpers.Core;
using TheTechIdea.Beep.Helpers.DataTypesHelpers;

namespace TheTechIdea.Beep.Helpers.UniversalDataSourceHelpers.RestApiHelpers
{
    /// <summary>
    /// Generic REST API helper for query generation and operations.
    /// Works with standard REST endpoints that follow REST conventions.
    ///
    /// Note: REST APIs have limited server-side capabilities.
    /// No transactions, joins, or aggregations support.
    /// Query parameters and JSON payloads are used for operations.
    /// </summary>
    public class RestApiHelper : IDataSourceHelper
    {
        private readonly IDMEEditor _dmeEditor;

        /// <summary>
        /// Initializes a new instance of the RestApiHelper class.
        /// </summary>
        /// <param name="dmeEditor">The IDMEEditor instance</param>
        public RestApiHelper(IDMEEditor dmeEditor)
        {
            _dmeEditor = dmeEditor ?? throw new ArgumentNullException(nameof(dmeEditor));
        }

        public DataSourceType SupportedType { get; set; } = DataSourceType.RestApi;
        public string Name => "Generic REST API";

        public DataSourceCapabilities Capabilities =>
            DataSourceCapabilityMatrix.GetCapabilities(SupportedType);

        #region Schema Operations

        /// <summary>
        /// Gets the REST endpoint to retrieve available resources (schemas).
        /// </summary>
        public (string Query, bool Success) GetSchemaQuery(string userName)
        {
            try
            {
                const string endpoint = "GET /api";
                return (endpoint, true);
            }
            catch
            {
                return ("", false);
            }
        }

        /// <summary>
        /// Gets the REST endpoint to check if a resource exists.
        /// </summary>
        public (string Query, bool Success) GetTableExistsQuery(string tableName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tableName))
                    return ("", false);

                string endpoint = $"HEAD /api/{tableName}";
                return (endpoint, true);
            }
            catch
            {
                return ("", false);
            }
        }

        /// <summary>
        /// Gets the REST endpoint to retrieve field information for a resource.
        /// </summary>
        public (string Query, bool Success) GetColumnInfoQuery(string tableName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tableName))
                    return ("", false);

                string endpoint = $"GET /api/{tableName}/schema";
                return (endpoint, true);
            }
            catch
            {
                return ("", false);
            }
        }

        #endregion

        #region Ddl Operations (Create, Alter, Drop) - Level 1 Schema Operations

        /// <summary>
        /// REST APIs typically don't support Ddl operations.
        /// Returns error indicating Ddl is not supported.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateCreateTableSql(
            EntityStructure entity,
            string schemaName = null,
            DataSourceType? dataSourceType = null)
        {
            return ("", false, "REST APIs do not support Ddl operations like CREATE TABLE");
        }

        /// <summary>
        /// REST APIs typically don't support Ddl operations.
        /// Returns error indicating Ddl is not supported.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateDropTableSql(string tableName, string schemaName = null)
        {
            return ("", false, "REST APIs do not support Ddl operations like DROP TABLE");
        }

        /// <summary>
        /// REST APIs typically don't support Ddl operations.
        /// Returns error indicating Ddl is not supported.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateTruncateTableSql(string tableName, string schemaName = null)
        {
            return ("", false, "REST APIs do not support Ddl operations like TRUNCATE TABLE");
        }

        /// <summary>
        /// REST APIs typically don't support Ddl operations.
        /// Returns error indicating Ddl is not supported.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateCreateIndexSql(
            string tableName,
            string indexName,
            string[] columns,
            Dictionary<string, object> options = null)
        {
            return ("", false, "REST APIs do not support Ddl operations like CREATE INDEX");
        }

        /// <summary>
        /// REST APIs typically don't support Ddl operations.
        /// Returns error indicating Ddl is not supported.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateAddColumnSql(string tableName, EntityField column)
        {
            return ("", false, "REST APIs do not support Ddl operations like ADD COLUMN");
        }

        /// <summary>
        /// REST APIs typically don't support Ddl operations.
        /// Returns error indicating Ddl is not supported.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateAlterColumnSql(string tableName, string columnName, EntityField newColumn)
        {
            return ("", false, "REST APIs do not support Ddl operations like ALTER COLUMN");
        }

        /// <summary>
        /// REST APIs typically don't support Ddl operations.
        /// Returns error indicating Ddl is not supported.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateDropColumnSql(string tableName, string columnName)
        {
            return ("", false, "REST APIs do not support Ddl operations like DROP COLUMN");
        }

        /// <summary>
        /// REST APIs typically don't support Ddl operations.
        /// Returns error indicating Ddl is not supported.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateRenameTableSql(string oldTableName, string newTableName)
        {
            return ("", false, "REST APIs do not support Ddl operations like RENAME TABLE");
        }

        /// <summary>
        /// REST APIs typically don't support Ddl operations.
        /// Returns error indicating Ddl is not supported.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateRenameColumnSql(string tableName, string oldColumnName, string newColumnName)
        {
            return ("", false, "REST APIs do not support Ddl operations like RENAME COLUMN");
        }

        #endregion

        #region Constraint Operations - Level 2 Schema Integrity

        /// <summary>
        /// REST APIs typically don't support constraint operations.
        /// Returns error indicating constraints are not supported.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateAddPrimaryKeySql(string tableName, params string[] columnNames)
        {
            return ("", false, "REST APIs do not support constraint operations like PRIMARY KEY");
        }

        /// <summary>
        /// REST APIs typically don't support constraint operations.
        /// Returns error indicating constraints are not supported.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateAddForeignKeySql(
            string tableName,
            string[] columnNames,
            string referencedTableName,
            string[] referencedColumnNames)
        {
            return ("", false, "REST APIs do not support constraint operations like FOREIGN KEY");
        }

        /// <summary>
        /// REST APIs typically don't support constraint operations.
        /// Returns error indicating constraints are not supported.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateAddConstraintSql(string tableName, string constraintName, string constraintDefinition)
        {
            return ("", false, "REST APIs do not support constraint operations");
        }

        /// <summary>
        /// REST APIs typically don't support constraint queries.
        /// Returns error indicating constraint queries are not supported.
        /// </summary>
        public (string Query, bool Success, string ErrorMessage) GetPrimaryKeyQuery(string tableName)
        {
            return ("", false, "REST APIs do not support constraint queries");
        }

        /// <summary>
        /// REST APIs typically don't support constraint queries.
        /// Returns error indicating constraint queries are not supported.
        /// </summary>
        public (string Query, bool Success, string ErrorMessage) GetForeignKeysQuery(string tableName)
        {
            return ("", false, "REST APIs do not support constraint queries");
        }

        /// <summary>
        /// REST APIs typically don't support constraint queries.
        /// Returns error indicating constraint queries are not supported.
        /// </summary>
        public (string Query, bool Success, string ErrorMessage) GetConstraintsQuery(string tableName)
        {
            return ("", false, "REST APIs do not support constraint queries");
        }

        #endregion

        #region Transaction Control - Level 3 ACID Support

        /// <summary>
        /// REST APIs typically don't support transactions.
        /// Returns error indicating transactions are not supported.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateBeginTransactionSql()
        {
            return ("", false, "REST APIs do not support transactions");
        }

        /// <summary>
        /// REST APIs typically don't support transactions.
        /// Returns error indicating transactions are not supported.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateCommitSql()
        {
            return ("", false, "REST APIs do not support transactions");
        }

        /// <summary>
        /// REST APIs typically don't support transactions.
        /// Returns error indicating transactions are not supported.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateRollbackSql()
        {
            return ("", false, "REST APIs do not support transactions");
        }

        #endregion

        #region DML Operations (Insert, Update, Delete, Select)

        /// <summary>
        /// Generates a REST API POST request to insert a record.
        /// </summary>
        public (string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage) GenerateInsertSql(
            string tableName,
            Dictionary<string, object> data)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tableName) || data == null || !data.Any())
                    return ("", null, false, "Invalid table name or data");

                string endpoint = $"POST /api/{tableName}";
                return (endpoint, data, true, "");
            }
            catch (Exception ex)
            {
                return ("", null, false, ex.Message);
            }
        }

        /// <summary>
        /// Generates a REST API PUT/PATCH request to update records.
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

                string endpoint = $"PUT /api/{tableName}";
                var parameters = new Dictionary<string, object>();

                // Add data fields
                foreach (var kvp in data)
                    parameters[kvp.Key] = kvp.Value;

                // Add condition fields as query parameters
                if (conditions != null && conditions.Any())
                {
                    foreach (var kvp in conditions)
                        parameters[$"condition_{kvp.Key}"] = kvp.Value;
                }

                return (endpoint, parameters, true, "");
            }
            catch (Exception ex)
            {
                return ("", null, false, ex.Message);
            }
        }

        /// <summary>
        /// Generates a REST API DELETE request to delete records.
        /// </summary>
        public (string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage) GenerateDeleteSql(
            string tableName,
            Dictionary<string, object> conditions)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tableName))
                    return ("", null, false, "Invalid table name");

                string endpoint = $"DELETE /api/{tableName}";
                var parameters = new Dictionary<string, object>();

                // Add condition fields as query parameters
                if (conditions != null && conditions.Any())
                {
                    foreach (var kvp in conditions)
                        parameters[$"condition_{kvp.Key}"] = kvp.Value;
                }

                return (endpoint, parameters, true, "");
            }
            catch (Exception ex)
            {
                return ("", null, false, ex.Message);
            }
        }

        /// <summary>
        /// Generates a REST API GET request to retrieve records.
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

                string endpoint = $"GET /api/{tableName}";
                var parameters = new Dictionary<string, object>();

                // Add column selection as query parameters
                if (columns != null && columns.Any())
                {
                    parameters["fields"] = string.Join(",", columns);
                }

                // Add condition fields as query parameters
                if (conditions != null && conditions.Any())
                {
                    foreach (var kvp in conditions)
                        parameters[kvp.Key] = kvp.Value;
                }

                // Add ordering
                if (!string.IsNullOrWhiteSpace(orderBy))
                {
                    parameters["orderBy"] = orderBy;
                }

                // Add paging
                if (skip.HasValue)
                {
                    parameters["skip"] = skip.Value;
                }
                if (take.HasValue)
                {
                    parameters["take"] = take.Value;
                }

                return (endpoint, parameters, true, "");
            }
            catch (Exception ex)
            {
                return ("", null, false, ex.Message);
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// REST APIs typically don't require identifier quoting.
        /// Returns the identifier as-is.
        /// </summary>
        public string QuoteIdentifier(string identifier)
        {
            return identifier;
        }

        /// <summary>
        /// Maps C# types to JSON types for REST APIs.
        /// </summary>
        /// <summary>
        /// Maps C# types to JSON types for REST APIs.
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
        /// Maps JSON types back to C# types for REST APIs.
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
        /// Validates entity structure for REST API compatibility.
        /// REST APIs are generally flexible with schema.
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

            // REST APIs are generally schema-flexible, so minimal validation
            return (errors.Count == 0, errors);
        }

        /// <summary>
        /// Checks if REST API supports a specific capability.
        /// REST APIs have limited capabilities compared to databases.
        /// </summary>
        public bool SupportsCapability(CapabilityType capability)
        {
            return Capabilities.IsCapable(capability);
        }

        /// <summary>
        /// REST APIs typically don't have string size limits.
        /// Returns -1 to indicate unlimited.
        /// </summary>
        public int GetMaxStringSize()
        {
            return -1; // Unlimited
        }

        /// <summary>
        /// REST APIs typically don't have numeric precision limits.
        /// Returns 0 to indicate unlimited.
        /// </summary>
        public int GetMaxNumericPrecision()
        {
            return 0; // Unlimited
        }

        #endregion
    }
}
