using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.Mapping
{
    /// <summary>
    /// Type-specific mapping configuration with fluent API
    /// Provides configuration options for mapping between specific source and destination types
    /// </summary>
    public sealed class TypeMap<TSource, TDest> : TypeMapBase
    {
        private readonly HashSet<string> _ignored;
        private readonly Dictionary<string, Delegate> _resolvers;

        internal TypeMap(AutoObjMapperOptions options) : base(options)
        {
            _ignored = new HashSet<string>(options.PropertyNameComparer);
            _resolvers = new Dictionary<string, Delegate>(options.PropertyNameComparer);
        }

        /// <summary>
        /// Ignore a destination property during mapping
        /// </summary>
        /// <param name="destPropertyName">Name of the destination property to ignore</param>
        /// <returns>The current TypeMap instance for fluent chaining</returns>
        public TypeMap<TSource, TDest> Ignore(string destPropertyName)
        {
            if (!string.IsNullOrWhiteSpace(destPropertyName))
                _ignored.Add(destPropertyName);
            return this;
        }

        /// <summary>
        /// Configure a custom resolver for a destination property
        /// </summary>
        /// <param name="destPropertyName">Name of the destination property</param>
        /// <param name="resolver">Function to resolve the property value</param>
        /// <returns>The current TypeMap instance for fluent chaining</returns>
        public TypeMap<TSource, TDest> ForMember(string destPropertyName, Func<TSource, object> resolver)
        {
            if (!string.IsNullOrWhiteSpace(destPropertyName) && resolver != null)
                _resolvers[destPropertyName] = resolver;
            return this;
        }

        /// <summary>
        /// Configure action to execute before mapping
        /// </summary>
        /// <param name="action">Action to execute before mapping</param>
        /// <returns>The current TypeMap instance for fluent chaining</returns>
        public TypeMap<TSource, TDest> BeforeMap(Action<TSource, TDest> action)
        {
            Before = action;
            return this;
        }

        /// <summary>
        /// Configure action to execute after mapping
        /// </summary>
        /// <param name="action">Action to execute after mapping</param>
        /// <returns>The current TypeMap instance for fluent chaining</returns>
        public TypeMap<TSource, TDest> AfterMap(Action<TSource, TDest> action)
        {
            After = action;
            return this;
        }

        internal override bool IsIgnored(string destPropName) => _ignored.Contains(destPropName);
        internal override bool TryGetResolver(string destPropName, out Delegate resolver) => _resolvers.TryGetValue(destPropName, out resolver);
        internal override Delegate BeforeMapDelegate => Before;
        internal override Delegate AfterMapDelegate => After;

        internal Action<TSource, TDest> Before { get; private set; }
        internal Action<TSource, TDest> After { get; private set; }

        /// <summary>
        /// Gets the number of ignored properties
        /// </summary>
        public int IgnoredPropertiesCount => _ignored.Count;

        /// <summary>
        /// Gets the number of custom resolvers configured
        /// </summary>
        public int CustomResolversCount => _resolvers.Count;

        /// <summary>
        /// Checks if a property is ignored
        /// </summary>
        /// <param name="propertyName">Property name to check</param>
        /// <returns>True if the property is ignored</returns>
        public bool IsPropertyIgnored(string propertyName)
        {
            return _ignored.Contains(propertyName);
        }

        /// <summary>
        /// Checks if a property has a custom resolver
        /// </summary>
        /// <param name="propertyName">Property name to check</param>
        /// <returns>True if the property has a custom resolver</returns>
        public bool HasCustomResolver(string propertyName)
        {
            return _resolvers.ContainsKey(propertyName);
        }
    }
}