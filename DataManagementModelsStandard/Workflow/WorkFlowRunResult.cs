using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Workflow
{
    /// <summary>Structured result returned after a complete workflow run.</summary>
    public class WorkFlowRunResult
    {
        public string  RunId          { get; set; } = Guid.NewGuid().ToString();
        public string  WorkFlowId     { get; set; } = string.Empty;
        public string  WorkFlowName   { get; set; } = string.Empty;
        public bool    Success        { get; set; }
        public string? ErrorMessage   { get; set; }

        public DateTime StartedAtUtc  { get; set; }
        public DateTime FinishedAtUtc { get; set; }
        public TimeSpan Duration      => FinishedAtUtc - StartedAtUtc;

        // ── Aggregates ────────────────────────────────────────────────────────
        public int  StepsTotal            { get; set; }
        public int  StepsCompleted        { get; set; }
        public int  StepsFailed           { get; set; }
        public int  StepsSkipped          { get; set; }
        public long TotalRecordsProcessed { get; set; }

        /// <summary>Per-step audit records in execution order.</summary>
        public List<StepExecutionRecord> StepRecords { get; set; } = new();
    }
}
