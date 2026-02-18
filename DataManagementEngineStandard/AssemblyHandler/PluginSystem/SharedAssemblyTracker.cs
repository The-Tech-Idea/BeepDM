using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TheTechIdea.Beep.Logger;

namespace TheTechIdea.Beep.Tools.PluginSystem
{
    /// <summary>
    /// Tracks which plugins are using which shared assemblies
    /// Enables smart unloading by determining when assemblies can be safely unloaded
    /// </summary>
    public class SharedAssemblyTracker
    {
        private readonly ConcurrentDictionary<Assembly, HashSet<string>> _assemblyUsers = new();
        private readonly ConcurrentDictionary<string, HashSet<Assembly>> _pluginAssemblies = new();
        private readonly object _lock = new object();
        private readonly IDMLogger _logger;

        public SharedAssemblyTracker(IDMLogger logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// Register that a plugin is using an assembly
        /// </summary>
        public void RegisterUsage(Assembly assembly, string pluginId)
        {
            if (assembly == null || string.IsNullOrWhiteSpace(pluginId))
                return;

            lock (_lock)
            {
                // Track assembly → plugins
                if (!_assemblyUsers.ContainsKey(assembly))
                {
                    _assemblyUsers[assembly] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                }
                _assemblyUsers[assembly].Add(pluginId);

                // Track plugin → assemblies
                if (!_pluginAssemblies.ContainsKey(pluginId))
                {
                    _pluginAssemblies[pluginId] = new HashSet<Assembly>();
                }
                _pluginAssemblies[pluginId].Add(assembly);

                _logger?.LogWithContext($"Registered assembly usage: {assembly.GetName().Name} by {pluginId}", null);
            }
        }

        /// <summary>
        /// Unregister a plugin's usage of an assembly
        /// </summary>
        public void UnregisterUsage(Assembly assembly, string pluginId)
        {
            if (assembly == null || string.IsNullOrWhiteSpace(pluginId))
                return;

            lock (_lock)
            {
                if (_assemblyUsers.TryGetValue(assembly, out var users))
                {
                    users.Remove(pluginId);
                    if (users.Count == 0)
                    {
                        _assemblyUsers.TryRemove(assembly, out _);
                    }
                }

                if (_pluginAssemblies.TryGetValue(pluginId, out var assemblies))
                {
                    assemblies.Remove(assembly);
                }

                _logger?.LogWithContext($"Unregistered assembly usage: {assembly.GetName().Name} by {pluginId}", null);
            }
        }

        /// <summary>
        /// Unregister all assemblies for a plugin
        /// </summary>
        public void UnregisterPlugin(string pluginId)
        {
            if (string.IsNullOrWhiteSpace(pluginId))
                return;

            lock (_lock)
            {
                if (_pluginAssemblies.TryGetValue(pluginId, out var assemblies))
                {
                    foreach (var assembly in assemblies.ToList())
                    {
                        UnregisterUsage(assembly, pluginId);
                    }
                    _pluginAssemblies.TryRemove(pluginId, out _);
                }

                _logger?.LogWithContext($"Unregistered all assemblies for plugin: {pluginId}", null);
            }
        }

        /// <summary>
        /// Check if an assembly can be safely unloaded
        /// </summary>
        public bool CanUnload(Assembly assembly)
        {
            lock (_lock)
            {
                return !_assemblyUsers.ContainsKey(assembly) || 
                       _assemblyUsers[assembly].Count == 0;
            }
        }

        /// <summary>
        /// Get all plugins using an assembly
        /// </summary>
        public List<string> GetUsers(Assembly assembly)
        {
            lock (_lock)
            {
                if (_assemblyUsers.TryGetValue(assembly, out var users))
                {
                    return users.ToList();
                }
                return new List<string>();
            }
        }

        /// <summary>
        /// Get all assemblies used by a plugin
        /// </summary>
        public List<Assembly> GetAssemblies(string pluginId)
        {
            lock (_lock)
            {
                if (_pluginAssemblies.TryGetValue(pluginId, out var assemblies))
                {
                    return assemblies.ToList();
                }
                return new List<Assembly>();
            }
        }

        /// <summary>
        /// Get reference count for an assembly
        /// </summary>
        public int GetReferenceCount(Assembly assembly)
        {
            lock (_lock)
            {
                if (_assemblyUsers.TryGetValue(assembly, out var users))
                {
                    return users.Count;
                }
                return 0;
            }
        }

        /// <summary>
        /// Check if a plugin can be safely unloaded
        /// </summary>
        public bool CanUnloadPlugin(string pluginId)
        {
            lock (_lock)
            {
                if (!_pluginAssemblies.TryGetValue(pluginId, out var assemblies))
                {
                    return true; // No assemblies tracked, safe to unload
                }

                // Check if all assemblies are either:
                // 1. Only used by this plugin (unique)
                // 2. Not used at all
                foreach (var assembly in assemblies)
                {
                    if (_assemblyUsers.TryGetValue(assembly, out var users))
                    {
                        // If more than this plugin uses it, can't unload safely
                        if (users.Count > 1 || (users.Count == 1 && !users.Contains(pluginId)))
                        {
                            return false;
                        }
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// Get assemblies that are shared vs unique to a plugin
        /// </summary>
        public (List<Assembly> shared, List<Assembly> unique) GetSharedAndUniqueAssemblies(string pluginId)
        {
            var shared = new List<Assembly>();
            var unique = new List<Assembly>();

            lock (_lock)
            {
                if (!_pluginAssemblies.TryGetValue(pluginId, out var assemblies))
                {
                    return (shared, unique);
                }

                foreach (var assembly in assemblies)
                {
                    var refCount = GetReferenceCount(assembly);
                    if (refCount > 1)
                    {
                        shared.Add(assembly);
                    }
                    else if (refCount == 1)
                    {
                        unique.Add(assembly);
                    }
                }
            }

            return (shared, unique);
        }

        /// <summary>
        /// Get plugins that would be affected if we unload this plugin's shared assemblies
        /// </summary>
        public List<string> GetAffectedPlugins(string pluginId)
        {
            var affected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            lock (_lock)
            {
                if (!_pluginAssemblies.TryGetValue(pluginId, out var assemblies))
                {
                    return new List<string>();
                }

                foreach (var assembly in assemblies)
                {
                    if (_assemblyUsers.TryGetValue(assembly, out var users))
                    {
                        foreach (var user in users.Where(u => !u.Equals(pluginId, StringComparison.OrdinalIgnoreCase)))
                        {
                            affected.Add(user);
                        }
                    }
                }
            }

            return affected.ToList();
        }

        /// <summary>
        /// Get statistics about assembly usage
        /// </summary>
        public AssemblyUsageStatistics GetStatistics()
        {
            lock (_lock)
            {
                var stats = new AssemblyUsageStatistics
                {
                    TotalPlugins = _pluginAssemblies.Count,
                    TotalAssemblies = _assemblyUsers.Count,
                    SharedAssemblies = _assemblyUsers.Count(kvp => kvp.Value.Count > 1),
                    UniqueAssemblies = _assemblyUsers.Count(kvp => kvp.Value.Count == 1)
                };

                return stats;
            }
        }

        /// <summary>
        /// Clear all tracking data
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _assemblyUsers.Clear();
                _pluginAssemblies.Clear();
                _logger?.LogWithContext("Assembly tracker cleared", null);
            }
        }
    }

    /// <summary>
    /// Statistics about assembly usage
    /// </summary>
    public class AssemblyUsageStatistics
    {
        public int TotalPlugins { get; set; }
        public int TotalAssemblies { get; set; }
        public int SharedAssemblies { get; set; }
        public int UniqueAssemblies { get; set; }

        public override string ToString()
        {
            return $"Plugins: {TotalPlugins}, Assemblies: {TotalAssemblies} " +
                   $"(Shared: {SharedAssemblies}, Unique: {UniqueAssemblies})";
        }
    }
}

