---
name: beepsync
description: Guidance for data synchronization between BeepDM datasources using BeepSyncManager and DataSyncSchema. Covers all production capabilities: full/incremental/CDC sync, bidirectional conflict resolution, retry/checkpoint, DQ gates, reconciliation, SLO/alerting, and parallel performance. Use when managing sync schemas, running sync operations, or implementing governance policies.
---

# BeepSync Data Synchronization Guide

Use this skill when synchronizing data between BeepDM datasources or working with any aspect of `BeepSyncManager` or `DataSyncSchema`.

## Use this skill when
- Building or validating `DataSyncSchema` definitions
- Running sync operations (full, incremental, CDC, bidirectional) through `BeepSyncManager`
- Configuring policy objects: `WatermarkPolicy`, `ConflictPolicy`, `RetryPolicy`, `DqPolicy`, `SloProfile`, `SyncPerformanceProfile`
- Working with field mappings, filters, governance, versioning, or schema persistence
- Investigating SLO compliance, alert rules, reconciliation reports, or DQ gate results
- Parallelising sync runs via `SyncAllDataParallelAsync`

## Do not use this skill when
- The task is primarily a staged import pipeline with replay, quarantine, or transformation rules. Use [`importing`](../importing/SKILL.md).
- The task is primarily ETL script generation or direct datasource copy. Use [`etl`](../etl/SKILL.md).

## Architecture

### Orchestrator
- `BeepSyncManager` (`BeepSyncManager.Orchestrator.cs`) — primary entry point; drives all sync phases per schema run.

### Helpers (all under `Helpers/`)
| Helper | Responsibility |
|--------|---------------|
| `SyncValidationHelper` | Schema and runtime validation, DQ gate evaluation, mapping quality gate |
| `SchemaPersistenceHelper` | Save/load schemas, versioned snapshots, checkpoint persistence |
| `SyncSchemaTranslator` | Converts `DataSyncSchema` → `DataImportConfiguration` |
| `FieldMappingHelper` | Auto-map, validate, drift-detect, and promote governed mappings |
| `SyncProgressHelper` | Progress reporting, logging, reconciliation report building, SLO metrics, alert evaluation |
| `DataSourceHelper` | Datasource open/close, entity read/write |

### Interfaces (`Interfaces/ISyncHelpers.cs`)
`IDataSourceHelper` · `IFieldMappingHelper` · `ISyncValidationHelper` · `ISyncProgressHelper` · `ISchemaPersistenceHelper`

### Models (engine, under `Models/`)
`SyncIntegrationContext` · `SyncPlanMetadata` · `CdcFilterContext`

### Cross-cutting classes
- `SyncMetrics` — run telemetry (records, rates, SLO tier, drift flag, retry count)
- `SyncRuleExecutionPolicies` — `DefaultSafe` and `FastPath` presets; `Resolve(mode)` helper

### Policy / DTO models (models project, `DataManagementModelsStandard/Editor/BeepSync/`)
`WatermarkPolicy` · `ConflictPolicy` · `RetryPolicy` · `DqPolicy` · `DqGateResult` · `ConflictEvidence` · `SyncCheckpoint` · `SyncSchemaVersion` · `SyncPolicies` · `SyncPreflightReport` · `SyncReconciliationReport` · `SloProfile` · `SyncAlertRecord` · `SyncPerformanceProfile`

---

## Phase Capabilities (implemented, all at 0 build errors)

| Phase | Feature | Key types / properties |
|-------|---------|------------------------|
| 1 | Contracts & foundation | `DataSyncSchema`, `FieldSyncData`, `SyncRunData`, `ISyncHelpers` interfaces |
| 2 | Schema governance & versioning | `SyncSchemaVersion`, `SyncMappingPolicy`, `SyncRulePolicy`, `SyncDefaultsPolicy`; `FieldMappingHelper.PromoteMappingState` |
| 3 | Incremental sync & CDC | `WatermarkPolicy` (Append / Upsert / CDC modes), `CdcFilterContext`; watermark field validated on startup |
| 4 | Bidirectional conflict resolution | `ConflictPolicy` (`SourceWins/DestWins/LastWriteWins/Manual`), `ConflictEvidence`; reverse-mapping pass |
| 5 | Reliability, retry & idempotency | `RetryPolicy` (MaxAttempts, BaseDelayMs, Jitter), `SyncCheckpoint` (resume offset, mapping version); exponential back-off |
| 6 | Data quality & reconciliation | `DqPolicy` (RuleKeys, RejectThreshold, QuarantineEnabled), `DqGateResult`, `SyncReconciliationReport`; mapping quality gate |
| 7 | Observability, SLO & alerting | `SloProfile` (AlertRuleKeys, latency/freshness/success SLOs), `SyncAlertRecord`; `SyncMetrics` with `SloComplianceTier`, `RejectRate`, `ConflictRate`; `EmitSloMetrics` + `EvaluateAlertRules` |
| 8 | Performance & scale | `SyncPerformanceProfile` (BatchSize, MaxParallelism, RulePolicyMode, DefaultsCacheTtlSeconds); `SyncRuleExecutionPolicies`; `SyncAllDataParallelAsync`; defaults profile warmup; mapping cache invalidation on version change |

---

## File Locations

