# Phase 2 — UnitofWork Enhancements

**Goal:** Surface missing data-management features on `UnitofWork<T>` that FormsManager and
BeepDataBlock delegates call today but the UOW doesn't yet provide: refresh/reload, single-item
revert, change summary, batch-commit with progress, query history, export/import helpers,
async find, item cloning, aggregate shortcuts, and undo/redo integration.

**Pre-condition:** Phase 1 complete (OBL exposes `GetChangedFields`, `LoadBatchAsync`, `MergeAsync`,
`GetInserted/Updated/Deleted` etc.).

**Files touched:** `DataManagementEngineStandard/Editor/UOW/`

---

## 2-A  `ChangeSummary` POCO + `GetChangeSummary()`

**New model file:** `UOW/Models/ChangeSummary.cs`  
**Add to:** `UnitofWork.Core.cs` + interface `IUnitofWork<T>`

### Model

```csharp
public class ChangeSummary
{
    public int InsertCount  { get; set; }
    public int UpdateCount  { get; set; }
    public int DeleteCount  { get; set; }
    public bool IsDirty     => InsertCount > 0 || UpdateCount > 0 || DeleteCount > 0;
    public int TotalChanges => InsertCount + UpdateCount + DeleteCount;
}
```

### API

```csharp
// Delegates to OBL.GetChangeSetSummary() (Phase 1-B)
ChangeSummary GetChangeSummary();

// Delegate list accessors through to OBL
IReadOnlyList<T> GetInsertedItems();
IReadOnlyList<T> GetUpdatedItems();
IReadOnlyList<T> GetDeletedItems();
```

---

## 2-B  `RefreshAsync` — Reload from Data Source

**Add to:** `UnitofWork.CRUD.cs`

Reloads all data for the current entity from `DataSource` and merges using OBL's
`MergeAsync(serverItems, ConflictMode.ServerWins)`.

### API

```csharp
Task<bool> RefreshAsync(
    List<AppFilter> filters = null,
    ConflictMode conflictMode = ConflictMode.ServerWins,
    CancellationToken ct = default);
```

### Implementation notes

- Calls `DataSource.GetEntity(EntityName, filters)`.
- Calls `Units.MergeAsync(serverItems, conflictMode, PrimaryKey, ct)`.
- Fires existing `PreQuery` / `PostQuery` events.
- Returns `true` on success; logs error and returns `false` on failure.

---

## 2-C  `RevertItemAsync` — Single-Item Revert

**Add to:** `UnitofWork.Core.Extensions.cs`

Restores a single entity to its original field values using OBL's `OriginalFieldValues`
(Phase 1-A).  Useful for "cancel edits on current row" without rolling back the whole UOW.

### API

```csharp
bool RevertItem(T item);                       // synchronous — just reflection writes

Task<bool> RevertItemAsync(                    // async version (for future remote validation)
    T item,
    CancellationToken ct = default);
```

### Implementation notes

- Call `Units.GetChangedFields(item)` to enumerate dirty fields.
- For each field, call `Units.GetOriginalValue(item, field)` → set via reflection.
- After restore, call `Units.GetTrackingItem(item).EntityState = Unchanged`.
- Fire `PostDelete` style event `OnItemReverted` (new event, optional subscriber).

---

## 2-D  Batch Commit

**Add to:** `UnitofWork.CRUD.cs`

Current `Commit()` sends all changes in one shot.  Large batches can time out.

### API

```csharp
Task<CommitBatchResult> CommitBatchAsync(
    int batchSize = 200,
    IProgress<CommitBatchProgress> progress = null,
    CancellationToken ct = default);

public class CommitBatchProgress
{
    public int TotalBatches   { get; set; }
    public int CurrentBatch   { get; set; }
    public int RecordsCommitted { get; set; }
    public string CurrentOperation { get; set; }  // "Insert" | "Update" | "Delete"
}

public class CommitBatchResult
{
    public bool Success     { get; set; }
    public int TotalCommitted { get; set; }
    public List<string> Errors { get; set; } = new();
}
```

### Implementation notes

- Chunk `GetInsertedItems()`, `GetUpdatedItems()`, `GetDeletedItems()` into batches of `batchSize`.
- Process in `CommitOrder` sequence (Deletes, Updates, Inserts or configurable).
- Call `DataSource.InsertIntoTableASync` / `UpdateTableASync` / `DeleteEntityAsync` per item.
- Report progress after each batch.
- If any batch fails: continue remaining batches (accumulate errors), return `Success=false`.

---

## 2-E  Query History

