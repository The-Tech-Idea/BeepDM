using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Tools;

namespace TheTechIdea.Beep.Tools.PluginSystem
{
    /// <summary>
    /// Manages hot-swapping and versioning of plugins
    /// </summary>
    public class PluginVersionManager : IDisposable
    {
        private readonly ConcurrentDictionary<string, List<string>> _pluginVersionHistory = new();
        private readonly ConcurrentDictionary<string, PluginBackup> _pluginBackups = new();
        private readonly PluginIsolationManager _isolationManager;
        private readonly PluginLifecycleManager _lifecycleManager;
        private readonly IDMLogger _logger;
        private bool _disposed = false;

        public PluginVersionManager(PluginIsolationManager isolationManager, 
                                  PluginLifecycleManager lifecycleManager, 
                                  IDMLogger logger)
        {
            _isolationManager = isolationManager;
            _lifecycleManager = lifecycleManager;
            _logger = logger;
        }

        /// <summary>
        /// Replaces a plugin with a new version at runtime
        /// </summary>
        public async Task<bool> ReplacePluginAsync(string pluginId, string newAssemblyPath, string newVersion = null)
        {
            if (string.IsNullOrWhiteSpace(pluginId) || string.IsNullOrWhiteSpace(newAssemblyPath))
                return false;

            if (!File.Exists(newAssemblyPath))
            {
                _logger?.LogWithContext($"New assembly file not found: {newAssemblyPath}", null);
                return false;
            }

            try
            {
                // Get current plugin info
                var currentPlugin = _isolationManager.GetPlugin(pluginId);
                if (currentPlugin == null)
                {
                    _logger?.LogWithContext($"Plugin not found for replacement: {pluginId}", null);
                    return false;
                }

                // Create backup of current plugin
                var backup = await CreatePluginBackupAsync(currentPlugin);
                if (backup == null)
                {
                    _logger?.LogWithContext($"Failed to create backup for plugin: {pluginId}", null);
                    return false;
                }

                try
                {
                    // Stop current plugin if running
                    if (currentPlugin.State == PluginState.Started)
                    {
                        _lifecycleManager.StopPlugin(pluginId);
                    }

                    // Unload current plugin
                    _isolationManager.UnloadPlugin(pluginId);

                    // Load new plugin version
                    var newPlugin = await _isolationManager.LoadPluginWithIsolationAsync(newAssemblyPath, pluginId);
                    if (newPlugin == null)
                    {
                        _logger?.LogWithContext($"Failed to load new plugin version: {newAssemblyPath}", null);
                        
                        // Restore from backup
                        await RestoreFromBackupAsync(backup);
                        return false;
                    }

                    // Update version information
                    if (!string.IsNullOrWhiteSpace(newVersion))
                    {
                        newPlugin.Version = newVersion;
                    }

                    // Register with lifecycle manager
                    _lifecycleManager.RegisterPlugin(newPlugin);

                    // Update version history
                    if (!_pluginVersionHistory.ContainsKey(pluginId))
                    {
                        _pluginVersionHistory[pluginId] = new List<string>();
                    }
                    _pluginVersionHistory[pluginId].Add(newPlugin.Version);

                    // Store backup for potential rollback
                    _pluginBackups[pluginId] = backup;

                    _logger?.LogWithContext($"Plugin replaced successfully: {pluginId} -> version {newPlugin.Version}", null);
                    
                    return true;
                }
                catch (Exception ex)
                {
                    _logger?.LogWithContext($"Failed to replace plugin {pluginId}, attempting rollback", ex);
                    
                    // Attempt to restore from backup
                    await RestoreFromBackupAsync(backup);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Failed to replace plugin: {pluginId}", ex);
                return false;
            }
        }

        /// <summary>
        /// Rolls back to a previous plugin version
        /// </summary>
        public async Task<bool> RollbackPluginAsync(string pluginId)
        {
            if (string.IsNullOrWhiteSpace(pluginId))
                return false;

            try
            {
                // Get backup for rollback
                if (!_pluginBackups.TryGetValue(pluginId, out PluginBackup backup))
                {
                    _logger?.LogWithContext($"No backup available for rollback: {pluginId}", null);
                    return false;
                }

                // Get current plugin
                var currentPlugin = _isolationManager.GetPlugin(pluginId);
                if (currentPlugin != null)
                {
                    // Stop current plugin if running
                    if (currentPlugin.State == PluginState.Started)
                    {
                        _lifecycleManager.StopPlugin(pluginId);
                    }

                    // Unload current plugin
                    _isolationManager.UnloadPlugin(pluginId);
                }

                // Restore from backup
                return await RestoreFromBackupAsync(backup);
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Failed to rollback plugin: {pluginId}", ex);
                return false;
            }
        }

        /// <summary>
        /// Gets version history for a plugin
        /// </summary>
        public List<string> GetPluginVersionHistory(string pluginId)
        {
            return _pluginVersionHistory.GetValueOrDefault(pluginId, new List<string>());
        }

        /// <summary>
        /// Creates a snapshot of the current plugin state
        /// </summary>
        public async Task<bool> CreatePluginSnapshotAsync(string pluginId, string snapshotName)
        {
            if (string.IsNullOrWhiteSpace(pluginId) || string.IsNullOrWhiteSpace(snapshotName))
                return false;

            try
            {
                var plugin = _isolationManager.GetPlugin(pluginId);
                if (plugin == null)
                    return false;

                var backup = await CreatePluginBackupAsync(plugin);
                if (backup == null)
                    return false;

                backup.SnapshotName = snapshotName;
                backup.CreatedAt = DateTime.UtcNow;

                // Store snapshot with unique key
                var snapshotKey = $"{pluginId}_{snapshotName}";
                _pluginBackups[snapshotKey] = backup;

                _logger?.LogWithContext($"Plugin snapshot created: {pluginId} -> {snapshotName}", null);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Failed to create plugin snapshot: {pluginId}", ex);
                return false;
            }
        }

        /// <summary>
        /// Restores plugin from a named snapshot
        /// </summary>
        public async Task<bool> RestoreFromSnapshotAsync(string pluginId, string snapshotName)
        {
            if (string.IsNullOrWhiteSpace(pluginId) || string.IsNullOrWhiteSpace(snapshotName))
                return false;

            try
            {
                var snapshotKey = $"{pluginId}_{snapshotName}";
                if (!_pluginBackups.TryGetValue(snapshotKey, out PluginBackup backup))
                {
                    _logger?.LogWithContext($"Snapshot not found: {pluginId} -> {snapshotName}", null);
                    return false;
                }

                // Get current plugin
                var currentPlugin = _isolationManager.GetPlugin(pluginId);
                if (currentPlugin != null)
                {
                    // Stop current plugin if running
                    if (currentPlugin.State == PluginState.Started)
                    {
                        _lifecycleManager.StopPlugin(pluginId);
                    }

                    // Unload current plugin
                    _isolationManager.UnloadPlugin(pluginId);
                }

                // Restore from snapshot
                return await RestoreFromBackupAsync(backup);
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Failed to restore from snapshot: {pluginId} -> {snapshotName}", ex);
                return false;
            }
        }

        // Helper methods
        private async Task<PluginBackup> CreatePluginBackupAsync(PluginInfo plugin)
        {
            try
            {
                var backup = new PluginBackup
                {
                    PluginId = plugin.Id,
                    OriginalPath = GetPluginAssemblyPath(plugin),
                    Version = plugin.Version,
                    State = plugin.State,
                    Health = plugin.Health,
                    Metadata = new Dictionary<string, object>(plugin.Metadata),
                    CreatedAt = DateTime.UtcNow
                };

                // Create backup copy of assembly file
                if (!string.IsNullOrWhiteSpace(backup.OriginalPath) && File.Exists(backup.OriginalPath))
                {
                    var backupDir = Path.Combine(Path.GetTempPath(), "PluginBackups", plugin.Id);
                    Directory.CreateDirectory(backupDir);
                    
                    backup.BackupPath = Path.Combine(backupDir, $"{plugin.Id}_{plugin.Version}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.dll");
                    
                    await Task.Run(() => File.Copy(backup.OriginalPath, backup.BackupPath, true));
                }

                return backup;
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Failed to create plugin backup: {plugin.Id}", ex);
                return null;
            }
        }

        private async Task<bool> RestoreFromBackupAsync(PluginBackup backup)
        {
            try
            {
                if (backup == null || string.IsNullOrWhiteSpace(backup.BackupPath) || !File.Exists(backup.BackupPath))
                {
                    _logger?.LogWithContext($"Invalid backup for restoration: {backup?.PluginId}", null);
                    return false;
                }

                // Load plugin from backup
                var restoredPlugin = await _isolationManager.LoadPluginWithIsolationAsync(backup.BackupPath, backup.PluginId);
                if (restoredPlugin == null)
                {
                    _logger?.LogWithContext($"Failed to load plugin from backup: {backup.PluginId}", null);
                    return false;
                }

                // Restore metadata
                restoredPlugin.Version = backup.Version;
                foreach (var kvp in backup.Metadata)
                {
                    restoredPlugin.Metadata[kvp.Key] = kvp.Value;
                }

                // Register with lifecycle manager
                _lifecycleManager.RegisterPlugin(restoredPlugin);

                _logger?.LogWithContext($"Plugin restored from backup: {backup.PluginId}", null);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Failed to restore plugin from backup: {backup?.PluginId}", ex);
                return false;
            }
        }

        private string GetPluginAssemblyPath(PluginInfo plugin)
        {
            try
            {
                return plugin.Assembly?.Location;
            }
            catch
            {
                return null;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                // Clean up backup files
                foreach (var backup in _pluginBackups.Values)
                {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(backup.BackupPath) && File.Exists(backup.BackupPath))
                        {
                            File.Delete(backup.BackupPath);
                        }
                    }
                    catch { } // Ignore cleanup errors
                }

                _pluginVersionHistory.Clear();
                _pluginBackups.Clear();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Represents a plugin backup for rollback purposes
    /// </summary>
    public class PluginBackup
    {
        public string PluginId { get; set; }
        public string OriginalPath { get; set; }
        public string BackupPath { get; set; }
        public string Version { get; set; }
        public PluginState State { get; set; }
        public PluginHealth Health { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public string SnapshotName { get; set; }
    }
}