using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        // _propertyCache was removed: the per-instance
        // ConcurrentDictionary<string, PropertyInfo> cache was a 100-line
        // reinvention of RecordPropertyAccessor (process-wide, typed
        // dictionary-of-dictionary catalog). All reflection call sites in
        // this helper now route through RecordPropertyAccessor.
        private static readonly Dictionary<string, string[]> _auditFieldPatterns = new()
        {
            ["CreatedDate"] = new[] { "CreatedDate", "Created_Date", "CreateDate", "DateCreated" },
            ["ModifiedDate"] = new[] { "ModifiedDate", "Modified_Date", "ModifyDate", "DateModified", "LastUpdated", "UpdatedDate" },
            ["CreatedBy"] = new[] { "CreatedBy", "Created_By", "CreateUser" },
            ["ModifiedBy"] = new[] { "ModifiedBy", "Modified_By", "ModifyUser", "LastUpdatedBy", "UpdatedBy" }
        };
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a Forms-simulation helper for the supplied editor and block registry.
        /// </summary>
        /// <param name="dmeEditor">Editor used for logging and datasource access.</param>
        /// <param name="blocks">Registered block metadata keyed by block name.</param>
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

                // Add user fields if current user is provided; fall back to Environment.UserName
                // when no explicit user is given so audit fields are never silently skipped.
                var effectiveUser = currentUser;
                if (string.IsNullOrEmpty(effectiveUser))
                    effectiveUser = Environment.UserName;
                if (!string.IsNullOrEmpty(effectiveUser))
                {
                    foreach (var userPattern in _auditFieldPatterns["CreatedBy"])
                    {
                        auditDefaults[userPattern] = effectiveUser;
                    }
                    foreach (var userPattern in _auditFieldPatterns["ModifiedBy"])
                    {
                        auditDefaults[userPattern] = effectiveUser;
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
        /// Sets a field value on a record. Delegates to
        /// <see cref="RecordPropertyAccessor"/> for the actual reflection,
        /// which means we share the process-wide PropertyInfo cache with
        /// every other FormsManager call site and get a throttled
        /// diagnostic on missing/read-only fields for free.
        /// </summary>
        public bool SetFieldValue(object record, string FieldName, object value)
        {
            if (record == null || string.IsNullOrWhiteSpace(FieldName))
                return false;

            try
            {
                return RecordPropertyAccessor.TrySetValue(record, FieldName, value, _dmeEditor);
            }
            catch (Exception ex)
            {
                LogError($"Error setting field '{FieldName}' on {record.GetType().Name}", ex);
                return false;
            }
        }

        /// <summary>
        /// Gets a field value from a record. Delegates to
        /// <see cref="RecordPropertyAccessor"/> for cached lookup.
        /// </summary>
        public object GetFieldValue(object record, string FieldName)
        {
            if (record == null || string.IsNullOrWhiteSpace(FieldName))
                return null;

            try
            {
                return RecordPropertyAccessor.GetValue(record, FieldName, _dmeEditor);
            }
            catch (Exception ex)
            {
                LogError($"Error getting field '{FieldName}' from {record.GetType().Name}", ex);
                return null;
            }
        }

        /// <summary>
        /// Executes a sequence generator for a field (Oracle sequence simulation)
        /// </summary>
        public bool ExecuteSequence(string blockName, object record, string FieldName, string sequenceName)
        {
            if (string.IsNullOrWhiteSpace(blockName) || record == null || 
                string.IsNullOrWhiteSpace(FieldName) || string.IsNullOrWhiteSpace(sequenceName))
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
                    // B1 (audit pass 3, 2026-06): the previous
                    // version silently no-op'd the field set if
                    // the sequence value was null, 0, or
                    // negative. Most DBs start sequences at 1,
                    // so 0 is suspicious and a clear signal
                    // of a misconfigured sequence (e.g. one
                    // that was reset to 0 and never advanced,
                    // or a test sequence the user forgot to
                    // populate). Distinguish the three
                    // failure modes in the log so the host can
                    // tell them apart.
                    if (sequenceValue == null)
                    {
                        LogError(
                            $"ExecuteSequence: sequence '{sequenceName}' on block '{blockName}' returned null. " +
                            $"Check the sequence registration in the unit of work.",
                            null);
                        return false;
                    }

                    int intValue;
                    try
                    {
                        intValue = Convert.ToInt32(sequenceValue);
                    }
                    catch (Exception ex)
                    {
                        LogError(
                            $"ExecuteSequence: sequence '{sequenceName}' on block '{blockName}' returned non-integer value '{sequenceValue}'",
                            ex);
                        return false;
                    }

                    if (intValue <= 0)
                    {
                        LogError(
                            $"ExecuteSequence: sequence '{sequenceName}' on block '{blockName}' returned non-positive value {intValue}. " +
                            $"Field '{FieldName}' not set. (Most DB sequences start at 1; check that the sequence was created and not reset to 0.)",
                            null);
                        return false;
                    }

                    var success = SetFieldValue(record, FieldName, sequenceValue);
                    if (success)
                    {
                        LogOperation($"Sequence '{sequenceName}' value {sequenceValue} set to field '{FieldName}' in block '{blockName}'");
                    }
                    return success;
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
        /// Gets a property value from an object. Delegates to
        /// <see cref="RecordPropertyAccessor"/> for the cached lookup.
        /// </summary>
        public object GetPropertyValue(object obj, string propertyName)
        {
            if (obj == null || string.IsNullOrWhiteSpace(propertyName))
                return null;

            try
            {
                return RecordPropertyAccessor.GetValue(obj, propertyName, _dmeEditor);
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
        /// Validates field constraints similar to Oracle Forms.
        /// </summary>
        /// <remarks>
        /// B5 (audit pass 3, 2026-06): the default
        /// <see cref="FieldConstraints"/> returned by
        /// <see cref="GetDefaultConstraints(Type, string)"/>
        /// has every constraint disabled (Required = false,
        /// MaxLength = 0, MinValue = null, MaxValue = null,
        /// no CustomValidator). Without an explicit
        /// <see cref="FieldConstraints"/> supplied by the
        /// caller, this method is effectively a no-op that
        /// always returns <c>IsValid = true</c>. The
        /// engine does not currently read entity annotations
        /// or other metadata to populate
        /// <see cref="FieldConstraints"/> automatically; the
        /// host must supply the constraints at registration
        /// time or via a future metadata-binding extension.
        /// </remarks>
        public ValidationResult ValidateField(object record, string FieldName, object value, FieldConstraints constraints = null)
        {
            var result = new ValidationResult { IsValid = true,FieldName = FieldName };

            if (record == null || string.IsNullOrWhiteSpace(FieldName))
            {
                result.IsValid = false;
                result.ErrorMessage = "Invalid parameters for field validation";
                return result;
            }

            try
            {
                constraints ??= GetDefaultConstraints(record.GetType(), FieldName);

                // Required field validation
                if (constraints.Required && IsNullOrEmpty(value))
                {
                    result.IsValid = false;
                    result.ErrorMessage = $"Field '{FieldName}' is required";
                    return result;
                }

                // Length validation for strings
                if (value is string stringValue && constraints.MaxLength > 0)
                {
                    if (stringValue.Length > constraints.MaxLength)
                    {
                        result.IsValid = false;
                        result.ErrorMessage = $"Field '{FieldName}' exceeds maximum length of {constraints.MaxLength}";
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
                        result.ErrorMessage = $"Field '{FieldName}' must be at least {constraints.MinValue.Value}";
                        return result;
                    }

                    if (constraints.MaxValue.HasValue && numericValue > constraints.MaxValue.Value)
                    {
                        result.IsValid = false;
                        result.ErrorMessage = $"Field '{FieldName}' must not exceed {constraints.MaxValue.Value}";
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

                LogOperation($"Field '{FieldName}' validation passed");
                return result;
            }
            catch (Exception ex)
            {
                LogError($"Error validating field '{FieldName}'", ex);
                result.IsValid = false;
                result.ErrorMessage = $"Validation error: {ex.Message}";
                return result;
            }
        }

        #endregion

        #region Private Helper Methods

        // GetCachedProperty removed: its per-instance string-keyed
        // cache has been replaced by RecordPropertyAccessor's
        // process-wide, type-keyed catalog.

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
            // B10 (audit pass 3, 2026-06): the previous
            // version caught the conversion exception and
            // silently returned the type's default. The
            // caller would see a "successful" set of a
            // bogus value (e.g. assigning 0 to an int
            // field when the user wanted to set "abc"). Now
            // log a diagnostic so the host can see the
            // failure.
            catch (Exception ex)
            {
                LogError(
                    $"ConvertValueToTargetType: failed to convert value '{value}' of type '{value.GetType().Name}' " +
                    $"to target type '{targetType.Name}'. Returning type default.",
                    ex);
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

        // B7 (audit pass 3, 2026-06): the numericTypes
        // array was allocated on every call. Moved to a
        // static field. The HashSet<T> variant would be
        // faster, but for an 11-element array linear scan is
        // fine and avoids the hash overhead.
        private static readonly Type[] _numericTypes =
        {
            typeof(byte), typeof(sbyte), typeof(short), typeof(ushort),
            typeof(int), typeof(uint), typeof(long), typeof(ulong),
            typeof(float), typeof(double), typeof(decimal)
        };

        private bool IsNumericType(Type type)
        {
            if (type == null) return false;

            return Array.IndexOf(_numericTypes, type) >= 0 ||
                   Array.IndexOf(_numericTypes, Nullable.GetUnderlyingType(type)) >= 0;
        }

        private FieldConstraints GetDefaultConstraints(Type recordType, string FieldName)
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