# Beep Forms Reliability And Enhancement Plan

Review date: 2026-10-03. Status: implementation in progress; Stages A-F are not closed.

Scope: `FormsManager` in BeepDM and its platform-neutral UI integration contracts.
WinForms/WPF controls, designer presentation and the separate IDE are not implementation
targets in this plan. External adapters must eventually run the conformance suite;
their current behavior has not been inspected or qualified here.

This is the current safety/conformance track, not a replacement for the historical
[feature roadmap](.plans/enhancement-plan.md) or its phase completion records. Existing
capabilities are not missing merely because their failure semantics need improvement.
Do not use historical scores or "all gaps resolved" statements as release evidence.

## Review Findings

Priorities: P1 = high-risk correctness/security fix before new capabilities;
P2 = reliability, contract or scalability improvement. These are source findings,
not claims that every provider or UI adapter reproduces the same failure.

| ID | Priority | Evidence And Impact | Required Direction |
| --- | --- | --- | --- |
| BF-01 | P1 | Original review found nested ownership and early tracking acceptance. [Enlisted UoW](../UOW/UnitofWork.Enlistment.cs) and the [current coordinator](FormsManager.TransactionCoordination.cs) now stage tracking under one owning provider transaction. See current qualification and remaining Stage A gates below. | Explicit owned/enlisted transaction contract; staged changes accepted only after the owning transaction confirms commit. |
| BF-02 | P1 | Original review found begin/commit exits without complete cleanup or partial-write evidence. The [current coordinator](FormsManager.TransactionCoordination.cs) now records per-block/provider outcomes, cleanup failures, unknown acknowledgements and replay guards. Provider-owned handles and remaining qualification are still open. | Per-provider/block outcome ledger and cleanup on every exit; preserve uncertainty and reconciliation requirements. |
| BF-03 | P1 | Original review found enhanced/detail bypasses. The [managed read boundary](FormsManager.ManagedReadPolicy.cs) now covers basic/enhanced/detail/count/aggregate reads with fail-closed filters and snapshot rechecks. [Current limits](QUERY-POLICY.md) include legacy late publication, auxiliary reads/caches and provider qualification. | One mandatory query-policy builder for every block read, including refresh, count and future provider paging. |
| BF-04 | P1 | Original review found empty/all-success prefixes incorrectly accepted after missing targets or lookup exceptions. [Current dirty save](Helpers/DirtyStateManager.cs) now retains a result for every requested target and rejects incomplete success. Typed no-effects guarantees, not message text, control retries. | Exactly one terminal result per captured target; missing targets, exceptions and unattempted blocks prevent aggregate success. |
| BF-05 | P1 | Original review found missing direct UoW teardown and first-error cleanup aborts. [Current lifetime](LIFETIME-CONTRACTS.md) captures subscription sources, retires lookup/admission, isolates teardown and drains manager-owned callbacks/registration acknowledgement. Full public-operation lifetime/cancellation and failed accessor physical detachment remain open. | Shared registration teardown, close admission first, cancel/drain async work and isolate cleanup failures. |
| BF-06 | P2 | [Registration](FormsManager.BlockRegistration.cs) and [leases](FormsManager.RegistrationLifetime.cs) now prepare before default item/root publication, preserve the prior registration on failure and revoke pending setup on close/unregister. Same-name overlap rejects; replacement requires gated events/prepared items. [Limits](LIFETIME-CONTRACTS.md) include legacy helpers, mutable graph/relationship revalidation, full system state and public-operation lifetime. This stage is not closed. | Prepare then atomically replace with rollback of unpublished resources; define consistent names and reject collisions. |
| BF-07 | P2 | [Current timers](Helpers/TimerManager.cs) admit callbacks by entry identity, prepare disabled and guard disposal; manual-scheduler tests cover queued stale callbacks and replacement failures. Forms event callbacks are tracked/drained. [Limits](LIFETIME-CONTRACTS.md) include already-admitted work, custom helpers and full public operations. | Entry generations, atomic publication/removal, lifecycle guards and deterministic callback tests. |
| BF-08 | P2 | [Captured detail coordination](DETAIL-COORDINATION.md) now shares query/detail ordering, targets deferred requests without shared mode mutation, preserves dirty branches and returns typed outcomes. [Default staged queries](READ-PUBLICATION.md) preserve implicit records/mode and supersede unpublished requests. Root identity/keys and registration targets are rechecked. Legacy Get still self-publishes and lacks a token; other public operations and validation/LOV generations remain open. | Serialize conflicting operations or stage results before generation-checked publication; force one relationship without mutating global configuration. |
| BF-09 | P2 | Original review found page state published before unchecked navigation/cancellation. [Current local paging](LOCAL-PAGING.md) captures authorized buffer/registration/request/configuration, verifies cursor acknowledgement, uses exact long math and publishes typed evidence. [Opt-in provider fetch](PROVIDER-PAGING.md) now stages bounded UTF-8 rows/count under mandatory policy and complete key ordering. Bounded prefetch/cache and external provider qualification remain open. | Preserve/document local cursor paging; introduce capability-gated provider paging separately and publish page state only after success. |

