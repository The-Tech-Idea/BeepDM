using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Tools;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Tools.PluginSystem
{
    /// <summary>
    /// Refactored AssemblyLoadingAssistant that delegates to SharedContextManager for true isolation
    /// Acts as a bridge between legacy AssemblyHandler interface and modern SharedContextManager
    /// </summary>
    public class AssemblyLoadingAssistant : IDisposable
    {
        private readonly IAssemblyHandler _assemblyHandler;
        private readonly SharedContextManager _sharedContextManager;
        private readonly IDMLogger _logger;
        private readonly ConcurrentDictionary<string, string> _pathToNuggetIdMapping = new(StringComparer.OrdinalIgnoreCase);
        private bool _disposed = false;

        public AssemblyLoadingAssistant(IAssemblyHandler assemblyHandler, SharedContextManager sharedContextManager)
        {
            _assemblyHandler = assemblyHandler;
            _sharedContextManager = sharedContextManager;
            _logger = assemblyHandler.Logger;
        }

        /// <summary>
        /// Legacy method - now delegates to SharedContextManager for true isolation
        /// </summary>
        public Assembly LoadAssemblySafely(string assemblyPath)
        {
            if (string.IsNullOrWhiteSpace(assemblyPath) || !File.Exists(assemblyPath))
            {
                return null;
            }

            try
            {
                // Check if already loaded via shared context
                var existingNugget = _sharedContextManager.GetLoadedNuggets()
                    .FirstOrDefault(n => n.LoadedAssemblies.Any(a => 
                        string.Equals(a.Location, assemblyPath, StringComparison.OrdinalIgnoreCase)));
                
                if (existingNugget != null)
                {
                    return existingNugget.LoadedAssemblies.First(a => 
                        string.Equals(a.Location, assemblyPath, StringComparison.OrdinalIgnoreCase));
                }

                // Load via shared context manager for true isolation
                var nuggetId = $"SingleAssembly_{Path.GetFileNameWithoutExtension(assemblyPath)}_{DateTime.UtcNow.Ticks}";
                var nuggetInfo = _sharedContextManager.LoadNuggetAsync(assemblyPath, nuggetId).GetAwaiter().GetResult();
                
                if (nuggetInfo?.LoadedAssemblies.Count > 0)
                {
                    _pathToNuggetIdMapping[assemblyPath] = nuggetId;
                    return nuggetInfo.LoadedAssemblies.First();
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Failed to load assembly safely: {assemblyPath}", ex);
                return null;
            }
        }

        /// <summary>
        /// Loads assemblies from directory using SharedContextManager - maintains legacy interface
        /// </summary>
        public string LoadAssembly(string path, FolderFileTypes fileTypes)
        {
            string result = "";

            if (!Directory.Exists(path))
            {
                result = $"Directory not found: {path}";
                _logger?.LogWithContext(result, null);
                return result;
            }

            try
            {
                // Use SharedContextManager for true isolation and unloading
                var nuggetId = $"Directory_{fileTypes}_{Path.GetFileName(path)}_{DateTime.UtcNow.Ticks}";
                var nuggetInfo = _sharedContextManager.LoadNuggetAsync(path, nuggetId).GetAwaiter().GetResult();

                if (nuggetInfo != null)
                {
                    // Add assemblies to AssemblyHandler collections for compatibility
                    foreach (var assembly in nuggetInfo.LoadedAssemblies)
                    {
                        if (!_assemblyHandler.LoadedAssemblies.Contains(assembly))
                        {
                            _assemblyHandler.LoadedAssemblies.Add(assembly);
                        }

                        var assemblyRep = new assemblies_rep(assembly, path, assembly.Location, fileTypes);
                        if (!_assemblyHandler.Assemblies.Any(a => a.DllLib == assembly))
                        {
                            _assemblyHandler.Assemblies.Add(assemblyRep);
                        }
                    }

                    _pathToNuggetIdMapping[path] = nuggetId;
                    result = $"Successfully loaded {nuggetInfo.LoadedAssemblies.Count} assemblies from {path} via SharedContext";
                }
                else
                {
                    result = $"Failed to load assemblies from {path}";
                }
            }
            catch (Exception ex)
            {
                result = $"Error loading assemblies: {ex.Message}";
                _logger?.LogWithContext(result, ex);
            }

            return result;
        }

        /// <summary>
        /// Loads runtime assemblies - these are NOT loaded via collectible context as they're system assemblies
        /// </summary>
        public string LoadAssemblyFromRuntime()
        {
            string result = "";

            try
            {
                var runtimeAssemblies = GetRuntimeAssemblies();

                foreach (Assembly assembly in runtimeAssemblies)
                {
                    try
                    {
                        // Runtime assemblies are added directly (not via collectible context)
                        // as they're system assemblies that should not be unloaded
                        if (_assemblyHandler.Assemblies.All(x => x.DllLib != assembly))
                        {
                            var assemblyRep = new assemblies_rep(assembly, "Runtime", assembly.Location ?? assembly.FullName, FolderFileTypes.Builtin);
                            _assemblyHandler.Assemblies.Add(assemblyRep);
                        }

                        if (!_assemblyHandler.LoadedAssemblies.Contains(assembly))
                        {
                            _assemblyHandler.LoadedAssemblies.Add(assembly);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWithContext($"Error processing runtime assembly: {assembly.FullName}", ex);
                    }
                }

                result = $"Successfully processed {runtimeAssemblies.Count} runtime assemblies";
            }
            catch (Exception ex)
            {
                result = "Failed to load runtime assemblies: " + ex.Message;
                _logger?.LogWithContext(result, ex);
            }

            return result;
        }

        /// <summary>
        /// Gets runtime assemblies - system assemblies that should not be unloaded
        /// </summary>
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
                _logger?.LogWithContext("Failed to load dependency assemblies", ex);
            }

            return assemblies.Distinct().ToList();
        }

        /// <summary>
        /// Loads assembly via SharedContextManager with full integration
        /// </summary>
        public async Task<Assembly> LoadAssemblyInSharedContextAsync(string assemblyPath, string contextId = null)
        {
            try
            {
                var nuggetId = contextId ?? $"SharedContext_{Path.GetFileNameWithoutExtension(assemblyPath)}_{DateTime.UtcNow.Ticks}";
                var nuggetInfo = await _sharedContextManager.LoadNuggetAsync(assemblyPath, nuggetId);
                
                if (nuggetInfo?.LoadedAssemblies.Count > 0)
                {
                    var assembly = nuggetInfo.LoadedAssemblies.First();
                    
                    // Add to AssemblyHandler collections for compatibility
                    if (!_assemblyHandler.LoadedAssemblies.Contains(assembly))
                    {
                        _assemblyHandler.LoadedAssemblies.Add(assembly);
                    }

                    var assemblyRep = new assemblies_rep(assembly, contextId ?? "SharedContext", assemblyPath, FolderFileTypes.Nugget);
                    if (!_assemblyHandler.Assemblies.Any(a => a.DllLib == assembly))
                    {
                        _assemblyHandler.Assemblies.Add(assemblyRep);
                    }

                    _pathToNuggetIdMapping[assemblyPath] = nuggetId;
                    _logger?.LogWithContext($"Assembly loaded in shared context with isolation: {assembly.FullName}", null);
                    
                    return assembly;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Failed to load assembly in shared context: {assemblyPath}", ex);
                return null;
            }
        }

        /// <summary>
        /// Unloads assembly by removing its nugget from shared context
        /// </summary>
        public bool UnloadAssembly(string assemblyPath)
        {
            try
            {
                if (_pathToNuggetIdMapping.TryGetValue(assemblyPath, out var nuggetId))
                {
                    var success = _sharedContextManager.UnloadNugget(nuggetId);
                    if (success)
                    {
                        _pathToNuggetIdMapping.TryRemove(assemblyPath, out _);
                        
                        // Remove from AssemblyHandler collections
                        var assemblyRep = _assemblyHandler.Assemblies.FirstOrDefault(a => 
                            string.Equals(a.DllLib?.Location, assemblyPath, StringComparison.OrdinalIgnoreCase));
                        if (assemblyRep != null)
                        {
                            _assemblyHandler.Assemblies.Remove(assemblyRep);
                            _assemblyHandler.LoadedAssemblies.Remove(assemblyRep.DllLib);
                        }
                    }
                    return success;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Failed to unload assembly: {assemblyPath}", ex);
                return false;
            }
        }

        /// <summary>
        /// Gets assemblies by file type - reads from AssemblyHandler properties
        /// </summary>
        public List<assemblies_rep> GetAssembliesByType(FolderFileTypes fileType)
        {
            return _assemblyHandler.Assemblies.Where(a => a.FileTypes == fileType).ToList();
        }

        /// <summary>
        /// Checks if an assembly is loaded (either in shared context or traditionally)
        /// </summary>
        public bool IsAssemblyLoaded(string assemblyPath)
        {
            string normalizedPath = Path.GetFullPath(assemblyPath);
            
            // Check shared context
            var existsInSharedContext = _sharedContextManager.GetLoadedNuggets()
                .Any(n => n.LoadedAssemblies.Any(a => 
                    string.Equals(a.Location, normalizedPath, StringComparison.OrdinalIgnoreCase)));
            
            // Check traditional loading
            var existsInHandler = _assemblyHandler.LoadedAssemblies
                .Any(a => string.Equals(a.Location, normalizedPath, StringComparison.OrdinalIgnoreCase));
            
            return existsInSharedContext || existsInHandler;
        }

        /// <summary>
        /// Gets assembly by name - searches both shared context and traditional assemblies
        /// </summary>
        public Assembly GetAssemblyByName(string assemblyName)
        {
            // Search shared context first
            var sharedAssembly = _sharedContextManager.GetSharedAssemblies()
                .FirstOrDefault(a => a.GetName().Name.Equals(assemblyName, StringComparison.OrdinalIgnoreCase));
            
            if (sharedAssembly != null)
                return sharedAssembly;
            
            // Fallback to traditional assemblies
            return _assemblyHandler.LoadedAssemblies
                .FirstOrDefault(a => a.GetName().Name.Equals(assemblyName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets comprehensive loading statistics from both systems
        /// </summary>
        public Dictionary<string, object> GetLoadingStatistics()
        {
            var sharedStats = _sharedContextManager.GetIntegratedStatistics();
            
            return new Dictionary<string, object>
            {
                ["AssemblyHandlerAssemblies"] = _assemblyHandler.Assemblies.Count,
                ["AssemblyHandlerLoadedAssemblies"] = _assemblyHandler.LoadedAssemblies.Count,
                ["SharedContextNuggets"] = sharedStats.GetValueOrDefault("LoadedNuggets", 0),
                ["SharedContextAssemblies"] = sharedStats.GetValueOrDefault("SharedAssemblies", 0),
                ["SharedContextTypes"] = sharedStats.GetValueOrDefault("CachedTypes", 0),
                ["PathToNuggetMappings"] = _pathToNuggetIdMapping.Count,
                ["AssembliesByType"] = _assemblyHandler.Assemblies
                    .GroupBy(a => a.FileTypes)
                    .ToDictionary(g => g.Key.ToString(), g => g.Count()),
                ["CollectibleContexts"] = sharedStats.GetValueOrDefault("LoadContexts", 0)
            };
        }

        /// <summary>
        /// Gets all nugget IDs managed by this assistant
        /// </summary>
        public IEnumerable<string> GetManagedNuggetIds()
        {
            return _pathToNuggetIdMapping.Values.ToList();
        }

        /// <summary>
        /// Unloads all assemblies managed by this assistant
        /// </summary>
        public void UnloadAllManagedAssemblies()
        {
            foreach (var nuggetId in _pathToNuggetIdMapping.Values.ToList())
            {
                try
                {
                    _sharedContextManager.UnloadNugget(nuggetId);
                }
                catch (Exception ex)
                {
                    _logger?.LogWithContext($"Failed to unload nugget during cleanup: {nuggetId}", ex);
                }
            }
            _pathToNuggetIdMapping.Clear();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                UnloadAllManagedAssemblies();
                _disposed = true;
            }
        }
    }
}