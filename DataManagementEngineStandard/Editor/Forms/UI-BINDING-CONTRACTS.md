# Opt-In Forms View Binding

Status: initial Stage E implementation, not real desktop adapter qualification or
completion of C/D. Existing IBeepFormsHost/IBlockView/IFieldPresenter APIs remain.

## Ownership And Admission

[FormsViewBinding](Helpers/FormsViewBinding.cs) is an opt-in adapter utility over
the existing host/view/presenter contracts, not a parallel Forms host. The host
already registered its block; the utility borrows host, manager, source, dispatcher
and notification service. Attach runs on the UI context with an unbound view.
It reserves one helper owner, subscribes exactly its own listeners, then binds and
queues initial rendering. Failed setup attempts remove every installed listener;
failed removals retain diagnostics and inactive callbacks cannot mutate the view.
Same-view overlap rejects without detaching the existing owner.

The manager must expose optional
[IFormsBindingTargets](../../../DataManagementModelsStandard/Editor/Forms/Hosts/IFormsBindingTargets.cs).
[BindingTargets](FormsManager.BindingTargets.cs) issues opaque immutable public
tokens with form/registration/record identity. Their captured record values, item
identity, mode/query revision and observed UoW revision use the existing
[record boundary](RECORD-TARGETS.md). Multiple tokens for the same current target
remain valid; capturing another token does not supersede unrelated work. Writes
clone private evidence and invalidate original tokens rather than rebasing them.

Record-aware checks also pin the security revision. Default field policies now copy
inputs and returned settings, and SetFieldSecurity increments SecurityRevision.
Publish changes through SetFieldSecurity; editing a GetFieldSecurity result no
longer mutates the active policy. Binding capture rejects configured security helpers
without IQuerySecuritySnapshotProvider, rather than silently dropping policy identity.
[Cached-buffer authorization](BUFFER-AUTHORIZATION.md) now rejects fresh tokens for
rows retained from an older read policy, before record-field capture. Managed
query/detail publication restores capture. Field-only policy changes invalidate
old tokens but permit a fresh remasked delivery without a provider read. Initial
scoped attachment requires managed publication; Clear or re-registration is not
authorization. Hosts must still immediately hide/clear already displayed text on
principal change when immediate privacy is required. [Policy reconciliation](POLICY-REPAINT.md)
now queues automatic clearing/remasking and drains it; raw mutations, instantaneous
privacy and real adapter qualification remain separate gates.
Registration-only checks ignore cursor/policy changes but still reject replacement,
closed manager and changed source. Tokens from another manager never authorize work.

## Dispatch And Delivery

The adapter provides optional platform-neutral
[IFormsDispatcher](../../../DataManagementModelsStandard/Editor/Forms/Hosts/IFormsDispatcher.cs).
InvokeAsync must dispatch in submission order, execute once with correct affinity,
and acknowledge physical completion/cancellation. No WinForms/WPF type is required.
The utility checks affinity, fences duplicate/late callbacks, rejects an acknowledged
but unexecuted action and drains an already-running action even if its dispatcher
task returns early. This is defensive fencing, not qualification of a real dispatcher.

Manager field changes queue a captured refresh; selected view activity relays
(CurrentChanged/PostQuery/PostCommit/PostCreate/PostDelete) refresh accepted state.
Known foreign sources reject; ItemChanged activity is not duplicated because the
manager field feed already covers it. A host must relay actual UoW PostCommit after
tracking acceptance, not relabel collection AfterSave as PostCommit. Host active-block
events request refresh. Message raised/cleared notifications preserve dispatcher order
and use the existing IFormsNotificationService/BeepViewState. They are registration-
scoped, not record-scoped; their payload content remains trusted host notification data.

Each action rechecks binding owner, host manager, view/view-state binding, pinned presenter
roster and current engine target. Changing the roster requires detach/attach; old
presenters are not refreshed or edited after replacement. FocusAsync requires an
accepted target explicitly captured at the engine operation's acknowledgement
boundary, a matching active block and enabled/visible presenter. Do not capture
a target only when a delayed focus callback finally runs. Legacy Boolean navigation
alone does not prove the record intended by a competing navigation operation.

FormViewDeliveryResult separates Delivered/Cancelled/Superseded/Rejected/Failed
from EffectsPossible and Acknowledged. Rendering is not multi-field atomic; setters
or focus may have effects before cancellation/reentrant changes or failure. Only
manager ApplyBindingValue is an edit acknowledgement; none of these states describe
database durability. Outcomes retain the most recent 128 deliveries. Adapters must
surface rejected user edits and failures, not assume the control value was persisted.

## Presentation And Feedback

Rendering uses current block/item/field-security state, honors insert versus update
tracking, hides invisible/denied data, disables readonly/criteria/masked edits and
applies current item errors. The host masks values; null is legitimate, never raw
fallback. A failed mask clears the affected value if the target is still current and
records failure. Byte buffers are copied before masking and presentation. Opaque
nested reference values are not deep-copied or proven safe by this utility.

Presenter events write the captured record through ApplyBindingValue, not a later
host CurrentItem. Writes enforce current permissions and preserve possible/acknowledged
effects. They follow the manager's normal item-change validation path, not custom
host-specific setter hooks. Adapters with additional edit pipelines must integrate
those explicitly before adopting this helper. QueryValue/criteria editing is not
handled as a record write by this helper.

