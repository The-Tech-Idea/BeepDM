using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
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

        #endregion
    }
}
