using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Workflow
{
    /// <summary>Per-step audit record captured during a workflow run.</summary>
    public class StepExecutionRecord
    {
        public string        StepId          { get; set; } = Guid.NewGuid().ToString();
        public string        StepName        { get; set; } = string.Empty;
        public StepActionKind Kind           { get; set; }
        public bool          Success         { get; set; }
        public string?       ErrorMessage    { get; set; }
        public DateTime      StartedAtUtc    { get; set; }
        public DateTime      FinishedAtUtc   { get; set; }
        public TimeSpan      Duration        => FinishedAtUtc - StartedAtUtc;
        public long          RecordsRead     { get; set; }
        public long          RecordsWritten  { get; set; }
        public long          RecordsRejected { get; set; }
        public int           RetryCount      { get; set; }

        /// <summary>Step-specific output data (pipeline run result, script output, etc.).</summary>
        public Dictionary<string, object> Output  { get; set; } = new();

        /// <summary>Row-level log entries from ETL/data steps.</summary>
        public List<LoadDataLogResult>    DataLogs { get; set; } = new();
    }
}
