using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Helpers;

namespace TheTechIdea.Beep.Helpers.RDBMSHelpers.EntityHelpers
{
    /// <summary>
    /// Helper class for validating entity structures and fields.
    /// </summary>
    public static class DatabaseEntityValidator
    {
        /// <summary>
        /// Validates an entity structure and returns errors if any.
        /// </summary>
        /// <param name="entity">The EntityStructure to validate</param>
        /// <returns>Tuple with validation result and error list</returns>
        public static (bool IsValid, List<string> ValidationErrors) ValidateEntityStructure(EntityStructure entity)
        {
            var errors = new List<string>();
            bool valid = true;
            
            if (entity == null)
            {
                errors.Add("Entity is null");
                valid = false;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(entity.EntityName))
                {
                    errors.Add("Entity name is empty");
                    valid = false;
                }
                
                if (entity.Fields == null || !entity.Fields.Any())
                {
                    errors.Add("Entity has no fields");
                    valid = false;
                }
                else
                {
                    // Validate individual fields
                    var fieldValidationErrors = ValidateEntityFields(entity.Fields);
                    errors.AddRange(fieldValidationErrors);
                    if (fieldValidationErrors.Any())
                        valid = false;
                }

                // Check for database type compatibility
                if (entity.DatabaseType == DataSourceType.NONE || entity.DatabaseType == DataSourceType.Unknown)
                {
                    errors.Add("Invalid or unknown database type");
                    valid = false;
                }

                // Validate naming conventions
                var namingErrors = DatabaseEntityNamingValidator.ValidateNamingConventions(entity);
                errors.AddRange(namingErrors);
                if (namingErrors.Any())
                    valid = false;
            }
            
            return (valid, errors);
        }

        /// <summary>
        /// Validates entity fields for common issues.
        /// </summary>
        /// <param name="fields">List of entity fields to validate</param>
        /// <returns>List of validation errors</returns>
        public static List<string> ValidateEntityFields(List<EntityField> fields)
        {
            var errors = new List<string>();
            var fieldNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var field in fields)
            {
                // Check for empty field names (using the correct property name)
                if (string.IsNullOrWhiteSpace(field.fieldname))
                {
                    errors.Add("Field has empty or null name");
                    continue;
                }

                // Check for duplicate field names
                if (fieldNames.Contains(field.fieldname))
                {
                    errors.Add($"Duplicate field name: {field.fieldname}");
                }
                else
                {
                    fieldNames.Add(field.fieldname);
                }

                // Check for invalid field types (using the correct property name)
                if (string.IsNullOrWhiteSpace(field.fieldtype))
                {
                    errors.Add($"Field '{field.fieldname}' has no data type specified");
                }

                // Check for reasonable field sizes
                if (field.Size1 < 0)
                {
                    errors.Add($"Field '{field.fieldname}' has negative size");
                }

                // Validate primary key constraints
                if (field.IsKey && field.AllowDBNull)
                {
                    errors.Add($"Primary key field '{field.fieldname}' cannot allow null values");
                }

                // Validate auto-increment fields
                if (field.IsAutoIncrement && !DatabaseEntityTypeHelper.IsNumericType(field.fieldtype))
                {
                    errors.Add($"Auto-increment field '{field.fieldname}' must be a numeric type");
                }

                // Validate required fields that allow null
                if (field.IsRequired && field.AllowDBNull)
                {
                    errors.Add($"Required field '{field.fieldname}' cannot allow null values");
                }

                // Validate indexed fields
                if (field.IsIndexed && field.fieldtype?.ToUpper().Contains("TEXT") == true && field.Size1 > 8000)
                {
                    errors.Add($"Indexed text field '{field.fieldname}' has size too large for efficient indexing");
                }
            }

            // Check for multiple auto-increment fields
            var autoIncrementFields = fields.Where(f => f.IsAutoIncrement).ToList();
            if (autoIncrementFields.Count > 1)
            {
                errors.Add("Multiple auto-increment fields are not allowed");
            }

            // Check for primary key existence
            var primaryKeyFields = fields.Where(f => f.IsKey).ToList();
            if (!primaryKeyFields.Any())
            {
                errors.Add("Entity has no primary key defined");
            }

            // Check for multiple identity fields
            var identityFields = fields.Where(f => f.IsIdentity).ToList();
            if (identityFields.Count > 1)
            {
                errors.Add("Multiple identity fields are not allowed");
            }

            return errors;
        }
    }
}