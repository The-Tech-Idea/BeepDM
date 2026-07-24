using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Updates;
using Xunit;

namespace TheTechIdea.Beep.Updates.Tests;

/// <summary>
/// Stage 11.C.2 — the composed <see cref="AppUpdateService"/>: check populates stale modules,
/// the app-update path plans a delta and drives the side-by-side applier + version ledger, and
/// the module path delegates to the module updater. All offline via a URL-mapped fake transport
/// and a fake link.
/// </summary>
public class AppUpdateServiceComposeTests : IDisposable
{
    private sealed class MapTransport : IFeedTransport
    {
        public readonly Dictionary<string, string> Texts = new();
        public readonly Dictionary<string, byte[]> Bytes = new();
        public Task<string> GetStringAsync(string url, CancellationToken ct = default)
            => Texts.TryGetValue(url, out var t) ? Task.FromResult(t) : throw new IOException($"no text for {url}");
        public Task<byte[]> GetBytesAsync(string url, CancellationToken ct = default)
            => Bytes.TryGetValue(url, out var b) ? Task.FromResult(b) : throw new IOException($"no bytes for {url}");
    }

    private sealed class FakeLink : IDirectoryLink
    {
        public readonly Dictionary<string, string> Targets = new(StringComparer.OrdinalIgnoreCase);
        public void Point(string linkPath, string targetPath) => Targets[linkPath] = targetPath;
    }

    private sealed class FakeModules : IModulePackageService
    {
        public List<InstalledModule> Installed = new();
        public List<(string, string)> Updated = new();
        public Task<IReadOnlyList<InstalledModule>> GetInstalledModulesAsync(string? dir = null, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<InstalledModule>>(Installed);
        public Task<ModuleUpdateOutcome> UpdateModuleAsync(string id, string version, string? dir = null, CancellationToken ct = default)
        {
            Updated.Add((id, version));
            return Task.FromResult(new ModuleUpdateOutcome { Success = true, NewVersion = version });
        }
    }

    private readonly string _root;

    public AppUpdateServiceComposeTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "beepcompose_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    public void Dispose() { try { Directory.Delete(_root, true); } catch { } }

    [Fact]
    public async Task ApplyAppUpdate_FullInstall_MaterializesFlips_AndRecordsVersion()
    {
        var bytes = Encoding.UTF8.GetBytes("app-v2-binary");
        var hash = Convert.ToHexString(SHA256.HashData(bytes));

        var transport = new MapTransport();
        transport.Texts["feed"] = """
        { "product":"MyApp", "channel":"stable",
          "latest": { "version":"2.0.0",
            "delta": { "manifestUrl":"manifest", "blobBaseUrl":"blobs/" } } }
        """;
        transport.Texts["manifest"] = $$"""
        { "solid": true, "entries": [ { "path":"app.exe", "blob":"{{hash}}", "size":{{bytes.Length}} } ] }
        """;
        transport.Bytes["blobs/" + hash] = bytes;

        var link = new FakeLink();
        string? recorded = null;
        var svc = new AppUpdateService(
            new UpdateSettings { FeedUrl = "feed", CurrentVersion = "1.0.0", InstallRoot = _root },
            new UpdateFeedClient(transport), modulePackages: null, link: link, recordVersion: v => recorded = v);

        var check = await svc.CheckAsync();
        Assert.True(check.AppUpdateAvailable);

        var apply = await svc.ApplyAppUpdateAsync(check);

        Assert.Equal(Errors.Ok, apply.Flag);
        Assert.Equal("app-v2-binary", File.ReadAllText(Path.Combine(_root, "app-2.0.0", "app.exe")));
        Assert.Equal(Path.Combine(_root, "app-2.0.0"), link.Targets[Path.Combine(_root, "current")]);
        Assert.Equal("2.0.0", recorded);
    }

    [Fact]
    public async Task Check_PopulatesStaleModules_FromFeedAndInventory()
    {
        var transport = new MapTransport();
        transport.Texts["feed"] = """
        { "product":"MyApp",
          "latest": { "version":"1.0.0" },
          "modules": [ { "id":"Drv.A", "version":"2.0.0", "required":false } ] }
        """;
        var modules = new FakeModules { Installed = { new InstalledModule { Id = "Drv.A", Version = "1.0.0" } } };

        var svc = new AppUpdateService(
            new UpdateSettings { FeedUrl = "feed", CurrentVersion = "1.0.0" },
            new UpdateFeedClient(transport), modulePackages: modules);

        var check = await svc.CheckAsync();

        Assert.True(check.Succeeded);
        Assert.False(check.AppUpdateAvailable);          // same app version
        Assert.True(check.AnyUpdateAvailable);            // but a module is stale
        Assert.Equal("Drv.A", Assert.Single(check.StaleModules).Id);

        var applied = await svc.ApplyModuleUpdatesAsync(check);
        Assert.Equal(Errors.Ok, applied.Flag);
        Assert.Equal(("Drv.A", "2.0.0"), Assert.Single(modules.Updated));
    }

    [Fact]
    public async Task ApplyAppUpdate_WithNoDelta_FailsClearly()
    {
        var transport = new MapTransport();
        transport.Texts["feed"] = """{ "product":"MyApp", "latest": { "version":"2.0.0" } }""";
        var svc = new AppUpdateService(
            new UpdateSettings { FeedUrl = "feed", CurrentVersion = "1.0.0", InstallRoot = _root },
            new UpdateFeedClient(transport));

        var apply = await svc.ApplyAppUpdateAsync(await svc.CheckAsync());

        Assert.Equal(Errors.Failed, apply.Flag);
        Assert.Contains("no delta", apply.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ApplyModuleUpdates_WithoutService_Fails()
    {
        var transport = new MapTransport();
        transport.Texts["feed"] = """{ "product":"MyApp", "latest": { "version":"1.0.0" } }""";
        var svc = new AppUpdateService(new UpdateSettings { FeedUrl = "feed", CurrentVersion = "1.0.0" }, new UpdateFeedClient(transport));

        var result = await svc.ApplyModuleUpdatesAsync(await svc.CheckAsync());

        Assert.Equal(Errors.Failed, result.Flag);
    }
}
