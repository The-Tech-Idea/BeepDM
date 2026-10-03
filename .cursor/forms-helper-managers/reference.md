# Forms Helper Ownership And Teardown

## Bounded Provider Reads

Read `DataManagementEngineStandard/Editor/Forms/PROVIDER-PAGING.md` and
`Forms.Tests/ProviderPagingTests.cs`. Opt-in default UoW/wrapper pages use the
existing query/detail gate, policy publication, dirty checks and buffer receipts;
physical provider work joins callback drain. No producer/getter/observer runs
under publication monitors. Unbounded Get is not a fallback after rejection.
Metadata caching is not a policy-authorized row-page cache; prefetch/cache remains open.

## Permission Configuration And Projection

Read `DataManagementEngineStandard/Editor/Forms/PERMISSION-PROJECTION.md`.
Existing block permission and item Enabled/Visible setters author configuration;
getters combine it with a registration-owned policy overlay. Same-value writes
under denial still configure restrictions. Clearing policy/admin does not lift
authored false; ItemInfo.Clone copies configuration rather than runtime denial.
Live DTO serialization reports effective flags, not authored definition persistence.

Manager projection visits all live registrations, including removed rules and
preinstalled registration policy. Default optional IItemSecurityProjection pins
the registry/item identities; lock order is helper registry -> security revision ->
manager registration. Keep publication owned-memory-only and callbacks outside
monitors. Isolate observer failures and stop older notifications after replacement
or newer policy. This is not whole-graph/configuration or check-to-use atomicity.
Custom helpers need independent qualification; legacy item projection lacks
registry gating/notifications. Raw item replacement does not auto-publish policy.
Clearing a rule still requires accepted managed query/detail authorization before
UI refresh. Preserve queued privacy/drain and native adapter gates.
Tests: `Forms.Tests/PermissionProjectionTests.cs` and `HostBehaviorTests.cs`.

## Opt-In UI Binding

In the BeepDM checkout read `UI-BINDING-CONTRACTS.md` before adapter work.
`FormsViewBinding` uses existing host/view/presenter interfaces, not a new host.
Attach on the UI context with a registered block and unbound view; do not retain
an adapter's old edit/subscription bridge alongside this opt-in bridge.

```csharp
await using var binding = FormsViewBinding.Attach(host, view, dispatcher, notifications);
var focus = await binding.FocusAsync(acceptedTarget, "Notes", cancellationToken);
if (focus.State != FormViewDeliveryState.Delivered)
    return;
```

Imports: TheTechIdea.Beep.Editor.Forms.Helpers, .Hosts and .Models. The dispatcher
implements IFormsDispatcher; notifications use the existing IFormsNotificationService.
Supply acceptedTarget at the engine acknowledgement boundary, never recapture a
record in a delayed focus callback. Manager targets require IFormsBindingTargets
and a security snapshot revision capability. Registration-only checks are not
record/focus authorization. Rejected delivery may still carry acknowledged effects.

Use IOriginAwareFieldPresenter for deferred programmatic echoes; user events use
Guid.Empty/zero. Legacy presenters only suppress inline per-presenter echoes.
Default field-security settings are copies: publish changes through SetFieldSecurity.
Preserve null masks and copied byte buffers; never substitute raw values on mask failure.
DisposeAsync closes admission and drains physical dispatch; do not self-drain or
infer an empty UI queue from manager callback drain. Relay actual UoW PostCommit,
not collection AfterSave, and keep durable write results separate from UI failures.
Run the E-01 through E-10 checklist against each real adapter. Query criteria/custom
edit hooks, arbitrary view state, error completion and asynchronous user-event provenance remain open.

## Cached UI Buffer
Read `BUFFER-AUTHORIZATION.md` for held-row binding after policy changes.
Default managed query/detail publication installs an opaque read-policy/buffer
receipt before observers; fresh UI capture fails before record getters when that
receipt is stale. Initial scoped attachment also needs accepted publication.
Clear or re-registration does not certify rows. Field-only SetFieldSecurity expires
old delivery tokens but allows fresh remasking without a provider query. Custom
readers need IUnitofWorkReadBufferIdentity and IUnitofWorkReadBufferStage qualification.
Read `POLICY-REPAINT.md`: default/facade policy feeds now queue clearing of revoked
presentation or remasking of authorized buffers, without querying/discarding rows.
The binding owns and drains its coalescing pump; inspect partial failures and use
RequestPolicyReconciliationAsync for deliberate UI reconciliation without republishing.
Immediate privacy still requires hiding/locking the surface before principal switch:
queued work or a refused setter is not proof that text was erased. Custom helpers
without a feed need facade/host refresh. Raw rows/caches, custom messages/state,
async error completion and policy flag/configuration ownership remain separate gates.

