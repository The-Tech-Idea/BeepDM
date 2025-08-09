using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Tools
{
    /// <summary>
    /// Plugin states for modern plugin lifecycle management
    /// </summary>
    public enum PluginState
    {
        Unloaded,
        Loaded,
        Initialized,
        Started,
        Stopped,
        Error,
        Failed
    }

    /// <summary>
    /// Plugin health status
    /// </summary>
    public enum PluginHealth
    {
        Healthy,
        Warning,
        Critical,
        Unknown
    }

    /// <summary>
    /// Plugin assembly resolution strategy
    /// </summary>
    public enum PluginResolutionStrategy
    {
        /// <summary>
        /// Search regular assemblies only
        /// </summary>
        RegularAssembliesOnly,
        
        /// <summary>
        /// Search isolated plugins only
        /// </summary>
        IsolatedPluginsOnly,
        
        /// <summary>
        /// Search regular assemblies first, then plugins
        /// </summary>
        RegularFirst,
        
        /// <summary>
        /// Search plugins first, then regular assemblies
        /// </summary>
        PluginsFirst,
        
        /// <summary>
        /// Search both simultaneously (parallel)
        /// </summary>
        Both
    }

    /// <summary>
    /// Unified plugin type - treats everything as a plugin
    /// </summary>
    public enum UnifiedPluginType
    {
        /// <summary>
        /// Regular assembly loaded in shared context
        /// </summary>
        SharedAssembly,
        
        /// <summary>
        /// Isolated plugin with unload capability
        /// </summary>
        IsolatedPlugin,
        
        /// <summary>
        /// Downloaded nugget package
        /// </summary>
        NuggetPackage,
        
        /// <summary>
        /// Built-in system component
        /// </summary>
        BuiltinComponent,
        
        /// <summary>
        /// Data source driver
        /// </summary>
        DataSourceDriver,
        
        /// <summary>
        /// Connection driver
        /// </summary>
        ConnectionDriver,
        
        /// <summary>
        /// Workflow component
        /// </summary>
        WorkflowComponent,
        
        /// <summary>
        /// UI component/addon
        /// </summary>
        UIComponent,
        
        /// <summary>
        /// Extension/loader component
        /// </summary>
        ExtensionComponent
    }

    /// <summary>
    /// Plugin information for unified plugin system - treats everything as a plugin
    /// </summary>
    public class PluginInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
        public string Author { get; set; }
        public PluginState State { get; set; }
        public PluginHealth Health { get; set; }
        public DateTime LoadedAt { get; set; }
        public Assembly Assembly { get; set; }
        public List<string> Dependencies { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
        public Exception LastError { get; set; }
        
        // New unified properties
        public UnifiedPluginType PluginType { get; set; }
        public bool IsSharedContext { get; set; } = true;
        public bool CanUnload { get; set; } = true;
        public string SourcePath { get; set; }
        public List<Type> ExportedTypes { get; set; } = new();
        public List<object> CreatedInstances { get; set; } = new();
        public FolderFileTypes FileType { get; set; }
    }

    /// <summary>
    /// Unified plugin container - represents any loadable component as a plugin
    /// </summary>
    public class UnifiedPlugin
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
        public UnifiedPluginType Type { get; set; }
        public PluginState State { get; set; }
        public PluginHealth Health { get; set; }
        public bool IsSharedContext { get; set; } = true;
        public bool CanUnload { get; set; } = true;
        public DateTime LoadedAt { get; set; }
        public string SourcePath { get; set; }
        
        // Assembly information
        public Assembly Assembly { get; set; }
        public List<Type> ExportedTypes { get; set; } = new();
        public Dictionary<string, Type> TypeCache { get; set; } = new();
        
        // Instance management
        public List<object> ActiveInstances { get; set; } = new();
        public Dictionary<string, object> NamedInstances { get; set; } = new();
        
        // Dependencies and relationships
        public List<string> Dependencies { get; set; } = new();
        public List<string> Dependents { get; set; } = new();
        
        // Metadata and configuration
        public Dictionary<string, object> Metadata { get; set; } = new();
        public Dictionary<string, object> Configuration { get; set; } = new();
        
        // Lifecycle events
        public event EventHandler<PluginEventArgs> StateChanged;
        public event EventHandler<PluginEventArgs> HealthChanged;
        public event EventHandler<PluginEventArgs> InstanceCreated;
        public event EventHandler<PluginEventArgs> InstanceDestroyed;

        /// <summary>
        /// Creates an instance of a type from this plugin
        /// </summary>
        public object CreateInstance(string typeName, params object[] args)
        {
            if (TypeCache.TryGetValue(typeName, out var type))
            {
                try
                {
                    var instance = Activator.CreateInstance(type, args);
                    ActiveInstances.Add(instance);
                    
                    InstanceCreated?.Invoke(this, new PluginEventArgs 
                    { 
                        Plugin = ConvertToPluginInfo(), 
                        Message = $"Instance created: {typeName}" 
                    });
                    
                    return instance;
                }
                catch (Exception ex)
                {
                    Health = PluginHealth.Warning;
                    throw new InvalidOperationException($"Failed to create instance of {typeName}", ex);
                }
            }
            return null;
        }

        /// <summary>
        /// Gets an instance by name (for singleton-like behavior)
        /// </summary>
        public T GetNamedInstance<T>(string name) where T : class
        {
            return NamedInstances.TryGetValue(name, out var instance) ? instance as T : null;
        }

        /// <summary>
        /// Sets a named instance
        /// </summary>
        public void SetNamedInstance(string name, object instance)
        {
            NamedInstances[name] = instance;
        }

        /// <summary>
        /// Checks if the plugin contains a specific type
        /// </summary>
        public bool HasType(string typeName)
        {
            return TypeCache.ContainsKey(typeName);
        }

        /// <summary>
        /// Gets all types implementing a specific interface
        /// </summary>
        public List<Type> GetTypesImplementing<T>()
        {
            return ExportedTypes.Where(t => typeof(T).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract).ToList();
        }

        /// <summary>
        /// Converts to legacy PluginInfo for compatibility
        /// </summary>
        public PluginInfo ConvertToPluginInfo()
        {
            return new PluginInfo
            {
                Id = Id,
                Name = Name,
                Version = Version,
                Description = Description,
                State = State,
                Health = Health,
                LoadedAt = LoadedAt,
                Assembly = Assembly,
                Dependencies = Dependencies,
                Metadata = Metadata,
                PluginType = Type,
                IsSharedContext = IsSharedContext,
                SourcePath = SourcePath,
                ExportedTypes = ExportedTypes,
                CreatedInstances = ActiveInstances
            };
        }
    }

    /// <summary>
    /// Plugin instance creation result
    /// </summary>
    public class PluginInstanceResult
    {
        public object Instance { get; set; }
        public Type InstanceType { get; set; }
        public string PluginId { get; set; }
        public bool IsFromIsolatedPlugin { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public Exception Exception { get; set; }
        public UnifiedPluginType PluginType { get; set; }
        public bool IsSharedContext { get; set; } = true;
    }

    /// <summary>
    /// Plugin type search result
    /// </summary>
    public class PluginTypeResult
    {
        public Type Type { get; set; }
        public string PluginId { get; set; }
        public Assembly Assembly { get; set; }
        public bool IsFromIsolatedPlugin { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public UnifiedPluginType PluginType { get; set; }
        public bool IsSharedContext { get; set; } = true;
    }

    /// <summary>
    /// Plugin context for dependency injection and services
    /// </summary>
    public interface IPluginContext
    {
        IServiceProvider Services { get; }
        IConfigEditor Configuration { get; }
        IDMLogger Logger { get; }
        IAssemblyHandler AssemblyHandler { get; }
        Dictionary<string, object> Properties { get; }
        
        // New unified context properties
        IUnifiedPluginManager PluginManager { get; }
        string CurrentPluginId { get; }
        UnifiedPluginType CurrentPluginType { get; }
    }

    /// <summary>
    /// Event arguments for plugin lifecycle events
    /// </summary>
    public class PluginEventArgs : EventArgs
    {
        public PluginInfo Plugin { get; set; }
        public string Message { get; set; }
        public Exception Error { get; set; }
        public UnifiedPluginType PluginType { get; set; }
        public string SourcePath { get; set; }
        public object Instance { get; set; }
    }

    /// <summary>
    /// Enhanced modern plugin interface - unified approach
    /// </summary>
    public interface IModernPlugin : IDisposable
    {
        string Id { get; }
        string Name { get; }
        string Version { get; }
        string Description { get; }
        PluginState State { get; }
        PluginHealth Health { get; }
        List<string> Dependencies { get; }
        UnifiedPluginType PluginType { get; }
        bool IsSharedContext { get; }

        bool Initialize(IPluginContext context);
        bool Start();
        bool Stop();
        bool Reload();
        PluginHealth CheckHealth();
        Dictionary<string, object> GetMetadata();
        
        // New unified methods
        bool CanCreateType(string typeName);
        object CreateTypeInstance(string typeName, params object[] args);
        List<Type> GetExportedTypes();
        
        event EventHandler<PluginEventArgs> StateChanged;
        event EventHandler<PluginEventArgs> HealthChanged;
    }

    /// <summary>
    /// Plugin messaging system for inter-plugin communication
    /// </summary>
    public interface IPluginMessageBus
    {
        void Subscribe<T>(string channel, Action<T> handler);
        void Unsubscribe(string channel);
        void Publish<T>(string channel, T message);
        void SendToPlugin<T>(string pluginId, string channel, T message);
        void RegisterPlugin(string pluginId, UnifiedPluginType pluginType);
        void UnregisterPlugin(string pluginId);
        List<string> GetRegisteredPlugins();
        List<string> GetPluginsByType(UnifiedPluginType pluginType);
    }

    

    /// <summary>
    /// Unified plugin manager interface - treats everything as plugins
    /// </summary>
    public interface IUnifiedPluginManager : IDisposable
    {
        // Plugin lifecycle
        Task<UnifiedPlugin> LoadPluginAsync(string path, UnifiedPluginType type = UnifiedPluginType.SharedAssembly, string pluginId = null);
        bool UnloadPlugin(string pluginId);
        UnifiedPlugin GetPlugin(string pluginId);
        List<UnifiedPlugin> GetPlugins();
        List<UnifiedPlugin> GetPluginsByType(UnifiedPluginType type);
        
        // Instance creation (unified approach)
        object CreateInstance(string typeName, params object[] args);
        T CreateInstance<T>(params object[] args) where T : class;
        object CreateInstanceFromPlugin(string pluginId, string typeName, params object[] args);
        
        // Type resolution (unified approach)
        Type GetType(string typeName);
        bool TypeExists(string typeName);
        List<Type> GetTypesImplementing<T>();
        List<Type> GetAllTypes();
        
        // Plugin discovery
        Task<List<UnifiedPlugin>> DiscoverPluginsAsync(string directoryPath);
        List<UnifiedPlugin> ScanAssemblyForComponents(Assembly assembly, UnifiedPluginType type);
        
        // Events
        event EventHandler<PluginEventArgs> PluginLoaded;
        event EventHandler<PluginEventArgs> PluginUnloaded;
        event EventHandler<PluginEventArgs> PluginStateChanged;
        event EventHandler<PluginEventArgs> InstanceCreated;
    }

    /// <summary>
    /// Plugin discovery result
    /// </summary>
    public class PluginDiscoveryResult
    {
        public List<UnifiedPlugin> DiscoveredPlugins { get; set; } = new();
        public List<string> FailedPaths { get; set; } = new();
        public Dictionary<string, Exception> Errors { get; set; } = new();
        public TimeSpan ScanDuration { get; set; }
        public int TotalAssembliesScanned { get; set; }
        public int TotalTypesDiscovered { get; set; }
    }

    /// <summary>
    /// Plugin factory for creating unified plugins from different sources
    /// </summary>
    public static class UnifiedPluginFactory
    {
        public static UnifiedPlugin CreateFromAssembly(Assembly assembly, UnifiedPluginType type, string sourcePath = null)
        {
            var plugin = new UnifiedPlugin
            {
                Id = $"{type}_{assembly.GetName().Name}_{Guid.NewGuid():N}",
                Name = assembly.GetName().Name,
                Version = assembly.GetName().Version?.ToString() ?? "1.0.0",
                Type = type,
                State = PluginState.Loaded,
                Health = PluginHealth.Healthy,
                Assembly = assembly,
                LoadedAt = DateTime.UtcNow,
                SourcePath = sourcePath ?? assembly.Location
            };

            // Extract types
            try
            {
                var types = assembly.GetTypes();
                plugin.ExportedTypes.AddRange(types);
                
                foreach (var t in types)
                {
                    if (!string.IsNullOrEmpty(t.FullName))
                    {
                        plugin.TypeCache[t.FullName] = t;
                    }
                }
            }
            catch (Exception ex)
            {
                plugin.Health = PluginHealth.Warning;
                plugin.Metadata["LoadError"] = ex.Message;
            }

            return plugin;
        }

        public static UnifiedPlugin CreateFromPluginInfo(PluginInfo pluginInfo)
        {
            return new UnifiedPlugin
            {
                Id = pluginInfo.Id,
                Name = pluginInfo.Name,
                Version = pluginInfo.Version,
                Description = pluginInfo.Description,
                Type = pluginInfo.PluginType,
                State = pluginInfo.State,
                Health = pluginInfo.Health,
                Assembly = pluginInfo.Assembly,
                LoadedAt = pluginInfo.LoadedAt,
                SourcePath = pluginInfo.SourcePath,
                ExportedTypes = pluginInfo.ExportedTypes,
                ActiveInstances = pluginInfo.CreatedInstances,
                Dependencies = pluginInfo.Dependencies,
                Metadata = pluginInfo.Metadata,
                IsSharedContext = pluginInfo.IsSharedContext
            };
        }
    }
}
