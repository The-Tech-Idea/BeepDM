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
    }
}
