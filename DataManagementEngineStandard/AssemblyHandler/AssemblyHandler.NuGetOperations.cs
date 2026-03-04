using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Versioning;
using TheTechIdea.Beep.Tools.PluginSystem;

namespace TheTechIdea.Beep.Tools
{
    /// <summary>
    /// AssemblyHandler partial class - NuGet Search, Download, and Package Operations
    /// Implements the new IAssemblyHandler NuGet members.
    /// </summary>
    public partial class AssemblyHandler
    {
        #region NuGet Search & Download (IAssemblyHandler)

        /// <summary>
        /// Searches NuGet for packages matching a keyword.
        /// </summary>
        public async Task<List<NuGetSearchResult>> SearchNuGetPackagesAsync(
            string searchTerm,
            int skip = 0,
            int take = 20,
            bool includePrerelease = false,
            CancellationToken token = default)
        {
            try
            {
                var sources = GetActiveSourceUrls();
                return await _packageDownloader.SearchPackagesAsync(searchTerm, skip, take, includePrerelease, sources, token);
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"SearchNuGetPackagesAsync: Error - {ex.Message}");
                return new List<NuGetSearchResult>();
            }
        }

        /// <summary>
        /// Gets all available versions for a NuGet package.
        /// </summary>
        public async Task<List<string>> GetNuGetPackageVersionsAsync(
            string packageId,
            bool includePrerelease = false,
            CancellationToken token = default)
        {
            try
            {
                var sources = GetActiveSourceUrls();
                var versions = await _packageDownloader.GetPackageVersionsAsync(packageId, includePrerelease, sources, token);
                return versions.Select(v => v.ToNormalizedString()).ToList();
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"GetNuGetPackageVersionsAsync: Error - {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// Downloads and loads a NuGet package with all its dependencies.
        /// </summary>
        public async Task<List<Assembly>> LoadNuggetFromNuGetAsync(
            string packageName,
            string version = null,
            IEnumerable<string> sources = null,
            bool useSingleSharedContext = true,
            string appInstallPath = null,
            bool useProcessHost = false)
        {
            var loadedAssemblies = new List<Assembly>();
            try
            {
                var sourceList = sources?.ToList() ?? GetActiveSourceUrls();

                var results = await _packageDownloader.DownloadPackageWithDependenciesAsync(packageName, version, sourceList);

                foreach (var kvp in results)
                {
                    var path = kvp.Value;
                    var useIsolated = !useSingleSharedContext;
                    var loaded = _nuggetManager.LoadNugget(path, useIsolated);
                    if (loaded)
                    {
                        var nuggetName = Path.GetFileNameWithoutExtension(path.TrimEnd(Path.DirectorySeparatorChar));
                        var nusAssemblies = _nuggetManager.GetNuggetAssemblies(nuggetName);
                        foreach (var a in nusAssemblies)
                        {
                            if (!LoadedAssemblies.Contains(a)) LoadedAssemblies.Add(a);
                            if (!loadedAssemblies.Contains(a)) loadedAssemblies.Add(a);
                        }

                        // Optionally install DLLs to application directory
                        try
                        {
                            var installPath = appInstallPath ?? Path.Combine(ConfigEditor.ExePath, "Plugins");
                            _packageDownloader.InstallPackageToAppDirectory(path, installPath, packageName, version);
                            if (useProcessHost)
                            {
                                var exe = Directory.GetFiles(installPath, "*.exe", SearchOption.AllDirectories).FirstOrDefault();
                                if (!string.IsNullOrWhiteSpace(exe))
                                {
                                    try
                                    {
                                        var psi = new System.Diagnostics.ProcessStartInfo { FileName = exe, UseShellExecute = false, CreateNoWindow = true };
                                        System.Diagnostics.Process.Start(psi);
                                        Logger?.WriteLog($"Started process-hosted plugin {packageName} @ {exe}");
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger?.WriteLog($"Failed to start process-hosted plugin: {ex.Message}");
                                    }
                                }
                            }
                        }
                        catch { }

                        RecordNuGetSuccess();
                    }
                    else
                    {
                        RecordNuGetFailure();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"LoadNuggetFromNuGetAsync: Error - {ex.Message}");
                RecordNuGetFailure();
            }

            return loadedAssemblies;
        }

        #endregion
    }
}
