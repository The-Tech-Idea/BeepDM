using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep.Workflow.Models.Base;
using TheTechIdea.Beep.Workflow.Models;

namespace TheTechIdea.Beep.Workflow.Models.Base
{
    /// <summary>
    /// Implementation partial class for Connection - contains business logic methods
    /// </summary>
    public partial class Connection
    {
        /// <summary>
        /// Validates the connection configuration
        /// </summary>
        public ValidationResult Validate()
        {
            var result = new ValidationResult();

            if (string.IsNullOrWhiteSpace(Name))
            {
                result.Errors.Add("Connection name is required");
            }

            if (string.IsNullOrWhiteSpace(ServiceName))
            {
                result.Errors.Add("Service name is required");
            }

            // Validate based on connection type
            ValidateConnectionType(result);

            // Validate authentication
            ValidateAuthentication(result);

            // Validate URLs and endpoints
            ValidateEndpoints(result);

            return result;
        }

        /// <summary>
        /// Validates connection type specific requirements
        /// </summary>
        private void ValidateConnectionType(ValidationResult result)
        {
            switch (ConnectionType)
            {
                case ConnectionType.ApiKey:
                    if (string.IsNullOrWhiteSpace(ApiKey))
                    {
                        result.Errors.Add("API Key is required for API Key connections");
                    }
                    break;

                case ConnectionType.OAuth2:
                    if (string.IsNullOrWhiteSpace(ClientId) || string.IsNullOrWhiteSpace(ClientSecret))
                    {
                        result.Errors.Add("Client ID and Client Secret are required for OAuth2 connections");
                    }
                    if (string.IsNullOrWhiteSpace(AuthorizationUrl) || string.IsNullOrWhiteSpace(TokenUrl))
                    {
                        result.Errors.Add("Authorization URL and Token URL are required for OAuth2 connections");
                    }
                    break;

                case ConnectionType.BasicAuth:
                    if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
                    {
                        result.Errors.Add("Username and Password are required for Basic Auth connections");
                    }
                    break;

                case ConnectionType.BearerToken:
                    if (string.IsNullOrWhiteSpace(AccessToken))
                    {
                        result.Errors.Add("Access Token is required for Bearer Token connections");
                    }
                    break;

                case ConnectionType.Database:
                    if (string.IsNullOrWhiteSpace(ConnectionString))
                    {
                        result.Errors.Add("Connection string is required for Database connections");
                    }
                    break;
            }
        }

        /// <summary>
        /// Validates authentication configuration
        /// </summary>
        private void ValidateAuthentication(ValidationResult result)
        {
            if (TimeoutSeconds <= 0)
            {
                result.Errors.Add("Timeout must be greater than 0 seconds");
            }

            if (RetryCount < 0)
            {
                result.Errors.Add("Retry count cannot be negative");
            }

            if (RateLimitPerMinute < 0)
            {
                result.Errors.Add("Rate limit cannot be negative");
            }
        }

        /// <summary>
        /// Validates endpoints and URLs
        /// </summary>
        private void ValidateEndpoints(ValidationResult result)
        {
            if (!string.IsNullOrWhiteSpace(BaseUrl))
            {
                try
                {
                    var uri = new Uri(BaseUrl);
                    if (!uri.IsAbsoluteUri)
                    {
                        result.Errors.Add("Base URL must be an absolute URL");
                    }
                }
                catch
                {
                    result.Errors.Add("Base URL is not a valid URL format");
                }
            }

            if (ConnectionType == ConnectionType.OAuth2)
            {
                ValidateOAuthUrls(result);
            }
        }

        /// <summary>
        /// Validates OAuth URLs
        /// </summary>
        private void ValidateOAuthUrls(ValidationResult result)
        {
            if (!string.IsNullOrWhiteSpace(AuthorizationUrl))
            {
                try
                {
                    var uri = new Uri(AuthorizationUrl);
                    if (!uri.IsAbsoluteUri)
                    {
                        result.Errors.Add("Authorization URL must be an absolute URL");
                    }
                }
                catch
                {
                    result.Errors.Add("Authorization URL is not a valid URL format");
                }
            }

            if (!string.IsNullOrWhiteSpace(TokenUrl))
            {
                try
                {
                    var uri = new Uri(TokenUrl);
                    if (!uri.IsAbsoluteUri)
                    {
                        result.Errors.Add("Token URL must be an absolute URL");
                    }
                }
                catch
                {
                    result.Errors.Add("Token URL is not a valid URL format");
                }
            }
        }

