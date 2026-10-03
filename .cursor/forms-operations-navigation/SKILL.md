---
name: forms-operations-navigation
description: FormsManager lifecycle, navigation and detail coordination guidance for BeepDM. Use when changing form open/close, typed commit outcomes, record movement, block switching or captured immediate/deferred detail synchronization.
---

# Forms Operations And Navigation

Use this skill for end-user form lifecycle behavior and record navigation patterns.

## File Locations
- `DataManagementEngineStandard/Editor/Forms/FormsManager.FormOperations.cs`
- `DataManagementEngineStandard/Editor/Forms/FormsManager.Navigation.cs`
- `DataManagementEngineStandard/Editor/Forms/FormsManager.Relationships.cs`
- `DataManagementEngineStandard/Editor/Forms/FormsManager.DetailCoordination.cs`

## Core APIs
- form lifecycle: `OpenFormAsync`, `CloseFormAsync`, `CommitFormAsync`, `RollbackFormAsync`, `ClearAllBlocksAsync`, `ClearBlockAsync`, `ValidateForm`
- navigation: `FirstRecordAsync`, `NextRecordAsync`, `PreviousRecordAsync`, `LastRecordAsync`, `NavigateToRecordAsync`, `SwitchToBlockAsync`, `GetCurrentRecordInfo`, `GetAllNavigationInfo`

## Working Rules
1. Switch block explicitly before block-scoped actions.
2. Assume navigation can be cancelled by validation or unsaved-change policy.
3. Commit/rollback through form-level APIs, not manual per-block calls.
4. Preserve detail-block synchronization after navigation where configured.

## Coordination Contracts
- Read `DataManagementEngineStandard/Editor/Forms/DETAIL-COORDINATION.md` and `LIFETIME-CONTRACTS.md` for ordering/close changes.
- Use optional `IFormsDetailSynchronization` for per-detail typed outcomes. Legacy Task sync APIs now throw on incomplete synchronization; cancellation propagates. Do not infer no effects or replay safety when a navigation/delete already changed state.
- Capture block/UoW/record keys and independent composite mappings before awaits. Target a deferred relationship without mutating its live mode or refreshing siblings.
- Preserve dirty branches and pending markers until acknowledged success. Private detail suppression belongs to a registration lease, not a reusable block name.
- Query/detail reads share manager-local ordering and reject awaited nested reads. Newer same-registration queries supersede older unpublished candidates. This is not an edit/commit/navigation scheduler, a UI dispatcher or raw-UoW protection.
- Default datasource UoWs/wrappers stage basic/enhanced/detail reads; implicit query rejection retains rows/cursor/mode. Prefer typed `ExecuteQueryWithOutcomeAsync` and inspect FormInstanceId/RegistrationId/RequestRevision, publication and notification outcomes. Read `READ-PUBLICATION.md`; accepted rows are not undone by observer failure.
- Read `RECORD-TARGETS.md` for validation/LOV/editor capture, observed cursor/edit ABA rejection and setter evidence. Prefer `ShowEditorWithOutcomeAsync`: late provider OK is not commit success. These operations join close/drain; raw helper events, policy/definition ABA, real adapters and final-window edits remain open.
- Close cancels/drains traversals, but legacy Get can still publish before late rejection. `ProviderMayHavePublished` means legacy invocation or accepted staged publication, not database rollback proof.
- For UI focus/delivery use `UI-BINDING-CONTRACTS.md` and opt-in FormsViewBinding. Pass the engine-acknowledged target to FocusAsync; do not recapture CurrentItem at dispatch. Full legacy navigation/custom host routing and real adapters remain unqualified.
- Prefer typed commit outcomes. Unknown/partial durability is not permission to automatically roll back/replay every provider.

## Related Skills
- [`forms`](../forms/SKILL.md)
- [`forms-helper-managers`](../forms-helper-managers/SKILL.md)
- [`forms-mode-transitions`](../forms-mode-transitions/SKILL.md)

## Detailed Reference
Read `LOCAL-PAGING.md` for typed local cursor outcomes and exact long math.
LoadPageAsync does not fetch a provider page; require acknowledged page state
before UI focus/refresh. Cancellation/failure can retain cursor effects or accepted
page state with notification errors. Read `PROVIDER-PAGING.md` for explicit bounded
fetch sharing query/detail ordering and physical drain. Out-of-range count shrink
retains prior rows; require RecordsPublished, not only ProviderPage count evidence.
Prefetch/cache and external provider qualification remain open.

Read `PERMISSION-PROJECTION.md` for configured versus effective permissions,
rule removal, registry/revision-gated default projection and injected-helper limits.
Setters configure permissions; getters include policy. Clearing rules neither lifts
authored restrictions nor authorizes old rows. Keep callbacks outside ownership locks.

For cached UI rows, read `BUFFER-AUTHORIZATION.md` in the checkout. Fresh binding
requires an accepted managed read after principal/block-policy changes; clearing or
re-registering rows is not authorization. Field-only changes permit fresh remasking.
Read `POLICY-REPAINT.md` for optional notifications, queued clearing/remasking and
explicit reconciliation after UI failures. Immediate privacy requires hiding before
principal switch; drain UI work and inspect outcomes. Native adapters remain unqualified.

Use [`reference.md`](./reference.md) for safe lifecycle patterns, diagnostics, and pitfalls.
