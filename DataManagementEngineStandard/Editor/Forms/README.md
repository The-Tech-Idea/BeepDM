# FormsManager

`FormsManager` is the BeepDM form-orchestration runtime in the `TheTechIdea.Beep.Editor.UOWManager` namespace. It is the current concrete implementation behind `IUnitofWorksManager` and coordinates block registration, navigation, mode transitions, master/detail synchronization, triggers, LOVs, validation, auditing, security, paging, and multi-form communication. The intent is to provide an Oracle Forms–compatible runtime surface that UIs (WinForms, Blazor, Razor) can call into without re-implementing the orchestration.

This README replaces the older `UnitofWorksManager` naming found in earlier notes. The public runtime surface is `FormsManager`; the compatibility interface is still named `IUnitofWorksManager`.

## Documentation map

This folder has multiple documents. Read them in this order for the cleanest introduction:

1. **[`architecture.md`](architecture.md)** — subsystems, layering, request flow, and the host/orchestrator/helper model. Read this first to understand what FormsManager *is* before reading the API.
2. **[`ORACLE-FORMS-MAPPING.md`](ORACLE-FORMS-MAPPING.md)** — every Oracle Forms concept that has a counterpart here, with the exact FormsManager method and a status (complete / partial / missing). This is the **biggest gap-tracking document** in this folder.
3. **[`functional-matrix.md`](functional-matrix.md)** — every public type and capability, in tabular form. Use this as a lookup reference.
4. **[`functionality/`](functionality/)** — per-subsystem deep-dives, one document per major concern (navigation, triggers, validation, LOV, master-detail, security, audit, multi-form, timers, alerts, sequences, performance, builtins).
5. **[`gaps.md`](gaps.md)** — what the engine does not yet do. Read after the mapping doc.
6. **[`enhancements.md`](enhancements.md)** — prioritized list of opportunities to close those gaps.

Related documentation outside this folder:

- **[`MIGRATION-GUIDE.md`](MIGRATION-GUIDE.md)** — older legacy migration notes.
- **[`EXECUTIVE_SUMMARY.md`](EXECUTIVE_SUMMARY.md)** — older state-of-the-engine assessment. **Out of date**; do not use as a current reference. See [`gaps.md`](gaps.md) for the current state.
- **[`Helpers/README.md`](Helpers/README.md)**, **[`Interfaces/README.md`](Interfaces/README.md)**, **[`Models/README.md`](Models/README.md)** — index docs for the three subfolders.
- **[`plan.instructions.md`](plan.instructions.md)** — historical planning notes.

## What FormsManager owns

- Block registration, current-block state, and form-level orchestration.
- Master/detail registration and detail synchronization.
- Query-mode and CRUD-mode transitions.
- Navigation history, savepoints, locks, and cross-block validation.
- Oracle Forms-style built-ins such as alerts, timers, sequences, and block properties.
- Multi-form registry / message / shared-block plumbing.
- Integration of helper managers for validation, LOV, triggers, audit, security, paging, and caching.
- Runtime trigger metadata and block UoW activity that host UIs may observe only through proxy layers such as `BeepForms` / `IBeepFormsHost`.

## What stays in `IUnitofWork`

- Persistence and actual CRUD execution.
- Dirty-state ownership.
- Commit and rollback semantics.
- Datasource-backed identity refresh and sequence generation when the datasource owns key allocation.
- Query execution and loaded-record storage.

Integrated WinForms controls should not subscribe to `FormsManager.Triggers` or raw block `IUnitofWork` instances directly. If UI surfaces need trigger or CRUD/query/current-record activity, add or use a `BeepForms` host proxy so workflow ownership stays inside FormsManager.

## Key generation and ownership rules

1. If a caller, default, or trigger already supplied a valid key, FormsManager preserves it.
2. If a datasource owns identity or auto-increment values, leave the key unset until insert or commit refresh completes.
3. If a real sequence exists, prefer the datasource or UoW sequence path before using the in-memory FormsManager sequence provider.
4. Use FormsManager sequences for Oracle-style built-ins, deterministic tests, or non-database-backed scenarios.
5. Do not auto-number composite keys.
6. Never consume sequence values during query, paging, navigation, or cache prefetch.

