# Phase 3 — UOW ↔ FormsManager Integration Bridge

**Goal:** Surface the new Phase-2 UOW capabilities through the FormsManager API layer so that
`BeepDataBlock` (and any other UI) can call them without touching `IUnitofWork<T>` directly.
Also adds the interface markers that let helpers discover whether a UOW supports the new features.

**Pre-condition:** Phase 1 + Phase 2 complete.

**Files touched:**
- `DataManagementEngineStandard/Editor/Forms/Interfaces/IUnitofWorksManagerInterfaces.cs`
- `DataManagementEngineStandard/Editor/Forms/FormsManager.cs` (and partials)

---

## 3-A  Capability-Marker Interfaces on `IUnitofWork`

Add lightweight marker interfaces so FormsManager and helpers can feature-detect at runtime
without casting to the generic `IUnitofWork<T>`.

**Target:** wherever `IUnitofWork` (non-generic) is defined.

```csharp
/// <summary>Marks a UOW that supports single-item revert.</summary>
public interface IRevertable
{
    bool RevertItem(object item);
}

/// <summary>Marks a UOW that supports batch commit with progress.</summary>
public interface IBatchCommittable
{
    Task<CommitBatchResult> CommitBatchAsync(
        int batchSize = 200,
        IProgress<CommitBatchProgress> progress = null,
        CancellationToken ct = default);
}

/// <summary>Marks a UOW that supports data export.</summary>
public interface IExportable
{
    DataTable ToDataTable();
    Task ToJsonAsync(Stream stream, CancellationToken ct = default);
    Task ToCsvAsync(Stream stream, char delimiter = ',', CancellationToken ct = default);
}

/// <summary>Marks a UOW that supports data import.</summary>
public interface IImportable
{
    Task<int> LoadFromJsonAsync(Stream stream, bool clearFirst = true, CancellationToken ct = default);
    Task<int> LoadFromCsvAsync(Stream stream, char delimiter = ',', bool clearFirst = true,
                               bool hasHeaderRow = true, CancellationToken ct = default);
}

/// <summary>Marks a UOW that exposes aggregate calculations.</summary>
public interface IAggregatable
{
    decimal Sum(string numericFieldName);
    decimal Average(string numericFieldName);
    int     Count(Func<object, bool> predicate = null);
}

/// <summary>Marks a UOW that supports undo/redo.</summary>
public interface IUndoable
{
    bool CanUndo { get; }
    bool CanRedo { get; }
    bool UndoLastAction();
    bool RedoLastAction();
    void EnableUndo(bool enable, int maxDepth = 50);
}

/// <summary>Marks a UOW with server-merge capability.</summary>
public interface IMergeable
{
    Task<bool> RefreshAsync(
        List<AppFilter> filters = null,
        ConflictMode conflictMode = ConflictMode.ServerWins,
        CancellationToken ct = default);
}
```

`UnitofWork<T>` will implement all of these via the additions in Phase 2.

---

## 3-B  `IUnitofWorksManager` Additions

Add the following regions to `IUnitofWorksManager`.

### 3-B-1  Undo/Redo per Block

```csharp
#region Undo / Redo

/// <summary>Enable or disable undo tracking for a block.</summary>
void SetBlockUndoEnabled(string blockName, bool enable, int maxDepth = 50);

/// <summary>Undo the last action in a block's OBL undo stack.</summary>
bool UndoBlock(string blockName);

/// <summary>Redo the last undone action in a block's OBL undo stack.</summary>
bool RedoBlock(string blockName);

bool CanUndoBlock(string blockName);
bool CanRedoBlock(string blockName);

#endregion
```

### 3-B-2  Change Summary per Block / per Form

```csharp
#region Change Summaries

ChangeSummary GetBlockChangeSummary(string blockName);

/// <summary>Returns one ChangeSummary per registered block.</summary>
IReadOnlyDictionary<string, ChangeSummary> GetFormChangeSummary();

#endregion
```

### 3-B-3  Block-Level Data Operations

```csharp
#region Block Data Operations

/// <summary>
/// Reload block data from the data source, merging with current edits.
/// </summary>
Task<bool> RefreshBlockAsync(
    string blockName,
    List<AppFilter> filters = null,
    ConflictMode conflictMode = ConflictMode.ServerWins,
    CancellationToken ct = default);

/// <summary>Revert the current record to its original field values.</summary>
bool RevertCurrentRecord(string blockName);

/// <summary>Revert the record at a specific index.</summary>
bool RevertRecord(string blockName, int recordIndex);

#endregion
```

### 3-B-4  Query History per Block

```csharp
#region Query History

IReadOnlyList<QueryHistoryEntry> GetBlockQueryHistory(string blockName);
void ClearBlockQueryHistory(string blockName);

#endregion
```

### 3-B-5  Aggregates per Block

```csharp
#region Block Aggregates

/// <summary>Sum a numeric field across all loaded records in the block.</summary>
decimal GetBlockSum(string blockName, string fieldName);

/// <summary>Average of a numeric field.</summary>
decimal GetBlockAverage(string blockName, string fieldName);

/// <summary>Count of records matching optional predicate.</summary>
int GetBlockCount(string blockName, Func<object, bool> predicate = null);

#endregion
```

