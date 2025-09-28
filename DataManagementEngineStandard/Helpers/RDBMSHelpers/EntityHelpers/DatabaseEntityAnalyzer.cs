using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Helpers;

namespace TheTechIdea.Beep.Helpers.RDBMSHelpers.EntityHelpers
{
    /// <summary>
    /// Helper class for analyzing and providing insights about entity structures.
    /// </summary>
    public static class DatabaseEntityAnalyzer
    {
        /// <summary>
        /// Gets entity compatibility information for different database types.
        /// </summary>
        /// <param name="entity">The entity to analyze</param>
        /// <returns>Dictionary containing compatibility information</returns>
        public static Dictionary<string, object> GetEntityCompatibilityInfo(EntityStructure entity)
        {
            var info = new Dictionary<string, object>();
            
            if (entity == null)
            {
                info["IsCompatible"] = false;
                info["Errors"] = new List<string> { "Entity is null" };
                return info;
            }

            var (isValid, errors) = DatabaseEntityValidator.ValidateEntityStructure(entity);
            info["IsCompatible"] = isValid;
            info["Errors"] = errors;

            if (isValid)
            {
                info["MaxIdentifierLength"] = DatabaseFeatureHelper.GetMaxIdentifierLength(entity.DatabaseType);
                info["SupportsAutoIncrement"] = DatabaseFeatureHelper.SupportsAutoIncrement(entity.DatabaseType);
                info["SupportsSequences"] = DatabaseFeatureHelper.SupportsSequences(entity.DatabaseType);
                info["SupportsViews"] = DatabaseFeatureHelper.SupportsViews(entity.DatabaseType);
                info["DatabaseFeatures"] = DatabaseFeatureHelper.GetSupportedFeatures(entity.DatabaseType);
                info["PrimaryKeyCount"] = entity.Fields?.Count(f => f.IsKey) ?? 0;
                info["AutoIncrementFields"] = entity.Fields?.Where(f => f.IsAutoIncrement).Select(f => f.fieldname).ToList() ?? new List<string>();
                info["UniqueFields"] = entity.Fields?.Where(f => f.IsUnique).Select(f => f.fieldname).ToList() ?? new List<string>();
                info["IndexedFields"] = entity.Fields?.Where(f => f.IsIndexed).Select(f => f.fieldname).ToList() ?? new List<string>();
            }

            return info;
        }

