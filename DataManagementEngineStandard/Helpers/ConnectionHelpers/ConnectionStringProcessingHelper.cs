using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Helpers.ConnectionHelpers
{
    /// <summary>
    /// Helper class for processing and manipulating connection strings with placeholder replacement and path normalization.
    /// </summary>
    public static class ConnectionStringProcessingHelper
    {
        /// <summary>
        /// Replaces placeholders in a connection string based on the provided parameters.
        /// </summary>
        /// <param name="dataSourceDriver">The driver configuration for the data source.</param>
        /// <param name="connectionProp">The connection properties.</param>
        /// <param name="dmeEditor">The DME editor.</param>
        /// <returns>The processed connection string with placeholders replaced.</returns>
        public static string ReplaceValueFromConnectionString(ConnectionDriversConfig dataSourceDriver, IConnectionProperties connectionProp, IDMEEditor dmeEditor)
        {
            if (dataSourceDriver == null || connectionProp == null || dmeEditor == null)
                return null;

            // Initialize connection string from driver if not present
            InitializeConnectionString(dataSourceDriver, connectionProp);

            // Determine the input string to process
            string input = DetermineInputString(connectionProp);
            if (string.IsNullOrEmpty(input))
                return null;

            // Process relative paths if present
            input = ProcessRelativePaths(input, connectionProp, dmeEditor);

            // Create replacement dictionary
            var replacements = CreateReplacementDictionary(connectionProp);

            // Process file paths if needed
            ProcessFilePaths(connectionProp, dmeEditor, replacements, ref input);

            // Apply all placeholder replacements
            return ApplyReplacements(input, replacements);
        }

        /// <summary>
        /// Initializes the connection string from the driver configuration if not already present.
        /// </summary>
        /// <param name="dataSourceDriver">The driver configuration.</param>
        /// <param name="connectionProp">The connection properties.</param>
        private static void InitializeConnectionString(ConnectionDriversConfig dataSourceDriver, IConnectionProperties connectionProp)
        {
            if (string.IsNullOrWhiteSpace(connectionProp.ConnectionString) && 
                !string.IsNullOrEmpty(dataSourceDriver.ConnectionString))
            {
                connectionProp.ConnectionString = dataSourceDriver.ConnectionString;
            }
        }

        /// <summary>
        /// Determines the input string to process based on available connection properties.
        /// </summary>
        /// <param name="connectionProp">The connection properties.</param>
        /// <returns>The input string to process.</returns>
        private static string DetermineInputString(IConnectionProperties connectionProp)
        {
            // Priority: ConnectionString > Url > File path
            if (!string.IsNullOrWhiteSpace(connectionProp.ConnectionString))
                return connectionProp.ConnectionString;

            if (!string.IsNullOrWhiteSpace(connectionProp.Url))
                return connectionProp.Url;

            if (HasFileProperties(connectionProp))
                return Path.Combine(connectionProp.FilePath ?? "", connectionProp.FileName ?? "");

            return string.Empty;
        }

        /// <summary>
        /// Checks if the connection properties have file-related properties.
        /// </summary>
        /// <param name="connectionProp">The connection properties.</param>
        /// <returns>True if file properties are present.</returns>
        private static bool HasFileProperties(IConnectionProperties connectionProp)
        {
            return !string.IsNullOrWhiteSpace(connectionProp.FilePath) || 
                   !string.IsNullOrWhiteSpace(connectionProp.FileName);
        }

        /// <summary>
        /// Processes relative paths in the input string.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <param name="connectionProp">The connection properties.</param>
        /// <param name="dmeEditor">The DME editor.</param>
        /// <returns>The processed input string.</returns>
        private static string ProcessRelativePaths(string input, IConnectionProperties connectionProp, IDMEEditor dmeEditor)
        {
            if (input.Contains("./"))
            {
                string fullPath = NormalizePath(input, dmeEditor.ConfigEditor.ExePath);
                connectionProp.FilePath = fullPath;
                return fullPath;
            }
            return input;
        }

        /// <summary>
        /// Creates a dictionary of placeholder replacements.
        /// </summary>
        /// <param name="connectionProp">The connection properties.</param>
        /// <returns>A dictionary of placeholder to value mappings.</returns>
        private static Dictionary<string, string> CreateReplacementDictionary(IConnectionProperties connectionProp)
        {
            return new Dictionary<string, string>
            {
                {"{Url}", connectionProp.Url ?? string.Empty},
                {"{Host}", connectionProp.Host ?? string.Empty},
                {"{UserID}", connectionProp.UserID ?? string.Empty},
                {"{Password}", connectionProp.Password ?? string.Empty},
                {"{DataBase}", connectionProp.Database ?? string.Empty},
                {"{Port}", connectionProp.Port.ToString() ?? string.Empty},
                {"{ApiKey}", connectionProp.ApiKey ?? string.Empty},
                {"{ConnectionName}", connectionProp.ConnectionName ?? string.Empty}
            };
        }

        /// <summary>
        /// Processes file paths and updates the replacement dictionary accordingly.
        /// </summary>
        /// <param name="connectionProp">The connection properties.</param>
        /// <param name="dmeEditor">The DME editor.</param>
        /// <param name="replacements">The replacement dictionary to update.</param>
        /// <param name="input">The input string to potentially modify.</param>
        private static void ProcessFilePaths(IConnectionProperties connectionProp, IDMEEditor dmeEditor, 
            Dictionary<string, string> replacements, ref string input)
        {
            if (!HasFileProperties(connectionProp))
                return;

            NormalizeFilePath(connectionProp, dmeEditor.ConfigEditor.ExePath);

            string fullFilePath = Path.Combine(connectionProp.FilePath ?? "", connectionProp.FileName ?? "");
            replacements["{File}"] = fullFilePath;
            replacements["{FilePath}"] = connectionProp.FilePath ?? string.Empty;
            replacements["{FileName}"] = connectionProp.FileName ?? string.Empty;

            // If no connection string was provided, use the file path as input
            if (string.IsNullOrWhiteSpace(connectionProp.ConnectionString))
            {
                input = fullFilePath;
            }
        }

        /// <summary>
        /// Applies all placeholder replacements to the input string.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <param name="replacements">The replacement dictionary.</param>
        /// <returns>The string with all placeholders replaced.</returns>
        private static string ApplyReplacements(string input, Dictionary<string, string> replacements)
        {
            string result = input;
            foreach (var replacement in replacements)
            {
                result = Regex.Replace(result, Regex.Escape(replacement.Key), replacement.Value, RegexOptions.IgnoreCase);
            }
            return result;
        }

        /// <summary>
        /// Normalizes a relative path to an absolute path.
        /// </summary>
        /// <param name="relativePath">The relative path to normalize.</param>
        /// <param name="basePath">The base path to use for normalization.</param>
        /// <returns>The normalized absolute path.</returns>
        public static string NormalizePath(string relativePath, string basePath)
        {
            if (string.IsNullOrEmpty(relativePath) || string.IsNullOrEmpty(basePath))
                return relativePath;

            string path = relativePath.Replace("./Beep", basePath);
            return path.Replace('/', '\\');
        }

        /// <summary>
        /// Normalizes the file path in connection properties.
        /// </summary>
        /// <param name="connectionProp">The connection properties to normalize.</param>
        /// <param name="basePath">The base path to use for normalization.</param>
        public static void NormalizeFilePath(IConnectionProperties connectionProp, string basePath)
        {
            if (connectionProp == null || string.IsNullOrEmpty(connectionProp.FilePath))
                return;

            if (connectionProp.FilePath.StartsWith(".") ||
                connectionProp.FilePath.Equals("/") ||
                connectionProp.FilePath.Equals("\\"))
            {
                string fullPath = Path.Combine(basePath, connectionProp.FilePath.TrimStart('.', '/', '\\'));
                connectionProp.FilePath = Path.GetFullPath(fullPath);
            }
        }

        /// <summary>
        /// Validates that all required placeholders in a connection string template have corresponding values.
        /// </summary>
        /// <param name="connectionStringTemplate">The connection string template.</param>
        /// <param name="connectionProp">The connection properties.</param>
        /// <returns>A list of missing required placeholders.</returns>
        public static List<string> ValidateRequiredPlaceholders(string connectionStringTemplate, IConnectionProperties connectionProp)
        {
            var missingPlaceholders = new List<string>();
            
            if (string.IsNullOrEmpty(connectionStringTemplate))
                return missingPlaceholders;

            var placeholderPattern = @"\{(\w+)\}";
            var matches = Regex.Matches(connectionStringTemplate, placeholderPattern);

            foreach (Match match in matches)
            {
                string placeholder = match.Groups[1].Value.ToLowerInvariant();
                bool hasValue = placeholder switch
                {
                    "url" => !string.IsNullOrEmpty(connectionProp.Url),
                    "host" => !string.IsNullOrEmpty(connectionProp.Host),
                    "userid" => !string.IsNullOrEmpty(connectionProp.UserID),
                    "password" => !string.IsNullOrEmpty(connectionProp.Password),
                    "database" => !string.IsNullOrEmpty(connectionProp.Database),
                    "port" => connectionProp.Port > 0,
                    "apikey" => !string.IsNullOrEmpty(connectionProp.ApiKey),
                    "file" => !string.IsNullOrEmpty(connectionProp.FileName),
                    "filepath" => !string.IsNullOrEmpty(connectionProp.FilePath),
                    "filename" => !string.IsNullOrEmpty(connectionProp.FileName),
                    "connectionname" => !string.IsNullOrEmpty(connectionProp.ConnectionName),
                    _ => true // Unknown placeholders are considered optional
                };

                if (!hasValue)
                {
                    missingPlaceholders.Add(match.Value);
                }
            }

            return missingPlaceholders;
        }

        /// <summary>
        /// Extracts all placeholders from a connection string template.
        /// </summary>
        /// <param name="connectionStringTemplate">The connection string template.</param>
        /// <returns>A list of all placeholders found in the template.</returns>
        public static List<string> ExtractPlaceholders(string connectionStringTemplate)
        {
            var placeholders = new List<string>();
            
            if (string.IsNullOrEmpty(connectionStringTemplate))
                return placeholders;

            var placeholderPattern = @"\{(\w+)\}";
            var matches = Regex.Matches(connectionStringTemplate, placeholderPattern);

            foreach (Match match in matches)
            {
                if (!placeholders.Contains(match.Value))
                {
                    placeholders.Add(match.Value);
                }
            }

            return placeholders;
        }
    }
}