namespace TheTechIdea.Beep.Editor.Migration.Tests;

/// <summary>
/// Phase 5: verify compensation + rollback — the exact API the Setup framework's
/// <c>SchemaSetupStep</c> depends on (<c>BuildCompensationPlan</c>, <c>CheckRollbackReadiness</c>,
/// <c>RollbackFailedExecution</c>).
///
/// KEY FINDING (surfaced by verification): <c>BuildCompensationPlan</c> only emits actions for
/// <b>high-risk or relational (FK/index)</b> operations (`if (!highRisk) continue`). A plain
/// <c>CreateEntity</c>/<c>AddMissingColumns</c> plan — what the planner produces and what
/// <c>SchemaSetupStep</c> runs — gets an <b>empty</b> compensation plan, so
/// <c>RollbackFailedExecution</c> has nothing to undo. The reversibility is *defined*
/// (<c>ResolveRollbackMode(CreateEntity)=ReversibleDdl</c>) but never *populated* for normal plans —
/// the same "capability gated on ops the planner never emits" pattern as the policy gate.
/// </summary>
public class RollbackCompensationTests
{
    private sealed class Product { public int Id { get; set; } public string Name { get; set; } }

    private static (MigrationManager m, MigrationPlanArtifact plan) CreatePlan()
    {
        var harness = new MigrationTestHarness()
            .WithDesired(typeof(Product), MigrationTestHarness.Entity("Product", "Id", "Name"));
        var m = harness.Build();
        return (m, m.BuildMigrationPlanForTypes(new[] { typeof(Product) }));
    }

    // ── the finding: additive plans get no compensation ──────────────────────

    [Fact]
    public void Compensation_PlainCreateEntityPlan_HasNoActions()
    {
        var (m, plan) = CreatePlan();
        var comp = m.BuildCompensationPlan(plan);
        // PINNED FINDING: CreateEntity is neither high-risk nor relational → no compensation action.
        Assert.Empty(comp.Actions);
    }

    // ── but the compensation engine IS correct when fed an op it handles ─────

    [Fact]
    public void Compensation_Engine_ProducesReversibleDrop_ForHighRiskCreateEntity()
    {
        var m = new MigrationTestHarness().Build();
        var plan = new MigrationPlanArtifact
        {
            PlanId = "hb", DataSourceCategory = DatasourceCategory.RDBMS,
            Operations = { new MigrationPlanOperation
            {
                EntityName = "Product", Kind = MigrationPlanOperationKind.CreateEntity,
                RiskLevel = MigrationPlanRiskLevel.High   // force it past the high-risk gate
            }}
        };

        var comp = m.BuildCompensationPlan(plan);
        var action = Assert.Single(comp.Actions);
        Assert.Equal(MigrationRollbackMode.ReversibleDdl, action.RollbackMode);   // create → drop
    }

    [Fact]
    public void Compensation_Engine_MarksAddColumn_ForwardFix_NotAutoUndo()
    {
        // Gap #5: even when included (here forced high-risk), AddMissingColumns is not auto-reversible.
        var m = new MigrationTestHarness().Build();
        var plan = new MigrationPlanArtifact
        {
            PlanId = "hb",
            Operations = { new MigrationPlanOperation
            {
                EntityName = "Product", Kind = MigrationPlanOperationKind.AddMissingColumns,
                RiskLevel = MigrationPlanRiskLevel.High
            }}
        };

        var action = Assert.Single(m.BuildCompensationPlan(plan).Actions);
        Assert.Equal(MigrationRollbackMode.ForwardFixWithCompensation, action.RollbackMode);
    }

    // ── readiness + rollback ─────────────────────────────────────────────────

    [Fact]
    public void RollbackReadiness_AdditivePlan_IsReady()
    {
        var (m, plan) = CreatePlan();
        var report = m.CheckRollbackReadiness(plan, backupConfirmed: true, restoreTestEvidenceProvided: true);
        Assert.True(report.IsReady);
    }

    [Fact]
    public void Rollback_DryRun_ProducesResult_WithoutMutating()
    {
        var (m, plan) = CreatePlan();
        var exec = m.ExecuteMigrationPlan(plan);

        var rollback = m.RollbackFailedExecution(exec.ExecutionToken, dryRun: true);
        Assert.NotNull(rollback);
        Assert.True(rollback.DryRun);
    }

    [Fact]
    public void Rollback_OfAdditivePlan_HasNothingToUndo()
    {
        // Consequence of the finding: a created entity's failed execution has no compensation action,
        // so live rollback drops nothing. This is what SchemaSetupStep.RollbackAsync would hit.
        var harness = new MigrationTestHarness()
            .WithDesired(typeof(Product), MigrationTestHarness.Entity("Product", "Id", "Name"));
        var m = harness.Build();
        var exec = m.ExecuteMigrationPlan(m.BuildMigrationPlanForTypes(new[] { typeof(Product) }));
        harness.ProviderCalls.Clear();

        m.RollbackFailedExecution(exec.ExecutionToken, dryRun: false);

        Assert.DoesNotContain(harness.ProviderCalls, c => c.StartsWith("DropEntity:"));
    }
}
