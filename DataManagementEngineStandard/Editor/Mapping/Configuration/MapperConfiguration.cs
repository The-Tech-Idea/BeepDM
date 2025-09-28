using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using TheTechIdea.Beep.Editor.Mapping.Interfaces;

namespace TheTechIdea.Beep.Editor.Mapping.Configuration
{
    /// <summary>
    /// Configuration API for AutoObjMapper
    /// </summary>
    public class MapperConfiguration : IMapperConfiguration
    {
        private readonly AutoObjMapperOptions _options;
        private readonly ConcurrentDictionary<(Type src, Type dest), ITypeMapBase> _maps;

        internal MapperConfiguration(AutoObjMapperOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _maps = new ConcurrentDictionary<(Type src, Type dest), ITypeMapBase>();
        }

        /// <summary>
        /// Configure mapping for specific type pair
        /// </summary>
        public ITypeMapConfiguration<TSource, TDest> For<TSource, TDest>()
        {
            var key = (typeof(TSource), typeof(TDest));
            var cfg = (TypeMapConfiguration<TSource, TDest>)_maps.GetOrAdd(key, 
                _ => new TypeMapConfiguration<TSource, TDest>(_options));
            return cfg;
        }

        /// <summary>
        /// Gets the count of registered type maps
        /// </summary>
        public int GetRegisteredTypeMapsCount()
        {
            return _maps.Count;
        }

        /// <summary>
        /// Gets type map for specific source and destination types
        /// </summary>
        public ITypeMapBase GetTypeMap(Type src, Type dest)
        {
            _maps.TryGetValue((src, dest), out var cfg);
            return cfg;
        }
    }

    /// <summary>
    /// Base class for type mapping configuration
    /// </summary>
    public abstract class TypeMapConfigurationBase : ITypeMapBase
    {
        protected readonly AutoObjMapperOptions Options;

        protected TypeMapConfigurationBase(AutoObjMapperOptions options)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public abstract bool IsIgnored(string destPropName);
        public abstract bool TryGetResolver(string destPropName, out Delegate resolver);
        public abstract Delegate BeforeMapDelegate { get; }
        public abstract Delegate AfterMapDelegate { get; }
    }

    /// <summary>
    /// Type-specific mapping configuration
    /// </summary>
    public sealed class TypeMapConfiguration<TSource, TDest> : TypeMapConfigurationBase, ITypeMapConfiguration<TSource, TDest>
    {
        private readonly HashSet<string> _ignored;
        private readonly Dictionary<string, Delegate> _resolvers;
        private Action<TSource, TDest> _beforeMap;
        private Action<TSource, TDest> _afterMap;

        internal TypeMapConfiguration(AutoObjMapperOptions options) : base(options)
        {
            _ignored = new HashSet<string>(options.PropertyNameComparer);
            _resolvers = new Dictionary<string, Delegate>(options.PropertyNameComparer);
        }

        public ITypeMapConfiguration<TSource, TDest> Ignore(string destPropertyName)
        {
            if (!string.IsNullOrWhiteSpace(destPropertyName))
                _ignored.Add(destPropertyName);
            return this;
        }

        public ITypeMapConfiguration<TSource, TDest> ForMember(string destPropertyName, Func<TSource, object> resolver)
        {
            if (!string.IsNullOrWhiteSpace(destPropertyName) && resolver != null)
                _resolvers[destPropertyName] = resolver;
            return this;
        }

        public ITypeMapConfiguration<TSource, TDest> BeforeMap(Action<TSource, TDest> action)
        {
            _beforeMap = action;
            return this;
        }

        public ITypeMapConfiguration<TSource, TDest> AfterMap(Action<TSource, TDest> action)
        {
            _afterMap = action;
            return this;
        }

        public override bool IsIgnored(string destPropName) => _ignored.Contains(destPropName);
        public override bool TryGetResolver(string destPropName, out Delegate resolver) => _resolvers.TryGetValue(destPropName, out resolver);
        public override Delegate BeforeMapDelegate => _beforeMap;
        public override Delegate AfterMapDelegate => _afterMap;
    }
}