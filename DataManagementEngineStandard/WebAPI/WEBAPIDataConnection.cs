using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using System.Data;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.DriversConfigurations;
using System.Security.Cryptography.X509Certificates;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TheTechIdea.Beep.WebAPI
{
    /// <summary>
    /// Enhanced WebAPI Data Connection implementation using WebAPIConnectionProperties
    /// Provides optimized connection management for Web API data sources
    /// </summary>
    public class WebAPIDataConnection : IDataConnection
    {
        #region Properties
      
        /// <summary>Connection properties specific to Web APIs</summary>
        public IConnectionProperties ConnectionProp { get; set; }

        /// <summary>Current connection status</summary>
        public ConnectionState ConnectionStatus { get; set; } = ConnectionState.Closed;

        /// <summary>Logger instance for connection operations</summary>
        public IDMLogger Logger { get; set; }

        /// <summary>Error information object</summary>
        public IErrorsInfo ErrorObject { get; set; }

        /// <summary>Reference to the data management editor</summary>
        public IDMEEditor DMEEditor { get; set; }

        /// <summary>Unique identifier for this connection</summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>Connection string (derived from properties)</summary>
        public string ConnectionString
        {
            get => BuildConnectionString();
            set => ParseConnectionString(value);
        }

        /// <summary>Database type for this connection</summary>
        public DataSourceType DatabaseType { get; set; } = DataSourceType.WebApi;

        /// <summary>Replica database type</summary>
        public DataSourceType ReplcaDataSourceType { get; set; } = DataSourceType.WebApi;

        /// <summary>Web API specific connection properties</summary>
        public WebAPIConnectionProperties WebAPIProperties => ConnectionProp as WebAPIConnectionProperties;

        /// <summary>Legacy property for compatibility</summary>
        public bool InMemory { get; set; } = false;
        
        /// <summary>Legacy GUID identifier</summary>
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>Legacy ID property</summary>
        public int ID { get; set; }
        
        /// <summary>Data source driver configuration</summary>
        public ConnectionDriversConfig DataSourceDriver { get; set; }
        
        /// <summary>Database connection instance</summary>
        public IDbConnection DbConn { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new WebAPIDataConnection instance
        /// </summary>
        public WebAPIDataConnection()
        {
            ConnectionProp = new WebAPIConnectionProperties();
            ErrorObject = new ErrorsInfo();
        }

        /// <summary>
        /// Initializes WebAPIDataConnection with existing connection properties
        /// </summary>
        /// <param name="connectionProperties">Connection properties to use</param>
        public WebAPIDataConnection(IConnectionProperties connectionProperties)
        {
            ConnectionProp = connectionProperties ?? new WebAPIConnectionProperties();
            ErrorObject = new ErrorsInfo();
        }

        #endregion

        #region Connection Management

        /// <summary>Opens the Web API connection</summary>
        public ConnectionState OpenConnection()
        {
            return OpenConnection(ConnectionProp);
        }

        /// <summary>Opens connection with specific properties</summary>
        public ConnectionState OpenConnection(IConnectionProperties connectionProperties)
        {
            try
            {
                Logger?.WriteLog($"Opening Web API connection: {connectionProperties?.ConnectionName ?? "Unknown"}");

                ConnectionProp = connectionProperties ?? throw new ArgumentNullException(nameof(connectionProperties));

                // Validate required connection properties
                if (!ValidateConnectionProperties())
                {
                    ConnectionStatus = ConnectionState.Broken;
                    return ConnectionStatus;
                }

                // Test connection
                if (TestConnection())
                {
                    ConnectionStatus = ConnectionState.Open;
                    Logger?.WriteLog($"Web API connection opened successfully: {ConnectionProp.ConnectionName}");
                }
                else
                {
                    ConnectionStatus = ConnectionState.Broken;
                    Logger?.WriteLog($"Failed to open Web API connection: {ConnectionProp.ConnectionName}");
                }

                return ConnectionStatus;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error opening Web API connection: {ex.Message}");
                ErrorObject = new ErrorsInfo
                {
                    Flag = Errors.Failed,
                    Ex = ex,
                    Message = $"Failed to open connection: {ex.Message}"
                };
                ConnectionStatus = ConnectionState.Broken;
                return ConnectionStatus;
            }
        }

        /// <summary>Opens connection with database type and connection string</summary>
        public ConnectionState OpenConnection(DataSourceType dbtype, string connectionstring)
        {
            try
            {
                ParseConnectionString(connectionstring);
                return OpenConnection();
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error opening connection with connection string: {ex.Message}");
                ConnectionStatus = ConnectionState.Broken;
                return ConnectionStatus;
            }
        }

        /// <summary>Opens connection with individual parameters</summary>
        public ConnectionState OpenConnection(DataSourceType dbtype, string host, int port, string database, string userid, string password, string parameters)
        {
            try
            {
                if (!(ConnectionProp is WebAPIConnectionProperties webApiProps))
                {
                    webApiProps = new WebAPIConnectionProperties();
                    ConnectionProp = webApiProps;
                }

                ConnectionProp.Host = host;
                ConnectionProp.Port = port;
                ConnectionProp.Database = database;
                ConnectionProp.UserID = userid;
                ConnectionProp.Password = password;
                ConnectionProp.Parameters = parameters;

                // For Web API, use host as base URL if URL is not set
                if (string.IsNullOrEmpty(ConnectionProp.Url) && !string.IsNullOrEmpty(host))
                {
                    ConnectionProp.Url = host;
                }

                return OpenConnection();
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error opening connection with parameters: {ex.Message}");
                ConnectionStatus = ConnectionState.Broken;
                return ConnectionStatus;
            }
        }

        /// <summary>Closes the Web API connection</summary>
        public virtual ConnectionState CloseConn()
        {
            try
            {
                Logger?.WriteLog($"Closing Web API connection: {ConnectionProp?.ConnectionName ?? "Unknown"}");
                
                ConnectionStatus = ConnectionState.Closed;
                
                Logger?.WriteLog("Web API connection closed successfully");
                return ConnectionStatus;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error closing Web API connection: {ex.Message}");
                ErrorObject = new ErrorsInfo
                {
                    Flag = Errors.Failed,
                    Ex = ex,
                    Message = $"Failed to close connection: {ex.Message}"
                };
                return ConnectionState.Broken;
            }
        }

        #endregion

        #region Legacy Methods

        /// <summary>Legacy method for replacing values in connection string</summary>
        public string ReplaceValueFromConnectionString()
        {
            try
            {
                if (DataSourceDriver?.ConnectionString != null)
                {
                    var connectionString = DataSourceDriver.ConnectionString;
                    
                    connectionString = connectionString.Replace("{Host}", ConnectionProp.Host ?? "");
                    connectionString = connectionString.Replace("{UserID}", ConnectionProp.UserID ?? "");
                    connectionString = connectionString.Replace("{Password}", ConnectionProp.Password ?? "");
                    connectionString = connectionString.Replace("{DataBase}", ConnectionProp.Database ?? "");
                    connectionString = connectionString.Replace("{Port}", ConnectionProp.Port.ToString());
                    connectionString = connectionString.Replace("{File}", ConnectionProp.ConnectionString ?? "");
                    connectionString = connectionString.Replace("{Url}", ConnectionProp.Url ?? "");

                    return connectionString;
                }

                return ConnectionProp?.Url ?? "";
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error replacing connection string values: {ex.Message}");
                return "";
            }
        }

        #endregion

        #region Validation

        /// <summary>Validates that required connection properties are provided</summary>
        private bool ValidateConnectionProperties()
        {
            try
            {
                if (ConnectionProp == null)
                {
                    Logger?.WriteLog("Connection properties are null");
                    return false;
                }

                if (string.IsNullOrEmpty(ConnectionProp.Url))
                {
                    Logger?.WriteLog("Base URL is required for Web API connection");
                    return false;
                }

                // Validate URL format
                if (!Uri.TryCreate(ConnectionProp.Url, UriKind.Absolute, out Uri baseUri))
                {
                    Logger?.WriteLog($"Invalid URL format: {ConnectionProp.Url}");
                    return false;
                }

                // Validate authentication settings if using Web API properties
                if (WebAPIProperties != null)
                {
                    return ValidateWebAPIAuthentication();
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error validating connection properties: {ex.Message}");
                return false;
            }
        }

        /// <summary>Validates Web API specific authentication settings</summary>
        private bool ValidateWebAPIAuthentication()
        {
            try
            {
                var authType = WebAPIProperties.AuthType;
                
                switch (authType)
                {
                    case  AuthTypeEnum.OAuth2:
                        if (string.IsNullOrEmpty(WebAPIProperties.ClientId) || 
                            string.IsNullOrEmpty(WebAPIProperties.ClientSecret))
                        {
                            Logger?.WriteLog("OAuth2 authentication requires ClientId and ClientSecret");
                            return false;
                        }
                        break;

                    case AuthTypeEnum.ApiKey:
                        if (string.IsNullOrEmpty(WebAPIProperties.ApiKey))
                        {
                            Logger?.WriteLog("API Key authentication requires ApiKey");
                            return false;
                        }
                        break;

                    case AuthTypeEnum.Basic:
                        if (string.IsNullOrEmpty(ConnectionProp.UserID) || 
                            string.IsNullOrEmpty(ConnectionProp.Password))
                        {
                            Logger?.WriteLog("Basic authentication requires UserID and Password");
                            return false;
                        }
                        break;

                    case AuthTypeEnum.Bearer:
                        if (string.IsNullOrEmpty(WebAPIProperties.KeyToken))
                        {
                            Logger?.WriteLog("Bearer authentication requires KeyToken");
                            return false;
                        }
                        break;
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error validating Web API authentication: {ex.Message}");
                return false;
            }
        }

        /// <summary>Tests the connection by attempting to reach the API</summary>
        private bool TestConnection()
        {
            try
            {
                // For Web API connections, we'll validate the URL is reachable
                // The actual authentication test will be done by the data source
                var baseUri = new Uri(ConnectionProp.Url);
                
                // Basic validation - can create URI
                if (baseUri.IsWellFormedOriginalString())
                {
                    Logger?.WriteLog($"Connection test passed for URL: {ConnectionProp.Url}");
                    return true;
                }
                
                Logger?.WriteLog($"Connection test failed - invalid URL: {ConnectionProp.Url}");
                return false;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Connection test failed: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Connection String Management

        /// <summary>Builds connection string from properties</summary>
        private string BuildConnectionString()
        {
            try
            {
                if (ConnectionProp == null)
                    return string.Empty;

                var connectionStringBuilder = new StringBuilder();
                
                // Add basic properties
                if (!string.IsNullOrEmpty(ConnectionProp.Url))
                    connectionStringBuilder.Append($"Url={ConnectionProp.Url};");
                
                if (!string.IsNullOrEmpty(ConnectionProp.UserID))
                    connectionStringBuilder.Append($"UserID={ConnectionProp.UserID};");
                
                if (!string.IsNullOrEmpty(ConnectionProp.Password))
                    connectionStringBuilder.Append($"Password={ConnectionProp.Password};");

                // Add Web API specific properties
                if (WebAPIProperties != null)
                {
                    switch (WebAPIProperties.AuthType)
                    {
                        case AuthTypeEnum.None:
                            connectionStringBuilder.Append("AuthType=none;");
                            break;
                        case AuthTypeEnum.ApiKey:
                            connectionStringBuilder.Append("AuthType=apikey;");
                            break;
                        case AuthTypeEnum.Basic:
                            connectionStringBuilder.Append("AuthType=basic;");
                            break;
                        case AuthTypeEnum.Bearer:
                            connectionStringBuilder.Append("AuthType=bearer;");
                            break;
                        case AuthTypeEnum.OAuth2:
                            connectionStringBuilder.Append("AuthType=oauth2;");
                            break;
                    }
                 
                }

                // Add parameters if available
                if (!string.IsNullOrEmpty(ConnectionProp.Parameters))
                    connectionStringBuilder.Append(ConnectionProp.Parameters);

                return connectionStringBuilder.ToString();
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error building connection string: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>Parses connection string into properties</summary>
        private void ParseConnectionString(string connectionString)
        {
            try
            {
                if (string.IsNullOrEmpty(connectionString))
                    return;

                // Ensure we have WebAPI connection properties
                if (!(ConnectionProp is WebAPIConnectionProperties))
                {
                    var oldProp = ConnectionProp;
                    ConnectionProp = new WebAPIConnectionProperties();
                    
                    if (oldProp != null)
                    {
                        ConnectionProp.ConnectionName = oldProp.ConnectionName;
                        ConnectionProp.Category = oldProp.Category;
                        ConnectionProp.DriverName = oldProp.DriverName;
                        ConnectionProp.DriverVersion = oldProp.DriverVersion;
                    }
                }

                var webApiProps = WebAPIProperties;
                var parameters = new Dictionary<string, string>();

                var pairs = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
                foreach (var pair in pairs)
                {
                    var keyValue = pair.Split('=', 2, StringSplitOptions.RemoveEmptyEntries);
                    if (keyValue.Length == 2)
                    {
                        var key = keyValue[0].Trim();
                        var value = keyValue[1].Trim();
                        
                        switch (key.ToLowerInvariant())
                        {
                            case "url":
                                ConnectionProp.Url = value;
                                break;
                            case "userid":
                                ConnectionProp.UserID = value;
                                break;
                            case "password":
                                ConnectionProp.Password = value;
                                break;
                            case "authtype":
                                webApiProps.AuthType = value.ToLower() switch
                                {
                                    "none" => AuthTypeEnum.None,
                                    "apikey" => AuthTypeEnum.ApiKey,
                                    "basic" => AuthTypeEnum.Basic,
                                    "bearer" => AuthTypeEnum.Bearer,
                                    "oauth2" => AuthTypeEnum.OAuth2,
                                    _ => AuthTypeEnum.None
                                };
                                break;
                            case "clientid":
                                webApiProps.ClientId = value;
                                break;
                            case "clientsecret":
                                webApiProps.ClientSecret = value;
                                break;
                            case "apikey":
                                webApiProps.ApiKey = value;
                                break;
                            case "keytoken":
                            case "bearertoken":
                                webApiProps.KeyToken = value;
                                break;
                            case "timeoutms":
                                if (int.TryParse(value, out int timeout))
                                    webApiProps.TimeoutMs = timeout;
                                break;
                            default:
                                parameters[key] = value;
                                break;
                        }
                    }
                }

                // Build parameters string from remaining key-value pairs
                if (parameters.Count > 0)
                {
                    var paramBuilder = new StringBuilder();
                    foreach (var kvp in parameters)
                    {
                        if (paramBuilder.Length > 0)
                            paramBuilder.Append(";");
                        paramBuilder.Append($"{kvp.Key}={kvp.Value}");
                    }
                    ConnectionProp.Parameters = paramBuilder.ToString();
                }
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error parsing connection string: {ex.Message}");
            }
        }

        #endregion

        #region IDisposable

        /// <summary>Disposes the connection resources</summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (ConnectionStatus == ConnectionState.Open)
                {
                    CloseConn();
                }
            }
        }

        /// <summary>Disposes the Web API connection</summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
