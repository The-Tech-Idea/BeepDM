---
name: beepsync
description: Implement and debug BeepDM synchronization through BeepSyncManager, mapped metadata admission, typed checkpoint storage, completion acknowledgements and diagnostic outcomes. Use for DataSyncSchema execution and recovery boundaries, not one-off importing or schema DDL.
---

# BeepSync

BeepSyncManager owns sync orchestration; SyncSchemaTranslator builds configurations
for DataImportManager, which moves the data. Use importing for its write/retry
behavior and migration/schema services for DDL/preflight rather than duplicating them.

## Working Set

- `DataManagementEngineStandard/Editor/BeepSync/BeepSyncManager.Core.cs`
- `DataManagementEngineStandard/Editor/BeepSync/BeepSyncManager.Sync.cs`
- `DataManagementEngineStandard/Editor/BeepSync/BeepSyncManager.Cdc.cs`
- `DataManagementEngineStandard/Editor/BeepSync/BeepSyncManager.SchemaManagement.cs`
- `DataManagementEngineStandard/Editor/BeepSync/BeepSyncManager.SchemaGovernance.cs`
- `DataManagementEngineStandard/Editor/BeepSync/Helpers/SchemaPersistenceHelper.cs` and its Serialization/Artifacts partials
- `DataManagementEngineStandard/Editor/BeepSync/Helpers/SyncSchemaTranslator.cs` and `SyncProgressHelper.cs`
- `DataManagementModelsStandard/Editor/DataSyncSchema.cs` and `Editor/BeepSync/`
- `tests/FrameworkReliabilityTests/SyncOutcomeTests.cs` and `SyncPersistenceTests.cs`

## Execution Boundaries

- Required default rules use editor-owned resolution and no metadata-only row
  cache. Reported fallbacks deny writes; earlier acknowledgements remain visible,
  with no whole-run retry or cursor advance. Read Importing/DEFAULTS-ADMISSION.md;
  both declared catalogs are captured before provider-opening validation, including
  empty catalogs. Reverse catalog denial precedes forward writes. A reverse row-
  rule failure can still follow forward acknowledgements; catalog admission does
  not freeze every policy/plugin context or prove provider recovery. Required
  resolver rosters/priorities are pinned before preflight and retained across both
  directions, even when forward callbacks register/remove resolvers. Nested required
  calls retain that roster and sticky failure; per-row SentData and admitted named
  lookup protect definitions. Registrations affect fresh runs; plugin internals
  and other policies are not frozen or sandboxed.
  Required shipped outer grammar rejects ignored arguments/empty slots and routes
  by operator, not words inside keys/arguments. Built-in SEQUENCE/INCREMENT time/hash
  placeholders deny required rows; a qualified allocator plugin is needed. Nested
  date calls and GUID aliases retain supported meanings; broader semantics remain.
  Required dot normalization preserves quoted literals/empty strings and denies
  missing segments before reads/writes. Quote decimals; condition/date/query groups
  retain declared roles. Public legacy parsing stays unchanged.
  Exact shipped required expression/formula ASTs qualify Boolean-only conditions,
  precedence, invariant numbers, typed exact comparisons and lazy branches before
  writes. Bare names are flat row fields; quote labels. Nested overrides retain
  pinned selection/sticky failure; unused branches are syntax-checked only.
  Read defaults admission for numeric bounds and remaining semantic/provider gates.
  Exact shipped required queries validate every filter before callbacks, bind
  closed invariant actual context and retain typed aggregates without skipping
  malformed values. Null collections deny; real empty COUNT/EXISTS are 0/false.
  Streaming limits/cancellation/root-status/disposal checks preserve earlier
  acknowledgements as Partial, not replay. Explicit query handles/context and
  live-provider translation/isolation remain separate qualification gates.
  Shipped required identity/scope rules use explicit string email/application-role
  keys and actual supported OS identity/folders, never row spoofing or inferred
  role/profile fallback. ENV reads only its declared scope; SYSTEMPATH means
  Machine PATH. These values are not authorization proof or immutable run context;
  read defaults admission for native/platform and legacy/custom limitations.
  Required shipped dates use ISO literals and invariant bounded formatting, not
  host-culture parsing. Nested bases must be actual DateTime/DateTimeOffset values
  from the pinned registry; kind/offset and exact tick/calendar arithmetic are
  retained. Clock leaves stay dynamic; this is not immutable time or DST policy.
  Required configuration uses explicit named flat string maps or declared Process
  prefixes, never bare-variable/editor/row inference. Missing/invalid explicit
  sources cannot fall back; connection alias conflicts deny. Host bridging and
  immutable run configuration remain separate work. See defaults admission.
- Typed import transformation failure stops whole-run retry even with zero writes;
  RecordsTransformationFailed retains pre-write failure evidence and leaves success
  dates/cursors unchanged. Read Editor/Importing/TRANSFORMATION-OUTCOMES.md for limits.
  Record DQ uses a separate post-transform/pre-write admission stage.

