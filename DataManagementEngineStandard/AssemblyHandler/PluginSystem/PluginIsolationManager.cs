using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using static System.WeakReference;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Tools;

namespace TheTechIdea.Beep.Tools.PluginSystem
{
    /// <summary>
    /// Collectible Assembly Load Context for true plugin isolation
    /// </summary>
    public class PluginLoadContext : AssemblyLoadContext
    {
        public string PluginId { get; }
        public string AssemblyPath { get; }
        public DateTime LoadedAt { get; }

        public PluginLoadContext(string pluginId, string assemblyPath) 
            : base(pluginId, isCollectible: true)
        {
            PluginId = pluginId;
            AssemblyPath = assemblyPath;
            LoadedAt = DateTime.UtcNow;
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            // Return null to let the default load context handle system assemblies
            return null;
        }
    }

    /// <summary>
    /// Manages plugin isolation and true unloading using AssemblyLoadContext
    /// </summary>
    public class PluginIsolationManager : IDisposable
    {
        private readonly ConcurrentDictionary<string, PluginInfo> _plugins = new();
        private readonly ConcurrentDictionary<string, PluginLoadContext> _pluginContexts = new();
        private readonly ConcurrentDictionary<string, WeakReference> _pluginInstances = new();
        private readonly ConcurrentDictionary<string, List<string>> _pluginVersionHistory = new();
        private readonly IDMLogger _logger;
        private bool _disposed = false;

