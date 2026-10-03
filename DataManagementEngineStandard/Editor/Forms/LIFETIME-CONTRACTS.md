# Forms Lifetime And Timer Contracts

Status: incremental Stage C implementation, not a full C/D/E conformance closeout.
The engine and Models contracts remain platform-neutral.
The [local paging increment](LOCAL-PAGING.md) also joins callback admission/drain,
links cancellation and waits for physical trigger acknowledgement before gate
retirement. This does not turn every general navigation operation into an admitted
or globally scheduled command; no full lifetime stage closes.

## Closing A Manager

[Dispose](FormsManager.Lifecycle.cs) closes callback admission before cleanup,
removes registered blocks from lookup, and shares direct UoW subscription teardown
with UnregisterBlock. Teardown targets the source captured when subscribing, even
if a caller later changes DataBlockInfo.UnitOfWork. Each cleanup action is isolated;
CleanupFailures retains up to 128 recent exceptions with resource names.
Repeated synchronous disposal is idempotent and does not wait for another close.

Default timer/performance/item helpers are owned and disposed. Injected instances are
borrowed: this manager detaches its notifications but does not dispose the helper
or any UoW/datasource. A retained injected timer helper may still have running
timers; its owner must manage those lifetimes. Name-wide deletion cannot safely
establish per-manager timer ownership in a shared helper.

[CallbackLifetime](FormsManager.CallbackLifetime.cs) tracks the manager's timer,
global bus, item-change and current-change handlers, including awaited work, and
RegisterBlock preparation through its cleanup acknowledgement.
Managed basic/enhanced/typed queries, their mode-transition wrapper and public
detail traversals join this count/drain with linked lifetime cancellation; see
[captured detail coordination](DETAIL-COORDINATION.md) and [read publication](READ-PUBLICATION.md).
Public LOV operations and synchronous field/block validation also join accounting;
LOV and typed-value validation use captured targets and linked close cancellation.
See [record targets](RECORD-TARGETS.md) for helper acknowledgement and partial-write limits.
Managed message Action handlers are tracked for their synchronous invocation.
DisposeAsync (IAsyncDisposable) waits for teardown and those admitted callbacks/
preparations/query/detail/LOV operations to finish. PendingCallbackCount includes these.
WaitForPendingCallbacksAsync(token) permits a host to cancel waiting; it does not
cancel the underlying provider/trigger or prove it stopped. When admission is
still open, the wait covers the captured callback set, not future callbacks.

Do not await disposal/drain from an owning callback, registration preparation or admitted manager operation.
That would await itself;
the runtime closes then rejects self-DisposeAsync explicitly. Call Dispose from
the callback and let the host drain after it returns. Do not block a UI dispatcher
while draining work that may need that dispatcher. There is no forced timeout or
invented completion for a noncooperative provider/trigger.

This is not yet a scheduler/drain for every public async Forms operation. Pending
scalar queries/commit/navigation and pre-RegisterBlock source-factory work need the C/D admission and
generation model. Callers must still coordinate those operations during close.
Legacy UoW Get may publish its own data after the manager closes; default staged
reads discard unpublished candidates after acknowledgement. Async-void delegates supplied as an Action are not awaitable
through that legacy Action contract and are not certified by callback draining.

## Registration And Teardown

Block identities use OrdinalIgnoreCase, consistent with relationships/security.
GetBlock's registry is authoritative: a shared or stale performance cache cannot
invent another registered block. Item/LOV continuations recheck captured block and
UoW identity; a replaced/disposed registration cannot receive their late item error.
Default EventManager marks a stored subscription inactive before attempting all
recorded base and optional removals. Removal failures aggregate
instead of skipping later removals; queued inactive delegates no longer dispatch.
An accessor that refuses removal can still retain a delegate; the failure is
evidence, not a claim that physical detachment succeeded.

The optional [IOwnedUnitOfWorkEventSubscriptions](../../../DataManagementModelsStandard/Editor/Forms/Interfaces/IOwnedUnitOfWorkEventSubscriptions.cs)
returns a lease for one complete captured-source subscription. Default EventManager
implements it, and Forms uses leases instead of name-keyed unsubscribe when
available. Same-name managers can share this helper without overwriting teardown
ownership. This does not scope the helper's public event stream to one manager;
host observers still receive the shared helper's events. Default EventManager also
implements [IGatedUnitOfWorkEventSubscriptions](../../../DataManagementModelsStandard/Editor/Forms/Interfaces/IPreparedBlockItems.cs).
Forms supplies captured registration admission: a prepared or retired candidate
cannot dispatch helper events. This nonthrowing predicate runs outside the helper
monitor and consults only owned registry state.

Attachment records cleanup before invoking each accessor, including accessors
that attach and then throw. Delegates remain inactive until the preparation
finishes. Failure unwinds every attempted attachment and propagates the original
error, or aggregates it with teardown errors. A present optional accessor failing
is not treated as a missing optional event. Missing public optional events are
skipped; arbitrary dynamic/explicit-interface event exposure is not certified.

The legacy name-keyed API now uses captured-source cleanup and preserves an old
subscription if preparation of its replacement fails. Failure retiring an old
subscription after publishing the new one is reported with the new subscription
still live; callers cannot infer that every thrown replacement had no effects.
Subscription failures now propagate rather than being logged and silently ignored.
Use owned leases for independent owners. Default helper lease disposal is
idempotent; an already-admitted event observer may finish after retirement.

UnregisterBlock retires lookup and any same-name preparation before leave/cleanup
notifications. The removed
block is therefore not queryable from a leave callback. Its Boolean is false when
cleanup fails, even though registry removal occurred; inspect CleanupFailures.
No UoW/datasource Dispose is performed by this teardown.

