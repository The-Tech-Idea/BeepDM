using System.Collections.Generic;

namespace TheTechIdea.Beep.Workflow
{
    /// <summary>
    /// Extended step definition that adds execution metadata on top of the
    /// existing <see cref="WorkFlowStep"/> model.
    /// </summary>
    public class WorkFlowStepDef : WorkFlowStep
    {
        // ── Dispatch ──────────────────────────────────────────────────────────
        public StepActionKind Kind             { get; set; } = StepActionKind.ETLPipeline;

        // ── ETL step ─────────────────────────────────────────────────────────
        /// <summary>Reference to a <c>PipelineDefinition.Id</c> (Kind == ETLPipeline).</summary>
        public string? PipelineId              { get; set; }
        public Dictionary<string, object> PipelineParams { get; set; } = new();

        // ── Script step ───────────────────────────────────────────────────────
        public string? ScriptBody              { get; set; }
        public string  ScriptLanguage          { get; set; } = "csharp";

        // ── Notification step ─────────────────────────────────────────────────
        public string? NotifierPluginId        { get; set; }
        public string? NotificationTemplate    { get; set; }

        // ── Approval step ─────────────────────────────────────────────────────
        public List<string> Approvers          { get; set; } = new();
        public int ApprovalTimeoutHours        { get; set; } = 24;

        // ── Wait step ─────────────────────────────────────────────────────────
        public int WaitSeconds                 { get; set; } = 0;

        // ── Sub-workflow step ─────────────────────────────────────────────────
        public string? SubWorkflowId           { get; set; }

        // ── Execution policy ──────────────────────────────────────────────────
        public WorkFlowRetryPolicy RetryPolicy { get; set; } = WorkFlowRetryPolicy.None;
        public int TimeoutSeconds              { get; set; } = 0;   // 0 = no timeout
        public OnFailureBehavior OnFailure     { get; set; } = OnFailureBehavior.Fail;
        /// <summary>Target step ID when <see cref="OnFailure"/> is <see cref="OnFailureBehavior.Route"/>.</summary>
        public string? OnFailureRouteToStepId  { get; set; }

        // ── Visual position (Phase 7 designer) ───────────────────────────────
        public float CanvasX { get; set; }
        public float CanvasY { get; set; }
    }
}