        public PluginIsolationManager(IDMLogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Loads an assembly with true isolation using collectible AssemblyLoadContext
        /// </summary>
        public async Task<PluginInfo> LoadPluginWithIsolationAsync(string assemblyPath, string pluginId = null)
        {
            if (string.IsNullOrWhiteSpace(assemblyPath) || !File.Exists(assemblyPath))
            {
                return null;
            }

            try
            {
                // Generate plugin ID if not provided
                if (string.IsNullOrWhiteSpace(pluginId))
                {
                    pluginId = $"Plugin_{Path.GetFileNameWithoutExtension(assemblyPath)}_{Guid.NewGuid():N}";
                }

                // Create isolated load context
                var loadContext = new PluginLoadContext(pluginId, assemblyPath);
                
                // Load assembly in isolation
                var assembly = await Task.Run(() => 
                {
                    using (var stream = File.OpenRead(assemblyPath))
                    {
                        return loadContext.LoadFromStream(stream);
                    }
                });

                // Create plugin info
                var pluginInfo = new PluginInfo
                {
                    Id = pluginId,
                    Name = assembly.GetName().Name,
                    Version = assembly.GetName().Version?.ToString() ?? "1.0.0",
                    Description = GetAssemblyDescription(assembly),
                    Author = GetAssemblyAuthor(assembly),
                    State = PluginState.Loaded,
                    Health = PluginHealth.Healthy,
                    LoadedAt = DateTime.UtcNow,
                    Assembly = assembly,
                    Dependencies = new List<string>(),
                    Metadata = new Dictionary<string, object>()
                };

                // Store plugin and context
                _plugins[pluginId] = pluginInfo;
                _pluginContexts[pluginId] = loadContext;

                // Initialize version history
                if (!_pluginVersionHistory.ContainsKey(pluginId))
                {
                    _pluginVersionHistory[pluginId] = new List<string>();
                }
                _pluginVersionHistory[pluginId].Add(pluginInfo.Version);

                _logger?.LogWithContext($"Plugin loaded with isolation: {pluginId}", pluginInfo);
                
                return pluginInfo;
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Failed to load plugin with isolation: {assemblyPath}", ex);
                return null;
            }
        }

        /// <summary>
        /// Unloads a plugin with true memory cleanup
        /// </summary>
        public bool UnloadPlugin(string pluginId)
        {
            if (string.IsNullOrWhiteSpace(pluginId))
                return false;

            try
            {
                // Get plugin info
                if (!_plugins.TryGetValue(pluginId, out PluginInfo pluginInfo))
                {
                    return false;
                }

                // Get load context
                if (_pluginContexts.TryRemove(pluginId, out PluginLoadContext loadContext))
                {
                    // Clear plugin instances
                    _pluginInstances.TryRemove(pluginId, out _);

                    // Unload the context (this triggers GC for collectible assemblies)
                    loadContext.Unload();

                    // Force garbage collection to clean up memory
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                }

                // Update plugin state
                pluginInfo.State = PluginState.Unloaded;
                pluginInfo.Health = PluginHealth.Unknown;

                // Remove from active plugins
                _plugins.TryRemove(pluginId, out _);

                _logger?.LogWithContext($"Plugin unloaded: {pluginId}", null);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Failed to unload plugin: {pluginId}", ex);
                return false;
            }
        }

        /// <summary>
        /// Gets all isolated plugin contexts
        /// </summary>
        public IEnumerable<string> GetIsolatedPluginIds()
        {
            return _plugins.Keys.ToList();
        }

        /// <summary>
        /// Gets all plugins
        /// </summary>
        public IEnumerable<PluginInfo> GetPlugins()
        {
            return _plugins.Values.ToList();
        }

        /// <summary>
        /// Gets plugin by ID
        /// </summary>
        public PluginInfo GetPlugin(string pluginId)
        {
            return _plugins.GetValueOrDefault(pluginId);
        }

        /// <summary>
        /// Stores a plugin instance with weak reference
        /// </summary>
        public void StorePluginInstance(string pluginId, object instance)
        {
            if (!string.IsNullOrWhiteSpace(pluginId) && instance != null)
            {
                _pluginInstances[pluginId] = new WeakReference(instance);
            }
        }

        /// <summary>
        /// Gets a plugin instance if still alive
        /// </summary>
        public T GetPluginInstance<T>(string pluginId) where T : class
        {
            if (_pluginInstances.TryGetValue(pluginId, out WeakReference weakRef) && 
                weakRef.Target is T instance)
            {
                return instance;
            }
            return null;
        }

        /// <summary>
        /// Gets version history for a plugin
        /// </summary>
        public List<string> GetPluginVersionHistory(string pluginId)
        {
            return _pluginVersionHistory.GetValueOrDefault(pluginId, new List<string>());
        }

        // Helper methods
        private string GetAssemblyDescription(Assembly assembly)
        {
            try
            {
                var descAttr = assembly.GetCustomAttribute<System.Reflection.AssemblyDescriptionAttribute>();
                return descAttr?.Description ?? "No description available";
            }
            catch
            {
                return "No description available";
            }
        }

        private string GetAssemblyAuthor(Assembly assembly)
        {
            try
            {
                var companyAttr = assembly.GetCustomAttribute<System.Reflection.AssemblyCompanyAttribute>();
                return companyAttr?.Company ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                // Unload all plugins
                foreach (var pluginId in _plugins.Keys.ToList())
                {
                    UnloadPlugin(pluginId);
                }

                _plugins.Clear();
                _pluginContexts.Clear();
                _pluginInstances.Clear();
                _pluginVersionHistory.Clear();

                _disposed = true;
            }
        }

        /// <summary>
        /// Plugin-aware instance creation with strategy support
        /// </summary>
        public PluginInstanceResult CreatePluginInstance(string strFullyQualifiedName, 
                                                         PluginResolutionStrategy strategy, 
                                                         params object[] args)
        {
            var result = new PluginInstanceResult { Success = false };

            try
            {
                switch (strategy)
                {
                    case PluginResolutionStrategy.IsolatedPluginsOnly:
                        return CreateInstanceFromIsolatedPlugins(strFullyQualifiedName, args);
                        
                    case PluginResolutionStrategy.RegularAssembliesOnly:
                        return CreateInstanceFromRegularAssemblies(strFullyQualifiedName, args);
                        
                    case PluginResolutionStrategy.PluginsFirst:
                        result = CreateInstanceFromIsolatedPlugins(strFullyQualifiedName, args);
                        if (!result.Success)
                            result = CreateInstanceFromRegularAssemblies(strFullyQualifiedName, args);
                        break;
                        
                    case PluginResolutionStrategy.RegularFirst:
                        result = CreateInstanceFromRegularAssemblies(strFullyQualifiedName, args);
                        if (!result.Success)
                            result = CreateInstanceFromIsolatedPlugins(strFullyQualifiedName, args);
                        break;
                        
                    case PluginResolutionStrategy.Both:
                        result = CreateInstanceFromRegularAssemblies(strFullyQualifiedName, args);
                        if (!result.Success)
                            result = CreateInstanceFromIsolatedPlugins(strFullyQualifiedName, args);
                        break;
                }

                return result;
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.ErrorMessage = ex.Message;
                _logger?.LogWithContext($"Failed to create plugin instance: {strFullyQualifiedName}", ex);
                return result;
            }
        }

        /// <summary>
        /// Enhanced GetInstance with plugin support
        /// </summary>
        public object GetInstanceWithPluginSupport(string strFullyQualifiedName)
        {
            var result = CreatePluginInstance(strFullyQualifiedName, PluginResolutionStrategy.PluginsFirst);
            return result.Success ? result.Instance : null;
        }

        /// <summary>
        /// Gets plugin type information
        /// </summary>
        public PluginTypeResult GetPluginType(string strFullyQualifiedName, PluginResolutionStrategy strategy)
        {
            var result = new PluginTypeResult { Success = false };

            try
            {
                foreach (var plugin in _plugins.Values)
                {
                    try
                    {
                        var type = plugin.Assembly.GetType(strFullyQualifiedName);
                        if (type != null)
                        {
                            result.Type = type;
                            result.Assembly = plugin.Assembly;
                            result.PluginId = plugin.Id;
                            result.IsFromIsolatedPlugin = true;
                            result.Success = true;
                            return result;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWithContext($"Failed to get type from plugin {plugin.Id}: {strFullyQualifiedName}", ex);
                    }
                }

                result.ErrorMessage = $"Type not found: {strFullyQualifiedName}";
                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        /// <summary>
        /// Creates instance from specific plugin
        /// </summary>
        public PluginInstanceResult CreateInstanceFromPlugin(string pluginId, string typeName, params object[] args)
        {
            var result = new PluginInstanceResult
            {
                Success = false,
                IsFromIsolatedPlugin = true,
                PluginId = pluginId
            };

            try
            {
                if (!_plugins.TryGetValue(pluginId, out var plugin))
                {
                    result.ErrorMessage = $"Plugin not found: {pluginId}";
                    return result;
                }

                var type = plugin.Assembly.GetType(typeName);
                if (type == null)
                {
                    result.ErrorMessage = $"Type not found in plugin: {typeName}";
                    return result;
                }

                var instance = Activator.CreateInstance(type, args);
                result.Instance = instance;
                result.InstanceType = type;
                result.Success = true;

                StorePluginInstance(pluginId, instance);
                return result;
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.ErrorMessage = ex.Message;
                _logger?.LogWithContext($"Failed to create instance from plugin {pluginId}: {typeName}", ex);
                return result;
            }
        }

        /// <summary>
        /// Enhanced CreateInstanceFromString with strategy
        /// </summary>
        public PluginInstanceResult CreateInstanceFromString(string typeName, PluginResolutionStrategy strategy, params object[] args)
        {
            return CreatePluginInstance(typeName, strategy, args);
        }

        /// <summary>
        /// Gets all available types from plugins and regular assemblies
        /// </summary>
        public List<Type> GetAllAvailableTypes(string namespaceName = null)
        {
            var types = new List<Type>();

            try
            {
                foreach (var plugin in _plugins.Values)
                {
                    try
                    {
                        var pluginTypes = plugin.Assembly.GetTypes();
                        if (!string.IsNullOrEmpty(namespaceName))
                        {
                            pluginTypes = pluginTypes.Where(t => t.Namespace?.StartsWith(namespaceName) == true).ToArray();
                        }
                        types.AddRange(pluginTypes);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWithContext($"Failed to get types from plugin {plugin.Id}", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Failed to get all available types", ex);
            }

            return types;
        }

        /// <summary>
        /// Checks if type exists in plugins
        /// </summary>
        public bool TypeExists(string strFullyQualifiedName, PluginResolutionStrategy strategy)
        {
            var result = GetPluginType(strFullyQualifiedName, strategy);
            return result.Success;
        }

        /// <summary>
        /// Gets instance creation statistics
        /// </summary>
        public Dictionary<string, object> GetInstanceCreationStats()
        {
            return new Dictionary<string, object>
            {
                ["LoadedPlugins"] = _plugins.Count,
                ["IsolatedContexts"] = _pluginContexts.Count,
                ["CachedInstances"] = _pluginInstances.Count,
                ["VersionHistories"] = _pluginVersionHistory.Count
            };
        }

        // Private helper methods for the delegation
        private PluginInstanceResult CreateInstanceFromIsolatedPlugins(string strFullyQualifiedName, params object[] args)
        {
            var result = new PluginInstanceResult { Success = false, IsFromIsolatedPlugin = true };

            try
            {
                foreach (var plugin in _plugins.Values)
                {
                    try
                    {
                        var type = plugin.Assembly.GetType(strFullyQualifiedName);
                        if (type != null)
                        {
                            var instance = Activator.CreateInstance(type, args);
                            result.Instance = instance;
                            result.InstanceType = type;
                            result.PluginId = plugin.Id;
                            result.Success = true;

                            StorePluginInstance(plugin.Id, instance);
                            return result;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWithContext($"Failed to create instance from plugin {plugin.Id}: {strFullyQualifiedName}", ex);
                    }
                }

                result.ErrorMessage = $"Type not found in any isolated plugin: {strFullyQualifiedName}";
                return result;
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        private PluginInstanceResult CreateInstanceFromRegularAssemblies(string strFullyQualifiedName, params object[] args)
        {
            var result = new PluginInstanceResult { Success = false, IsFromIsolatedPlugin = false };

            try
            {
                // This would need to delegate back to AssemblyHandler's regular type resolution
                // For now, return failure so AssemblyHandler handles it
                result.ErrorMessage = $"Regular assembly type resolution delegated to AssemblyHandler: {strFullyQualifiedName}";
                return result;
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }
    }
}