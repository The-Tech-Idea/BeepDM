# FormsManager — Architecture

This document describes the structure of `FormsManager` and its supporting helpers. It is the "what is it" companion to the README's "how do I use it."

## 30-second summary

`FormsManager` is a **partial-class orchestrator** that owns no significant logic itself. It is a thin coordinator over 24 helper managers (one per concern: validation, LOV, triggers, security, audit, paging, etc.). It composes them, exposes them as properties, and routes every public method to the right helper.

The intent is to provide an Oracle Forms–compatible runtime surface that UIs (WinForms, Blazor, Razor) can call into without re-implementing the orchestration. The UIs provide an `IBuiltinHost` adapter; the engine returns the `IBeepBuiltins` built-in surface.

## Folder layout

```
DataManagementEngineStandard/Editor/Forms/
├── README.md                          ← top-level index
├── EXECUTIVE_SUMMARY.md               ← stale (do not use)
├── MIGRATION-GUIDE.md                 ← legacy notes
├── plan.instructions.md               ← historical
│
├── FormsManager.Core.cs               ← constructor + field + lifetime
├── FormsManager.Properties.cs         ← readonly public properties (24 helpers + state)
├── FormsManager.Alerts.cs             ← message, alert, confirm
├── FormsManager.Audit.cs              ← audit configuration + query/export
├── FormsManager.BasicDataOps.cs       ← insert/delete/query
├── FormsManager.BlockProperties.cs    ← SET_BLOCK_PROPERTY + friend shortcuts
├── FormsManager.BlockRegistration.cs  ← register/unregister block + savepoint + setup-from-source
├── FormsManager.DataOperations.cs     ← undo/redo, batch commit, export/import, aggregates, state snapshot
├── FormsManager.DirtyState.cs         ← unsaved-changes handling
├── FormsManager.DmlTriggers.cs        ← DML trigger wrappers
├── FormsManager.EnhancedOperations.cs ← insert/update/query with audit defaults
├── FormsManager.FormOperations.cs     ← open/close/commit/rollback/clear
├── FormsManager.FormsSimulation.cs    ← SetFieldValue, GetFieldValue, ExecuteSequence
├── FormsManager.GenericOperations.cs  ← typed wrappers + ShowLOVAsync
├── FormsManager.Helpers.cs            ← private cross-helper plumbing
├── FormsManager.InterFormComm.cs      ← :GLOBAL.* + message bus + shared blocks
├── FormsManager.KeyTriggers.cs        ← KEY- trigger registration + firing
├── FormsManager.Lifecycle.cs          ← Dispose
├── FormsManager.ModeTransitions.cs    ← ENTER_QUERY, EXECUTE_QUERY, CRUD transitions
├── FormsManager.MultiFormNavigation.cs← CALL_FORM, OPEN_FORM, NEW_FORM, return-to-caller
├── FormsManager.Navigation.cs         ← record navigation + history + GO_BLOCK/GO_ITEM
├── FormsManager.Performance.cs        ← paging, fetch-ahead, cache, lazy load
├── FormsManager.Relationships.cs      ← master/detail registration + sync
├── FormsManager.Security.cs           ← security context + block/field security + masking
├── FormsManager.Sequences.cs          ← sequences + item defaults + copy-field
├── FormsManager.Timers.cs             ← CREATE_TIMER, DELETE_TIMER
├── FormsManager.TriggerChaining.cs    ← trigger execution graph + log
├── FormsManager.Validation.cs         ← field/block validation entry points
│
├── Builtins/                          ← Oracle Forms built-in surface (IBeepBuiltins)
│   ├── IBeepBuiltins.cs               ← the 30+ built-ins interface
│   └── BeepBuiltinException.cs        ← Forms-style error codes (e.g. FRM-41003)
│
├── Configuration/                     ← configuration DTOs
│   ├── ConfigurationManager.cs
│   ├── FormConfiguration.cs
│   ├── NavigationConfiguration.cs
│   ├── PerformanceConfiguration.cs
│   ├── UnitofWorksManagerConfiguration.cs
│   └── ValidationConfiguration.cs
│
├── Helpers/                           ← 24 helper managers
│   ├── FormsSimulationHelper.cs
│   ├── EventManager.cs                ← 23+ events for block/record/DML lifecycle
│   ├── TriggerManager.cs              ← 46KB; full trigger engine
│   ├── TriggerLibrary.cs              ← 23KB; built-in trigger catalog
│   ├── TriggerDependencyManager.cs    ← trigger ordering + cycle detection
│   ├── TriggerExecutionLog.cs         ← trigger log
│   ├── ValidationManager.cs           ← 42KB; full validation engine
│   ├── ValidationRuleLibrary.cs       ← built-in rules
│   ├── ValidationRuleBuilder.cs       ← fluent rule builder
│   ├── LOVManager.cs                  ← 22KB; LOV orchestration + cache
│   ├── ItemPropertyManager.cs         ← 35KB; SET_ITEM_PROPERTY + GET_ITEM_PROPERTY
│   ├── SystemVariablesManager.cs      ← 18KB; :SYSTEM.* variables
│   ├── MasterDetailKeyResolver.cs     ← 15KB; key matching across master/detail
│   ├── PerformanceManager.cs          ← 22KB; caching + metrics
│   ├── PagingManager.cs               ← page state
│   ├── RelationshipManager.cs         ← 16KB; legacy standalone (prefer FormsManager.Relationships)
│   ├── QueryBuilderManager.cs         ← 10KB; filter composition
│   ├── SavepointManager.cs            ← savepoint lifecycle
│   ├── LockManager.cs                 ← block lock
│   ├── DirtyStateManager.cs           ← 20KB; dirty tracking
│   ├── CrossBlockValidationManager.cs ← cross-block rules
│   ├── BlockErrorLog.cs               ← per-block error log
│   ├── AuditManager.cs                ← audit orchestrator
│   ├── FileAuditStore.cs              ← file-backed IAuditStore
│   ├── InMemoryAuditStore.cs          ← in-memory IAuditStore
│   ├── SecurityManager.cs             ← block/field security + masking
│   ├── SequenceProvider.cs            ← in-memory sequences
│   ├── TimerManager.cs                ← CREATE_TIMER + WHEN-TIMER-EXPIRED
│   ├── BlockFactory.cs                ← block-from-source factory
│   ├── BlockPropertyManager.cs        ← block-level property bag
│   ├── DefaultAlertProvider.cs        ← default IAlertProvider
│   ├── MessageQueueManager.cs         ← per-block message queue
│   ├── FormRegistry.cs                ← multi-form registry
│   ├── FormMessageBus.cs              ← inter-form message bus
│   ├── SharedBlockManager.cs          ← cross-form shared blocks
│   ├── TypeBridgeAdapters.cs          ← 12KB; typed record helpers
│   ├── NavigationHistoryManager.cs    ← per-block back/forward stack
│   └── FormsManager.original.cs.bak   ← BACKUP; safe to delete
│
├── Interfaces/
│   └── IUnitofWorksManagerInterfaces.cs ← 38 interfaces/types, 2557 lines
│
└── Models/                            ← 62 model files (DTOs, enums, EventArgs)
```

