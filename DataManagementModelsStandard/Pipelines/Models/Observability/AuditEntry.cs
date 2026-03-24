using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Pipelines.Observability
{
    /// <summary>
    /// Immutable audit record of a state-changing operation.
    /// Written append-only by <c>PipelineManager</c>, <c>SchedulerHost</c>, and <c>AlertingEngine</c>.
    /// </summary>
    public class AuditEntry
    {
        public string  Id          { get; set; } = Guid.NewGuid().ToString();
        /// <summary>e.g. "PipelineCreated", "PipelineDeleted", "RunTriggered", "ConfigChanged", "AlertAcknowledged"</summary>
        public string  Action      { get; set; } = string.Empty;
        /// <summary>"Pipeline" | "Schedule" | "AlertRule" | "Run"</summary>
        public string  EntityType  { get; set; } = string.Empty;
        public string  EntityId    { get; set; } = string.Empty;
        public string  EntityName  { get; set; } = string.Empty;
        public string? PerformedBy { get; set; }
        public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
        /// <summary>JSON snapshot of the entity before the change, or null for creates.</summary>
        public string? PreviousValue { get; set; }
        /// <summary>JSON snapshot of the entity after the change, or null for deletes.</summary>
        public string? NewValue      { get; set; }
        public string? IpAddress     { get; set; }
        /// <summary>SHA-256 hash of the previous audit entry, forming a tamper-evident chain.</summary>
        public string? PreviousHash  { get; set; }
        /// <summary>SHA-256 hash of this entry's content (excluding PreviousHash itself).</summary>
        public string? EntryHash     { get; set; }
        public Dictionary<string, string> Tags { get; set; } = new();
    }
}
