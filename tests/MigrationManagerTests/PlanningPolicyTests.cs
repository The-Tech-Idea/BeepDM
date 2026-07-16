namespace TheTechIdea.Beep.Editor.Migration.Tests;

/// <summary>
/// Phase 2: verify planning, policy evaluation, and CI gates against the harness — and pin the
/// policy-side of gap #1 (the destructive block rule is unreachable because the planner never
/// produces a destructive operation).
/// </summary>
public class PlanningPolicyTests
{
    private sealed class Product { public int Id { get; set; } public string Name { get; set; } }

    private static MigrationManager ManagerFor(EntityStructureShape shape, out MigrationPlanArtifact plan)
    {
        var harness = new MigrationTestHarness()
            .WithDesired(typeof(Product), MigrationTestHarness.Entity("Product", "Id", "Name", "Price"));
        if (shape == EntityStructureShape.PartiallyExists)
            harness.WithExisting(MigrationTestHarness.Entity("Product", "Id", "Name")); // Price missing
        var m = harness.Build();
        plan = m.BuildMigrationPlanForTypes(new[] { typeof(Product) });
        return m;
    }

    private enum EntityStructureShape { Missing, PartiallyExists }

    // ── planning ─────────────────────────────────────────────────────────────

    [Fact]
    public void Plan_HasStableHash_AcrossBuilds()
    {
        ManagerFor(EntityStructureShape.Missing, out var a);
        ManagerFor(EntityStructureShape.Missing, out var b);
        Assert.Equal(a.PlanHash, b.PlanHash);   // deterministic plan hash
    }

    [Fact]
    public void Plan_MissingColumn_ProducesAddMissingColumns()
    {
        ManagerFor(EntityStructureShape.PartiallyExists, out var plan);
        var op = Assert.Single(plan.Operations, o => o.Kind == MigrationPlanOperationKind.AddMissingColumns);
        Assert.Contains(op.MissingColumns, c => c.Contains("Price", StringComparison.OrdinalIgnoreCase));
    }

    // ── policy ───────────────────────────────────────────────────────────────

    [Fact]
    public void Policy_AdditivePlan_IsNotBlocking()
    {
        var m = ManagerFor(EntityStructureShape.PartiallyExists, out var plan);
        var eval = m.EvaluateMigrationPlanPolicy(plan);
        Assert.False(eval.HasBlockingFindings);
    }

    [Fact]
    public void Policy_Engine_BlocksDestructiveDropColumn_InProtectedEnvironment()
    {
        // The policy engine IS correct: fed a plan that DOES contain a column-drop, it blocks in a
        // protected tier. This isolates gap #1 — the rule works; the PLANNER is what never produces
        // a column/table-destructive op to feed it.
        var m = new MigrationTestHarness().Build();
        var plan = new MigrationPlanArtifact
        {
            PlanId = "hand-built",
            Operations = { new MigrationPlanOperation
            {
                EntityName = "Product", Kind = MigrationPlanOperationKind.DropColumn,
                TargetName = "Obsolete", IsDestructive = true, RiskLevel = MigrationPlanRiskLevel.High
            }}
        };

        var eval = m.EvaluateMigrationPlanPolicy(plan, new MigrationPolicyOptions
        {
            EnvironmentTier = MigrationEnvironmentTier.Production,
            BlockDestructiveInProtectedEnvironments = true
        });

        Assert.True(eval.HasBlockingFindings);
    }

    [Fact]
    public void Planner_NeverEmits_ColumnOrTable_DestructiveOps()
    {
        // PINNED GAP #1 (planning side, precise): the planner can emit Drop FK/Index (constraint
        // reversals), but NEVER AlterColumn/DropColumn/DropEntity/RenameEntity/RenameColumn/Truncate.
        var desired = MigrationTestHarness.Entity("Product", "Id");            // drops Name, retypes nothing
        var current = MigrationTestHarness.Entity("Product", "Id", "Name");
        new MigrationTestHarness().WithDesired(typeof(Product), desired).WithExisting(current)
            .Build().BuildMigrationPlanForTypes(new[] { typeof(Product) })
            .Operations.ForEach(o => Assert.DoesNotContain(o.Kind, new[]
            {
                MigrationPlanOperationKind.AlterColumn, MigrationPlanOperationKind.DropColumn,
                MigrationPlanOperationKind.DropEntity, MigrationPlanOperationKind.RenameEntity,
                MigrationPlanOperationKind.RenameColumn, MigrationPlanOperationKind.TruncateEntity
            }));
    }

    // ── CI gates ─────────────────────────────────────────────────────────────

    [Fact]
    public void CiValidation_CleanAdditivePlan_CanMerge()
    {
        var m = ManagerFor(EntityStructureShape.PartiallyExists, out var plan);
        var report = m.ValidatePlanForCi(plan);
        Assert.True(report.CanMerge);
        Assert.NotEmpty(report.Gates);   // the four gates ran
    }
}
