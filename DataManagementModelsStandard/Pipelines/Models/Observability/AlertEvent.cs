using System;

namespace TheTechIdea.Beep.Pipelines.Observability
{
    /// <summary>
    /// A fired instance of an <see cref="AlertRule"/>.
    /// Written to the alert event log by <c>AlertingEngine</c>.
    /// </summary>
    public class AlertEvent
    {
        public string       EventId       { get; set; } = Guid.NewGuid().ToString();
        public string       RuleId        { get; set; } = string.Empty;
        public string       RuleName      { get; set; } = string.Empty;
        public string       PipelineId    { get; set; } = string.Empty;
        public string       PipelineName  { get; set; } = string.Empty;
        /// <summary>The run that triggered this alert (null for liveness alerts).</summary>
        public string?      RunId         { get; set; }
        public AlertSeverity Severity     { get; set; }
        public string       Message       { get; set; } = string.Empty;
        public DateTime     FiredAtUtc    { get; set; } = DateTime.UtcNow;
        public bool         Acknowledged  { get; set; }
        public string?      AcknowledgedBy { get; set; }
        public DateTime?    AcknowledgedAt { get; set; }
    }
}
