---
name: beepdm-importing
description: Use when running data import operations in BeepDM — bulk moving rows from a source datasource to a destination, with batch processing, validation, transformation, retry, and error-store replay. Hands off to Schema Migration (preflight) and BeepSync (DataSyncSchema execution) skills.
---

# beepdm-importing

For owned run definitions use Models `EntityMetadataSnapshot.Capture`, not the
legacy shallow `EntityStructure.Clone`. Capture rejects unsupported/cyclic/oversized
graphs; do not fall back on failure. Coordinate capture and keep its mutable result
private. Read DataManagementModelsStandard/DataBase/METADATA-SNAPSHOTS.md; the API
does not automatically wire import/sync metadata admission. Engine generation now
uses source-sensitive exact type identity; bare cache seeds cannot override fields.
Read Engine ConfigUtil/GENERATED-TYPES.md for retention and compatibility limits.

`DataImportManager` is the **data-movement** service in BeepDM. It does the actual work of pulling rows from a source datasource and writing them to a destination — in batches, with validation, transformation, retry, and progress reporting.

## When to use this skill

- Importing rows from one datasource to another.
- Building batch-import pipelines with validation + transformation.
- Replaying records that previously failed into the error store.
- Configuring pause / resume / cancel behaviour for long imports.

## Do NOT use this skill for

- Validating schema compatibility before the import → use **beepdm-schema** (the dedicated preflight service).
- Cross-datasource schema alignment planning → use **beepdm-schema**.
- Executing a `DataSyncSchema` uses `BeepSyncManager`; use **beepdm-sync** for checkpoint acknowledgement, storage and diagnostic boundaries rather than assuming importing owns sync governance.
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
- **Batch** — splitting and per-record retry without replaying acknowledged rows
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

- Built-in required transforms return typed ImportTransformationResult through
  optional IDataImportTransformationOutcome. Null/failed results never write input.
  RecordsTransformationFailed is part of RecordsFailed, not WriteAttempts or
  HasUncertainWrites; preserve earlier acknowledgements and cancellation outcomes.
  Legacy built-in stage methods now throw safe ImportTransformationException.
- Strict field mapping has no hidden default stage and defaults to Reject unless
  a conversion fallback/warning policy was explicitly registered. Use the configured
  ApplyDefaults stage; custom helpers must not conceal failures.
- Normal-write QualityRules/sync record DQ now use captured dedicated admission
  after transforms/defaults and before writes, once per row rather than retry.
  Required failures deny; explicit Advisory/Warn are counted. Quarantine requires
  acknowledged saves and reject-store failures never admit target writes. Read
  Editor/Importing/QUALITY-ADMISSION.md for counts, trusted plugin/serialization
  limits and open durable reject gates. Sync thresholds/failed counts have a
  separate contract in BeepSync/THRESHOLDS-AND-FAILURE-EVIDENCE.md. Required catalog
  admission and editor-owned row resolution now deny failures/reported fallbacks,
  normalize once and bypass metadata-only caches. Read DEFAULTS-ADMISSION.md for
  safe counts/diagnostics and remaining full intent/grammar/plugin/provider gates.
  Caller configuration stays unpopulated; each run refreshes implicit catalogs.
  Closed bounded literals and per-row byte copies are owned. Both declared sync
  catalogs, including empty ones, are captured before provider-opening validation;
  that does not pre-evaluate reverse row rules or freeze every policy/context.
  Required roster/priorities are pinned before reads/preflight for both directions;
  fresh runs see registration changes. Per-row SentData/named lookups retain owned
  definitions. Nested wrappers keep failures sticky, reject foreign editors and
  omit raw telemetry; captured plugin instances remain trusted mutable code.
  Required shipped outer grammar routes by operator and rejects ignored arity/empty
  slots/invalid scope names. Nested date arguments work; shipped time/hash SEQUENCE/
  INCREMENT demonstrations deny required rows. Use a qualified allocator plugin;
  this does not qualify query/identity/provider semantics or freeze values.
  Required-only dot parsing preserves quoted/empty literals and denies missing
  segments. Dots outside quotes/nested calls separate arguments; quote decimals.
  Grouping quotes retain declared expression/mode/filter roles; public legacy
  parsing is unchanged. Read DEFAULTS-ADMISSION.md before modifying DSL contracts.
  Exact shipped required expressions/formulas use bounded pre-read AST parsing,
  Boolean-only conditions, precedence, typed exact comparisons, invariant numbers
  and lazy branches. Bare names read flat row fields; quote labels. Nested custom
  overrides retain pinned selection/failure/cancellation. Unused branches are
  syntax-checked, not semantically evaluated; numeric/provider/plugin gates remain.
  Exact shipped required datasource queries validate every filter before callbacks,
  bind closed invariant actual row/named values and retain typed aggregates without
  skipping malformed values. Null collections deny; real empty COUNT/EXISTS are
  0/false. Streaming limits/cancellation/root-status/disposal do not qualify provider
  allocation, hidden errors, isolation or ownership. Supply query context explicitly.
  Shipped required identity/scope rules require explicit string email/role context
  and actual supported OS identity/folders, not row identity or inferred substitutes.
  ENV reads only its scope; SYSTEMPATH is Machine PATH. Host values are not
  authorization proof or immutable run identity; read defaults admission for limits.
  Required shipped dates use ISO literals and invariant bounded formatting, not
  host-culture parsing. Nested bases must be actual DateTime/DateTimeOffset values
  from the pinned registry; kind/offset and exact tick/calendar arithmetic are
  retained. Clock leaves stay dynamic; this is not immutable time or DST policy.
  Required configuration uses explicit named flat string maps or declared Process
  prefixes, never bare-variable/editor/row inference. Missing/invalid explicit
  sources cannot fall back; connection alias conflicts deny. Host bridging and
  immutable run configuration remain separate work. See defaults admission.
  Bound mapped sync validates captured provider pairs and
  required targets with both destination field lists populated. Missing targets
  reject instead of inferring mapped DDL; unbound imports remain legacy. Read
  Editor/Importing/TRANSFORMATION-OUTCOMES.md and BeepSync/STORAGE-AND-OUTCOMES.md;
  a standalone quality helper or a mock transform is not admission verification.