**New helper file:** `UOW/Helpers/UnitofWorkQueryHistory.cs`  
**Add to:** `UnitofWork.CRUD.cs` (record on each `Get()` call)

### Model

```csharp
public class QueryHistoryEntry
{
    public DateTime ExecutedAt   { get; set; }
    public List<AppFilter> Filters { get; set; }
    public int RowCount          { get; set; }
    public TimeSpan Duration     { get; set; }
    public bool Succeeded        { get; set; }
}
```

### API on UOW

```csharp
IReadOnlyList<QueryHistoryEntry> QueryHistory { get; }   // last 20 entries FIFO
void ClearQueryHistory();
int MaxQueryHistorySize { get; set; }   // default 20
```

### Implementation notes

- Add `List<QueryHistoryEntry> _queryHistory` field.
- In `Get()` and `ExecuteQueryAsync` wrap the data-source call with a `Stopwatch`.
- Push entry to `_queryHistory`; trim if `> MaxQueryHistorySize`.

---

## 2-F  Data Export Helpers

**New file:** `UOW/Helpers/UnitofWorkExportHelper.cs`

These cover the most common export needs; heavy formats (XLSX) remain plugin territory.

### API

```csharp
// Returns a DataTable for binding to WinForms grids that can't use OBL directly
DataTable ToDataTable();

// Writes JSON array to any stream
Task ToJsonAsync(Stream stream, CancellationToken ct = default);

// Writes CSV with a header row; uses EntityStructure field order when available
Task ToCsvAsync(
    Stream stream,
    char delimiter = ',',
    CancellationToken ct = default);
```

### Implementation notes

- `ToDataTable()`: iterate `EntityStructure.Fields` for column definitions; iterate `Units` for rows.
- `ToJsonAsync`: `System.Text.Json.JsonSerializer.SerializeAsync`.
- `ToCsvAsync`: write header from property names; rows from reflection (cached via `GetCachedProperty`).

---

## 2-G  Data Import Helpers

**Add to:** `UOW/Helpers/UnitofWorkExportHelper.cs`

### API

```csharp
// Deserialize JSON array → LoadBatchAsync into Units (Phase 1-C)
Task<int> LoadFromJsonAsync(
    Stream stream,
    bool clearFirst = true,
    CancellationToken ct = default);

// Parse CSV → LoadBatchAsync into Units
Task<int> LoadFromCsvAsync(
    Stream stream,
    char delimiter = ',',
    bool clearFirst = true,
    bool hasHeaderRow = true,
    CancellationToken ct = default);
```

### Implementation notes

- `LoadFromJsonAsync`: `JsonSerializer.DeserializeAsync<List<T>>`.
- `LoadFromCsvAsync`: read line-by-line, map header columns to properties via reflection cache.
- Both call `Units.LoadBatchAsync(items, progress: null, ct)` (Phase 1-C).
- Return the count of records loaded; log errors per-row without aborting the whole import.

---

## 2-H  `FindAsync` + `CloneItem`

**Add to:** `UnitofWork.Core.Extensions.cs`

Small but frequently-needed helpers.

### API

```csharp
Task<T> FindAsync(Func<T, bool> predicate, CancellationToken ct = default);
Task<List<T>> FindManyAsync(Func<T, bool> predicate, CancellationToken ct = default);

// Shallow copy by default; pass deepCopy:true to clone nested objects via JSON round-trip
T CloneItem(T item, bool deepCopy = false);
```

---

## 2-I  Aggregate Shortcuts

**Add to:** `UnitofWork.Core.Extensions.cs`

Delegates to `Units` (OBL) aggregate support; avoids UI layer pulling `UOW.Units` directly.

### API

```csharp
decimal Sum(string  numericFieldName);
decimal Average(string numericFieldName);
TField  Min<TField>(string fieldName) where TField : IComparable;
TField  Max<TField>(string fieldName) where TField : IComparable;
int     Count(Func<T, bool> predicate = null);
```

### Implementation notes  

- Use cached `GetCachedProperty` + LINQ; run synchronously (in-memory).

---

## 2-J  `UndoLastAction` / `RedoLastAction`

**Add to:** `UnitofWork.Core.Extensions.cs`

Surfaces OBL's built-in undo engine (already exists, just not exposed on IUnitofWork).

### API

```csharp
bool UndoLastAction();   // delegates to Units.Undo()
bool RedoLastAction();   // delegates to Units.Redo()
bool CanUndo { get; }    // delegates to Units.CanUndo
bool CanRedo { get; }    // delegates to Units.CanRedo
void EnableUndo(bool enable, int maxDepth = 50);
```

