using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.BeepSync
{
    /// <summary>
    /// Service Level Objective profile that defines thresholds governing when a sync run is
    /// considered compliant.  Stored on <see cref="DataSyncSchema.SloProfile"/>.
    /// </summary>
    public class SloProfile
    {
        /// <summary>
        /// Display name that identifies the SLO tier, e.g. <c>Critical</c>, <c>Standard</c>,
        /// or <c>NonCritical</c>.
        /// </summary>
        public string ProfileName { get; set; }

        /// <summary>
        /// Minimum acceptable success rate (0.0–1.0).  E.g. <c>0.99</c> means 99 %.
        /// Default: <c>0.95</c>.
        /// </summary>
        public double MinSuccessRate { get; set; } = 0.95;

        /// <summary>
        /// Maximum allowed run duration in milliseconds.  Default: <c>300_000</c> (5 minutes).
        /// </summary>
        public long MaxDurationMs { get; set; } = 300_000;

        /// <summary>
        /// Maximum allowed data-freshness lag in seconds.  Default: <c>3600</c> (1 hour).
        /// </summary>
        public double MaxFreshnessLagSeconds { get; set; } = 3600;

        /// <summary>
        /// Maximum acceptable conflict rate (0.0–1.0).  Default: <c>0.05</c> (5 %).
        /// </summary>
        public double MaxConflictRate { get; set; } = 0.05;

        /// <summary>
        /// Maximum acceptable reject rate (0.0–1.0).  Default: <c>0.05</c> (5 %).
        /// </summary>
        public double MaxRejectRate { get; set; } = 0.05;

        /// <summary>
        /// Ordered list of alert rule keys evaluated against <see cref="SyncMetrics"/> at the
        /// end of each run.  E.g. <c>sync.alert.low-success-rate</c>.
        /// </summary>
        public List<string> AlertRuleKeys { get; set; } = new List<string>();
    }
}