Additional review work, not yet proven defects: all-record validation coverage,
field-level authorization/masking routes, dependency cycles and same-name blocks
across forms, record-lock release, savepoint semantics, cross-form call-stack
reentrancy, audit-store failures and configuration aliasing. These require focused
tests rather than assumptions based on API names.

## Contract Decisions

1. Keep the engine UI-agnostic. Contracts/models live in
   [Models/Editor/Forms](../../../DataManagementModelsStandard/Editor/Forms);
   runtime orchestration and helpers stay in Engine. Do not add desktop assemblies
   or platform-specific dispatcher types to either public Forms contract.
2. Retain existing interfaces and behavior-compatible overloads where possible.
   Introduce optional capability interfaces/typed results rather than adding
   mandatory members to every `IDataSource`, `IUnitofWork` or host implementation.
   New stricter behavior must be documented; legacy Boolean/error APIs project
   the typed result without collapsing partial/unknown outcomes into success.
3. Distinguish mutation authority from notification. Before-operation triggers
   can cancel within documented boundaries; after-commit observers cannot reverse
   durability. Report notification/audit failures separately from write failure.
4. Use captured `(form instance, block identity, registration generation)` targets,
   not flattened block-name strings. Pin provider and metadata for an operation;
   use the existing metadata snapshot facilities where suitable, not duplicate
   copies with weaker ownership. Capture effective configuration before awaits.
5. Transaction outcomes must distinguish committed, rolled back, failed-before-write,
   unattempted, partially applied and unknown/reconciliation-required. Do not
   retry a timeout/connection error solely by matching message text when writes
   may already have occurred. Never promise ACID across independent providers.
6. The manager's operation scheduler and host's UI dispatcher are separate
   responsibilities. Serialize conflicting engine mutations; marshal presenters,
   focus, dialogs and view notifications through the adapter. Do not hold locks
   while calling a user trigger, provider or host callback. Define nested-operation
   admission explicitly so triggers cannot deadlock awaiting their own gate.
7. A configured security policy applies to every managed entry point. Capture
   context/policy revision; validate supported filter grammar and parameter binding
   before provider reads. Raw UoW/data access is a trusted low-level escape hatch,
   not proof of database authorization. Masked presentation/export paths must not
   silently substitute raw values after masking fails.

## Ordered Implementation Backlog

Stages A-F and the test-matrix/initial skill portion of G are in progress; bounded provider prefetch/cache, external qualification and release gates remain open.
Effort S/M/L is relative implementation complexity,
not a delivery-date promise. Roles identify responsibility, not assigned people.
Implement each stage with tests before advancing; avoid a rewrite of the facade.