---

## Interface Updates

**`IUnitofWork<T>`** (`DataManagementModelsStandard` — wherever this lives) must gain:

```csharp
ChangeSummary            GetChangeSummary();
IReadOnlyList<T>         GetInsertedItems();
IReadOnlyList<T>         GetUpdatedItems();
IReadOnlyList<T>         GetDeletedItems();
Task<bool>               RefreshAsync(List<AppFilter> filters, ConflictMode conflictMode, CancellationToken ct);
bool                     RevertItem(T item);
Task<bool>               RevertItemAsync(T item, CancellationToken ct);
Task<CommitBatchResult>  CommitBatchAsync(int batchSize, IProgress<CommitBatchProgress> progress, CancellationToken ct);
IReadOnlyList<QueryHistoryEntry> QueryHistory { get; }
DataTable                ToDataTable();
Task                     ToJsonAsync(Stream stream, CancellationToken ct);
Task                     ToCsvAsync(Stream stream, char delimiter, CancellationToken ct);
Task<int>                LoadFromJsonAsync(Stream stream, bool clearFirst, CancellationToken ct);
Task<int>                LoadFromCsvAsync(Stream stream, char delimiter, bool clearFirst, bool hasHeaderRow, CancellationToken ct);
Task<T>                  FindAsync(Func<T, bool> predicate, CancellationToken ct);
T                        CloneItem(T item, bool deepCopy);
bool                     CanUndo { get; }
bool                     CanRedo { get; }
bool                     UndoLastAction();
bool                     RedoLastAction();
void                     EnableUndo(bool enable, int maxDepth);
```

Non-generic `IUnitofWork` must gain `GetChangeSummary()`, `RefreshAsync()`, `CommitBatchAsync()`,
`CanUndo/CanRedo/UndoLastAction/RedoLastAction` (without type parameters).

---

## Checklist

| # | Task | File | Status |
|---|---|---|---|
| 2-A.1 | Add `ChangeSummary` POCO | `UOW/Models/ChangeSummary.cs` (new) | [ ] |
| 2-A.2 | Implement `GetChangeSummary/InsertedItems/UpdatedItems/DeletedItems` on UOW | `UnitofWork.Core.cs` | [ ] |
| 2-A.3 | Add to `IUnitofWork<T>` | `IUnitofWork interfaces file` | [ ] |
| 2-B.1 | Implement `RefreshAsync` | `UnitofWork.CRUD.cs` | [ ] |
| 2-B.2 | Add to `IUnitofWork<T>` | `IUnitofWork interfaces file` | [ ] |
| 2-C.1 | Implement `RevertItem` + `RevertItemAsync` | `UnitofWork.Core.Extensions.cs` | [ ] |
| 2-C.2 | Add `OnItemReverted` event | `UnitofWork.Core.cs` | [ ] |
| 2-D.1 | Add `CommitBatchProgress` + `CommitBatchResult` POCOs | `UOW/Models/ChangeSummary.cs` | [ ] |
| 2-D.2 | Implement `CommitBatchAsync` | `UnitofWork.CRUD.cs` | [ ] |
| 2-E.1 | Add `QueryHistoryEntry` POCO | `UOW/Models/` new file | [ ] |
| 2-E.2 | Create `UnitofWorkQueryHistory.cs` helper | `UOW/Helpers/` | [ ] |
| 2-E.3 | Hook `_queryHistory` push into `Get()` + `ExecuteQueryAsync` | `UnitofWork.CRUD.cs` | [ ] |
| 2-F.1 | Implement `ToDataTable/ToJsonAsync/ToCsvAsync` | `UOW/Helpers/UnitofWorkExportHelper.cs` (new) | [ ] |
| 2-G.1 | Implement `LoadFromJsonAsync/LoadFromCsvAsync` | `UOW/Helpers/UnitofWorkExportHelper.cs` | [ ] |
| 2-H.1 | Implement `FindAsync/FindManyAsync/CloneItem` | `UnitofWork.Core.Extensions.cs` | [ ] |
| 2-I.1 | Implement `Sum/Average/Min/Max/Count` | `UnitofWork.Core.Extensions.cs` | [ ] |
| 2-J.1 | Implement `UndoLastAction/RedoLastAction/CanUndo/CanRedo/EnableUndo` | `UnitofWork.Core.Extensions.cs` | [ ] |
| 2-K.1 | Update all `IUnitofWork<T>` + non-generic `IUnitofWork` declarations | interfaces file | [ ] |
