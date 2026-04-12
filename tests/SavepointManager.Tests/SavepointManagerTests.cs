using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using SavepointManagerClass = TheTechIdea.Beep.Editor.UOWManager.Helpers.SavepointManager;

namespace SavepointManager.Tests;

public class SavepointManagerTests
{
    [Fact]
    public void CreateSavepoint_WithStateDetails_StoresMetadataAndSnapshot()
    {
        var manager = new SavepointManagerClass();
        var snapshot = new Dictionary<string, object>
        {
            ["CustomerId"] = "ALFKI",
            ["City"] = "Berlin"
        };

        var name = manager.CreateSavepoint("Orders", "BeforeEdit", 2, 25, true, snapshot);

        name.Should().Be("BeforeEdit");
        var savepoint = manager.ListSavepoints("Orders").Should().ContainSingle().Subject;
        savepoint.Name.Should().Be("BeforeEdit");
        savepoint.BlockName.Should().Be("Orders");
        savepoint.RecordIndex.Should().Be(2);
        savepoint.RecordCount.Should().Be(25);
        savepoint.WasDirty.Should().BeTrue();
        savepoint.RecordSnapshot.Should().BeEquivalentTo(snapshot);
        manager.SavepointExists("Orders", "BeforeEdit").Should().BeTrue();
    }

    [Fact]
    public void CreateSavepoint_WithoutName_GeneratesUniqueNamesPerBlock()
    {
        var manager = new SavepointManagerClass();

        var first = manager.CreateSavepoint("Orders");
        var second = manager.CreateSavepoint("Orders");

        first.Should().StartWith("SP_1_");
        second.Should().StartWith("SP_2_");
        second.Should().NotBe(first);
        manager.ListSavepoints("Orders").Select(sp => sp.Name).Should().Contain(new[] { first, second });
    }

    [Fact]
    public async Task RollbackToSavepointAsync_RemovesSavepointsCreatedAfterTarget()
    {
        var manager = new SavepointManagerClass();
        manager.CreateSavepoint("Orders", "SP1");
        manager.CreateSavepoint("Orders", "SP2");
        manager.CreateSavepoint("Orders", "SP3");

        var savepoints = manager.ListSavepoints("Orders").ToList();
        savepoints[0].Timestamp = new DateTime(2026, 1, 1, 0, 0, 1, DateTimeKind.Utc);
        savepoints[1].Timestamp = new DateTime(2026, 1, 1, 0, 0, 2, DateTimeKind.Utc);
        savepoints[2].Timestamp = new DateTime(2026, 1, 1, 0, 0, 3, DateTimeKind.Utc);

        var rolledBack = await manager.RollbackToSavepointAsync("Orders", "SP2");

        rolledBack.Should().BeTrue();
        manager.SavepointExists("Orders", "SP1").Should().BeTrue();
        manager.SavepointExists("Orders", "SP2").Should().BeTrue();
        manager.SavepointExists("Orders", "SP3").Should().BeFalse();
        manager.ListSavepoints("Orders").Select(sp => sp.Name).Should().Equal("SP1", "SP2");
    }

    [Fact]
    public async Task RollbackToSavepointAsync_WhenSavepointDoesNotExist_ReturnsFalse()
    {
        var manager = new SavepointManagerClass();
        manager.CreateSavepoint("Orders", "SP1");

        var rolledBack = await manager.RollbackToSavepointAsync("Orders", "Missing");

        rolledBack.Should().BeFalse();
        manager.ListSavepoints("Orders").Select(sp => sp.Name).Should().Equal("SP1");
    }

    [Fact]
    public void ReleaseSavepoint_AndReleaseAllSavepoints_RemoveEntries()
    {
        var manager = new SavepointManagerClass();
        manager.CreateSavepoint("Orders", "SP1");
        manager.CreateSavepoint("Orders", "SP2");
        manager.CreateSavepoint("Customers", "CSP1");

        var released = manager.ReleaseSavepoint("Orders", "SP1");

        released.Should().BeTrue();
        manager.SavepointExists("Orders", "SP1").Should().BeFalse();
        manager.SavepointExists("Orders", "SP2").Should().BeTrue();
        manager.SavepointExists("Customers", "CSP1").Should().BeTrue();

        manager.ReleaseAllSavepoints("Orders");

        manager.ListSavepoints("Orders").Should().BeEmpty();
        manager.SavepointExists("Customers", "CSP1").Should().BeTrue();
    }
}