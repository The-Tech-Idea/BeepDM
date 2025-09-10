using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Report;
using System.Net.Http;
using System.Text.Json;

namespace TheTechIdea.Beep.WebAPI
{
    /// <summary>
    /// Data retrieval &amp; CRUD operations using helpers.
    /// </summary>
    public partial class WebAPIDataSource
    {
        /// <inheritdoc />
        public IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
        {
            try
            {
                Logger.WriteLog($"Getting entity: {EntityName}");
                var entity = GetEntityStructure(EntityName, false);
                if (entity == null)
                {
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = $"Entity {EntityName} not found";
                    return null;
                }

                var endpointConfig = _configHelper.GetEndpointConfiguration(EntityName);
                var url = _dataHelper.BuildEndpointUrl(_configHelper.BaseUrl, endpointConfig.GetEndpoint, filter);
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
                
                var response = _requestHelper.SendWithRetryAsync(request, $"GetEntity-{EntityName}").Result;

                if (response.IsSuccessStatusCode)
                {
                    var content = response.Content.ReadAsStringAsync().Result;
                    return _dataHelper.ProcessApiResponse(content, entity);
                }
                else
                {
                    _errorHelper.HandleErrorResponseAsync(response).Wait();
                    return null;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error in GetEntity: {ex.Message}");
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                return null;
            }
        }

        /// <inheritdoc />
        public PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            try
            {
                Logger.WriteLog($"Getting paged entity: {EntityName}, page {pageNumber}, size {pageSize}");
                var entity = GetEntityStructure(EntityName, false);
                if (entity == null)
                {
                    return new PagedResult { Data = null, TotalRecords = 0, PageNumber = pageNumber, PageSize = pageSize };
                }

                var endpointConfig = _configHelper.GetEndpointConfiguration(EntityName);
                var paginationConfig = _configHelper.GetPaginationConfiguration();
                var url = _dataHelper.BuildEndpointUrl(_configHelper.BaseUrl, endpointConfig.ListEndpoint, filter, pageNumber, pageSize);
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                
                // Add headers from configuration
                var headers = _configHelper.GetHeaders();
                foreach (var header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }
                
                // Add authentication headers
                _authHelper.AddAuthenticationHeaders(request);
                
                var response = _requestHelper.SendWithRetryAsync(request, $"GetEntityPaged-{EntityName}").Result;

                if (response.IsSuccessStatusCode)
                {
                    var content = response.Content.ReadAsStringAsync().Result;
                    var data = _dataHelper.ProcessApiResponse(content, entity);
                    var total = _dataHelper.GetTotalRecordsFromResponse(content, response.Headers);
                    return new PagedResult { Data = data, TotalRecords = total, PageNumber = pageNumber, PageSize = pageSize };
                }
                else
                {
                    _errorHelper.HandleErrorResponseAsync(response).Wait();
                    return new PagedResult { Data = null, TotalRecords = 0, PageNumber = pageNumber, PageSize = pageSize };
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error in GetEntity (paged): {ex.Message}");
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                return new PagedResult { Data = null, TotalRecords = 0, PageNumber = pageNumber, PageSize = pageSize };
            }
        }

        /// <inheritdoc />
        public Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            return Task.Run(() => GetEntity(EntityName, Filter));
        }

        /// <inheritdoc />
        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            try
            {
                Logger.WriteLog($"Inserting entity: {EntityName}");
                var entity = GetEntityStructure(EntityName, false);
                if (entity == null)
                {
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = $"Entity {EntityName} not found";
                    return ErrorObject;
                }

                var endpointConfig = _configHelper.GetEndpointConfiguration(EntityName);
                var url = _dataHelper.BuildEndpointUrl(_configHelper.BaseUrl, endpointConfig.PostEndpoint);
                var json = JsonSerializer.Serialize(InsertedData);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
                
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
                
                var response = _requestHelper.SendWithRetryAsync(request, $"InsertEntity-{EntityName}").Result;

                if (response.IsSuccessStatusCode)
                {
                    ErrorObject.Flag = Errors.Ok;
                    return ErrorObject;
                }
                else
                {
                    _errorHelper.HandleErrorResponseAsync(response).Wait();
                    return ErrorObject;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error in InsertEntity: {ex.Message}");
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                return ErrorObject;
            }
        }

