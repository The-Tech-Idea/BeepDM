using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.NuGet;
using TheTechIdea.Beep.NuGetManagement.Helpers;
 

namespace TheTechIdea.Beep.NuGetManagement.Services
{
    public class SearchService
    {
        private readonly IDMLogger _logger;
        private readonly NuGetSdkHelper _sdkHelper;
        private readonly SourceManager _sourceManager;

        public SearchService(IDMLogger logger, NuGetSdkHelper sdkHelper, SourceManager sourceManager)
        {
            _logger = logger;
            _sdkHelper = sdkHelper;
            _sourceManager = sourceManager;
        }

        public async Task<List<PackageSearchResult>> SearchAsync(string searchTerm, SearchOptions options = null)
        {
            var results = new List<PackageSearchResult>();
            options ??= new SearchOptions();

            if (string.IsNullOrWhiteSpace(searchTerm))
                return results;

            try
            {
                var sources = string.IsNullOrWhiteSpace(options.Source)
                    ? _sourceManager.GetActiveSourceUrls()
                    : new List<string> { options.Source };

                foreach (var sourceUrl in sources)
                {
                    try
                    {
                        if (NuGetSdkHelper.IsLocalSource(sourceUrl))
                        {
                            // Search local directory
                            var localResults = SearchLocalDirectory(sourceUrl, searchTerm, options);
                            results.AddRange(localResults);
                        }
                        else
                        {
                            // Search remote feed
                            var repository = _sdkHelper.CreateRepository(sourceUrl);
                            if (repository == null) continue;

                            var resource = await _sdkHelper.GetSearchResourceAsync(repository);
                            if (resource == null) continue;

                            var searchFilter = new SearchFilter(options.IncludePrerelease);
                            var packages = await resource.SearchAsync(
                                searchTerm,
                                searchFilter,
                                options.Skip,
                                options.Take,
                                _sdkHelper.NuGetLogger,
                                CancellationToken.None);

                            foreach (var package in packages)
                            {
                                if (results.Any(r => r.PackageId.Equals(package.Identity.Id, StringComparison.OrdinalIgnoreCase)))
                                    continue;

                                results.Add(new PackageSearchResult
                                {
                                    PackageId = package.Identity.Id,
                                    Version = package.Identity.Version?.ToNormalizedString(),
                                    Description = package.Description,
                                    Authors = package.Authors,
                                    DownloadCount = package.DownloadCount ?? 0,
                                    IconUrl = package.IconUrl?.ToString(),
                                    ProjectUrl = package.ProjectUrl?.ToString(),
                                    Tags = package.Tags?.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries)?.ToList() ?? new List<string>(),
                                    IsPrerelease = package.Identity.Version?.IsPrerelease ?? false,
                                    Source = sourceUrl
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWithContext($"Error searching {sourceUrl}: {ex.Message}", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Error searching packages: {ex.Message}", ex);
            }

            return results;
        }

        public async Task<List<string>> GetVersionsAsync(string packageId, bool includePrerelease = false, string source = null)
        {
            var results = new List<string>();

            try
            {
                var sources = string.IsNullOrWhiteSpace(source)
                    ? _sourceManager.GetActiveSourceUrls()
                    : new List<string> { source };

                foreach (var sourceUrl in sources)
                {
                    try
                    {
                        if (NuGetSdkHelper.IsLocalSource(sourceUrl))
                        {
                            var localVersions = GetLocalVersions(sourceUrl, packageId);
                            results.AddRange(localVersions);
                        }
                        else
                        {
                            var repository = _sdkHelper.CreateRepository(sourceUrl);
                            if (repository == null) continue;

                            var resource = await _sdkHelper.GetFindPackageResourceAsync(repository);
                            if (resource == null) continue;

                            var versions = await resource.GetAllVersionsAsync(
                                packageId,
                                _sdkHelper.CacheContext,
                                _sdkHelper.NuGetLogger,
                                CancellationToken.None);

                            if (versions == null) continue;

                            var versionList = versions
                                .Where(v => includePrerelease || !v.IsPrerelease)
                                .OrderByDescending(v => v)
                                .Select(v => v.ToNormalizedString())
                                .ToList();

                            results.AddRange(versionList);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWithContext($"Error getting versions from {sourceUrl}: {ex.Message}", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Error getting versions for {packageId}: {ex.Message}", ex);
            }

            return results.Distinct().ToList();
        }

        public async Task<PackageMetadata> GetMetadataAsync(string packageId, string version, string source = null)
        {
            try
            {
                var sources = string.IsNullOrWhiteSpace(source)
                    ? _sourceManager.GetActiveSourceUrls()
                    : new List<string> { source };

                foreach (var sourceUrl in sources)
                {
                    try
                    {
                        if (NuGetSdkHelper.IsLocalSource(sourceUrl))
                        {
                            var localMetadata = GetLocalMetadata(sourceUrl, packageId, version);
                            if (localMetadata != null) return localMetadata;
                        }
                        else
                        {
                            var repository = _sdkHelper.CreateRepository(sourceUrl);
                            if (repository == null) continue;

                            var resource = await _sdkHelper.GetMetadataResourceAsync(repository);
                            if (resource == null) continue;

                            // For remote sources, we'd need to download the package to read metadata
                            // This is a simplified version
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWithContext($"Error getting metadata from {sourceUrl}: {ex.Message}", ex);
                    }
                }

                // Fallback: try to read from cache
                var cachePath = _sdkHelper.GetPackagePathInCache(packageId, NuGetVersion.Parse(version));
                if (Directory.Exists(cachePath))
                {
                    var nupkgFiles = Directory.GetFiles(cachePath, "*.nupkg", SearchOption.TopDirectoryOnly);
                    if (nupkgFiles.Any())
                    {
                        using (var reader = _sdkHelper.OpenPackage(nupkgFiles.First()))
                        {
                            if (reader != null)
                            {
                                return await _sdkHelper.ReadPackageMetadataAsync(reader);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Error getting metadata for {packageId}: {ex.Message}", ex);
            }

            return null;
        }

        public async Task<List<PackageDependency>> GetDependenciesAsync(string packageId, string version, string source = null)
        {
            try
            {
                var metadata = await GetMetadataAsync(packageId, version, source);
                return metadata?.Dependencies ?? new List<PackageDependency>();
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Error getting dependencies for {packageId}: {ex.Message}", ex);
                return new List<PackageDependency>();
            }
        }

        private List<PackageSearchResult> SearchLocalDirectory(string directoryPath, string searchTerm, SearchOptions options)
        {
            var results = new List<PackageSearchResult>();

            try
            {
                if (!Directory.Exists(directoryPath))
                    return results;

                var nupkgFiles = Directory.GetFiles(directoryPath, "*.nupkg", SearchOption.AllDirectories);
                
                foreach (var file in nupkgFiles)
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    if (options.ExactMatch)
                    {
                        if (!fileName.StartsWith(searchTerm, StringComparison.OrdinalIgnoreCase))
                            continue;
                    }
                    else
                    {
                        if (!fileName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                            continue;
                    }

                    // Parse package info from filename
                    var parts = fileName.Split('.');
                    if (parts.Length < 2) continue;

                    // Try to extract version from end
                    string packageName = null;
                    string version = null;
                    for (int i = parts.Length - 1; i >= 1; i--)
                    {
                        var versionStr = string.Join(".", parts.Skip(i));
                        if (System.Version.TryParse(versionStr, out _) || versionStr.Split('.').All(p => int.TryParse(p, out _)))
                        {
                            packageName = string.Join(".", parts.Take(i));
                            version = versionStr;
                            break;
                        }
                    }

                    if (string.IsNullOrEmpty(packageName)) continue;

                    results.Add(new PackageSearchResult
                    {
                        PackageId = packageName,
                        Version = version,
                        Description = $"Local package: {fileName}",
                        Source = directoryPath,
                        IsPrerelease = version?.Contains('-') ?? false
                    });
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Error searching local directory {directoryPath}: {ex.Message}", ex);
            }

            return results;
        }

        private List<string> GetLocalVersions(string directoryPath, string packageId)
        {
            var versions = new List<string>();

            try
            {
                if (!Directory.Exists(directoryPath))
                    return versions;

                var pattern = $"{packageId}.*.nupkg";
                var files = Directory.GetFiles(directoryPath, pattern, SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var parts = fileName.Split('.');
                    for (int i = parts.Length - 1; i >= 1; i--)
                    {
                        var versionStr = string.Join(".", parts.Skip(i));
                        if (System.Version.TryParse(versionStr, out _) || versionStr.Split('.').All(p => int.TryParse(p, out _)))
                        {
                            versions.Add(versionStr);
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Error getting local versions: {ex.Message}", ex);
            }

            return versions.Distinct().OrderByDescending(v => v).ToList();
        }

        private PackageMetadata GetLocalMetadata(string directoryPath, string packageId, string version)
        {
            try
            {
                var nupkgPath = Path.Combine(directoryPath, $"{packageId}.{version}.nupkg");
                if (!File.Exists(nupkgPath))
                    return null;

                using (var reader = _sdkHelper.OpenPackage(nupkgPath))
                {
                    if (reader != null)
                    {
                        // Task.Run keeps the read's awaits off the caller's SynchronizationContext,
                        // so a UI caller blocked here in GetResult() cannot deadlock against them.
                        return Task.Run(() => _sdkHelper.ReadPackageMetadataAsync(reader)).GetAwaiter().GetResult();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Error reading local metadata: {ex.Message}", ex);
            }

            return null;
        }
    }
}
