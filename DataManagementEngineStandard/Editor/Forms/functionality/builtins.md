# FormsManager — Built-ins (`IBeepBuiltins`)

This document covers `IBeepBuiltins`, the canonical Oracle Forms built-in surface. The interface is implemented by the engine for hosts that prefer the Oracle verbs to the orchestrator-level methods.

## What `IBeepBuiltins` is

A thin routing layer that maps each Oracle Forms built-in verb to the corresponding `FormsManager` method (or, for UI-specific built-ins, to a host-routed call via `IBuiltinHost`). It exists so a host that has historically used Oracle Forms verbs can keep doing so without learning the orchestrator's API.

The engine provides the interface but does not ship a default implementation. The implementation is a per-host adapter that wraps the host's `FormsManager` instance and routes each call. The WinForms `BeepForms` / `BeepFormsHost` produces one; the Blazor / Razor hosts should produce their own.

## Two interfaces: `IBeepBuiltins` and `IBuiltinHost`

- **`IBeepBuiltins`** — the Oracle Forms verb surface (the "what").
- **`IBuiltinHost`** — the engine-side callback surface (the "where"). Every method on `IBeepBuiltins` that needs a host (e.g. `ShowLov` needs the host to render the dialog) routes through `IBuiltinHost` to get the host's implementation.

This separation is what makes the engine UI-agnostic. The engine owns the "what"; the host owns the "how".

## The full built-ins surface

`IBeepBuiltins.cs` (in `Builtins/`) defines the following methods. They are organized by category; the mapping to Oracle Forms is in [`ORACLE-FORMS-MAPPING.md`](../ORACLE-FORMS-MAPPING.md) section 22.

### Identity

- `string? CurrentBlock { get; }`
- `string? CurrentItem { get; }`
- `IBuiltinHost Host { get; }`

### Block navigation

- `bool GoBlock(string blockName);`
- `bool NextBlock();`
- `bool PreviousBlock();`
- `bool FirstBlock();`
- `bool LastBlock();`

### Record navigation

- `bool FirstRecord();`
- `bool LastRecord();`
- `bool NextRecord();`
- `bool PreviousRecord();`
- `bool GoRecord(int oneBased);`

### Item navigation

- `bool GoItem(string itemName);`
- `bool NextItem();`
- `bool PreviousItem();`

### Item / block properties

- `bool SetItemProperty(string itemName, string property, object? value);`
- `object? GetItemProperty(string itemName, string property);`
- `bool SetBlockProperty(string blockName, string property, object? value);`
- `object? GetBlockProperty(string blockName, string property);`

### LOV

- `bool ShowLov(string blockName, string fieldName);`
- `bool ShowLov(string blockName, string fieldName, out object? selectedValue);`

### Transaction / form lifecycle

- `bool Commit();`
- `Task<bool> CommitAsync(CancellationToken ct = default);`
- `bool Rollback();`
- `Task<bool> RollbackAsync(CancellationToken ct = default);`
- `bool Post();`
- `Task<bool> PostAsync(CancellationToken ct = default);`

### Query mode

- `bool EnterQuery();`
- `bool ExecuteQuery();`
- `bool ExitQuery();`
- `Task<bool> ExecuteQueryAsync(CancellationToken ct = default);`

### Clear / reset

- `bool ClearBlock();`
- `bool ClearForm();`
- `bool ClearRecord();`

### Mode introspection

- `DataBlockMode GetBlockMode(string blockName);`
- `bool SetBlockMode(string blockName, DataBlockMode mode);`

### Messaging

- `void Message(string text, int ack = 0, BeepBuiltinMessageSeverity severity = BeepBuiltinMessageSeverity.Info);`
- `void ClearMessage();`
- `Task<int> AlertAsync(string title, string message, BeepBuiltinAlertStyle style, string button1, string? button2 = null, string? button3 = null, CancellationToken ct = default);`

### Diagnostics

- `IReadOnlyList<string> GetAvailableBuiltins();`

### Multi-form

- `bool OpenForm(string formName);`
- `bool CloseForm(string formName);`
- `bool GoForm(string formName);`
- `void SetGlobal(string name, object? value);`
- `object? GetGlobal(string name);`

