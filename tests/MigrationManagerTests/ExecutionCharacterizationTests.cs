namespace TheTechIdea.Beep.Editor.Migration.Tests;

/// <summary>
/// Phase 1 characterization of the two apply paths (pins gaps #1 execution-side and #7). The
/// governed <c>ExecuteMigrationPlan</c> path and the ungoverned imperative API behave differently
/// for destructive ops — this records that.
/// </summary>
public class ExecutionCharacterizationTests
{
    private sealed class Product { public int Id { get; set; } public string Name { get; set; } }

    // ── the imperative API DOES route destructive ops through the provider ───

    [Fact]
    public void Imperative_DropEntity_RoutesThroughProvider()
    {
        var harness = new MigrationTestHarness()
            .WithExisting(MigrationTestHarness.Entity("Product", "Id", "Name"));

        var result = harness.Build().DropEntity("Product");

        Assert.Equal(Errors.Ok, result.Flag);
        Assert.Contains("DropEntity:Product", harness.ProviderCalls);
    }

    [Fact]
    public void Imperative_AlterColumn_RoutesThroughProvider()
    {
        var harness = new MigrationTestHarness()
            .WithExisting(MigrationTestHarness.Entity("Product", "Id", "Name"));

        var newCol = new TheTechIdea.Beep.DataBase.EntityField { FieldName = "Name", Fieldtype = "System.Int32" };
        var result = harness.Build().AlterColumn("Product", "Name", newCol);

        // Destructive/alter ops work — but ONLY via this ungoverned path (no plan, no policy gate).
        Assert.Equal(Errors.Ok, result.Flag);
        Assert.Contains("AlterColumn:Product.Name", harness.ProviderCalls);
    }

    // ── the governed path executes additive plans end-to-end ─────────────────

    [Fact]
    public void Governed_ExecutePlan_CreatesMissingEntity()
    {
        var harness = new MigrationTestHarness()
            .WithDesired(typeof(Product), MigrationTestHarness.Entity("Product", "Id", "Name"));
        var manager = harness.Build();

        var plan = manager.BuildMigrationPlanForTypes(new[] { typeof(Product) });
        var result = manager.ExecuteMigrationPlan(plan);

        // Records the actual outcome of the governed path for an additive plan. If this ever changes,
        // we want to know — it's the path SchemaSetupStep runs.
        Assert.NotNull(result);
        Assert.Equal(plan.PlanHash, result.Checkpoint?.PlanHash);
    }

    // ── GAP #2/#3: no idempotency — re-running re-plans the same create ──────

    [Fact]
    public void RePlanning_SameSchema_StillPlansCreate_NoAppliedHistoryGate()
    {
        // Nothing consults migration history to skip an already-applied migration; a fresh manager
        // over the same (still-missing) entity plans the create again. Pins the write-only-history /
        // no-IsMigrationApplied gap (future: "idempotency").
        var harness = new MigrationTestHarness()
            .WithDesired(typeof(Product), MigrationTestHarness.Entity("Product", "Id", "Name"));

        var plan1 = harness.Build().BuildMigrationPlanForTypes(new[] { typeof(Product) });
        var plan2 = harness.Build().BuildMigrationPlanForTypes(new[] { typeof(Product) });

        Assert.Contains(plan1.Operations, o => o.Kind == MigrationPlanOperationKind.CreateEntity);
        Assert.Contains(plan2.Operations, o => o.Kind == MigrationPlanOperationKind.CreateEntity);
    }
}
