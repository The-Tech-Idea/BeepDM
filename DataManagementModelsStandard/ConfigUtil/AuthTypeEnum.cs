using System;

namespace TheTechIdea.Beep.ConfigUtil
{
    /// <summary>
    /// Enumeration of supported authentication types for Web API connections
    /// Based on the WebAPIAuthenticationHelper implementation
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
        OAuth2 = 4
    }

    /// <summary>
    /// Extension methods for AuthTypeEnum
    /// </summary>
    public static class AuthTypeEnumExtensions
    {
        /// <summary>
        /// Converts AuthTypeEnum to string representation used by WebAPIAuthenticationHelper
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
            if (string.IsNullOrEmpty(authTypeString))
                return AuthTypeEnum.None;

            return authTypeString.ToLower() switch
            {
                "none" => AuthTypeEnum.None,
                "apikey" => AuthTypeEnum.ApiKey,
                "basic" => AuthTypeEnum.Basic,
                "bearer" => AuthTypeEnum.Bearer,
                "oauth2" => AuthTypeEnum.OAuth2,
                _ => AuthTypeEnum.None
            };
        }

        /// <summary>
        /// Gets a description of what credentials are required for each auth type
        /// </summary>
        /// <param name="authType">The authentication type</param>
        /// <returns>Description of required credentials</returns>
        public static string GetRequiredCredentials(this AuthTypeEnum authType)
        {
            return authType switch
            {
                AuthTypeEnum.None => "No credentials required",
                AuthTypeEnum.ApiKey => "ApiKey property required. Optionally set ApiKeyHeader parameter (default: X-API-Key)",
                AuthTypeEnum.Basic => "UserID and Password properties required",
                AuthTypeEnum.Bearer => "Either ApiKey for static token, or OAuth2 parameters for dynamic token",
                AuthTypeEnum.OAuth2 => "TokenUrl, ClientId, ClientSecret required. Optional: Scope, GrantType, RedirectUri, AuthCode",
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
            return authType == AuthTypeEnum.Bearer || authType == AuthTypeEnum.OAuth2;
        }
    }
}
