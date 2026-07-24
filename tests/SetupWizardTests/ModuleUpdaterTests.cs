using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Updates;
using Xunit;

namespace TheTechIdea.Beep.Updates.Tests;

/// <summary>
/// Stage 11.C.1 — the feed-pinned module channel. Staleness is decided against the pinned
/// version (a pin can hold a module back), only stale modules are touched, required-missing is
/// blocking, and a package that fails its feed hash is rejected. All via a fake package service,
/// which also proves the updater never touches application files.
/// </summary>
public class ModuleUpdaterTests
{
    private sealed class FakePackages : IModulePackageService
    {
        public List<InstalledModule> Installed = new();
        public List<(string id, string version)> UpdateCalls = new();
        public Func<string, string, ModuleUpdateOutcome>? OnUpdate;

        public Task<IReadOnlyList<InstalledModule>> GetInstalledModulesAsync(string? installDirectory = null, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<InstalledModule>>(Installed);

        public Task<ModuleUpdateOutcome> UpdateModuleAsync(string packageId, string version, string? installDirectory = null, CancellationToken ct = default)
        {
            UpdateCalls.Add((packageId, version));
            return Task.FromResult(OnUpdate?.Invoke(packageId, version)
                ?? new ModuleUpdateOutcome { Success = true, NewVersion = version });
        }
    }

    private static ModuleRef Mod(string id, string version, bool required = false, string? sha = null)
        => new() { Id = id, Version = version, Required = required, Sha256 = sha };

    private static InstalledModule Inst(string id, string version) => new() { Id = id, Version = version };

    // ── Staleness ──

    [Fact]
    public void PinnedVersionDiffers_IsStale_SameIsNot()
    {
        var feed = new[] { Mod("A", "2.0.0"), Mod("B", "1.0.0") };
        var installed = new[] { Inst("A", "1.0.0"), Inst("B", "1.0.0") };

        var stale = ModuleUpdater.ComputeStaleModules(feed, installed);

        Assert.Equal("A", Assert.Single(stale).Id);
    }

    [Fact]
    public void FeedPin_WinsOverLatest_DowngradeIsStale()
    {
        // Installed is newer than the pin — the feed deliberately holds the module at 2.4.1.
        var stale = ModuleUpdater.ComputeStaleModules(new[] { Mod("A", "2.4.1") }, new[] { Inst("A", "2.5.0") });

        Assert.Equal("2.4.1", Assert.Single(stale).Version);
    }

    [Fact]
    public void OptionalMissing_IsSkipped_RequiredMissing_IsIncluded()
    {
        var feed = new[] { Mod("Opt", "1.0.0", required: false), Mod("Req", "1.0.0", required: true) };

        var stale = ModuleUpdater.ComputeStaleModules(feed, Array.Empty<InstalledModule>());

        Assert.Equal("Req", Assert.Single(stale).Id);
    }

    [Fact]
    public void HasBlockingRequiredModule_ReflectsRequiredFlag()
    {
        Assert.True(ModuleUpdater.HasBlockingRequiredModule(new[] { Mod("R", "1.0.0", required: true) }));
        Assert.False(ModuleUpdater.HasBlockingRequiredModule(new[] { Mod("O", "1.0.0", required: false) }));
    }

    // ── Apply ──

    [Fact]
    public async Task Apply_UpdatesOnlyStaleModules_ToPinnedVersion()
    {
        var packages = new FakePackages();
        var updater = new ModuleUpdater(packages);
        var stale = new[] { Mod("A", "2.0.0") };

        var report = await updater.ApplyAsync(stale, installDirectory: "modules");

        Assert.True(report.AllSucceeded);
        var call = Assert.Single(packages.UpdateCalls);
        Assert.Equal(("A", "2.0.0"), call);              // exactly the stale module, at the pinned version
        Assert.Equal(("A", "2.0.0"), Assert.Single(report.Updated));
    }

    [Fact]
    public async Task Apply_RecordsFailure_WhenPackageServiceFails()
    {
        var packages = new FakePackages { OnUpdate = (_, _) => new ModuleUpdateOutcome { Success = false, Error = "feed unreachable" } };

        var report = await new ModuleUpdater(packages).ApplyAsync(new[] { Mod("A", "2.0.0") });

        Assert.False(report.AllSucceeded);
        Assert.Equal(("A", "feed unreachable"), Assert.Single(report.Failed));
    }

    [Fact]
    public async Task Apply_RejectsPackage_ThatFailsFeedHash()
    {
        var pkg = Path.Combine(Path.GetTempPath(), $"beepmod_{Guid.NewGuid():N}.nupkg");
        File.WriteAllText(pkg, "tampered-package-bytes");
        try
        {
            var packages = new FakePackages
            {
                OnUpdate = (_, v) => new ModuleUpdateOutcome { Success = true, NewVersion = v, PackagePath = pkg }
            };
            var report = await new ModuleUpdater(packages).ApplyAsync(new[] { Mod("A", "2.0.0", sha: "DEADBEEF") });

            Assert.False(report.AllSucceeded);
            Assert.Contains("SHA-256", Assert.Single(report.Failed).Error);
        }
        finally { try { File.Delete(pkg); } catch { } }
    }

    [Fact]
    public async Task Apply_AcceptsPackage_WithMatchingFeedHash()
    {
        var pkg = Path.Combine(Path.GetTempPath(), $"beepmod_{Guid.NewGuid():N}.nupkg");
        var bytes = System.Text.Encoding.UTF8.GetBytes("genuine-package");
        File.WriteAllBytes(pkg, bytes);
        var sha = Convert.ToHexString(SHA256.HashData(bytes));
        try
        {
            var packages = new FakePackages
            {
                OnUpdate = (_, v) => new ModuleUpdateOutcome { Success = true, NewVersion = v, PackagePath = pkg }
            };
            var report = await new ModuleUpdater(packages).ApplyAsync(new[] { Mod("A", "2.0.0", sha: sha) });

            Assert.True(report.AllSucceeded);
        }
        finally { try { File.Delete(pkg); } catch { } }
    }
}