| Stage | IDs / Owner / Effort | Deliverables And Dependencies | Exit Gate |
| --- | --- | --- | --- |
| A: Transaction reliability | BF-01, BF-02, BF-04; Forms + UoW/provider maintainers; L | Capture commit targets; add optional transaction enlistment/staged acceptance; typed aggregate result; every-exit cleanup; safe retry classification. Dependent on UoW/provider transaction semantics, not migration implementation. | Real UoW + strict provider test proves one transaction per shared datasource, no early tracking acceptance, no leaked open transaction, and accurate partial/unknown outcomes. At least one live relational provider qualifies atomic rollback before claiming it. |
| B: Query policy consistency | BF-03; Forms + security/provider maintainers; M | Central authorization/filter composition reused by basic/enhanced queries, count and detail refresh; inventory LOV/record groups/export for declared policy scope. Start regression tests alongside A; finish before provider paging. | Denied reads never call provider. Tenant filter survives every route and caller filter combination; malformed/unresolved policy fails before read without broadening access. |
| C: Lifetime And Registration | BF-05, BF-06, BF-07; Forms maintainer; M | Registration lease and unified teardown; owned vs borrowed helpers/UoW/bus subscriptions; lifetime cancellation; safe async drain; timer identity; atomic replacement; consistent key rules. | Dispose/re-register fault and race tests show no leaked handlers, replacement timer corruption, half-registration, borrowed-helper disposal or post-close state mutation. |
| D: Operation And Validation Ordering | BF-08 + additional validation/dependency review; Forms maintainer; L | Form/block generation scheduler; captured record identity for validation/LOV; immutable relationship request; dirty-detail policy; deterministic commit graph using composite identities; cancellation at admission and safe boundaries. Requires C lifetime model and A result semantics. | A-slow/B-fast navigation leaves B details; stale LOV/validation cannot annotate another record; dirty details are never silently replaced; cycles and reentrant commands terminate with actionable results. |
| E: Host Conformance | Host contracts; Models + adapter maintainers; M | Executable fake dispatcher/host/view fixtures around existing contracts; additive optional scheduling/lifetime/error capabilities where needed; documented attach/detach, event order, focus and dialog semantics. Design alongside C/D; gate on their completion. | Tests exercise behavior, not just reflection signatures; worker-thread notifications dispatch once, detach invalidates callbacks, cancellation does not focus stale fields, and committed-with-notification-error is not displayed as an unsaved write. |
| F: Paging And Performance | BF-09; Forms + provider maintainers; M/L | Clarify local paging; typed long-count/overflow-safe page math; clamp after count shrink; optional bounded provider paging with deterministic ordering and mandatory policy; bounded prefetch/cache keyed by query/policy/generation. Requires B/C/D. | Local cancellation/failed navigation preserves old page. Provider page tests verify bounded rows/bytes, stable ordering, policy-aware cache invalidation and no stale completion or dirty-buffer replacement. |
| G: Closeout And Release | Docs/tests/skills; maintainers; S/M | TFM test matrix; reproducible dependency versions; targeted analyzer baseline; migration guide and source-backed Forms skill paths; truthful capability matrix with provider/adapter qualification evidence. | All gates linked to test IDs/results. No unsupported parity/atomicity/paging claims; independent provider/UI adapters remain explicitly unqualified until tested. |

## UI Integration Contract Work

Use [IBeepFormsHost](../../../DataManagementModelsStandard/Editor/Forms/Hosts/IBeepFormsHost.cs),
[IBlockView](../../../DataManagementModelsStandard/Editor/Forms/Hosts/IBlockView.cs),
[IFieldPresenter](../../../DataManagementModelsStandard/Editor/Forms/Hosts/IFieldPresenter.cs)
and existing notification/navigation contracts as the starting point, not a new
parallel host API. Review the full call surface before selecting optional extensions.

The opt-in [view-binding increment](UI-BINDING-CONTRACTS.md) now adds captured
manager targets, optional dispatcher/origin capabilities and a reusable adapter
utility over these contracts. Its executable fixture qualifies that utility and
publishes E-01 through E-10 adapter gates; it does not qualify real host routing.

- Binding is a generation-bound lease: attach once, detach idempotently, unsubscribe
  exactly the lease's subscriptions, and reject stale callbacks after rebind/close.
- Document whether events are engine-thread or host-dispatched. UI adapters own
  affinity; fake dispatcher tests must assert this without loading WinForms/WPF.
- Navigation and editing return an explicit result before changing focus, page,
  item-error or current-block display. Capture record identity, not just index.
- LOV/editor/alert requests carry cancellation and originating record/block
  generation. Apply accepted values only if the target remains valid and editable.
- Prevent recursive presenter-to-manager-to-presenter loops using origin/revision
  markers, not global suppression that discards unrelated edits.
- Preserve runtime record types and metadata ownership during binding. Support
  multiple record types without assuming every bound row derives from `Entity`;
  qualify existing reflection/generic paths before changing them.
- Inventory field reads, exports, audit payloads and diagnostics for sensitive raw
  values. Host-supplied role strings are not independently verified identities.
- Publish an adapter conformance checklist; real control integration remains an
  external acceptance dependency, not something reflection tests prove.

## Required Test Matrix

Extend [Forms.Tests](../Forms.Tests) rather than creating an unrelated test harness.
Retain the existing smoke/reflection tests, but add behavior-focused suites:

