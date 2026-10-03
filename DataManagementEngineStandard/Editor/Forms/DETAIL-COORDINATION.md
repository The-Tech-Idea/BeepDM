# Captured Detail Coordination

Status: an incremental C/D implementation, not full operation/UI conformance.

## Typed Outcomes And Compatibility

FormsManager implements optional
[IFormsDetailSynchronization](../../../DataManagementModelsStandard/Editor/Forms/Interfaces/IFormsDetailSynchronization.cs):
SynchronizeDetailBlocksWithOutcomeAsync and SynchronizeDeferredDetailWithOutcomeAsync
return [DetailSynchronizationResult](../../../DataManagementModelsStandard/Editor/Forms/Models/DetailSynchronizationResult.cs).
Each captured detail reports Refreshed, Cleared, Deferred, BlockedDirty,
Superseded, Missing, Failed or Unattempted, with form instance/name identity.
AllDetailsCurrent requires a nonempty set of successfully refreshed/cleared details;
an empty/no-op or deferred request cannot claim that a read occurred.

The existing Task-returning sync methods now throw InvalidOperationException for
incomplete results, rather than silently logging read failure and permitting a
dependent navigation/delete to proceed as if synchronization succeeded. Cancellation
remains OperationCanceledException. Use the typed capability for individual branch
diagnostics; IUnitofWorksManager members are unchanged. A failure after a delete/
navigation side effect does not establish no effects or permission to replay it.
Broader typed mutation outcomes remain open.

## Request And Queue Ownership

[DetailCoordination](FormsManager.DetailCoordination.cs) captures registered block/
UoW references, relationship coordination and independent field pairs before its
first await. Root current-record identity and invariant key values are captured
then rechecked at queue admission, trigger completion and read/clear boundaries.
Descendant record values are captured after their parent refresh; their block and
relationship targets remain those originally captured. Later changes to public
relationship objects do not redirect an already captured request.

Traversals serialize within one manager, including across different master roots.
A slow A and queued B cannot perform overlapping detail Get calls; once A
acknowledges, B can leave its own details last. The gate is not FIFO-guaranteed or
a global scheduler for edit/commit/navigation, another manager, or raw UoW
calls. Basic/enhanced queries now share its managed read gate. A nested query/detail
read awaited from a managed trigger/provider is rejected
immediately instead of waiting on its own gate. Schedule follow-up work after the
current operation. No ownership monitor is held across a provider/trigger/host call.

Field-pair lists are authoritative when nonempty; string key parsing is the fallback.
All composite fields participate in invariant filters. An absent/unreadable runtime
property fails, not clears. A genuinely null/empty master key clears the immediate
subtree; deferred branches remain untouched until explicitly forced. Cycles, more
than 256 blocks or more than 128 hierarchy levels fail before provider execution.
This bounded traversal is not qualification of the commit dependency graph.

## Deferred And Dirty Details

Forced deferred synchronization targets only that requested relationship/subtree.
It never changes the live Coordination property or refreshes immediate siblings.
The pending marker remains until acknowledged successful read/clear; failure,
cancellation or dirty state does not prematurely clear it. Marker changes and
sync suppression use captured registration identity, so retired work cannot
silence or clear a replacement's state. Unregister prunes incoming/outgoing
relationships even though its block has already left lookup.

Any already-dirty target/descendant blocks a branch before Get or Clear. No automatic
save/discard or UI prompt occurs; the host must explicitly resolve unsaved changes.
Independent clean branches can finish and retain individual outcomes. A failed
parent's descendants remain unattempted rather than queried from stale parent rows.
This conservative admission check does not serialize arbitrary concurrent edits
or prove that a custom provider preserves dirty state while publishing a read.

## Cancellation And Publication Limits

Default datasource UoWs/wrappers now prepare detail reads through
[staged publication](READ-PUBLICATION.md). An unpublished stale/cancelled/policy-
rejected candidate leaves prior records intact. The optional security revision gate
and registration retirement protect the owned pointer swap. RecordsPublished marks
confirmed acceptance; NotificationFailures do not undo accepted rows or require
blind replay. Custom security helpers need the publication capability before this
path can call the provider. No fallback occurs after staged preparation fails.

Detail traversals join the manager's admitted callback/preparation count and drain.
Dispose closes admission, cancels queued/active traversals and rejects later ones.
ON-POPULATE-DETAILS receives the linked caller/lifetime token. Awaited providers
without cancellation must acknowledge before async disposal/drain can finish;
there is no forced timeout or fabricated provider stop. The private cancellation
source/gate retire after all admitted work acknowledges, including synchronous
close whose outstanding callbacks finish later. Do not self-drain from a traversal.

Legacy IUnitofWork.Get has no cancellation or staged-publication contract. It may
publish Units before late stale/security/cancellation rejection, including after
close. ProviderMayHavePublished records that Get was invoked (or a stage published), even if its subsequent
outcome is failed/superseded. A null Get result is failure/unknown publication,
not an empty successful result. Providers must return a nonnull empty collection
for a successful zero-row query. Clear acknowledgement likewise does not certify
all effects of a custom Clear implementation.

Broader cross-operation generations, validation/LOV record identity,
typed navigation/delete side effects, adapter dispatcher behavior and all-public-
operation cancellation remain open. Do not infer those guarantees from this queue.
See [lifetime](LIFETIME-CONTRACTS.md), [query policy](QUERY-POLICY.md),
[tests](../Forms.Tests/DetailCoordinationTests.cs), [real UoW staging tests](../Forms.Tests/StagedReadTests.cs) and
[qualification log](IMPLEMENTATION-LOG.md).
