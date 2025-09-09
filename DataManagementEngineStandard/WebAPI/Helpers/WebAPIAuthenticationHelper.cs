using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Threading;
using System.Net.Http.Headers;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.WebAPI.Helpers
{
    /// <summary>
    /// Authentication helper for Web API connections supporting various auth methods
    /// </summary>
    public class WebAPIAuthenticationHelper : IDisposable
    {
        private readonly WebAPIConnectionProperties _connectionProps;
        private readonly IDMLogger _logger;
        private readonly HttpClient _httpClient;
        private string _accessToken;
        private DateTime _tokenExpiry = DateTime.MinValue;
        private readonly object _tokenLock = new object();

        public WebAPIAuthenticationHelper(IConnectionProperties connectionProps, IDMLogger logger, HttpClient httpClient)
        {
            _connectionProps = (WebAPIConnectionProperties)(connectionProps ?? throw new ArgumentNullException(nameof(connectionProps)));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<bool> EnsureAuthenticatedAsync()
        {
            var authType = GetAuthenticationType();
            if (string.IsNullOrEmpty(authType) || authType.Equals("none", StringComparison.OrdinalIgnoreCase))
                return true; // No authentication required

            switch (authType.ToLower())
            {
                case "oauth2":
                case "bearer":
                    return await EnsureBearerTokenAsync();
                case "apikey":
                case "basic":
                    return true; // These are handled per-request
                default:
                    return true;
            }
        }

        public void AddAuthenticationHeaders(HttpRequestMessage request)
        {
            var authType = GetAuthenticationType()?.ToLower();
            
            switch (authType)
            {
                case "bearer":
                case "oauth2":
                    if (!string.IsNullOrEmpty(_accessToken))
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                    break;

                case "apikey":
                    if (!string.IsNullOrEmpty(_connectionProps.ApiKey))
                    {
                        var headerName = GetParameterValue("ApiKeyHeader") ?? "X-API-Key";
                        request.Headers.Add(headerName, _connectionProps.ApiKey);
                    }
                    break;

                case "basic":
                    if (!string.IsNullOrEmpty(_connectionProps.UserID))
                    {
                        var credentials = Convert.ToBase64String(
                            System.Text.Encoding.UTF8.GetBytes($"{_connectionProps.UserID}:{_connectionProps.Password}"));
                        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
                    }
                    break;
            }
        }

        private async Task<bool> EnsureBearerTokenAsync()
        {
            lock (_tokenLock)
            {
                if (DateTime.Now < _tokenExpiry && !string.IsNullOrEmpty(_accessToken))
                    return true; // Token still valid
            }

            return await RefreshTokenAsync();
        }

        private async Task<bool> RefreshTokenAsync()
        {
            try
            {
                var authUrl = GetParameterValue("AuthUrl") ?? GetParameterValue("TokenUrl");
                if (string.IsNullOrEmpty(authUrl))
                {
                    _logger?.WriteLog($"No auth URL configured for OAuth2 authentication");
                    return false;
                }

                var authRequest = new HttpRequestMessage(HttpMethod.Post, authUrl);
                var authPayload = PrepareAuthPayload();
                
                if (!string.IsNullOrEmpty(authPayload))
                {
                    authRequest.Content = new StringContent(authPayload, System.Text.Encoding.UTF8, "application/json");
                }

                var response = await _httpClient.SendAsync(authRequest);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);

                if (tokenResponse != null && tokenResponse.ContainsKey("access_token"))
                {
                    _accessToken = tokenResponse["access_token"].ToString();
                    
                    var expiresIn = tokenResponse.ContainsKey("expires_in") 
                        ? Convert.ToInt32(tokenResponse["expires_in"]) 
                        : 3600;
                    _tokenExpiry = DateTime.Now.AddSeconds(expiresIn - 60); // Refresh 1 minute early

                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Token refresh failed: {ex.Message}");
            }

            return false;
        }

        private string PrepareAuthPayload()
        {
            var grantType = GetParameterValue("GrantType")?.ToLower() ?? "client_credentials";

            switch (grantType)
            {
                case "client_credentials":
                    return JsonSerializer.Serialize(new
                    {
                        grant_type = "client_credentials",
                        client_id = GetParameterValue("ClientId"),
                        client_secret = GetParameterValue("ClientSecret"),
                        scope = GetParameterValue("Scope") ?? ""
                    });

                case "password":
                    return JsonSerializer.Serialize(new
                    {
                        grant_type = "password",
                        username = _connectionProps.UserID,
                        password = _connectionProps.Password,
                        client_id = GetParameterValue("ClientId"),
                        client_secret = GetParameterValue("ClientSecret")
                    });

                case "authorization_code":
                    return JsonSerializer.Serialize(new
                    {
                        grant_type = "authorization_code",
                        code = GetParameterValue("AuthCode"),
                        client_id = GetParameterValue("ClientId"),
                        client_secret = GetParameterValue("ClientSecret"),
                        redirect_uri = GetParameterValue("RedirectUri")
                    });

                default:
                    return string.Empty;
            }
        }

        private string GetAuthenticationType()
        {
            return GetParameterValue("AuthType") ?? "none";
        }

        private string GetParameterValue(string paramName)
        {
            // Parse parameters from the Parameters string or use dedicated properties
            if (!string.IsNullOrEmpty(_connectionProps.Parameters))
            {
                var parameters = ParseParameters(_connectionProps.Parameters);
                return parameters.ContainsKey(paramName) ? parameters[paramName] : null;
            }
            return null;
        }

        private Dictionary<string, string> ParseParameters(string parametersString)
        {
            var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            
            if (string.IsNullOrEmpty(parametersString))
                return parameters;

            var pairs = parametersString.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var pair in pairs)
            {
                var keyValue = pair.Split('=', 2, StringSplitOptions.RemoveEmptyEntries);
                if (keyValue.Length == 2)
                {
                    parameters[keyValue[0].Trim()] = keyValue[1].Trim();
                }
            }

            return parameters;
        }

        public void Dispose()
        {
            // HttpClient is managed externally, so we don't dispose it here
        }
    }
}
