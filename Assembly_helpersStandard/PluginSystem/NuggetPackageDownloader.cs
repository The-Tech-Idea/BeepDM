using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using NuGet.Common;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using TheTechIdea.Beep.Logger;

namespace TheTechIdea.Beep.Tools.PluginSystem
{
    /// <summary>
    /// NuGet package downloader using official NuGet SDK.
    /// Uses standard NuGet global packages folder for caching.
    /// </summary>
    public class NuggetPackageDownloader
    {
        private readonly string _globalPackagesFolder;
        private readonly IDMLogger _logger;
        private readonly NuGetFramework _targetFramework;
        private readonly SourceCacheContext _cacheContext;
        private readonly ILogger _nugetLogger;

        // System packages that should be skipped during dependency resolution
        private static readonly HashSet<string> SystemPackages = new(StringComparer.OrdinalIgnoreCase)
        {
            "System.Runtime", "System.Collections", "System.Linq", "System.Threading",
            "System.Threading.Tasks", "System.Text.Encoding", "System.IO", "System.Reflection",
            "System.Diagnostics.Debug", "System.Diagnostics.Tools", "System.Diagnostics.Process",
            "System.ComponentModel", "System.ComponentModel.Primitives", "System.ComponentModel.TypeConverter",
            "System.ObjectModel", "System.Globalization", "System.Resources.ResourceManager",
            "System.Runtime.Extensions", "System.Runtime.InteropServices", "System.Runtime.InteropServices.RuntimeInformation",
            "System.Text.RegularExpressions", "System.Xml.ReaderWriter", "System.Xml.XDocument",
            "System.Net.Primitives", "System.Net.Http", "System.Security.Cryptography.Algorithms",
            "System.Security.Cryptography.Primitives", "System.Security.Principal",
            "Microsoft.NETCore.Platforms", "Microsoft.NETCore.Targets", "NETStandard.Library",
            "Microsoft.CSharp", "System.Dynamic.Runtime", "System.Linq.Expressions",
            "System.Memory", "System.Buffers", "System.Numerics.Vectors",
            "System.Runtime.CompilerServices.Unsafe", "System.Threading.Tasks.Extensions",
            "System.ValueTuple", "System.Collections.Immutable", "System.Collections.Concurrent"
        };

        /// <summary>
        /// Gets the standard NuGet global packages folder path.
        /// Default: %USERPROFILE%\.nuget\packages (Windows) or ~/.nuget/packages (Unix)
        /// </summary>
        public static string GetDefaultGlobalPackagesFolder()
        {
            var envPath = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
            if (!string.IsNullOrEmpty(envPath) && Directory.Exists(envPath))
            {
                return envPath;
            }
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(userProfile, ".nuget", "packages");
        }

        /// <summary>
        /// Creates a NuggetPackageDownloader using the standard NuGet global packages folder.
        /// </summary>
        public NuggetPackageDownloader(IDMLogger logger) : this(null, logger)
        {
        }

        /// <summary>
        /// Creates a NuggetPackageDownloader with optional custom packages folder.
        /// </summary>
        public NuggetPackageDownloader(string? globalPackagesFolder, IDMLogger logger)
        {
            _globalPackagesFolder = globalPackagesFolder ?? GetDefaultGlobalPackagesFolder();
            _logger = logger;
            _nugetLogger = NullLogger.Instance;
            _cacheContext = new SourceCacheContext();
            _targetFramework = DetectTargetFramework();

            _logger?.LogWithContext($"Using NuGet global packages folder: {_globalPackagesFolder}", null);

            if (!Directory.Exists(_globalPackagesFolder))
                Directory.CreateDirectory(_globalPackagesFolder);
        }

        /// <summary>
        /// Gets the package path in the global packages folder.
        /// Format: {globalPackagesFolder}/{packageId.ToLower()}/{version}/
        /// </summary>
        private string GetPackagePath(string packageId, NuGetVersion version)
        {
            return Path.Combine(_globalPackagesFolder, packageId.ToLowerInvariant(), version.ToNormalizedString());
        }

        /// <summary>
        /// Checks if a package already exists in the global packages folder.
        /// </summary>
        public bool IsPackageInCache(string packageId, NuGetVersion version)
        {
            var packagePath = GetPackagePath(packageId, version);
            var nupkgPath = Path.Combine(packagePath, $"{packageId.ToLowerInvariant()}.{version.ToNormalizedString()}.nupkg");
            return Directory.Exists(packagePath) && File.Exists(nupkgPath);
        }