| Suite | Minimum Cases |
| --- | --- |
| CommitOwnershipTests | Real UoW with two blocks sharing one strict datasource; nested begin rejected; second-block failure; generated-key restoration and pending tracking; distinct providers; begin throws after one opened; commit null/failure/throws; rollback reports failure; partial file writes; observer/audit throws after durable commit; unknown commit acknowledgement is not replayed. |
| SaveOutcomeTests | Missing/unregistered captured block; absent UoW; exception before result; StopOnFirstError with earlier success; every captured block accounted for; same block names in two forms; deterministic dependency cycles/order. |
| QueryPolicyTests | Basic/enhanced/detail/deferred/count routes; Query-mode enhanced call; denied query; row filter with caller OR conditions; missing policy parameter; invalid grammar; role/context revision changes during async work; cache cannot cross tenant scope. |
| LifetimeAndTimerTests | Retained UoW after dispose; detach/re-register while callback waits; cleanup helper throws; shared borrowed helpers remain usable; create after dispose rejected; queued old timer callback cannot fire/remove replacement; no timing-based sleeps for race assertions. |
| AtomicRegistrationTests | Failed replacement retains prior root, handlers and item identity/dirty/error/focus/rule/tab state; candidate events stay gated; close/unregister revoke blocked setup and drain acknowledgement; same-name overlap/reentrancy reject; unsupported helpers and cross-form item aliases reject without disturbing the prior owner. Full graph/system-state rollback remains open. |
| CoordinationAndValidationTests | Barrier-controlled out-of-order requests; deferred sibling mode unaffected; failed detail Get reported; dirty details preserved; noncurrent dirty records validated; LOV completion after cursor change; composite/null/culture-invariant master keys; nested trigger admission/cancellation. |
| DetailCoordinationTests | Captured serialized A/B requests; targeted deferred reads; no premature marker removal; dirty detail/descendant preservation; composite keys; failed/null/unreadable inputs; close/queue cancellation; exact suppression identity; cycle/size limits and reentrant rejection. Other public operations and validation/LOV generations remain open. |
| StagedReadTests | Actual default UoW/wrapper prepare/accept/abort; preserved prior rows/cursor/tracking on stale/cancel/dirty/policy/registration rejection; physical provider drain; observer failures after publication; copied filters and authoritative tenant predicate; queued new-master ordering; framework-bound SQLite read. Broader operations and arbitrary concurrent edits remain open. |
| QueryPublicationTests | Actual UoW basic/enhanced/typed routes; preserved implicit records/cursor/mode; explicit ENTER_QUERY compatibility; queued request supersession and copied filters; registration identity; final policy/retirement rejection; shared query/detail ordering; close/cancellation acknowledgement; published-with-observer-error outcomes; framework-bound SQLite query. Broader operations, raw edits and host conformance remain open. |
| RecordTargetTests | Real UoW/wrapper and default LOV helper; cursor/edit ABA, field/request/registration identity, captured selection, repeated-edit annotations, reentrant synchronous validation, linked close/cancellation drain, failed triggers/loads, nested LOV rejection, disposed revision source and partial setter evidence. Helper events, auxiliary policy, async rules, arbitrary concurrent edits and host conformance remain open. |
| HostBehaviorTests | Real default UoW with existing host/view/presenter interfaces and recording dispatcher: affinity/order, own detach, identity-bound focus, cancellation, roster/view-state retirement, inline/deferred origin echoes, nullable edits, policy revisions/masking, byte copies, plain/dictionary records, truthful post-commit UI state, duplicate/late/early acknowledgement fencing. Full real host routing/adapters remain open. |
| PagingTests | Existing local cursor semantics, zero rows/size, count shrink and overflow; ignored/failed navigation; cancellation; bounded provider fetch/prefetch, stable sort and identity, policy/generation-aware cache and dirty-row protection. |

Use barriers or a fake clock/scheduler for races, not wall-clock timing luck.
Add at least one opt-in live transactional provider lane before asserting atomicity;
record provider/version and transaction behavior. Fake providers must reject nested
transactions and simulate unknown outcomes, not merely count mock calls.
Run Forms tests on net8.0/net9.0/net10.0 after explicitly multi-targeting its project.
TFM compilation alone is not runtime qualification. Pin test package versions in
line with repository dependency policy rather than leaving floating versions.

## Verified Baseline And Boundaries

On 2026-10-03, the current checkout completed a forced full solution build with
zero errors and 6,232 warnings. Forms tests passed 221/221 on net8.0, no failures
or skips, against that rebuilt source. Existing Forms commit tests mock UoW commit;
they do not exercise the nested transaction path identified in BF-01. Host contract
tests include reflection shape checks, not complete adapter behavior qualification.

Commands used (package/document generation disabled to avoid unrelated output):

```powershell
dotnet build BeepDM.sln --no-incremental -p:GeneratePackageOnBuild=false -p:GenerateDocumentationFile=false -v:q
dotnet test DataManagementEngineStandard/Editor/Forms.Tests/FormsManager.Tests.csproj -f net8.0 --no-build --no-restore -p:GeneratePackageOnBuild=false -p:GenerateDocumentationFile=false -v:q --logger 'console;verbosity=normal'
```

