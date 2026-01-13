using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Tools.PluginSystem;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Beep.Vis;

#pragma warning disable 1591 // Suppress XML comment warnings for public members
using TypeInfo = System.Reflection.TypeInfo;

namespace TheTechIdea.Beep.Tools
{
    /// <summary>
    /// Modern AssemblyHandler implementation using SharedContextManager as the core loading system
    /// This provides unified loading/unloading for DLLs, plugins, nuggets, and assemblies
    /// </summary>
    public class SharedContextAssemblyHandler : IAssemblyHandler
    {
        #region Private Fields
    private readonly SharedContextManager _sharedContextManager;
    // Driver assistant retained for specialized driver configuration extraction.
    private readonly DriverDiscoveryAssistant _driverAssistant;
    // New scanning abstraction
    private readonly IScanningService _scanningService;
    // NuggetManager for nugget package handling
    private readonly NuggetManager _nuggetManager;
        private readonly PluginRegistry _pluginRegistry;
        private readonly PluginInstaller _pluginInstaller;
        
        private readonly List<assemblies_rep> _assemblies = new();
        private readonly List<Assembly> _loadedAssemblies = new();
        
        private bool _disposed = false;
        #endregion

        #region Properties - IAssemblyHandler Implementation
        /// <summary>
        /// Instantiated loader extension objects that have already been created and executed
        /// </summary>
        public List<ILoaderExtention> LoaderExtensionInstances { get; set; } = new List<ILoaderExtention>();

        public List<Type> LoaderExtensions { get; set; } = new List<Type>();
        /// <summary>
        /// List of classes that extend the loader functionality.
        /// </summary>
        public List<AssemblyClassDefinition> LoaderExtensionClasses { get; set; } = new List<AssemblyClassDefinition>();


        /// <summary>Namespaces to ignore during scanning.</summary>
        public List<string> NamespacestoIgnore { get; set; } = new();
    /// <summary>
    /// Collection of loaded assembly representations. Wrapped to avoid external replacement of internal list.
    /// Setting replaces contents while preserving backing list reference.
    /// </summary>
    public List<assemblies_rep> Assemblies
    {
        get => _assemblies;
        set
        {
            _assemblies.Clear();
            if (value != null) _assemblies.AddRange(value);
        }
    }
    /// <summary>
    /// List of loaded assemblies (raw Assembly objects). Wrapper for internal list.
    /// </summary>
    public List<Assembly> LoadedAssemblies
    {
        get => _loadedAssemblies;
        set
        {
            _loadedAssemblies.Clear();
            if (value != null) _loadedAssemblies.AddRange(value);
        }
    }
    /// <summary>
    /// Discovered data source class definitions. Delegated directly to ConfigEditor storage to avoid duplication.
    /// </summary>
    public List<AssemblyClassDefinition> DataSourcesClasses
    {
        get => ConfigEditor?.DataSourcesClasses ?? new List<AssemblyClassDefinition>();
        set
        {
            if (ConfigEditor?.DataSourcesClasses == null) return;
            ConfigEditor.DataSourcesClasses.Clear();
            if (value != null) ConfigEditor.DataSourcesClasses.AddRange(value);
        }
    }
    /// <summary>Configuration editor reference.</summary>
    public IConfigEditor ConfigEditor { get; set; }
    /// <summary>Error object for reporting.</summary>
    public IErrorsInfo ErrorObject { get; set; }
    /// <summary>Logger instance.</summary>
    public IDMLogger Logger { get; set; }
    /// <summary>Utility functions provider.</summary>
    public IUtil Utilfunction { get; set; }
    /// <summary>Plugin registry storing installed plugin metadata.</summary>
    public PluginRegistry PluginRegistry => _pluginRegistry;
    /// <summary>Plugin installer/uninstaller helper.</summary>
    public PluginInstaller PluginInstaller => _pluginInstaller;

    // Plugin system removed – handler now limited to interface surface only
        #endregion

        #region Constructor
        public SharedContextAssemblyHandler(IConfigEditor configEditor, IErrorsInfo errorObject, IDMLogger logger, IUtil utilFunction)
        {
            ConfigEditor = configEditor ?? throw new ArgumentNullException(nameof(configEditor));
            ErrorObject = errorObject ?? throw new ArgumentNullException(nameof(errorObject));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Utilfunction = utilFunction ?? throw new ArgumentNullException(nameof(utilFunction));

            // Initialize SharedContextManager first - this is the core of the plugin system
            // It handles all assembly loading with proper isolation and reference resolution
            var registry = new PluginRegistry(ConfigEditor?.ExePath ?? AppContext.BaseDirectory, Logger);
            _pluginRegistry = registry;
            _pluginInstaller = new PluginInstaller(_pluginRegistry, Logger);
            _sharedContextManager = new SharedContextManager(Logger, useSingleSharedContext: true, registry);

            // Initialize NuggetManager for nugget package handling
            _nuggetManager = new NuggetManager(Logger, ErrorObject, Utilfunction);

            // Initialize retained assistant and scanning service
            _driverAssistant = new DriverDiscoveryAssistant(_sharedContextManager, ConfigEditor, Logger);
            _scanningService = new ScanningService(_sharedContextManager, ConfigEditor, Logger);

            // Setup assembly resolver to integrate SharedContextManager with AppDomain resolution
            // This ensures that when any code requests an assembly, we check the shared context first
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            Logger?.LogWithContext("SharedContextAssemblyHandler initialized with integrated plugin system and NuggetManager", null);
        }
        #endregion

        #region Core Loading Methods

