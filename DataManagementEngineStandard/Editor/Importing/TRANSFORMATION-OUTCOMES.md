# Import Transformation Outcomes

## Strict Built-In Execution

DataImportTransformationHelper implements the additive Models-owned
IDataImportTransformationOutcome. TransformRecord returns ImportTransformationResult
with a stage, output record on success and exception type only on failure. The
batch helper prefers this contract; a null/failed outcome never falls back to the
legacy callback or writes the original input. A failure includes no record payload,
raw exception message or inner exception in the returned batch diagnostics.

Projection, mapping, configured defaults and custom transformation are required
when enabled. A missing destination map, failed getter/setter/conversion, missing
default metadata, unresolved non-empty default rule, unknown field transform or
null custom result rejects that row before a provider call. Disabled defaults do
not execute. Static defaults use the configured definition, not a second catalog
lookup; dictionaries, ExpandoObject, DataRow and readable/writable public POCO
properties are supported. Reflection/default failures do not route through the
legacy Util setter, which may hide failed assignments.

MapObjectToAnotherStrict maps fields without implicit defaults and does not use
legacy compiled plans that can swallow field failures. Import configuration owns
the separate default stage and ApplyDefaults switch. Conversion defaults to Reject
in this path; deliberately registered WarnAndContinue/UseFallback and mapping
skip/null policies remain explicit opt-ins, not errors the importer overrides.
Legacy MappingManager.MapObjectToAnother remains best-effort and is not equivalent.

## Outcome And Cancellation

RecordsTransformationFailed is a subset of RecordsFailed, not WriteAttempts or
HasUncertainWrites. It aggregates through DataImportManager. Earlier provider
acknowledgements remain RecordsSucceeded; a later transformation failure yields
Partial, never Completed. Record retry cannot rerun a failed transformation.
BeepSync stops whole-run replay for a typed transformation failure even with zero
acknowledged rows and leaves success dates/cursors unchanged.

Cancellation is checked before and after each configured stage and after a custom
helper returns. Cancellation during transformation produces Cancelled without an
uncertain provider write. This cannot interrupt a synchronous callback or undo a
mutation it already made to caller-owned objects. A callback is executable host
code, not a sandbox or a rollback transaction.

## Compatibility And Remaining Gates

Existing IDataImportTransformationHelper signatures remain unchanged. Built-in
ApplyTransformationPipeline and configured standalone stage methods now propagate
safe ImportTransformationException instead of returning the input on failure.
Callers relying on silent fallback must handle this stricter behavior explicitly.
Custom legacy helpers still execute once; thrown/null results fail and exceptions
are sanitized, but the engine cannot detect a helper that hides failure internally.
Custom typed helpers must accurately acknowledge their own transformations.

Normal QualityRules and sync DqPolicy.RuleKeys now execute in a dedicated
post-transform/pre-write stage; read [quality admission](QUALITY-ADMISSION.md) for
required/advisory decisions, counts and ownership. This does not complete data
quality/default-catalog governance. Captured required/advisory sync attempt
thresholds and failed counts are now verified separately; read
[threshold/failure contract](../BeepSync/THRESHOLDS-AND-FAILURE-EVIDENCE.md).
Required manager/direct-batch catalog loading now denies failed/ambiguous lookup.
Required row resolution normalizes once through an editor-owned registry, bypasses
metadata-only caching and denies reported error/warning fallbacks without raw logs.
Read [defaults admission](DEFAULTS-ADMISSION.md) for exact compatibility and limits:
run-owned defaults refresh implicit catalogs without populating caller configuration,
copy closed literals and retain both declared sync catalogs before provider-opening
validation. Required roster/priority snapshots and per-row SentData also retain
admitted intent across source/provider registration edits; nested wrapper failures
stay sticky and named lookups use admitted definitions. Full immutable policy/plugin
context, grammar/custom-plugin/logging
and provider qualification remain open. Sync binds captured real metadata into both destination lists
and validates pairs/required targets before either direction writes. Bound configs
require an existing target and never infer mapped DDL; ordinary unbound imports
retain legacy validation/creation behavior. Read ../BeepSync/STORAGE-AND-OUTCOMES.md
for the exact existing-target contract. Record enforcement uses the separate stage
above; durable reject/operator/provider qualification remains.

No row-level rollback, durable reject channel, exactly-once replay, bounded source
memory, all-platform support or package-consumer compatibility is established here.
The initial 29 local cases used recording providers and precompiled mapping targets.
Strict mapping tests now use actual ClassCreator/Roslyn-generated objects; additional
generated-type/cache regressions cover changed definitions and concurrency. Read
ConfigUtil/GENERATED-TYPES.md for bare-cache override changes, exact type selection
and assembly-retention limits. SyncMappingAdmissionTests now adds 27 cases per TFM
with actual translated forward/reverse payloads and fail-closed metadata admission.
This still does not prove mapped schema creation or live providers.

See tests/FrameworkReliabilityTests/ImportTransformationTests.cs,
SyncTransformationOutcomeTests.cs and the framework implementation log for evidence.
