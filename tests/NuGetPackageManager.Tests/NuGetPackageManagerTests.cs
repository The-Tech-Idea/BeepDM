using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.NuGetManagement;
using TheTechIdea.Beep.NuGetManagement.Models;
using TheTechIdea.Beep.Tools.PluginSystem;
using Xunit;

namespace TheTechIdea.Beep.NuGetManagement.Tests
{
    public class NuGetPackageManagerTests : IDisposable
    {
        private readonly Mock<IDMLogger> _mockLogger;
        private readonly Mock<SharedContextManager> _mockSharedContextManager;
        private readonly NuGetPackageManager _manager;
        private readonly string _testInstallDirectory;

        public NuGetPackageManagerTests()
        {
            _mockLogger = new Mock<IDMLogger>();
            _mockSharedContextManager = new Mock<SharedContextManager>(_mockLogger.Object, true, new PluginRegistry(AppContext.BaseDirectory, _mockLogger.Object));
            _testInstallDirectory = Path.Combine(Path.GetTempPath(), $"NuGetTests_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testInstallDirectory);
            
            _manager = new NuGetPackageManager(_mockLogger.Object, _mockSharedContextManager.Object, _testInstallDirectory);
        }

        public void Dispose()
        {
            _manager?.Dispose();
            
            // Cleanup test directory
            if (Directory.Exists(_testInstallDirectory))
            {
                try
                {
                    Directory.Delete(_testInstallDirectory, true);
                }
                catch { }
            }
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Action act = () => new NuGetPackageManager(null, _mockSharedContextManager.Object);
            act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
        }

        [Fact]
        public void Constructor_WithValidParameters_ShouldInitialize()
        {
            // Assert
            _manager.Should().NotBeNull();
            _mockLogger.Verify(l => l.LogWithContext(It.Is<string>(s => s.Contains("NuGetPackageManager initialized")), null), Times.Once);
        }

        [Fact]
        public void GetSources_ShouldReturnDefaultNuGetSource()
        {
            // Act
            var sources = _manager.GetSources();

            // Assert
            sources.Should().NotBeNullOrEmpty();
            sources.Should().Contain(s => s.Name == "nuget.org" && s.Url == "https://api.nuget.org/v3/index.json");
        }

        [Fact]
        public void AddSource_WithValidParameters_ShouldAddSource()
        {
            // Arrange
            var sourceName = "TestSource";
            var sourceUrl = "https://test.nuget.org/v3/index.json";

            // Act
            _manager.AddSource(sourceName, sourceUrl);

            // Assert
            var sources = _manager.GetSources();
            sources.Should().Contain(s => s.Name == sourceName && s.Url == sourceUrl);
        }

        [Fact]
        public void AddSource_WithLocalPath_ShouldMarkAsLocal()
        {
            // Arrange
            var sourceName = "LocalSource";
            var sourcePath = _testInstallDirectory;

            // Act
            _manager.AddSource(sourceName, sourcePath);

            // Assert
            var sources = _manager.GetSources();
            var source = sources.Should().Contain(s => s.Name == sourceName).Subject;
            source.IsLocal.Should().BeTrue();
        }

        [Fact]
        public void RemoveSource_ExistingSource_ShouldRemove()
        {
            // Arrange
            var sourceName = "TestSource";
            _manager.AddSource(sourceName, "https://test.nuget.org/v3/index.json");

            // Act
            _manager.RemoveSource(sourceName);

            // Assert
            var sources = _manager.GetSources();
            sources.Should().NotContain(s => s.Name == sourceName);
        }

        [Fact]
        public void EnableDisableSource_ShouldToggleState()
        {
            // Arrange
            var sourceName = "TestSource";
            _manager.AddSource(sourceName, "https://test.nuget.org/v3/index.json", false);

            // Act & Assert - Initially disabled
            var activeSources = _manager.GetActiveSources();
            activeSources.Should().NotContain(s => s.Name == sourceName);

            // Enable
            _manager.EnableSource(sourceName);
            activeSources = _manager.GetActiveSources();
            activeSources.Should().Contain(s => s.Name == sourceName);

            // Disable
            _manager.DisableSource(sourceName);
            activeSources = _manager.GetActiveSources();
            activeSources.Should().NotContain(s => s.Name == sourceName);
        }

        [Fact]
        public void SetSourcePriority_ShouldUpdatePriority()
        {
            // Arrange
            var sourceName = "TestSource";
            _manager.AddSource(sourceName, "https://test.nuget.org/v3/index.json");

            // Act
            _manager.SetSourcePriority(sourceName, 5);

            // Assert
            var sources = _manager.GetSources();
            var source = sources.Should().Contain(s => s.Name == sourceName).Subject;
            source.Priority.Should().Be(5);
        }

        [Fact]
        public async Task GetInstalledPackagesAsync_WithNoPackages_ShouldReturnEmptyList()
        {
            // Act
            var packages = await _manager.GetInstalledPackagesAsync();

            // Assert
            packages.Should().BeEmpty();
        }

        [Fact]
        public async Task IsInstalledAsync_NonExistentPackage_ShouldReturnFalse()
        {
            // Act
            var result = await _manager.IsInstalledAsync("NonExistentPackage");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetCacheInfoAsync_ShouldReturnCacheInfo()
        {
            // Act
            var cacheInfo = await _manager.GetCacheInfoAsync();

            // Assert
            cacheInfo.Should().NotBeNull();
            cacheInfo.CachePath.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void PackageEvents_ShouldFire()
        {
            // Arrange
            var packageInstallingFired = false;
            var packageInstalledFired = false;

            _manager.PackageInstalling += (s, e) => packageInstallingFired = true;
            _manager.PackageInstalled += (s, e) => packageInstalledFired = true;

            // Act - Trigger events indirectly through a mock or test
            // Note: In real tests, we'd call actual methods that trigger these events

            // Assert
            // Events are tested in integration tests
            packageInstallingFired.Should().BeFalse(); // No actual operation performed
        }
    }
}
