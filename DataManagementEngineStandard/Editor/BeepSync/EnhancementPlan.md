# BeepSync Enhancement Plan — `BeepSyncManager` as Import Orchestrator

**Date:** 2026-03-03  
**Scope:** `DataManagementEngineStandard/Editor/BeepSync/`

---

## 1. Core Architectural Decision

`DataImportManager` (in `Editor/Importing/`) already implements everything needed for the actual data transfer:

| Concern | DataImportManager provides |
|---------|---------------------------|
| Batch processing | `BatchSize`, `MaxRetries`, `OnBatchError` |
| Incremental sync | `SyncMode.Incremental`, `WatermarkColumn`, `IWatermarkStore` |
| Error store | `IImportErrorStore`, `JsonFileImportErrorStore` |
| Run history | `IImportRunHistoryStore`, `ImportRunRecord` |
| Data quality | `IDataQualityRule`, `NotNullRule`, `UniqueRule`, `RangeRule`, `RegexRule` |
| Schema drift | `SchemaDriftPolicy`, `SchemaComparator` |
| Staging | `StagingOptions` |
| Replay | `ReplayFailedRecordsAsync` |
| Preflight | `RunMigrationPreflightAsync` |
| Profiling | `DataProfiler.ProfileAsync` |
| Field mapping | `TransformationHelper` — mapping, defaults, custom transform |
| Validation | `ValidationHelper.ValidateImportConfiguration` |
| Progress | `ProgressHelper`, `GetImportStatus()`, `ImportStatus` |

**`BeepSyncManager` must not reimplement any of the above.**

`BeepSyncManager` is the **sync orchestrator**: it manages `DataSyncSchema` objects (bidirectional, multi-entity, scheduled), translates each schema into a `DataImportConfiguration`, and delegates execution to `DataImportManager`. It is the scheduler and coordinator, not the transfer engine.

```
DataSyncSchema  ──▶  BeepSyncManager  ──▶  DataImportConfiguration  ──▶  DataImportManager
(sync contract)       (orchestrator)         (import contract)              (transfer engine)
```

---

## 2. Existing Infrastructure to Reuse

### `DataImportManager` (`Editor/Importing/`)
The execution engine. `BeepSyncManager` creates one per sync run and calls `RunImportAsync`.

### `DataSourceLifecycleHelper` (`Helpers/DataSourceLifecycleHelper.cs`)
- `OpenWithRetryAsync(ds, maxRetries)` — connection validation before sync starts
- `ValidateDataSourceAsync(ds)` — pre-flight datasource health check

### `ErrorHandlingHelper` (`Helpers/ErrorHandlingHelper.cs`)
- `ExecuteWithErrorHandlingAsync<T>` — wraps sync orchestration steps
- `HandleException(ex, context, editor)` — all catch blocks
- `CreateErrorInfo(ex, context)` — builds `IErrorsInfo` failure results
- `TryExecute(action, context, editor)` — fire-and-forget cleanup (log sync run)

---

## 3. Current State

| File | Role | State |
|------|------|-------|
| `BeepSyncManager.Orchestrator.cs` | Modern orchestrator | **Done** |
| `Interfaces/ISyncHelpers.cs` | Helper interfaces | **Done** |
| `Helpers/FieldMappingHelper.cs` | AutoMapFields only — MapFields/CreateDestinationEntity removed | **Done** |
| `Helpers/SyncValidationHelper.cs` | Uses `DataSourceLifecycleHelper.OpenWithRetryAsync` | **Done** |
| `Helpers/SyncProgressHelper.cs` | Progress + logging via `LogMessage` wrapper | **Done** |
| `Helpers/SchemaPersistenceHelper.cs` | Fully async JSON schema load/save | **Done** |
| `Helpers/SyncSchemaTranslator.cs` | Translates `DataSyncSchema` → `DataImportConfiguration` | **Done** |
| `Helpers/DataSourceHelper.cs` | `IDataSourceHelper` implementation | **Done** |
| `SyncMetrics.cs` | Sync run metrics; `FromImportRunRecord` factory | **Done** |
| `DataSyncSchema.cs` | BatchSize, RunPreflight, ConflictResolutionStrategy, DriftPolicy added | **Done** |

---

## 4. Phased Execution Plan

### ✅ Phase 1 — `DataSyncSchema` → `DataImportConfiguration` Translator
**File:** `Helpers/SyncSchemaTranslator.cs`  
Converts any `DataSyncSchema` into a `DataImportConfiguration` ready for `DataImportManager.RunImportAsync`. Handles forward and reverse (bidirectional) configurations. Field mappings translated to `EntityDataMap`.

---

### ✅ Phase 2 — Create `DataSourceHelper`
**File:** `Helpers/DataSourceHelper.cs`  
Implements `IDataSourceHelper`. Delegates to `DataSourceLifecycleHelper` for connection validation; all data operations wrapped in `ErrorHandlingHelper.ExecuteWithErrorHandlingAsync`.

---

### ✅ Phase 3 — Enhance `SyncValidationHelper`
**File:** `Helpers/SyncValidationHelper.cs`  
Pre-flight checks before handing off to `DataImportManager`.
- `ValidateSchema` — required field checks + mapped field non-empty validation.
- `ValidateDataSource` — uses `DataSourceLifecycleHelper.OpenWithRetryAsync(ds, 3)`.
- `ValidateSyncOperation` — combines schema + both datasources + entity existence + sync field presence.

---