## Architecture (one-paragraph version)

`FormsManager` is a partial-class orchestrator that owns no significant logic itself. It composes 24 helper managers (one per concern: validation, LOV, triggers, security, audit, paging, etc.), exposes them as properties (`manager.LOV`, `manager.Triggers`, `manager.Validation`, …), and routes every public method to the right helper. The winforms / blazor / razor host implements `IBuiltinHost` and gets the `IBeepBuiltins` Oracle-style built-in surface. Persistence stays in `IUnitofWork`; orchestration is the FormsManager's job. Full detail in [`architecture.md`](architecture.md).

## Quick start

```csharp
using TheTechIdea.Beep.Editor.UOWManager;

var manager = new FormsManager(dmeEditor)
{
    CurrentFormName = "CUSTOMER_ORDERS"
};

manager.RegisterBlock<CustomerDto>(
    blockName: "CUSTOMERS",
    unitOfWork: customerUow,
    entityStructure: customerEntity,
    dataSourceName: "Northwind",
    isMasterBlock: true);

manager.RegisterBlock<OrderDto>(
    blockName: "ORDERS",
    unitOfWork: orderUow,
    entityStructure: orderEntity,
    dataSourceName: "Northwind");

manager.CreateMasterDetailRelation(
    masterBlockName: "CUSTOMERS",
    detailBlockName: "ORDERS",
    masterKeyField: "CustomerId",
    detailForeignKeyField: "CustomerId");
```

If the `UnitofWork` already carries `EntityStructure`, you can register without passing it again:

```csharp
manager.RegisterBlock<CustomerDto>(
    blockName: "CUSTOMERS",
    unitOfWork: customerUow,
    dataSourceName: "Northwind",
    isMasterBlock: true);
```

## Common workflows

### Form lifecycle and query mode

```csharp
await manager.OpenFormAsync("CUSTOMER_ORDERS");

await manager.EnterQueryModeAsync("CUSTOMERS");

var filters = new List<AppFilter>
{
    new AppFilter { FieldName = "Country", Operator = "=", FilterValue = "USA" }
};

await manager.ExecuteQueryAndEnterCrudModeAsync("CUSTOMERS", filters);
await manager.FirstRecordAsync("CUSTOMERS");
await manager.NextRecordAsync("CUSTOMERS");

var commitResult = await manager.CommitFormAsync();
if (commitResult.Flag != Errors.Ok)
{
    await manager.RollbackFormAsync();
}
```

### Typed block registration and access

```csharp
manager.RegisterBlock<ProductDto>("PRODUCTS", productUow, productEntity, "Catalog");

var productBlock = manager.GetBlock<ProductDto>("PRODUCTS");
await manager.InsertRecordAsync<ProductDto>("PRODUCTS", new ProductDto
{
    ProductId = "P-100",
    Name = "Widget"
});
```

### LOV registration and selection

```csharp
var customerLov = LOVDefinition
    .CreateLookup("CUSTOMER_LOV", "Northwind", "Customers", "CustomerId", "CompanyName")
    .MapField("CompanyName", "CustomerName");

manager.LOV.RegisterLOV("ORDERS", "CustomerId", customerLov);

var lovResult = await manager.ShowLOVAsync("ORDERS", "CustomerId", searchText: "alf");
var selected = lovResult.Records.FirstOrDefault();

if (selected != null)
{
    await manager.ShowLOVAsync("ORDERS", "CustomerId", selectedRecord: selected);
}
```

`ShowLOVAsync` is the orchestration entry point. It loads data through `LOVManager`, respects cache settings, and writes both the return field and any related mapped fields back to the current record.

### Export, import, undo, and aggregates

```csharp
using var jsonStream = new MemoryStream();
await manager.ExportBlockToJsonAsync("ORDERS", jsonStream);

jsonStream.Position = 0;
await manager.ImportBlockFromJsonAsync("ORDERS_ARCHIVE", jsonStream, clearFirst: true);

manager.SetBlockUndoEnabled("ORDERS", enable: true);
var total = manager.GetBlockSum("ORDERS", "LineTotal");
var count = manager.GetBlockCount("ORDERS");
```

