namespace TheTechIdea.Beep.Workflow
{
    /// <summary>How a workflow run may be initiated.</summary>
    public enum TriggerKind { Manual, Scheduled, EventDriven, WebHook, PipelineCompletion }

    /// <summary>Describes when and how a workflow is triggered.</summary>
    public class WorkFlowTrigger
    {
        public TriggerKind Kind        { get; set; } = TriggerKind.Manual;
        /// <summary>Cron expression (used when Kind == Scheduled).</summary>
        public string? CronExpression  { get; set; }
        /// <summary>Event topic name (used when Kind == EventDriven).</summary>
        public string? EventTopic      { get; set; }
        /// <summary>Pipeline Id whose completion fires this workflow (used when Kind == PipelineCompletion).</summary>
        public string? PipelineId      { get; set; }
    }
}