Legacy presenters get per-presenter nested origin suppression for inline SetValue
echoes, not global suppression of unrelated user edits. For deferred echoes implement
optional [IOriginAwareFieldPresenter](../../../DataManagementModelsStandard/Editor/Forms/Hosts/IOriginAwareFieldPresenter.cs).
Programmatic SetValue carries a binding origin and increasing revision; relayed
events preserve it. User events use Guid.Empty/zero. Programmatic echoes, including
ones from retired bindings, never reenter record writes. Legacy deferred echo/user
events without provenance remain unqualified, as do adapters which mislabel origin.

## Detach And Remaining Gates

Dispose runs on the UI context, closes admission first, cancels queued work and
unsubscribes/Unbinds only its captured owner. DisposeAsync closes immediately, dispatches
cleanup, then waits for physical delivery acknowledgement. Do not self-drain: dispose
synchronously from a delivery and drain from the host afterward. Manager close/rebind
invalidates queued work; it does not dispose this borrowed adapter helper. Dispose
the binding from the host lifecycle. Failed dispatcher cleanup is retained in
CleanupFailures; repair on the UI context rather than claiming physical detachment.

The [behavior fixture](../Forms.Tests/HostBehaviorTests.cs) uses real default UoWs,
existing host/view/presenter interfaces and a recording manual dispatcher. It tests
worker affinity/order, retirement/cleanup, focus identity/cancellation, nullable edits,
origin echoes, policy revision and mask failures, copied buffers, plain/dictionary
records and committed tracking with a failed UI notification. Plain legacy UoWs
still have weaker observed/value/reference checks, not canonical ABA proof.

Full real host routing, attach/Unbind callback side effects, trigger/focus order,
asynchronous user-event record provenance, all-public scheduling/lifetime, graph/
metadata ownership, synchronous policy privacy/error-completion repaint, arbitrary final-window edits,
auxiliary provider/cache authorization and concrete desktop adapters remain gates.
Do not simultaneously keep an adapter's old subscriptions/edit bridge and this
helper's bridge; migrate deliberately and run this checklist with the actual adapter.

## Adapter Qualification Checklist

The local fixture qualifies the helper, not an adapter that merely implements the interfaces.
Record adapter/version/TFM and run these behavior gates before release:

| Gate | Required Adapter Evidence | Local Fixture |
| --- | --- | --- |
| E-01 | No worker-thread UI getters/setters; dispatch each notification once in order. | WorkerNotificationsDispatchOnceInOrderWithoutWorkerUiAccess |
| E-02 | Attach/unwind/detach owns only its subscriptions and binding; borrowed manager/source survive. | DetachIsIdempotentAndUnsubscribesOnlyItsCapturedListeners; FailedSubscriptionAttachUnwindsViewAndListenersWithoutRetiringManager |
| E-03 | Old record/registration/view-state/presenter callbacks cannot refresh a rebound view or focus stale items. | QueuedOldBindingCannotRenderOrNotifyAfterRetirement; ChangedViewStateDoesNotReceiveOldBindingNotifications; PresenterReplacementInvalidatesOldRosterWithoutWritingRemovedControls |
| E-04 | Accepted engine target precedes focus dispatch; cancelled/refused focus preserves truthful acknowledgement. | CapturedFocusCannotFocusAChangedOrCancelledRecord; FocusRefusalIsRejectedAndNeverAcknowledged |
| E-05 | Nullable values, byte copies, runtime row types and policy/masking/error delivery behave as declared. | NullUserEditIsCapturedAndOldTokenCannotReplayTheWrite; MaskingNullOrFailureNeverFallsBackToRawPresentation; BindingPreservesPlainAndDictionaryRuntimeRecordsWithoutEntityCoercion |
| E-06 | Inline/deferred/retired programmatic echoes do not write back; unrelated user events remain admitted with record provenance. | UnrelatedUserEditInsidePresentationIsNotGloballySuppressed; OriginAwareDelayedAndRetiredBindingEchoesNeverReenterRecordWrites |
| E-07 | PostCommit is relayed after tracking acceptance; UI notification failure is not displayed/replayed as an unsaved database write. | NotificationFailureDoesNotTurnAcknowledgedDatabaseCommitIntoAnUnsavedWrite |
| E-08 | Dispatcher/dialog cancellation and close drain physical work; duplicate/late/failed callbacks have actionable evidence. | AsyncDetachWaitsForQueuedPhysicalAcknowledgement; DuplicateOrLateDispatcherExecutionCannotTouchControlsTwice; RecordTargetTests editor cancellation cases |
| E-09 | Custom trigger/navigation/query-criteria pipelines preserve their full callback/focus semantics without a duplicate legacy bridge. | External adapter evidence still required; local helper is not full host routing |
| E-10 | Policy publications automatically reconcile without raw reads/discard; latest revisions/owners win, clearing failures are visible and physical drain is honored. Immediate privacy must hide/lock before principal switch. | PolicyPublicationAutomaticallyClearsPresentationWithoutReadingOrDiscardingRows; FieldOnlyPublicationAutomaticallyRepaintsCurrentAuthorizedBuffer; QueuedPolicyCannotClearAnotherBindingOwner; FailingPrivacyClearReportsPartialEffectsWithoutUndoingPolicyOrRows |
