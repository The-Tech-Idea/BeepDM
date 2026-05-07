using System;
using System.IO;
using System.Linq;
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
    public class LockFileTests : IDisposable
    {
        private readonly DMLogger _logger;
        private readonly Mock<SharedContextManager> _mockSharedContextManager;
        private readonly NuGetPackageManager _manager;
        private readonly string _testDirectory;

        public LockFileTests()
        {
            _logger = new DMLogger();
            _testDirectory = Path.Combine(Path.GetTempPath(), $"LockFileTests_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDirectory);
            
            var registry = new PluginRegistry(_testDirectory, _logger);
            _mockSharedContextManager = new Mock<SharedContextManager>(_logger, true, registry);
            _manager = new NuGetPackageManager(_logger, _mockSharedContextManager.Object, _testDirectory);
        }

        public void Dispose()
        {
            _manager?.Dispose();
            
            if (Directory.Exists(_testDirectory))
            {
                try
                {
                    Directory.Delete(_testDirectory, true);
                }
                catch { }
            }
        }

        [Fact]
        public void HasLockFile_NoLockFile_ShouldReturnFalse()
        {
            // Act
            var result = _manager.HasLockFile();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void GetLockFilePath_ShouldReturnCorrectPath()
        {
            // Act
            var path = _manager.GetLockFilePath();

            // Assert
            path.Should().Be(Path.Combine(_testDirectory, "packages.lock.json"));
        }

        [Fact]
        public async Task GenerateLockFileAsync_NoPackages_ShouldGenerateEmptyLockFile()
        {
            // Act
            var result = await _manager.GenerateLockFileAsync();

            // Assert
            result.Should().BeTrue();
            _manager.HasLockFile().Should().BeTrue();
            
            var lockFilePath = _manager.GetLockFilePath();
            File.Exists(lockFilePath).Should().BeTrue();
            
            var content = File.ReadAllText(lockFilePath);
            content.Should().Contain("GeneratedAt");
            content.Should().Contain("Packages");
        }

        [Fact]
        public async Task RestoreFromLockFileAsync_NoLockFile_ShouldReturnError()
        {
            // Act
            var result = await _manager.RestoreFromLockFileAsync();

            // Assert
            result.TotalRequested.Should().Be(0);
            result.Error.Should().Contain("not found");
        }

        [Fact]
        public async Task RestoreFromLockFileAsync_InvalidLockFile_ShouldReturnError()
        {
            // Arrange
            var lockFilePath = Path.Combine(_testDirectory, "packages.lock.json");
            File.WriteAllText(lockFilePath, "invalid json");

            // Act
            var result = await _manager.RestoreFromLockFileAsync();

            // Assert
            result.Error.Should().Contain("lock file");
        }

        [Fact]
        public async Task LockFileRoundTrip_GenerateAndRead_ShouldWork()
        {
            // Arrange - Create a fake lock file manually
            var lockFilePath = Path.Combine(_testDirectory, "packages.lock.json");
            var lockData = new LockFileData
            {
                GeneratedAt = DateTime.UtcNow,
                Packages = new System.Collections.Generic.List<LockFilePackage>
                {
                    new LockFilePackage
                    {
                        PackageId = "TestPackage",
                        Version = "1.0.0",
                        Source = "https://api.nuget.org/v3/index.json",
                        Dependencies = new System.Collections.Generic.List<string>()
                    }
                }
            };
            
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(lockData, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(lockFilePath, json);

            // Act & Assert - Verify the lock file can be read
            _manager.HasLockFile().Should().BeTrue();
            
            // Note: We can't actually restore because it would try to download
            // But we can verify the lock file exists and is valid JSON
            var content = File.ReadAllText(lockFilePath);
            var readData = Newtonsoft.Json.JsonConvert.DeserializeObject<LockFileData>(content);
            readData.Should().NotBeNull();
            readData.Packages.Should().HaveCount(1);
            readData.Packages[0].PackageId.Should().Be("TestPackage");
            readData.Packages[0].Version.Should().Be("1.0.0");
        }

        [Fact]
        public void LockFileData_Model_ShouldHaveCorrectDefaults()
        {
            // Arrange
            var data = new LockFileData();

            // Assert
            data.GeneratedAt.Should().Be(default(DateTime));
            data.Packages.Should().NotBeNull();
            data.Packages.Should().BeEmpty();
        }

        [Fact]
        public void LockFilePackage_Model_ShouldHaveCorrectDefaults()
        {
            // Arrange
            var package = new LockFilePackage();

            // Assert
            package.PackageId.Should().BeNull();
            package.Version.Should().BeNull();
            package.Source.Should().BeNull();
            package.Dependencies.Should().NotBeNull();
            package.Dependencies.Should().BeEmpty();
        }
    }
}
