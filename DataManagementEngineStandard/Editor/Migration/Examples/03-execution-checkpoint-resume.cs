// Example 03 — Execute, Checkpoint, Resume
//
// Use execution policy + resumable checkpoints for production safety.

using System;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Migration;

namespace TheTechIdea.Beep.Editor.Migration.Examples
{
    /// <summary>
    /// Phase 5 — execute the plan with a retry policy, capture the checkpoint on
    /// failure, and resume from the last completed step.
    /// </summary>
    public static class Example03_ExecutionCheckpointResume
    {
        public static MigrationExecutionResult Run(
            IDMEEditor editor,
            IDataSource dataSource,
            MigrationPlanArtifact plan)
        {
            if (editor == null) throw new ArgumentNullException(nameof(editor));
            if (dataSource == null) throw new ArgumentNullException(nameof(dataSource));
            if (plan == null) throw new ArgumentNullException(nameof(plan));

            var migrationManager = new MigrationManager(editor, dataSource);

            // ── 1. Build an execution policy with retry + operator intervention ─
            var execPolicy = new MigrationExecutionPolicy
            {
                MaxTransientRetries = 3,
                RetryDelayMilliseconds = 500,
                RequireOperatorInterventionOnHardFail = true
            };

            // ── 2. Run the plan ─────────────────────────────────────────────────
            MigrationExecutionResult result = migrationManager.ExecuteMigrationPlan(
                plan,
                policy: execPolicy);

            // ── 3. On failure, capture the checkpoint and resume ───────────────
            if (!result.Success && !string.IsNullOrWhiteSpace(result.ExecutionToken))
            {
                MigrationExecutionCheckpoint checkpoint = migrationManager.GetExecutionCheckpoint(result.ExecutionToken);
                Console.WriteLine($"Last completed step: {checkpoint?.LastCompletedStep}");

                // after operator action / fix:
                MigrationExecutionResult resumed = migrationManager.ResumeMigrationPlan(
                    result.ExecutionToken,
                    execPolicy);
                Console.WriteLine($"Resumed success: {resumed.Success}");
                return resumed;
            }

            return result;
        }
    }
}
