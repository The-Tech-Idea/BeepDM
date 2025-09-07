using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;
using System.Net.Http;
using System.Text.Json;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.WebAPI
{
    /// <summary>
    /// Structure and metadata related methods.
    /// </summary>
    public partial class WebAPIDataSource
    {
        /// <summary>
        /// Gets the list of available entities from the Web API
        /// </summary>
        public List<string> GetEntitesList()
        {
            try
            {
                Logger.WriteLog("Getting entities list");
                var cacheKey = _cacheHelper.GenerateCacheKey("EntitiesList");
                var cachedList = _cacheHelper.Get<List<string>>(cacheKey);
                if (cachedList != null)
                {
                    return cachedList;
                }

                var endpointConfig = _configHelper.GetEndpointConfiguration("entities");
                var url = _dataHelper.BuildEndpointUrl(_configHelper.BaseUrl, endpointConfig.ListEndpoint);
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                
                // Ensure authentication before making request
                _authHelper.EnsureAuthenticatedAsync().Wait();
                
                // Add headers from configuration
                var headers = _configHelper.GetHeaders();
                foreach (var header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }
                
                // Add authentication headers
                _authHelper.AddAuthenticationHeaders(request);
                
                var response = _requestHelper.SendWithRetryAsync(request, "GetEntitiesList").Result;

                if (response.IsSuccessStatusCode)
                {
                    var content = response.Content.ReadAsStringAsync().Result;
                    var entities = JsonSerializer.Deserialize<List<string>>(content) ?? new List<string>();
                    _cacheHelper.Set(cacheKey, entities, TimeSpan.FromMinutes(30));
                    return entities;
                }
                else
                {
                    _errorHelper.HandleErrorResponseAsync(response).Wait();
                    return new List<string>();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error in GetEntitesList: {ex.Message}");
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                return new List<string>();
            }
        }
        /// <summary>
        /// Checks if an entity exists in the data source
        /// </summary>
        public bool CheckEntityExist(string EntityName)
        {
            try
            {
                var entities = GetEntitesList();
                return entities.Contains(EntityName, StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error in CheckEntityExist: {ex.Message}");
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                return false;
            }
        }
        /// <summary>
        /// Gets the index of an entity in the entities list
        /// </summary>
        public int GetEntityIdx(string entityName)
        {
            try
            {
                var entities = GetEntitesList();
                return entities.FindIndex(e => string.Equals(e, entityName, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error in GetEntityIdx: {ex.Message}");
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                return -1;
            }
        }
        /// <summary>
        /// Gets the structure/metadata of an entity
        /// </summary>
        public EntityStructure GetEntityStructure(string EntityName, bool refresh)
        {
            try
            {
                Logger.WriteLog($"Getting entity structure: {EntityName}");
                
                if (!refresh)
                {
                    var cacheKey = _cacheHelper.GenerateCacheKey("EntityStructure", EntityName);
                    var cachedStructure = _cacheHelper.Get<EntityStructure>(cacheKey);
                    if (cachedStructure != null)
                    {
                        return cachedStructure;
                    }
                }

                var endpointConfig = _configHelper.GetEndpointConfiguration(EntityName);
                var url = _dataHelper.BuildEndpointUrl(_configHelper.BaseUrl, $"{endpointConfig.GetEndpoint}/schema");
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                
                // Add headers from configuration
                var headers = _configHelper.GetHeaders();
                foreach (var header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }
                
                // Add authentication headers
                _authHelper.AddAuthenticationHeaders(request);
                
                var response = _requestHelper.SendWithRetryAsync(request, $"GetEntityStructure-{EntityName}").Result;

                if (response.IsSuccessStatusCode)
                {
                    var content = response.Content.ReadAsStringAsync().Result;
                    var jsonSamples = new List<string> { content };
                    var entityStructure = _schemaHelper.InferSchemaFromJsonAsync(EntityName, jsonSamples, refresh).Result;
                    
                    return entityStructure;
                }
                else
                {
                    _errorHelper.HandleErrorResponseAsync(response).Wait();
                    return null;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error in GetEntityStructure: {ex.Message}");
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                return null;
            }
        }
        /// <summary>
        /// Gets the structure/metadata of an entity using an existing structure as reference
        /// </summary>
        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            return GetEntityStructure(fnd.EntityName, refresh);
        }
        /// <summary>
        /// Gets the .NET type for an entity
        /// </summary>
        public Type GetEntityType(string EntityName)
        {
            try
            {
                var structure = GetEntityStructure(EntityName, false);
                return structure?.GetType();
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error in GetEntityType: {ex.Message}");
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                return null;
            }
        }
        /// <summary>
        /// Gets foreign key relationships for an entity
        /// </summary>
        public List<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            try
            {
                Logger.WriteLog($"Getting foreign keys for entity: {entityname}");
                // Web APIs may not have traditional foreign key relationships
                // This could be extended to parse API documentation or schema
                return new List<RelationShipKeys>();
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error in GetEntityforeignkeys: {ex.Message}");
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                return new List<RelationShipKeys>();
            }
        }

        /// <summary>
        /// Gets child table relationships for an entity
        /// </summary>
        public List<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            try
            {
                Logger.WriteLog($"Getting child tables for: {tablename}");
                // Web APIs may not have traditional parent-child relationships
                // This could be extended to parse API documentation or schema
                return new List<ChildRelation>();
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error in GetChildTablesList: {ex.Message}");
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                return new List<ChildRelation>();
            }
        }

        /// <summary>
        /// Creates an entity structure
        /// </summary>
        public bool CreateEntityAs(EntityStructure entity)
        {
            try
            {
                Logger.WriteLog($"Creating entity: {entity.EntityName}");
                // For Web APIs, this might involve creating a new endpoint or updating API documentation
                // For now, we'll just validate the entity structure
                if (entity == null || string.IsNullOrEmpty(entity.EntityName))
                {
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = "Invalid entity structure";
                    return false;
                }

                ErrorObject.Flag = Errors.Ok;
                return true;
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error in CreateEntityAs: {ex.Message}");
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                return false;
            }
        }
    }
}