### 3-B-6  Batch Commit

```csharp
#region Batch Commit

/// <summary>Commit all dirty blocks in batches across the whole form.</summary>
Task<CommitBatchResult> CommitFormBatchAsync(
    int batchSize = 200,
    IProgress<CommitBatchProgress> progress = null,
    CancellationToken ct = default);

/// <summary>Commit a single block in batches.</summary>
Task<CommitBatchResult> CommitBlockBatchAsync(
    string blockName,
    int batchSize = 200,
    IProgress<CommitBatchProgress> progress = null,
    CancellationToken ct = default);

#endregion
```

### 3-B-7  Export / Import per Block

```csharp
#region Block Export / Import

Task ExportBlockToJsonAsync(string blockName, Stream stream, CancellationToken ct = default);
Task ExportBlockToCsvAsync(string blockName, Stream stream, char delimiter = ',', CancellationToken ct = default);
DataTable GetBlockAsDataTable(string blockName);

Task<int> ImportBlockFromJsonAsync(string blockName, Stream stream,
    bool clearFirst = true, CancellationToken ct = default);
Task<int> ImportBlockFromCsvAsync(string blockName, Stream stream,
    char delimiter = ',', bool clearFirst = true, bool hasHeaderRow = true,
    CancellationToken ct = default);

#endregion
```

### 3-B-8  Grouping per Block

```csharp
#region Block Grouping

/// <summary>
/// Returns grouped view of the block's current data set.
/// TKey must be the type of the grouping field value.
/// </summary>
IReadOnlyList<ItemGroup<object>> GetBlockGroups(string blockName, string fieldName);

#endregion
```

---

## 3-C  FormsManager Implementation

These new `IUnitofWorksManager` methods are thin facades that:

1. look up `GetBlock(blockName)` → `GetUnitOfWork(blockName)`.
2. feature-detect the marker interface (e.g. `uow as IUndoable`).
3. delegate to the UOW method.
4. return a sensible default when the UOW doesn't implement the interface.

**New partial file:** `FormsManager.DataOperations.cs`

All 3-B methods go here.  Pattern:

```csharp
public bool UndoBlock(string blockName)
{
    var uow = GetUnitOfWork(blockName);
    return (uow as IUndoable)?.UndoLastAction() ?? false;
}

public ChangeSummary GetBlockChangeSummary(string blockName)
{
    var uow = GetUnitOfWork(blockName);
    if (uow is UnitOfWorkWrapper wrapper)
        return /* delegate to wrapper */ ...;
    // direct cast path for typed UOW
    return new ChangeSummary();
}
```

Since `UnitOfWorkWrapper` wraps a dynamic object, export/import/undo need to be piped through
the wrapper.  Update `UnitOfWorkWrapper` / `UnitOfWorkWrapperExtensions.cs` to forward
any new methods where the underlying UOW supports them.

---

## Checklist

| # | Task | File | Status |
|---|---|---|---|
| 3-A.1 | Define `IRevertable`, `IBatchCommittable`, `IExportable`, `IImportable`, `IAggregatable`, `IUndoable`, `IMergeable` | `IUnitofWork<T>` location or new `IUnitofWorkCapabilities.cs` | [ ] |
| 3-A.2 | `UnitofWork<T>` declares all 7 marker interfaces | `UnitofWork.Core.cs` | [ ] |
| 3-B.1 | Add undo/redo region to `IUnitofWorksManager` | `IUnitofWorksManagerInterfaces.cs` | [ ] |
| 3-B.2 | Add change-summary region to `IUnitofWorksManager` | `IUnitofWorksManagerInterfaces.cs` | [ ] |
| 3-B.3 | Add block-data-ops region to `IUnitofWorksManager` | `IUnitofWorksManagerInterfaces.cs` | [ ] |
| 3-B.4 | Add query-history region to `IUnitofWorksManager` | `IUnitofWorksManagerInterfaces.cs` | [ ] |
| 3-B.5 | Add aggregates region to `IUnitofWorksManager` | `IUnitofWorksManagerInterfaces.cs` | [ ] |
| 3-B.6 | Add batch-commit region to `IUnitofWorksManager` | `IUnitofWorksManagerInterfaces.cs` | [ ] |
| 3-B.7 | Add export/import region to `IUnitofWorksManager` | `IUnitofWorksManagerInterfaces.cs` | [ ] |
| 3-B.8 | Add grouping region to `IUnitofWorksManager` | `IUnitofWorksManagerInterfaces.cs` | [ ] |
| 3-C.1 | Implement all new methods in `FormsManager.DataOperations.cs` (new partial) | `FormsManager.DataOperations.cs` | [ ] |
| 3-C.2 | Update `UnitOfWorkWrapper` to forward new capabilities | `UnitOfWorkWrapper.cs` + `UnitOfWorkWrapperExtensions.cs` | [ ] |
