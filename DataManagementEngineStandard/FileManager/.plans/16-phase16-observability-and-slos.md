# Phase 16 — Observability and SLOs

| Attribute      | Value                                       |
|----------------|---------------------------------------------|
| Phase          | 16                                          |
| Status         | planned                                     |
| Priority       | High                                        |
| Dependencies   | Phase 10 (rollout/KPIs), Phase 11 (ingestion contracts), Phase 13 (resilience) |
| Est. Effort    | 4 days                                      |

---

## 1. Goal

Make every file ingestion job **fully observable** from a single pane of glass:
- Structured telemetry (traces, metrics, logs) emitted in OpenTelemetry format.
- SLO definitions that are machine-enforceable at ingestion time.
- Health-check endpoints usable by load-balancers, container orchestrators, and monitoring agents.
- Alerting thresholds codified alongside the implementation — no undocumented tribal knowledge.

---

## 2. Observability Pillars

### 2.1 Structured Telemetry

All telemetry follows the **OTel (OpenTelemetry) semantic conventions** for database and messaging operations.

| Signal | What is captured | OTel signal type |
|--------|-----------------|-----------------|
| Ingestion job lifecycle | Job start, transitions, complete/fail | Span (Trace) |
| Checkpoint saved | byte offset, row count, timestamp | Span event |
| Row error / dead-letter | error category, row number, column | Span event |
| Schema drift detected | drift type, column name, baseline version | Span event |
| File checksum computed | file path, duration, hash | Span |
| Rows-per-second throughput | batch commit count / elapsed seconds | Metric (histogram) |
| Rejection rate | rejected / total rows | Metric (gauge) |
| Queue depth (pending jobs) | count of Pending + Suspended jobs | Metric (gauge) |
| File size processed | bytes | Metric (counter) |

### 2.2 Telemetry Contracts

```csharp
namespace TheTechIdea.Beep.FileManager.Observability
{
    public interface IFileIngestionTelemetry
    {
        /// <summary>Starts a span for a top-level ingestion job. Returns a disposable activity.</summary>
        IDisposable BeginJob(string jobId, string sourceSystem, string entityName, string tenantId);

        /// <summary>Records a state transition event on the current span.</summary>
        void RecordTransition(string jobId, string fromState, string toState, string reason = null);

        /// <summary>Records a checkpoint event.</summary>
        void RecordCheckpoint(string jobId, long bytesRead, long rowsCommitted);

        /// <summary>Records a dead-letter event.</summary>
        void RecordDeadLetter(string jobId, long rowIndex, string errorCategory, string columnName);

        /// <summary>Records the final job completion metrics.</summary>
        void RecordCompletion(string jobId, long totalRows, long committedRows, long rejectedRows,
                              TimeSpan elapsed, bool succeeded);

        /// <summary>Increments the rows-processed counter.</summary>
        void IncrementRows(long count);

        /// <summary>Increments the bytes-processed counter.</summary>
        void IncrementBytes(long bytes);
    }
}
```

### 2.3 OTel Activity Source registration

```csharp
// In BeepServices DI registration:
services.AddOpenTelemetry()
    .WithTracing(builder => builder
        .AddSource("TheTechIdea.Beep.FileManager")
        .AddOtlpExporter())
    .WithMetrics(builder => builder
        .AddMeter("TheTechIdea.Beep.FileManager.Metrics")
        .AddOtlpExporter());
```

---

## 3. SLO Definitions

### 3.1 SLO table

| SLO Name | Target | Measurement window | Breach action |
|----------|--------|-------------------|---------------|
| **Ingestion Latency p95** | < 30 seconds per 100K rows | Rolling 1 hour | Alert on-call |
| **Ingestion Latency p99** | < 60 seconds per 100K rows | Rolling 1 hour | Alert on-call + page |
| **Throughput** | >= 50 000 rows/second | Rolling 5 minutes | Auto-suspend throttled jobs |
| **Error Rate** | < 1% rejected rows per job | Per job | Alert data steward if >= 1%; suspend job if >= 5% |
| **Job Completion Rate** | >= 99% of started jobs Complete within SLA window | Rolling 24 hours | Alert engineering |
| **Schema Drift False-Positive Rate** | < 5% of non-breaking drifts quarantine a file | Rolling 24 hours | Tune classification thresholds |
| **Dead-Letter Backlog** | < 10 000 unresolved entries | Instantaneous | Alert data ops |