- DqPolicy.RuleKeys now evaluate actual transformed forward/reverse payloads once
  per row with captured engine/keys/actions/mode/limits. Require actual Boolean
  SolveRule results; missing/throwing required engines/rules and malformed outputs
  cannot pass. Explicit Advisory/Warn are counted rather than silently skipped.
  Complete named reject channels must already be open/provisioned; an explicit
  errorStore takes precedence. Quarantine counts only acknowledged saves.
  Returned bidirectional counts retain forward acknowledgements on reverse failure
  or cancellation; rejected/required failed rows stop blind replay and cursor advance.
  Read Engine Editor/Importing/QUALITY-ADMISSION.md. RecordFailureMode is record-only:
  thresholds have independent captured ThresholdFailureMode and default Required
  semantics. BatchThresholdEnabled=false is the explicit record-only opt-out.
  Require exact ContinueRun/AbortRun actions, measured attempted-row denominators
  (both admitted directions), finite limits and once-per-attempt evaluation.
  LastRunBatchThresholdResult exposes advisory warnings and required failures.
  Failed/cancelled checkpointed runs now publish typed FailureEvidence under the
  captured run identity; Failed records block restart and cannot reopen.
  Inspect LastRunFailureCheckpointStatus and SyncCheckpointFailureResult.CheckpointStage/
  ImportResult if publication fails. Completion-save failure does not rewrite possibly
  committed Completed state. Read Editor/BeepSync/THRESHOLDS-AND-FAILURE-EVIDENCE.md
  for cooperative cleanup/compatibility limits. Optional durable file reject recovery
  now has operator preparation/CAS claims and actual provider acknowledgements.
  Use BeepSyncManager.ReplayRejectedRecordAsync with current direction-specific
  record policy; reverse requires Bidirectional. This never completes a failed run,
  advances its cursor or reevaluates a whole-attempt threshold. Claimed/uncertain
  rows require authorized reconciliation, not automatic expiry or index marking.
  Read Editor/Importing/REJECT-RECOVERY.md; native channels, provider-run admission,
  full intent/cursor agreement and live-provider recovery remain open.

- Mapped runs bind captured provider metadata into both destination field lists
  through SyncSchemaTranslator.BindEntityMetadata. Validate actual pairs/required
  target coverage, not same-name overlap; provider names/types are authority.
  Validate both generated target shapes before either writes; include the exact
  key pair once. A failed reverse generation must not follow a forward write.
  RequireBoundMappingMetadata requires an existing target, even if creation was
  requested. Do not infer mapped DDL or bypass validation with empty field lists.
  Read STORAGE-AND-OUTCOMES.md and test actual generated provider payloads in
  SyncMappingAdmissionTests. Capture needs host coordination; policies/context
  are not complete immutable intent. Preflight failures must not admit import.

- Only typed Timestamp CDC executes. Sequence/CompositeKey fail before writes;
  typed storage alone does not implement ordering, deduplication or offset replay.
- Inspect `IErrorsInfo.Flag`. Partial, failed and cancelled imports cannot publish
  Success or advance the cursor. Whole-run retry stops after acknowledged/uncertain
  writes, including reverse-import failure; reconcile before manually replaying.
- `RetryPolicy.CheckpointEnabled` requires `ISyncPersistenceAcknowledgement` for
  both Running and Completed. Failed startup admits no import. Failed completion
  returns `SyncCheckpointFailureResult` with acknowledged count/status/run ID and
  reconciliation required; it does not retry even an empty run's terminal save.
- After Saved completion, advisory SLO/alert/history/property-notification failures
  are separate diagnostics. Inspect `DiagnosticFailed` and
  `LastRunDiagnosticFailures`; raw observer/rule/logger exceptions are not logged.
  Diagnose subscription-removal failure rather than claiming the engine unsubscribed.
- Success updates schema/cursor in memory; `SaveSchemasAsync` remains explicit and
  can fail. Schema and Completed are not one transaction. Providers must deduplicate
  or an operator must reconcile restart gaps before replay.
- Legacy provider calls are synchronous and source reads can materialize all rows.
  Batch size or parallel fan-out does not guarantee bounded memory/native cancellation.
  Coordinate shared schemas, managers and provider targets at the host boundary.

## Storage Boundaries

- Built-in root is `<ConfigPath>/BeepSync`, otherwise the legacy AppData root.
  Ambiguous old global stores require explicit ownership/migration. An explicit
  storage-directory constructor isolates hosts/tests; CreateWithPersistence selects
  a custom adapter which must acknowledge checkpointed execution.
- Per-schema updates coordinate the entire read/change/save. Whole SaveSchemas is
  replacement, not merging stale snapshots. Leases are not provider-run ownership.
- Version-1 envelopes use closed typed cursors and exact-ID hashed artifact paths.
  Corrupt/foreign/unknown files are preserved and rejected, not fresh empty state.
  Versions are immutable; Completed cannot be reopened under the same run ID.
- Untagged legacy cursors/raw-name artifacts require explicit migration. Do not
  guess types, discard evidence or clear a checkpoint to make a retry pass.
- ClearCheckpointForRunAsync checks ownership, but is an operator recovery action,
  not automatic success cleanup. Backups cover validated schema snapshots only.

## Open Contracts

Schema promotion still publishes live state before two saves and uses an incomplete
intent fingerprint. Required attempt thresholds/failed counts are verified slices,
not complete policy capture, durable cursor agreement or native/provider-run recovery.
Do not advertise those as durable governance, or promise universal exactly-once,
transactional provider writes, automatic key replay or cross-OS qualification.

Read `DataManagementEngineStandard/Editor/BeepSync/STORAGE-AND-OUTCOMES.md` for the
authoritative contract and [reference.md](reference.md) for current API usage.
