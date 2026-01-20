using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Helpers.UniversalDataSourceHelpers.Core;
using TheTechIdea.Beep.Helpers.DataTypesHelpers;

namespace TheTechIdea.Beep.Helpers.UniversalDataSourceHelpers.MongoDBHelpers
{
    /// <summary>
    /// MongoDB-specific helper for query generation and operations.
    /// MongoDB uses BSON documents and aggregation pipelines instead of SQL.
    /// This helper generates MongoDB query syntax and aggregation pipelines.
    ///
    /// Note: MongoDB doesn't support traditional JOINs. Use $lookup for embedded aggregations.
    /// Transactions are supported for multi-document operations (v4.0+).
    /// </summary>
    public class MongoDBHelper : IDataSourceHelper
    {
        private readonly IDMEEditor _dmeEditor;

        /// <summary>
        /// Initializes a new instance of the MongoDBHelper class.
        /// </summary>
        /// <param name="dmeEditor">The IDMEEditor instance</param>
        public MongoDBHelper(IDMEEditor dmeEditor)
        {
            _dmeEditor = dmeEditor ?? throw new ArgumentNullException(nameof(dmeEditor));
        }

        public DataSourceType SupportedType { get; set; } = DataSourceType.MongoDB;
        public string Name => "MongoDB";

        public DataSourceCapabilities Capabilities =>
            DataSourceCapabilityMatrix.GetCapabilities(DataSourceType.MongoDB);

        #region Schema Operations

        /// <summary>
        /// MongoDB doesn't have traditional schemas, but collections can be listed.
        /// Returns a query to retrieve collection names.
        /// </summary>
        public (string Query, bool Success) GetSchemaQuery(string userName)
        {
            try
            {
                // MongoDB command to list collections
                const string query = "{ \"listCollections\": 1 }";
                return (query, true);
            }
            catch
            {
                return ("", false);
            }
        }

        /// <summary>
        /// Gets a query to check if a collection exists.
        /// </summary>
        public (string Query, bool Success) GetTableExistsQuery(string collectionName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(collectionName))
                    return ("", false);

