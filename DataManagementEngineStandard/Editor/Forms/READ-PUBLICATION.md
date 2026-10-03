# Staged Managed Read Publication

Status: incremental B/C/D implementation. Default UoW basic/enhanced/detail reads
are staged. Clear operations, all-operation scheduling and external adapters remain
unqualified by this increment.

## Capability And Ownership

Optional [IStagedUnitofWorkRead](../../../DataManagementModelsStandard/Editor/IStagedUnitofWorkRead.cs)
prepares a private candidate without replacing live records. The default
[UnitofWork implementation](../UOW/UnitofWork.ReadStage.cs) and
[wrapper](../UOW/UnitOfWorkWrapper.cs) expose this capability for datasource reads.
List mode is unsupported; an overridden Get does not implicitly opt in. Derived
adapters must deliberately implement their staged semantics, not bypass custom Get.

Dispose every prepared stage, including after publication. It retains admission
against overlapping read/commit, collection/source replacement and legacy Get/Clear
until disposal. Direct edits/cursor movement remain possible but observed changes
invalidate the stage. Preparation, rejection and unpublished disposal retain the
previous collection/cursor/tracking. No borrowed prior collection, datasource or UoW
is disposed by the read stage or manager. Null provider results/rows, failed mapping,
PreQuery cancellation and dirty records reject, not masquerade as empty success.

Preparation awaits IDataSource.GetEntityAsync. That existing interface has no token
overload: cancellation rejects at safe boundaries but waits for provider acknowledgement.
There is no early WaitAsync abandonment, fabricated provider stop or automatic retry.
Tenant-scoped staged reads append their authoritative predicate independently of a
caller predicate on the same field. Filters passed to the provider are copied.

Candidate record shells have independent Entity observers. Typed rows are shallow
copies; arbitrary nested values/custom events and transitive metadata graphs are
not deep-isolated. Dictionary/POCO conversion rejects invalid conversions rather
than silently dropping rows. Missing record fields retain their default values.
Unbounded eager materialization is not provider paging or a memory-budget guarantee.

The separate [bounded provider capability](PROVIDER-PAGING.md) reuses the same
owned ReadStage publication but reads a bounded UTF-8 scalar-row response. Its
exact declared-field, row/byte/count and complete-key validation is stricter than
the eager shell/POCO path described above. Neither path enables prefetch/cache.

## Authorization And Publication

[Detail coordination](FormsManager.DetailCoordination.cs) uses staging when supported,
with no legacy fallback after preparation/authorization fails. It rechecks captured
master identity/keys, registration, policy and observed dirty state before acceptance.
The default security helper implements optional
[IQuerySecurityPublication](../../../DataManagementModelsStandard/Editor/Forms/Interfaces/IQuerySecurityPublication.cs):
its revision gate combines with manager registration retirement and the UoW's short
owned-pointer publication action. Injected helpers without this capability reject
staged managed reads before provider execution. Implement the contract or retain an
explicitly legacy UoW path; never remove mandatory policy to restore a read.

Publish's authorizer must synchronously invoke its supplied action once on the
calling thread or throw without invoking it. No providers, triggers, observers,
metadata getters or host dispatchers run inside these ownership monitors. A saved
action expires after authorization. After swapping, observer/history failures are
isolated and reported as NotificationFailures; PostQuery cannot undo publication.
Publication is not database durability and does not make a failed write replay-safe.

Detail outcomes expose RecordsPublished only for confirmed staged publication.
ProviderMayHavePublished is true after a staged publication or legacy Get invocation;
it is not proof of database writes. Refreshed with NotificationFailures means rows
were accepted, not an unpublished query to blindly rerun. Close drains the physical
provider acknowledgement and discards unpublished candidates; it does not dispose
the borrowed UoW. UI-affinity/notification presentation remains the adapter's job.

## Query Pipeline And Outcomes

