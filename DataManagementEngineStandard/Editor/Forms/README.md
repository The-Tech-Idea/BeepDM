# FormsManager — Oracle Forms Runtime Engine

`FormsManager` is the BeepDM form-orchestration runtime in the `TheTechIdea.Beep.Editor.UOWManager`
namespace. It implements `IUnitofWorksManager` and coordinates block registration, navigation,
mode transitions, master/detail synchronization, triggers, LOVs, validation, auditing, security,
paging, multi-form communication, alerts, timers, sequences, savepoints, and undo/redo.

**Last updated:** 2026-06-17 — All 37 gaps resolved (32 fixed, 5 deferred). 42 code-quality
fixes applied across 10 audit passes. Cross-project IDE/WPF/WinForms compile issues fixed.

---

## 1. File Map — 30 Partial Classes + 60 Models + 34 Helpers

```
Editor/Forms/
├── FormsManager.Core.cs               Constructor + 24 DI helpers + lifetime
├── FormsManager.Properties.cs         28+ readonly public properties
├── FormsManager.BlockRegistration.cs  Register / Unregister / SetupBlock / Savepoints
├── FormsManager.DataOperations.cs     Undo/Redo / Batch commit / Export/Import / Aggregates
├── FormsManager.BasicDataOps.cs       Insert / Delete / EnterQuery / ExecuteQuery / PostBlockAsync
├── FormsManager.Navigation.cs         Record + block navigation / GO_BLOCK / GO_ITEM / NEXT_ITEM
├── FormsManager.ModeTransitions.cs    ENTER_QUERY → EXECUTE_QUERY → CRUD transitions
├── FormsManager.Relationships.cs      Master/detail + composite keys / StandaloneBlocks / RegisterDiscoveredForm
├── FormsManager.FormOperations.cs     Open / Close / Commit / Rollback / Clear / Validate / cross-form tx
├── FormsManager.GenericOperations.cs  Typed block wrappers + ShowLOVAsync
├── FormsManager.EnhancedOperations.cs Insert/update with audit defaults + query enhancement
├── FormsManager.FormsSimulation.cs    SetFieldValue / GetFieldValue / ExecuteSequence / ClearItem
├── FormsManager.DmlTriggers.cs        ON-INSERT / ON-UPDATE / ON-DELETE wrappers + RaiseFormTriggerAsync
├── FormsManager.KeyTriggers.cs        KEY- trigger registration + default actions (Task<TriggerResult>)
├── FormsManager.TriggerChaining.cs    Trigger execution DAG + execution log (DI-injected)
├── FormsManager.Validation.cs         Field + block validation (event + rule-based)
├── FormsManager.Security.cs           Block/field security + row filtering + masking + ClearBlockSecurity
├── FormsManager.Audit.cs              Audit trail configuration + query + export
├── FormsManager.Alerts.cs             MESSAGE / SHOW_ALERT / BELL equivalents + ShowAlertAsync
├── FormsManager.Timers.cs             CREATE_TIMER / DELETE_TIMER
├── FormsManager.Sequences.cs          Sequences + item defaults + COPY/APPLY_DEFAULT_VALUES
├── FormsManager.InterFormComm.cs      :GLOBAL.* / parameters / message bus / shared blocks
├── FormsManager.MultiFormNavigation.cs CALL_FORM / NEW_FORM / OpenFormModelessAsync / ReturnToCallerAsync
├── FormsManager.BlockProperties.cs    SET_BLOCK_PROPERTY / GET_BLOCK_PROPERTY
├── FormsManager.Performance.cs        Paging / fetch-ahead / lazy load / cache / UoW virtual mode
├── FormsManager.DirtyState.cs         Unsaved-changes detection + handling
├── FormsManager.Lifecycle.cs          Dispose (all events properly unsubscribed)
├── FormsManager.Helpers.cs            Private plumbing (SuppressSync, ResolveEntityType, etc.)
├── FormsManager.RecordGroups.cs       Record groups + Parameter lists + Client info
├── FormsManager.ExtendedOperations.cs UoW surfacing: bookmarks, computed columns, freeze, find/clone, change log,
│                                       virtual mode, aggregates, transactions, TEXT_IO, app properties
│
├── Builtins/                          Oracle Forms built-in surface
│   ├── IBeepBuiltins.cs               ~53 built-ins (PostBlockAsync added)
│   ├── BeepBuiltinException.cs        BEEP- error codes (not FRM-)
│   └── BeepFormsHostAdapter.cs        IBuiltinHost → IBeepFormsHost bridge
│
├── Configuration/ (6 files)           JSON-backed config DTOs
├── Helpers/ (34 files)                24 manager classes + utilities
├── Hosts/                             IBeepFormsHost (CancellationToken + PostBlockAsync + ShowAlertAsync)
├── Interfaces/ (10 files)             IUnitofWorksManager + 38 sub-interfaces
├── Models/ (60+ files)                DTOs, enums, EventArgs, BlockStatus
└── functionality/ (15 .md files)      Per-subsystem deep-dive docs
```