                // MongoDB command to check collection existence
                string query = $"{{ \"listCollections\": 1, \"filter\": {{ \"name\": \"{collectionName}\" }} }}";
                return (query, true);
            }
            catch
            {
                return ("", false);
            }
        }

        /// <summary>
        /// Gets a query to retrieve field information for a collection.
        /// MongoDB is schema-less, so this returns a sample document structure.
        /// </summary>
        public (string Query, bool Success) GetColumnInfoQuery(string collectionName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(collectionName))
                    return ("", false);

                // MongoDB aggregation to get schema information
                string query = $"{{ \"aggregate\": \"{collectionName}\", \"pipeline\": [ {{ \"$sample\": {{ \"size\": 1 }} }} ] }}";
                return (query, true);
            }
            catch
            {
                return ("", false);
            }
        }

        #endregion

        #region Ddl Operations (Create, Alter, Drop) - Level 1 Schema Operations

        /// <summary>
        /// MongoDB collections are created implicitly when first document is inserted.
        /// This method acknowledges that Ddl is not needed for collection creation.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateCreateTableSql(
            EntityStructure entity,
            string schemaName = null,
            DataSourceType? dataSourceType = null)
        {
            return ("", true, "MongoDB collections are created implicitly - no Ddl needed");
        }

        /// <summary>
        /// Generates MongoDB command to drop a collection.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateDropTableSql(string collectionName, string schemaName = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(collectionName))
                    return ("", false, "Collection name cannot be empty");

                string command = $"{{ \"drop\": \"{collectionName}\" }}";
                return (command, true, "");
            }
            catch (Exception ex)
            {
                return ("", false, ex.Message);
            }
        }

        /// <summary>
        /// Generates MongoDB command to remove all documents from a collection.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateTruncateTableSql(string collectionName, string schemaName = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(collectionName))
                    return ("", false, "Collection name cannot be empty");

                string command = $"{{ \"delete\": \"{collectionName}\", \"deletes\": [{{ \"q\": {{}}, \"limit\": 0 }}] }}";
                return (command, true, "");
            }
            catch (Exception ex)
            {
                return ("", false, ex.Message);
            }
        }

        /// <summary>
        /// Generates MongoDB command to create an index.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateCreateIndexSql(
            string collectionName,
            string indexName,
            string[] columns,
            Dictionary<string, object> options = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(collectionName) || columns == null || !columns.Any())
                    return ("", false, "Invalid collection name or columns");

                var indexKeys = new Dictionary<string, int>();
                foreach (var column in columns)
                    indexKeys[column] = 1; // Ascending order

                var indexSpec = new Dictionary<string, object>
                {
                    ["createIndexes"] = collectionName,
                    ["indexes"] = new[]
                    {
                        new Dictionary<string, object>
                        {
                            ["key"] = indexKeys,
                            ["name"] = indexName ?? $"{collectionName}_{string.Join("_", columns)}_idx"
                        }
                    }
                };

                if (options != null)
                {
                    if (options.TryGetValue("unique", out var unique) && (bool)unique)
                    {
                        var indexesArray = indexSpec["indexes"] as Dictionary<string, object>[];
                        if (indexesArray != null && indexesArray.Length > 0)
                            indexesArray[0]["unique"] = true;
                    }
                }

                return (Newtonsoft.Json.JsonConvert.SerializeObject(indexSpec), true, "");
            }
            catch (Exception ex)
            {
                return ("", false, ex.Message);
            }
        }

        /// <summary>
        /// MongoDB is schema-less, so adding columns is not applicable.
        /// Returns success since no action is needed.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateAddColumnSql(string collectionName, EntityField column)
        {
            if (column == null || string.IsNullOrWhiteSpace(collectionName))
                return ("", false, "Collection name or column is missing");

            switch (SupportedType)
            {
                case DataSourceType.OrientDB:
                    return GenerateOrientDbAddProperty(collectionName, column);
                case DataSourceType.MongoDB:
                    return GenerateMongoDbAddProperty(collectionName, column);
                case DataSourceType.ArangoDB:
                case DataSourceType.CouchDB:
                case DataSourceType.Couchbase:
                case DataSourceType.DynamoDB:
                case DataSourceType.Firebase:
                case DataSourceType.LiteDB:
                case DataSourceType.RavenDB:
                    return ("", true, $"{SupportedType} is schema-less - no DDL required");
                default:
                    return ("", true, "Schema is flexible - no DDL required");
            }
        }

        /// <summary>
        /// MongoDB is schema-less, so altering columns is not applicable.
        /// Returns success since no action is needed.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateAlterColumnSql(string collectionName, string columnName, EntityField newColumn)
        {
            return ("", true, "MongoDB is schema-less - column types are dynamic");
        }

        /// <summary>
        /// MongoDB is schema-less, so dropping columns is not applicable.
        /// Returns success since no action is needed.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateDropColumnSql(string collectionName, string columnName)
        {
            return ("", true, "MongoDB is schema-less - columns cannot be dropped individually");
        }

        /// <summary>
        /// Generates MongoDB command to rename a collection.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateRenameTableSql(string oldCollectionName, string newCollectionName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(oldCollectionName) || string.IsNullOrWhiteSpace(newCollectionName))
                    return ("", false, "Collection names cannot be empty");

                string command = $"{{ \"renameCollection\": \"{oldCollectionName}\", \"to\": \"{newCollectionName}\" }}";
                return (command, true, "");
            }
            catch (Exception ex)
            {
                return ("", false, ex.Message);
            }
        }

        /// <summary>
        /// MongoDB doesn't support renaming individual fields in existing documents.
        /// This would require an update operation on all documents.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateRenameColumnSql(string collectionName, string oldColumnName, string newColumnName)
        {
            return ("", false, "MongoDB does not support renaming columns - requires manual document updates");
        }

        #endregion

        private (string Sql, bool Success, string ErrorMessage) GenerateMongoDbAddProperty(string collectionName, EntityField column)
        {
            var bsonType = MapToMongoBsonType(column);
            var json = $"{{ \"collMod\": \"{collectionName}\", \"validator\": {{ \"$jsonSchema\": {{ \"bsonType\": \"object\", \"properties\": {{ \"{column.FieldName}\": {{ \"bsonType\": \"{bsonType}\" }} }} }} }} }}";
            return (json, true, "MongoDB validator update for new field");
        }

        private (string Sql, bool Success, string ErrorMessage) GenerateOrientDbAddProperty(string className, EntityField column)
        {
            var typeName = MapToOrientDbType(column);
            var sql = $"CREATE PROPERTY {className}.{column.FieldName} {typeName}";
            return (sql, true, "OrientDB property creation");
        }

        private string MapToMongoBsonType(EntityField column)
        {
            if (column == null || string.IsNullOrWhiteSpace(column.Fieldtype))
                return "string";

            var t = column.Fieldtype.ToLowerInvariant();
            if (t.Contains("int") || t.Contains("long") || t.Contains("short"))
                return "int";
            if (t.Contains("decimal") || t.Contains("numeric") || t.Contains("double") || t.Contains("float"))
                return "double";
            if (t.Contains("bool"))
                return "bool";
            if (t.Contains("date") || t.Contains("time"))
                return "date";
            if (t.Contains("guid"))
                return "string";
            if (t.Contains("byte") || t.Contains("binary"))
                return "binData";
            return "string";
        }

        private string MapToOrientDbType(EntityField column)
        {
            if (column == null || string.IsNullOrWhiteSpace(column.Fieldtype))
                return "STRING";

            var t = column.Fieldtype.ToLowerInvariant();
            if (t.Contains("int") || t.Contains("long") || t.Contains("short"))
                return "INTEGER";
            if (t.Contains("decimal") || t.Contains("numeric"))
                return "DECIMAL";
            if (t.Contains("double") || t.Contains("float"))
                return "DOUBLE";
            if (t.Contains("bool"))
                return "BOOLEAN";
            if (t.Contains("date") || t.Contains("time"))
                return "DATETIME";
            if (t.Contains("byte") || t.Contains("binary"))
                return "BINARY";
            return "STRING";
        }

        #region Constraint Operations - Level 2 Schema Integrity

        /// <summary>
        /// MongoDB doesn't support traditional primary keys.
        /// The _id field serves as the primary key.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateAddPrimaryKeySql(string collectionName, params string[] columnNames)
        {
            return ("", false, "MongoDB uses _id field as primary key - cannot add additional primary keys");
        }

        /// <summary>
        /// MongoDB doesn't support foreign key constraints.
        /// Relationships are handled application-side or via $lookup.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateAddForeignKeySql(
            string collectionName,
            string[] columnNames,
            string referencedCollectionName,
            string[] referencedColumnNames)
        {
            return ("", false, "MongoDB does not support foreign key constraints");
        }

        /// <summary>
        /// MongoDB doesn't support traditional constraints.
        /// Validation rules can be set at collection level.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateAddConstraintSql(string collectionName, string constraintName, string constraintDefinition)
        {
            return ("", false, "MongoDB does not support traditional constraints - use validation rules instead");
        }

        /// <summary>
        /// MongoDB doesn't have traditional primary key queries.
        /// The _id field is the primary key.
        /// </summary>
        public (string Query, bool Success, string ErrorMessage) GetPrimaryKeyQuery(string collectionName)
        {
            return ("", false, "MongoDB uses _id field as primary key");
        }

        /// <summary>
        /// MongoDB doesn't support foreign key constraints.
        /// </summary>
        public (string Query, bool Success, string ErrorMessage) GetForeignKeysQuery(string collectionName)
        {
            return ("", false, "MongoDB does not support foreign key constraints");
        }

        /// <summary>
        /// MongoDB doesn't support traditional constraints.
        /// </summary>
        public (string Query, bool Success, string ErrorMessage) GetConstraintsQuery(string collectionName)
        {
            return ("", false, "MongoDB does not support traditional constraints");
        }

        #endregion

        #region Transaction Control - Level 3 ACID Support

        /// <summary>
        /// Generates MongoDB command to start a transaction (v4.0+).
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateBeginTransactionSql()
        {
            try
            {
                // MongoDB transaction start
                const string command = "{ \"startTransaction\": {} }";
                return (command, true, "");
            }
            catch (Exception ex)
            {
                return ("", false, ex.Message);
            }
        }

        /// <summary>
        /// Generates MongoDB command to commit a transaction.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateCommitSql()
        {
            try
            {
                const string command = "{ \"commitTransaction\": {} }";
                return (command, true, "");
            }
            catch (Exception ex)
            {
                return ("", false, ex.Message);
            }
        }

        /// <summary>
        /// Generates MongoDB command to abort a transaction.
        /// </summary>
        public (string Sql, bool Success, string ErrorMessage) GenerateRollbackSql()
        {
            try
            {
                const string command = "{ \"abortTransaction\": {} }";
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
        /// Generates MongoDB insert command.
        /// </summary>
        public (string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage) GenerateInsertSql(
            string collectionName,
            Dictionary<string, object> data)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(collectionName) || data == null || !data.Any())
                    return ("", null, false, "Invalid collection name or data");

                var insertCommand = new Dictionary<string, object>
                {
                    ["insert"] = collectionName,
                    ["documents"] = new[] { data }
                };

                return (Newtonsoft.Json.JsonConvert.SerializeObject(insertCommand), data, true, "");
            }
            catch (Exception ex)
            {
                return ("", null, false, ex.Message);
            }
        }

        /// <summary>
        /// Generates MongoDB update command.
        /// </summary>
        public (string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage) GenerateUpdateSql(
            string collectionName,
            Dictionary<string, object> data,
            Dictionary<string, object> conditions)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(collectionName) || data == null || !data.Any())
                    return ("", null, false, "Invalid collection name or data");

                var updateCommand = new Dictionary<string, object>
                {
                    ["update"] = collectionName,
                    ["updates"] = new[]
                    {
                        new Dictionary<string, object>
                        {
                            ["q"] = conditions ?? new Dictionary<string, object>(),
                            ["u"] = new Dictionary<string, object> { ["$set"] = data },
                            ["multi"] = true
                        }
                    }
                };

                var parameters = new Dictionary<string, object>();
                if (conditions != null)
                    foreach (var kvp in conditions)
                        parameters[kvp.Key] = kvp.Value;
                foreach (var kvp in data)
                    parameters[kvp.Key] = kvp.Value;

                return (Newtonsoft.Json.JsonConvert.SerializeObject(updateCommand), parameters, true, "");
            }
            catch (Exception ex)
            {
                return ("", null, false, ex.Message);
            }
        }

        /// <summary>
        /// Generates MongoDB delete command.
        /// </summary>
        public (string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage) GenerateDeleteSql(
            string collectionName,
            Dictionary<string, object> conditions)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(collectionName))
                    return ("", null, false, "Invalid collection name");

                var deleteCommand = new Dictionary<string, object>
                {
                    ["delete"] = collectionName,
                    ["deletes"] = new[]
                    {
                        new Dictionary<string, object>
                        {
                            ["q"] = conditions ?? new Dictionary<string, object>(),
                            ["limit"] = 0 // Delete all matching documents
                        }
                    }
                };

                return (Newtonsoft.Json.JsonConvert.SerializeObject(deleteCommand), conditions, true, "");
            }
            catch (Exception ex)
            {
                return ("", null, false, ex.Message);
            }
        }

        /// <summary>
        /// Generates MongoDB find command or aggregation pipeline.
        /// </summary>
        public (string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage) GenerateSelectSql(
            string collectionName,
            IEnumerable<string> columns = null,
            Dictionary<string, object> conditions = null,
            string orderBy = null,
            int? skip = null,
            int? take = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(collectionName))
                    return ("", null, false, "Invalid collection name");

                var findCommand = new Dictionary<string, object>
                {
                    ["find"] = collectionName
                };

                // Add filter
                if (conditions != null && conditions.Any())
                {
                    findCommand["filter"] = conditions;
                }

                // Add projection (field selection)
                if (columns != null && columns.Any())
                {
                    var projection = new Dictionary<string, int>();
                    foreach (var column in columns)
                        projection[column] = 1;
                    findCommand["projection"] = projection;
                }

                // Add sorting
                if (!string.IsNullOrWhiteSpace(orderBy))
                {
                    // Simple sort parsing - assumes format "field1 asc, field2 desc"
                    var sortSpec = new Dictionary<string, int>();
                    var sortParts = orderBy.Split(',');
                    foreach (var part in sortParts)
                    {
                        var trimmed = part.Trim();
                        if (trimmed.EndsWith(" desc", StringComparison.OrdinalIgnoreCase))
                        {
                            var field = trimmed.Substring(0, trimmed.Length - 5).Trim();
                            sortSpec[field] = -1;
                        }
                        else if (trimmed.EndsWith(" asc", StringComparison.OrdinalIgnoreCase))
                        {
                            var field = trimmed.Substring(0, trimmed.Length - 4).Trim();
                            sortSpec[field] = 1;
                        }
                        else
                        {
                            sortSpec[trimmed] = 1; // Default ascending
                        }
                    }
                    findCommand["sort"] = sortSpec;
                }

                // Add paging
                if (skip.HasValue)
                {
                    findCommand["skip"] = skip.Value;
                }
                if (take.HasValue)
                {
                    findCommand["limit"] = take.Value;
                }

                var parameters = new Dictionary<string, object>();
                if (conditions != null)
                    foreach (var kvp in conditions)
                        parameters[kvp.Key] = kvp.Value;

                return (Newtonsoft.Json.JsonConvert.SerializeObject(findCommand), parameters, true, "");
            }
            catch (Exception ex)
            {
                return ("", null, false, ex.Message);
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// MongoDB doesn't require identifier quoting for collection names.
        /// Returns the identifier as-is.
        /// </summary>
        public string QuoteIdentifier(string identifier)
        {
            return identifier;
        }

        /// <summary>
        /// Maps C# types to MongoDB BSON types.
        /// Uses DataTypeMappingRepository for mapping.
        /// </summary>
        public string MapClrTypeToDatasourceType(Type clrType, int? size = null, int? precision = null, int? scale = null)
        {
            try
            {
                if (clrType == null)
                    return "string";

                var netTypeName = clrType.FullName ?? clrType.Name;
                
                // Get mappings for this datasource type
                var mappings = DataTypeMappingRepository.GetDataTypes(SupportedType, _dmeEditor);
                if (mappings != null && mappings.Any())
                {
                    var exactMatch = mappings.FirstOrDefault(m => 
                        m.NetDataType.Equals(netTypeName, StringComparison.OrdinalIgnoreCase) && m.Fav)
                        ?? mappings.FirstOrDefault(m => m.NetDataType.Equals(netTypeName, StringComparison.OrdinalIgnoreCase));
                    
                    if (exactMatch != null)
                        return exactMatch.DataType;
                }

                // Minimal fallback for error cases only
                return "string";
            }
            catch
            {
                return "string"; // Minimal fallback
            }
        }

        /// <summary>
        /// Maps MongoDB BSON types back to C# types.
        /// Uses DataTypeMappingRepository for mapping.
        /// </summary>
        public Type MapDatasourceTypeToClrType(string datasourceType)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(datasourceType))
                    return typeof(object);

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

                // Minimal fallback for error cases only
                return typeof(object);
            }
            catch
            {
                return typeof(object); // Minimal fallback
            }
        }

        /// <summary>
        /// Validates entity structure for MongoDB compatibility.
        /// MongoDB is schema-flexible, so validation is minimal.
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

            // MongoDB has some restrictions on collection names
            if (entity.EntityName?.StartsWith("system.") == true)
            {
                errors.Add("Collection name cannot start with 'system.'");
            }

            if (entity.EntityName?.Contains("$") == true)
            {
                errors.Add("Collection name cannot contain '$' character");
            }

            return (errors.Count == 0, errors);
        }

        /// <summary>
        /// Checks if MongoDB supports a specific capability.
        /// </summary>
        public bool SupportsCapability(CapabilityType capability)
        {
            return Capabilities.IsCapable(capability);
        }

        /// <summary>
        /// MongoDB doesn't have string size limits.
        /// Returns -1 to indicate unlimited.
        /// </summary>
        public int GetMaxStringSize()
        {
            return -1; // Unlimited
        }

        /// <summary>
        /// MongoDB uses IEEE 754 floating point, so precision is limited.
        /// Returns 15 (double precision).
        /// </summary>
        public int GetMaxNumericPrecision()
        {
            return 15; // Double precision
        }

        #endregion
    }
}

