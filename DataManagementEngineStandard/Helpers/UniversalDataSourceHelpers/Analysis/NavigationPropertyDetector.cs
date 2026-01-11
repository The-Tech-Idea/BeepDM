using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Helpers.UniversalDataSourceHelpers.Analysis
{
    /// <summary>
    /// Analyzes POCO properties to detect navigation properties and relationship patterns.
    /// Enables automatic identification of foreign keys, collections, and object references
    /// for advanced relationship mapping.
    /// 
    /// Navigation Properties are properties that represent relationships between entities:
    /// - Collection properties (ICollection<T>, List<T>, IEnumerable<T>)
    /// - Single object references (Customer, Order, etc.)
    /// - Virtual properties (used by ORMs for lazy loading)
    /// 
    /// This is distinct from scalar fields which represent database columns.
    /// </summary>
    public static class NavigationPropertyDetector
    {
        /// <summary>
        /// Detects if a property is a collection-based navigation property.
        /// Examples: ICollection<Order>, List<Customer>, IEnumerable<Product>
        /// </summary>
        /// <param name="prop">Property to analyze</param>
        /// <returns>true if property represents a collection of complex objects</returns>
        public static bool IsCollectionNavigation(PropertyInfo prop)
        {
            if (prop == null)
                return false;

            var propType = prop.PropertyType;

            // Check if it's a collection type (but not string or byte[])
            if (typeof(System.Collections.IEnumerable).IsAssignableFrom(propType) &&
                propType != typeof(string) && propType != typeof(byte[]))
                return true;

            return false;
        }

        /// <summary>
        /// Detects if a property is a single object reference navigation property.
        /// Examples: Customer customer, Order order (not ICollection)
        /// </summary>
        /// <param name="prop">Property to analyze</param>
        /// <returns>true if property references a single complex object</returns>
        public static bool IsSingleObjectNavigation(PropertyInfo prop)
        {
            if (prop == null)
                return false;

            var propType = prop.PropertyType;

            // Must not be a primitive, string, or collection
            if (propType.IsPrimitive || propType == typeof(string) || 
                propType == typeof(decimal) || propType == typeof(DateTime) ||
                propType == typeof(Guid) || propType == typeof(byte[]))
                return false;

            // Exclude DateTime, TimeSpan, and other common value types
            if (propType.IsValueType && !IsNullableType(propType))
                return false;

            // Exclude collections
            if (typeof(System.Collections.IEnumerable).IsAssignableFrom(propType))
                return false;

            // If we get here, it's a complex type (class reference)
            return !propType.IsPrimitive;
        }

        /// <summary>
        /// Detects if a property uses virtual access (typical of ORM lazy loading).
        /// Examples: virtual Customer customer, virtual ICollection<Order> Orders
        /// </summary>
        /// <param name="prop">Property to analyze</param>
        /// <returns>true if property getter/setter is virtual</returns>
        public static bool IsVirtualNavigation(PropertyInfo prop)
        {
            if (prop == null)
                return false;

            var getMethod = prop.GetGetMethod();
            var setMethod = prop.GetSetMethod();

            // Check if getter is virtual (but not final, which means it was overridden)
            if (getMethod != null && getMethod.IsVirtual && !getMethod.IsFinal)
                return true;

            // Check if setter is virtual
            if (setMethod != null && setMethod.IsVirtual && !setMethod.IsFinal)
                return true;

            return false;
        }

        /// <summary>
        /// Comprehensive check: detects if a property is ANY type of navigation property.
        /// </summary>
        /// <param name="prop">Property to analyze</param>
        /// <returns>true if property represents a relationship (collection, reference, or virtual)</returns>
        public static bool IsNavigationProperty(PropertyInfo prop)
        {
            return IsCollectionNavigation(prop) || 
                   IsSingleObjectNavigation(prop) || 
                   IsVirtualNavigation(prop);
        }

        /// <summary>
        /// Gets the underlying type that a property references (for collections).
        /// For ICollection<Order>, returns Order.
        /// For Order, returns Order.
        /// </summary>
        /// <param name="prop">Navigation property to analyze</param>
        /// <returns>The referenced type, or null if not a valid navigation property</returns>
        public static Type GetReferencedType(PropertyInfo prop)
        {
            if (prop == null)
                return null;

            var propType = prop.PropertyType;

            // If it's a collection, get the element type
            if (IsCollectionNavigation(prop))
            {
                // Handle generic collections: ICollection<T>, List<T>, etc.
                if (propType.IsGenericType)
                {
                    var genericArgs = propType.GetGenericArguments();
                    if (genericArgs.Length > 0)
                        return genericArgs[0];
                }

                // Handle non-generic IEnumerable (fallback)
                var enumerableInterface = propType.GetInterface("IEnumerable`1");
                if (enumerableInterface != null)
                {
                    var genericArgs = enumerableInterface.GetGenericArguments();
                    if (genericArgs.Length > 0)
                        return genericArgs[0];
                }
            }

            // If it's a single object reference, return the type directly
            if (IsSingleObjectNavigation(prop))
                return propType;

            // Handle nullable types: Order? → Order
            var underlyingType = Nullable.GetUnderlyingType(propType);
            if (underlyingType != null && IsSingleObjectNavigation(prop))
                return underlyingType;

            return null;
        }

        /// <summary>
        /// Detects if a property is a nullable reference type (e.g., Order?, Customer?).
        /// </summary>
        /// <param name="prop">Property to analyze</param>
        /// <returns>true if property type is nullable</returns>
        public static bool IsNullableReference(PropertyInfo prop)
        {
            if (prop == null)
                return false;

            var propType = prop.PropertyType;

            // Check for Nullable<T>
            if (Nullable.GetUnderlyingType(propType) != null)
                return true;

            // For reference types, check if marked as nullable (C# 8.0+)
            // This is a simplified check; nullable context requires deeper analysis
            return !propType.IsValueType && propType != typeof(string);
        }

        /// <summary>
        /// Detects if a property could be a foreign key field.
        /// Looks for properties named {NavigationPropertyName}Id or {ClassName}Id.
        /// </summary>
        /// <param name="navProperty">Navigation property to find FK for</param>
        /// <param name="entityType">Type containing both properties</param>
        /// <returns>Foreign key property, or null if not found</returns>
        public static PropertyInfo FindForeignKeyProperty(PropertyInfo navProperty, Type entityType)
        {
            if (navProperty == null || entityType == null)
                return null;

            var referencedType = GetReferencedType(navProperty);
            if (referencedType == null)
                return null;

            var allProperties = GetPublicProperties(entityType);
            
            // Expected FK name patterns
            var fkCandidates = new[]
            {
                $"{navProperty.Name}Id",                    // OrderId for Order property
                $"{referencedType.Name}Id",                // OrderId for referenced Order type
                $"{navProperty.Name}Key",                  // OrderKey for Order property
                navProperty.Name.TrimEnd('s') + "Id"      // OrderId for Orders property
            };

            foreach (var candidate in fkCandidates)
            {
                var fkProp = allProperties.FirstOrDefault(p =>
                    p.Name.Equals(candidate, StringComparison.OrdinalIgnoreCase) &&
                    IsIntegralType(p.PropertyType));

                if (fkProp != null)
                    return fkProp;
            }

            return null;
        }

        /// <summary>
        /// Detects if a type has an inverse navigation property pointing back to the source type.
        /// For example, if Customer has Orders property, Order.Customer is the inverse.
        /// </summary>
        /// <param name="navProperty">Source navigation property</param>
        /// <param name="sourceType">Type containing the source property</param>
        /// <returns>Inverse property, or null if not found or one-way relationship</returns>
        public static PropertyInfo FindInverseNavigationProperty(PropertyInfo navProperty, Type sourceType)
        {
            if (navProperty == null || sourceType == null)
                return null;

            var referencedType = GetReferencedType(navProperty);
            if (referencedType == null)
                return null;

            var referencedProperties = GetPublicProperties(referencedType);

            // Look for properties that reference back to the source type
            foreach (var prop in referencedProperties)
            {
                if (!IsNavigationProperty(prop))
                    continue;

                var propReferences = GetReferencedType(prop);
                if (propReferences == sourceType)
                    return prop;
            }

            return null;
        }

        /// <summary>
        /// Gets the cardinality category of a navigation property.
        /// </summary>
        /// <param name="prop">Navigation property to analyze</param>
        /// <returns>Collection indicates one-to-many; single reference indicates one-to-one or many-to-one</returns>
        public static NavigationPropertyType GetNavigationPropertyType(PropertyInfo prop)
        {
            if (prop == null)
                return NavigationPropertyType.Unknown;

            if (IsCollectionNavigation(prop))
                return NavigationPropertyType.Collection;

            if (IsSingleObjectNavigation(prop))
                return NavigationPropertyType.SingleReference;

            return NavigationPropertyType.Unknown;
        }

        /// <summary>
        /// Extracts all navigation properties from a type.
        /// </summary>
        /// <param name="type">Type to analyze</param>
        /// <returns>List of all navigation properties found</returns>
        public static List<PropertyInfo> GetNavigationProperties(Type type)
        {
            if (type == null)
                return new List<PropertyInfo>();

            var allProps = GetPublicProperties(type);
            return allProps.Where(IsNavigationProperty).ToList();
        }

        /// <summary>
        /// Extracts all scalar (non-navigation) properties from a type.
        /// </summary>
        /// <param name="type">Type to analyze</param>
        /// <returns>List of all scalar properties found</returns>
        public static List<PropertyInfo> GetScalarProperties(Type type)
        {
            if (type == null)
                return new List<PropertyInfo>();

            var allProps = GetPublicProperties(type);
            return allProps.Where(p => !IsNavigationProperty(p)).ToList();
        }

        /// <summary>
        /// Comprehensive relationship analysis: returns all navigation info for a property.
        /// </summary>
        /// <param name="navProperty">Navigation property to analyze</param>
        /// <param name="sourceType">Type containing the navigation property</param>
        /// <returns>Complete navigation relationship details</returns>
        public static NavigationPropertyInfo AnalyzeNavigationProperty(PropertyInfo navProperty, Type sourceType)
        {
            if (navProperty == null || sourceType == null)
                return null;

            var referencedType = GetReferencedType(navProperty);
            if (referencedType == null)
                return null;

            return new NavigationPropertyInfo
            {
                PropertyName = navProperty.Name,
                PropertyType = navProperty.PropertyType,
                NavigationType = GetNavigationPropertyType(navProperty),
                ReferencedType = referencedType,
                SourceType = sourceType,
                IsVirtual = IsVirtualNavigation(navProperty),
                IsNullable = IsNullableReference(navProperty),
                ForeignKeyProperty = FindForeignKeyProperty(navProperty, sourceType),
                InverseProperty = FindInverseNavigationProperty(navProperty, sourceType)
            };
        }

        // ==================== Helper Methods ====================

        /// <summary>
        /// Gets all public properties from a type, excluding indexers.
        /// </summary>
        private static PropertyInfo[] GetPublicProperties(Type type)
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                .Where(p => p.GetIndexParameters().Length == 0)  // Exclude indexers
                .ToArray();
        }

        /// <summary>
        /// Determines if a type is nullable (Nullable<T> or reference type).
        /// </summary>
        private static bool IsNullableType(Type type)
        {
            if (type == null)
                return true;

            if (type.IsValueType)
                return Nullable.GetUnderlyingType(type) != null;

            return true;
        }

        /// <summary>
        /// Determines if a type is an integral type (int, long, short, etc.).
        /// Used for foreign key detection.
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

    /// <summary>
    /// Categorizes the type of navigation property.
    /// </summary>
    public enum NavigationPropertyType
    {
        /// <summary>
        /// Collection type (ICollection<T>, List<T>): represents one-to-many or many-to-many.
        /// </summary>
        Collection,

        /// <summary>
        /// Single object reference: represents one-to-one or many-to-one relationship.
        /// </summary>
        SingleReference,

        /// <summary>
        /// Could not determine navigation property type.
        /// </summary>
        Unknown
    }

    /// <summary>
    /// Complete analysis result for a navigation property.
    /// Contains all metadata needed to infer relationships and cardinality.
    /// </summary>
    public class NavigationPropertyInfo
    {
        /// <summary>
        /// Name of the navigation property (e.g., "Orders", "Customer").
        /// </summary>
        public string PropertyName { get; set; }

        /// <summary>
        /// C# type of the property (e.g., ICollection<Order>, Customer).
        /// </summary>
        public Type PropertyType { get; set; }

        /// <summary>
        /// Category of navigation property (Collection or SingleReference).
        /// </summary>
        public NavigationPropertyType NavigationType { get; set; }

        /// <summary>
        /// The type being referenced (e.g., Order for ICollection<Order> or Customer).
        /// </summary>
        public Type ReferencedType { get; set; }

        /// <summary>
        /// The type containing this navigation property.
        /// </summary>
        public Type SourceType { get; set; }

        /// <summary>
        /// Whether property uses virtual access (ORM lazy loading indicator).
        /// </summary>
        public bool IsVirtual { get; set; }

        /// <summary>
        /// Whether property can be null (nullable reference type or nullable FK).
        /// </summary>
        public bool IsNullable { get; set; }

        /// <summary>
        /// Foreign key property if found, e.g., CustomerId for Customer property.
        /// Null for collection navigation (FK is on the child).
        /// </summary>
        public PropertyInfo ForeignKeyProperty { get; set; }

        /// <summary>
        /// Inverse navigation property if bidirectional relationship detected.
        /// E.g., Customer property on Order if Orders collection found on Customer.
        /// </summary>
        public PropertyInfo InverseProperty { get; set; }

        /// <summary>
        /// Display-friendly summary of the relationship.
        /// </summary>
        public override string ToString()
        {
            var relationship = InverseProperty != null ? "bidirectional" : "unidirectional";
            var fkInfo = ForeignKeyProperty != null ? $" (FK: {ForeignKeyProperty.Name})" : "";
            return $"{PropertyName}: {NavigationType} → {ReferencedType?.Name} ({relationship}){fkInfo}";
        }
    }
}
