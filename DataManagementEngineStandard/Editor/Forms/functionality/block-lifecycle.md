# FormsManager — Block Lifecycle and Registration

This document covers the registration and lifecycle of data blocks in `FormsManager`. A "block" in the Oracle Forms sense is a logical unit of data on the form — a single table/query result that the user can navigate, query, and update. In `FormsManager`, a block has metadata (`DataBlockInfo`), a `IUnitofWork` for persistence, and an optional set of triggers/validations/LOVs/security that apply to it.

## Block registration

### The three registration entry points

```csharp
// 1. Caller already has the IUnitofWork
manager.RegisterBlock(
    blockName: "CUSTOMERS",
    unitOfWork: customerUow,
    dataSourceName: "Northwind",
    isMasterBlock: true);

// 2. Caller has the IUnitofWork + EntityStructure
manager.RegisterBlock(
    blockName: "CUSTOMERS",
    unitOfWork: customerUow,
    entityStructure: customerEntity,
    dataSourceName: "Northwind",
    isMasterBlock: true);

// 3. Caller has only the connection + entity names
//    Engine opens the connection, fetches EntityStructure, creates IUnitofWork, registers.
await manager.SetupBlockAsync(
    blockName: "CUSTOMERS",
    connectionName: "Northwind",
    entityName: "Customers",
    isMasterBlock: true);
```

FormsManager 3.0+ adds a new `SetupBlockAsync` overload that:
- Opens the named datasource if needed.
- Fetches `IEntityStructure` from the schema.
- Creates a default `IUnitofWork`.
- Registers the block.

This is the **single-call bootstrap entry point** for UI layers (BeepForms, BeepBlock) that must never touch `IDataSource` directly. It delegates to `RegisterBlockFromSourceAsync` internally.

### The `IUnitofWork` requirement

Every block must have a non-null `IUnitofWork` at registration time. The UoW owns:
- The current record index.
- The loaded record set.
- The dirty-state tracking.
- The actual SQL execution for query/insert/update/delete.

`FormsManager` is the orchestrator; `IUnitofWork` is the persistence layer. The two communicate through events (`ItemChanged`, `CurrentChanged`, `OnUnsavedChanges`) that `FormsManager.Core.cs` subscribes to during `RegisterBlock`.

### The `DataBlockInfo` envelope

After registration, the block is stored as a `DataBlockInfo` in the engine's internal `ConcurrentDictionary<string, DataBlockInfo>`. `DataBlockInfo` carries:
- The block name.
- The `IUnitofWork`.
- The `IEntityStructure`.
- The `DataSourceName`.
- The `IsMasterBlock` flag.
- Trigger / validation / property metadata (lazy).

`GetBlock(name)` returns the `DataBlockInfo`. `GetUnitOfWork(name)` returns just the UoW.

### ConcurrentDictionary + thread safety

The block registry is a `ConcurrentDictionary`. Reads are lock-free; writes (registration, unregistration) are atomic. The orchestrator is **thread-safe for registration and lookup**, but per-block operations (navigation, validation) happen on the per-block UoW and are not synchronized at the manager level.

## Block unregistration

`UnregisterBlock(name)` does this, in order:

1. Fires `OnBlockLeave` (the WHEN-LEAVE-BLOCK trigger).
2. Removes the block from all master/detail relationships.
3. Unsubscribes from UoW events.
4. Removes the block from the registry.
5. Clears the per-block navigation history.
6. Returns `true` on success.

There is no "soft unregister" — `UnregisterBlock` is a hard remove. The block can be re-registered with a different UoW if needed.

## Block properties (`BlockProperty` enum)

The `SET_BLOCK_PROPERTY` / `GET_BLOCK_PROPERTY` family. See [`architecture.md`](../architecture.md) for the orchestrator wiring; this section lists what's settable.

The `BlockProperty` enum lives in `Models/BlockPropertyEnum.cs`. The most-used values:

| Property | Type | What it controls |
| --- | --- | --- |
| `INSERT_ALLOWED` | bool | `INSERT_RECORD` is permitted. |
| `UPDATE_ALLOWED` | bool | `UPDATE_RECORD` is permitted. |
| `DELETE_ALLOWED` | bool | `DELETE_RECORD` is permitted. |
| `QUERY_ALLOWED` | bool | `EXECUTE_QUERY` is permitted. |
| `DEFAULT_WHERE` | string | The default filter applied to every `EXECUTE_QUERY`. |
| `ORDER_BY` | string | The default order applied to every `EXECUTE_QUERY`. |
| `NAVIGABLE` | bool | Whether `GO_BLOCK` is permitted. |
| `RECORD_VISUAL_ATTRIBUTE` | string | Visual attribute for the current record (UI-specific). |
| `CURRENT_RECORD_VISUAL_ATTRIBUTE` | string | Visual attribute for the current record (alias). |

Per-property shortcuts are exposed for the most common ones:

```csharp
manager.SetInsertAllowed("ORDERS", true);
manager.SetUpdateAllowed("ORDERS", true);
manager.SetDeleteAllowed("ORDERS", false);
manager.SetQueryAllowed("ORDERS", true);
manager.SetDefaultWhere("ORDERS", "IsDeleted = 0");
manager.SetOrderBy("ORDERS", "OrderDate desc");
```

