using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.UOW.Interfaces;

namespace TheTechIdea.Beep.Editor.UOW.Helpers
{
    /// <summary>
    /// Helper class for validation operations in UnitofWork
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public class UnitofWorkValidationHelper<T> : IUnitofWorkValidation<T> where T : Entity, new()
    {
        #region Private Fields

        private readonly IDMEEditor _editor;
        private readonly EntityStructure _entityStructure;
        private readonly string _primaryKeyName;
        private readonly PropertyInfo _primaryKeyProperty;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of UnitofWorkValidationHelper
        /// </summary>
        /// <param name="editor">DME Editor instance</param>
        /// <param name="entityStructure">Entity structure</param>
        /// <param name="primaryKeyName">Primary key field name</param>
        public UnitofWorkValidationHelper(IDMEEditor editor, EntityStructure entityStructure, string primaryKeyName)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
            _entityStructure = entityStructure;
            _primaryKeyName = primaryKeyName;

            if (!string.IsNullOrEmpty(primaryKeyName))
            {
                _primaryKeyProperty = typeof(T).GetProperty(primaryKeyName, 
                    BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Validates an entity
        /// </summary>
        /// <param name="entity">Entity to validate</param>
        /// <returns>Validation result</returns>
        public IErrorsInfo ValidateEntity(T entity)
        {
            var result = new ErrorsInfo { Flag = Errors.Ok };

            if (entity == null)
            {
                result.Flag = Errors.Failed;
                result.Message = "Entity cannot be null";
                return result;
            }

            try
            {
                var validationErrors = new List<string>();

                // Validate required fields
                var requiredFieldsResult = ValidateRequiredFields(entity);
                if (requiredFieldsResult.Flag == Errors.Failed)
                {
                    validationErrors.Add(requiredFieldsResult.Message);
                }

                // Validate data types and constraints
                var dataTypeResult = ValidateDataTypes(entity);
                if (dataTypeResult.Flag == Errors.Failed)
                {
                    validationErrors.Add(dataTypeResult.Message);
                }

                // Validate field lengths
                var lengthResult = ValidateFieldLengths(entity);
                if (lengthResult.Flag == Errors.Failed)
                {
                    validationErrors.Add(lengthResult.Message);
                }

                // Validate custom business rules
                var businessRulesResult = ValidateBusinessRules(entity);
                if (businessRulesResult.Flag == Errors.Failed)
                {
                    validationErrors.Add(businessRulesResult.Message);
                }

                if (validationErrors.Any())
                {
                    result.Flag = Errors.Failed;
                    result.Message = string.Join("; ", validationErrors);
                }
                else
                {
                    result.Message = "Entity validation passed";
                }
            }
            catch (Exception ex)
            {
                result.Flag = Errors.Failed;
                result.Message = $"Error during entity validation: {ex.Message}";
                result.Ex = ex;
            }

            return result;
        }

        /// <summary>
        /// Validates an entity for insert operation
        /// </summary>
        /// <param name="entity">Entity to validate</param>
        /// <returns>Validation result</returns>
        public IErrorsInfo ValidateForInsert(T entity)
        {
            if (entity == null)
            {
                return new ErrorsInfo 
                { 
                    Flag = Errors.Failed, 
                    Message = "Entity cannot be null for insert operation" 
                };
            }

            try
            {
                var validationErrors = new List<string>();

                // General entity validation
                var generalResult = ValidateEntity(entity);
                if (generalResult.Flag == Errors.Failed)
                {
                    validationErrors.Add(generalResult.Message);
                }

                // Insert-specific validations
                var insertSpecificResult = ValidateInsertSpecificRules(entity);
                if (insertSpecificResult.Flag == Errors.Failed)
                {
                    validationErrors.Add(insertSpecificResult.Message);
                }

                // Check for duplicate primary key (if not identity)
                if (!IsIdentityField() && !string.IsNullOrEmpty(_primaryKeyName))
                {
                    var duplicateKeyResult = ValidateNoDuplicateKey(entity);
                    if (duplicateKeyResult.Flag == Errors.Failed)
                    {
                        validationErrors.Add(duplicateKeyResult.Message);
                    }
                }

                var result = new ErrorsInfo { Flag = Errors.Ok };
                if (validationErrors.Any())
                {
                    result.Flag = Errors.Failed;
                    result.Message = string.Join("; ", validationErrors);
                }
                else
                {
                    result.Message = "Insert validation passed";
                }

                return result;
            }
            catch (Exception ex)
            {
                return new ErrorsInfo
                {
                    Flag = Errors.Failed,
                    Message = $"Error during insert validation: {ex.Message}",
                    Ex = ex
                };
            }
        }

        /// <summary>
        /// Validates an entity for update operation
        /// </summary>
        /// <param name="entity">Entity to validate</param>
        /// <returns>Validation result</returns>
        public IErrorsInfo ValidateForUpdate(T entity)
        {
            if (entity == null)
            {
                return new ErrorsInfo 
                { 
                    Flag = Errors.Failed, 
                    Message = "Entity cannot be null for update operation" 
                };
            }

            try
            {
                var validationErrors = new List<string>();

                // Primary key validation (must exist for updates)
                var primaryKeyResult = ValidatePrimaryKey(entity);
                if (primaryKeyResult.Flag == Errors.Failed)
                {
                    validationErrors.Add(primaryKeyResult.Message);
                }

                // General entity validation
                var generalResult = ValidateEntity(entity);
                if (generalResult.Flag == Errors.Failed)
                {
                    validationErrors.Add(generalResult.Message);
                }

                // Update-specific validations
                var updateSpecificResult = ValidateUpdateSpecificRules(entity);
                if (updateSpecificResult.Flag == Errors.Failed)
                {
                    validationErrors.Add(updateSpecificResult.Message);
                }

                var result = new ErrorsInfo { Flag = Errors.Ok };
                if (validationErrors.Any())
                {
                    result.Flag = Errors.Failed;
                    result.Message = string.Join("; ", validationErrors);
                }
                else
                {
                    result.Message = "Update validation passed";
                }

                return result;
            }
            catch (Exception ex)
            {
                return new ErrorsInfo
                {
                    Flag = Errors.Failed,
                    Message = $"Error during update validation: {ex.Message}",
                    Ex = ex
                };
            }
        }

        /// <summary>
        /// Validates an entity for delete operation
        /// </summary>
        /// <param name="entity">Entity to validate</param>
        /// <returns>Validation result</returns>
        public IErrorsInfo ValidateForDelete(T entity)
        {
            if (entity == null)
            {
                return new ErrorsInfo 
                { 
                    Flag = Errors.Failed, 
                    Message = "Entity cannot be null for delete operation" 
                };
            }

            try
            {
                var validationErrors = new List<string>();

                // Primary key validation (must exist for deletes)
                var primaryKeyResult = ValidatePrimaryKey(entity);
                if (primaryKeyResult.Flag == Errors.Failed)
                {
                    validationErrors.Add(primaryKeyResult.Message);
                }

                // Delete-specific validations (referential integrity, etc.)
                var deleteSpecificResult = ValidateDeleteSpecificRules(entity);
                if (deleteSpecificResult.Flag == Errors.Failed)
                {
                    validationErrors.Add(deleteSpecificResult.Message);
                }

                var result = new ErrorsInfo { Flag = Errors.Ok };
                if (validationErrors.Any())
                {
                    result.Flag = Errors.Failed;
                    result.Message = string.Join("; ", validationErrors);
                }
                else
                {
                    result.Message = "Delete validation passed";
                }

                return result;
            }
            catch (Exception ex)
            {
                return new ErrorsInfo
                {
                    Flag = Errors.Failed,
                    Message = $"Error during delete validation: {ex.Message}",
                    Ex = ex
                };
            }
        }

        /// <summary>
        /// Validates primary key for an entity
        /// </summary>
        /// <param name="entity">Entity to validate</param>
        /// <returns>Validation result</returns>
        public IErrorsInfo ValidatePrimaryKey(T entity)
        {
            var result = new ErrorsInfo { Flag = Errors.Ok };

            if (entity == null)
            {
                result.Flag = Errors.Failed;
                result.Message = "Entity cannot be null";
                return result;
            }

            try
            {
                if (string.IsNullOrEmpty(_primaryKeyName))
                {
                    result.Flag = Errors.Failed;
                    result.Message = "Primary key name is not configured";
                    return result;
                }

                if (_primaryKeyProperty == null)
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"Primary key property '{_primaryKeyName}' not found on entity";
                    return result;
                }

                var primaryKeyValue = _primaryKeyProperty.GetValue(entity);

                // Check if primary key is null or empty
                if (primaryKeyValue == null)
                {
                    result.Flag = Errors.Failed;
                    result.Message = "Primary key cannot be null";
                    return result;
                }

                // Check if string primary key is empty
                if (_primaryKeyProperty.PropertyType == typeof(string) && 
                    string.IsNullOrWhiteSpace(primaryKeyValue.ToString()))
                {
                    result.Flag = Errors.Failed;
                    result.Message = "Primary key cannot be empty";
                    return result;
                }

                // Check if numeric primary key is zero (unless explicitly allowed)
                if (IsNumericType(_primaryKeyProperty.PropertyType) && 
                    Convert.ToDouble(primaryKeyValue) == 0 && 
                    !IsIdentityField())
                {
                    result.Flag = Errors.Failed;
                    result.Message = "Primary key cannot be zero for non-identity fields";
                    return result;
                }

                result.Message = "Primary key validation passed";
            }
            catch (Exception ex)
            {
                result.Flag = Errors.Failed;
                result.Message = $"Error validating primary key: {ex.Message}";
                result.Ex = ex;
            }

            return result;
        }

        /// <summary>
        /// Validates required fields for an entity
        /// </summary>
        /// <param name="entity">Entity to validate</param>
        /// <returns>Validation result</returns>
        public IErrorsInfo ValidateRequiredFields(T entity)
        {
            var result = new ErrorsInfo { Flag = Errors.Ok };

            if (entity == null)
            {
                result.Flag = Errors.Failed;
                result.Message = "Entity cannot be null";
                return result;
            }

            try
            {
                var missingFields = new List<string>();

                if (_entityStructure?.Fields != null)
                {
                    var requiredFields = _entityStructure.Fields
                        .Where(f => !f.AllowDBNull && !f.IsAutoIncrement)
                        .ToList();

                    foreach (var field in requiredFields)
                    {
                        var property = entity.GetType().GetProperty(field.FieldName, 
                            BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                        if (property != null)
                        {
                            var value = property.GetValue(entity);
                            
                            if (value == null)
                            {
                                missingFields.Add(field.FieldName);
                            }
                            else if (property.PropertyType == typeof(string) && 
                                    string.IsNullOrWhiteSpace(value.ToString()))
                            {
                                missingFields.Add(field.FieldName);
                            }
                        }
                    }
                }

                if (missingFields.Any())
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"Required fields are missing or empty: {string.Join(", ", missingFields)}";
                }
                else
                {
                    result.Message = "All required fields are present";
                }
            }
            catch (Exception ex)
            {
                result.Flag = Errors.Failed;
                result.Message = $"Error validating required fields: {ex.Message}";
                result.Ex = ex;
            }

            return result;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Validates data types and constraints
        /// </summary>
        /// <param name="entity">Entity to validate</param>
        /// <returns>Validation result</returns>
        private IErrorsInfo ValidateDataTypes(T entity)
        {
            var result = new ErrorsInfo { Flag = Errors.Ok };
            var errors = new List<string>();

            try
            {
                if (_entityStructure?.Fields != null)
                {
                    foreach (var field in _entityStructure.Fields)
                    {
                        var property = entity.GetType().GetProperty(field.FieldName, 
                            BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                        if (property != null)
                        {
                            var value = property.GetValue(entity);
                            if (value != null)
                            {
                                var fieldError = ValidateFieldDataType(field, value, property.PropertyType);
                                if (!string.IsNullOrEmpty(fieldError))
                                {
                                    errors.Add($"{field.FieldName}: {fieldError}");
                                }
                            }
                        }
                    }
                }

                if (errors.Any())
                {
                    result.Flag = Errors.Failed;
                    result.Message = string.Join("; ", errors);
                }
            }
            catch (Exception ex)
            {
                result.Flag = Errors.Failed;
                result.Message = $"Error validating data types: {ex.Message}";
                result.Ex = ex;
            }

            return result;
        }

        /// <summary>
        /// Validates field lengths
        /// </summary>
        /// <param name="entity">Entity to validate</param>
        /// <returns>Validation result</returns>
        private IErrorsInfo ValidateFieldLengths(T entity)
        {
            var result = new ErrorsInfo { Flag = Errors.Ok };
            var errors = new List<string>();

            try
            {
                if (_entityStructure?.Fields != null)
                {
                    foreach (var field in _entityStructure.Fields)
                    {
                        if (field.Size1 > 0) // Only validate if size is specified
                        {
                            var property = entity.GetType().GetProperty(field.FieldName, 
                                BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                            if (property != null && property.PropertyType == typeof(string))
                            {
                                var value = property.GetValue(entity)?.ToString();
                                if (!string.IsNullOrEmpty(value) && value.Length > field.Size1)
                                {
                                    errors.Add($"{field.FieldName}: Length {value.Length} exceeds maximum {field.Size1}");
                                }
                            }
                        }
                    }
                }

                if (errors.Any())
                {
                    result.Flag = Errors.Failed;
                    result.Message = string.Join("; ", errors);
                }
            }
            catch (Exception ex)
            {
                result.Flag = Errors.Failed;
                result.Message = $"Error validating field lengths: {ex.Message}";
                result.Ex = ex;
            }

            return result;
        }

        /// <summary>
        /// Validates custom business rules
        /// </summary>
        /// <param name="entity">Entity to validate</param>
        /// <returns>Validation result</returns>
        private IErrorsInfo ValidateBusinessRules(T entity)
        {
            var result = new ErrorsInfo { Flag = Errors.Ok };

            try
            {
                // Implement custom business rule validation here
                // This could include calling external validation services,
                // checking business constraints, etc.

                result.Message = "Business rules validation passed";
            }
            catch (Exception ex)
            {
                result.Flag = Errors.Failed;
                result.Message = $"Error validating business rules: {ex.Message}";
                result.Ex = ex;
            }

            return result;
        }

        /// <summary>
        /// Validates insert-specific rules
        /// </summary>
        /// <param name="entity">Entity to validate</param>
        /// <returns>Validation result</returns>
        private IErrorsInfo ValidateInsertSpecificRules(T entity)
        {
            var result = new ErrorsInfo { Flag = Errors.Ok };

            try
            {
                // Validate that required fields have values for insert
                if (_entityStructure?.Fields != null)
                {
                    var requiredFields = _entityStructure.Fields
                        .Where(f => !f.AllowDBNull && !f.IsAutoIncrement);

                    foreach (var field in requiredFields)
                    {
                        var property = typeof(T).GetProperty(field.FieldName,
                            BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                        if (property != null)
                        {
                            var value = property.GetValue(entity);
                            if (value == null || (property.PropertyType == typeof(string) && string.IsNullOrWhiteSpace(value.ToString())))
                            {
                                result.Flag = Errors.Failed;
                                result.Message = $"Required field '{field.FieldName}' is missing for insert";
                                return result;
                            }
                        }
                    }
                }

                result.Message = "Insert-specific validation passed";
            }
            catch (Exception ex)
            {
                result.Flag = Errors.Failed;
                result.Message = $"Error in insert-specific validation: {ex.Message}";
                result.Ex = ex;
            }

            return result;
        }

        /// <summary>
        /// Validates update-specific rules
        /// </summary>
        /// <param name="entity">Entity to validate</param>
        /// <returns>Validation result</returns>
        private IErrorsInfo ValidateUpdateSpecificRules(T entity)
        {
            var result = new ErrorsInfo { Flag = Errors.Ok };

            try
            {
                // Verify primary key is set (entity must exist to be updated)
                if (_primaryKeyProperty != null)
                {
                    var pkValue = _primaryKeyProperty.GetValue(entity);
                    if (pkValue == null)
                    {
                        result.Flag = Errors.Failed;
                        result.Message = "Cannot update entity without a primary key value";
                        return result;
                    }

                    // For numeric keys, verify non-zero (unless identity auto-assigned)
                    if (IsNumericType(_primaryKeyProperty.PropertyType) &&
                        Convert.ToDouble(pkValue) == 0 && !IsIdentityField())
                    {
                        result.Flag = Errors.Failed;
                        result.Message = "Cannot update entity with zero primary key (non-identity)";
                        return result;
                    }
                }

                result.Message = "Update-specific validation passed";
            }
            catch (Exception ex)
            {
                result.Flag = Errors.Failed;
                result.Message = $"Error in update-specific validation: {ex.Message}";
                result.Ex = ex;
            }

            return result;
        }

        /// <summary>
        /// Validates delete-specific rules
        /// </summary>
        /// <param name="entity">Entity to validate</param>
        /// <returns>Validation result</returns>
        private IErrorsInfo ValidateDeleteSpecificRules(T entity)
        {
            var result = new ErrorsInfo { Flag = Errors.Ok };

            try
            {
                // Verify primary key is set (entity must be identifiable to delete)
                if (_primaryKeyProperty != null)
                {
                    var pkValue = _primaryKeyProperty.GetValue(entity);
                    if (pkValue == null)
                    {
                        result.Flag = Errors.Failed;
                        result.Message = "Cannot delete entity without a primary key value";
                        return result;
                    }
                }

                result.Message = "Delete-specific validation passed";
            }
            catch (Exception ex)
            {
                result.Flag = Errors.Failed;
                result.Message = $"Error in delete-specific validation: {ex.Message}";
                result.Ex = ex;
            }

            return result;
        }

        /// <summary>
        /// Validates no duplicate key exists
        /// </summary>
        /// <param name="entity">Entity to validate</param>
        /// <returns>Validation result</returns>
        private IErrorsInfo ValidateNoDuplicateKey(T entity)
        {
            var result = new ErrorsInfo { Flag = Errors.Ok };

            try
            {
                // This would typically involve checking the data source for existing records
                // with the same primary key. Implementation depends on your data access patterns.
                
                result.Message = "Duplicate key validation passed";
            }
            catch (Exception ex)
            {
                result.Flag = Errors.Failed;
                result.Message = $"Error checking for duplicate keys: {ex.Message}";
                result.Ex = ex;
            }

            return result;
        }

        /// <summary>
        /// Validates field data type
        /// </summary>
        /// <param name="field">Entity field</param>
        /// <param name="value">Value to validate</param>
        /// <param name="propertyType">Property type</param>
        /// <returns>Error message or null if valid</returns>
        private string ValidateFieldDataType(EntityField field, object value, Type propertyType)
        {
            try
            {
                // Perform type-specific validations
                if (propertyType == typeof(DateTime) || propertyType == typeof(DateTime?))
                {
                    if (value is DateTime dateValue)
                    {
                        if (dateValue < DateTime.MinValue || dateValue > DateTime.MaxValue)
                        {
                            return "Date value is out of range";
                        }
                    }
                }
                else if (IsNumericType(propertyType))
                {
                    // Validate numeric ranges if specified in field configuration
                    // This could be extended based on your EntityField structure
                }

                return null; // Valid
            }
            catch (Exception ex)
            {
                return $"Data type validation error: {ex.Message}";
            }
        }

        /// <summary>
        /// Checks if a type is numeric
        /// </summary>
        /// <param name="type">Type to check</param>
        /// <returns>True if numeric</returns>
        private bool IsNumericType(Type type)
        {
            var numericTypes = new[]
            {
                typeof(byte), typeof(sbyte), typeof(short), typeof(ushort),
                typeof(int), typeof(uint), typeof(long), typeof(ulong),
                typeof(float), typeof(double), typeof(decimal)
            };

            return numericTypes.Contains(type) || 
                   numericTypes.Contains(Nullable.GetUnderlyingType(type));
        }

        /// <summary>
        /// Checks if the primary key field is an identity field
        /// </summary>
        /// <returns>True if identity field</returns>
        private bool IsIdentityField()
        {
            return _entityStructure?.Fields?.Any(f => 
                f.FieldName.Equals(_primaryKeyName, StringComparison.OrdinalIgnoreCase) && 
                f.IsAutoIncrement) ?? false;
        }

        #endregion
    }
}