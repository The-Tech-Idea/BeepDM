using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.WebAPI
{
    /// <summary>
    /// Enhanced connection properties for Web API connections with comprehensive settings.
    /// This class extends IConnectionProperties with Web API-specific configuration options
    /// including authentication, caching, rate limiting, and response handling.
    /// </summary>
    public class WebAPIConnectionProperties : IConnectionProperties
    {
        #region Core Connection Properties

        /// <summary>
        /// Unique identifier for this connection
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// Globally unique identifier for this connection instance
        /// </summary>
        public string GuidID { get; set; } = System.Guid.NewGuid().ToString();

        /// <summary>
        /// Human-readable name for this connection
        /// </summary>
        public string ConnectionName { get; set; }

        /// <summary>
        /// Base URL for the Web API (e.g., "https://api.example.com/v1")
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Database name (may be used for API versioning or endpoint identification)
        /// </summary>
        public string Database { get; set; }

        /// <summary>
        /// Oracle-specific SID or service name (not typically used for Web APIs)
        /// </summary>
        public string OracleSIDorService { get; set; }

        /// <summary>
        /// Type of data source (always WebApi for this class)
        /// </summary>
        public DataSourceType DatabaseType { get; set; } = DataSourceType.WebApi;

        /// <summary>
        /// Category of the data source (always WEBAPI for this class)
        /// </summary>
        public DatasourceCategory Category { get; set; } = DatasourceCategory.WEBAPI;

        /// <summary>
        /// Name of the driver handling this connection
        /// </summary>
        public string DriverName { get; set; }

        /// <summary>
        /// Version of the driver
        /// </summary>
        public string DriverVersion { get; set; }

        /// <summary>
        /// Hostname or IP address of the API server
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// Additional connection parameters as semicolon-separated key-value pairs
        /// </summary>
        public string Parameters { get; set; }

        /// <summary>
        /// Password for basic authentication
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Port number for the API server (typically 80 for HTTP, 443 for HTTPS)
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Schema name (may be used for API versioning)
        /// </summary>
        public string SchemaName { get; set; }

        /// <summary>
        /// Username for basic authentication
        /// </summary>
        public string UserID { get; set; }

        /// <summary>
        /// File path (not typically used for Web APIs)
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// File name (not typically used for Web APIs)
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Extended configuration data (JSON format)
        /// </summary>
        public string Ext { get; set; }

        /// <summary>
        /// Whether this connection has been drawn in the UI
        /// </summary>
        public bool Drawn { get; set; }

        /// <summary>
        /// Path to SSL certificate file
        /// </summary>
        public string CertificatePath { get; set; }

        /// <summary>
        /// Alternative URL (may be used for redirects or fallbacks)
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Security token for authentication
        /// </summary>
        public string KeyToken { get; set; }

        /// <summary>
        /// API key for authentication
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// List of available databases/endpoints
        /// </summary>
        public List<string> Databases { get; set; } = new List<string>();

        /// <summary>
        /// List of entity structures discovered from the API
        /// </summary>
        public List<EntityStructure> Entities { get; set; } = new List<EntityStructure>();

        /// <summary>
        /// Custom HTTP headers to include in requests
        /// </summary>
        public List<WebApiHeader> Headers { get; set; } = new List<WebApiHeader>();

        /// <summary>
        /// Default values for data source parameters
        /// </summary>
        public List<DefaultValue> DatasourceDefaults { get; set; } = new List<DefaultValue>();

        /// <summary>
        /// Field delimiter character (not typically used for Web APIs)
        /// </summary>
        public char Delimiter { get; set; }

        /// <summary>
        /// Whether this connection is marked as favorite
        /// </summary>
        public bool Favourite { get; set; }

        /// <summary>
        /// Whether this is a local connection
        /// </summary>
        public bool IsLocal { get; set; }

        /// <summary>
        /// Whether this is a remote connection (always true for Web APIs)
        /// </summary>
        public bool IsRemote { get; set; } = true;

        /// <summary>
        /// Whether this is a Web API connection (always true for this class)
        /// </summary>
        public bool IsWebApi { get; set; } = true;

        /// <summary>
        /// Whether this is a file-based connection
        /// </summary>
        public bool IsFile { get; set; }

        /// <summary>
        /// Whether this is a database connection
        /// </summary>
        public bool IsDatabase { get; set; }

        /// <summary>
        /// Whether this is a composite connection
        /// </summary>
        public bool IsComposite { get; set; }

        /// <summary>
        /// Whether this is a cloud-based connection
        /// </summary>
        public bool IsCloud { get; set; }

        /// <summary>
        /// Whether this connection is marked as favorite
        /// </summary>
        public bool IsFavourite { get; set; }

        /// <summary>
        /// Whether this is the default connection
        /// </summary>
        public bool IsDefault { get; set; }

        /// <summary>
        /// Whether this is an in-memory connection
        /// </summary>
        public bool IsInMemory { get; set; }

        #endregion

        #region Web API Specific Properties

        /// <summary>
        /// Type of authentication to use
        /// </summary>
        private AuthTypeEnum _authType;
        public AuthTypeEnum AuthType
        {
            get => _authType;
            set
            {
                _authType = value;
                UpdateAuthenticationRequirements();
            }
        }

        /// <summary>
        /// Client ID for OAuth2 authentication
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Client secret for OAuth2 authentication
        /// </summary>
        public string ClientSecret { get; set; }

        /// <summary>
        /// OAuth2 authorization URL
        /// </summary>
        public string AuthUrl { get; set; }

        /// <summary>
        /// OAuth2 token endpoint URL
        /// </summary>
        public string TokenUrl { get; set; }

        /// <summary>
        /// OAuth2 scope parameter
        /// </summary>
        public string Scope { get; set; }

        /// <summary>
        /// OAuth2 grant type (client_credentials, password, authorization_code)
        /// </summary>
        public string GrantType { get; set; } = "client_credentials";

        /// <summary>
        /// HTTP header name for API key authentication (default: "X-API-Key")
        /// </summary>
        public string ApiKeyHeader { get; set; } = "X-API-Key";

        /// <summary>
        /// OAuth2 redirect URI for authorization code flow
        /// </summary>
        public string RedirectUri { get; set; }

        /// <summary>
        /// OAuth2 authorization code for authorization code flow
        /// </summary>
        public string AuthCode { get; set; }

        #endregion

        #region Configuration Settings

        /// <summary>
        /// Request timeout in milliseconds (default: 30000)
        /// </summary>
        public int TimeoutMs { get; set; } = 30000;

        /// <summary>
        /// Maximum number of retry attempts for failed requests (default: 3)
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Delay between retry attempts in milliseconds (default: 1000)
        /// </summary>
        public int RetryIntervalMs { get; set; } = 1000;

        /// <summary>
        /// Whether to use a proxy server
        /// </summary>
        public bool UseProxy { get; set; }

        /// <summary>
        /// Proxy server URL
        /// </summary>
        public string ProxyUrl { get; set; }

        /// <summary>
        /// Proxy server port
        /// </summary>
        public int ProxyPort { get; set; }

        /// <summary>
        /// Username for proxy authentication
        /// </summary>
        public string ProxyUser { get; set; }

        /// <summary>
        /// Password for proxy authentication
        /// </summary>
        public string ProxyPassword { get; set; }

        /// <summary>
        /// Whether to bypass proxy for local addresses
        /// </summary>
        public bool BypassProxyOnLocal { get; set; }

        /// <summary>
        /// Whether to use default proxy credentials
        /// </summary>
        public bool UseDefaultProxyCredentials { get; set; }

        /// <summary>
        /// Whether to ignore SSL certificate errors
        /// </summary>
        public bool IgnoreSSLErrors { get; set; }

        /// <summary>
        /// Whether to validate server SSL certificates
        /// </summary>
        public bool ValidateServerCertificate { get; set; } = true;

        /// <summary>
        /// Path to client certificate file for mutual TLS
        /// </summary>
        public string ClientCertificatePath { get; set; }

        /// <summary>
        /// Password for client certificate
        /// </summary>
        public string ClientCertificatePassword { get; set; }

        /// <summary>
        /// Whether the API requires authentication
        /// </summary>
        public bool RequiresAuthentication { get; set; }

        /// <summary>
        /// Whether tokens need to be automatically refreshed
        /// </summary>
        public bool RequiresTokenRefresh { get; set; }

        /// <summary>
        /// Number of retry attempts for failed requests (default: 3)
        /// </summary>
        public int RetryCount { get; set; } = 3;

        /// <summary>
        /// Delay between retry attempts in milliseconds (default: 1000)
        /// </summary>
        public int RetryDelayMs { get; set; } = 1000;

        /// <summary>
        /// Cache expiry time in minutes (default: 15)
        /// </summary>
        public int CacheExpiryMinutes { get; set; } = 15;

        /// <summary>
        /// Maximum number of concurrent requests (default: 10)
        /// </summary>
        public int MaxConcurrentRequests { get; set; } = 10;

        /// <summary>
        /// Whether to enable response caching (default: true)
        /// </summary>
        public bool EnableCaching { get; set; } = true;

        /// <summary>
        /// Whether to enable HTTP compression (default: true)
        /// </summary>
        public bool EnableCompression { get; set; } = true;

        /// <summary>
        /// User agent string for HTTP requests (default: "BeepDM-WebAPI/1.0")
        /// </summary>
        public string UserAgent { get; set; } = "BeepDM-WebAPI/1.0";

        #endregion

        #region Rate Limiting

        /// <summary>
        /// Maximum number of requests per minute (default: 60)
        /// </summary>
        public int RateLimitRequestsPerMinute { get; set; } = 60;

        /// <summary>
        /// Whether to enable rate limiting (default: true)
        /// </summary>
        public bool EnableRateLimit { get; set; } = true;

        #endregion

        #region Response Handling

        /// <summary>
        /// Expected response format: "json", "xml", "csv", "text" (default: "json")
        /// </summary>
        public string ResponseFormat { get; set; } = "json";

        /// <summary>
        /// JSONPath or XPath expression to extract data from response
        /// </summary>
        public string DataPath { get; set; }

        /// <summary>
        /// JSONPath or XPath expression to extract total count from response
        /// </summary>
        public string TotalCountPath { get; set; }

        #endregion

        #region Pagination Settings

        /// <summary>
        /// Query parameter name for page number (default: "page")
        /// </summary>
        public string PageNumberParameter { get; set; } = "page";

        /// <summary>
        /// Query parameter name for page size (default: "limit")
        /// </summary>
        public string PageSizeParameter { get; set; } = "limit";

        /// <summary>
        /// Default page size for requests (default: 100)
        /// </summary>
        public int DefaultPageSize { get; set; } = 100;

        /// <summary>
        /// Maximum allowed page size (default: 1000)
        /// </summary>
        public int MaxPageSize { get; set; } = 1000;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of WebAPIConnectionProperties with default values
        /// </summary>
        public WebAPIConnectionProperties()
        {
            // Set default headers
            Headers.Add(new WebApiHeader { Headername = "Accept", Headervalue = "application/json" });
            Headers.Add(new WebApiHeader { Headername = "User-Agent", Headervalue = UserAgent });

            // Set authentication requirements based on AuthType
            UpdateAuthenticationRequirements();
        }

        #endregion

        #region Parameter Management Methods

        /// <summary>
        /// Gets a connection parameter value by name from the Parameters string
        /// </summary>
        /// <param name="paramName">Name of the parameter to retrieve</param>
        /// <returns>Parameter value if found, null otherwise</returns>
        public string GetParameterValue(string paramName)
        {
            if (string.IsNullOrEmpty(Parameters))
                return null;

            var parameters = ParseParameters(Parameters);
            return parameters.ContainsKey(paramName) ? parameters[paramName] : null;
        }

        /// <summary>
        /// Sets a connection parameter value in the Parameters string
        /// </summary>
        /// <param name="paramName">Name of the parameter to set</param>
        /// <param name="value">Value to set for the parameter</param>
        public void SetParameterValue(string paramName, string value)
        {
            var parameters = ParseParameters(Parameters ?? string.Empty);
            parameters[paramName] = value;
            Parameters = string.Join(";", parameters.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        }

        /// <summary>
        /// Parses the Parameters string into a dictionary
        /// </summary>
        /// <param name="parametersString">Semicolon-separated key-value pairs</param>
        /// <returns>Dictionary containing the parsed parameters</returns>
        private Dictionary<string, string> ParseParameters(string parametersString)
        {
            var parameters = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrEmpty(parametersString))
                return parameters;

            var pairs = parametersString.Split(';', System.StringSplitOptions.RemoveEmptyEntries);
            foreach (var pair in pairs)
            {
                var keyValue = pair.Split('=', 2, System.StringSplitOptions.RemoveEmptyEntries);
                if (keyValue.Length == 2)
                {
                    parameters[keyValue[0].Trim()] = keyValue[1].Trim();
                }
            }

            return parameters;
        }

        #endregion

        #region Validation Methods

        /// <summary>
        /// Updates authentication requirement flags based on the current AuthType
        /// </summary>
        private void UpdateAuthenticationRequirements()
        {
            RequiresAuthentication = AuthType != AuthTypeEnum.None;
            RequiresTokenRefresh = AuthType == AuthTypeEnum.Bearer || AuthType == AuthTypeEnum.OAuth2;
        }

        /// <summary>
        /// Validates that all required properties for the current AuthType are set
        /// </summary>
        /// <returns>True if validation passes, false otherwise</returns>
        public bool ValidateConfiguration()
        {
            if (string.IsNullOrEmpty(ConnectionString))
                return false;

            switch (AuthType)
            {
                case AuthTypeEnum.ApiKey:
                    return !string.IsNullOrEmpty(ApiKey);

                case AuthTypeEnum.Basic:
                    return !string.IsNullOrEmpty(UserID) && !string.IsNullOrEmpty(Password);

                case AuthTypeEnum.Bearer:
                case AuthTypeEnum.OAuth2:
                    return !string.IsNullOrEmpty(ClientId) && !string.IsNullOrEmpty(ClientSecret) &&
                           !string.IsNullOrEmpty(TokenUrl);

                case AuthTypeEnum.None:
                default:
                    return true;
            }
        }

        /// <summary>
        /// Gets a description of missing required configuration for the current AuthType
        /// </summary>
        /// <returns>Description of missing configuration, or empty string if valid</returns>
        public string GetValidationErrors()
        {
            var errors = new List<string>();

            if (string.IsNullOrEmpty(ConnectionString))
                errors.Add("ConnectionString is required");

            switch (AuthType)
            {
                case AuthTypeEnum.ApiKey:
                    if (string.IsNullOrEmpty(ApiKey))
                        errors.Add("ApiKey is required for ApiKey authentication");
                    break;

                case AuthTypeEnum.Basic:
                    if (string.IsNullOrEmpty(UserID))
                        errors.Add("UserID is required for Basic authentication");
                    if (string.IsNullOrEmpty(Password))
                        errors.Add("Password is required for Basic authentication");
                    break;

                case AuthTypeEnum.Bearer:
                case AuthTypeEnum.OAuth2:
                    if (string.IsNullOrEmpty(ClientId))
                        errors.Add("ClientId is required for OAuth2 authentication");
                    if (string.IsNullOrEmpty(ClientSecret))
                        errors.Add("ClientSecret is required for OAuth2 authentication");
                    if (string.IsNullOrEmpty(TokenUrl))
                        errors.Add("TokenUrl is required for OAuth2 authentication");
                    break;
            }

            return string.Join("; ", errors);
        }

        #endregion
    }
}
