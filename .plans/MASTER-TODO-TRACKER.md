# MASTER TODO TRACKER
# OBL / UOW / FormsManager Enhancement Plan

**Goal:** Systematically enhance the three core layers — `ObservableBindingList<T>`,
`UnitofWork<T>`, and `FormsManager` — to unlock richer data-management capabilities
with clean API delegation all the way from the UI (`BeepDataBlock`) down to the database.

**Repos:**
- OBL: `c:\...\BeepDM\DataManagementModelsStandard\ObservableBindingList\`
- UOW: `c:\...\BeepDM\DataManagementEngineStandard\Editor\UOW\`
- FormsManager: `c:\...\BeepDM\DataManagementEngineStandard\Editor\Forms\`

---

## Phase Overview

| # | Phase | Document | Status |
|---|---|---|---|
| 1 | OBL Core Enhancements | [PHASE-01](PHASE-01-OBL-Enhancements.md) | [x] |
| 2 | UOW Enhancements | [PHASE-02](PHASE-02-UOW-Enhancements.md) | [x] |
| 3 | UOW ↔ FormsManager Bridge | [PHASE-03](PHASE-03-UOW-FormsManager-Bridge.md) | [x] |
| 4 | FormsManager Advanced Operations | [PHASE-04](PHASE-04-FormsManager-Advanced.md) | [x] |

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
