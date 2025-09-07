using System;
using System.Data;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Addin; // PassedArgs
using TheTechIdea.Beep.Report; // IErrorsInfo
using TheTechIdea.Beep.ConfigUtil;
using System.Net.Http;

namespace TheTechIdea.Beep.WebAPI
{
    /// <summary>
    /// Connection and transaction skeleton implementations.
    /// </summary>
    public partial class WebAPIDataSource
    {
        /// <inheritdoc />
        public ConnectionState Openconnection()
        {
            try
            {
                Logger.WriteLog("Opening Web API connection");
                
                // Validate configuration first
                var validationResult = _configHelper.ValidateConfiguration();
                if (!validationResult.IsValid)
                {
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = string.Join("; ", validationResult.Issues);
                    return ConnectionState.Broken;
                }

                // Test connection with a simple request
                var endpointConfig = _configHelper.GetEndpointConfiguration("health");
                var url = _dataHelper.BuildEndpointUrl(_configHelper.BaseUrl, endpointConfig.GetEndpoint);
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                
                // Add headers from configuration
                var headers = _configHelper.GetHeaders();
                foreach (var header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }
                
                // Add authentication headers
                _authHelper.AddAuthenticationHeaders(request);
                
                var response = _requestHelper.SendWithRetryAsync(request, "ConnectionTest").Result;

                if (response.IsSuccessStatusCode)
                {
                    // Ensure authentication is set up
                    _authHelper.EnsureAuthenticatedAsync().Wait();
                    ErrorObject.Flag = Errors.Ok;
                    return ConnectionState.Open;
                }
                else
                {
                    _errorHelper.HandleErrorResponseAsync(response).Wait();
                    return ConnectionState.Broken;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error opening connection: {ex.Message}");
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                return ConnectionState.Broken;
            }
        }

        /// <inheritdoc />
        public ConnectionState Closeconnection()
        {
            try
            {
                Logger.WriteLog("Closing Web API connection");
                
                // No specific logout needed for Web APIs
                ErrorObject.Flag = Errors.Ok;
                return ConnectionState.Closed;
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error closing connection: {ex.Message}");
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                return ConnectionState.Broken;
            }
        }

        /// <inheritdoc />
        public IErrorsInfo BeginTransaction(PassedArgs args)
        {
            try
            {
                Logger.WriteLog("Beginning Web API transaction");
                // Web APIs typically don't support transactions in the traditional sense
                // This could be implemented as starting a batch operation or session
                ErrorObject.Flag = Errors.Ok;
                return ErrorObject;
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error beginning transaction: {ex.Message}");
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                return ErrorObject;
            }
        }

        /// <inheritdoc />
        public IErrorsInfo EndTransaction(PassedArgs args)
        {
            try
            {
                Logger.WriteLog("Ending Web API transaction");
                // End the batch operation or session
                ErrorObject.Flag = Errors.Ok;
                return ErrorObject;
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error ending transaction: {ex.Message}");
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                return ErrorObject;
            }
        }

        /// <inheritdoc />
        public IErrorsInfo Commit(PassedArgs args)
        {
            try
            {
                Logger.WriteLog("Committing Web API transaction");
                // Commit the batch operation
                ErrorObject.Flag = Errors.Ok;
                return ErrorObject;
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error committing transaction: {ex.Message}");
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                return ErrorObject;
            }
        }
    }
}
