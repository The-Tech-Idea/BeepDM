# Record Quality Admission

## Actual Write Boundary

Normal built-in DataImportManager/DataImportBatchHelper execution now evaluates
QualityRules and optional Models IImportRecordAdmission after the complete required
transformation/default/custom pipeline and before the first InsertEntity attempt.
Admission is once per transformed record, not once per provider retry. A failed
transformation never invokes record quality. This is not CustomTransformation
acting as a gate, and standalone DataQualityEvaluator/replay remains a separate
legacy path with different semantics.

DataImportManager captures ordered rule references, field names/actions, failure
mode, observed timeout, error-store reference and context before initialization or
target creation. The same session spans all batches. Direct batch calls capture
once per call. The captured descriptors are private, but executable rule objects,
their closures, custom stores/gates, providers and record payloads remain trusted
host-owned mutable implementations. Coordinate capture and runtime ownership;
this is not complete immutable intent, a sandbox or a rollback boundary.

## Decisions And Counts

- Required is the default QualityFailureMode. False predicates use captured
  Block/Quarantine/Warn actions; missing/ambiguous fields, getter/predicate failures
  and observed timeout deny a row unless Advisory was explicitly selected.
  Advisory evaluation failure permits writing but increments evaluation-failed and
  warning counters. Warn is not a rejection and does not increment RecordsFailed.
- Field access supports string-key dictionaries, ExpandoObject, DataRow and public
  POCO properties. Names match case-insensitively; ambiguous or absent fields fail
  rather than being silently treated as null. Explicit null/DBNull values reach
  the rule as null. Rule.FailureMessage is not invoked and raw rule/store messages
  are not placed into admission errors.
- RecordsQualityEvaluated counts admitted-to-quality records once, even with
  multiple rules. RecordsQualityRejected and required evaluation failures are
  included in RecordsFailed. RecordsBlocked counts explicit Block; RecordsWarned
  counts warned records once. These are not WriteAttempts or uncertain target writes.
  Advisory evaluation failures can coexist with Completed by explicit policy.
- Quarantine requires a supplied store before target mutation. RecordsQuarantined
  increments only when SaveAsync succeeds; RejectStoreFailures separately counts
  failed/unknown/cancelled reject saves. A failed reject save never admits the row.
  Optional Block stores are also attempted and their failures remain observable.
- Results include QualityAdmissionFailed for invalid admission configuration and
  RequiresReconciliation when a failed/partial/cancelled result includes earlier
  acknowledgements or uncertain target writes. Status exposes blocked/quarantined/
  warned totals. No reject is silently converted into a successful processed row.

Limits: at most 128 ordinary rules, timeout 1..60000 ms. Time/cancellation are
observed at synchronous boundaries; a hung plugin cannot be forcibly interrupted.
Unknown mode/action, null rule, missing Quarantine store or failed gate validation
reject before source fetching/target DDL. Runtime RecordAdmission is excluded from
System.Text.Json/Newtonsoft JSON and XML configuration serialization. Legacy custom
batch helpers without the actual admission path cannot run configured quality and
are rejected before mutation; their old interface/signatures remain unchanged.
Custom gates must return a non-null closed ImportRecordAdmissionResult and honestly
validate their dependencies. The engine cannot detect implementations hiding errors.

## Sync Integration

Sync captures DqPolicy.RuleKeys, RecordFailureMode, OnRecordFailure, schema identity,
rule-engine reference and depth/time limits before import. Both directions use a
dedicated gate over actual transformed/generated payloads and the correct target
entity name. Required missing engines/rules and lookup errors deny sync before
target mutation. SolveRule must return an actual Boolean, not a coerced string,
absent value or arbitrary object. Each evaluation gets a new RuleExecutionPolicy;
mutable static execution profiles are not used for record admission.

Explicit errorStore takes precedence over a complete configured reject channel.
Partially specified channels reject. Named channels resolve to an already-open,
existing entity before sync import; no reject-channel DDL is inferred. Captured
provider inserts must return Errors.Ok. Built-in DataSourceImportErrorStore now also
checks write acknowledgement instead of ignoring it. Custom SaveAsync returning
success is still a trusted acknowledgement, not proof of commit/durability.

Rejected/required failed rows stop whole-sync retry even with zero target writes,
leave old success dates/cursors unchanged and prevent completion publication.
Bidirectional results combine actual forward/reverse counts; reverse failure or
cancellation does not hide earlier forward acknowledgements. Store failures remain
separate from uncertain target writes. Defaults are the existing import pipeline,
not a second call through SyncIntegrationContext.DefaultsManager or the legacy
FillDefaultsBeforeEval flag. Lower-level catalog/resolver qualification is open.

## Remaining Gates

Sync now separately captures required/advisory attempt thresholds, enforces a real
attempted-row denominator and strict action outputs, and publishes acknowledged
failed-run checkpoint counts. RecordFailureMode remains record-only. Threshold
success never overrides required record rejection. Read
[threshold/failure contract](../BeepSync/THRESHOLDS-AND-FAILURE-EVIDENCE.md)
for explicit record-only opt-out, empty attempts, failure-save statuses and limits.

Save acknowledgement is not an atomic target/reject transaction. Raw transformed
records are intentionally passed to a configured reject store: hosts own access,
privacy, retention and encryption. Built-in capture now adds durable run/reject
identity and a closed destination snapshot. The file store supports atomic
operator preparation/claims and actual acknowledged row replay; read
[Reject Recovery](REJECT-RECOVERY.md) for state, compatibility and restart limits.
The named provider sink does not infer provider-specific load/triage/replay.
Native/custom adapters, full provider/run recovery and live-provider commit/recovery
remain P1-12/P2-06 qualification. Existing fallback
default/resolver diagnostics, rule implementation safety and all-route redaction
remain open. No clean-pack, Unix or package-consumer qualification is claimed.

Tests: FrameworkReliabilityTests/ImportQualityAdmissionTests (33 cases) and
SyncRecordQualityTests (18 cases) per configured TFM, with actual ClassCreator/
Roslyn mapped payloads, dependency failures, mutation, cancellation, strict outputs,
record shapes, defaults, observed limits, store failures and combined acknowledgements.