### Alerts, block properties, sequences, and timers

```csharp
manager.SetDefaultWhere("ORDERS", "IsDeleted = 0");
manager.SetOrderBy("ORDERS", "OrderDate desc");
manager.SetInsertAllowed("ORDERS", true);

manager.SetItemDefault("ORDERS", "Status", () => "Draft");
var nextNumber = manager.GetNextSequence("ORDER_SEQ");

var timer = manager.CreateTimer("REFRESH_ORDERS", TimeSpan.FromSeconds(30), repeating: true);
var confirmed = await manager.ConfirmAsync("Commit", "Save all pending changes?");

if (confirmed)
{
    await manager.CommitFormAsync();
}
```

### Multi-form, globals, and messaging

```csharp
manager.SetGlobalVariable("ACTIVE_CUSTOMER_ID", "ALFKI");
manager.SubscribeToMessage("CustomerChanged", message =>
{
    Console.WriteLine($"Received {message.MessageType} from {message.SenderForm}");
});

manager.PostMessage("ORDER_FORM", "CustomerChanged", "ALFKI");
await manager.CallFormAsync(
    "ORDER_FORM",
    new Dictionary<string, object> { ["CustomerId"] = "ALFKI" },
    FormCallMode.Modal);
```

Use `CallFormAsync` for modal child-form behavior. Use the overload `OpenFormAsync(string formName, Dictionary<string, object> parameters)` for modeless Oracle-style `OPEN_FORM`. The simpler `OpenFormAsync(string formName)` continues to represent the current form lifecycle open operation.

### Audit, security, and paging

```csharp
manager.SetAuditUser("fahad");
manager.ConfigureAudit(config =>
{
    config.Enabled = true;
    config.AuditedBlocks.Add("ORDERS");
});

manager.SetSecurityContext(new SecurityContext
{
    UserName = "fahad",
    Roles = new List<SecurityRole>
    {
        new SecurityRole { Name = "OrderClerk", Permissions = SecurityPermission.Query | SecurityPermission.Update }
    }
});

manager.SetBlockPageSize("ORDERS", 50);
manager.SetTotalRecordCount("ORDERS", 1200);
manager.SetFetchAheadDepth("ORDERS", 2);

var page = await manager.LoadPageAsync("ORDERS", 3);
var auditEntries = manager.GetAuditLog("ORDERS");
```

## Oracle Forms coverage snapshot (high-level)

