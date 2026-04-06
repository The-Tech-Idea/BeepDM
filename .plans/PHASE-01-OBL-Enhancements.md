# Phase 1 — ObservableBindingList Core Enhancements

**Goal:** Add missing API surface to `ObservableBindingList<T>` that FormsManager and UOW need
but currently have to work around: field-level original-value queries, change-set exports,
async bulk load, async search, server-merge with conflict resolution, and lightweight grouping.

**Files touched:** `DataManagementModelsStandard/ObservableBindingList/`

---

## 1-A  Field-Level Change Introspection

**New file:** `ObservableBindingList.ChangeInspection.cs`

Tracking records already store entity state (`Added / Modified / Deleted / Unchanged`).
The gap is that UOW and FormsManager cannot ask "which fields changed" or "what was the old value".

### API

```csharp
// Returns null when item is not tracked or field was not changed
object GetOriginalValue(T item, string fieldName);

// Returns list of field names whose value differs from the original snapshot
IReadOnlyList<string> GetChangedFields(T item);

// Returns (OriginalValue, CurrentValue) pair — convenience for grids / diff views
(object Original, object Current) GetFieldDelta(T item, string fieldName);

// True when at least one field on item differs from its original snapshot
bool HasFieldChanges(T item);
```

### Implementation notes

- Tracking already has `EntityState`.  Add `Dictionary<string, object> OriginalFieldValues`
  to `Tracking` (captured once, when item first transitions from `Unchanged → Modified`).
- Capture snapshot lazily inside `Item_PropertyChanged` when
  `tracking.EntityState == EntityState.Unchanged` and before updating it to `Modified`.
- `GetOriginalValue` / `GetChangedFields` walk `OriginalFieldValues`.

---

## 1-B  Change-Set Export

**Add to:** `ObservableBindingList.Tracking.cs` (or new file `ObservableBindingList.ChangeSet.cs`)

FormsManager can then ask for only the dirty data to commit, instead of scanning everything.

### API

```csharp
IReadOnlyList<T> GetInserted();
IReadOnlyList<T> GetUpdated();
IReadOnlyList<T> GetDeleted();   // returns the DeletedList items
IReadOnlyList<T> GetDirty();     // union of Inserted + Updated

// Returns a snapshot with counts — no allocations
ChangeSetSummary GetChangeSetSummary();
// ChangeSetSummary POCO: int InsertCount, UpdateCount, DeleteCount, bool IsDirty
```

---

## 1-C  Batch / Bulk Load

**Add to:** `ObservableBindingList.CRUD.cs`

`AddRange` already exists but fires individual `CollectionChanged` events.
Add a suppressed-notification path for high-volume loads (thousands of rows).

### API

```csharp
// Synchronous: suppress notifications, add all, fire single Reset at end
void LoadBatch(IEnumerable<T> items);

// Async: add in batchSize chunks, report progress, support cancellation
Task LoadBatchAsync(
    IEnumerable<T> items,
    int batchSize = 500,
    IProgress<int> progress = null,
    CancellationToken ct = default);
```

### Implementation notes

- Set `_suppressNotification = true` during load; fire `CollectionChanged(Reset)` once at end.
- `LoadBatchAsync` yields back to caller between chunks (`await Task.Yield()`).
- Reset all tracking to `Unchanged` after a bulk load (isInitial load, not edits).

---

## 1-D  Async Search

**Add to:** `ObservableBindingList.Search.cs`

Current `Search(predicate)` is synchronous.  Fine for <10 k records; blocks UI thread for larger sets.

### API

```csharp
Task<IReadOnlyList<T>> SearchAsync(
    Func<T, bool> predicate,
    CancellationToken ct = default);

// Chunked search — yields results incrementally
IAsyncEnumerable<T> SearchStreamAsync(
    Func<T, bool> predicate,
    CancellationToken ct = default);
```

---

## 1-E  Server Merge / Conflict Resolution

**New file:** `ObservableBindingList.Merge.cs`

FormsManager needs to handle optimistic-concurrency scenarios where the server returns
an updated dataset while the client has local edits.