        /// <inheritdoc />
        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            try
            {
                Logger.WriteLog($"Updating entity: {EntityName}");
                var entity = GetEntityStructure(EntityName, false);
                if (entity == null)
                {
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = $"Entity {EntityName} not found";
                    return ErrorObject;
                }

                var endpointConfig = _configHelper.GetEndpointConfiguration(EntityName);
                var url = _dataHelper.BuildEndpointUrl(_configHelper.BaseUrl, endpointConfig.PutEndpoint);
                var json = JsonSerializer.Serialize(UploadDataRow);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var request = new HttpRequestMessage(HttpMethod.Put, url) { Content = content };
                
                // Add headers from configuration
                var headers = _configHelper.GetHeaders();
                foreach (var header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }
                
                // Add authentication headers
                _authHelper.AddAuthenticationHeaders(request);
                
                var response = _requestHelper.SendWithRetryAsync(request, $"UpdateEntity-{EntityName}").Result;

                if (response.IsSuccessStatusCode)
                {
                    ErrorObject.Flag = Errors.Ok;
                    return ErrorObject;
                }
                else
                {
                    _errorHelper.HandleErrorResponseAsync(response).Wait();
                    return ErrorObject;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error in UpdateEntity: {ex.Message}");
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                return ErrorObject;
            }
        }

        /// <inheritdoc />
        public IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow)
        {
            try
            {
                Logger.WriteLog($"Deleting entity: {EntityName}");
                var entity = GetEntityStructure(EntityName, false);
                if (entity == null)
                {
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = $"Entity {EntityName} not found";
                    return ErrorObject;
                }

                var endpointConfig = _configHelper.GetEndpointConfiguration(EntityName);
                var url = _dataHelper.BuildEndpointUrl(_configHelper.BaseUrl, endpointConfig.DeleteEndpoint);
                var request = new HttpRequestMessage(HttpMethod.Delete, url);
                
                // Add headers from configuration
                var headers = _configHelper.GetHeaders();
                foreach (var header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }
                
                // Add authentication headers
                _authHelper.AddAuthenticationHeaders(request);
                
                var response = _requestHelper.SendWithRetryAsync(request, $"DeleteEntity-{EntityName}").Result;

                if (response.IsSuccessStatusCode)
                {
                    ErrorObject.Flag = Errors.Ok;
                    return ErrorObject;
                }
                else
                {
                    _errorHelper.HandleErrorResponseAsync(response).Wait();
                    return ErrorObject;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error in DeleteEntity: {ex.Message}");
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                return ErrorObject;
            }
        }

        /// <inheritdoc />
        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            // For bulk update, iterate through the data and update each item
            try
            {
                Logger.WriteLog($"Bulk updating entities: {EntityName}");
                var entity = GetEntityStructure(EntityName, false);
                if (entity == null)
                {
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = $"Entity {EntityName} not found";
                    return ErrorObject;
                }

                if (UploadData is IEnumerable<object> items)
                {
                    int count = 0;
                    foreach (var item in items)
                    {
                        var result = UpdateEntity(EntityName, item);
                        if (result.Flag == Errors.Failed)
                        {
                            return result;
                        }
                        count++;
                        progress?.Report(new PassedArgs { ParameterInt1 = count });
                    }
                    ErrorObject.Flag = Errors.Ok;
                    return ErrorObject;
                }
                else
                {
                    return UpdateEntity(EntityName, UploadData);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error in UpdateEntities: {ex.Message}");
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                return ErrorObject;
            }
        }
    }
}