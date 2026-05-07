using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NuGet.Versioning;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.NuGetManagement.Helpers;
using TheTechIdea.Beep.NuGetManagement.Models;
using TheTechIdea.Beep.Tools;

namespace TheTechIdea.Beep.NuGetManagement.Services
{
    public class UpdateService
    {
        private readonly IDMLogger _logger;
        private readonly NuGetSdkHelper _sdkHelper;
        private readonly DownloadService _downloadService;
        private readonly InstallService _installService;
        private readonly LoadService _loadService;
        private readonly SourceManager _sourceManager;

        public UpdateService(
            IDMLogger logger,
            NuGetSdkHelper sdkHelper,
            DownloadService downloadService,
            InstallService installService,
            LoadService loadService,
            SourceManager sourceManager)
        {
            _logger = logger;
            _sdkHelper = sdkHelper;
            _downloadService = downloadService;
            _installService = installService;
            _loadService = loadService;
            _sourceManager = sourceManager;
        }

        public async Task<PackageUpdateResult> UpdateAsync(string packageId, string version = null, string installDirectory = null)
        {
            var result = new PackageUpdateResult { PackageId = packageId };

            try
            {
                // Get current version
                var currentVersion = GetCurrentVersion(packageId, installDirectory);
                result.OldVersion = currentVersion;

                // Get latest version
                NuGetVersion latestVersion = null;
                if (!string.IsNullOrWhiteSpace(version))
                {
                    NuGetVersion.TryParse(version, out latestVersion);
                }
                else
                {
                    latestVersion = await GetLatestVersionAsync(packageId);
                }

                if (latestVersion == null)
                {
                    result.Error = $"Could not determine version to update to for {packageId}";
                    return result;
                }

                result.NewVersion = latestVersion.ToNormalizedString();

                // Check if update is needed
                if (!string.IsNullOrEmpty(currentVersion) && NuGetVersion.Parse(currentVersion) >= latestVersion)
                {
                    result.WasUpdated = false;
                    result.Success = true;
                    _logger?.LogWithContext($"Package {packageId} is already up to date ({currentVersion})", null);
                    return result;
                }

                // Download new version
                _logger?.LogWithContext($"Updating {packageId} from {currentVersion ?? "unknown"} to {latestVersion}...", null);

                var downloadResult = await _downloadService.DownloadAsync(packageId, latestVersion.ToNormalizedString());
                if (!downloadResult.Success)
                {
                    result.Error = $"Failed to download {packageId} {latestVersion}: {downloadResult.Error}";
                    return result;
                }

                // Unload old version if loaded
                await _loadService.UnloadAsync(packageId);

                // Install new version
                if (!string.IsNullOrWhiteSpace(installDirectory))
                {
                    var installResult = await _installService.InstallAsync(
                        downloadResult.InstallPath,
                        installDirectory,
                        packageId,
                        latestVersion.ToNormalizedString(),
                        true);

                    if (!installResult.Success)
                    {
                        result.Error = $"Failed to install {packageId}: {installResult.Error}";
                        return result;
                    }

                    // Load new version
                    await _loadService.LoadAsync(installResult.InstallPath, packageId, latestVersion.ToNormalizedString());
                }

                result.Success = true;
                result.WasUpdated = true;
                _logger?.LogWithContext($"Successfully updated {packageId} to {latestVersion}", null);

                return result;
            }
            catch (Exception ex)
            {
                result.Error = $"Error updating {packageId}: {ex.Message}";
                _logger?.LogWithContext(result.Error, ex);
                return result;
            }
        }

        public async Task<BulkUpdateResult> BulkUpdateAsync(List<string> packageIds = null, string installDirectory = null)
        {
            var result = new BulkUpdateResult();
            var packagesToCheck = packageIds ?? GetInstalledPackageIds(installDirectory);

            result.TotalChecked = packagesToCheck.Count;

            foreach (var packageId in packagesToCheck)
            {
                var updateResult = await UpdateAsync(packageId, null, installDirectory);
                
                if (updateResult.Success)
                {
                    if (updateResult.WasUpdated)
                        result.Updated++;
                    else
                        result.AlreadyLatest++;
                }
                else
                {
                    result.Failed++;
                }

                result.Results.Add(updateResult);
            }

            return result;
        }

        public async Task<bool> CheckForUpdatesAsync(string packageId)
        {
            var currentVersion = GetCurrentVersion(packageId);
            var latestVersion = await GetLatestVersionAsync(packageId);

            if (string.IsNullOrEmpty(currentVersion) || latestVersion == null)
                return false;

            return NuGetVersion.Parse(currentVersion) < latestVersion;
        }

        public async Task<List<(string PackageId, string CurrentVersion, string LatestVersion)>> GetPackagesWithUpdatesAsync(string installDirectory = null)
        {
            var result = new List<(string, string, string)>();
            var installedPackages = GetInstalledPackageIds(installDirectory);

            foreach (var packageId in installedPackages)
            {
                var currentVersion = GetCurrentVersion(packageId, installDirectory);
                var latestVersion = await GetLatestVersionAsync(packageId);

                if (!string.IsNullOrEmpty(currentVersion) && latestVersion != null &&
                    NuGetVersion.Parse(currentVersion) < latestVersion)
                {
                    result.Add((packageId, currentVersion, latestVersion.ToNormalizedString()));
                }
            }

            return result;
        }

        private async Task<NuGetVersion> GetLatestVersionAsync(string packageId)
        {
            try
            {
                var sources = _sourceManager.GetActiveSourceUrls();
                foreach (var sourceUrl in sources)
                {
                    var repository = _sdkHelper.CreateRepository(sourceUrl);
                    if (repository == null) continue;

                    var resource = await _sdkHelper.GetFindPackageResourceAsync(repository);
                    if (resource == null) continue;

                    var versions = await resource.GetAllVersionsAsync(packageId, _sdkHelper.CacheContext, _sdkHelper.NuGetLogger, System.Threading.CancellationToken.None);
                    var latest = versions?.OrderByDescending(v => v).FirstOrDefault();
                    if (latest != null) return latest;
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Error getting latest version for {packageId}: {ex.Message}", ex);
                return null;
            }
        }

        private string GetCurrentVersion(string packageId, string installDirectory = null)
        {
            if (string.IsNullOrWhiteSpace(installDirectory))
                return null;

            var packageDir = Path.Combine(installDirectory, packageId);
            if (!Directory.Exists(packageDir))
                return null;

            var versionDirs = Directory.GetDirectories(packageDir);
            if (!versionDirs.Any())
                return null;

            // Get the latest installed version
            var latest = versionDirs
                .Select(d => Path.GetFileName(d))
                .Where(v => NuGetVersion.TryParse(v, out _))
                .Select(v => NuGetVersion.Parse(v))
                .OrderByDescending(v => v)
                .FirstOrDefault();

            return latest?.ToNormalizedString();
        }

        private List<string> GetInstalledPackageIds(string installDirectory)
        {
            if (string.IsNullOrWhiteSpace(installDirectory) || !Directory.Exists(installDirectory))
                return new List<string>();

            return Directory.GetDirectories(installDirectory)
                .Select(d => Path.GetFileName(d))
                .ToList();
        }
    }
}
