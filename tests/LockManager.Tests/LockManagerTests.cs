using System.Threading.Tasks;
using FluentAssertions;
using TheTechIdea.Beep.Editor.Forms.Models;
using Xunit;
using LockManagerClass = TheTechIdea.Beep.Editor.UOWManager.Helpers.LockManager;

namespace LockManager.Tests;

public class LockManagerTests
{
    [Fact]
    public void SetLockConfiguration_RoundTripsPerBlock()
    {
        var manager = new LockManagerClass();

        manager.SetLockMode("Orders", LockMode.Automatic);
        manager.SetLockOnEdit("Orders", true);

        manager.GetLockMode("Orders").Should().Be(LockMode.Automatic);
        manager.GetLockOnEdit("Orders").Should().BeTrue();
        manager.GetLockMode("Customers").Should().Be(LockMode.None);
        manager.GetLockOnEdit("Customers").Should().BeFalse();
    }

    [Fact]
    public async Task LockCurrentRecordAsync_WhenModeIsNone_DoesNotCreateStoredLock()
    {
        var manager = new LockManagerClass();
        manager.SetCurrentRecordIndex("Orders", 4);

        var locked = await manager.LockCurrentRecordAsync("Orders");

        locked.Should().BeTrue();
        manager.IsRecordLocked("Orders", 4).Should().BeFalse();
        manager.GetLockedRecordCount("Orders").Should().Be(0);
    }

    [Fact]
    public async Task LockCurrentRecordAsync_WhenLockingEnabled_CreatesLockAndIsIdempotent()
    {
        var manager = new LockManagerClass();
        manager.SetLockMode("Orders", LockMode.Manual);
        manager.SetCurrentRecordIndex("Orders", 2);

        var first = await manager.LockCurrentRecordAsync("Orders");
        var second = await manager.LockCurrentRecordAsync("Orders");

        first.Should().BeTrue();
        second.Should().BeTrue();
        manager.IsCurrentRecordLocked("Orders").Should().BeTrue();
        manager.GetLockedRecordCount("Orders").Should().Be(1);

        var info = manager.GetLockInfo("Orders", 2);
        info.Should().NotBeNull();
        info!.BlockName.Should().Be("Orders");
        info.RecordIndex.Should().Be(2);
        info.LockedBy.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task AutoLockIfNeededAsync_WhenAutomaticAndLockOnEditEnabled_CreatesLock()
    {
        var manager = new LockManagerClass();
        manager.SetLockMode("Orders", LockMode.Automatic);
        manager.SetLockOnEdit("Orders", true);
        manager.SetCurrentRecordIndex("Orders", 7);

        var locked = await manager.AutoLockIfNeededAsync("Orders");

        locked.Should().BeTrue();
        manager.IsRecordLocked("Orders", 7).Should().BeTrue();
        manager.GetLockedRecordCount("Orders").Should().Be(1);
    }

    [Fact]
    public async Task AutoLockIfNeededAsync_WhenDisabled_DoesNotCreateLock()
    {
        var manager = new LockManagerClass();
        manager.SetLockMode("Orders", LockMode.Automatic);
        manager.SetLockOnEdit("Orders", false);
        manager.SetCurrentRecordIndex("Orders", 7);

        var locked = await manager.AutoLockIfNeededAsync("Orders");

        locked.Should().BeTrue();
        manager.IsRecordLocked("Orders", 7).Should().BeFalse();
        manager.GetLockedRecordCount("Orders").Should().Be(0);
    }

    [Fact]
    public async Task UnlockCurrentRecord_AndUnlockAllRecords_RemoveStoredLocks()
    {
        var manager = new LockManagerClass();
        manager.SetLockMode("Orders", LockMode.Manual);
        manager.SetCurrentRecordIndex("Orders", 1);
        await manager.LockCurrentRecordAsync("Orders");
        manager.SetCurrentRecordIndex("Orders", 3);
        await manager.LockCurrentRecordAsync("Orders");

        var unlockedCurrent = manager.UnlockCurrentRecord("Orders");

        unlockedCurrent.Should().BeTrue();
        manager.IsRecordLocked("Orders", 1).Should().BeTrue();
        manager.IsRecordLocked("Orders", 3).Should().BeFalse();
        manager.GetAllLocks("Orders").Should().ContainSingle();

        manager.UnlockAllRecords("Orders");

        manager.GetLockedRecordCount("Orders").Should().Be(0);
        manager.GetAllLocks("Orders").Should().BeEmpty();
    }
}