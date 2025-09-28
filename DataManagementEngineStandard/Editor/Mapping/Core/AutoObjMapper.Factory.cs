using System;
using TheTechIdea.Beep.Editor.Mapping.Configuration;
using TheTechIdea.Beep.Editor.Mapping.Interfaces;

namespace TheTechIdea.Beep.Editor.Mapping
{
    /// <summary>
    /// AutoObjMapper - Factory and Builder functionality
    /// Provides factory methods and builder pattern for creating mappers
    /// </summary>
    public sealed partial class AutoObjMapper
    {
        // Static factory methods
        public static class Factory
        {
            /// <summary>
            /// Creates a mapper with default options
            /// </summary>
            public static AutoObjMapper CreateDefault()
            {
                return new AutoObjMapper();
            }

            /// <summary>
            /// Creates a mapper with custom options
            /// </summary>
            public static AutoObjMapper CreateWithOptions(AutoObjMapperOptions options)
            {
                return new AutoObjMapper(options);
            }

            /// <summary>
            /// Creates a mapper using fluent configuration
            /// </summary>
            public static AutoObjMapper CreateWithConfiguration(Action<MapperOptionsBuilder> configure)
            {
                var builder = new MapperOptionsBuilder();
                configure?.Invoke(builder);
                return new AutoObjMapper(builder.Build());
            }

            /// <summary>
            /// Creates a high-performance mapper optimized for speed
            /// </summary>
            public static AutoObjMapper CreateHighPerformance()
            {
                var options = new AutoObjMapperOptions
                {
                    IgnoreNullSourceValues = false,
                    IncludeNonPublicSetters = false,
                    ThrowOnMappingError = false,
                    EnableStatistics = false,
                    PropertyNameComparer = StringComparer.Ordinal // Fastest comparer
                };
                return new AutoObjMapper(options);
            }

            /// <summary>
            /// Creates a mapper optimized for debugging and diagnostics
            /// </summary>
            public static AutoObjMapper CreateDiagnostic()
            {
                var options = new AutoObjMapperOptions
                {
                    IgnoreNullSourceValues = true,
                    IncludeNonPublicSetters = true,
                    ThrowOnMappingError = true,
                    EnableStatistics = true,
                    PropertyNameComparer = StringComparer.InvariantCultureIgnoreCase
                };
                return new AutoObjMapper(options);
            }
        }

        /// <summary>
        /// Builder class for creating AutoObjMapperOptions fluently
        /// </summary>
        public class MapperOptionsBuilder
        {
            private readonly AutoObjMapperOptions _options;

            public MapperOptionsBuilder()
            {
                _options = new AutoObjMapperOptions();
            }

            /// <summary>
            /// Configure null source value handling
            /// </summary>
            public MapperOptionsBuilder IgnoreNullSourceValues(bool ignore = true)
            {
                _options.IgnoreNullSourceValues = ignore;
                return this;
            }

            /// <summary>
            /// Configure non-public setter inclusion
            /// </summary>
            public MapperOptionsBuilder IncludeNonPublicSetters(bool include = true)
            {
                _options.IncludeNonPublicSetters = include;
                return this;
            }

            /// <summary>
            /// Configure error handling behavior
            /// </summary>
            public MapperOptionsBuilder ThrowOnMappingError(bool throwOnError = true)
            {
                _options.ThrowOnMappingError = throwOnError;
                return this;
            }

            /// <summary>
            /// Configure statistics collection
            /// </summary>
            public MapperOptionsBuilder EnableStatistics(bool enable = true)
            {
                _options.EnableStatistics = enable;
                return this;
            }

            /// <summary>
            /// Configure property name comparison
            /// </summary>
            public MapperOptionsBuilder UsePropertyNameComparer(System.Collections.Generic.IEqualityComparer<string> comparer)
            {
                _options.PropertyNameComparer = comparer ?? StringComparer.InvariantCultureIgnoreCase;
                return this;
            }

            /// <summary>
            /// Configure case-sensitive property matching
            /// </summary>
            public MapperOptionsBuilder UseCaseSensitivePropertyMatching(bool caseSensitive = true)
            {
                _options.PropertyNameComparer = caseSensitive ? 
                    StringComparer.Ordinal : 
                    StringComparer.InvariantCultureIgnoreCase;
                return this;
            }

            /// <summary>
            /// Configure maximum depth for circular reference detection
            /// </summary>
            public MapperOptionsBuilder WithMaxDepth(int maxDepth)
            {
                if (maxDepth < 1)
                    throw new ArgumentOutOfRangeException(nameof(maxDepth), "Maximum depth must be at least 1");
                
                _options.MaxDepth = maxDepth;
                return this;
            }

            /// <summary>
            /// Builds the AutoObjMapperOptions
            /// </summary>
            public AutoObjMapperOptions Build()
            {
                return _options;
            }
        }
    }
}