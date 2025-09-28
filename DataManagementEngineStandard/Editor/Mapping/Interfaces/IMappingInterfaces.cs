using System;

namespace TheTechIdea.Beep.Editor.Mapping.Interfaces
{
    /// <summary>
    /// Interface for the main AutoObjMapper functionality
    /// </summary>
    public interface IAutoObjMapper
    {
        /// <summary>
        /// Gets the current mapper options
        /// </summary>
        AutoObjMapperOptions Options { get; }

        /// <summary>
        /// Access to configuration
        /// </summary>
        IMapperConfiguration Configure(Action<IMapperConfiguration> configure);

        /// <summary>
        /// Maps source object to a new destination instance
        /// </summary>
        TDest Map<TSource, TDest>(TSource source) where TDest : new();

        /// <summary>
        /// Maps source object to an existing destination instance
        /// </summary>
        TDest Map<TSource, TDest>(TSource source, TDest destination);

        /// <summary>
        /// Gets statistics about cached mappers
        /// </summary>
        MappingStatistics GetStatistics();

        /// <summary>
        /// Clears all cached compiled mappers
        /// </summary>
        void ClearCache();
    }

    /// <summary>
    /// Interface for mapper configuration
    /// </summary>
    public interface IMapperConfiguration
    {
        /// <summary>
        /// Configure mapping for specific type pair
        /// </summary>
        ITypeMapConfiguration<TSource, TDest> For<TSource, TDest>();

        /// <summary>
        /// Gets the count of registered type maps
        /// </summary>
        int GetRegisteredTypeMapsCount();

        /// <summary>
        /// Gets type map for specific source and destination types
        /// </summary>
        ITypeMapBase GetTypeMap(Type src, Type dest);
    }

    /// <summary>
    /// Interface for type-specific mapping configuration
    /// </summary>
    public interface ITypeMapConfiguration<TSource, TDest>
    {
        /// <summary>
        /// Ignore a destination property during mapping
        /// </summary>
        ITypeMapConfiguration<TSource, TDest> Ignore(string destPropertyName);

        /// <summary>
        /// Configure custom resolver for a destination property
        /// </summary>
        ITypeMapConfiguration<TSource, TDest> ForMember(string destPropertyName, Func<TSource, object> resolver);

        /// <summary>
        /// Configure action to execute before mapping
        /// </summary>
        ITypeMapConfiguration<TSource, TDest> BeforeMap(Action<TSource, TDest> action);

        /// <summary>
        /// Configure action to execute after mapping
        /// </summary>
        ITypeMapConfiguration<TSource, TDest> AfterMap(Action<TSource, TDest> action);
    }

    /// <summary>
    /// Base interface for type map configuration
    /// </summary>
    public interface ITypeMapBase
    {
        /// <summary>
        /// Check if a destination property is ignored
        /// </summary>
        bool IsIgnored(string destPropName);

        /// <summary>
        /// Try to get a custom resolver for a destination property
        /// </summary>
        bool TryGetResolver(string destPropName, out Delegate resolver);

        /// <summary>
        /// Gets the before-map action delegate
        /// </summary>
        Delegate BeforeMapDelegate { get; }

        /// <summary>
        /// Gets the after-map action delegate
        /// </summary>
        Delegate AfterMapDelegate { get; }
    }
}