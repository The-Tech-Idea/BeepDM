using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Pipelines.Models
{
    // ─── Pipeline tier ────────────────────────────────────────────────────────

    /// <summary>
    /// Business criticality tier controlling which migration wave a pipeline belongs to
    /// and how strictly its SLOs are enforced.
    /// </summary>
    public enum PipelineTier
    {
        /// <summary>Non-critical pipelines (Wave 1). Failures tolerated in canary window.</summary>
        NonCritical,
        /// <summary>Standard production pipelines (Wave 2). Normal SLO enforcement.</summary>
        Standard,
        /// <summary>Business-critical pipelines (Wave 3). Strict SLO, mandatory rollback triggers.</summary>
        BusinessCritical
    }

    // ─── Migration wave ───────────────────────────────────────────────────────

    /// <summary>
    /// Represents a cohort of pipelines that migrate together under unified exit criteria
    /// and rollback triggers.
    /// </summary>
    public class MigrationWave
    {
        /// <summary>Unique identifier for this wave.</summary>
        public string WaveId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>Human-readable label (e.g. "Wave 1 – Non-Critical").</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Criticality tier that all pipelines in this wave must share.</summary>
        public PipelineTier Tier { get; set; } = PipelineTier.NonCritical;

        /// <summary>Pipeline IDs enrolled in this wave.</summary>
        public List<string> PipelineIds { get; set; } = new();

        /// <summary>Current lifecycle status of this wave.</summary>
        public WaveStatus Status { get; set; } = WaveStatus.Pending;

        /// <summary>UTC time the wave was promoted to InProgress.</summary>
        public DateTime? StartedAtUtc { get; set; }

        /// <summary>UTC time the wave reached Completed or RolledBack.</summary>
        public DateTime? FinishedAtUtc { get; set; }

        /// <summary>
        /// Minimum success-rate percentage (0-100) required for wave completion.
        /// Default: 99.0.
        /// </summary>
        public double ExitSuccessRateMin { get; set; } = 99.0;

        /// <summary>
        /// Maximum reject ratio percentage (0-100) allowed during canary window.
        /// Default: 1.0.
        /// </summary>
        public double ExitRejectRatioMax { get; set; } = 1.0;

        /// <summary>
        /// Maximum P95 duration increase percentage compared to baseline before rollback.
        /// Default: 20.0.
        /// </summary>
        public double RollbackDurationDeviationPct { get; set; } = 20.0;

        /// <summary>
        /// If success rate drops below this threshold during canary window, trigger automatic rollback.
        /// Default: 95.0.
        /// </summary>
        public double RollbackSuccessRateThreshold { get; set; } = 95.0;

        /// <summary>
        /// Post-cutover health check durations in hours (e.g. [24, 72, 168]).
        /// KPIs are measured at each interval.
        /// </summary>
        public List<int> HealthCheckHours { get; set; } = new() { 24, 72, 168 };

        /// <summary>Risk/rationale notes added by the migration team.</summary>
        public string? Notes { get; set; }

        /// <summary>User who promoted this wave.</summary>
        public string? PromotedBy { get; set; }

        /// <summary>User who rolled back this wave (if applicable).</summary>
        public string? RolledBackBy { get; set; }
    }

    /// <summary>Lifecycle state of a migration wave.</summary>
    public enum WaveStatus
    {
        /// <summary>Wave defined but migration not yet started.</summary>
        Pending,
        /// <summary>Canary/shadow runs active; monitoring in progress.</summary>
        InProgress,
        /// <summary>All pipelines successfully promoted; all health checks passed.</summary>
        Completed,
        /// <summary>Wave rolled back due to threshold violation.</summary>
        RolledBack,
        /// <summary>Wave failed due to unrecoverable errors.</summary>
        Failed
    }

    // ─── Canary/shadow comparison ─────────────────────────────────────────────

    /// <summary>
    /// Records the outcome of a single canary or shadow comparison run.
    /// </summary>
    public class CanaryRunComparison
    {
        /// <summary>Unique identifier for this comparison record.</summary>
        public string ComparisonId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>Pipeline ID under test.</summary>
        public string PipelineId { get; set; } = string.Empty;

        /// <summary>Type of parallel run.</summary>
        public CanaryRunType RunType { get; set; }

        /// <summary>Run result from the baseline (current) definition.</summary>
        public string BaselineRunId { get; set; } = string.Empty;

        /// <summary>Run result from the candidate (new) definition.</summary>
        public string CandidateRunId { get; set; } = string.Empty;

        public long   BaselineRecordsWritten  { get; set; }
        public long   CandidateRecordsWritten { get; set; }
        public long   BaselineRecordsRejected { get; set; }
        public long   CandidateRecordsRejected { get; set; }
        public double BaselineDurationMs      { get; set; }
        public double CandidateDurationMs     { get; set; }

        /// <summary>Whether the candidate matched baseline within acceptable thresholds.</summary>
        public bool   Passed { get; set; }

        /// <summary>Human-readable explanation of any divergence.</summary>
        public string? DivergenceReason { get; set; }

        public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
    }

    /// <summary>Mode of parallel run comparison.</summary>
    public enum CanaryRunType
    {
        /// <summary>Both pipelines produce output; differences are flagged.</summary>
        Canary,
        /// <summary>Candidate runs in parallel but its output is discarded; only metrics compared.</summary>
        Shadow
    }
}
