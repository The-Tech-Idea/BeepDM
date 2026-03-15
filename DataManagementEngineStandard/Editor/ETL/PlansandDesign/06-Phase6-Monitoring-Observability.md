# Phase 6 — Monitoring, Observability & Alerting

**Version:** 1.0  
**Date:** 2026-03-13  
**Status:** Design  
**Depends on:** Phase 2 (Engine), Phase 3 (Workflow), Phase 5 (Scheduling)

---

## 1. Objective

Make every aspect of pipeline and workflow execution visible, queryable, and alertable — at the level of enterprise tools like Azure Data Factory Monitor or Apache NiFi's provenance feed. This phase delivers:

1. **Structured Run Logs** — every run, step, and rejected row logged with rich context
2. **Data Lineage** — queryable column-level lineage across all pipeline runs
3. **Metrics & KPIs** — throughput, latency, DQ pass rates, error rates per pipeline
4. **Alerting** — configurable alert rules with notifier plugins (email, webhook, Teams)
5. **Audit Trail** — immutable record of who triggered what, when, and what changed
6. **Dashboard Data API** — feed for UI dashboards (Phase 7) and external tools

---

## 2. Component Map

```
TheTechIdea.Beep.Pipelines.Observability/
├── PipelineRunLog.cs                  ← per-run summary record
├── StepRunLog.cs                      ← per-step detail
├── RowRunLog.cs                       ← per-row rejection/warning log
├── DataLineageRecord.cs               ← column-to-column lineage (from Phase 4)
├── PipelineMetrics.cs                 ← aggregated KPI model
├── AlertRule.cs                       ← when/then triggering rule
├── AlertEvent.cs                      ← fired when alert triggers
├── AuditEntry.cs                      ← who/what/when for changes
├── ObservabilityStore.cs              ← persistence layer
├── MetricsEngine.cs                   ← computes KPIs from run logs
├── AlertingEngine.cs                  ← evaluates alert rules, fires notifiers
└── Notifiers/
    ├── EmailNotifier.cs
    ├── WebhookNotifier.cs
    └── LogFileNotifier.cs
```

---

## 3. Run Log Models

### 3.1 PipelineRunLog

```csharp
namespace TheTechIdea.Beep.Pipelines.Observability
{
    /// <summary>
    /// Complete audit record of a single pipeline execution.
    /// Written at end of run (success or failure).
    /// </summary>
    public class PipelineRunLog
    {
        public string   RunId             { get; set; } = Guid.NewGuid().ToString();
        public string   PipelineId        { get; set; } = string.Empty;
        public string   PipelineName      { get; set; } = string.Empty;
        public string   PipelineVersion   { get; set; } = string.Empty;
        public string   TriggerSource     { get; set; } = string.Empty;  // "cron"|"file"|"manual"|"api"
        public string   TriggerDetail     { get; set; } = string.Empty;
        public string?  TriggeredBy       { get; set; }                  // user/service identity
        public DateTime StartedAtUtc      { get; set; }
        public DateTime FinishedAtUtc     { get; set; }
        public TimeSpan Duration          => FinishedAtUtc - StartedAtUtc;
        public RunStatus Status           { get; set; }                  // Success|Failed|Cancelled|Partial
        public string?  ErrorMessage      { get; set; }
        public int      RetryNumber       { get; set; }                  // 0 = first attempt
        public string?  ResumedFromRunId  { get; set; }                  // if checkpoint resume

        // ── Volumes ──────────────────────────────────────────────────────
        public long RecordsRead         { get; set; }
        public long RecordsWritten      { get; set; }
        public long RecordsRejected     { get; set; }
        public long RecordsWarned       { get; set; }
        public long BytesProcessed      { get; set; }

        // ── Per-step detail ───────────────────────────────────────────────
        public List<StepRunLog> StepLogs { get; set; } = new();

        // ── DQ summary ───────────────────────────────────────────────────
        public double DQPassRate        { get; set; }
        public List<string> TopDQFailures { get; set; } = new();

        // ── Tags ──────────────────────────────────────────────────────────
        public Dictionary<string, string> Tags { get; set; } = new();
    }

    public enum RunStatus { Running, Success, Failed, Cancelled, Partial, Skipped }
}
```

### 3.2 StepRunLog

