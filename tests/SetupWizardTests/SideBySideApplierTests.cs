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
/// Stage 11.B.2 — the side-by-side applier: materialize into <c>app-&lt;ver&gt;</c>, flip the link,
/// never touch the running version, abort a corrupt blob before flipping, and roll back / retire.
/// Uses a fake link so the logic is exercised without real filesystem junctions.
/// </summary>
public class SideBySideApplierTests : IDisposable
{
    private sealed class FakeLink : IDirectoryLink
    {
        public readonly Dictionary<string, string> Targets = new(StringComparer.OrdinalIgnoreCase);
        public int PointCalls;
        public void Point(string linkPath, string targetPath) { Targets[linkPath] = targetPath; PointCalls++; }
    }

    private readonly string _root;
    private readonly FakeLink _link = new();
    private readonly SideBySideApplier _applier;

    public SideBySideApplierTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "beepsxs_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        _applier = new SideBySideApplier(_link);
    }

    public void Dispose() { try { Directory.Delete(_root, true); } catch { } }

    private string CurrentLink => Path.Combine(_root, SideBySideApplier.CurrentLinkName);
    private string VerDir(string v) => Path.Combine(_root, "app-" + v);

    private static (PayloadManifest manifest, Dictionary<string, byte[]> blobs) Build(params (string path, string content)[] files)
    {
        var m = new PayloadManifest();
        var blobs = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var (path, content) in files)
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            var hash = Convert.ToHexString(SHA256.HashData(bytes));
            m.Entries.Add(new PayloadEntry { Path = path, Blob = hash, Size = bytes.Length });
            blobs[hash] = bytes;
        }
        return (m, blobs);
    }

    private static Func<string, CancellationToken, Task<byte[]>> Serve(Dictionary<string, byte[]> blobs)
        => (h, _) => Task.FromResult(blobs.TryGetValue(h, out var b) ? b : Array.Empty<byte>());

    // ── Full install ──

    [Fact]
    public async Task FullInstall_MaterializesFiles_AndFlipsLink()
    {
        var (remote, blobs) = Build(("app.exe", "binary-v1"), ("docs/readme.txt", "hello"));
        var plan = DeltaPlanner.ComputePlan(remote, local: null);

        var result = await _applier.ApplyAsync(new SideBySideApplier.Request
        {
            InstallRoot = _root, NewVersion = "1.0.0", Plan = plan, FetchBlob = Serve(blobs)
        });

        Assert.Equal(Errors.Ok, result.Flag);
        Assert.Equal("binary-v1", File.ReadAllText(Path.Combine(VerDir("1.0.0"), "app.exe")));
        Assert.Equal("hello", File.ReadAllText(Path.Combine(VerDir("1.0.0"), "docs", "readme.txt")));
        Assert.Equal(VerDir("1.0.0"), _link.Targets[CurrentLink]);
    }

    // ── Delta seeds from current ──

    [Fact]
    public async Task Delta_SeedsUnchangedFromCurrent_WritesChanged_DeletesRemoved()
    {
        // Pre-existing installed version on disk.
        Directory.CreateDirectory(VerDir("1.0.0"));
        File.WriteAllText(Path.Combine(VerDir("1.0.0"), "keep.txt"), "keep");
        File.WriteAllText(Path.Combine(VerDir("1.0.0"), "old.txt"), "old");

        var (local, _) = Build(("keep.txt", "keep"), ("old.txt", "old"));
        var (remote, blobs) = Build(("keep.txt", "keep"), ("changed.txt", "brand-new"));
        var plan = DeltaPlanner.ComputePlan(remote, local);

        var result = await _applier.ApplyAsync(new SideBySideApplier.Request
        {
            InstallRoot = _root, NewVersion = "1.1.0", CurrentVersion = "1.0.0", Plan = plan, FetchBlob = Serve(blobs)
        });

        Assert.Equal(Errors.Ok, result.Flag);
        Assert.Equal("keep", File.ReadAllText(Path.Combine(VerDir("1.1.0"), "keep.txt")));      // seeded
        Assert.Equal("brand-new", File.ReadAllText(Path.Combine(VerDir("1.1.0"), "changed.txt"))); // written
        Assert.False(File.Exists(Path.Combine(VerDir("1.1.0"), "old.txt")));                    // deleted
        Assert.Equal(VerDir("1.1.0"), _link.Targets[CurrentLink]);
    }

    [Fact]
    public async Task RunningVersion_IsNeverTouched()
    {
        Directory.CreateDirectory(VerDir("1.0.0"));
        File.WriteAllText(Path.Combine(VerDir("1.0.0"), "keep.txt"), "keep");
        File.WriteAllText(Path.Combine(VerDir("1.0.0"), "old.txt"), "old");

        var (local, _) = Build(("keep.txt", "keep"), ("old.txt", "old"));
        var (remote, blobs) = Build(("keep.txt", "keep"), ("changed.txt", "new"));
        var plan = DeltaPlanner.ComputePlan(remote, local);

        await _applier.ApplyAsync(new SideBySideApplier.Request
        { InstallRoot = _root, NewVersion = "1.1.0", CurrentVersion = "1.0.0", Plan = plan, FetchBlob = Serve(blobs) });

        // The old version folder is exactly as it was.
        Assert.Equal("keep", File.ReadAllText(Path.Combine(VerDir("1.0.0"), "keep.txt")));
        Assert.Equal("old", File.ReadAllText(Path.Combine(VerDir("1.0.0"), "old.txt")));
    }

    // ── Corrupt blob ──

    [Fact]
    public async Task CorruptBlob_Aborts_BeforeFlip()
    {
        var (remote, _) = Build(("app.exe", "genuine"));
        var plan = DeltaPlanner.ComputePlan(remote, local: null);

        // Serve tampered bytes for every hash → verification must fail.
        Func<string, CancellationToken, Task<byte[]>> tampered = (_, _) => Task.FromResult(Encoding.UTF8.GetBytes("TAMPERED"));

        var result = await _applier.ApplyAsync(new SideBySideApplier.Request
        { InstallRoot = _root, NewVersion = "1.0.0", Plan = plan, FetchBlob = tampered });

        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Contains("verification", result.Message);
        Assert.False(Directory.Exists(VerDir("1.0.0")), "a failed apply must leave no version folder");
        Assert.Equal(0, _link.PointCalls);
    }

    // ── Rollback ──

    [Fact]
    public async Task Rollback_FlipsBackToPreviousVersion()
    {
        var (m1, b1) = Build(("app.exe", "v1"));
        await _applier.ApplyAsync(new SideBySideApplier.Request
        { InstallRoot = _root, NewVersion = "1.0.0", Plan = DeltaPlanner.ComputePlan(m1, null), FetchBlob = Serve(b1) });

        var (m2, b2) = Build(("app.exe", "v2"));
        await _applier.ApplyAsync(new SideBySideApplier.Request
        { InstallRoot = _root, NewVersion = "1.1.0", CurrentVersion = "1.0.0", Plan = DeltaPlanner.ComputePlan(m2, null), FetchBlob = Serve(b2) });
        Assert.Equal(VerDir("1.1.0"), _link.Targets[CurrentLink]);

        var rb = _applier.Rollback(_root);

        Assert.Equal(Errors.Ok, rb.Flag);
        Assert.Equal(VerDir("1.0.0"), _link.Targets[CurrentLink]);
    }

    [Fact]
    public void Rollback_WithNoPrevious_Fails()
        => Assert.Equal(Errors.Failed, _applier.Rollback(_root).Flag);

    // ── Retirement ──

    [Fact]
    public async Task Retire_DeletesOldVersions_KeepsCurrentAndPrevious()
    {
        foreach (var (v, prev) in new[] { ("1.0.0", (string?)null), ("1.1.0", "1.0.0"), ("1.2.0", "1.1.0") })
        {
            var (m, b) = Build(("app.exe", "v-" + v));
            await _applier.ApplyAsync(new SideBySideApplier.Request
            { InstallRoot = _root, NewVersion = v, CurrentVersion = prev, Plan = DeltaPlanner.ComputePlan(m, null), FetchBlob = Serve(b) });
        }

        var removed = _applier.RetireOldVersions(_root);

        Assert.Equal(1, removed);
        Assert.False(Directory.Exists(VerDir("1.0.0")), "1.0.0 is neither current nor previous");
        Assert.True(Directory.Exists(VerDir("1.1.0")), "previous is kept for rollback");
        Assert.True(Directory.Exists(VerDir("1.2.0")), "current is kept");
    }

    // ── Crash safety ──

    [Fact]
    public async Task StaleStaging_FromAnInterruptedRun_IsCleanedOnNextApply()
    {
        var stale = Path.Combine(_root, ".staging-9.9.9");
        Directory.CreateDirectory(stale);
        File.WriteAllText(Path.Combine(stale, "half-written.tmp"), "junk");

        var (m, b) = Build(("app.exe", "v1"));
        await _applier.ApplyAsync(new SideBySideApplier.Request
        { InstallRoot = _root, NewVersion = "1.0.0", Plan = DeltaPlanner.ComputePlan(m, null), FetchBlob = Serve(b) });

        Assert.False(Directory.Exists(stale), "leftover staging from a dead run must be swept before applying");
    }
}
