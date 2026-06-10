---
name: beepdm-importing
description: Use when running data import operations in BeepDM — bulk moving rows from a source datasource to a destination, with batch processing, validation, transformation, retry, and error-store replay. Hands off to Schema Migration (preflight) and BeepSync (DataSyncSchema execution) skills.
---

# beepdm-importing

`DataImportManager` is the **data-movement** service in BeepDM. It does the actual work of pulling rows from a source datasource and writing them to a destination — in batches, with validation, transformation, retry, and progress reporting.

## When to use this skill

- Importing rows from one datasource to another.
- Building batch-import pipelines with validation + transformation.
- Replaying records that previously failed into the error store.
- Configuring pause / resume / cancel behaviour for long imports.

## Do NOT use this skill for

- Validating schema compatibility before the import → use **beepdm-schema** (the dedicated preflight service).
- Cross-datasource schema alignment planning → use **beepdm-schema**.
- Executing a `DataSyncSchema` (the persistent, governed, retried sync contract) → use **beepdm-beepsync** (`BeepSyncManager`).
- DDL on a single datasource → use **beepdm-migration**.

## File Locations

`DataManagementEngineStandard/Editor/Importing/`:

- `DataImportManager.cs` — main orchestrator (lifecycle, mode, dispose)
- `DataImportManager.Core.cs` — data source operations, entity management, fetching
- `DataImportManager.Replay.cs` — dead-letter replay (Phase 9)
- `DataImportManager.Migration.cs` — **back-compat shims** that delegate to `ISchemaManager`
- `Interfaces/IDataImportInterfaces.cs` — `IDataImportManager` contract + helpers
- `Helpers/` — `DataImportValidationHelper`, `DataImportTransformationHelper`, `DataImportBatchHelper`, `DataImportProgressHelper`
- `Schema/` — `SchemaSnapshot`, `SchemaComparator`, `SchemaSnapshotStore` (used by both import and the new schema-migration service)
- `Sync/` — watermark store for incremental sync
- `Quality/` — data-quality rules + evaluator
- `History/` — run-history store

## Public API

```csharp
var dm = new DataImportManager(dmeEditor);

var cfg = dm.CreateImportConfiguration("Customers", "Northwind", "DimCustomer", "Warehouse");
cfg.AddMissingColumns = true;
cfg.CreateDestinationIfNotExists = true;
cfg.OnBatchError = BatchErrorStrategy.Retry;
cfg.MaxRetries = 3;

var status = await dm.RunImportAsync(cfg, progress, token);
```

## Helpers (composed, not duplicated)

`DataImportManager` composes four helpers:

- **Validation** — config + entity mapping + data source + entity compatibility
- **Transformation** — field filtering, entity mapping, defaults, custom transform
- **Batch** — optimal batch size, splitting, per-batch processing with retry
- **Progress** — logging + `IProgress<IPassedArgs>` reporting

## Schema-migration shim (the new thing)

`IDataImportManager.RunMigrationPreflightAsync` and `BuildSyncDraftAsync` are now **back-compat shims** that delegate to `ISchemaManager`. New code should call the schema-migration service directly — it doesn't need the import manager.

## How this skill works with the rest of the data-management layer

| Direction | Layer | What flows |
|---|---|---|
| → **schema** | `ISchemaManager` | The two schema-related methods on `IDataImportManager` are now thin shims; the import manager delegates preflight + draft to the unified service. |
| ↔ **beepsync** | `BeepSyncManager` | A `DataSyncSchema` produced by the schema-migration service can be executed by `BeepSyncManager`; `DataImportManager` is the lower-level data mover for one-off imports. |
| → **migration** | `MigrationManager` | The import manager may need the destination schema to exist before importing. It calls `EnsureDestinationEntityExists` (via `IDataSource.CreateEntityAs`) for the simple case; for complex DDL it should call `MigrationManager`. |
| ← **etl** | `ETLEditor` | `ETLEditor.TryRunImportingPreflightAsync` historically called the import manager; it now calls `ISchemaManager` directly. |
| ← **setup** | Setup Framework | Phase 6 (seeding) may invoke the import manager for non-trivial initial data loads. |
| ← **configuration** | `ConfigEditor` | Reads connections / mappings through `IDMEEditor`. |

## Design Rules

- All write paths go through UoW when the destination needs transactional semantics.
- Validation runs **before** any data movement; a failed validation returns `IErrorsInfo` with `Flag=Failed`.
- Batch size is auto-calculated when not specified (`_batchHelper.CalculateOptimalBatchSize`).
- Pause / resume / cancel use `ManualResetEventSlim` + `CancellationTokenSource` — do not block on the import thread.
- `ReplayFailedRecordsAsync` reads from `IImportErrorStore`; never silently swallow errors.

## Cross-references

- See **beepdm-schema** for the preflight / draft service that handles schema alignment planning.
- See **beepdm-retry** for the shared `IRetryPipeline` primitive. New retryable import operations should compose it. The BeepSync runtime executor (which sits on top of importing) is documented inline in the source (`Editor/BeepSync/BeepSyncManager.Sync.cs:140`); there is no `beepdm-beepsync` skill.
- See **beepdm-migration** for the DDL-on-one-datasource counterpart.
- See **beepdm-etl** for the pipeline engine that orchestrates larger flows.
