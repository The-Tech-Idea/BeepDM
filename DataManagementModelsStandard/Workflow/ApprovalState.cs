using System;

namespace TheTechIdea.Beep.Workflow
{
    /// <summary>
    /// Approval state persisted for Approval-kind steps so that the engine can
    /// resume after a human approves or rejects the workflow.
    /// </summary>
    public class ApprovalState
    {
        public string RunId           { get; set; } = string.Empty;
        public string StepId          { get; set; } = string.Empty;
        public ApprovalDecision Decision { get; set; } = ApprovalDecision.Pending;
        public string ApproverNote    { get; set; } = string.Empty;
        public string ApprovedBy      { get; set; } = string.Empty;
        public DateTime DecidedAtUtc  { get; set; }
    }
}
