# BeepSync Storage And Outcomes

## Scope And Ownership

Required import defaults use editor-owned resolution, safe fallback denial and
cache bypass. Resolver failures retain earlier acknowledgements, stop whole-run
retry and preserve cursors. Read [defaults admission](../Importing/DEFAULTS-ADMISSION.md)
for run-owned closed literals and both-direction catalog capture before provider-
opening validation. Empty catalogs are retained; reverse denial precedes forward
writes. Required resolver rosters are also pinned before preflight and retained
through forward/reverse execution; registrations affect fresh runs. Nested calls
keep that roster, safe diagnostics and sticky failure, not a last-global registry.
Per-row SentData/named lookups cannot alter retained definitions or reread catalogs.
This does not pre-evaluate every reverse rule or freeze all schema/plugin
intent, and it does not establish atomic cursor/provider recovery.

BeepSyncManager translates a DataSyncSchema into DataImportManager runs. Legacy
IDataSource calls remain synchronous and source reads can materialize all rows.
BatchSize does not bound total source memory or make provider calls interruptible.
Only Timestamp CDC execution is supported; typed storage does not enable Sequence
or CompositeKey execution, deduplication or automatic offset replay.

The built-in SchemaPersistenceHelper uses `<ConfigPath>/BeepSync` when ConfigPath
is configured. Otherwise it retains the legacy AppData/TheTechIdea/Beep/
BeepSyncManager root. An existing shared legacy store blocks creation of a new
configured root until ownership/migration is explicitly selected. Hosts/tests can
use `new BeepSyncManager(editor, context, storageDirectory)`; custom stores use
`BeepSyncManager.CreateWithPersistence(editor, storage, context)`.
Do not concurrently execute one mutable schema/manager or shared provider target
without host coordination. Storage leases do not own provider execution.

## Mapped Run Admission

Translated mappings are definitions, not executable metadata on their own.
SyncDataAsync preflight captures actual provider structures with Models
EntityMetadataSnapshot.Capture. SyncSchemaTranslator.BindEntityMetadata binds a
separate capture per direction into SourceEntityStructure/DestEntityStructure and
both destination EntityFields/SelectedDestFields before import validation or
generation. Provider field names/types govern; FieldSyncData type strings do not.
Field names are matched case-insensitively and normalized to provider casing.

Mapping-aware preflight accepts explicit renames instead of requiring same-name
overlap. Missing/ambiguous fields, empty types, malformed pairs, repeated target
assignments and unmapped required non-auto-increment targets reject. Required
coverage does not presume an implicit default catalog will fill the field. Both
directions of bidirectional sync are bound and validated before either writes.
Bound import validation compiles each target using the actual generation route
and checks writable properties against metadata before either import. Unsupported
reverse generated names/shapes cannot defer failure until after forward writes.
This does not infer conversion success for unread row values. A custom generator
may run again during transformation and remains trusted mutable host code, not
a frozen implementation or sandbox; wider generator qualification is open.
The exact key pair is included once; either-side matches do not suppress it.

Bound configurations set additive Models RequireBoundMappingMetadata. Import
validates actual pairs and bound shape rather than bypassing validation. Existing
target presence is checked before sync admission and again before import. A
missing mapped destination rejects even with CreateDestinationIfNotExists=true:
this slice does not infer renamed DDL, create from source type declarations or
reuse source structure under a destination name. Provision the target explicitly
through the schema/migration path. Legacy unbound import creation is unchanged
and remains separately unqualified.

Preflight exceptions/null/non-Ok outcomes no longer fall through to import; caller
cancellation returns Cancelled without writes. Preflight exception diagnostics are
fixed safe messages, not provider payloads. This is not all-route diagnostic safety.
Captured metadata/pairs survive edits made after admission; capture itself needs
host coordination and configs remain private mutable objects. Schema policies,
filter objects and integration context are not a complete immutable run intent.
Provider existence/DDL can race external actors; no distributed target lease or
automatic schema-drift refresh is established. The preflight now rejects
unsupported metadata graphs instead of proceeding with shared live structures.

Tests: SyncMappingAdmissionTests adds 27 cases per configured TFM, including real
ClassCreator/Roslyn-generated forward/reverse payloads, rename/type authority,
mutation isolation, reverse-required coverage, cancellation and no-DDL failures.
No bare type-cache seeds or global cache clears substitute for actual generation.

## Snapshot Protocol

- `SyncSchemas.json` uses FormatVersion=1 and Schemas. Closed tagged cursor values
  preserve supported primitive, DateTime/DateTimeOffset, Guid, byte array, array
  and string-key dictionary types. Unknown tags/arbitrary CLR types are rejected.
- Checkpoints use `checkpoints/checkpoint-v1-<SHA256 exact UTF8 schema ID>.json`.
  Versions use `versions/schema-v1-<same hash>/v<number>.json`. Artifact kind,
  outer/inner identity, version and required checkpoint fields are validated.
  Identity comparison is ordinal; do not trim/case-fold IDs during migration.
- Per-schema upsert/delete owns the complete load/validate/change/save lease.
  Whole SaveSchemas is explicit replacement, not a merge of stale snapshots.
  Version numbers are immutable; exact repeated content is allowed, changed
  content is rejected. Corrupt versions are not silently skipped by load/diff.
- Schema records must supply exactly one string ID before deserialization; model
  constructor defaults cannot invent persisted identity. Unambiguous legacy Id/id/ID
  property casing is accepted, but identity values remain exact and case-sensitive.
