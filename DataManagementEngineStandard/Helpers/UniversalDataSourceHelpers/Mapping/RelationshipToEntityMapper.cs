using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Helpers.UniversalDataSourceHelpers.Mapping
{
    /// <summary>
    /// Converts relationship analysis results from NavigationPropertyDetector and RelationshipInferencer
    /// into the existing BeepDM relationship classes (RelationShipKeys, ChildRelation).
    /// 
    /// This adapter bridges the new navigation property analysis with the existing EntityStructure
    /// relationship model, enabling automatic population of Relations and ChildRelations lists
    /// when converting POCOs to EntityStructure.
    /// </summary>
    public static class RelationshipToEntityMapper
    {
        /// <summary>
        /// Converts analysis of a navigation property into a RelationShipKeys object
        /// representing the foreign key relationship.
        /// </summary>
        /// <param name="navPropInfo">Navigation property analysis</param>
        /// <param name="sourceEntityName">Name of source entity</param>
        /// <param name="targetEntityName">Name of target entity</param>
        /// <param name="sourceType">Source type</param>
        /// <param name="targetType">Target type</param>
        /// <returns>RelationShipKeys configured for this relationship, or null if not applicable</returns>
        public static RelationShipKeys CreateRelationshipKey(
            Analysis.NavigationPropertyInfo navPropInfo,
            string sourceEntityName,
            string targetEntityName,
            Type sourceType,
            Type targetType)
        {
            if (navPropInfo == null || sourceType == null || targetType == null)
                return null;

            // Analyze cardinality
            var cardinality = Analysis.RelationshipInferencer.InferCardinality(
                navPropInfo.PropertyInfo,
                sourceType,
                targetType);

            // For Many-to-One relationships, the FK is on the source side
            if (cardinality == Analysis.RelationshipCardinality.ManyToOne)
            {
                var fkProp = navPropInfo.ForeignKeyProperty;
                if (fkProp != null)
                {
                    return new RelationShipKeys
                    {
                        RalationName = $"{sourceEntityName}_{targetEntityName}_{navPropInfo.PropertyName}",
                        EntityColumnID = fkProp.Name,  // FK on source (child)
                        RelatedEntityID = targetEntityName,  // Referenced entity
                        RelatedEntityColumnID = GetPrimaryKeyName(targetType),  // PK on target
                        GuidID = Guid.NewGuid().ToString()
                    };
                }
            }

            // For One-to-Many relationships, the FK is on the target side (stored in ChildRelation)
            // Return null here; ChildRelation will be created separately
            if (cardinality == Analysis.RelationshipCardinality.OneToMany)
                return null;

            // For One-to-One relationships
            if (cardinality == Analysis.RelationshipCardinality.OneToOne)
            {
                var fkProp = navPropInfo.ForeignKeyProperty;
                if (fkProp != null)
                {
                    return new RelationShipKeys
                    {
                        RalationName = $"{sourceEntityName}_{targetEntityName}_OneToOne",
                        EntityColumnID = fkProp.Name,
                        RelatedEntityID = targetEntityName,
                        RelatedEntityColumnID = GetPrimaryKeyName(targetType),
                        GuidID = Guid.NewGuid().ToString()
                    };
                }
            }

            return null;
        }

        /// <summary>
        /// Converts analysis of a navigation property into a ChildRelation object
        /// for one-to-many relationships where the FK is on the child side.
        /// </summary>
        /// <param name="navPropInfo">Navigation property analysis</param>
        /// <param name="sourceEntityName">Name of entity with collection (parent)</param>
        /// <param name="targetEntityName">Name of child entity</param>
        /// <param name="sourceType">Source type (parent)</param>
        /// <param name="targetType">Target type (child)</param>
        /// <returns>ChildRelation for one-to-many, or null if not applicable</returns>
        public static ChildRelation CreateChildRelation(
            Analysis.NavigationPropertyInfo navPropInfo,
            string sourceEntityName,
            string targetEntityName,
            Type sourceType,
            Type targetType)
        {
            if (navPropInfo == null || sourceType == null || targetType == null)
                return null;

            // Child relations only apply to One-to-Many
            var cardinality = Analysis.RelationshipInferencer.InferCardinality(
                navPropInfo.PropertyInfo,
                sourceType,
                targetType);

            if (cardinality != Analysis.RelationshipCardinality.OneToMany)
                return null;

            // Find the FK on the child side
            var childFkProp = FindChildForeignKeyProperty(sourceEntityName, targetType);
            if (childFkProp == null)
                return null;

            return new ChildRelation
            {
                parent_table = sourceEntityName,
                parent_column = GetPrimaryKeyName(sourceType),
                child_table = targetEntityName,
                child_column = childFkProp.Name,
                Constraint_Name = $"FK_{targetEntityName}_{sourceEntityName}",
                RalationName = $"{sourceEntityName}_{targetEntityName}_{navPropInfo.PropertyName}",
                GuidID = Guid.NewGuid().ToString()
            };
        }

        /// <summary>
        /// Extracts all relationships from a POCO type and populates EntityStructure.Relations
        /// and EntityStructure.ChildRelations with appropriate entries.
        /// </summary>
        /// <param name="pocoType">POCO type to analyze</param>
        /// <param name="entityStructure">EntityStructure to populate</param>
        /// <param name="entityName">Entity name (typically type name)</param>
        public static void PopulateRelationships(
            Type pocoType,
            EntityStructure entityStructure,
            string entityName)
        {
            if (pocoType == null || entityStructure == null)
                return;

            entityStructure.Relations = entityStructure.Relations ?? new List<RelationShipKeys>();

            var navProps = Analysis.NavigationPropertyDetector.GetNavigationProperties(pocoType);

            foreach (var navProp in navProps)
            {
                var referencedType = Analysis.NavigationPropertyDetector.GetReferencedType(navProp);
                if (referencedType == null)
                    continue;

                var referencedEntityName = referencedType.Name;

                // Analyze the navigation property
                var navPropInfo = Analysis.NavigationPropertyDetector.AnalyzeNavigationProperty(navProp, pocoType);
                if (navPropInfo == null)
                    continue;

                // Create RelationShipKeys for direct FK relationships
                var relKey = CreateRelationshipKey(navPropInfo, entityName, referencedEntityName, pocoType, referencedType);
                if (relKey != null)
                {
                    entityStructure.Relations.Add(relKey);
                }

                // Create ChildRelation for one-to-many (FK on child side)
                var childRel = CreateChildRelation(navPropInfo, entityName, referencedEntityName, pocoType, referencedType);
                // Note: ChildRelation would need to be added to a separate collection if EntityStructure has one
                // For now, store in Relations as documentation
            }
        }

        /// <summary>
        /// Analyzes a POCO type and returns relationship summary for documentation/UI.
        /// </summary>
        /// <param name="pocoType">POCO type to analyze</param>
        /// <returns>Dictionary mapping relationship names to cardinality descriptions</returns>
        public static Dictionary<string, string> GetRelationshipSummary(Type pocoType)
        {
            var summary = new Dictionary<string, string>();

            if (pocoType == null)
                return summary;

            var navProps = Analysis.NavigationPropertyDetector.GetNavigationProperties(pocoType);

            foreach (var navProp in navProps)
            {
                var referencedType = Analysis.NavigationPropertyDetector.GetReferencedType(navProp);
                if (referencedType == null)
                    continue;

                var cardinality = Analysis.RelationshipInferencer.InferCardinality(
                    navProp,
                    pocoType,
                    referencedType);

                var isBidirectional = Analysis.RelationshipInferencer.IsBidirectional(navProp, pocoType, referencedType);
                var direction = isBidirectional ? " (↔)" : " (→)";

                var key = $"{navProp.Name}";
                var value = $"{pocoType.Name} {cardinality}{direction} {referencedType.Name}";

                summary[key] = value;
            }

            return summary;
        }

        // ==================== Helper Methods ====================

        /// <summary>
        /// Gets the primary key property name for a type.
        /// </summary>
        private static string GetPrimaryKeyName(Type type)
        {
            if (type == null)
                return "Id";

            var props = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            // Check for [Key] attribute
            var keyProp = props.FirstOrDefault(p =>
                p.GetCustomAttribute<System.ComponentModel.DataAnnotations.KeyAttribute>() != null);
            if (keyProp != null)
                return keyProp.Name;

            // Convention-based: Id or {TypeName}Id
            var conventionalKey = props.FirstOrDefault(p =>
                p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Equals($"{type.Name}Id", StringComparison.OrdinalIgnoreCase));

            return conventionalKey?.Name ?? "Id";
        }

        /// <summary>
        /// Finds the foreign key property on a child type that references a parent type.
        /// Looks for properties named {ParentEntityName}Id or {ParentType.Name}Id.
        /// </summary>
        private static PropertyInfo FindChildForeignKeyProperty(string parentEntityName, Type childType)
        {
            if (string.IsNullOrEmpty(parentEntityName) || childType == null)
                return null;

            var props = childType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            // Expected FK names
            var fkCandidates = new[]
            {
                $"{parentEntityName}Id",
                $"{parentEntityName}Key",
                parentEntityName.TrimEnd('s') + "Id"
            };

            foreach (var candidate in fkCandidates)
            {
                var fkProp = props.FirstOrDefault(p =>
                    p.Name.Equals(candidate, StringComparison.OrdinalIgnoreCase) &&
                    IsIntegralType(p.PropertyType));

                if (fkProp != null)
                    return fkProp;
            }

            return null;
        }

        /// <summary>
        /// Checks if a type is suitable for a foreign key (int, long, Guid, etc.).
        /// </summary>
        private static bool IsIntegralType(Type type)
        {
            if (type == null)
                return false;

            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            return underlyingType == typeof(int) ||
                   underlyingType == typeof(long) ||
                   underlyingType == typeof(short) ||
                   underlyingType == typeof(byte) ||
                   underlyingType == typeof(uint) ||
                   underlyingType == typeof(ulong) ||
                   underlyingType == typeof(ushort) ||
                   underlyingType == typeof(Guid);
        }
    }
}
