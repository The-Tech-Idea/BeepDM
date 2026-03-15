using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Addin;

namespace TheTechIdea.Beep.Workflow
{
    /// <summary>
    /// Carries all runtime state for one workflow execution, shared across all
    /// steps in the run.
    /// </summary>
    public class WorkFlowRunContext
    {
        public string RunId              { get; }            = Guid.NewGuid().ToString();
        public string WorkFlowId         { get; init; }      = string.Empty;
        public string WorkFlowName       { get; init; }      = string.Empty;
        public DateTime StartedAtUtc     { get; }            = DateTime.UtcNow;
        public IDMEEditor DMEEditor      { get; init; }      = null!;
        public IProgress<PassedArgs>? Progress { get; init; }
        public System.Threading.CancellationToken Token { get; init; }

        /// <summary>Resolved parameters available to all steps.</summary>
        public IReadOnlyDictionary<string, object> Parameters { get; init; }
            = new Dictionary<string, object>();

        /// <summary>Shared state bag — steps communicate results here.</summary>
        public Dictionary<string, object> State { get; } = new();

        /// <summary>Completed step results, keyed by step ID.</summary>
        public Dictionary<string, StepExecutionRecord> StepResults { get; } = new();

        // ── Telemetry ─────────────────────────────────────────────────────────
        public int  StepsTotal            { get; set; }
        public int  StepsCompleted        { get; set; }
        public int  StepsFailed           { get; set; }
        public int  StepsSkipped          { get; set; }
        public long TotalRecordsProcessed { get; set; }

        /// <summary>ID of the step currently executing.</summary>
        public string? CurrentStepId      { get; set; }

        public void ReportProgress(string message, int pct = -1) =>
            Progress?.Report(new PassedArgs { Messege = message, ParameterInt1 = pct });
    }
}
