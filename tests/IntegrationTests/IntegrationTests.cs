using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using TheTechIdea.Beep.Tools;
using TheTechIdea.Beep.Tools.PluginSystem;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Utilities;
using Moq;
using FluentAssertions;

namespace Assembly_helpers.IntegrationTests
{
    public class IntegrationTests : IDisposable
    {
        private readonly string _workspaceTemp;
        private readonly Mock<IDMLogger> _logger;

        public IntegrationTests()
        {
            _workspaceTemp = TestUtils.CreateTempDirectory("inttest_workspace_");
            _logger = new Mock<IDMLogger>();
        }

        public void Dispose()
        {
            try { Directory.Delete(_workspaceTemp, true); } catch { }
            GC.Collect(); GC.WaitForPendingFinalizers();
        }

        [Fact]
        public async Task MultiFeedInstallAndLoadAsync()
        {
            // Arrange: create two feeds with different versions of the same package
            var feed1 = Path.Combine(_workspaceTemp, "feed1"); Directory.CreateDirectory(feed1);
            var feed2 = Path.Combine(_workspaceTemp, "feed2"); Directory.CreateDirectory(feed2);

            string packageId = "Sample.MultiFeedPkg";
            var code = "public class Foo { public string Hello => \"from v1\"; }";
            var nupkg1 = await TestUtils.PackClassLibraryAsync(packageId, "1.0.0", code, feed1);

            var codeV2 = "public class Foo { public string Hello => \"from v2\"; }";
            var nupkg2 = await TestUtils.PackClassLibraryAsync(packageId, "2.0.0", codeV2, feed2);

            var downloadDir = Path.Combine(_workspaceTemp, "downloads"); Directory.CreateDirectory(downloadDir);

            var dmLogger = _logger.Object;
            var downloader = new NuggetPackageDownloader(downloadDir, dmLogger);

            // Act: ask downloader to fetch specific version from feed1
            var downloadedV1 = await downloader.DownloadPackageAsync(packageId, "1.0.0", new[] { feed1, feed2 });
            downloadedV1.Should().NotBeNullOrEmpty();
            var extractedV1 = downloader.ExtractNuGetPackage(downloadedV1);
            extractedV1.Should().NotBeNullOrEmpty();

            // Now install to app plugin path and ensure it's placed in Plugins/<id>/<version>
            var appPluginsDir = Path.Combine(_workspaceTemp, "PluginsApp"); Directory.CreateDirectory(appPluginsDir);
            downloader.InstallPackageToAppDirectory(Path.GetDirectoryName(downloadedV1)!, appPluginsDir, packageId, "1.0.0", overwrite: false)
                .Should().NotBeNullOrEmpty();

            // Install version 2 to ensure both exist
            var downloadedV2 = await downloader.DownloadPackageAsync(packageId, "2.0.0", new[] { feed1, feed2 });
            var extractedV2 = downloader.ExtractNuGetPackage(downloadedV2);
            downloadedV2.Should().NotBeNullOrEmpty();
            extractedV2.Should().NotBeNullOrEmpty();
            downloader.InstallPackageToAppDirectory(extractedV2, appPluginsDir, packageId, "2.0.0", overwrite: false)
                .Should().NotBeNullOrEmpty();

            // Assert: both version folders exist
            Directory.Exists(Path.Combine(appPluginsDir, packageId, "1.0.0")).Should().BeTrue();
            Directory.Exists(Path.Combine(appPluginsDir, packageId, "2.0.0")).Should().BeTrue();
        }

