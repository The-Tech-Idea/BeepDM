using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.NuGetManagement.Helpers;
using TheTechIdea.Beep.NuGetManagement.Models;
using TheTechIdea.Beep.NuGetManagement.Services;
using TheTechIdea.Beep.Tools;
using TheTechIdea.Beep.Tools.PluginSystem;
using NuGetSourceConfig = TheTechIdea.Beep.Tools.NuGetSourceConfig;

namespace TheTechIdea.Beep.NuGetManagement
{
    /// <summary>
    /// Centralized NuGet package manager providing complete package lifecycle management
    /// including download, install, load, update, and uninstall operations.
    /// Supports multiple sources, caching, and shared context loading.
    /// </summary>
    public class NuGetPackageManager : IDisposable
    {
        private readonly IDMLogger _logger;
        private readonly NuGetSdkHelper _sdkHelper;
        private readonly SourceManager _sourceManager;
        private readonly DownloadService _downloadService;
        private readonly InstallService _installService;
        private readonly LoadService _loadService;
        private readonly UpdateService _updateService;
        private readonly UninstallService _uninstallService;
        private readonly CacheManager _cacheManager;
        private readonly SearchService _searchService;
        private readonly VulnerabilityService _vulnerabilityService;
        private readonly PackageSigningService _signingService;
        private readonly string _defaultInstallDirectory;
        private readonly string _lockFilePath;
        private bool _disposed;

        /// <summary>
        /// Fired when a package installation is starting.
        /// </summary>
        public event EventHandler<PackageEventArgs> PackageInstalling;
        /// <summary>
        /// Fired when a package installation has completed.
        /// </summary>
        public event EventHandler<PackageEventArgs> PackageInstalled;
        /// <summary>
        /// Fired when a package loading is starting.
        /// </summary>
        public event EventHandler<PackageEventArgs> PackageLoading;
        /// <summary>
        /// Fired when a package has been loaded.
        /// </summary>
        public event EventHandler<PackageEventArgs> PackageLoaded;
        /// <summary>
        /// Fired when a package update is starting.
        /// </summary>
        public event EventHandler<PackageEventArgs> PackageUpdating;
        /// <summary>
        /// Fired when a package update has completed.
        /// </summary>
        public event EventHandler<PackageEventArgs> PackageUpdated;
        /// <summary>
        /// Fired when a package uninstallation is starting.
        /// </summary>
        public event EventHandler<PackageEventArgs> PackageUninstalling;
        /// <summary>
        /// Fired when a package has been uninstalled.
        /// </summary>
        public event EventHandler<PackageEventArgs> PackageUninstalled;

        /// <summary>
        /// Initializes a new instance of the NuGetPackageManager with default configuration.
        /// </summary>
        /// <param name="logger">The logger for diagnostic output. Cannot be null.</param>
        /// <param name="sharedContextManager">The shared context manager for assembly loading.</param>
        /// <param name="installDirectory">Optional custom installation directory. Defaults to {BaseDirectory}/Plugins.</param>
        /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
        public NuGetPackageManager(IDMLogger logger, SharedContextManager sharedContextManager, string installDirectory = null)
            : this(logger, sharedContextManager, new NuGetManagerConfig { InstallDirectory = installDirectory })
        {
        }

        /// <summary>
        /// Initializes a new instance of the NuGetPackageManager with custom configuration.
        /// </summary>
        /// <param name="logger">The logger for diagnostic output. Cannot be null.</param>
        /// <param name="sharedContextManager">The shared context manager for assembly loading.</param>
        /// <param name="config">Configuration options for the manager.</param>
        /// <exception cref="ArgumentNullException">Thrown when logger or config is null.</exception>
        public NuGetPackageManager(IDMLogger logger, SharedContextManager sharedContextManager, NuGetManagerConfig config)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            if (config == null) throw new ArgumentNullException(nameof(config));
            
