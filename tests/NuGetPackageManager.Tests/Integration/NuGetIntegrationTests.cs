using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.NuGetManagement;
using TheTechIdea.Beep.Tools.PluginSystem;
using Xunit;
using Xunit.Abstractions;

namespace TheTechIdea.Beep.NuGetManagement.Tests.Integration
{
    /// <summary>
    /// Integration tests that verify NuGetPackageManager works with real NuGet feeds.
    /// These tests require internet connectivity and may be slow.
    /// </summary>
    public class NuGetIntegrationTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly DMLogger _logger;
        private readonly SharedContextManager _sharedContextManager;
        private readonly NuGetPackageManager _manager;
        private readonly string _testDirectory;

        public NuGetIntegrationTests(ITestOutputHelper output)
        {
            _output = output;
            _testDirectory = Path.Combine(Path.GetTempPath(), $"NuGetIntegration_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDirectory);
            
            _logger = new DMLogger();
            _logger.LogWithContext("Integration test initialized", null);
            
            var registry = new PluginRegistry(_testDirectory, _logger);
            _sharedContextManager = new SharedContextManager(_logger, true, registry);
            _manager = new NuGetPackageManager(_logger, _sharedContextManager, _testDirectory);
        }

        public void Dispose()
        {
            _manager?.Dispose();
            _sharedContextManager?.Dispose();
            
            try
            {
                if (Directory.Exists(_testDirectory))
                    Directory.Delete(_testDirectory, true);
            }
            catch { }
        }

        [Fact]
        public async Task SearchAsync_RealNuGetOrg_ShouldReturnResults()
        {
            // Arrange
            var searchTerm = "Newtonsoft.Json";

            // Act
            var results = await _manager.SearchAsync(searchTerm, new Models.SearchOptions { Take = 5 });

            // Assert
            results.Should().NotBeNullOrEmpty();
            results.Should().Contain(r => r.PackageId.Equals("Newtonsoft.Json", StringComparison.OrdinalIgnoreCase));
            
            foreach (var result in results)
            {
                _output.WriteLine($"Found: {result.PackageId} v{result.Version} - {result.Description}");
            }
        }

        [Fact]
        public async Task GetVersionsAsync_RealPackage_ShouldReturnVersions()
        {
            // Arrange
            var packageId = "Newtonsoft.Json";

            // Act
            var versions = await _manager.GetVersionsAsync(packageId);

            // Assert
            versions.Should().NotBeNullOrEmpty();
            versions.Should().Contain(v => v.StartsWith("13."));
            
            _output.WriteLine($"Found {versions.Count} versions of {packageId}");
            foreach (var version in versions.Take(5))
            {
                _output.WriteLine($"  - {version}");
            }
        }

        [Fact]
        public async Task GetMetadataAsync_RealPackage_ShouldReturnMetadata()
        {
            // Arrange
            var packageId = "Newtonsoft.Json";
            var version = "13.0.1";

            // Act
            var metadata = await _manager.GetMetadataAsync(packageId, version);

            // Assert
            metadata.Should().NotBeNull();
            metadata.PackageId.Should().Be(packageId);
            metadata.Version.Should().Be(version);
            metadata.Description.Should().NotBeNullOrEmpty();
            metadata.Authors.Should().NotBeNullOrEmpty();
            
            _output.WriteLine($"Package: {metadata.PackageId}");
            _output.WriteLine($"Version: {metadata.Version}");
            _output.WriteLine($"Description: {metadata.Description}");
            _output.WriteLine($"Authors: {metadata.Authors}");
            _output.WriteLine($"Dependencies: {metadata.Dependencies?.Count ?? 0}");
        }

        [Fact]
        public async Task GetDependenciesAsync_RealPackage_ShouldReturnDependencies()
        {
            // Arrange
            var packageId = "Microsoft.Extensions.DependencyInjection";
            var version = "8.0.0";

            // Act
            var dependencies = await _manager.GetDependenciesAsync(packageId, version);

            // Assert
            dependencies.Should().NotBeNull();
            
            _output.WriteLine($"Found {dependencies.Count} dependencies for {packageId} {version}");
            foreach (var dep in dependencies.Take(10))
            {
                _output.WriteLine($"  - {dep.PackageId} ({dep.VersionRange})");
            }
        }

        [Fact]
        public async Task DownloadAsync_SmallPackage_ShouldDownloadSuccessfully()
        {
            // Arrange
            var packageId = "Newtonsoft.Json";
            var version = "13.0.1";

            // Act
            var result = await _manager.DownloadAsync(packageId, version);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.PackageId.Should().Be(packageId);
            result.Version.Should().Be(version);
            result.InstallPath.Should().NotBeNullOrEmpty();
            
            // Verify files exist
            Directory.Exists(result.InstallPath).Should().BeTrue();
            var nupkgFiles = Directory.GetFiles(result.InstallPath, "*.nupkg");
            nupkgFiles.Should().NotBeEmpty();
            
            _output.WriteLine($"Downloaded to: {result.InstallPath}");
            _output.WriteLine($"Duration: {result.DurationMs}ms");
        }

        [Fact]
        public async Task DownloadAsync_LatestVersion_ShouldResolveAndDownload()
        {
            // Arrange
            var packageId = "Newtonsoft.Json";

            // Act
            var result = await _manager.DownloadAsync(packageId);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.PackageId.Should().Be(packageId);
            result.Version.Should().NotBeNullOrEmpty();
            
            _output.WriteLine($"Resolved and downloaded version: {result.Version}");
        }

        [Fact]
        public async Task InstallAsync_RealPackage_ShouldInstallToDirectory()
        {
            // Arrange
            var packageId = "Newtonsoft.Json";
            var version = "13.0.1";

            // Act
            var result = await _manager.InstallAsync(packageId, version, _testDirectory);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.InstallPath.Should().Contain(packageId);
            
            // Verify DLL was extracted
            var dllFiles = Directory.GetFiles(result.InstallPath, "*.dll", SearchOption.AllDirectories);
            dllFiles.Should().NotBeEmpty();
            dllFiles.Should().Contain(f => f.Contains("Newtonsoft.Json.dll"));
            
            _output.WriteLine($"Installed to: {result.InstallPath}");
            _output.WriteLine($"Files installed: {result.InstalledFiles.Count}");
            foreach (var file in result.InstalledFiles.Take(5))
            {
                _output.WriteLine($"  - {file}");
            }
        }

        [Fact]
        public async Task IsCached_AfterDownload_ShouldReturnTrue()
        {
            // Arrange
            var packageId = "Newtonsoft.Json";
            var version = "13.0.1";
            await _manager.DownloadAsync(packageId, version);

            // Act
            var isCached = _manager.IsCached(packageId, version);

            // Assert
            isCached.Should().BeTrue();
        }

        [Fact]
        public async Task GetCachedPackagesAsync_AfterDownload_ShouldIncludePackage()
        {
            // Arrange
            var packageId = "Newtonsoft.Json";
            var version = "13.0.1";
            await _manager.DownloadAsync(packageId, version);

            // Act
            var cached = await _manager.GetCachedPackagesAsync();

            // Assert
            cached.Should().Contain(p => 
                p.PackageId.Equals(packageId, StringComparison.OrdinalIgnoreCase) && 
                p.Version == version);
        }

        [Fact]
        public async Task FullLifecycle_DownloadInstallUpdateUninstall()
        {
            // Arrange
            var packageId = "Newtonsoft.Json";
            var version = "13.0.1";
            var installPath = Path.Combine(_testDirectory, "LifecycleTest");
            Directory.CreateDirectory(installPath);

            // Act - Download
            var downloadResult = await _manager.DownloadAsync(packageId, version);
            downloadResult.Success.Should().BeTrue();
            _output.WriteLine($"1. Downloaded: {downloadResult.Success}");

            // Act - Install
            var installResult = await _manager.InstallAsync(packageId, version, installPath);
            installResult.Success.Should().BeTrue();
            _output.WriteLine($"2. Installed: {installResult.Success}");

            // Act - Verify installed
            var isInstalled = await _manager.IsInstalledAsync(packageId, installPath);
            isInstalled.Should().BeTrue();
            _output.WriteLine($"3. IsInstalled: {isInstalled}");

            // Act - Get installed packages
            var installed = await _manager.GetInstalledPackagesAsync(installPath);
            installed.Should().Contain(p => p.PackageId == packageId);
            _output.WriteLine($"4. Installed packages count: {installed.Count}");

            // Act - Uninstall
            var uninstalled = await _manager.UninstallAsync(packageId, false, installPath);
            uninstalled.Should().BeTrue();
            _output.WriteLine($"5. Uninstalled: {uninstalled}");

            // Assert - Verify removed
            isInstalled = await _manager.IsInstalledAsync(packageId, installPath);
            isInstalled.Should().BeFalse();
            _output.WriteLine($"6. IsInstalled after uninstall: {isInstalled}");
        }

        [Fact]
        public async Task SourceManagement_AddCustomSource_ShouldWork()
        {
            // Arrange
            var sourceName = "TestNuGet";
            var sourceUrl = "https://api.nuget.org/v3/index.json";

            // Act
            _manager.AddSource(sourceName, sourceUrl);
            var sources = _manager.GetSources();

            // Assert
            sources.Should().Contain(s => s.Name == sourceName && s.Url == sourceUrl);
            
            // Test health check
            var isHealthy = await _manager.TestSourceAsync(sourceName);
            isHealthy.Should().BeTrue();
            
            _output.WriteLine($"Source {sourceName} is healthy: {isHealthy}");
        }

        [Fact]
        public async Task CacheManagement_ClearSpecificPackage_ShouldWork()
        {
            // Arrange
            var packageId = "Newtonsoft.Json";
            var version = "13.0.1";
            await _manager.DownloadAsync(packageId, version);
            
            var beforeCache = await _manager.GetCachedPackagesAsync();
            beforeCache.Should().NotBeEmpty();

            // Act
            await _manager.ClearCacheAsync(packageId, version);

            // Assert
            var afterCache = await _manager.GetCachedPackagesAsync();
            afterCache.Should().NotContain(p => 
                p.PackageId.Equals(packageId, StringComparison.OrdinalIgnoreCase) && 
                p.Version == version);
        }
    }
}
