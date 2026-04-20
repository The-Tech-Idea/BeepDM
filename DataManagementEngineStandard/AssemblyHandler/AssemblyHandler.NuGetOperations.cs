using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DriversConfigurations;
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
                return await _nuggetManager.SearchNuGetPackagesAsync(searchTerm, skip, take, includePrerelease, sources, token);
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
                return await _nuggetManager.GetNuGetPackageVersionsAsync(packageId, includePrerelease, sources, token);
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
            try
            {
                var sourceList = sources?.ToList() ?? GetActiveSourceUrls();
                var loadedAssemblies = await _nuggetManager.LoadNuggetFromNuGetAsync(
                    packageName,
                    version,
                    sourceList,
                    useSingleSharedContext,
                    progress: null,
                    token: CancellationToken.None,
                    appInstallPath: appInstallPath,
                    useProcessHost: useProcessHost);

                SyncNuggetAssembliesToHandlerCollections(loadedAssemblies);
                if (loadedAssemblies.Count > 0)
                {
                    foreach (var assembly in loadedAssemblies)
                    {
                        var drivers = GetDrivers(assembly);
                        var owningPackage = string.IsNullOrWhiteSpace(assembly?.Location)
                            ? packageName
                            : _nuggetManager.FindNuggetByAssemblyPath(assembly.Location) ?? packageName;
                        foreach (var driver in drivers)
                        {
                            if (!string.IsNullOrWhiteSpace(driver?.DriverClass))
                            {
                                TrackDriverPackage(owningPackage, version, driver.DriverClass, driver.DatasourceType);
                            }
                        }
                    }
                    RecordNuGetSuccess();
                }
                else
                {
                    RecordNuGetFailure();
                }
                return loadedAssemblies;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"LoadNuggetFromNuGetAsync: Error - {ex.Message}");
                RecordNuGetFailure();
                return new List<Assembly>();
            }
        }

        private IEnumerable<string> EnumerateLocalPackageCandidates(ConnectionDriversConfig driver)
        {
            if (driver == null || string.IsNullOrWhiteSpace(driver.PackageName))
            {
                yield break;
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(driver.NuggetSource))
            {
                var explicitPath = driver.NuggetSource.Trim();
                if (seen.Add(explicitPath))
                {
                    yield return explicitPath;
                }
            }

            var packageName = driver.PackageName.Trim();
            var packageNameLower = packageName.ToLowerInvariant();
            var versions = new[] { driver.NuggetVersion?.Trim(), driver.version?.Trim() }
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var pluginsRoot = Path.Combine(AppContext.BaseDirectory, "Plugins", packageName);
            foreach (var version in versions)
            {
                var candidate = Path.Combine(pluginsRoot, version);
                if (seen.Add(candidate))
                {
                    yield return candidate;
                }
            }

            if (seen.Add(pluginsRoot))
            {
                yield return pluginsRoot;
            }

            if (Directory.Exists(pluginsRoot))
            {
                foreach (var dir in Directory.GetDirectories(pluginsRoot)
                    .OrderByDescending(d => d, StringComparer.OrdinalIgnoreCase))
                {
                    if (seen.Add(dir))
                    {
                        yield return dir;
                    }
                }
            }

            var globalRoot = Path.Combine(NuggetPackageDownloader.GetDefaultGlobalPackagesFolder(), packageNameLower);
            foreach (var version in versions)
            {
                var candidate = Path.Combine(globalRoot, version);
                if (seen.Add(candidate))
                {
                    yield return candidate;
                }
            }

            if (seen.Add(globalRoot))
            {
                yield return globalRoot;
            }

            if (Directory.Exists(globalRoot))
            {
                foreach (var dir in Directory.GetDirectories(globalRoot)
                    .OrderByDescending(d => d, StringComparer.OrdinalIgnoreCase))
                {
                    if (seen.Add(dir))
                    {
                        yield return dir;
                    }
                }
            }
        }

        private static bool IsLoadablePackageDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                return false;
            }

            if (Directory.GetFiles(path, "*.dll", SearchOption.TopDirectoryOnly).Any())
            {
                return true;
            }

            var libPath = Path.Combine(path, "lib");
            if (!Directory.Exists(libPath))
            {
                return false;
            }

            return Directory.GetDirectories(libPath)
                .Any(dir => Directory.GetFiles(dir, "*.dll", SearchOption.TopDirectoryOnly).Any());
        }

        private static string NormalizeLoadablePackagePath(string path, IEnumerable<string> preferredVersions)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            if (File.Exists(path))
            {
                return path;
            }

            if (!Directory.Exists(path))
            {
                return null;
            }

            if (IsLoadablePackageDirectory(path))
            {
                return path;
            }

            var versionLookup = new HashSet<string>(
                preferredVersions?.Where(v => !string.IsNullOrWhiteSpace(v)) ?? Enumerable.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);

            var childDirectories = Directory.GetDirectories(path)
                .OrderByDescending(d => d, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var child in childDirectories)
            {
                if (versionLookup.Contains(Path.GetFileName(child)) && IsLoadablePackageDirectory(child))
                {
                    return child;
                }
            }

            foreach (var child in childDirectories)
            {
                if (IsLoadablePackageDirectory(child))
                {
                    return child;
                }
            }

            return null;
        }

        public string ResolveLocalPackagePath(ConnectionDriversConfig driver)
        {
            var preferredVersions = new[] { driver?.NuggetVersion?.Trim(), driver?.version?.Trim() }
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var candidate in EnumerateLocalPackageCandidates(driver))
            {
                var loadablePath = NormalizeLoadablePackagePath(candidate, preferredVersions);
                if (!string.IsNullOrWhiteSpace(loadablePath))
                {
                    return loadablePath;
                }
            }

            return null;
        }

        public bool HasLocalPackage(ConnectionDriversConfig driver)
        {
            return !string.IsNullOrWhiteSpace(ResolveLocalPackagePath(driver));
        }

        public bool LoadDriverFromLocalPackage(ConnectionDriversConfig driver, out string loadPath)
        {
            loadPath = null;

            if (driver == null)
            {
                return false;
            }

            if (IsDriverClassLoaded(driver.classHandler, driver.dllname))
            {
                driver.IsMissing = false;
                driver.NuggetMissing = false;
                return true;
            }

            loadPath = ResolveLocalPackagePath(driver);
            if (string.IsNullOrWhiteSpace(loadPath))
            {
                driver.NuggetMissing = true;
                driver.IsMissing = true;
                return false;
            }

            driver.NuggetSource = loadPath;
            driver.NuggetMissing = false;

            if (!LoadNugget(loadPath))
            {
                driver.IsMissing = true;
                return false;
            }

            driver.IsMissing = !IsDriverClassLoaded(driver.classHandler, driver.dllname);
            return !driver.IsMissing;
        }

        #endregion
    }
}