        [Fact]
        public async Task CrossPluginTypeSharingWithSharedContextAsync()
        {
            // Arrange: create a common package and a dependent plugin
            var feed = Path.Combine(_workspaceTemp, "feed_sharing"); Directory.CreateDirectory(feed);

            var commonId = "Common.SharedLib"; var commonVersion = "1.0.0";
            var commonCode = "namespace Common.SharedLib { public class SharedClass { public string Value() => \"HelloShared\"; }}";

            var (commonNupkg, _) = await TestUtils.PackDependentClassLibrariesAsync(commonId, commonVersion, "", "", commonCode, "", feed);
            // Note: above util packs any package; we will now pack a plugin referencing common

            var pluginId = "Plugin.UsesCommon"; var pluginVersion = "1.0.0";
            var pluginCode = "using Common.SharedLib; namespace Plugin.UsesCommon { public class PluginClass { public string Run() => new SharedClass().Value(); } }";
            var (_, pluginProjectDir) = await TestUtils.PackDependentClassLibrariesAsync(commonId, commonVersion, pluginId, pluginVersion, commonCode, pluginCode, feed);

            // Now use the NuggetPluginLoader to load them with useSingleSharedContext = true
            var downloader = new NuggetPackageDownloader(Path.Combine(_workspaceTemp, "downloads2"), _logger.Object);
            // Use SharedContextManager to load both packages and exercise cross-plugin sharing
            var registry = new PluginRegistry(_workspaceTemp, _logger.Object);
            var shared = new SharedContextManager(_logger.Object, useSingleSharedContext: true, pluginRegistry: registry);

            // Find pak paths
            var commonNupkgPath = Path.Combine(feed, $"{commonId}.{commonVersion}.nupkg");
            var pluginNupkgPath = Directory.GetFiles(feed, "*.nupkg", SearchOption.TopDirectoryOnly)
                .FirstOrDefault(p => p.Contains(pluginId.ToLower()));

            // Extract both packages
            string commonExtract = Path.Combine(_workspaceTemp, "common_extracted");
            string pluginExtract = Path.Combine(_workspaceTemp, "plugin_extracted");
            System.IO.Compression.ZipFile.ExtractToDirectory(commonNupkgPath, commonExtract);
            System.IO.Compression.ZipFile.ExtractToDirectory(pluginNupkgPath, pluginExtract);

            // Use shared context manager to load both
            var commonLib = Directory.GetDirectories(commonExtract, "*net*").FirstOrDefault();
            var pluginLib = Directory.GetDirectories(pluginExtract, "*net*").FirstOrDefault();
            var commonNugget = await shared.LoadNuggetAsync(commonLib);
            var pluginNugget = await shared.LoadNuggetAsync(pluginLib);

            // Now, instantiate plugin class and ensure it uses the SharedClass
            var pluginAssembly = pluginNugget.LoadedAssemblies.FirstOrDefault();
            var pluginType = pluginAssembly.GetType("Plugin.UsesCommon.PluginClass");
            var instance = Activator.CreateInstance(pluginType);
            var result = pluginType.GetMethod("Run").Invoke(instance, null) as string;
            result.Should().Be("HelloShared");
        }

        [Fact]
        public async Task UnloadReclaimsMemoryInPerNuggetContextsAsync()
        {
            // Arrange: create plugin
            var feed = Path.Combine(_workspaceTemp, "feed_unload"); Directory.CreateDirectory(feed);
            var pkgId = "Unload.TestPkg";
            var code = "public class MemoryHog { public string Data => \"Ghi\"; }";
            var nupkg = await TestUtils.PackClassLibraryAsync(pkgId, "1.0.0", code, feed);
            var extract = Path.Combine(_workspaceTemp, "unload_extracted");
            System.IO.Compression.ZipFile.ExtractToDirectory(nupkg, extract);
            var lib = Directory.GetDirectories(extract, "*net*").First();

            var mockUtil = new Mock<TheTechIdea.Beep.Utilities.IUtil>();
            var errors = new TheTechIdea.Beep.ConfigUtil.ErrorsInfo();
            var mgr = new NuggetManager(_logger.Object, errors, mockUtil.Object);

            // Act: Load in isolated context
            bool loaded = mgr.LoadNugget(lib, useIsolatedContext: true);
            loaded.Should().BeTrue();
            var assemblies = mgr.GetNuggetAssemblies(Path.GetFileName(lib));
            assemblies.Should().NotBeEmpty();
            var asm = assemblies.First();
            var type = asm.GetType("MemoryHog");
            var obj = Activator.CreateInstance(type);
            var weak = new WeakReference(obj);

            // Drop strong refs
            obj = null;
            type = null;
            asm = null;
            assemblies = null;

            // Unload
            var unloadOk = mgr.UnloadNugget(Path.GetFileName(lib));
            unloadOk.Should().BeTrue();

            // Force GC
            for (int i=0;i<3;i++) { GC.Collect(); GC.WaitForPendingFinalizers(); }

            // Assert: object collected
            weak.IsAlive.Should().BeFalse();
        }

