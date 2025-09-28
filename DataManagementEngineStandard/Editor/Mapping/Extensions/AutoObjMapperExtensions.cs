using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Editor.Mapping.Helpers;

namespace TheTechIdea.Beep.Editor.Mapping.Extensions
{
    /// <summary>
    /// Extension methods for AutoObjMapper to provide additional utility functions
    /// </summary>
    public static class AutoObjMapperExtensions
    {
        /// <summary>
        /// Maps a collection of objects
        /// </summary>
        public static IEnumerable<TDest> MapCollection<TSource, TDest>(
            this AutoObjMapper mapper, 
            IEnumerable<TSource> source) 
            where TDest : new()
        {
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));
            if (source == null) return Enumerable.Empty<TDest>();

            return source.Select(mapper.Map<TSource, TDest>);
        }

        /// <summary>
        /// Maps a collection of objects to an existing collection
        /// </summary>
        public static void MapCollection<TSource, TDest>(
            this AutoObjMapper mapper,
            IEnumerable<TSource> source,
            ICollection<TDest> destination,
            Func<TDest> destinationFactory)
        {
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            if (destinationFactory == null) throw new ArgumentNullException(nameof(destinationFactory));

            if (source == null) return;

            foreach (var item in source)
            {
                var dest = destinationFactory();
                mapper.Map(item, dest);
                destination.Add(dest);
            }
        }

        /// <summary>
        /// Maps objects with performance monitoring enabled
        /// </summary>
        public static TDest MapWithPerformanceTracking<TSource, TDest>(
            this AutoObjMapper mapper,
            TSource source,
            TDest destination,
            out TimeSpan elapsed)
        {
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));

            elapsed = TimeSpan.Zero;
            var start = DateTime.UtcNow;
            
            try
            {
                return mapper.Map(source, destination);
            }
            finally
            {
                elapsed = DateTime.UtcNow - start;
            }
        }

        /// <summary>
        /// Creates a fluent configuration builder
        /// </summary>
        public static FluentConfigurationBuilder<TSource, TDest> CreateConfiguration<TSource, TDest>(
            this AutoObjMapper mapper)
        {
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));
            return new FluentConfigurationBuilder<TSource, TDest>(mapper);
        }

        /// <summary>
        /// Validates and maps with detailed result information
        /// </summary>
        public static DetailedMappingResult<TDest> MapWithDetails<TSource, TDest>(
            this AutoObjMapper mapper,
            TSource source,
            TDest destination)
        {
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));

            var validation = mapper.ValidateMapping<TSource, TDest>();
            var start = DateTime.UtcNow;
            Exception mappingException = null;
            TDest result = default;

            try
            {
                result = mapper.Map(source, destination);
            }
            catch (Exception ex)
            {
                mappingException = ex;
            }

            var elapsed = DateTime.UtcNow - start;

            return new DetailedMappingResult<TDest>
            {
                IsSuccess = mappingException == null && validation.IsValid,
                Value = result,
                ValidationResult = validation,
                Exception = mappingException,
                ElapsedTime = elapsed
            };
        }
    }

    /// <summary>
    /// Fluent configuration builder for type mappings
    /// </summary>
    public class FluentConfigurationBuilder<TSource, TDest>
    {
        private readonly AutoObjMapper _mapper;
        private readonly Interfaces.ITypeMapConfiguration<TSource, TDest> _config;

        internal FluentConfigurationBuilder(AutoObjMapper mapper)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _config = mapper.Configure(c => { }).For<TSource, TDest>();
        }

        /// <summary>
        /// Ignore a destination property
        /// </summary>
        public FluentConfigurationBuilder<TSource, TDest> Ignore(string propertyName)
        {
            _config.Ignore(propertyName);
            return this;
        }

        /// <summary>
        /// Configure custom resolver for a property
        /// </summary>
        public FluentConfigurationBuilder<TSource, TDest> ForMember(string propertyName, Func<TSource, object> resolver)
        {
            _config.ForMember(propertyName, resolver);
            return this;
        }

        /// <summary>
        /// Configure before-map action
        /// </summary>
        public FluentConfigurationBuilder<TSource, TDest> BeforeMap(Action<TSource, TDest> action)
        {
            _config.BeforeMap(action);
            return this;
        }

        /// <summary>
        /// Configure after-map action
        /// </summary>
        public FluentConfigurationBuilder<TSource, TDest> AfterMap(Action<TSource, TDest> action)
        {
            _config.AfterMap(action);
            return this;
        }

        /// <summary>
        /// Apply the configuration and return the mapper
        /// </summary>
        public AutoObjMapper Build()
        {
            return _mapper;
        }
    }

    /// <summary>
    /// Detailed result of a mapping operation with full diagnostics
    /// </summary>
    public class DetailedMappingResult<T>
    {
        /// <summary>
        /// Indicates if the mapping was successful
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// The mapped value
        /// </summary>
        public T Value { get; set; }

        /// <summary>
        /// Validation result
        /// </summary>
        public MappingValidationResult ValidationResult { get; set; }

        /// <summary>
        /// Exception that occurred during mapping (if any)
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// Time elapsed during mapping
        /// </summary>
        public TimeSpan ElapsedTime { get; set; }

        /// <summary>
        /// Gets a detailed summary of the mapping result
        /// </summary>
        public override string ToString()
        {
            var status = IsSuccess ? "SUCCESS" : "FAILED";
            var summary = $"Mapping {status} in {ElapsedTime.TotalMilliseconds:F2}ms";
            
            if (ValidationResult?.HasWarnings == true)
                summary += $" (Warnings: {ValidationResult.Warnings.Count})";
            
            if (Exception != null)
                summary += $" (Error: {Exception.Message})";

            return summary;
        }
    }
}