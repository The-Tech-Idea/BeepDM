using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.RegularExpressions;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Report;
using System.Net.Http;

namespace TheTechIdea.Beep.WebAPI.Helpers
{
    /// <summary>
    /// Schema discovery and validation helper for Web APIs
    /// Supports OpenAPI/Swagger, JSON Schema, and dynamic schema inference
    /// </summary>
    public class WebAPISchemaHelper : IDisposable
    {
        #region Private Fields

        private readonly IDMLogger _logger;
        private readonly string _datasourceName;
        private readonly Dictionary<string, EntityStructure> _schemaCache;
        private readonly Dictionary<string, DateTime> _schemaCacheTimestamps;
        private readonly object _cacheLock = new object();
        private bool _disposed;

        #endregion

        #region Properties

        /// <summary>Schema cache expiration time in minutes</summary>
        public int SchemaCacheExpirationMinutes { get; set; } = 60;

        /// <summary>Maximum depth for nested object analysis</summary>
        public int MaxNestingDepth { get; set; } = 5;

        /// <summary>Minimum sample size for schema inference</summary>
        public int MinSampleSize { get; set; } = 3;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes the schema helper
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="datasourceName">Data source name for logging</param>
        public WebAPISchemaHelper(IDMLogger logger, string datasourceName)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _datasourceName = datasourceName ?? throw new ArgumentNullException(nameof(datasourceName));
            _schemaCache = new Dictionary<string, EntityStructure>();
            _schemaCacheTimestamps = new Dictionary<string, DateTime>();
        }

        #endregion

        #region Public Methods
        public virtual async Task<EntityStructure> InferEntityStructureAsync(string entityName)
        {
            // This is a simplified example. A real implementation would need a way to get sample data.
            // For instance, by making a request to a known endpoint for that entity.
            var request = new HttpRequestMessage(HttpMethod.Get, entityName);
            var response = await new HttpClient().SendAsync(request); // Simplified; use a shared client in real code
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return await InferSchemaFromJsonAsync(entityName, new[] { json });
            }

