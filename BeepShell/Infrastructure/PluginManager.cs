using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Spectre.Console;
using TheTechIdea.Beep.Editor;

namespace BeepShell.Infrastructure
{
    /// <summary>
    /// Concrete implementation of plugin manager
    /// </summary>
    public class PluginManager : IPluginManager
    {
        private readonly IDMEEditor _editor;
        private readonly ConcurrentDictionary<string, PluginContainer> _plugins = new();
        private readonly object _lock = new object();

        public IReadOnlyList<IShellPlugin> LoadedPlugins => 
            _plugins.Values.Select(p => p.Plugin).ToList();

        public PluginManager(IDMEEditor editor)
        {
            _editor = editor;
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Plugin loading requires dynamic assembly loading")]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Plugin system requires reflection")]
        public async Task<PluginLoadResult> LoadPluginAsync(string assemblyPath, bool isolated = true)
        {
            var sw = Stopwatch.StartNew();
            var result = new PluginLoadResult();

            try
            {
                if (!File.Exists(assemblyPath))
                {
                    result.Success = false;
                    result.Message = $"Assembly not found: {assemblyPath}";
                    return result;
                }

                Assembly assembly;
                PluginLoadContext loadContext = null;

                if (isolated)
                {
                    // Load in isolated context for hot-reload support
                    loadContext = new PluginLoadContext(assemblyPath, isCollectible: true);
                    assembly = loadContext.LoadFromAssemblyPath(assemblyPath);
                }
                else
                {
                    // Load in default context (cannot be unloaded)
                    assembly = Assembly.LoadFrom(assemblyPath);
                }

                // Find plugin type
                var pluginType = assembly.GetTypes()
                    .FirstOrDefault(t => typeof(IShellPlugin).IsAssignableFrom(t) && 
                                        !t.IsInterface && 
                                        !t.IsAbstract);

                if (pluginType == null)
                {
                    result.Success = false;
                    result.Message = "No IShellPlugin implementation found in assembly";
                    return result;
                }

                // Create plugin instance
                var plugin = (IShellPlugin)Activator.CreateInstance(pluginType);

                // Check if already loaded
                if (_plugins.ContainsKey(plugin.PluginId))
                {
                    result.Success = false;
                    result.Message = $"Plugin {plugin.PluginId} is already loaded";
                    return result;
                }

                // Initialize plugin
                plugin.Initialize(_editor);
                plugin.OnLoad();

                // Store plugin container
                var container = new PluginContainer
                {
                    Plugin = plugin,
                    Assembly = assembly,
                    LoadContext = loadContext,
                    AssemblyPath = assemblyPath,
                    LoadTime = DateTime.Now,
                    IsIsolated = isolated
                };

                _plugins.TryAdd(plugin.PluginId, container);

                sw.Stop();
                result.Success = true;
                result.Message = $"Plugin '{plugin.ExtensionName}' loaded successfully";
                result.Plugin = plugin;
                result.LoadTime = sw.Elapsed;

                _editor.Logger?.WriteLog($"Plugin loaded: {plugin.ExtensionName} v{plugin.Version} in {sw.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Failed to load plugin: {ex.Message}";
                result.Errors.Add(ex.ToString());
                _editor.Logger?.WriteLog($"Plugin load error: {ex}");
            }

            return result;
        }

        public async Task<bool> UnloadPluginAsync(string pluginId)
        {
            try
            {
                if (!_plugins.TryRemove(pluginId, out var container))
                {
                    return false;
                }

                // Prepare for unload
                if (container.Plugin.SupportsHotReload)
                {
                    var canUnload = await container.Plugin.PrepareUnloadAsync();
                    if (!canUnload)
                    {
                        _editor.Logger?.WriteLog($"Plugin {pluginId} declined unload request");
                        _plugins.TryAdd(pluginId, container); // Put it back
                        return false;
                    }
                }

                // Cleanup
                container.Plugin.OnUnload();
                container.Plugin.Cleanup();

                // Unload assembly if isolated
                if (container.IsIsolated && container.LoadContext != null)
                {
                    container.LoadContext.Unload();
                    
                    // Force garbage collection
                    for (int i = 0; i < 3; i++)
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }
                }

                _editor.Logger?.WriteLog($"Plugin unloaded: {container.Plugin.ExtensionName}");
                return true;
            }
            catch (Exception ex)
            {
                _editor.Logger?.WriteLog($"Plugin unload error: {ex}");
                return false;
            }
        }

        public async Task<PluginLoadResult> ReloadPluginAsync(string pluginId)
        {
            if (!_plugins.TryGetValue(pluginId, out var container))
            {
                return new PluginLoadResult
                {
                    Success = false,
                    Message = "Plugin not found"
                };
            }

            if (!container.Plugin.SupportsHotReload)
            {
                return new PluginLoadResult
                {
                    Success = false,
                    Message = "Plugin does not support hot reload"
                };
            }

            var assemblyPath = container.AssemblyPath;
            var isIsolated = container.IsIsolated;

            // Unload old version
            var unloaded = await UnloadPluginAsync(pluginId);
            if (!unloaded)
            {
                return new PluginLoadResult
                {
                    Success = false,
                    Message = "Failed to unload plugin for reload"
                };
            }

            // Wait for cleanup
            await Task.Delay(500);

            // Load new version
            var result = await LoadPluginAsync(assemblyPath, isIsolated);
            
            if (result.Success)
            {
                await result.Plugin.OnReloadAsync();
                result.Message = $"Plugin '{result.Plugin.ExtensionName}' reloaded successfully";
            }

            return result;
        }

        public IShellPlugin GetPlugin(string pluginId)
        {
            return _plugins.TryGetValue(pluginId, out var container) 
                ? container.Plugin 
                : null;
        }

        public bool IsPluginLoaded(string pluginId)
        {
            return _plugins.ContainsKey(pluginId);
        }

        public IEnumerable<IShellCommand> GetAllCommands()
        {
            return _plugins.Values
                .SelectMany(p => p.Plugin.GetCommands())
                .ToList();
        }

        public IEnumerable<IShellWorkflow> GetAllWorkflows()
        {
            return _plugins.Values
                .SelectMany(p => p.Plugin.GetWorkflows())
                .ToList();
        }

        public void UnloadAll()
        {
            foreach (var pluginId in _plugins.Keys.ToList())
            {
                UnloadPluginAsync(pluginId).GetAwaiter().GetResult();
            }
        }

        private class PluginContainer
        {
            public IShellPlugin Plugin { get; set; }
            public Assembly Assembly { get; set; }
            public PluginLoadContext LoadContext { get; set; }
            public string AssemblyPath { get; set; }
            public DateTime LoadTime { get; set; }
            public bool IsIsolated { get; set; }
        }
    }
}
