using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Tools
{
    /// <summary>
    /// Manages NuGet packages (nuggets) loading and unloading with support for both traditional and isolated contexts
    /// Can be used by both AssemblyHandler and SharedContextAssemblyHandler
    /// </summary>
    public class NuggetManager
    {
        #region Private Fields
        private readonly IDMLogger _logger;
        private readonly IErrorsInfo _errorObject;
        private readonly IUtil _utilFunction;
        
        // Track loaded nuggets and their assemblies
        private readonly ConcurrentDictionary<string, NuggetInfo> _loadedNuggets = new();
        private readonly ConcurrentDictionary<string, List<Assembly>> _nuggetAssemblies = new();
        private readonly ConcurrentDictionary<string, AssemblyLoadContext> _nuggetContexts = new();
        
        // Track assembly path to nugget mappings
        private readonly ConcurrentDictionary<string, string> _assemblyPathToNugget = new();
        
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the NuggetManager
        /// </summary>
        public NuggetManager(IDMLogger logger, IErrorsInfo errorObject, IUtil utilFunction)
        {
            _logger = logger;
            _errorObject = errorObject;
            _utilFunction = utilFunction;
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Load a nugget from specified path (supports both single DLL and directory)
        /// </summary>
        /// <param name="path">Path to nugget directory or DLL file</param>
        /// <param name="useIsolatedContext">If true, uses AssemblyLoadContext for isolation (.NET Core/.NET 5+)</param>
        /// <returns>True if loaded successfully</returns>
        public bool LoadNugget(string path, bool useIsolatedContext = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    _logger?.WriteLog("LoadNugget: Path is null or empty");
                    return false;
                }

                if (!File.Exists(path) && !Directory.Exists(path))
                {
                    _logger?.WriteLog($"LoadNugget: Path does not exist: {path}");
                    return false;
                }

                // Determine nugget name
                string nuggetName = GetNuggetName(path);
                
                // Check if already loaded
                if (_loadedNuggets.ContainsKey(nuggetName))
                {
                    _logger?.WriteLog($"LoadNugget: Nugget '{nuggetName}' is already loaded");
                    return true;
                }

                List<Assembly> loadedAssemblies = new List<Assembly>();
                AssemblyLoadContext context = null;

                if (useIsolatedContext)
                {
                    // Load using isolated AssemblyLoadContext (.NET Core/.NET 5+)
                    context = new AssemblyLoadContext(nuggetName, isCollectible: true);
                    loadedAssemblies = LoadNuggetInContext(path, context);
                    _nuggetContexts.TryAdd(nuggetName, context);
                }
                else
                {
                    // Load using traditional method (shared AppDomain)
                    loadedAssemblies = LoadNuggetTraditional(path);
                }

                if (loadedAssemblies.Count == 0)
                {
                    _logger?.WriteLog($"LoadNugget: No assemblies loaded from '{path}'");
                    return false;
                }

                // Store nugget info
                var nuggetInfo = new NuggetInfo
                {
                    Id = nuggetName,
                    Name = nuggetName,
                    Version = GetNuggetVersion(loadedAssemblies.First()),
                    LoadedAt = DateTime.UtcNow,
                    LoadedAssemblies = loadedAssemblies,
                    SourcePath = path,
                    IsSharedContext = !useIsolatedContext,
                    IsActive = true
                };

                _loadedNuggets.TryAdd(nuggetName, nuggetInfo);
                _nuggetAssemblies.TryAdd(nuggetName, loadedAssemblies);

                // Map assembly paths to nugget
                foreach (var assembly in loadedAssemblies)
                {
                    if (!string.IsNullOrEmpty(assembly.Location))
                    {
                        _assemblyPathToNugget.TryAdd(assembly.Location, nuggetName);
                    }
                }

                _logger?.WriteLog($"LoadNugget: Successfully loaded '{nuggetName}' with {loadedAssemblies.Count} assemblies");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"LoadNugget: Error loading from '{path}': {ex.Message}");
                _errorObject.Flag = Errors.Failed;
                _errorObject.Message = ex.Message;
                _errorObject.Ex = ex;
                return false;
            }
        }

        /// <summary>
        /// Unload a nugget by name
        /// </summary>
        /// <param name="nuggetName">Name of the nugget to unload</param>
        /// <returns>True if unloaded successfully</returns>
        public bool UnloadNugget(string nuggetName)
        {
            try
            {
                if (!_loadedNuggets.TryGetValue(nuggetName, out var nuggetInfo))
                {
                    _logger?.WriteLog($"UnloadNugget: Nugget '{nuggetName}' not found");
                    return false;
                }

                // Remove from tracking
                _loadedNuggets.TryRemove(nuggetName, out _);
                _nuggetAssemblies.TryRemove(nuggetName, out var assemblies);

                // Remove assembly path mappings
                if (assemblies != null)
                {
                    foreach (var assembly in assemblies)
                    {
                        if (!string.IsNullOrEmpty(assembly.Location))
                        {
                            _assemblyPathToNugget.TryRemove(assembly.Location, out _);
                        }
                    }
                }

                // Unload context if isolated
                if (_nuggetContexts.TryRemove(nuggetName, out var context))
                {
                    context.Unload();
                    
                    // Force garbage collection for isolated contexts
                    for (int i = 0; i < 3; i++)
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }
                    
                    _logger?.WriteLog($"UnloadNugget: Unloaded isolated context for '{nuggetName}'");
                }
                else
                {
                    _logger?.WriteLog($"UnloadNugget: Removed tracking for '{nuggetName}' (shared context - assemblies remain in memory)");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"UnloadNugget: Error unloading '{nuggetName}': {ex.Message}");
                _errorObject.Flag = Errors.Failed;
                _errorObject.Message = ex.Message;
                _errorObject.Ex = ex;
                return false;
            }
        }

        /// <summary>
        /// Get assemblies loaded by a specific nugget
        /// </summary>
        public List<Assembly> GetNuggetAssemblies(string nuggetName)
        {
            if (_nuggetAssemblies.TryGetValue(nuggetName, out var assemblies))
            {
                return new List<Assembly>(assemblies);
            }
            return new List<Assembly>();
        }

        /// <summary>
        /// Get information about a loaded nugget
        /// </summary>
        public NuggetInfo GetNuggetInfo(string nuggetName)
        {
            _loadedNuggets.TryGetValue(nuggetName, out var info);
            return info;
        }

        /// <summary>
        /// Get all loaded nuggets
        /// </summary>
        public List<NuggetInfo> GetAllNuggets()
        {
            return _loadedNuggets.Values.ToList();
        }

        /// <summary>
        /// Check if a nugget is loaded
        /// </summary>
        public bool IsNuggetLoaded(string nuggetName)
        {
            return _loadedNuggets.ContainsKey(nuggetName);
        }

        /// <summary>
        /// Find which nugget owns a specific assembly path
        /// </summary>
        public string FindNuggetByAssemblyPath(string assemblyPath)
        {
            _assemblyPathToNugget.TryGetValue(assemblyPath, out var nuggetName);
            return nuggetName;
        }

        /// <summary>
        /// Clear all loaded nuggets (tracking only for shared context)
        /// </summary>
        public void Clear()
        {
            try
            {
                // Unload all isolated contexts
                foreach (var kvp in _nuggetContexts)
                {
                    try
                    {
                        kvp.Value.Unload();
                    }
                    catch (Exception ex)
                    {
                        _logger?.WriteLog($"Clear: Error unloading context '{kvp.Key}': {ex.Message}");
                    }
                }

                _nuggetContexts.Clear();
                _loadedNuggets.Clear();
                _nuggetAssemblies.Clear();
                _assemblyPathToNugget.Clear();

                // Force garbage collection
                for (int i = 0; i < 3; i++)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }

                _logger?.WriteLog("Clear: All nuggets cleared");
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Clear: Error: {ex.Message}");
            }
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Load nugget assemblies using isolated AssemblyLoadContext
        /// </summary>
        private List<Assembly> LoadNuggetInContext(string path, AssemblyLoadContext context)
        {
            List<Assembly> assemblies = new List<Assembly>();

            try
            {
                if (File.Exists(path) && path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    // Single DLL file
                    var assembly = context.LoadFromAssemblyPath(path);
                    assemblies.Add(assembly);
                }
                else if (Directory.Exists(path))
                {
                    // Directory - load all DLLs
                    var dllFiles = Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories);
                    foreach (var dllFile in dllFiles)
                    {
                        try
                        {
                            var assembly = context.LoadFromAssemblyPath(dllFile);
                            assemblies.Add(assembly);
                        }
                        catch (Exception ex)
                        {
                            _logger?.WriteLog($"LoadNuggetInContext: Error loading '{dllFile}': {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"LoadNuggetInContext: Error: {ex.Message}");
            }

            return assemblies;
        }

        /// <summary>
        /// Load nugget assemblies using traditional method (shared AppDomain)
        /// </summary>
        private List<Assembly> LoadNuggetTraditional(string path)
        {
            List<Assembly> assemblies = new List<Assembly>();

            try
            {
                if (File.Exists(path) && path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    // Single DLL file
                    var assembly = Assembly.LoadFrom(path);
                    assemblies.Add(assembly);
                }
                else if (Directory.Exists(path))
                {
                    // Directory - load all DLLs
                    var dllFiles = Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories);
                    foreach (var dllFile in dllFiles)
                    {
                        try
                        {
                            var assembly = Assembly.LoadFrom(dllFile);
                            assemblies.Add(assembly);
                        }
                        catch (Exception ex)
                        {
                            _logger?.WriteLog($"LoadNuggetTraditional: Error loading '{dllFile}': {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"LoadNuggetTraditional: Error: {ex.Message}");
            }

            return assemblies;
        }

        /// <summary>
        /// Get nugget name from path
        /// </summary>
        private string GetNuggetName(string path)
        {
            if (File.Exists(path))
            {
                return Path.GetFileNameWithoutExtension(path);
            }
            else if (Directory.Exists(path))
            {
                return new DirectoryInfo(path).Name;
            }
            return Path.GetFileNameWithoutExtension(path);
        }

        /// <summary>
        /// Get nugget version from assembly
        /// </summary>
        private string GetNuggetVersion(Assembly assembly)
        {
            try
            {
                return assembly.GetName().Version?.ToString() ?? "1.0.0";
            }
            catch
            {
                return "1.0.0";
            }
        }

        #endregion
    }
}