```csharp
public class StepRunLog
{
    public string     StepId           { get; set; } = string.Empty;
    public string     StepName         { get; set; } = string.Empty;
    public StepKind   Kind             { get; set; }
    public string     PluginId         { get; set; } = string.Empty;
    public RunStatus  Status           { get; set; }
    public DateTime   StartedAtUtc     { get; set; }
    public DateTime   FinishedAtUtc    { get; set; }
    public TimeSpan   Duration         => FinishedAtUtc - StartedAtUtc;
    public string?    ErrorMessage     { get; set; }
    public int        RetryCount       { get; set; }
    public long       RecordsIn        { get; set; }
    public long       RecordsOut       { get; set; }
    public long       RecordsRejected  { get; set; }

    /// <summary>Row-level rejection logs (capped at MaxRowLogs per step).</summary>
    public List<RowRunLog> RowLogs     { get; set; } = new();

    /// <summary>Step-specific telemetry (plugin-defined).</summary>
    public Dictionary<string, object> Metrics { get; set; } = new();
}
```

### 3.3 RowRunLog

```csharp
public class RowRunLog
{
    public string RowId          { get; set; } = Guid.NewGuid().ToString();
    public string RunId          { get; set; } = string.Empty;
    public string StepId         { get; set; } = string.Empty;
    public long   RowNumber      { get; set; }
    public string Outcome        { get; set; } = string.Empty;  // "Rejected" | "Warning" | "Error"
    public string RuleName       { get; set; } = string.Empty;
    public string Message        { get; set; } = string.Empty;
    public DateTime Timestamp    { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Serialized field values of the offending row (up to 50 fields, truncated).
    /// Sensitive columns masked automatically based on masking config.
    /// </summary>
    public Dictionary<string, string?> FieldSnapshot { get; set; } = new();
}
```

---

## 4. Data Lineage Store

Column-level lineage is accumulated per run in `PipelineRunContext.LineageEntries` and persisted after completion.

### Lineage Query API
```csharp
public interface ILineageStore
{
    /// <summary>
    /// Get all lineage records for a specific run.
    /// </summary>
    Task<IReadOnlyList<DataLineageRecord>> GetByRunAsync(string runId);

    /// <summary>
    /// Trace where a destination column's data came from (backward lineage).
    /// Returns chain: destField ← transformer ← sourceField ← ...
    /// </summary>
    Task<IReadOnlyList<DataLineageRecord>> TraceBackwardAsync(
        string destDataSource, string destEntity, string destField);

    /// <summary>
    /// Trace all downstream fields that received data from a source column (forward).
    /// </summary>
    Task<IReadOnlyList<DataLineageRecord>> TraceForwardAsync(
        string srcDataSource, string srcEntity, string srcField);

    /// <summary>
    /// Get full lineage graph between two data sources.
    /// </summary>
    Task<LineageGraph> GetGraphAsync(string srcDataSource, string destDataSource);
}

public class LineageGraph
{
    public List<DataLineageRecord> Nodes { get; set; } = new();
    public List<LineageEdge>       Edges { get; set; } = new();
}

public record LineageEdge(string FromNodeId, string ToNodeId, string Label);
```

---

## 5. Metrics Engine

### PipelineMetrics Model

```csharp
public class PipelineMetrics
{
    public string PipelineId          { get; set; } = string.Empty;
    public string PipelineName        { get; set; } = string.Empty;
    public DateTime PeriodStart       { get; set; }
    public DateTime PeriodEnd         { get; set; }

    // ── Run counts ────────────────────────────────────────────────────────
    public int TotalRuns              { get; set; }
    public int SuccessfulRuns         { get; set; }
    public int FailedRuns             { get; set; }
    public int CancelledRuns          { get; set; }
    public double SuccessRate         => TotalRuns == 0 ? 0 : (double)SuccessfulRuns / TotalRuns;

    // ── Latency ───────────────────────────────────────────────────────────
    public TimeSpan AvgDuration       { get; set; }
    public TimeSpan MinDuration       { get; set; }
    public TimeSpan MaxDuration       { get; set; }
    public TimeSpan P95Duration       { get; set; }     // 95th percentile

    // ── Throughput ────────────────────────────────────────────────────────
    public long TotalRecordsProcessed { get; set; }
    public double AvgRowsPerSecond    { get; set; }
    public long TotalBytesProcessed   { get; set; }

    // ── Data Quality ──────────────────────────────────────────────────────
    public double AvgDQPassRate       { get; set; }
    public long TotalRejected         { get; set; }
    public long TotalWarned           { get; set; }

    // ── Error trends ─────────────────────────────────────────────────────
    public List<MetricDataPoint> RunsOverTime    { get; set; } = new();  // daily counts
    public List<MetricDataPoint> RowsOverTime    { get; set; } = new();
    public List<string> TopErrors               { get; set; } = new();
}

public record MetricDataPoint(DateTime DateUtc, double Value, string Label = "");
```