- Missing files are fresh state; empty/corrupt/foreign/unknown-version files are
  not. Failed reads/updates/deletes preserve evidence and surface failure.
  Built-in legacy Task save methods throw; optional ISyncPersistenceAcknowledgement
  returns Saved, Failed, Cancelled or Unsupported. A null result is not Saved.
- Eligible legacy schema arrays may upgrade on write only with null/tagged cursor
  values. Untagged legacy cursors and raw-name checkpoint/version artifacts require
  explicit migration; types/ownership are not guessed. Back up original bytes.
- CreateBackupAsync validates a schema snapshot before a uniquely named atomic
  backup. It is not a transactionally consistent schema/version/checkpoint backup
  or an automatic recovery selector. ClearCheckpointForRunAsync validates ownership
  under the lease; clear only after operator reconciliation, never to bypass errors.

See [file persistence contract](../../Services/Persistence/README.md) for leases,
replacement, cancellation, killed-owner, local-filesystem and power-loss limits.

## Run Completion

When RetryPolicy.CheckpointEnabled is true, both Running before import and
Completed after successful imports require ISyncPersistenceAcknowledgement.
Missing/failed startup acknowledgement admits no import. Failure at completion
returns SyncCheckpointFailureResult with the run ID, acknowledged write count,
uncertain-write flag, persistence status and RequiresReconciliation. It does not
publish Success, advance the in-memory cursor or retry even an empty run's terminal
save. Actual earlier provider writes are not represented as rolled back.

Completed retains counts/context instead of deleting evidence. A saved Completed
acknowledgement is authoritative for that checkpoint; later cancellation does not
undo it. Success/date/cursor publication follows it. SLO/alert/run-history and
property-notification exceptions are diagnostic failures, not failed writes.
DiagnosticFailed observers are independently isolated; LastRunDiagnosticFailures
returns a copy of this run's failures containing only operation/exception type.
Raw diagnostic exceptions are not logged. Failed diagnostic callbacks/loggers
cannot suppress later diagnostic stages. Audit-unsubscription failure is reported
but does not prove the external rule engine removed its subscription.

Running, Failed, stale, changed-context or unresolved partial checkpoints block restart
and preserve evidence. A context-matched Completed record allows a new run; an
unresolved different run cannot silently replace the prior record. Same-run offset
regression, reopening Completed/Failed and clearing reconciliation flags are rejected.
This is conservative run recovery, not successful-batch/key replay.

## Thresholds And Failed Runs

Enabled thresholds capture required/advisory intent before writes and run once per
attempt over measured combined import counts, including partial failures. Exact
ContinueRun/AbortRun actions and a finite numeric reject limit replace legacy
silent skips/coercion. Required errors cannot publish completion or cause blind
retry; Advisory warnings remain explicit. Record-only policies must opt out with
BatchThresholdEnabled=false. After acknowledged startup, failed/cancelled runs
publish typed FailureEvidence with actual acknowledgements under the admitted
checkpoint identity. Failed-save acknowledgements remain observable with original
counts; corrupt evidence is preserved and terminal Failed records cannot reopen.
Read [threshold/failure contract](THRESHOLDS-AND-FAILURE-EVIDENCE.md) for exact
denominator/timing, cooperative cleanup, restart, compatibility and scope limits.

## Remaining Gates

Typed import transformation failures now stop whole-run retry, including zero-write
runs, and preserve previous success dates/cursors. Earlier acknowledgements remain
visible. Read ../Importing/TRANSFORMATION-OUTCOMES.md for strict execution and custom
helper/default-catalog limits. Existing-target mapped metadata admission is locally
verified as above. Configured per-record DQ now executes over transformed forward/
reverse rows with captured record policy/context, required/advisory decisions and
separate reject-store outcomes. Read [quality admission](../Importing/QUALITY-ADMISSION.md)
for counts, Boolean result requirements, named channel provisioning and limits.
Bidirectional returned counts combine both directions and retain forward evidence
on reverse failure/cancellation. Rejected imports never publish Success or advance
cursors. Durable file reject triage/CAS and actual direction-specific row replay
are now qualified locally; read [Reject Recovery](../Importing/REJECT-RECOVERY.md).
Row replay does not complete a failed run or advance its cursor. Mapped schema
creation, complete policy intent and native/provider-run recovery remain open.

Success does not automatically save the schema/watermark snapshot. Call
SaveSchemasAsync explicitly and handle its failure, or use the helper's acknowledged
per-schema save. Checkpoint and schema writes are not one transaction; a restart
between them can leave an old cursor alongside Completed. Reconcile before replay
if the provider cannot safely deduplicate. A null/disabled retry checkpoint policy
also means no durable run checkpoint, not an equivalent recovery guarantee.

Schema promotion still mutates live state before separate version/schema saves;
its fingerprint is not complete approved execution intent. Required attempt
thresholds and failed acknowledgement evidence are locally verified, not a fix to
promotion or cursor agreement. No universal
exactly-once delivery, transactional DDL, distributed admission, bounded streaming,
cross-OS qualification or package-reader compatibility is claimed.
Legacy reconciliation/SLO scanned/insert/update totals are not complete provider
accounting; terminal checkpoint counts and failure results are the acknowledged
run evidence. Broader diagnostics/metrics accuracy remains Phase 4 work.

Tests: FrameworkReliabilityTests/SyncPersistenceTests and SyncOutcomeTests cover
typed child-process reload, concurrent storage writers, corrupt/legacy preservation,
immutable artifacts, mandatory startup/terminal acknowledgement, actual Windows
replacement denial and isolated diagnostics. See the framework implementation log
for the exact test/OS/TFM evidence; mocks do not prove live provider recovery.
