using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.WebAPI
{
    /// <summary>
    /// IWebAPIDataSource specific members (skeleton implementation).
    /// </summary>
    public partial class WebAPIDataSource
    {
        /// <summary>
        /// Reads data from the Web API with pagination support
        /// </summary>
        public async Task<IEnumerable<object>> ReadData(bool HeaderExist, int fromline = 0, int toline = 100)
        {
            try
            {
                Logger.WriteLog($"Reading data from Web API, lines {fromline} to {toline}");
                
                // This is a generic read method - in practice, you'd need to specify which entity
                // For now, we'll assume a default entity or return empty list
                var result = new List<object>();
                
                // If no specific entity is set, return empty
                if (string.IsNullOrEmpty(DatasourceName))
                {
                    Logger.WriteLog("No datasource/entity specified for ReadData");
                    return result;
                }

                // Use the first available entity as default
                var entities = GetEntitesList();
                if (entities.Count == 0)
                {
                    Logger.WriteLog("No entities available");
                    return result;
                }

                var defaultEntity = entities[0];
                var endpointConfig = _configHelper.GetEndpointConfiguration(defaultEntity);
                var paginationConfig = _configHelper.GetPaginationConfiguration();
                var url = _dataHelper.BuildEndpointUrl(_configHelper.BaseUrl, endpointConfig.ListEndpoint);
                
                // Add pagination parameters
                var queryParams = new List<string>
                {
                    $"{paginationConfig.OffsetParameterName}={fromline}",
                    $"{paginationConfig.LimitParameterName}={toline - fromline}"
                };
                
                if (queryParams.Count > 0)
                {
                    url += "?" + string.Join("&", queryParams);
                }

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                
                // Add headers from configuration
                var headers = _configHelper.GetHeaders();
                foreach (var header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }
                
                // Add authentication headers
                _authHelper.AddAuthenticationHeaders(request);
                
                var response = await _requestHelper.SendWithRetryAsync(request, $"ReadData-{defaultEntity}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonSerializer.Deserialize<List<object>>(content) ?? new List<object>();
                    result.AddRange(data);
                }
                else
                {
                    await _errorHelper.HandleErrorResponseAsync(response);
                }

                return result;
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error in ReadData: {ex.Message}");
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                return new List<object>();
            }
        }
    }
}