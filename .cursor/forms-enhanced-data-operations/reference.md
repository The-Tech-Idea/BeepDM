# Managed Queries And Enhanced Operations

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


## Query A Registered Block

This fragment assumes a live `FormsManager forms`, an `ORDERS` block with
`Id`, `TenantId` and `Status` declared in its metadata, and a tenant identity
obtained from the trusted host context. Imports: `System`,
`System.Collections.Generic`, `System.Threading`, `TheTechIdea.Beep.Editor.Forms.Models`,
`TheTechIdea.Beep.Report` and `TheTechIdea.Beep.ConfigUtil`.

```csharp
forms.SetDefaultWhere("ORDERS", "Id > 0");
forms.SetBlockSecurity("ORDERS", new BlockSecurity
{
    AllowQuery = true,
    RowFilterClause = "TenantId = :tenant",
    RowFilterValues = new Dictionary<string, object> { ["tenant"] = tenantId }
});

var filters = new List<AppFilter>
{
    new AppFilter { FieldName = "Status", Operator = "=", FilterValue = "NEW" }
};
var query = await forms.ExecuteQueryWithOutcomeAsync("ORDERS", filters, cancellationToken);
if (!query.ReadAcknowledged)
    return;
// Inspect publication and NotificationFailures; do not replay an accepted read blindly.

var matchingCount = await forms.CountQueryAsync("ORDERS", filters);
if (matchingCount < 0)
    throw new InvalidOperationException("Managed count failed.");
```

The fragment assumes a CancellationToken cancellationToken supplied by the host.
The staged implicit query preserves records/cursor/mode until acceptance; it is
not an ENTER_QUERY action. Explicit ENTER_QUERY retains its clearing/trigger behavior.
Resolve dirty edits explicitly; do not pre-clear or remove mandatory policy. Inspect
UsedStaging/RecordsPublished/LegacyPublicationPossible separately. Cancellation after
admission returns Cancelled after acknowledgement; pre-cancelled calls throw before
admission. Observer failure after acceptance belongs to NotificationFailures.
FormInstanceId/RegistrationId/RequestRevision identifies the request, not a complete
UI binding/record generation. Validate that target again in the host dispatcher.

`GetRecordCount` reports the loaded block, not a provider-wide count.
`CountQueryAsync` returns an int and uses -1 for failures, including overflow;
a separate long ProviderPage observation is available through FetchPageWithOutcomeAsync;
a general typed long scalar result is still open. Read PROVIDER-PAGING.md: count
evidence alone is not accepted publication. Provider prefetch/cache remains open.

## Policy Boundaries

Basic/enhanced/detail/count/source-aggregate managed routes share one fail-closed
policy compiler for the supported bounded AND grammar. Named parameters must
resolve, fields must be declared and unsupported expressions fail before execution.
Caller filters remain data; mandatory policy is supplied by a trusted authority,
not a tenant-selecting UI control.

Default owned security snapshots are copied. Injected security helpers need the
snapshot capability for policy-dependent managed reads. Do not use the legacy
permissive parser or raw SQL as a policy fallback. Source aggregates accept only
the closed declared-field grammar documented in `QUERY-POLICY.md`.

Default datasource basic/enhanced/detail UoWs/wrappers use `IStagedUnitofWorkRead` to retain prior
rows until revision/registration-authorized acceptance. Custom security helpers need
`IQuerySecurityPublication`. Do not fall back to Get after preparation fails.
Dispose the stage; post-acceptance observers belong to `NotificationFailures`, not
an unpublished read to blindly repeat. Read `READ-PUBLICATION.md` for the contract.

Legacy/custom UoWs can self-publish/pre-clear. Query/detail reads share ordering;
new same-registration query requests supersede unpublished older candidates.
Awaited nested managed reads reject rather than deadlock. Auxiliary reads, broader
scheduling and arbitrary providers remain open. `StagedReadTests.cs` and
`QueryPublicationTests.cs` exercise actual UoWs and framework-bound SQLite execution.

## Enhanced Writes

Read the current `FormsManager.EnhancedOperations.cs` implementation before
combining `CreateNewRecord`, `InsertRecordEnhancedAsync` and
`UpdateCurrentRecordAsync`; their tracking/validation/trigger steps must not be
duplicated by a UI adapter. Register generic UoWs through
`UnitOfWorkWrapper`, or supply the non-generic `IUnitofWork` contract directly.

Inspect each `IErrorsInfo` and retain warning/error context. Commit through
`CommitFormWithOutcomeAsync`, inspecting per-block/provider durability and
reconciliation requirements. Query refresh is not a save operation, and an
unknown/partial commit must not be blindly replayed.

## Validation And LOV Boundary

`RECORD-TARGETS.md` covers manager validation/LOV record/item/request capture and
observed UoW revisions. Prefer ShowLOVWithOutcomeAsync and retain AppliedFields/
SelectionEffectsPossible on failed/cancelled selection; setters are not atomic.
This does not apply block read policy to another LOV/lookup entity or qualify helper
cache/events. Raw helpers, async rule context and real adapters remain open.

## Evidence

In `DataManagementEngineStandard/Editor/Forms/`, read `QUERY-POLICY.md`,
`COMMIT-OWNERSHIP.md` and `IMPLEMENTATION-LOG.md`.
The adjacent `Forms.Tests/QueryPolicyTests.cs` and
`Forms.Tests/SqliteQueryPolicyTests.cs` exercise once-only policy combination,
denial/changed snapshots, injection values, scalar overflow and actual SQLite
read/count/aggregate agreement. External plugins, full UI adapter conformance and
provider paging still need independent qualification.