[QueryExecution](FormsManager.QueryExecution.cs) is the shared basic/enhanced query
pipeline. Optional [IFormsQueryOutcomes](../../../DataManagementModelsStandard/Editor/Forms/Interfaces/IFormsQueryOutcomes.cs)
exposes ExecuteQueryWithOutcomeAsync(filters, token) and
[FormQueryResult](../../../DataManagementModelsStandard/Editor/Forms/Models/FormQueryResult.cs).
FormInstanceId, canonical BlockName, RegistrationId and RequestRevision identify
the request; revisions are monotonic within one registration, not across rebinds.
ReadAcknowledged/Completed does not by itself assert staged publication: inspect
RecordsPublished, UsedStaging and LegacyPublicationPossible separately.

Query and detail reads share one manager-local ordering gate; awaited nested reads
from their triggers/providers reject rather than wait on their own gate. Captured
query filters/policy/registration precede queueing. A newer same-registration query
supersedes older unpublished candidates, even if that newer request later cancels.
Already-published rows are not undone; stale/cancelled later observers are reported
separately. This is not a navigation/edit/commit scheduler or a UI dispatcher.

Default staged implicit execution retains records, cursor and mode until acceptance,
then publishes CRUD mode with the owned row swap. It is not an ENTER_QUERY action
and does not synthesize EnterQuery/BlockEnter events. Explicit EnterQueryModeAsync
still validates/clears criteria buffers and fires its EnterQuery trigger. Dirty
captured targets/details block implicit execution without automatic save/discard.
The mode-transition convenience wrapper retains publication evidence through
post-load validation/navigation warnings and joins lifetime drain; navigation's
internal record/lifetime guarantees are still separate work.

Pre-cancelled or post-close typed calls reject before admission. Cancellation after
admission returns Cancelled/Failed after physical acknowledgement, retaining possible
legacy publication evidence. Cancellation/observer failure after staged acceptance
retains Completed/RecordsPublished with NotificationFailures. Basic Boolean and
enhanced IErrorsInfo methods project this outcome; they do not blindly replay reads.
Legacy null Get is failure/unknown publication, not successful empty data.

## Remaining Gates And Evidence

[Cached-buffer authorization](BUFFER-AUTHORIZATION.md) now installs default UoW
opaque buffer receipts inside accepted managed query/detail publication, before
observers. UI target capture checks the read-policy/provider/buffer evidence before
record getters; failed/legacy reads do not certify a new-context buffer. This is
not protection of arbitrary raw row mutations or every public Forms operation.

Legacy/custom UoWs can pre-clear implicit mode buffers or publish before late
rejection, including after close. The default staged path has no fallback after
preparation/publication failure. This is not a global scheduler
for navigation/edit/commit, another manager or raw data access. Mutable metadata,
arbitrary concurrent edits between observed-change checks, master changes during
the final publication window, LOV/validation record generations and custom helpers
still require further work. Do not infer those guarantees from reference checks.
The [record-target boundary](RECORD-TARGETS.md) now protects manager validation/LOV
annotations and selection against captured target changes; raw helper events,
auxiliary read policy and final-window concurrent edits remain separate gates.

[StagedReadTests](../Forms.Tests/StagedReadTests.cs) uses actual default UoWs/wrappers,
barriers and a SQLite ADO.NET adapter with the framework filter builder/binder.
It tests rejection/abort, changed targets/dirty edits, single publication, observer
failure, copied filters/tenant boundaries, policy gate rejection, close/cancellation
and queued new-master ordering. This does not qualify external provider plugins,
real desktop controls or Unix. [QueryPublicationTests](../Forms.Tests/QueryPublicationTests.cs)
extends actual-UoW/SQLite coverage to all query routes, preserved implicit mode,
supersession, copied queued filters, dirty branches, final publication rejection,
notification isolation, shared query/detail ordering and lifetime drain.
See [implementation evidence](IMPLEMENTATION-LOG.md).