## Layering

There are four conceptual layers, from top (closest to the host UI) to bottom (closest to the data source):

```
┌─────────────────────────────────────────────────────────────────┐
│ Layer 4 — Host (WinForms / Blazor / Razor)                       │
│ Implements IBuiltinHost, provides IBeepBuiltins to the engine.   │
│ Owns rendering, focus, keyboard, visual attributes.              │
└─────────────────────────────────────────────────────────────────┘
                              │ IBuiltinHost
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Layer 3 — Built-ins surface (Builtins/)                          │
│ IBeepBuiltins — Oracle Forms built-in surface (GO_BLOCK,         │
│ NEXT_RECORD, SHOW_LOV, MESSAGE, ALERT, COMMIT, POST, ...).      │
│ Pure routing: every call resolves to a FormsManager method.     │
└─────────────────────────────────────────────────────────────────┘
                              │ FormsManager.X
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Layer 2 — Orchestrator (FormsManager partial classes)           │
│ The 28 FormsManager*.cs files. Owns state, validates input,      │
│ routes to helpers, fires events, builds result DTOs.            │
│ No significant business logic lives here.                       │
└─────────────────────────────────────────────────────────────────┘
                              │ Helper.X
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Layer 1 — Helpers (Helpers/)                                     │
│ 24 helper managers, each owning one concern:                     │
│ validation, LOV, triggers, security, audit, paging, paging,     │
│ sequences, timers, performance, etc.                             │
└─────────────────────────────────────────────────────────────────┘
                              │ IUnitofWork / IDMEEditor
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Layer 0 — Data source + UoW (engine core)                         │
│ IDMEEditor, IUnitofWork, IDataSource — already in BeepDM core.   │
│ Persistence, dirty tracking, query execution, commit/rollback.   │
└─────────────────────────────────────────────────────────────────┘
```