        /// <summary>
        /// Downloads a package using NuGet SDK directly.
        /// </summary>
        public async Task<string?> DownloadPackageAsync(string packageName, string? version = null, IEnumerable<string>? sources = null)
        {
            try
            {
                var sourceList = (sources ?? new[] { "https://api.nuget.org/v3/index.json" }).ToList();
                NuGetVersion? nugetVersion = null;

                if (!string.IsNullOrWhiteSpace(version))
                {
                    if (!NuGetVersion.TryParse(version, out nugetVersion))
                    {
                        _logger?.LogWithContext($"Invalid version format: {version}", null);
                        return null;
                    }
                }

                // Check cache first
                if (nugetVersion != null && IsPackageInCache(packageName, nugetVersion))
                {
                    var cachedPath = GetPackagePath(packageName, nugetVersion);
                    _logger?.LogWithContext($"✓ Using cached package: {packageName} {nugetVersion}", null);
                    return cachedPath;
                }

                // Try each source
                foreach (var sourceUrl in sourceList)
                {
                    try
                    {
                        var repository = Repository.Factory.GetCoreV3(sourceUrl);
                        var resource = await repository.GetResourceAsync<FindPackageByIdResource>();

                        // Get version if not specified
                        if (nugetVersion == null)
                        {
                            var versions = await resource.GetAllVersionsAsync(packageName, _cacheContext, _nugetLogger, CancellationToken.None);
                            nugetVersion = versions?.OrderByDescending(v => v).FirstOrDefault();
                            if (nugetVersion == null)
                            {
                                _logger?.LogWithContext($"No versions found for {packageName} on {sourceUrl}", null);
                                continue;
                            }
                        }

                        // Check cache again with resolved version
                        if (IsPackageInCache(packageName, nugetVersion))
                        {
                            var cachedPath = GetPackagePath(packageName, nugetVersion);
                            _logger?.LogWithContext($"✓ Using cached package: {packageName} {nugetVersion}", null);
                            return cachedPath;
                        }

                        // Download package
                        var packagePath = GetPackagePath(packageName, nugetVersion);
                        Directory.CreateDirectory(packagePath);

                        var nupkgFileName = $"{packageName.ToLowerInvariant()}.{nugetVersion.ToNormalizedString()}.nupkg";
                        var nupkgPath = Path.Combine(packagePath, nupkgFileName);

                        _logger?.LogWithContext($"Downloading {packageName} {nugetVersion} from {sourceUrl}...", null);

                        using (var packageStream = new MemoryStream())
                        {
                            var success = await resource.CopyNupkgToStreamAsync(
                                packageName,
                                nugetVersion,
                                packageStream,
                                _cacheContext,
                                _nugetLogger,
                                CancellationToken.None);

                            if (!success)
                            {
                                _logger?.LogWithContext($"Failed to download {packageName} from {sourceUrl}", null);
                                continue;
                            }

                            // Save nupkg
                            packageStream.Position = 0;
                            using (var fileStream = File.Create(nupkgPath))
                            {
                                await packageStream.CopyToAsync(fileStream);
                            }

                            // Extract package
                            packageStream.Position = 0;
                            using (var zipArchive = new ZipArchive(packageStream, ZipArchiveMode.Read))
                            {
                                zipArchive.ExtractToDirectory(packagePath, overwriteFiles: true);
                            }
                        }

                        _logger?.LogWithContext($"✓ Downloaded {packageName} {nugetVersion}", null);
                        return packagePath;
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWithContext($"Error downloading from {sourceUrl}: {ex.Message}", null);
                    }
                }

                _logger?.LogWithContext($"Failed to download {packageName} from any source", null);
                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Error downloading package {packageName}: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// Downloads a package with all its dependencies.
        /// </summary>
        public async Task<Dictionary<string, string>> DownloadPackageWithDependenciesAsync(
            string packageName,
            string? version = null,
            IEnumerable<string>? sources = null,
            HashSet<string>? processedPackages = null)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            processedPackages ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var sourceList = (sources ?? new[] { "https://api.nuget.org/v3/index.json" }).ToList();

            try
            {
                // Download main package
                var packagePath = await DownloadPackageAsync(packageName, version, sourceList);
                if (string.IsNullOrEmpty(packagePath))
                {
                    _logger?.LogWithContext($"Failed to download main package: {packageName}", null);
                    return result;
                }

                processedPackages.Add(packageName.ToLowerInvariant());

                // Find compatible DLLs
                var compatiblePath = FindCompatibleFrameworkFolder(packagePath);
                if (!string.IsNullOrEmpty(compatiblePath))
                {
                    result[packageName] = compatiblePath;
                }
                else
                {
                    result[packageName] = packagePath;
                }

                // Get dependencies from nuspec
                var dependencies = GetPackageDependencies(packagePath);
                _logger?.LogWithContext($"Found {dependencies.Count} dependencies for {packageName}", null);

                // Download dependencies
                foreach (var (depName, depVersion) in dependencies)
                {
                    if (processedPackages.Contains(depName.ToLowerInvariant()))
                        continue;

                    if (IsSystemPackage(depName))
                        continue;

                    if (IsPackageLoadedInMemory(depName))
                    {
                        _logger?.LogWithContext($"Skipping {depName} - already loaded in memory", null);
                        continue;
                    }

                    var depResults = await DownloadPackageWithDependenciesAsync(depName, depVersion, sourceList, processedPackages);
                    foreach (var kvp in depResults)
                    {
                        if (!result.ContainsKey(kvp.Key))
                        {
                            result[kvp.Key] = kvp.Value;
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Error downloading {packageName} with dependencies: {ex.Message}", ex);
                return result;
            }
        }

        /// <summary>
        /// Searches for available versions of a package.
        /// </summary>
        public async Task<string[]> SearchPackageVersionsAsync(string packageName, IEnumerable<string>? sources = null)
        {
            try
            {
                var sourceUrl = sources?.FirstOrDefault() ?? "https://api.nuget.org/v3/index.json";
                var repository = Repository.Factory.GetCoreV3(sourceUrl);
                var resource = await repository.GetResourceAsync<FindPackageByIdResource>();

                var versions = await resource.GetAllVersionsAsync(packageName, _cacheContext, _nugetLogger, CancellationToken.None);
                return versions?.OrderByDescending(v => v).Take(20).Select(v => v.ToNormalizedString()).ToArray() ?? Array.Empty<string>();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// Extracts a NuGet package.
        /// </summary>
        public string? ExtractNuGetPackage(string nupkgPath)
        {
            try
            {
                if (!File.Exists(nupkgPath))
                {
                    _logger?.LogWithContext($"Package file not found: {nupkgPath}", null);
                    return null;
                }

                var extractDir = Path.GetDirectoryName(nupkgPath);
                if (string.IsNullOrEmpty(extractDir))
                {
                    extractDir = Path.Combine(_globalPackagesFolder, Path.GetFileNameWithoutExtension(nupkgPath));
                }

                ZipFile.ExtractToDirectory(nupkgPath, extractDir, overwriteFiles: true);
                _logger?.LogWithContext($"Package extracted: {extractDir}", null);

                var compatible = FindCompatibleFrameworkFolder(extractDir);
                return !string.IsNullOrEmpty(compatible) ? compatible : extractDir;
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Error extracting package: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// Installs package files to the application directory.
        /// </summary>
        public string? InstallPackageToAppDirectory(string packageFrameworkPath, string? appDirectory, string? pluginId = null, string? version = null, bool overwrite = false)
        {
            if (string.IsNullOrWhiteSpace(packageFrameworkPath) || !Directory.Exists(packageFrameworkPath))
            {
                _logger?.LogWithContext($"InstallPackageToAppDirectory: invalid source path {packageFrameworkPath}", null);
                return null;
            }

            var baseTargets = appDirectory ?? Path.Combine(AppContext.BaseDirectory, "Plugins");
            var pluginFolderName = pluginId ?? Path.GetFileName(packageFrameworkPath).Replace(".", "_");
            var ver = !string.IsNullOrWhiteSpace(version) ? version : DateTime.UtcNow.Ticks.ToString();
            var targetDir = Path.Combine(baseTargets, pluginFolderName, ver);

            try
            {
                if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

                foreach (var file in Directory.GetFiles(packageFrameworkPath, "*.dll", SearchOption.AllDirectories))
                {
                    var dest = Path.Combine(targetDir, Path.GetFileName(file));
                    if (File.Exists(dest) && !overwrite) continue;
                    File.Copy(file, dest, overwrite);
                }

                _logger?.LogWithContext($"Installed package to {targetDir}", null);
                return targetDir;
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"InstallPackageToAppDirectory failed: {ex.Message}", ex);
                return null;
            }
        }

        #region Private Helper Methods

        private NuGetFramework DetectTargetFramework()
        {
            var frameworkDescription = RuntimeInformation.FrameworkDescription;
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

        private bool IsPackageLoadedInMemory(string packageId)
        {
            if (string.IsNullOrWhiteSpace(packageId))
                return false;

            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            return loadedAssemblies.Any(a =>
                a.GetName().Name?.Equals(packageId, StringComparison.OrdinalIgnoreCase) == true ||
                a.GetName().Name?.StartsWith(packageId + ".", StringComparison.OrdinalIgnoreCase) == true);
        }

        private bool IsSystemPackage(string packageName)
        {
            if (string.IsNullOrWhiteSpace(packageName)) return false;
            return SystemPackages.Contains(packageName) ||
                   packageName.StartsWith("System.", StringComparison.OrdinalIgnoreCase) ||
                   packageName.StartsWith("Microsoft.NETCore", StringComparison.OrdinalIgnoreCase) ||
                   packageName.StartsWith("Microsoft.Extensions", StringComparison.OrdinalIgnoreCase);
        }

        private List<(string Name, string? Version)> GetPackageDependencies(string packagePath)
        {
            var dependencies = new List<(string, string?)>();
            try
            {
                var nuspecFiles = Directory.GetFiles(packagePath, "*.nuspec", SearchOption.AllDirectories);
                if (!nuspecFiles.Any()) return dependencies;

                var nuspecPath = nuspecFiles.First();
                var doc = XDocument.Load(nuspecPath);
                var ns = doc.Root?.GetDefaultNamespace();
                if (ns == null) return dependencies;

                // Get framework-specific dependencies
                var dependencyGroups = doc.Descendants(ns + "dependencies").Elements(ns + "group");
                foreach (var group in dependencyGroups)
                {
                    var targetFramework = group.Attribute("targetFramework")?.Value;
                    if (!string.IsNullOrEmpty(targetFramework) && !IsCompatibleFramework(targetFramework)) continue;

                    foreach (var dep in group.Elements(ns + "dependency"))
                    {
                        var depName = dep.Attribute("id")?.Value;
                        var depVersion = dep.Attribute("version")?.Value;
                        if (!string.IsNullOrEmpty(depName) && !IsSystemPackage(depName))
                        {
                            dependencies.Add((depName, depVersion));
                        }
                    }
                }

                // Get direct dependencies (no group)
                var directDeps = doc.Descendants(ns + "dependencies").Elements(ns + "dependency");
                foreach (var dep in directDeps)
                {
                    var depName = dep.Attribute("id")?.Value;
                    var depVersion = dep.Attribute("version")?.Value;
                    if (!string.IsNullOrEmpty(depName) && !IsSystemPackage(depName))
                    {
                        dependencies.Add((depName, depVersion));
                    }
                }

                return dependencies.Distinct().ToList();
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Error parsing nuspec: {ex.Message}", ex);
                return dependencies;
            }
        }

        private bool IsCompatibleFramework(string targetFramework)
        {
            var compatible = new[] { "net10.0", "net9.0", "net8.0", "net7.0", "net6.0", "net5.0", "netcoreapp3.1", "netstandard2.1", "netstandard2.0" };
            return compatible.Any(f => targetFramework.StartsWith(f, StringComparison.OrdinalIgnoreCase) ||
                                       targetFramework.Contains(f, StringComparison.OrdinalIgnoreCase));
        }

        private string? FindCompatibleFrameworkFolder(string packageDirectory)
        {
            try
            {
                var libPath = Path.Combine(packageDirectory, "lib");
                if (Directory.Exists(libPath))
                {
                    var match = FindBestFrameworkMatch(libPath);
                    if (!string.IsNullOrEmpty(match)) return match;
                }

                var runtimesPath = Path.Combine(packageDirectory, "runtimes");
                if (Directory.Exists(runtimesPath))
                {
                    var runtimePreference = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                        ? new[] { "win-x64", "win-x86", "win", "any" }
                        : new[] { "linux-x64", "unix", "any" };

                    foreach (var runtimeId in runtimePreference)
                    {
                        var runtimeDir = Path.Combine(runtimesPath, runtimeId);
                        if (Directory.Exists(runtimeDir))
                        {
                            var runtimeLib = Path.Combine(runtimeDir, "lib");
                            if (Directory.Exists(runtimeLib))
                            {
                                var match = FindBestFrameworkMatch(runtimeLib);
                                if (!string.IsNullOrEmpty(match)) return match;
                            }

                            var nativeDir = Path.Combine(runtimeDir, "native");
                            if (Directory.Exists(nativeDir) && Directory.GetFiles(nativeDir, "*.dll").Any())
                            {
                                return nativeDir;
                            }
                        }
                    }
                }

                // Check for DLLs directly in package root
                if (Directory.GetFiles(packageDirectory, "*.dll", SearchOption.TopDirectoryOnly).Any())
                {
                    return packageDirectory;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Error finding compatible framework: {ex.Message}", ex);
                return null;
            }
        }

        private string? FindBestFrameworkMatch(string libFolder)
        {
            var preferredOrder = new[] { "net10.0", "net9.0", "net8.0", "net8.0-windows", "net7.0", "net6.0", "net5.0", "netcoreapp3.1", "netstandard2.1", "netstandard2.0" };
            var frameworkFolders = Directory.Exists(libFolder) ? Directory.GetDirectories(libFolder) : Array.Empty<string>();

            foreach (var preferred in preferredOrder)
            {
                var targetFolder = frameworkFolders.FirstOrDefault(f => Path.GetFileName(f).Equals(preferred, StringComparison.OrdinalIgnoreCase));
                if (targetFolder != null && Directory.GetFiles(targetFolder, "*.dll").Any())
                {
                    return targetFolder;
                }
            }

            return frameworkFolders.FirstOrDefault(d => Directory.GetFiles(d, "*.dll").Any());
        }

        #endregion
    }
}
