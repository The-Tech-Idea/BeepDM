using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyModel;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Tools
{
    /// <summary>
    /// AssemblyHandler partial class - Assembly Loading Methods
    /// </summary>
    public partial class AssemblyHandler
    {

        #region Initialization

        /// <summary>
        /// Initialize loaded assemblies from dependency context and current domain
        /// </summary>
        private void InitializeLoadedAssemblies()
        {
            try
            {
                // Get current, executing, and calling assemblies
                var assemblies = new List<Assembly>
                {
                    Assembly.GetExecutingAssembly(),
                    Assembly.GetCallingAssembly(),
                    Assembly.GetEntryAssembly()
                };

                // Get dependency assemblies
                var dependencyAssemblies = DependencyContext.Default.RuntimeLibraries
                    .SelectMany(library => library.GetDefaultAssemblyNames(DependencyContext.Default))
                    .Select(Assembly.Load)
                    .Where(assembly => !assembly.FullName.StartsWith("System") && !assembly.FullName.StartsWith("Microsoft"))
                    .ToList();

                // Combine both sets of assemblies
                LoadedAssemblies = dependencyAssemblies.Concat(assemblies)
                    .Where(a => a != null)
                    .Distinct()
                    .ToList();

                Logger?.WriteLog($"InitializeLoadedAssemblies: Loaded {LoadedAssemblies.Count} assemblies");
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"InitializeLoadedAssemblies: Error - {ex.Message}");
            }
        }

        #endregion

        #region Assembly Loading

        /// <summary>
        /// Load a single assembly from path
        /// </summary>
        public Assembly LoadAssembly(string path)
        {
            ErrorObject.Flag = Errors.Ok;
            string res = "";
            Assembly loadedAssembly = null;
            
            try
            {
                if (_loadedAssemblyCache.TryGetValue(path, out loadedAssembly))
                {
                    return loadedAssembly; // Return cached assembly if it exists
                }
                
                loadedAssembly = LoadAssemblySafely(path);
                if (loadedAssembly != null)
                {
                    assemblies_rep x = new assemblies_rep(loadedAssembly, path, path, FolderFileTypes.SharedAssembly);
                    Assemblies.Add(x);
                    _loadedAssemblyCache[path] = loadedAssembly; // Cache the loaded assembly
                    LoadedAssemblies.Add(loadedAssembly);
                }
            }
            catch (FileLoadException loadEx)
            {
                ErrorObject.Flag = Errors.Failed;
                res = "The Assembly has already been loaded" + loadEx.Message;
            }
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
        /// Load assemblies from a directory or file path
        /// </summary>
        public string LoadAssembly(string path, FolderFileTypes fileTypes)
        {
            ErrorObject.Flag = Errors.Ok;
            string res = "";
            
            try
            {
                if (!Directory.Exists(path))
                {
                    Logger?.WriteLog($"LoadAssembly: Directory does not exist: {path}");
                    return res;
                }

                foreach (string s in Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories))
                {
                    try
                    {
                        // Check if already loaded
                        if (_loadedAssemblyCache.ContainsKey(s))
                        {
                            continue;
                        }

                        Assembly loadedAssembly = LoadAssemblySafely(s);
                        if (loadedAssembly != null)
                        {
                            assemblies_rep x = new assemblies_rep(loadedAssembly, s, s, fileTypes);
                            Assemblies.Add(x);
                            _loadedAssemblyCache[s] = loadedAssembly;
                            
                            if (!LoadedAssemblies.Contains(loadedAssembly))
                            {
                                LoadedAssemblies.Add(loadedAssembly);
                            }
                        }
                    }
                    catch (FileLoadException loadEx)
                    {
                        res = "The Assembly has already been loaded" + loadEx.Message;
                    }
                    catch (BadImageFormatException imgEx)
                    {
                        res = imgEx.Message;
                    }
                    catch (Exception ex)
                    {
                        res = ex.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                res = ex.Message;
            }
            
            ErrorObject.Message = res;
            return res;
        }

        /// <summary>
        /// Safely load an assembly with error handling
        /// </summary>
        private Assembly LoadAssemblySafely(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    Logger?.WriteLog($"LoadAssemblySafely: File does not exist: {path}");
                    return null;
                }

                return Assembly.LoadFrom(path);
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"LoadAssemblySafely: Error loading '{path}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Load runtime assemblies and add to Assemblies list
        /// </summary>
        public string LoadAssemblyFormRunTime()
        {
            ErrorObject.Flag = Errors.Ok;
            string res = "";

            foreach (Assembly loadedAssembly in LoadedAssemblies)
            {
                try
                {
                    // If loadedassembly not found in Assemblies then add to assemblies
                    if (Assemblies.Where(x => x.DllLib == loadedAssembly).Count() == 0)
                    {
                        assemblies_rep x = new assemblies_rep(loadedAssembly, "Builtin", loadedAssembly.FullName, FolderFileTypes.Builtin);
                        Assemblies.Add(x);
                    }
                }
                catch (FileLoadException loadEx)
                {
                    ErrorObject.Flag = Errors.Failed;
                    res = "The Assembly has already been loaded" + loadEx.Message;
                }
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
            }

            ErrorObject.Message = res;
            return res;
        }

        /// <summary>
        /// Load all assemblies from configured folders
        /// </summary>
        public IErrorsInfo LoadAllAssembly(IProgress<PassedArgs> progress, CancellationToken token)
        {
            Progress= progress;
            Token= token;
            ErrorObject.Flag = Errors.Ok;
            string res;
            Utilfunction.FunctionHierarchy = new List<ParentChildObject>();
            Utilfunction.Namespacelist = new List<string>();
            Utilfunction.Classlist = new List<string>();
            DataDriversConfig = new List<ConnectionDriversConfig>();

            SendMessege(progress, token, "Getting Builtin Classes");
            GetBuiltinClasses();
            LoadAssemblyFormRunTime();
            
            SendMessege(progress, token, "Getting FrameWork Extensions");
            GetExtensionScanners(progress, token);

            SendMessege(progress, token, "Getting Drivers Classes");
            LoadFolderAssemblies(FolderFileTypes.ConnectionDriver, progress, token);

            SendMessege(progress, token, "Getting Data Sources Classes");
            LoadFolderAssemblies(FolderFileTypes.DataSources, progress, token);

            if (ConfigEditor.ConfigType != BeepConfigType.DataConnector)
            {
                SendMessege(progress, token, "Getting Project and Addin Classes");
                LoadFolderAssemblies(FolderFileTypes.ProjectClass, progress, token);

                SendMessege(progress, token, "Getting Other DLL Classes");
                LoadFolderAssemblies(FolderFileTypes.OtherDLL, progress, token);
            }

            // Scan assemblies for drivers and data sources
            SendMessege(progress, token, "Scanning Classes For Drivers");
            ScanForDrivers();

            SendMessege(progress, token, "Scanning Classes For DataSources");
            ScanForDataSources();

            if (ConfigEditor.ConfigType != BeepConfigType.DataConnector)
            {
                SendMessege(progress, token, "Scanning Classes For Addins");
                LoadFolderAssemblies(FolderFileTypes.Addin, progress, token);

                SendMessege(progress, token, "Scanning Classes For Project's and Addin's");
                ScanProjectAndAddinAssemblies();
            }

            SendMessege(progress, token, "Adding Default Engine Drivers");
            AddEngineDefaultDrivers();
            
            SendMessege(progress, token, "Organizing Drivers");
            CheckDriverAlreadyExistinList();
            
            SendMessege(progress, token, "Scanning Extensions");
            ScanExtensions();
            
            //SendMessege(progress, token, "Scanning Folders For Extension Project's and Addin's");
            //ScanExtensionsInAssemblies();

            if (ConfigEditor.ConfigType != BeepConfigType.DataConnector)
            {
                Utilfunction.FunctionHierarchy = GetAddinObjectsFromTree();
            }

            return ErrorObject;
        }

        /// <summary>
        /// Load assemblies from a specific folder type
        /// </summary>
        private void LoadFolderAssemblies(FolderFileTypes folderType, IProgress<PassedArgs> progress, CancellationToken token)
        {
            string res;
            var folderPaths = new List<string>();
            
            // Get paths from Config.Folders
            if (ConfigEditor?.Config?.Folders != null)
            {
                folderPaths = ConfigEditor.Config.Folders
                    .Where(c => c != null && !string.IsNullOrEmpty(c.FolderPath) && c.FolderFilesType == folderType)
                    .Select(x => x.FolderPath)
                    .ToList();
            }
            
            // Fallback to default paths if no folders configured
            if (folderPaths.Count == 0 && ConfigEditor?.Config != null)
            {
                string fallbackPath = null;
                
                if (folderType == FolderFileTypes.ConnectionDriver && !string.IsNullOrEmpty(ConfigEditor.Config.ConnectionDriversPath))
                {
                    fallbackPath = ConfigEditor.Config.ConnectionDriversPath;
                }
                else if (folderType == FolderFileTypes.DataSources && !string.IsNullOrEmpty(ConfigEditor.Config.DataSourcesPath))
                {
                    fallbackPath = ConfigEditor.Config.DataSourcesPath;
                }
                else if (ConfigEditor?.ExePath != null)
                {
                    // Try default path based on folder type
                    var folderName = folderType == FolderFileTypes.ConnectionDriver ? "ConnectionDrivers" :
                                    folderType == FolderFileTypes.DataSources ? "DataSources" :
                                    folderType.ToString();
                    var defaultPath = Path.Combine(ConfigEditor.ExePath, folderName);
                    if (Directory.Exists(defaultPath))
                    {
                        fallbackPath = defaultPath;
                    }
                }
                
                if (!string.IsNullOrEmpty(fallbackPath))
                {
                    Logger?.WriteLog($"LoadFolderAssemblies: Using fallback path for {folderType}: {fallbackPath}");
                    folderPaths.Add(fallbackPath);
                }
            }
            
            foreach (string p in folderPaths)
            {
                try
                {
                    SendMessege(progress, token, $"Loading assemblies from {p}");
                    LoadAssembly(p, folderType);
                    
                    // Also scan subfolders (for NuGet packages downloaded to subfolders)
                    if (Directory.Exists(p))
                    {
                        foreach (var subDir in Directory.GetDirectories(p))
                        {
                            try
                            {
                                Logger?.WriteLog($"LoadFolderAssemblies: Scanning subfolder: {Path.GetFileName(subDir)}");
                                LoadAssembly(subDir, folderType);
                            }
                            catch (Exception subEx)
                            {
                                Logger?.WriteLog($"LoadFolderAssemblies: Failed to load from subfolder {subDir}: {subEx.Message}");
                            }
                        }
                    }
                }
                catch (FileLoadException loadEx)
                {
                    ErrorObject.Flag = Errors.Failed;
                    res = "The Assembly has already been loaded" + loadEx.Message;
                }
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
            }
        }

        /// <summary>
        /// Get extension scanners from loader extensions folder
        /// </summary>
        public void GetExtensionScanners(IProgress<PassedArgs> progress, CancellationToken token)
        {
            try
            {
                LoadAssembly(Path.Combine(ConfigEditor.ExePath, "LoadingExtensions"), FolderFileTypes.LoaderExtensions);
            }
            catch (FileLoadException loadEx)
            {
                ErrorObject.Flag = Errors.Failed;
            }
            catch (BadImageFormatException imgEx)
            {
                ErrorObject.Flag = Errors.Failed;
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
            }

            foreach (assemblies_rep s in Assemblies.Where(x => x.FileTypes == FolderFileTypes.LoaderExtensions))
            {
                try
                {
                    ScanAssembly(s.DllLib);
                }
                catch (Exception ex)
                {
                    ErrorObject.Flag = Errors.Failed;
                }
            }
        }

        /// <summary>
        /// Get builtin classes from current and entry assemblies
        /// </summary>
        public IErrorsInfo GetBuiltinClasses()
        {
            var assemblies = LoadedAssemblies;

            foreach (Assembly item in assemblies)
            {
                try
                {
                    if (!item.FullName.StartsWith("System") && !item.FullName.StartsWith("Microsoft"))
                    {
                        SendMessege(Progress, Token, $"Getting Builtin Classes from {item.FullName}");
                        Assemblies.Add(new assemblies_rep(item, "", item.FullName, FolderFileTypes.Builtin));
                        ScanAssembly(item);
                    }
                }
                catch (Exception ex)
                {
                    Logger?.WriteLog($"GetBuiltinClasses: Error loading assembly {ex.Message}");
                }
            }

            // Scan current and entry assemblies
            Assembly currentAssem = Assembly.GetExecutingAssembly();
            Assembly rootassembly = Assembly.GetEntryAssembly();

            try
            {
                if (!currentAssem.FullName.StartsWith("System") && !currentAssem.FullName.StartsWith("Microsoft"))
                {
                    SendMessege(Progress, Token, $"Getting Builtin Classes from {currentAssem.FullName}");
                    ScanAssembly(currentAssem);
                }
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"GetBuiltinClasses: Error scanning current assembly {ex.Message}");
            }

            try
            {
                if (rootassembly != null)
                {
                    ScanAssembly(rootassembly);
                }
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"GetBuiltinClasses: Error scanning root assembly {ex.Message}");
            }

            return ErrorObject;
        }

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
                Logger?.WriteLog($"LoadAssembliesFromFolder: Directory does not exist: {folderPath}");
                return loadedAssemblies;
            }

            try
            {
                foreach (string dllPath in Directory.GetFiles(folderPath, "*.dll", SearchOption.AllDirectories))
                {
                    try
                    {
                        // Skip native DLLs in runtimes folders
                        if (dllPath.Contains($"{Path.DirectorySeparatorChar}runtimes{Path.DirectorySeparatorChar}") ||
                            dllPath.Contains("/runtimes/"))
                        {
                            continue;
                        }

                        // Check if already loaded
                        if (_loadedAssemblyCache.ContainsKey(dllPath))
                        {
                            var cached = _loadedAssemblyCache[dllPath];
                            if (!loadedAssemblies.Contains(cached))
                                loadedAssemblies.Add(cached);
                            continue;
                        }

                        Assembly loadedAssembly = LoadAssemblySafely(dllPath);
                        if (loadedAssembly != null)
                        {
                            // Add to collections
                            assemblies_rep x = new assemblies_rep(loadedAssembly, dllPath, dllPath, folderFileType);
                            if (!Assemblies.Any(a => a.DllLib == loadedAssembly))
                            {
                                Assemblies.Add(x);
                            }
                            
                            _loadedAssemblyCache[dllPath] = loadedAssembly;
                            
                            if (!LoadedAssemblies.Contains(loadedAssembly))
                            {
                                LoadedAssemblies.Add(loadedAssembly);
                            }

                            // Scan for DataSources if requested
                            if (scanForDataSources)
                            {
                                ScanAssemblyForDataSources(loadedAssembly);
                            }
                            else
                            {
                                // Full scan for all types
                                ScanAssembly(loadedAssembly);
                            }

                            loadedAssemblies.Add(loadedAssembly);
                            Logger?.WriteLog($"LoadAssembliesFromFolder: Loaded and scanned {loadedAssembly.GetName().Name} from {dllPath}");
                        }
                    }
                    catch (FileLoadException loadEx)
                    {
                        Logger?.WriteLog($"LoadAssembliesFromFolder: Assembly already loaded: {dllPath} - {loadEx.Message}");
                    }
                    catch (BadImageFormatException imgEx)
                    {
                        Logger?.WriteLog($"LoadAssembliesFromFolder: Bad image format (likely native DLL): {dllPath} - {imgEx.Message}");
                    }
                    catch (Exception ex)
                    {
                        Logger?.WriteLog($"LoadAssembliesFromFolder: Error loading {dllPath}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"LoadAssembliesFromFolder: Error processing folder {folderPath}: {ex.Message}");
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
            }

            Logger?.WriteLog($"LoadAssembliesFromFolder: Successfully loaded {loadedAssemblies.Count} assembly(ies) from {folderPath}");
            return loadedAssemblies;
        }

        #endregion

        #region Nugget Management

        /// <summary>
        /// Load a NuGet package from specified path
        /// </summary>
        public bool LoadNugget(string path)
        {
            try
            {
                // Use isolated context for hot-reload capability
                var result = _nuggetManager.LoadNugget(path, useIsolatedContext: true);

                if (result)
                {
                    // Add nugget assemblies to LoadedAssemblies
                    var nuggetName = Path.GetFileNameWithoutExtension(path);
                    var nuggetAssemblies = _nuggetManager.GetNuggetAssemblies(nuggetName);

                    foreach (var assembly in nuggetAssemblies)
                    {
                        if (!LoadedAssemblies.Contains(assembly))
                        {
                            LoadedAssemblies.Add(assembly);

                            // Add to Assemblies list
                            var assemblyRep = new assemblies_rep(assembly, path, assembly.FullName, FolderFileTypes.OtherDLL);
                            if (!Assemblies.Any(a => a.DllLib == assembly))
                            {
                                Assemblies.Add(assemblyRep);
                            }

                            // Scan the assembly for types
                            ScanAssembly(assembly);
                        }
                    }

                    Logger?.WriteLog($"LoadNugget: Successfully loaded and scanned nugget from {path}");
                }

                return result;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"LoadNugget: Error - {ex.Message}");
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Unload a NuGet package by name
        /// </summary>
        public bool UnloadNugget(string nuggetname)
        {
            try
            {
                var result = _nuggetManager.UnloadNugget(nuggetname);

                if (result)
                {
                    Logger?.WriteLog($"UnloadNugget: Successfully unloaded '{nuggetname}'");
                }

                return result;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"UnloadNugget: Error - {ex.Message}");
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Download a nugget from NuGet.org and load it into the engine
        /// </summary>
        public async Task<List<Assembly>> LoadNuggetFromNuGetAsync(string packageName, string version = null, IEnumerable<string> sources = null, bool useSingleSharedContext = true, string appInstallPath = null, bool useProcessHost = false)
        {
            var loadedAssemblies = new List<Assembly>();
            try
            {
                // Use NuggetPackageDownloader to fetch all packages and dependencies
                // Uses standard NuGet global packages folder for caching
                var downloader = new TheTechIdea.Beep.Tools.PluginSystem.NuggetPackageDownloader(Logger);
                var results = await downloader.DownloadPackageWithDependenciesAsync(packageName, version, sources);

                foreach (var kvp in results)
                {
                    var path = kvp.Value; // path to folder with DLLs
                    var useIsolated = !useSingleSharedContext;
                    var loaded = _nuggetManager.LoadNugget(path, useIsolated);
                    if (loaded)
                    {
                        var nuggetName = Path.GetFileNameWithoutExtension(path.TrimEnd(Path.DirectorySeparatorChar));
                        var nusAssemblies = _nuggetManager.GetNuggetAssemblies(nuggetName);
                        foreach (var a in nusAssemblies)
                        {
                            if (!LoadedAssemblies.Contains(a)) LoadedAssemblies.Add(a);
                            if (!loadedAssemblies.Contains(a)) loadedAssemblies.Add(a);
                        }
                        // Optionally install DLLs to application directory for improved resolution
                        try
                        {
                            var installPath = appInstallPath ?? Path.Combine(ConfigEditor.ExePath, "Plugins");
                            downloader.InstallPackageToAppDirectory(path, installPath, packageName, version);
                            if (useProcessHost)
                            {
                                var exe = Directory.GetFiles(installPath, "*.exe", SearchOption.AllDirectories).FirstOrDefault();
                                if (!string.IsNullOrWhiteSpace(exe))
                                {
                                    try
                                    {
                                        var psi = new System.Diagnostics.ProcessStartInfo { FileName = exe, UseShellExecute = false, CreateNoWindow = true };
                                        System.Diagnostics.Process.Start(psi);
                                        Logger?.WriteLog($"Started process-hosted plugin {packageName} @ {exe}");
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger?.WriteLog($"Failed to start process-hosted plugin: {ex.Message}");
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"LoadNuggetFromNuGetAsync: Error - {ex.Message}");
            }

            return loadedAssemblies;
        }

        /// <summary>
        /// Unload an assembly by name
        /// </summary>
        public bool UnloadAssembly(string assemblyname)
        {
            try
            {
                // Find and remove from LoadedAssemblies
                var assembly = LoadedAssemblies.FirstOrDefault(a =>
                    a.GetName().Name.Equals(assemblyname, StringComparison.OrdinalIgnoreCase));

                if (assembly != null)
                {
                    LoadedAssemblies.Remove(assembly);

                    // Remove from Assemblies list
                    var assemblyRep = Assemblies.FirstOrDefault(a => a.DllLib == assembly);
                    if (assemblyRep != null)
                    {
                        Assemblies.Remove(assemblyRep);
                    }

                    // Remove from cache
                    _loadedAssemblyCache.TryRemove(assembly.Location, out _);

                    Logger?.WriteLog($"UnloadAssembly: Removed tracking for '{assemblyname}'");
                    return true;
                }

                Logger?.WriteLog($"UnloadAssembly: Assembly '{assemblyname}' not found");
                return false;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"UnloadAssembly: Error - {ex.Message}");
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                return false;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Send progress message
        /// </summary>
        private void SendMessege(IProgress<PassedArgs> progress, CancellationToken token, string message)
        {
            if (progress != null)
            {
                PassedArgs args = new PassedArgs { Messege = message };
                progress.Report(args);
            }
        }

        #endregion
    }
}
