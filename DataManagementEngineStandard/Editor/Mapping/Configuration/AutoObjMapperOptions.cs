using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.Mapping
{
    /// <summary>
    /// Global options for the AutoObjMapper
    /// </summary>
    public class AutoObjMapperOptions
    {
        /// <summary>
        /// Default options instance
        /// </summary>
        public static AutoObjMapperOptions Default => new AutoObjMapperOptions();

        /// <summary>
        /// When true, null values in source properties will not overwrite destination properties
        /// </summary>
        public bool IgnoreNullSourceValues { get; set; } = true;

        /// <summary>
        /// When true, includes non-public setters in destination properties
        /// </summary>
        public bool IncludeNonPublicSetters { get; set; } = false;

        /// <summary>
        /// Comparer used for property name matching
        /// </summary>
        public IEqualityComparer<string> PropertyNameComparer { get; set; } = StringComparer.InvariantCultureIgnoreCase;

        /// <summary>
        /// When true, throws exceptions for mapping errors. When false, logs errors and continues.
        /// </summary>
        public bool ThrowOnMappingError { get; set; } = false;

        /// <summary>
        /// Maximum depth for circular reference detection
        /// </summary>
        public int MaxDepth { get; set; } = 10;

        /// <summary>
        /// When true, enables performance monitoring and statistics collection
        /// </summary>
        public bool EnableStatistics { get; set; } = false;
    }

    /// <summary>
    /// Statistics about mapping operations
    /// </summary>
    public class MappingStatistics
    {
        /// <summary>
        /// Number of cached compiled mappers
        /// </summary>
        public int CachedMappersCount { get; set; }

        /// <summary>
        /// Number of registered type maps
        /// </summary>
        public int RegisteredTypeMapsCount { get; set; }

        /// <summary>
        /// Total number of mappings performed
        /// </summary>
        public long TotalMappingsPerformed { get; set; }

        /// <summary>
        /// Total time spent on mappings
        /// </summary>
        public TimeSpan TotalMappingTime { get; set; }
    }
}