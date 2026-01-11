using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Helpers.UniversalDataSourceHelpers.Analysis
{
    /// <summary>
    /// Infers relationship cardinality and constraints from POCO object models.
    /// Determines whether relationships are One-to-One, One-to-Many, Many-to-Many,
    /// or Self-Referencing based on property patterns, attributes, and type analysis.
    /// 
    /// Decision Priority:
    /// 1. Explicit attributes ([ForeignKey], [InverseProperty])
    /// 2. Naming conventions (XXXId for foreign keys)
    /// 3. Type analysis (ICollection indicates collection side)
    /// 4. Bidirectional detection (matching navigation properties)
    /// </summary>
    public static class RelationshipInferencer
    {
        /// <summary>
        /// Infers the cardinality of a relationship between source and target types
        /// based on a source navigation property.
        /// </summary>
        /// <param name="sourceNavProperty">Navigation property from source type</param>
        /// <param name="sourceType">Type containing the navigation property</param>
        /// <param name="targetType">Type being referenced by navigation property</param>
        /// <returns>Inferred relationship cardinality</returns>
        public static RelationshipCardinality InferCardinality(
            PropertyInfo sourceNavProperty,
            Type sourceType,
            Type targetType)
        {
            if (sourceNavProperty == null || sourceType == null || targetType == null)
                return RelationshipCardinality.Unknown;

            // Detect self-reference first
            if (sourceType == targetType)
                return RelationshipCardinality.SelfReference;

            // Check for explicit attributes
            var fkAttribute = sourceNavProperty.GetCustomAttribute<ForeignKeyAttribute>();
            if (fkAttribute != null)
            {
                // Explicit FK attribute indicates direction
                // If collection, it's one-to-many on the other side
                if (NavigationPropertyDetector.IsCollectionNavigation(sourceNavProperty))
                    return RelationshipCardinality.OneToMany;

                // If single reference with FK, it's many-to-one
                return RelationshipCardinality.ManyToOne;
            }

            // Analyze based on navigation property type
            var navType = NavigationPropertyDetector.GetNavigationPropertyType(sourceNavProperty);

            if (navType == NavigationPropertyType.Collection)
            {
                // Collection from source to target = One-to-Many
                // Unless we detect Many-to-Many pattern
                if (IsManyToManyPattern(sourceNavProperty, sourceType, targetType))
                    return RelationshipCardinality.ManyToMany;

                return RelationshipCardinality.OneToMany;
            }

            if (navType == NavigationPropertyType.SingleReference)
            {
                // Single reference = either One-to-One or Many-to-One
                // Check if target has matching collection property
                var targetHasCollection = HasCollectionNavigationTo(targetType, sourceType);

                if (targetHasCollection)
                {
                    // Target has collection pointing back = Many-to-One
                    return RelationshipCardinality.ManyToOne;
                }

                // No collection on other side = One-to-One
                return RelationshipCardinality.OneToOne;
            }

            return RelationshipCardinality.Unknown;
        }

        /// <summary>
        /// Detects the foreign key property for a navigation relationship.
        /// Looks for properties named {NavigationName}Id, {ReferencedTypeName}Id, or
        /// marked with [ForeignKey] attribute.
        /// </summary>
        /// <param name="navigationProperty">Navigation property to find FK for</param>
        /// <param name="sourceType">Type containing the navigation property</param>
        /// <returns>Foreign key property, or null if not found or not applicable</returns>
        public static PropertyInfo DetectForeignKeyProperty(PropertyInfo navigationProperty, Type sourceType)
        {
            if (navigationProperty == null || sourceType == null)
                return null;

            // Check for explicit [ForeignKey] attribute
            var fkAttribute = navigationProperty.GetCustomAttribute<ForeignKeyAttribute>();
            if (fkAttribute != null)
            {
                var allProps = GetPublicProperties(sourceType);
                var fkProp = allProps.FirstOrDefault(p =>
                    p.Name.Equals(fkAttribute.Name, StringComparison.OrdinalIgnoreCase));

                if (fkProp != null)
                    return fkProp;
            }

            // Use detector helper to find FK by convention
            return NavigationPropertyDetector.FindForeignKeyProperty(navigationProperty, sourceType);
        }

        /// <summary>
        /// Detects if a navigation property represents a many-to-many relationship.
        /// Typically indicated by collection properties on both sides without
        /// explicit foreign key on either side.
        /// </summary>
        /// <param name="navigationProperty">Navigation property to check</param>
        /// <param name="sourceType">Type containing the property</param>
        /// <param name="targetType">Type being referenced</param>
        /// <returns>true if many-to-many pattern detected</returns>
        public static bool IsManyToManyPattern(PropertyInfo navigationProperty, Type sourceType, Type targetType)
        {
            if (navigationProperty == null || sourceType == null || targetType == null)
                return false;

            // Many-to-many requires collection on source
            if (!NavigationPropertyDetector.IsCollectionNavigation(navigationProperty))
                return false;

            // Check if target has a collection property pointing back to source
            var targetProps = GetPublicProperties(targetType);
            var reverseCollection = targetProps.FirstOrDefault(p =>
                NavigationPropertyDetector.IsCollectionNavigation(p) &&
                NavigationPropertyDetector.GetReferencedType(p) == sourceType);

            if (reverseCollection == null)
                return false;

            // Both sides are collections = many-to-many pattern
            // Also check: neither side should have an explicit FK
            var sourceFk = DetectForeignKeyProperty(navigationProperty, sourceType);
            if (sourceFk != null)
                return false;  // Has FK = one-to-many, not many-to-many

            return true;
        }

        /// <summary>
        /// Detects if a relationship is bidirectional (both types have navigation properties
        /// pointing to each other).
        /// </summary>
        /// <param name="sourceNavProperty">Navigation property from source</param>
        /// <param name="sourceType">Source type</param>
        /// <param name="targetType">Target type</param>
        /// <returns>true if bidirectional relationship detected</returns>
        public static bool IsBidirectional(PropertyInfo sourceNavProperty, Type sourceType, Type targetType)
        {
            if (sourceNavProperty == null || sourceType == null || targetType == null)
                return false;

            var inverseProperty = NavigationPropertyDetector.FindInverseNavigationProperty(sourceNavProperty, sourceType);
            return inverseProperty != null;
        }

        /// <summary>
        /// Gets the inverse property name following naming conventions.
        /// For Orders collection on Customer, inverse is typically Customer on Order.
        /// </summary>
        /// <param name="navigationProperty">Navigation property</param>
        /// <param name="sourceType">Source type</param>
        /// <returns>Expected inverse property name, or null if not determinable</returns>
        public static string GetInversePropertyName(PropertyInfo navigationProperty, Type sourceType)
        {
            if (navigationProperty == null || sourceType == null)
                return null;

            // Check for explicit [InverseProperty] attribute
            var inverseAttr = navigationProperty.GetCustomAttribute<InversePropertyAttribute>();
            if (inverseAttr != null)
                return inverseAttr.Property;

            // Convention-based naming:
            // - If navigation is singular (Customer), inverse might be {SourceName}s or keep singular
            // - If navigation is plural (Orders), inverse is typically singular (Order → Customer)

            var navName = navigationProperty.Name;
            var sourceName = sourceType.Name;

            // For collections: "Orders" → "Order" (singular) or plural back to source
            if (NavigationPropertyDetector.IsCollectionNavigation(navigationProperty))
            {
                // Try singular form: Orders → Order, Customers → Customer
                var singularName = navName.EndsWith("s") ? navName.Substring(0, navName.Length - 1) : navName;
                if (singularName != navName)
                    return singularName;

                // If no plural form, try source name
                return sourceName;
            }

            // For single references: Customer → Customers or Customers
            if (NavigationPropertyDetector.IsSingleObjectNavigation(navigationProperty))
            {
                // Try pluralized form
                return navName + "s";
            }

            return null;
        }

        /// <summary>
        /// Detects if a type has a cascade delete relationship with another type.
        /// Cascade delete typically occurs on the "many" side pointing to "one" side.
        /// </summary>
        /// <param name="navigationProperty">Navigation property to check</param>
        /// <returns>true if cascade delete behavior expected</returns>
        public static bool ShouldCascadeDelete(PropertyInfo navigationProperty)
        {
            if (navigationProperty == null)
                return false;

            // Check for explicit [DeleteBehavior] if available in EF attributes
            // For now, return true for collection properties (one-to-many cascade delete typical)
            return NavigationPropertyDetector.IsCollectionNavigation(navigationProperty);
        }

        /// <summary>
        /// Analyzes relationship directionality and returns cardinality from both perspectives.
        /// Useful for bidirectional relationships.
        /// </summary>
        /// <param name="sourceNavProperty">Navigation from source</param>
        /// <param name="sourceType">Source type</param>
        /// <param name="targetType">Target type</param>
        /// <returns>Relationship analysis including both directions</returns>
        public static RelationshipAnalysis AnalyzeRelationship(
            PropertyInfo sourceNavProperty,
            Type sourceType,
            Type targetType)
        {
            var sourceCardinality = InferCardinality(sourceNavProperty, sourceType, targetType);
            var isBidirectional = IsBidirectional(sourceNavProperty, sourceType, targetType);
            var targetCardinality = RelationshipCardinality.Unknown;

            if (isBidirectional)
            {
                var inverseProperty = NavigationPropertyDetector.FindInverseNavigationProperty(sourceNavProperty, sourceType);
                if (inverseProperty != null)
                {
                    targetCardinality = InferCardinality(inverseProperty, targetType, sourceType);
                }
            }

            var foreignKeyProp = DetectForeignKeyProperty(sourceNavProperty, sourceType);
            var inversePropertyName = GetInversePropertyName(sourceNavProperty, sourceType);
            var cascadeDelete = ShouldCascadeDelete(sourceNavProperty);

            return new RelationshipAnalysis
            {
                SourceType = sourceType,
                TargetType = targetType,
                SourceProperty = sourceNavProperty,
                SourceCardinality = sourceCardinality,
                TargetCardinality = targetCardinality,
                IsBidirectional = isBidirectional,
                ForeignKeyProperty = foreignKeyProp,
                InversePropertyName = inversePropertyName,
                ShouldCascadeDelete = cascadeDelete,
                IsLazyLoaded = NavigationPropertyDetector.IsVirtualNavigation(sourceNavProperty)
            };
        }

        // ==================== Helper Methods ====================

        /// <summary>
        /// Checks if a type has a collection navigation property pointing to another type.
        /// </summary>
        private static bool HasCollectionNavigationTo(Type sourceType, Type targetType)
        {
            if (sourceType == null || targetType == null)
                return false;

            var props = GetPublicProperties(sourceType);
            return props.Any(p =>
                NavigationPropertyDetector.IsCollectionNavigation(p) &&
                NavigationPropertyDetector.GetReferencedType(p) == targetType);
        }

        /// <summary>
        /// Gets all public properties from a type, excluding indexers.
        /// </summary>
        private static PropertyInfo[] GetPublicProperties(Type type)
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                .Where(p => p.GetIndexParameters().Length == 0)
                .ToArray();
        }
    }

    /// <summary>
    /// Categorizes relationship cardinality.
    /// </summary>
    public enum RelationshipCardinality
    {
        /// <summary>
        /// One-to-One: Each source entity has exactly one target entity.
        /// Example: Customer has one Address, Address belongs to one Customer.
        /// </summary>
        OneToOne,

        /// <summary>
        /// One-to-Many: One source entity has many target entities.
        /// Example: Customer has many Orders.
        /// </summary>
        OneToMany,

        /// <summary>
        /// Many-to-One: Many source entities have one target entity.
        /// Inverse of One-to-Many from target perspective.
        /// Example: Many Orders belong to one Customer.
        /// </summary>
        ManyToOne,

        /// <summary>
        /// Many-to-Many: Source and target entities have many-to-many relationship.
        /// Usually requires a joining table in database.
        /// Example: Student has many Courses, Course has many Students.
        /// </summary>
        ManyToMany,

        /// <summary>
        /// Self-Reference: Entity references itself (hierarchical relationships).
        /// Example: Employee has Manager (which is also an Employee).
        /// </summary>
        SelfReference,

        /// <summary>
        /// Could not determine relationship cardinality.
        /// </summary>
        Unknown
    }

    /// <summary>
    /// Complete relationship analysis result.
    /// </summary>
    public class RelationshipAnalysis
    {
        /// <summary>
        /// Source entity type.
        /// </summary>
        public Type SourceType { get; set; }

        /// <summary>
        /// Target entity type.
        /// </summary>
        public Type TargetType { get; set; }

        /// <summary>
        /// Navigation property from source to target.
        /// </summary>
        public PropertyInfo SourceProperty { get; set; }

        /// <summary>
        /// Cardinality from source perspective.
        /// </summary>
        public RelationshipCardinality SourceCardinality { get; set; }

        /// <summary>
        /// Cardinality from target perspective (if bidirectional).
        /// </summary>
        public RelationshipCardinality TargetCardinality { get; set; }

        /// <summary>
        /// Whether relationship is bidirectional.
        /// </summary>
        public bool IsBidirectional { get; set; }

        /// <summary>
        /// Foreign key property (if applicable).
        /// </summary>
        public PropertyInfo ForeignKeyProperty { get; set; }

        /// <summary>
        /// Expected name of inverse navigation property.
        /// </summary>
        public string InversePropertyName { get; set; }

        /// <summary>
        /// Whether deleting source should cascade delete target.
        /// </summary>
        public bool ShouldCascadeDelete { get; set; }

        /// <summary>
        /// Whether relationship uses lazy loading (virtual properties).
        /// </summary>
        public bool IsLazyLoaded { get; set; }

        /// <summary>
        /// Display-friendly summary.
        /// </summary>
        public override string ToString()
        {
            var direction = IsBidirectional ? $"{SourceCardinality} ↔ {TargetCardinality}" : $"{SourceCardinality}";
            return $"{SourceType?.Name} {direction} {TargetType?.Name}";
        }
    }
}
