using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Workflow.Models.Base;
using TheTechIdea.Beep.Workflow.Models;

namespace TheTechIdea.Beep.Workflow.Models.Helpers
{
    /// <summary>
    /// Helper class for Connection operations
    /// </summary>
    public static class ConnectionHelper
    {
        /// <summary>
        /// Creates a deep clone of a connection
        /// </summary>
        public static Connection CloneConnection(Connection original)
        {
            if (original == null) return null;

            var cloned = new Connection
            {
                Name = original.Name,
                Description = original.Description,
                ConnectionType = original.ConnectionType,
                ServiceName = original.ServiceName,
                ApiKey = original.ApiKey,
                ApiSecret = original.ApiSecret,
                AccessToken = original.AccessToken,
                RefreshToken = original.RefreshToken,
                ClientId = original.ClientId,
                ClientSecret = original.ClientSecret,
                BaseUrl = original.BaseUrl,
                AuthorizationUrl = original.AuthorizationUrl,
                TokenUrl = original.TokenUrl,
                Scope = original.Scope,
                ConnectionString = original.ConnectionString,
                DatabaseName = original.DatabaseName,
                ServerName = original.ServerName,
                Port = original.Port,
                Username = original.Username,
                Password = original.Password,
                ConfigurationJson = original.ConfigurationJson,
                IsActive = original.IsActive,
                TimeoutSeconds = original.TimeoutSeconds,
                RetryCount = original.RetryCount,
                RateLimitPerMinute = original.RateLimitPerMinute,
                CreatedDate = original.CreatedDate,
                ModifiedDate = original.ModifiedDate,
                CreatedBy = original.CreatedBy,
                ModifiedBy = original.ModifiedBy
            };

            return cloned;
        }

        /// <summary>
        /// Validates a connection and returns detailed validation results
        /// </summary>
        public static ValidationResult ValidateConnection(Connection connection)
        {
            var result = new ValidationResult();

            if (connection == null)
            {
                result.Errors.Add("Connection is null");
                return result;
            }

            // Basic validation
            if (string.IsNullOrWhiteSpace(connection.Name))
            {
                result.Errors.Add("Connection name is required");
            }

            if (connection.Name?.Length > 200)
            {
                result.Errors.Add("Connection name cannot exceed 200 characters");
            }

            if (string.IsNullOrWhiteSpace(connection.ServiceName))
            {
                result.Errors.Add("Service name is required");
            }

            // Type-specific validation
            ValidateConnectionType(connection, result);

            // Authentication validation
            ValidateAuthentication(connection, result);

            // URL validation
            ValidateUrls(connection, result);

            return result;
        }

        /// <summary>
        /// Validates connection type specific requirements
        /// </summary>
        private static void ValidateConnectionType(Connection connection, ValidationResult result)
        {
            switch (connection.ConnectionType)
            {
                case ConnectionType.ApiKey:
                    if (string.IsNullOrWhiteSpace(connection.ApiKey))
                    {
                        result.Errors.Add($"Connection '{connection.Name}': API Key is required for API Key connections");
                    }
                    break;

                case ConnectionType.OAuth2:
                    if (string.IsNullOrWhiteSpace(connection.ClientId))
                    {
                        result.Errors.Add($"Connection '{connection.Name}': Client ID is required for OAuth2 connections");
                    }
                    if (string.IsNullOrWhiteSpace(connection.ClientSecret))
                    {
                        result.Errors.Add($"Connection '{connection.Name}': Client Secret is required for OAuth2 connections");
                    }
                    if (string.IsNullOrWhiteSpace(connection.AuthorizationUrl))
                    {
                        result.Errors.Add($"Connection '{connection.Name}': Authorization URL is required for OAuth2 connections");
                    }
                    if (string.IsNullOrWhiteSpace(connection.TokenUrl))
                    {
                        result.Errors.Add($"Connection '{connection.Name}': Token URL is required for OAuth2 connections");
                    }
                    break;

                case ConnectionType.BasicAuth:
                    if (string.IsNullOrWhiteSpace(connection.Username))
                    {
                        result.Errors.Add($"Connection '{connection.Name}': Username is required for Basic Auth connections");
                    }
                    if (string.IsNullOrWhiteSpace(connection.Password))
                    {
                        result.Errors.Add($"Connection '{connection.Name}': Password is required for Basic Auth connections");
                    }
                    break;

                case ConnectionType.BearerToken:
                    if (string.IsNullOrWhiteSpace(connection.AccessToken))
                    {
                        result.Errors.Add($"Connection '{connection.Name}': Access Token is required for Bearer Token connections");
                    }
                    break;

                case ConnectionType.Database:
                    if (string.IsNullOrWhiteSpace(connection.ConnectionString))
                    {
                        result.Errors.Add($"Connection '{connection.Name}': Connection string is required for Database connections");
                    }
                    break;
            }
        }

        /// <summary>
        /// Validates authentication settings
        /// </summary>
        private static void ValidateAuthentication(Connection connection, ValidationResult result)
        {
            if (connection.TimeoutSeconds <= 0)
            {
                result.Errors.Add($"Connection '{connection.Name}': Timeout must be greater than 0 seconds");
            }

            if (connection.RetryCount < 0)
            {
                result.Errors.Add($"Connection '{connection.Name}': Retry count cannot be negative");
            }

            if (connection.RateLimitPerMinute < 0)
            {
                result.Errors.Add($"Connection '{connection.Name}': Rate limit cannot be negative");
            }
        }

