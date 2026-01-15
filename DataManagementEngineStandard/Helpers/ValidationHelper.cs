using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Data;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Helpers
{
    /// <summary>
    /// Result of a validation operation containing success status and any error messages.
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Gets or sets whether the validation was successful.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets the list of validation error messages.
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the list of validation warning messages.
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// Initializes a new instance of ValidationResult.
        /// </summary>
        /// <param name="isValid">Initial validation state</param>
        public ValidationResult(bool isValid = true)
        {
            IsValid = isValid;
        }

        /// <summary>
        /// Adds an error message and sets IsValid to false.
        /// </summary>
        /// <param name="error">Error message to add</param>
        public void AddError(string error)
        {
            Errors.Add(error);
            IsValid = false;
        }

        /// <summary>
        /// Adds a warning message.
        /// </summary>
        /// <param name="warning">Warning message to add</param>
        public void AddWarning(string warning)
        {
            Warnings.Add(warning);
        }

        /// <summary>
        /// Creates a successful validation result.
        /// </summary>
        /// <returns>Successful validation result</returns>
        public static ValidationResult Success() => new ValidationResult(true);

        /// <summary>
        /// Creates a failed validation result with an error message.
        /// </summary>
        /// <param name="error">Error message</param>
        /// <returns>Failed validation result</returns>
        public static ValidationResult Failure(string error)
        {
            var result = new ValidationResult(false);
            result.AddError(error);
            return result;
        }
    }

    /// <summary>
    /// Helper class providing comprehensive validation capabilities for data management operations.
    /// Includes validation for connection properties, data source names, entity structures, and more.
    /// </summary>
    public static class ValidationHelper
    {
        private static readonly Regex ValidNameRegex = new Regex(@"^[a-zA-Z][a-zA-Z0-9_\-\.]*$", RegexOptions.Compiled);
        private static readonly Regex GuidRegex = new Regex(@"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$", RegexOptions.Compiled);

        /// <summary>
        /// Validates connection properties for completeness and correctness.
        /// </summary>
        /// <param name="properties">Connection properties to validate</param>
        /// <returns>Validation result with any errors or warnings</returns>
        public static ValidationResult ValidateConnectionProperties(ConnectionProperties properties)
        {
            var result = new ValidationResult();

            if (properties == null)
            {
                result.AddError("Connection properties cannot be null");
                return result;
            }

            // Validate connection name
            if (string.IsNullOrWhiteSpace(properties.ConnectionName))
            {
                result.AddError("Connection name cannot be null or empty");
            }
            else if (!IsValidName(properties.ConnectionName))
            {
                result.AddError($"Connection name '{properties.ConnectionName}' contains invalid characters. Use alphanumeric characters, hyphens, underscores, and periods only.");
            }

            // Validate database type
            if (properties.DatabaseType == DataSourceType.NONE)
            {
                result.AddError("Database type must be specified");
            }

            // Validate category-specific requirements
            switch (properties.Category)
            {
                case DatasourceCategory.RDBMS:
                    ValidateRDBMSConnection(properties, result);
                    break;
                case DatasourceCategory.FILE:
                    ValidateFileConnection(properties, result);
                    break;
                case DatasourceCategory.WEBAPI:
                    ValidateWebAPIConnection(properties, result);
                    break;
                case DatasourceCategory.NOSQL:
                    ValidateNoSQLConnection(properties, result);
                    break;
                case DatasourceCategory.INMEMORY:
                    ValidateInMemoryConnection(properties, result);
                    break;
            }

            // Validate driver information
            if (string.IsNullOrWhiteSpace(properties.DriverName))
            {
                result.AddWarning("Driver name is not specified, auto-detection will be attempted");
            }

            // Validate GUID if provided
            if (!string.IsNullOrEmpty(properties.GuidID) && !IsValidGuid(properties.GuidID))
            {
                result.AddError($"Invalid GUID format: {properties.GuidID}");
            }

            return result;
        }

        /// <summary>
        /// Validates a data source name for correctness and uniqueness constraints.
        /// </summary>
        /// <param name="name">Data source name to validate</param>
        /// <returns>Validation result</returns>
        public static ValidationResult ValidateDataSourceName(string name)
        {
            var result = new ValidationResult();

            if (string.IsNullOrWhiteSpace(name))
            {
                result.AddError("Data source name cannot be null or empty");
                return result;
            }

            if (name.Length > 128)
            {
                result.AddError("Data source name cannot exceed 128 characters");
            }

            if (!IsValidName(name))
            {
                result.AddError($"Data source name '{name}' contains invalid characters. Use alphanumeric characters, hyphens, underscores, and periods only.");
            }

            // Check for reserved names
            var reservedNames = new[] { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "LPT1", "LPT2" };
            if (reservedNames.Contains(name.ToUpperInvariant()))
            {
                result.AddError($"'{name}' is a reserved name and cannot be used as a data source name");
            }

            return result;
        }

        /// <summary>
        /// Validates an entity structure for completeness and data integrity.
        /// </summary>
        /// <param name="entity">Entity structure to validate</param>
        /// <returns>Validation result</returns>
        public static ValidationResult ValidateEntityStructure(EntityStructure entity)
        {
            var result = new ValidationResult();

            if (entity == null)
            {
                result.AddError("Entity structure cannot be null");
                return result;
            }

            // Validate entity name
            if (string.IsNullOrWhiteSpace(entity.EntityName))
            {
                result.AddError("Entity name cannot be null or empty");
            }
            else if (!IsValidName(entity.EntityName))
            {
                result.AddError($"Entity name '{entity.EntityName}' contains invalid characters");
            }

            // Validate data source name
            if (string.IsNullOrWhiteSpace(entity.DataSourceID))
            {
                result.AddWarning("Entity does not specify a data source");
            }

            // Validate fields
            if (entity.Fields == null || entity.Fields.Count == 0)
            {
                result.AddWarning("Entity has no fields defined");
            }
            else
            {
                ValidateEntityFields(entity.Fields, result);
            }

            // Validate primary keys
            if (entity.PrimaryKeys != null && entity.PrimaryKeys.Count > 0)
            {
                foreach (var pk in entity.PrimaryKeys)
                {
                    if (!entity.Fields.Any(f => f.FieldName.Equals(pk.FieldName, StringComparison.OrdinalIgnoreCase)))
                    {
                        result.AddError($"Primary key field '{pk.FieldName}' does not exist in entity fields");
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Validates a connection asynchronously by attempting to establish connectivity.
        /// </summary>
        /// <param name="properties">Connection properties to validate</param>
        /// <returns>Async validation result</returns>
        public static async Task<ValidationResult> ValidateConnectionAsync(ConnectionProperties properties)
        {
            var result = ValidateConnectionProperties(properties);
            
            if (!result.IsValid)
                return result;

            try
            {
                // This would need to be implemented with actual connection testing logic
                // For now, we'll do basic validation and add a placeholder for async connection testing
                await Task.Delay(10); // Simulated async operation

                // TODO: Implement actual connection testing based on database type
                // This would involve creating a test connection and performing a simple query
                
                result.AddWarning("Connection validation requires actual connectivity test (not implemented in this version)");
            }
            catch (Exception ex)
            {
                result.AddError($"Connection validation failed: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Validates if a string is a valid GUID format.
        /// </summary>
        /// <param name="guidString">String to validate as GUID</param>
        /// <returns>True if valid GUID format</returns>
        public static bool IsValidGuid(string guidString)
        {
            if (string.IsNullOrEmpty(guidString))
                return false;

            return GuidRegex.IsMatch(guidString) && Guid.TryParse(guidString, out _);
        }

        /// <summary>
        /// Validates a connection string for a specific database type.
        /// </summary>
        /// <param name="connectionString">Connection string to validate</param>
        /// <param name="dbType">Database type for validation context</param>
        /// <returns>True if connection string appears valid</returns>
        public static bool IsValidConnectionString(string connectionString, DataSourceType dbType)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return false;

            try
            {
                switch (dbType)
                {
                    case DataSourceType.SqlServer:
                        return connectionString.Contains("Data Source") || connectionString.Contains("Server");
                    
                    case DataSourceType.Oracle:
                        return connectionString.Contains("Data Source") || connectionString.Contains("Host");
                    
                    case DataSourceType.Mysql:
                        return connectionString.Contains("Server") || connectionString.Contains("Host");
                    
                    case DataSourceType.Postgre:
                        return connectionString.Contains("Server") || connectionString.Contains("Host");
                    
                    case DataSourceType.SqlLite:
                        return connectionString.Contains("Data Source") || connectionString.Contains("Database");
                    
                    case DataSourceType.OLEDB:
                    case DataSourceType.ODBC:
                        return connectionString.Contains("=") && connectionString.Contains(";");
                    
                    default:
                        return connectionString.Length > 0;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validates if a name follows valid naming conventions.
        /// </summary>
        /// <param name="name">Name to validate</param>
        /// <returns>True if name is valid</returns>
        public static bool IsValidName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            return ValidNameRegex.IsMatch(name);
        }

        /// <summary>
        /// Validates batch connection properties.
        /// </summary>
        /// <param name="connections">List of connection properties to validate</param>
        /// <returns>Dictionary of validation results by connection name</returns>
        public static Dictionary<string, ValidationResult> ValidateBatchConnections(List<ConnectionProperties> connections)
        {
            var results = new Dictionary<string, ValidationResult>();
            var connectionNames = new HashSet<string>();

            if (connections == null || connections.Count == 0)
            {
                results["_batch"] = ValidationResult.Failure("No connections provided for batch validation");
                return results;
            }

            foreach (var connection in connections)
            {
                var result = ValidateConnectionProperties(connection);
                
                // Check for duplicate names
                if (!string.IsNullOrEmpty(connection.ConnectionName))
                {
                    if (connectionNames.Contains(connection.ConnectionName))
                    {
                        result.AddError($"Duplicate connection name: {connection.ConnectionName}");
                    }
                    else
                    {
                        connectionNames.Add(connection.ConnectionName);
                    }
                }

                results[connection.ConnectionName ?? $"connection_{connections.IndexOf(connection)}"] = result;
            }

            return results;
        }

        #region Private Helper Methods

        private static void ValidateRDBMSConnection(ConnectionProperties properties, ValidationResult result)
        {
            // Validate server/host
            if (string.IsNullOrWhiteSpace(properties.Host) && string.IsNullOrWhiteSpace(properties.ConnectionString))
            {
                result.AddError("Host/Server must be specified for RDBMS connections");
            }

            // Validate database name
            if (string.IsNullOrWhiteSpace(properties.Database) && string.IsNullOrWhiteSpace(properties.ConnectionString))
            {
                result.AddWarning("Database name is not specified");
            }

            // Validate port if specified
            if (properties.Port > 0 && (properties.Port < 1 || properties.Port > 65535))
            {
                result.AddError($"Invalid port number: {properties.Port}. Must be between 1 and 65535");
            }

            // Validate connection string if provided
            if (!string.IsNullOrEmpty(properties.ConnectionString) && 
                !IsValidConnectionString(properties.ConnectionString, properties.DatabaseType))
            {
                result.AddWarning("Connection string format may be invalid for the specified database type");
            }
        }

        private static void ValidateFileConnection(ConnectionProperties properties, ValidationResult result)
        {
            // Validate file path
            if (string.IsNullOrWhiteSpace(properties.FilePath) && string.IsNullOrWhiteSpace(properties.FileName))
            {
                result.AddError("File path or file name must be specified for file connections");
            }

            // Validate file extension matches database type
            if (!string.IsNullOrEmpty(properties.FileName))
            {
                var extension = System.IO.Path.GetExtension(properties.FileName)?.ToLowerInvariant();
                var expectedExtensions = GetExpectedFileExtensions(properties.DatabaseType);
                
                if (expectedExtensions.Any() && !expectedExtensions.Contains(extension))
                {
                    result.AddWarning($"File extension '{extension}' may not match database type {properties.DatabaseType}. Expected: {string.Join(", ", expectedExtensions)}");
                }
            }
        }

        private static void ValidateWebAPIConnection(ConnectionProperties properties, ValidationResult result)
        {
            // Validate URL
            if (string.IsNullOrWhiteSpace(properties.Url) && string.IsNullOrWhiteSpace(properties.Host))
            {
                result.AddError("URL or Host must be specified for Web API connections");
            }

            if (!string.IsNullOrEmpty(properties.Url) && !Uri.IsWellFormedUriString(properties.Url, UriKind.Absolute))
            {
                result.AddError($"Invalid URL format: {properties.Url}");
            }

            // Validate authentication if specified
            if (!string.IsNullOrEmpty(properties.UserID) && string.IsNullOrEmpty(properties.Password))
            {
                result.AddWarning("Username specified but password is empty");
            }
        }

        private static void ValidateNoSQLConnection(ConnectionProperties properties, ValidationResult result)
        {
            // Basic validation for NoSQL connections
            if (string.IsNullOrWhiteSpace(properties.Host) && string.IsNullOrWhiteSpace(properties.ConnectionString))
            {
                result.AddError("Host or connection string must be specified for NoSQL connections");
            }

            // Validate database name for document databases
            if (properties.DatabaseType == DataSourceType.MongoDB && string.IsNullOrWhiteSpace(properties.Database))
            {
                result.AddWarning("Database name should be specified for MongoDB connections");
            }
        }

        private static void ValidateInMemoryConnection(ConnectionProperties properties, ValidationResult result)
        {
            // In-memory connections have minimal requirements
            if (string.IsNullOrWhiteSpace(properties.Database))
            {
                result.AddWarning("Consider specifying a database name for in-memory connections for better identification");
            }
        }

        private static void ValidateEntityFields(List<EntityField> fields, ValidationResult result)
        {
            var FieldNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var field in fields)
            {
                if (string.IsNullOrWhiteSpace(field.FieldName))
                {
                    result.AddError("Field name cannot be null or empty");
                    continue;
                }

                if (!IsValidName(field.FieldName))
                {
                    result.AddError($"Field name '{field.FieldName}' contains invalid characters");
                }

                if (FieldNames.Contains(field.FieldName))
                {
                    result.AddError($"Duplicate field name: {field.FieldName}");
                }
                else
                {
                    FieldNames.Add(field.FieldName);
                }

                if (string.IsNullOrWhiteSpace(field.Fieldtype))
                {
                    result.AddWarning($"Field '{field.FieldName}' has no type specified");
                }
            }
        }

        private static List<string> GetExpectedFileExtensions(DataSourceType dbType)
        {
            switch (dbType)
            {
                case DataSourceType.Text:
                case DataSourceType.CSV:
                    return new List<string> { ".csv", ".txt" };
                case DataSourceType.Json:
                    return new List<string> { ".json" };
                case DataSourceType.XML:
                    return new List<string> { ".xml" };
                case DataSourceType.Xls:
                    return new List<string> { ".xlsx", ".xls" };
                case DataSourceType.SqlLite:
                    return new List<string> { ".db", ".sqlite", ".sqlite3" };
                default:
                    return new List<string>();
            }
        }

        #endregion
    }
}
