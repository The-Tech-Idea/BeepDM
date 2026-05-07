using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.NuGetManagement.Models;
using TheTechIdea.Beep.NuGetManagement.Services;
using Xunit;

namespace TheTechIdea.Beep.NuGetManagement.Tests
{
    public class CacheManagerTests : IDisposable
    {
        private readonly Mock<IDMLogger> _mockLogger;
        private readonly CacheManager _cacheManager;
        private readonly string _testCacheDir;

        public CacheManagerTests()
        {
            _mockLogger = new Mock<IDMLogger>();
            _testCacheDir = Path.Combine(Path.GetTempPath(), $"CacheTests_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testCacheDir);
            _cacheManager = new CacheManager(_mockLogger.Object, _testCacheDir, 100, true);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testCacheDir))
            {
                try
                {
                    Directory.Delete(_testCacheDir, true);
                }
                catch { }
            }
        }

        [Fact]
        public void Constructor_ShouldSetCachePath()
        {
            // Assert
            _cacheManager.CachePath.Should().Be(_testCacheDir);
        }

        [Fact]
        public async Task GetCachedPackagesAsync_EmptyCache_ShouldReturnEmpty()
        {
            // Act
            var packages = await _cacheManager.GetCachedPackagesAsync();

            // Assert
            packages.Should().BeEmpty();
        }

        [Fact]
        public async Task GetCacheInfoAsync_EmptyCache_ShouldReturnZeroSize()
        {
            // Act
            var info = await _cacheManager.GetCacheInfoAsync();

            // Assert
            info.Should().NotBeNull();
            info.CachePath.Should().Be(_testCacheDir);
            info.TotalSizeBytes.Should().Be(0);
            info.PackageCount.Should().Be(0);
        }

        [Fact]
        public void IsPackageInCache_NonExistent_ShouldReturnFalse()
        {
            // Act
            var result = _cacheManager.IsPackageInCache("NonExistent", "1.0.0");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ClearCacheAsync_All_ShouldRemoveEverything()
        {
            // Arrange
            CreateFakePackage("TestPackage", "1.0.0");
            CreateFakePackage("TestPackage", "2.0.0");

            // Act
            await _cacheManager.ClearCacheAsync();

            // Assert
            var packages = await _cacheManager.GetCachedPackagesAsync();
            packages.Should().BeEmpty();
        }

        [Fact]
        public async Task ClearCacheAsync_SpecificPackage_ShouldRemoveOnlyThatPackage()
        {
            // Arrange
            CreateFakePackage("Package1", "1.0.0");
            CreateFakePackage("Package2", "1.0.0");

            // Act
            await _cacheManager.ClearCacheAsync("Package1");

            // Assert
            var packages = await _cacheManager.GetCachedPackagesAsync();
            packages.Should().Contain(p => p.PackageId == "package2");
            packages.Should().NotContain(p => p.PackageId == "package1");
        }

        [Fact]
        public async Task ClearCacheAsync_SpecificVersion_ShouldRemoveOnlyThatVersion()
        {
            // Arrange
            CreateFakePackage("Package1", "1.0.0");
            CreateFakePackage("Package1", "2.0.0");

            // Act
            await _cacheManager.ClearCacheAsync("Package1", "1.0.0");

            // Assert
            var packages = await _cacheManager.GetCachedPackagesAsync();
            packages.Should().Contain(p => p.Version == "2.0.0");
            packages.Should().NotContain(p => p.Version == "1.0.0");
        }

        [Fact]
        public async Task GetCachedPackagesAsync_WithPackages_ShouldReturnThem()
        {
            // Arrange
            CreateFakePackage("TestPackage", "1.0.0");

            // Act
            var packages = await _cacheManager.GetCachedPackagesAsync();

            // Assert
            packages.Should().HaveCount(1);
            packages[0].PackageId.Should().Be("testpackage");
            packages[0].Version.Should().Be("1.0.0");
        }

        [Fact]
        public async Task CleanupOldPackagesAsync_ShouldRemoveOldPackages()
        {
            // Arrange
            CreateFakePackage("OldPackage", "1.0.0", DateTime.UtcNow.AddDays(-60));
            CreateFakePackage("NewPackage", "1.0.0", DateTime.UtcNow);

            // Act
            await _cacheManager.CleanupOldPackagesAsync(30);

            // Assert
            var packages = await _cacheManager.GetCachedPackagesAsync();
            packages.Should().NotContain(p => p.PackageId == "oldpackage");
            packages.Should().Contain(p => p.PackageId == "newpackage");
        }

        private void CreateFakePackage(string packageId, string version, DateTime? creationTime = null)
        {
            var packageDir = Path.Combine(_testCacheDir, packageId.ToLowerInvariant(), version);
            Directory.CreateDirectory(packageDir);
            
            var nupkgPath = Path.Combine(packageDir, $"{packageId.ToLowerInvariant()}.{version}.nupkg");
            File.WriteAllText(nupkgPath, "fake package content");
            
            if (creationTime.HasValue)
            {
                File.SetCreationTimeUtc(nupkgPath, creationTime.Value);
                File.SetLastAccessTimeUtc(nupkgPath, creationTime.Value);
            }
        }
    }
}
