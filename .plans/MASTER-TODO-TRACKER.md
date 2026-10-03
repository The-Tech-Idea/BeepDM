# MASTER TODO TRACKER
# BeepDM Enhancement Plans

**Goal:** Systematically enhance the core data-management layers in `BeepDM`
to reach enterprise parity with documented phase roadmaps, and ship the
WinForms UI shells that wrap each subsystem using the canonical
`beep-winform-design` skill.

---

## Phase Overview

### Forms Reliability Track (Review: 2026-10-03)

Scope: FormsManager and platform-neutral UI integration contracts.
[Current review and ordered plan](../DataManagementEngineStandard/Editor/Forms/RELIABILITY-AND-ENHANCEMENT-PLAN.md)
contains nine prioritized source findings and stages A-G; all implementation
stages are tracked in the [Forms tracker](../DataManagementEngineStandard/Editor/Forms/.plans/todo-tracker.md#current-reliability-track).
Stages A/B/C/D/E implementation is in progress; [commit contracts](../DataManagementEngineStandard/Editor/Forms/COMMIT-OWNERSHIP.md)
and [implementation evidence](../DataManagementEngineStandard/Editor/Forms/IMPLEMENTATION-LOG.md)
record 795 source-built Forms passes per TFM (2,385 executions), 1,393 framework
regressions per TFM and the preceding 12 focused UoW regressions per TFM.
[Managed query policy](../DataManagementEngineStandard/Editor/Forms/QUERY-POLICY.md)
covers basic/enhanced/detail/count/aggregate reads with fail-closed compilation;
default UoWs stage basic/enhanced/detail reads; auxiliary read/cache targets remain open. Real SQLite
ADO.NET reads/scalars and commit/rollback are qualified through a
test adapter; external provider plugins and UI adapters remain unqualified.
[Lifetime contracts](../DataManagementEngineStandard/Editor/Forms/LIFETIME-CONTRACTS.md)
cover captured teardown, borrowed helpers, gated event/message leases, attachment
unwind, staged default item/root replacement, pending setup revocation/drain and
timer identity. Full public-operation admission/cancellation, graph/system-state
rollback and host conformance remain open. [Captured detail coordination](../DataManagementEngineStandard/Editor/Forms/DETAIL-COORDINATION.md)
adds serialized targeted requests, conservative dirty admission, linked close
cancellation/drain and per-detail outcomes. [Default staged managed reads](../DataManagementEngineStandard/Editor/Forms/READ-PUBLICATION.md)
retain prior rows/cursor/mode on rejected implicit queries, serialize query/detail
reads and supersede older unpublished queries by registration/request identity.
Typed outcomes distinguish confirmed publication from observer failures and drain
admitted work before acknowledging cancellation; legacy Get still self-publishes. Global operation scheduling,
arbitrary concurrent edits and validation/LOV record generations remain open.
[Captured validation/LOV targets](../DataManagementEngineStandard/Editor/Forms/RECORD-TARGETS.md)
now reject stale manager annotations/selection using record/item/request identity
and canonical observed UoW revisions, including cursor/edit ABA. Typed LOV outcomes
retain partial setter evidence and join close/drain. Raw helper events, auxiliary
lookup policy/cache, async rule context and UI adapter generations remain open.
Captured editor completion now separates provider OK from acknowledged current writes,
enforces editability/raw-text disclosure and drains physical popup acknowledgement.
The manual UI dispatch fixture qualifies the provider boundary, not real adapters.
The [opt-in view-binding increment](../DataManagementEngineStandard/Editor/Forms/UI-BINDING-CONTRACTS.md)
adds an adapter utility over existing contracts, immutable captured targets,
dispatcher/origin capabilities, policy-revision/field-policy ownership, masked/null/
byte-safe presentation, captured writes/focus and physical delivery/detach drain.
Its real-UoW/mock-host/manual-dispatch fixture covers 46 added cases per TFM;
concrete adapters must run the published E-01 through E-10 checklist. Custom host
edit/query pipelines, async user/error provenance and instantaneous privacy remain open.
The [cached-buffer binding increment](../DataManagementEngineStandard/Editor/Forms/BUFFER-AUTHORIZATION.md)
adds 30 real-UoW cases per TFM: managed query/detail receipts revoke fresh binding
of older-context buffers; field-only changes permit fresh remasking. Local tenant
scope and buffer replacement/ABA invalidate receipts, including in-flight reads.
Raw row mutation/auxiliary caches and instantaneous privacy remain open.
The [policy-reconciliation increment](../DataManagementEngineStandard/Editor/Forms/POLICY-REPAINT.md)
adds 31 notification/UI/drain cases per TFM. Default/facade feeds now queue clearing
of revoked presentation or remasking of authorized rows, preserving dirty records
and rejecting stale owners. Deliberate reconciliation, partial clearing failure
evidence and physical callback/dispatcher drain are implemented. Custom state/
message payloads, error completion and native adapters remain open.
The [permission-projection increment](../DataManagementEngineStandard/Editor/Forms/PERMISSION-PROJECTION.md)
separates configured flags from runtime grants, restores removed rules and gates
default item publication by registry/registration/revision identity. Its 24 added
cases qualify authored writes under denial, reset, clone/serializer boundaries,
replacement/observer races, ownership and preinstalled policy; whole configuration,
raw helper mutation feeds and injected-helper qualification remain open.
Hide/lock before principal switch for immediate privacy.
The preceding combined green Forms/framework matrix was 5,892 executions. An unchanged
net10 lifecycle alias-removal case failed on the preceding full run and then passed
isolated/full rechecks; it and earlier child-owner instability remain release gates.
The previous binding increment's final combined source-built matrix passed 6,030 executions;
these prior instability records remain open, not erased by this clean run.
The preceding cached-buffer increment passed 6,120 source-built Forms/framework
executions across net8/net9/net10; no complete stage or release gate is closed.
Preceding policy-reconciliation matrix: 6,213 source-built Forms/framework executions
across net8/net9/net10. Earlier intermittent release failures remain unresolved.
Preceding permission-projection matrix: 6,282 source-built Forms/framework executions
across net8/net9/net10, with no failures/skips. No complete stage closes.
The preceding final permission-projection matrix, including the failed-action one-shot fence:
6,285 executions (702 Forms and 1,393 framework per TFM), no failures/skips.
The [local paging increment](../DataManagementEngineStandard/Editor/Forms/LOCAL-PAGING.md)
adds 30 cases and implements typed local cursor acknowledgement, exact long math,
stored zero/count shrink, captured ownership/configuration and physical drain.
Stage F remains in progress. The subsequent
[bounded provider fetch increment](../DataManagementEngineStandard/Editor/Forms/PROVIDER-PAGING.md)
adds 63 cases, optional producer/stage capabilities, complete key ordering, actual
UTF-8 row/byte bounds, long count/page evidence, mandatory policy and shared
dirty/supersession/drain handling. Its native SQLite test lane uses coherent count/
page transactions and bound LIMIT/OFFSET; it does not qualify external plugins.
Policy/query/registration-aware bounded prefetch/cache remains required and
unimplemented. Neither paged overloads nor stored settings activate it.
Final local-paging matrix: 6,375 source-built Forms/framework executions across
net8/net9/net10, no failures/skips. Source-referenced examples compile on all three
targets with zero warnings/errors; earlier intermittent release gates remain open.
Current provider-fetch matrix: 6,564 source-built Forms/framework executions across
net8/net9/net10 (795 Forms and 1,393 framework per TFM), no failures/skips.
Earlier intermittent release gates remain unresolved; all A-G remain open.
Five Forms skills and their direct installed copies have updated
references; their example fragments compile across net8/net9/net10, not a full
skill or UI adapter conformance closeout.
Baseline: 221 net8.0 Forms tests passed and a forced solution build completed
with zero errors and 6,232 solution-wide warnings. Provider atomicity, external
UI adapters remain unqualified; the newer matrix qualifies net9/net10 Forms
execution. Older framework
and Forms completion/build notes below describe their respective prior audits.

### Framework Reliability Track (Review: 2026-10-02)

Latest planning-only refresh (2026-10-03):
[current review and ordered enhancement plan](framework/REVIEW-AND-PLAN-CURRENT.md).
Fresh net9.0 Engine build fails with five migration refactor errors and 1,987
warnings; preceding green inventories are historical. Complete compilation and
actual ownership/captured storage, then safe compensation. Sync publication,
remaining configuration, provider inventory and release preparation remain open
parallel lanes. Existing IDs/checkmarks and in-progress source are preserved.

Subsequent required configuration increment: 4,719 source-built Windows passes, zero
failures/skips, 1,240 reliability cases per TFM. Editor-owned registration, one
normalization, cache bypass and reported-fallback denial are verified with 34 new
cases per TFM. Forty further cases verify caller-safe implicit refresh, closed
bounded literals/per-row byte copies and both declared catalogs before provider-
opening validation. A further 43 cases pin roster/priorities through registration
edits, protect row SentData/admitted named lookup and retain sticky safe nested
failures/selector callback boundaries. Another 59 shipped outer-grammar cases
qualify exact routing/arity, nested dates, quoted keys/operands, GUID aliases and
sequence-placeholder denial. A further 47 cases preserve required dot literals/
empty strings and declared grouping/actual filters, without changing public
legacy parsing. Another 82 cases qualify bounded shipped required expression/formula
ASTs, Boolean-only conditions, precedence, exact typed comparisons, invariant numbers,
Decimal rounding, lazy branches and pre-read limits/cancellation. Nested overrides
retain pinned selection; unused branches are syntax-checked only. Broader numeric/
query-provider/identity/scope/date/NFEL semantics remain. Another 80 cases qualify
required query/filter plans, actual closed invariant binding, null/empty distinction,
typed aggregates, safe callback/cursor/disposal failures and bounded streaming/
cancellation. Partial acknowledgements survive query failure without replay;
explicit context, eager allocation, hidden failures, isolation and translation remain.
Another 56 cases qualify required identity/scope rules: explicit string email/
application-role context without row spoofing or inferred substitutes, strict
conflicts/types/limits, getter cancellation/failure, actual OS SID/membership and
folders, STA/MTA Downloads ownership and exact environment scopes. SYSTEMPATH is
Machine PATH; ENV(PATH) is Process PATH. Supplied host strings are not authorization
proof, and OS/host context is not immutable or all-platform qualified.
Another 84 cases qualify required dates: Gregorian ISO literals without host-culture
fallback, retained UTC kind/explicit offsets, exact fractional tick/calendar
arithmetic, invariant bounded formats, pre-callback known-date-tree checks and
actual typed nested/property results through the pinned roster. Clock aliases have
explicit kinds and retain dynamic time; no immutable run clock or named-zone DST
policy is claimed. Legacy direct and custom subclass contracts remain separate.
Another 83 cases qualify required configuration: exact declared environment
prefixes, named flat string maps, function/colon/dot aliases, full bounded capture
before key selection, no row/editor inference and connection alias conflict denial.
Missing/invalid explicit sources cannot fall back. Callback/disposal failures and
cancellation deny with safe Partial counts. Capture is per resolution, not run-owned
configuration or automatic host/credential integration; legacy/custom paths remain.

B1 immutable policy/plugin context and rule/provider
qualification remain before B2 promotion/cursor agreement; five phases stay open.

Latest planning-only refresh: [2026-10-03 current source review and delivery plan](framework/REVIEW-REFRESH-2026-10-03.md).
Rebuilt Windows matrix: 2,895 passing executions, zero failures/skips, with 632
reliability cases per TFM. Required defaults-catalog admission is locally tested;
defaults/resolver ownership remains before staged promotion/cursor agreement.
Configuration/security and migration ownership run alongside provider/release
preparation. No runtime changes or broad phase completion in this refresh.
The paragraphs below retain preceding implementation/planning evidence.

The review's red baseline is corrected by actual acknowledged file reject replay:
the current full Windows matrix passes 2,868 executions, zero failures/skips,
with 623 reliability cases per TFM. Operator preparation/CAS claims, actual
direction-specific writes, uncertain/restarted recovery and corrupt-file protection
are verified. Native/provider-run recovery and defaults/resolvers remain before
promotion/cursor qualification. Existing verified
record/threshold admission and metadata/type fixes remain delivered; broader
defaults, provider, persistence, lifecycle and release gates remain open.

The cross-cutting review and proposed five-phase enhancement plan live in
[framework/MASTER-FRAMEWORK-TRACKER.md](framework/MASTER-FRAMEWORK-TRACKER.md).
It records source-backed findings, the current test baseline, dependencies, and
acceptance criteria. Phases 1-3 have partial implementation; the last recorded full
Windows solution matrix passed 2,136 executions with 379 reliability cases each
net8/9/10. The original planning refresh started no tests; subsequent sync storage,
terminal acknowledgement and diagnostic isolation changes have full local TFM
evidence. Runtime isolation, normal datasource lifecycle and P3-04/05 captured
migration intent/storage are verified slices, not new backlog items. Next finish
required record-quality, mapped-sync metadata and deeper default qualification
(R14/R15/R16; strict transform core verified), promotion and
durable cursor agreement (remaining R12/R13), then remaining configuration/security
qualification and P3-06 recovery/admission. Lifecycle, conformance and bounded
execution follow; provider inventory and build/CI start in parallel. Existing
subsystem plans remain in place and should be reconciled against current code
before delivery. Start with [the current review](framework/CURRENT-REVIEW.md).

### Track A — Core Enhancements (Phase 1–4)

**Repos:**
- OBL: `c:\...\BeepDM\DataManagementModelsStandard\ObservableBindingList\`
- UOW: `c:\...\BeepDM\DataManagementEngineStandard\Editor\UOW\`
- FormsManager: `c:\...\BeepDM\DataManagementEngineStandard\Editor\Forms\`

| # | Phase | Document | Status |
|---|---|---|---|
| 1 | OBL Core Enhancements | [PHASE-01](PHASE-01-OBL-Enhancements.md) | [x] |
| 2 | UOW Enhancements | [PHASE-02](PHASE-02-UOW-Enhancements.md) | [x] |
| 3 | UOW ↔ FormsManager Bridge | [PHASE-03](PHASE-03-UOW-FormsManager-Bridge.md) | [x] |
| 4 | FormsManager Advanced Operations | [PHASE-04](PHASE-04-FormsManager-Advanced.md) | [x] |

### Track C — Setup Framework → Solution Control Plane (Phase 1–8)

**Goal:** Grow `SetUp/` from a single-app first-run wizard into the one place a developer manages a
solution and its parts — same product for solo (local JSON) and enterprise (shared/remote + RBAC).

**Repos:**
- Setup: `c:\...\BeepDM\DataManagementEngineStandard\SetUp\` + `…\DataManagementModelsStandard\SetUp\`
- Solution aggregate (Phase 7): `c:\...\BeepDM\DataManagementEngineStandard\Services\AppMap\`
- Master tracker: [setup/MASTER-SETUP-TRACKER.md](setup/MASTER-SETUP-TRACKER.md)

| # | Phase | Document | Status |
|---|---|---|---|
| 1 | Stabilize & correctness | [PHASE-01](setup/PHASE-01-Stabilize-Correctness.md) | [ ] |
| 2 | Serializable `SetupDefinition` (keystone) | [PHASE-02](setup/PHASE-02-Serializable-SetupDefinition.md) | [ ] |
| 3 | Pluggable state store + concurrency | [PHASE-03](setup/PHASE-03-State-Store-And-Concurrency.md) | [ ] |
| 4 | Rollback & compensation | [PHASE-04](setup/PHASE-04-Rollback-And-Compensation.md) | [ ] |
| 5 | Identity, RBAC & approvals | [PHASE-05](setup/PHASE-05-Identity-RBAC-Approvals.md) | [ ] |
| 6 | Audit, reporting & telemetry | [PHASE-06](setup/PHASE-06-Audit-Reporting-Telemetry.md) | [ ] |
| 7 | Solution aggregate & multi-app | [PHASE-07](setup/PHASE-07-Solution-Aggregate-MultiApp.md) | [ ] |
| 8 | CLI, unattended & CI | [PHASE-08](setup/PHASE-08-CLI-Unattended-CI.md) | [ ] |

> Phase 1 must land first — three of its bugs make the *default* wizard unusable. Phase 2 is the
> keystone: until a setup definition is data rather than C#, phases 3–8 are all blocked.
> Current docs: [`SetUp/README.md`](../DataManagementEngineStandard/SetUp/README.md).

---

### Track B — Editor Managers parity + WinForms shells (Phase 5–10)

**Repos:**
- Engines: `c:\...\BeepDM\DataManagementEngineStandard\Editor\BeepSync\`,
  `…\Editor\Migration\`, `…\Editor\Defaults\`, `…\Editor\Mapping\`, `…\Editor\Importing\`
- UI: `c:\...\Beep.Winform.Data.Integrated\Beep.Winform.Data.Integrated.Views\Configuration\`
- Master tracker: [editor-managers/MASTER-EDITOR-MANAGERS-TRACKER.md](editor-managers/MASTER-EDITOR-MANAGERS-TRACKER.md)

| # | Phase | Document | Status |
|---|---|---|---|
| 5 | BeepSyncManager parity + rollout | [PHASE-05](editor-managers/PHASE-05-BeepSyncManager.md) | [ ] |
| 6 | MigrationManager parity (examples) | [PHASE-06](editor-managers/PHASE-06-MigrationManager.md) | [ ] |
| 7 | DefaultsManager parity | [PHASE-07](editor-managers/PHASE-07-DefaultsManager.md) | [ ] |
| 8 | MappingManager parity | [PHASE-08](editor-managers/PHASE-08-MappingManager.md) | [ ] |
| 9 | DataImportManager parity (.plans + examples) | [PHASE-09](editor-managers/PHASE-09-DataImportManager.md) | [ ] |
| 10 | WinForms Configuration shells (design skill) | [PHASE-10](editor-managers/PHASE-10-WinForms-Configuration-Shells.md) | [ ] |

---

## Phase 1 — OBL Core Enhancements

**Doc:** [PHASE-01-OBL-Enhancements.md](PHASE-01-OBL-Enhancements.md)  
**Target:** `DataManagementModelsStandard/ObservableBindingList/`

### 1-A Field-Level Change Introspection
- [x] Add `OriginalFieldValues` dict to `Tracking`
- [x] Capture original snapshot in `Item_PropertyChanged` (first `Unchanged→Modified` transition)
- [x] Implement `GetOriginalValue(item, fieldName)` → `ObservableBindingList.ChangeInspection.cs` (new)
- [x] Implement `GetChangedFields(item)` → same file
- [x] Implement `GetFieldDelta(item, fieldName)` → same file
- [x] Implement `HasFieldChanges(item)` → same file

### 1-B Change-Set Export
- [x] Add `ChangeSetSummary` POCO to `ObservableChanges.cs`
- [x] Implement `GetInserted() / GetUpdated() / GetDeleted() / GetDirty() / GetChangeSetSummary()`

### 1-C Batch / Bulk Load
- [x] Implement `LoadBatch(IEnumerable<T>)` with notification suppression
- [x] Implement `LoadBatchAsync(items, batchSize, progress, ct)`

### 1-D Async Search
- [x] Implement `SearchAsync(predicate, ct)`
- [x] Implement `SearchStreamAsync(predicate, ct)` (IAsyncEnumerable)

### 1-E Server Merge / Conflict Resolution
- [x] Define `ConflictMode` enum
- [x] Define `MergeResult<T>` POCO
- [x] Implement `Merge(serverItems, mode, pkField)` → `ObservableBindingList.Merge.cs` (new)
- [x] Implement `MergeAsync(serverItems, mode, pkField, ct)` → same file

### 1-F Grouping Support
- [x] Define `ItemGroup<T>` POCO
- [x] Implement `GetGroups<TKey>(keySelector, ascending)` → `ObservableBindingList.Grouping.cs` (new)

---

## Phase 2 — UOW Enhancements

**Doc:** [PHASE-02-UOW-Enhancements.md](PHASE-02-UOW-Enhancements.md)  
**Target:** `DataManagementEngineStandard/Editor/UOW/`

### 2-A ChangeSummary
- [x] Add `ChangeSummary` POCO → `UOW/Models/ChangeSummary.cs` (new)
- [x] Implement `GetChangeSummary()`, `GetInsertedItems()`, `GetUpdatedItems()`, `GetDeletedItems()`
- [x] Add to `IUnitofWork<T>`

### 2-B RefreshAsync
- [x] Implement `RefreshAsync(filters, conflictMode, ct)` in `UnitofWork.CRUD.cs`
- [x] Add to `IUnitofWork<T>`

### 2-C RevertItem
- [x] Implement `RevertItem(item)` + `RevertItemAsync(item, ct)` in `UnitofWork.Core.Extensions.cs`
- [x] Add `OnItemReverted` event to `UnitofWork.Core.cs`

### 2-D CommitBatchAsync
- [x] Add `CommitBatchProgress` + `CommitBatchResult` POCOs
- [x] Implement `CommitBatchAsync(batchSize, progress, ct)` in `UnitofWork.CRUD.cs`

### 2-E Query History
- [x] Add `QueryHistoryEntry` POCO → `UOW/Models/`
- [x] Create `UnitofWorkQueryHistory.cs` helper
- [x] Hook push into `Get()` + `ExecuteQueryAsync`
- [x] Expose `QueryHistory` property on `IUnitofWork<T>`

### 2-F Data Export
- [x] Create `UnitofWorkExportHelper.cs`
- [x] Implement `ToDataTable()`, `ToJsonAsync(stream, ct)`, `ToCsvAsync(stream, delimiter, ct)`

### 2-G Data Import
- [x] Implement `LoadFromJsonAsync(stream, clearFirst, ct)` in `UnitofWorkExportHelper.cs`
- [x] Implement `LoadFromCsvAsync(stream, delimiter, clearFirst, hasHeader, ct)`

### 2-H FindAsync + CloneItem
- [x] Implement `FindAsync(predicate, ct)`, `FindManyAsync(predicate, ct)`
- [x] Implement `CloneItem(item, deepCopy)` in `UnitofWork.Core.Extensions.cs`

### 2-I Aggregate Shortcuts
- [x] Implement `Sum`, `Average`, `Min<TField>`, `Max<TField>`, `Count` in `UnitofWork.Core.Extensions.cs`

### 2-J Undo/Redo Surface
- [x] Implement `UndoLastAction()`, `RedoLastAction()`, `CanUndo`, `CanRedo`, `EnableUndo(bool, int)`
- [x] Add to `IUnitofWork<T>`

### 2-K Interface Updates
- [x] Update `IUnitofWork<T>` with all new members (2-A through 2-J)
- [x] Update non-generic `IUnitofWork` with key subset

---

## Phase 3 — UOW ↔ FormsManager Bridge

**Doc:** [PHASE-03-UOW-FormsManager-Bridge.md](PHASE-03-UOW-FormsManager-Bridge.md)  
**Target:** `Forms/Interfaces/` + `Forms/FormsManager.*`

### 3-A Capability Marker Interfaces
- [x] Define `IRevertable`, `IBatchCommittable`, `IExportable`, `IImportable`, `IAggregatable`, `IUndoable`, `IMergeable`
- [x] `UnitofWork<T>` declares all 7 marker interfaces

### 3-B IUnitofWorksManager Additions
- [x] Add undo/redo region (SetBlockUndoEnabled, UndoBlock, RedoBlock, CanUndoBlock, CanRedoBlock)
- [x] Add change-summary region (GetBlockChangeSummary, GetFormChangeSummary)
- [x] Add block-data-ops region (RefreshBlockAsync, RevertCurrentRecord, RevertRecord)
- [x] Add query-history region (GetBlockQueryHistory, ClearBlockQueryHistory)
- [x] Add aggregates region (GetBlockSum, GetBlockAverage, GetBlockCount)
- [x] Add batch-commit region (CommitFormBatchAsync, CommitBlockBatchAsync)
- [x] Add export/import region (ExportBlockToJsonAsync, ExportBlockToCsvAsync, GetBlockAsDataTable, ImportBlockFromJsonAsync, ImportBlockFromCsvAsync)
- [x] Add grouping region (GetBlockGroups)

### 3-C FormsManager Implementation
- [x] Create `FormsManager.DataOperations.cs` (new partial)
- [x] Implement all 3-B methods as thin facades
- [x] Update `UnitOfWorkWrapper` + `UnitOfWorkWrapperExtensions.cs` to forward new capabilities

---

## Phase 4 — FormsManager Advanced Operations

**Doc:** [PHASE-04-FormsManager-Advanced.md](PHASE-04-FormsManager-Advanced.md)  
**Target:** `Forms/`

### 4-A FK-Aware Commit Ordering
- [x] Implement `BuildCommitOrder()` — Kahn's topological sort over `_relationships`
- [x] Replace block-iteration loop in `CommitFormAsync`

### 4-B Form State Persistence
- [x] Add `FormStateSnapshot` + `BlockStateSnapshot` POCOs → `Models/FormStateSnapshot.cs`
- [x] Add `SaveFormState()` + `RestoreFormStateAsync(snapshot, ct)` to `IUnitofWorksManager`
- [x] Implement both in `FormsManager.FormOperations.cs`

### 4-C Cross-Block Validation
- [x] Add `CrossBlockValidationRule` POCO → `Models/CrossBlockValidationRule.cs`
- [x] Add `RegisterCrossBlockRule`, `UnregisterCrossBlockRule`, `ValidateCrossBlock` to interface
- [x] Create `CrossBlockValidationManager.cs` helper
- [x] Wire `_crossBlockValidation` into `FormsManager.cs` + call in `CommitFormAsync`

### 4-D Block-Level Navigation History
- [x] Add `NavigationHistoryEntry` POCO → `Models/NavigationHistoryEntry.cs`
- [x] Add `NavigateBackAsync`, `NavigateForwardAsync`, `CanNavigateBack/Forward`, `GetNavigationHistory`, `ClearNavigationHistory` to interface
- [x] Create `NavigationHistoryManager.cs` helper
- [x] Wire push into `FormsManager.Navigation.cs`

### 4-E Block Clone / Snapshot
- [x] Add `CloneBlockDataAsync` + `DuplicateCurrentRecordAsync` to interface
- [x] Implement in `FormsManager.DataOperations.cs`

### 4-F Block Change Feed
- [x] Add `BlockFieldChangedEventArgs` POCO → `Models/BlockFieldChangedEventArgs.cs`
- [x] Add `OnBlockFieldChanged` event to `IUnitofWorksManager`
- [x] Subscribe in `RegisterBlock`, fire from OBL `ItemChanged` handler
- [x] Unsubscribe in `UnregisterBlock`

---

## New Files Summary

| File | Phase |
|---|---|
| `OBL/ObservableBindingList.ChangeInspection.cs` | 1-A |
| `OBL/ObservableBindingList.Merge.cs` | 1-E |
| `OBL/ObservableBindingList.Grouping.cs` | 1-F |
| `UOW/Models/ChangeSummary.cs` | 2-A |
| `UOW/Models/QueryHistoryEntry.cs` | 2-E |
| `UOW/Helpers/UnitofWorkExportHelper.cs` | 2-F / 2-G |
| `UOW/Helpers/UnitofWorkQueryHistory.cs` | 2-E |
| `Forms/Models/FormStateSnapshot.cs` | 4-B |
| `Forms/Models/CrossBlockValidationRule.cs` | 4-C |
| `Forms/Models/NavigationHistoryEntry.cs` | 4-D |
| `Forms/Models/BlockFieldChangedEventArgs.cs` | 4-F |
| `Forms/Helpers/CrossBlockValidationManager.cs` | 4-C |
| `Forms/Helpers/NavigationHistoryManager.cs` | 4-D |
| `Forms/FormsManager.DataOperations.cs` | 3-C |

## Modified Files Summary

| File | Change | Phase |
|---|---|---|
| `OBL/Tracking.cs` | Add `OriginalFieldValues` dict + snapshot capture | 1-A |
| `OBL/ObservableBindingList.ListChanges.cs` | Hook snapshot capture into `Item_PropertyChanged` | 1-A |
| `OBL/ObservableBindingList.CRUD.cs` | Add `LoadBatch` / `LoadBatchAsync` | 1-C |
| `OBL/ObservableBindingList.Search.cs` | Add `SearchAsync` / `SearchStreamAsync` | 1-D |
| `OBL/ObservableChanges.cs` | Add `ChangeSetSummary` POCO | 1-B |
| `UOW/UnitofWork.Core.cs` | Add `OnItemReverted` event + declare marker interfaces | 2-C / 3-A |
| `UOW/UnitofWork.CRUD.cs` | Add `RefreshAsync`, `CommitBatchAsync`, query-history hook | 2-B / 2-D / 2-E |
| `UOW/UnitofWork.Core.Extensions.cs` | Add `RevertItem`, `FindAsync`, `CloneItem`, aggregates, undo surface | 2-C / 2-H / 2-I / 2-J |
| `UOW/UnitOfWorkWrapper.cs` | Forward new capabilities | 3-C |
| `UOW/UnitOfWorkWrapperExtensions.cs` | Forward new capabilities | 3-C |
| `Forms/Interfaces/IUnitofWorksManagerInterfaces.cs` | Add all Phase-3 regions + Phase-4 API | 3-B / 4-* |
| `Forms/FormsManager.cs` | Add new manager fields, event wiring | 3-C / 4-F |
| `Forms/FormsManager.FormOperations.cs` | Topological commit order, SaveFormState, RestoreFormState | 4-A / 4-B |
| `Forms/FormsManager.Navigation.cs` | Wire navigation-history push | 4-D |

---

## Progress Legend

- `[x]` Not started
- `[~]` In progress
- `[x]` Complete
- `[-]` Deferred / skipped
