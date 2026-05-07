using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using TheTechIdea.Beep.Logger;
 

namespace TheTechIdea.Beep.NuGetManagement.Helpers
{
    public class NuGetSdkHelper
    {
        private readonly IDMLogger _logger;
        private readonly ILogger _nugetLogger;
        private readonly SourceCacheContext _cacheContext;
        private readonly NuGetFramework _targetFramework;

        public NuGetSdkHelper(IDMLogger logger)
        {
            _logger = logger;
            _nugetLogger = NullLogger.Instance;
            _cacheContext = new SourceCacheContext();
            _targetFramework = DetectTargetFramework();
        }

        public NuGetFramework TargetFramework => _targetFramework;
        public SourceCacheContext CacheContext => _cacheContext;
        public ILogger NuGetLogger => _nugetLogger;

        public SourceRepository CreateRepository(string sourceUrl)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sourceUrl))
                    return null;

                if (IsLocalSource(sourceUrl))
                {
                    var settings = Settings.LoadDefaultSettings(null);
                    var packageSource = new PackageSource(sourceUrl);
                    var providers = new System.Collections.Generic.List<INuGetResourceProvider>
                    {
                        new LocalV2FindPackageByIdResourceProvider(),
                        new LocalV3FindPackageByIdResourceProvider()
                    };
                    return new SourceRepository(packageSource, providers);
                }

                return Repository.Factory.GetCoreV3(sourceUrl);
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Error creating repository for {sourceUrl}: {ex.Message}", ex);
                return null;
            }
        }

        public async Task<FindPackageByIdResource> GetFindPackageResourceAsync(SourceRepository repository)
        {
            if (repository == null) return null;
            try
            {
                return await repository.GetResourceAsync<FindPackageByIdResource>();
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Error getting FindPackageByIdResource: {ex.Message}", ex);
                return null;
            }
        }

        public async Task<PackageSearchResource> GetSearchResourceAsync(SourceRepository repository)
        {
            if (repository == null) return null;
            try
            {
                return await repository.GetResourceAsync<PackageSearchResource>();
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Error getting PackageSearchResource: {ex.Message}", ex);
                return null;
            }
        }

        public async Task<PackageMetadataResource> GetMetadataResourceAsync(SourceRepository repository)
        {
            if (repository == null) return null;
            try
            {
                return await repository.GetResourceAsync<PackageMetadataResource>();
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Error getting PackageMetadataResource: {ex.Message}", ex);
                return null;
            }
        }

        public async Task<bool> DownloadPackageAsync(FindPackageByIdResource resource, string packageId, NuGetVersion version, string outputPath)
        {
            try
            {
                var packagePath = Path.Combine(outputPath, $"{packageId.ToLowerInvariant()}.{version.ToNormalizedString()}.nupkg");
                
                using (var packageStream = new MemoryStream())
                {
                    var success = await resource.CopyNupkgToStreamAsync(
                        packageId,
                        version,
                        packageStream,
                        _cacheContext,
                        _nugetLogger,
                        CancellationToken.None);

                    if (!success)
                    {
                        _logger?.LogWithContext($"Failed to download {packageId} {version}", null);
                        return false;
                    }

                    packageStream.Position = 0;
                    using (var fileStream = File.Create(packagePath))
                    {
                        await packageStream.CopyToAsync(fileStream);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Error downloading package {packageId}: {ex.Message}", ex);
                return false;
            }
        }

        public PackageArchiveReader OpenPackage(string nupkgPath)
        {
            try
            {
                if (!File.Exists(nupkgPath))
                    return null;

                var stream = File.OpenRead(nupkgPath);
                return new PackageArchiveReader(stream);
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Error opening package {nupkgPath}: {ex.Message}", ex);
                return null;
            }
        }

        public async Task<PackageMetadata> ReadPackageMetadataAsync(PackageArchiveReader reader)
        {
            try
            {
                using (var nuspecStream = await reader.GetNuspecAsync(CancellationToken.None))
                {
                    var nuspec = new NuspecReader(nuspecStream);
                    var metadata = new PackageMetadata
                    {
                        PackageId = nuspec.GetId(),
                        Version = nuspec.GetVersion().ToNormalizedString(),
                        Description = nuspec.GetDescription(),
                        Authors = nuspec.GetAuthors(),
                        IconUrl = nuspec.GetIconUrl()?.ToString(),
                        ProjectUrl = nuspec.GetProjectUrl()?.ToString(),
                        LicenseUrl = nuspec.GetLicenseUrl()?.ToString(),
                        LicenseType = nuspec.GetLicenseMetadata()?.Type.ToString(),
                        Tags = nuspec.GetTags()?.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries)?.ToList() ?? new System.Collections.Generic.List<string>(),
                        TargetFramework = _targetFramework.GetShortFolderName(),
                        IsPrerelease = nuspec.GetVersion().IsPrerelease
                    };

                    var dependencyGroups = nuspec.GetDependencyGroups();
                    foreach (var group in dependencyGroups)
                    {
                        var framework = group.TargetFramework.GetShortFolderName();
                        foreach (var dep in group.Packages)
                        {
                            metadata.Dependencies.Add(new PackageDependency
                            {
                                PackageId = dep.Id,
                                VersionRange = dep.VersionRange?.ToNormalizedString(),
                                TargetFramework = framework
                            });
                        }
                    }

                    return metadata;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Error reading package metadata: {ex.Message}", ex);
                return null;
            }
        }

        public string GetGlobalPackagesFolder()
        {
            var envPath = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
            if (!string.IsNullOrEmpty(envPath) && Directory.Exists(envPath))
                return envPath;

            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(userProfile, ".nuget", "packages");
        }

        public string GetPackagePathInCache(string packageId, NuGetVersion version)
        {
            var globalFolder = GetGlobalPackagesFolder();
            return Path.Combine(globalFolder, packageId.ToLowerInvariant(), version.ToNormalizedString());
        }

        public bool IsPackageInCache(string packageId, NuGetVersion version)
        {
            var packagePath = GetPackagePathInCache(packageId, version);
            var nupkgPath = Path.Combine(packagePath, $"{packageId.ToLowerInvariant()}.{version.ToNormalizedString()}.nupkg");
            return Directory.Exists(packagePath) && File.Exists(nupkgPath);
        }

        public static bool IsLocalSource(string sourceUrl)
        {
            if (string.IsNullOrWhiteSpace(sourceUrl))
                return false;

            if (sourceUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                sourceUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return false;

            return Directory.Exists(sourceUrl) || File.Exists(sourceUrl) || 
                   sourceUrl.Contains(Path.DirectorySeparatorChar) || sourceUrl.Contains('/');
        }

        public static bool IsSystemPackage(string packageName)
        {
            if (string.IsNullOrWhiteSpace(packageName)) return false;
            
            var systemPackages = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "System.Runtime", "System.Collections", "System.Linq", "System.Threading",
                "System.Threading.Tasks", "System.IO", "System.Reflection",
                "Microsoft.NETCore.Platforms", "Microsoft.NETCore.Targets", "NETStandard.Library",
                "Microsoft.CSharp", "System.Dynamic.Runtime", "System.Linq.Expressions"
            };

            return systemPackages.Contains(packageName) ||
                   packageName.StartsWith("System.", StringComparison.OrdinalIgnoreCase) ||
                   packageName.StartsWith("Microsoft.NETCore", StringComparison.OrdinalIgnoreCase) ||
                   packageName.StartsWith("Microsoft.Extensions", StringComparison.OrdinalIgnoreCase);
        }

        private NuGetFramework DetectTargetFramework()
        {
            var frameworkDescription = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
            if (frameworkDescription.Contains(".NET 10"))
                return NuGetFramework.Parse("net10.0");
            if (frameworkDescription.Contains(".NET 9"))
                return NuGetFramework.Parse("net9.0");
            if (frameworkDescription.Contains(".NET 8"))
                return NuGetFramework.Parse("net8.0");
            if (frameworkDescription.Contains(".NET 7"))
                return NuGetFramework.Parse("net7.0");
            if (frameworkDescription.Contains(".NET 6"))
                return NuGetFramework.Parse("net6.0");
            return NuGetFramework.Parse("net8.0");
        }
    }
}