### 3.2 `ISloEnforcer`

```csharp
namespace TheTechIdea.Beep.FileManager.Observability
{
    public interface ISloEnforcer
    {
        /// <summary>
        /// Called after each committed batch.  
        /// Evaluates current metrics against SLO thresholds
        /// and returns a list of violations (empty list = all green).
        /// </summary>
        IReadOnlyList<SloViolation> Evaluate(IngestionMetricsSnapshot snapshot);
    }

    public sealed record IngestionMetricsSnapshot(
        string JobId,
        long RowsRead,
        long RowsCommitted,
        long RowsRejected,
        long BytesRead,
        TimeSpan Elapsed,
        int UnresolvedDeadLetters);

    public sealed record SloViolation(
        string SloName,
        string Detail,
        SloSeverity Severity);

    public enum SloSeverity { Warning, Critical }
}
```

### 3.3 SLO enforcement in the ingestion loop

```
After each CommitBatch():
    snapshot = BuildSnapshot(jobId, rowsRead, rowsCommitted, rowsRejected, bytesRead, elapsed)
    violations = sloEnforcer.Evaluate(snapshot)
    for each violation:
        telemetry.RecordSloViolation(violation)
        if violation.Severity == Critical:
            // suspend job and notify
            stateStore.TransitionAsync(jobId, Suspended, $"SLO breach: {violation.SloName}")
            alertingService.NotifyAsync(violation)
```

---

## 4. Health Check Endpoints

### 4.1 `IFileManagerHealthCheck`

```csharp
namespace TheTechIdea.Beep.FileManager.Observability
{
    public interface IFileManagerHealthCheck
    {
        /// <summary>
        /// Returns the current health of the FileManager subsystem.
        /// Called by load balancers, orchestrators (K8s liveness probe), and monitoring agents.
        /// </summary>
        Task<HealthCheckResult> CheckAsync(CancellationToken ct = default);
    }

    public sealed class HealthCheckResult
    {
        public HealthStatus Status { get; init; }       // Healthy | Degraded | Unhealthy
        public string Description { get; init; }
        public IReadOnlyDictionary<string, object> Data { get; init; }  // detailed metrics
    }

    public enum HealthStatus { Healthy, Degraded, Unhealthy }
}
```

### 4.2 Health check data payload (example)

```json
{
  "status": "Degraded",
  "description": "Dead-letter backlog exceeds warning threshold",
  "data": {
    "activeJobs": 3,
    "pendingJobs": 12,
    "suspendedJobs": 1,
    "failedJobsLast1h": 0,
    "completedJobsLast1h": 47,
    "unresolvedDeadLetters": 8203,
    "deadLetterWarningThreshold": 5000,
    "avgThroughputRowsPerSec": 62400,
    "lastJobCompletedAt": "2025-01-15T10:22:00Z"
  }
}
```

### 4.3 Registration in ASP.NET Core

```csharp
services.AddHealthChecks()
    .AddCheck<BeepFileManagerHealthCheck>("beep-filemanager");

app.MapHealthChecks("/health/filemanager");
```

---

## 5. Alerting Thresholds

All thresholds are configurable via the same config system (JSON / Beep ConfigEditor).  
Defaults shown below:

```json
{
  "FileManagerAlerts": {
    "DeadLetterWarningThreshold": 5000,
    "DeadLetterCriticalThreshold": 10000,
    "RowRejectionRateWarningPercent": 1.0,
    "RowRejectionRateCriticalPercent": 5.0,
    "JobSlaWindowMinutes": 60,
    "MinThroughputRowsPerSecond": 10000,
    "MaxPendingJobs": 50
  }
}
```

### 5.1 `IFileIngestionAlerting`

```csharp
namespace TheTechIdea.Beep.FileManager.Observability
{
    public interface IFileIngestionAlerting
    {
        Task NotifyAsync(SloViolation violation, string jobId, ITenantContext context, CancellationToken ct = default);
        Task NotifyJobFailedAsync(string jobId, string reason, ITenantContext context, CancellationToken ct = default);
        Task NotifyDeadLetterBacklogAsync(int backlogSize, string tenantId, CancellationToken ct = default);
    }
}
```

Provide two built-in implementations:
- `LoggingFileIngestionAlerting` — writes to `IDMEEditor.Logger` (default).
- `PassEventFileIngestionAlerting` — raises `DMEEditor.PassEvent` for Beep's existing notification bus.

