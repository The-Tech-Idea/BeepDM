using System;
using System.IO;
using System.Linq;
using TheTechIdea.Beep.Logger;

namespace TheTechIdea.Beep.Tools.PluginSystem
{
    /// <summary>
    /// Removes installed plugins from disk and unregisters them from the local plugin registry.
    /// </summary>
    public class PluginInstaller
    {
        private readonly IDMLogger _logger;
        private readonly PluginRegistry _registry;

        /// <summary>
        /// Initializes a plugin installer bound to a registry and logger.
        /// </summary>
        /// <param name="registry">Registry that tracks installed plugin metadata.</param>
        /// <param name="logger">Logger used for uninstall diagnostics.</param>
        public PluginInstaller(PluginRegistry registry, IDMLogger logger)
        {
            _registry = registry;
            _logger = logger;
        }

        /// <summary>
        /// Uninstalls a plugin from disk and removes its registry entry.
        /// </summary>
        /// <param name="pluginId">Logical plugin identifier.</param>
        /// <param name="version">Optional version hint retained for API compatibility.</param>
        /// <param name="force">Whether to continue removal even when the plugin is loaded or file deletion partially fails.</param>
        /// <returns><see langword="true"/> when the plugin is removed or already absent from disk and successfully unregistered; otherwise, <see langword="false"/>.</returns>
        public bool Uninstall(string pluginId, string version = null, bool force = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(pluginId)) return false;
                var installed = _registry?.GetPlugin(pluginId);
                if (installed == null) return false;
                var path = installed.InstallPath;
                if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
                {
                    _registry?.Unregister(pluginId);
                    return true;
                }

                // If plugin is loaded, and not forced, do not remove
                if (installed.State == "Loaded" && !force)
                {
                    _logger?.LogWithContext($"Plugin {pluginId} is loaded; aborting uninstall (use force=true to override)", null);
                    return false;
                }

                try
                {
                    Directory.Delete(path, true);
                }
                catch (Exception ex)
                {
                    _logger?.LogWithContext($"Failed to delete plugin directory {path}", ex);
                    if (!force) return false;
                }

                _registry?.Unregister(pluginId);
                _logger?.LogWithContext($"Plugin {pluginId} uninstalled from {path}", null);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Uninstall failed for {pluginId}", ex);
                return false;
            }
        }
    }
}
