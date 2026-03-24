using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Pipelines.Models
{
    /// <summary>
    /// Persisted definition of a scheduled pipeline or workflow trigger.
    /// Stored as JSON under {BeepDataPath}/Schedules/{id}.schedule.json.
    /// </summary>
    public class ScheduleDefinition
    {
        /// <summary>Unique identifier (auto-generated GUID).</summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>Human-readable name for this schedule.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>ID of the pipeline or workflow to run.</summary>
        public string PipelineId { get; set; } = string.Empty;

        /// <summary>When true, <see cref="PipelineId"/> refers to a workflow rather than a pipeline.</summary>
        public bool IsWorkflow { get; set; } = false;

        /// <summary>Plugin ID of the scheduler (e.g. <c>"beep.schedule.cron"</c>).</summary>
        public string SchedulerPluginId { get; set; } = string.Empty;

        /// <summary>Key-value configuration forwarded to the scheduler plugin via <c>Configure()</c>.</summary>
        public Dictionary<string, object> SchedulerConfig { get; set; } = new();

        /// <summary>Whether this schedule is active. Disabled schedules are loaded but not started.</summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>Dispatch priority: 1 (highest) – 10 (lowest). Default 5.</summary>
        public int Priority { get; set; } = 5;

        /// <summary>Maximum simultaneous runs for this pipeline. Default 1.</summary>
        public int MaxConcurrentRuns { get; set; } = 1;

        /// <summary>Per-run timeout in seconds. 0 = no timeout.</summary>
        public int TimeoutSeconds { get; set; } = 0;

        /// <summary>How to retry failed runs.</summary>
        public ScheduleRetryPolicy RetryPolicy { get; set; } = new();

        /// <summary>Rate-limit guard for this schedule.</summary>
        public RateLimitPolicy RateLimitPolicy { get; set; } = new();

        /// <summary>Other schedule IDs that must complete successfully before this one fires.</summary>
        public List<string> DependsOn { get; set; } = new();

        /// <summary>UTC time of the next scheduled run (computed by the scheduler plugin).</summary>
        public DateTime? NextRunAt { get; set; }

        /// <summary>UTC time the schedule last ran.</summary>
        public DateTime? LastRunAt { get; set; }

        /// <summary>Status string of the most recent run (e.g. "Success", "Failed").</summary>
        public string? LastRunStatus { get; set; }

        /// <summary>UTC time this definition was created.</summary>
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>UTC time this definition was last modified.</summary>
        public DateTime ModifiedAtUtc { get; set; } = DateTime.UtcNow;

        // ── Phase 5: Governance and Orchestration Fields ───────────────────

        /// <summary>Owner (team or individual) accountable for this schedule.</summary>
        public string Owner { get; set; } = string.Empty;

        /// <summary>Optional steward responsible for data quality of the pipeline output.</summary>
        public string Steward { get; set; } = string.Empty;

        /// <summary>Freshness SLA in seconds — maximum allowed age of data before an alert fires. 0 = no SLA.</summary>
        public int FreshnessSlaSeconds { get; set; } = 0;

        /// <summary>
        /// Workload class used for queue isolation and concurrency quotas.
        /// E.g. "critical", "standard", "backfill". Default "standard".
        /// </summary>
        public string WorkloadClass { get; set; } = "standard";

        /// <summary>
        /// Run mode: "full" (complete refresh) or "incremental" (CDC/watermark-based).
        /// Default "full".
        /// </summary>
        public string RunMode { get; set; } = "full";

        /// <summary>
        /// Configuration for incremental/CDC runs.
        /// Only used when <see cref="RunMode"/> is "incremental".
        /// </summary>
        public WatermarkConfig Watermark { get; set; } = new();

        /// <summary>
        /// Maximum seconds to wait for upstream dependencies before timing out.
        /// 0 = wait indefinitely.
        /// </summary>
        public int DependencyMaxWaitSeconds { get; set; } = 0;

        /// <summary>
        /// Circuit breaker: consecutive failure count before auto-disabling this schedule.
        /// 0 = circuit breaker disabled.
        /// </summary>
        public int CircuitBreakerThreshold { get; set; } = 0;

        /// <summary>Current consecutive failure count (runtime state, persisted for continuity).</summary>
        public int ConsecutiveFailures { get; set; } = 0;

        /// <summary>When true, this schedule was auto-disabled by the circuit breaker.</summary>
        public bool CircuitBreakerTripped { get; set; } = false;

        /// <summary>Tags for classification and filtering.</summary>
        public List<string> Tags { get; set; } = new();
    }

    /// <summary>
    /// Configuration for watermark-based incremental/CDC processing.
    /// Stores the column or key used to track incremental progress.
    /// </summary>
    public class WatermarkConfig
    {
        /// <summary>Column name in the source entity that holds the watermark value (e.g. "ModifiedDate").</summary>
        public string WatermarkColumn { get; set; } = string.Empty;

        /// <summary>Serialized last-known watermark value. Null = first run (full initial load).</summary>
        public string? LastWatermarkValue { get; set; }

        /// <summary>Data type of the watermark column: "datetime", "long", "string".</summary>
        public string WatermarkType { get; set; } = "datetime";

        /// <summary>
        /// Overlap/lookback seconds subtracted from the watermark to guard against late-arriving data.
        /// Default 0 (no overlap).
        /// </summary>
        public int LookbackSeconds { get; set; } = 0;
    }
}
