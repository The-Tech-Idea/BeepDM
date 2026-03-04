---
name: importing
description: Guidance for DataImportManager usage — configuration, incremental sync, data quality, schema drift, error store, run history, staging, profiling, preflight, and replay in BeepDM.
---

# Data Import Guide

Use this skill when importing data from external sources, mapping fields, running batched imports, enforcing data quality, detecting schema drift, or replaying failed records.

## Architecture

`DataImportManager` is a partial-class orchestrator split across:
- `DataImportManager.cs` — constructor, properties, backward-compat API
- `DataImportManager.Core.cs` — data source init, entity fetching, batch execution
- `DataImportManager.Migration.cs` — preflight validation, sync-draft generation
- `DataImportManager.Replay.cs` — dead-letter replay of quarantined records

Four helper properties are always available:
- `ValidationHelper` (`IDataImportValidationHelper`) — config/structure/datasource checks
- `TransformationHelper` (`IDataImportTransformationHelper`) — field filter, mapping, defaults, custom transform
- `BatchHelper` (`IDataImportBatchHelper`) — optimal batch size, retry logic
- `ProgressHelper` (`IDataImportProgressHelper`) — logging, metrics, error list

## Core Types

| Type | Purpose |
|---|---|
| `DataImportManager` | Main orchestrator |
| `DataImportConfiguration` | Full import config (all phases) |
| `ImportContext` | Typed serialisable entry point |
| `ImportStatus` | Live thread-safe snapshot (State, counts, Metrics) |
| `ImportRunRecord` | Audit record written to run history after each run |
| `ImportErrorRecord` | Single quarantined/blocked record in the error store |
| `StagingOptions` | Controls staging entity before normalization |
| `DataProfile` / `FieldProfile` | Per-entity field statistics from `DataProfiler.ProfileAsync` |
| `SchemaSnapshot` / `SchemaDriftReport` | Before/after schema comparison |

## Workflow

1. Create `DataImportManager(editor)`.
2. Create config with `CreateImportConfiguration(...)` or build an `ImportContext`.
3. Optionally: attach `QualityRules`, `ErrorStore`, `RunHistoryStore`, `Staging`.
4. Optionally: call `RunMigrationPreflightAsync(config)` before executing.
5. Run `RunImportAsync(config, progress, token)` or `RunImportAsync(context, progress, token)`.
6. Inspect `GetImportStatus()` or `RunHistoryStore` for results.
7. Replay failures with `ReplayFailedRecordsAsync(contextKey, progress, token)`.

## DataImportConfiguration — Key Properties

```
Basic:        SourceEntityName, SourceDataSourceName, DestEntityName, DestDataSourceName
Filtering:    SourceFilters (List<AppFilter>), SelectedFields
Transform:    CustomTransformation, Mapping (EntityDataMap), DefaultValues, ApplyDefaults
Batching:     BatchSize (default 50), MaxRetries (default 3), OnBatchError (Skip/Abort)
Schema:       CreateDestinationIfNotExists, AddMissingColumns, DriftPolicy
Preflight:    RunMigrationPreflight, CreateSyncProfileDraft
Sync:         SyncMode (FullRefresh/Incremental/Upsert), WatermarkColumn, LastWatermarkValue, UpsertKeyColumns
Quality:      QualityRules (List<IDataQualityRule>)
Error store:  ErrorStore (IImportErrorStore)
History:      RunHistoryStore (IImportRunHistoryStore)
Staging:      Staging (StagingOptions)
```

## Basic Import

```csharp
using var importManager = new DataImportManager(editor);

var config = importManager.CreateImportConfiguration(
    "LegacyCustomers", "LegacyDB", "Customers", "MainDB");

config.SelectedFields = new List<string> { "Name", "Email" };
config.BatchSize = 200;
config.ApplyDefaults = true;

var validation = importManager.ValidationHelper.ValidateImportConfiguration(config);
if (validation.Flag == Errors.Ok)
    await importManager.RunImportAsync(config, progress, CancellationToken.None);
```

## Preflight (Migration.cs)

