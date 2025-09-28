using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.UOWManager.Helpers
{
    /// <summary>
    /// Helper class for Oracle Forms simulation functionality
    /// </summary>
    public class FormsSimulationHelper : IFormsSimulationHelper
    {
        #region Fields
        private readonly IDMEEditor _dmeEditor;
        private readonly ConcurrentDictionary<string, DataBlockInfo> _blocks;
        private readonly ConcurrentDictionary<string, PropertyInfo> _propertyCache = new();
        private static readonly Dictionary<string, string[]> _auditFieldPatterns = new()
        {
            ["CreatedDate"] = new[] { "CreatedDate", "Created_Date", "CreateDate", "DateCreated" },
            ["ModifiedDate"] = new[] { "ModifiedDate", "Modified_Date", "ModifyDate", "DateModified", "LastUpdated", "UpdatedDate" },
            ["CreatedBy"] = new[] { "CreatedBy", "Created_By", "CreateUser" },
            ["ModifiedBy"] = new[] { "ModifiedBy", "Modified_By", "ModifyUser", "LastUpdatedBy", "UpdatedBy" }
        };
        #endregion

        #region Constructor
        public FormsSimulationHelper(
            IDMEEditor dmeEditor,
            ConcurrentDictionary<string, DataBlockInfo> blocks)
        {
            _dmeEditor = dmeEditor ?? throw new ArgumentNullException(nameof(dmeEditor));
            _blocks = blocks ?? throw new ArgumentNullException(nameof(blocks));
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Sets default values for common audit fields when a new record is created
        /// Similar to Oracle Forms default value triggers
        /// </summary>
        public void SetAuditDefaults(object record, string currentUser = null)
        {
            if (record == null) return;

            try
            {
                var now = DateTime.Now;
                var auditDefaults = new Dictionary<string, object>();

                // Add date fields
                foreach (var datePattern in _auditFieldPatterns["CreatedDate"])
                {
                    auditDefaults[datePattern] = now;
                }
                foreach (var datePattern in _auditFieldPatterns["ModifiedDate"])
                {
                    auditDefaults[datePattern] = now;
                }

                // Add user fields if current user is provided
                if (!string.IsNullOrEmpty(currentUser))
                {
                    foreach (var userPattern in _auditFieldPatterns["CreatedBy"])
                    {
                        auditDefaults[userPattern] = currentUser;
                    }
                    foreach (var userPattern in _auditFieldPatterns["ModifiedBy"])
                    {
                        auditDefaults[userPattern] = currentUser;
                    }
                }

                // Apply defaults to record
                foreach (var fieldDefault in auditDefaults)
                {
                    SetFieldValue(record, fieldDefault.Key, fieldDefault.Value);
                }

                LogOperation($"Audit defaults applied to {record.GetType().Name}");
            }
            catch (Exception ex)
            {
                LogError($"Error setting audit defaults for {record?.GetType().Name}", ex);
            }
        }

        /// <summary>
        /// Sets a field value on a record using reflection with enhanced error handling and type conversion
        /// </summary>
        public bool SetFieldValue(object record, string fieldName, object value)
        {
            if (record == null || string.IsNullOrWhiteSpace(fieldName))
                return false;

            try
            {
                var property = GetCachedProperty(record.GetType(), fieldName);
                if (property == null || !property.CanWrite)
                    return false;

                // Convert value to the correct type if needed
                var convertedValue = ConvertValueToTargetType(value, property.PropertyType);
                
                // Set the value
                property.SetValue(record, convertedValue);
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Error setting field '{fieldName}' on {record.GetType().Name}", ex);
                return false;
            }
        }

        /// <summary>
        /// Gets a field value from a record using reflection with caching
        /// </summary>
        public object GetFieldValue(object record, string fieldName)
        {
            if (record == null || string.IsNullOrWhiteSpace(fieldName))
                return null;

            try
            {
                var property = GetCachedProperty(record.GetType(), fieldName);
                return property?.GetValue(record);
            }
            catch (Exception ex)
            {
                LogError($"Error getting field '{fieldName}' from {record.GetType().Name}", ex);
                return null;
            }
        }

        /// <summary>
        /// Executes a sequence generator for a field (Oracle sequence simulation)
        /// </summary>
        public bool ExecuteSequence(string blockName, object record, string fieldName, string sequenceName)
        {
            if (string.IsNullOrWhiteSpace(blockName) || record == null || 
                string.IsNullOrWhiteSpace(fieldName) || string.IsNullOrWhiteSpace(sequenceName))
                return false;

            try
            {
                if (!_blocks.TryGetValue(blockName, out var blockInfo) || blockInfo.UnitOfWork == null)
                {
                    LogError($"Block '{blockName}' not found or has no unit of work", null);
                    return false;
                }

                // Try to get sequence using the UnitOfWork's GetSeq method
                var unitOfWorkType = blockInfo.UnitOfWork.GetType();
                var getSeqMethod = unitOfWorkType.GetMethod("GetSeq", new[] { typeof(string) });
                
                if (getSeqMethod != null)
                {
                    var sequenceValue = getSeqMethod.Invoke(blockInfo.UnitOfWork, new object[] { sequenceName });
                    if (sequenceValue != null && Convert.ToInt32(sequenceValue) > 0)
                    {
                        var success = SetFieldValue(record, fieldName, sequenceValue);
                        if (success)
                        {
                            LogOperation($"Sequence '{sequenceName}' value {sequenceValue} set to field '{fieldName}' in block '{blockName}'");
                        }
                        return success;
                    }
                }

                LogError($"Could not generate sequence value for '{sequenceName}'", null);
                return false;
            }
            catch (Exception ex)
            {
                LogError($"Error executing sequence '{sequenceName}' for block '{blockName}'", ex);
                return false;
            }
        }

        /// <summary>
        /// Gets a property value from an object using reflection with enhanced error handling
        /// </summary>
        public object GetPropertyValue(object obj, string propertyName)
        {
            if (obj == null || string.IsNullOrWhiteSpace(propertyName))
                return null;

            try
            {
                var property = GetCachedProperty(obj.GetType(), propertyName);
                return property?.GetValue(obj);
            }
            catch (Exception ex)
            {
                LogError($"Error getting property '{propertyName}' from {obj.GetType().Name}", ex);
                return null;
            }
        }

        /// <summary>
        /// Sets common Oracle Forms system variables
        /// </summary>
        public void SetSystemVariables(object record, SystemVariableType variableType, object value = null)
        {
            if (record == null) return;

            try
            {
                switch (variableType)
                {
                    case SystemVariableType.SystemDate:
                        SetFieldValue(record, "SYSTEM_DATE", DateTime.Now.Date);
                        break;
                    case SystemVariableType.SystemDateTime:
                        SetFieldValue(record, "SYSTEM_DATETIME", DateTime.Now);
                        break;
                    case SystemVariableType.SystemUser:
                        var user = value?.ToString() ?? Environment.UserName;
                        SetFieldValue(record, "SYSTEM_USER", user);
                        break;
                    case SystemVariableType.RecordStatus:
                        SetFieldValue(record, "RECORD_STATUS", value ?? "NEW");
                        break;
                }
            }
            catch (Exception ex)
            {
                LogError($"Error setting system variable {variableType}", ex);
            }
        }

        /// <summary>
        /// Validates field constraints similar to Oracle Forms
        /// </summary>
        public ValidationResult ValidateField(object record, string fieldName, object value, FieldConstraints constraints = null)
        {
            var result = new ValidationResult { IsValid = true, FieldName = fieldName };

            if (record == null || string.IsNullOrWhiteSpace(fieldName))
            {
                result.IsValid = false;
                result.ErrorMessage = "Invalid parameters for field validation";
                return result;
            }

            try
            {
                constraints ??= GetDefaultConstraints(record.GetType(), fieldName);

                // Required field validation
                if (constraints.Required && IsNullOrEmpty(value))
                {
                    result.IsValid = false;
                    result.ErrorMessage = $"Field '{fieldName}' is required";
                    return result;
                }

                // Length validation for strings
                if (value is string stringValue && constraints.MaxLength > 0)
                {
                    if (stringValue.Length > constraints.MaxLength)
                    {
                        result.IsValid = false;
                        result.ErrorMessage = $"Field '{fieldName}' exceeds maximum length of {constraints.MaxLength}";
                        return result;
                    }
                }

                // Range validation for numeric types
                if (IsNumericType(value?.GetType()) && (constraints.MinValue.HasValue || constraints.MaxValue.HasValue))
                {
                    var numericValue = Convert.ToDouble(value);
                    
                    if (constraints.MinValue.HasValue && numericValue < constraints.MinValue.Value)
                    {
                        result.IsValid = false;
                        result.ErrorMessage = $"Field '{fieldName}' must be at least {constraints.MinValue.Value}";
                        return result;
                    }
                    
                    if (constraints.MaxValue.HasValue && numericValue > constraints.MaxValue.Value)
                    {
                        result.IsValid = false;
                        result.ErrorMessage = $"Field '{fieldName}' must not exceed {constraints.MaxValue.Value}";
                        return result;
                    }
                }

                // Custom validation
                if (constraints.CustomValidator != null)
                {
                    var customResult = constraints.CustomValidator(value);
                    if (!customResult.IsValid)
                    {
                        result.IsValid = false;
                        result.ErrorMessage = customResult.ErrorMessage;
                        return result;
                    }
                }

                LogOperation($"Field '{fieldName}' validation passed");
                return result;
            }
            catch (Exception ex)
            {
                LogError($"Error validating field '{fieldName}'", ex);
                result.IsValid = false;
                result.ErrorMessage = $"Validation error: {ex.Message}";
                return result;
            }
        }

        #endregion

        #region Private Helper Methods

        private PropertyInfo GetCachedProperty(Type type, string propertyName)
        {
            var cacheKey = $"{type.FullName}.{propertyName}";
            
            return _propertyCache.GetOrAdd(cacheKey, _ =>
            {
                return type.GetProperty(propertyName,
                    BindingFlags.IgnoreCase |
                    BindingFlags.Public |
                    BindingFlags.Instance);
            });
        }

        private object ConvertValueToTargetType(object value, Type targetType)
        {
            if (value == null)
                return GetDefaultValueForType(targetType);

            if (targetType.IsAssignableFrom(value.GetType()))
                return value;

            // Handle nullable types
            var actualTargetType = targetType;
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                actualTargetType = Nullable.GetUnderlyingType(targetType);
            }

            try
            {
                // Special handling for common conversions
                if (actualTargetType == typeof(DateTime) && value is string stringValue)
                {
                    if (DateTime.TryParse(stringValue, out var dateResult))
                        return dateResult;
                }

                if (actualTargetType == typeof(bool) && value is string boolString)
                {
                    return boolString.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                           boolString.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                           boolString.Equals("yes", StringComparison.OrdinalIgnoreCase);
                }

                return Convert.ChangeType(value, actualTargetType);
            }
            catch
            {
                return GetDefaultValueForType(targetType);
            }
        }

        private object GetDefaultValueForType(Type type)
        {
            if (type.IsValueType)
                return Activator.CreateInstance(type);
            return null;
        }

        private bool IsNullOrEmpty(object value)
        {
            return value == null ||
                   (value is string str && string.IsNullOrWhiteSpace(str)) ||
                   value == DBNull.Value;
        }

        private bool IsNumericType(Type type)
        {
            if (type == null) return false;

            var numericTypes = new[]
            {
                typeof(byte), typeof(sbyte), typeof(short), typeof(ushort),
                typeof(int), typeof(uint), typeof(long), typeof(ulong),
                typeof(float), typeof(double), typeof(decimal)
            };

            return Array.Exists(numericTypes, t => t == type) ||
                   Array.Exists(numericTypes, t => t == Nullable.GetUnderlyingType(type));
        }

        private FieldConstraints GetDefaultConstraints(Type recordType, string fieldName)
        {
            // This could be enhanced to read from entity structure or annotations
            return new FieldConstraints();
        }

        private void LogOperation(string message)
        {
            _dmeEditor?.AddLogMessage("FormsSimulationHelper", message, DateTime.Now, 0, null, Errors.Ok);
        }

        private void LogError(string message, Exception ex)
        {
            _dmeEditor?.AddLogMessage("FormsSimulationHelper", $"{message}: {ex?.Message}", DateTime.Now, -1, null, Errors.Failed);
        }

        #endregion
    }
}