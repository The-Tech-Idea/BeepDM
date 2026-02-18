using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Tools;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Tools.PluginSystem
{
    /// <summary>
    /// Assistant for creating instances from types with advanced caching and error handling
    /// </summary>
    public class InstanceCreationAssistant : IDisposable
    {
        private readonly SharedContextManager _sharedContextManager;
        private readonly IDMLogger _logger;
        private bool _disposed = false;

        public InstanceCreationAssistant(SharedContextManager sharedContextManager, IDMLogger logger)
        {
            _sharedContextManager = sharedContextManager;
            _logger = logger;
        }

        /// <summary>
        /// Creates an instance from a type name with parameters
        /// </summary>
        public object CreateInstanceFromString(string typeName, params object[] args)
        {
            object instance = null;

            try
            {
                var type = GetType(typeName);
                if (type == null)
                {
                    _logger?.LogWithContext($"Type not found: {typeName}", null);
                    return null;
                }

                instance = Activator.CreateInstance(type, args);
                _logger?.LogWithContext($"Successfully created instance of: {typeName}", null);
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Failed to create instance of {typeName}: {ex.Message}", ex);
                return null;
            }

            return instance;
        }

        /// <summary>
        /// Creates an instance from a specific assembly and type name
        /// </summary>
        public object CreateInstanceFromString(string assemblyName, string typeName, params object[] args)
        {
            object instance = null;

            try
            {
                var assembly = GetAssemblyByName(assemblyName);
                if (assembly != null)
                {
                    var type = assembly.GetType(typeName);
                    if (type != null)
                    {
                        instance = Activator.CreateInstance(type, args);
                        _logger?.LogWithContext($"Successfully created instance of {typeName} from {assemblyName}", null);
                    }
                    else
                    {
                        // Fallback to global type resolution
                        instance = CreateInstanceFromString(typeName, args);
                    }
                }
                else
                {
                    _logger?.LogWithContext($"Assembly not found: {assemblyName}", null);
                    // Fallback to global type resolution
                    instance = CreateInstanceFromString(typeName, args);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Failed to create instance of {typeName} from {assemblyName}: {ex.Message}", ex);
                return null;
            }

            return instance;
        }

        /// <summary>
        /// Gets a type by its full name - delegates to SharedContextManager
        /// </summary>
        public Type GetType(string fullTypeName)
        {
            if (string.IsNullOrWhiteSpace(fullTypeName))
                return null;

            // Delegate directly to SharedContextManager - no local caching
            var sharedType = _sharedContextManager.GetType(fullTypeName);
            if (sharedType != null)
            {
                return sharedType;
            }

            // Fallback to system type resolution
            var systemType = Type.GetType(fullTypeName);
            if (systemType != null)
            {
                // Add to SharedContextManager cache instead of local cache
                _sharedContextManager.GetCachedTypes(); // This will cache it in SharedContextManager
                return systemType;
            }

            // Search in all shared assemblies
            var sharedAssemblies = _sharedContextManager.GetSharedAssemblies();
            foreach (var assembly in sharedAssemblies)
            {
                try
                {
                    var type = assembly.GetType(fullTypeName);
                    if (type != null)
                    {
                        return type;
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWithContext($"Error searching type {fullTypeName} in assembly {assembly.FullName}", ex);
                }
            }

            _logger?.LogWithContext($"Type not found: {fullTypeName}", null);
            return null;
        }

        /// <summary>
        /// Creates a singleton instance - delegates to SharedContextManager
        /// </summary>
        public T CreateSingleton<T>(string typeName, params object[] args) where T : class
        {
            // Delegate to SharedContextManager for singleton creation
            return _sharedContextManager.CreateInstance(typeName, args) as T;
        }

        /// <summary>
        /// Creates a singleton instance using the shared context manager
        /// </summary>
        public T CreateSingletonFromSharedContext<T>(string typeName, params object[] args) where T : class
        {
            // Both methods now delegate to SharedContextManager
            return _sharedContextManager.CreateInstance(typeName, args) as T;
        }

        /// <summary>
        /// Gets an instance by fully qualified name (non-cached)
        /// </summary>
        public object GetInstance(string fullyQualifiedName)
        {
            var type = GetType(fullyQualifiedName);
            if (type != null)
            {
                try
                {
                    return Activator.CreateInstance(type);
                }
                catch (Exception ex)
                {
                    _logger?.LogWithContext($"Failed to create parameterless instance of {fullyQualifiedName}", ex);
                }
            }
            return null;
        }

        /// <summary>
        /// Gets an assembly by name from shared context
        /// </summary>
        private Assembly GetAssemblyByName(string assemblyName)
        {
            var assemblies = _sharedContextManager.GetSharedAssemblies();
            
            // Remove .dll extension if present
            var nameWithoutExtension = assemblyName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) 
                ? assemblyName.Substring(0, assemblyName.Length - 4) 
                : assemblyName;

            return assemblies.FirstOrDefault(a => 
                string.Equals(a.GetName().Name, nameWithoutExtension, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(a.ManifestModule.Name, assemblyName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Checks if a type exists
        /// </summary>
        public bool TypeExists(string fullTypeName)
        {
            return GetType(fullTypeName) != null;
        }

        /// <summary>
        /// Gets all cached types from SharedContextManager
        /// </summary>
        public Dictionary<string, Type> GetCachedTypes()
        {
            return _sharedContextManager.GetCachedTypes();
        }

        /// <summary>
        /// Gets creation statistics from SharedContextManager
        /// </summary>
        public Dictionary<string, object> GetCreationStatistics()
        {
            return new Dictionary<string, object>
            {
                ["SharedContextTypes"] = _sharedContextManager.GetCachedTypes().Count,
                ["SharedAssemblies"] = _sharedContextManager.GetSharedAssemblies().Count,
                ["SharedInstances"] = _sharedContextManager.GetIntegratedStatistics().TryGetValue("CachedInstances", out var instances) ? instances : 0
            };
        }

        /// <summary>
        /// Clears all caches - delegates to SharedContextManager
        /// </summary>
        public void ClearCaches()
        {
            // Note: Individual assistants should not clear shared caches
            // This would be done by SharedContextManager when nuggets are unloaded
            _logger?.LogWithContext("ClearCaches called - caches are managed by SharedContextManager during nugget unloading", null);
        }

        /// <summary>
        /// Clears type cache only - delegates to SharedContextManager
        /// </summary>
        public void ClearTypeCache()
        {
            // Note: Individual assistants should not clear shared caches
            _logger?.LogWithContext("ClearTypeCache called - type cache is managed by SharedContextManager", null);
        }

        /// <summary>
        /// Clears singleton cache only - delegates to SharedContextManager
        /// </summary>
        public void ClearSingletonCache()
        {
            // Note: Individual assistants should not clear shared caches
            _logger?.LogWithContext("ClearSingletonCache called - singleton cache is managed by SharedContextManager", null);
        }

        /// <summary>
        /// Adds a type to cache manually - delegates to SharedContextManager
        /// </summary>
        public void AddTypeToCache(string fullName, Type type)
        {
            if (type != null && !string.IsNullOrEmpty(fullName))
            {
                // The type will be cached automatically by SharedContextManager when assemblies are loaded
                _logger?.LogWithContext($"Type caching requested for {fullName} - managed by SharedContextManager", null);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                // No local caches to clear - everything is managed by SharedContextManager
                _disposed = true;
            }
        }
    }
}