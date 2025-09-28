using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Defaults;
using TheTechIdea.Beep.Editor.UOW.Interfaces;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.UOW.Helpers
{
    /// <summary>
    /// Helper class for managing default values in UnitofWork operations
    /// Integrates with DefaultsManager for comprehensive default value handling
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public class UnitofWorkDefaultsHelper<T> : IUnitofWorkDefaults<T> where T : Entity, new()
    {
        #region Private Fields

        private readonly IDMEEditor _editor;
        private readonly string _dataSourceName;
        private readonly string _entityName;
        private readonly Dictionary<string, DefaultValue> _cachedDefaults;
        private readonly object _lockObject = new object();
        private DateTime _lastCacheUpdate = DateTime.MinValue;
        private readonly TimeSpan _cacheTimeout = TimeSpan.FromMinutes(5);

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of UnitofWorkDefaultsHelper
        /// </summary>
        /// <param name="editor">DME Editor instance</param>
        /// <param name="dataSourceName">Data source name</param>
        /// <param name="entityName">Entity name</param>
        public UnitofWorkDefaultsHelper(IDMEEditor editor, string dataSourceName, string entityName)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
            _dataSourceName = dataSourceName;
            _entityName = entityName;
            _cachedDefaults = new Dictionary<string, DefaultValue>(StringComparer.OrdinalIgnoreCase);

            // Initialize DefaultsManager if not already done
            DefaultsManager.Initialize(_editor);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Applies default values to an entity
        /// </summary>
        /// <param name="entity">Entity to apply defaults to</param>
        /// <param name="context">Context for default value resolution</param>
        public void ApplyDefaults(T entity, DefaultValueContext context)
        {
            if (entity == null) return;

            try
            {
                EnsureCacheUpdated();
                
                var entityType = entity.GetType();
                var parameters = CreatePassedArgs(context);

                foreach (var property in entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (!property.CanWrite) continue;

                    var fieldName = $"{_entityName}.{property.Name}";
                    var currentValue = property.GetValue(entity);

                    // Apply defaults only if current value is null or default
                    if (ShouldApplyDefault(currentValue, property.PropertyType, context))
                    {
                        ApplyDefaultToProperty(entity, property, fieldName, parameters);
                    }
                }
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage("UnitofWorkDefaultsHelper", 
                    $"Error applying defaults to entity: {ex.Message}", 
                    DateTime.Now, -1, null, Errors.Failed);
            }
        }

        /// <summary>
        /// Applies default values to an entity asynchronously
        /// </summary>
        /// <param name="entity">Entity to apply defaults to</param>
        /// <param name="context">Context for default value resolution</param>
        /// <returns>Entity with defaults applied</returns>
        public Task<T> ApplyDefaultsAsync(T entity, DefaultValueContext context)
        {
            return Task.Run(() =>
            {
                ApplyDefaults(entity, context);
                return entity;
            });
        }

        /// <summary>
        /// Checks if defaults are configured for a specific field
        /// </summary>
        /// <param name="fieldName">Name of the field to check</param>
        /// <returns>True if defaults exist for the field</returns>
        public bool HasDefaults(string fieldName)
        {
            EnsureCacheUpdated();
            var fullFieldName = fieldName.Contains('.') ? fieldName : $"{_entityName}.{fieldName}";
            return _cachedDefaults.ContainsKey(fullFieldName);
        }

        /// <summary>
        /// Gets the default value configuration for a specific field
        /// </summary>
        /// <param name="fieldName">Name of the field</param>
        /// <returns>Default value configuration or null if not found</returns>
        public DefaultValue GetDefaultForField(string fieldName)
        {
            EnsureCacheUpdated();
            var fullFieldName = fieldName.Contains('.') ? fieldName : $"{_entityName}.{fieldName}";
            return _cachedDefaults.TryGetValue(fullFieldName, out var defaultValue) ? defaultValue : null;
        }

        /// <summary>
        /// Applies defaults for insert operations
        /// </summary>
        /// <param name="entity">Entity being inserted</param>
        public void ApplyInsertDefaults(T entity)
        {
            var context = new DefaultValueContext
            {
                Operation = "Insert",
                DataSourceName = _dataSourceName,
                EntityName = _entityName,
                IsNewEntity = true,
                UserContext = GetCurrentUser()
            };

            ApplyDefaults(entity, context);
        }

        /// <summary>
        /// Applies defaults for update operations
        /// </summary>
        /// <param name="entity">Entity being updated</param>
        public void ApplyUpdateDefaults(T entity)
        {
            var context = new DefaultValueContext
            {
                Operation = "Update",
                DataSourceName = _dataSourceName,
                EntityName = _entityName,
                IsNewEntity = false,
                UserContext = GetCurrentUser()
            };

            // Apply only update-specific defaults (like ModifiedDate, ModifiedBy)
            ApplyConditionalDefaults(entity, context, isUpdate: true);
        }

        /// <summary>
        /// Validates that applied defaults meet entity constraints
        /// </summary>
        /// <param name="entity">Entity with applied defaults</param>
        /// <returns>Validation result</returns>
        public IErrorsInfo ValidateAppliedDefaults(T entity)
        {
            var result = new ErrorsInfo { Flag = Errors.Ok };

            try
            {
                if (entity == null)
                {
                    result.Flag = Errors.Failed;
                    result.Message = "Entity is null";
                    return result;
                }

                var entityType = entity.GetType();
                var validationErrors = new List<string>();

                foreach (var property in entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    var value = property.GetValue(entity);
                    var fieldName = $"{_entityName}.{property.Name}";
                    
                    if (HasDefaults(fieldName))
                    {
                        var validationError = ValidateDefaultValue(property, value);
                        if (!string.IsNullOrEmpty(validationError))
                        {
                            validationErrors.Add($"{property.Name}: {validationError}");
                        }
                    }
                }

                if (validationErrors.Any())
                {
                    result.Flag = Errors.Failed;
                    result.Message = string.Join("; ", validationErrors);
                }
                else
                {
                    result.Message = "All applied defaults are valid";
                }
            }
            catch (Exception ex)
            {
                result.Flag = Errors.Failed;
                result.Message = $"Error validating applied defaults: {ex.Message}";
                result.Ex = ex;
            }

            return result;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Ensures the defaults cache is up to date
        /// </summary>
        private void EnsureCacheUpdated()
        {
            lock (_lockObject)
            {
                if (DateTime.Now - _lastCacheUpdate > _cacheTimeout)
                {
                    RefreshCache();
                }
            }
        }

        /// <summary>
        /// Refreshes the defaults cache from DefaultsManager
        /// </summary>
        private void RefreshCache()
        {
            try
            {
                _cachedDefaults.Clear();
                
                if (!string.IsNullOrEmpty(_dataSourceName))
                {
                    var entityDefaults = DefaultsManager.GetEntityDefaults(_editor, _dataSourceName, _entityName);
                    
                    foreach (var kvp in entityDefaults)
                    {
                        var fullFieldName = $"{_entityName}.{kvp.Key}";
                        _cachedDefaults[fullFieldName] = kvp.Value;
                    }
                }

                _lastCacheUpdate = DateTime.Now;
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage("UnitofWorkDefaultsHelper",
                    $"Error refreshing defaults cache: {ex.Message}",
                    DateTime.Now, -1, null, Errors.Failed);
            }
        }

        /// <summary>
        /// Determines if a default should be applied to a property
        /// </summary>
        /// <param name="currentValue">Current property value</param>
        /// <param name="propertyType">Property type</param>
        /// <param name="context">Default value context</param>
        /// <returns>True if default should be applied</returns>
        private bool ShouldApplyDefault(object currentValue, Type propertyType, DefaultValueContext context)
        {
            // For updates, only apply defaults to audit fields or if explicitly configured
            if (context.Operation == "Update")
            {
                return IsAuditField(context) || IsExplicitlyConfiguredForUpdate(context);
            }

            // For inserts, apply if current value is null or default
            if (currentValue == null) return true;

            if (propertyType == typeof(string))
                return string.IsNullOrWhiteSpace((string)currentValue);

            if (propertyType.IsValueType)
                return currentValue.Equals(Activator.CreateInstance(propertyType));

            return false;
        }

        /// <summary>
        /// Applies default value to a specific property
        /// </summary>
        /// <param name="entity">Entity to apply default to</param>
        /// <param name="property">Property to set</param>
        /// <param name="fieldName">Field name for default lookup</param>
        /// <param name="parameters">Parameters for default resolution</param>
        private void ApplyDefaultToProperty(T entity, PropertyInfo property, string fieldName, IPassedArgs parameters)
        {
            try
            {
                var defaultValue = GetDefaultForField(fieldName);
                if (defaultValue == null) return;

                var resolvedValue = DefaultsManager.ResolveDefaultValue(_editor, defaultValue, parameters);
                if (resolvedValue == null) return;

                // Convert the resolved value to the property type
                var convertedValue = ConvertValueToPropertyType(resolvedValue, property.PropertyType);
                if (convertedValue != null)
                {
                    property.SetValue(entity, convertedValue);
                }
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage("UnitofWorkDefaultsHelper",
                    $"Error applying default to property {property.Name}: {ex.Message}",
                    DateTime.Now, -1, null, Errors.Failed);
            }
        }

        /// <summary>
        /// Applies conditional defaults based on context
        /// </summary>
        /// <param name="entity">Entity to apply defaults to</param>
        /// <param name="context">Default value context</param>
        /// <param name="isUpdate">Whether this is an update operation</param>
        private void ApplyConditionalDefaults(T entity, DefaultValueContext context, bool isUpdate)
        {
            EnsureCacheUpdated();
            var entityType = entity.GetType();
            var parameters = CreatePassedArgs(context);

            // Define update-specific fields that should get defaults
            var updateFields = new[] { "ModifiedDate", "ModifiedBy", "LastModified", "UpdatedAt", "UpdatedBy", "Version" };

            foreach (var property in entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!property.CanWrite) continue;

                var fieldName = $"{_entityName}.{property.Name}";
                
                // For updates, only apply to specific audit fields or explicitly configured fields
                if (isUpdate && !updateFields.Contains(property.Name, StringComparer.OrdinalIgnoreCase))
                {
                    var defaultValue = GetDefaultForField(fieldName);
                    if (defaultValue?.propertyType != DefaultValueType.Rule) continue; // Only rule-based defaults for non-audit fields on update
                }

                ApplyDefaultToProperty(entity, property, fieldName, parameters);
            }
        }

        /// <summary>
        /// Creates PassedArgs for default value resolution
        /// </summary>
        /// <param name="context">Default value context</param>
        /// <returns>PassedArgs instance</returns>
        private IPassedArgs CreatePassedArgs(DefaultValueContext context)
        {
            var args = new PassedArgs
            {
                DataSource = _editor.GetDataSource(_dataSourceName),
                DatasourceName = _dataSourceName,
                CurrentEntity = _entityName,
                ParameterString1 = context.Operation,
                ParameterString2 = context.UserContext,
                ParameterDate1 = context.Timestamp
            };

            // Add context parameters
            if (context.Parameters != null)
            {
                var paramIndex = 1;
                foreach (var kvp in context.Parameters)
                {
                    switch (paramIndex)
                    {
                        case 1: args.ParameterString3 = kvp.Value?.ToString(); break;
                        case 2: args.ParameterInt1 = Convert.ToInt32(kvp.Value ?? 0); break;
                        case 3: args.ParameterInt2 = Convert.ToInt32(kvp.Value ?? 0); break;
                        // Add more as needed
                    }
                    paramIndex++;
                    if (paramIndex > 3) break; // Limit to available parameters
                }
            }

            return args;
        }

        /// <summary>
        /// Converts a value to the target property type
        /// </summary>
        /// <param name="value">Value to convert</param>
        /// <param name="targetType">Target type</param>
        /// <returns>Converted value</returns>
        private object ConvertValueToPropertyType(object value, Type targetType)
        {
            if (value == null) return null;
            if (targetType.IsAssignableFrom(value.GetType())) return value;

            try
            {
                // Handle nullable types
                if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    targetType = Nullable.GetUnderlyingType(targetType);
                }

                // Handle specific type conversions
                if (targetType == typeof(Guid) && value is string stringValue)
                {
                    return Guid.TryParse(stringValue, out var guid) ? guid : Guid.NewGuid();
                }

                return Convert.ChangeType(value, targetType);
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage("UnitofWorkDefaultsHelper",
                    $"Error converting value '{value}' to type '{targetType}': {ex.Message}",
                    DateTime.Now, -1, null, Errors.Failed);
                return null;
            }
        }

        /// <summary>
        /// Validates a default value for a property
        /// </summary>
        /// <param name="property">Property to validate</param>
        /// <param name="value">Value to validate</param>
        /// <returns>Validation error message or null if valid</returns>
        private string ValidateDefaultValue(PropertyInfo property, object value)
        {
            try
            {
                if (value == null && !IsNullableType(property.PropertyType))
                {
                    return "Value cannot be null for non-nullable type";
                }

                // Add more specific validations as needed
                return null;
            }
            catch (Exception ex)
            {
                return $"Validation error: {ex.Message}";
            }
        }

        /// <summary>
        /// Checks if a type is nullable
        /// </summary>
        /// <param name="type">Type to check</param>
        /// <returns>True if type is nullable</returns>
        private bool IsNullableType(Type type)
        {
            return !type.IsValueType || 
                   (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>));
        }

        /// <summary>
        /// Checks if the context indicates an audit field
        /// </summary>
        /// <param name="context">Default value context</param>
        /// <returns>True if this is an audit field context</returns>
        private bool IsAuditField(DefaultValueContext context)
        {
            // This would be expanded based on your audit field conventions
            var auditFields = new[] { "ModifiedDate", "ModifiedBy", "LastModified", "UpdatedAt", "UpdatedBy" };
            return context.Parameters.ContainsKey("FieldName") && 
                   auditFields.Contains(context.Parameters["FieldName"]?.ToString(), StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Checks if field is explicitly configured for update defaults
        /// </summary>
        /// <param name="context">Default value context</param>
        /// <returns>True if configured for update defaults</returns>
        private bool IsExplicitlyConfiguredForUpdate(DefaultValueContext context)
        {
            // This would check your default configuration to see if this field
            // is specifically configured to receive defaults on updates
            return false; // Implement based on your configuration structure
        }

        /// <summary>
        /// Gets the current user context
        /// </summary>
        /// <returns>Current user identifier</returns>
        private string GetCurrentUser()
        {
            // Implement based on your user context system
            return Environment.UserName;
        }

        #endregion
    }
}