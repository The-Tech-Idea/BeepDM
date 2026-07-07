// Example 04 — Compensation and Rollback
//
// Prepare rollback evidence before execution, and run rollback when needed.

using System;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Migration;

namespace TheTechIdea.Beep.Editor.Migration.Examples
{
    /// <summary>
    /// Phase 6 — build a compensation plan, check rollback readiness, and run
    /// rollback when an execution has failed.
    /// </summary>
    public static class Example04_RollbackCompensation
    {
        public static (MigrationCompensationPlan Compensation, MigrationRollbackReadinessReport Readiness)
            Run(IDMEEditor editor, IDataSource dataSource, MigrationPlanArtifact plan,
                string failedExecutionToken = null)
        {
            if (editor == null) throw new ArgumentNullException(nameof(editor));
            if (dataSource == null) throw new ArgumentNullException(nameof(dataSource));
            if (plan == null) throw new ArgumentNullException(nameof(plan));

            var migrationManager = new MigrationManager(editor, dataSource);

            // ── 1. Build the compensation plan ─────────────────────────────────
            MigrationCompensationPlan compensationPlan = migrationManager.BuildCompensationPlan(plan);

            // ── 2. Check rollback readiness (caller provides evidence) ─────────
            MigrationRollbackReadinessReport readiness = migrationManager.CheckRollbackReadiness(
                plan,
                backupConfirmed: true,
                restoreTestEvidenceProvided: true,
                restoreTestEvidence: "Restore test run id: restore-2026-03-16-01");

            if (!readiness.IsReady)
            {
                throw new InvalidOperationException("Rollback readiness failed.");
            }

            // ── 3. If a previous run failed, run rollback (dry-run first) ───────
            if (!string.IsNullOrWhiteSpace(failedExecutionToken))
            {
                MigrationRollbackResult rollbackResult = migrationManager.RollbackFailedExecution(
                    executionToken: failedExecutionToken,
                    dryRun: true);

                Console.WriteLine($"Rollback simulation success: {rollbackResult.Success}");
            }

            return (compensationPlan, readiness);
        }
    }
}