## Captured Editor Popup


Use optional `IFormsEditorOutcomes`/`ShowEditorWithOutcomeAsync` for popups:

```csharp
var edit = await forms.ShowEditorWithOutcomeAsync("ORDERS", "Notes", cancellationToken);
if (!edit.Committed && edit.WriteEffectsPossible)
    return; // Reconcile possible effects; do not replay provider OK blindly.
```

Requires a registered editable Notes field. Imports: System.Threading and
TheTechIdea.Beep.Editor.UOWManager. Provider acknowledgement, provider OK,
setter attempt and setter acknowledgement are separate evidence. Legacy
ShowEditorAsync returns OK only for accepted current completion. Failed/denied/
superseded results withhold text but may retain write effects. Check the state
and ErrorMessage; do not treat every rejected outcome as user cancellation.

The manager captures record/item/request/definition identity, copies popup inputs,
and enforces insert/update permissions, disabled/hidden/read-only and raw-text
masking restrictions before disclosure and write. Masked popups reject even for
admins. Borrowed providers own UI dispatch/dismissal and must acknowledge physical
completion before close/drain can finish. Do not self-drain from a provider.
Read RECORD-TARGETS.md in the checkout for compatibility and limits: policy/definition
ABA, unobserved/final-window edits and full host/view/presenter conformance remain open.


## Borrowed Timer And Message Helpers

This fragment assumes a valid `dmeEditor` and a synchronous
`Action<FormMessage> onChanged` supplied by the host. Imports:
`System`, `TheTechIdea.Beep.Editor.Forms.Helpers`,
`TheTechIdea.Beep.Editor.Forms.Models` and
`TheTechIdea.Beep.Editor.UOWManager`.

```csharp
using var timers = new TimerManager(TimeProvider.System);
var bus = new FormMessageBus();
await using var forms = new FormsManager(dmeEditor,
    timerManager: timers, messageBus: bus);

if (!await forms.OpenFormAsync("Orders"))
    return;

forms.SubscribeToMessage("Changed", onChanged);
forms.CreateTimer("RefreshReminder", TimeSpan.FromMinutes(1), repeating: true);
// The host runs its form session here.
forms.UnsubscribeFromMessage("Changed");
forms.DeleteTimer("RefreshReminder");
```

Disposal occurs in reverse declaration order: Forms closes/drains before the
host disposes its timer helper. Injected timer/performance helpers and UoWs are
borrowed, not disposed by Forms. When multiple managers borrow one timer helper,
coordinate names and timer ownership in the host: no per-manager timer lease is
currently available. This fragment does not subscribe UI refresh work or certify
timer-trigger ordering on a dispatcher.

## Timer Invariants

`TimerManager(TimeProvider)` permits deterministic scheduler injection. Returned
timer definitions are snapshots, not control handles; modify scheduling through
Create/Delete rather than editing those objects. A retired queued callback cannot
fire or expire its same-name replacement. Single replacement activation failure
preserves the prior timer; overlapping failed replacements do not promise
transactional preservation of the original.

One-shot entries expire before notification. Synchronous overlapping repeating
ticks coalesce. Already-admitted observers may finish after deletion; async-void
observer continuations are not covered by that synchronous dispatch guard.
Inspect `CallbackFailures` for bounded scheduler/observer cleanup diagnostics.

## Event And Message Ownership

Direct UoW subscriptions detach from the originally captured source, not a later
mutable `DataBlockInfo.UnitOfWork` value. Default EventManager implements
`IOwnedUnitOfWorkEventSubscriptions`, returning a lease for each captured-source
subscription, including same-name owners. Forms disposes exactly its lease.
Delegates stay inactive during preparation, and failure unwinds every attempted
add, including accessors that attach then throw. Present optional accessor failures
propagate; they are not silently treated as missing events.

Lease disposal deactivates delegates before independently attempting recorded
removals. Accessor failure can leave an inactive delegate physically retained;
inspect aggregate cleanup evidence rather than claiming physical detachment.
The helper's public event stream remains shared, not owner-scoped. Legacy
name-keyed replacement can publish its new subscription before failure retiring
the old one; do not assume every thrown replacement had no effects.

