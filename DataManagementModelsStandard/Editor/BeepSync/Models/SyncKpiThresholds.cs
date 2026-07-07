namespace TheTechIdea.Beep.Editor.BeepSync
{
    /// <summary>
    /// Quantitative thresholds that gate promotion to the next <see cref="SyncRolloutWave"/>.
    /// Defaults match the Phase 7 SLO profile values so existing plans can promote
    /// without reconfiguration.
    /// </summary>
    public sealed class SyncKpiThresholds
    {
        public double MaxRejectRate { get; init; } = 0.05;          // 5%
        public double MaxConflictRate { get; init; } = 0.10;        // 10%
        public int    MaxFreshnessLagSeconds { get; init; } = 300;  // 5 min
        public string MinSloComplianceTier { get; init; } = "Standard"; // "Gold" | "Standard" | "Bronze"
    }
}
