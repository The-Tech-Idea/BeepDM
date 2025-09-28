using TheTechIdea.Beep.DriversConfigurations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Helpers.ConnectionHelpers;

namespace TheTechIdea.Beep.Helpers
{
    /// <summary>
    /// Core facade helper class for managing database and data source connections.
    /// Delegates operations to specialized helper classes for better maintainability.
    /// </summary>
    public static partial class ConnectionHelper
    {
        #region Driver Linking Operations (Delegated to ConnectionDriverLinkingHelper)

        /// <summary>
        /// Links a connection to its corresponding drivers in the configuration editor.
        /// </summary>
        /// <param name="cn">The connection properties.</param>
        /// <param name="configEditor">The configuration editor.</param>
        /// <returns>The connection drivers configuration.</returns>
        public static ConnectionDriversConfig LinkConnection2Drivers(IConnectionProperties cn, IConfigEditor configEditor)
        {
            return ConnectionDriverLinkingHelper.LinkConnection2Drivers(cn, configEditor);
        }

        /// <summary>
        /// Gets all available driver configurations for a specific data source type.
        /// </summary>
        /// <param name="dataSourceType">The data source type to filter by.</param>
        /// <param name="configEditor">The configuration editor.</param>
        /// <returns>A list of matching driver configurations.</returns>
        public static List<ConnectionDriversConfig> GetDriversForDataSourceType(DataSourceType dataSourceType, IConfigEditor configEditor)
        {
            return ConnectionDriverLinkingHelper.GetDriversForDataSourceType(dataSourceType, configEditor);
        }

        /// <summary>
        /// Gets all available driver configurations that support a specific file extension.
        /// </summary>
        /// <param name="fileExtension">The file extension (without the dot).</param>
        /// <param name="configEditor">The configuration editor.</param>
        /// <returns>A list of matching driver configurations.</returns>
        public static List<ConnectionDriversConfig> GetDriversForFileExtension(string fileExtension, IConfigEditor configEditor)
        {
            return ConnectionDriverLinkingHelper.GetDriversForFileExtension(fileExtension, configEditor);
        }

        /// <summary>
        /// Gets the best matching driver for the given connection properties.
        /// </summary>
        /// <param name="connectionProperties">The connection properties.</param>
        /// <param name="configEditor">The configuration editor.</param>
        /// <returns>The best matching driver configuration or null if none found.</returns>
        public static ConnectionDriversConfig GetBestMatchingDriver(IConnectionProperties connectionProperties, IConfigEditor configEditor)
        {
            return ConnectionDriverLinkingHelper.GetBestMatchingDriver(connectionProperties, configEditor);
        }

        #endregion

        #region Connection String Processing Operations (Delegated to ConnectionStringProcessingHelper)

        /// <summary>
        /// Replaces placeholders in a connection string based on the provided parameters.
        /// </summary>
        /// <param name="DataSourceDriver">The driver configuration for the data source.</param>
        /// <param name="ConnectionProp">The connection properties.</param>
        /// <param name="DMEEditor">The DME editor.</param>
        /// <returns>The processed connection string with placeholders replaced.</returns>
        public static string ReplaceValueFromConnectionString(ConnectionDriversConfig DataSourceDriver, IConnectionProperties ConnectionProp, IDMEEditor DMEEditor)
        {
            return ConnectionStringProcessingHelper.ReplaceValueFromConnectionString(DataSourceDriver, ConnectionProp, DMEEditor);
        }

        /// <summary>
        /// Normalizes a relative path to an absolute path.
        /// </summary>
        /// <param name="relativePath">The relative path to normalize.</param>
        /// <param name="basePath">The base path to use for normalization.</param>
        /// <returns>The normalized absolute path.</returns>
        public static string NormalizePath(string relativePath, string basePath)
        {
            return ConnectionStringProcessingHelper.NormalizePath(relativePath, basePath);
        }

        /// <summary>
        /// Normalizes the file path in connection properties.
        /// </summary>
        /// <param name="connectionProp">The connection properties to normalize.</param>
        /// <param name="basePath">The base path to use for normalization.</param>
        public static void NormalizeFilePath(IConnectionProperties connectionProp, string basePath)
        {
            ConnectionStringProcessingHelper.NormalizeFilePath(connectionProp, basePath);
        }

        /// <summary>
        /// Validates that all required placeholders in a connection string template have corresponding values.
        /// </summary>
        /// <param name="connectionStringTemplate">The connection string template.</param>
        /// <param name="connectionProp">The connection properties.</param>
        /// <returns>A list of missing required placeholders.</returns>
        public static List<string> ValidateRequiredPlaceholders(string connectionStringTemplate, IConnectionProperties connectionProp)
        {
            return ConnectionStringProcessingHelper.ValidateRequiredPlaceholders(connectionStringTemplate, connectionProp);
        }

        #endregion

        #region Connection String Validation Operations (Delegated to ConnectionStringValidationHelper)

        /// <summary>
        /// Validates a connection string for a specific data source type.
        /// </summary>
        /// <param name="connectionString">The connection string to validate.</param>
        /// <param name="dataSourceType">The type of the data source.</param>
        /// <returns>True if the connection string is valid, otherwise false.</returns>
        public static bool IsConnectionStringValid(string connectionString, DataSourceType dataSourceType)
        {
            return ConnectionStringValidationHelper.IsConnectionStringValid(connectionString, dataSourceType);
        }

        /// <summary>
        /// Validates a SQL Server connection string.
        /// </summary>
        /// <param name="connectionString">The connection string to validate.</param>
        /// <returns>True if valid, false otherwise.</returns>
        public static bool ValidateSqlServerConnectionString(string connectionString)
        {
            return ConnectionStringValidationHelper.ValidateSqlServerConnectionString(connectionString);
        }

