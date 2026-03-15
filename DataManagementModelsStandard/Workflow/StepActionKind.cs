namespace TheTechIdea.Beep.Workflow
{
    /// <summary>Identifies the type of action a workflow step performs.</summary>
    public enum StepActionKind
    {
        ETLPipeline,
        Script,
        Notification,
        Approval,
        Wait,
        SubWorkflow,
        DataQuality,
        SchemaSync,
        Merge,
        Split
    }

    /// <summary>Controls what the engine does when a step fails.</summary>
    public enum OnFailureBehavior
    {
        /// <summary>Abort the entire workflow run.</summary>
        Fail,
        /// <summary>Log the failure and continue to the next step.</summary>
        Skip,
        /// <summary>Route execution to <see cref="WorkFlowStepDef.OnFailureRouteToStepId"/>.</summary>
        Route,
        /// <summary>Retry using the step's <see cref="WorkFlowStepDef.RetryPolicy"/>.</summary>
        Retry
    }

    /// <summary>Approval decision for an Approval step.</summary>
    public enum ApprovalDecision { Pending, Approved, Rejected }
}
