using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.NuGet;

namespace TheTechIdea.Beep.NuGetManagement.Services
{
    public class LoadService
    {
        private readonly IDMLogger _logger;
        private readonly IAssemblyLoadContext _loadContext;
        private readonly Dictionary<string, Assembly> _loadedAssemblies = new Dictionary<string, Assembly>();
        private readonly object _lock = new object();

        public LoadService(IDMLogger logger, IAssemblyLoadContext loadContext)
        {
            _logger = logger;
            _loadContext = loadContext ?? throw new ArgumentNullException(nameof(loadContext));
        }

        public async Task<NuggetInfo> LoadAsync(string packagePath, string packageId, string version, bool useSharedContext = true)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(packagePath) || !Directory.Exists(packagePath))
                {
                    _logger?.LogWithContext($"Invalid package path for loading: {packagePath}", null);
                    return null;
                }

                // Load via IAssemblyLoadContext
                var nuggetInfo = await _loadContext.LoadPackageAsync(packagePath, packageId);
                
                if (nuggetInfo?.LoadedAssemblies != null && nuggetInfo.LoadedAssemblies.Count > 0)
                {
                    lock (_lock)
                    {
                        foreach (var assembly in nuggetInfo.LoadedAssemblies)
                        {
                            var assemblyName = assembly.GetName().Name;
                            if (!_loadedAssemblies.ContainsKey(assemblyName))
                            {
                                _loadedAssemblies[assemblyName] = assembly;
                            }
                        }
                    }

                    _logger?.LogWithContext($"Loaded {packageId} with {nuggetInfo.LoadedAssemblies.Count} assemblies", nuggetInfo);
                }
                else
                {
                    _logger?.LogWithContext($"Failed to load package {packageId} - no assemblies loaded", null);
                }

                return nuggetInfo;
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Error loading package {packageId}: {ex.Message}", ex);
                return null;
            }
        }

        public bool IsLoaded(string packageId)
        {
            lock (_lock)
            {
                return _loadedAssemblies.ContainsKey(packageId);
            }
        }

        public Assembly GetLoadedAssembly(string assemblyName)
        {
            lock (_lock)
            {
                _loadedAssemblies.TryGetValue(assemblyName, out var assembly);
                return assembly;
            }
        }

        public List<Assembly> GetAllLoadedAssemblies()
        {
            lock (_lock)
            {
                return _loadedAssemblies.Values.ToList();
            }
        }

        public async Task<bool> UnloadAsync(string packageId)
        {
            try
            {
                if (!_loadContext.IsPackageLoaded(packageId))
                {
                    _logger?.LogWithContext($"Package {packageId} not found for unloading", null);
                    return false;
                }

                var result = _loadContext.UnloadPackage(packageId);
                
                if (result)
                {
                    lock (_lock)
                    {
                        var toRemove = _loadedAssemblies.Keys.Where(k => k.Contains(packageId)).ToList();
                        foreach (var key in toRemove)
                        {
                            _loadedAssemblies.Remove(key);
                        }
                    }
                    _logger?.LogWithContext($"Unloaded package {packageId}", null);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Error unloading package {packageId}: {ex.Message}", ex);
                return false;
            }
        }
    }
}