### MetricsEngine

```csharp
public class MetricsEngine
{
    public Task<PipelineMetrics> ComputeAsync(
        string pipelineId,
        DateTime from,
        DateTime to);

    public Task<IReadOnlyList<PipelineMetrics>> ComputeAllAsync(
        DateTime from,
        DateTime to);

    /// <summary>Live metrics for currently running pipelines.</summary>
    public PipelineMetrics GetLiveMetrics(string runId);
}
```

---

## 6. Alerting Engine

### Alert Rule Model

```csharp
public class AlertRule
{
    public string Id           { get; set; } = Guid.NewGuid().ToString();
    public string Name         { get; set; } = string.Empty;
    public string Description  { get; set; } = string.Empty;
    public bool   IsEnabled    { get; set; } = true;

    /// <summary>
    /// Which pipeline(s) this rule applies to.
    /// Null = applies to ALL pipelines.
    /// </summary>
    public List<string>? PipelineIds { get; set; }

    public AlertTrigger Trigger   { get; set; } = AlertTrigger.OnFailure;
    public string? Condition      { get; set; }   // expression evaluated on PipelineRunLog
    // e.g. "RecordsRejected > 1000"  or  "DQPassRate < 0.95"  or  "Duration > 00:30:00"

    public AlertSeverity Severity { get; set; } = AlertSeverity.Warning;

    /// <summary>Which notifiers fire when this rule triggers.</summary>
    public List<string> NotifierPluginIds { get; set; } = new();
    public Dictionary<string, object> NotifierConfig { get; set; } = new();

    /// <summary>Don't re-fire for the same pipeline within this window (minutes).</summary>
    public int SilenceWindowMinutes { get; set; } = 60;
}

public enum AlertTrigger
{
    OnFailure,          // pipeline run ends with Status = Failed
    OnSuccess,          // pipeline run ends with Status = Success
    OnCompletion,       // either way
    OnDQThreshold,      // DQ pass rate below threshold
    OnRejectedThreshold,// rejected row count above threshold
    OnDurationThreshold,// duration above threshold
    OnNoRunWithin,      // expected run has not occurred within N hours
    OnCustomExpression  // user-defined condition expression
}

public enum AlertSeverity { Info, Warning, Error, Critical }
```

### AlertEvent

```csharp
public class AlertEvent
{
    public string     EventId       { get; set; } = Guid.NewGuid().ToString();
    public string     RuleId        { get; set; } = string.Empty;
    public string     RuleName      { get; set; } = string.Empty;
    public string     PipelineId    { get; set; } = string.Empty;
    public string     PipelineName  { get; set; } = string.Empty;
    public string?    RunId         { get; set; }
    public AlertSeverity Severity   { get; set; }
    public string     Message       { get; set; } = string.Empty;
    public DateTime   FiredAtUtc    { get; set; } = DateTime.UtcNow;
    public bool       Acknowledged  { get; set; }
    public string?    AcknowledgedBy { get; set; }
    public DateTime?  AcknowledgedAt { get; set; }
}
```

### AlertingEngine

```csharp
public class AlertingEngine
{
    public event EventHandler<AlertEvent> AlertFired;

    /// <summary>Evaluate all enabled alert rules against a completed run log.</summary>
    public Task EvaluateAsync(PipelineRunLog runLog, CancellationToken token);

    /// <summary>Evaluate "no run within" rules (called on schedule).</summary>
    public Task EvaluateLivenessAsync(CancellationToken token);

    /// <summary>Acknowledge an alert event.</summary>
    public Task AcknowledgeAsync(string eventId, string acknowledgedBy);

    public Task<IReadOnlyList<AlertEvent>> GetRecentAlertsAsync(int limit = 100);
    public Task<IReadOnlyList<AlertEvent>> GetUnacknowledgedAsync();
}
```

---

