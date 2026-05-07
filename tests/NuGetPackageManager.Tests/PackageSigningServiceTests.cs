using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.NuGetManagement.Services;
using Xunit;

namespace TheTechIdea.Beep.NuGetManagement.Tests
{
    public class PackageSigningServiceTests : IDisposable
    {
        private readonly DMLogger _logger;
        private readonly PackageSigningService _service;
        private readonly string _testDirectory;

        public PackageSigningServiceTests()
        {
            _logger = new DMLogger();
            _service = new PackageSigningService(_logger);
            _testDirectory = Path.Combine(Path.GetTempPath(), $"SigningTests_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDirectory);
        }

        public void Dispose()
        {
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
        public async Task IsSignedAsync_NonExistentFile_ShouldReturnFalse()
        {
            // Arrange
            var path = Path.Combine(_testDirectory, "nonexistent.nupkg");

            // Act
            var result = await _service.IsSignedAsync(path);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task IsSignedAsync_EmptyFile_ShouldReturnFalse()
        {
            // Arrange
            var path = Path.Combine(_testDirectory, "empty.nupkg");
            File.WriteAllText(path, "not a real package");

            // Act
            var result = await _service.IsSignedAsync(path);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task VerifySignatureAsync_NonExistentFile_ShouldReturnInvalid()
        {
            // Arrange
            var path = Path.Combine(_testDirectory, "nonexistent.nupkg");

            // Act
            var result = await _service.VerifySignatureAsync(path);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Contain("not found");
        }

        [Fact]
        public async Task VerifySignatureAsync_EmptyFile_ShouldReturnInvalid()
        {
            // Arrange
            var path = Path.Combine(_testDirectory, "empty.nupkg");
            File.WriteAllText(path, "not a real package");

            // Act
            var result = await _service.VerifySignatureAsync(path);

            // Assert
            result.IsSigned.Should().BeFalse();
            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task GetSignatureInfoAsync_NonExistentFile_ShouldReturnNotSigned()
        {
            // Arrange
            var path = Path.Combine(_testDirectory, "nonexistent.nupkg");

            // Act
            var result = await _service.GetSignatureInfoAsync(path);

            // Assert
            result.IsSigned.Should().BeFalse();
            result.PackagePath.Should().Be(path);
        }

        [Fact]
        public async Task GetSignatureInfoAsync_EmptyFile_ShouldReturnNotSigned()
        {
            // Arrange
            var path = Path.Combine(_testDirectory, "empty.nupkg");
            File.WriteAllText(path, "not a real package");

            // Act
            var result = await _service.GetSignatureInfoAsync(path);

            // Assert
            result.IsSigned.Should().BeFalse();
        }
    }
}
