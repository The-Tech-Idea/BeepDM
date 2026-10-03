---
name: forms-helper-managers
description: FormsManager helper guidance for BeepDM. Use when changing dirty saves, relationships, item registration/security projection, UoW event ownership, timers/messages, captured validation/LOV/editor targets, UI provider acknowledgement, or helper lifetime behavior.
---

# Forms Helper Managers

Use this skill for helper classes under `Editor/Forms/Helpers` and their
FormsManager relationship/registration integration.

## File Locations
- `DataManagementEngineStandard/Editor/Forms/FormsManager.Relationships.cs`
- `DataManagementEngineStandard/Editor/Forms/Helpers/DirtyStateManager.cs`
- `DataManagementEngineStandard/Editor/Forms/Helpers/EventManager.cs`
- `DataManagementEngineStandard/Editor/Forms/Helpers/FormsSimulationHelper.cs`
- `DataManagementEngineStandard/Editor/Forms/Helpers/PerformanceManager.cs`
- `DataManagementEngineStandard/Editor/Forms/Helpers/TimerManager.cs`
- `DataManagementEngineStandard/Editor/Forms/Helpers/FormMessageBus.cs`
- `DataManagementEngineStandard/Editor/Forms/FormsManager.CallbackLifetime.cs`
- `DataManagementEngineStandard/Editor/Forms/FormsManager.RegistrationLifetime.cs`
- `DataManagementEngineStandard/Editor/Forms/FormsManager.RecordTargets.cs`
- `DataManagementEngineStandard/Editor/Forms/FormsManager.LovOutcomes.cs`
- `DataManagementEngineStandard/Editor/Forms/FormsManager.Editor.cs`
- `DataManagementModelsStandard/Editor/Forms/Interfaces/IFormsEditorOutcomes.cs`
- `DataManagementEngineStandard/Editor/Forms/Helpers/FormsViewBinding.cs`
- `DataManagementEngineStandard/Editor/Forms/FormsManager.BindingTargets.cs`
- `DataManagementModelsStandard/Editor/Forms/Hosts/IFormsBindingTargets.cs`
- `DataManagementModelsStandard/Editor/IUnitofWorkRecordRevision.cs`
- `DataManagementEngineStandard/Editor/Forms/Helpers/ItemPropertyManager.Registration.cs`

## Responsibilities
- `FormsManager.Relationships`: master-detail relation registration and synchronization
- `DirtyStateManager`: unsaved-change analysis and save/rollback decisions
- `EventManager`: trigger/event pipeline and unit-of-work subscriptions
- `FormsSimulationHelper`: reflection-based field access, audit defaults, sequences, and system variables
- `PerformanceManager`: cache and metrics support for block access

## Working Rules
1. Register blocks before relationship creation.
2. Preserve centralized event subscription/unsubscription paths.
3. Keep dirty-state checks in navigation and mode transitions.
4. Reuse simulation helpers instead of duplicating reflection logic in callers.

## Lifetime Rules
- Read `DataManagementEngineStandard/Editor/Forms/LIFETIME-CONTRACTS.md` before changing teardown or callback ownership.
- Detach the captured subscription source, not a later mutable block UoW. Retire callbacks before removal, isolate cleanup actions, and preserve failure evidence.
- Default EventManager implements `IGatedUnitOfWorkEventSubscriptions`; keep its owned lease and registration admission predicate. Record removal before calling an add accessor and unwind failed attempts. Missing optional events differ from present accessors that throw.
- Default ItemPropertyManager implements `IPreparedBlockItems`: prepare without touching live items, commit with owned-memory-only registry publication, restore the exact old store on failure, retire by lease identity. Replacement requires both capabilities; same-name overlap rejects and shared item helpers reject cross-form name aliases.
- Default SecurityManager implements `IQuerySecurityPublication`: revision-gate only the owned-memory staged read action. No provider/trigger/observer or metadata getter belongs inside publication monitors. Custom helpers without this capability reject staged managed reads before provider execution; do not fall back after a stage fails. See `READ-PUBLICATION.md`.
- Dispose only helpers created by the manager. Injected item/timer/performance helpers and UoWs remain borrowed.
- Default bus leases remove one handler, not all subscriptions sharing a form/type name. Legacy buses can only deactivate guarded wrappers without the optional lease capability.
- Timer callbacks carry entry identity. Prepare disabled, acknowledge activation before delivery, and keep schedulers/observers outside ownership monitors. Returned timer definitions are snapshots.
- Use a manual `TimeProvider` and barriers to test queued old callbacks and failed activation; do not rely on sleeps to create races.
- Drain includes registration, managed queries/detail/LOV/editor, their admitted callbacks and synchronous manager validation. It does not include every scalar/navigation/commit or source-factory operation. Borrowed editor providers own dispatch/dismissal and physical acknowledgement. Do not self-drain.
- Read `RECORD-TARGETS.md`: validation/LOV/editor captures record/item/request identity and observed revisions. Typed LOV/editor outcomes preserve setter evidence; popup inputs are copied and permission/raw-disclosure checks re-run before writing. Helper events/cache, lookup policy, async rules/context and final-window edits remain open; no callback/setter belongs under ownership monitors.

- Read `UI-BINDING-CONTRACTS.md` for opt-in binding ownership, copied/revisioned field policies, dispatcher acknowledgement, origin-aware presenters and adapter gates. UI bindings have their own host-owned drain; manager close does not dispose them.

## Related Skills
- [`forms`](../forms/SKILL.md)
- [`forms-operations-navigation`](../forms-operations-navigation/SKILL.md)
- [`forms-performance-configuration`](../forms-performance-configuration/SKILL.md)

## Detailed Reference
Read `PROVIDER-PAGING.md` before implementing provider reads: opt-in staged fetch
shares mandatory query policy, ordering, dirty protection, buffer receipts and
physical drain. Legacy paged Get is not a bounded capability or safe fallback.
Provider prefetch/cache remains unimplemented.

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

Use [`reference.md`](./reference.md) for helper responsibilities, trigger patterns, and pitfalls.