## 7. Notifier Plugins

### IPipelineNotifier

```csharp
public interface IPipelineNotifier : IPipelinePlugin
{
    Task NotifyAsync(AlertEvent alertEvent, CancellationToken token);
}
```

### Built-in Notifiers

#### EmailNotifier
```
PluginId: "beep.notify.email"
Config:
  SmtpHost:     "smtp.gmail.com"
  SmtpPort:     587
  UseTls:       true
  Username:     "${env:SMTP_USER}"       // env var substitution
  Password:     "${env:SMTP_PASS}"
  From:         "etl@company.com"
  To:           ["ops@company.com", "dba@company.com"]
  Subject:      "[{Severity}] Pipeline {PipelineName} — {AlertRule}"
  BodyTemplate: "Run {RunId} failed at {FinishedAt}.\nError: {Message}\nRecords: {RecordsRead}"
```

#### WebhookNotifier
```
PluginId: "beep.notify.webhook"
Config:
  Url:          "https://hooks.slack.com/T.../..."
  Method:       "POST"
  Headers:      { "Content-Type": "application/json" }
  PayloadTemplate: |
    {
      "text": ":red_circle: *{PipelineName}* failed: {Message}",
      "attachments": [{"fields": [{"title": "RunId", "value": "{RunId}"}]}]
    }
  HmacSecret:   "${env:WEBHOOK_SECRET}"   // optional HMAC-SHA256 signing
```

#### LogFileNotifier
```
PluginId: "beep.notify.logfile"
Config:
  FilePath:  "C:/Logs/pipeline-alerts.log"
  MaxSizeMb: 50
  Format:    "{Timestamp:O} [{Severity}] {PipelineName}: {Message}"
```

---

## 8. Audit Trail

Every state-changing operation is written as an append-only `AuditEntry`:

```csharp
public class AuditEntry
{
    public string   Id            { get; set; } = Guid.NewGuid().ToString();
    public string   Action        { get; set; } = string.Empty;  // "PipelineCreated","RunTriggered","ConfigChanged"
    public string   EntityType    { get; set; } = string.Empty;  // "Pipeline"|"Schedule"|"AlertRule"
    public string   EntityId      { get; set; } = string.Empty;
    public string   EntityName    { get; set; } = string.Empty;
    public string?  PerformedBy   { get; set; }
    public DateTime PerformedAt   { get; set; } = DateTime.UtcNow;
    public string?  PreviousValue { get; set; }  // JSON snapshot of before
    public string?  NewValue      { get; set; }  // JSON snapshot of after
    public string?  IpAddress     { get; set; }
    public Dictionary<string, string> Tags { get; set; } = new();
}
```

Audit entries are **append-only** — no update or delete. Written by:
- `PipelineManager.SaveAsync()` / `DeleteAsync()`
- `SchedulerHost.TriggerManualAsync()`
- `AlertingEngine.AcknowledgeAsync()`

---

## 9. ObservabilityStore — Persistence

```csharp
public class ObservabilityStore
{
    // Storage paths (all under ExePath/)
    //   RunLogs/          {runId}.run.json
    //   Lineage/          {runId}.lineage.json
    //   Alerts/           alert-events.jsonl   (append-only JSONL)
    //   Audit/            audit.jsonl          (append-only JSONL)
    //   Metrics/          {pipelineId}.metrics-cache.json

    // Run Logs
    Task SaveRunLogAsync(PipelineRunLog log);
    Task<PipelineRunLog?> GetRunLogAsync(string runId);
    Task<IReadOnlyList<PipelineRunLog>> QueryRunLogsAsync(RunLogQuery query);

    // Lineage
    Task AppendLineageAsync(IEnumerable<DataLineageRecord> records);
    Task<IReadOnlyList<DataLineageRecord>> GetLineageAsync(string runId);

    // Alert Events
    Task AppendAlertEventAsync(AlertEvent evt);
    Task<IReadOnlyList<AlertEvent>> GetAlertEventsAsync(AlertEventQuery query);
    Task UpdateAlertAcknowledgementAsync(string eventId, string by, DateTime at);

    // Audit
    Task AppendAuditAsync(AuditEntry entry);
    Task<IReadOnlyList<AuditEntry>> GetAuditTrailAsync(AuditQuery query);

    // Metrics cache
    Task SaveMetricsCacheAsync(PipelineMetrics metrics);
    Task<PipelineMetrics?> GetMetricsCacheAsync(string pipelineId);
}
```