        public Assembly LoadAssembly(string path)
        {
            ErrorObject.Flag = Errors.Ok;
            string res = "";
            Assembly loadedAssembly = null;
            try
            {
                LoadAssembly( path, FolderFileTypes.SharedAssembly);

            }
            catch (FileLoadException loadEx)
            {
                ErrorObject.Flag = Errors.Failed;
                res = "The Assembly has already been loaded" + loadEx.Message;
            } // The Assembly has already been loaded.
            catch (BadImageFormatException imgEx)
            {
                ErrorObject.Flag = Errors.Failed;
                res = imgEx.Message;
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                res = ex.Message;
            }
            ErrorObject.Message = res;
            return loadedAssembly;
        }
        /// <summary>
        /// Loads assemblies from a path using SharedContextManager
        /// </summary>
        public string LoadAssembly(string path, FolderFileTypes fileTypes)
        {
            ErrorObject.Flag = Errors.Ok;
            string result = "";

            try
            {
                // CRITICAL: For multi-targeted projects, resolve to framework-specific subdirectory
                string resolvedPath = ResolveFrameworkSpecificPath(path);
                
                var nuggetInfo = _sharedContextManager.LoadNuggetAsync(resolvedPath, $"Nugget_{fileTypes}_{DateTime.UtcNow.Ticks}").GetAwaiter().GetResult();
                
                if (nuggetInfo != null)
                {
                    // Add assemblies to our collections
                    foreach (var assembly in nuggetInfo.LoadedAssemblies)
                    {
                        if (!_loadedAssemblies.Contains(assembly))
                        {
                            _loadedAssemblies.Add(assembly);
                        }

                        var assemblyRep = new assemblies_rep(assembly, path, assembly.Location, fileTypes);
                        if (!_assemblies.Any(a => a.DllLib == assembly))
                        {
                            _assemblies.Add(assemblyRep);
                        }
                    }

                    result = $"Successfully loaded from {resolvedPath}: {nuggetInfo.Id} with {nuggetInfo.LoadedAssemblies.Count} assemblies";
                    Logger?.LogWithContext(result, nuggetInfo);
                }
                else
                {
                    ErrorObject.Flag = Errors.Failed;
                    result = $"Failed to load assemblies from path: {resolvedPath}";
                    Logger?.LogWithContext(result, null);
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                result = $"Error loading assemblies: {ex.Message}";
                Logger?.LogWithContext(result, ex);
            }

            ErrorObject.Message = result;
            return result;
        }

        /// <summary>
        /// Loads all assemblies from configured folders
        /// </summary>
        public IErrorsInfo LoadAllAssembly(IProgress<PassedArgs> progress, CancellationToken token)
        {
            ErrorObject.Flag = Errors.Ok;

            try
            {
                // Log configuration state for diagnostics
                if (ConfigEditor?.Config != null)
                {
                    var foldersCount = ConfigEditor.Config.Folders?.Count ?? 0;
                    var connectionDriversPath = ConfigEditor.Config.ConnectionDriversPath ?? "null";
                    var dataSourcesPath = ConfigEditor.Config.DataSourcesPath ?? "null";
                    Logger?.LogWithContext($"LoadAllAssembly: Config.Folders count: {foldersCount}, ConnectionDriversPath: {connectionDriversPath}, DataSourcesPath: {dataSourcesPath}", null);
                }
                else
                {
                    Logger?.LogWithContext("LoadAllAssembly: ConfigEditor or Config is null", null);
                }

                SendMessage(progress, token, "Getting Builtin Classes");
                GetBuiltinClasses();

                SendMessage(progress, token, "Loading Runtime Assemblies");
                LoadAssemblyFromRuntime();

                SendMessage(progress, token, "Getting Framework Extensions");
                LoadFrameworkExtensions(progress, token);

                SendMessage(progress, token, "Getting Driver Classes");
                LoadDriverClasses(progress, token);

                SendMessage(progress, token, "Getting Data Source Classes");
                LoadDataSourceClasses(progress, token);

                if (ConfigEditor.ConfigType != BeepConfigType.DataConnector)
                {
                    SendMessage(progress, token, "Getting Project and Addin Classes");
                    LoadProjectAndAddinClasses(progress, token);

                    SendMessage(progress, token, "Getting Other DLL Classes");
                    LoadOtherDLLClasses(progress, token);

                    SendMessage(progress, token, "Loading Addins");
                    LoadAddinClasses(progress, token);
                }

                SendMessage(progress, token, "Scanning for Drivers");
                ScanForDrivers();

                SendMessage(progress, token, "Scanning for Data Sources");
                ScanForDataSources();

                if (ConfigEditor.ConfigType != BeepConfigType.DataConnector)
                {
                    SendMessage(progress, token, "Scanning for Addins");
                    ScanForAddins();
                }

                SendMessage(progress, token, "Adding Default Engine Drivers");
                AddEngineDefaultDrivers();

                SendMessage(progress, token, "Organizing Drivers");
                _driverAssistant.CheckDriverAlreadyExistInList();

                SendMessage(progress, token, "Processing Extensions");
                ProcessExtensions();

                if (ConfigEditor.ConfigType != BeepConfigType.DataConnector)
                {
                    SendMessage(progress, token, "Building Addin Hierarchy");
                    Utilfunction.FunctionHierarchy = BuildAddinHierarchy();
                }

                Logger?.LogWithContext("Assembly loading completed successfully", GetLoadingStatistics());
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                Logger?.LogWithContext("Assembly loading failed", ex);
            }

            return ErrorObject;
        }
        #endregion

        #region Helper Loading Methods
        private void LoadFrameworkExtensions(IProgress<PassedArgs> progress, CancellationToken token)
        {
            try
            {
                if (ConfigEditor?.ExePath == null)
                {
                    Logger?.LogWithContext("ConfigEditor.ExePath is null, skipping framework extensions loading", null);
                    return;
                }

                var extensionPath = Path.Combine(ConfigEditor.ExePath, "LoadingExtensions");
                if (Directory.Exists(extensionPath))
                {
                    LoadAssembly(extensionPath, FolderFileTypes.LoaderExtensions);
                }
            }
            catch (Exception ex)
            {
                Logger?.LogWithContext("Failed to load framework extensions", ex);
            }
        }

        private void LoadDriverClasses(IProgress<PassedArgs> progress, CancellationToken token)
        {
            var driverFolders = new List<string>();

            // First, try to get folders from Config.Folders
            if (ConfigEditor?.Config?.Folders != null && ConfigEditor.Config.Folders.Count > 0)
            {
                driverFolders = ConfigEditor.Config.Folders
                    .Where(c => c != null && !string.IsNullOrEmpty(c.FolderPath) && c.FolderFilesType == FolderFileTypes.ConnectionDriver)
                    .Select(x => x.FolderPath)
                    .ToList();
            }

            // Fallback: Use Config.ConnectionDriversPath if Folders is null/empty or no driver folders found
            if (driverFolders.Count == 0 && !string.IsNullOrEmpty(ConfigEditor?.Config?.ConnectionDriversPath))
            {
                Logger?.LogWithContext($"Config.Folders is null/empty or has no ConnectionDriver folders. Using fallback path: {ConfigEditor.Config.ConnectionDriversPath}", null);
                driverFolders.Add(ConfigEditor.Config.ConnectionDriversPath);
            }

            // If still no paths, try default path
            if (driverFolders.Count == 0 && ConfigEditor?.ExePath != null)
            {
                var defaultPath = Path.Combine(ConfigEditor.ExePath, "ConnectionDrivers");
                if (Directory.Exists(defaultPath))
                {
                    Logger?.LogWithContext($"Using default ConnectionDrivers path: {defaultPath}", null);
                    driverFolders.Add(defaultPath);
                }
            }

            if (driverFolders.Count == 0)
            {
                Logger?.LogWithContext("No driver folder paths found. Skipping driver classes loading.", null);
                return;
            }

            foreach (var path in driverFolders)
            {
                try
                {
                    // Load from main folder
                    LoadAssembly(path, FolderFileTypes.ConnectionDriver);
                    
                    // Also scan subfolders (for NuGet packages downloaded to subfolders like ConnectionDrivers/SqlServer/)
                    if (Directory.Exists(path))
                    {
                        foreach (var subDir in Directory.GetDirectories(path))
                        {
                            try
                            {
                                Logger?.LogWithContext($"Scanning driver subfolder: {Path.GetFileName(subDir)}", null);
                                LoadAssembly(subDir, FolderFileTypes.ConnectionDriver);
                            }
                            catch (Exception subEx)
                            {
                                Logger?.LogWithContext($"Failed to load driver classes from subfolder {subDir}", subEx);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger?.LogWithContext($"Failed to load driver classes from {path}", ex);
                }
            }
        }

        private void LoadDataSourceClasses(IProgress<PassedArgs> progress, CancellationToken token)
        {
            var dataSourceFolders = new List<string>();

            // First, try to get folders from Config.Folders
            if (ConfigEditor?.Config?.Folders != null && ConfigEditor.Config.Folders.Count > 0)
            {
                dataSourceFolders = ConfigEditor.Config.Folders
                    .Where(c => c != null && !string.IsNullOrEmpty(c.FolderPath) && c.FolderFilesType == FolderFileTypes.DataSources)
                    .Select(x => x.FolderPath)
                    .ToList();
            }

            // Fallback: Use Config.DataSourcesPath if Folders is null/empty or no data source folders found
            if (dataSourceFolders.Count == 0 && !string.IsNullOrEmpty(ConfigEditor?.Config?.DataSourcesPath))
            {
                Logger?.LogWithContext($"Config.Folders is null/empty or has no DataSources folders. Using fallback path: {ConfigEditor.Config.DataSourcesPath}", null);
                dataSourceFolders.Add(ConfigEditor.Config.DataSourcesPath);
            }

            // If still no paths, try default path
            if (dataSourceFolders.Count == 0 && ConfigEditor?.ExePath != null)
            {
                var defaultPath = Path.Combine(ConfigEditor.ExePath, "DataSources");
                if (Directory.Exists(defaultPath))
                {
                    Logger?.LogWithContext($"Using default DataSources path: {defaultPath}", null);
                    dataSourceFolders.Add(defaultPath);
                }
            }

            if (dataSourceFolders.Count == 0)
            {
                Logger?.LogWithContext("No data source folder paths found. Skipping data source classes loading.", null);
                return;
            }

            foreach (var path in dataSourceFolders)
            {
                try
                {
                    // Load from main folder
                    LoadAssembly(path, FolderFileTypes.DataSources);
                    
                    // Also scan subfolders (for NuGet packages downloaded to subfolders like DataSources/SqlServer/)
                    if (Directory.Exists(path))
                    {
                        foreach (var subDir in Directory.GetDirectories(path))
                        {
                            try
                            {
                                Logger?.LogWithContext($"Scanning datasource subfolder: {Path.GetFileName(subDir)}", null);
                                LoadAssembly(subDir, FolderFileTypes.DataSources);
                            }
                            catch (Exception subEx)
                            {
                                Logger?.LogWithContext($"Failed to load datasource classes from subfolder {subDir}", subEx);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger?.LogWithContext($"Failed to load data source classes from {path}", ex);
                }
            }
        }

        private void LoadProjectAndAddinClasses(IProgress<PassedArgs> progress, CancellationToken token)
        {
            if (ConfigEditor?.Config?.Folders == null)
            {
                Logger?.LogWithContext("ConfigEditor.Config.Folders is null, skipping project and addin classes loading", null);
                return;
            }

            var projectFolders = ConfigEditor.Config.Folders
                .Where(c => c != null && !string.IsNullOrEmpty(c.FolderPath) && c.FolderFilesType == FolderFileTypes.ProjectClass)
                .Select(x => x.FolderPath);

            foreach (var path in projectFolders)
            {
                try
                {
                    LoadAssembly(path, FolderFileTypes.ProjectClass);
                }
                catch (Exception ex)
                {
                    Logger?.LogWithContext($"Failed to load project classes from {path}", ex);
                }
            }
        }

        private void LoadOtherDLLClasses(IProgress<PassedArgs> progress, CancellationToken token)
        {
            if (ConfigEditor?.Config?.Folders == null)
            {
                Logger?.LogWithContext("ConfigEditor.Config.Folders is null, skipping other DLL classes loading", null);
                return;
            }

            var otherDllFolders = ConfigEditor.Config.Folders
                .Where(c => c != null && !string.IsNullOrEmpty(c.FolderPath) && c.FolderFilesType == FolderFileTypes.OtherDLL)
                .Select(x => x.FolderPath);

            foreach (var path in otherDllFolders)
            {
                try
                {
                    LoadAssembly(path, FolderFileTypes.OtherDLL);
                }
                catch (Exception ex)
                {
                    Logger?.LogWithContext($"Failed to load other DLL classes from {path}", ex);
                }
            }
        }

        private void LoadAddinClasses(IProgress<PassedArgs> progress, CancellationToken token)
        {
            if (ConfigEditor?.Config?.Folders == null)
            {
                Logger?.LogWithContext("ConfigEditor.Config.Folders is null, skipping addin classes loading", null);
                return;
            }

            var addinFolders = ConfigEditor.Config.Folders
                .Where(c => c != null && !string.IsNullOrEmpty(c.FolderPath) && c.FolderFilesType == FolderFileTypes.Addin)
                .Select(x => x.FolderPath);

            foreach (var path in addinFolders)
            {
                try
                {
                    LoadAssembly(path, FolderFileTypes.Addin);
                }
                catch (Exception ex)
                {
                    Logger?.LogWithContext($"Failed to load addin classes from {path}", ex);
                }
            }
        }

        private void LoadAssemblyFromRuntime()
        {
            try
            {
                var runtimeAssemblies = GetRuntimeAssemblies();
                
                // CRITICAL: Also get all already-loaded assemblies from AppDomain
                // This includes project references that are already in the default context
                var alreadyLoadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => !a.IsDynamic && 
                               !string.IsNullOrEmpty(a.Location) &&
                               !a.FullName.StartsWith("System.") &&
                               !a.FullName.StartsWith("Microsoft."))
                    .ToList();

                // Combine both lists
                var allAssemblies = runtimeAssemblies.Concat(alreadyLoadedAssemblies).Distinct().ToList();

                foreach (var assembly in allAssemblies)
                {
                    if (!_loadedAssemblies.Contains(assembly))
                    {
                        _loadedAssemblies.Add(assembly);
                        
                        // Determine the file type
                        var fileType = FolderFileTypes.Builtin;
                        var location = assembly.Location ?? "Runtime";
                        
                        // Check if it's a project reference (in same directory as entry assembly)
                        var entryPath = Assembly.GetEntryAssembly()?.Location;
                        if (!string.IsNullOrEmpty(entryPath) && !string.IsNullOrEmpty(assembly.Location))
                        {
                            var entryDir = Path.GetDirectoryName(entryPath);
                            var assemblyDir = Path.GetDirectoryName(assembly.Location);
                            if (string.Equals(entryDir, assemblyDir, StringComparison.OrdinalIgnoreCase))
                            {
                                fileType = FolderFileTypes.ProjectClass;
                                Logger?.LogWithContext($"Detected project reference: {assembly.GetName().Name}", null);
                            }
                        }
                        
                        var assemblyRep = new assemblies_rep(assembly, location, assembly.Location ?? assembly.FullName, fileType);
                        if (!_assemblies.Any(a => a.DllLib == assembly))
                        {
                            _assemblies.Add(assemblyRep);
                        }
                        
                        // CRITICAL: Register with SharedContextManager so it can resolve these assemblies
                        // This ensures project references are visible in the shared context
                        RegisterAssemblyWithSharedContext(assembly);
                    }
                }
                
                Logger?.LogWithContext($"Loaded {allAssemblies.Count} runtime and project reference assemblies", null);
            }
            catch (Exception ex)
            {
                Logger?.LogWithContext("Failed to load runtime assemblies", ex);
            }
        }

        /// <summary>
        /// Registers an already-loaded assembly with the SharedContextManager
        /// This is critical for project references that are loaded in the default context
        /// </summary>
        private void RegisterAssemblyWithSharedContext(Assembly assembly)
        {
            try
            {
                // Use the new public method to register existing assemblies
                _sharedContextManager.RegisterExistingAssembly(assembly, "AppDomain");
            }
            catch (Exception ex)
            {
                Logger?.LogWithContext($"Failed to register assembly with SharedContextManager: {assembly.GetName().Name}", ex);
            }
        }

        private List<Assembly> GetRuntimeAssemblies()
        {
            var assemblies = new List<Assembly>();

            var coreAssemblies = new[]
            {
                Assembly.GetExecutingAssembly(),
                Assembly.GetCallingAssembly(),
                Assembly.GetEntryAssembly()
            }.Where(a => a != null);

            assemblies.AddRange(coreAssemblies);

            try
            {
                var dependencyContext = Microsoft.Extensions.DependencyModel.DependencyContext.Default;
                if (dependencyContext != null)
                {
                    var dependencyAssemblies = dependencyContext.RuntimeLibraries
                        .SelectMany(library => library.RuntimeAssemblyGroups.SelectMany(g => g.AssetPaths))
                        .Where(path => path.EndsWith(".dll"))
                        .Select(path =>
                        {
                            try
                            {
                                return Assembly.LoadFrom(Path.Combine(AppContext.BaseDirectory, path));
                            }
                            catch
                            {
                                return null;
                            }
                        })
                        .Where(a => a != null && !a.FullName.StartsWith("System") && !a.FullName.StartsWith("Microsoft"))
                        .ToList();

                    if (dependencyAssemblies != null)
                    {
                        assemblies.AddRange(dependencyAssemblies);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger?.LogWithContext("Failed to load dependency assemblies", ex);
            }

            return assemblies.Distinct().ToList();
        }
        #endregion

        #region Scanning Methods
        private void ScanForDrivers()
        {
            var driverAssemblies = _assemblies.Where(c =>
                c.FileTypes == FolderFileTypes.ConnectionDriver ||
                c.FileTypes == FolderFileTypes.Builtin).ToList();

            foreach (var item in driverAssemblies)
            {
                try
                {
                    _driverAssistant.GetDrivers(item.DllLib);
                }
                catch (Exception ex)
                {
                    Logger?.LogWithContext($"Failed to scan drivers in {item.DllName}", ex);
                }
            }
        }

        private void ScanForDataSources()
        {
            var dataSourceAssemblies = _assemblies.Where(c => c.FileTypes == FolderFileTypes.DataSources).ToList();

            foreach (var item in dataSourceAssemblies)
            {
                try
                {
                    _scanningService.ScanAssemblyForDataSources(item.DllLib, DataSourcesClasses);
                }
                catch (Exception ex)
                {
                    Logger?.LogWithContext($"Failed to scan data sources in {item.DllName}", ex);
                }
            }
        }

        private void ScanForAddins()
        {
            var addinAssemblies = _assemblies.Where(x =>
                x.FileTypes == FolderFileTypes.ProjectClass ||
                x.FileTypes == FolderFileTypes.Addin).ToList();

            foreach (var assembly in addinAssemblies)
            {
                try
                {
                    _scanningService.ScanAssembly(assembly.DllLib, null);
                }
                catch (Exception ex)
                {
                    Logger?.LogWithContext($"Failed to scan addins in {assembly.DllName}", ex);
                }
            }
        }

        private void ProcessExtensions()
        {
            // Process loader extensions
            var extensionAssemblies = _assemblies.Where(x =>
                x.FileTypes == FolderFileTypes.LoaderExtensions).ToList();

            foreach (var assembly in extensionAssemblies)
            {
                try
                {
                    ScanExtensions(assembly.DllLib);
                }
                catch (Exception ex)
                {
                    Logger?.LogWithContext($"Failed to process extensions in {assembly.DllName}", ex);
                }
            }
        }

        private void ScanExtensions(Assembly assembly)
        {
            // Use SharedContextManager for instantiation (factory cached & consistent)
            var loaderExtensions = _sharedContextManager.DiscoveredLoaderExtensions
                .Where(le => le.type != null)
                .Select(le => le.type)
                .Where(t => typeof(ILoaderExtention).IsAssignableFrom(t))
                .ToList();

            foreach (var extensionType in loaderExtensions)
            {
                try
                {
                    var instance = _sharedContextManager.CreateInstance(extensionType.FullName, this) as ILoaderExtention;
                    instance?.Scan(assembly);
                }
                catch (Exception ex)
                {
                    Logger?.LogWithContext($"Failed to run extension scan on {assembly.FullName}", ex);
                }
            }
        }
        #endregion

        #region IAssemblyHandler Implementation
        public IErrorsInfo GetBuiltinClasses()
        {
            var builtinAssemblies = _loadedAssemblies.Where(a =>
                !a.FullName.StartsWith("System") &&
                !a.FullName.StartsWith("Microsoft"));

            foreach (var assembly in builtinAssemblies)
            {
                try
                {
                    var assemblyRep = new assemblies_rep(assembly, "", assembly.FullName, FolderFileTypes.Builtin);
                    if (!_assemblies.Any(a => a.DllLib == assembly))
                    {
                        _assemblies.Add(assemblyRep);
                    }
                    _scanningService.ScanAssembly(assembly, null);
                }
                catch (Exception ex)
                {
                    Logger?.LogWithContext($"Error processing builtin assembly {assembly.FullName}", ex);
                }
            }

            return ErrorObject;
        }

        public Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            // Ignore resource assemblies
            if (args?.Name == null || args.Name.Contains(".resources"))
                return null;

            try
            {
                // Parse the assembly name
                var assemblyName = new AssemblyName(args.Name);
                
                // CRITICAL: Use SharedContextManager's integrated resolver first
                // This ensures proper type identity across all plugins and contexts
                if (_sharedContextManager != null)
                {
                    var resolvedAssembly = _sharedContextManager.ResolveAssembly(assemblyName);
                    if (resolvedAssembly != null)
                    {
                        Logger?.LogWithContext($"Resolved assembly via SharedContextManager: {assemblyName.Name}", null);
                        return resolvedAssembly;
                    }
                }

                // Fallback: Try from our local loaded assemblies
                if (_loadedAssemblies != null && _loadedAssemblies.Count > 0)
                {
                    var assembly = _loadedAssemblies.FirstOrDefault(a => 
                        a?.GetName()?.Name != null && 
                        a.GetName().Name.Equals(assemblyName.Name, StringComparison.OrdinalIgnoreCase));
                    
                    if (assembly != null)
                    {
                        Logger?.LogWithContext($"Resolved assembly from local cache: {assemblyName.Name}", null);
                        return assembly;
                    }
                }

                // Last resort: Try to load on-demand from configured folders
                // Add null checks for ConfigEditor and its properties
                if (ConfigEditor?.Config?.Folders != null)
                {
                    var simpleName = assemblyName.Name;
                    var allFolders = ConfigEditor.Config.Folders
                        .Where(c => c != null && 
                                  !string.IsNullOrEmpty(c.FolderPath) &&
                                  (c.FolderFilesType == FolderFileTypes.OtherDLL ||
                                   c.FolderFilesType == FolderFileTypes.ConnectionDriver ||
                                   c.FolderFilesType == FolderFileTypes.ProjectClass ||
                                   c.FolderFilesType == FolderFileTypes.DataSources));

                    foreach (var folder in allFolders)
                    {
                        try
                        {
                            if (!Directory.Exists(folder.FolderPath))
                                continue;

                            // CRITICAL: First try framework-specific subdirectory
                            var resolvedFolderPath = ResolveFrameworkSpecificPath(folder.FolderPath);
                            var di = new DirectoryInfo(resolvedFolderPath);
                            
                            // Search in resolved path first (TopDirectoryOnly to avoid wrong framework versions)
                            var module = di.GetFiles($"{simpleName}.dll", SearchOption.TopDirectoryOnly).FirstOrDefault();
                            
                            // If not found and we're in a framework-specific path, try parent folder too
                            if (module == null && resolvedFolderPath != folder.FolderPath)
                            {
                                di = new DirectoryInfo(folder.FolderPath);
                                module = di.GetFiles($"{simpleName}.dll", SearchOption.TopDirectoryOnly).FirstOrDefault();
                            }
                            
                            if (module != null && _sharedContextManager != null)
                            {
                                // Load via SharedContextManager to maintain proper isolation and reference sharing
                                Logger?.LogWithContext($"Loading assembly on-demand from: {module.FullName}", null);
                                var nugget = _sharedContextManager.LoadNuggetAsync(
                                    module.FullName, 
                                    $"OnDemand_{simpleName}_{DateTime.UtcNow.Ticks}"
                                ).GetAwaiter().GetResult();
                                
                                if (nugget?.LoadedAssemblies != null && nugget.LoadedAssemblies.Count > 0)
                                {
                                    var loadedAssembly = nugget.LoadedAssemblies.FirstOrDefault(a => 
                                        a?.GetName()?.Name != null &&
                                        a.GetName().Name.Equals(simpleName, StringComparison.OrdinalIgnoreCase));
                                    
                                    if (loadedAssembly != null)
                                    {
                                        // Add to our local tracking
                                        if (_loadedAssemblies != null && !_loadedAssemblies.Contains(loadedAssembly))
                                        {
                                            _loadedAssemblies.Add(loadedAssembly);
                                            
                                            if (_assemblies != null)
                                            {
                                                _assemblies.Add(new assemblies_rep(loadedAssembly, folder.FolderPath, 
                                                    loadedAssembly.Location, folder.FolderFilesType));
                                            }
                                        }
                                        
                                        Logger?.LogWithContext($"Successfully loaded and resolved: {simpleName}", null);
                                        return loadedAssembly;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger?.LogWithContext($"Failed to resolve assembly {simpleName} from {folder.FolderPath}", ex);
                        }
                    }
                }

                Logger?.LogWithContext($"Unable to resolve assembly: {args.Name}", null);
                return null;
            }
            catch (Exception ex)
            {
                Logger?.LogWithContext($"Error in assembly resolution for: {args.Name}", ex);
                return null;
            }
        }

    /// <summary>Create instance by fully qualified type name using shared context.</summary>
    public object CreateInstanceFromString(string typeName, params object[] args)
        {
            return _sharedContextManager.CreateInstance(typeName, args);
        }

    /// <summary>Create instance by type name optionally constrained to a dll.</summary>
    public object CreateInstanceFromString(string dll, string typeName, params object[] args)
        {
            return _sharedContextManager.CreateInstance(typeName, args) ?? CreateInstanceFromAssembly(dll, typeName, args);
        }

    /// <summary>Get (create) an instance of a type by full name.</summary>
    public object GetInstance(string strFullyQualifiedName)
        {
            return _sharedContextManager.CreateInstance(strFullyQualifiedName);
        }

    /// <summary>Resolve a type from shared context.</summary>
    public Type GetType(string strFullyQualifiedName)
        {
            return _sharedContextManager.GetType(strFullyQualifiedName);
        }

        public List<ConnectionDriversConfig> GetDrivers(Assembly asm)
        {
            return _driverAssistant.GetDrivers(asm);
        }

        public bool AddEngineDefaultDrivers()
        {
            return _driverAssistant.AddEngineDefaultDrivers(DataSourcesClasses);
        }

        public void CheckDriverAlreadyExistinList()
        {
            _driverAssistant.CheckDriverAlreadyExistInList();
        }

        public List<ParentChildObject> GetAddinObjects(Assembly asm)
        {
            // Implementation similar to original but using shared context
            return new List<ParentChildObject>();
        }

        public ParentChildObject RearrangeAddin(string p, string parentid, string Objt)
        {
            // Implementation for arranging addins in hierarchy
            return new ParentChildObject();
        }

        public bool RunMethod(object ObjInstance, string FullClassName, string MethodName)
        {
            try
            {
                var cls = ConfigEditor.BranchesClasses.FirstOrDefault(x => x.className == FullClassName);
                if (cls != null)
                {
                    var method = cls.Methods.FirstOrDefault(x => x.Caption == MethodName)?.Info;
                    method?.Invoke(ObjInstance, null);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger?.LogWithContext($"Failed to run method {MethodName}", ex);
            }
            return false;
        }

    /// <summary>Delegates building of AssemblyClassDefinition to the scanning service.</summary>
    public AssemblyClassDefinition GetAssemblyClassDefinition(TypeInfo type, string typename)
        => _scanningService.GetAssemblyClassDefinition(type, typename);

    /// <summary>No-op retained for interface compatibility; caching handled by SharedContextManager.</summary>
    public void AddTypeToCache(string fullName, Type type) { }

    /// <summary>
    /// Load assemblies from a folder and scan for DataSources
    /// Designed for loading DLLs extracted from NuGet packages
    /// </summary>
    /// <param name="folderPath">Path to folder containing DLLs</param>
    /// <param name="folderFileType">Type of folder (DataSources, ConnectionDriver, etc.)</param>
    /// <param name="scanForDataSources">Whether to scan specifically for IDataSource implementations</param>
    /// <returns>List of successfully loaded assemblies</returns>
    public List<Assembly> LoadAssembliesFromFolder(string folderPath, FolderFileTypes folderFileType, bool scanForDataSources = true)
    {
        var loadedAssemblies = new List<Assembly>();
        
        if (!Directory.Exists(folderPath))
        {
            Logger?.LogWithContext($"LoadAssembliesFromFolder: Directory does not exist: {folderPath}", null);
            return loadedAssemblies;
        }

        try
        {
            // Resolve framework-specific path if applicable
            string resolvedPath = ResolveFrameworkSpecificPath(folderPath);
            
            foreach (string dllPath in Directory.GetFiles(resolvedPath, "*.dll", SearchOption.AllDirectories))
            {
                // Skip native DLLs in runtimes folders
                if (dllPath.Contains($"{Path.DirectorySeparatorChar}runtimes{Path.DirectorySeparatorChar}") ||
                    dllPath.Contains("/runtimes/"))
                {
                    continue;
                }
                
                // Check if already loaded by location
                if (_loadedAssemblies.Any(a => !string.IsNullOrEmpty(a.Location) && 
                    a.Location.Equals(dllPath, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }
                
                try
                {
                    // Load via SharedContextManager (can handle both files and directories)
                    var nuggetInfo = _sharedContextManager.LoadNuggetAsync(
                        dllPath, 
                        $"Folder_{folderFileType}_{Path.GetFileNameWithoutExtension(dllPath)}_{DateTime.UtcNow.Ticks}"
                    ).GetAwaiter().GetResult();
                    
                    if (nuggetInfo != null && nuggetInfo.LoadedAssemblies != null)
                    {
                        foreach (var assembly in nuggetInfo.LoadedAssemblies)
                        {
                            // Add to tracking collections
                            if (!_loadedAssemblies.Contains(assembly))
                            {
                                _loadedAssemblies.Add(assembly);
                                loadedAssemblies.Add(assembly);
                            }
                            
                            // Add to assemblies_rep collection
                            if (!_assemblies.Any(a => a.DllLib == assembly))
                            {
                                _assemblies.Add(new assemblies_rep(assembly, dllPath, assembly.Location ?? dllPath, folderFileType));
                            }
                            
                            // Scan based on flag
                            if (scanForDataSources)
                            {
                                _scanningService?.ScanAssemblyForDataSources(assembly, DataSourcesClasses);
                            }
                            else
                            {
                                _scanningService?.ScanAssembly(assembly, null);
                            }
                        }
                    }
                }
                catch (BadImageFormatException)
                {
                    Logger?.LogWithContext($"LoadAssembliesFromFolder: Skipping native DLL: {Path.GetFileName(dllPath)}", null);
                }
                catch (Exception ex)
                {
                    Logger?.LogWithContext($"LoadAssembliesFromFolder: Error loading {dllPath}: {ex.Message}", ex);
                }
            }
        }
        catch (Exception ex)
        {
            Logger?.LogWithContext($"LoadAssembliesFromFolder: Error processing folder {folderPath}: {ex.Message}", ex);
            ErrorObject.Flag = Errors.Failed;
            ErrorObject.Message = ex.Message;
        }

        Logger?.LogWithContext($"LoadAssembliesFromFolder: Loaded {loadedAssemblies.Count} assembly(ies) from {folderPath}", null);
        return loadedAssemblies;
    }

    #region Nugget Management

    /// <summary>
    /// Load a nugget package from specified path
    /// </summary>
    public bool LoadNugget(string path)
    {
        try
        {
            ErrorObject.Flag = Errors.Ok;
            
            // Use NuggetManager to load the nugget with isolated context for hot-reload
            var result = _nuggetManager.LoadNugget(path, useIsolatedContext: true);

            if (result)
            {
                // Get loaded assemblies from nugget
                var nuggetName = Path.GetFileNameWithoutExtension(path);
                var nuggetAssemblies = _nuggetManager.GetNuggetAssemblies(nuggetName);

                // Add to tracking lists
                foreach (var assembly in nuggetAssemblies)
                {
                    if (!_loadedAssemblies.Contains(assembly))
                    {
                        _loadedAssemblies.Add(assembly);
                    }

                    var assemblyRep = new assemblies_rep(assembly, path, assembly.FullName, FolderFileTypes.OtherDLL);
                    if (!_assemblies.Any(a => a.DllLib == assembly))
                    {
                        _assemblies.Add(assemblyRep);
                    }

                    // Scan assembly using scanning service
                    _scanningService?.ScanAssembly(assembly);
                }

                Logger?.LogWithContext($"Successfully loaded nugget: {nuggetName}", null);
            }

            return result;
        }
        catch (Exception ex)
        {
            ErrorObject.Flag = Errors.Failed;
            ErrorObject.Message = ex.Message;
            ErrorObject.Ex = ex;
            Logger?.LogWithContext($"Error loading nugget from {path}", ex);
            return false;
        }
    }

    /// <summary>
    /// Unload a nugget package by name
    /// </summary>
    public bool UnloadNugget(string nuggetname)
    {
        try
        {
            ErrorObject.Flag = Errors.Ok;
            
            var result = _nuggetManager.UnloadNugget(nuggetname);

            if (result)
            {
                Logger?.LogWithContext($"Successfully unloaded nugget: {nuggetname}", null);
            }
            else
            {
                Logger?.LogWithContext($"Nugget not found: {nuggetname}", null);
            }

            return result;
        }
        catch (Exception ex)
        {
            ErrorObject.Flag = Errors.Failed;
            ErrorObject.Message = ex.Message;
            ErrorObject.Ex = ex;
            Logger?.LogWithContext($"Error unloading nugget: {nuggetname}", ex);
            return false;
        }
    }

    /// <summary>
    /// Unload an assembly by name
    /// </summary>
    public bool UnloadAssembly(string assemblyname)
    {
        try
        {
            ErrorObject.Flag = Errors.Ok;

            // Find assembly in loaded assemblies
            var assembly = _loadedAssemblies.FirstOrDefault(a =>
                a.GetName().Name.Equals(assemblyname, StringComparison.OrdinalIgnoreCase));

            if (assembly != null)
            {
                // Remove from tracking lists
                _loadedAssemblies.Remove(assembly);

                var assemblyRep = _assemblies.FirstOrDefault(a => a.DllLib == assembly);
                if (assemblyRep != null)
                {
                    _assemblies.Remove(assemblyRep);
                }

                // Check if this assembly belongs to a nugget and try to find the nugget
                var nuggetName = _nuggetManager.FindNuggetByAssemblyPath(assembly.Location);
                if (!string.IsNullOrEmpty(nuggetName))
                {
                    _nuggetManager.UnloadNugget(nuggetName);
                }

                Logger?.LogWithContext($"Successfully unloaded assembly: {assemblyname}", null);
                return true;
            }

            Logger?.LogWithContext($"Assembly not found: {assemblyname}", null);
            return false;
        }
        catch (Exception ex)
        {
            ErrorObject.Flag = Errors.Failed;
            ErrorObject.Message = ex.Message;
            ErrorObject.Ex = ex;
            Logger?.LogWithContext($"Error unloading assembly: {assemblyname}", ex);
            return false;
        }
    }

    #endregion

    #region Inlined Scanning Logic (formerly in assistants)
    // (Duplicate helper block removed)
    #endregion

        private List<ParentChildObject> BuildAddinHierarchy()
        {
            var hierarchy = new List<ParentChildObject>();
            foreach (var tree in ConfigEditor.AddinTreeStructure)
            {
                try
                {
                    if (tree.PackageName.Contains("Properties")) continue;
                    var parts = tree.PackageName.Split('.');
                    for (int i = 0; i < parts.Length; i++)
                    {
                        string objType = (i == parts.Length - 1) ? tree.ObjectType : "namespace";
                        string parentId = i == 0 ? null : parts[i - 1];
                        var item = new ParentChildObject
                        {
                            id = parts[i],
                            ParentID = parentId,
                            ObjType = objType,
                            AddinName = parts[i],
                            Description = parts[i]
                        };
                        if (!hierarchy.Any(h => h.id == item.id && h.ParentID == item.ParentID))
                        {
                            hierarchy.Add(item);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger?.LogWithContext($"Error processing addin tree: {tree.PackageName}", ex);
                }
            }
            ConfigEditor.SaveAddinTreeStructure();
            return hierarchy;
        }
        #endregion

    #region Dispose
        public void Dispose()
        {
            if (!_disposed)
            {
                // Dispose retained driver assistant only
                _driverAssistant?.Dispose();
                
                // Dispose shared context manager
                _sharedContextManager?.Dispose();
                
                // Remove assembly resolver
                AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
                
                _disposed = true;
                
                Logger?.LogWithContext("SharedContextAssemblyHandler disposed with full cleanup", null);
            }
        }
        #endregion

        /// <summary>
        /// Gets all discovered drivers from the shared context
        /// </summary>
        public List<ConnectionDriversConfig> GetAllDiscoveredDrivers()
        {
            return _sharedContextManager.DiscoveredDrivers;
        }

        /// <summary>
        /// Gets all discovered data sources from the shared context
        /// </summary>
        public List<AssemblyClassDefinition> GetAllDiscoveredDataSources()
        {
            return _sharedContextManager.DiscoveredDataSources;
        }

        /// <summary>
        /// Gets all discovered addins from the shared context
        /// </summary>
        public List<AssemblyClassDefinition> GetAllDiscoveredAddins()
        {
            return _sharedContextManager.DiscoveredAddins;
        }

        /// <summary>
        /// Gets all discovered loader extensions from the shared context
        /// </summary>
        public List<AssemblyClassDefinition> GetAllDiscoveredLoaderExtensions()
        {
            return _sharedContextManager.DiscoveredLoaderExtensions;
        }

        /// <summary>
        /// Gets all discovered workflow actions from the shared context
        /// </summary>
        public List<AssemblyClassDefinition> GetAllDiscoveredWorkflowActions()
        {
            return _sharedContextManager.DiscoveredWorkflowActions;
        }

        /// <summary>
        /// Gets all discovered view models from the shared context
        /// </summary>
        public List<AssemblyClassDefinition> GetAllDiscoveredViewModels()
        {
            return _sharedContextManager.DiscoveredViewModels;
        }

        /// <summary>
        /// Gets comprehensive discovery statistics
        /// </summary>
        public Dictionary<string, object> GetDiscoveryStatistics()
        {
            return new Dictionary<string, object>
            {
                ["DriverDiscoveryStats"] = _driverAssistant.GetDriverDiscoveryStatistics(),
                ["ScanningStats"] = _scanningService.GetScanningStatistics(),
                ["InstanceCreationStats"] = new Dictionary<string, object>{{"SharedContextTypes",_sharedContextManager.GetCachedTypes().Count}},
                ["SharedContextStats"] = _sharedContextManager.GetIntegratedStatistics()
            };
        }

        #region Helper Methods
        private void SendMessage(IProgress<PassedArgs> progress, CancellationToken token, string message = null)
        {
            if (progress != null)
            {
                var args = new PassedArgs 
                { 
                    EventType = "Update", 
                    Messege = message, 
                    ErrorCode = ErrorObject.Message 
                };
                progress.Report(args);
            }
        }

        /// <summary>
        /// Resolves a path to framework-specific subdirectory if it exists.
        /// For multi-targeted projects, returns path/net8.0 (or net9.0, etc.) if it exists.
        /// Otherwise returns the original path.
        /// </summary>
        private string ResolveFrameworkSpecificPath(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                    return path;

                // If it's a file path, return as-is
                if (File.Exists(path))
                    return path;

                // If it's a directory, check for framework-specific subdirectory
                if (Directory.Exists(path))
                {
                    var targetFramework = GetCurrentTargetFramework();
                    var frameworkPath = Path.Combine(path, targetFramework);
                    
                    if (Directory.Exists(frameworkPath))
                    {
                        Logger?.LogWithContext($"Resolved framework-specific path: {path} -> {frameworkPath}", null);
                        return frameworkPath;
                    }
                    
                    // Also check for -windows variant (e.g., net8.0-windows)
                    var windowsFrameworkPath = Path.Combine(path, $"{targetFramework}-windows");
                    if (Directory.Exists(windowsFrameworkPath))
                    {
                        Logger?.LogWithContext($"Resolved framework-specific path: {path} -> {windowsFrameworkPath}", null);
                        return windowsFrameworkPath;
                    }
                }

                // Return original path if no framework-specific directory found
                return path;
            }
            catch (Exception ex)
            {
                Logger?.LogWithContext($"Error resolving framework-specific path for: {path}", ex);
                return path;
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
                Logger?.LogWithContext("Error detecting target framework, defaulting to net8.0", ex);
                return "net8.0"; // Safe default
            }
        }

        private Dictionary<string, object> GetLoadingStatistics()
        {
            return new Dictionary<string, object>
            {
                ["TotalAssemblies"] = _assemblies.Count,
                ["LoadedAssemblies"] = _loadedAssemblies.Count,
                ["DataSources"] = DataSourcesClasses.Count,
                ["SharedNuggets"] = _sharedContextManager.GetLoadedNuggets().Count(),
                ["SharedAssemblies"] = _sharedContextManager.GetSharedAssemblies().Count,
                ["CachedTypes"] = _sharedContextManager.GetCachedTypes().Count,
                // Now we can access discovered drivers from SharedContextManager!
                ["DiscoveredDrivers"] = _sharedContextManager.DiscoveredDrivers.Count,
                ["DiscoveredDataSources"] = _sharedContextManager.DiscoveredDataSources.Count,
                ["DiscoveredAddins"] = _sharedContextManager.DiscoveredAddins.Count,
                ["DiscoveredWorkflowActions"] = _sharedContextManager.DiscoveredWorkflowActions.Count,
                ["DiscoveredViewModels"] = _sharedContextManager.DiscoveredViewModels.Count,
                ["DiscoveredLoaderExtensions"] = _sharedContextManager.DiscoveredLoaderExtensions.Count
            };
        }
        #endregion

        #region Instance Creation Helper (Delegated)
        // Removed local logic; delegate fully to SharedContextManager fallback
        private object CreateInstanceFromAssembly(string dll, string typeName, params object[] args) =>
            _sharedContextManager.CreateInstanceFromAssembly(dll, typeName, args);
        #endregion
    }
}