Default FormMessageBus implements `IOwnedFormMessageSubscriptions`.
Forms unsubscribe/disposal removes only that manager's leased handlers, not
another manager sharing its form name. A legacy bus receives an inert guarded
wrapper after close but may retain the wrapper physically. Do not use
`UnsubscribeAll(formName)` as instance-specific cleanup.

Keep `Action<FormMessage>` handlers synchronous; async-void Actions cannot be
drained through the legacy signature. A host needing awaited work must own and
track that work separately.

## Prepared Block Registration

Default EventManager also implements `IGatedUnitOfWorkEventSubscriptions`.
Forms gates candidate events by captured registration identity before publication
and after retirement. Owned-only custom helpers do not qualify replacement.
Default ItemPropertyManager implements `IPreparedBlockItems`; its stage commits
items/tab order and a pure owned-memory registry action together, restoring the
exact prior store when publication fails. Lease retirement cannot delete a
replacement's items. No provider/helper/observer belongs in that publication action.

Same-name concurrent/reentrant changes reject without waiting. Prior lookup,
handlers and item state remain live while preparation runs. Close/unregister
revoke pending setup; physical cleanup and async drain wait for its acknowledgement.
Do not self-drain from preparation. Unpublished UoW metadata/source assignments
are conditionally restored; successful assignments stay with the borrowed UoW.

Legacy helpers retain fresh-registration best-effort cleanup only; replacement
rejects before source writes/attachments without both optional capabilities.
Shared default item helpers reject cross-form same-name bindings, not transparently
namespace them. Use separate helpers or names. Post-publication observers/old
teardown cannot undo a complete registration; inspect `CleanupFailures`.

This is in-memory publication, not database durability or a transitive snapshot.
Relationship/schema revalidation, full form/system-variable rollback, all-public-
operation lifetime and unchanged-block record generations remain open. Already-
admitted work may finish after retirement; arbitrary helper behavior is unqualified.

## Read Publication Helper Contract

Default SecurityManager implements `IQuerySecurityPublication`. Its revision gate
accepts only synchronous owned-memory publication, not provider reads, metadata
getters or observers under its monitor. Forms combines this with registration
retirement, query supersession and default UoW staged publication. Custom security helpers without the
capability reject staged managed reads before provider execution. Do not retry with
legacy Get after staging fails or omit policy. Read `READ-PUBLICATION.md` and
`Forms.Tests/StagedReadTests.cs` for qualification and remaining gates.

## Captured Validation And LOV

Read `RECORD-TARGETS.md` and `Forms.Tests/RecordTargetTests.cs`. Manager paths pin
record/collection/item/request identity before helpers or awaits, and recheck before
error annotation/selection. Default UoW canonical revisions precede cursor/item/
collection callbacks; wrappers retain support identity after disposal and fail closed.
Do not substitute the staged-read audit revision: a later audit notification of the
original edit would incorrectly invalidate its own awaited validation.

Copy related mappings/selected values before load; never fetch a new CurrentItem
after await to apply the old selection. Retain typed AppliedFields and possible
setter effects after partial failure. Expected own setter notifications may advance
only that captured request; nested edits/navigation stop subsequent fields. No
setters/observers/providers under ownership monitors. Auxiliary lookup policy,
helper events, async rules/context, full edit scheduling and host conformance remain open.

## Qualification Fixtures

Read `LIFETIME-CONTRACTS.md` and `IMPLEMENTATION-LOG.md` in
`DataManagementEngineStandard/Editor/Forms/`.
`Forms.Tests/LifetimeAndTimerTests.cs` uses a manual TimeProvider, explicitly
queued stale callbacks and barriers to exercise activation failures, source
replacement, teardown failures, callback drain and same-form message ownership.
These default-helper tests do not qualify arbitrary injected helpers or UI
dispatchers.

`Forms.Tests/SubscriptionOwnershipTests.cs` exercises base/optional attachment
failure, exact attempted removals, aggregate failure evidence, inactive prepared
callbacks, same-name leases/managers, fresh-registration cleanup and retirement
while a factory returns.

`Forms.Tests/AtomicRegistrationTests.cs` covers default failed replacement retaining
item identity/dirty/error/focus/rule/tab state, blocked setup close/unregister,
same-name rejection, candidate event gating, metadata restoration and identity-
based retirement. Its boundaries are not all public operations or UI conformance.

