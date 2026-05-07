using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Versioning;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.NuGetManagement.Helpers;
using TheTechIdea.Beep.NuGetManagement.Models;

namespace TheTechIdea.Beep.NuGetManagement.Services
{
    /// <summary>
    /// Handles downloading NuGet packages from configured sources to the global cache.
    /// </summary>
    public class DownloadService
    {
        private readonly IDMLogger _logger;
        private readonly NuGetSdkHelper _sdkHelper;
        private readonly SourceManager _sourceManager;

        /// <summary>
        /// Initializes a new instance of the DownloadService.
        /// </summary>
        /// <param name="logger">The logger for diagnostic output.</param>
        /// <param name="sdkHelper">The NuGet SDK helper.</param>
        /// <param name="sourceManager">The source manager for resolving feeds.</param>
        public DownloadService(IDMLogger logger, NuGetSdkHelper sdkHelper, SourceManager sourceManager)
        {
            _logger = logger;
            _sdkHelper = sdkHelper;
            _sourceManager = sourceManager;
        }

        /// <summary>
        /// Downloads a NuGet package from the configured sources.
        /// </summary>
        /// <param name="packageId">The package identifier.</param>
        /// <param name="version">The specific version. If null, latest is resolved.</param>
        /// <param name="source">Optional specific source URL.</param>
        /// <returns>A <see cref="PackageInstallResult"/> containing download status and path.</returns>
        public async Task<PackageInstallResult> DownloadAsync(string packageId, string version = null, string source = null)
        {
            var result = new PackageInstallResult { PackageId = packageId };
            var startTime = DateTime.UtcNow;

            try
            {
                // Parse version
                NuGetVersion nugetVersion = null;
                if (!string.IsNullOrWhiteSpace(version))
                {
                    if (!NuGetVersion.TryParse(version, out nugetVersion))
                    {
                        result.Error = $"Invalid version format: {version}";
                        return result;
                    }
                }

                // Check cache first
                if (nugetVersion != null && _sdkHelper.IsPackageInCache(packageId, nugetVersion))
                {
                    var cachedPath = _sdkHelper.GetPackagePathInCache(packageId, nugetVersion);
                    result.Success = true;
                    result.Version = nugetVersion.ToNormalizedString();
                    result.InstallPath = cachedPath;
                    _logger?.LogWithContext($"Using cached package: {packageId} {nugetVersion}", null);
                    return result;
                }

                // Get sources to query
                var sources = GetSourcesToQuery(source);
                if (!sources.Any())
                {
                    result.Error = "No valid NuGet sources configured";
                    return result;
                }

                // Try each source
                foreach (var sourceUrl in sources)
                {
                    try
                    {
                        var repository = _sdkHelper.CreateRepository(sourceUrl);
                        if (repository == null) continue;

                        var resource = await _sdkHelper.GetFindPackageResourceAsync(repository);
                        if (resource == null) continue;

                        // Get version if not specified
                        if (nugetVersion == null)
                        {
                            var versions = await resource.GetAllVersionsAsync(packageId, _sdkHelper.CacheContext, _sdkHelper.NuGetLogger, CancellationToken.None);
                            nugetVersion = versions?.OrderByDescending(v => v).FirstOrDefault();
                            if (nugetVersion == null)
                            {
                                _logger?.LogWithContext($"No versions found for {packageId} on {sourceUrl}", null);
                                continue;
                            }
                        }

                        // Check cache again with resolved version
                        if (_sdkHelper.IsPackageInCache(packageId, nugetVersion))
                        {
                            var cachedPath = _sdkHelper.GetPackagePathInCache(packageId, nugetVersion);
                            result.Success = true;
                            result.Version = nugetVersion.ToNormalizedString();
                            result.InstallPath = cachedPath;
                            _logger?.LogWithContext($"Using cached package: {packageId} {nugetVersion}", null);
                            return result;
                        }

                        // Download package
                        var packagePath = _sdkHelper.GetPackagePathInCache(packageId, nugetVersion);
                        Directory.CreateDirectory(packagePath);

                        _logger?.LogWithContext($"Downloading {packageId} {nugetVersion} from {sourceUrl}...", null);

                        var success = await _sdkHelper.DownloadPackageAsync(resource, packageId, nugetVersion, packagePath);
                        if (success)
                        {
                            result.Success = true;
                            result.Version = nugetVersion.ToNormalizedString();
                            result.InstallPath = packagePath;
                            result.CompletedAt = DateTime.UtcNow;
                            result.DurationMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
                            _logger?.LogWithContext($"Downloaded {packageId} {nugetVersion}", null);
                            return result;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWithContext($"Error downloading from {sourceUrl}: {ex.Message}", ex);
                    }
                }

                result.Error = $"Failed to download {packageId} from any source";
                return result;
            }
            catch (Exception ex)
            {
                result.Error = $"Error downloading package {packageId}: {ex.Message}";
                _logger?.LogWithContext(result.Error, ex);
                return result;
            }
        }

        public async Task<Dictionary<string, string>> DownloadWithDependenciesAsync(
            string packageId,
            string version = null,
            string source = null,
            HashSet<string> processedPackages = null,
            IProgress<PassedArgs> progress = null,
            CancellationToken token = default)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            processedPackages ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                progress?.Report(new PassedArgs { Messege = $"Downloading {packageId} {version ?? "latest"}..." });

                var downloadResult = await DownloadAsync(packageId, version, source);
                if (!downloadResult.Success || string.IsNullOrEmpty(downloadResult.InstallPath))
                {
                    _logger?.LogWithContext($"Failed to download main package: {packageId}", null);
                    return result;
                }

                processedPackages.Add(packageId.ToLowerInvariant());
                result[packageId] = downloadResult.InstallPath;

                // Read dependencies from downloaded package
                var dependencies = await GetDependenciesFromPackageAsync(downloadResult.InstallPath);
                _logger?.LogWithContext($"Found {dependencies.Count} dependencies for {packageId}", null);

                int depIndex = 0;
                foreach (var dep in dependencies)
                {
                    if (token.IsCancellationRequested) break;
                    if (processedPackages.Contains(dep.PackageId.ToLowerInvariant())) continue;
                    if (NuGetSdkHelper.IsSystemPackage(dep.PackageId)) continue;

                    depIndex++;
                    progress?.Report(new PassedArgs { Messege = $"Downloading dependency {depIndex}/{dependencies.Count}: {dep.PackageId}..." });

                    var depResults = await DownloadWithDependenciesAsync(dep.PackageId, dep.VersionRange, source, processedPackages, progress, token);
                    foreach (var kvp in depResults)
                    {
                        if (!result.ContainsKey(kvp.Key))
                            result[kvp.Key] = kvp.Value;
                    }
                }

                progress?.Report(new PassedArgs { Messege = $"Completed download of {packageId} ({result.Count} packages)" });
                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Error downloading {packageId} with dependencies: {ex.Message}", ex);
                return result;
            }
        }

        private List<string> GetSourcesToQuery(string specificSource)
        {
            if (!string.IsNullOrWhiteSpace(specificSource))
                return new List<string> { specificSource };

            return _sourceManager.GetActiveSourceUrls();
        }

        private async Task<List<PackageDependency>> GetDependenciesFromPackageAsync(string packagePath)
        {
            var dependencies = new List<PackageDependency>();
            try
            {
                var nupkgFiles = Directory.GetFiles(packagePath, "*.nupkg", SearchOption.TopDirectoryOnly);
                if (!nupkgFiles.Any()) return dependencies;

                using (var reader = _sdkHelper.OpenPackage(nupkgFiles.First()))
                {
                    if (reader == null) return dependencies;
                    var metadata = await _sdkHelper.ReadPackageMetadataAsync(reader);
                    return metadata?.Dependencies ?? dependencies;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Error reading dependencies: {ex.Message}", ex);
                return dependencies;
            }
        }
    }
}