```
DataManagementEngineStandard/Editor/BeepSync/
  BeepSyncManager.Orchestrator.cs          # main orchestrator
  SyncMetrics.cs                           # run telemetry model
  SyncRuleExecutionPolicies.cs             # DefaultSafe / FastPath presets
  Helpers/
    SyncValidationHelper.cs
    SchemaPersistenceHelper.cs
    SyncSchemaTranslator.cs
    FieldMappingHelper.cs
    SyncProgressHelper.cs
    DataSourceHelper.cs
  Interfaces/
    ISyncHelpers.cs
  Models/
    SyncIntegrationContext.cs
    SyncPlanMetadata.cs
    CdcFilterContext.cs

DataManagementModelsStandard/Editor/
  DataSyncSchema.cs                        # main schema + FieldSyncData, SyncRunData
  BeepSync/
    WatermarkPolicy.cs
    ConflictPolicy.cs
    RetryPolicy.cs
    DqPolicy.cs  /  DqGateResult.cs
    ConflictEvidence.cs
    SyncCheckpoint.cs
    SyncSchemaVersion.cs
    SyncPolicies.cs  /  SyncPreflightReport.cs
    SyncReconciliationReport.cs
    SloProfile.cs  /  SyncAlertRecord.cs
    SyncPerformanceProfile.cs
```

---

## Typical Workflow

1. **Create / load** a `DataSyncSchema` — set source/destination names, entity names, key fields, `SyncType`, `SyncDirection`.
2. **Attach policies** as needed: `WatermarkPolicy`, `ConflictPolicy`, `RetryPolicy`, `DqPolicy`, `SloProfile`, `PerfProfile`.
3. **Auto-map or manually define** `MappedFields`; run `FieldMappingHelper.AutoMapFields` then `ValidateFieldMappings`.
4. **Validate** with `ValidateSchema` / `ValidateSyncOperation` / `ValidateWatermarkPolicy`.
5. **Run** `await SyncDataAsync(schema, token, progress)` (or `SyncAllDataParallelAsync` for bulk fan-out).
6. **Inspect** `SyncStatus`, `SyncStatusMessage`, `LastSyncDate`, `LastReconciliationReport`, `LastRunAlerts`.
7. **Persist** with `SaveSchemasAsync()` or `SaveVersionedSchemaAsync(schema, version)`.

---

## Key `DataSyncSchema` Properties

| Property | Type | Purpose |
|----------|------|---------|
| `SourceDataSourceName` / `DestinationDataSourceName` | string | Registered datasource names |
| `SourceEntityName` / `DestinationEntityName` | string | Entity / table names |
| `SourceKeyField` / `DestinationKeyField` | string | Primary key fields |
| `SourceSyncDataField` | string | Watermark / tracking field for incremental |
| `SyncType` | string | `"Full"` \| `"Incremental"` |
| `SyncDirection` | string | `"OneWay"` \| `"Bidirectional"` |
| `BatchSize` | int | Records per batch |
| `WatermarkPolicy` | `WatermarkPolicy` | Incremental / CDC strategy |
| `ConflictPolicy` | `ConflictPolicy` | Bidirectional conflict resolution |
| `RetryPolicy` | `RetryPolicy` | Retry / back-off / idempotency |
| `DqPolicy` | `DqPolicy` | DQ gate rules and reject threshold |
| `SloProfile` | `SloProfile` | SLO targets and alert rule keys |
| `PerfProfile` | `SyncPerformanceProfile` | Parallelism, rule policy mode, defaults cache TTL |
| `ActiveCheckpoint` | `SyncCheckpoint` | Resume offset from last run |
| `CurrentSchemaVersion` | `SyncSchemaVersion` | Versioned mapping snapshot |
| `LastReconciliationReport` | `SyncReconciliationReport` | Row-count reconciliation output |
| `LastRunAlerts` | `List<SyncAlertRecord>` | Alert records raised last run |

---

## Pitfalls

- **Missing key fields**: `SourceKeyField` / `DestinationKeyField` must be set; omission produces duplicates or missed updates.
- **Bidirectional without reverse mappings**: Every destination→source field pair must be safe to invert; use `ConflictPolicy.SourceWins` conservatively at first.
- **WatermarkPolicy + Full sync mix**: Setting `SyncType = "Incremental"` without a `WatermarkPolicy` (or vice-versa) will skip watermark filtering.
- **DQ threshold abort**: If `DqPolicy.RejectThreshold` is too low, a noisy source will abort every run. Tune with a first `Full` run before enforcing.
- **Defaults profile warmup on cold start**: `PerfProfile.WarmUpDefaultsProfileOnRun = true` logs a warning if `DefaultsManager` has no profile registered — this is expected and non-fatal.
- **Parallel sync side-effects**: `SyncAllDataParallelAsync` runs schemas concurrently; schemas sharing a destination datasource may interleave writes. Use schema-level locks or order sequentially for shared destinations.
- **Forgetting to save**: `LastReconciliationReport`, `LastRunAlerts`, and updated `ActiveCheckpoint` are in-memory until `SaveSchemasAsync` is called.

---

## Related Skills
- [`importing`](../importing/SKILL.md)
- [`etl`](../etl/SKILL.md)
- [`mapping`](../mapping/SKILL.md)

## Detailed Reference
See [`reference.md`](./reference.md) for code examples covering every phase.