        [Fact]
        public async Task UninstallCleansFilesAndRegistryAsync()
        {
            // Arrange: create a plugin and register it
            var feed = Path.Combine(_workspaceTemp, "feed_uninstall"); Directory.CreateDirectory(feed);
            var pkgId = "Uninstall.TestPkg";
            var pkgVersion = "1.0.0";
            var code = "public class Simple { public string Hello => \"ok\"; }";
            var nupkg = await TestUtils.PackClassLibraryAsync(pkgId, pkgVersion, code, feed);
            var extracted = Path.Combine(_workspaceTemp, "uninstall_extracted"); System.IO.Compression.ZipFile.ExtractToDirectory(nupkg, extracted);
            var lib = Directory.GetDirectories(extracted, "*net*").First();

            // use PluginInstaller & PluginRegistry
            var reg = new PluginRegistry(_workspaceTemp, _logger.Object);
            // Register plugin
            var installPath = Path.Combine(_workspaceTemp, "Plugins", pkgId, pkgVersion);
            Directory.CreateDirectory(installPath);
            // write dummy file
            File.WriteAllText(Path.Combine(installPath, "dummy.txt"), "hello");
            var info = new InstalledPluginInfo { Id = pkgId, Name = pkgId, Version = pkgVersion, InstallPath = installPath, State = "Unloaded", Source = feed };
            reg.Register(info);

            var installer = new PluginInstaller(reg, _logger.Object);
            // Act: uninstall
            var ok = installer.Uninstall(pkgId);
            ok.Should().BeTrue();
            // Assert: registry no longer has plugin and files removed
            reg.GetPlugin(pkgId).Should().BeNull();
            Directory.Exists(installPath).Should().BeFalse();
        }

        [Fact]
        public async Task PluginVersionManagementConflictResolutionAsync()
        {
            // Arrange
            var feed = Path.Combine(_workspaceTemp, "feed_versions"); Directory.CreateDirectory(feed);
            var pkgId = "Conflict.TestPkg";
            var v1code = "public class A { public static string Id => \"v1\"; }";
            var v2code = "public class A { public static string Id => \"v2\"; }";
            var nupkgV1 = await TestUtils.PackClassLibraryAsync(pkgId, "1.0.0", v1code, feed);
            var nupkgV2 = await TestUtils.PackClassLibraryAsync(pkgId, "2.0.0", v2code, feed);
            var downloader = new NuggetPackageDownloader(Path.Combine(_workspaceTemp, "downloads_versions"), _logger.Object);
            var extractedV1 = downloader.ExtractNuGetPackage(nupkgV1);
            var appPluginsDir = Path.Combine(_workspaceTemp, "PluginsAppVersions"); Directory.CreateDirectory(appPluginsDir);

            // Act: install v1
            var install1 = downloader.InstallPackageToAppDirectory(extractedV1, appPluginsDir, pkgId, "1.0.0", overwrite: false);
            install1.Should().NotBeNullOrEmpty();

            // Mark a file in the installed plugin to detect overwrite
            var marker = Path.Combine(install1, "marker.txt");
            File.WriteAllText(marker, "marker-v1");

            // Re-install same version with overwrite false - marker should remain
            var install1Again = downloader.InstallPackageToAppDirectory(extractedV1, appPluginsDir, pkgId, "1.0.0", overwrite: false);
            File.ReadAllText(marker).Should().Be("marker-v1");

            // Install v2 - both folders should exist and marker still in v1
            var extractedV2 = downloader.ExtractNuGetPackage(nupkgV2);
            var install2 = downloader.InstallPackageToAppDirectory(extractedV2, appPluginsDir, pkgId, "2.0.0", overwrite: false);
            Directory.Exists(Path.Combine(appPluginsDir, pkgId, "1.0.0")).Should().BeTrue();
            Directory.Exists(Path.Combine(appPluginsDir, pkgId, "2.0.0")).Should().BeTrue();
            File.ReadAllText(marker).Should().Be("marker-v1");
        }
    }
}