| Oracle Forms concept | FormsManager API | Status |
| --- | --- | --- |
| `ENTER_QUERY` | `EnterQueryModeAsync` | ✅ complete |
| `EXECUTE_QUERY` | `ExecuteQueryAndEnterCrudModeAsync`, `ExecuteQueryAsync` | ✅ complete |
| `COMMIT_FORM` | `CommitFormAsync` | ✅ complete |
| `ROLLBACK_FORM` | `RollbackFormAsync` | ✅ complete |
| `CLEAR_FORM` / `CLEAR_BLOCK` / `CLEAR_RECORD` | `ClearAllBlocksAsync`, `ClearBlockAsync` | ✅ complete |
| `GO_BLOCK` / `GO_ITEM` / `GO_RECORD` | `SwitchToBlockAsync`, `GoItemAsync`, `NavigateToRecordAsync` | ✅ complete |
| `NEXT_RECORD` / `PREVIOUS_RECORD` | `NextRecordAsync`, `PreviousRecordAsync` | ✅ complete |
| `SHOW_LOV` | `ShowLOVAsync` plus `LOV.RegisterLOV` | ✅ complete |
| `SET_BLOCK_PROPERTY` | `SetBlockProperty`, `SetDefaultWhere`, `SetOrderBy` | ✅ complete |
| `MESSAGE` / `SHOW_ALERT` | `SetMessage`, `ShowAlertAsync`, `ConfirmAsync` | ✅ complete |
| `:SEQUENCE.NEXTVAL` | `GetNextSequence` | ✅ complete |
| `CREATE_TIMER` | `CreateTimer` | ✅ complete |
| `CALL_FORM` / `OPEN_FORM` / `NEW_FORM` | `CallFormAsync`, `OpenFormAsync`, `NewFormAsync` | ✅ complete |
| `:GLOBAL.*` | `SetGlobalVariable`, `GetGlobalVariable` | ✅ complete |
| `POST` (record-level commit) | `PostAsync` (via `IBeepBuiltins`) | ✅ complete |
| `KEY-` triggers | `RegisterKeyTrigger` | ✅ complete |
| WHEN-NEW-BLOCK-INSTANCE / WHEN-NEW-RECORD-INSTANCE | Trigger system | ✅ complete |
| WHEN-VALIDATE-RECORD / WHEN-VALIDATE-ITEM | Trigger system + validation hooks | ✅ complete |
| `RAISE_FORM_TRIGGER_FAILURE` | `RaiseFormTriggerFailure` (via `IBeepBuiltins`) | ✅ complete |
| `POPUP_LOV` / `LIST_VALUES` | `PopupLov`, `ListValues` (via `IBeepBuiltins`) | ✅ complete |
| `SET_APPLICATION_PROPERTY` / `GET_APPLICATION_PROPERTY` | `SetApplicationProperty` / `GetApplicationProperty` (via `IBeepBuiltins`) | ✅ complete |
| `ENTER_QUERY` with multiple filter criteria | `ExecuteQueryAsync` accepts a filter list | ✅ complete |
| Visual attributes / font / color | ❌ **not in this layer** — UI-specific, owned by host | n/a |
| `PL/SQL` library procedures | ❌ not emulated | ❌ missing |
| `LOV`-column properties (display width, return-item, etc.) | ✅ partial — width/return only | ⚠️ partial |
| Multi-form transactional rollback | ⚠️ partial — single-form rollback works; cross-form rollback not coordinated | ⚠️ partial |
| `WHEN-CUSTOM-ITEM-EVENT` | ⚠️ partial — wired via `EventManager` but no canonical custom-item event type | ⚠️ partial |

For the **full** mapping including items NOT in this summary, see [`ORACLE-FORMS-MAPPING.md`](ORACLE-FORMS-MAPPING.md).

## Current test coverage

### Unit suites

- `FormsManager.Core.Tests`
- `FormsManager.Navigation.Tests`
- `FormsManager.FormOperations.Tests`
- `FormsManager.ModeTransitions.Tests`
- `TriggerManager.Tests`
- `ValidationManager.Tests`
- `LOVManager.Tests`
- `SavepointManager.Tests`
- `LockManager.Tests`

### Integration slices

- Master/detail cascade when the master current record changes.
- Full form lifecycle from open through query, edit, commit, and close.
- Cross-block validation stopping commit before dirty-state save.
- Concurrent block-local navigation across separate blocks.
- Concrete-datasource LOV loading, cache reuse, and selected-record population.
- JSON and CSV export/import round-trips through block capabilities.

The LOV integration slice exposed a real orchestration bug: the selected LOV return value was not being written back to the bound field when `LOVManager` returned its internal `__RETURN_VALUE__` sentinel. `ShowLOVAsync` now normalizes that sentinel to the requested field name before applying values.

## Notes for callers

- Prefer FormsManager master/detail APIs over the deprecated standalone `IRelationshipManager` abstraction.
- Prefer `ShowLOVAsync` over applying `LOVManager.GetRelatedFieldValues(...)` directly so the return-field mapping logic stays centralized.
- `PagingManager` tracks page state only. Callers still own total-count population and datasource query execution.
- UI layers own rendering, focus, and keyboard plumbing; FormsManager provides a UI-agnostic runtime surface.

## Stale / superseded documents in this folder

- **`EXECUTIVE_SUMMARY.md`** — written against an earlier state of the engine. Mentions features (e.g. "Update operations rely on reflection", "No LOV implementation exists") that are no longer true. **Do not use as a current reference.** The current Oracle Forms coverage is captured in [`ORACLE-FORMS-MAPPING.md`](ORACLE-FORMS-MAPPING.md).
- **`MIGRATION-GUIDE.md`** — older legacy migration notes. Kept for historical context.
- **`FormsManager.original.cs.bak`** — backup of the pre-partial-class file. Safe to delete; everything has been moved to typed partials.
