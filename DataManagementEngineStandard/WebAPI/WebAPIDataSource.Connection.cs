using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using System.Net.Http;
using TheTechIdea.Beep.Report;
using System.Data;
using TheTechIdea.Beep.Addin;
using System.Text.Json;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.WebAPI
{
    /// <summary>
    /// Connection management partial class for WebAPIDataSource
    /// Handles connection operations, entity discovery, and validation
    /// </summary>
    public partial class WebAPIDataSource
    {
        #region Connection Operations

        /// <summary>Opens connection to the Web API</summary>
        public ConnectionState Openconnection()
        {
            try
            {
                Logger?.WriteLog($"Opening connection to Web API: {DatasourceName}");
                
                if (Dataconnection?.ConnectionStatus == ConnectionState.Open)
                {
                    Logger?.WriteLog("Connection already open");
                    return ConnectionState.Open;
                }

                ErrorObject.Flag = Errors.Ok;
                
                cn.OpenConnection();
                ConnectionStatus = cn.ConnectionStatus;
                
                if (ConnectionStatus == ConnectionState.Open)
                {
                    Logger?.WriteLog($"Successfully connected to {DatasourceName}");
                    
                    // Initialize entities if not already done
                    if (Entities == null || Entities.Count == 0)
                    {
                        GetEntitiesAsync().Wait();
                    }
                }
                else
                {
                    Logger?.WriteLog($"Failed to connect to {DatasourceName}");
                    ErrorObject.Ex = new Exception($"Could not connect to {DatasourceName}");
                    ErrorObject.Flag = Errors.Failed;
                }

                return ConnectionStatus;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error opening connection: {ex.Message}");
                ErrorObject.Ex = ex;
                ErrorObject.Flag = Errors.Failed;
                ConnectionStatus = ConnectionState.Broken;
                return ConnectionStatus;
            }
        }

        /// <summary>Closes connection to the Web API</summary>
        public ConnectionState Closeconnection()
        {
            try
            {
                Logger?.WriteLog($"Closing connection to Web API: {DatasourceName}");
                
                if (Dataconnection?.ConnectionStatus == ConnectionState.Closed)
                {
                    Logger?.WriteLog("Connection already closed");
                    return ConnectionState.Closed;
                }

                cn.CloseConn();
                ConnectionStatus = ConnectionState.Closed;
                
                Logger?.WriteLog($"Connection to {DatasourceName} closed");
                return ConnectionStatus;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error closing connection: {ex.Message}");
                ErrorObject.Ex = ex;
                ErrorObject.Flag = Errors.Failed;
                ConnectionStatus = ConnectionState.Broken;
                return ConnectionStatus;
            }
        }

        /// <summary>Checks if connection is valid</summary>
        public bool CheckConnection()
        {
            try
            {
                if (ConnectionStatus == ConnectionState.Open)
                {
                    // Test connection with a simple health check
                    return TestConnectionAsync().Result;
                }
                return false;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Connection check failed: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> TestConnectionAsync()
        {
            try
            {
                var baseUrl = Dataconnection.ConnectionProp.Url;
                if (string.IsNullOrEmpty(baseUrl))
                {
                    return false;
                }

                // Try to make a simple HEAD or GET request to test connectivity
                var healthCheckEndpoint = GetConfigurationValue("HealthCheckEndpoint", "");
                var testUrl = !string.IsNullOrEmpty(healthCheckEndpoint) 
                    ? _dataHelper.BuildEndpointUrl(baseUrl, healthCheckEndpoint)
                    : baseUrl;

                var request = new HttpRequestMessage(HttpMethod.Head, testUrl);
                await _authHelper.EnsureAuthenticatedAsync();
                _authHelper.AddAuthenticationHeaders(request);
                var response = await _requestHelper.SendWithRetryAsync(request, "TestConnection");

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Connection test failed: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Entity Discovery

        /// <summary>Gets list of available entities from the API</summary>
        public async Task<List<EntityStructure>> GetEntitiesAsync()
        {
            try
            {
                Logger?.WriteLog($"Discovering entities for {DatasourceName}");

                if (Entities != null && Entities.Count > 0)
                {
                    Logger?.WriteLog($"Returning cached entities ({Entities.Count} found)");
                    return Entities;
                }

                // Check cache first
                var cacheKey = $"entities_{DatasourceName}";
                var cachedEntities = await _cacheHelper.GetOrSetAsync(cacheKey, async () =>
                {
                    return await DiscoverEntitiesFromAPI();
                }, TimeSpan.FromMinutes(30));

                Entities = cachedEntities ?? new List<EntityStructure>();
                EntitiesNames = Entities.Select(e => e.EntityName).ToList();

                Logger?.WriteLog($"Found {Entities.Count} entities");
                return Entities;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error discovering entities: {ex.Message}");
                ErrorObject.Ex = ex;
                ErrorObject.Flag = Errors.Failed;
                return new List<EntityStructure>();
            }
        }

        private async Task<List<EntityStructure>> DiscoverEntitiesFromAPI()
        {
            var entities = new List<EntityStructure>();

            try
            {
                // Get entities configuration from connection properties
                var entitiesConfig = GetConfigurationValue("Entities", "");
                if (!string.IsNullOrEmpty(entitiesConfig))
                {
                    // Parse configured entities
                    entities.AddRange(ParseEntitiesFromConfiguration(entitiesConfig));
                }
                else
                {
                    // Try to discover entities automatically
                    entities.AddRange(await AutoDiscoverEntities());
                }

                // If still no entities, create a default one
                if (entities.Count == 0)
                {
                    entities.Add(CreateDefaultEntity());
                }

                return entities;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error in entity discovery: {ex.Message}");
                return new List<EntityStructure> { CreateDefaultEntity() };
            }
        }

        private List<EntityStructure> ParseEntitiesFromConfiguration(string entitiesConfig)
        {
            var entities = new List<EntityStructure>();

            try
            {
                var entityNames = entitiesConfig.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var entityName in entityNames)
                {
                    var trimmedName = entityName.Trim();
                    if (!string.IsNullOrEmpty(trimmedName))
                    {
                        var entity = new EntityStructure
                        {
                            EntityName = trimmedName,
                            DatasourceEntityName = trimmedName,
                            DataSourceID = GuidID,
                            SchemaOrOwnerOrDatabase = DatasourceName,
                            DatabaseType = DatasourceType,
                            Fields = new List<EntityField>()
                        };

                        entities.Add(entity);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error parsing entities configuration: {ex.Message}");
            }

            return entities;
        }

        private async Task<List<EntityStructure>> AutoDiscoverEntities()
        {
            var entities = new List<EntityStructure>();

            try
            {
                var baseUrl = Dataconnection.ConnectionProp.Url;
                var discoveryEndpoint = GetConfigurationValue("DiscoveryEndpoint", "");
                
                if (!string.IsNullOrEmpty(discoveryEndpoint))
                {
                    var discoveryUrl = _dataHelper.BuildEndpointUrl(baseUrl, discoveryEndpoint);
                    var request = new HttpRequestMessage(HttpMethod.Get, discoveryUrl);
                    await _authHelper.EnsureAuthenticatedAsync();
                    _authHelper.AddAuthenticationHeaders(request);
                    var response = await _requestHelper.SendWithRetryAsync(request, "AutoDiscoverEntities");

                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        entities.AddRange(_dataHelper.ParseEntitiesFromDiscoveryResponse(content));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Auto discovery failed: {ex.Message}");
            }

            return entities;
        }

        private EntityStructure CreateDefaultEntity()
        {
            var defaultEntityName = GetConfigurationValue("DefaultEntityName", "data");
            
            return new EntityStructure
            {
                EntityName = defaultEntityName,
                DatasourceEntityName = defaultEntityName,
                DataSourceID = GuidID,
                SchemaOrOwnerOrDatabase = DatasourceName,
                DatabaseType = DatasourceType,
                Fields = new List<EntityField>
                {
                    new EntityField
                    {
                        fieldname = "id",
                        fieldtype = "System.String",
                        Size1 = 50,
                        AllowDBNull = true
                    },
                    new EntityField
                    {
                        fieldname = "data",
                        fieldtype = "System.String",
                        Size1 = -1,
                        AllowDBNull = true
                    }
                }
            };
        }

        /// <summary>Synchronous wrapper for GetEntitiesAsync</summary>
        public List<EntityStructure> GetEntities()
        {
            return GetEntitiesAsync().Result;
        }

        /// <summary>Gets a specific entity structure by name</summary>
        public EntityStructure GetEntityStructure(string entityName, bool refresh = false)
        {
            try
            {
                if (refresh || Entities == null || Entities.Count == 0)
                {
                    GetEntitiesAsync().Wait();
                }

                var entity = Entities?.FirstOrDefault(e => 
                    string.Equals(e.EntityName, entityName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(e.DatasourceEntityName, entityName, StringComparison.OrdinalIgnoreCase));

                if (entity == null)
                {
                    Logger?.WriteLog($"Entity '{entityName}' not found");
                    return null;
                }

                // Ensure entity has fields
                if (entity.Fields == null || entity.Fields.Count == 0)
                {
                    entity = InferEntityStructureAsync(entityName).Result;
                }

                return entity;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error getting entity structure for '{entityName}': {ex.Message}");
                ErrorObject.Ex = ex;
                ErrorObject.Flag = Errors.Failed;
                return null;
            }
        }

        private async Task<EntityStructure> InferEntityStructureAsync(string entityName)
        {
            try
            {
                // Try to get sample data to infer structure
                var baseUrl = Dataconnection.ConnectionProp.Url;
                var endpoint = entityName.ToLower(); // Use string method instead
                var url = _dataHelper.BuildEndpointUrl(baseUrl, endpoint, null, 0, 1);

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                await _authHelper.EnsureAuthenticatedAsync();
                _authHelper.AddAuthenticationHeaders(request);
                // Skip custom headers for now to avoid conversion issues
                
                var response = await _requestHelper.SendWithRetryAsync(request, $"InferStructure_{entityName}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(content))
                    {
                        using (var doc = JsonDocument.Parse(content))
                        {
                            var root = doc.RootElement;
                            
                            // Get first item from array or object itself
                            JsonElement sampleElement = root;
                            if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
                            {
                                sampleElement = root[0];
                            }
                            
                            return _dataHelper.InferEntityStructureFromSample(entityName, sampleElement);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error inferring structure for '{entityName}': {ex.Message}");
            }

            // Return default structure if inference fails
            return CreateDefaultEntity();
        }

        #endregion

        #region Validation

        /// <summary>Checks if entity exists</summary>
        public bool CheckEntityExist(string EntityName)
        {
            try
            {
                if (Entities == null || Entities.Count == 0)
                {
                    GetEntitiesAsync().Wait();
                }

                return Entities?.Any(e => 
                    string.Equals(e.EntityName, EntityName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(e.DatasourceEntityName, EntityName, StringComparison.OrdinalIgnoreCase)) ?? false;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error checking if entity '{EntityName}' exists: {ex.Message}");
                return false;
            }
        }

        #endregion
    }
}
