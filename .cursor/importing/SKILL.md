---
name: importing
description: Guidance for DataImportManager usage in BeepDM, including configuration, incremental sync, data quality, schema drift, error store, run history, staging, profiling, and replay. Use when you need governed import execution rather than lower-level ETL script orchestration.
---

# Data Import Guide

Use `EntityMetadataSnapshot.Capture` (Models) when binding owned run metadata;
legacy `EntityStructure.Clone` shares collections. Capture rejects unsupported,
cyclic and oversized graphs; never fall back to caller metadata after failure.
The result is mutable: coordinate capture and keep it private. Read
`DataManagementModelsStandard/DataBase/METADATA-SNAPSHOTS.md`. Normal import/sync
metadata capture is not automatically wired by this API alone. Sync now binds
captured provider metadata for existing-target mapped admission; ordinary unbound
imports and policy/context capture remain separate work.
Engine generated targets now use requested-name/source identity, not bare cache
overrides. Read Engine ConfigUtil/GENERATED-TYPES.md for compatibility/cache limits;
read Editor/BeepSync/STORAGE-AND-OUTCOMES.md for actual mapped-sync admission limits.

Use this skill when importing data through `DataImportManager` and its helper ecosystem.

## Durable Reject Recovery

Read Engine `Editor/Importing/REJECT-RECOVERY.md` before triage/replay. Use
`GetRejectContextKey` and durable RejectId/revision, not batch/index coordinates.
Operator preparation and a persisted exclusive claim precede one actual write;
only Errors.Ok acknowledges recovery. Replay uses the destination snapshot/current
metadata and quality, never reruns transforms/defaults. Missing/legacy stores and
empty quality policy fail closed. Claimed/uncertain writes remain blocked across
restart until authorized provider reconciliation; never auto-expire them.
Inspect ImportRejectReplayResult counts/persistence/reconciliation. File recovery
is not a native-provider transaction, full sync completion or exactly-once guarantee.

## Use this skill when
- Running governed imports with validation, transformation, batching, and progress tracking
- Managing replay, error stores, run history, watermarks, staging, or profiling
- Building sync/import flows that need policy-driven behavior

## Do not use this skill when
- The task is primarily schema/data copy via ETL scripts or direct datasource ETL helpers. Use [`etl`](../etl/SKILL.md).
- The task is specifically sync-schema orchestration. Use [`beepsync`](../beepsync/SKILL.md).

## Architecture
- `DataImportManager` is a partial-class orchestrator.
- Helpers:
  - `ValidationHelper`
  - `TransformationHelper`
  - `BatchHelper`
  - `ProgressHelper`
- Additional modules:
  - `ErrorStore`
  - `History`
  - `Quality`
  - `Schema`
  - `Sync` watermark storage
  - `Profiling`
  - `Staging`

## File Locations
- `DataManagementEngineStandard/Editor/Importing/DataImportManager.cs`
- `DataManagementEngineStandard/Editor/Importing/DataImportManager.Core.cs`
- `DataManagementEngineStandard/Editor/Importing/DataImportManager.Migration.cs`
- `DataManagementEngineStandard/Editor/Importing/DataImportManager.Replay.cs`
- `DataManagementEngineStandard/Editor/Importing/Interfaces/IDataImportInterfaces.cs`
- `DataManagementEngineStandard/Editor/Importing/Helpers/`
- `DataManagementEngineStandard/Editor/Importing/Quality/`
- `DataManagementEngineStandard/Editor/Importing/ErrorStore/`
- `DataManagementEngineStandard/Editor/Importing/History/`
- `DataManagementEngineStandard/Editor/Importing/Sync/`
- `DataManagementEngineStandard/Editor/Importing/Profiling/`

## Working Rules
- Built-in transformations are strict. The batch helper prefers optional
  `IDataImportTransformationOutcome`; null/failed results admit no write.
  `RecordsTransformationFailed` is included in RecordsFailed, not WriteAttempts
  or HasUncertainWrites. Earlier acknowledgements remain visible; sync does not
  advance its cursor or retry a whole run on these failures.
- Legacy built-in stage methods now throw safe ImportTransformationException
  instead of returning the input. Custom legacy helpers must not hide failures.
  Strict mapping has no implicit default stage and defaults to Reject conversion;
  explicitly registered fallback/warning policies remain opt-ins.
