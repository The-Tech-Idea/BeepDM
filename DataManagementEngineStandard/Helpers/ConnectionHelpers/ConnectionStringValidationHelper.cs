using System;
using System.Text.RegularExpressions;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Helpers.ConnectionHelpers
{
    /// <summary>
    /// Helper class for validating connection strings for different data source types.
    /// </summary>
    public static class ConnectionStringValidationHelper
    {
        /// <summary>
        /// Validates a connection string for a specific data source type.
        /// </summary>
        /// <param name="connectionString">The connection string to validate.</param>
        /// <param name="dataSourceType">The type of the data source.</param>
        /// <returns>True if the connection string is valid, otherwise false.</returns>
        public static bool IsConnectionStringValid(string connectionString, DataSourceType dataSourceType)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return false;

            return dataSourceType switch
            {
                DataSourceType.SqlServer => ValidateSqlServerConnectionString(connectionString),
                DataSourceType.Mysql => ValidateMySqlConnectionString(connectionString),
                DataSourceType.SqlLite => ValidateSQLiteConnectionString(connectionString),
                DataSourceType.Oracle => ValidateOracleConnectionString(connectionString),
                DataSourceType.Postgre => ValidatePostgreSQLConnectionString(connectionString),
                DataSourceType.MongoDB => ValidateMongoDBConnectionString(connectionString),
                DataSourceType.Redis => ValidateRedisConnectionString(connectionString),
                DataSourceType.OLEDB => ValidateOleDBConnectionString(connectionString),
                DataSourceType.ODBC => ValidateODBCConnectionString(connectionString),
                _ => ValidateGenericConnectionString(connectionString)
            };
        }

        /// <summary>
        /// Validates a SQL Server connection string.
        /// </summary>
        /// <param name="connectionString">The connection string to validate.</param>
        /// <returns>True if valid, false otherwise.</returns>
        public static bool ValidateSqlServerConnectionString(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return false;

            // SQL Server requires either Server/Data Source and optionally Database/Initial Catalog
            bool hasServer = ContainsParameter(connectionString, "Server") ||
                           ContainsParameter(connectionString, "Data Source") ||
                           ContainsParameter(connectionString, "Address") ||
                           ContainsParameter(connectionString, "Addr") ||
                           ContainsParameter(connectionString, "Network Address");

            // Check for integrated security or user credentials
            bool hasAuth = ContainsParameter(connectionString, "Integrated Security") ||
                          ContainsParameter(connectionString, "Trusted_Connection") ||
                          (ContainsParameter(connectionString, "User ID") || ContainsParameter(connectionString, "UID"));

            return hasServer;
        }

        /// <summary>
        /// Validates a MySQL connection string.
        /// </summary>
        /// <param name="connectionString">The connection string to validate.</param>
        /// <returns>True if valid, false otherwise.</returns>
        public static bool ValidateMySqlConnectionString(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return false;

            // MySQL requires Server/Host
            return ContainsParameter(connectionString, "Server") ||
                   ContainsParameter(connectionString, "Host") ||
                   ContainsParameter(connectionString, "Data Source");
        }

        /// <summary>
        /// Validates a SQLite connection string.
        /// </summary>
        /// <param name="connectionString">The connection string to validate.</param>
        /// <returns>True if valid, false otherwise.</returns>
        public static bool ValidateSQLiteConnectionString(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return false;

            // SQLite requires Data Source (file path)
            return ContainsParameter(connectionString, "Data Source") ||
                   ContainsParameter(connectionString, "DataSource");
        }

        /// <summary>
        /// Validates an Oracle connection string.
        /// </summary>
        /// <param name="connectionString">The connection string to validate.</param>
        /// <returns>True if valid, false otherwise.</returns>
        public static bool ValidateOracleConnectionString(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return false;

            // Oracle can use Data Source (TNS name) or Server + Port + Service Name
            bool hasTnsName = ContainsParameter(connectionString, "Data Source");
            bool hasServerInfo = ContainsParameter(connectionString, "Server") ||
                                ContainsParameter(connectionString, "Host");

            return hasTnsName || hasServerInfo;
        }

        /// <summary>
        /// Validates a PostgreSQL connection string.
        /// </summary>
        /// <param name="connectionString">The connection string to validate.</param>
        /// <returns>True if valid, false otherwise.</returns>
        public static bool ValidatePostgreSQLConnectionString(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return false;

            // PostgreSQL requires Server/Host
            return ContainsParameter(connectionString, "Server") ||
                   ContainsParameter(connectionString, "Host") ||
                   ContainsParameter(connectionString, "Data Source");
        }

        /// <summary>
        /// Validates a MongoDB connection string.
        /// </summary>
        /// <param name="connectionString">The connection string to validate.</param>
        /// <returns>True if valid, false otherwise.</returns>
        public static bool ValidateMongoDBConnectionString(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return false;

            // MongoDB connection strings typically start with mongodb:// or mongodb+srv://
            return connectionString.StartsWith("mongodb://", StringComparison.OrdinalIgnoreCase) ||
                   connectionString.StartsWith("mongodb+srv://", StringComparison.OrdinalIgnoreCase) ||
                   ContainsParameter(connectionString, "Server") ||
                   ContainsParameter(connectionString, "Host");
        }

        /// <summary>
        /// Validates a Redis connection string.
        /// </summary>
        /// <param name="connectionString">The connection string to validate.</param>
        /// <returns>True if valid, false otherwise.</returns>
        public static bool ValidateRedisConnectionString(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return false;

            // Redis connection strings can be simple host:port format or contain parameters
            return ContainsParameter(connectionString, "Server") ||
                   ContainsParameter(connectionString, "Host") ||
                   Regex.IsMatch(connectionString, @"^[^=]+:\d+$") || // Simple host:port format
                   connectionString.Contains("="); // Standard connection string format
        }

        /// <summary>
        /// Validates an OleDB connection string.
        /// </summary>
        /// <param name="connectionString">The connection string to validate.</param>
        /// <returns>True if valid, false otherwise.</returns>
        public static bool ValidateOleDBConnectionString(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return false;

            // OleDB requires Provider
            return ContainsParameter(connectionString, "Provider");
        }

        /// <summary>
        /// Validates an ODBC connection string.
        /// </summary>
        /// <param name="connectionString">The connection string to validate.</param>
        /// <returns>True if valid, false otherwise.</returns>
        public static bool ValidateODBCConnectionString(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return false;

            // ODBC can use DSN or Driver
            return ContainsParameter(connectionString, "DSN") ||
                   ContainsParameter(connectionString, "Driver") ||
                   ContainsParameter(connectionString, "FILEDSN");
        }

        /// <summary>
        /// Validates a generic connection string by checking for basic key-value pair structure.
        /// </summary>
        /// <param name="connectionString">The connection string to validate.</param>
        /// <returns>True if valid, false otherwise.</returns>
        public static bool ValidateGenericConnectionString(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return false;

            // Basic check for key-value pair structure
            return connectionString.Contains("=");
        }

        /// <summary>
        /// Checks if a connection string contains a specific parameter.
        /// </summary>
        /// <param name="connectionString">The connection string to check.</param>
        /// <param name="parameterName">The parameter name to look for.</param>
        /// <returns>True if the parameter is found, false otherwise.</returns>
        private static bool ContainsParameter(string connectionString, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(connectionString) || string.IsNullOrWhiteSpace(parameterName))
                return false;

            // Create pattern to match parameter name followed by equals sign
            string pattern = $@"\b{Regex.Escape(parameterName)}\s*=";
            return Regex.IsMatch(connectionString, pattern, RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Gets validation requirements for a specific data source type.
        /// </summary>
        /// <param name="dataSourceType">The data source type.</param>
        /// <returns>A string describing the validation requirements.</returns>
        public static string GetValidationRequirements(DataSourceType dataSourceType)
        {
            return dataSourceType switch
            {
                DataSourceType.SqlServer => "Requires: Server/Data Source. Optional: Database/Initial Catalog, authentication parameters.",
                DataSourceType.Mysql => "Requires: Server/Host. Optional: Database, Port, authentication parameters.",
                DataSourceType.SqlLite => "Requires: Data Source (file path).",
                DataSourceType.Oracle => "Requires: Data Source (TNS name) OR Server/Host. Optional: Port, Service Name, authentication parameters.",
                DataSourceType.Postgre => "Requires: Server/Host. Optional: Database, Port, authentication parameters.",
                DataSourceType.MongoDB => "Requires: mongodb:// or mongodb+srv:// prefix OR Server/Host. Optional: Database, authentication parameters.",
                DataSourceType.Redis => "Requires: Server/Host OR host:port format. Optional: Password, Database.",
                DataSourceType.OLEDB => "Requires: Provider. Additional parameters depend on the specific provider.",
                DataSourceType.ODBC => "Requires: DSN OR Driver OR FILEDSN. Additional parameters depend on the driver.",
                _ => "Basic requirement: Must contain at least one key=value pair."
            };
        }

        /// <summary>
        /// Validates the structure of a connection string without checking data source specific requirements.
        /// </summary>
        /// <param name="connectionString">The connection string to validate.</param>
        /// <returns>True if the structure is valid, false otherwise.</returns>
        public static bool ValidateConnectionStringStructure(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return false;

            // Check for basic key=value structure
            if (!connectionString.Contains("="))
                return false;

            // Validate that we don't have malformed key=value pairs
            var pairs = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var pair in pairs)
            {
                var trimmedPair = pair.Trim();
                if (string.IsNullOrEmpty(trimmedPair))
                    continue;

                var equalsIndex = trimmedPair.IndexOf('=');
                if (equalsIndex <= 0 || equalsIndex == trimmedPair.Length - 1)
                    return false; // Invalid key=value format
            }

            return true;
        }
    }
}