The host UIs (WinForms `BeepForms` / Blazor / Razor) implement `IBuiltinHost` and call `IBeepBuiltins` methods. The engine's job is to be **UI-agnostic** — every method that interacts with the user (alert, message, LOV dialog) routes through the host via the `IBuiltinHost` interface so the same engine works against a WinForms dialog or a Blazor component.

## Public surface

`FormsManager` exposes:

- **~150 public methods** (see `functional-matrix.md` for the full list)
- **~30 public properties** (most of them are exposed helper managers)
- **5 public events** (`OnFormOpen`, `OnFormClose`, `OnFormCommit`, `OnFormRollback`, `OnFormValidate`, `OnNavigate`, `OnCurrentChanged`, `OnBlockFieldChanged`)
- **18 helper properties** (each is the helper manager for a concern)

Total: roughly 200 public surface elements. The largest helpers (TriggerManager at 46KB, ValidationManager at 42KB, ItemPropertyManager at 35KB) carry most of the implementation weight; the FormsManager itself is mostly routing.

## Request flow: a worked example

`manager.EnterQueryModeAsync("CUSTOMERS")` (a 6-line method on `FormsManager.ModeTransitions.cs`):

1. **Validate input** — block exists, has a UoW, isn't already in query mode.
2. **Check unsaved changes** — `_crossBlockValidation` + `CheckAndHandleUnsavedChangesAsync` block if there's pending work.
3. **Fire pre-event** — `_eventManager.TriggerPreQuery(blockName)` raises `OnPreQuery` (cancellable).
4. **Call into the UoW** — `unitOfWork.EnterQueryMode()`. The actual mode switch is in the persistence layer.
5. **Fire WHEN-NEW-BLOCK-INSTANCE** — `_triggerManager.FireBlockTriggerAsync("WHEN-NEW-BLOCK-INSTANCE", blockName)`. If a rule is registered, it runs.
6. **Set system variables** — `_systemVariablesManager.UpdateForModeChange(blockName, DataBlockMode.Query)`.
7. **Build result** — `IErrorsInfo` with `Flag = Errors.Ok` (or `Failed` on any step).
8. **Fire post-event** — `_eventManager.TriggerPostQuery(blockName)`.
9. **Return** the result.

The orchestrator method is ~25 lines. The helper methods it calls (especially in `EventManager` and `TriggerManager`) carry the actual semantics. This pattern repeats across all 28 partials.

## Composition vs. inheritance

`FormsManager` does **not** inherit from a base class. It composes its helpers:

```csharp
public partial class FormsManager : IUnitofWorksManager
{
    private readonly IDirtyStateManager _dirtyStateManager;     // injected
    private readonly IValidationManager _validationManager;      // injected
    // ... 22 more ...
    public IDirtyStateManager DirtyStateManager => _dirtyStateManager;     // exposed
    public IValidationManager Validation => _validationManager;            // exposed
    // ... 18 more ...
}
```

The constructor (28 lines in `FormsManager.Core.cs`) takes every helper as an optional parameter. If you pass `null`, the manager constructs a default implementation:

```csharp
public FormsManager(
    IDMEEditor dmeEditor,
    IDirtyStateManager dirtyStateManager = null,
    IValidationManager validationManager = null,
    // ... 22 more ...
)
{
    _validationManager = validationManager ?? new ValidationManager();
    _lovManager = lovManager ?? new LOVManager(_dmeEditor, _blocks);
    // ...
}
```

This means:

