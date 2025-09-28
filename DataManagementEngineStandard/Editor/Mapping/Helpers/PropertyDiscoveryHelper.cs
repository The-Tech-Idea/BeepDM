using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TheTechIdea.Beep.Editor.Mapping.Helpers
{
    /// <summary>
    /// Helper class for discovering and analyzing properties for mapping
    /// </summary>
    public class PropertyDiscoveryHelper
    {
        private readonly AutoObjMapperOptions _options;

        public PropertyDiscoveryHelper(AutoObjMapperOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Gets property mappings between source and destination types
        /// </summary>
        public List<PropertyMapping> GetPropertyMappings<TSource, TDest>()
        {
            var sourceType = typeof(TSource);
            var destType = typeof(TDest);

            var sourceProperties = GetReadableProperties(sourceType);
            var destProperties = GetWritableProperties(destType);

            var mappings = new List<PropertyMapping>();

            foreach (var destProp in destProperties)
            {
                var sourceProp = FindMatchingSourceProperty(sourceProperties, destProp.Name);
                mappings.Add(new PropertyMapping
                {
                    SourceProperty = sourceProp,
                    DestProperty = destProp
                });
            }

            return mappings;
        }

        /// <summary>
        /// Gets all readable properties from a type
        /// </summary>
        public Dictionary<string, PropertyInfo> GetReadableProperties(Type type)
        {
            return type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                      .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                      .ToDictionary(p => p.Name, p => p, _options.PropertyNameComparer);
        }

        /// <summary>
        /// Gets all writable properties from a type
        /// </summary>
        public List<PropertyInfo> GetWritableProperties(Type type)
        {
            var bindingFlags = BindingFlags.Instance | BindingFlags.Public;
            if (_options.IncludeNonPublicSetters)
                bindingFlags |= BindingFlags.NonPublic;

            return type.GetProperties(bindingFlags)
                      .Where(p => p.CanWrite && p.GetIndexParameters().Length == 0)
                      .ToList();
        }

        /// <summary>
        /// Finds a matching source property for the given destination property name
        /// </summary>
        private PropertyInfo FindMatchingSourceProperty(Dictionary<string, PropertyInfo> sourceProperties, string destPropertyName)
        {
            return sourceProperties.TryGetValue(destPropertyName, out var sourceProp) ? sourceProp : null;
        }
    }

    /// <summary>
    /// Represents a property mapping between source and destination
    /// </summary>
    public class PropertyMapping
    {
        /// <summary>
        /// Source property (can be null if no matching property found)
        /// </summary>
        public PropertyInfo SourceProperty { get; set; }

        /// <summary>
        /// Destination property
        /// </summary>
        public PropertyInfo DestProperty { get; set; }

        /// <summary>
        /// Indicates if this mapping has a valid source property
        /// </summary>
        public bool IsValid => SourceProperty != null;
    }
}