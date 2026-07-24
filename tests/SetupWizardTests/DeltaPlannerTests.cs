using System.Linq;
using TheTechIdea.Beep.Updates;
using Xunit;

namespace TheTechIdea.Beep.Updates.Tests;

/// <summary>
/// Stage 11.B.1 — the pure delta planner. Blob-level diffing means a rename or a duplicate costs
/// zero download; the matrix below pins that, plus writes, deletes, the full-install fallback, and
/// the byte accounting the UI reports as "saved vs full".
/// </summary>
public class DeltaPlannerTests
{
    private static PayloadManifest M(params (string path, string blob, long size)[] entries)
        => new() { Entries = entries.Select(e => new PayloadEntry { Path = e.path, Blob = e.blob, Size = e.size }).ToList() };

    [Fact]
    public void Unchanged_PlansNothing()
    {
        var m = M(("a.dll", "B1", 100), ("b.dll", "B2", 200));

        var plan = DeltaPlanner.ComputePlan(remote: m, local: M(("a.dll", "B1", 100), ("b.dll", "B2", 200)));

        Assert.False(plan.IsFullInstall);
        Assert.Empty(plan.BlobsToFetch);
        Assert.Empty(plan.FilesToWrite);
        Assert.Empty(plan.FilesToDelete);
        Assert.Equal(0, plan.DownloadBytes);
    }

    [Fact]
    public void OneFileChanged_FetchesOnlyThatBlob()
    {
        var local = M(("a.dll", "B1", 100), ("b.dll", "B2", 200));
        var remote = M(("a.dll", "B1new", 150), ("b.dll", "B2", 200)); // only a.dll changed

        var plan = DeltaPlanner.ComputePlan(remote, local);

        Assert.Equal("a.dll", Assert.Single(plan.FilesToWrite).Path);
        Assert.Equal("B1new", Assert.Single(plan.BlobsToFetch).Hash);
        Assert.Equal(150, plan.DownloadBytes);
        Assert.Empty(plan.FilesToDelete);
    }

    [Fact]
    public void Rename_SameBlob_CostsZeroDownload()
    {
        var local = M(("old/name.dll", "BX", 500));
        var remote = M(("new/name.dll", "BX", 500)); // moved, identical content

        var plan = DeltaPlanner.ComputePlan(remote, local);

        Assert.Equal("new/name.dll", Assert.Single(plan.FilesToWrite).Path);
        Assert.Equal("old/name.dll", Assert.Single(plan.FilesToDelete));
        Assert.Empty(plan.BlobsToFetch);
        Assert.Equal(0, plan.DownloadBytes);
    }

    [Fact]
    public void RemovedFile_IsPlannedForDeletion()
    {
        var local = M(("keep.dll", "B1", 100), ("gone.dll", "B2", 200));
        var remote = M(("keep.dll", "B1", 100));

        var plan = DeltaPlanner.ComputePlan(remote, local);

        Assert.Equal("gone.dll", Assert.Single(plan.FilesToDelete));
        Assert.Empty(plan.BlobsToFetch);
        Assert.Empty(plan.FilesToWrite);
    }

    [Fact]
    public void NoLocalManifest_IsFullInstall()
    {
        var remote = M(("a.dll", "B1", 100), ("b.dll", "B2", 200));

        var plan = DeltaPlanner.ComputePlan(remote, local: null);

        Assert.True(plan.IsFullInstall);
        Assert.Equal(2, plan.BlobsToFetch.Count);
        Assert.Equal(2, plan.FilesToWrite.Count);
        Assert.Equal(300, plan.DownloadBytes);
        Assert.Equal(300, plan.FullBytes);
    }

    [Fact]
    public void SharedBlob_IsCountedAndFetchedOnce()
    {
        // Two files with identical content collapse to one blob.
        var remote = M(("a.dll", "SAME", 100), ("copy/a.dll", "SAME", 100));

        var plan = DeltaPlanner.ComputePlan(remote, local: null);

        Assert.Single(plan.BlobsToFetch);           // one blob, not two
        Assert.Equal(2, plan.FilesToWrite.Count);   // but both files written
        Assert.Equal(100, plan.DownloadBytes);
        Assert.Equal(100, plan.FullBytes);
    }

    [Fact]
    public void Savings_AreFullMinusDownload()
    {
        var local = M(("a.dll", "B1", 100), ("b.dll", "B2", 900));
        var remote = M(("a.dll", "B1new", 100), ("b.dll", "B2", 900)); // only the 100-byte file changed

        var plan = DeltaPlanner.ComputePlan(remote, local);

        Assert.Equal(100, plan.DownloadBytes);
        Assert.Equal(1000, plan.FullBytes);
        Assert.Equal(900, plan.SavingsBytes);
    }
}
