using System;
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
    /// Helper class that downloads NuGet packages (nuggets) and loads them into the shared context
    /// using AssemblyLoadingAssistant. It avoids creating references to BeepShell.Shared.
    /// </summary>
    public class NuggetPluginLoader
    {
        private readonly NuggetPackageDownloader _downloader;
        private readonly AssemblyLoadingAssistant _loaderAssistant;
        private readonly IDMLogger _logger;
        private readonly PluginRegistry _registry;
        private readonly PluginProcessManager _processManager;

        public NuggetPluginLoader(NuggetPackageDownloader downloader, AssemblyLoadingAssistant loaderAssistant, PluginRegistry registry, IDMLogger logger, PluginProcessManager processManager = null)
        {
            _downloader = downloader ?? throw new ArgumentNullException(nameof(downloader));
            _loaderAssistant = loaderAssistant ?? throw new ArgumentNullException(nameof(loaderAssistant));
            _logger = logger;
            _registry = registry;
            _processManager = processManager ?? new PluginProcessManager(logger);
        }

        /// <summary>
        /// Downloads a nugget and dependencies, extracts each package and loads assemblies from the package directories
        /// using the AssemblyLoadingAssistant.
        /// </summary>
        public async Task<List<Assembly>> LoadNuggetAsPluginAsync(string packageName, string? version = null, IEnumerable<string> sources = null, bool useSingleSharedContext = true, string? appInstallPath = null, bool useProcessHost = false)
        {
            var loaded = new List<Assembly>();
            if (string.IsNullOrWhiteSpace(packageName)) return loaded;

                        try
            {
                // Ensure shared context mode is set according to preference
                try { await _loaderAssistant.SetSharedContextModeAsync(useSingleSharedContext); } catch { }

                var packages = await _downloader.DownloadPackageWithDependenciesAsync(packageName, version, sources);
                foreach (var kvp in packages)
                {
                    string package = kvp.Key;
                    string path = kvp.Value; // this is the compatible framework folder (DLLs) or package root

                    try
                    {
                        // Optionally copy to app directory for runtime resolution
                        if (!string.IsNullOrWhiteSpace(appInstallPath))
                        {
                            try { _downloader.InstallPackageToAppDirectory(path, appInstallPath); } catch { }
                        }
                        // If an appInstallPath is provided, determine plugin id and version and create manifest & register
                        if (!string.IsNullOrWhiteSpace(appInstallPath))
                        {
                            try
                            {
                                // Derive plugin id and version where possible
                                var pluginId = package; // default to package name
                                var pluginVersion = version ?? "latest";
                                var installDir = _downloader.InstallPackageToAppDirectory(path, appInstallPath, pluginId, pluginVersion, overwrite: false);
                                if (!string.IsNullOrWhiteSpace(installDir))
                                {
                                    var manifest = new PluginManifest { Id = pluginId, Name = pluginId, Version = pluginVersion, Source = (sources != null ? string.Join(';', sources) : "nuget"), Signed = false };
                                    var manifestPath = Path.Combine(installDir, "plugin.json");
                                    try { File.WriteAllText(manifestPath, System.Text.Json.JsonSerializer.Serialize(manifest, new System.Text.Json.JsonSerializerOptions { WriteIndented = true })); } catch { }
                                    try
                                    {
                                        _registry?.Register(new InstalledPluginInfo { Id = pluginId, Name = manifest.Name, Version = manifest.Version, Source = manifest.Source, InstallPath = installDir, State = "Installed" });
                                    }
                                    catch (Exception exr) { _logger?.LogWithContext("Failed to register plugin", exr); }
                                    if (useProcessHost)
                                    {
                                        // If an executable exists in installed folder, start it as a host
                                        var exe = Directory.GetFiles(installDir, "*.exe", SearchOption.AllDirectories).FirstOrDefault();
                                        if (!string.IsNullOrWhiteSpace(exe))
                                        {
                                            try
                                            {
                                                var pInfo = _processManager?.StartPluginProcess(pluginId, installDir, exe);
                                                if (pInfo != null)
                                                {
                                                    _logger?.LogWithContext($"Started plugin process for {pluginId} from {exe}", null);
                                                    _registry?.UpdateState(pluginId, "RunningInProcess");
                                                }
                                            }
                                            catch (Exception exProc)
                                            {
                                                _logger?.LogWithContext($"Failed to start plugin process for {pluginId}", exProc);
                                            }
                                        }
                                    }
                                }
                            }
                            catch { }
                        }

                        // Load via AssemblyLoadingAssistant - the helper adds to assembly handler and returns a status string
                        var status = _loaderAssistant.LoadAssembly(path, FolderFileTypes.Nugget);
                        _logger?.LogWithContext($"Loaded nugget package {package}: {status}", null);

                        // Find assemblies that were loaded from this path
                        var assemblies = _loaderAssistant.GetAssembliesByType(FolderFileTypes.Nugget)
                            .Where(a => a.DllLib != null && a.DllLibPath != null && Path.GetFullPath(a.DllLibPath).StartsWith(Path.GetFullPath(path), StringComparison.OrdinalIgnoreCase))
                            .Select(a => a.DllLib)
                            .ToList();

                        foreach (var asm in assemblies)
                        {
                            if (!loaded.Contains(asm)) loaded.Add(asm);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWithContext($"Failed to load assemblies from package folder {path}: {ex.Message}", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Failed to download and load nugget: {ex.Message}", ex);
            }

            return loaded;
        }
    }
}
