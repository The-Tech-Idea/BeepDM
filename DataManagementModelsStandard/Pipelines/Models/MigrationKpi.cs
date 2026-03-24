using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Pipelines.Models
{
    /// <summary>
    /// KPI snapshot for a migration wave or individual pipeline tier,
    /// computed over a governance review window (typically one week).
    /// </summary>
    public class MigrationKpiSnapshot
    {
        /// <summary>Wave or group label this snapshot covers.</summary>
        public string WaveId { get; set; } = string.Empty;

        /// <summary>Pipeline tier the KPIs were computed for.</summary>
        public PipelineTier Tier { get; set; }

        /// <summary>Start of the measurement window (UTC).</summary>
        public DateTime WindowStart { get; set; }

        /// <summary>End of the measurement window (UTC).</summary>
        public DateTime WindowEnd { get; set; }

        /// <summary>Total pipeline runs executed in the window.</summary>
        public int TotalRuns { get; set; }

        // ── Reliability KPIs ──────────────────────────────────────────────

        /// <summary>Percentage of runs that succeeded (0-100).</summary>
        public double SuccessRatePct { get; set; }

        /// <summary>
        /// Retry inflation ratio = actual retries / total runs.
        /// Values above 0.1 indicate instability.
        /// </summary>
        public double RetryInflationRatio { get; set; }

        /// <summary>Number of runs that timed out.</summary>
        public int TimeoutCount { get; set; }

        // ── Quality KPIs ──────────────────────────────────────────────────

        /// <summary>Percentage of records rejected across all runs (0-100).</summary>
        public double DqRejectRatioPct { get; set; }

        /// <summary>Percentage of runs with lineage tracking enabled (0-100).</summary>
        public double LineageCoveragePct { get; set; }

        /// <summary>Percentage of runs that met their SLA freshness window (0-100).</summary>
        public double SlaFreshnessPct { get; set; }

        // ── Business KPIs ─────────────────────────────────────────────────

        /// <summary>Number of incidents triggered by pipelines in this wave during the window.</summary>
        public int IncidentCount { get; set; }

        /// <summary>
        /// Average time (hours) from pipeline change commit to successful production run.
        /// Lower is better (change lead time).
        /// </summary>
        public double AvgChangeLeadTimeHours { get; set; }

        // ── Performance KPIs ─────────────────────────────────────────────

        /// <summary>Average P95 duration across all pipelines in the wave.</summary>
        public TimeSpan AvgP95Duration { get; set; }

        /// <summary>Average cost units consumed per run.</summary>
        public double AvgCostPerRun { get; set; }

        // ── Wave health gate evaluation ───────────────────────────────────

        /// <summary>Whether this snapshot met all exit criteria for the wave.</summary>
        public bool MeetsExitCriteria { get; set; }

        /// <summary>Whether any KPI crossed a rollback trigger threshold.</summary>
        public bool TriggersRollback { get; set; }

        /// <summary>Human-readable explanation of gate violations, if any.</summary>
        public List<string> Violations { get; set; } = new();

        /// <summary>UTC time this snapshot was computed.</summary>
        public DateTime ComputedAtUtc { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Weekly governance board report aggregating all wave KPIs
    /// and overall migration health.
    /// </summary>
    public class KpiGovernanceReport
    {
        /// <summary>Unique report identifier.</summary>
        public string ReportId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>Governance cadence window start (UTC).</summary>
        public DateTime WindowStart { get; set; }

        /// <summary>Governance cadence window end (UTC).</summary>
        public DateTime WindowEnd { get; set; }

        /// <summary>KPI snapshots per wave.</summary>
        public List<MigrationKpiSnapshot> WaveSnapshots { get; set; } = new();

        /// <summary>Overall migration completion percentage (0-100).</summary>
        public double OverallCompletionPct { get; set; }

        /// <summary>Number of waves with active rollback triggers.</summary>
        public int WavesWithRollbackTrigger { get; set; }

        /// <summary>Recommendation provided by the governance engine.</summary>
        public string? Recommendation { get; set; }

        /// <summary>UTC time this report was generated.</summary>
        public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
