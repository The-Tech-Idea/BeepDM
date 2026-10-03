namespace TheTechIdea.Beep.Editor.Migration.Tests;

/// <summary>
/// Phase 4: verify the core execution path (checkpoint, resume, gate sequence), including durable
/// resume — the checkpoint is re-hydrated from persisted history after the in-memory store is gone.
/// </summary>
[Collection("Checkpoint restart")]
public class ExecutionCheckpointResumeTests
{
    private sealed class Product { public int Id { get; set; } public string Name { get; set; } }
    private sealed class Order { public int Id { get; set; } public string Ref { get; set; } }

    private static (MigrationManager m, MigrationPlanArtifact plan) NewAdditive()
    {
        var harness = new MigrationTestHarness()
            .WithDesired(typeof(Product), MigrationTestHarness.Entity("Product", "Id", "Name"));
        var m = harness.Build();
        return (m, m.BuildMigrationPlanForTypes(new[] { typeof(Product) }));
    }

    [Fact]
    public void Execute_AdditivePlan_ProducesCheckpointWithSteps()
    {
        var (m, plan) = NewAdditive();
        var result = m.ExecuteMigrationPlan(plan);

        Assert.NotNull(result.Checkpoint);
        Assert.NotEmpty(result.Checkpoint.Steps);
        Assert.False(string.IsNullOrEmpty(result.ExecutionToken));
    }

    [Fact]
    public void GetExecutionCheckpoint_ReturnsCheckpoint_WithinProcess()
    {
        var (m, plan) = NewAdditive();
        var result = m.ExecuteMigrationPlan(plan);

        var fetched = m.GetExecutionCheckpoint(result.ExecutionToken);
        Assert.NotNull(fetched);
        Assert.Equal(result.ExecutionToken, fetched.ExecutionToken);
    }

    [Fact]
    public void PlanHashMismatch_OnReusedToken_IsRejected()
    {
        // A token created for one plan hash cannot be reused for a different plan.
        var harness = new MigrationTestHarness()
            .WithDesired(typeof(Product), MigrationTestHarness.Entity("Product", "Id", "Name"));
        var m = harness.Build();
        var plan = m.BuildMigrationPlanForTypes(new[] { typeof(Product) });
        var first = m.ExecuteMigrationPlan(plan);

        // A genuinely different plan (different entity → different hash).
        harness.WithDesired(typeof(Order), MigrationTestHarness.Entity("Order", "Id", "Ref"));
        var other = harness.Build();
        var otherPlan = other.BuildMigrationPlanForTypes(new[] { typeof(Order) });
        Assert.NotEqual(plan.PlanHash, otherPlan.PlanHash);   // self-validate the premise

        var reused = other.ExecuteMigrationPlan(otherPlan, executionToken: first.ExecutionToken);
        Assert.False(reused.Success);
        Assert.Contains("different migration plan hash", reused.Message);
    }

    [Fact]
    public void Resume_SurvivesA_Restart_FromPersistedCheckpoint()
    {
        // A new manager has no preview cache; it must reload the fixture's stored history.
        var (m, plan) = NewAdditive();
        var result = m.ExecuteMigrationPlan(plan);
        var token = result.ExecutionToken;

        m = new MigrationManager(m.DMEEditor, m.MigrateDataSource) { ExecutionTargetIdentity = m.ExecutionTargetIdentity };

        var fetched = m.GetExecutionCheckpoint(token);
        Assert.NotNull(fetched);
        Assert.Equal(token, fetched.ExecutionToken);

        var resumed = m.ResumeMigrationPlan(token);
        Assert.True(resumed.Success, resumed.Message);
        Assert.True(resumed.ResumedFromCheckpoint);
    }

    [Fact]
    public void PlanHash_Changes_WithCreateEntityColumnSet()
    {
        // Approval/checkpoint identity must cover the complete schema, not just the table name.
        var planA = new MigrationTestHarness()
            .WithDesired(typeof(Product), MigrationTestHarness.Entity("Product", "Id", "Name"))
            .Build().BuildMigrationPlanForTypes(new[] { typeof(Product) });
        var planB = new MigrationTestHarness()
            .WithDesired(typeof(Product), MigrationTestHarness.Entity("Product", "Id", "Name", "Price"))
            .Build().BuildMigrationPlanForTypes(new[] { typeof(Product) });

        Assert.NotEqual(planA.PlanHash, planB.PlanHash);
    }

}
