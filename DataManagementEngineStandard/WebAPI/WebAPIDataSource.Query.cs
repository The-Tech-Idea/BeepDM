using System;
using System.ComponentModel;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using System.Net.Http;
using System.Collections.Generic;

namespace TheTechIdea.Beep.WebAPI
{
    /// <summary>
    /// Query &amp; scalar interface stubs.
    /// </summary>
    public partial class WebAPIDataSource
    {
        /// <inheritdoc />
        public virtual IEnumerable<object> RunQuery(string qrystr)
        {
            try
            {
                Logger.WriteLog($"Running query: {qrystr}");
                
                // If qrystr is already a full URL, use it directly, otherwise build it
                string url;
                if (qrystr.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    url = qrystr;
                }
                else
                {
                    var endpointConfig = _configHelper.GetEndpointConfiguration("query");
                    url = _dataHelper.BuildEndpointUrl(_configHelper.BaseUrl, endpointConfig.GetEndpoint);
                    // Append query string if it's not already in the URL
                    if (!string.IsNullOrEmpty(qrystr) && !url.Contains("?"))
                    {
                        url += "?" + qrystr.TrimStart('?');
                    }
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
                
                var response = _requestHelper.SendWithRetryAsync(request, "RunQuery").Result;
                
                if (response.IsSuccessStatusCode)
                {
                    var content = response.Content.ReadAsStringAsync().Result;
                    return _dataHelper.ProcessApiResponse(content, null); // No entity structure for generic query
                }
                else
                {
                    _errorHelper.HandleErrorResponseAsync(response).Wait();
                    return null;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error in RunQuery: {ex.Message}");
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                return null;
            }
        }
        /// <inheritdoc />
        public virtual double GetScalar(string query)
        {
            try
            {
                Logger.WriteLog($"Getting scalar from: {query}");
                
                // If query is already a full URL, use it directly, otherwise build it
                string url;
                if (query.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    url = query;
                }
                else
                {
                    var endpointConfig = _configHelper.GetEndpointConfiguration("scalar");
                    url = _dataHelper.BuildEndpointUrl(_configHelper.BaseUrl, endpointConfig.GetEndpoint);
                    // Append query string if it's not already in the URL
                    if (!string.IsNullOrEmpty(query) && !url.Contains("?"))
                    {
                        url += "?" + query.TrimStart('?');
                    }
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
                
                var response = _requestHelper.SendWithRetryAsync(request, "GetScalar").Result;
                
                if (response.IsSuccessStatusCode)
                {
                    var content = response.Content.ReadAsStringAsync().Result;
                    if (double.TryParse(content.Trim(), out var result))
                    {
                        return result;
                    }
                    else
                    {
                        Logger.WriteLog("Could not parse scalar value from response");
                        return 0.0;
                    }
                }
                else
                {
                    _errorHelper.HandleErrorResponseAsync(response).Wait();
                    return 0.0;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error in GetScalar: {ex.Message}");
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                return 0.0;
            }
        }

        /// <inheritdoc />
        public virtual Task<double> GetScalarAsync(string query)
        {
            return Task.Run(() => GetScalar(query));
        }

        /// <inheritdoc />
        public virtual IErrorsInfo ExecuteSql(string sql)
        {
            Logger.WriteLog("ExecuteSql is not supported for WebAPIDataSource. Use specific entity methods.");
            ErrorObject.Flag = Errors.Failed;
            ErrorObject.Message = "ExecuteSql is not supported. Use Insert/Update/DeleteEntity methods.";
            return ErrorObject;
        }
    }
}