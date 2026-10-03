# Forms Lifecycle And Detail Coordination

## Provider Page Operations

FetchPageWithOutcomeAsync is separate from LoadLocalPageWithOutcomeAsync. Read
`DataManagementEngineStandard/Editor/Forms/PROVIDER-PAGING.md`: provider fetch shares
query/detail ordering, supersession, dirty protection and physical drain. Typed
PageOutOfRange retains prior rows and does not silently re-fetch. ProviderPage
count evidence is not acceptance; inspect RecordsPublished and notification
failures before UI delivery. Long provider pages do not set local int CurrentPage.
Policy/query/registration-aware bounded prefetch/cache remains required and open.

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


Use these fragments with an existing live FormsManager and registered blocks.
For generic UoWs, register through UnitOfWorkWrapper; the host owns UoWs/providers.
Keep Engine/Models UI-neutral and marshal UI focus/notifications in the host adapter.

## Targeted Deferred Detail

The host configured CUSTOMERS -> ORDERS with Deferred coordination. Imports:
System.Threading, TheTechIdea.Beep.ConfigUtil and
TheTechIdea.Beep.Editor.UOWManager.

```csharp
var sync = await forms.SynchronizeDeferredDetailWithOutcomeAsync(
    "CUSTOMERS", "ORDERS", cancellationToken);
if (sync.Flag != Errors.Ok || !sync.AllDetailsCurrent)
    return;
// The host can now evaluate the originating binding/record before dispatching focus.
```

Inspect each Details entry: BlockedDirty requires an explicit save/discard decision;
Superseded is not a current target; Failed/Unattempted cannot be treated as empty
successful data. Deferred/no-op does not make AllDetailsCurrent true.
ProviderMayHavePublished means legacy Get was invoked or a staged read published.
RecordsPublished confirms staged acceptance; NotificationFailures reports observers
that failed after acceptance. Default datasource UoWs/wrappers prepare private
candidates and retain prior rows on pre-publication rejection. Custom security
helpers require the publication capability. Read `READ-PUBLICATION.md` for limits.
Do not silently replay a write because its post-write synchronization failed.

Requests copy relationship modes/mappings and registered targets before awaits.
A forced deferred request does not mutate Coordination or refresh immediate siblings.
Pending markers clear only after acknowledged success. Nonempty composite mapping
lists are authoritative; unreadable keys fail instead of being treated as null.
Already-dirty targets/descendants prevent reads and clears. This admission check
does not serialize arbitrary concurrent edits.

## Ordering And Close

Query/detail reads serialize within one FormsManager and reject awaited nested
reads from their own triggers/providers. Query revisions supersede older unpublished
candidates. This is not global edit/commit/navigation scheduling or a UI dispatcher. A new master request waits for an
older uncooperative Get to acknowledge; cancellation cannot forcibly stop legacy Get.

The host must coordinate other public operations before final lifetime disposal.
CloseFormAsync is logical close/unsaved-change behavior; DisposeAsync releases and
drains callbacks, registration preparation and managed query/detail operations. Do not self-drain
from those operations or block a dispatcher their continuations need.

```csharp
if (!await forms.CloseFormAsync())
    return;
await forms.DisposeAsync();
```

Check navigation Boolean/error results before changing UI focus. A false result
after a cursor/delete side effect is not proof of no effects. Use
CommitFormWithOutcomeAsync for durability/reconciliation; do not automatically
rollback/replay all providers after a failed legacy flag.

## Evidence

Read DETAIL-COORDINATION.md, LIFETIME-CONTRACTS.md, COMMIT-OWNERSHIP.md and
IMPLEMENTATION-LOG.md under DataManagementEngineStandard/Editor/Forms/.
DetailCoordinationTests exercises barriers, targeted requests, dirty branches,
captured keys/registration identity, cancellation, failure outcomes and cycle limits.
StagedReadTests and QueryPublicationTests qualify default query/detail preparation/
acceptance and framework-bound SQLite reads. Typed query outcomes include registration/
request identity and retain publication evidence through cancellation/observer errors.
`RECORD-TARGETS.md` and RecordTargetTests qualify manager validation/LOV target
checks and cursor/edit ABA for observed default revisions, plus partial-selection
and close/drain evidence. Raw helper events, async rules/context, editor completion,
arbitrary final-window edits and real adapters remain open.

