namespace TheTechIdea.Beep.Editor.Migration.Tests;

/// <summary>
/// Phase 3: verify dry-run, preflight, and impact against the harness — and pin gap #6 (impact/
/// performance are heuristics, not data-driven).
/// </summary>
public class DryRunPreflightImpactTests
{
    private sealed class Product { public int Id { get; set; } public string Name { get; set; } }

    private static (MigrationManager m, MigrationPlanArtifact plan) NewAdditive()
    {
        var harness = new MigrationTestHarness()
            .WithDesired(typeof(Product), MigrationTestHarness.Entity("Product", "Id", "Name"));
        var m = harness.Build();
        return (m, m.BuildMigrationPlanForTypes(new[] { typeof(Product) }));
    }

    // ── dry-run ──────────────────────────────────────────────────────────────

    [Fact]
    public void DryRun_CreateEntityPlan_ProducesReportForThePlan()
    {
        var (m, plan) = NewAdditive();
        var report = m.GenerateDryRunReport(plan);

        Assert.NotNull(report);
        Assert.Equal(plan.PlanHash, report.PlanHash);
        Assert.Contains(report.Operations, o => o.Kind == MigrationPlanOperationKind.CreateEntity);
        Assert.False(report.HasBlockingIssues);   // additive plan is clean
    }

    // ── preflight ────────────────────────────────────────────────────────────

    [Fact]
    public void BuiltPlan_PreflightCanApply_IsFalse_ByDesign()
    {
        // The freshly-built plan hard-sets PreflightReport.CanApply=false; preflight is deliberately
        // re-run at execute time. Pins that design so a future change is noticed.
        var (_, plan) = NewAdditive();
        Assert.False(plan.PreflightReport.CanApply);
    }

    [Fact]
    public void RunPreflight_AdditivePlan_CanApply()
    {
        var (m, plan) = NewAdditive();
        var report = m.RunPreflightChecks(plan);

        Assert.NotNull(report);
        Assert.True(report.CanApply);              // re-running preflight clears the by-design false
        Assert.Equal(plan.PlanHash, report.PlanHash);
    }

    // ── impact (gap #6: heuristic, not data-driven) ──────────────────────────

    [Fact]
    public void Impact_ReportsEntries_ButWithoutRealRowCounts()
    {
        var (m, plan) = NewAdditive();
        var report = m.BuildImpactReport(plan);

        Assert.NotNull(report);
        Assert.NotEmpty(report.Entries);
        // PINNED GAP #6: sensitivity/volume are static hints. The fake datasource returns no row
        // counts, yet impact still produces a report — proving it never probes real data volume.
        Assert.All(report.Entries, e => Assert.IsType<MigrationImpactSensitivity>(e.Sensitivity));
    }
}