        /// <summary>
        /// Suggests improvements for an entity structure.
        /// </summary>
        /// <param name="entity">The entity to analyze</param>
        /// <returns>List of improvement suggestions</returns>
        public static List<string> SuggestEntityImprovements(EntityStructure entity)
        {
            var suggestions = new List<string>();
            
            if (entity == null || entity.Fields == null)
                return suggestions;

            // Check for missing unique constraints on potential unique fields
            var potentialUniqueFields = entity.Fields
                .Where(f => !f.IsKey && !f.IsUnique && 
                           (f.fieldname.ToLower().Contains("email") || 
                            f.fieldname.ToLower().Contains("code") ||
                            f.fieldname.ToLower().Contains("number") ||
                            f.fieldname.ToLower().Contains("username")))
                .ToList();
            
            foreach (var field in potentialUniqueFields)
            {
                suggestions.Add($"Consider making field '{field.fieldname}' unique if it should contain unique values");
            }

            // Check for overly long varchar/text fields
            var textFields = entity.Fields
                .Where(f => f.fieldtype.ToUpper().Contains("VARCHAR") || 
                           f.fieldtype.ToUpper().Contains("CHAR") ||
                           f.fieldtype.ToUpper().Contains("TEXT"))
                .Where(f => f.Size1 > 1000)
                .ToList();
            
            foreach (var field in textFields)
            {
                suggestions.Add($"Field '{field.fieldname}' has a very large size ({field.Size1}). Consider using appropriate TEXT type for better performance");
            }

            // Check for missing created/modified timestamp fields
            var hasCreatedField = entity.Fields.Any(f => 
                f.fieldname.ToLower().Contains("created") || 
                f.fieldname.ToLower().Contains("inserted") ||
                f.fieldname.ToLower().Contains("createdate"));
            
            var hasModifiedField = entity.Fields.Any(f => 
                f.fieldname.ToLower().Contains("modified") || 
                f.fieldname.ToLower().Contains("updated") ||
                f.fieldname.ToLower().Contains("modifydate"));

            if (!hasCreatedField)
            {
                suggestions.Add("Consider adding a 'CreatedDate' timestamp field for audit purposes");
            }

            if (!hasModifiedField)
            {
                suggestions.Add("Consider adding a 'ModifiedDate' timestamp field for audit purposes");
            }

            // Check for fields that should be indexed
            var potentialIndexFields = entity.Fields
                .Where(f => !f.IsIndexed && !f.IsKey &&
                           (f.fieldname.ToLower().Contains("id") && !f.fieldname.Equals("id", StringComparison.OrdinalIgnoreCase) ||
                            f.fieldname.ToLower().Contains("code") ||
                            f.fieldname.ToLower().Contains("status")))
                .ToList();

            foreach (var field in potentialIndexFields)
            {
                suggestions.Add($"Consider adding an index on field '{field.fieldname}' if it's frequently used in WHERE clauses");
            }

            // Check for missing descriptions
            var fieldsWithoutDescription = entity.Fields
                .Where(f => string.IsNullOrWhiteSpace(f.Description))
                .ToList();

            if (fieldsWithoutDescription.Count > entity.Fields.Count / 2)
            {
                suggestions.Add("Consider adding descriptions to fields for better documentation");
            }

            // Check for inconsistent nullable settings
            var requiredButNullable = entity.Fields
                .Where(f => f.IsRequired && f.AllowDBNull)
                .ToList();

            if (requiredButNullable.Any())
            {
                suggestions.Add("Some fields are marked as required but allow null values - consider reviewing nullable settings");
            }

            return suggestions;
        }

        /// <summary>
        /// Gets statistics about the entity structure.
        /// </summary>
        /// <param name="entity">The entity to analyze</param>
        /// <returns>Dictionary containing entity statistics</returns>
        public static Dictionary<string, object> GetEntityStatistics(EntityStructure entity)
        {
            var stats = new Dictionary<string, object>();

            if (entity == null)
            {
                stats["Error"] = "Entity is null";
                return stats;
            }

            stats["EntityName"] = entity.EntityName;
            stats["DatabaseType"] = entity.DatabaseType.ToString();
            stats["TotalFields"] = entity.Fields?.Count ?? 0;
            stats["PrimaryKeyFields"] = entity.Fields?.Count(f => f.IsKey) ?? 0;
            stats["UniqueFields"] = entity.Fields?.Count(f => f.IsUnique) ?? 0;
            stats["IndexedFields"] = entity.Fields?.Count(f => f.IsIndexed) ?? 0;
            stats["RequiredFields"] = entity.Fields?.Count(f => f.IsRequired) ?? 0;
            stats["NullableFields"] = entity.Fields?.Count(f => f.AllowDBNull) ?? 0;
            stats["AutoIncrementFields"] = entity.Fields?.Count(f => f.IsAutoIncrement) ?? 0;
            stats["IdentityFields"] = entity.Fields?.Count(f => f.IsIdentity) ?? 0;

            if (entity.Fields != null && entity.Fields.Any())
            {
                var fieldTypes = entity.Fields
                    .GroupBy(f => f.fieldtype)
                    .ToDictionary(g => g.Key, g => g.Count());
                stats["FieldTypeDistribution"] = fieldTypes;

                var fieldCategories = entity.Fields
                    .GroupBy(f => f.fieldCategory)
                    .ToDictionary(g => g.Key.ToString(), g => g.Count());
                stats["FieldCategoryDistribution"] = fieldCategories;
            }

            return stats;
        }
    }
}