            return null;
        }
        /// <summary>
        /// Infers entity structure from JSON response data
        /// </summary>
        /// <param name="entityName">Name of the entity</param>
        /// <param name="jsonData">JSON data samples</param>
        /// <param name="forceRefresh">Force schema refresh</param>
        /// <returns>Inferred entity structure</returns>
        public virtual Task<EntityStructure> InferSchemaFromJsonAsync(string entityName, 
            IEnumerable<string> jsonData, 
            bool forceRefresh = false)
        {
            return Task.Run(() => InferSchemaFromJson(entityName, jsonData, forceRefresh));
        }

        /// <summary>
        /// Infers entity structure from JSON response data (synchronous version)
        /// </summary>
        private EntityStructure InferSchemaFromJson(string entityName, 
            IEnumerable<string> jsonData, 
            bool forceRefresh = false)
        {
            if (string.IsNullOrEmpty(entityName))
                throw new ArgumentException("Entity name cannot be empty", nameof(entityName));

            if (jsonData == null || !jsonData.Any())
                throw new ArgumentException("JSON data cannot be empty", nameof(jsonData));

            // Check cache first
            if (!forceRefresh && TryGetCachedSchema(entityName, out var cachedSchema))
            {
                return cachedSchema;
            }

            var schema = new EntityStructure
            {
                EntityName = entityName,
                DatasourceEntityName = entityName,
                DataSourceID = _datasourceName,
                Fields = new List<EntityField>()
            };

            var fieldAnalysis = new Dictionary<string, FieldAnalysis>();

            try
            {
                // Analyze each JSON sample
                foreach (var json in jsonData.Take(50)) // Limit samples for performance
                {
                    if (string.IsNullOrWhiteSpace(json))
                        continue;

                    try
                    {
                        var doc = JsonDocument.Parse(json);
                        AnalyzeJsonElement(doc.RootElement, "", fieldAnalysis, 0);
                    }
                    catch (JsonException ex)
                    {
                        _logger?.WriteLog($"Failed to parse JSON sample for {entityName}: {ex.Message}");
                        continue;
                    }
                }

                // Convert analysis to entity fields
                foreach (var kvp in fieldAnalysis.OrderBy(x => x.Key))
                {
                    var field = CreateEntityFieldFromAnalysis(kvp.Key, kvp.Value);
                    if (field != null)
                    {
                        schema.Fields.Add(field);
                    }
                }

                // Cache the schema
                CacheSchema(entityName, schema);

                _logger?.WriteLog($"Inferred schema for {entityName} with {schema.Fields.Count} fields");
                return schema;
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error inferring schema for {entityName}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Validates entity structure against actual data
        /// </summary>
        /// <param name="entity">Entity structure to validate</param>
        /// <param name="jsonData">JSON data samples</param>
        /// <returns>Validation results</returns>
        public virtual Task<SchemaValidationResult> ValidateSchemaAsync(EntityStructure entity, 
            IEnumerable<string> jsonData)
        {
            return Task.Run(() => ValidateSchema(entity, jsonData));
        }

        /// <summary>
        /// Validates entity structure against actual data (synchronous version)
        /// </summary>
        private SchemaValidationResult ValidateSchema(EntityStructure entity, 
            IEnumerable<string> jsonData)
        {
            var result = new SchemaValidationResult
            {
                EntityName = entity.EntityName,
                IsValid = true,
                Issues = new List<string>(),
                SuggestedChanges = new List<string>()
            };

            try
            {
                var actualFields = new HashSet<string>();
                var fieldTypes = new Dictionary<string, HashSet<Type>>();

                // Analyze actual data
                foreach (var json in jsonData.Take(20))
                {
                    if (string.IsNullOrWhiteSpace(json))
                        continue;

                    try
                    {
                        var doc = JsonDocument.Parse(json);
                        CollectActualFields(doc.RootElement, "", actualFields, fieldTypes, 0);
                    }
                    catch (JsonException)
                    {
                        continue;
                    }
                }

                // Check for missing fields in schema
                var schemaFields = new HashSet<string>(entity.Fields.Select(f => f.fieldname));
                var missingInSchema = actualFields.Except(schemaFields).ToList();
                var missingInData = schemaFields.Except(actualFields).ToList();

                if (missingInSchema.Any())
                {
                    result.IsValid = false;
                    result.Issues.Add($"Fields found in data but not in schema: {string.Join(", ", missingInSchema)}");
                    result.SuggestedChanges.Add("Consider updating schema to include missing fields");
                }

                if (missingInData.Any())
                {
                    result.Issues.Add($"Fields in schema but not found in recent data: {string.Join(", ", missingInData)}");
                    result.SuggestedChanges.Add("Fields may be optional or deprecated");
                }

                // Check field type consistency
                foreach (var field in entity.Fields)
                {
                    if (fieldTypes.ContainsKey(field.fieldname))
                    {
                        var actualTypes = fieldTypes[field.fieldname];
                        var expectedType = GetTypeFromString(field.fieldtype);
                        
                        if (!actualTypes.Contains(expectedType) && actualTypes.Any())
                        {
                            result.IsValid = false;
                            result.Issues.Add($"Field {field.fieldname} type mismatch. Expected: {field.fieldtype}, Found: {string.Join(", ", actualTypes.Select(t => t.Name))}");
                            result.SuggestedChanges.Add($"Update {field.fieldname} field type");
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error validating schema for {entity.EntityName}: {ex.Message}");
                result.IsValid = false;
                result.Issues.Add($"Validation error: {ex.Message}");
                return result;
            }
        }

        /// <summary>
        /// Updates entity structure based on new data
        /// </summary>
        /// <param name="entity">Current entity structure</param>
        /// <param name="jsonData">New JSON data samples</param>
        /// <returns>Updated entity structure</returns>
        public virtual async Task<EntityStructure> UpdateSchemaAsync(EntityStructure entity, IEnumerable<string> jsonData)
        {
            var validation = await ValidateSchemaAsync(entity, jsonData);
            
            if (validation.IsValid)
            {
                return entity; // No changes needed
            }

            // Create updated schema
            var updatedSchema = await InferSchemaFromJsonAsync(entity.EntityName, jsonData, true);
            
            // Preserve existing field metadata where possible
            foreach (var existingField in entity.Fields)
            {
                var matchingField = updatedSchema.Fields.FirstOrDefault(f => f.fieldname == existingField.fieldname);
                if (matchingField != null)
                {
                    // Preserve custom metadata
                    matchingField.IsKey = existingField.IsKey;
                    matchingField.IsUnique = existingField.IsUnique;
                    matchingField.IsAutoIncrement = existingField.IsAutoIncrement;
                    matchingField.AllowDBNull = existingField.AllowDBNull;
                }
            }

            CacheSchema(entity.EntityName, updatedSchema);
            return updatedSchema;
        }

        /// <summary>
        /// Gets cached schema if available and not expired
        /// </summary>
        /// <param name="entityName">Entity name</param>
        /// <param name="schema">Output schema if found</param>
        /// <returns>True if cached schema is available</returns>
        public virtual bool TryGetCachedSchema(string entityName, out EntityStructure schema)
        {
            lock (_cacheLock)
            {
                schema = null;
                
                if (!_schemaCache.ContainsKey(entityName))
                    return false;

                if (!_schemaCacheTimestamps.ContainsKey(entityName))
                    return false;

                var cacheTime = _schemaCacheTimestamps[entityName];
                if (DateTime.UtcNow - cacheTime > TimeSpan.FromMinutes(SchemaCacheExpirationMinutes))
                {
                    _schemaCache.Remove(entityName);
                    _schemaCacheTimestamps.Remove(entityName);
                    return false;
                }

                schema = _schemaCache[entityName];
                return true;
            }
        }

        /// <summary>
        /// Clears schema cache
        /// </summary>
        public virtual void ClearCache()
        {
            lock (_cacheLock)
            {
                _schemaCache.Clear();
                _schemaCacheTimestamps.Clear();
            }
        }

        #endregion

        #region Private Methods

        private void AnalyzeJsonElement(JsonElement element, string basePath, Dictionary<string, FieldAnalysis> analysis, int depth)
        {
            if (depth > MaxNestingDepth)
                return;

            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject())
                    {
                        var propertyPath = string.IsNullOrEmpty(basePath) ? property.Name : $"{basePath}.{property.Name}";
                        AnalyzeJsonElement(property.Value, propertyPath, analysis, depth + 1);
                    }
                    break;

                case JsonValueKind.Array:
                    if (element.GetArrayLength() > 0)
                    {
                        // Analyze first few array elements
                        var index = 0;
                        foreach (var item in element.EnumerateArray().Take(3))
                        {
                            AnalyzeJsonElement(item, basePath, analysis, depth + 1);
                            index++;
                        }
                    }
                    break;

                default:
                    // Leaf value - analyze type
                    if (!string.IsNullOrEmpty(basePath))
                    {
                        if (!analysis.ContainsKey(basePath))
                        {
                            analysis[basePath] = new FieldAnalysis { FieldName = basePath };
                        }

                        var fieldAnalysis = analysis[basePath];
                        fieldAnalysis.SampleCount++;
                        fieldAnalysis.ValueKinds.Add(element.ValueKind);

                        // Analyze value characteristics
                        AnalyzeValue(element, fieldAnalysis);
                    }
                    break;
            }
        }

        private void AnalyzeValue(JsonElement element, FieldAnalysis analysis)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    var stringValue = element.GetString();
                    analysis.StringLengths.Add(stringValue?.Length ?? 0);
                    
                    // Check for date patterns
                    if (DateTime.TryParse(stringValue, out _))
                    {
                        analysis.PossibleTypes.Add(typeof(DateTime));
                    }
                    else if (Guid.TryParse(stringValue, out _))
                    {
                        analysis.PossibleTypes.Add(typeof(Guid));
                    }
                    else
                    {
                        analysis.PossibleTypes.Add(typeof(string));
                    }
                    break;

                case JsonValueKind.Number:
                    if (element.TryGetInt32(out _))
                        analysis.PossibleTypes.Add(typeof(int));
                    else if (element.TryGetInt64(out _))
                        analysis.PossibleTypes.Add(typeof(long));
                    else
                        analysis.PossibleTypes.Add(typeof(double));
                    break;

                case JsonValueKind.True:
                case JsonValueKind.False:
                    analysis.PossibleTypes.Add(typeof(bool));
                    break;

                case JsonValueKind.Null:
                    analysis.NullCount++;
                    break;
            }
        }

        private EntityField CreateEntityFieldFromAnalysis(string fieldName, FieldAnalysis analysis)
        {
            var mostCommonType = analysis.PossibleTypes.GroupBy(t => t)
                .OrderByDescending(g => g.Count())
                .First().Key;

            var field = new EntityField
            {
                fieldname = fieldName,
                fieldtype = mostCommonType.ToString(),
                AllowDBNull = analysis.NullCount > 0,
                Size1 = analysis.StringLengths.Any() ? analysis.StringLengths.Max() : 0,
                IsKey = false,
                IsUnique = false,
                IsAutoIncrement = false
            };

            // Set field category based on type - using custom properties if available
            if (mostCommonType == typeof(string))
            {
                // field.fieldcategory = "Text";
            }
            else if (mostCommonType == typeof(int) || mostCommonType == typeof(long))
            {
                // field.fieldcategory = "Number";
            }
            else if (mostCommonType == typeof(double) || mostCommonType == typeof(decimal))
            {
                // field.fieldcategory = "Decimal";
            }
            else if (mostCommonType == typeof(DateTime))
            {
                // field.fieldcategory = "Date";
            }
            else if (mostCommonType == typeof(bool))
            {
                // field.fieldcategory = "Boolean";
            }
            else
            {
                // field.fieldcategory = "Object";
            }

            return field;
        }

        private void CollectActualFields(JsonElement element, string basePath, HashSet<string> fields, Dictionary<string, HashSet<Type>> fieldTypes, int depth)
        {
            if (depth > MaxNestingDepth)
                return;

            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject())
                    {
                        var propertyPath = string.IsNullOrEmpty(basePath) ? property.Name : $"{basePath}.{property.Name}";
                        CollectActualFields(property.Value, propertyPath, fields, fieldTypes, depth + 1);
                    }
                    break;

                case JsonValueKind.Array:
                    if (element.GetArrayLength() > 0)
                    {
                        foreach (var item in element.EnumerateArray().Take(1))
                        {
                            CollectActualFields(item, basePath, fields, fieldTypes, depth + 1);
                        }
                    }
                    break;

                default:
                    if (!string.IsNullOrEmpty(basePath))
                    {
                        fields.Add(basePath);
                        
                        if (!fieldTypes.ContainsKey(basePath))
                            fieldTypes[basePath] = new HashSet<Type>();

                        var type = GetTypeFromJsonElement(element);
                        fieldTypes[basePath].Add(type);
                    }
                    break;
            }
        }

        private Type GetTypeFromJsonElement(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    var stringValue = element.GetString();
                    if (DateTime.TryParse(stringValue, out _))
                        return typeof(DateTime);
                    if (Guid.TryParse(stringValue, out _))
                        return typeof(Guid);
                    return typeof(string);

                case JsonValueKind.Number:
                    if (element.TryGetInt32(out _))
                        return typeof(int);
                    if (element.TryGetInt64(out _))
                        return typeof(long);
                    return typeof(double);

                case JsonValueKind.True:
                case JsonValueKind.False:
                    return typeof(bool);

                default:
                    return typeof(object);
            }
        }

        private Type GetTypeFromString(string typeString)
        {
            switch (typeString)
            {
                case "System.String": return typeof(string);
                case "System.Int32": return typeof(int);
                case "System.Int64": return typeof(long);
                case "System.Double": return typeof(double);
                case "System.Decimal": return typeof(decimal);
                case "System.Boolean": return typeof(bool);
                case "System.DateTime": return typeof(DateTime);
                case "System.Guid": return typeof(Guid);
                default: return typeof(object);
            }
        }

        private void CacheSchema(string entityName, EntityStructure schema)
        {
            lock (_cacheLock)
            {
                _schemaCache[entityName] = schema;
                _schemaCacheTimestamps[entityName] = DateTime.UtcNow;
            }
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes resources used by the schema helper
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                ClearCache();
                _disposed = true;
            }
        }

        #endregion

        #region Helper Classes

        private class FieldAnalysis
        {
            public string FieldName { get; set; }
            public int SampleCount { get; set; }
            public int NullCount { get; set; }
            public HashSet<JsonValueKind> ValueKinds { get; set; } = new HashSet<JsonValueKind>();
            public HashSet<Type> PossibleTypes { get; set; } = new HashSet<Type>();
            public List<int> StringLengths { get; set; } = new List<int>();
        }

        /// <summary>
        /// Schema validation result
        /// </summary>
        public class SchemaValidationResult
        {
            /// <summary>Name of the validated entity</summary>
            public string EntityName { get; set; }
            
            /// <summary>Indicates if the schema is valid</summary>
            public bool IsValid { get; set; }
            
            /// <summary>List of validation issues found</summary>
            public List<string> Issues { get; set; } = new List<string>();
            
            /// <summary>List of suggested changes to fix issues</summary>
            public List<string> SuggestedChanges { get; set; } = new List<string>();
        }

        #endregion
    }
}
