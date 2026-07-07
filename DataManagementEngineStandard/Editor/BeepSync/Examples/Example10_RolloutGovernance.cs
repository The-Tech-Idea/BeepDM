// Example 10 — Rollout governance
// Demonstrates evaluating promotion eligibility based on measured sync KPIs and promoting
// the plan to the next wave when allowed.
//
// Phases covered: Phase 10 (Rollout Governance & KPI Gates)

using System;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.BeepSync;

namespace TheTechIdea.Beep.Editor.BeepSync.Examples
{
    /// <summary>
    /// Phase 10 — Wave promotion: evaluate the plan's measured KPIs against the threshold
    /// policy and call <c>PromoteWave</c> when the gates pass.
    /// </summary>
    public static class Example10_RolloutGovernance
    {
        public static SyncRolloutWave Run(
            IDMEEditor editor,
            DataSyncSchema schema,
            SyncMetrics lastRunMetrics,
            SyncKpiThresholds thresholds = null)
        {
            if (editor == null) throw new ArgumentNullException(nameof(editor));
            if (schema == null) throw new ArgumentNullException(nameof(schema));
            if (lastRunMetrics == null) throw new ArgumentNullException(nameof(lastRunMetrics));

            // ── 1. Create the manager ──────────────────────────────────────────────
            var syncManager = new BeepSyncManager(editor);

            // ── 2. Evaluate promotion eligibility ─────────────────────────────────
            SyncGovernanceReport report = syncManager.EvaluateRolloutGovernance(
                schema, lastRunMetrics, thresholds);

            // ── 3. Log the outcome ────────────────────────────────────────────────
            Console.WriteLine(
                $"Plan '{report.PlanId}': {report.CurrentWave} → {report.RecommendedWave} " +
                $"(promote={report.Promote}, blockers={report.BlockerReasons.Count})");
            foreach (var blocker in report.BlockerReasons)
            {
                Console.WriteLine($"  - {blocker}");
            }

            // ── 4. Promote when allowed ───────────────────────────────────────────
            if (report.Promote)
            {
                var promoteResult = syncManager.PromoteWave(schema, report.RecommendedWave);
                if (promoteResult.Flag == Errors.Ok)
                {
                    Console.WriteLine($"Promoted: {promoteResult.Message}");
                    return report.RecommendedWave;
                }

                Console.Error.WriteLine($"Promotion failed: {promoteResult.Message}");
            }

            return schema.CurrentWave;
        }
    }
}