### ConflictMode enum

```csharp
public enum ConflictMode
{
    ClientWins,      // keep local edits, discard server changes for conflicting fields
    ServerWins,      // overwrite local edits with server values
    KeepBoth,        // mark item as Conflicted; app resolves manually
    ThrowOnConflict  // throw ConflictException listing conflicting fields
}
```

### API

```csharp
// Synchronous merge
MergeResult<T> Merge(
    IEnumerable<T> serverItems,
    ConflictMode mode = ConflictMode.ServerWins,
    string primaryKeyField = null);

// Async merge for large sets
Task<MergeResult<T>> MergeAsync(
    IEnumerable<T> serverItems,
    ConflictMode mode = ConflictMode.ServerWins,
    string primaryKeyField = null,
    CancellationToken ct = default);

// MergeResult POCO: int Added, Updated, Conflicted, Unchanged;
//                   IReadOnlyList<T> ConflictedItems
```

### Implementation notes

- Match server items to local items by primary-key field (reflection, cached via `GetCachedProperty`).
- For each matched pair: compare field-by-field.  If item is `Unchanged` locally → apply server update silently.
- If item is `Modified` locally and server value differs → apply `ConflictMode` logic.
- New server items (not in local list) → `LoadBatch([newItem])`.
- Server-deleted items (in local list but absent from server) → mark `Deleted`.

---

## 1-F  Grouping Support

**New file:** `ObservableBindingList.Grouping.cs`

Lightweight client-side grouping for display layers — does NOT modify the actual list.

### API

```csharp
// Returns groups in key order
IReadOnlyList<ItemGroup<T>> GetGroups<TKey>(
    Func<T, TKey> keySelector,
    bool ascending = true);

// ItemGroup<T> POCO:
//   TKey Key
//   string KeyDisplay  (key.ToString() by default)
//   IReadOnlyList<T> Items
//   int Count
```

### Implementation notes

- Works on the current working set (`_currentWorkingSet ?? originalList`).
- Re-runs on demand (not cached), keeps it simple.

---

## Checklist

| # | Task | File | Status |
|---|---|---|---|
| 1-A.1 | Add `OriginalFieldValues` dict to `Tracking` | `Tracking.cs` | [ ] |
| 1-A.2 | Capture snapshot in `Item_PropertyChanged` | `ObservableBindingList.ListChanges.cs` | [ ] |
| 1-A.3 | Implement `GetOriginalValue`, `GetChangedFields`, `GetFieldDelta`, `HasFieldChanges` | `ObservableBindingList.ChangeInspection.cs` (new) | [ ] |
| 1-B.1 | Define `ChangeSetSummary` POCO | `ObservableChanges.cs` | [ ] |
| 1-B.2 | Implement `GetInserted/Updated/Deleted/Dirty/GetChangeSetSummary` | `ObservableBindingList.ChangeSet.cs` (new) | [ ] |
| 1-C.1 | Implement `LoadBatch(IEnumerable<T>)` | `ObservableBindingList.CRUD.cs` | [ ] |
| 1-C.2 | Implement `LoadBatchAsync(...)` | `ObservableBindingList.CRUD.cs` | [ ] |
| 1-D.1 | Implement `SearchAsync` | `ObservableBindingList.Search.cs` | [ ] |
| 1-D.2 | Implement `SearchStreamAsync` | `ObservableBindingList.Search.cs` | [ ] |
| 1-E.1 | Define `ConflictMode` enum + `MergeResult<T>` POCO | new `ObservableBindingList.Merge.cs` | [ ] |
| 1-E.2 | Implement `Merge(...)` | `ObservableBindingList.Merge.cs` | [ ] |
| 1-E.3 | Implement `MergeAsync(...)` | `ObservableBindingList.Merge.cs` | [ ] |
| 1-F.1 | Define `ItemGroup<T>` POCO | new `ObservableBindingList.Grouping.cs` | [ ] |
| 1-F.2 | Implement `GetGroups<TKey>` | `ObservableBindingList.Grouping.cs` | [ ] |
