using System;
using System.Collections.Generic;
using System.Threading;

namespace TheTechIdea.Beep.Pipelines.Models
{
    /// <summary>
    /// A pending pipeline or workflow run waiting in the <c>PipelineRunQueue</c>.
    /// Created by <c>SchedulerHost</c> when a trigger fires.
    /// </summary>
    public sealed class QueuedRun
    {
        /// <summary>Unique run identifier assigned at creation.</summary>
        public string RunId { get; } = Guid.NewGuid().ToString();

        /// <summary>The <see cref="ScheduleDefinition.Id"/> that originated this run.</summary>
        public string ScheduleId { get; init; } = string.Empty;

        /// <summary>ID of the pipeline or workflow to execute.</summary>
        public string PipelineId { get; init; } = string.Empty;

        /// <summary>When true, <see cref="PipelineId"/> is a workflow ID.</summary>
        public bool IsWorkflow { get; init; }

        /// <summary>Dispatch priority: 1 (highest) – 10 (lowest).</summary>
        public int Priority { get; init; } = 5;

        /// <summary>UTC time the trigger fired.</summary>
        public DateTime TriggeredAtUtc { get; } = DateTime.UtcNow;

        /// <summary>Who or what triggered this run (e.g. "cron", "filewatch", "manual").</summary>
        public string TriggerSource { get; init; } = string.Empty;

        /// <summary>Optional parameter overrides to pass to the pipeline/workflow engine.</summary>
        public IReadOnlyDictionary<string, object>? OverrideParams { get; init; }

        /// <summary>Cancellation source for this individual run.</summary>
        public CancellationTokenSource Cts { get; } = new();
    }
}
