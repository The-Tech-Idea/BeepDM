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
    public class SourceManagerTests : IDisposable
    {
        private readonly Mock<IDMLogger> _mockLogger;
        private readonly SourceManager _sourceManager;
        private readonly string _testConfigDir;

        public SourceManagerTests()
        {
            _mockLogger = new Mock<IDMLogger>();
            _testConfigDir = Path.Combine(Path.GetTempPath(), $"SourceManagerTests_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testConfigDir);
            _sourceManager = new SourceManager(_mockLogger.Object, _testConfigDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testConfigDir))
            {
                try
                {
                    Directory.Delete(_testConfigDir, true);
                }
                catch { }
            }
        }

        [Fact]
        public void Constructor_ShouldCreateDefaultNuGetSource()
        {
            // Act
            var sources = _sourceManager.GetSources();

            // Assert
            sources.Should().Contain(s => s.Name == "nuget.org" && s.Url == "https://api.nuget.org/v3/index.json");
        }

        [Fact]
        public void AddSource_WithNewSource_ShouldAdd()
        {
            // Arrange
            var name = "TestSource";
            var url = "https://test.com";

            // Act
            _sourceManager.AddSource(name, url);

            // Assert
            var sources = _sourceManager.GetSources();
            sources.Should().Contain(s => s.Name == name && s.Url == url);
        }

        [Fact]
        public void AddSource_WithExistingName_ShouldUpdate()
        {
            // Arrange
            var name = "TestSource";
            _sourceManager.AddSource(name, "https://old.com");

            // Act
            _sourceManager.AddSource(name, "https://new.com");

            // Assert
            var sources = _sourceManager.GetSources();
            sources.Should().ContainSingle(s => s.Name == name)
                .Which.Url.Should().Be("https://new.com");
        }

        [Fact]
        public void AddSource_WithLocalPath_ShouldSetIsLocal()
        {
            // Arrange
            var path = _testConfigDir;

            // Act
            _sourceManager.AddSource("Local", path);

            // Assert
            var sources = _sourceManager.GetSources();
            sources.Should().Contain(s => s.Name == "Local" && s.IsLocal == true);
        }

        [Fact]
        public void AddSource_WithCredentials_ShouldStoreCredentials()
        {
            // Arrange
            var name = "SecureSource";
            var url = "https://secure.com";
            var username = "user";
            var password = "pass";
            var apiKey = "key123";

            // Act
            _sourceManager.AddSource(name, url, true, username, password, apiKey);

            // Assert
            var sources = _sourceManager.GetSources();
            var source = sources.Should().Contain(s => s.Name == name).Subject;
            source.Username.Should().Be(username);
            source.Password.Should().Be(password);
            source.ApiKey.Should().Be(apiKey);
        }

        [Fact]
        public void RemoveSource_Existing_ShouldRemove()
        {
            // Arrange
            var name = "ToRemove";
            _sourceManager.AddSource(name, "https://remove.com");

            // Act
            _sourceManager.RemoveSource(name);

            // Assert
            var sources = _sourceManager.GetSources();
            sources.Should().NotContain(s => s.Name == name);
        }

        [Fact]
        public void RemoveSource_NonExisting_ShouldNotThrow()
        {
            // Act
            Action act = () => _sourceManager.RemoveSource("NonExisting");

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void EnableDisableSource_ShouldToggle()
        {
            // Arrange
            var name = "ToggleTest";
            _sourceManager.AddSource(name, "https://toggle.com", false);

            // Act & Assert
            _sourceManager.GetActiveSources().Should().NotContain(s => s.Name == name);
            
            _sourceManager.EnableSource(name);
            _sourceManager.GetActiveSources().Should().Contain(s => s.Name == name);
            
            _sourceManager.DisableSource(name);
            _sourceManager.GetActiveSources().Should().NotContain(s => s.Name == name);
        }

        [Fact]
        public void GetActiveSources_ShouldOnlyReturnEnabled()
        {
            // Arrange
            _sourceManager.AddSource("Enabled1", "https://enabled1.com", true);
            _sourceManager.AddSource("Disabled", "https://disabled.com", false);
            _sourceManager.AddSource("Enabled2", "https://enabled2.com", true);

            // Act
            var active = _sourceManager.GetActiveSources();

            // Assert
            active.Should().HaveCount(3); // Default nuget.org + 2 enabled
            active.Should().Contain(s => s.Name == "nuget.org");
            active.Should().Contain(s => s.Name == "Enabled1");
            active.Should().Contain(s => s.Name == "Enabled2");
            active.Should().NotContain(s => s.Name == "Disabled");
        }

        [Fact]
        public void GetActiveSourceUrls_ShouldReturnUrls()
        {
            // Arrange
            _sourceManager.AddSource("Test", "https://test.com", true);

            // Act
            var urls = _sourceManager.GetActiveSourceUrls();

            // Assert
            urls.Should().Contain("https://api.nuget.org/v3/index.json");
            urls.Should().Contain("https://test.com");
        }

        [Fact]
        public void SetSourcePriority_ShouldUpdate()
        {
            // Arrange
            var name = "PriorityTest";
            _sourceManager.AddSource(name, "https://priority.com");

            // Act
            _sourceManager.SetSourcePriority(name, 1);

            // Assert
            var sources = _sourceManager.GetSources();
            sources.Should().Contain(s => s.Name == name && s.Priority == 1);
        }

        [Fact]
        public async Task TestSourceAsync_LocalExistingDirectory_ShouldReturnTrue()
        {
            // Arrange
            var name = "LocalTest";
            _sourceManager.AddSource(name, _testConfigDir, true);

            // Act
            var result = await _sourceManager.TestSourceAsync(name);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task TestSourceAsync_LocalNonExistingDirectory_ShouldReturnFalse()
        {
            // Arrange
            var name = "BadLocal";
            _sourceManager.AddSource(name, "C:\\NonExistingPath12345", true);

            // Act
            var result = await _sourceManager.TestSourceAsync(name);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Sources_ShouldPersistToDisk()
        {
            // Arrange
            var name = "Persistent";
            _sourceManager.AddSource(name, "https://persistent.com");

            // Act - Create new instance with same config directory
            var newManager = new SourceManager(_mockLogger.Object, _testConfigDir);

            // Assert
            var sources = newManager.GetSources();
            sources.Should().Contain(s => s.Name == name);
        }
    }
}
