---
name: beepdm-sync
description: Use for BeepDM DataSyncSchema execution, typed sync storage, checkpoint acknowledgements and recovery or diagnostic boundaries. Delegate actual row importing to beepdm-importing and DDL/preflight to beepdm-schema or beepdm-migration.
---

# BeepDM Sync

Required defaults use editor-owned resolution, one normalization and no shared
row-result cache. Failure retains acknowledged counts and does not retry a sync
or advance cursors. Read Importing/DEFAULTS-ADMISSION.md: run-owned closed literals
leave caller configuration unpopulated and both declared catalogs (even empty) are
captured before provider-opening validation. Reverse denial precedes forward writes,
but row rules are not pre-evaluated and full schema/plugin intent remains open.
Required resolver rosters/priorities are pinned before preflight across both
directions; registration edits affect fresh runs. Nested required wrappers retain
the same editor/roster and sticky failure, with per-row SentData/admitted named
definitions and safe telemetry. Plugin internals and other policies remain mutable.
Required shipped outer grammar routes by operator and rejects ignored arity/empty
slots/invalid scope names. Nested date calls and GUID aliases are supported;
time/hash SEQUENCE/INCREMENT demos deny required rows. A qualified allocator plugin
is needed; query/identity/provider semantics and dynamic values remain open.
Required-only dot parsing preserves quoted/empty literals and denies missing
segments. Dots outside quotes/nested calls separate arguments; quote decimals.
Grouping quotes retain declared expression/mode/filter roles; public legacy parsing
is unchanged. Read DEFAULTS-ADMISSION.md before modifying DSL contracts.
Exact shipped required expression/formula resolvers parse a bounded AST before
field reads: Boolean-only conditions, operator precedence, typed exact comparisons,
invariant numbers and lazy branches. Bare names read flat row fields; quote labels.
Nested custom calls retain pinned selection and required failure/cancellation.
Unused branches are syntax-checked, not semantically prequalified. Read defaults
admission for scalar/result bounds, Decimal/IEEE arithmetic and remaining gates.
Exact shipped required datasource queries validate every filter before callbacks,
bind closed invariant row/named values and never discard malformed filters or
aggregate values. Quoted @Name is literal; missing/ambiguous/null bindings deny.
Null collections deny; actual empty COUNT/EXISTS return 0/false. Typed extrema
retain field types; numeric aggregates use required Decimal/IEEE arithmetic.
Consumption is streaming/bounded with cancellation, root provider-status checks
and disposal; FIRST/SCALAR/EXISTS read only one row. Scalar bytes are copied, while
FIRST returns the actual record. Read defaults admission for limits and explicit
query context: provider handles/status/translation/isolation remain unqualified,
and ordinary import contexts do not implicitly pick a query datasource.
Required shipped identity/scope defaults require explicit string UserEmail/UserRole
or exact ApplicationRole context, never imported-row identity, fabricated email or
group/generic-role substitution. Conflicting sources, invalid values and getter
failure/cancellation deny. ENV reads only its declared scope; SYSTEMPATH is Machine
PATH and ENV(PATH) is Process PATH. Named OS folders/Windows identity use actual
supported APIs; no unrelated profile fallback. Host strings are not authorization
proof and OS/host context is not captured immutably. Read defaults admission for
native resource/platform limits and legacy/custom separation.

Required shipped date plans validate known nested date syntax/offset/format roles
before callbacks. Use Gregorian ISO yyyy-MM-dd or full second-resolution ISO
timestamps (one-to-seven fractional digits, optional Z/signed HH:mm offset).
No zone retains Unspecified DateTime, Z UTC DateTime, explicit offset DateTimeOffset.
Days/hours/minutes require exact whole ticks from signed invariant decimal text;
months/years require Int32 integers. FORMAT is invariant and bounded, not a fallback.
Nested registry/context values must be actual DateTime/DateTimeOffset, not reparsed
strings, DateOnly, time spans or observers. Clock leaves capture dynamic instants;
arithmetic does not qualify named-timezone DST rules, host clock immutability or
arbitrary plugin grammar. Legacy direct/subclass semantics remain separate.

Required configuration aliases bind explicit AppSettings/AppConfig/WebConfig/
ConnectionStrings Objects entries implementing IReadOnlyDictionary<string,string>.
The complete selected flat map is bounded/captured before case-insensitive lookup;
duplicate sources/keys, malformed strings/counts, callbacks/disposal and cancellation
deny. Genuine empty strings retain value. Explicit maps never fall back. Without
AppSettings, only APPSETTING_<key> Process values are read; use ENV(key) for bare
variables. AppConfig/WebConfig require real host maps. Connection aliases must yield
one real value or identical values, never conflicts. Ordinary import contexts do not
implicitly supply maps; per-resolution capture is not immutable run configuration.
Do not infer editor credential decryption or host keys; govern secret destinations/
reject persistence. Read defaults admission for bounds and legacy/custom separation.