        /// <summary>
        /// Tests the connection
        /// </summary>
        public async Task<ConnectionTestResult> TestConnectionAsync()
        {
            var result = new ConnectionTestResult
            {
                ConnectionId = Id,
                ConnectionName = Name,
                TestedAt = DateTime.UtcNow
            };

            try
            {
                // Basic validation first
                var validation = Validate();
                if (!validation.IsValid)
                {
                    result.Status = ConnectionTestStatus.ConfigurationError;
                    result.ErrorMessage = string.Join("; ", validation.Errors);
                    return result;
                }

                // Perform actual connection test based on type
                switch (ConnectionType)
                {
                    case ConnectionType.ApiKey:
                    case ConnectionType.BearerToken:
                        result = await TestApiConnectionAsync();
                        break;

                    case ConnectionType.BasicAuth:
                        result = await TestBasicAuthConnectionAsync();
                        break;

                    case ConnectionType.OAuth2:
                        result = await TestOAuthConnectionAsync();
                        break;

                    case ConnectionType.Database:
                        result = await TestDatabaseConnectionAsync();
                        break;

                    default:
                        result.Status = ConnectionTestStatus.ConfigurationError;
                        result.ErrorMessage = "Unsupported connection type";
                        break;
                }
            }
            catch (Exception ex)
            {
                result.Status = ConnectionTestStatus.Failed;
                result.ErrorMessage = ex.Message;
                result.ErrorDetails = ex.ToString();
            }

            // Update connection status
            UpdateConnectionStatus(result);

            return result;
        }

        /// <summary>
        /// Tests API connection
        /// </summary>
        private async Task<ConnectionTestResult> TestApiConnectionAsync()
        {
            // Placeholder implementation - would make actual HTTP call
            var result = new ConnectionTestResult
            {
                ConnectionId = Id,
                ConnectionName = Name,
                TestedAt = DateTime.UtcNow,
                Status = ConnectionTestStatus.Success,
                ResponseTimeMs = 100
            };

            return result;
        }

        /// <summary>
        /// Tests Basic Auth connection
        /// </summary>
        private async Task<ConnectionTestResult> TestBasicAuthConnectionAsync()
        {
            // Placeholder implementation
            var result = new ConnectionTestResult
            {
                ConnectionId = Id,
                ConnectionName = Name,
                TestedAt = DateTime.UtcNow,
                Status = ConnectionTestStatus.Success,
                ResponseTimeMs = 150
            };

            return result;
        }

        /// <summary>
        /// Tests OAuth connection
        /// </summary>
        private async Task<ConnectionTestResult> TestOAuthConnectionAsync()
        {
            // Placeholder implementation
            var result = new ConnectionTestResult
            {
                ConnectionId = Id,
                ConnectionName = Name,
                TestedAt = DateTime.UtcNow,
                Status = ConnectionTestStatus.Success,
                ResponseTimeMs = 200
            };

            return result;
        }

        /// <summary>
        /// Tests database connection
        /// </summary>
        private async Task<ConnectionTestResult> TestDatabaseConnectionAsync()
        {
            // Placeholder implementation
            var result = new ConnectionTestResult
            {
                ConnectionId = Id,
                ConnectionName = Name,
                TestedAt = DateTime.UtcNow,
                Status = ConnectionTestStatus.Success,
                ResponseTimeMs = 50
            };

            return result;
        }