            _sdkHelper = new NuGetSdkHelper(logger);
            _sourceManager = new SourceManager(logger);
            
            // Add configured sources
            foreach (var source in config.DefaultSources)
            {
                _sourceManager.AddSource(source.Name, source.Url, source.Enabled, source.Username, source.Password);
            }
            
            _downloadService = new DownloadService(logger, _sdkHelper, _sourceManager);
            _installService = new InstallService(logger);
            _loadService = new LoadService(logger, sharedContextManager);
            _updateService = new UpdateService(logger, _sdkHelper, _downloadService, _installService, _loadService, _sourceManager);
            _uninstallService = new UninstallService(logger, _loadService);
            _cacheManager = new CacheManager(logger, config.CachePath, config.MaxCacheSizeMB, config.AutoCleanup);
            _searchService = new SearchService(logger, _sdkHelper, _sourceManager);
            _vulnerabilityService = new VulnerabilityService(logger);
            _signingService = new PackageSigningService(logger);
            _defaultInstallDirectory = config.InstallDirectory ?? Path.Combine(AppContext.BaseDirectory, "Plugins");
            _lockFilePath = Path.Combine(_defaultInstallDirectory, "packages.lock.json");

            if (!Directory.Exists(_defaultInstallDirectory))
                Directory.CreateDirectory(_defaultInstallDirectory);

            _logger?.LogWithContext("NuGetPackageManager initialized with configuration", null);
        }

        #region Lifecycle Operations

        /// <summary>
        /// Downloads a NuGet package from the configured sources to the global cache.
        /// </summary>
        /// <param name="packageId">The package identifier (e.g., "Newtonsoft.Json").</param>
        /// <param name="version">The specific version to download. If null, the latest version is resolved.</param>
        /// <param name="source">Optional specific source URL to use. If null, queries all active sources.</param>
        /// <returns>A <see cref="PackageInstallResult"/> containing download status and path.</returns>
        public async Task<PackageInstallResult> DownloadAsync(string packageId, string version = null, string source = null)
        {
            return await _downloadService.DownloadAsync(packageId, version, source);
        }

        /// <summary>
        /// Downloads and installs a NuGet package to the application directory.
        /// </summary>
        /// <param name="packageId">The package identifier.</param>
        /// <param name="version">The specific version. If null, latest is used.</param>
        /// <param name="installPath">Optional custom installation directory. Defaults to configured directory.</param>
        /// <param name="overwrite">If true, overwrites existing files.</param>
        /// <returns>A <see cref="PackageInstallResult"/> containing installation status and path.</returns>
        public async Task<PackageInstallResult> InstallAsync(string packageId, string version = null, string installPath = null, bool overwrite = false)
        {
            var downloadResult = await DownloadAsync(packageId, version);
            if (!downloadResult.Success)
                return downloadResult;

            return await _installService.InstallAsync(
                downloadResult.InstallPath,
                installPath ?? _defaultInstallDirectory,
                packageId,
                downloadResult.Version,
                overwrite);
        }

