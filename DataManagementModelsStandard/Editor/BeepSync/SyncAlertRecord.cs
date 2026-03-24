using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.BeepSync
{
    /// <summary>
    /// An alert artifact emitted by <see cref="BeepSyncManager"/> when an SLO alert rule fires
    /// during or after a sync run.  Each record maps to exactly one fired alert rule.
    /// </summary>
    public class SyncAlertRecord
    {
        /// <summary>Unique identifier for this alert instance.</summary>
        public string AlertId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>Schema identifier this alert belongs to.</summary>
        public string SchemaId { get; set; }

        /// <summary>Run identifier during which the alert was emitted.</summary>
        public string RunId { get; set; }

        /// <summary>
        /// Correlation identifier for tracing — includes mapping plan version when available.
        /// Format: <c>{SchemaId}.{RunId}.{MappingPlanVersion}</c>.
        /// </summary>
        public string CorrelationId { get; set; }

        /// <summary>The alert rule key that triggered this record, e.g. <c>sync.alert.low-success-rate</c>.</summary>
        public string RuleKey { get; set; }

        /// <summary>Severity tier: <c>Critical</c>, <c>Warning</c>, or <c>Info</c>.</summary>
        public string Severity { get; set; }

        /// <summary>Human-readable reason describing why the alert fired.</summary>
        public string Reason { get; set; }

        /// <summary>Suggested action operators should take to resolve the alert.</summary>
        public string RemediationHint { get; set; }

        /// <summary>UTC timestamp when the alert was emitted (stamped by DefaultsManager when available).</summary>
        public DateTime EmittedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Name of the user/process that emitted the alert.</summary>
        public string EmittedBy { get; set; } = Environment.UserName;

        /// <summary>Lifecycle status: <c>Open</c>, <c>Acknowledged</c>, or <c>Resolved</c>.</summary>
        public string Status { get; set; } = "Open";

        /// <summary>
        /// Additional diagnostic context values (e.g., measured metric vs SLO threshold,
        /// mapping drift notice, etc.).
        /// </summary>
        public Dictionary<string, object> AdditionalContext { get; set; } = new Dictionary<string, object>();
    }
}