        /// <summary>
        /// Updates connection status based on test result
        /// </summary>
        private void UpdateConnectionStatus(ConnectionTestResult result)
        {
            LastTestedDate = result.TestedAt;

            if (result.Status == ConnectionTestStatus.Success)
            {
                Status = ConnectionStatus.Active;
            }
            else
            {
                Status = ConnectionStatus.Error;
            }

            ModifiedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Checks if rate limit allows the request
        /// </summary>
        public bool CanMakeRequest()
        {
            if (RateLimitPerMinute <= 0) return true;

            var now = DateTime.UtcNow;
            var minuteAgo = now.AddMinutes(-1);

            if (LastRequestDate.HasValue && LastRequestDate.Value > minuteAgo)
            {
                return RequestCountThisMinute < RateLimitPerMinute;
            }
            else
            {
                // Reset counter for new minute
                RequestCountThisMinute = 0;
                LastRequestDate = now;
                return true;
            }
        }

        /// <summary>
        /// Records a request for rate limiting
        /// </summary>
        public void RecordRequest()
        {
            var now = DateTime.UtcNow;
            var minuteAgo = now.AddMinutes(-1);

            if (!LastRequestDate.HasValue || LastRequestDate.Value <= minuteAgo)
            {
                RequestCountThisMinute = 1;
                LastRequestDate = now;
            }
            else
            {
                RequestCountThisMinute++;
            }
        }

        /// <summary>
        /// Gets the connection configuration as a dictionary
        /// </summary>
        public Dictionary<string, object> GetConfiguration()
        {
            var config = new Dictionary<string, object>
            {
                ["ConnectionType"] = ConnectionType.ToString(),
                ["ServiceName"] = ServiceName,
                ["BaseUrl"] = BaseUrl,
                ["TimeoutSeconds"] = TimeoutSeconds,
                ["RetryCount"] = RetryCount,
                ["RateLimitPerMinute"] = RateLimitPerMinute
            };

            // Add type-specific configuration
            switch (ConnectionType)
            {
                case ConnectionType.ApiKey:
                    config["ApiKey"] = !string.IsNullOrWhiteSpace(ApiKey) ? "***" : null;
                    break;

                case ConnectionType.OAuth2:
                    config["ClientId"] = ClientId;
                    config["AuthorizationUrl"] = AuthorizationUrl;
                    config["TokenUrl"] = TokenUrl;
                    config["Scope"] = Scope;
                    break;

                case ConnectionType.BasicAuth:
                    config["Username"] = Username;
                    break;

                case ConnectionType.BearerToken:
                    config["AccessToken"] = !string.IsNullOrWhiteSpace(AccessToken) ? "***" : null;
                    break;

                case ConnectionType.Database:
                    config["DatabaseName"] = DatabaseName;
                    config["ServerName"] = ServerName;
                    config["Port"] = Port;
                    break;
            }

            return config;
        }

        /// <summary>
        /// Creates a copy of this connection for a different configuration
        /// </summary>
        public Connection CreateCopy(string newName = null)
        {
            return new Connection
            {
                Name = newName ?? $"{Name}_Copy",
                Description = Description,
                ConnectionType = ConnectionType,
                ServiceName = ServiceName,
                ApiKey = ApiKey,
                ApiSecret = ApiSecret,
                AccessToken = AccessToken,
                RefreshToken = RefreshToken,
                ClientId = ClientId,
                ClientSecret = ClientSecret,
                BaseUrl = BaseUrl,
                AuthorizationUrl = AuthorizationUrl,
                TokenUrl = TokenUrl,
                Scope = Scope,
                ConnectionString = ConnectionString,
                DatabaseName = DatabaseName,
                ServerName = ServerName,
                Port = Port,
                Username = Username,
                Password = Password,
                ConfigurationJson = ConfigurationJson,
                TimeoutSeconds = TimeoutSeconds,
                RetryCount = RetryCount,
                RateLimitPerMinute = RateLimitPerMinute,
                IsActive = false, // Copy is inactive by default
                CreatedDate = DateTime.UtcNow,
                CreatedBy = CreatedBy
            };
        }
    }

    /// <summary>
    /// Connection test result
    /// </summary>
    public class ConnectionTestResult
    {
        public int ConnectionId { get; set; }
        public string ConnectionName { get; set; }
        public ConnectionTestStatus Status { get; set; }
        public DateTime TestedAt { get; set; }
        public long ResponseTimeMs { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorDetails { get; set; }
        public string ResponseData { get; set; }
    }
}