External integrations (PagerDuty, OpsGenie, Slack) implemented as plugins outside this phase.

---

## 6. Metrics Naming Convention

| Metric | OTel name | Unit | Type |
|--------|-----------|------|------|
| Rows processed | `beep.filemanager.rows_processed` | `{row}` | Counter |
| Rows rejected | `beep.filemanager.rows_rejected` | `{row}` | Counter |
| Bytes processed | `beep.filemanager.bytes_processed` | `By` | Counter |
| Jobs started | `beep.filemanager.jobs_started` | `{job}` | Counter |
| Jobs completed | `beep.filemanager.jobs_completed` | `{job}` | Counter |
| Jobs failed | `beep.filemanager.jobs_failed` | `{job}` | Counter |
| Ingestion duration | `beep.filemanager.ingestion_duration` | `s` | Histogram |
| Throughput | `beep.filemanager.throughput_rows_per_second` | `{row}/s` | Gauge |
| Dead-letter backlog | `beep.filemanager.dead_letter_backlog` | `{entry}` | Gauge |
| Pending jobs | `beep.filemanager.pending_jobs` | `{job}` | Gauge |

All metrics carry standard attributes: `tenant_id`, `source_system`, `entity_name`.

---

## 7. Distributed Tracing — Span Hierarchy

```
BeepFileManager.IngestJob  [root span]
    ├── BeepFileManager.ComputeChecksum
    ├── BeepFileManager.ValidateSchema
    │       └── BeepFileManager.DetectDrift
    ├── BeepFileManager.IngestRows
    │       ├── event: checkpoint.saved  (rowsCommitted=10000, bytesRead=5242880)
    │       ├── event: row.rejected      (rowIndex=10023, errorCategory=TypeConversionError)
    │       └── event: checkpoint.saved  (rowsCommitted=20000, ...)
    └── BeepFileManager.PublishLineage
```

---

## 8. Acceptance Criteria

| # | Criterion | Test |
|---|-----------|------|
| 1 | A completed ingestion job produces a root OTel span with child spans for Validate and IngestRows | Integration |
| 2 | `ISloEnforcer` returns a `Critical` violation when rejection rate exceeds 5% | Unit |
| 3 | `IFileManagerHealthCheck.CheckAsync` returns `Degraded` when dead-letter backlog > warning threshold | Unit |
| 4 | `IFileManagerHealthCheck.CheckAsync` returns `Unhealthy` when dead-letter backlog > critical threshold | Unit |
| 5 | `beep.filemanager.rows_processed` increments by the correct count for a known file | Integration |
| 6 | Alerting fires `NotifyJobFailedAsync` when a job transitions to `Failed` state | Unit |
| 7 | All metrics include `tenant_id` attribute | Unit |

---

## 9. Deliverables

| Artifact | Location |
|----------|----------|
| `Observability/IFileIngestionTelemetry.cs` | `FileManager/Observability/` |
| `Observability/OtelFileIngestionTelemetry.cs` | `FileManager/Observability/Implementations/` |
| `Observability/NullFileIngestionTelemetry.cs` | `FileManager/Observability/Implementations/` |
| `Observability/ISloEnforcer.cs` | `FileManager/Observability/` |
| `Observability/DefaultSloEnforcer.cs` | `FileManager/Observability/Implementations/` |
| `Observability/IFileManagerHealthCheck.cs` | `FileManager/Observability/` |
| `Observability/BeepFileManagerHealthCheck.cs` | `FileManager/Observability/Implementations/` |
| `Observability/IFileIngestionAlerting.cs` | `FileManager/Observability/` |
| `Observability/LoggingFileIngestionAlerting.cs` | `FileManager/Observability/Implementations/` |
| `Observability/FileManagerAlertConfig.cs` | `FileManager/Observability/` |
| Unit tests | `tests/FileManager/ObservabilityTests.cs` |

---

## 10. Enterprise Standards Traceability

| Standard | Clause | Addressed |
|----------|--------|-----------|
| SRE (Google) | SLO/SLA definitions | SLO table + `ISloEnforcer` |
| OpenTelemetry Spec | Semantic conventions for DB | Span hierarchy, metric naming |
| NIST SP 800-137 | Continuous monitoring | Health checks + alerting |
| ISO/IEC 25010 | Reliability — availability | Job completion rate SLO |
| SOC 2 Type II | Availability criteria | Health endpoint + dead-letter SLO |
