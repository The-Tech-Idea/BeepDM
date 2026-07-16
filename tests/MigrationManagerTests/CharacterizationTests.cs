namespace TheTechIdea.Beep.Editor.Migration.Tests;

/// <summary>
/// Phase 1 characterization tests: these PIN today's behavior — <b>including the known-wrong
/// structural gaps</b> — so later work can't silently regress the paths that a real consumer
/// (<c>SchemaSetupStep</c>) depends on. Each test that pins a gap says so, and names the future
/// phase that would flip it.
/// </summary>
public class CharacterizationTests
{
    private sealed class Product { public int Id { get; set; } public string Name { get; set; } }

    // ── Column type changes are NOT auto-planned (by design) ─────────────────

    [Fact]
    public void Planner_DoesNotEmit_AlterColumn_WhenExistingColumnTypeChanged()
    {
        // Desired: Name is an int. Current DB: Name is a string. AlterColumn is deliberately NOT
        // auto-planned: a reflected .NET type ("System.Int32") vs a live DB type string is not a
        // reliable equality signal, so auto-diffing would flag every column as "changed". Type
        // changes are driven by the imperative AlterColumn(...) API instead.
        var desired = MigrationTestHarness.Entity("Product", "Id");
        desired.Fields.Add(new TheTechIdea.Beep.DataBase.EntityField
        { FieldName = "Name", Fieldtype = "System.Int32", AllowDBNull = true });

        var current = MigrationTestHarness.Entity("Product", "Id", "Name"); // Name = string

        var plan = new MigrationTestHarness()
            .WithDesired(typeof(Product), desired)
            .WithExisting(current)
            .Build()
            .BuildMigrationPlanForTypes(new[] { typeof(Product) }, includeDestructive: true);

        Assert.DoesNotContain(plan.Operations, o => o.Kind == MigrationPlanOperationKind.AlterColumn);
    }

    // ── DropColumn: not planned by default; opt-in via includeDestructive ─────

    [Fact]
    public void Planner_OmitsDropColumn_ByDefault()
    {
        // Default plans stay additive-safe: a column removed from the model is NOT dropped unless asked.
        var desired = MigrationTestHarness.Entity("Product", "Id", "Name");
        var current = MigrationTestHarness.Entity("Product", "Id", "Name", "Obsolete");

        var plan = new MigrationTestHarness()
            .WithDesired(typeof(Product), desired)
            .WithExisting(current)
            .Build()
            .BuildMigrationPlanForTypes(new[] { typeof(Product) }); // includeDestructive defaults to false

        Assert.DoesNotContain(plan.Operations, o => o.Kind == MigrationPlanOperationKind.DropColumn);
    }

    [Fact]
    public void Planner_EmitsDropColumn_WhenIncludeDestructive()
    {
        // Desired drops "Obsolete"; current DB still has it. With includeDestructive the planner emits a
        // DropColumn op, marked destructive/high-risk so policy + compensation engage.
        var desired = MigrationTestHarness.Entity("Product", "Id", "Name");
        var current = MigrationTestHarness.Entity("Product", "Id", "Name", "Obsolete");

        var plan = new MigrationTestHarness()
            .WithDesired(typeof(Product), desired)
            .WithExisting(current)
            .Build()
            .BuildMigrationPlanForTypes(new[] { typeof(Product) }, includeDestructive: true);

        var drop = Assert.Single(plan.Operations, o => o.Kind == MigrationPlanOperationKind.DropColumn);
        Assert.Contains("Obsolete", drop.MissingColumns);
        Assert.True(drop.IsDestructive);
        Assert.Equal(MigrationPlanRiskLevel.High, drop.RiskLevel);
    }

    [Fact]
    public void Planner_DoesEmit_AddMissingColumns_WhenDesiredAddsAColumn()
    {
        // The additive path DOES work — this is the behavior we must not regress.
        var desired = MigrationTestHarness.Entity("Product", "Id", "Name", "Price");
        var current = MigrationTestHarness.Entity("Product", "Id", "Name");

        var plan = new MigrationTestHarness()
            .WithDesired(typeof(Product), desired)
            .WithExisting(current)
            .Build()
            .BuildMigrationPlanForTypes(new[] { typeof(Product) });

        Assert.Contains(plan.Operations, o => o.Kind == MigrationPlanOperationKind.AddMissingColumns);
    }
}
