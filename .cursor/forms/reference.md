# Forms Registration And Host Integration

## Provider Page Delivery

Read `DataManagementEngineStandard/Editor/Forms/PROVIDER-PAGING.md` when integrating
FetchPageWithOutcomeAsync. ProviderPage is a long count/page observation, not an
accepted UI page until RecordsPublished is true. Use captured binding identity
and host dispatch; do not write local CurrentPage from remote page numbers.
Bounded staged fetch now exists; prefetch/cache and real adapters remain open.

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

```csharp
forms.SetFieldSecurity("ORDERS", "Notes", new FieldSecurity { Editable = false });
var notes = forms.ItemProperties.GetItem("ORDERS", "Notes");
notes.Enabled = false; // Configure a restriction even though policy already denies.
forms.ClearBlockSecurity("ORDERS");
// notes.Enabled remains false; a managed read is still required before UI refresh.
```

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


## Register Existing Units Of Work

The host supplies live non-generic `TheTechIdea.Beep.Editor.IUnitofWork`
instances and `IEntityStructure` metadata. For a generic `UnitofWork<T>`, pass
`new UnitOfWorkWrapper(typedUow)` from `TheTechIdea.Beep.Editor.UOW`, not the
generic UoW directly. The host retains ownership of the UoWs and data sources.

This fragment assumes those inputs and a valid `dmeEditor` already exist:

```csharp
await using var forms = new FormsManager(dmeEditor);
forms.RegisterBlock("CUSTOMERS", customerUow, customerStructure,
    "MyDatabase", isMasterBlock: true);
forms.RegisterBlock("ORDERS", orderUow, orderStructure,
    "MyDatabase", isMasterBlock: false);
forms.CreateMasterDetailRelation("CUSTOMERS", "ORDERS", "Id", "CustomerId");

if (!await forms.OpenFormAsync("CustomerOrders"))
    return;

var query = await forms.ExecuteQueryAndEnterCrudModeAsync("CUSTOMERS");
if (query.Flag != Errors.Ok)
    throw new InvalidOperationException(query.Message);
```

Imports: `System`, `TheTechIdea.Beep.Editor.UOWManager` and
`TheTechIdea.Beep.ConfigUtil`. Block names are case-insensitive; relationship
fields must exist in the supplied metadata. Default registration stages item/root
publication and preserves the prior registration/item state on preparation failure.
Close/unregister revoke pending setup; cleanup/drain waits for setup acknowledgement.
Same-name concurrent/reentrant changes reject instead of waiting.

Replacement requires `IGatedUnitOfWorkEventSubscriptions` and `IPreparedBlockItems`;
legacy injected helpers only support fresh registration. Shared default item helpers
reject same-name bindings from different form instances. Successful replacement
does not certify relationship/schema revalidation or full system-state rollback.
Post-publication notification/cleanup failures retain the new registration; inspect
`CleanupFailures`. `Blocks` snapshots the dictionary, not its mutable block values.

## Commit Edited Records

After the host has edited records through the appropriate registered-block
operations, obtain a typed durability result rather than trusting only a flag:

```csharp
var outcome = await forms.CommitFormWithOutcomeAsync();
if (outcome.RequiresReconciliation || outcome.HasPartialCommit)
{
    // Preserve tracking and route to provider-aware reconciliation; do not replay.
    return;
}
```

Inspect `Blocks`, `DataSources`, `UsesIndependentCommits`, `Flag` and
notification diagnostics for the actual outcome. A no-write commit does not make
`AllWritesCommitted` true. Failure after a durable write is not permission to
repeat that write. Multiple independent providers do not imply distributed ACID.

## Host Close And Notifications

`CloseFormAsync` is a logical form operation with unsaved-change handling;
`Dispose`/`DisposeAsync` release manager lifetime resources. The host must coordinate
remaining scalar/commit/navigation operations before final disposal. Async disposal
drains callbacks, registration acknowledgement, managed query/detail/LOV operations
and synchronous manager validation,
not every public operation or source-factory work.

If closing inside a manager-owned callback or registration preparation, use synchronous `Dispose` and let
the host await `DisposeAsync` after that callback returns. Do not synchronously
block a dispatcher required by pending callbacks. Inspect `CleanupFailures`.

Default datasource basic/enhanced/detail UoWs/wrappers stage reads before publication.
Read `READ-PUBLICATION.md`: rejected implicit queries retain records/cursor/mode;
`RecordsPublished` and `NotificationFailures` separate acceptance from observers.
Typed query cancellation returns an outcome after admitted work acknowledges;
pre-cancelled calls reject before admission. Request revisions belong to one
RegistrationId. The host must still validate its binding/record before UI dispatch.

Forms events do not establish UI-thread affinity. Keep dispatcher marshaling,
display lifetime and UI cancellation in the host adapter; do not introduce
WinForms/WPF dependencies into Engine or Models for this integration.

## Captured LOV Selection

Read `RECORD-TARGETS.md` before changing manager validation/LOV targets. The typed
API preserves load and partial setter evidence through stale/cancelled completion.
This fragment assumes host-supplied object selected and CancellationToken cancellationToken:

```csharp
var lov = await forms.ShowLOVWithOutcomeAsync("ORDERS", "CustomerId",
    selectedRecord: selected, cancellationToken: cancellationToken);
if (!lov.Success)
{
    // Preserve AppliedFields/SelectionEffectsPossible and reconcile; do not blindly replay.
    return;
}
// Recheck the adapter's binding/record before presentation or focus.
```

Default observed UoW revisions reject cursor/edit ABA. Legacy sources have weaker
notification/value/reference checks; opaque graphs and concurrent check-to-setter
edits are not qualified. Raw helper events/cache and provider policy remain separate.

## Source And Evidence

In the BeepDM checkout, consult
`DataManagementEngineStandard/Editor/Forms/{COMMIT-OWNERSHIP,QUERY-POLICY,LIFETIME-CONTRACTS}.md`.
Behavioral fixtures live under `DataManagementEngineStandard/Editor/Forms.Tests/`
in `CommitOwnershipTests.cs`, `QueryPolicyTests.cs` and
`LifetimeAndTimerTests.cs` and `AtomicRegistrationTests.cs`, with real SQLite qualification in the corresponding
SQLite tests. The current plan/log, not historical completion labels, define the
remaining gates.
