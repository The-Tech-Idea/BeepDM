using TheTechIdea.Beep.Editor.Migration;

namespace TheTechIdea.Beep.Editor.Migration.Tests;

/// <summary>
/// Phase 1 keystone: proves the in-memory harness can construct and drive a real
/// <see cref="MigrationManager"/> with no live database. If these pass, the later verification
/// phases have somewhere to live.
/// </summary>
public class HarnessSmokeTests
{
    private sealed class Product { public int Id { get; set; } public string Name { get; set; } }

    [Fact]
    public void Harness_BuildsManager_AndPlansCreateForMissingEntity()
    {
        var harness = new MigrationTestHarness()
            .WithDesired(typeof(Product), MigrationTestHarness.Entity("Product", "Id", "Name"));
        // Product does not exist in the DB → the plan should propose creating it.

        var manager = harness.Build();
        var plan = manager.BuildMigrationPlanForTypes(new[] { typeof(Product) });

        Assert.NotNull(plan);
        Assert.NotEmpty(plan.Operations);
        Assert.Contains(plan.Operations, op => op.Kind == MigrationPlanOperationKind.CreateEntity
                                               && op.EntityName == "Product");
        Assert.False(string.IsNullOrEmpty(plan.PlanHash));
    }

    [Fact]
    public void Harness_ExistingUpToDateEntity_PlansUpToDate()
    {
        var product = MigrationTestHarness.Entity("Product", "Id", "Name");
        var harness = new MigrationTestHarness()
            .WithDesired(typeof(Product), product)
            .WithExisting(MigrationTestHarness.Entity("Product", "Id", "Name"));

        var plan = harness.Build().BuildMigrationPlanForTypes(new[] { typeof(Product) });

        Assert.NotNull(plan);
        // Same fields present → no create, no missing columns.
        Assert.DoesNotContain(plan.Operations, op => op.Kind == MigrationPlanOperationKind.CreateEntity);
    }
}
