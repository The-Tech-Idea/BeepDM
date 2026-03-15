using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using static System.WeakReference;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Tools;

namespace TheTechIdea.Beep.Tools.PluginSystem
{
    /// <summary>
    /// Manages plugin lifecycle states and transitions
    /// </summary>
    public class PluginLifecycleManager : IDisposable
    {
        private readonly ConcurrentDictionary<string, PluginInfo> _plugins = new();
        private readonly ConcurrentDictionary<string, WeakReference> _pluginInstances = new();
        private readonly IDMLogger _logger;
        private bool _disposed = false;

        // Events
        public event EventHandler<PluginEventArgs> PluginStateChanged;
        public event EventHandler<PluginEventArgs> PluginHealthChanged;
        public event EventHandler<PluginEventArgs> PluginError;

        public PluginLifecycleManager(IDMLogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Registers a plugin with the lifecycle manager
        /// </summary>
        public void RegisterPlugin(PluginInfo pluginInfo)
        {
            if (pluginInfo != null && !string.IsNullOrWhiteSpace(pluginInfo.Id))
            {
                _plugins[pluginInfo.Id] = pluginInfo;
            }
        }

        /// <summary>
        /// Gets all loaded plugins with state information
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
        /// Initializes a plugin
        /// </summary>
        public bool InitializePlugin(string pluginId, IPluginContext context = null)
        {
            if (string.IsNullOrWhiteSpace(pluginId))
                return false;

            try
            {
                if (!_plugins.TryGetValue(pluginId, out PluginInfo pluginInfo))
                {
                    _logger?.LogWithContext($"Plugin not found: {pluginId}", null);
                    return false;
                }

                if (pluginInfo.State != PluginState.Loaded)
                {
                    _logger?.LogWithContext($"Plugin {pluginId} is not in Loaded state. Current state: {pluginInfo.State}", null);
                    return false;
                }

                // Find modern plugin implementations
                var modernPluginTypes = FindModernPluginTypes(pluginInfo.Assembly);
                
                foreach (var pluginType in modernPluginTypes)
                {
                    try
                    {
                        // Create instance of modern plugin
                        var plugin = Activator.CreateInstance(pluginType) as IModernPlugin;
                        
                        if (plugin != null)
                        {
                            // Initialize the plugin
                            bool initialized = plugin.Initialize(context);
                            
                            if (initialized)
                            {
                                // Store weak reference to plugin instance
                                _pluginInstances[pluginId] = new WeakReference(plugin);
                                
                                // Subscribe to plugin events
                                plugin.StateChanged += (s, e) => OnPluginStateChanged(pluginId, e);
                                plugin.HealthChanged += (s, e) => OnPluginHealthChanged(pluginId, e);
                                
                                // Update plugin state
                                UpdatePluginState(pluginInfo, PluginState.Initialized);
                                pluginInfo.Health = plugin.Health;
                                
                                // Merge plugin metadata
                                var metadata = plugin.GetMetadata();
                                foreach (var kvp in metadata)
                                {
                                    pluginInfo.Metadata[kvp.Key] = kvp.Value;
                                }
                                
                                _logger?.LogWithContext($"Plugin initialized: {pluginId}", null);
                                
                                return true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWithContext($"Failed to initialize plugin type {pluginType.Name} in {pluginId}", ex);
                        continue; // Try next plugin type
                    }
                }

                // If no modern plugins found, mark as initialized anyway for backward compatibility
                UpdatePluginState(pluginInfo, PluginState.Initialized);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Failed to initialize plugin: {pluginId}", ex);
                
                if (_plugins.TryGetValue(pluginId, out PluginInfo errorPlugin))
                {
                    UpdatePluginState(errorPlugin, PluginState.Error);
                    errorPlugin.LastError = ex;
                }

                OnPluginError(pluginId, ex, $"Failed to initialize plugin: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Starts a plugin
        /// </summary>
        public bool StartPlugin(string pluginId)
        {
            if (string.IsNullOrWhiteSpace(pluginId))
                return false;

            try
            {
                if (!_plugins.TryGetValue(pluginId, out PluginInfo pluginInfo))
                    return false;

                if (pluginInfo.State != PluginState.Initialized)
                {
                    _logger?.LogWithContext($"Plugin {pluginId} must be initialized before starting. Current state: {pluginInfo.State}", null);
                    return false;
                }

                // Try to get modern plugin instance
                if (_pluginInstances.TryGetValue(pluginId, out WeakReference weakRef) && 
                    weakRef.Target is IModernPlugin plugin)
                {
                    bool started = plugin.Start();
                    if (started)
                    {
                        UpdatePluginState(pluginInfo, PluginState.Started);
                        pluginInfo.Health = plugin.Health;
                        
                        _logger?.LogWithContext($"Plugin started: {pluginId}", null);
                        return true;
                    }
                }
                else
                {
                    // For backward compatibility, mark as started
                    UpdatePluginState(pluginInfo, PluginState.Started);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Failed to start plugin: {pluginId}", ex);
                
                if (_plugins.TryGetValue(pluginId, out PluginInfo errorPlugin))
                {
                    UpdatePluginState(errorPlugin, PluginState.Error);
                    errorPlugin.LastError = ex;
                }

                OnPluginError(pluginId, ex, $"Failed to start plugin: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Stops a plugin
        /// </summary>
        public bool StopPlugin(string pluginId)
        {
            if (string.IsNullOrWhiteSpace(pluginId))
                return false;

            try
            {
                if (!_plugins.TryGetValue(pluginId, out PluginInfo pluginInfo))
                    return false;

                if (pluginInfo.State != PluginState.Started)
                {
                    _logger?.LogWithContext($"Plugin {pluginId} is not started. Current state: {pluginInfo.State}", null);
                    return true; // Already stopped
                }

                // Try to get modern plugin instance
                if (_pluginInstances.TryGetValue(pluginId, out WeakReference weakRef) && 
                    weakRef.Target is IModernPlugin plugin)
                {
                    bool stopped = plugin.Stop();
                    if (stopped)
                    {
                        UpdatePluginState(pluginInfo, PluginState.Stopped);
                        pluginInfo.Health = plugin.Health;
                        
                        _logger?.LogWithContext($"Plugin stopped: {pluginId}", null);
                        return true;
                    }
                }
                else
                {
                    // For backward compatibility, mark as stopped
                    UpdatePluginState(pluginInfo, PluginState.Stopped);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Failed to stop plugin: {pluginId}", ex);
                
                if (_plugins.TryGetValue(pluginId, out PluginInfo errorPlugin))
                {
                    UpdatePluginState(errorPlugin, PluginState.Error);
                    errorPlugin.LastError = ex;
                }

                OnPluginError(pluginId, ex, $"Failed to stop plugin: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Reloads a plugin
        /// </summary>
        public bool ReloadPlugin(string pluginId)
        {
            if (string.IsNullOrWhiteSpace(pluginId))
                return false;

            try
            {
                if (!_plugins.TryGetValue(pluginId, out PluginInfo pluginInfo))
                    return false;

                // Try to get modern plugin instance
                if (_pluginInstances.TryGetValue(pluginId, out WeakReference weakRef) && 
                    weakRef.Target is IModernPlugin plugin)
                {
                    bool reloaded = plugin.Reload();
                    if (reloaded)
                    {
                        pluginInfo.Health = plugin.Health;
                        _logger?.LogWithContext($"Plugin reloaded: {pluginId}", null);
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Failed to reload plugin: {pluginId}", ex);
                OnPluginError(pluginId, ex, $"Failed to reload plugin: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks plugin health
        /// </summary>
        public PluginHealth CheckPluginHealth(string pluginId)
        {
            if (string.IsNullOrWhiteSpace(pluginId))
                return PluginHealth.Unknown;

            try
            {
                if (!_plugins.TryGetValue(pluginId, out PluginInfo pluginInfo))
                    return PluginHealth.Unknown;

                // Try to get modern plugin instance
                if (_pluginInstances.TryGetValue(pluginId, out WeakReference weakRef) && 
                    weakRef.Target is IModernPlugin plugin)
                {
                    var health = plugin.CheckHealth();
                    pluginInfo.Health = health;
                    return health;
                }

                return pluginInfo.Health;
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Failed to check plugin health: {pluginId}", ex);
                return PluginHealth.Critical;
            }
        }

        /// <summary>
        /// Gets plugin metadata
        /// </summary>
        public Dictionary<string, object> GetPluginMetadata(string pluginId)
        {
            if (_plugins.TryGetValue(pluginId, out PluginInfo pluginInfo))
            {
                return new Dictionary<string, object>(pluginInfo.Metadata);
            }
            return new Dictionary<string, object>();
        }

        // Helper methods
        private void UpdatePluginState(PluginInfo pluginInfo, PluginState newState)
        {
            var oldState = pluginInfo.State;
            pluginInfo.State = newState;

            PluginStateChanged?.Invoke(this, new PluginEventArgs 
            { 
                Plugin = pluginInfo, 
                Message = $"Plugin state changed from {oldState} to {newState}"
            });
        }

        private List<Type> FindModernPluginTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes()
                    .Where(t => typeof(IModernPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                    .ToList();
            }
            catch
            {
                return new List<Type>();
            }
        }

        private void OnPluginStateChanged(string pluginId, PluginEventArgs e)
        {
            if (_plugins.TryGetValue(pluginId, out PluginInfo pluginInfo))
            {
                PluginStateChanged?.Invoke(this, new PluginEventArgs 
                { 
                    Plugin = pluginInfo, 
                    Message = e.Message 
                });
            }
        }

        private void OnPluginHealthChanged(string pluginId, PluginEventArgs e)
        {
            if (_plugins.TryGetValue(pluginId, out PluginInfo pluginInfo))
            {
                pluginInfo.Health = e.Plugin?.Health ?? PluginHealth.Unknown;
                
                PluginHealthChanged?.Invoke(this, new PluginEventArgs 
                { 
                    Plugin = pluginInfo, 
                    Message = e.Message 
                });
            }
        }

        private void OnPluginError(string pluginId, Exception error, string message)
        {
            PluginError?.Invoke(this, new PluginEventArgs 
            { 
                Plugin = _plugins.GetValueOrDefault(pluginId), 
                Error = error, 
                Message = message
            });
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                // Stop all running plugins
                foreach (var plugin in _plugins.Values.Where(p => p.State == PluginState.Started))
                {
                    StopPlugin(plugin.Id);
                }

                _plugins.Clear();
                _pluginInstances.Clear();
                _disposed = true;
            }
        }
    }
}