### Extended built-ins

- `object? PopupLov(string blockName, string fieldName, string? searchText = null);`
- `IReadOnlyList<object> ListValues(string blockName, string fieldName);`
- `void SetApplicationProperty(string name, object? value);`
- `object? GetApplicationProperty(string name);`
- `void SetFormProperty(string name, object? value);`
- `object? GetFormProperty(string name);`
- `void RaiseFormTriggerFailure(string failureCode, string message);`

## The `IBuiltinHost` surface

For built-ins that need host UI rendering, the engine calls back through `IBuiltinHost`:

### Block / item state

- `string? ActiveBlockName { get; }`
- `string? ActiveItemName { get; }`
- `bool TrySetActiveBlock(string blockName);`
- `bool TrySetActiveItem(string blockName, string itemName);`
- `bool IsBlockRegistered(string blockName);`
- `bool IsItemRegistered(string blockName, string itemName);`
- `IReadOnlyList<string> GetRegisteredBlockNames();`
- `IReadOnlyList<string> GetRegisteredItemNames(string blockName);`
- `DataBlockInfo? GetBlockInfo(string blockName);`
- `IUnitofWork? GetBlockUnitOfWork(string blockName);`
- `object? GetCurrentBlockItem(string blockName);`

### Mode / record

- `bool IsBlockQueryAllowed(string blockName);`
- `void SetBlockCurrentRecordIndex(string blockName, int index);`
- `int GetBlockRecordCount(string blockName);`
- `DataBlockMode GetBlockMode(string blockName);`
- `void SetBlockMode(string blockName, DataBlockMode mode);`

### LOV (host-rendered)

- `bool HasLov(string blockName, string fieldName);`
- `LOVDefinition? GetLov(string blockName, string fieldName);`
- `Task<LOVResult> ShowLovAsync(string blockName, string fieldName, string? searchText, CancellationToken ct);`

### Item / block property bag

- `bool TryGetItemProperty(string blockName, string itemName, string property, out object? value);`
- `bool TrySetItemProperty(string blockName, string itemName, string property, object? value);`
- `bool TryGetBlockProperty(string blockName, string property, out object? value);`
- `bool TrySetBlockProperty(string blockName, string property, object? value);`

### Mutation

- `Task<bool> SaveBlockAsync(string blockName, CancellationToken ct);`
- `Task<bool> RollbackBlockAsync(string blockName, CancellationToken ct);`
- `Task<bool> InsertBlockRecordAsync(string blockName, CancellationToken ct);`
- `Task<bool> DeleteBlockCurrentRecordAsync(string blockName, CancellationToken ct);`
- `Task<bool> ExecuteQueryAsync(string blockName, CancellationToken ct);`
- `Task<bool> ClearBlockAsync(string blockName, CancellationToken ct);`
- `Task<bool> ClearRecordAsync(string blockName, CancellationToken ct);`

### Validation

- `RecordValidationResult? ValidateBlockRecord(string blockName, IDictionary<string, object> record, ValidationTiming timing);`

### Messaging (host-routed status bar / alert)

- `void PublishMessage(string message, int messageLevel, BeepBuiltinMessageSeverity severity);`
- `void ClearMessage();`
- `Task<int> ShowAlertAsync(string title, string message, BeepBuiltinAlertStyle style, string button1, string? button2, string? button3, CancellationToken ct);`

### Trigger plumbing (host-fanned-out)

- `void RaiseBuiltinTriggerExecuting(TriggerExecutingEventArgs args);`
- `void RaiseBuiltinTriggerExecuted(TriggerExecutedEventArgs args);`

### Multi-form (host-routed)

- `object? MultiFormOpenForm(string formName);`
- `bool MultiFormCloseForm(string formName);`
- `bool MultiFormGoForm(string formName);`
- `void MultiFormSetGlobal(string name, object? value);`
- `object? MultiFormGetGlobal(string name);`

### Application / Form property bag

- `void SetApplicationProperty(string name, object? value);`
- `object? GetApplicationProperty(string name);`
- `void SetFormProperty(string formName, string name, object? value);`
- `object? GetFormProperty(string formName, string name);`

