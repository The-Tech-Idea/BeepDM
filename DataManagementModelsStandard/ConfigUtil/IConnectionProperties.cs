using System.Collections.Generic;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.ConfigUtil
{
    /// <summary>
    /// Defines the properties for a data source connection.
    /// </summary>
    public interface IConnectionProperties
    {
        #region General Properties

        /// <summary>
        /// Gets or sets the unique identifier for the connection.
        /// </summary>
        int ID { get; set; }

        /// <summary>
        /// Gets or sets the GUID for the connection.
        /// </summary>
        string GuidID { get; set; }

        /// <summary>
        /// Gets or sets the user-friendly name for the connection.
        /// </summary>
        string ConnectionName { get; set; }

        /// <summary>
        /// Gets or sets the full connection string.
        /// </summary>
        string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the category of the data source (e.g., RDBMS, NoSQL, File).
        /// </summary>
        DatasourceCategory Category { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this connection is marked as a favorite.
        /// </summary>
        bool Favourite { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this is the default connection.
        /// </summary>
        bool IsDefault { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the connection is represented on a design surface.
        /// </summary>
        bool Drawn { get; set; }

        #endregion

        #region Type and State Flags

        /// <summary>
        /// Gets or sets a value indicating whether the data source is local.
        /// </summary>
        bool IsLocal { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the data source is remote.
        /// </summary>
        bool IsRemote { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the data source is a Web API.
        /// </summary>
        bool IsWebApi { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the data source is a file.
        /// </summary>
        bool IsFile { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the data source is a database.
        /// </summary>
        bool IsDatabase { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the data source is a composite of other data sources.
        /// </summary>
        bool IsComposite { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the data source is cloud-based.
        /// </summary>
        bool IsCloud { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this connection is a favorite.
        /// </summary>
        bool IsFavourite { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the data is stored in-memory.
        /// </summary>
        bool IsInMemory { get; set; }

        #endregion

        #region Database Properties

        /// <summary>
        /// Gets or sets the type of the database (e.g., MySQL, SQLServer, Oracle).
        /// </summary>
        DataSourceType DatabaseType { get; set; }

        /// <summary>
        /// Gets or sets the name of the database.
        /// </summary>
        string Database { get; set; }

        /// <summary>
        /// Gets or sets the list of available databases.
        /// </summary>
        List<string> Databases { get; set; }

        /// <summary>
        /// Gets or sets the schema name.
        /// </summary>
        string SchemaName { get; set; }

        /// <summary>
        /// Gets or sets the Oracle SID or Service Name.
        /// </summary>
        string OracleSIDorService { get; set; }

        #endregion

        #region File Properties

        /// <summary>
        /// Gets or sets the file path for file-based data sources.
        /// </summary>
        string FilePath { get; set; }

        /// <summary>
        /// Gets or sets the file name for file-based data sources.
        /// </summary>
        string FileName { get; set; }

        /// <summary>
        /// Gets or sets the file extension.
        /// </summary>
        string Ext { get; set; }

        /// <summary>
        /// Gets or sets the delimiter character for delimited files.
        /// </summary>
        char Delimiter { get; set; }

        #endregion

        #region Network and Remote Connection Properties

        /// <summary>
        /// Gets or sets the host name or IP address.
        /// </summary>
        string Host { get; set; }

        /// <summary>
        /// Gets or sets the port number.
        /// </summary>
        int Port { get; set; }

        /// <summary>
        /// Gets or sets the URL for web-based services.
        /// </summary>
        string Url { get; set; }

        #endregion

        #region Authentication and Security

        /// <summary>
        /// Gets or sets the user ID for authentication.
        /// </summary>
        string UserID { get; set; }

        /// <summary>
        /// Gets or sets the password for authentication.
        /// </summary>
        string Password { get; set; }

        /// <summary>
        /// Gets or sets the API key for authentication.
        /// </summary>
        string ApiKey { get; set; }

        /// <summary>
        /// Gets or sets a key or token for authentication.
        /// </summary>
        string KeyToken { get; set; }

        /// <summary>
        /// Gets or sets the path to a security certificate.
        /// </summary>
        string CertificatePath { get; set; }

        #endregion

        #region Driver and Parameters

        /// <summary>
        /// Gets or sets the name of the database driver.
        /// </summary>
        string DriverName { get; set; }

        /// <summary>
        /// Gets or sets the version of the database driver.
        /// </summary>
        string DriverVersion { get; set; }

        /// <summary>
        /// Gets or sets additional connection parameters as a string.
        /// </summary>
        string Parameters { get; set; }

        #endregion

        #region Metadata

        /// <summary>
        /// Gets or sets the list of entity structures (e.g., tables, views) in the data source.
        /// </summary>
        List<EntityStructure> Entities { get; set; }

        /// <summary>
        /// Gets or sets default values for data source properties.
        /// </summary>
        List<DefaultValue> DatasourceDefaults { get; set; }

        #endregion

        #region Web API Properties

        /// <summary>
        /// Gets or sets the list of headers for Web API requests.
        /// </summary>
        List<WebApiHeader> Headers { get; set; }

        #endregion

        #region Web API Authentication
        /// <summary>
        /// Gets or sets the Client ID for OAuth2 authentication.
        /// </summary>
        string ClientId { get; set; }
        /// <summary>
        /// Gets or sets the Client Secret for OAuth2 authentication.
        /// </summary>
        string ClientSecret { get; set; }
        /// <summary>
        /// Gets or sets the authentication type for the Web API.
        /// </summary>
        AuthTypeEnum AuthType { get; set; }
        /// <summary>
        /// Gets or sets the authorization URL for OAuth2.
        /// </summary>
        string AuthUrl { get; set; }
        /// <summary>
        /// Gets or sets the token URL for OAuth2.
        /// </summary>
        string TokenUrl { get; set; }
        /// <summary>
        /// Gets or sets the scope for OAuth2.
        /// </summary>
        string Scope { get; set; }
        /// <summary>
        /// Gets or sets the grant type for OAuth2 (e.g., client_credentials).
        /// </summary>
        string GrantType { get; set; }
        /// <summary>
        /// Gets or sets the header name for the API key.
        /// </summary>
        string ApiKeyHeader { get; set; }
        /// <summary>
        /// Gets or sets the redirect URI for OAuth2 authorization code flow.
        /// </summary>
        string RedirectUri { get; set; }
        /// <summary>
        /// Gets or sets the authorization code from the OAuth2 flow.
        /// </summary>
        string AuthCode { get; set; }
        /// <summary>
        /// Gets or sets the request timeout in milliseconds.
        /// </summary>
        int TimeoutMs { get; set; }
        /// <summary>
        /// Gets or sets the maximum number of retries for failed requests.
        /// </summary>
        int MaxRetries { get; set; }
        /// <summary>
        /// Gets or sets the interval between retries in milliseconds.
        /// </summary>
        int RetryIntervalMs { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether to use a proxy.
        /// </summary>
        bool UseProxy { get; set; }
        /// <summary>
        /// Gets or sets the proxy URL.
        /// </summary>
        string ProxyUrl { get; set; }
        /// <summary>
        /// Gets or sets the proxy port.
        /// </summary>
        int ProxyPort { get; set; }
        /// <summary>
        /// Gets or sets the proxy user name.
        /// </summary>
        string ProxyUser { get; set; }
        /// <summary>
        /// Gets or sets the proxy password.
        /// </summary>
        string ProxyPassword { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether to bypass the proxy for local addresses.
        /// </summary>
        bool BypassProxyOnLocal { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether to use default proxy credentials.
        /// </summary>
        bool UseDefaultProxyCredentials { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether to ignore SSL errors.
        /// </summary>
        bool IgnoreSSLErrors { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether to validate the server certificate.
        /// </summary>
        bool ValidateServerCertificate { get; set; }
        /// <summary>
        /// Gets or sets the path to the client certificate.
        /// </summary>
        string ClientCertificatePath { get; set; }
        /// <summary>
        /// Gets or sets the password for the client certificate.
        /// </summary>
        string ClientCertificatePassword { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the API requires authentication.
        /// </summary>
        bool RequiresAuthentication { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the token needs to be refreshed.
        /// </summary>
        bool RequiresTokenRefresh { get; set; }

        #endregion
    }
}