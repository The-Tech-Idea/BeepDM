using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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
        /// Load a nugget from specified path (supports .nupkg files, single DLL, and directory)
        /// </summary>
        /// <param name="path">Path to nugget .nupkg file, directory, or DLL file</param>
        /// <param name="useIsolatedContext">If true, uses AssemblyLoadContext for isolation (.NET Core/.NET 5+)</param>
        /// <returns>True if loaded successfully</returns>
        public bool LoadNugget(string path, bool useIsolatedContext = false)
        {
            string extractedPath = null;
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

                // If path is a .nupkg file, extract it first
                if (File.Exists(path) && path.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase))
                {
                    _logger?.WriteLog($"LoadNugget: Extracting .nupkg file: {path}");
                    extractedPath = ExtractNuGetPackage(path);
                    if (string.IsNullOrEmpty(extractedPath) || !Directory.Exists(extractedPath))
                    {
                        _logger?.WriteLog($"LoadNugget: Failed to extract .nupkg file: {path}");
                        return false;
                    }
                    path = extractedPath; // Use extracted directory for loading
                    _logger?.WriteLog($"LoadNugget: Extracted to: {extractedPath}");
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
            finally
            {
                // Note: We don't delete extractedPath here because assemblies may reference files in it
                // The extracted directory will remain until the nugget is unloaded or the process exits
                // If cleanup is needed, it should be done in UnloadNugget
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
        /// Checks if a repository path is a filesystem path (directory or file)
        /// </summary>
        public static bool IsFilesystemRepository(string repoPath)
        {
            if (string.IsNullOrWhiteSpace(repoPath))
                return false;
            
            // If it starts with http:// or https://, it's a URL
            if (repoPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                repoPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return false;
            
            // Check if it exists as a file or directory
            if (File.Exists(repoPath) || Directory.Exists(repoPath))
                return true;
            
            // Check if it looks like a path (contains path separators)
            if (repoPath.Contains(Path.DirectorySeparatorChar) || repoPath.Contains('/'))
                return true;
            
            return false;
        }

        /// <summary>
        /// Gets package versions from a filesystem repository (directory containing .nupkg files)
        /// </summary>
        public List<string> GetPackageVersionsFromFilesystem(string packageName, string repoPath)
        {
            var versions = new List<string>();
            
            try
            {
                if (Directory.Exists(repoPath))
                {
                    // Search for .nupkg files matching the package ID
                    var packageFiles = Directory.GetFiles(repoPath, $"{packageName}.*.nupkg", SearchOption.AllDirectories)
                        .Where(f => Path.GetFileName(f).StartsWith($"{packageName}.", StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    
                    if (!packageFiles.Any())
                    {
                        // Also try case-insensitive search
                        packageFiles = Directory.GetFiles(repoPath, "*.nupkg", SearchOption.AllDirectories)
                            .Where(f => Path.GetFileNameWithoutExtension(f).StartsWith($"{packageName}.", StringComparison.OrdinalIgnoreCase))
                            .ToList();
                    }
                    
                    foreach (var file in packageFiles)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(file);
                        // Format: PackageId.Version (e.g., "MyPackage.1.2.3")
                        var parts = fileName.Split('.');
                        if (parts.Length >= 2)
                        {
                            // Try to parse version from the end
                            for (int i = parts.Length - 1; i >= 1; i--)
                            {
                                var versionStr = string.Join(".", parts.Skip(i));
                                // Try to parse as version (basic check - could use NuGet.Versioning if available)
                                if (System.Version.TryParse(versionStr, out _) || 
                                    versionStr.Split('.').All(p => int.TryParse(p, out _)))
                                {
                                    versions.Add(versionStr);
                                    break;
                                }
                            }
                        }
                    }
                    
                    // Sort versions (newest first) - simple string comparison for now
                    versions = versions
                        .OrderByDescending(v => v, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }
                else if (File.Exists(repoPath) && repoPath.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase))
                {
                    // Single .nupkg file
                    var fileName = Path.GetFileNameWithoutExtension(repoPath);
                    if (fileName.StartsWith($"{packageName}.", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = fileName.Split('.');
                        for (int i = parts.Length - 1; i >= 1; i--)
                        {
                            var versionStr = string.Join(".", parts.Skip(i));
                            if (System.Version.TryParse(versionStr, out _) || 
                                versionStr.Split('.').All(p => int.TryParse(p, out _)))
                            {
                                versions.Add(versionStr);
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"GetPackageVersionsFromFilesystem: Error getting versions from filesystem {repoPath}: {ex.Message}");
            }
            
            return versions;
        }

        /// <summary>
        /// Checks if a package is already installed in a destination folder
        /// </summary>
        /// <param name="packageName">Name of the package</param>
        /// <param name="destinationFolder">Destination folder to check</param>
        /// <param name="expectedAssemblyName">Optional expected assembly name to check for</param>
        /// <param name="expectedClassName">Optional expected class name to verify the package is loaded</param>
        /// <returns>True if package appears to be installed</returns>
        public bool IsPackageAlreadyInstalled(string packageName, string destinationFolder, string? expectedAssemblyName = null, string? expectedClassName = null)
        {
            if (!Directory.Exists(destinationFolder))
                return false;

            // Check for DLL files that match the package name
            var dllFiles = Directory.GetFiles(destinationFolder, "*.dll", SearchOption.AllDirectories);
            
            // If expected assembly name is provided, check for it specifically
            if (!string.IsNullOrWhiteSpace(expectedAssemblyName))
            {
                var expectedDll = Path.Combine(destinationFolder, $"{expectedAssemblyName}.dll");
                if (File.Exists(expectedDll))
                {
                    _logger?.WriteLog($"IsPackageAlreadyInstalled: Package {packageName} already installed: {expectedDll}");
                    return true;
                }
                
                // Also check in subdirectories
                if (dllFiles.Any(dll => Path.GetFileNameWithoutExtension(dll).Equals(expectedAssemblyName, StringComparison.OrdinalIgnoreCase)))
                {
                    _logger?.WriteLog($"IsPackageAlreadyInstalled: Package {packageName} already installed (found matching assembly)");
                    return true;
                }
            }
            
            // Check if any DLL matches the package name (case-insensitive)
            var packageNameLower = packageName.ToLowerInvariant();
            if (dllFiles.Any(dll => Path.GetFileNameWithoutExtension(dll).ToLowerInvariant().Contains(packageNameLower)))
            {
                _logger?.WriteLog($"IsPackageAlreadyInstalled: Package {packageName} may already be installed (found matching DLL)");
                return true;
            }
            
            return false;
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
        /// Framework folders in priority order (most compatible first)
        /// </summary>
        private static readonly string[] FrameworkFolders = new[]
        {
            $"net{Environment.Version.Major}.{Environment.Version.Minor}",
            $"net{Environment.Version.Major}.0",
            "net8.0", "net7.0", "net6.0",
            "netstandard2.1", "netstandard2.0", "netstandard1.6",
            "netcoreapp3.1", "netcoreapp3.0", "netcoreapp2.1",
            "net48", "net472", "net461", "net46", "net45"
        };

        /// <summary>
        /// Get compatible DLLs from a path, prioritizing framework-specific folders and skipping native DLLs
        /// </summary>
        private IEnumerable<string> GetCompatibleDlls(string path)
        {
            var dlls = new List<string>();
            
            // Check for lib folder with framework subfolders
            var libPath = Path.Combine(path, "lib");
            if (Directory.Exists(libPath))
            {
                // Try each framework in priority order
                foreach (var fw in FrameworkFolders)
                {
                    var fwPath = Path.Combine(libPath, fw);
                    if (Directory.Exists(fwPath))
                    {
                        dlls.AddRange(Directory.GetFiles(fwPath, "*.dll", SearchOption.TopDirectoryOnly));
                        _logger?.WriteLog($"GetCompatibleDlls: Found framework folder '{fw}' with {dlls.Count} DLL(s)");
                        break; // Use first compatible framework
                    }
                }
            }
            
            // If no lib folder or no framework matches, get root DLLs
            if (!dlls.Any())
            {
                dlls.AddRange(Directory.GetFiles(path, "*.dll", SearchOption.TopDirectoryOnly));
            }
            
            // Filter out DLLs in runtimes folder (native DLLs)
            return dlls.Where(d => 
                !d.Contains($"{Path.DirectorySeparatorChar}runtimes{Path.DirectorySeparatorChar}") &&
                !d.Contains("/runtimes/"));
        }

        /// <summary>
        /// Load nugget assemblies using isolated AssemblyLoadContext
        /// </summary>
        private List<Assembly> LoadNuggetInContext(string path, AssemblyLoadContext context)
        {
            List<Assembly> assemblies = new List<Assembly>();

            try
            {
                IEnumerable<string> dllFiles;
                
                if (File.Exists(path) && path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    // Single DLL file
                    dllFiles = new[] { path };
                }
                else if (Directory.Exists(path))
                {
                    // Directory - use smart scanning to get compatible DLLs
                    dllFiles = GetCompatibleDlls(path);
                }
                else
                {
                    return assemblies;
                }

                foreach (var dllFile in dllFiles)
                {
                    try
                    {
                        var assembly = context.LoadFromAssemblyPath(dllFile);
                        assemblies.Add(assembly);
                        _logger?.WriteLog($"LoadNuggetInContext: Loaded {Path.GetFileName(dllFile)}");
                    }
                    catch (BadImageFormatException)
                    {
                        _logger?.WriteLog($"LoadNuggetInContext: Skipping native DLL: {Path.GetFileName(dllFile)}");
                    }
                    catch (Exception ex)
                    {
                        _logger?.WriteLog($"LoadNuggetInContext: Error loading '{dllFile}': {ex.Message}");
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
                IEnumerable<string> dllFiles;
                
                if (File.Exists(path) && path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    // Single DLL file
                    dllFiles = new[] { path };
                }
                else if (Directory.Exists(path))
                {
                    // Directory - use smart scanning to get compatible DLLs
                    dllFiles = GetCompatibleDlls(path);
                }
                else
                {
                    return assemblies;
                }

                foreach (var dllFile in dllFiles)
                {
                    try
                    {
                        var assembly = Assembly.LoadFrom(dllFile);
                        assemblies.Add(assembly);
                        _logger?.WriteLog($"LoadNuggetTraditional: Loaded {Path.GetFileName(dllFile)}");
                    }
                    catch (BadImageFormatException)
                    {
                        _logger?.WriteLog($"LoadNuggetTraditional: Skipping native DLL: {Path.GetFileName(dllFile)}");
                    }
                    catch (Exception ex)
                    {
                        _logger?.WriteLog($"LoadNuggetTraditional: Error loading '{dllFile}': {ex.Message}");
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

        /// <summary>
        /// Extracts a .nupkg file to a temporary directory
        /// </summary>
        /// <param name="nupkgPath">Path to the .nupkg file</param>
        /// <returns>Path to the extracted directory, or null if extraction failed</returns>
        private string ExtractNuGetPackage(string nupkgPath)
        {
            try
            {
                if (!File.Exists(nupkgPath))
                {
                    _logger?.WriteLog($"ExtractNuGetPackage: File not found: {nupkgPath}");
                    return null;
                }

                // Extract to a subdirectory next to the .nupkg file
                var extractDir = Path.Combine(
                    Path.GetDirectoryName(nupkgPath) ?? Path.GetTempPath(),
                    Path.GetFileNameWithoutExtension(nupkgPath) + "_extracted");

                // If directory already exists, use it (may have been extracted before)
                if (!Directory.Exists(extractDir))
                {
                    Directory.CreateDirectory(extractDir);
                }

                // Extract the .nupkg file (it's a ZIP archive)
                ZipFile.ExtractToDirectory(nupkgPath, extractDir, overwriteFiles: true);

                _logger?.WriteLog($"ExtractNuGetPackage: Extracted '{nupkgPath}' to '{extractDir}'");
                return extractDir;
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"ExtractNuGetPackage: Error extracting '{nupkgPath}': {ex.Message}");
                return null;
            }
        }

        #endregion
    }
}
