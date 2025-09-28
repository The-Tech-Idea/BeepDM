using System;
using TheTechIdea.Beep.Editor.Mapping.Helpers;

namespace TheTechIdea.Beep.Editor.Mapping
{
    /// <summary>
    /// Static helper methods and utilities for AutoObjMapper
    /// Provides convenient static access to common mapping operations
    /// </summary>
    public static class AutoObjMapperUtilities
    {
        private static readonly Lazy<AutoObjMapper> _defaultMapper = new(() => new AutoObjMapper());

        /// <summary>
        /// Gets the default mapper instance (thread-safe singleton)
        /// </summary>
        public static AutoObjMapper DefaultMapper => _defaultMapper.Value;

        /// <summary>
        /// Maps using the default mapper instance
        /// </summary>
        /// <typeparam name="TSource">Source type</typeparam>
        /// <typeparam name="TDest">Destination type</typeparam>
        /// <param name="source">Source object</param>
        /// <returns>Mapped destination object</returns>
        public static TDest Map<TSource, TDest>(TSource source) where TDest : new()
        {
            return _defaultMapper.Value.Map<TSource, TDest>(source);
        }

        /// <summary>
        /// Maps using the default mapper instance to an existing destination
        /// </summary>
        /// <typeparam name="TSource">Source type</typeparam>
        /// <typeparam name="TDest">Destination type</typeparam>
        /// <param name="source">Source object</param>
        /// <param name="destination">Destination object</param>
        /// <returns>The destination object with mapped values</returns>
        public static TDest Map<TSource, TDest>(TSource source, TDest destination)
        {
            return _defaultMapper.Value.Map(source, destination);
        }

        /// <summary>
        /// Type conversion utility method
        /// </summary>
        /// <param name="value">Value to convert</param>
        /// <param name="targetType">Target type</param>
        /// <returns>Converted value</returns>
        public static object TryConvert(object value, Type targetType)
        {
            return TypeConversionHelper.TryConvert(value, targetType);
        }

        /// <summary>
        /// Safe mapping that returns a result instead of throwing exceptions
        /// </summary>
        /// <typeparam name="TSource">Source type</typeparam>
        /// <typeparam name="TDest">Destination type</typeparam>
        /// <param name="source">Source object</param>
        /// <returns>Mapping result with success/error information</returns>
        public static MappingResult<TDest> TryMap<TSource, TDest>(TSource source) where TDest : new()
        {
            return _defaultMapper.Value.TryMap<TSource, TDest>(source);
        }

        /// <summary>
        /// Safe mapping to an existing destination that returns a result instead of throwing exceptions
        /// </summary>
        /// <typeparam name="TSource">Source type</typeparam>
        /// <typeparam name="TDest">Destination type</typeparam>
        /// <param name="source">Source object</param>
        /// <param name="destination">Destination object</param>
        /// <returns>Mapping result with success/error information</returns>
        public static MappingResult<TDest> TryMap<TSource, TDest>(TSource source, TDest destination)
        {
            return _defaultMapper.Value.TryMap(source, destination);
        }

        /// <summary>
        /// Validates mapping between two types using the default mapper
        /// </summary>
        /// <typeparam name="TSource">Source type</typeparam>
        /// <typeparam name="TDest">Destination type</typeparam>
        /// <returns>Validation result</returns>
        public static MappingValidationResult ValidateMapping<TSource, TDest>()
        {
            return _defaultMapper.Value.ValidateMapping<TSource, TDest>();
        }

        /// <summary>
        /// Gets statistics from the default mapper
        /// </summary>
        /// <returns>Mapping statistics</returns>
        public static MappingStatistics GetStatistics()
        {
            return _defaultMapper.Value.GetStatistics();
        }

        /// <summary>
        /// Clears the cache of the default mapper
        /// </summary>
        public static void ClearCache()
        {
            _defaultMapper.Value.ClearCache();
        }

        /// <summary>
        /// Creates a new mapper with high-performance configuration
        /// </summary>
        /// <returns>High-performance configured mapper</returns>
        public static AutoObjMapper CreateFastMapper()
        {
            return AutoObjMapper.Factory.CreateHighPerformance();
        }

        /// <summary>
        /// Creates a new mapper with diagnostic configuration
        /// </summary>
        /// <returns>Diagnostic configured mapper</returns>
        public static AutoObjMapper CreateDiagnosticMapper()
        {
            return AutoObjMapper.Factory.CreateDiagnostic();
        }
    }
}