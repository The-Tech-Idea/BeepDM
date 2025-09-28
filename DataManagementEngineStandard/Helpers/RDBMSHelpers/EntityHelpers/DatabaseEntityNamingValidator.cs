using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Helpers;

namespace TheTechIdea.Beep.Helpers.RDBMSHelpers.EntityHelpers
{
    /// <summary>
    /// Helper class for validating naming conventions for entities and fields.
    /// </summary>
    public static class DatabaseEntityNamingValidator
    {
        /// <summary>
        /// Validates naming conventions for entity and field names.
        /// </summary>
        /// <param name="entity">The entity to validate</param>
        /// <returns>List of naming convention errors</returns>
        public static List<string> ValidateNamingConventions(EntityStructure entity)
        {
            var errors = new List<string>();
            var maxIdentifierLength = DatabaseFeatureHelper.GetMaxIdentifierLength(entity.DatabaseType);

            // Validate entity name length
            if (entity.EntityName.Length > maxIdentifierLength)
            {
                errors.Add($"Entity name '{entity.EntityName}' exceeds maximum length of {maxIdentifierLength} for {entity.DatabaseType}");
            }

            // Validate entity name characters
            if (!IsValidIdentifier(entity.EntityName))
            {
                errors.Add($"Entity name '{entity.EntityName}' contains invalid characters");
            }

            // Validate field names
            if (entity.Fields != null)
            {
                foreach (var field in entity.Fields)
                {
                    if (field.fieldname.Length > maxIdentifierLength)
                    {
                        errors.Add($"Field name '{field.fieldname}' exceeds maximum length of {maxIdentifierLength} for {entity.DatabaseType}");
                    }

                    if (!IsValidIdentifier(field.fieldname))
                    {
                        errors.Add($"Field name '{field.fieldname}' contains invalid characters");
                    }

                    // Check for reserved keywords
                    if (DatabaseEntityReservedKeywordChecker.IsReservedKeyword(field.fieldname, entity.DatabaseType))
                    {
                        errors.Add($"Field name '{field.fieldname}' is a reserved keyword in {entity.DatabaseType}");
                    }
                }
            }

            // Check if entity name is a reserved keyword
            if (DatabaseEntityReservedKeywordChecker.IsReservedKeyword(entity.EntityName, entity.DatabaseType))
            {
                errors.Add($"Entity name '{entity.EntityName}' is a reserved keyword in {entity.DatabaseType}");
            }

            return errors;
        }

        /// <summary>
        /// Checks if a string is a valid database identifier.
        /// </summary>
        /// <param name="identifier">The identifier to check</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool IsValidIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                return false;

            // Must start with letter or underscore
            if (!char.IsLetter(identifier[0]) && identifier[0] != '_')
                return false;

            // Can only contain letters, digits, and underscores
            return identifier.All(c => char.IsLetterOrDigit(c) || c == '_');
        }
    }
}