- The default wiring lives in one place (the constructor).
- Tests can swap any helper for a mock by passing it in.
- UIs that want to override a helper (e.g. a custom `IAlertProvider` that uses the host's dialog) just pass it.

The two interfaces — `IUnitofWorksManager` (the public surface for legacy callers) and `IBeepBuiltins` (the Oracle Forms built-ins) — sit on top of the same orchestrator.

## State

FormsManager holds the following state (all in `FormsManager.Core.cs`):

| Field | Purpose |
| --- | --- |
| `IDMEEditor _dmeEditor` | The engine root. |
| `ConcurrentDictionary<string, DataBlockInfo> _blocks` | Registered blocks by name. |
| `ConcurrentDictionary<string, List<DataBlockRelationship>> _relationships` | Master/detail relationships. |
| `string _currentFormName` | The currently open form. |
| `string _currentBlockName` | The currently focused block. |
| `Stack<FormCallStackEntry> _callStack` | For `CALL_FORM` / `RETURN`. |
| `ConcurrentDictionary<string, object> _formParameters` | Parameters passed via `CALL_FORM` / `OPEN_FORM`. |
| 24 helper instances | Injected via constructor. |
| `object _lockObject` | For thread-safe transitions. |
| `bool _disposed` | IDisposable lifecycle. |

The orchestrator **does not own** block-level state (records, current index, dirty flags). Those live on the per-block `IUnitofWork` and on the `DataBlockInfo` envelope. FormsManager holds *the index of which block is current* but not *what's in the current block*.

## Events

FormsManager has 5 events on the orchestrator itself, plus 23+ events on the various helpers (total 54 events across the folder). The orchestrator-level events are:

- `OnFormOpen` / `OnFormClose` / `OnFormCommit` / `OnFormRollback` / `OnFormValidate` — `FormTriggerEventArgs` (cancellable)
- `OnNavigate` / `OnCurrentChanged` — `NavigationTriggerEventArgs`
- `OnBlockFieldChanged` — `BlockFieldChangedEventArgs` (per-field change feed)

Helper-level events (e.g. `TriggerManager.TriggerExecuting`, `ValidationManager.ValidationFailed`, `LOVManager.LOVDataLoaded`, `SecurityManager.OnSecurityViolation`, `MessageQueueManager.OnMessage`) are exposed through the helper properties. UI hosts subscribe to whichever level is appropriate — orchestrator events for top-level form lifecycle, helper events for fine-grained concerns.

## Thread safety

`FormsManager` is **thread-safe for read operations** and **serialized for state transitions**. Specifically:

- `ConcurrentDictionary` is used for `_blocks`, `_relationships`, `_formParameters`, `_itemChangedHandlers`, `_mdCurrentChangedHandlers`, `_syncSuppressCount`.
- The current-form-name and current-block-name are mutated under `_lockObject` for the duration of mode transitions and form open/close.
- Per-block operations (navigation, validation, field change) don't lock at the FormsManager level — they coordinate through the per-block UoW and the per-block helpers.
- The host UI is expected to call public methods on the UI thread (WinForms message loop) or via `Task.Run` with a captured sync context. Cross-thread calls into a `IUnitofWork` that's bound to a UI thread are not supported and will fail.

The original design was for WinForms single-threaded use. The async surface is real, but the underlying UoW / datasource calls still expect a UI-thread sync context in practice.

## Lifecycle

`FormsManager` is `IDisposable`. `Dispose()` (`FormsManager.Lifecycle.cs`) calls `Dispose(true)` on each disposable helper (currently: `IEventManager` indirectly, `IPerformanceManager`, `ITimerManager`, `ITriggerManager`). After `Dispose`, the manager should not be used.

`new FormsManager(dmeEditor)` is the typical entry point. The class is registered via the `IUnitofWorksManager` interface for legacy callers and via direct construction for new code.

## What this layer is NOT

- **Not a renderer.** Visual attributes, fonts, colors, layouts, focus, keyboard — all owned by the host UI.
- **Not a query builder on its own.** `QueryBuilderManager` helps, but the actual SQL lives in `IUnitofWork` and `IDataSource`.
- **Not a security model.** `SecurityManager` checks `BlockSecurity` and `FieldSecurity` against a `SecurityContext`, but the actual authentication and authorization live in the host (or in `IBeepEditor`'s user store).
- **Not a state machine for transactions.** Cross-form transactional rollback is partial; FormsManager assumes one form owns the active transaction at a time.

## See also

- [`ORACLE-FORMS-MAPPING.md`](ORACLE-FORMS-MAPPING.md) — the full concept-to-API mapping.
- [`functional-matrix.md`](functional-matrix.md) — every public type and capability in tabular form.
- [`functionality/`](functionality/) — per-subsystem deep-dives.
