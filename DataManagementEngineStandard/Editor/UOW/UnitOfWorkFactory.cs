using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Linq;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Editor.UOW
{
    /// <summary>
    /// Simplified factory for creating UnitOfWork instances when types are determined at runtime.
    /// For compile-time known types, use UnitofWork&lt;T&gt; constructors directly.
    /// </summary>
    public static class UnitOfWorkFactory
    {
        #region Private Fields

        private static readonly ConcurrentDictionary<string, Type> _typeCache = new();
        private static readonly ConcurrentDictionary<string, ConstructorInfo> _constructorCache = new();
        private static readonly ConcurrentDictionary<string, Type[]> _assemblyTypeCache = new();

        #endregion

        #region Configuration

        /// <summary>Configuration options for the factory</summary>
        public static class Configuration
        {
            public static bool EnableCaching { get; set; } = true;
            public static bool EnableValidation { get; set; } = true;
            /// <summary>Maximum number of entries in each cache before eviction</summary>
            public static int MaxCacheSize { get; set; } = 500;
        }

        #endregion

        #region Main Factory Methods - Dynamic Type Creation

        /// <summary>
        /// Creates a UnitOfWork instance when the entity type is determined at runtime.
        /// For compile-time known types, use: new UnitofWork&lt;YourType&gt;(...) directly.
        /// </summary>
        /// <param name="entityType">The runtime-determined entity type</param>
        /// <param name="dmeEditor">The IDMEEditor instance</param>
        /// <param name="datasourceName">The name of the data source</param>
        /// <param name="entityName">The name of the entity</param>
        /// <param name="primaryKey">The primary key field name (optional)</param>
        /// <returns>UnitOfWork instance wrapped for safe usage</returns>
        public static IUnitOfWorkWrapper CreateUnitOfWork(Type entityType, IDMEEditor dmeEditor, 
            string datasourceName, string entityName, string primaryKey = null)
        {
            ValidateParameters(entityType, dmeEditor, datasourceName, entityName);

            try
            {
                var uowInstance = CreateUnitOfWorkInternal(entityType, dmeEditor, datasourceName, entityName, primaryKey);
                return new UnitOfWorkWrapper(uowInstance);
            }
            catch (Exception ex)
            {
                LogError($"Failed to create UnitOfWork for entity type {entityType?.Name}", ex, dmeEditor);
                throw new InvalidOperationException($"Could not create UnitOfWork for entity type {entityType?.Name}", ex);
            }
        }

        /// <summary>
        /// Creates a UnitOfWork instance with EntityStructure when the type is determined at runtime
        /// </summary>
        public static IUnitOfWorkWrapper CreateUnitOfWork(Type entityType, IDMEEditor dmeEditor, 
            string datasourceName, string entityName, EntityStructure entityStructure, string primaryKey = null)
        {
            ValidateParameters(entityType, dmeEditor, datasourceName, entityName);
            
            if (entityStructure == null)
                throw new ArgumentNullException(nameof(entityStructure));

            try
            {
                var uowInstance = primaryKey != null 
                    ? CreateUnitOfWorkInternal(entityType, dmeEditor, datasourceName, entityName, entityStructure, primaryKey)
                    : CreateUnitOfWorkInternal(entityType, dmeEditor, datasourceName, entityName, entityStructure);
                
                return new UnitOfWorkWrapper(uowInstance);
            }
            catch (Exception ex)
            {
                LogError($"Failed to create UnitOfWork with EntityStructure for entity type {entityType?.Name}", ex, dmeEditor);
                throw new InvalidOperationException($"Could not create UnitOfWork for entity type {entityType?.Name}", ex);
            }
        }

        #endregion

        #region Type Resolution from String (Legacy Support)

        /// <summary>
        /// Creates a UnitOfWork from a string type name (legacy support)
        /// For new code, resolve the Type first and use the Type-based overload
        /// </summary>
        /// <param name="entityTypeName">Full name of the entity type</param>
        /// <param name="dmeEditor">The IDMEEditor instance</param>
        /// <param name="datasourceName">The name of the data source</param>
        /// <param name="entityName">The name of the entity</param>
        /// <returns>UnitOfWork instance</returns>
        public static object CreateUnitOfWork(string entityTypeName, IDMEEditor dmeEditor, 
            string datasourceName, string entityName)
        {
            if (string.IsNullOrWhiteSpace(entityTypeName))
                throw new ArgumentException("Entity type name cannot be null or empty", nameof(entityTypeName));

            var entityType = ResolveType(entityTypeName);
            var wrapper = CreateUnitOfWork(entityType, dmeEditor, datasourceName, entityName);
            
            // Return the wrapped instance directly for legacy compatibility
            return ((UnitOfWorkWrapper)wrapper);
        }

        /// <summary>
        /// Legacy method - kept for backward compatibility
        /// </summary>
        public static object GetUnitOfWork(string entityName, string dataSourceName, IDMEEditor dmeEditor)
        {
            return CreateUnitOfWork(entityName, dmeEditor, dataSourceName, entityName);
        }

        #endregion

        #region Batch Operations for Dynamic Scenarios

        /// <summary>
        /// Creates multiple UnitOfWork instances for runtime-determined types
        /// Useful for configuration-driven or plugin scenarios
        /// </summary>
        public static Dictionary<string, IUnitOfWorkWrapper> CreateUnitOfWorkBatch(
            Dictionary<Type, string> entityTypes, IDMEEditor dmeEditor, string datasourceName)
        {
            if (entityTypes == null || entityTypes.Count == 0)
                throw new ArgumentException("Entity types dictionary cannot be null or empty", nameof(entityTypes));

            var results = new Dictionary<string, IUnitOfWorkWrapper>();
            var errors = new List<Exception>();

            foreach (var kvp in entityTypes)
            {
                try
                {
                    var uow = CreateUnitOfWork(kvp.Key, dmeEditor, datasourceName, kvp.Value);
                    results[kvp.Value] = uow;
                }
                catch (Exception ex)
                {
                    errors.Add(new InvalidOperationException($"Failed to create UnitOfWork for {kvp.Value}", ex));
                }
            }

            if (errors.Count > 0 && results.Count == 0)
            {
                throw new AggregateException("Failed to create any UnitOfWork instances", errors);
            }

            return results;
        }

        /// <summary>
        /// Creates UnitOfWork from an entity instance (automatically detects type)
        /// </summary>
        public static IUnitOfWorkWrapper CreateUnitOfWorkFromInstance(object entityInstance, IDMEEditor dmeEditor, 
            string datasourceName, string entityName = null)
        {
            if (entityInstance == null)
                throw new ArgumentNullException(nameof(entityInstance));

            var entityType = entityInstance.GetType();
            var actualEntityName = entityName ?? entityType.Name;

            return CreateUnitOfWork(entityType, dmeEditor, datasourceName, actualEntityName);
        }

        #endregion

        #region Internal Implementation

        private static object CreateUnitOfWorkInternal(Type entityType, IDMEEditor dmeEditor, 
            string datasourceName, string entityName, params object[] additionalArgs)
        {
            var cacheKey = $"ctor_{entityType.FullName}_{additionalArgs.Length}";
            
            if (!Configuration.EnableCaching || !_constructorCache.TryGetValue(cacheKey, out var constructor))
            {
                var uowGenericType = typeof(UnitofWork<>).MakeGenericType(entityType);
                var parameterTypes = GetParameterTypes(additionalArgs);
                constructor = uowGenericType.GetConstructor(parameterTypes);

                if (constructor == null)
                {
                    throw new InvalidOperationException(
                        $"Could not find suitable constructor for UnitOfWork<{entityType.Name}> with {parameterTypes.Length} parameters");
                }

                if (Configuration.EnableCaching)
                {
                    _constructorCache.TryAdd(cacheKey, constructor);
                }
            }

            var constructorArgs = new object[] { dmeEditor, datasourceName, entityName }
                .Concat(additionalArgs ?? new object[0]).ToArray();

            return constructor.Invoke(constructorArgs);
        }

        private static Type[] GetParameterTypes(object[] additionalArgs)
        {
            var baseTypes = new[] { typeof(IDMEEditor), typeof(string), typeof(string) };
            
            if (additionalArgs == null || additionalArgs.Length == 0)
                return baseTypes;

            var additionalTypes = additionalArgs.Select(arg => arg?.GetType() ?? typeof(object)).ToArray();
            return baseTypes.Concat(additionalTypes).ToArray();
        }

        private static Type ResolveType(string typeName)
        {
            var cacheKey = $"type_{typeName}";
            
            if (Configuration.EnableCaching && _typeCache.TryGetValue(cacheKey, out var cachedType))
                return cachedType;

            var entityType = Type.GetType(typeName) ?? FindTypeInLoadedAssemblies(typeName);
            
            if (entityType == null)
            {
                throw new ArgumentException($"Could not find type: {typeName}. Make sure the type name is fully qualified and the assembly is loaded.");
            }

            if (Configuration.EnableValidation)
            {
                ValidateEntityType(entityType);
            }

            if (Configuration.EnableCaching)
            {
                TrimCacheIfNeeded();
                _typeCache.TryAdd(cacheKey, entityType);
            }

            return entityType;
        }

        private static Type FindTypeInLoadedAssemblies(string typeName)
        {
            try
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        var type = assembly.GetType(typeName);
                        if (type != null) return type;

                        // Cache GetTypes() per assembly to avoid repeated expensive calls
                        var assemblyKey = assembly.FullName;
                        var types = _assemblyTypeCache.GetOrAdd(assemblyKey, _ =>
                        {
                            try { return assembly.GetTypes(); }
                            catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t != null).ToArray(); }
                        });

                        foreach (var t in types)
                        {
                            if (t.Name == typeName || t.FullName == typeName)
                                return t;
                        }
                    }
                    catch (ReflectionTypeLoadException)
                    {
                        continue; // Skip assemblies that can't be loaded
                    }
                }
            }
            catch (Exception)
            {
                // If we can't search assemblies, return null
            }

            return null;
        }

        /// <summary>
        /// Evicts oldest entries if cache exceeds max size
        /// </summary>
        private static void TrimCacheIfNeeded()
        {
            if (_typeCache.Count > Configuration.MaxCacheSize)
            {
                // Remove roughly half the entries to avoid frequent eviction
                var keysToRemove = _typeCache.Keys.Take(_typeCache.Count / 2).ToList();
                foreach (var key in keysToRemove)
                {
                    _typeCache.TryRemove(key, out _);
                }
            }
            if (_constructorCache.Count > Configuration.MaxCacheSize)
            {
                var keysToRemove = _constructorCache.Keys.Take(_constructorCache.Count / 2).ToList();
                foreach (var key in keysToRemove)
                {
                    _constructorCache.TryRemove(key, out _);
                }
            }
        }

        #endregion

        #region Validation

        private static void ValidateParameters(Type entityType, IDMEEditor dmeEditor, 
            string datasourceName, string entityName)
        {
            if (entityType == null)
                throw new ArgumentNullException(nameof(entityType));
            if (dmeEditor == null)
                throw new ArgumentNullException(nameof(dmeEditor));
            if (string.IsNullOrWhiteSpace(datasourceName))
                throw new ArgumentException("Data source name cannot be null or empty", nameof(datasourceName));
            if (string.IsNullOrWhiteSpace(entityName))
                throw new ArgumentException("Entity name cannot be null or empty", nameof(entityName));

            if (Configuration.EnableValidation)
            {
                ValidateEntityType(entityType);
            }
        }

        private static void ValidateEntityType(Type entityType)
        {
            if (entityType.IsAbstract)
                throw new ArgumentException($"Entity type {entityType.Name} cannot be abstract", nameof(entityType));

            if (entityType.IsInterface)
                throw new ArgumentException($"Entity type {entityType.Name} cannot be an interface", nameof(entityType));

            // Check for parameterless constructor
            if (entityType.GetConstructor(Type.EmptyTypes) == null)
                throw new ArgumentException($"Entity type {entityType.Name} must have a parameterless constructor", nameof(entityType));
        }

        #endregion

        #region Utility Methods

        /// <summary>Clears all caches</summary>
        public static void ClearCaches()
        {
            _typeCache.Clear();
            _constructorCache.Clear();
            _assemblyTypeCache.Clear();
        }

        /// <summary>Gets cache statistics</summary>
        public static CacheStatistics GetCacheStatistics()
        {
            return new CacheStatistics
            {
                TypeCacheCount = _typeCache.Count,
                ConstructorCacheCount = _constructorCache.Count,
                CachingEnabled = Configuration.EnableCaching
            };
        }

        private static void LogError(string message, Exception ex, IDMEEditor dmeEditor)
        {
            try
            {
                dmeEditor?.AddLogMessage("UnitOfWorkFactory", $"ERROR: {message} - {ex.Message}", 
                    DateTime.Now, -1, null, Errors.Failed);
            }
            catch
            {
                Console.WriteLine($"UnitOfWorkFactory ERROR: {message} - {ex.Message}");
            }
        }

        #endregion
    }

    #region Supporting Classes

    /// <summary>Cache statistics for monitoring factory performance</summary>
    public class CacheStatistics
    {
        public int TypeCacheCount { get; set; }
        public int ConstructorCacheCount { get; set; }
        public bool CachingEnabled { get; set; }
        
        public override string ToString()
        {
            return $"Types: {TypeCacheCount}, Constructors: {ConstructorCacheCount}, Enabled: {CachingEnabled}";
        }
    }

    #endregion
}