        /// <summary>
        /// Installs and loads a NuGet package with all its dependencies into the assembly context.
        /// If the package is not installed, it will be downloaded and installed first.
        /// </summary>
        /// <param name="packageId">The package identifier.</param>
        /// <param name="version">The specific version. If null, latest is used.</param>
        /// <param name="useSharedContext">If true, loads into the shared context. If false, creates isolated context.</param>
        /// <param name="installPath">Optional custom installation directory.</param>
        /// <returns>A <see cref="NuggetInfo"/> containing loaded assembly information, or null if failed.</returns>
        public async Task<NuggetInfo> LoadAsync(string packageId, string version = null, bool useSharedContext = true, string installPath = null)
        {
            OnPackageLoading(packageId, version);

            try
            {
                // Check if already installed
                var targetDir = installPath ?? _defaultInstallDirectory;
                var isInstalled = _installService.IsPackageInstalled(packageId, targetDir, version);

                if (!isInstalled)
                {
                    var installResult = await InstallAsync(packageId, version, targetDir);
                    if (!installResult.Success)
                    {
                        _logger?.LogWithContext($"Failed to install {packageId} for loading: {installResult.Error}", null);
                        return null;
                    }
                }

                var packageDir = _installService.GetInstallPath(packageId, targetDir, version);
                var nuggetInfo = await _loadService.LoadAsync(packageDir, packageId, version, useSharedContext);

                OnPackageLoaded(packageId, version);
                return nuggetInfo;
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Error loading {packageId}: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// Updates a NuGet package to the latest or specified version.
        /// Unloads the old version, downloads and installs the new version, then loads it.
        /// </summary>
        /// <param name="packageId">The package identifier.</param>
        /// <param name="version">The target version. If null, updates to latest.</param>
        /// <param name="installPath">Optional custom installation directory.</param>
        /// <returns>A <see cref="PackageUpdateResult"/> containing update status and version information.</returns>
        public async Task<PackageUpdateResult> UpdateAsync(string packageId, string version = null, string installPath = null)
        {
            OnPackageUpdating(packageId, version);

            var result = await _updateService.UpdateAsync(packageId, version, installPath ?? _defaultInstallDirectory);

            if (result.Success && result.WasUpdated)
                OnPackageUpdated(packageId, result.NewVersion);

            return result;
        }

        /// <summary>
        /// Uninstalls a NuGet package by unloading it and removing its files.
        /// </summary>
        /// <param name="packageId">The package identifier.</param>
        /// <param name="removeDependencies">If true, also removes dependency packages.</param>
        /// <param name="installPath">Optional custom installation directory.</param>
        /// <returns>True if uninstallation succeeded; otherwise, false.</returns>
        public async Task<bool> UninstallAsync(string packageId, bool removeDependencies = false, string installPath = null)
        {
            OnPackageUninstalling(packageId);

            var result = await _uninstallService.UninstallAsync(packageId, installPath ?? _defaultInstallDirectory, removeDependencies);

            if (result)
                OnPackageUninstalled(packageId);

            return result;
        }

        /// <summary>
        /// Repairs a corrupted package by re-downloading and reinstalling it.
        /// </summary>
        /// <param name="packageId">The package identifier.</param>
        /// <param name="installPath">Optional custom installation directory.</param>
        /// <returns>True if repair succeeded; otherwise, false.</returns>
        public async Task<bool> RepairAsync(string packageId, string installPath = null)
        {
            return await _uninstallService.RepairAsync(
                packageId,
                installPath ?? _defaultInstallDirectory,
                _downloadService,
                _installService);
        }

        /// <summary>
        /// Verifies the integrity of an installed package by checking its files.
        /// </summary>
        /// <param name="packageId">The package identifier.</param>
        /// <param name="installPath">Optional custom installation directory.</param>
        /// <returns>True if package is valid; otherwise, false.</returns>
        public async Task<bool> VerifyAsync(string packageId, string installPath = null)
        {
            return await _uninstallService.VerifyAsync(packageId, installPath ?? _defaultInstallDirectory);
        }

        #endregion

        #region Search & Discovery

        /// <summary>
        /// Searches for NuGet packages across all configured sources.
        /// </summary>
        /// <param name="searchTerm">The search keyword or package name.</param>
        /// <param name="options">Optional search filters (skip, take, prerelease, etc.).</param>
        /// <returns>A list of <see cref="PackageSearchResult"/> matching the search criteria.</returns>
        public async Task<List<PackageSearchResult>> SearchAsync(string searchTerm, SearchOptions options = null)
        {
            return await _searchService.SearchAsync(searchTerm, options);
        }

        /// <summary>
        /// Gets all available versions for a specific package.
        /// </summary>
        /// <param name="packageId">The package identifier.</param>
        /// <param name="includePrerelease">If true, includes prerelease versions.</param>
        /// <param name="source">Optional specific source to query.</param>
        /// <returns>A list of version strings ordered from newest to oldest.</returns>
        public async Task<List<string>> GetVersionsAsync(string packageId, bool includePrerelease = false, string source = null)
        {
            return await _searchService.GetVersionsAsync(packageId, includePrerelease, source);
        }

        /// <summary>
        /// Gets detailed metadata for a specific package version.
        /// </summary>
        /// <param name="packageId">The package identifier.</param>
        /// <param name="version">The specific version.</param>
        /// <param name="source">Optional specific source to query.</param>
        /// <returns>A <see cref="PackageMetadata"/> object, or null if not found.</returns>
        public async Task<PackageMetadata> GetMetadataAsync(string packageId, string version, string source = null)
        {
            return await _searchService.GetMetadataAsync(packageId, version, source);
        }

        /// <summary>
        /// Gets the dependency tree for a specific package version.
        /// </summary>
        /// <param name="packageId">The package identifier.</param>
        /// <param name="version">The specific version.</param>
        /// <param name="source">Optional specific source to query.</param>
        /// <returns>A list of <see cref="PackageDependency"/> objects.</returns>
        public async Task<List<PackageDependency>> GetDependenciesAsync(string packageId, string version, string source = null)
        {
            return await _searchService.GetDependenciesAsync(packageId, version, source);
        }

        #endregion

        #region Query & Status

        /// <summary>
        /// Gets all installed packages from the installation directory.
        /// </summary>
        /// <param name="installPath">Optional custom installation directory.</param>
        /// <returns>A list of <see cref="InstalledPackageInfo"/> objects.</returns>
        public async Task<List<InstalledPackageInfo>> GetInstalledPackagesAsync(string installPath = null)
        {
            var result = new List<InstalledPackageInfo>();
            var targetDir = installPath ?? _defaultInstallDirectory;

            if (!Directory.Exists(targetDir))
                return result;

            foreach (var packageDir in Directory.GetDirectories(targetDir))
            {
                var packageId = Path.GetFileName(packageDir);
                foreach (var versionDir in Directory.GetDirectories(packageDir))
                {
                    var version = Path.GetFileName(versionDir);
                    var info = new InstalledPackageInfo
                    {
                        PackageId = packageId,
                        Version = version,
                        InstallPath = versionDir,
                        InstalledDate = Directory.GetCreationTimeUtc(versionDir),
                        IsLoaded = _loadService.IsLoaded(packageId),
                        LoadedAssemblyPaths = Directory.GetFiles(versionDir, "*.dll", SearchOption.AllDirectories).ToList()
                    };
                    result.Add(info);
                }
            }

            return result;
        }

        /// <summary>
        /// Gets information for a specific installed package.
        /// </summary>
        /// <param name="packageId">The package identifier.</param>
        /// <param name="installPath">Optional custom installation directory.</param>
        /// <returns>An <see cref="InstalledPackageInfo"/> object, or null if not installed.</returns>
        public async Task<InstalledPackageInfo> GetInstalledPackageAsync(string packageId, string installPath = null)
        {
            var packages = await GetInstalledPackagesAsync(installPath);
            return packages.FirstOrDefault(p => p.PackageId.Equals(packageId, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Checks if a package is installed.
        /// </summary>
        /// <param name="packageId">The package identifier.</param>
        /// <param name="installPath">Optional custom installation directory.</param>
        /// <returns>True if installed; otherwise, false.</returns>
        public async Task<bool> IsInstalledAsync(string packageId, string installPath = null)
        {
            return _installService.IsPackageInstalled(packageId, installPath ?? _defaultInstallDirectory);
        }

        /// <summary>
        /// Checks if a package is currently loaded in memory.
        /// </summary>
        /// <param name="packageId">The package identifier.</param>
        /// <returns>True if loaded; otherwise, false.</returns>
        public async Task<bool> IsLoadedAsync(string packageId)
        {
            return _loadService.IsLoaded(packageId);
        }

        /// <summary>
        /// Gets usage statistics for a package or overall statistics.
        /// </summary>
        /// <param name="packageId">Optional specific package. If null, returns overall statistics.</param>
        /// <returns>A <see cref="PackageStatistics"/> object.</returns>
        public async Task<PackageStatistics> GetStatisticsAsync(string packageId = null)
        {
            var stats = new PackageStatistics { PackageId = packageId };
            
            if (!string.IsNullOrWhiteSpace(packageId))
            {
                var installed = await GetInstalledPackageAsync(packageId);
                if (installed != null)
                {
                    stats.FirstInstalled = installed.InstalledDate;
                    stats.LoadCount = installed.LoadedAssemblyPaths.Count;
                }
            }

            return stats;
        }

        #endregion

        #region Source Management

        /// <summary>
        /// Gets all configured NuGet sources (enabled and disabled).
        /// </summary>
        /// <returns>A list of <see cref="NuGetSourceConfig"/> objects.</returns>
        public List<NuGetSourceConfig> GetSources() => _sourceManager.GetSources();
        
        /// <summary>
        /// Gets only the enabled (active) NuGet sources.
        /// </summary>
        /// <returns>A list of active <see cref="NuGetSourceConfig"/> objects.</returns>
        public List<NuGetSourceConfig> GetActiveSources() => _sourceManager.GetActiveSources();
        
        /// <summary>
        /// Gets the URLs of all active NuGet sources.
        /// </summary>
        /// <returns>A list of source URLs.</returns>
        public List<string> GetActiveSourceUrls() => _sourceManager.GetActiveSourceUrls();

        /// <summary>
        /// Adds or updates a NuGet source.
        /// </summary>
        /// <param name="name">The source name.</param>
        /// <param name="url">The source URL or local path.</param>
        /// <param name="isEnabled">Whether the source is initially enabled.</param>
        /// <param name="username">Optional username for authenticated feeds.</param>
        /// <param name="password">Optional password for authenticated feeds.</param>
        /// <param name="apiKey">Optional API key for publishing.</param>
        public void AddSource(string name, string url, bool isEnabled = true, string username = null, string password = null, string apiKey = null)
        {
            _sourceManager.AddSource(name, url, isEnabled, username, password, apiKey);
        }

        /// <summary>
        /// Removes a NuGet source by name.
        /// </summary>
        /// <param name="name">The source name.</param>
        public void RemoveSource(string name) => _sourceManager.RemoveSource(name);
        
        /// <summary>
        /// Enables a previously disabled NuGet source.
        /// </summary>
        /// <param name="name">The source name.</param>
        public void EnableSource(string name) => _sourceManager.EnableSource(name);
        
        /// <summary>
        /// Disables a NuGet source without removing it.
        /// </summary>
        /// <param name="name">The source name.</param>
        public void DisableSource(string name) => _sourceManager.DisableSource(name);
        
        /// <summary>
        /// Sets the priority for source resolution ordering.
        /// </summary>
        /// <param name="name">The source name.</param>
        /// <param name="priority">Lower numbers have higher priority.</param>
        public void SetSourcePriority(string name, int priority) => _sourceManager.SetSourcePriority(name, priority);
        
        /// <summary>
        /// Tests if a NuGet source is accessible and healthy.
        /// </summary>
        /// <param name="name">The source name.</param>
        /// <returns>True if the source is healthy; otherwise, false.</returns>
        public async Task<bool> TestSourceAsync(string name) => await _sourceManager.TestSourceAsync(name);

        #endregion

        #region Cache Management

        /// <summary>
        /// Gets all packages currently cached in the global NuGet cache.
        /// </summary>
        /// <returns>A list of <see cref="CachedPackageInfo"/> objects.</returns>
        public async Task<List<CachedPackageInfo>> GetCachedPackagesAsync() => await _cacheManager.GetCachedPackagesAsync();
        
        /// <summary>
        /// Gets information about the NuGet cache (size, count, limits).
        /// </summary>
        /// <returns>A <see cref="CacheInfo"/> object.</returns>
        public async Task<CacheInfo> GetCacheInfoAsync() => await _cacheManager.GetCacheInfoAsync();
        
        /// <summary>
        /// Checks if a specific package version exists in the cache.
        /// </summary>
        /// <param name="packageId">The package identifier.</param>
        /// <param name="version">The version string.</param>
        /// <returns>True if cached; otherwise, false.</returns>
        public bool IsCached(string packageId, string version) => _cacheManager.IsPackageInCache(packageId, version);
        
        /// <summary>
        /// Clears packages from the cache.
        /// </summary>
        /// <param name="packageId">Optional specific package to clear. If null, clears all.</param>
        /// <param name="version">Optional specific version to clear.</param>
        public async Task ClearCacheAsync(string packageId = null, string version = null) => await _cacheManager.ClearCacheAsync(packageId, version);
        
        /// <summary>
        /// Removes packages from cache that haven't been accessed recently.
        /// </summary>
        /// <param name="daysToKeep">Packages older than this many days are removed.</param>
        public async Task CleanupOldPackagesAsync(int daysToKeep = 30) => await _cacheManager.CleanupOldPackagesAsync(daysToKeep);

        #endregion

        #region Bulk Operations

        /// <summary>
        /// Installs multiple packages in a single operation.
        /// </summary>
        /// <param name="packages">List of packages to install with versions.</param>
        /// <param name="installPath">Optional custom installation directory.</param>
        /// <returns>A <see cref="BulkInstallResult"/> with per-package results.</returns>
        public async Task<BulkInstallResult> BulkInstallAsync(List<PackageRequest> packages, string installPath = null)
        {
            var results = new BulkInstallResult { TotalRequested = packages.Count };

            foreach (var package in packages)
            {
                var installResult = await InstallAsync(package.PackageId, package.Version, installPath);
                
                if (installResult.Success)
                    results.Successful++;
                else
                    results.Failed++;

                results.Results.Add(installResult);
            }

            return results;
        }

        /// <summary>
        /// Updates multiple packages in a single operation.
        /// </summary>
        /// <param name="packageIds">List of package IDs to update. If null, updates all installed packages.</param>
        /// <param name="installPath">Optional custom installation directory.</param>
        /// <returns>A <see cref="BulkUpdateResult"/> with update status for each package.</returns>
        public async Task<BulkUpdateResult> BulkUpdateAsync(List<string> packageIds = null, string installPath = null)
        {
            return await _updateService.BulkUpdateAsync(packageIds, installPath ?? _defaultInstallDirectory);
        }

        /// <summary>
        /// Uninstalls multiple packages in a single operation.
        /// </summary>
        /// <param name="packageIds">List of package IDs to uninstall.</param>
        /// <param name="installPath">Optional custom installation directory.</param>
        /// <returns>A <see cref="BulkUninstallResult"/> with per-package results.</returns>
        public async Task<BulkUninstallResult> BulkUninstallAsync(List<string> packageIds, string installPath = null)
        {
            return await _uninstallService.BulkUninstallAsync(packageIds, installPath ?? _defaultInstallDirectory);
        }

        #endregion

        #region Security

        /// <summary>
        /// Checks for known vulnerabilities in a specific package version.
        /// </summary>
        /// <param name="packageId">The package identifier.</param>
        /// <param name="version">The specific version.</param>
        /// <returns>A list of <see cref="VulnerabilityInfo"/> objects, or empty if none found.</returns>
        public async Task<List<VulnerabilityInfo>> CheckVulnerabilitiesAsync(string packageId, string version)
        {
            return await _vulnerabilityService.CheckVulnerabilitiesAsync(packageId, version);
        }

        /// <summary>
        /// Checks if a NuGet package file is cryptographically signed.
        /// </summary>
        /// <param name="packagePath">Path to the .nupkg file.</param>
        /// <returns>True if signed; otherwise, false.</returns>
        public async Task<bool> IsPackageSignedAsync(string packagePath)
        {
            return await _vulnerabilityService.IsPackageSignedAsync(packagePath);
        }

        /// <summary>
        /// Checks if a NuGet source URL is from a trusted provider.
        /// </summary>
        /// <param name="sourceUrl">The source URL to check.</param>
        /// <returns>True if trusted; otherwise, false.</returns>
        public async Task<bool> IsTrustedSourceAsync(string sourceUrl)
        {
            return await _vulnerabilityService.IsTrustedSourceAsync(sourceUrl);
        }

        /// <summary>
        /// Verifies the digital signature of a NuGet package.
        /// </summary>
        /// <param name="packagePath">Path to the .nupkg file.</param>
        /// <param name="allowUntrusted">If true, allows untrusted certificates.</param>
        /// <returns>A <see cref="SignatureVerificationResult"/> with detailed verification information.</returns>
        public async Task<SignatureVerificationResult> VerifyPackageSignatureAsync(string packagePath, bool allowUntrusted = false)
        {
            return await _signingService.VerifySignatureAsync(packagePath, allowUntrusted);
        }

        /// <summary>
        /// Gets signature information for a NuGet package without full verification.
        /// </summary>
        /// <param name="packagePath">Path to the .nupkg file.</param>
        /// <returns>A <see cref="SignatureInfo"/> object with signature details.</returns>
        public async Task<SignatureInfo> GetPackageSignatureInfoAsync(string packagePath)
        {
            return await _signingService.GetSignatureInfoAsync(packagePath);
        }

        #endregion

        #region Lock File Support

        /// <summary>
        /// Generates a lock file containing exact versions of all installed packages.
        /// This enables deterministic restores across different machines and builds.
        /// </summary>
        /// <returns>True if the lock file was generated successfully.</returns>
        public async Task<bool> GenerateLockFileAsync()
        {
            try
            {
                var installed = await GetInstalledPackagesAsync();
                var lockData = new LockFileData
                {
                    GeneratedAt = DateTime.UtcNow,
                    Packages = installed.Select(p => new LockFilePackage
                    {
                        PackageId = p.PackageId,
                        Version = p.Version,
                        Source = p.Source,
                        Dependencies = p.Dependencies?.ToList() ?? new List<string>()
                    }).ToList()
                };

                var json = Newtonsoft.Json.JsonConvert.SerializeObject(lockData, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(_lockFilePath, json);
                
                _logger?.LogWithContext($"Generated lock file with {lockData.Packages.Count} packages: {_lockFilePath}", null);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Error generating lock file: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Restores packages from a lock file, ensuring exact versions are installed.
        /// </summary>
        /// <param name="lockFilePath">Optional path to lock file. Defaults to packages.lock.json in install directory.</param>
        /// <returns>A <see cref="BulkInstallResult"/> with restore status.</returns>
        public async Task<BulkInstallResult> RestoreFromLockFileAsync(string lockFilePath = null)
        {
            var result = new BulkInstallResult();
            var path = lockFilePath ?? _lockFilePath;

            try
            {
                if (!File.Exists(path))
                {
                    result.Error = $"Lock file not found: {path}";
                    return result;
                }

                var json = File.ReadAllText(path);
                var lockData = Newtonsoft.Json.JsonConvert.DeserializeObject<LockFileData>(json);

                if (lockData?.Packages == null)
                {
                    result.Error = "Invalid lock file format";
                    return result;
                }

                var requests = lockData.Packages.Select(p => new PackageRequest
                {
                    PackageId = p.PackageId,
                    Version = p.Version
                }).ToList();

                result = await BulkInstallAsync(requests);
                
                _logger?.LogWithContext($"Restored {result.Successful}/{result.TotalRequested} packages from lock file", null);
                return result;
            }
            catch (Exception ex)
            {
                result.Error = $"Error restoring from lock file: {ex.Message}";
                _logger?.LogWithContext($"Error restoring from lock file {path}", ex);
                return result;
            }
        }

        /// <summary>
        /// Checks if a lock file exists.
        /// </summary>
        /// <returns>True if a lock file exists; otherwise, false.</returns>
        public bool HasLockFile()
        {
            return File.Exists(_lockFilePath);
        }

        /// <summary>
        /// Gets the path to the lock file.
        /// </summary>
        /// <returns>The lock file path.</returns>
        public string GetLockFilePath()
        {
            return _lockFilePath;
        }

        #endregion

        #region Import/Export

        /// <summary>
        /// Imports packages from a JSON file and installs them.
        /// </summary>
        /// <param name="filePath">Path to the JSON file containing package list.</param>
        public async Task ImportPackagesAsync(string filePath)
        {
            try
            {
                var json = File.ReadAllText(filePath);
                var packages = Newtonsoft.Json.JsonConvert.DeserializeObject<List<PackageRequest>>(json);
                
                if (packages != null)
                {
                    await BulkInstallAsync(packages);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Error importing packages from {filePath}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Exports installed packages to a JSON file.
        /// </summary>
        /// <param name="filePath">Path to write the JSON file.</param>
        public async Task ExportPackagesAsync(string filePath)
        {
            try
            {
                var installed = await GetInstalledPackagesAsync();
                var packages = installed.Select(p => new PackageRequest
                {
                    PackageId = p.PackageId,
                    Version = p.Version
                }).ToList();

                var json = Newtonsoft.Json.JsonConvert.SerializeObject(packages, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Error exporting packages to {filePath}: {ex.Message}", ex);
            }
        }

        #endregion

        #region Event Helpers

        /// <summary>
        /// Raises the PackageLoading event.
        /// </summary>
        private void OnPackageLoading(string packageId, string version)
        {
            PackageLoading?.Invoke(this, new PackageEventArgs { PackageId = packageId, Version = version, Message = $"Loading {packageId}..." });
        }

        /// <summary>
        /// Raises the PackageLoaded event.
        /// </summary>
        private void OnPackageLoaded(string packageId, string version)
        {
            PackageLoaded?.Invoke(this, new PackageEventArgs { PackageId = packageId, Version = version, Message = $"Loaded {packageId}" });
        }

        /// <summary>
        /// Raises the PackageUpdating event.
        /// </summary>
        private void OnPackageUpdating(string packageId, string version)
        {
            PackageUpdating?.Invoke(this, new PackageEventArgs { PackageId = packageId, Version = version, Message = $"Updating {packageId}..." });
        }

        /// <summary>
        /// Raises the PackageUpdated event.
        /// </summary>
        private void OnPackageUpdated(string packageId, string version)
        {
            PackageUpdated?.Invoke(this, new PackageEventArgs { PackageId = packageId, Version = version, Message = $"Updated {packageId} to {version}" });
        }

        /// <summary>
        /// Raises the PackageUninstalling event.
        /// </summary>
        private void OnPackageUninstalling(string packageId)
        {
            PackageUninstalling?.Invoke(this, new PackageEventArgs { PackageId = packageId, Message = $"Uninstalling {packageId}..." });
        }

        /// <summary>
        /// Raises the PackageUninstalled event.
        /// </summary>
        private void OnPackageUninstalled(string packageId)
        {
            PackageUninstalled?.Invoke(this, new PackageEventArgs { PackageId = packageId, Message = $"Uninstalled {packageId}" });
        }

        #endregion

        /// <summary>
        /// Releases resources used by the NuGetPackageManager.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _logger?.LogWithContext("NuGetPackageManager disposed", null);
            }
        }
    }
}
