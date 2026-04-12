using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using TheTechIdea.Beep.Logger;

namespace TheTechIdea.Beep.Tools.PluginSystem
{
    /// <summary>
    /// Runtime metadata for an out-of-process plugin host.
    /// </summary>
    public class PluginProcessInfo
    {
        /// <summary>Gets or sets the logical plugin identifier.</summary>
        public string PluginId { get; set; }
        /// <summary>Gets or sets the plugin version being hosted.</summary>
        public string Version { get; set; }
        /// <summary>Gets or sets the child process instance hosting the plugin.</summary>
        public Process Process { get; set; }
        /// <summary>Gets or sets the IPC pipe name used to communicate with the host.</summary>
        public string PipeName { get; set; }
        /// <summary>Gets or sets the installation directory of the plugin.</summary>
        public string InstallPath { get; set; }
    }

    /// <summary>
    /// Starts and stops external plugin host processes.
    /// </summary>
    public class PluginProcessManager
    {
        private readonly IDMLogger _logger;
        private readonly ConcurrentDictionary<string, PluginProcessInfo> _processes = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initializes a process manager for out-of-process plugins.
        /// </summary>
        /// <param name="logger">Logger used for process host diagnostics.</param>
        public PluginProcessManager(IDMLogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Starts a process to host a plugin.
        /// </summary>
        /// <param name="pluginId">Logical plugin identifier.</param>
        /// <param name="installPath">Plugin installation directory.</param>
        /// <param name="entryAssemblyPath">Executable path used to host the plugin.</param>
        /// <param name="args">Optional command-line arguments for the host process.</param>
        /// <returns>Process metadata when startup succeeds; otherwise <see langword="null"/>.</returns>
        public PluginProcessInfo StartPluginProcess(string pluginId, string installPath, string entryAssemblyPath, string args = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(entryAssemblyPath) || !File.Exists(entryAssemblyPath))
                {
                    _logger?.LogWithContext("Invalid entry assembly for plugin process", null);
                    return null;
                }

                var psi = new ProcessStartInfo
                {
                    FileName = entryAssemblyPath, // if this is an .exe
                    Arguments = args ?? string.Empty,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                var proc = Process.Start(psi);
                var pInfo = new PluginProcessInfo { PluginId = pluginId, Process = proc, InstallPath = installPath };
                _processes[pluginId] = pInfo;
                _logger?.LogWithContext($"Started plugin process {pluginId} PID:{proc?.Id}", null);
                return pInfo;
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Failed to start plugin process for {pluginId}", ex);
                return null;
            }
        }

        /// <summary>
        /// Stops a running plugin host process.
        /// </summary>
        /// <param name="pluginId">Logical plugin identifier.</param>
        /// <param name="force">Whether to forcibly terminate the process instead of requesting a graceful close.</param>
        /// <returns><see langword="true"/> when a tracked process is stopped and removed; otherwise <see langword="false"/>.</returns>
        public bool StopPluginProcess(string pluginId, bool force = false)
        {
            if (string.IsNullOrWhiteSpace(pluginId) || !_processes.TryGetValue(pluginId, out var info)) return false;
            try
            {
                if (info.Process != null && !info.Process.HasExited)
                {
                    if (force) info.Process.Kill(true);
                    else info.Process.CloseMainWindow();
                }
                _processes.TryRemove(pluginId, out _);
                _logger?.LogWithContext($"Stopped plugin process {pluginId}", null);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Failed to stop plugin process {pluginId}", ex);
                return false;
            }
        }
    }
}
