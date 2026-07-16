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

    // ── GAP #1: the governed planner is additive-only ────────────────────────

    [Fact]
    public void Planner_DoesNotEmit_AlterColumn_WhenExistingColumnTypeChanged()
    {
        // Desired: Name is an int. Current DB: Name is a string. A real migration engine would plan
        // an AlterColumn. This one does NOT — BuildPlanOperation only detects MISSING columns to add,
        // never diffs an existing column against a changed desired one.
        var desired = MigrationTestHarness.Entity("Product", "Id");
        desired.Fields.Add(new TheTechIdea.Beep.DataBase.EntityField
        { FieldName = "Name", Fieldtype = "System.Int32", AllowDBNull = true });

        var current = MigrationTestHarness.Entity("Product", "Id", "Name"); // Name = string

        var plan = new MigrationTestHarness()
            .WithDesired(typeof(Product), desired)
            .WithExisting(current)
            .Build()
            .BuildMigrationPlanForTypes(new[] { typeof(Product) });

        // PINNED GAP (future: "close additive-only pipeline"): no AlterColumn is ever produced.
        Assert.DoesNotContain(plan.Operations, o => o.Kind == MigrationPlanOperationKind.AlterColumn);
    }

    [Fact]
    public void Planner_DoesNotEmit_DropColumn_WhenDesiredRemovesAColumn()
    {
        // Desired drops "Obsolete"; current DB still has it. A real engine plans a DropColumn.
        var desired = MigrationTestHarness.Entity("Product", "Id", "Name");
        var current = MigrationTestHarness.Entity("Product", "Id", "Name", "Obsolete");

        var plan = new MigrationTestHarness()
            .WithDesired(typeof(Product), desired)
            .WithExisting(current)
            .Build()
            .BuildMigrationPlanForTypes(new[] { typeof(Product) });

        // PINNED GAP: removed columns are invisible to the planner.
        Assert.DoesNotContain(plan.Operations, o => o.Kind == MigrationPlanOperationKind.DropColumn);
        Assert.DoesNotContain(plan.Operations, o => o.Kind == MigrationPlanOperationKind.DropEntity);
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
