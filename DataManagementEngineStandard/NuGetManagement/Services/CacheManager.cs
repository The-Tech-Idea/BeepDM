using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep.Logger;
 

namespace TheTechIdea.Beep.NuGetManagement.Services
{
    /// <summary>
    /// Manages the NuGet global packages cache including listing, clearing, and cleanup operations.
    /// </summary>
    public class CacheManager
    {
        private readonly IDMLogger _logger;
        private readonly string _cachePath;
        private readonly long _maxCacheSizeBytes;
        private readonly bool _autoCleanup;

        /// <summary>
        /// Initializes a new instance of the CacheManager.
        /// </summary>
        /// <param name="logger">The logger for diagnostic output.</param>
        /// <param name="cachePath">Optional custom cache path. Defaults to %USERPROFILE%/.nuget/packages.</param>
        /// <param name="maxCacheSizeMB">Maximum cache size in megabytes.</param>
        /// <param name="autoCleanup">Whether automatic cleanup is enabled.</param>
        public CacheManager(IDMLogger logger, string cachePath = null, long maxCacheSizeMB = 1024, bool autoCleanup = true)
        {
            _logger = logger;
            _cachePath = cachePath ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".nuget", "packages");
            _maxCacheSizeBytes = maxCacheSizeMB * 1024 * 1024;
            _autoCleanup = autoCleanup;
        }

        /// <summary>
        /// Gets the root cache directory path.
        /// </summary>
        public string CachePath => _cachePath;

        /// <summary>
        /// Gets all packages currently in the cache.
        /// </summary>
        /// <returns>List of cached package information.</returns>
        public async Task<List<CachedPackageInfo>> GetCachedPackagesAsync()
        {
            var result = new List<CachedPackageInfo>();

            try
            {
                if (!Directory.Exists(_cachePath))
                    return result;

                var packageDirs = Directory.GetDirectories(_cachePath);
                foreach (var packageDir in packageDirs)
                {
                    var packageId = Path.GetFileName(packageDir);
                    var versionDirs = Directory.GetDirectories(packageDir);

                    foreach (var versionDir in versionDirs)
                    {
                        var version = Path.GetFileName(versionDir);
                        var nupkgFiles = Directory.GetFiles(versionDir, "*.nupkg", SearchOption.TopDirectoryOnly);
                        
                        if (nupkgFiles.Any())
                        {
                            var fileInfo = new FileInfo(nupkgFiles.First());
                            result.Add(new CachedPackageInfo
                            {
                                PackageId = packageId,
                                Version = version,
                                Path = versionDir,
                                SizeBytes = fileInfo.Length,
                                CachedDate = fileInfo.CreationTimeUtc,
                                LastAccessed = fileInfo.LastAccessTimeUtc
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Error listing cached packages: {ex.Message}", ex);
            }

            return result.OrderByDescending(p => p.LastAccessed).ToList();
        }

        public async Task<CacheInfo> GetCacheInfoAsync()
        {
            try
            {
                var packages = await GetCachedPackagesAsync();
                var totalSize = packages.Sum(p => p.SizeBytes);

                return new CacheInfo
                {
                    CachePath = _cachePath,
                    TotalSizeBytes = totalSize,
                    PackageCount = packages.Count,
                    MaxSizeBytes = _maxCacheSizeBytes,
                    AutoCleanup = _autoCleanup
                };
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Error getting cache info: {ex.Message}", ex);
                return new CacheInfo { CachePath = _cachePath };
            }
        }

        public bool IsPackageInCache(string packageId, string version)
        {
            try
            {
                var packagePath = Path.Combine(_cachePath, packageId.ToLowerInvariant(), version);
                var nupkgPath = Path.Combine(packagePath, $"{packageId.ToLowerInvariant()}.{version}.nupkg");
                return Directory.Exists(packagePath) && File.Exists(nupkgPath);
            }
            catch
            {
                return false;
            }
        }

        public async Task ClearCacheAsync(string packageId = null, string version = null)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(packageId) && !string.IsNullOrWhiteSpace(version))
                {
                    // Remove specific package version
                    var packagePath = Path.Combine(_cachePath, packageId.ToLowerInvariant(), version);
                    if (Directory.Exists(packagePath))
                    {
                        Directory.Delete(packagePath, true);
                        _logger?.LogWithContext($"Removed cached package: {packageId} {version}", null);
                    }
                }
                else if (!string.IsNullOrWhiteSpace(packageId))
                {
                    // Remove all versions of package
                    var packagePath = Path.Combine(_cachePath, packageId.ToLowerInvariant());
                    if (Directory.Exists(packagePath))
                    {
                        Directory.Delete(packagePath, true);
                        _logger?.LogWithContext($"Removed all cached versions of: {packageId}", null);
                    }
                }
                else
                {
                    // Clear entire cache
                    if (Directory.Exists(_cachePath))
                    {
                        foreach (var dir in Directory.GetDirectories(_cachePath))
                        {
                            Directory.Delete(dir, true);
                        }
                        _logger?.LogWithContext("Cleared entire package cache", null);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Error clearing cache: {ex.Message}", ex);
            }
        }

        public async Task CleanupOldPackagesAsync(int daysToKeep = 30)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
                var packages = await GetCachedPackagesAsync();
                var removed = 0;

                foreach (var package in packages)
                {
                    if (package.LastAccessed < cutoffDate)
                    {
                        if (Directory.Exists(package.Path))
                        {
                            Directory.Delete(package.Path, true);
                            removed++;
                        }
                    }
                }

                _logger?.LogWithContext($"Cleaned up {removed} old packages (older than {daysToKeep} days)", null);
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Error cleaning up old packages: {ex.Message}", ex);
            }
        }

        public async Task EnforceCacheSizeLimitAsync()
        {
            try
            {
                var cacheInfo = await GetCacheInfoAsync();
                if (cacheInfo.TotalSizeBytes <= _maxCacheSizeBytes)
                    return;

                var packages = await GetCachedPackagesAsync();
                var currentSize = cacheInfo.TotalSizeBytes;

                // Remove oldest packages first
                foreach (var package in packages.OrderBy(p => p.LastAccessed))
                {
                    if (currentSize <= _maxCacheSizeBytes)
                        break;

                    if (Directory.Exists(package.Path))
                    {
                        Directory.Delete(package.Path, true);
                        currentSize -= package.SizeBytes;
                    }
                }

                _logger?.LogWithContext($"Enforced cache size limit. Removed oldest packages.", null);
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Error enforcing cache size limit: {ex.Message}", ex);
            }
        }
    }
}
