# BeepSync Reference

## Required Defaults

Read Engine Editor/Importing/DEFAULTS-ADMISSION.md before changing defaults.
Configuration failure is TransformationAdmissionFailed; row resolution failure
is RecordsTransformationFailed. Required expressions normalize once with an
editor-owned registry and no metadata-only result cache. Reported warning/error
fallbacks deny writes without raw logs. Custom registration is retained per editor;
run-owned defaults do not populate caller configuration. Implicit catalogs refresh
per run; closed bounded literals and per-row byte copies retain owned intent. Both
declared sync catalogs are captured before provider-opening validation; empty
catalogs are not relooked up. Nonblank rules omit unused PropertyValue from captured
SentData. Ordered resolver rosters/priorities are captured before reads/preflight
and retained for admitted rows and both directions; fresh runs see registrations.
Per-row SentData cannot retarget retained defaults. Nested required wrappers pin
the same editor/roster, use admitted named definitions and keep caught failures
sticky; telemetry omits raw diagnostic fields in that scope. Plugin internals,
other policies and full grammar/provider qualification remain open. Do not
generalize required semantics to legacy APIs or claim all reverse rules were pre-evaluated.

Required shipped date/GUID/user/system routing matches the operator token rather
than words in arguments. Nine exact shipped types validate outer arity/atomic keys;
empty comma slots, extra ignored arguments and invalid environment scope names
deny rows. Nested date calls and quoted format commas are supported. Built-in
SEQUENCE/INCREMENT/AUTOINCREMENT remain time/hash demos and deny required rows;
use a qualified allocator plugin instead of assuming durable/unique numbers.
GUID zero-argument aliases/formats and custom semantic contracts remain supported.
Read DEFAULTS-ADMISSION.md for dynamic values, shared structural helper behavior
and remaining identity/context/platform and full provider qualification.

Required-only dot parsing preserves literal quotes/empty strings, rejects missing
segments and retains errors for legacy bare tokens; public legacy parsing is unchanged.
Dots outside quotes/nested calls separate arguments: ADD.2.3 resolves 5; quote
decimal arguments. COALESCE.'false'.'fallback' is the string false; IF.true.''.'else'
is empty. EXPRESSION.'A+B' is literal, while EXPRESSION.A+B is arithmetic.
Grouping quotes apply only to declared condition/calculation/date/logical/math-name/
query mode/filter roles. Unknown plugin dot dialects are not rewritten. Known
overrides receive canonical quotes/roles; numeric helpers unwrap quoted operands
in required work. Read DEFAULTS-ADMISSION.md for exact roles and remaining semantics.

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

## Mapped Metadata

ToImportConfiguration/ToReverseImportConfiguration produce mapping definitions,
not executable destination metadata. SyncDataAsync now captures actual provider
structures and calls BindEntityMetadata for both directions before importing.
Direct translator callers must bind actual source/target metadata themselves and
keep the config private; the binding API does not resolve provider handles.
RequireBoundMappingMetadata validates pairs, required targets and generated target
shapes, and requires an existing target. It never infers mapped DDL even with
creation enabled. Declared FieldSyncData types and bare type-cache seeds are not
authority. Read Engine Editor/BeepSync/STORAGE-AND-OUTCOMES.md for exact limits.

## Run And Inspect

Configured DqPolicy.RuleKeys now run as dedicated post-transform/pre-write gates.
RecordFailureMode defaults to Required; OnRecordFailure defaults to Block.
Advisory/Warn are explicit opt-ins with warning/evaluation-failure counters.
Quarantine requires a supplied store or complete open/provisioned named channel.
Actual Boolean SolveRule results are required; string coercion is not pass.
Bidirectional ImportExecutionResult combines both directions' acknowledgements and
quality outcomes, including reverse failure/cancellation. RequiresReconciliation
reports failed runs with earlier acknowledged/uncertain target work. Read Engine
Editor/Importing/QUALITY-ADMISSION.md for record admission and reject-store limits.

