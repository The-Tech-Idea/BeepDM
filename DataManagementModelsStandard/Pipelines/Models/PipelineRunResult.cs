using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Pipelines.Models
{
    /// <summary>
    /// Summary of a completed (or failed) pipeline run.
    /// Returned by the engine, persisted to the run history store.
    /// </summary>
    public class PipelineRunResult
    {
        /// <summary>Unique identifier for this run instance.</summary>
        public string RunId       { get; init; } = string.Empty;

        /// <summary>ID of the pipeline definition that was executed.</summary>
        public string PipelineId  { get; init; } = string.Empty;

        /// <summary>Display name of the pipeline.</summary>
        public string PipelineName { get; init; } = string.Empty;

        /// <summary>Terminal status of this run.</summary>
        public RunStatus Status   { get; set; } = RunStatus.Running;

        /// <summary>UTC time the run started.</summary>
        public DateTime StartedAtUtc  { get; init; } = DateTime.UtcNow;

        /// <summary>UTC time the run finished (or was cancelled / failed).</summary>
        public DateTime? FinishedAtUtc { get; set; }

        /// <summary>Wall-clock duration of the run, or null if still executing.</summary>
        public TimeSpan? Duration => FinishedAtUtc.HasValue
            ? FinishedAtUtc.Value - StartedAtUtc
            : null;

        /// <summary>Top-level error message set when <see cref="Status"/> is <see cref="RunStatus.Failed"/>.</summary>
        public string? ErrorMessage { get; set; }

        // ── Aggregate counters ─────────────────────────────────────────────
        public long RecordsRead     { get; set; }
        public long RecordsWritten  { get; set; }
        public long RecordsRejected { get; set; }
        public long RecordsWarned   { get; set; }
        public long BytesProcessed  { get; set; }

        // ── Per-step results ───────────────────────────────────────────────
        public List<PipelineStepResult> StepResults { get; set; } = new();
    }

    /// <summary>
    /// Summary for a single step within a pipeline run.
    /// </summary>
    public class PipelineStepResult
    {
        public string    StepId    { get; init; } = string.Empty;
        public string    StepName  { get; init; } = string.Empty;
        public StepKind  Kind      { get; init; }
        public RunStatus Status    { get; set; }
        public DateTime  StartedAt { get; init; } = DateTime.UtcNow;
        public DateTime? FinishedAt { get; set; }
        public long   RecordsIn   { get; set; }
        public long   RecordsOut  { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
