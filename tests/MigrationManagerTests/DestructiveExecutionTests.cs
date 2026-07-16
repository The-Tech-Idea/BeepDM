namespace TheTechIdea.Beep.Editor.Migration.Tests;

/// <summary>
/// Governed destructive migration: the planner emits a DropColumn (opt-in), the executor runs it
/// through the per-datasource provider (no raw DDL), and the governance gates (compensation +
/// rollback readiness + policy approval) are enforced. This is the end-to-end path that closes the
/// former additive-only gap — proven with the recording provider, no live DB.
/// </summary>
public class DestructiveExecutionTests
{
    private sealed class Product { public int Id { get; set; } public string Name { get; set; } }

    private static (MigrationTestHarness harness, MigrationManager m, MigrationPlanArtifact plan) DestructivePlan()
    {
        var harness = new MigrationTestHarness()
            .WithDesired(typeof(Product), MigrationTestHarness.Entity("Product", "Id", "Name"))
            .WithExisting(MigrationTestHarness.Entity("Product", "Id", "Name", "Obsolete"));
        var m = harness.Build();
        var plan = m.BuildMigrationPlanForTypes(new[] { typeof(Product) }, includeDestructive: true);
        return (harness, m, plan);
    }

    private static MigrationPolicyOptions Approval() => new()
    {
        EnvironmentTier = MigrationEnvironmentTier.Development,
        RequireApprovalForHighRisk = true,
        RequireApprovalForCriticalRisk = true,
        BlockDestructiveInProtectedEnvironments = true,
        Approver = "qa",
        OverrideReason = "unit-test destructive apply"
    };

    [Fact]
    public void DropColumnPlan_IsBlocked_WithoutApproval()
    {
        var (harness, m, plan) = DestructivePlan();

        // No approval options → the high-risk DropColumn is blocked at the preflight policy gate.
        var result = m.ExecuteMigrationPlan(plan);

        Assert.False(result.Success);
        Assert.DoesNotContain(harness.ProviderCalls, c => c.StartsWith("DropColumn:"));
    }

    [Fact]
    public void DropColumnPlan_Executes_ThroughProvider_WhenApprovedAndBackedUp()
    {
        var (harness, m, plan) = DestructivePlan();

        // Sanity: the planner did emit the destructive op.
        Assert.Contains(plan.Operations, o => o.Kind == MigrationPlanOperationKind.DropColumn);

        // Approve: supply backup + restore evidence and an approver/override for the high-risk op.
        plan.RollbackReadinessReport = m.CheckRollbackReadiness(
            plan, backupConfirmed: true, restoreTestEvidenceProvided: true);

        var result = m.ExecuteMigrationPlan(plan, policyOptions: Approval());

        Assert.True(result.Success, result.Message);
        Assert.Contains("DropColumn:Product.Obsolete", harness.ProviderCalls);
    }
}