Enabled DQ defaults to a Required attempt threshold. Set BatchThresholdEnabled=false
for record-only policies, or register BatchThresholdRuleKey with exact action outputs
ContinueRun/AbortRun. ThresholdFailureMode is independent of RecordFailureMode;
Advisory warnings/errors are exposed by LastRunBatchThresholdResult. Actual attempted
rows across admitted directions form the denominator, including rejected rows;
empty attempts have zero rate and still evaluate. Neither ContinueRun nor an
advisory threshold can override a failed import. Read Engine Editor/BeepSync/
THRESHOLDS-AND-FAILURE-EVIDENCE.md for semantics and compatibility.

```csharp
using var manager = new BeepSyncManager(editor, integrationContext, storageDirectory);
manager.AddSyncSchema(schema); // Previously configured and validated DataSyncSchema.
manager.DiagnosticFailed += (_, failure) =>
    diagnosticSink.Record(failure.Operation, failure.ExceptionType);

var result = await manager.SyncDataAsync(schema, cancellationToken, progress);
if (result is SyncCheckpointFailureResult failure)
{
    // These are provider acknowledgements, not rolled-back writes or safe replay offsets.
    recoverySink.Record(failure.RunId, failure.RecordsAcknowledged,
        failure.HasUncertainWrites, failure.CheckpointPersistenceStatus);
}
if (result.Flag != Errors.Ok) return;

// Explicit schema/watermark persistence, separate from the run checkpoint.
await manager.SaveSchemasAsync(); // Throws on failure; handle at the host boundary.
```

The sinks above represent host callbacks, not BeepDM APIs. Diagnostic callbacks
are isolated from outcomes but still run synchronously and should be short.
Failure to save schema after Completed needs reconciliation before unsafe replay.

## Required Checkpoints

```csharp
schema.RetryPolicy = new RetryPolicy
{
    MaxAttempts = 3,
    BaseDelayMs = 1000,
    BackoffMode = "Exponential",
    CheckpointEnabled = true
};
```

Running must be acknowledged before import. Completed must be acknowledged before
Success/date/cursor publication. Failed mandatory completion retains counts and
requires reconciliation; it is not retried. Null/disabled RetryPolicy means no
equivalent durable run guarantee. Running/changed/stale partial evidence blocks
restart. Failed/cancelled runs after acknowledged startup publish typed FailureEvidence
with actual counts under the captured run identity. LastRunFailureCheckpointStatus
reports publication; failed/null/throwing saves return SyncCheckpointFailureResult
with CheckpointStage=Failure and the original ImportResult. A separate cooperative
cleanup token preserves cancellation evidence, not a hard deadline or rollback.
Terminal Failed snapshots cannot reopen. Setting ActiveCheckpoint=null does not
clear authoritative disk evidence. Durable file reject replay is a separate
operator-prepared/CAS-claimed row operation through ReplayRejectedRecordAsync.
It applies current direction-specific quality without advancing a cursor,
clearing a failed checkpoint or publishing Completed. Read
`Editor/Importing/REJECT-RECOVERY.md`; native/provider-run recovery remains open.

## Acknowledged Per-Schema Save

```csharp
var store = new SchemaPersistenceHelper(editor, storageDirectory);
var saved = await store.SaveSchemaAcknowledgedAsync(schema, cancellationToken);
saved.ThrowIfNotSaved();
var checkpoint = await store.LoadCheckpointAsync(schema.Id);
```

This preserves other schemas under the complete update lease. SaveSchemas is whole
replacement. Acknowledgement is a cooperating local-filesystem result, not a
distributed transaction, power-loss guarantee or provider-run lock.

## Recovery And Compatibility

Preserve original corrupt/legacy bytes before an explicit migration. Versioned
artifacts bind exact ordinal schema IDs. Supported tagged values round-trip without
arbitrary CLR type activation. Untagged cursor types are never guessed.
ClearCheckpointForRunAsync rejects changed ownership; only use after the operator
has reconciled actual provider state and authorized clearing that specific run.

Do not infer key replay, full approval hashing, transactional promotion or required
DQ gate safety from model properties. See STORAGE-AND-OUTCOMES.md and the framework
tracker for available behavior versus unfinished gates. Import still owns the actual
data move; a sync retry cannot undo earlier acknowledged forward writes.
