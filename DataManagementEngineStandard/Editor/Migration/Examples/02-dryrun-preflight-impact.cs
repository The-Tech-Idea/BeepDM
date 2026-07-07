// Example 02 — Dry-Run, Preflight, and Impact
//
// Generate safety evidence before approval/execution.

using System;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Migration;

namespace TheTechIdea.Beep.Editor.Migration.Examples
{
    /// <summary>
    /// Phase 4 — generate dry-run, preflight, and impact reports so a reviewer
    /// can see what the migration will do without applying it.
    /// </summary>
    public static class Example02_DryRunPreflightImpact
    {
        public static (MigrationDryRunReport DryRun, MigrationPreflightReport Preflight,
                       MigrationImpactReport Impact, MigrationPerformancePlan Performance)
            Run(IDMEEditor editor, IDataSource dataSource, MigrationPlanArtifact plan)
        {
            if (editor == null) throw new ArgumentNullException(nameof(editor));
            if (dataSource == null) throw new ArgumentNullException(nameof(dataSource));
            if (plan == null) throw new ArgumentNullException(nameof(plan));

            var migrationManager = new MigrationManager(editor, dataSource);

            // ── 1. Dry-run ──────────────────────────────────────────────────────────
            MigrationDryRunReport dryRun = migrationManager.GenerateDryRunReport(plan);
            if (dryRun.HasBlockingIssues)
            {
                throw new InvalidOperationException("Dry-run has blocking issues.");
            }

            // ── 2. Preflight checks (gates before apply) ────────────────────────
            MigrationPreflightReport preflight = migrationManager.RunPreflightChecks(plan);
            if (!preflight.CanApply)
            {
                throw new InvalidOperationException("Preflight checks did not pass.");
            }

            // ── 3. Impact report (sensitivity, locks, data loss risk) ───────────
            MigrationImpactReport impact = migrationManager.BuildImpactReport(plan);

            // ── 4. Performance plan (window estimate + KPIs) ─────────────────────
            MigrationPerformancePlan performance = migrationManager.BuildPerformancePlan(plan);

            Console.WriteLine($"Dry-run operations: {dryRun.Operations.Count}");
            Console.WriteLine($"Impact entries: {impact.Entries.Count}");
            Console.WriteLine($"Estimated window (min): {performance?.Kpis?.PlannedMigrationWindowMinutes}");

            return (dryRun, preflight, impact, performance);
        }
    }
}
