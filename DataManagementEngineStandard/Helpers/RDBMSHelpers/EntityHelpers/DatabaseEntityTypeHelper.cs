using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Helpers.RDBMSHelpers.EntityHelpers
{
    /// <summary>
    /// Helper class for working with field types and creating basic entity fields.
    /// </summary>
    public static class DatabaseEntityTypeHelper
    {
        /// <summary>
        /// Checks if a field type is numeric.
        /// </summary>
        /// <param name="fieldType">The field type to check</param>
        /// <returns>True if numeric, false otherwise</returns>
        public static bool IsNumericType(string fieldType)
        {
            if (string.IsNullOrWhiteSpace(fieldType))
                return false;

            var normalizedType = fieldType.ToUpperInvariant().Replace("SYSTEM.", "");
            
            return normalizedType switch
            {
                "INT" or "INTEGER" or "INT32" or "INT16" or "INT64" or
                "BYTE" or "SBYTE" or "SHORT" or "LONG" or "UINT16" or
                "UINT32" or "UINT64" or "DECIMAL" or "DOUBLE" or
                "SINGLE" or "FLOAT" or "NUMBER" => true,
                _ => false
            };
        }

        /// <summary>
        /// Creates a basic EntityField with common defaults.
        /// </summary>
        /// <param name="fieldName">Name of the field</param>
        /// <param name="fieldType">Type of the field</param>
        /// <param name="allowNull">Whether the field allows null values</param>
        /// <param name="isKey">Whether the field is a primary key</param>
        /// <returns>A new EntityField with the specified properties</returns>
        public static EntityField CreateBasicField(string fieldName, string fieldType, bool allowNull = true, bool isKey = false)
        {
            return new EntityField
            {
                fieldname = fieldName,
                fieldtype = fieldType,
                AllowDBNull = allowNull,
                IsKey = isKey,
                IsRequired = !allowNull,
                Size1 = GetDefaultSizeForType(fieldType),
                fieldCategory = GetFieldCategoryForType(fieldType)
            };
        }

        /// <summary>
        /// Gets the default size for a field type.
        /// </summary>
        /// <param name="fieldType">The field type</param>
        /// <returns>Default size for the field type</returns>
        public static int GetDefaultSizeForType(string fieldType)
        {
            var normalizedType = fieldType?.ToUpperInvariant() ?? "";
            
            return normalizedType switch
            {
                "VARCHAR" or "NVARCHAR" or "CHAR" or "NCHAR" => 255,
                "TEXT" or "NTEXT" => 0, // No size limit
                "INT" or "INTEGER" => 4,
                "BIGINT" => 8,
                "SMALLINT" => 2,
                "TINYINT" => 1,
                "DECIMAL" or "NUMERIC" => 18,
                "FLOAT" or "REAL" => 8,
                "DATETIME" or "DATETIME2" => 0,
                "BIT" => 1,
                _ => 255
            };
        }

        /// <summary>
        /// Gets the field category for a field type.
        /// </summary>
        /// <param name="fieldType">The field type</param>
        /// <returns>DbFieldCategory for the field type</returns>
        public static DbFieldCategory GetFieldCategoryForType(string fieldType)
        {
            var normalizedType = fieldType?.ToUpperInvariant() ?? "";
            
            return normalizedType switch
            {
                "INT" or "INTEGER" or "BIGINT" or "SMALLINT" or "TINYINT" => DbFieldCategory.Integer,
                "FLOAT" or "REAL" or "DECIMAL" or "NUMERIC" or "DOUBLE" => DbFieldCategory.Double,
                "DATETIME" or "DATETIME2" or "DATE" or "TIME" or "TIMESTAMP" => DbFieldCategory.Date,
                "BIT" or "BOOLEAN" => DbFieldCategory.Boolean,
                "BINARY" or "VARBINARY" or "IMAGE" or "BLOB" => DbFieldCategory.Binary,
                _ => DbFieldCategory.String
            };
        }
    }
}