        /// <summary>
        /// Validates URLs
        /// </summary>
        private static void ValidateUrls(Connection connection, ValidationResult result)
        {
            if (!string.IsNullOrWhiteSpace(connection.BaseUrl))
            {
                if (!IsValidUrl(connection.BaseUrl))
                {
                    result.Errors.Add($"Connection '{connection.Name}': Base URL is not a valid URL");
                }
            }

            if (connection.ConnectionType == ConnectionType.OAuth2)
            {
                if (!string.IsNullOrWhiteSpace(connection.AuthorizationUrl) && !IsValidUrl(connection.AuthorizationUrl))
                {
                    result.Errors.Add($"Connection '{connection.Name}': Authorization URL is not a valid URL");
                }

                if (!string.IsNullOrWhiteSpace(connection.TokenUrl) && !IsValidUrl(connection.TokenUrl))
                {
                    result.Errors.Add($"Connection '{connection.Name}': Token URL is not a valid URL");
                }
            }
        }

        /// <summary>
        /// Checks if a string is a valid URL
        /// </summary>
        private static bool IsValidUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                return uri.IsAbsoluteUri;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets connection statistics
        /// </summary>
        public static ConnectionStatistics GetConnectionStatistics(Connection connection)
        {
            if (connection == null) return new ConnectionStatistics();

            return new ConnectionStatistics
            {
                TestLogCount = connection.TestLogs?.Count ?? 0,
                SuccessfulTestCount = connection.TestLogs?.Count(t => t.Status == ConnectionTestStatus.Success) ?? 0,
                FailedTestCount = connection.TestLogs?.Count(t => t.Status != ConnectionTestStatus.Success) ?? 0,
                LastTestDate = connection.LastTestedDate,
                IsActive = connection.IsActive && connection.Status == ConnectionStatus.Active,
                DaysSinceLastTest = connection.LastTestedDate.HasValue ?
                    (DateTime.UtcNow - connection.LastTestedDate.Value).Days : (int?)null
            };
        }

        /// <summary>
        /// Gets connections by service name
        /// </summary>
        public static IEnumerable<Connection> GetConnectionsByService(IEnumerable<Connection> connections, string serviceName)
        {
            if (connections == null || string.IsNullOrWhiteSpace(serviceName))
            {
                return Enumerable.Empty<Connection>();
            }

            return connections.Where(c =>
                c.ServiceName?.Contains(serviceName, StringComparison.OrdinalIgnoreCase) == true);
        }

        /// <summary>
        /// Gets active connections
        /// </summary>
        public static IEnumerable<Connection> GetActiveConnections(IEnumerable<Connection> connections)
        {
            if (connections == null) return Enumerable.Empty<Connection>();

            return connections.Where(c => c.IsActive && c.Status == ConnectionStatus.Active);
        }

        /// <summary>
        /// Gets connections by type
        /// </summary>
        public static IEnumerable<Connection> GetConnectionsByType(IEnumerable<Connection> connections, ConnectionType connectionType)
        {
            if (connections == null) return Enumerable.Empty<Connection>();

            return connections.Where(c => c.ConnectionType == connectionType);
        }

        /// <summary>
        /// Checks if a connection needs reauthorization (for OAuth)
        /// </summary>
        public static bool NeedsReauthorization(Connection connection)
        {
            if (connection == null || connection.ConnectionType != ConnectionType.OAuth2)
            {
                return false;
            }

            if (connection.TokenExpiryDate.HasValue)
            {
                // Add 5 minute buffer for token refresh
                return DateTime.UtcNow >= connection.TokenExpiryDate.Value.AddMinutes(-5);
            }

            // If no expiry date but has refresh token, might need refresh
            return !string.IsNullOrWhiteSpace(connection.RefreshToken) &&
                   string.IsNullOrWhiteSpace(connection.AccessToken);
        }

        /// <summary>
        /// Gets connection health status
        /// </summary>
        public static ConnectionHealthStatus GetHealthStatus(Connection connection)
        {
            if (connection == null)
            {
                return ConnectionHealthStatus.Unknown;
            }

            if (!connection.IsActive)
            {
                return ConnectionHealthStatus.Inactive;
            }

            if (connection.Status == ConnectionStatus.Error)
            {
                return ConnectionHealthStatus.Error;
            }

            if (connection.Status == ConnectionStatus.Expired)
            {
                return ConnectionHealthStatus.Expired;
            }

            if (NeedsReauthorization(connection))
            {
                return ConnectionHealthStatus.NeedsReauthorization;
            }

            if (connection.LastTestedDate.HasValue)
            {
                var daysSinceLastTest = (DateTime.UtcNow - connection.LastTestedDate.Value).Days;
                if (daysSinceLastTest > 7)
                {
                    return ConnectionHealthStatus.Stale;
                }
            }

            return ConnectionHealthStatus.Healthy;
        }

        /// <summary>
        /// Masks sensitive information in connection configuration
        /// </summary>
        public static Dictionary<string, object> GetMaskedConfiguration(Connection connection)
        {
            if (connection == null) return new Dictionary<string, object>();

            var config = connection.GetConfiguration();

            // Mask sensitive fields
            var sensitiveKeys = new[] { "ApiKey", "ApiSecret", "AccessToken", "RefreshToken", "ClientSecret", "Password", "ConnectionString" };

            foreach (var key in sensitiveKeys)
            {
                if (config.ContainsKey(key) && config[key] != null)
                {
                    config[key] = "***";
                }
            }

            return config;
        }
    }

    /// <summary>
    /// Connection statistics
    /// </summary>
    public class ConnectionStatistics
    {
        public int TestLogCount { get; set; }
        public int SuccessfulTestCount { get; set; }
        public int FailedTestCount { get; set; }
        public DateTime? LastTestDate { get; set; }
        public bool IsActive { get; set; }
        public int? DaysSinceLastTest { get; set; }
    }
}
