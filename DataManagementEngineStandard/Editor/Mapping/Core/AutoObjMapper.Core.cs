using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using TheTechIdea.Beep.Editor.Mapping.Configuration;
using TheTechIdea.Beep.Editor.Mapping.Interfaces;

namespace TheTechIdea.Beep.Editor.Mapping
{
    /// <summary>
    /// Core AutoObjMapper class - Main entry point for object mapping operations.
    /// Lightweight object mapper inspired by popular mappers (AutoMapper/TinyMapper).
    /// Goals:
    /// - Fast: expression-compiled assignment actions cached per (TSource, TDest)
    /// - Safe: nullable/enum conversions, optional ignore-null behavior
    /// - Flexible: per type-pair configuration (ignore members, custom resolvers, before/after hooks)
    /// - Zero dependencies, works with plain POCOs
    /// </summary>
    public sealed partial class AutoObjMapper : IAutoObjMapper
    {
        private readonly AutoObjMapperOptions _options;
        private readonly IMapperConfiguration _config;
        private readonly ConcurrentDictionary<(Type src, Type dest), Delegate> _compiledSetters;

        public AutoObjMapper(AutoObjMapperOptions options = null)
        {
            _options = options ?? AutoObjMapperOptions.Default;
            _config = new MapperConfiguration(_options);
            _compiledSetters = new ConcurrentDictionary<(Type src, Type dest), Delegate>();
        }

        /// <summary>
        /// Gets the current mapper options
        /// </summary>
        public AutoObjMapperOptions Options => _options;

        /// <summary>
        /// Access to configuration
        /// </summary>
        public IMapperConfiguration Configure(Action<IMapperConfiguration> configure)
        {
            configure?.Invoke(_config);
            // Clear compiled cache if config changed for safety
            _compiledSetters.Clear();
            return _config;
        }

        /// <summary>
        /// Maps source object to a new destination instance
        /// </summary>
        public TDest Map<TSource, TDest>(TSource source)
            where TDest : new()
        {
            if (source == null) return default;
            var dest = new TDest();
            return Map(source, dest);
        }

        /// <summary>
        /// Maps source object to an existing destination instance
        /// </summary>
        public TDest Map<TSource, TDest>(TSource source, TDest destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            if (source == null) return destination;

            var setter = GetOrCreateSetter<TSource, TDest>();
            setter(source, destination);
            return destination;
        }

        /// <summary>
        /// Gets statistics about cached mappers
        /// </summary>
        public MappingStatistics GetStatistics()
        {
            return new MappingStatistics
            {
                CachedMappersCount = _compiledSetters.Count,
                RegisteredTypeMapsCount = _config.GetRegisteredTypeMapsCount()
            };
        }

        /// <summary>
        /// Clears all cached compiled mappers
        /// </summary>
        public void ClearCache()
        {
            _compiledSetters.Clear();
        }

        private Action<TSource, TDest> GetOrCreateSetter<TSource, TDest>()
        {
            var key = (typeof(TSource), typeof(TDest));
            return (Action<TSource, TDest>)_compiledSetters.GetOrAdd(key, _ => BuildSetter<TSource, TDest>());
        }
    }
}