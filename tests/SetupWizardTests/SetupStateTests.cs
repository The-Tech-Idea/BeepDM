using System.Collections.Concurrent;
using TheTechIdea.Beep.Addin;
using Moq;

namespace TheTechIdea.Beep.SetUp.Tests;

public class SetupStateTests
{
    [Fact]
    public void RunId_IsAssigned_ByWizard_OnFreshRun()
    {
        var state = new SetupState();
        Assert.Null(state.RunId);
    }

    [Fact]
    public void IsStepCompleted_ReturnsTrue_WhenInCompletedSet()
    {
        var state = new SetupState();
        state.CompletedStepIds.Add("step-1");
        Assert.True(state.IsStepCompleted("step-1"));
    }

    [Fact]
    public void IsStepCompleted_ReturnsTrue_WhenInSkippedSet()
    {
        var state = new SetupState();
        state.SkippedStepIds.Add("step-2");
        Assert.True(state.IsStepCompleted("step-2"));
    }

    [Fact]
    public void IsStepCompleted_ReturnsFalse_ForUnknownStep()
    {
        var state = new SetupState();
        Assert.False(state.IsStepCompleted("nonexistent"));
    }

    [Fact]
    public void FailedStepId_TracksLastFailure()
    {
        var state = new SetupState();
        state.FailedStepId = "driver-provision";
        Assert.Equal("driver-provision", state.FailedStepId);
    }

    [Fact]
    public void SchemaHash_PersistsEntityHash()
    {
        var state = new SetupState();
        state.SchemaHash = "ABC123";
        Assert.Equal("ABC123", state.SchemaHash);
    }

    [Fact]
    public void Serialization_RoundTrips_BasicProperties()
    {
        var state = new SetupState
        {
            RunId = Guid.NewGuid().ToString("N"),
            SchemaHash = "HASH123",
            StartedAt = DateTimeOffset.UtcNow,
            FailedStepId = "schema-setup"
        };
        state.CompletedStepIds.Add("driver-provision");
        state.CompletedStepIds.Add("connection-config");
        state.SkippedStepIds.Add("data-import");
        state.CompletedSeederIds.Add("roles-seeder");
        state.Metadata["MigrationPlanId"] = "plan-1";

        var json = System.Text.Json.JsonSerializer.Serialize(state);
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<SetupState>(json);

        Assert.NotNull(deserialized);
        Assert.Equal(state.RunId, deserialized!.RunId);
        Assert.Equal(state.SchemaHash, deserialized.SchemaHash);
        Assert.Equal(state.FailedStepId, deserialized.FailedStepId);
        Assert.Contains("driver-provision", deserialized.CompletedStepIds);
        Assert.Contains("connection-config", deserialized.CompletedStepIds);
        Assert.Contains("data-import", deserialized.SkippedStepIds);
        Assert.Contains("roles-seeder", deserialized.CompletedSeederIds);
        Assert.Equal("plan-1", deserialized.Metadata["MigrationPlanId"]);
    }
}