## 2. Architecture

FormsManager follows a **flat partial-class + DI composition** model. All 24 helper
managers are injectable through the constructor with default fallbacks. The engine
is UI-agnostic — UIs (WinForms, WPF, Blazor) provide an `IBuiltinHost` adapter.

```
Host UI (WinForms / WPF / Blazor)
  → IBeepBuiltins  (53 built-ins)
    → IBuiltinHost  (adapter bridge)
      → IUnitofWorksManager  (FormsManager)
        → 24 helper managers  (one per concern)
          → IUnitofWork / IDataSource  (persistence layer)
```

## 3. Key Public API

The `IUnitofWorksManager` interface exposes **~200 methods** across these categories:
- Block registration, CRUD, navigation, mode transitions, form lifecycle
- Master-detail relationships (single + composite key)
- Triggers (200+ types), validation, LOVs, security, audit
- Multi-form (CALL_FORM, OPEN_FORM, NEW_FORM), inter-form messages
- Record groups, parameter lists, client info, application properties
- Bookmarks, computed columns, freeze/batch, entity search, change log
- UoW virtual/lazy loading, source aggregates, source transactions
- Post, Text I/O, ClearItem, savepoints, undo/redo, batch export/import
- Alerts (MESSAGE, SHOW_ALERT), sequences, timers

## 4. Gap Status (2026-06-17)

**All 37 gaps resolved:**
- **P0 (14/14 fixed):** Correctness fixes, custom-item-event, dependency depth limit, DI bypass
- **P1 (5/7 fixed, 1 deferred, 1 already existed):** Composite keys, record groups, parameter lists, client info, PROGRAM_UNIT deferred
- **P2 (3/5 fixed, 2 deferred):** WHERE clause parser, TEXT_IO, app properties; VARR/DBMS_PIPE deferred
- **P3 (9/11 fixed, 2 deferred):** Bookmarks, computed columns, freeze/batch, find/clone, change log, virtual mode, aggregates, transactions; auto-discovery/entity lifecycle deferred

**42 code-quality fixes:** Duplicate methods, DI bypass, adapter stubs, orphaned dispose, security memory leak, silent catch blocks, sync-over-async deadlocks, fragile type strings, missing readonly

## 5. Documentation Map

| Document | Purpose |
|----------|---------|
| **[`ORACLE-FORMS-MAPPING.md`](ORACLE-FORMS-MAPPING.md)** | Oracle Forms concept → method mapping |
| **[`gaps.md`](gaps.md)** | All 37 gaps tracked with status |
| **[`enhancements.md`](enhancements.md)** | Improvement opportunities |
| **[`architecture.md`](architecture.md)** | Subsystems, layering, host model |
| **[`.plans/enhancement-plan.md`](.plans/enhancement-plan.md)** | Phased roadmap |
| **[`Models/README.md`](Models/README.md)** | Model catalog |
| **[`Configuration/README.md`](Configuration/README.md)** | Configuration DTOs |