- QualityRules and sync RuleKeys now run after actual transformations/defaults
  and before the first provider attempt, once per row rather than per retry.
  Capture ordered fields/actions, mode, store and gate context before mutation.
  Required failures deny writes; explicit Advisory/Warn increment warning counts.
  Quarantine needs a store and counts only acknowledged saves; failed reject saves
  never admit rows. Read Editor/Importing/QUALITY-ADMISSION.md for exact counters,
  trusted plugin/serialization limits; REJECT-RECOVERY.md defines file row recovery
  and still-open native/provider-run gates.
  Sync attempt thresholds and failed-run checkpoint counts now have a separate
  verified boundary; read BeepSync/THRESHOLDS-AND-FAILURE-EVIDENCE.md rather than
  treating record rejection as successful threshold-approved progress.
  Required catalog failure denies configuration admission; inspect
  TransformationAdmissionFailed rather than inventing failed-row counts.
  Required rules normalize once through the editor-owned resolver registry,
  bypass metadata-only caches and deny reported error/warning fallbacks with safe
  outcomes. Custom registration survives switching editors. Read
  Editor/Importing/DEFAULTS-ADMISSION.md. Run-owned defaults refresh implicit
  catalogs without populating caller configuration; literals use a closed bounded
  set and byte arrays are cloned per row. Sync captures both declared catalogs
  before provider-opening validation, including empty catalogs. Required rosters/
  priorities are pinned before source reads and retained for both directions;
  registrations affect fresh runs. Per-row SentData and admitted named lookups
  protect retained definitions. Nested facade/manager/telemetry/default wrappers
  keep failures sticky and deny foreign-editor rebinding; scoped telemetry omits
  raw diagnostic fields. Full policy/plugin immutability, grammar/logging and
  provider qualification remain. Required shipped outer grammar now uses exact
  operator routing and rejects ignored arity/empty slots/invalid environment scopes;
  nested date arguments work. Built-in time/hash SEQUENCE/INCREMENT placeholders
  deny required rows; use a qualified allocator plugin, not a uniqueness assumption.
  Captured plugin instances are trusted code; query-provider/identity gates remain.
  Required-only dot normalization preserves literal/empty-string quotes, rejects
  missing segments and separates dots outside quotes/nested calls. Quote decimal
  operands; grouping quotes apply only to declared expression/mode/filter roles.
  Public legacy parsing is unchanged; read DEFAULTS-ADMISSION.md before DSL edits.
  Exact shipped required expressions/formulas use a bounded pre-read AST with
  Boolean-only conditions, precedence, exact typed comparisons, invariant numeric
  literals and lazy branches. Quote labels; bare names read flat row fields.
  Nested custom calls retain pinned selection/failure/cancellation; unused branches
  are syntax-checked, not semantically evaluated. See the contract for numeric
  bounds and remaining identity/NFEL/plugin/provider gates.
  Exact shipped required queries validate all filters before callbacks, bind actual
  closed invariant context values, reject null collections and malformed aggregate
  values, and retain typed extrema. Real empty COUNT/EXISTS return 0/false.
  Streaming limits, cancellation/root-status checks and disposal do not qualify
  eager provider allocation, hidden failures, read isolation or context ownership.
  Query handles/names must be supplied explicitly; do not infer them from import.
  Shipped required identity defaults require explicit string email/role keys, never
  row identity or a fabricated/group/generic-role fallback. ENV reads only the
  declared scope; SYSTEMPATH is Machine PATH, ENV(PATH) Process PATH. Actual OS
  identity/folder reads and host strings are not immutable context or authorization
  proof. Read defaults admission before changing platform/native/identity behavior.
  Required shipped dates use ISO literals and invariant bounded formatting, not
  host-culture parsing. Nested bases must be actual DateTime/DateTimeOffset values
  from the pinned registry; kind/offset and exact tick/calendar arithmetic are
  retained. Clock leaves stay dynamic; this is not immutable time or DST policy.
  Required configuration uses explicit named flat string maps or declared Process
  prefixes, never bare-variable/editor/row inference. Missing/invalid explicit
  sources cannot fall back; connection alias conflicts deny. Host bridging and
  immutable run configuration remain separate work. See defaults admission.
  Bound mapped imports
  validate captured actual pairs and required target coverage, with both destination
  field lists populated. RequireBoundMappingMetadata rejects missing targets
  instead of inferring mapped DDL; unbound import behavior remains legacy.
  Read `Editor/Importing/TRANSFORMATION-OUTCOMES.md` before claiming governance.
- File error/history stores coordinate full JSONL updates across instances and
  cooperating processes; corrupt/foreign-context records are not treated as empty.
- FileWatermarkStore preserves closed tagged scalar/composite dictionary/array
  types in a version-1 envelope with context validation and hashed filenames.
  Storage support does not enable Sequence/CompositeKey BeepSync execution modes.
- Default file-store constructors remain; folder overloads support isolated hosts
  and tests. Legacy sanitized files are explicitly rejected pending identity/type
  migration, never silently overwritten or interpreted using the current culture.
- Preserve .beep.lock sidecars. Read Services/Persistence/README.md for recovery
  and platform limits. Whole-file JSONL rewrites still need bounded retention;
  error-store replay is not a provider-backed exactly-once guarantee.
- `RunImportAsync` returns `ImportExecutionResult` via `IErrorsInfo`. Only
  `ImportOutcome.Completed` maps to Ok; Partial/Failed/Cancelled map to Failed.
- Counts represent datasource acknowledgements, not assumed transaction commits.
  Inspect RecordsSucceeded/Failed/Skipped and WriteAttempts; skipped failures
  are not successful writes. The helper writes InsertEntity directly.
- Retry only explicitly failed records, never the entire partially written batch.
  Exceptions/null/ambiguous results set HasUncertainWrites and block blind replay.
1. Validate configuration before execution.
2. Use preflight when schema drift or migration alignment matters.
3. Treat error store, replay, and run history as first-class workflow pieces, not afterthoughts.
4. Use watermarks and sync mode intentionally; do not mix incremental semantics casually.

## Related Skills
- [`beepsync`](../beepsync/SKILL.md)
- [`etl`](../etl/SKILL.md)
- [`migration`](../migration/SKILL.md)

## Detailed Reference
Use [`reference.md`](./reference.md) for configuration properties, examples, and pitfalls.
