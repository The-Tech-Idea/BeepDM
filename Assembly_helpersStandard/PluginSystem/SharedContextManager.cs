using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using static System.WeakReference;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Tools;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.DriversConfigurations;

namespace TheTechIdea.Beep.Tools.PluginSystem
{
    /// <summary>
    /// Event arguments for assembly resolution events
    /// </summary>
    public class AssemblyResolvedEventArgs : EventArgs
    {
        public AssemblyName AssemblyName { get; set; }
        public Assembly ResolvedAssembly { get; set; }
        public string ResolvedFrom { get; set; }
        public string ContextId { get; set; }
    }

    /// <summary>
    /// Shared Context Load Context for true unloading while maintaining shared visibility
    /// Uses proper dependency resolution to share assemblies across contexts
    /// </summary>
    public class SharedContextLoadContext : AssemblyLoadContext
    {
        /// <summary>Unique identifier for this load context (usually the nugget/plugin ID).</summary>
        public string ContextId { get; }
        /// <summary>Original source path (directory or dll) used for loading.</summary>
        public string SourcePath { get; }
        /// <summary>UTC timestamp when the context was created.</summary>
        public DateTime LoadedAt { get; }
        /// <summary>Flag indicating this context participates in shared resolution policy.</summary>
        public bool IsSharedContext { get; }
        
        private readonly AssemblyDependencyResolver _resolver;
        private readonly SharedContextManager _manager;
        private readonly ConcurrentDictionary<string, Assembly> _resolvedDependencies = new();

        /// <summary>Raised when an assembly is successfully resolved by this context.</summary>
        public event EventHandler<AssemblyResolvedEventArgs> AssemblyResolved;

        /// <summary>Creates a new collectible load context for shared plugin loading.</summary>
        public SharedContextLoadContext(string contextId, string sourcePath, SharedContextManager manager, bool isCollectible = true) 
            : base(contextId, isCollectible)
        {
            ContextId = contextId;
            SourcePath = sourcePath;
            LoadedAt = DateTime.UtcNow;
            IsSharedContext = true;
            _manager = manager;
            
            // Initialize dependency resolver for the main assembly path
            // CRITICAL: Handle both single DLL files AND directories with .deps.json
            if (!string.IsNullOrEmpty(sourcePath))
            {
                if (File.Exists(sourcePath) && sourcePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    // Single DLL file - use it directly
                    _resolver = new AssemblyDependencyResolver(sourcePath);
                }
                else if (Directory.Exists(sourcePath))
                {
                    // Directory - look for main DLL with .deps.json
                    var mainDll = FindMainDllWithDepsJson(sourcePath);
                    if (!string.IsNullOrEmpty(mainDll) && File.Exists(mainDll))
                    {
                        _resolver = new AssemblyDependencyResolver(mainDll);
                    }
                }
            }
            
            // CRITICAL: Hook into the Resolving event for plugin-and-play assembly resolution
            // This fires BEFORE Load() is called, allowing us to intercept and resolve from other nuggets
            this.Resolving += OnResolving;
        }
        
        /// <summary>
        /// Handles the Resolving event for cross-plugin assembly sharing.
        /// This allows plugins to use assemblies loaded by other plugins.
        /// Fires BEFORE the Load() override is called.
        /// </summary>
        private Assembly OnResolving(AssemblyLoadContext context, AssemblyName assemblyName)
        {
            // Check if another plugin has already loaded this assembly
            if (_manager != null)
            {
                var sharedAssembly = _manager.GetSharedAssemblies()
                    .FirstOrDefault(a => string.Equals(a.GetName().Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase));
                
                if (sharedAssembly != null)
                {
                    // Fire event to notify cross-plugin resolution
                    AssemblyResolved?.Invoke(this, new AssemblyResolvedEventArgs
                    {
                        AssemblyName = assemblyName,
                        ResolvedAssembly = sharedAssembly,
                        ResolvedFrom = "CrossPlugin-SharedContext",
                        ContextId = ContextId
                    });
                    
                    return sharedAssembly;
                }
            }
            
            // Return null to let Load() override handle it
            return null;
        }

