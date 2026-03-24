# BeepSync Quick Reference

---

## Phase 1 — Basic Full Sync

```csharp
// Initialize
var syncManager = new BeepSyncManager(dmeEditor);

// Create schema
var schema = new DataSyncSchema
{
    Id                        = Guid.NewGuid().ToString(),
    SourceDataSourceName      = "SourceDB",
    DestinationDataSourceName = "DestDB",
    SourceEntityName          = "Customers",
    DestinationEntityName     = "Customers",
    SourceKeyField            = "CustomerId",
    DestinationKeyField       = "CustomerId",
    SyncType                  = "Full",
    SyncDirection             = "OneWay",
    BatchSize                 = 500,
};

// Auto-map fields
var fieldMappingHelper = new FieldMappingHelper(dmeEditor);
schema.MappedFields = new ObservableBindingList<FieldSyncData>(
    fieldMappingHelper.AutoMapFields("SourceDB", "Customers", "DestDB", "Customers"));

// Validate then run
syncManager.AddSyncSchema(schema);
var validation = syncManager.ValidateSchema(schema);
if (validation.Flag == Errors.Ok)
    await syncManager.SyncDataAsync(schema, cancellationToken, progress);

// Persist
await syncManager.SaveSchemasAsync();
```

---

## Phase 2 — Schema Versioning & Governance

```csharp
// Attach a version snapshot before first deploy
schema.CurrentSchemaVersion = new SyncSchemaVersion
{
    Version        = 1,
    MappingVersion = "v1.0",
    ApprovalState  = "Draft",   // Draft | Approved | Deprecated
    ChangedBy      = "alice",
    ChangedAt      = DateTime.UtcNow,
    ChangeNote     = "Initial mapping",
};

// Promote approval state
fieldMappingHelper.PromoteMappingState(schema, "Approved");

// Persist versioned snapshot
await schemaPersistenceHelper.SaveVersionedSchemaAsync(schema, schema.CurrentSchemaVersion);

// Load history
var history = await schemaPersistenceHelper.LoadSchemaVersionsAsync(schema.Id);

// Diff versus persisted
var diff = await schemaPersistenceHelper.DiffSchemaToPersistedAsync(schema);
if (!string.IsNullOrEmpty(diff)) Console.WriteLine("Drift detected:\n" + diff);
```

---

## Phase 3 — Incremental Sync & CDC

```csharp
// Incremental via watermark (Append or Upsert)
schema.SyncType       = "Incremental";
schema.WatermarkPolicy = new WatermarkPolicy
{
    WatermarkField = "UpdatedAt",
    WatermarkMode  = "Upsert",   // Append | Upsert | CDC
    InitialValue   = "1970-01-01T00:00:00Z",
};

// CDC mode — build filter context
schema.WatermarkPolicy.WatermarkMode = "CDC";
// The orchestrator creates CdcFilterContext internally;
// the source datasource must support CDC change queries.

// Validate watermark before run
var wmResult = syncManager.ValidateWatermarkPolicy(schema); // via ISyncValidationHelper
```

---

## Phase 4 — Bidirectional Conflict Resolution

```csharp
schema.SyncDirection = "Bidirectional";

schema.ConflictPolicy = new ConflictPolicy
{
    Strategy            = "LastWriteWins",  // SourceWins | DestWins | LastWriteWins | Manual
    TimestampFieldSource = "UpdatedAt",
    TimestampFieldDest   = "UpdatedAt",
    LogConflicts         = true,
};

// After a run, conflict evidence is available in the reconciliation report
// schema.LastReconciliationReport.ConflictCount
```

---

## Phase 5 — Retry, Checkpoint & Idempotency

```csharp
schema.RetryPolicy = new RetryPolicy
{
    MaxAttempts = 3,
    BaseDelayMs = 2000,
    UseJitter   = true,
    MaxDelayMs  = 30000,
};

// The orchestrator persists a SyncCheckpoint after each successful batch;
// on retry the run resumes from LastProcessedKey.

// Inspect or reset checkpoint
schema.ActiveCheckpoint = null;   // force full re-run from start
await syncManager.SaveSchemasAsync();
```

---

## Phase 6 — Data Quality Gates & Reconciliation

```csharp
schema.DqPolicy = new DqPolicy
{
    RuleKeys          = new List<string> { "dq.customers.email", "dq.customers.required" },
    RejectThreshold   = 0.05,   // abort if >5 % of records fail DQ
    QuarantineEnabled = true,
    DefaultFillFirst  = true,   // apply DefaultsManager before DQ evaluation
};

// After run — inspect report
var report = schema.LastReconciliationReport;
Console.WriteLine($"Source:{report.SourceRowCount} Dest:{report.DestRowCount} " +
                  $"Rejects:{report.RejectCount} MappingQuality:{report.MappingQualityBand}");

// Mapping quality gate (checked automatically by orchestrator)
// schema.MappingPolicy.MinQualityScore = 80;  // block sync if score < 80
```

---

## Phase 7 — SLO, Observability & Alerting

