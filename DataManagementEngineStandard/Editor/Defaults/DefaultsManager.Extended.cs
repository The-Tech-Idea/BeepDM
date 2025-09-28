using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Editor.Defaults
{
    /// <summary>
    /// Extended functionality for DefaultsManager
    /// </summary>
    public partial class DefaultsManager
    {
        #region Entity Column Defaults

        /// <summary>
        /// Sets a default value for a specific column in an entity
        /// </summary>
        /// <param name="editor">The DME Editor instance</param>
        /// <param name="dataSourceName">Name of the data source</param>
        /// <param name="entityName">Name of the entity/table</param>
        /// <param name="columnName">Name of the column</param>
        /// <param name="defaultValue">Default value or rule</param>
        /// <param name="isRule">True if defaultValue is a rule, false if static value</param>
        /// <returns>Operation result</returns>
        public static IErrorsInfo SetColumnDefault(IDMEEditor editor, string dataSourceName, string entityName, 
            string columnName, string defaultValue, bool isRule = false)
        {
            EnsureInitialized(editor);

            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(dataSourceName))
                    return CreateError("Data source name cannot be empty");
                
                if (string.IsNullOrWhiteSpace(entityName))
                    return CreateError("Entity name cannot be empty");
                
                if (string.IsNullOrWhiteSpace(columnName))
                    return CreateError("Column name cannot be empty");

                // Create field identifier for entity column
                var fieldName = $"{entityName}.{columnName}";
                
                // Create default value
                var (validation, defaultValueObj) = CreateDefaultValue(editor, fieldName, 
                    isRule ? null : defaultValue, 
                    isRule ? defaultValue : null);

                if (validation.Flag == Errors.Failed)
                    return validation;

                // Get existing defaults
                var defaults = GetDefaults(editor, dataSourceName);
                
                // Remove existing default for this field if it exists
                defaults.RemoveAll(d => d.PropertyName.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
                
                // Add new default
                defaults.Add(defaultValueObj);

                // Save defaults
                return SaveDefaults(editor, defaults, dataSourceName);
            }
            catch (Exception ex)
            {
                return CreateError($"Error setting column default: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the default value for a specific column in an entity
        /// </summary>
        /// <param name="editor">The DME Editor instance</param>
        /// <param name="dataSourceName">Name of the data source</param>
        /// <param name="entityName">Name of the entity/table</param>
        /// <param name="columnName">Name of the column</param>
        /// <param name="parameters">Optional parameters for rule resolution</param>
        /// <returns>Resolved default value</returns>
        public static object GetColumnDefault(IDMEEditor editor, string dataSourceName, string entityName, 
            string columnName, IPassedArgs parameters = null)
        {
            EnsureInitialized(editor);

            var fieldName = $"{entityName}.{columnName}";
            return ResolveDefaultValue(editor, dataSourceName, fieldName, parameters);
        }

        /// <summary>
        /// Removes the default value for a specific column in an entity
        /// </summary>
        /// <param name="editor">The DME Editor instance</param>
        /// <param name="dataSourceName">Name of the data source</param>
        /// <param name="entityName">Name of the entity/table</param>
        /// <param name="columnName">Name of the column</param>
        /// <returns>Operation result</returns>
        public static IErrorsInfo RemoveColumnDefault(IDMEEditor editor, string dataSourceName, string entityName, string columnName)
        {
            EnsureInitialized(editor);

            try
            {
                var fieldName = $"{entityName}.{columnName}";
                var defaults = GetDefaults(editor, dataSourceName);
                
                var removed = defaults.RemoveAll(d => d.PropertyName.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
                
                if (removed > 0)
                {
                    var result = SaveDefaults(editor, defaults, dataSourceName);
                    if (result.Flag == Errors.Ok)
                    {
                        result.Message = $"Removed default for {entityName}.{columnName}";
                    }
                    return result;
                }
                else
                {
                    return new ErrorsInfo
                    {
                        Flag = Errors.Ok,
                        Message = $"No default found for {entityName}.{columnName}"
                    };
                }
            }
            catch (Exception ex)
            {
                return CreateError($"Error removing column default: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets all column defaults for a specific entity
        /// </summary>
        /// <param name="editor">The DME Editor instance</param>
        /// <param name="dataSourceName">Name of the data source</param>
        /// <param name="entityName">Name of the entity/table</param>
        /// <returns>Dictionary of column names and their default values</returns>
        public static Dictionary<string, DefaultValue> GetEntityDefaults(IDMEEditor editor, string dataSourceName, string entityName)
        {
            EnsureInitialized(editor);

            var defaults = GetDefaults(editor, dataSourceName);
            var entityPrefix = $"{entityName}.";
            
            return defaults
                .Where(d => d.PropertyName.StartsWith(entityPrefix, StringComparison.OrdinalIgnoreCase))
                .ToDictionary(
                    d => d.PropertyName.Substring(entityPrefix.Length),
                    d => d,
                    StringComparer.OrdinalIgnoreCase
                );
        }

        #endregion

        #region Bulk Operations

        /// <summary>
        /// Sets multiple column defaults at once
        /// </summary>
        /// <param name="editor">The DME Editor instance</param>
        /// <param name="dataSourceName">Name of the data source</param>
        /// <param name="entityName">Name of the entity/table</param>
        /// <param name="columnDefaults">Dictionary of column names and their default configurations</param>
        /// <returns>Operation result with details of successes and failures</returns>
        public static IErrorsInfo SetMultipleColumnDefaults(IDMEEditor editor, string dataSourceName, string entityName, 
            Dictionary<string, (string value, bool isRule)> columnDefaults)
        {
            EnsureInitialized(editor);

            try
            {
                if (columnDefaults == null || !columnDefaults.Any())
                    return CreateError("No column defaults provided");

                var defaults = GetDefaults(editor, dataSourceName);
                var entityPrefix = $"{entityName}.";
                var errors = new List<string>();
                var successes = new List<string>();

                // Remove existing defaults for this entity
                defaults.RemoveAll(d => d.PropertyName.StartsWith(entityPrefix, StringComparison.OrdinalIgnoreCase));

                // Add new defaults
                foreach (var kvp in columnDefaults)
                {
                    try
                    {
                        var fieldName = $"{entityName}.{kvp.Key}";
                        var (validation, defaultValue) = CreateDefaultValue(editor, fieldName,
                            kvp.Value.isRule ? null : kvp.Value.value,
                            kvp.Value.isRule ? kvp.Value.value : null);

                        if (validation.Flag == Errors.Ok)
                        {
                            defaults.Add(defaultValue);
                            successes.Add(kvp.Key);
                        }
                        else
                        {
                            errors.Add($"{kvp.Key}: {validation.Message}");
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"{kvp.Key}: {ex.Message}");
                    }
                }

                // Save all changes
                var saveResult = SaveDefaults(editor, defaults, dataSourceName);
                if (saveResult.Flag == Errors.Failed)
                {
                    return saveResult;
                }

                // Prepare result message
                var message = $"Processed {columnDefaults.Count} column defaults for {entityName}. ";
                message += $"Successes: {successes.Count}";
                if (errors.Any())
                {
                    message += $", Failures: {errors.Count} ({string.Join(", ", errors)})";
                }

                return new ErrorsInfo
                {
                    Flag = errors.Any() ? Errors.Failed : Errors.Ok,
                    Message = message
                };
            }
            catch (Exception ex)
            {
                return CreateError($"Error setting multiple column defaults: {ex.Message}");
            }
        }

        /// <summary>
        /// Applies default values to a data record
        /// </summary>
        /// <param name="editor">The DME Editor instance</param>
        /// <param name="dataSourceName">Name of the data source</param>
        /// <param name="entityName">Name of the entity</param>
        /// <param name="record">Record to apply defaults to</param>
        /// <param name="parameters">Optional parameters for rule resolution</param>
        /// <returns>Updated record with default values applied</returns>
        public static object ApplyDefaultsToRecord(IDMEEditor editor, string dataSourceName, string entityName, 
            object record, IPassedArgs parameters = null)
        {
            EnsureInitialized(editor);

            try
            {
                if (record == null)
                    return null;

                var entityDefaults = GetEntityDefaults(editor, dataSourceName, entityName);
                if (!entityDefaults.Any())
                    return record;

                var recordType = record.GetType();

                foreach (var columnDefault in entityDefaults)
                {
                    var property = recordType.GetProperty(columnDefault.Key);
                    if (property != null && property.CanWrite)
                    {
                        // Check if property is null or has default value
                        var currentValue = property.GetValue(record);
                        if (IsDefaultValue(currentValue, property.PropertyType))
                        {
                            // Apply default value
                            var defaultValue = ResolveDefaultValue(editor, columnDefault.Value, parameters);
                            if (defaultValue != null)
                            {
                                try
                                {
                                    // Convert value to property type if needed
                                    var convertedValue = Convert.ChangeType(defaultValue, property.PropertyType);
                                    property.SetValue(record, convertedValue);
                                }
                                catch (Exception ex)
                                {
                                    _logger?.WriteLog($"DefaultsManager: Error setting default value for {columnDefault.Key}: {ex.Message}");
                                }
                            }
                        }
                    }
                }

                return record;
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"DefaultsManager: Error applying defaults to record: {ex.Message}");
                return record;
            }
        }

        #endregion

        #region Template and Import/Export

        /// <summary>
        /// Creates a template for common default value patterns
        /// </summary>
        /// <param name="editor">The DME Editor instance</param>
        /// <param name="templateType">Type of template to create</param>
        /// <returns>List of default value templates</returns>
        public static List<DefaultValue> CreateDefaultValueTemplate(IDMEEditor editor, DefaultValueTemplateType templateType)
        {
            EnsureInitialized(editor);

            var templates = new List<DefaultValue>();

            switch (templateType)
            {
                case DefaultValueTemplateType.AuditFields:
                    templates.AddRange(new[]
                    {
                        _defaultValueHelper.CreateDefaultValue("CreatedBy", null, "USERNAME"),
                        _defaultValueHelper.CreateDefaultValue("CreatedDate", null, "NOW"),
                        _defaultValueHelper.CreateDefaultValue("ModifiedBy", null, "USERNAME"),
                        _defaultValueHelper.CreateDefaultValue("ModifiedDate", null, "NOW")
                    });
                    break;

                case DefaultValueTemplateType.SystemFields:
                    templates.AddRange(new[]
                    {
                        _defaultValueHelper.CreateDefaultValue("ID", null, "NEWGUID"),
                        _defaultValueHelper.CreateDefaultValue("Version", null, "APPVERSION"),
                        _defaultValueHelper.CreateDefaultValue("MachineName", null, "MACHINENAME")
                    });
                    break;

                case DefaultValueTemplateType.CommonDefaults:
                    templates.AddRange(new[]
                    {
                        _defaultValueHelper.CreateDefaultValue("IsActive", "true"),
                        _defaultValueHelper.CreateDefaultValue("SortOrder", "0"),
                        _defaultValueHelper.CreateDefaultValue("Status", "Active")
                    });
                    break;
            }

            return templates;
        }

        /// <summary>
        /// Exports defaults configuration for a data source to JSON
        /// </summary>
        /// <param name="editor">The DME Editor instance</param>
        /// <param name="dataSourceName">Name of the data source</param>
        /// <returns>JSON representation of defaults or null if error</returns>
        public static string ExportDefaults(IDMEEditor editor, string dataSourceName)
        {
            EnsureInitialized(editor);

            try
            {
                var defaults = GetDefaults(editor, dataSourceName);
                
                if (defaults == null || !defaults.Any())
                {
                    _logger?.WriteLog($"DefaultsManager: No defaults found for '{dataSourceName}'");
                    return "[]";
                }

                var json = System.Text.Json.JsonSerializer.Serialize(defaults, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });

                _logger?.WriteLog($"DefaultsManager: Exported {defaults.Count} defaults for '{dataSourceName}'");
                return json;
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"DefaultsManager: Error exporting defaults for '{dataSourceName}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Exports defaults configuration with enhanced result information
        /// </summary>
        /// <param name="editor">The DME Editor instance</param>
        /// <param name="dataSourceName">Name of the data source</param>
        /// <returns>Tuple with operation result and JSON data</returns>
        public static (IErrorsInfo result, string json) ExportDefaultsWithResult(IDMEEditor editor, string dataSourceName)
        {
            EnsureInitialized(editor);

            try
            {
                var defaults = GetDefaults(editor, dataSourceName);
                
                if (defaults == null || !defaults.Any())
                {
                    return (new ErrorsInfo
                    {
                        Flag = Errors.Ok,
                        Message = $"No defaults found for '{dataSourceName}'"
                    }, "[]");
                }

                var json = System.Text.Json.JsonSerializer.Serialize(defaults, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });

                _logger?.WriteLog($"DefaultsManager: Exported {defaults.Count} defaults for '{dataSourceName}'");

                return (new ErrorsInfo
                {
                    Flag = Errors.Ok,
                    Message = $"Successfully exported {defaults.Count} defaults"
                }, json);
            }
            catch (Exception ex)
            {
                var errorMsg = $"Error exporting defaults for '{dataSourceName}': {ex.Message}";
                _logger?.WriteLog($"DefaultsManager: {errorMsg}");
                
                return (new ErrorsInfo
                {
                    Flag = Errors.Failed,
                    Message = errorMsg
                }, string.Empty);
            }
        }

        /// <summary>
        /// Imports default value configurations from a serialized format
        /// </summary>
        /// <param name="editor">The DME Editor instance</param>
        /// <param name="dataSourceName">Name of the data source</param>
        /// <param name="serializedDefaults">Serialized default values configuration</param>
        /// <param name="replaceExisting">Whether to replace existing defaults</param>
        /// <returns>Import operation result</returns>
        public static IErrorsInfo ImportDefaults(IDMEEditor editor, string dataSourceName, string serializedDefaults, bool replaceExisting = false)
        {
            EnsureInitialized(editor);

            try
            {
                var importedDefaults = System.Text.Json.JsonSerializer.Deserialize<List<DefaultValue>>(serializedDefaults);
                
                if (importedDefaults == null || !importedDefaults.Any())
                {
                    return new ErrorsInfo
                    {
                        Flag = Errors.Ok,
                        Message = "No defaults found in JSON"
                    };
                }

                List<DefaultValue> finalDefaults;
                string message;

                if (replaceExisting)
                {
                    finalDefaults = importedDefaults;
                    message = $"Imported {importedDefaults.Count} defaults (replaced existing)";
                }
                else
                {
                    var existingDefaults = GetDefaults(editor, dataSourceName);
                    var existingNames = new HashSet<string>(existingDefaults.Select(d => d.PropertyName), StringComparer.OrdinalIgnoreCase);
                    
                    // Only add new defaults that don't already exist
                    var newDefaults = importedDefaults.Where(d => !existingNames.Contains(d.PropertyName)).ToList();
                    finalDefaults = existingDefaults.Concat(newDefaults).ToList();
                    
                    message = $"Imported {newDefaults.Count} new defaults. {importedDefaults.Count - newDefaults.Count} skipped (already exist).";
                }

                var result = SaveDefaults(editor, finalDefaults, dataSourceName);
                
                if (result.Flag == Errors.Ok)
                {
                    // Force persistence to ensure changes are saved
                    try
                    {
                        editor.ConfigEditor.SaveDataconnectionsValues();
                        _logger?.WriteLog($"DefaultsManager: {message} for '{dataSourceName}'");
                        
                        result.Message = message;
                    }
                    catch (Exception persistEx)
                    {
                        result.Flag = Errors.Failed;
                        result.Message = $"Import succeeded but persistence failed: {persistEx.Message}";
                    }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                var errorMsg = $"Error importing defaults for '{dataSourceName}': {ex.Message}";
                _logger?.WriteLog($"DefaultsManager: {errorMsg}");
                
                return new ErrorsInfo
                {
                    Flag = Errors.Failed,
                    Message = errorMsg
                };
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Checks if a value is considered a default/empty value for its type
        /// </summary>
        /// <param name="value">Value to check</param>
        /// <param name="type">Type of the value</param>
        /// <returns>True if value is considered default/empty</returns>
        private static bool IsDefaultValue(object value, Type type)
        {
            if (value == null)
                return true;

            if (type == typeof(string))
                return string.IsNullOrWhiteSpace((string)value);

            if (type.IsValueType)
                return value.Equals(Activator.CreateInstance(type));

            return false;
        }

        /// <summary>
        /// Creates an error result
        /// </summary>
        /// <param name="message">Error message</param>
        /// <returns>Error information object</returns>
        private static IErrorsInfo CreateError(string message)
        {
            return new ErrorsInfo
            {
                Flag = Errors.Failed,
                Message = message
            };
        }

        #endregion
    }

    /// <summary>
    /// Types of default value templates
    /// </summary>
    public enum DefaultValueTemplateType
    {
        /// <summary>
        /// Audit fields (CreatedBy, CreatedDate, ModifiedBy, ModifiedDate)
        /// </summary>
        AuditFields,
        
        /// <summary>
        /// System fields (ID, Version, MachineName)
        /// </summary>
        SystemFields,
        
        /// <summary>
        /// Common default fields (IsActive, SortOrder, Status)
        /// </summary>
        CommonDefaults
    }
}