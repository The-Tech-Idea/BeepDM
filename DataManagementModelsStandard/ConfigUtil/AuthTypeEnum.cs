using System;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.ConfigUtil
{
    /// <summary>
    /// Comprehensive enumeration of authentication types for all connection types
    /// (databases, Web APIs, file systems, cloud services, etc.)
    /// Expanded from Web API-only to support all data source types
    /// </summary>
    public enum AuthTypeEnum
    {
        /// <summary>
        /// No authentication required
        /// </summary>
        None = 0,

        /// <summary>
        /// API Key authentication using custom header (default: X-API-Key)
        /// Requires ApiKey property to be set
        /// </summary>
        ApiKey = 1,

        /// <summary>
        /// Basic HTTP authentication using username and password
        /// Requires UserID and Password properties to be set
        /// </summary>
        Basic = 2,

        /// <summary>
        /// Bearer token authentication
        /// Can use static token or OAuth2 flow depending on configuration
        /// </summary>
        Bearer = 3,

        /// <summary>
        /// OAuth2 authentication with automatic token management
        /// Supports client_credentials, password, and authorization_code grant types
        /// Requires TokenUrl, ClientId, and ClientSecret (or other credentials based on grant type)
        /// </summary>
        OAuth2 = 4,

        /// <summary>
        /// Username and password authentication (database standard)
        /// Requires UserID and Password properties
        /// </summary>
        UserPassword = 5,

        /// <summary>
        /// Windows Integrated Security (NTLM/Negotiate)
        /// Uses current Windows user credentials
        /// SQL Server: Set IntegratedSecurity=true or TrustedConnection=true
        /// </summary>
        Windows = 6,

        /// <summary>
        /// Kerberos authentication
        /// Requires KerberosServiceName, KerberosRealm, and optionally KerberosKdc
        /// Used for distributed authentication in enterprise environments
        /// </summary>
        Kerberos = 7,

        /// <summary>
        /// Certificate-based authentication
        /// Requires CertificatePath or ClientCertificatePath
        /// Optional: ClientCertificatePassword for encrypted certificates
        /// </summary>
        Certificate = 8,

        /// <summary>
        /// Azure Active Directory (AAD/Entra ID) authentication
        /// Requires TenantId, ApplicationId, and optionally ClientId/ClientSecret
        /// Used for Azure SQL, Cosmos DB, and other Azure services
        /// </summary>
        AzureActiveDirectory = 9,

        /// <summary>
        /// LDAP (Lightweight Directory Access Protocol) authentication
        /// Requires UserID, Password, and Domain
        /// Used for directory services authentication
        /// </summary>
        LDAP = 10,

        /// <summary>
        /// SAML (Security Assertion Markup Language) authentication
        /// Requires AuthUrl and other SAML-specific parameters
        /// Used for single sign-on (SSO) scenarios
        /// </summary>
        SAML = 11,

        /// <summary>
        /// JWT (JSON Web Token) authentication
        /// Requires a pre-obtained JWT token in ApiKey or KeyToken property
        /// </summary>
        JWT = 12,

        /// <summary>
        /// Digest authentication (RFC 2617)
        /// Requires UserID and Password properties
        /// More secure than Basic authentication
        /// </summary>
        Digest = 13,

        /// <summary>
        /// Trusted connection (SQL Server specific)
        /// Uses Windows authentication without explicit credentials
        /// Same as Windows but specifically for SQL Server
        /// </summary>
        Trusted = 14,

        /// <summary>
        /// Custom authentication implementation
        /// Uses additional parameters from ParameterList
        /// Allows for provider-specific authentication mechanisms
        /// </summary>
        Custom = 99
    }

    /// <summary>
    /// Extension methods for AuthTypeEnum
    /// </summary>
    public static class AuthTypeEnumExtensions
    {
        /// <summary>
        /// Converts AuthTypeEnum to string representation
        /// </summary>
        /// <param name="authType">The authentication type</param>
        /// <returns>String representation for the authentication helper</returns>
        public static string ToStringValue(this AuthTypeEnum authType)
        {
            return authType switch
            {
                AuthTypeEnum.None => "none",
                AuthTypeEnum.ApiKey => "apikey",
                AuthTypeEnum.Basic => "basic",
                AuthTypeEnum.Bearer => "bearer",
                AuthTypeEnum.OAuth2 => "oauth2",
                AuthTypeEnum.UserPassword => "userpassword",
                AuthTypeEnum.Windows => "windows",
                AuthTypeEnum.Kerberos => "kerberos",
                AuthTypeEnum.Certificate => "certificate",
                AuthTypeEnum.AzureActiveDirectory => "azuread",
                AuthTypeEnum.LDAP => "ldap",
                AuthTypeEnum.SAML => "saml",
                AuthTypeEnum.JWT => "jwt",
                AuthTypeEnum.Digest => "digest",
                AuthTypeEnum.Trusted => "trusted",
                AuthTypeEnum.Custom => "custom",
                _ => "none"
            };
        }

        /// <summary>
        /// Parses string value to AuthTypeEnum
        /// </summary>
        /// <param name="authTypeString">String representation of auth type</param>
        /// <returns>Corresponding AuthTypeEnum value</returns>
        public static AuthTypeEnum FromString(string authTypeString)
        {
            if (string.IsNullOrWhiteSpace(authTypeString))
                return AuthTypeEnum.None;

            return authTypeString.Trim().ToLowerInvariant() switch
            {
                "none" => AuthTypeEnum.None,
                "apikey" or "api-key" or "api_key" => AuthTypeEnum.ApiKey,
                "basic" => AuthTypeEnum.Basic,
                "bearer" => AuthTypeEnum.Bearer,
                "oauth2" or "oauth" => AuthTypeEnum.OAuth2,
                "userpassword" or "user" or "password" or "username" => AuthTypeEnum.UserPassword,
                "windows" or "ntlm" or "integrated" or "integratedsecurity" => AuthTypeEnum.Windows,
                "kerberos" => AuthTypeEnum.Kerberos,
                "certificate" or "cert" => AuthTypeEnum.Certificate,
                "azuread" or "aad" or "azureactivedirectory" or "azure" or "entra" or "entraid" => AuthTypeEnum.AzureActiveDirectory,
                "ldap" => AuthTypeEnum.LDAP,
                "saml" => AuthTypeEnum.SAML,
                "jwt" => AuthTypeEnum.JWT,
                "digest" => AuthTypeEnum.Digest,
                "trusted" or "trustedconnection" => AuthTypeEnum.Trusted,
                "custom" => AuthTypeEnum.Custom,
                _ => AuthTypeEnum.None
            };
        }

        /// <summary>
        /// Gets a description of what credentials/properties are required for each auth type
        /// </summary>
        /// <param name="authType">The authentication type</param>
        /// <returns>Description of required credentials</returns>
        public static string GetRequiredCredentials(this AuthTypeEnum authType)
        {
            return authType switch
            {
                AuthTypeEnum.None => "No credentials required",
                AuthTypeEnum.ApiKey => "ApiKey property required. Optionally set ApiKeyHeader parameter (default: X-API-Key)",
                AuthTypeEnum.Basic => "UserID and Password properties required (Base64 encoded for HTTP)",
                AuthTypeEnum.Bearer => "Either ApiKey for static token, or OAuth2 parameters for dynamic token",
                AuthTypeEnum.OAuth2 => "TokenUrl, ClientId, ClientSecret required. Optional: Scope, GrantType, RedirectUri, AuthCode",
                AuthTypeEnum.UserPassword => "UserID and Password properties required",
                AuthTypeEnum.Windows => "Uses current Windows credentials. Set IntegratedSecurity=true or TrustedConnection=true",
                AuthTypeEnum.Kerberos => "KerberosServiceName, KerberosRealm required. Optional: KerberosKdc, KerberosConfigPath",
                AuthTypeEnum.Certificate => "CertificatePath or ClientCertificatePath required. Optional: ClientCertificatePassword, ClientCertificateThumbprint",
                AuthTypeEnum.AzureActiveDirectory => "TenantId, ApplicationId required. Optional: ClientId, ClientSecret, Authority, Resource",
                AuthTypeEnum.LDAP => "UserID, Password, Domain required",
                AuthTypeEnum.SAML => "AuthUrl and SAML-specific parameters required",
                AuthTypeEnum.JWT => "Pre-obtained JWT token in ApiKey or KeyToken property required",
                AuthTypeEnum.Digest => "UserID and Password properties required (more secure than Basic)",
                AuthTypeEnum.Trusted => "Uses Windows authentication. Set TrustedConnection=true (SQL Server specific)",
                AuthTypeEnum.Custom => "Uses custom implementation with ParameterList dictionary",
                _ => "Unknown authentication type"
            };
        }

        /// <summary>
        /// Checks if the auth type requires automatic token refresh
        /// </summary>
        /// <param name="authType">The authentication type</param>
        /// <returns>True if token refresh is handled automatically</returns>
        public static bool RequiresTokenRefresh(this AuthTypeEnum authType)
        {
            return authType == AuthTypeEnum.Bearer || 
                   authType == AuthTypeEnum.OAuth2 ||
                   authType == AuthTypeEnum.AzureActiveDirectory;
        }

        /// <summary>
        /// Checks if this authentication type requires a username/password
        /// </summary>
        public static bool RequiresUserPassword(this AuthTypeEnum authType)
        {
            return authType switch
            {
                AuthTypeEnum.UserPassword or
                AuthTypeEnum.Basic or
                AuthTypeEnum.LDAP or
                AuthTypeEnum.Digest => true,
                _ => false
            };
        }

        /// <summary>
        /// Checks if this authentication type uses Windows credentials
        /// </summary>
        public static bool UsesWindowsCredentials(this AuthTypeEnum authType)
        {
            return authType == AuthTypeEnum.Windows || 
                   authType == AuthTypeEnum.Trusted ||
                   authType == AuthTypeEnum.Kerberos;
        }

        /// <summary>
        /// Checks if this authentication type requires OAuth configuration
        /// </summary>
        public static bool RequiresOAuthConfig(this AuthTypeEnum authType)
        {
            return authType == AuthTypeEnum.OAuth2 ||
                   authType == AuthTypeEnum.AzureActiveDirectory;
        }

        /// <summary>
        /// Checks if this authentication type requires a certificate
        /// </summary>
        public static bool RequiresCertificate(this AuthTypeEnum authType)
        {
            return authType == AuthTypeEnum.Certificate;
        }

        /// <summary>
        /// Checks if this authentication type is suitable for Web APIs
        /// </summary>
        public static bool IsWebApiAuth(this AuthTypeEnum authType)
        {
            return authType switch
            {
                AuthTypeEnum.None or
                AuthTypeEnum.ApiKey or
                AuthTypeEnum.Basic or
                AuthTypeEnum.Bearer or
                AuthTypeEnum.OAuth2 or
                AuthTypeEnum.JWT or
                AuthTypeEnum.Digest or
                AuthTypeEnum.Certificate => true,
                _ => false
            };
        }

        /// <summary>
        /// Checks if this authentication type is suitable for databases
        /// </summary>
        public static bool IsDatabaseAuth(this AuthTypeEnum authType)
        {
            return authType switch
            {
                AuthTypeEnum.UserPassword or
                AuthTypeEnum.Windows or
                AuthTypeEnum.Trusted or
                AuthTypeEnum.Kerberos or
                AuthTypeEnum.Certificate or
                AuthTypeEnum.AzureActiveDirectory => true,
                _ => false
            };
        }

        /// <summary>
        /// Gets the recommended authentication type for a given data source type
        /// </summary>
        public static AuthTypeEnum GetDefaultForDataSourceType(DataSourceType dataSourceType)
        {
            return dataSourceType switch
            {
                DataSourceType.SqlServer => AuthTypeEnum.Windows,
                DataSourceType.Mysql => AuthTypeEnum.UserPassword,
                DataSourceType.Postgre => AuthTypeEnum.UserPassword,
                DataSourceType.Oracle => AuthTypeEnum.UserPassword,
                DataSourceType.DB2 => AuthTypeEnum.UserPassword,
                DataSourceType.MongoDB => AuthTypeEnum.UserPassword,
                DataSourceType.WebApi or DataSourceType.RestApi => AuthTypeEnum.Bearer,
                DataSourceType.SOAP => AuthTypeEnum.Basic,
                DataSourceType.CSV or DataSourceType.Text or DataSourceType.Xls => AuthTypeEnum.None,
                DataSourceType.Json or DataSourceType.XML => AuthTypeEnum.None,
                _ => AuthTypeEnum.None
            };
        }
    }
}