Read `Editor/BeepSync/BeepSyncManager.Core.cs`, `Sync.cs`, `Cdc.cs`,
`SchemaGovernance.cs`, the `SchemaPersistenceHelper` partials and
`Editor/BeepSync/STORAGE-AND-OUTCOMES.md` under DataManagementEngineStandard.
Public DTOs/optional acknowledgement contracts belong in DataManagementModelsStandard.

- BeepSyncManager translates schemas to DataImportManager runs; preserve actual
  acknowledged counts and stop whole-run retries after acknowledged/uncertain writes.
- Typed transformation failures also stop whole-run retry with zero writes and keep
  success dates/cursors unchanged. Read Editor/Importing/TRANSFORMATION-OUTCOMES.md;
  record DQ uses a separate dedicated admission stage.
- DqPolicy.RuleKeys now evaluate actual transformed forward/reverse payloads with
  captured record policy/engine. Require Boolean output; missing/throwing required
  dependencies cannot pass. Explicit Advisory/Warn are counted. Reject channels
  need acknowledged saves and existing provisioning. Combined results preserve
  forward acknowledgements on reverse failure/cancellation. Read Importing/
  QUALITY-ADMISSION.md and REJECT-RECOVERY.md; native/provider-run recovery remains open.
- Required attempt thresholds capture engine/key/mode/limits before writes; only
  exact ContinueRun/AbortRun actions are valid. Denominator is actual attempted
  rows across admitted directions, with empty rate zero. Advisory evidence is
  explicit; BatchThresholdEnabled=false opts out for record-only policies.
  LastRunBatchThresholdResult exposes the decision. Failed checkpointed runs save
  typed FailureEvidence under captured identity; inspect failure-save status and
  CheckpointStage/ImportResult if acknowledgement fails. Read BeepSync/
  THRESHOLDS-AND-FAILURE-EVIDENCE.md; this is not cursor agreement or reject replay.
- SyncSchemaTranslator.BindEntityMetadata binds captured actual provider metadata
  into both destination field lists. Validate renames/pairs/required targets and
  exact key pairs and both generated target shapes before either bidirectional
  write. RequireBoundMappingMetadata
  requires existing targets, even if creation was requested; do not infer mapped
  DDL. Use actual generated payload tests in SyncMappingAdmissionTests, not bare
  type-cache pre-seeds. Policies/context and live-provider qualification remain open.
- Only Timestamp CDC executes. Closed typed storage is not sequence/composite
  execution, deduplication or automatic key/offset replay.
- CheckpointEnabled requires acknowledged Running and Completed. Failed startup
  admits no import; failed terminal persistence returns SyncCheckpointFailureResult
  with count/status/run ID and reconciliation required. It cannot publish Success.
- Saved completion precedes Success/date/cursor publication. SLO/alert/history and
  property-notification failures are separate sanitized DiagnosticFailed events;
  observer/logger failures cannot reclassify the write outcome.
- SaveSchemasAsync remains explicit; schema/cursor and Completed are not a single
  transaction. Reconcile restart gaps before replay if provider writes are not idempotent.
- Built-in per-schema storage updates own the complete lease; whole SaveSchemas
  replaces snapshots. Preserve corrupt/legacy evidence, exact-ID hashed identities
  and immutable version artifacts. Select host storage roots deliberately.
- Running/Failed/changed/stale partial evidence blocks restart. Do not automatically clear
  checkpoint evidence; run-owned clearing still needs operator reconciliation.
- Optional durable file reject triage/replay now uses operator preparation and
  exclusive claims. BeepSyncManager.ReplayRejectedRecordAsync captures current
  direction-specific quality; reverse requires Bidirectional. It does not complete
  failed runs, advance cursors or reevaluate attempt thresholds. Reconcile uncertain
  provider evidence explicitly. Read Editor/Importing/REJECT-RECOVERY.md.
- Promotion agreement, complete sync approval hashes, native/full provider recovery,
  provider-run admission, streaming and supported Unix qualification remain open.

Validate with SyncOutcomeTests and SyncPersistenceTests plus the full source-built
TFM matrix. Mocks do not establish live-provider recovery or package compatibility.