```csharp
var pre = await importManager.RunMigrationPreflightAsync(config, msg => Console.WriteLine(msg));
if (pre.Flag != Errors.Ok) return;
```

## Incremental Sync (Phase 6)

```csharp
config.SyncMode         = SyncMode.Incremental;
config.WatermarkColumn  = "UpdatedAt";
config.LastWatermarkValue = await watermarkStore.LoadWatermarkAsync(contextKey);
// Watermark stores: FileWatermarkStore (disk) · InMemoryWatermarkStore (tests)
```

## Data Quality Rules (Phase 7)

```csharp
config.QualityRules.Add(new NotNullRule("Email",   DataQualityAction.Block));
config.QualityRules.Add(new UniqueRule("OrderId",  DataQualityAction.Quarantine));
config.QualityRules.Add(new RangeRule("Age", 0, 150, DataQualityAction.Warn));
config.QualityRules.Add(new RegexRule("PostCode", @"^[A-Z]{1,2}\d", DataQualityAction.Block));
// Actions: Block (skip+store) · Quarantine (skip+store) · Warn (pass+count)
```

## Schema Drift (Phase 8)

```csharp
config.DriftPolicy = SchemaDriftPolicy.AutoAddColumns; // or Strict / Ignore
// SchemaComparator.Compare(baseline, current) → SchemaDriftReport
```

## Error Store & Replay (Phase 9)

```csharp
config.ErrorStore = new JsonFileImportErrorStore();
// Later: replay all quarantined records
await importManager.ReplayFailedRecordsAsync(contextKey, progress, token);
```

## Run History (Phase 10)

```csharp
config.RunHistoryStore = new JsonFileImportRunHistoryStore();
var history = await config.RunHistoryStore.LoadAsync(contextKey, token);
// ImportRunRecord has: RunId, StartedAt, FinalState, RecordsRead/Written/Blocked, FinalWatermark
```

## Staging (Phase 11)

```csharp
config.Staging = new StagingOptions { Enabled = true, StagingEntitySuffix = "_raw", DropStagingAfterNormalize = true };
```

## Data Profiling

```csharp
var profile = await DataProfiler.ProfileAsync(editor, "SourceDB", "Customers", sampleSize: 500);
foreach (var f in profile.Fields)
    Console.WriteLine($"{f.FieldName}: nulls={f.NullCount}, distinct={f.DistinctCount}");
```

## Lifecycle Control

```csharp
importManager.PauseImport();
importManager.ResumeImport();
importManager.CancelImport();
var status = importManager.GetImportStatus(); // thread-safe snapshot
```

## Pitfalls
- `BatchSize` defaults to 50 — tune up for large datasets.
- `UniqueRule` holds state per-instance; recreate it for each run.
- `JsonFileImportErrorStore` / `JsonFileImportRunHistoryStore` use `EnvironmentService.CreateAppfolder("Importing")` — ensure BeepRoot is configured.
- Without `RunHistoryStore`, watermarks must be managed manually.
- Always call `ValidateImportConfiguration` (not the old `ValidateConfiguration`) — signature changed.

## File Locations
```
DataManagementEngineStandard/Editor/Importing/
  DataImportManager.cs                  — main class, helpers, backward-compat props
  DataImportManager.Core.cs             — data source ops, batch execution
  DataImportManager.Migration.cs        — preflight, sync-draft
  DataImportManager.Replay.cs           — dead-letter replay
  Interfaces/IDataImportInterfaces.cs   — all interfaces + DataImportConfiguration + ImportContext
  Helpers/                              — Validation/Transformation/Batch/Progress helpers
  Quality/                              — IDataQualityRule, DataQualityEvaluator, BuiltInRules
  Schema/                               — SchemaComparator, SchemaSnapshot
  ErrorStore/                           — IImportErrorStore, JsonFileImportErrorStore
  History/                              — IImportRunHistoryStore, ImportRunRecord
  Staging/                              — StagingOptions
  Sync/                                 — IWatermarkStore, FileWatermarkStore
  Profiling/                            — DataProfiler, FieldProfile
```

