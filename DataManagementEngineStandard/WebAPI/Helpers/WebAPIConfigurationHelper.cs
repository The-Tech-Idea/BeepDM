using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.WebAPI.Helpers
{
    /// <summary>
    /// Configuration management helper for Web API DataSource
    /// Handles connection properties, endpoint configurations, and API-specific settings
    /// </summary>
    public class WebAPIConfigurationHelper : IDisposable
    {
        #region Private Fields

        private readonly IDMLogger _logger;
        private readonly string _datasourceName;
        private readonly WebAPIConnectionProperties _connectionProperties;
        private readonly Dictionary<string, object> _configCache;
        private bool _disposed;

        #endregion

        #region Properties

        /// <summary>Base URL for the API</summary>
        public string BaseUrl => GetConfigValue<string>("BaseUrl", "");

        /// <summary>API version</summary>
        public string ApiVersion => GetConfigValue<string>("ApiVersion", "v1");

        /// <summary>Authentication type</summary>
        public string AuthenticationType => GetConfigValue<string>("AuthenticationType", "None");

        /// <summary>Request timeout in milliseconds</summary>
        public int TimeoutMs => GetConfigValue<int>("TimeoutMs", 30000);

        /// <summary>Maximum retry attempts</summary>
        public int MaxRetries => GetConfigValue<int>("MaxRetries", 3);

        /// <summary>Retry delay in milliseconds</summary>
        public int RetryDelayMs => GetConfigValue<int>("RetryDelayMs", 1000);

        /// <summary>Enable response caching</summary>
        public bool CacheEnabled => GetConfigValue<bool>("CacheEnabled", true);

        /// <summary>Cache duration in minutes</summary>
        public int CacheDurationMinutes => GetConfigValue<int>("CacheDurationMinutes", 15);

        /// <summary>Enable rate limiting</summary>
        public bool RateLimitEnabled => GetConfigValue<bool>("RateLimitEnabled", true);

        /// <summary>Requests per second limit</summary>
        public int RequestsPerSecond => GetConfigValue<int>("RequestsPerSecond", 10);

        /// <summary>Enable pagination</summary>
        public bool PagingEnabled => GetConfigValue<bool>("PagingEnabled", true);

        /// <summary>Default page size</summary>
        public int PageSize => GetConfigValue<int>("PageSize", 100);

        /// <summary>Maximum concurrent requests</summary>
        public int MaxConcurrentRequests => GetConfigValue<int>("MaxConcurrentRequests", 10);

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes the configuration helper
        /// </summary>
        /// <param name="connectionProperties">Connection properties</param>
        /// <param name="logger">Logger instance</param>
        /// <param name="datasourceName">Data source name</param>
        public WebAPIConfigurationHelper(IConnectionProperties connectionProperties, IDMLogger logger, string datasourceName)
        {
            _connectionProperties = (WebAPIConnectionProperties)(connectionProperties ?? throw new ArgumentNullException(nameof(connectionProperties)));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _datasourceName = datasourceName ?? throw new ArgumentNullException(nameof(datasourceName));
            _configCache = new Dictionary<string, object>();

            LoadConfiguration();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets a configuration value with type conversion
        /// </summary>
        /// <typeparam name="T">Type to convert to</typeparam>
        /// <param name="key">Configuration key</param>
        /// <param name="defaultValue">Default value if key not found</param>
        /// <returns>Configuration value</returns>
        public virtual T GetConfigValue<T>(string key, T defaultValue = default)
        {
            if (string.IsNullOrEmpty(key))
                return defaultValue;

            try
            {
                // Check cache first
                if (_configCache.ContainsKey(key))
                {
                    var cachedValue = _configCache[key];
                    if (cachedValue is T directValue)
                        return directValue;
                    
                    return ConvertValue<T>(cachedValue, defaultValue);
                }

                // Check connection properties
                var value = GetFromConnectionProperties(key);
                if (value != null)
                {
                    var convertedValue = ConvertValue<T>(value, defaultValue);
                    _configCache[key] = convertedValue;
                    return convertedValue;
                }

                return defaultValue;
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error getting config value for '{key}': {ex.Message}");
                return defaultValue;
            }
        }

        /// <summary>
        /// Sets a configuration value
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <param name="value">Configuration value</param>
        public virtual void SetConfigValue(string key, object value)
        {
            if (string.IsNullOrEmpty(key))
                return;

            try
            {
                _configCache[key] = value;
                
                // Also update connection properties if possible
                SetToConnectionProperties(key, value);

                _logger?.WriteLog($"Set configuration value '{key}' = '{value}'");
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error setting config value for '{key}': {ex.Message}");
            }
        }

        /// <summary>
        /// Gets HTTP headers from configuration
        /// </summary>
        /// <returns>Dictionary of headers</returns>
        public virtual Dictionary<string, string> GetHeaders()
        {
            var headers = new Dictionary<string, string>();

            try
            {
                // Get headers from connection properties
                var headersValue = GetFromConnectionProperties("Headers");
                if (headersValue != null)
                {
                    if (headersValue is Dictionary<string, string> directHeaders)
                    {
                        return directHeaders;
                    }
                    
                    if (headersValue is string headersJson)
                    {
                        var parsedHeaders = JsonSerializer.Deserialize<Dictionary<string, string>>(headersJson);
                        if (parsedHeaders != null)
                            return parsedHeaders;
                    }
                }

                // Default headers
                headers["User-Agent"] = $"BeepDM-WebAPI/1.0";
                headers["Accept"] = "application/json";

            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error getting headers: {ex.Message}");
            }

            return headers;
        }

        /// <summary>
        /// Gets endpoint configuration for a specific entity
        /// </summary>
        /// <param name="entityName">Entity name</param>
        /// <returns>Endpoint configuration</returns>
        public virtual EndpointConfiguration GetEndpointConfiguration(string entityName)
        {
            var config = new EndpointConfiguration
            {
                EntityName = entityName,
                GetEndpoint = GetConfigValue($"Endpoints.{entityName}.Get", $"/{entityName}"),
                PostEndpoint = GetConfigValue($"Endpoints.{entityName}.Post", $"/{entityName}"),
                PutEndpoint = GetConfigValue($"Endpoints.{entityName}.Put", $"/{entityName}"),
                DeleteEndpoint = GetConfigValue($"Endpoints.{entityName}.Delete", $"/{entityName}"),
                ListEndpoint = GetConfigValue($"Endpoints.{entityName}.List", $"/{entityName}"),
                HttpMethod = GetConfigValue($"Endpoints.{entityName}.Method", "GET"),
                RequiresAuth = GetConfigValue($"Endpoints.{entityName}.RequiresAuth", true),
                CacheDuration = GetConfigValue($"Endpoints.{entityName}.CacheDuration", CacheDurationMinutes),
                RateLimit = GetConfigValue($"Endpoints.{entityName}.RateLimit", RequestsPerSecond)
            };

            return config;
        }

        /// <summary>
        /// Gets authentication configuration from connection properties
        /// </summary>
        /// <returns>The WebAPIConnectionProperties instance with authentication settings</returns>
        public WebAPIConnectionProperties GetAuthenticationConfiguration()
        {
            return _connectionProperties;
        }

        /// <summary>
        /// Gets pagination configuration
        /// </summary>
        /// <returns>Pagination configuration</returns>
        public virtual PaginationConfiguration GetPaginationConfiguration()
        {
            var config = new PaginationConfiguration
            {
                Enabled = PagingEnabled,
                DefaultPageSize = PageSize,
                MaxPageSize = GetConfigValue<int>("Paging.MaxPageSize", 1000),
                PageParameterName = GetConfigValue<string>("Paging.PageParam", "page"),
                SizeParameterName = GetConfigValue<string>("Paging.SizeParam", "size"),
                OffsetParameterName = GetConfigValue<string>("Paging.OffsetParam", "offset"),
                LimitParameterName = GetConfigValue<string>("Paging.LimitParam", "limit"),
                Style = GetConfigValue<string>("Paging.Style", "PageSize") // PageSize, OffsetLimit, Cursor
            };

            return config;
        }

        /// <summary>
        /// Validates the current configuration
        /// </summary>
        /// <returns>Validation result</returns>
        public virtual ConfigurationValidationResult ValidateConfiguration()
        {
            var result = new ConfigurationValidationResult
            {
                IsValid = true
            };

            try
            {
                // Validate base URL
                if (string.IsNullOrWhiteSpace(BaseUrl))
                {
                    result.IsValid = false;
                    result.AddError("BaseUrl", "BaseUrl is required");
                }
                else
                {
                    if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri))
                    {
                        result.IsValid = false;
                        result.AddError("BaseUrl", "BaseUrl is not a valid URL");
                    }
                    else if (uri.Scheme != "https" && uri.Scheme != "http")
                    {
                        result.IsValid = false;
                        result.AddError("BaseUrl", "BaseUrl must use HTTP or HTTPS protocol");
                    }
                }

                // Validate authentication
                var authConfig = GetAuthenticationConfiguration();
                var authValidation = ValidateAuthentication(authConfig);
                foreach (var error in authValidation.Errors)
                {
                    result.AddError("Authentication", error);
                }
                foreach (var warning in authValidation.Warnings)
                {
                    result.AddWarning("Authentication", warning);
                }
                if (!authValidation.IsValid)
                    result.IsValid = false;

                // Validate timeouts
                if (TimeoutMs <= 0)
                {
                    result.AddError("TimeoutMs", "TimeoutMs must be greater than 0");
                    result.IsValid = false;
                }
                else if (TimeoutMs < 1000)
                {
                    result.AddWarning("TimeoutMs", "TimeoutMs is very low, consider increasing for better reliability");
                }

                // Validate retry settings
                if (MaxRetries < 0)
                {
                    result.AddError("MaxRetries", "MaxRetries cannot be negative");
                    result.IsValid = false;
                }

                if (RetryDelayMs <= 0)
                {
                    result.AddError("RetryDelayMs", "RetryDelayMs must be greater than 0");
                    result.IsValid = false;
                }

                // Validate rate limiting
                if (RequestsPerSecond <= 0)
                {
                    result.AddError("RequestsPerSecond", "RequestsPerSecond must be greater than 0");
                    result.IsValid = false;
                }

                // Validate pagination
                if (PageSize <= 0)
                {
                    result.AddError("PageSize", "PageSize must be greater than 0");
                    result.IsValid = false;
                }
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.AddError("Validation", $"Configuration validation error: {ex.Message}");
            }

            return result;
        }

        #endregion

        #region Private Methods

        private void LoadConfiguration()
        {
            try
            {
                // Pre-load common configuration values
                var commonKeys = new[]
                {
                    "BaseUrl", "ApiVersion", "AuthenticationType", "TimeoutMs", "MaxRetries",
                    "RetryDelayMs", "CacheEnabled", "CacheDurationMinutes", "RateLimitEnabled",
                    "RequestsPerSecond", "PagingEnabled", "PageSize", "MaxConcurrentRequests"
                };

                foreach (var key in commonKeys)
                {
                    var value = GetFromConnectionProperties(key);
                    if (value != null)
                    {
                        _configCache[key] = value;
                    }
                }

                _logger?.WriteLog($"Loaded configuration for {_datasourceName}");
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error loading configuration: {ex.Message}");
            }
        }

        private object GetFromConnectionProperties(string key)
        {
            if (_connectionProperties == null)
                return null;

            try
            {
                // Check if it's a WebAPIConnectionProperties with specific properties
                if (_connectionProperties is WebAPIConnectionProperties webApiProps)
                {
                    switch (key.ToLowerInvariant())
                    {
                        case "authtype":
                        case "authenticationtype":
                            return webApiProps.AuthType;
                        case "clientid":
                            return webApiProps.ClientId;
                        case "clientsecret":
                            return webApiProps.ClientSecret;
                        case "apikey":
                            return webApiProps.ApiKey;
                        case "apikeyheader":
                            return webApiProps.ApiKeyHeader;
                        case "tokenurl":
                            return webApiProps.TokenUrl;
                        case "scope":
                            return webApiProps.Scope;
                        case "timeoutms":
                            return webApiProps.TimeoutMs;
                        case "retrycount":
                        case "maxretries":
                            return webApiProps.RetryCount;
                        case "retrydelayms":
                            return webApiProps.RetryDelayMs;
                        case "cacheenabled":
                            return webApiProps.EnableCaching;
                        case "cachedurationminutes":
                        case "cacheexpiryminutes":
                            return webApiProps.CacheExpiryMinutes;
                        case "ratelimitenabled":
                            return webApiProps.EnableRateLimit;
                        case "requestsperminute":
                            return webApiProps.RateLimitRequestsPerMinute;
                        case "requestspersecond":
                            return webApiProps.RateLimitRequestsPerMinute / 60;
                        case "maxconcurrentrequests":
                            return webApiProps.MaxConcurrentRequests;
                        case "pagesize":
                        case "defaultpagesize":
                            return webApiProps.DefaultPageSize;
                        case "maxpagesize":
                            return webApiProps.MaxPageSize;
                        case "pagingenabled":
                            return true; // Always enabled for web APIs
                    }
                }

                // Check parameters string - assume it's serialized JSON or key=value pairs
                if (!string.IsNullOrEmpty(_connectionProperties.Parameters))
                {
                    try
                    {
                        var parametersDict = JsonSerializer.Deserialize<Dictionary<string, object>>(_connectionProperties.Parameters);
                        if (parametersDict != null && parametersDict.ContainsKey(key))
                        {
                            return parametersDict[key];
                        }
                    }
                    catch
                    {
                        // If not JSON, try parsing as key=value pairs
                        var pairs = _connectionProperties.Parameters.Split(';', '&');
                        foreach (var pair in pairs)
                        {
                            var parts = pair.Split('=');
                            if (parts.Length == 2 && parts[0].Trim().Equals(key, StringComparison.OrdinalIgnoreCase))
                            {
                                return parts[1].Trim();
                            }
                        }
                    }
                }

                // Check standard properties
                switch (key.ToLowerInvariant())
                {
                    case "baseurl":
                    case "url":
                        return _connectionProperties.Url;
                    case "username":
                        return _connectionProperties.UserID;
                    case "password":
                        return _connectionProperties.Password;
                    case "host":
                        return _connectionProperties.Host;
                    case "port":
                        return _connectionProperties.Port;
                    case "database":
                    case "schema":
                        return _connectionProperties.SchemaName;
                }
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error getting '{key}' from connection properties: {ex.Message}");
            }

            return null;
        }

        private void SetToConnectionProperties(string key, object value)
        {
            if (_connectionProperties == null)
                return;

            try
            {
                // If it's a WebAPIConnectionProperties, set specific properties
                if (_connectionProperties is WebAPIConnectionProperties webApiProps)
                {
                    switch (key.ToLowerInvariant())
                    {
                        case "authtype":
                        case "authenticationtype":
                            if (Enum.TryParse<AuthTypeEnum>(value?.ToString(), true, out var authType))
                                webApiProps.AuthType = authType;
                            return;
                        case "clientid":
                            webApiProps.ClientId = value?.ToString();
                            return;
                        case "clientsecret":
                            webApiProps.ClientSecret = value?.ToString();
                            return;
                        case "apikey":
                            webApiProps.ApiKey = value?.ToString();
                            return;
                        case "timeoutms":
                            if (int.TryParse(value?.ToString(), out var timeout))
                                webApiProps.TimeoutMs = timeout;
                            return;
                        // Add more specific property mappings as needed
                    }
                }

                // Update parameters string
                if (!string.IsNullOrEmpty(_connectionProperties.Parameters))
                {
                    try
                    {
                        var parametersDict = JsonSerializer.Deserialize<Dictionary<string, object>>(_connectionProperties.Parameters);
                        if (parametersDict == null)
                            parametersDict = new Dictionary<string, object>();
                        
                        parametersDict[key] = value;
                        _connectionProperties.Parameters = JsonSerializer.Serialize(parametersDict);
                    }
                    catch
                    {
                        // Fallback to simple format
                        _connectionProperties.Parameters += $";{key}={value}";
                    }
                }
                else
                {
                    var parametersDict = new Dictionary<string, object> { [key] = value };
                    _connectionProperties.Parameters = JsonSerializer.Serialize(parametersDict);
                }
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error setting '{key}' to connection properties: {ex.Message}");
            }
        }

        private T ConvertValue<T>(object value, T defaultValue)
        {
            if (value == null)
                return defaultValue;

            try
            {
                if (value is T directValue)
                    return directValue;

                var targetType = typeof(T);
                
                // Handle nullable types
                if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    targetType = Nullable.GetUnderlyingType(targetType);
                }

                // Convert value
                return (T)Convert.ChangeType(value, targetType);
            }
            catch
            {
                return defaultValue;
            }
        }

        private ConfigurationValidationResult ValidateAuthentication(WebAPIConnectionProperties authConfig)
        {
            var result = new ConfigurationValidationResult
            {
                IsValid = true
            };

            switch (authConfig.AuthType)
            {
                case AuthTypeEnum.OAuth2:
                    if (string.IsNullOrEmpty(authConfig.ClientId))
                    {
                        result.IsValid = false;
                        result.AddError("ClientId", "OAuth2 requires ClientId");
                    }
                    if (string.IsNullOrEmpty(authConfig.ClientSecret))
                    {
                        result.IsValid = false;
                        result.AddError("ClientSecret", "OAuth2 requires ClientSecret");
                    }
                    if (string.IsNullOrEmpty(authConfig.TokenUrl))
                    {
                        result.IsValid = false;
                        result.AddError("TokenUrl", "OAuth2 requires TokenUrl");
                    }
                    break;

                case AuthTypeEnum.Bearer:
                    // Bearer token will be obtained through OAuth2 or provided directly
                    break;

                case AuthTypeEnum.ApiKey:
                    if (string.IsNullOrEmpty(authConfig.ApiKey))
                    {
                        result.IsValid = false;
                        result.AddError("ApiKey", "API Key authentication requires ApiKey");
                    }
                    break;

                case AuthTypeEnum.Basic:
                    if (string.IsNullOrEmpty(authConfig.UserID))
                    {
                        result.IsValid = false;
                        result.AddError("Username", "Basic authentication requires Username");
                    }
                    if (string.IsNullOrEmpty(authConfig.Password))
                    {
                        result.IsValid = false;
                        result.AddError("Password", "Basic authentication requires Password");
                    }
                    break;

                case AuthTypeEnum.None:
                default:
                    // No authentication - valid
                    break;
            }

            return result;
        }

        private string GetParameterValue(string paramName)
        {
            // Check if it's a WebAPIConnectionProperties with specific properties first
            if (_connectionProperties is WebAPIConnectionProperties webApiProps)
            {
                switch (paramName.ToLowerInvariant())
                {
                    case "refreshurl":
                        return GetFromConnectionProperties("RefreshUrl")?.ToString();
                    case "authurl":
                        return webApiProps.AuthUrl;
                    case "tokenurl":
                        return webApiProps.TokenUrl;
                    case "scope":
                        return webApiProps.Scope;
                    case "granttype":
                        return webApiProps.GrantType;
                    case "redirecturi":
                        return webApiProps.RedirectUri;
                    case "authcode":
                        return webApiProps.AuthCode;
                }
            }

            // Parse parameters from the Parameters string
            if (!string.IsNullOrEmpty(_connectionProperties.Parameters))
            {
                var parameters = ParseParameters(_connectionProperties.Parameters);
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

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes resources used by the configuration helper
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _configCache?.Clear();
                _disposed = true;
            }
        }
        #endregion

      

        #region Configuration Classes

        /// <summary>
        /// Endpoint configuration for specific entities
        /// </summary>
        public class EndpointConfiguration
        {
            /// <summary>Entity name</summary>
            public string EntityName { get; set; }
            
            /// <summary>GET endpoint path</summary>
            public string GetEndpoint { get; set; }
            
            /// <summary>POST endpoint path</summary>
            public string PostEndpoint { get; set; }
            
            /// <summary>PUT endpoint path</summary>
            public string PutEndpoint { get; set; }
            
            /// <summary>DELETE endpoint path</summary>
            public string DeleteEndpoint { get; set; }
            
            /// <summary>LIST endpoint path</summary>
            public string ListEndpoint { get; set; }
            
            /// <summary>HTTP method for queries</summary>
            public string HttpMethod { get; set; }
            
            /// <summary>Whether authentication is required</summary>
            public bool RequiresAuth { get; set; }
            
            /// <summary>Cache duration in minutes</summary>
            public int CacheDuration { get; set; }
            
            /// <summary>Rate limit for this endpoint</summary>
            public int RateLimit { get; set; }
        }

        /// <summary>
        /// Result of configuration validation
        /// </summary>
        public class ConfigurationValidationResult
        {
            /// <summary>Whether configuration is valid</summary>
            public bool IsValid { get; set; }
            
            /// <summary>List of validation errors</summary>
            public List<string> Errors { get; } = new List<string>();
            
            /// <summary>List of validation warnings</summary>
            public List<string> Warnings { get; } = new List<string>();

            /// <summary>Adds an error with a key prefix</summary>
            public void AddError(string key, string message)
            {
                Errors.Add(string.IsNullOrWhiteSpace(key) ? message : $"{key}: {message}");
            }

            /// <summary>Adds a warning with a key prefix</summary>
            public void AddWarning(string key, string message)
            {
                Warnings.Add(string.IsNullOrWhiteSpace(key) ? message : $"{key}: {message}");
            }
        }

        /// <summary>
        /// Pagination configuration
        /// </summary>
        public class PaginationConfiguration
        {
            /// <summary>Whether pagination is enabled</summary>
            public bool Enabled { get; set; }
            
            /// <summary>Default page size</summary>
            public int DefaultPageSize { get; set; }
            
            /// <summary>Maximum page size</summary>
            public int MaxPageSize { get; set; }
            
            /// <summary>Page parameter name</summary>
            public string PageParameterName { get; set; }
            
            /// <summary>Size parameter name</summary>
            public string SizeParameterName { get; set; }
            
            /// <summary>Offset parameter name</summary>
            public string OffsetParameterName { get; set; }
            
            /// <summary>Limit parameter name</summary>
            public string LimitParameterName { get; set; }
            
            /// <summary>Pagination style (PageSize, OffsetLimit, Cursor)</summary>
            public string Style { get; set; }
        }

        #endregion
    }
}