```csharp
schema.SloProfile = new SloProfile
{
    TargetSuccessRatePct  = 99.0,
    MaxFreshnessLagSecs   = 300,
    MaxConflictRatePct    = 2.0,
    AlertRuleKeys         = new List<string> { "alert.sync.highRejectRate", "alert.sync.freshness" },
};

// After run
var metrics = syncManager.LastSyncMetrics;              // SyncMetrics
Console.WriteLine($"SLO tier: {metrics.SloComplianceTier}");  // Platinum | Gold | Silver | Degraded

var alerts = schema.LastRunAlerts;  // List<SyncAlertRecord>
foreach (var a in alerts)
    Console.WriteLine($"[{a.Severity}] {a.RuleKey}: {a.Message}");
```

### SyncMetrics fields (Phase 7+)
| Property | Description |
|----------|-------------|
| `SloComplianceTier` | `Platinum` / `Gold` / `Silver` / `Degraded` |
| `RejectRate` | DQ rejects ÷ total records |
| `ConflictRate` | Conflicts ÷ total records |
| `FreshnessLagSeconds` | Elapsed since last watermark value |
| `RetryCount` | Retry attempts this run |
| `RuleEvaluationCount` | Total rule engine evaluations |
| `MappingDriftDetected` | True when live mapping differs from checkpoint version |
| `MappingPlanVersion` | Version string from checkpoint / schema version |
| `CorrelationId` | Run correlation ID for log tracing |

---

## Phase 8 — Performance, Scale & Plan Caching

```csharp
schema.PerfProfile = new SyncPerformanceProfile
{
    BatchSize                  = 2000,
    MaxParallelism             = 8,
    RulePolicyMode             = "FastPath",  // "Safe" | "FastPath"
    DefaultsCacheTtlSeconds    = 600,
    WarmUpDefaultsProfileOnRun = true,
    UseParallelBatches         = true,
    ParallelBatchQueueDepth    = 16,
};

// Resolve policy explicitly (orchestrator does this automatically)
var policy = SyncRuleExecutionPolicies.Resolve(schema.PerfProfile.RulePolicyMode);
// -> FastPath: MaxDepth=3, MaxExecutionMs=2000
// -> Safe:     MaxDepth=10, MaxExecutionMs=5000

// Run all schemas in parallel (bounded by max MaxParallelism across schemas)
await syncManager.SyncAllDataParallelAsync(cancellationToken, progress);
```

### SyncPerformanceProfile fields
| Property | Default | Description |
|----------|---------|-------------|
| `BatchSize` | 1000 | Records per import batch |
| `MaxParallelism` | 4 | Max concurrent schemas in `SyncAllDataParallelAsync` |
| `RulePolicyMode` | `"Safe"` | `"Safe"` or `"FastPath"` (see `SyncRuleExecutionPolicies`) |
| `DefaultsCacheTtlSeconds` | 300 | Informational TTL for cached `EntityDefaultsProfile` |
| `WarmUpDefaultsProfileOnRun` | true | Pre-fetch defaults profile before retry loop |
| `SkipRulesOnCleanBatch` | false | Skip rule engine when batch has no DQ failures |
| `UseParallelBatches` | true | Participate in parallel fan-out |
| `ParallelBatchQueueDepth` | 8 | Max queued batches awaiting the semaphore |

---

## Schema Management API

```csharp
syncManager.AddSyncSchema(schema);
syncManager.UpdateSyncSchema(schema);
syncManager.RemoveSyncSchema(schemaId);
var schemas = await syncManager.LoadSchemasAsync();
await syncManager.SaveSchemasAsync();

// Sequential run — all schemas one at a time
await syncManager.SyncAllDataAsync(token, progress);

// Parallel run — bounded by PerfProfile.MaxParallelism
await syncManager.SyncAllDataParallelAsync(token, progress);
```

---

## Validation API

```csharp
var result = syncManager.ValidateSchema(schema);
var result = syncManager.ValidateDataSource("MyDB");
var result = syncManager.ValidateEntity("MyDB", "Customers");
var result = syncManager.ValidateSyncOperation(schema);
var result = syncManager.ValidateWatermarkPolicy(schema);
```

---

## Orchestrator internal phases (per `SyncDataAsync` run)

1. Preflight — datasource/entity validation, mapping quality gate
2. Schema version drift detection
3. Watermark / CDC filter build
4. DQ counter reset, rule-audit subscription (Phase 7)
5. **Phase 8**: rule policy resolution, defaults profile warmup, mapping cache invalidation
6. Retry loop (exponential back-off with jitter)
   - Checkpoint resume if `ActiveCheckpoint` present
   - Translator → import config → `DataImportManager.RunImportAsync`
   - DQ gate evaluation per record; reject / quarantine / abort
   - Conflict resolution (bidirectional)
   - Checkpoint persist on success
7. Reconciliation report build
8. SLO metric emission + alert rule evaluation
9. Schema status + `LastSyncDate` update; `SaveSchemasAsync`

