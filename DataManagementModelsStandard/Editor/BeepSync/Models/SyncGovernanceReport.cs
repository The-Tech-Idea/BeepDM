using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.BeepSync
{
    /// <summary>
    /// Output of <c>BeepSyncManager.EvaluateRolloutGovernance</c>: the recommended
    /// wave, the snapshot of measured KPIs, and a list of human-readable blocker
    /// reasons when promotion is denied.
    /// </summary>
    public sealed class SyncGovernanceReport
    {
        public string PlanId { get; init; } = string.Empty;
        public SyncRolloutWave CurrentWave { get; init; }
        public SyncRolloutWave RecommendedWave { get; init; }
        public bool Promote { get; init; }
        public IReadOnlyList<string> BlockerReasons { get; init; } = new List<string>();
        public SyncMetrics MeasuredKpis { get; init; } = new();
        public SyncKpiThresholds Thresholds { get; init; } = new();
    }
}
