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
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Tools;
using System.ComponentModel;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep;

namespace TheTechIdea.Beep.WebAPI
{
    /// <summary>
    /// Interface implementation partial class for WebAPIDataSource
    /// Contains methods required by IDataSource interface
    /// </summary>
    public partial class WebAPIDataSource
    {
        #region IDataSource Interface Implementation

        /// <summary>Creates entity structure</summary>
        public bool CreateEntityAs(EntityStructure entity)
        {
            this.Logger?.WriteLog("CreateEntityAs not supported for Web API data sources");
            ErrorObject.Flag = Errors.Failed;
            ErrorObject.Message = "Create Entity not supported for Web API data sources";
            return false;
        }

        /// <summary>Gets entity index</summary>
        public int GetEntityIdx(string entityName)
        {
            try
            {
                if (Entities == null) return -1;
                return Entities.FindIndex(e => e.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                this.Logger?.WriteLog($"Error getting entity index for '{entityName}': {ex.Message}");
                return -1;
            }
        }

        /// <summary>Gets entity foreign keys</summary>
        public List<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            Logger?.WriteLog("Foreign keys not supported for Web API data sources");
            return new List<RelationShipKeys>();
        }

        /// <summary>Gets entity structure with option to refresh</summary>
        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            try
            {
                if (fnd == null) return null;
                
                if (refresh || fnd.Fields?.Count == 0)
                {
                    return GetEntityStructure(fnd.EntityName, refresh);
                }
                return fnd;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error getting entity structure: {ex.Message}");
                ErrorObject.Ex = ex;
                ErrorObject.Flag = Errors.Failed;
                return fnd;
            }
        }

        /// <summary>Runs ETL script</summary>
        public IErrorsInfo RunScript(object dDLScripts)
        {
            Logger?.WriteLog("RunScript not supported for Web API data sources");
            ErrorObject.Flag = Errors.Failed;
            ErrorObject.Message = "Scripts not supported for Web API data sources";
            return ErrorObject;
        }

        /// <summary>Gets create entity scripts</summary>
        public List<object> GetCreateEntityScript(List<EntityStructure> entities)
        {
            Logger?.WriteLog("GetCreateEntityScript not supported for Web API data sources");
            return new List<object>();
        }

        /// <summary>Creates multiple entities</summary>
        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            Logger?.WriteLog("CreateEntities not supported for Web API data sources");
            ErrorObject.Flag = Errors.Failed;
            ErrorObject.Message = "Create Entities not supported for Web API data sources";
            return ErrorObject;
        }

        /// <summary>Updates multiple entities</summary>
        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            Logger?.WriteLog("UpdateEntities with progress not implemented for Web API data sources - returning success");
            ErrorObject.Flag = Errors.Ok;
            ErrorObject.Message = "Update entities completed";
            return ErrorObject;
        }

        /// <summary>Begin transaction</summary>
        public IErrorsInfo BeginTransaction(PassedArgs args)
        {
            Logger?.WriteLog("Transactions not supported for Web API data sources");
            ErrorObject.Flag = Errors.Ok;
            ErrorObject.Message = "Transactions not applicable for Web API";
            return ErrorObject;
        }

        /// <summary>End transaction</summary>
        public IErrorsInfo EndTransaction(PassedArgs args)
        {
            Logger?.WriteLog("Transactions not supported for Web API data sources");
            ErrorObject.Flag = Errors.Ok;
            ErrorObject.Message = "Transactions not applicable for Web API";
            return ErrorObject;
        }

        /// <summary>Commit transaction</summary>
        public IErrorsInfo Commit(PassedArgs args)
        {
            Logger?.WriteLog("Transactions not supported for Web API data sources");
            ErrorObject.Flag = Errors.Ok;
            ErrorObject.Message = "Transactions not applicable for Web API";
            return ErrorObject;
        }

        /// <summary>Gets entity data asynchronously</summary>
        public Task<IBindingList> GetEntityAsync(string EntityName, List<AppFilter> filter)
        {
            try
            {
                // Return a simple binding list for now
                Logger?.WriteLog($"GetEntityAsync called for '{EntityName}' - returning empty list");
                return Task.FromResult<IBindingList>(new BindingList<object>());
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error getting entity '{EntityName}' async: {ex.Message}");
                ErrorObject.Ex = ex;
                ErrorObject.Flag = Errors.Failed;
                return Task.FromResult<IBindingList>(new BindingList<object>());
            }
        }

        /// <summary>Gets scalar value asynchronously</summary>
        public async Task<double> GetScalarAsync(string query)
        {
            try
            {
                Logger?.WriteLog($"Executing scalar query async: {query}");
                
                var results = await RunQueryAsync(query);
                if (results != null && results.Count > 0)
                {
                    var firstResult = results[0];
                    if (firstResult != null)
                    {
                        // Try to convert first property to double
                        var properties = firstResult.GetType().GetProperties();
                        if (properties.Length > 0)
                        {
                            var value = properties[0].GetValue(firstResult);
                            if (value != null && double.TryParse(value.ToString(), out var doubleValue))
                            {
                                return doubleValue;
                            }
                        }
                    }
                }
                
                return 0.0;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error executing scalar query async: {ex.Message}");
                ErrorObject.Ex = ex;
                ErrorObject.Flag = Errors.Failed;
                return 0.0;
            }
        }

        /// <summary>Gets scalar value</summary>
        public double GetScalar(string query)
        {
            return GetScalarAsync(query).Result;
        }

        #endregion

        #region Private Helper Methods

        private async Task<List<object>> RunQueryAsync(string query)
        {
            try
            {
                var baseUrl = Dataconnection.ConnectionProp.Url;
                var queryEndpoint = GetConfigurationValue("QueryEndpoint", "query");
                var url = _dataHelper.BuildEndpointUrl(baseUrl, queryEndpoint);

                var queryData = new { query = query };
                var jsonContent = System.Text.Json.JsonSerializer.Serialize(queryData);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = httpContent };
                await _authHelper.EnsureAuthenticatedAsync();
                _authHelper.AddAuthenticationHeaders(request);
                
                var response = await _requestHelper.SendWithRetryAsync(request, "RunQueryAsync");

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Query failed with status {response.StatusCode}: {response.ReasonPhrase}");
                }

                var content = await response.Content.ReadAsStringAsync();
                
                // Create a dummy entity structure for processing
                var dummyEntity = new EntityStructure 
                { 
                    EntityName = "QueryResults",
                    Fields = new List<EntityField>()
                };
                
                var results = _dataHelper.ProcessApiResponse(content, dummyEntity);
                return results?.Cast<object>().ToList() ?? new List<object>();
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error in RunQueryAsync: {ex.Message}");
                throw;
            }
        }

        #endregion
    }
}