        /// <summary>
        /// Validates a MySQL connection string.
        /// </summary>
        /// <param name="connectionString">The connection string to validate.</param>
        /// <returns>True if valid, false otherwise.</returns>
        public static bool ValidateMySqlConnectionString(string connectionString)
        {
            return ConnectionStringValidationHelper.ValidateMySqlConnectionString(connectionString);
        }

        /// <summary>
        /// Validates a SQLite connection string.
        /// </summary>
        /// <param name="connectionString">The connection string to validate.</param>
        /// <returns>True if valid, false otherwise.</returns>
        public static bool ValidateSQLiteConnectionString(string connectionString)
        {
            return ConnectionStringValidationHelper.ValidateSQLiteConnectionString(connectionString);
        }

        /// <summary>
        /// Gets validation requirements for a specific data source type.
        /// </summary>
        /// <param name="dataSourceType">The data source type.</param>
        /// <returns>A string describing the validation requirements.</returns>
        public static string GetValidationRequirements(DataSourceType dataSourceType)
        {
            return ConnectionStringValidationHelper.GetValidationRequirements(dataSourceType);
        }

        /// <summary>
        /// Validates the structure of a connection string without checking data source specific requirements.
        /// </summary>
        /// <param name="connectionString">The connection string to validate.</param>
        /// <returns>True if the structure is valid, false otherwise.</returns>
        public static bool ValidateConnectionStringStructure(string connectionString)
        {
            return ConnectionStringValidationHelper.ValidateConnectionStringStructure(connectionString);
        }

        #endregion

        #region Connection String Security Operations (Delegated to ConnectionStringSecurityHelper)

        /// <summary>
        /// Creates a secure version of a connection string by masking sensitive information.
        /// </summary>
        /// <param name="connectionString">The connection string to secure.</param>
        /// <returns>A secure version of the connection string with sensitive data masked.</returns>
        public static string SecureConnectionString(string connectionString)
        {
            return ConnectionStringSecurityHelper.SecureConnectionString(connectionString);
        }

        /// <summary>
        /// Checks if a connection string contains potentially sensitive information.
        /// </summary>
        /// <param name="connectionString">The connection string to check.</param>
        /// <returns>True if sensitive information is detected, false otherwise.</returns>
        public static bool ContainsSensitiveInformation(string connectionString)
        {
            return ConnectionStringSecurityHelper.ContainsSensitiveInformation(connectionString);
        }

        /// <summary>
        /// Extracts and masks only the sensitive parts of a connection string, leaving non-sensitive parts intact.
        /// </summary>
        /// <param name="connectionString">The connection string to process.</param>
        /// <param name="maskChar">The character to use for masking (default is '*').</param>
        /// <param name="visibleChars">Number of characters to keep visible at the start and end (default is 2).</param>
        /// <returns>The connection string with selective masking applied.</returns>
        public static string SelectiveMask(string connectionString, char maskChar = '*', int visibleChars = 2)
        {
            return ConnectionStringSecurityHelper.SelectiveMask(connectionString, maskChar, visibleChars);
        }

        /// <summary>
        /// Gets a list of parameter names that are considered sensitive.
        /// </summary>
        /// <returns>An array of sensitive parameter names.</returns>
        public static string[] GetSensitiveParameterNames()
        {
            return ConnectionStringSecurityHelper.GetSensitiveParameterNames();
        }

        /// <summary>
        /// Validates that a connection string has been properly secured (no plain text sensitive information).
        /// </summary>
        /// <param name="connectionString">The connection string to validate.</param>
        /// <returns>True if the connection string appears to be secured, false otherwise.</returns>
        public static bool IsConnectionStringSecured(string connectionString)
        {
            return ConnectionStringSecurityHelper.IsConnectionStringSecured(connectionString);
        }

        #endregion

        #region Configuration Operations (Existing functionality)

        /// <summary>
        /// Returns a list of ConnectionDriversConfig objects representing different connection configurations.
        /// </summary>
        /// <returns>A list of ConnectionDriversConfig objects representing different connection configurations.</returns>
        public static List<ConnectionDriversConfig> GetAllConnectionConfigs()
        {
            List<ConnectionDriversConfig> configs = new List<ConnectionDriversConfig>();

            // Add all configurations from different categories
            configs.AddRange(GetRDBMSConfigs());
            configs.AddRange(GetNoSQLConfigs());
            configs.AddRange(GetVectorDBConfigs());
            configs.AddRange(GetFileConfigs());
            configs.AddRange(GetCloudConfigs());
            configs.AddRange(GetStreamingConfigs());
            configs.AddRange(GetInMemoryConfigs());
            configs.AddRange(GetCacheConfigs());
            configs.AddRange(GetWebAPIConfigs());
            
            // Add new connector categories
            configs.AddRange(GetAllConnectorConfigs());
            configs.AddRange(GetBlockchainConnectorConfigs());

            return configs;
        }

        /// <summary>
        /// Gets all connector configurations from all connector categories.
        /// </summary>
        /// <returns>List of all connector configurations.</returns>
        public static List<ConnectionDriversConfig> GetAllConnectorConfigs()
        {
            List<ConnectionDriversConfig> configs = new List<ConnectionDriversConfig>();

            // Add all connector categories
            configs.AddRange(GetCRMConnectorConfigs());
            configs.AddRange(GetMarketingConnectorConfigs());
            configs.AddRange(GetECommerceConnectorConfigs());
            configs.AddRange(GetProjectManagementConnectorConfigs());
            configs.AddRange(GetCommunicationConnectorConfigs());
            // Add more connector categories as they are created

            return configs;
        }
        #endregion
    }
}
