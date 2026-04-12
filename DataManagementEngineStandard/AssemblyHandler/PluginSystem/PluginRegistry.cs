using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using TheTechIdea.Beep.Logger;

namespace TheTechIdea.Beep.Tools.PluginSystem
{
    /// <summary>
    /// Persisted metadata describing an installed plugin.
    /// </summary>
    public class InstalledPluginInfo
    {
        /// <summary>Gets or sets the logical plugin identifier.</summary>
        public string Id { get; set; }
        /// <summary>Gets or sets the display name of the plugin.</summary>
        public string Name { get; set; }
        /// <summary>Gets or sets the installed plugin version.</summary>
        public string Version { get; set; }
        /// <summary>Gets or sets the source package or origin of the plugin.</summary>
        public string Source { get; set; }
        /// <summary>Gets or sets the plugin installation directory.</summary>
        public string InstallPath { get; set; }
        /// <summary>Gets or sets the persisted lifecycle state string.</summary>
        public string State { get; set; } // Loaded/Unloaded/Installed
        /// <summary>Gets or sets when the plugin was registered as installed.</summary>
        public DateTime InstalledAt { get; set; }
    }

    /// <summary>
    /// Stores installed plugin metadata on disk in a JSON registry file.
    /// </summary>
    public class PluginRegistry
    {
        private readonly string _registryPath;
        private readonly IDMLogger _logger;
        private readonly object _lock = new object();
        private Dictionary<string, InstalledPluginInfo> _plugins = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initializes a plugin registry rooted under the supplied base path.
        /// </summary>
        /// <param name="basePath">Base directory used to resolve the registry file path.</param>
        /// <param name="logger">Logger used for registry diagnostics.</param>
        public PluginRegistry(string basePath, IDMLogger logger)
        {
            _logger = logger;
            _registryPath = Path.Combine(basePath ?? AppContext.BaseDirectory, "plugins_registry.json");
            Load();
        }

        private void Load()
        {
            try
            {
                if (!File.Exists(_registryPath)) return;
                var json = File.ReadAllText(_registryPath);
                var list = JsonSerializer.Deserialize<List<InstalledPluginInfo>>(json);
                if (list != null) _plugins = list.ToDictionary(p => p.Id, p => p, StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext("Failed to load plugin registry", ex);
            }
        }

        private void Save()
        {
            try
            {
                lock (_lock)
                {
                    var json = JsonSerializer.Serialize(_plugins.Values.ToList(), new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(_registryPath, json);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext("Failed to save plugin registry", ex);
            }
        }

        /// <summary>
        /// Returns a snapshot of all registered plugins.
        /// </summary>
        public IEnumerable<InstalledPluginInfo> GetInstalledPlugins()
        {
            lock (_lock) return _plugins.Values.ToList();
        }

        /// <summary>
        /// Returns a specific plugin entry by identifier.
        /// </summary>
        /// <param name="id">Logical plugin identifier.</param>
        /// <returns>The matching plugin entry, or <see langword="null"/> when not found.</returns>
        public InstalledPluginInfo GetPlugin(string id)
        {
            lock (_lock) return _plugins.GetValueOrDefault(id);
        }

        /// <summary>
        /// Adds or replaces a plugin entry in the registry.
        /// </summary>
        /// <param name="info">Plugin metadata to persist.</param>
        public void Register(InstalledPluginInfo info)
        {
            if (info == null || string.IsNullOrWhiteSpace(info.Id)) return;
            lock (_lock)
            {
                info.InstalledAt = DateTime.UtcNow;
                _plugins[info.Id] = info;
                Save();
            }
            _logger?.LogWithContext($"Registered plugin {info.Id} v{info.Version} @ {info.InstallPath}", null);
        }

        /// <summary>
        /// Removes a plugin entry from the registry.
        /// </summary>
        /// <param name="id">Logical plugin identifier.</param>
        public void Unregister(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return;
            lock (_lock)
            {
                if (_plugins.Remove(id)) Save();
            }
            _logger?.LogWithContext($"Unregistered plugin {id}", null);
        }

        /// <summary>
        /// Updates the persisted state of a registered plugin.
        /// </summary>
        /// <param name="id">Logical plugin identifier.</param>
        /// <param name="state">New state string to persist.</param>
        public void UpdateState(string id, string state)
        {
            if (string.IsNullOrWhiteSpace(id)) return;
            lock (_lock)
            {
                if (_plugins.TryGetValue(id, out var info))
                {
                    info.State = state;
                    Save();
                }
            }
        }
    }
}
