---
name: forms
description: Entry-point guidance for FormsManager orchestration in BeepDM. Use when implementing Oracle Forms style behavior with block registration, master-detail coordination, mode transitions, navigation, dirty-state handling, trigger/event flow, and performance/configuration policies.
---

# Forms Manager Guide

Use this skill for platform-neutral `FormsManager` orchestration, not desktop control redesign.

## Use this skill when
- Wiring a new block-based form over `UnitofWork` instances
- Coordinating master-detail behavior, form lifecycle, and navigation
- Deciding whether a change belongs in mode transitions, helpers, enhanced CRUD, or performance/configuration

## Do not use this skill when
- The task is only about direct `UnitofWork` behavior outside forms orchestration. Use [`unitofwork`](../unitofwork/SKILL.md).
- The task is only about import/sync/ETL pipelines. Use [`importing`](../importing/SKILL.md), [`beepsync`](../beepsync/SKILL.md), or [`etl`](../etl/SKILL.md).

## Architecture
- `FormsManager` is the coordinator class in `TheTechIdea.Beep.Editor.UOWManager`.
- Core responsibilities are split across partial classes:
  - construction in `FormsManager.Core.cs`, registration in `FormsManager.BlockRegistration.cs`
  - prepared registration ownership/publication in `FormsManager.RegistrationLifetime.cs`
  - commit outcomes in `FormsManager.TransactionCoordination.cs`
  - disposal in `FormsManager.Lifecycle.cs` and callback draining in `FormsManager.CallbackLifetime.cs`
  - form open/close in `FormsManager.FormOperations.cs`
  - navigation in `FormsManager.Navigation.cs`
  - relationship coordination in `FormsManager.Relationships.cs`
  - captured detail traversal/outcomes in `FormsManager.DetailCoordination.cs`
  - mode flow in `FormsManager.ModeTransitions.cs`
  - CRUD/query enhancements in `FormsManager.EnhancedOperations.cs`
- Helper managers own focused behavior:
  - `DirtyStateManager`
  - `EventManager`
  - `FormsSimulationHelper`
  - `PerformanceManager`
  - `ConfigurationManager`

## File Locations
- `DataManagementEngineStandard/Editor/Forms/FormsManager.Core.cs`
- `DataManagementEngineStandard/Editor/Forms/FormsManager.FormOperations.cs`
- `DataManagementEngineStandard/Editor/Forms/FormsManager.Navigation.cs`
- `DataManagementEngineStandard/Editor/Forms/FormsManager.ModeTransitions.cs`
- `DataManagementEngineStandard/Editor/Forms/FormsManager.EnhancedOperations.cs`
- `DataManagementEngineStandard/Editor/Forms/Helpers/`
- `DataManagementModelsStandard/Editor/Forms/Configuration/`
- `DataManagementModelsStandard/Editor/Forms/Models/`
- `DataManagementModelsStandard/Editor/Forms/Interfaces/`

## Fast Workflow
1. Create `FormsManager(editor)`.
2. Register each block with `RegisterBlock(...)`.
3. Create relationships with `CreateMasterDetailRelation(...)`.
4. Open the form and enter the right mode for the target block.
5. Use form/navigation/CRUD APIs through `FormsManager`, not direct ad-hoc block mutations.
6. Commit or roll back through form-level APIs.

## Reliability Contracts

In the BeepDM checkout, read the relevant current source contracts under
`DataManagementEngineStandard/Editor/Forms/`:
- `COMMIT-OWNERSHIP.md`: enlisted writes, typed outcomes, independent/unknown commits and reconciliation. Prefer `CommitFormWithOutcomeAsync`; do not infer multi-provider ACID from a legacy success flag.
- `QUERY-POLICY.md`: shared fail-closed managed block reads and safe scalar binding. Auxiliary reads/caches and legacy late publication still need qualification.
- `DETAIL-COORDINATION.md`: captured serialized detail requests, targeted deferred synchronization, dirty branch admission and typed outcomes. Other public operations and legacy read publication remain open.
- `READ-PUBLICATION.md`: default UoW/wrapper staged basic/enhanced/detail reads, shared managed read ordering and query supersession. Prefer `ExecuteQueryWithOutcomeAsync` for cancellation/publication evidence; inspect registration/request identity and separate NotificationFailures. Raw concurrent edits and real UI adapters remain unqualified.
- `RECORD-TARGETS.md`: captured validation/LOV/editor targets and observed revisions. Prefer `ShowLOVWithOutcomeAsync` and `ShowEditorWithOutcomeAsync` for setter evidence; provider OK is not an accepted edit. Auxiliary lookup policy and full UI conformance remain open.
- `UI-BINDING-CONTRACTS.md`: opt-in FormsViewBinding over existing host/view/presenter contracts, captured edit/focus identities, dispatcher/origin protocol and E-01 through E-10 checklist. Default field policies are copies; publish via SetFieldSecurity. This is not qualification of a real adapter.
- `LIFETIME-CONTRACTS.md`: staged default item/root replacement, gated event leases, captured teardown, borrowed helpers, timer identity and callback/registration acknowledgement draining. Replacement requires gated events/prepared items; legacy helpers only support fresh registration.
- `RELIABILITY-AND-ENHANCEMENT-PLAN.md` and `IMPLEMENTATION-LOG.md`: current gates/evidence. Historical completion notes do not certify adapters, mutable graph/system-state rollback, paging or full Oracle Forms parity.

Do not drain from an owning callback or registration preparation; dispose synchronously there and drain from
the host afterward. Public-operation scheduling and record-generation protection
are still separate work, not guarantees of callback draining.

## Specialized Skills
- [`forms-mode-transitions`](../forms-mode-transitions/SKILL.md)
- [`forms-operations-navigation`](../forms-operations-navigation/SKILL.md)
- [`forms-enhanced-data-operations`](../forms-enhanced-data-operations/SKILL.md)
- [`forms-helper-managers`](../forms-helper-managers/SKILL.md)
- [`forms-performance-configuration`](../forms-performance-configuration/SKILL.md)

## Integration with the data-management layer

`FormsManager` is BeepDM's platform-neutral form runtime. UoWs track DML;
Forms coordinates provider boundaries, typed outcomes and managed read policy.

| Direction | Layer | What flows |
|---|---|---|
| → **unitofwork** | `UnitofWork<T>` | All form saves flow through UoW. Forms does not write to the datasource directly. |
| ← **configeditor** | `ConfigEditor` façade | Reads entity structure from config cache; falls back to `IDataSource.GetEntityStructure`. |
| → **migration** | `MigrationManager` | Schema drift detected by Forms is reported, not auto-migrated. |
| ← **setup** | Setup Framework | Setup is invisible; Forms is visible. After setup runs, Forms is what the user sees. |
| ↔ **etl** | Pipeline engine | Forms displays ETL output; Forms does not trigger ETL. |

The Mavis cross-project equivalent of this skill lives at `.harness/skills/beepdm-forms/SKILL.md`.

## Detailed Reference
Read `LOCAL-PAGING.md` for typed local cursor outcomes and exact long math.
LoadPageAsync does not fetch a provider page; require acknowledged page state
before UI focus/refresh. Cancellation/failure can retain cursor effects or accepted
page state with notification errors. Read `PROVIDER-PAGING.md` for opt-in bounded
staged fetch under mandatory policy, row/byte limits and complete key ordering.
ProviderPage is count evidence; require RecordsPublished before UI acceptance.
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

Use [`reference.md`](./reference.md) for scenarios and examples.
