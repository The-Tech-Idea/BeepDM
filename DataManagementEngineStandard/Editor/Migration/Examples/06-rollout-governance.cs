// Example 06 — Rollout Governance Gates
//
// Promote by wave with KPI and hard-stop policy.

using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Migration;

namespace TheTechIdea.Beep.Editor.Migration.Examples
{
    /// <summary>
    /// Phase 10 — evaluate the plan against a wave + KPI thresholds + hard-stop
    /// policy before promoting to a higher rollout wave.
    /// </summary>
    public static class Example06_RolloutGovernance
    {
        public static MigrationRolloutGovernanceReport Run(
            IDMEEditor editor,
            IDataSource dataSource,
            MigrationPlanArtifact plan)
        {
            if (editor == null) throw new ArgumentNullException(nameof(editor));
            if (dataSource == null) throw new ArgumentNullException(nameof(dataSource));
            if (plan == null) throw new ArgumentNullException(nameof(plan));

            var migrationManager = new MigrationManager(editor, dataSource);

            // ── 1. Build the governance request ────────────────────────────────
            var governanceRequest = new MigrationRolloutGovernanceRequest
            {
                Wave = MigrationRolloutWave.Wave2StandardProduction,
                IsCriticalDataSource = false,
                ReviewedBy = "release-manager",
                Notes = "Release train 2026.03.16",
                Thresholds = new MigrationRolloutKpiThresholds
                {
                    MinSuccessRate = 0.95,
                    MaxMeanExecutionDurationMilliseconds = 120000,
                    MaxRollbackInvocationRate = 0.10,
                    MaxPolicyBlockRatio = 0.25
                },
                HardStopPolicy = new MigrationRolloutHardStopPolicy
                {
                    StopOnAnyCriticalDiagnostic = true,
                    StopOnAnyRollbackForCriticalWave = true,
                    MaxFailureRate = 0.10
                }
            };

            // ── 2. Evaluate ────────────────────────────────────────────────────
            MigrationRolloutGovernanceReport governance =
                migrationManager.EvaluateRolloutGovernance(plan, governanceRequest);

            if (!governance.CanPromote)
            {
                Console.WriteLine(
                    $"Promotion blocked. Hard stop: {governance.HardStopTriggered}, " +
                    $"reason: {governance.HardStopReason}");
                foreach (var gate in governance.Gates)
                {
                    Console.WriteLine($"{gate.Gate} => {gate.Decision} ({gate.Observed} vs {gate.Threshold})");
                }
            }
            return governance;
        }
    }
}
