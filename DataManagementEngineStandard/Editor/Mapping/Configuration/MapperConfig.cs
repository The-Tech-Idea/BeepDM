using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.Mapping
{
    /// <summary>
    /// Configuration API for legacy mapper compatibility
    /// Provides configuration interface for type mappings
    /// </summary>
    public class MapperConfig
    {
        private readonly AutoObjMapperOptions _options;
        private readonly ConcurrentDictionary<(Type src, Type dest), ITypeMapConfig> _maps = new();

        internal MapperConfig(AutoObjMapperOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Configure mapping for specific type pair
        /// </summary>
        public TypeMap<TSource, TDest> For<TSource, TDest>()
        {
            var key = (typeof(TSource), typeof(TDest));
            var cfg = (TypeMap<TSource, TDest>)_maps.GetOrAdd(key, _ => new TypeMap<TSource, TDest>(_options));
            return cfg;
        }

        /// <summary>
        /// Gets type map configuration for specific types
        /// </summary>
        internal TypeMapBase GetTypeMap(Type src, Type dest)
        {
            _maps.TryGetValue((src, dest), out var cfg);
            return cfg as TypeMapBase;
        }

        /// <summary>
        /// Gets the number of configured type maps
        /// </summary>
        public int GetConfiguredMapsCount()
        {
            return _maps.Count;
        }

        /// <summary>
        /// Clears all configured type maps
        /// </summary>
        public void Clear()
        {
            _maps.Clear();
        }
    }
}