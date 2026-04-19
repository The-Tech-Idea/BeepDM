# FormsManager

FormsManager is the BeepDM form-orchestration runtime in the `TheTechIdea.Beep.Editor.UOWManager` namespace. It is the current concrete implementation behind `IUnitofWorksManager` and coordinates block registration, navigation, mode transitions, master/detail synchronization, triggers, LOVs, validation, auditing, security, paging, and multi-form communication.

This README replaces the older `UnitofWorksManager` naming found in earlier notes. The public runtime surface is `FormsManager`; the compatibility interface is still named `IUnitofWorksManager`.

## Related documentation

- [MIGRATION-GUIDE.md](MIGRATION-GUIDE.md)
- [ORACLE-FORMS-MAPPING.md](ORACLE-FORMS-MAPPING.md)
- [Helpers/README.md](Helpers/README.md)
- [Interfaces/README.md](Interfaces/README.md)
- [Models/README.md](Models/README.md)

## What FormsManager owns

- Block registration, current-block state, and form-level orchestration.
- Master/detail registration and detail synchronization.
- Query-mode and CRUD-mode transitions.
- Navigation history, savepoints, locks, and cross-block validation.
- Oracle Forms-style built-ins such as alerts, timers, sequences, and block properties.
- Multi-form registry/message/shared-block plumbing.
- Integration of helper managers for validation, LOV, triggers, audit, security, paging, and caching.
- Runtime trigger metadata and block UoW activity that host UIs may observe only through proxy layers such as `BeepForms` / `IBeepFormsHost`.

## What stays in IUnitofWork

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

## Architecture

### Partial-class surface

| File | Responsibility |
| --- | --- |
| `FormsManager.cs` | Core registration, properties, DI wiring, master/detail ownership |
| `FormsManager.FormOperations.cs` | Open, close, commit, rollback, clear-form behavior |
| `FormsManager.Navigation.cs` | Record navigation, block switching, history integration |
| `FormsManager.ModeTransitions.cs` | `ENTER_QUERY`, `EXECUTE_QUERY`, CRUD transition rules |
| `FormsManager.EnhancedOperations.cs` | Insert, update, delete, duplicate, audit defaults |
| `FormsManager.DataOperations.cs` | Undo/redo, batch commit, export/import, aggregates, state persistence |
| `FormsManager.GenericOperations.cs` | Typed registration and `ShowLOVAsync` |
| `FormsManager.BlockProperties.cs` | `SET_BLOCK_PROPERTY` / `GET_BLOCK_PROPERTY` equivalents |
| `FormsManager.Alerts.cs` | `MESSAGE`, `SHOW_ALERT`, confirmation helpers |
| `FormsManager.Sequences.cs` | Sequence, default-value, copy-field helpers |
| `FormsManager.Timers.cs` | `CREATE_TIMER`, `DELETE_TIMER`, `GET_TIMER` |
| `FormsManager.MultiFormNavigation.cs` | `CALL_FORM`, modeless `OPEN_FORM`, `NEW_FORM`, return-to-caller |
| `FormsManager.InterFormComm.cs` | `:GLOBAL.*`, message bus, shared blocks |
| `FormsManager.Security.cs` | Security context, block security, field security, masking |
| `FormsManager.Audit.cs` | Audit capture, export, purge, underlying manager access |
| `FormsManager.Performance.cs` | Paging, lazy loading, cache invalidation, fetch-ahead |
| `FormsManager.KeyTriggers.cs` | KEY-* trigger wrappers and default keyboard actions |
| `FormsManager.TriggerChaining.cs` | Trigger execution graph and dependency logging |

### Helper-manager surface

FormsManager exposes helper managers as properties so callers can opt into lower-level behavior without reimplementing orchestration.

- `DirtyStateManager`
- `SystemVariables`
- `Validation`
- `LOV`
- `ItemProperties`
- `Triggers`
- `Savepoints`
- `Locking`
- `QueryBuilder`
- `ErrorLog`
- `Messages`
- `BlockFactory`
- `BlockProperties`
- `AlertProvider`
- `Sequences`
- `Timers`
- `Registry`
- `MessageBus`
- `SharedBlocks`
- `AuditManager`
- `Security`
- `Paging`

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

## Oracle Forms coverage snapshot

| Oracle Forms concept | FormsManager API | Notes |
| --- | --- | --- |
| `ENTER_QUERY` | `EnterQueryModeAsync` | Moves a block into query mode with unsaved-change checks |
| `EXECUTE_QUERY` | `ExecuteQueryAndEnterCrudModeAsync`, `ExecuteQueryAsync` | Query execution returns to CRUD mode when appropriate |
| `COMMIT_FORM` | `CommitFormAsync` | Validates, saves dirty blocks, and coordinates locks/audit |
| `ROLLBACK_FORM` | `RollbackFormAsync` | Rolls back dirty blocks and clears transient state |
| `CLEAR_FORM` | `ClearAllBlocksAsync` | Clears all registered blocks |
| `GO_BLOCK` | `SwitchToBlockAsync` | Current block selection stays in FormsManager |
| `GO_RECORD` | `NavigateToRecordAsync` | Also backed by history helpers |
| `SHOW_LOV` | `ShowLOVAsync` plus `LOV.RegisterLOV` | UI rendering stays outside FormsManager |
| `SET_BLOCK_PROPERTY` | `SetBlockProperty`, `SetDefaultWhere`, `SetOrderBy` | Block metadata is stored on `DataBlockInfo` |
| `MESSAGE` / `SHOW_ALERT` | `SetMessage`, `ShowAlertAsync`, `ConfirmAsync` | UI provider is injected through `IAlertProvider` |
| `:SEQUENCE.NEXTVAL` | `GetNextSequence` | Use datasource-backed sequences first when available |
| `CREATE_TIMER` | `CreateTimer` | Expiry raises `WHEN-TIMER-EXPIRED` |
| `CALL_FORM` / `OPEN_FORM` / `NEW_FORM` | `CallFormAsync`, `OpenFormAsync`, `NewFormAsync` | Registry-backed, UI-agnostic multi-form behavior |
| `:GLOBAL.*` | `SetGlobalVariable`, `GetGlobalVariable` | Shared global state across forms |

For a fuller mapping, see [ORACLE-FORMS-MAPPING.md](ORACLE-FORMS-MAPPING.md).

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