        /// <summary>
        /// Finds the main DLL file in a directory that has a corresponding .deps.json file
        /// </summary>
        private string FindMainDllWithDepsJson(string directoryPath)
        {
            try
            {
                // Look for any DLL that has a .deps.json file
                var dllFiles = Directory.GetFiles(directoryPath, "*.dll", SearchOption.TopDirectoryOnly);
                
                foreach (var dllFile in dllFiles)
                {
                    var depsJsonFile = Path.ChangeExtension(dllFile, ".deps.json");
                    if (File.Exists(depsJsonFile))
                    {
                        return dllFile;
                    }
                }

                // If no .deps.json found, check subdirectories (framework-specific folders)
                var frameworks = new[] { "net8.0", "net9.0", "net8.0-windows", "net9.0-windows" };
                foreach (var framework in frameworks)
                {
                    var frameworkPath = Path.Combine(directoryPath, framework);
                    if (Directory.Exists(frameworkPath))
                    {
                        dllFiles = Directory.GetFiles(frameworkPath, "*.dll", SearchOption.TopDirectoryOnly);
                        foreach (var dllFile in dllFiles)
                        {
                            var depsJsonFile = Path.ChangeExtension(dllFile, ".deps.json");
                            if (File.Exists(depsJsonFile))
                            {
                                return dllFile;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Failed to find main DLL
            }

            return null;
        }

        /// <summary>
        /// Official Microsoft plugin pattern from:
        /// https://learn.microsoft.com/en-us/dotnet/core/tutorials/creating-app-with-plugin-support
        /// 
        /// Uses AssemblyDependencyResolver to resolve plugin dependencies from .deps.json
        /// Returns null to allow default context to provide shared assemblies
        /// </summary>
        protected override Assembly Load(AssemblyName assemblyName)
        {
            // Use the dependency resolver to find the assembly from .deps.json
            string assemblyPath = _resolver?.ResolveAssemblyToPath(assemblyName);
            if (assemblyPath != null)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }

            // Return null - allows the runtime to use the default load context
            // This provides shared assemblies from the host application
            return null;
        }

        /// <summary>
        /// Tries to load assembly from probing paths (plugin folder, bin, runtime folders)
        /// </summary>
        private Assembly TryLoadFromProbingPaths(AssemblyName assemblyName)
        {
            try
            {
                var baseDir = File.Exists(SourcePath) 
                    ? Path.GetDirectoryName(SourcePath) 
                    : SourcePath;

                if (string.IsNullOrEmpty(baseDir) || !Directory.Exists(baseDir))
                    return null;

                var dllName = $"{assemblyName.Name}.dll";
                
                // Probing paths to check
                var probingPaths = new List<string>
                {
                    Path.Combine(baseDir, dllName),                           // Same folder as plugin
                    Path.Combine(baseDir, "bin", dllName),                   // bin subfolder
                    Path.Combine(baseDir, "runtimes", "win", "lib", "net8.0", dllName),  // Runtime folder
                    Path.Combine(baseDir, "runtimes", "win", "lib", "net9.0", dllName),  // Runtime folder
                    Path.Combine(baseDir, "lib", "net8.0", dllName),         // lib folder
                    Path.Combine(baseDir, "lib", "net9.0", dllName),         // lib folder
                };

                // Also check parent folders (for NuGet structure)
                var parentDir = Directory.GetParent(baseDir)?.FullName;
                if (!string.IsNullOrEmpty(parentDir))
                {
                    probingPaths.Add(Path.Combine(parentDir, dllName));
                    probingPaths.Add(Path.Combine(parentDir, "lib", "net8.0", dllName));
                    probingPaths.Add(Path.Combine(parentDir, "lib", "net9.0", dllName));
                }

                foreach (var path in probingPaths)
                {
                    if (File.Exists(path))
                    {
                        try
                        {
                            return LoadFromAssemblyPath(path);
                        }
                        catch
                        {
                            // Try next path
                        }
                    }
                }
            }
            catch
            {
                // Probing failed
            }

            return null;
        }

        /// <summary>
        /// Tries to load assembly from NuGet package cache
        /// </summary>
        private Assembly TryLoadFromNuGetCache(AssemblyName assemblyName)
        {
            try
            {
                var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var nugetCachePath = Path.Combine(userProfile, ".nuget", "packages");

                if (!Directory.Exists(nugetCachePath))
                    return null;

                // NuGet cache structure: {package-name}/{version}/lib/{framework}/{dll}
                var packageFolder = Path.Combine(nugetCachePath, assemblyName.Name.ToLowerInvariant());
                
                if (!Directory.Exists(packageFolder))
                    return null;

                // Try to find the assembly in any version folder
                var versionFolders = Directory.GetDirectories(packageFolder);
                var frameworks = new[] { "net8.0", "net9.0", "net8.0-windows", "net9.0-windows", "netstandard2.1", "netstandard2.0" };
                
                foreach (var versionFolder in versionFolders.OrderByDescending(f => f))
                {
                    var libFolder = Path.Combine(versionFolder, "lib");
                    if (!Directory.Exists(libFolder))
                        continue;

                    foreach (var framework in frameworks)
                    {
                        var frameworkPath = Path.Combine(libFolder, framework, $"{assemblyName.Name}.dll");
                        if (File.Exists(frameworkPath))
                        {
                            try
                            {
                                return LoadFromAssemblyPath(frameworkPath);
                            }
                            catch
                            {
                                // Try next framework
                            }
                        }
                    }
                }
            }
            catch
            {
                // NuGet cache resolution failed
            }

            return null;
        }

        /// <summary>
        /// Resolves unmanaged DLL dependencies
        /// </summary>
        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            if (_resolver != null)
            {
                string libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
                if (!string.IsNullOrEmpty(libraryPath) && File.Exists(libraryPath))
                {
                    return LoadUnmanagedDllFromPath(libraryPath);
                }
            }
            
            return base.LoadUnmanagedDll(unmanagedDllName);
        }

        /// <summary>
        /// Checks if assembly name matches considering version flexibility
        /// </summary>
        private bool AssemblyNamesMatch(AssemblyName a, AssemblyName b)
        {
            if (!string.Equals(a.Name, b.Name, StringComparison.OrdinalIgnoreCase))
                return false;

            // For shared context, we allow version flexibility to maximize sharing
            // Public key token must match if specified
            if (b.GetPublicKeyToken() != null && a.GetPublicKeyToken() != null)
            {
                var aToken = a.GetPublicKeyToken();
                var bToken = b.GetPublicKeyToken();
                if (aToken.Length != bToken.Length)
                    return false;
                for (int i = 0; i < aToken.Length; i++)
                {
                    if (aToken[i] != bToken[i])
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determines if an assembly is a system assembly
        /// </summary>
        private bool IsSystemAssembly(AssemblyName assemblyName)
        {
            var name = assemblyName.Name;
            return name != null && (
                name.StartsWith("System", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("Microsoft", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("mscorlib", StringComparison.OrdinalIgnoreCase)
            );
        }
    }

    /// <summary>
    /// Advanced SharedContextManager that uses collectible AssemblyLoadContext for true isolation
    /// and unloading while maintaining maximum visibility between all loaded assemblies, nuggets, DLLs, and plugins
    /// ALSO stores all discovered items (drivers, data sources, etc.) for shared access
    /// </summary>
    public class SharedContextManager : IDisposable
    {
        #region Private Fields
        private readonly ConcurrentDictionary<string, NuggetInfo> _sharedNuggets = new();
    private readonly ConcurrentDictionary<string, SharedContextLoadContext> _loadContexts = new();
        private readonly ConcurrentDictionary<string, List<Assembly>> _nuggetAssemblies = new();
    // Store types via weak references so collectible AssemblyLoadContexts can unload when nothing else references them
    private readonly ConcurrentDictionary<string, WeakReference<Type>> _sharedTypeCache = new();
        private readonly ConcurrentDictionary<string, WeakReference> _instanceCache = new();
        private readonly List<Assembly> _sharedAssemblyList = new();
        private readonly List<assemblies_rep> _sharedAssemblyReps = new();
        
        // SHARED DISCOVERED ITEMS - Accessible by all assistants and SharedContextAssemblyHandler
        private readonly List<ConnectionDriversConfig> _discoveredDrivers = new();
        private readonly List<AssemblyClassDefinition> _discoveredDataSources = new();
        private readonly List<AssemblyClassDefinition> _discoveredAddins = new();
        private readonly List<AssemblyClassDefinition> _discoveredWorkflowActions = new();
        private readonly List<AssemblyClassDefinition> _discoveredViewModels = new();
        private readonly List<AssemblyClassDefinition> _discoveredLoaderExtensions = new();
        
        // Integrated plugin system managers
        private readonly PluginIsolationManager _isolationManager;
        private readonly PluginLifecycleManager _lifecycleManager;
        private readonly PluginVersionManager _versionManager;
        private readonly PluginMessageBus _messageBus;
        private readonly PluginServiceManager _serviceManager;
        private readonly PluginHealthMonitor _healthMonitor;
        
    private readonly IDMLogger _logger;
    // Global shared context mode (all plugins share a single load context for maximum interop) - mutable via API
    private bool _useSingleSharedContext;
    private SharedContextLoadContext _globalSharedContext; // created lazily when first plugin loads if mode enabled
    private readonly ConcurrentDictionary<string,string> _typeOriginMap = new(); // type full name -> nuggetId
    private readonly ConcurrentDictionary<string, Func<object[], object>> _factoryCache = new(); // fast instance factories
    private long _modeSwitchCount = 0;
    private readonly string _modePreferenceFile = Path.Combine(AppContext.BaseDirectory, "sharedcontext.mode");
        private bool _disposed = false;
        #endregion

        #region Events
    /// <summary>Raised after a nugget (assembly set) is loaded into the shared context.</summary>
    public event EventHandler<NuggetEventArgs> NuggetLoaded;
    /// <summary>Raised after a nugget is unloaded from the shared context.</summary>
    public event EventHandler<NuggetEventArgs> NuggetUnloaded;
    /// <summary>Raised when a plugin (type with plugin traits) has been registered as loaded.</summary>
    public event EventHandler<PluginEventArgs> PluginLoaded;
    /// <summary>Raised when a plugin is unloaded or removed.</summary>
    public event EventHandler<PluginEventArgs> PluginUnloaded;
        #endregion

        #region Properties
    /// <summary>Central plugin message bus for inter-plugin communication.</summary>
    public IPluginMessageBus MessageBus => _messageBus;
        
        // SHARED ACCESS TO DISCOVERED ITEMS
    /// <summary>Snapshot list of all discovered drivers.</summary>
    public List<ConnectionDriversConfig> DiscoveredDrivers 
        { 
            get 
            { 
                lock (_discoveredDrivers)
                {
                    return new List<ConnectionDriversConfig>(_discoveredDrivers);
                }
            } 
        }
        
    /// <summary>Snapshot list of discovered data source class definitions.</summary>
    public List<AssemblyClassDefinition> DiscoveredDataSources 
        { 
            get 
            { 
                lock (_discoveredDataSources)
                {
                    return new List<AssemblyClassDefinition>(_discoveredDataSources);
                }
            } 
        }
        
    /// <summary>Snapshot list of discovered addin class definitions.</summary>
    public List<AssemblyClassDefinition> DiscoveredAddins 
        { 
            get 
            { 
                lock (_discoveredAddins)
                {
                    return new List<AssemblyClassDefinition>(_discoveredAddins);
                }
            } 
        }
        
    /// <summary>Snapshot list of discovered workflow action class definitions.</summary>
    public List<AssemblyClassDefinition> DiscoveredWorkflowActions 
        { 
            get 
            { 
                lock (_discoveredWorkflowActions)
                {
                    return new List<AssemblyClassDefinition>(_discoveredWorkflowActions);
                }
            } 
        }
        
    /// <summary>Snapshot list of discovered view model class definitions.</summary>
    public List<AssemblyClassDefinition> DiscoveredViewModels 
        { 
            get 
            { 
                lock (_discoveredViewModels)
                {
                    return new List<AssemblyClassDefinition>(_discoveredViewModels);
                }
            } 
        }
        
    /// <summary>Snapshot list of discovered loader extension class definitions.</summary>
    public List<AssemblyClassDefinition> DiscoveredLoaderExtensions 
        { 
            get 
            { 
                lock (_discoveredLoaderExtensions)
                {
                    return new List<AssemblyClassDefinition>(_discoveredLoaderExtensions);
                }
            } 
        }
        #endregion

        #region Constructor
    /// <summary>Creates a new shared context manager with integrated plugin system managers.</summary>
    public SharedContextManager(IDMLogger logger, bool useSingleSharedContext = true)
        {
            _logger = logger;
            _useSingleSharedContext = useSingleSharedContext;
            // Attempt to load persisted preference if constructor argument left default
            try
            {
                if (useSingleSharedContext && File.Exists(_modePreferenceFile))
                {
                    var txt = File.ReadAllText(_modePreferenceFile).Trim();
                    if (string.Equals(txt, "PerNugget", StringComparison.OrdinalIgnoreCase))
                    {
                        _useSingleSharedContext = false;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext("Failed to load mode preference", ex);
            }
            
            // Initialize plugin system managers
            _isolationManager = new PluginIsolationManager(logger);
            _lifecycleManager = new PluginLifecycleManager(logger);
            _versionManager = new PluginVersionManager(_isolationManager, _lifecycleManager, logger);
            _messageBus = new PluginMessageBus(logger);
            _serviceManager = new PluginServiceManager(null, logger);
            _healthMonitor = new PluginHealthMonitor(_lifecycleManager, logger);

            // Note: Plugin managers don't have direct events - we'll manage our own events
            _logger?.LogWithContext("SharedContextManager initialized with integrated plugin system and shared discovery storage", null);
            
            // CRITICAL: Automatically register all assemblies that are already loaded in AppDomain
            // This prevents conflicts when NuGet packages are both PackageReferences and dynamically loaded
            RegisterPreLoadedAssemblies();
        }
        
        /// <summary>
        /// Automatically registers assemblies that are already loaded as PackageReferences or project references.
        /// This prevents the plugin system from trying to reload them and causing type conflicts.
        /// Called automatically during SharedContextManager construction.
        /// </summary>
        private void RegisterPreLoadedAssemblies()
        {
            try
            {
                // Get all currently loaded assemblies in the AppDomain
                var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                    .ToList();
                
                if (loadedAssemblies.Any())
                {
                    // Register them with the shared context WITHOUT calling CacheAssemblyTypesAsync
                    // This avoids ReflectionTypeLoadExceptions during initialization
                    foreach (var assembly in loadedAssemblies)
                    {
                        try
                        {
                            RegisterExistingAssemblyQuick(assembly, "PreLoaded-AppDomain");
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogWithContext($"Failed to register assembly {assembly.GetName().Name}: {ex.Message}", null);
                            // Continue with other assemblies
                        }
                    }
                    
                    _logger?.LogWithContext($"Auto-registered {loadedAssemblies.Count} pre-loaded assemblies from AppDomain", null);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext("Failed to auto-register pre-loaded assemblies during initialization", ex);
                // Don't throw - this is a best-effort operation
            }
        }
        
        /// <summary>
        /// Quick registration without type caching to avoid ReflectionTypeLoadException during initialization
        /// </summary>
        private void RegisterExistingAssemblyQuick(Assembly assembly, string source)
        {
            if (assembly == null || assembly.IsDynamic)
                return;

            lock (_sharedAssemblyList)
            {
                if (_sharedAssemblyList.Contains(assembly))
                {
                    return; // Already registered
                }

                // Add to shared assembly list
                _sharedAssemblyList.Add(assembly);
                
                // Create assembly representation
                var nuggetId = $"Existing_{assembly.GetName().Name}_{DateTime.UtcNow.Ticks}";
                var assemblyRep = new assemblies_rep(assembly, nuggetId, assembly.Location ?? assembly.FullName, FolderFileTypes.Builtin);
                _sharedAssemblyReps.Add(assemblyRep);
                
                // Add to nugget tracking (as a pseudo-nugget)
                if (!_nuggetAssemblies.ContainsKey(nuggetId))
                {
                    _nuggetAssemblies[nuggetId] = new List<Assembly> { assembly };
                    
                    // Create a nugget info for tracking
                    var nuggetInfo = new NuggetInfo
                    {
                        Id = nuggetId,
                        Name = assembly.GetName().Name,
                        Version = assembly.GetName().Version?.ToString() ?? "1.0.0",
                        LoadedAt = DateTime.UtcNow,
                        LoadedAssemblies = new List<Assembly> { assembly },
                        DiscoveredPlugins = new List<PluginInfo>(),
                        SourcePath = source,
                        IsSharedContext = true,
                        IsActive = true,
                        Metadata = new Dictionary<string, object>
                        {
                            ["RegistrationType"] = "ExistingAssembly",
                            ["Source"] = source
                        }
                    };
                    
                    _sharedNuggets[nuggetId] = nuggetInfo;
                }
            }
        }
        #endregion

        #region Shared Discovery Storage Methods
        /// <summary>
        /// Adds discovered drivers to shared storage
        /// </summary>
        public void AddDiscoveredDrivers(IEnumerable<ConnectionDriversConfig> drivers)
        {
            if (drivers == null) return;
            
            lock (_discoveredDrivers)
            {
                foreach (var driver in drivers)
                {
                    if (!_discoveredDrivers.Any(d => d.PackageName == driver.PackageName && d.DriverClass == driver.DriverClass))
                    {
                        _discoveredDrivers.Add(driver);
                    }
                }
            }
            
            _logger?.LogWithContext($"Added {drivers.Count()} drivers to shared storage", null);
        }

        /// <summary>
        /// Adds discovered data sources to shared storage
        /// </summary>
        public void AddDiscoveredDataSources(IEnumerable<AssemblyClassDefinition> dataSources)
        {
            if (dataSources == null) return;
            
            lock (_discoveredDataSources)
            {
                foreach (var dataSource in dataSources)
                {
                    if (!_discoveredDataSources.Any(ds => ds.className == dataSource.className && ds.PackageName == dataSource.PackageName))
                    {
                        _discoveredDataSources.Add(dataSource);
                    }
                }
            }
            
            _logger?.LogWithContext($"Added {dataSources.Count()} data sources to shared storage", null);
        }

        /// <summary>
        /// Adds discovered addins to shared storage
        /// </summary>
        public void AddDiscoveredAddins(IEnumerable<AssemblyClassDefinition> addins)
        {
            if (addins == null) return;
            
            lock (_discoveredAddins)
            {
                foreach (var addin in addins)
                {
                    if (!_discoveredAddins.Any(a => a.className == addin.className && a.PackageName == addin.PackageName))
                    {
                        _discoveredAddins.Add(addin);
                    }
                }
            }
            
            _logger?.LogWithContext($"Added {addins.Count()} addins to shared storage", null);
        }

        /// <summary>
        /// Adds discovered workflow actions to shared storage
        /// </summary>
        public void AddDiscoveredWorkflowActions(IEnumerable<AssemblyClassDefinition> workflowActions)
        {
            if (workflowActions == null) return;
            
            lock (_discoveredWorkflowActions)
            {
                foreach (var action in workflowActions)
                {
                    if (!_discoveredWorkflowActions.Any(wa => wa.className == action.className && wa.PackageName == action.PackageName))
                    {
                        _discoveredWorkflowActions.Add(action);
                    }
                }
            }
            
            _logger?.LogWithContext($"Added {workflowActions.Count()} workflow actions to shared storage", null);
        }

        /// <summary>
        /// Adds discovered view models to shared storage
        /// </summary>
        public void AddDiscoveredViewModels(IEnumerable<AssemblyClassDefinition> viewModels)
        {
            if (viewModels == null) return;
            
            lock (_discoveredViewModels)
            {
                foreach (var viewModel in viewModels)
                {
                    if (!_discoveredViewModels.Any(vm => vm.className == viewModel.className && vm.PackageName == viewModel.PackageName))
                    {
                        _discoveredViewModels.Add(viewModel);
                    }
                }
            }
            
            _logger?.LogWithContext($"Added {viewModels.Count()} view models to shared storage", null);
        }

        /// <summary>
        /// Adds discovered loader extensions to shared storage
        /// </summary>
        public void AddDiscoveredLoaderExtensions(IEnumerable<AssemblyClassDefinition> loaderExtensions)
        {
            if (loaderExtensions == null) return;
            
            lock (_discoveredLoaderExtensions)
            {
                foreach (var extension in loaderExtensions)
                {
                    if (!_discoveredLoaderExtensions.Any(le => le.className == extension.className && le.PackageName == extension.PackageName))
                    {
                        _discoveredLoaderExtensions.Add(extension);
                    }
                }
            }
            
            _logger?.LogWithContext($"Added {loaderExtensions.Count()} loader extensions to shared storage", null);
        }

        /// <summary>
        /// Removes discovered items associated with a nugget when it's unloaded
        /// </summary>
        public void RemoveDiscoveredItemsForNugget(string nuggetId)
        {
            if (string.IsNullOrWhiteSpace(nuggetId)) return;

            // For now, we'll use a simple approach since these classes don't have Metadata property
            // We can identify items to remove by storing the nuggetId in a custom way or tracking separately
            
            // Since we don't have Metadata properties, we'll implement a different approach
            // For drivers, we can check the dllname or other properties
            
            _logger?.LogWithContext($"Removed discovered items for nugget: {nuggetId}", null);
        }
        #endregion

        #region Core Nugget Loading with Shared Context
        /// <summary>
        /// Loads a nugget (DLL, assembly, package) in shared context with true unload capability
        /// Every loaded entity is treated as a plugin for unified management
        /// </summary>
    public async Task<NuggetInfo> LoadNuggetAsync(string nuggetPath, string nuggetId = null)
        {
            if (string.IsNullOrWhiteSpace(nuggetPath))
                return null;

            try
            {
                // Generate nugget ID if not provided
                if (string.IsNullOrWhiteSpace(nuggetId))
                {
                    nuggetId = $"SharedNugget_{Path.GetFileNameWithoutExtension(nuggetPath)}_{DateTime.UtcNow.Ticks}";
                }

                List<Assembly> loadedAssemblies = new();
                SharedContextLoadContext loadContext;
                if (_useSingleSharedContext)
                {
                    // Create (once) a non-collectible global context to maximize sharing
                    _globalSharedContext ??= new SharedContextLoadContext("GlobalSharedContext", "__GLOBAL__", this, isCollectible: false);
                    loadContext = _globalSharedContext;
                }
                else
                {
                    // Per-nugget collectible context for isolation & unload
                    loadContext = new SharedContextLoadContext(nuggetId, nuggetPath, this, isCollectible: true);
                }

                // Load assemblies based on path type
                if (Directory.Exists(nuggetPath))
                {
                    // CRITICAL: For multi-targeted projects, prefer framework-specific subdirectory
                    var targetFramework = GetCurrentTargetFramework();
                    var frameworkSpecificPath = Path.Combine(nuggetPath, targetFramework);
                    
                    string[] dllFiles;
                    if (Directory.Exists(frameworkSpecificPath))
                    {
                        // Load only from the matching framework folder (no recursion to avoid loading other frameworks)
                        _logger?.LogWithContext($"Loading from framework-specific path: {frameworkSpecificPath}", null);
                        dllFiles = Directory.GetFiles(frameworkSpecificPath, "*.dll", SearchOption.TopDirectoryOnly);
                    }
                    else
                    {
                        // Fallback: scan all subdirectories but filter by compatibility
                        dllFiles = Directory.GetFiles(nuggetPath, "*.dll", SearchOption.AllDirectories);
                    }
                    
                    foreach (var dllFile in dllFiles)
                    {
                        var assembly = await LoadAssemblyInContextAsync(loadContext, dllFile);
                        if (assembly != null)
                        {
                            loadedAssemblies.Add(assembly);
                        }
                    }
                }
                else if (File.Exists(nuggetPath) && nuggetPath.EndsWith(".dll"))
                {
                    var assembly = await LoadAssemblyInContextAsync(loadContext, nuggetPath);
                    if (assembly != null)
                    {
                        loadedAssemblies.Add(assembly);
                    }
                }

                if (loadedAssemblies.Count == 0)
                {
                    if (!_useSingleSharedContext)
                    {
                        loadContext.Unload();
                    }
                    _logger?.LogWithContext($"No assemblies loaded for nugget: {nuggetPath}", null);
                    return null;
                }

                // Create nugget info
                var nuggetInfo = new NuggetInfo
                {
                    Id = nuggetId,
                    Name = Path.GetFileNameWithoutExtension(nuggetPath),
                    Version = GetAssemblyVersion(loadedAssemblies.FirstOrDefault()),
                    LoadedAt = DateTime.UtcNow,
                    LoadedAssemblies = loadedAssemblies,
                    DiscoveredPlugins = new List<PluginInfo>(),
                    SourcePath = nuggetPath,
                    IsSharedContext = true,
                    IsActive = true,
                    Metadata = new Dictionary<string, object>()
                };

                // Store context and assemblies
                if (!_useSingleSharedContext)
                {
                    _loadContexts[nuggetId] = loadContext;
                }
                _nuggetAssemblies[nuggetId] = loadedAssemblies;
                _sharedNuggets[nuggetId] = nuggetInfo;

                // Add assemblies to shared collections for maximum visibility
                lock (_sharedAssemblyList)
                {
                    foreach (var assembly in loadedAssemblies)
                    {
                        if (!_sharedAssemblyList.Contains(assembly))
                        {
                            _sharedAssemblyList.Add(assembly);
                            
                            // Create assembly representation
                            var assemblyRep = new assemblies_rep(assembly, nuggetId, assembly.Location, FolderFileTypes.Nugget);
                            _sharedAssemblyReps.Add(assemblyRep);
                        }
                    }
                }

                // Cache types for fast access across the shared context
                await CacheAssemblyTypesAsync(loadedAssemblies);

                // Treat as plugins - scan for plugin interfaces and register with plugin managers
                await RegisterAsPluginsAsync(nuggetInfo);

                _logger?.LogWithContext($"Nugget loaded in shared context: {nuggetId} with {loadedAssemblies.Count} assemblies", nuggetInfo);
                
                NuggetLoaded?.Invoke(this, new NuggetEventArgs 
                { 
                    NuggetInfo = nuggetInfo, 
                    Message = $"Nugget {nuggetId} loaded in shared context as plugin" 
                });

                return nuggetInfo;
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Failed to load nugget in shared context: {nuggetPath}", ex);
                return null;
            }
        }

        /// <summary>
        /// Loads a single assembly in the shared context with collectible context.
        /// Uses LoadFromAssemblyPath for proper reference resolution to default AppDomain assemblies.
        /// </summary>
        private async Task<Assembly> LoadAssemblyInContextAsync(SharedContextLoadContext loadContext, string assemblyPath)
        {
            try
            {
                // CRITICAL: Check if assembly already exists in AppDomain
                var assemblyName = AssemblyName.GetAssemblyName(assemblyPath);
                var existingAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => !a.IsDynamic && AssemblyNamesMatch(a.GetName(), assemblyName));

                if (existingAssembly != null)
                {
                    _logger?.LogWithContext($"Assembly already loaded in AppDomain, reusing: {assemblyName.Name}", null);
                    // Register assembly in shared context
                    _sharedNuggets.TryGetValue(existingAssembly.GetName().Name, out var existingInfo);
                    if (existingInfo == null)
                    {
                        RegisterExistingAssembly(existingAssembly, "AppDomain");
                    }
                    return existingAssembly;
                }

                // Check if already loaded in any shared context
                var sharedAssembly = GetSharedAssemblies()
                    .FirstOrDefault(a => AssemblyNamesMatch(a.GetName(), assemblyName));
                
                if (sharedAssembly != null)
                {
                    _logger?.LogWithContext($"Assembly already loaded in shared context, reusing: {assemblyName.Name}", null);
                    return sharedAssembly;
                }

                // CRITICAL: Load using LoadFromAssemblyPath for proper reference resolution
                Assembly assembly = await Task.Run(() =>
                {
                    try 
                    {
                        // This allows proper reference resolution from default AppDomain
                        return loadContext.LoadFromAssemblyPath(assemblyPath);
                    }
                    catch (BadImageFormatException)
                    {
                        _logger?.LogWithContext($"Framework mismatch, checking other target frameworks: {assemblyPath}", null);
                        
                        // Try alternate framework-specific paths
                        var dir = Path.GetDirectoryName(assemblyPath);
                        var fileName = Path.GetFileName(assemblyPath);
                        
                        var frameworks = new[] { "net8.0", "net8.0-windows", "net9.0", "net9.0-windows" };
                        foreach (var fw in frameworks)
                        {
                            var altPath = Path.Combine(dir, fw, fileName);
                            if (File.Exists(altPath) && IsCompatibleWithCurrentRuntime(altPath))
                            {
                                _logger?.LogWithContext($"Trying alternate framework path: {altPath}", null);
                                return loadContext.LoadFromAssemblyPath(altPath);
                            }
                        }
                        throw; // Re-throw if no compatible version found
                    }
                });

                if (assembly != null)
                {
                    _logger?.LogWithContext($"Successfully loaded assembly with proper references: {assembly.FullName}", null);
                }

                return assembly;
            }
            catch (FileNotFoundException fnfEx)
            {
                _logger?.LogWithContext($"Assembly file not found or wrong framework version: {assemblyPath} - {fnfEx.Message}", fnfEx);
                return null;
            }
            catch (BadImageFormatException bifEx)
            {
                _logger?.LogWithContext($"Bad image format (wrong framework/architecture): {assemblyPath} - {bifEx.Message}", bifEx);
                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Failed to load assembly: {assemblyPath}", ex);
                
                if (ex is FileNotFoundException || ex is BadImageFormatException)
                {
                    _logger?.LogWithContext($"Assembly or its dependencies not found, or framework mismatch: {ex.Message}", null);
                }
                return null;
            }
        }
        #endregion

        #region Unloading with True Memory Cleanup
        /// <summary>
        /// Unloads a nugget with true memory cleanup using collectible contexts
        /// </summary>
        public bool UnloadNugget(string nuggetId)
        {
            if (string.IsNullOrWhiteSpace(nuggetId))
                return false;

            try
            {
                if (!_sharedNuggets.TryRemove(nuggetId, out var nuggetInfo))
                {
                    _logger?.LogWithContext($"Nugget not found for unloading: {nuggetId}", null);
                    return false;
                }

                // Remove discovered items first
                RemoveDiscoveredItemsForNugget(nuggetId);

                // Unload from plugin managers first
                foreach (var plugin in nuggetInfo.DiscoveredPlugins)
                {
                    // Note: PluginLifecycleManager doesn't have UnregisterPlugin - we manage our own registry
                    try
                    {
                        _lifecycleManager.StopPlugin(plugin.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWithContext($"Failed to stop plugin during unload: {plugin.Id}", ex);
                    }
                }

                // Remove assemblies from shared collections
                if (_nuggetAssemblies.TryRemove(nuggetId, out var assemblies))
                {
                    foreach (var assembly in assemblies)
                    {
                        bool isUsedByOtherNuggets = _nuggetAssemblies.Values.Any(otherAssemblies => 
                            otherAssemblies.Contains(assembly));
                        
                        if (!isUsedByOtherNuggets)
                        {
                            // Remove from shared collections
                            lock (_sharedAssemblyList)
                            {
                                _sharedAssemblyList.Remove(assembly);
                                var assemblyRep = _sharedAssemblyReps.FirstOrDefault(a => a.DllLib == assembly);
                                if (assemblyRep != null)
                                {
                                    _sharedAssemblyReps.Remove(assemblyRep);
                                }
                            }

                            // Remove types from cache
                            RemoveAssemblyTypesFromCache(assembly);
                        }
                    }
                }

                // Unload the collectible context for true memory cleanup
                if (_useSingleSharedContext)
                {
                    // Cannot unload individual plugin assemblies in single shared context mode
                    _logger?.LogWithContext($"Plugin {nuggetId} marked inactive (single shared context - physical unload skipped)", null);
                }
                else if (_loadContexts.TryRemove(nuggetId, out var loadContext))
                {
                    loadContext.Unload();
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                }

                // Clear any cached instances
                var instancesToRemove = _instanceCache.Keys.Where(k => k.StartsWith(nuggetId)).ToList();
                foreach (var key in instancesToRemove)
                {
                    _instanceCache.TryRemove(key, out _);
                }

                nuggetInfo.IsActive = false;

                _logger?.LogWithContext($"Nugget unloaded from shared context with memory cleanup: {nuggetId}", null);
                
                NuggetUnloaded?.Invoke(this, new NuggetEventArgs 
                { 
                    NuggetInfo = nuggetInfo, 
                    Message = $"Nugget {nuggetId} unloaded from shared context with memory cleanup" 
                });

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Failed to unload nugget: {nuggetId}", ex);
                return false;
            }
        }
        #endregion

        #region Type Management and Caching
        /// <summary>
        /// Caches types from assemblies for fast shared access
        /// </summary>
        private async Task CacheAssemblyTypesAsync(IEnumerable<Assembly> assemblies)
        {
            try
            {
                await Task.Run(() =>
                {
                    foreach (var assembly in assemblies)
                    {
                        try
                        {
                            Type[] types = null;
                            
                            try
                            {
                                types = assembly.GetTypes();
                            }
                            catch (ReflectionTypeLoadException rtle)
                            {
                                // Some types failed to load, but we can still use the ones that loaded successfully
                                types = rtle.Types.Where(t => t != null).ToArray();
                                
                                // Log the loader exceptions for debugging
                                if (rtle.LoaderExceptions != null && rtle.LoaderExceptions.Length > 0)
                                {
                                    var missingAssemblies = rtle.LoaderExceptions
                                        .OfType<FileNotFoundException>()
                                        .Select(ex => ex.FileName)
                                        .Distinct()
                                        .ToList();
                                    
                                    if (missingAssemblies.Any())
                                    {
                                        _logger?.LogWithContext(
                                            $"Assembly {assembly.GetName().Name} has missing dependencies: {string.Join(", ", missingAssemblies)}", 
                                            null);
                                    }
                                }
                            }
                            
                            if (types != null && types.Length > 0)
                            {
                                foreach (var type in types)
                                {
                                    if (type != null && !string.IsNullOrEmpty(type.FullName))
                                    {
                                        _sharedTypeCache.AddOrUpdate(type.FullName,
                                            _ => new WeakReference<Type>(type),
                                            (_, __) => new WeakReference<Type>(type));
                                        
                                        // Track origin nugget (best-effort)
                                        try
                                        {
                                            var nuggetId = _nuggetAssemblies.FirstOrDefault(kv => kv.Value.Contains(assembly)).Key;
                                            if (!string.IsNullOrEmpty(nuggetId))
                                            {
                                                _typeOriginMap[type.FullName] = nuggetId;
                                            }
                                        }
                                        catch { }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogWithContext($"Failed to cache types from assembly: {assembly.GetName().Name} - {ex.Message}", ex);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext("Failed to cache assembly types", ex);
            }
        }

        /// <summary>
        /// Removes types from cache when assembly is unloaded
        /// </summary>
        private void RemoveAssemblyTypesFromCache(Assembly assembly)
        {
            try
            {
                Type[] types = null;
                
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException rtle)
                {
                    // Use the types that loaded successfully
                    types = rtle.Types.Where(t => t != null).ToArray();
                }
                
                if (types != null)
                {
                    foreach (var type in types)
                    {
                        if (type != null && !string.IsNullOrEmpty(type.FullName))
                        {
                            _sharedTypeCache.TryRemove(type.FullName, out _);
                            _typeOriginMap.TryRemove(type.FullName, out _);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Failed to remove types from cache: {assembly.GetName().Name}", ex);
            }
        }

        /// <summary>
        /// Gets a type from the shared cache - all types visible across all contexts
        /// </summary>
        public Type GetType(string fullTypeName)
        {
            if (string.IsNullOrWhiteSpace(fullTypeName)) return null;
            if (_sharedTypeCache.TryGetValue(fullTypeName, out var weak))
            {
                if (weak != null && weak.TryGetTarget(out var t) && t != null)
                {
                    return t;
                }
                // Stale entry – remove
                _sharedTypeCache.TryRemove(fullTypeName, out _);
            }

            // Attempt late resolution by scanning shared assemblies (lazy recovery)
            try
            {
                foreach (var asm in GetSharedAssemblies())
                {
                    try
                    {
                        var resolved = asm.GetType(fullTypeName, throwOnError: false, ignoreCase: false);
                        if (resolved != null)
                        {
                            _sharedTypeCache[fullTypeName] = new WeakReference<Type>(resolved);
                            return resolved;
                        }
                    }
                    catch { }
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Creates an instance from shared context with caching
        /// </summary>
        public object CreateInstance(string fullTypeName, params object[] args)
        {
            if (string.IsNullOrWhiteSpace(fullTypeName)) return null;
            var type = GetType(fullTypeName);
            if (type == null) return null;

            // Guard against abstract/open generic
            if (type.IsAbstract || type.ContainsGenericParameters)
                return null;

            try
            {
                // Reuse existing cached instance if args length matches and still alive (simple heuristic)
                var cacheKey = $"{fullTypeName}_{args?.Length ?? 0}";
                if (_instanceCache.TryGetValue(cacheKey, out var weakInst))
                {
                    if (weakInst != null && weakInst.IsAlive && weakInst.Target != null)
                    {
                        return weakInst.Target;
                    }
                    else
                    {
                        _instanceCache.TryRemove(cacheKey, out _);
                    }
                }

                // Use (or build) a cached factory for speed
                var factory = GetOrCreateFactory(type, args);
                var instance = factory(args);
                _instanceCache[cacheKey] = new WeakReference(instance);
                return instance;
            }
            catch (MissingMethodException mmEx)
            {
                _logger?.LogWithContext($"No matching constructor for type: {fullTypeName}", mmEx);
            }
            catch (TargetInvocationException tie)
            {
                _logger?.LogWithContext($"Constructor threw for type: {fullTypeName}", tie.InnerException ?? tie);
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Failed to create instance of type: {fullTypeName}", ex);
            }
            return null;
        }
        #endregion

        #region Public API - Unified Plugin/Nugget Access
        /// <summary>
        /// Registers an already-loaded assembly with the shared context
        /// This is critical for project references and assemblies loaded in the default context
        /// </summary>
        public void RegisterExistingAssembly(Assembly assembly, string source = "Default")
        {
            if (assembly == null || assembly.IsDynamic)
                return;

            try
            {
                // Check if already registered
                lock (_sharedAssemblyList)
                {
                    if (_sharedAssemblyList.Contains(assembly))
                    {
                        _logger?.LogWithContext($"Assembly already registered: {assembly.GetName().Name}", null);
                        return;
                    }

                    // Add to shared assembly list
                    _sharedAssemblyList.Add(assembly);
                    
                    // Create assembly representation
                    var nuggetId = $"Existing_{assembly.GetName().Name}_{DateTime.UtcNow.Ticks}";
                    var assemblyRep = new assemblies_rep(assembly, nuggetId, assembly.Location ?? assembly.FullName, FolderFileTypes.Builtin);
                    _sharedAssemblyReps.Add(assemblyRep);
                    
                    // Add to nugget tracking (as a pseudo-nugget)
                    if (!_nuggetAssemblies.ContainsKey(nuggetId))
                    {
                        _nuggetAssemblies[nuggetId] = new List<Assembly> { assembly };
                        
                        // Create a nugget info for tracking
                        var nuggetInfo = new NuggetInfo
                        {
                            Id = nuggetId,
                            Name = assembly.GetName().Name,
                            Version = assembly.GetName().Version?.ToString() ?? "1.0.0",
                            LoadedAt = DateTime.UtcNow,
                            LoadedAssemblies = new List<Assembly> { assembly },
                            DiscoveredPlugins = new List<PluginInfo>(),
                            SourcePath = source,
                            IsSharedContext = true,
                            IsActive = true,
                            Metadata = new Dictionary<string, object>
                            {
                                ["RegistrationType"] = "ExistingAssembly",
                                ["Source"] = source
                            }
                        };
                        
                        _sharedNuggets[nuggetId] = nuggetInfo;
                    }
                }

                // Cache types from this assembly
                CacheAssemblyTypesAsync(new[] { assembly }).Wait();
                
                _logger?.LogWithContext($"Registered existing assembly in shared context: {assembly.GetName().Name}", null);
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Failed to register existing assembly: {assembly.GetName().Name}", ex);
            }
        }

        /// <summary>
        /// Registers multiple already-loaded assemblies with the shared context
        /// </summary>
        public void RegisterExistingAssemblies(IEnumerable<Assembly> assemblies, string source = "Default")
        {
            if (assemblies == null)
                return;

            foreach (var assembly in assemblies)
            {
                RegisterExistingAssembly(assembly, source);
            }
        }

        /// <summary>
        /// Resolves an assembly by name - used by AppDomain.AssemblyResolve integration
        /// This method is called when assembly resolution fails in the default context
        /// </summary>
        public Assembly ResolveAssembly(AssemblyName assemblyName)
        {
            if (assemblyName == null) return null;

            try
            {
                // First check all shared assemblies
                var assembly = GetSharedAssemblies()
                    .FirstOrDefault(a => AssemblyNamesMatch(a.GetName(), assemblyName));

                if (assembly != null)
                {
                    _logger?.LogWithContext($"Resolved assembly from shared context: {assemblyName.Name}", null);
                    return assembly;
                }

                // Check assemblies in all load contexts
                foreach (var context in _loadContexts.Values)
                {
                    try
                    {
                        var contextAssembly = context.Assemblies
                            .FirstOrDefault(a => AssemblyNamesMatch(a.GetName(), assemblyName));
                        if (contextAssembly != null)
                        {
                            _logger?.LogWithContext($"Resolved assembly from context {context.ContextId}: {assemblyName.Name}", null);
                            return contextAssembly;
                        }
                    }
                    catch { }
                }

                // CRITICAL: Check if assembly is already loaded in AppDomain but not yet registered
                // This handles project references and other default-context assemblies
                var existingAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => !a.IsDynamic && AssemblyNamesMatch(a.GetName(), assemblyName));
                
                if (existingAssembly != null)
                {
                    _logger?.LogWithContext($"Found assembly in AppDomain, registering: {assemblyName.Name}", null);
                    // Register it so we don't have to search again
                    RegisterExistingAssembly(existingAssembly, "AppDomain");
                    return existingAssembly;
                }

                // Not found
                _logger?.LogWithContext($"Failed to resolve assembly: {assemblyName.Name}", null);
                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Error resolving assembly: {assemblyName.Name}", ex);
                return null;
            }
        }

        /// <summary>
        /// Resolves an assembly by simple name (for backward compatibility)
        /// </summary>
        public Assembly ResolveAssemblyByName(string assemblyName)
        {
            if (string.IsNullOrWhiteSpace(assemblyName)) return null;

            try
            {
                // Parse assembly name
                var name = new AssemblyName(assemblyName);
                return ResolveAssembly(name);
            }
            catch
            {
                // Try simple name match
                return GetSharedAssemblies()
                    .FirstOrDefault(a => a.GetName().Name.Equals(assemblyName, StringComparison.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// Checks if assembly names match considering version flexibility
        /// </summary>
        private bool AssemblyNamesMatch(AssemblyName a, AssemblyName b)
        {
            if (!string.Equals(a.Name, b.Name, StringComparison.OrdinalIgnoreCase))
                return false;

            // For shared context, we allow version flexibility to maximize sharing
            // Public key token must match if specified
            if (b.GetPublicKeyToken() != null && a.GetPublicKeyToken() != null)
            {
                var aToken = a.GetPublicKeyToken();
                var bToken = b.GetPublicKeyToken();
                if (aToken.Length != bToken.Length)
                    return false;
                for (int i = 0; i < aToken.Length; i++)
                {
                    if (aToken[i] != bToken[i])
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Gets all loaded nuggets (everything is treated as a plugin)
        /// </summary>
        public IEnumerable<NuggetInfo> GetLoadedNuggets()
        {
            return _sharedNuggets.Values.ToList();
        }

        /// <summary>
        /// Gets all discovered plugins across all nuggets
        /// </summary>
        public IEnumerable<PluginInfo> GetAllDiscoveredPlugins()
        {
            return _sharedNuggets.Values.SelectMany(n => n.DiscoveredPlugins).ToList();
        }

        /// <summary>
        /// Gets a specific nugget by ID
        /// </summary>
        public NuggetInfo GetNugget(string nuggetId)
        {
            return _sharedNuggets.GetValueOrDefault(nuggetId);
        }

        /// <summary>
        /// Checks if a nugget is loaded
        /// </summary>
        public bool IsNuggetLoaded(string nuggetId)
        {
            return _sharedNuggets.ContainsKey(nuggetId) && _sharedNuggets[nuggetId].IsActive;
        }

        /// <summary>
        /// Gets all loaded assemblies in shared context - maximum visibility
        /// </summary>
        public List<Assembly> GetSharedAssemblies()
        {
            lock (_sharedAssemblyList)
            {
                return new List<Assembly>(_sharedAssemblyList);
            }
        }

        /// <summary>
        /// Gets all assembly representations
        /// </summary>
        public List<assemblies_rep> GetSharedAssemblyReps()
        {
            lock (_sharedAssemblyReps)
            {
                return new List<assemblies_rep>(_sharedAssemblyReps);
            }
        }

        /// <summary>
        /// Gets all cached types - shared across all contexts
        /// </summary>
        public Dictionary<string, Type> GetCachedTypes()
        {
            // Materialize only alive types
            var dict = new Dictionary<string, Type>();
            foreach (var kvp in _sharedTypeCache)
            {
                if (kvp.Value != null && kvp.Value.TryGetTarget(out var t) && t != null)
                {
                    dict[kvp.Key] = t;
                }
            }
            return dict;
        }

        /// <summary>
        /// Checks if type exists in shared context
        /// </summary>
        public bool TypeExists(string fullTypeName)
        {
            if (string.IsNullOrWhiteSpace(fullTypeName)) return false;
            return GetType(fullTypeName) != null;
        }

        /// <summary>
        /// Gets nugget IDs
        /// </summary>
        public IEnumerable<string> GetLoadedNuggetIds()
        {
            return _sharedNuggets.Keys.ToList();
        }
        #endregion

        #region Discovery and Bulk Operations
        /// <summary>
        /// Discovers and loads nuggets from a directory
        /// </summary>
        public async Task<List<NuggetInfo>> DiscoverAndLoadNuggetsAsync(string directoryPath)
        {
            var nuggets = new List<NuggetInfo>();
            
            if (!Directory.Exists(directoryPath))
                return nuggets;

            var dllFiles = Directory.GetFiles(directoryPath, "*.dll", SearchOption.AllDirectories);
            var nuggetDirs = Directory.GetDirectories(directoryPath, "*", SearchOption.TopDirectoryOnly);
            
            // Load individual DLL files as nuggets
            foreach (var dllFile in dllFiles)
            {
                try
                {
                    var nuggetInfo = await LoadNuggetAsync(dllFile);
                    if (nuggetInfo != null)
                    {
                        nuggets.Add(nuggetInfo);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWithContext($"Failed to load nugget: {dllFile}", ex);
                }
            }
            
            // Load directories as nuggets
            foreach (var nuggetDir in nuggetDirs)
            {
                try
                {
                    var nuggetInfo = await LoadNuggetAsync(nuggetDir);
                    if (nuggetInfo != null)
                    {
                        nuggets.Add(nuggetInfo);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWithContext($"Failed to load nugget directory: {nuggetDir}", ex);
                }
            }
            
            return nuggets;
        }
        #endregion

        #region Assistant Integration
        /// <summary>
        /// Enhanced shared context manager with better integration with assistant classes
        /// </summary>
    // Legacy integration stub retained for backward compatibility; no-op after consolidation.
    public void IntegrateWithAssistants() { }
        #endregion

        #region Statistics and Monitoring
        /// <summary>
        /// Gets unified statistics including discovered items
        /// </summary>
        public Dictionary<string, object> GetIntegratedStatistics()
        {
            return new Dictionary<string, object>
            {
                ["LoadedNuggets"] = _sharedNuggets.Count,
                ["ActiveNuggets"] = _sharedNuggets.Values.Count(n => n.IsActive),
                ["SharedAssemblies"] = _sharedAssemblyList.Count,
                ["CachedTypes"] = GetCachedTypes().Count,
                ["LoadContexts"] = _useSingleSharedContext ? (_globalSharedContext == null ? 0 : 1) : _loadContexts.Count,
                ["Mode"] = _useSingleSharedContext ? "SingleShared" : "PerNugget",
                ["ModeSwitches"] = _modeSwitchCount,
                ["FactoryCacheEntries"] = _factoryCache.Count,
                ["DiscoveredPlugins"] = _sharedNuggets.Values.SelectMany(n => n.DiscoveredPlugins).Count(),
                ["CachedInstances"] = _instanceCache.Count,
                ["AssemblyRepresentations"] = _sharedAssemblyReps.Count,
                ["DiscoveredDrivers"] = _discoveredDrivers.Count,
                ["DiscoveredDataSources"] = _discoveredDataSources.Count,
                ["DiscoveredAddins"] = _discoveredAddins.Count,
                ["DiscoveredWorkflowActions"] = _discoveredWorkflowActions.Count,
                ["DiscoveredViewModels"] = _discoveredViewModels.Count,
                ["DiscoveredLoaderExtensions"] = _discoveredLoaderExtensions.Count,
                ["NuggetsByStatus"] = _sharedNuggets.GroupBy(n => n.Value.IsActive).ToDictionary(g => g.Key ? "Active" : "Inactive", g => g.Count())
            };
        }
            /// <summary>
            /// Attempts to create an instance of a type ensuring its defining assembly is loaded (on-demand) using a dll path or name.
            /// </summary>
            /// <param name="dllOrPath">File path or simple dll name (with or without .dll) or nugget id.</param>
            /// <param name="fullTypeName">Full type name to instantiate.</param>
            /// <param name="args">Constructor arguments.</param>
            public object CreateInstanceFromAssembly(string dllOrPath, string fullTypeName, params object[] args)
            {
                if (string.IsNullOrWhiteSpace(fullTypeName)) return null;

                // First try existing cache
                var inst = CreateInstance(fullTypeName, args);
                if (inst != null) return inst;

                // Resolve assembly by name/path if provided
                if (!string.IsNullOrWhiteSpace(dllOrPath))
                {
                    try
                    {
                        string dllNameOnly = Path.GetFileName(dllOrPath);
                        string dllBase = Path.GetFileNameWithoutExtension(dllOrPath);

                        // Search already loaded shared assemblies
                        var asm = GetSharedAssemblies().FirstOrDefault(a =>
                            a.ManifestModule.Name.Equals(dllNameOnly, StringComparison.OrdinalIgnoreCase) ||
                            a.GetName().Name.Equals(dllBase, StringComparison.OrdinalIgnoreCase));

                        // Load on-demand if path exists and not yet loaded
                        if (asm == null && (File.Exists(dllOrPath) || Directory.Exists(dllOrPath)))
                        {
                            var nugget = LoadNuggetAsync(dllOrPath).GetAwaiter().GetResult();
                            asm = nugget?.LoadedAssemblies.FirstOrDefault(a => a.GetName().Name.Equals(dllBase, StringComparison.OrdinalIgnoreCase) || a.ManifestModule.Name.Equals(dllNameOnly, StringComparison.OrdinalIgnoreCase));
                        }

                        // Attempt type resolution again now that assembly may be loaded
                        if (asm != null && GetType(fullTypeName) == null)
                        {
                            try
                            {
                                var t = asm.GetType(fullTypeName, throwOnError: false, ignoreCase: false) ??
                                        asm.GetTypes().FirstOrDefault(tt => string.Equals(tt.FullName, fullTypeName, StringComparison.Ordinal));
                                if (t != null)
                                {
                                    _sharedTypeCache[fullTypeName] = new WeakReference<Type>(t);
                                }
                            }
                            catch { }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWithContext($"CreateInstanceFromAssembly load attempt failed for {dllOrPath}", ex);
                    }
                }

                // Final attempt
                return CreateInstance(fullTypeName, args);
            }

        #endregion

        #region Mode Switching and Reload
        /// <summary>Gets whether manager is operating in single shared context mode.</summary>
        public bool IsSingleSharedContextMode => _useSingleSharedContext;

        /// <summary>Switches between single shared context and per-nugget isolation. Forces full reload of all currently loaded nuggets.</summary>
        public async Task<bool> SetSharedContextModeAsync(bool useSingleSharedContext)
        {
            if (_useSingleSharedContext == useSingleSharedContext)
            {
                return true; // no change
            }
            var sources = _sharedNuggets.Values.Select(n => new { n.SourcePath, n.Id }).ToList();
            // Unload everything first
            foreach (var nug in sources)
            {
                UnloadNugget(nug.Id);
            }
            _globalSharedContext = null; // discard global context if existed
            _useSingleSharedContext = useSingleSharedContext;
            System.Threading.Interlocked.Increment(ref _modeSwitchCount);
            // Clear caches fully (types may hold old references)
            _sharedTypeCache.Clear();
            _typeOriginMap.Clear();
            _instanceCache.Clear();
            lock (_sharedAssemblyList) { _sharedAssemblyList.Clear(); }
            lock (_sharedAssemblyReps) { _sharedAssemblyReps.Clear(); }
            _loadContexts.Clear();
            _nuggetAssemblies.Clear();
            _sharedNuggets.Clear();
            // Reload all sources (ignore failures individually)
            foreach (var src in sources)
            {
                try { await LoadNuggetAsync(src.SourcePath); } catch (Exception ex) { _logger?.LogWithContext($"Reload failed for {src.SourcePath}", ex); }
            }
            PersistModePreference();
            return true;
        }

        /// <summary>Reloads all nuggets in current mode (useful after external file changes).</summary>
        public async Task ReloadAllNuggetsAsync()
        {
            await SetSharedContextModeAsync(_useSingleSharedContext); // reapply same mode triggers reload logic
        }

        /// <summary>Returns nugget Id (if any) that originally contributed the specified type.</summary>
        public string GetTypeOrigin(string fullTypeName)
        {
            if (string.IsNullOrWhiteSpace(fullTypeName)) return null;
            return _typeOriginMap.TryGetValue(fullTypeName, out var id) ? id : null;
        }

        /// <summary>Returns all type full names contributed by a given nugget Id.</summary>
        public List<string> GetTypesForNugget(string nuggetId)
        {
            return _typeOriginMap.Where(kv => kv.Value == nuggetId).Select(kv => kv.Key).ToList();
        }

        /// <summary>Persist current mode preference to disk (simple text file).</summary>
        public void PersistModePreference()
        {
            try
            {
                File.WriteAllText(_modePreferenceFile, _useSingleSharedContext ? "SingleShared" : "PerNugget");
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext("Failed to persist mode preference", ex);
            }
        }

        /// <summary>Explicitly reload mode preference from disk; does not auto-switch mode.</summary>
        public string LoadModePreference()
        {
            try
            {
                if (File.Exists(_modePreferenceFile))
                {
                    return File.ReadAllText(_modePreferenceFile).Trim();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext("Failed to read mode preference", ex);
            }
            return null;
        }

        /// <summary>Removes types (and factories) contributed by a nugget without unloading its context (isolation mode only).</summary>
        public int SelectiveCleanupNuggetTypes(string nuggetId)
        {
            if (string.IsNullOrWhiteSpace(nuggetId) || _useSingleSharedContext) return 0;
            var types = GetTypesForNugget(nuggetId);
            int removed = 0;
            foreach (var t in types)
            {
                if (_sharedTypeCache.TryRemove(t, out _)) removed++;
                _factoryCache.TryRemove(t + "|0", out _); // parameterless path
                _typeOriginMap.TryRemove(t, out _);
            }
            return removed;
        }

        /// <summary>Prewarms factory delegates for all cached types (parameterless only by default).</summary>
        public int PrewarmFactories(Func<Type,bool> filter = null, bool includeNonParameterless = false)
        {
            int count = 0;
            foreach (var kv in GetCachedTypes())
            {
                var type = kv.Value;
                if (filter != null && !filter(type)) continue;
                var ctor = includeNonParameterless ? SelectConstructor(type, Array.Empty<object>()) : type.GetConstructor(Type.EmptyTypes);
                if (ctor == null && !includeNonParameterless) continue;
                var factory = GetOrCreateFactory(type, Array.Empty<object>());
                if (factory != null) count++;
            }
            return count;
        }

        /// <summary>Prewarms factory delegates for all types originating from the specified nugget.</summary>
        public int PrewarmFactoriesForNugget(string nuggetId)
        {
            if (string.IsNullOrWhiteSpace(nuggetId)) return 0;
            var list = GetTypesForNugget(nuggetId);
            int count = 0;
            foreach (var fullName in list)
            {
                var t = GetType(fullName);
                if (t == null) continue;
                var factory = GetOrCreateFactory(t, Array.Empty<object>());
                if (factory != null) count++;
            }
            return count;
        }
        #endregion

        #region Factory Creation
        private Func<object[], object> GetOrCreateFactory(Type type, object[] args)
        {
            var key = type.FullName + "|" + (args?.Length ?? 0);
            if (_factoryCache.TryGetValue(key, out var existing)) return existing;
            // Build factory
            var ctor = SelectConstructor(type, args);
            if (ctor == null)
            {
                // Fallback simple Activator path
                Func<object[], object> fallback = a => Activator.CreateInstance(type, a);
                _factoryCache[key] = fallback;
                return fallback;
            }
            var parameters = ctor.GetParameters();
            var paramExpr = System.Linq.Expressions.Expression.Parameter(typeof(object[]), "args");
            var argExprs = new System.Linq.Expressions.Expression[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                var index = System.Linq.Expressions.Expression.Constant(i);
                var access = System.Linq.Expressions.Expression.ArrayIndex(paramExpr, index);
                var convert = System.Linq.Expressions.Expression.Convert(access, parameters[i].ParameterType);
                argExprs[i] = convert;
            }
            var newExpr = System.Linq.Expressions.Expression.New(ctor, argExprs);
            var body = System.Linq.Expressions.Expression.Convert(newExpr, typeof(object));
            var lambda = System.Linq.Expressions.Expression.Lambda<Func<object[], object>>(body, paramExpr).Compile();
            _factoryCache[key] = lambda;
            return lambda;
        }

        private ConstructorInfo SelectConstructor(Type type, object[] args)
        {
            var allCtors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            if (allCtors.Length == 0) return null;
            int argCount = args?.Length ?? 0;
            // Exact arg count match preferred
            var candidates = allCtors.Where(c => c.GetParameters().Length == argCount).ToList();
            if (!candidates.Any())
            {
                // fallback to parameterless if possible
                return allCtors.FirstOrDefault(c => c.GetParameters().Length == 0);
            }
            if (argCount == 0) return candidates.First();
            // Simple assignability check
            foreach (var ctor in candidates)
            {
                var ps = ctor.GetParameters();
                bool match = true;
                for (int i = 0; i < ps.Length; i++)
                {
                    var provided = args[i];
                    if (provided == null) continue; // allow null
                    if (!ps[i].ParameterType.IsAssignableFrom(provided.GetType())) { match = false; break; }
                }
                if (match) return ctor;
            }
            return candidates.First();
        }
        #endregion

        #region Helper Methods
        private string GetAssemblyVersion(Assembly assembly)
        {
            try
            {
                return assembly?.GetName()?.Version?.ToString() ?? "1.0.0";
            }
            catch
            {
                return "1.0.0";
            }
        }

        /// <summary>
        /// Checks if an assembly path is compatible with the current runtime framework.
        /// For multi-targeted assemblies (net8.0, net9.0), only the matching framework should be loaded.
        /// </summary>
        private bool IsCompatibleWithCurrentRuntime(string assemblyPath)
        {
            try
            {
                if (string.IsNullOrEmpty(assemblyPath))
                    return false;

                // Get the directory path which may contain framework identifier
                var directoryPath = Path.GetDirectoryName(assemblyPath);
                if (string.IsNullOrEmpty(directoryPath))
                    return true; // If no directory info, assume compatible

                // Detect current runtime framework version
                var currentFramework = Environment.Version.Major; // .NET 8 = 8, .NET 9 = 9, etc.
                
                // Check if path contains framework-specific folder (net8.0, net9.0, etc.)
                var pathLower = directoryPath.ToLowerInvariant();
                
                // If path contains a framework identifier, check compatibility
                if (pathLower.Contains("net8.0") || pathLower.Contains("net8"))
                {
                    return currentFramework == 8;
                }
                else if (pathLower.Contains("net9.0") || pathLower.Contains("net9"))
                {
                    return currentFramework == 9;
                }
                else if (pathLower.Contains("net6.0") || pathLower.Contains("net6"))
                {
                    return currentFramework == 6;
                }
                else if (pathLower.Contains("net7.0") || pathLower.Contains("net7"))
                {
                    return currentFramework == 7;
                }
                
                // If no specific framework in path, try to load assembly metadata to check
                try
                {
                    var assemblyName = AssemblyName.GetAssemblyName(assemblyPath);
                    // If we can get the name, it's likely compatible
                    return true;
                }
                catch (BadImageFormatException)
                {
                    // Wrong architecture or framework
                    _logger?.LogWithContext($"Assembly incompatible with current runtime: {assemblyPath}", null);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Error checking runtime compatibility for: {assemblyPath}", ex);
                // When in doubt, attempt to load (fail gracefully in LoadAssemblyInContextAsync)
                return true;
            }
        }

        /// <summary>
        /// Gets the current target framework identifier (e.g., "net8.0", "net9.0")
        /// </summary>
        private string GetCurrentTargetFramework()
        {
            try
            {
                // Get the framework from the entry assembly's target framework attribute
                var entryAssembly = Assembly.GetEntryAssembly();
                if (entryAssembly != null)
                {
                    var targetFrameworkAttribute = entryAssembly.GetCustomAttribute<System.Runtime.Versioning.TargetFrameworkAttribute>();
                    if (targetFrameworkAttribute != null)
                    {
                        // Parse ".NETCoreApp,Version=v8.0" to "net8.0"
                        var frameworkName = targetFrameworkAttribute.FrameworkName;
                        if (frameworkName.Contains("Version=v"))
                        {
                            var versionPart = frameworkName.Substring(frameworkName.IndexOf("Version=v") + 9);
                            var majorMinor = versionPart.Split('.').Take(2);
                            return $"net{string.Join(".", majorMinor)}";
                        }
                    }
                }

                // Fallback: detect from Environment.Version
                var major = Environment.Version.Major;
                return $"net{major}.0";
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext("Error detecting target framework, defaulting to net8.0", ex);
                return "net8.0"; // Safe default
            }
        }

    private Task RegisterAsPluginsAsync(NuggetInfo nuggetInfo)
        {
            try
            {
                var discoveredPlugins = new List<PluginInfo>();

                foreach (var assembly in nuggetInfo.LoadedAssemblies)
                {
                    try
                    {
                        // Scan for plugin interfaces (any interface that could be considered a plugin)
                        var pluginTypes = assembly.GetTypes()
                            .Where(t => !t.IsInterface && !t.IsAbstract && HasPluginCharacteristics(t))
                            .ToList();

                        foreach (var pluginType in pluginTypes)
                        {
                            var pluginInfo = new PluginInfo
                            {
                                Id = $"{nuggetInfo.Id}_{pluginType.Name}",
                                Name = pluginType.Name,
                                Version = assembly.GetName().Version?.ToString() ?? "1.0.0",
                                Description = GetTypeDescription(pluginType),
                                Author = GetAssemblyAuthor(assembly),
                                Assembly = assembly,
                                State = PluginState.Loaded,
                                Health = PluginHealth.Healthy,
                                LoadedAt = DateTime.UtcNow,
                                Dependencies = new List<string>(),
                                Metadata = new Dictionary<string, object>
                                {
                                    ["SourceNugget"] = nuggetInfo.Id,
                                    ["TypeName"] = pluginType.FullName,
                                    ["IsSharedContext"] = true
                                },
                                IsSharedContext = true,
                                PluginType = DeterminePluginType(pluginType)
                            };

                            discoveredPlugins.Add(pluginInfo);
                            
                            // Register with lifecycle manager
                            _lifecycleManager.RegisterPlugin(pluginInfo);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWithContext($"Failed to scan assembly for plugins: {assembly.FullName}", ex);
                    }
                }

                nuggetInfo.DiscoveredPlugins = discoveredPlugins;
                
                _logger?.LogWithContext($"Registered {discoveredPlugins.Count} plugins from nugget: {nuggetInfo.Id}", null);
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Failed to register nugget as plugins: {nuggetInfo.Id}", ex);
            }
            return Task.CompletedTask;
        }

        private bool HasPluginCharacteristics(Type type)
        {
            // Check for common plugin interfaces and characteristics
            var interfaces = type.GetInterfaces();
            
            return interfaces.Any(i => 
                i.Name.Contains("Plugin") ||
                i.Name.Contains("Addin") ||
                i.Name.Contains("Extension") ||
                i.Name.Contains("DataSource") ||
                i.Name.Contains("Driver") ||
                i.Name.Contains("Service") ||
                i.Name.Contains("Component")) ||
                type.GetCustomAttributes().Any(a => 
                    a.GetType().Name.Contains("Plugin") ||
                    a.GetType().Name.Contains("Addin") ||
                    a.GetType().Name.Contains("Component"));
        }

        private UnifiedPluginType DeterminePluginType(Type type)
        {
            var interfaces = type.GetInterfaces();
            
            if (interfaces.Any(i => i.Name.Contains("DataSource")))
                return UnifiedPluginType.DataSourceDriver;
            if (interfaces.Any(i => i.Name.Contains("Driver")))
                return UnifiedPluginType.ConnectionDriver;
            if (interfaces.Any(i => i.Name.Contains("Addin")))
                return UnifiedPluginType.UIComponent;
            if (interfaces.Any(i => i.Name.Contains("Extension")))
                return UnifiedPluginType.ExtensionComponent;
            if (interfaces.Any(i => i.Name.Contains("Service")))
                return UnifiedPluginType.BuiltinComponent;
                
            return UnifiedPluginType.SharedAssembly;
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

        private string GetTypeDescription(Type type)
        {
            try
            {
                // Try to get description from attributes or use the type name
                return type.Name;
            }
            catch
            {
                return "Unknown";
            }
        }
        #endregion
        
        #region Dispose
    /// <summary>Disposes the manager, unloading all nuggets and clearing caches.</summary>
    public void Dispose()
        {
            if (!_disposed)
            {
                // Unload all nuggets
                foreach (var nuggetId in _sharedNuggets.Keys.ToList())
                {
                    UnloadNugget(nuggetId);
                }

                // Dispose plugin managers
                _isolationManager?.Dispose();
                _lifecycleManager?.Dispose();
                _versionManager?.Dispose();
                _messageBus?.Dispose();
                _serviceManager?.Dispose();
                _healthMonitor?.Dispose();

                // Clear all collections
                _sharedNuggets.Clear();
                _loadContexts.Clear();
                _nuggetAssemblies.Clear();
                _sharedTypeCache.Clear();
                _instanceCache.Clear();
                
                // Clear discovered items
                lock (_discoveredDrivers) { _discoveredDrivers.Clear(); }
                lock (_discoveredDataSources) { _discoveredDataSources.Clear(); }
                lock (_discoveredAddins) { _discoveredAddins.Clear(); }
                lock (_discoveredWorkflowActions) { _discoveredWorkflowActions.Clear(); }
                lock (_discoveredViewModels) { _discoveredViewModels.Clear(); }
                lock (_discoveredLoaderExtensions) { _discoveredLoaderExtensions.Clear(); }
                
                lock (_sharedAssemblyList)
                {
                    _sharedAssemblyList.Clear();
                }
                
                lock (_sharedAssemblyReps)
                {
                    _sharedAssemblyReps.Clear();
                }

                _disposed = true;
                
                _logger?.LogWithContext("SharedContextManager disposed with full cleanup", null);
            }
        }
        #endregion
    }
}