### LIST_VALUES (host-rendered)

- `IReadOnlyList<object> ListLovRecords(string blockName, string fieldName);`

## How the routing works

When the host calls `IBeepBuiltins.ShowLov(block, field)`:

1. `ShowLov` calls into the engine (via the host's `FormsManager`).
2. The engine resolves the LOV definition, checks the cache, and loads data.
3. The engine calls `IBuiltinHost.ShowLovAsync(block, field, searchText, ct)` to render the dialog.
4. The host shows the dialog, the user picks a row (or cancels).
5. The host returns the chosen record via `LOVResult`.
6. The engine writes the result back to the bound field (and any mapped fields).
7. The engine raises the `LOVDataLoaded` event (and any others).

Steps 3-5 are the **host-routed** part. The engine doesn't render UI; the host does.

## Why a separate `IBuiltinHost`

The engine is **UI-agnostic** — it works for WinForms, Blazor, Razor, console, or test. The `IBuiltinHost` interface is the seam where the host plugs in its UI:

- For WinForms: `BeepFormsHost` in `Beep.Winform.Data.Integrated.Controls`.
- For Blazor Server: a Razor component implementing the same interface.
- For tests: a `NoOpHost` that records calls without rendering anything.

The interface is deliberately small (one method per host concern). The engine doesn't need to know about WinForms dialogs, Blazor components, etc. — only that the host can render them.

## `BeepBuiltinException`

When a built-in fails with a Forms-style error (e.g. `FRM-41003` "This form is currently in ENTER-QUERY mode"), the engine throws `BeepBuiltinException` with the failure code and message. The host catches and displays the error.

```csharp
try
{
    builtins.Post();
}
catch (BeepBuiltinException ex)
{
    Console.WriteLine($"{ex.FailureCode}: {ex.Message}");
}
```

`RaiseFormTriggerFailure` is the user-facing way to throw this from a custom trigger.

## Mapping summary

The `IBeepBuiltins` ↔ `IBuiltinHost` ↔ `FormsManager` ↔ `IDMEEditor` chain:

```
┌────────────────────┐
│ Host UI            │  (WinForms, Blazor, Razor, ...)
│                    │
│ IBeepBuiltins impl  │ ──► routes ──► IBuiltinHost impl
│                    │                  │
│ (knows about       │                  │ (knows about host UI)
│  the verb surface) │                  │
└──────────┬─────────┘                  │
           │                             │
           ▼                             ▼
   ┌──────────────┐             ┌────────────────┐
   │ FormsManager │             │ IBuiltinHost   │
   │ (engine)     │             │ impl (host)    │
   └──────┬───────┘             └────────────────┘
          │
          ▼
   ┌──────────────┐
   │ IDMEEditor   │
   │ + IUnitofWork│
   └──────────────┘
```

The host knows the verbs (Oracle Forms surface). The engine knows the orchestration. The host's UI knows the rendering.

## Notes for callers

- Prefer `IBeepBuiltins` over `FormsManager` direct methods when the calling code is "I have a verb, do the thing" (e.g. the WinForms `BeepForms` proxy uses `IBeepBuiltins` exclusively).
- Prefer `FormsManager` direct methods when the calling code is doing orchestration ("register these blocks, then enter query mode, then...").
- The `IBeepBuiltins` interface is **stable** — adding a new built-in is a non-breaking change. Changing the signature of an existing built-in would break hosts.
- The `IBuiltinHost` interface is **host-implemented** — the engine only calls into it. Adding a method to it requires updating every host that implements it.
- Built-ins are **synchronous** in the engine's view. The host can implement async behaviors (e.g. async `ShowLovAsync` for a Blazor component) but the engine treats the call as completed by the time the host's method returns.

## See also

- [`architecture.md`](../architecture.md) — the four-layer model and where `IBeepBuiltins` sits.
- [`ORACLE-FORMS-MAPPING.md`](../ORACLE-FORMS-MAPPING.md) section 22 — the full built-in mapping.
- [`lov.md`](lov.md) — the LOV surface in detail.
- [`multi-form.md`](multi-form.md) — the multi-form built-ins.
- [`alerts.md`](alerts.md) — the alert / message surface.