## Block savepoints

`CreateBlockSavepoint(name, optionalLabel)` captures the current record by reflection (IDictionary → typed dictionary → public properties fallback). Returns the savepoint name (or auto-generated name if `optionalLabel` is null).

`RollbackToSavepointAsync(name, optionalLabel, ct)` restores the snapshot. The rollback:
1. Looks up the savepoint in `SavepointManager`.
2. Calls the UoW's `Rollback()`.
3. Moves the UoW to the saved record index (if it's still valid).
4. Restores the current record's field values from the snapshot.
5. Updates the system variables.

`RollbackToSavepoint` is best-effort: a property may not be writable (projected/read-only), in which case it's silently skipped. This matches the Oracle Forms semantics where `KEY-EXIT` doesn't always restore the full record state — it restores what it can.

## Block state

A registered block has:
- A `CurrentRecord` (the UoW's `CurrentItem`).
- A `CurrentRecordIndex` (the UoW's current index).
- A `TotalRecordCount` (the UoW's `TotalItemCount`, after query).
- A `Mode` (`DataBlockMode` enum: `Query`, `EnterQuery`, `Crud`, `New`).
- A `Status` (`CHANGED`, `NEW`, `QUERY` — the same names as Oracle's `:SYSTEM.BLOCK_STATUS`).
- A `IsDirty` flag (set if any record has unsaved changes).

`FormsManager.GetCurrentRecordInfo(blockName)` returns the navigation metadata. `GetBlockMode` returns the mode.

## Lifecycle of a block

1. **Created** — `RegisterBlock` or `SetupBlockAsync`. Fires `OnBlockEnter`.
2. **Queried** — `ExecuteQueryAsync` runs the datasource query, populates the UoW, sets `Mode = Crud`. Fires `OnPreQuery` / `OnPostQuery`.
3. **Navigated** — `NextRecordAsync`, etc. move the current record. Fires `OnNavigate` / `OnCurrentChanged`.
4. **Modified** — field changes are tracked by the UoW; `OnBlockFieldChanged` fires per change. `OnValidateField` fires if validation is configured.
5. **Committed** — `CommitFormAsync` validates, then commits. Fires `OnPreCommit` / `OnPostCommit` on each block.
6. **Rolled back** — `RollbackFormAsync` reverts. Fires `OnPreRollback` / `OnPostRollback`.
7. **Cleared** — `ClearBlockAsync` empties the block. Fires `OnBlockClear`.
8. **Unregistered** — `UnregisterBlock` removes the block. Fires `OnBlockLeave`.

## Block registration failure modes

| Failure | What happens |
| --- | --- |
| `blockName` is null/empty | `ArgumentException` (sync) or returns `false` (async). |
| `unitOfWork` is null | `ArgumentException` from `ValidateBlockRegistrationParameters`. |
| `entityStructure` is null and UoW has none | `ArgumentNullException` (sync) or returns `false` (async). |
| Block already exists with a different UoW | The existing block is replaced. The old UoW's events are unsubscribed. |
| `RegisterBlockFromSourceAsync` cannot resolve (connection name, entity name) | Returns `false`. Logs to `Status` and `_eventManager.TriggerError`. |

## Internal state that the orchestrator owns vs. delegates

| State | Owned by | Exposed via |
| --- | --- | --- |
| Block list | `FormsManager` (ConcurrentDictionary) | `manager.Blocks`, `manager.BlockCount` |
| Current form name | `FormsManager` (`_currentFormName`) | `manager.CurrentFormName` |
| Current block name | `FormsManager` (`_currentBlockName`) | `manager.CurrentBlockName` |
| Current record per block | `IUnitofWork` | `manager.GetCurrentRecord(blockName)` |
| Dirty state per block | `IDirtyStateManager` (helper) | `manager.IsDirty`, `manager.DirtyStateManager` |
| Block mode | `IUnitofWork` (via DataBlockMode field) | `manager.GetBlockMode(blockName)` |
| Per-block event subscriptions | `FormsManager` (concurrent dicts of handlers) | — |
| Navigation history | `NavigationHistoryManager` (helper) | `manager.GetNavigationHistory(blockName)` |

## Notes for callers

- Register blocks before opening a form that uses them. `OpenFormAsync` does not auto-register.
- Use `SetupBlockAsync` for the "I have connection + entity name, do the rest" path. It is the recommended entry point for UI layers.
- The block registry survives form open/close. `CloseFormAsync` does NOT unregister blocks. If you want to clear all blocks, call `ClearAllBlocksAsync` (clears data) or iterate `UnregisterBlock` (removes the block entirely).
- Concurrent registration of the same block name is not safe. Two callers racing on `RegisterBlock("ORDERS", ...)` will leave the registry in an indeterminate state. The first wins; the second replaces the first's events.
- Concurrent registration of *different* blocks is safe.

## See also

- [`architecture.md`](../architecture.md) — the 4-layer model.
- [`navigation.md`](navigation.md) — record navigation within a block.
- [`mode-transitions.md`](mode-transitions.md) — query/CRUD mode transitions.
- [`master-detail.md`](master-detail.md) — multi-block relationships.
