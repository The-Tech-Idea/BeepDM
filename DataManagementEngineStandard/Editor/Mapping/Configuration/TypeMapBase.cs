using System;

namespace TheTechIdea.Beep.Editor.Mapping
{
    /// <summary>
    /// Base class for type mapping configuration
    /// Provides common functionality for all type map configurations
    /// </summary>
    public abstract class TypeMapBase : ITypeMapConfig
    {
        protected readonly AutoObjMapperOptions Options;

        protected TypeMapBase(AutoObjMapperOptions options)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Checks if a destination property is ignored in the mapping
        /// </summary>
        internal abstract bool IsIgnored(string destPropName);

        /// <summary>
        /// Attempts to get a custom resolver for a destination property
        /// </summary>
        internal abstract bool TryGetResolver(string destPropName, out Delegate resolver);

        /// <summary>
        /// Gets the delegate to execute before mapping
        /// </summary>
        internal abstract Delegate BeforeMapDelegate { get; }

        /// <summary>
        /// Gets the delegate to execute after mapping
        /// </summary>
        internal abstract Delegate AfterMapDelegate { get; }
    }
}