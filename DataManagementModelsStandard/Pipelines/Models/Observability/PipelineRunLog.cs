using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Pipelines.Models;

namespace TheTechIdea.Beep.Pipelines.Observability
{
    /// <summary>Complete audit record of a single pipeline execution.</summary>
    public class PipelineRunLog
    {
        public string  RunId           { get; set; } = Guid.NewGuid().ToString();
        public string  PipelineId      { get; set; } = string.Empty;
        public string  PipelineName    { get; set; } = string.Empty;
        public string  PipelineVersion { get; set; } = string.Empty;
        /// <summary>"cron" | "filewatch" | "manual" | "dependency" | "api"</summary>
        public string  TriggerSource   { get; set; } = string.Empty;
        public string  TriggerDetail   { get; set; } = string.Empty;
        /// <summary>User or service identity that triggered the run.</summary>
        public string? TriggeredBy     { get; set; }
        public DateTime StartedAtUtc   { get; set; }
        public DateTime FinishedAtUtc  { get; set; }
        public TimeSpan Duration       => FinishedAtUtc - StartedAtUtc;
        public RunStatus Status        { get; set; }
        public string?  ErrorMessage   { get; set; }
        /// <summary>0 = first attempt; &gt;0 = retry number.</summary>
        public int RetryNumber         { get; set; }
        public string? ResumedFromRunId { get; set; }

        // ── Volumes ──────────────────────────────────────────────────────────
        public long RecordsRead      { get; set; }
        public long RecordsWritten   { get; set; }
        public long RecordsRejected  { get; set; }
        public long RecordsWarned    { get; set; }
        public long BytesProcessed   { get; set; }

        // ── Performance & cost ─────────────────────────────────────────────
        /// <summary>Peak memory usage in bytes during this run (0 if not tracked).</summary>
        public long MemoryPeakBytes { get; set; }

        /// <summary>Workload class assigned to this run.</summary>
        public string WorkloadClass { get; set; } = "standard";

        /// <summary>Estimated cost in abstract cost units (computed post-run).</summary>
        public double EstimatedCostUnits { get; set; }

        // ── Per-step detail ───────────────────────────────────────────────────
        public List<StepRunLog> StepLogs { get; set; } = new();

        // ── DQ summary ───────────────────────────────────────────────────────
        public double       DQPassRate    { get; set; }
        public List<string> TopDQFailures { get; set; } = new();

        // ── Tags ──────────────────────────────────────────────────────────────
        public Dictionary<string, string> Tags { get; set; } = new();
    }

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Per-step execution record nested inside a <see cref="PipelineRunLog"/>.</summary>
    public class StepRunLog
    {
        public string    StepId          { get; set; } = string.Empty;
        public string    StepName        { get; set; } = string.Empty;
        public StepKind  Kind            { get; set; }
        public string    PluginId        { get; set; } = string.Empty;
        public RunStatus Status          { get; set; }
        public DateTime  StartedAtUtc    { get; set; }
        public DateTime  FinishedAtUtc   { get; set; }
        public TimeSpan  Duration        => FinishedAtUtc - StartedAtUtc;
        public string?   ErrorMessage    { get; set; }
        public int       RetryCount      { get; set; }
        public long      RecordsIn       { get; set; }
        public long      RecordsOut      { get; set; }
        public long      RecordsRejected { get; set; }

        /// <summary>Row-level rejection / warning logs (capped via MaxRowLogs).</summary>
        public List<RowRunLog> RowLogs { get; set; } = new();

        /// <summary>Plugin-emitted telemetry (latency, bytes, custom KPIs).</summary>
        public Dictionary<string, object> Metrics { get; set; } = new();
    }

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Per-row rejection or warning entry captured during a step.</summary>
    public class RowRunLog
    {
        public string   RowId        { get; set; } = Guid.NewGuid().ToString();
        public string   RunId        { get; set; } = string.Empty;
        public string   StepId       { get; set; } = string.Empty;
        public long     RowNumber    { get; set; }
        /// <summary>"Rejected" | "Warning" | "Error"</summary>
        public string   Outcome      { get; set; } = string.Empty;
        public string   RuleName     { get; set; } = string.Empty;
        public string   Message      { get; set; } = string.Empty;
        public DateTime Timestamp    { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Field values of the offending row (up to 50 fields).
        /// Sensitive columns are masked by <c>MaskingConfig</c> before writing.
        /// </summary>
        public Dictionary<string, string?> FieldSnapshot { get; set; } = new();
    }
}
