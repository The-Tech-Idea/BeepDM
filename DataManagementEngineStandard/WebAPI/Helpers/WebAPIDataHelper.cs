using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using System.Text;
using System.Reflection;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.WebAPI.Helpers
{
    /// <summary>
    /// Data processing helper for Web API responses
    /// </summary>
    public class WebAPIDataHelper
    {
        private readonly IDMLogger _logger;
        private readonly string _dataSourceName;

        public WebAPIDataHelper(IDMLogger logger, string dataSourceName)
        {
            _logger = logger;
            _dataSourceName = dataSourceName ?? "WebAPI";
        }

        public string BuildEndpointUrl(string baseUrl, string endpoint, List<AppFilter> filters = null, int? pageNumber = null, int? pageSize = null)
        {
            var url = new StringBuilder();
            url.Append(baseUrl?.TrimEnd('/'));
            url.Append('/');
            url.Append(endpoint?.TrimStart('/'));

            var queryParams = new List<string>();

            // Add filters as query parameters
            if (filters?.Any() == true)
            {
                foreach (var filter in filters.Where(f => !string.IsNullOrEmpty(f.FilterValue)))
                {
                    var paramName = filter.FieldName;
                    var paramValue = Uri.EscapeDataString(filter.FilterValue);
                    queryParams.Add($"{paramName}={paramValue}");
                }
            }

            // Add pagination parameters
            if (pageNumber.HasValue)
            {
                queryParams.Add($"page={pageNumber.Value}");
            }
            
            if (pageSize.HasValue)
            {
                queryParams.Add($"limit={pageSize.Value}");
            }

            if (queryParams.Any())
            {
                url.Append('?');
                url.Append(string.Join("&", queryParams));
            }

            return url.ToString();
        }

        public string BuildEntityEndpoint(EntityStructure entity, List<AppFilter> filters = null, int? pageNumber = null, int? pageSize = null)
        {
            var endpoint = entity.DatasourceEntityName ?? entity.EntityName;
            
            // Use custom query if available
            if (!string.IsNullOrEmpty(entity.CustomBuildQuery))
            {
                return ProcessCustomQuery(entity.CustomBuildQuery, filters, entity.Parameters);
            }

            return endpoint;
        }

        public IEnumerable<object> ProcessApiResponse(string jsonResponse, EntityStructure entityStructure)
        {
            try
            {
                if (string.IsNullOrEmpty(jsonResponse))
                    return new List<object>();

                // Try to deserialize as JSON
                using (var document = JsonDocument.Parse(jsonResponse))
                {
                    var root = document.RootElement;
                    
                    // Handle different response formats
                    if (root.ValueKind == JsonValueKind.Array)
                    {
                        return ProcessJsonArray(root, entityStructure);
                    }
                    else if (root.ValueKind == JsonValueKind.Object)
                    {
                        return ProcessJsonObject(root, entityStructure);
                    }
                }
            }
            catch (JsonException ex)
            {
                _logger?.WriteLog($"Failed to parse JSON response: {ex.Message}");
                // Try to handle as plain text or other format
                return ProcessNonJsonResponse(jsonResponse, entityStructure);
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error processing API response: {ex.Message}");
            }

            return new List<object>();
        }

        public int GetTotalRecordsFromResponse(string jsonResponse, System.Net.Http.Headers.HttpResponseHeaders headers = null)
        {
            try
            {
                // Try to get from headers first
                if (headers != null && headers.TryGetValues("X-Total-Count", out var totalCountValues))
                {
                    if (int.TryParse(totalCountValues.FirstOrDefault(), out var totalFromHeader))
                        return totalFromHeader;
                }

                // Try to get from JSON response
                if (!string.IsNullOrEmpty(jsonResponse))
                {
                    using (var document = JsonDocument.Parse(jsonResponse))
                    {
                        var root = document.RootElement;
                        
                        // Look for common total count fields
                        var totalFields = new[] { "total", "totalCount", "total_count", "count", "totalItems", "total_items" };
                        
                        foreach (var field in totalFields)
                        {
                            if (root.TryGetProperty(field, out var totalElement) && 
                                totalElement.ValueKind == JsonValueKind.Number)
                            {
                                return totalElement.GetInt32();
                            }
                        }

                        // If it's an array, return the array length
                        if (root.ValueKind == JsonValueKind.Array)
                        {
                            return root.GetArrayLength();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error extracting total records count: {ex.Message}");
            }

            return 0;
        }

        private IEnumerable<object> ProcessJsonArray(JsonElement arrayElement, EntityStructure entityStructure)
        {
            var result = new List<object>();

            foreach (var item in arrayElement.EnumerateArray())
            {
                var obj = ConvertJsonElementToObject(item, entityStructure);
                result.Add(obj);
            }

            return result;
        }

        private IEnumerable<object> ProcessJsonObject(JsonElement objectElement, EntityStructure entityStructure)
        {
            var dataFields = new[] { "data", "items", "results", "records", "content" };
            
            foreach (var field in dataFields)
            {
                if (objectElement.TryGetProperty(field, out var dataElement) && 
                    dataElement.ValueKind == JsonValueKind.Array)
                {
                    return ProcessJsonArray(dataElement, entityStructure);
                }
            }

            var singleList = new List<object> { ConvertJsonElementToObject(objectElement, entityStructure) };
            return singleList;
        }

        private object ConvertJsonElementToObject(JsonElement jsonElement, EntityStructure entityStructure)
        {
            var dictionary = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            foreach (var property in jsonElement.EnumerateObject())
            {
                dictionary[property.Name] = ConvertJsonValueToClrValue(property.Value);
            }

            return dictionary;
        }

        private object ConvertJsonValueToClrValue(JsonElement jsonValue)
        {
            switch (jsonValue.ValueKind)
            {
                case JsonValueKind.String:
                    return jsonValue.GetString();
                
                case JsonValueKind.Number:
                    if (jsonValue.TryGetInt32(out var i)) return i;
                    if (jsonValue.TryGetInt64(out var l)) return l;
                    if (jsonValue.TryGetDouble(out var d)) return d;
                    return jsonValue.GetDecimal();
                
                case JsonValueKind.True:
                    return true;
                
                case JsonValueKind.False:
                    return false;
                
                case JsonValueKind.Null:
                    return null;
                
                case JsonValueKind.Array:
                    var list = new List<object>();
                    foreach (var item in jsonValue.EnumerateArray())
                    {
                        list.Add(ConvertJsonValueToClrValue(item));
                    }
                    return list;
                
                case JsonValueKind.Object:
                    var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    foreach (var prop in jsonValue.EnumerateObject())
                    {
                        dict[prop.Name] = ConvertJsonValueToClrValue(prop.Value);
                    }
                    return dict;
                
                default:
                    return jsonValue.ToString();
            }
        }

        private IEnumerable<object> ProcessNonJsonResponse(string response, EntityStructure entityStructure)
        {
            var list = new List<object>();
            list.Add(new Dictionary<string, object> { ["response"] = response });
            return list;
        }

        private string ProcessCustomQuery(string customQuery, List<AppFilter> filters, List<EntityParameters> parameters)
        {
            var processedQuery = customQuery;

            // Replace parameters with actual values
            if (parameters?.Any() == true && filters?.Any() == true)
            {
                foreach (var param in parameters)
                {
                    var filterValue = filters
                        .Where(f => f.FieldName.Equals(param.parameterName, StringComparison.OrdinalIgnoreCase))
                        .Select(f => f.FilterValue)
                        .FirstOrDefault();

                    if (!string.IsNullOrEmpty(filterValue))
                    {
                        var placeholder = $"{{{param.parameterIndex}}}";
                        processedQuery = processedQuery.Replace(placeholder, Uri.EscapeDataString(filterValue));
                    }
                }
            }

            return processedQuery;
        }

        public HttpRequestMessage CreateRequest(HttpMethod method, string endpoint, object data = null)
        {
            var request = new HttpRequestMessage(method, endpoint);

            if (data != null && (method == HttpMethod.Post || method == HttpMethod.Put || method == HttpMethod.Patch))
            {
                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            return request;
        }

        public void AddCustomHeaders(HttpRequestMessage request, List<WebApiHeader> headers)
        {
            if (headers?.Any() == true)
            {
                foreach (var header in headers)
                {
                    try
                    {
                        request.Headers.Add(header.Headername, header.Headervalue);
                    }
                    catch (InvalidOperationException)
                    {
                        // Some headers need to be added to content headers
                        try
                        {
                            if (request.Content != null)
                            {
                                request.Content.Headers.Add(header.Headername, header.Headervalue);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger?.WriteLog($"Could not add header {header.Headername}: {ex.Message}");
                        }
                    }
                }
            }
        }

        /// <summary>Builds entity endpoint for specific ID</summary>
        public string BuildEntityEndpoint(string entityName, string id)
        {
            var endpoint = entityName.ToLower();
            if (!string.IsNullOrEmpty(id))
            {
                endpoint = $"{endpoint}/{id}";
            }
            return endpoint;
        }

        /// <summary>Extracts ID value from data object</summary>
        public object ExtractIdValue(object data)
        {
            if (data == null) return null;

            try
            {
                // Try common ID field names
                var idFields = new[] { "id", "Id", "ID", "_id", "uuid", "key" };
                
                if (data is Dictionary<string, object> dict)
                {
                    foreach (var field in idFields) if (dict.ContainsKey(field)) return dict[field];
                    return dict.Values.FirstOrDefault();
                }

                // Use reflection for other object types
                var type = data.GetType();
                foreach (var field in idFields)
                {
                    var prop = type.GetProperty(field, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                    if (prop != null) return prop.GetValue(data);
                }

                // Try first property as fallback
                return type.GetProperties(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault()?.GetValue(data);
            }
            catch (Exception ex) { _logger?.WriteLog($"Error extracting ID value: {ex.Message}"); return null; }
        }

        /// <summary>Parses entities from discovery API response</summary>
        public List<EntityStructure> ParseEntitiesFromDiscoveryResponse(string jsonResponse)
        {
            var entities = new List<EntityStructure>();

            try
            {
                if (string.IsNullOrEmpty(jsonResponse)) return entities;

                var jsonDoc = JsonDocument.Parse(jsonResponse);
                JsonElement dataElement = jsonDoc.RootElement;
                
                // Handle different response formats
                if (dataElement.TryGetProperty("data", out var d) ||
                    dataElement.TryGetProperty("entities", out d) ||
                    dataElement.TryGetProperty("resources", out d) ||
                    dataElement.TryGetProperty("endpoints", out d))
                { dataElement = d; }

                if (dataElement.ValueKind == JsonValueKind.Array)
                { foreach (var item in dataElement.EnumerateArray()) { var e = ParseEntityFromJsonElement(item); if (e != null) entities.Add(e); } }
                else if (dataElement.ValueKind == JsonValueKind.Object)
                { var e = ParseEntityFromJsonElement(dataElement); if (e != null) entities.Add(e); }

                _logger?.WriteLog($"Parsed {entities.Count} entities from discovery response");
            }
            catch (Exception ex) { _logger?.WriteLog($"Error parsing entities: {ex.Message}"); }

            return entities;
        }

        /// <summary>Infers entity structure from sample data</summary>
        public EntityStructure InferEntityStructureFromSample(string entityName, JsonElement sampleData)
        {
            try
            {
                var entity = new EntityStructure { EntityName = entityName, DataSourceID = "WebAPI", DatasourceEntityName = entityName, Fields = new List<EntityField>() };
                if (sampleData.ValueKind == JsonValueKind.Object)
                { foreach (var prop in sampleData.EnumerateObject()) { var field = CreateFieldFromJsonProperty(prop); if (field != null) entity.Fields.Add(field); } }
                _logger?.WriteLog($"Inferred structure for entity '{entityName}' with {entity.Fields.Count} fields");
                return entity;
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error inferring structure for '{entityName}': {ex.Message}");
                return new EntityStructure { EntityName = entityName, DataSourceID = "WebAPI", Fields = new List<EntityField>() };
            }
        }

        /// <summary>Generates cache key from multiple parameters</summary>
        public string GenerateCacheKey(string baseKey, List<AppFilter> filters = null, int pageNumber = 0, int pageSize = 0)
        {
            var keyBuilder = new StringBuilder(baseKey);
            
            if (filters != null && filters.Count > 0)
            {
                keyBuilder.Append("_filters:");
                foreach (var f in filters) keyBuilder.Append($"{f.FieldName}={f.FilterValue}_");
            }
            if (pageNumber > 0 || pageSize > 0) keyBuilder.Append($"_page:{pageNumber}_size:{pageSize}");
            return keyBuilder.ToString();
        }

        #region Private Methods
        private EntityStructure ParseEntityFromJsonElement(JsonElement element)
        {
            try
            {
                string entityName = null;
                if (element.TryGetProperty("name", out var nameElement)) entityName = nameElement.GetString();
                else if (element.TryGetProperty("entityName", out var en)) entityName = en.GetString();
                else if (element.TryGetProperty("resource", out var resourceElement)) entityName = resourceElement.GetString();
                else if (element.TryGetProperty("endpoint", out var endpointElement)) entityName = endpointElement.GetString();
                if (string.IsNullOrEmpty(entityName)) return null;
                var entity = new EntityStructure { EntityName = entityName, DataSourceID = "WebAPI", DatasourceEntityName = entityName, Fields = new List<EntityField>() };
                if (element.TryGetProperty("schema", out var schemaElement) || element.TryGetProperty("fields", out schemaElement) || element.TryGetProperty("properties", out schemaElement))
                { if (schemaElement.ValueKind == JsonValueKind.Object) foreach (var p in schemaElement.EnumerateObject()) { var field = CreateFieldFromJsonProperty(p); if (field != null) entity.Fields.Add(field); } }
                return entity;
            }
            catch (Exception ex) { _logger?.WriteLog($"Error parsing entity: {ex.Message}"); return null; }
        }
        private EntityField CreateFieldFromJsonProperty(JsonProperty property)
        {
            try
            {
                return new EntityField { fieldname = property.Name, fieldtype = InferDataTypeFromJsonValue(property.Value), IsKey = IsLikelyIdField(property.Name), AllowDBNull = true, IsUnique = IsLikelyIdField(property.Name) };
            }
            catch (Exception ex) { _logger?.WriteLog($"Error creating field '{property.Name}': {ex.Message}"); return null; }
        }
        private string InferDataTypeFromJsonValue(JsonElement value)
        {
            switch (value.ValueKind)
            {
                case JsonValueKind.String: return "System.String";
                case JsonValueKind.Number: if (value.TryGetInt32(out _)) return "System.Int32"; if (value.TryGetInt64(out _)) return "System.Int64"; return "System.Double";
                case JsonValueKind.True:
                case JsonValueKind.False: return "System.Boolean";
                case JsonValueKind.Array: return "System.String";
                case JsonValueKind.Object: return "System.String";
                default: return "System.String";
            }
        }
        private bool IsLikelyIdField(string fieldName)
        { var idFields = new[] { "id", "_id", "uuid", "key", "pk", "primarykey" }; return idFields.Contains(fieldName.ToLower()); }
        #endregion
    }
}
