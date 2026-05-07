using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Tools;
using TheTechIdea.Beep.Tools.PluginSystem;

namespace TheTechIdea.Beep.NuGetManagement.Services
{
    public class LoadService
    {
        private readonly IDMLogger _logger;
        private readonly SharedContextManager _sharedContextManager;
        private readonly Dictionary<string, Assembly> _loadedAssemblies = new Dictionary<string, Assembly>();
        private readonly object _lock = new object();

        public LoadService(IDMLogger logger, SharedContextManager sharedContextManager)
        {
            _logger = logger;
            _sharedContextManager = sharedContextManager;
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

                // Set shared context mode
                if (_sharedContextManager != null && _sharedContextManager.IsSingleSharedContextMode != useSharedContext)
                {
                    await _sharedContextManager.SetSharedContextModeAsync(useSharedContext);
                }

                // Load via SharedContextManager
                var nuggetInfo = await _sharedContextManager.LoadNuggetAsync(packagePath, $"NuGet_{packageId}_{DateTime.UtcNow.Ticks}");
                
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
                var nuggetId = _sharedContextManager.GetLoadedNuggets()
                    .FirstOrDefault(n => n.Id.Contains(packageId))?.Id;

                if (string.IsNullOrEmpty(nuggetId))
                {
                    _logger?.LogWithContext($"Package {packageId} not found for unloading", null);
                    return false;
                }

                var result = _sharedContextManager.UnloadNugget(nuggetId);
                
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