### Query Models

```csharp
public class RunLogQuery
{
    public string? PipelineId  { get; set; }
    public RunStatus? Status   { get; set; }
    public DateTime? From      { get; set; }
    public DateTime? To        { get; set; }
    public int Limit           { get; set; } = 100;
    public int Offset          { get; set; } = 0;
    public string? OrderBy     { get; set; } = "StartedAtUtc DESC";
}
```

---

## 10. Dashboard Data API

A query facade that feeds the Phase 7 UI dashboard and any external tools:

```csharp
public class PipelineDashboardApi
{
    // Overview
    Task<DashboardSummary> GetSummaryAsync(DateTime from, DateTime to);

    // Pipeline-specific
    Task<PipelineMetrics> GetPipelineMetricsAsync(string pipelineId, DateTime from, DateTime to);
    Task<IReadOnlyList<PipelineRunLog>> GetRecentRunsAsync(string? pipelineId, int limit);

    // Active runs (live)
    IReadOnlyList<LiveRunStatus> GetActiveRuns();

    // Alerts
    Task<IReadOnlyList<AlertEvent>> GetRecentAlertsAsync(int limit);

    // Lineage
    Task<LineageGraph> GetLineageGraphAsync(string srcDataSource, string destDataSource);

    // Audit
    Task<IReadOnlyList<AuditEntry>> GetAuditTrailAsync(string entityType, string entityId);
}

public class DashboardSummary
{
    public int TotalPipelines        { get; set; }
    public int ActiveRuns            { get; set; }
    public int RunsToday             { get; set; }
    public int FailuresToday         { get; set; }
    public long RowsProcessedToday   { get; set; }
    public double AvgSuccessRate     { get; set; }
    public List<AlertEvent> RecentAlerts { get; set; } = new();
    public List<PipelineRunLog> RecentRuns { get; set; } = new();
}
```

---

## 11. Sensitive Data Masking

Row logs capture field snapshots for debugging, but sensitive columns must be masked:

```csharp
public class MaskingConfig
{
    /// <summary>Column names that are always masked regardless of pipeline.</summary>
    public List<string> GlobalMaskedFields { get; set; } = new()
    {
        "Password", "CreditCard", "SSN", "NationalId", "CVV", "PIN", "SecretKey"
    };

    public MaskingStrategy Strategy { get; set; } = MaskingStrategy.Redact;
    public int ShowFirstChars       { get; set; } = 0;   // for Partial strategy
    public int ShowLastChars        { get; set; } = 4;   // e.g. "****1234"
}

public enum MaskingStrategy
{
    Redact,     // "***REDACTED***"
    Partial,    // "****1234"
    Hash        // SHA256 of value
}
```

---

## 12. Deliverables (Implementation Checklist)

- [ ] `PipelineRunLog.cs`, `StepRunLog.cs`, `RowRunLog.cs`
- [ ] `DataLineageRecord.cs` (from Phase 4 — centralized here)
- [ ] `PipelineMetrics.cs` + `MetricDataPoint`
- [ ] `AlertRule.cs`, `AlertEvent.cs`
- [ ] `AuditEntry.cs`
- [ ] `ObservabilityStore.cs` (JSON/JSONL persistence)
- [ ] `MetricsEngine.cs`
- [ ] `AlertingEngine.cs`
- [ ] `EmailNotifier.cs`
- [ ] `WebhookNotifier.cs`
- [ ] `LogFileNotifier.cs`
- [ ] `PipelineDashboardApi.cs`
- [ ] `MaskingConfig.cs`
- [ ] Wire `AlertingEngine.EvaluateAsync()` into `PipelineEngine` post-run
- [ ] Wire `AuditEntry` writes into `PipelineManager` and `SchedulerHost`
- [ ] Unit tests: metrics calculation, alert rule evaluation

---

## 13. Estimated Effort

| Task | Days |
|------|------|
| Log models (3 files) | 1 |
| Metrics model + engine | 2.5 |
| Alert rule + engine | 3 |
| Notifier plugins (3) | 2 |
| ObservabilityStore | 2 |
| Dashboard API | 1.5 |
| Masking config | 0.5 |
| Integration + wire-up | 2 |
| Tests | 2 |
| **Total** | **16.5 days** |
