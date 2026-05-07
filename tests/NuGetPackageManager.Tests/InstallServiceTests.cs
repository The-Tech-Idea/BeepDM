using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.NuGetManagement.Services;
using Xunit;

namespace TheTechIdea.Beep.NuGetManagement.Tests
{
    public class InstallServiceTests : IDisposable
    {
        private readonly Mock<IDMLogger> _mockLogger;
        private readonly InstallService _installService;
        private readonly string _testSourceDir;
        private readonly string _testInstallDir;

        public InstallServiceTests()
        {
            _mockLogger = new Mock<IDMLogger>();
            _installService = new InstallService(_mockLogger.Object);
            _testSourceDir = Path.Combine(Path.GetTempPath(), $"InstallSource_{Guid.NewGuid()}");
            _testInstallDir = Path.Combine(Path.GetTempPath(), $"InstallTarget_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testSourceDir);
            Directory.CreateDirectory(_testInstallDir);
        }

        public void Dispose()
        {
            CleanupDirectory(_testSourceDir);
            CleanupDirectory(_testInstallDir);
        }

        private void CleanupDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                try
                {
                    Directory.Delete(path, true);
                }
                catch { }
            }
        }

        [Fact]
        public async Task InstallAsync_WithValidPath_ShouldInstall()
        {
            // Arrange
            var packageId = "TestPackage";
            var version = "1.0.0";
            CreateFakePackageContent(_testSourceDir, "Test.dll");

            // Act
            var result = await _installService.InstallAsync(_testSourceDir, _testInstallDir, packageId, version);

            // Assert
            result.Success.Should().BeTrue();
            result.InstallPath.Should().Contain(packageId);
            result.InstalledFiles.Should().Contain("Test.dll");
            
            var installedDll = Path.Combine(result.InstallPath, "Test.dll");
            File.Exists(installedDll).Should().BeTrue();
        }

        [Fact]
        public async Task InstallAsync_WithInvalidPath_ShouldFail()
        {
            // Arrange
            var invalidPath = "C:\\NonExistingPath12345";

            // Act
            var result = await _installService.InstallAsync(invalidPath, _testInstallDir, "Test", "1.0");

            // Assert
            result.Success.Should().BeFalse();
            result.Error.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task InstallAsync_WithOverwrite_ShouldReplaceExisting()
        {
            // Arrange
            var packageId = "TestPackage";
            var version = "1.0.0";
            var targetDir = Path.Combine(_testInstallDir, packageId, version);
            Directory.CreateDirectory(targetDir);
            File.WriteAllText(Path.Combine(targetDir, "Test.dll"), "old content");
            
            CreateFakePackageContent(_testSourceDir, "Test.dll", "new content");

            // Act
            var result = await _installService.InstallAsync(_testSourceDir, _testInstallDir, packageId, version, true);

            // Assert
            result.Success.Should().BeTrue();
            var content = File.ReadAllText(Path.Combine(targetDir, "Test.dll"));
            content.Should().Be("new content");
        }

        [Fact]
        public async Task InstallAsync_WithoutOverwrite_ShouldSkipExisting()
        {
            // Arrange
            var packageId = "TestPackage";
            var version = "1.0.0";
            var targetDir = Path.Combine(_testInstallDir, packageId, version);
            Directory.CreateDirectory(targetDir);
            File.WriteAllText(Path.Combine(targetDir, "Test.dll"), "old content");
            
            CreateFakePackageContent(_testSourceDir, "Test.dll", "new content");

            // Act
            var result = await _installService.InstallAsync(_testSourceDir, _testInstallDir, packageId, version, false);

            // Assert
            result.Success.Should().BeTrue();
            result.Warnings.Should().Contain(w => w.Contains("Skipped"));
            var content = File.ReadAllText(Path.Combine(targetDir, "Test.dll"));
            content.Should().Be("old content");
        }

        [Fact]
        public void IsPackageInstalled_InstalledPackage_ShouldReturnTrue()
        {
            // Arrange
            var packageId = "TestPackage";
            var packageDir = Path.Combine(_testInstallDir, packageId, "1.0.0");
            Directory.CreateDirectory(packageDir);
            File.WriteAllText(Path.Combine(packageDir, "Test.dll"), "content");

            // Act
            var result = _installService.IsPackageInstalled(packageId, _testInstallDir);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsPackageInstalled_NotInstalled_ShouldReturnFalse()
        {
            // Act
            var result = _installService.IsPackageInstalled("NonExistent", _testInstallDir);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void GetInstallPath_ShouldReturnCorrectPath()
        {
            // Act
            var path = _installService.GetInstallPath("Test", _testInstallDir, "1.0.0");

            // Assert
            path.Should().Be(Path.Combine(_testInstallDir, "Test", "1.0.0"));
        }

        [Fact]
        public async Task BulkInstallAsync_ShouldInstallMultiple()
        {
            // Arrange
            var packages = new System.Collections.Generic.List<TheTechIdea.Beep.NuGetManagement.Models.PackageRequest>
            {
                new TheTechIdea.Beep.NuGetManagement.Models.PackageRequest { PackageId = "Pkg1" },
                new TheTechIdea.Beep.NuGetManagement.Models.PackageRequest { PackageId = "Pkg2" }
            };
            
            CreateFakePackageContent(_testSourceDir, "Pkg1.dll");
            CreateFakePackageContent(_testSourceDir, "Pkg2.dll");

            // Act
            var result = await _installService.BulkInstallAsync(packages, _testInstallDir);

            // Assert
            result.TotalRequested.Should().Be(2);
        }

        private void CreateFakePackageContent(string directory, string fileName, string content = "fake")
        {
            File.WriteAllText(Path.Combine(directory, fileName), content);
        }
    }
}