### ✅ Phase 4 — Create `BeepSyncManager` Class
**File:** `BeepSyncManager.Orchestrator.cs`  
Modern orchestrator. Manages schemas; delegates transfers to `DataImportManager`.
- `SyncDataAsync` — validate → translate → preflight (if `RunPreflight`) → run import → bidirectional second pass → update schema state.
- `SyncAllDataAsync` — iterates all schemas with cancellation support.
- Schema management: `AddSyncSchema`, `RemoveSyncSchema`, `UpdateSyncSchema`, `AddFilter`, `RemoveFilter`, `AddFieldMapping`.
- Persistence: `SaveSchemasAsync`/`LoadSchemasAsync` delegated to `SchemaPersistenceHelper`.

---

### ✅ Phase 5 — `DataSyncSchema` Model Additions
**File:** `DataManagementModelsStandard/Editor/DataSyncSchema.cs`  
Added 6 new properties with defaults — existing serialized schemas deserialize without error:

```csharp
public int    BatchSize                    { get; set; } = 0;
public bool   RunPreflight                 { get; set; } = false;
public string ConflictResolutionStrategy   { get; set; } = "SourceWins";
public bool   CreateDestinationIfNotExists { get; set; } = false;
public bool   AddMissingColumns            { get; set; } = false;
public string DriftPolicy                  { get; set; } = "Ignore";
```

---

### ✅ Phase 6 — Async `SchemaPersistenceHelper`
**File:** `Helpers/SchemaPersistenceHelper.cs`  
`SaveSchemasAsync` / `LoadSchemasAsync` use `File.WriteAllTextAsync` / `File.ReadAllTextAsync`. Path: `AppData/TheTechIdea/Beep/BeepSyncManager/SyncSchemas.json`. Synchronous `SaveSchemas()` / `LoadSchemas()` are thin wrappers.

---

### ✅ Phase 7 — `SyncMetrics` ↔ `ImportRunRecord` Mapping
**File:** `SyncMetrics.cs`  
Added static factory:

```csharp
public static SyncMetrics FromImportRunRecord(ImportRunRecord record) => new SyncMetrics
{
    SchemaID          = record.ContextKey,
    SyncDate          = record.StartedAt,
    TotalRecords      = (int)record.RecordsRead,
    SuccessfulRecords = (int)record.RecordsWritten,
    FailedRecords     = (int)record.RecordsBlocked,
    Duration          = record.FinishedAt.HasValue ? record.FinishedAt.Value - record.StartedAt : TimeSpan.Zero
};
```

---

### ✅ Phase 8 — Remove Superseded Helper Code
Removed logic that `DataImportManager` now owns:

| File | Action |
|------|--------|
| `Helpers/FieldMappingHelper.cs` | Removed `MapFields`, `CreateDestinationEntity` — delegated to `TransformationHelper`. Kept `AutoMapFields` (still useful for building `schema.MappedFields`). |
| `Interfaces/ISyncHelpers.cs` | Removed `MapFields` and `CreateDestinationEntity` from `IFieldMappingHelper`. |
| `DataSyncManager.cs` | **Deleted** — no legacy wrapper needed. `BeepSyncManager` is the only sync manager. |

---

### Phase 9 — Regression & Validation
Checklist:
- [ ] `SyncSchemaTranslator.ToImportConfiguration` produces valid config for Full, Incremental, and Upsert modes
- [ ] `BeepSyncManager.SyncDataAsync` transfers all records (not just one) for a simple full-refresh sync
- [ ] Bidirectional sync runs two `DataImportManager` passes; source and destination are swapped correctly
- [ ] `SyncMetrics.FromImportRunRecord` returns correct counts from a completed run
- [ ] `RunPreflight = true` runs migration preflight and blocks sync on failure
- [ ] `BatchSize` on schema is forwarded to `config.BatchSize`
- [ ] `SchemaPersistenceHelper.SaveSchemasAsync` does not block the calling thread
- [ ] `CancellationToken` propagated to `DataImportManager.RunImportAsync`
- [ ] New `DataSyncSchema` properties deserialize correctly from old JSON (defaults applied)

---

## 5. Implementation Order

```
Phase 1  (SyncSchemaTranslator)        ← DONE
    → Phase 2  (DataSourceHelper)      ← DONE
    → Phase 3  (SyncValidationHelper)  ← DONE
    → Phase 4  (BeepSyncManager)       ← DONE
    → Phase 5  (DataSyncSchema model)  ← DONE
    → Phase 6  (async persistence)     ← DONE
    → Phase 7  (SyncMetrics mapping)   ← DONE
    → Phase 8  (cleanup)               ← DONE
    → Phase 9  (regression gate)       ← PENDING
```

---

## 6. Done Criteria

| # | Criterion |
|---|-----------|
| 1 | `BeepSyncManager` class exists, compiles, and uses `DataImportManager` as its transfer engine. |
| 2 | `SyncSchemaTranslator.ToImportConfiguration` converts any `DataSyncSchema` to a valid `DataImportConfiguration`. |
| 3 | `SyncDataAsync` transfers **all** records; single-record bug is eliminated. |
| 4 | Bidirectional sync is implemented (two `DataImportManager` passes). |
| 5 | `SyncDataAsync` returns `IErrorsInfo` — callers can inspect success or failure. |
| 6 | `DataSourceHelper` implements `IDataSourceHelper`; delegates to `DataSourceLifecycleHelper`. |
| 7 | `SyncValidationHelper` delegates datasource health checks to `DataSourceLifecycleHelper.OpenWithRetryAsync`. |
| 8 | `SyncMetrics.FromImportRunRecord` maps `ImportRunRecord` → `SyncMetrics`. |
| 9 | `SchemaPersistenceHelper.SaveSchemasAsync` / `LoadSchemasAsync` are fully async. |
| 10 | No business logic is duplicated between BeepSync helpers and `DataImportManager` helpers. |
| 11 | Regression checklist (Phase 9) passes. |
