using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using TheTechIdea.Beep.Logger;

namespace TheTechIdea.Beep.Tools.PluginSystem
{
    public class PluginProcessInfo
    {
        public string PluginId { get; set; }
        public string Version { get; set; }
        public Process Process { get; set; }
        public string PipeName { get; set; }
        public string InstallPath { get; set; }
    }

    public class PluginProcessManager
    {
        private readonly IDMLogger _logger;
        private readonly ConcurrentDictionary<string, PluginProcessInfo> _processes = new(StringComparer.OrdinalIgnoreCase);

        public PluginProcessManager(IDMLogger logger)
        {
            _logger = logger;
        }

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
