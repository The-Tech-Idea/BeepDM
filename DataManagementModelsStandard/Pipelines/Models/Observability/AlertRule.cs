using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Pipelines.Observability
{
    /// <summary>
    /// Defines when and how an alert should fire against pipeline run logs.
    /// Evaluated by <c>AlertingEngine</c> after every run completion.
    /// </summary>
    public class AlertRule
    {
        public string Id          { get; set; } = Guid.NewGuid().ToString();
        public string Name        { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool   IsEnabled   { get; set; } = true;

        /// <summary>
        /// Which pipelines this rule applies to.
        /// <c>null</c> means all pipelines.
        /// </summary>
        public List<string>? PipelineIds { get; set; }

        public AlertTrigger  Trigger  { get; set; } = AlertTrigger.OnFailure;

        /// <summary>
        /// Optional expression evaluated on a <see cref="PipelineRunLog"/>.
        /// Examples:
        /// <c>"RecordsRejected > 1000"</c>, <c>"DQPassRate &lt; 0.95"</c>
        /// Used when <see cref="Trigger"/> is <see cref="AlertTrigger.OnCustomExpression"/> or any threshold trigger.
        /// </summary>
        public string? Condition { get; set; }

        public AlertSeverity Severity { get; set; } = AlertSeverity.Warning;

        /// <summary>Plugin IDs of notifiers to fire (email, webhook, logfile, etc.).</summary>
        public List<string> NotifierPluginIds { get; set; } = new();

        /// <summary>Merged config passed to each notifier (overrides per-plugin defaults).</summary>
        public Dictionary<string, object> NotifierConfig { get; set; } = new();

        /// <summary>Do not re-fire for the same pipeline within this window (minutes). Default 60.</summary>
        public int SilenceWindowMinutes { get; set; } = 60;
    }

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Defines what condition causes an alert rule to trigger.</summary>
    public enum AlertTrigger
    {
        /// <summary>Pipeline run ends with Status = Failed.</summary>
        OnFailure,
        /// <summary>Pipeline run ends with Status = Success.</summary>
        OnSuccess,
        /// <summary>Pipeline run completes with any terminal status.</summary>
        OnCompletion,
        /// <summary>DQ pass rate drops below a configured threshold.</summary>
        OnDQThreshold,
        /// <summary>Rejected row count exceeds a configured threshold.</summary>
        OnRejectedThreshold,
        /// <summary>Run duration exceeds a configured threshold.</summary>
        OnDurationThreshold,
        /// <summary>An expected run has not occurred within N hours.</summary>
        OnNoRunWithin,
        /// <summary>User-defined expression on the run log evaluates to true.</summary>
        OnCustomExpression,
        /// <summary>Peak memory usage exceeds a configured threshold (bytes).</summary>
        OnMemoryThreshold,
        /// <summary>Estimated cost exceeds a configured threshold (cost units).</summary>
        OnCostThreshold,
        /// <summary>A caller failed pre-run authorization checks.</summary>
        OnUnauthorizedAccess,
        /// <summary>A security policy violation was detected (e.g. missing owner, restricted w/o masking).</summary>
        OnPolicyViolation
    }

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Severity classification for alert events.</summary>
    public enum AlertSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }
}
