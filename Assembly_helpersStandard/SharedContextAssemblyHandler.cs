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
        
        private readonly List<assemblies_rep> _assemblies = new();
        private readonly List<Assembly> _loadedAssemblies = new();
        
        private bool _disposed = false;
        #endregion

        #region Properties - IAssemblyHandler Implementation
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

    // Plugin system removed – handler now limited to interface surface only
        #endregion

        #region Constructor
        public SharedContextAssemblyHandler(IConfigEditor configEditor, IErrorsInfo errorObject, IDMLogger logger, IUtil utilFunction)
        {
            ConfigEditor = configEditor ?? throw new ArgumentNullException(nameof(configEditor));
            ErrorObject = errorObject ?? throw new ArgumentNullException(nameof(errorObject));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Utilfunction = utilFunction ?? throw new ArgumentNullException(nameof(utilFunction));

            // Initialize SharedContextManager first
            _sharedContextManager = new SharedContextManager(Logger);

            // Initialize retained assistant and scanning service
            _driverAssistant = new DriverDiscoveryAssistant(_sharedContextManager, ConfigEditor, Logger);
            _scanningService = new ScanningService(_sharedContextManager, ConfigEditor, Logger);

            // Setup assembly resolver
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            Logger?.LogWithContext("SharedContextAssemblyHandler initialized with refactored bridge pattern", null);
        }
        #endregion

        #region Core Loading Methods
        /// <summary>
        /// Loads assemblies from a path using SharedContextManager
        /// </summary>
        public string LoadAssembly(string path, FolderFileTypes fileTypes)
        {
            ErrorObject.Flag = Errors.Ok;
            string result = "";

            try
            {
                var nuggetInfo = _sharedContextManager.LoadNuggetAsync(path, $"Nugget_{fileTypes}_{DateTime.UtcNow.Ticks}").GetAwaiter().GetResult();
                
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

                    result = $"Successfully loaded nugget: {nuggetInfo.Id} with {nuggetInfo.LoadedAssemblies.Count} assemblies";
                    Logger?.LogWithContext(result, nuggetInfo);
                }
                else
                {
                    ErrorObject.Flag = Errors.Failed;
                    result = $"Failed to load assemblies from path: {path}";
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
            var driverFolders = ConfigEditor.Config.Folders
                .Where(c => c.FolderFilesType == FolderFileTypes.ConnectionDriver)
                .Select(x => x.FolderPath);

            foreach (var path in driverFolders)
            {
                try
                {
                    LoadAssembly(path, FolderFileTypes.ConnectionDriver);
                }
                catch (Exception ex)
                {
                    Logger?.LogWithContext($"Failed to load driver classes from {path}", ex);
                }
            }
        }

        private void LoadDataSourceClasses(IProgress<PassedArgs> progress, CancellationToken token)
        {
            var dataSourceFolders = ConfigEditor.Config.Folders
                .Where(c => c.FolderFilesType == FolderFileTypes.DataSources)
                .Select(x => x.FolderPath);

            foreach (var path in dataSourceFolders)
            {
                try
                {
                    LoadAssembly(path, FolderFileTypes.DataSources);
                }
                catch (Exception ex)
                {
                    Logger?.LogWithContext($"Failed to load data source classes from {path}", ex);
                }
            }
        }

        private void LoadProjectAndAddinClasses(IProgress<PassedArgs> progress, CancellationToken token)
        {
            var projectFolders = ConfigEditor.Config.Folders
                .Where(c => c.FolderFilesType == FolderFileTypes.ProjectClass)
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
            var otherDllFolders = ConfigEditor.Config.Folders
                .Where(c => c.FolderFilesType == FolderFileTypes.OtherDLL)
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
            var addinFolders = ConfigEditor.Config.Folders
                .Where(c => c.FolderFilesType == FolderFileTypes.Addin)
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
                foreach (var assembly in runtimeAssemblies)
                {
                    if (!_loadedAssemblies.Contains(assembly))
                    {
                        _loadedAssemblies.Add(assembly);
                        var assemblyRep = new assemblies_rep(assembly, "Runtime", assembly.Location, FolderFileTypes.Builtin);
                        if (!_assemblies.Any(a => a.DllLib == assembly))
                        {
                            _assemblies.Add(assemblyRep);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger?.LogWithContext("Failed to load runtime assemblies", ex);
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
            if (args.Name.Contains(".resources"))
                return null;

            var assemblyName = args.Name.Split(',')[0];
            
            // Try from shared context first
            var sharedAssemblies = _sharedContextManager.GetSharedAssemblies();
            var assembly = sharedAssemblies.FirstOrDefault(a => a.FullName.StartsWith(assemblyName));
            if (assembly != null)
                return assembly;

            // Try from loaded assemblies
            assembly = _loadedAssemblies.FirstOrDefault(a => a.FullName.StartsWith(assemblyName));
            if (assembly != null)
                return assembly;

            // Try to load from configured folders
            var allFolders = ConfigEditor.Config.Folders
                .Where(c => c.FolderFilesType == FolderFileTypes.OtherDLL ||
                           c.FolderFilesType == FolderFileTypes.ConnectionDriver ||
                           c.FolderFilesType == FolderFileTypes.ProjectClass);

            foreach (var folder in allFolders)
            {
                try
                {
                    var di = new DirectoryInfo(folder.FolderPath);
                    var module = di.GetFiles($"{assemblyName}.dll").FirstOrDefault();
                    if (module != null)
                    {
                        // Load via shared context to ensure collectible/unload support
                        var nugget = _sharedContextManager.LoadNuggetAsync(module.FullName, $"Resolve_{assemblyName}_{DateTime.UtcNow.Ticks}").GetAwaiter().GetResult();
                        return nugget?.LoadedAssemblies.FirstOrDefault(a => a.GetName().Name.Equals(assemblyName, StringComparison.OrdinalIgnoreCase));
                    }
                }
                catch (Exception ex)
                {
                    Logger?.LogWithContext($"Failed to resolve assembly {assemblyName} from {folder.FolderPath}", ex);
                }
            }

            return null;
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