using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using TheTechIdea.Beep.Logger;

namespace TheTechIdea.Beep.Tools.PluginSystem
{
    public class InstalledPluginInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string Source { get; set; }
        public string InstallPath { get; set; }
        public string State { get; set; } // Loaded/Unloaded/Installed
        public DateTime InstalledAt { get; set; }
    }

    public class PluginRegistry
    {
        private readonly string _registryPath;
        private readonly IDMLogger _logger;
        private readonly object _lock = new object();
        private Dictionary<string, InstalledPluginInfo> _plugins = new(StringComparer.OrdinalIgnoreCase);

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

        public IEnumerable<InstalledPluginInfo> GetInstalledPlugins()
        {
            lock (_lock) return _plugins.Values.ToList();
        }

        public InstalledPluginInfo GetPlugin(string id)
        {
            lock (_lock) return _plugins.GetValueOrDefault(id);
        }

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

        public void Unregister(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return;
            lock (_lock)
            {
                if (_plugins.Remove(id)) Save();
            }
            _logger?.LogWithContext($"Unregistered plugin {id}", null);
        }

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