- The current import helper writes directly through `IDataSource.InsertEntity`;
  it does not guarantee a transaction or honor native upsert automatically.
- `RunImportAsync` returns `ImportExecutionResult` through its existing
  `IErrorsInfo` signature. Check `Outcome`, acknowledged `RecordsSucceeded`,
  `RecordsFailed`, `RecordsSkipped`, and `WriteAttempts`; only Completed maps to Ok.
- Partial failure, exhausted retries, and cancellation are not success, even when
  `OnBatchError=Skip`. Skipped records remain included in the failed count.
- Retry only explicitly failed records. Exceptions/null/ambiguous acknowledgements
  can mean a write was applied; `HasUncertainWrites` blocks blind replay.
- BeepSync does not advance a cursor on failed/cancelled imports and does not
  automatically replay a whole sync after acknowledged or uncertain writes.
- The current BeepSync CDC executor accepts only Timestamp mode, with a typed
  DateTime/DateTimeOffset cursor and a bounded read window. Sequence/CompositeKey
  are rejected before writes, not silently converted to timestamps.
- Validation runs **before** any data movement; a failed validation returns `IErrorsInfo` with `Flag=Failed`.
- Set BatchSize to a positive value; current configuration validation rejects
  non-positive sizes before the later automatic-size fallback is reached.
- Pause / resume / cancel use `ManualResetEventSlim` + `CancellationTokenSource` — do not block on the import thread.
- Durable file replay requires optional IImportRejectRecoveryStore, operator
  preparation and a persisted CAS claim before one actual provider write. Use
  GetRejectContextKey/RejectId/revision, not indexes. Replay destination snapshots
  with current metadata/quality, not transforms/defaults again. Missing/legacy
  stores and empty policy fail closed. Uncertain/claimed rows need reconciliation;
  only acknowledged writes mark Replayed. Read Editor/Importing/REJECT-RECOVERY.md.
- File error/history stores coordinate JSONL updates across instances/processes
  and reject corrupt or foreign-context records instead of resetting to empty.
- FileWatermarkStore uses a versioned typed envelope and hashed context filenames.
  Scalar and nested dictionary/array values round-trip; unsupported types fail.
  Composite cursor storage does not enable unsupported BeepSync execution modes.
- All three file stores retain their default constructors and add folder overloads.
  Legacy sanitized files require explicit identity/type migration and are not
  silently guessed or overwritten. Never delete `.beep.lock` files to unlock them.
  See `Services/Persistence/README.md`; whole-file JSONL updates are not a scalable
  retention backend or an exactly-once replay mechanism.

## Cross-references

- See **beepdm-schema** for the preflight / draft service that handles schema alignment planning.
- See **beepdm-retry** for the shared `IRetryPipeline` primitive. New retryable import operations should compose it. The BeepSync runtime executor (which sits on top of importing) is documented inline in the source (`Editor/BeepSync/BeepSyncManager.Sync.cs:140`); there is no `beepdm-beepsync` skill.
- See **beepdm-migration** for the DDL-on-one-datasource counterpart.
- See **beepdm-etl** for the pipeline engine that orchestrates larger flows.
