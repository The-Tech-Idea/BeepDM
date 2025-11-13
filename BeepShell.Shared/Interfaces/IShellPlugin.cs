using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using BeepShell.Shared.Models;

namespace BeepShell.Shared.Interfaces
{
    /// <summary>
    /// Hot-reloadable plugin interface for dynamic loading/unloading without shell restart.
    /// Uses AssemblyLoadContext for true isolation and unloading.
    /// </summary>
    public interface IShellPlugin : IShellExtension
    {
        /// <summary>
        /// Plugin unique identifier (GUID recommended)
        /// </summary>
        string PluginId { get; }

        /// <summary>
        /// Plugin assembly location
        /// </summary>
        string AssemblyPath { get; }

        /// <summary>
        /// Can this plugin be hot-reloaded?
        /// </summary>
        bool SupportsHotReload { get; }

        /// <summary>
        /// Called before plugin is unloaded (save state, close resources)
        /// </summary>
        Task<bool> PrepareUnloadAsync();

        /// <summary>
        /// Called after plugin is reloaded (restore state)
        /// </summary>
        Task OnReloadAsync();

        /// <summary>
        /// Get plugin health status
        /// </summary>
        PluginHealthStatus GetHealthStatus();
    }

    /// <summary>
    /// Plugin manager for loading, unloading, and managing plugins
    /// </summary>
    public interface IPluginManager
    {
        /// <summary>
        /// Loaded plugins
        /// </summary>
        IReadOnlyList<IShellPlugin> LoadedPlugins { get; }

        /// <summary>
        /// Load a plugin from path
        /// </summary>
        Task<PluginLoadResult> LoadPluginAsync(string assemblyPath, bool isolated = true);

        /// <summary>
        /// Unload a plugin by ID
        /// </summary>
        Task<bool> UnloadPluginAsync(string pluginId);

        /// <summary>
        /// Reload a plugin
        /// </summary>
        Task<PluginLoadResult> ReloadPluginAsync(string pluginId);

        /// <summary>
        /// Get plugin by ID
        /// </summary>
        IShellPlugin? GetPlugin(string pluginId);

        /// <summary>
        /// Check if plugin is loaded
        /// </summary>
        bool IsPluginLoaded(string pluginId);

        /// <summary>
        /// Get all plugin commands
        /// </summary>
        IEnumerable<IShellCommand> GetAllCommands();

        /// <summary>
        /// Get all plugin workflows
        /// </summary>
        IEnumerable<IShellWorkflow> GetAllWorkflows();
    }

    /// <summary>
    /// Plugin load context for isolation
    /// </summary>
    public class PluginLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;

        public PluginLoadContext(string pluginPath, bool isCollectible = true) 
            : base($"Plugin_{Path.GetFileNameWithoutExtension(pluginPath)}", isCollectible)
        {
            _resolver = new AssemblyDependencyResolver(pluginPath);
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            // Try to resolve from plugin dependencies first
            var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            if (assemblyPath != null)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }

            // Fall back to default context for shared assemblies
            return null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            if (libraryPath != null)
            {
                return LoadUnmanagedDllFromPath(libraryPath);
            }

            return IntPtr.Zero;
        }
    }
}
