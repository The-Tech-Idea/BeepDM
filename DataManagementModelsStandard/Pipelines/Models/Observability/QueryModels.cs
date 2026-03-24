using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Pipelines.Models;

namespace TheTechIdea.Beep.Pipelines.Observability
{
    /// <summary>Filter parameters for querying stored run logs.</summary>
    public class RunLogQuery
    {
        public string?    PipelineId { get; set; }
        public RunStatus? Status     { get; set; }
        public DateTime?  From       { get; set; }
        public DateTime?  To         { get; set; }
        public int        Limit      { get; set; } = 100;
        public int        Offset     { get; set; } = 0;
        public string     OrderBy    { get; set; } = "StartedAtUtc DESC";
    }

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Filter parameters for querying stored alert events.</summary>
    public class AlertEventQuery
    {
        public string?       PipelineId    { get; set; }
        public string?       RuleId        { get; set; }
        public AlertSeverity? Severity     { get; set; }
        public bool?         Acknowledged  { get; set; }
        public DateTime?     From          { get; set; }
        public DateTime?     To            { get; set; }
        public int           Limit         { get; set; } = 100;
        public int           Offset        { get; set; } = 0;
    }

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Filter parameters for querying the audit trail.</summary>
    public class AuditQuery
    {
        public string?   EntityType  { get; set; }
        public string?   EntityId    { get; set; }
        public string?   Action      { get; set; }
        public string?   PerformedBy { get; set; }
        public DateTime? From        { get; set; }
        public DateTime? To          { get; set; }
        public int       Limit       { get; set; } = 200;
        public int       Offset      { get; set; } = 0;
    }

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Snapshot of a currently executing run for live dashboard display.</summary>
    public class LiveRunStatus
    {
        public string   RunId         { get; set; } = string.Empty;
        public string   PipelineId    { get; set; } = string.Empty;
        public string   PipelineName  { get; set; } = string.Empty;
        public DateTime StartedAtUtc  { get; set; }
        public string   CurrentStep   { get; set; } = string.Empty;
        public long     RecordsRead   { get; set; }
        public long     RecordsWritten { get; set; }
        public long     RecordsRejected { get; set; }
        /// <summary>Current estimated memory usage in bytes (0 if not tracked).</summary>
        public long     MemoryUsageBytes { get; set; }
        /// <summary>Workload class of this run.</summary>
        public string   WorkloadClass { get; set; } = string.Empty;
    }

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>High-level summary for the main dashboard view.</summary>
    public class DashboardSummary
    {
        public int    TotalPipelines      { get; set; }
        public int    ActiveRuns          { get; set; }
        public int    RunsToday           { get; set; }
        public int    FailuresToday       { get; set; }
        public long   RowsProcessedToday  { get; set; }
        public double AvgSuccessRate      { get; set; }

        // ── Cost & resource ──────────────────────────────────────────────
        /// <summary>Total cost units consumed today.</summary>
        public double CostToday           { get; set; }
        /// <summary>Average peak memory (bytes) across today's runs.</summary>
        public long   AvgMemoryPeakToday  { get; set; }

        public List<AlertEvent>      RecentAlerts { get; set; } = new();
        public List<PipelineRunLog>  RecentRuns   { get; set; } = new();
    }
}