## Prepared Registration And Replacement

[Registration lifetime](FormsManager.RegistrationLifetime.cs) reserves each name
through preparation/cleanup acknowledgement. Overlapping or reentrant same-name
changes reject without waiting; unrelated names can prepare independently. The
previous root remains live while metadata, configuration, subscriptions, cache
and item candidates prepare. No provider/accessor/helper/observer runs under the
manager's publication monitor.

Default [ItemPropertyManager](Helpers/ItemPropertyManager.Registration.cs) implements
[IPreparedBlockItems/IBlockItemsRegistration](../../../DataManagementModelsStandard/Editor/Forms/Interfaces/IPreparedBlockItems.cs).
Commit pairs the candidate item store/tab order with an owned-memory-only registry
publication action. If that action fails, it restores the exact prior item store;
item identity, dirty/error/focus/rule state and tab order survive failed replacement.
Retiring an old item lease cannot remove the new lease's store. This is in-memory
publication, not a database transaction or an immutable runtime graph.

Replacement requires gated owned event subscriptions and prepared item helpers;
unsupported injected helpers throw NotSupportedException before source writes or
attachments. Legacy helpers still support fresh registration with recorded,
best-effort cleanup; hidden helper side effects are not qualified. Implementations
of the optional capabilities must honor their preparation/rollback/admission
contracts. Owned subscriptions without dispatch gating do not qualify replacement.

Shared default item helpers reject the same block name bound to different form
instances rather than aliasing their stores. Use distinct helper instances or
names. Event leases can share names, but do not namespace the shared event stream.
Public ItemInfo/DataBlockInfo objects remain mutable; Blocks is a read-only
dictionary snapshot, not a transitive metadata/configuration snapshot.

Close/unregister revoke pending publication immediately. Physical cleanup waits
until an in-flight external setup call acknowledges completion, then attempts
every recorded cleanup once. Async drain includes that acknowledgement; it cannot
forcibly stop a noncooperative provider. Prepared UoW metadata/source assignments
are conditionally restored on failure only if still reference-identical and never
published. Successful metadata assignments remain with the borrowed UoW.

After complete publication, old teardown and leave/enter/log observers are isolated:
their failures stay in CleanupFailures and do not undo the new registration or
throw a misleading no-effects result. The legacy void API has no typed notification
outcome. Leave events carry only a name, not a generation; UI event-order/identity
conformance remains open. Already-admitted work may finish after retirement.

Relationships/history are retained on successful replacement, but role/key/schema
revalidation, full form/system-variable rollback, transitive graph pinning and
record identity still require A/D work. Owned current-block variables are cleared
when registration teardown leaves no current block; this is not a complete state
rollback. All-public-operation admission/cancellation and host dispatcher behavior
remain C/D/E gates. See AtomicRegistrationTests for the exact covered boundaries.

## Owned Message Subscriptions

The optional [IOwnedFormMessageSubscriptions](../../../DataManagementModelsStandard/Editor/Forms/Interfaces/IOwnedFormMessageSubscriptions.cs)
returns a lease for exactly one handler. Default FormMessageBus implements it.
Forms captures that lease and disables/removes its own handlers on explicit
unsubscribe or disposal, regardless of another manager sharing the form name.
UnsubscribeFromMessage is instance-scoped, not a name-wide bus unsubscribe.
Renaming a manager does not prevent eventual disposal of prior-name leases.

Legacy buses without leases receive a guarded wrapper: closure payloads are cleared
and late delivery becomes inert. The bus may retain the inactive wrapper until its
owner clears subscriptions; physical removal cannot be proved by the old interface.
Global OnFormMessage event ownership is detached separately. Dispatcher affinity,
sensitive payload handling and full notification error results remain Stage E work.

## Default Timer Semantics

[TimerManager](Helpers/TimerManager.cs) uses TimeProvider (system by default) and
retains its parameterless constructor. Tests inject a manual scheduler; no sleeps
are needed to reproduce queued old callbacks and activation races.

Timers are prepared disabled, published by entry identity, and activated without
calling schedulers/observers under the ownership monitor. Early ticks wait for
activation acknowledgement. Invalid intervals and serial failed replacement
activation preserve the prior timer. A superseded or retired callback cannot fire
or remove a same-name replacement. Overlapping failed replacements do not restore
a retired/disposed entry; preserving the original across that overlap is not a
transactional replacement guarantee.

Definitions returned by CreateTimer/GetTimer/GetAllTimers are snapshots, not control
handles. Mutating one does not change owned scheduling or callback metadata; reread
GetTimer to obtain current repeating-timer state. One-shot entries expire before
notification. Delete/dispose reject queued callbacks; already-admitted observers
may finish. Synchronous overlapping ticks during a slow notification coalesce,
not queue indefinitely. An async-void subscriber's later work is outside this
synchronous dispatch guard; Forms tracks its own async timer handler separately.

Observer/scheduler cleanup failures are isolated and CallbackFailures retains up
to 64 recent failures. Each handler is attempted even when another observer throws.
An unsuccessful physical timer disposal remains a diagnostic failure; logically
retired callbacks cannot republish the removed entry. This does not qualify custom
ITimerManager implementations or claim timer-trigger ordering on a UI dispatcher.

The optional [view-binding helper](UI-BINDING-CONTRACTS.md) has a separate adapter-
owned dispatch lifetime. Manager close invalidates its targets but does not dispose
borrowed adapter bindings. Host cleanup must detach/drain them; manager callback drain
is not proof that an external UI dispatch queue is empty.

See [implementation evidence](IMPLEMENTATION-LOG.md) and
[remaining reliability stages](RELIABILITY-AND-ENHANCEMENT-PLAN.md).