At review time the test project targeted net8.0 only. No live provider, real desktop
adapter, race fix, full Oracle Forms parity or net9/net10 Forms execution was
qualified by that baseline. Current implementation uses a multi-TFM test project;
see its qualification log below. The large warning count is solution-wide, not a Forms
warning inventory. Historical migration/framework work remains separate and
uncommitted; this planning pass does not implement or certify those changes.

## Completion Rules

Track stages A-G in the [current tracker section](.plans/todo-tracker.md#current-reliability-track).
Close an item only with implementation, regression tests, source-built result logs,
documented compatibility and linked provider/adapter evidence where required.
Do not mark the reliability track complete because the older feature phases are
complete. Update this review and source line anchors as code changes.

## Implementation Progress

See [Commit Ownership](COMMIT-OWNERSHIP.md) for the implemented contracts and
explicit compatibility/concurrency limits. [Implementation Log](IMPLEMENTATION-LOG.md)
records source-built results and initial qualification failures. Stage A remains
open for metadata/configuration pinning, foreign-key propagation rollback,
notification/audit/lock cleanup, cross-form graph qualification and provider-owned
handles. The [query increment](QUERY-POLICY.md) adds shared read policy and safe
scalars but does not close Stage B: legacy late publication and auxiliary read/cache
qualification remain. [Lifetime contracts](LIFETIME-CONTRACTS.md) document initial
Stage C callback/ownership/timer and default prepared registration work. Wider
public-operation admission/cancellation, graph/system-state and host gates remain
open. Do not infer completion of D-G from these increments.
The [detail increment](DETAIL-COORDINATION.md) begins D with captured traversals,
dirty admission and typed results, not full request/record-generation scheduling.
The [staged detail increment](READ-PUBLICATION.md) adds prepare/accept/abort to actual
default datasource UoWs/wrappers, revision-gated publication and separate observer
failures. The same boundary now covers basic/enhanced queries, preserves implicit
records/mode and uses query supersession with shared managed read ordering. Legacy
UoWs still self-publish; wider scheduling,
metadata ownership, concurrent-edit and host conformance gates remain open.
The [record-target increment](RECORD-TARGETS.md) guards manager validation/LOV
annotation and selection, adds canonical observed UoW revisions and typed partial
selection evidence, and joins LOV lifetime drain. It does not close raw helper
events, auxiliary lookup policy, general scheduling or UI conformance gates.
The editor increment adds captured popup completion, typed provider/setter evidence,
editable/raw-disclosure checks and physical acknowledgement draining with a manual
UI-provider dispatch fixture. Broader host/view/presenter binding and focus/error
contracts, mutable policy/definition ABA and real desktop adapters remain open.
The [view-binding increment](UI-BINDING-CONTRACTS.md) provides an opt-in helper,
captured read/edit/focus identities, default field-policy ownership/revision and
physical dispatch drain. Concrete host callbacks, query-criteria/custom edit paths,
synchronous policy privacy/error repaint, async user-event provenance and real adapters remain gates.
The [buffer-authorization increment](BUFFER-AUTHORIZATION.md) rejects fresh binding
targets over prior-context rows and restores them only after accepted managed
query/detail publication. Field-only masking keeps read authorization while expiring
old delivery tokens. Raw buffer/row mutation, auxiliary caches, synchronous privacy
and broader public-operation policy remain open; no stage closes.
The [policy-reconciliation increment](POLICY-REPAINT.md) now queues automatic
clearing/remasking for default/facade policy feeds, isolates partial clear failures,
rejects stale ownership and drains both policy callbacks and the UI pump. Custom
metadata/state/messages, async error completion, immediate privacy and concrete
adapter E-10 qualification remain open. The [permission increment](PERMISSION-PROJECTION.md)
separates authored flags from runtime grants, projects rule removal and preinstalled
policies, and fences default registry/registration/revision publication. Whole
configuration/graph ownership, raw helper mutation feeds and custom-helper
qualification remain open; no complete stage closes.
The [local paging increment](LOCAL-PAGING.md) implements F's cursor acknowledgement,
typed partial outcomes, exact long arithmetic, count shrink/zero and callback drain.
It does not implement bounded provider fetch/prefetch/cache or close Stage F.
The subsequent [provider fetch increment](PROVIDER-PAGING.md) implements explicit
capability-gated staged fetching, scalar UTF-8 row/byte bounds, long count/page
evidence, complete unique-key ordering and mandatory policy on the shared query
pipeline. A native SQLite test producer and malformed/race fixtures add 63 cases.
This builds on the relevant tested B/C/D read-path prerequisites, not closure of
their broader gates. Bounded policy/query/registration-aware prefetch/cache,
production provider and real host qualification remain open; all A-G stay open.
