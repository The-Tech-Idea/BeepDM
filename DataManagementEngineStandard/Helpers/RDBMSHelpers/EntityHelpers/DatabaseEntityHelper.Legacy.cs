using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Helpers.RDBMSHelpers.EntityHelpers
{
    /// <summary>
    /// Partial class containing legacy methods for backward compatibility.
    /// These methods are marked as obsolete and delegate to the new specialized helper classes.
    /// </summary>
    public static partial class DatabaseEntityHelper
    {
        #region Legacy Methods (Backward Compatibility)

        /// <summary>
        /// Validates entity fields for common issues.
        /// </summary>
        /// <param name="fields">List of entity fields to validate</param>
        /// <returns>List of validation errors</returns>
        [Obsolete("This method is deprecated. Use DatabaseEntityValidator.ValidateEntityFields instead.", false)]
        public static List<string> ValidateEntityFields(List<EntityField> fields)
        {
            return DatabaseEntityValidator.ValidateEntityFields(fields);
        }

        /// <summary>
        /// Validates naming conventions for entity and field names.
        /// </summary>
        /// <param name="entity">The entity to validate</param>
        /// <returns>List of naming convention errors</returns>
        [Obsolete("This method is deprecated. Use DatabaseEntityNamingValidator.ValidateNamingConventions instead.", false)]
        public static List<string> ValidateNamingConventions(EntityStructure entity)
        {
            return DatabaseEntityNamingValidator.ValidateNamingConventions(entity);
        }

        /// <summary>
        /// Checks if a string is a valid database identifier.
        /// </summary>
        /// <param name="identifier">The identifier to check</param>
        /// <returns>True if valid, false otherwise</returns>
        [Obsolete("This method is deprecated. Use DatabaseEntityNamingValidator.IsValidIdentifier instead.", false)]
        public static bool IsValidIdentifier(string identifier)
        {
            return DatabaseEntityNamingValidator.IsValidIdentifier(identifier);
        }

        /// <summary>
        /// Checks if a field type is numeric.
        /// </summary>
        /// <param name="Fieldtype">The field type to check</param>
        /// <returns>True if numeric, false otherwise</returns>
        [Obsolete("This method is deprecated. Use DatabaseEntityTypeHelper.IsNumericType instead.", false)]
        public static bool IsNumericType(string Fieldtype)
        {
            return DatabaseEntityTypeHelper.IsNumericType(Fieldtype);
        }

        /// <summary>
        /// Checks if an identifier is a reserved keyword for the given database type.
        /// </summary>
        /// <param name="identifier">The identifier to check</param>
        /// <param name="databaseType">The database type</param>
        /// <returns>True if it's a reserved keyword</returns>
        [Obsolete("This method is deprecated. Use DatabaseEntityReservedKeywordChecker.IsReservedKeyword instead.", false)]
        public static bool IsReservedKeyword(string identifier, DataSourceType databaseType)
        {
            return DatabaseEntityReservedKeywordChecker.IsReservedKeyword(identifier, databaseType);
        }

        /// <summary>
        /// Gets the default size for a field type.
        /// </summary>
        /// <param name="Fieldtype">The field type</param>
        /// <returns>Default size for the field type</returns>
        [Obsolete("This method is deprecated. Use DatabaseEntityTypeHelper.GetDefaultSizeForType instead.", false)]
        public static int GetDefaultSizeForType(string Fieldtype)
        {
            return DatabaseEntityTypeHelper.GetDefaultSizeForType(Fieldtype);
        }

        /// <summary>
        /// Gets the field category for a field type.
        /// </summary>
        /// <param name="Fieldtype">The field type</param>
        /// <returns>DbFieldCategory for the field type</returns>
        [Obsolete("This method is deprecated. Use DatabaseEntityTypeHelper.GetFieldCategoryForType instead.", false)]
        public static DbFieldCategory GetFieldCategoryForType(string Fieldtype)
        {
            return DatabaseEntityTypeHelper.GetFieldCategoryForType(Fieldtype);
        }

        #endregion
    }
}