
# ObservableBindingList Enterprise Enhancement Plan

**Related Plan**: [observablebindinglist_partialclasses.plan.md](observablebindinglist_partialclasses.plan.md) — partial class split must be done first  
**Approach**: Keep all existing method signatures and logic. Fix bugs surgically within each method. No RefreshView() pipeline replacement.

---

## Phase 1: Tracking and Dirty State (HIGHEST PRIORITY)

The foundation for everything else. Without proper tracking, AcceptChanges/RejectChanges, undo/redo, and commit operations cannot work correctly.

**Target partial files**: `ObservableBindingList.Tracking.cs`, `ObservableBindingList.ListChanges.cs`

### 1A. Enhance Tracking Class

**File:** `DataManagementModelsStandard/Editor/Tracking.cs`

Current Tracking only has: `UniqueId`, `OriginalIndex`, `CurrentIndex`, `EntityState`, `IsSaved`, `IsNew`, `EntityName`, `PKFieldName/Value/Type`.

Add:

```csharp
public Dictionary<string, object> OriginalValues { get; set; }
public List<string> ModifiedProperties { get; set; } = new List<string>();
public DateTime? ModifiedAt { get; set; }
public string ModifiedBy { get; set; }
public int Version { get; set; } = 0;
public bool IsDirty => EntityState != EntityState.Unchanged;
```

**File:** `DataManagementModelsStandard/Editor/EntityState.cs`

Add `Detached` state for items removed from tracking without being persisted:

```csharp
public enum EntityState
{
    Added,
    Modified,
    Deleted,
    Unchanged,
    Detached
}
```

### 1B. Original Value Snapshots — Fix Critical Bug: `Item_PropertyChanged` Never Sets EntityState

**CRITICAL BUG**: The current `Item_PropertyChanged` handler (in `ObservableBindingList.ListChanges.cs`) validates and fires events but **never** looks up tracking or sets `EntityState = Modified`. This means `GetPendingChanges()` returns an empty `Modified` list for all property-level edits — the most common edit path. Only `SetItem` (full item replacement) updates tracking state. This makes change-tracking non-functional for in-place property edits.

**Fix** in `Item_PropertyChanged`: when entity is `Unchanged`, snapshot ALL current property values into `Tracking.OriginalValues` BEFORE changing state to `Modified`. This only happens once (first edit).

```csharp
void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
{
    T item = (T)sender;
    var tracking = GetTrackingItem(item);
    if (tracking != null && tracking.EntityState == EntityState.Unchanged)
    {
        // First modification -- snapshot original values BEFORE changing state
        tracking.OriginalValues = SnapshotValues(item);
        tracking.EntityState = EntityState.Modified;
        tracking.ModifiedAt = DateTime.UtcNow;
        tracking.ModifiedBy = CurrentUser;
    }
    if (tracking != null)
    {
        if (!tracking.ModifiedProperties.Contains(e.PropertyName))
            tracking.ModifiedProperties.Add(e.PropertyName);
        tracking.Version++;
    }
    // ... rest of existing validation and notification logic (keep as-is)
}
```

New methods:

- `string CurrentUser { get; set; }` — configurable, defaults to `Environment.UserName`
- `Dictionary<string, object> SnapshotValues(T item)` — reads all properties via `GetCachedProperties()` (existing static cache)
- `object GetOriginalValue(T item, string propertyName)` — returns value from snapshot
- `T GetOriginalItem(T item)` — reconstructs a copy with original values
- `bool IsDirty(T item)` — checks `tracking.EntityState != Unchanged`
- `Dictionary<string, (object Original, object Current)> GetChanges(T item)` — per-field diff

### 1C. HasChanges and DirtyItems

New properties on ObservableBindingList:

- `bool HasChanges` — true if any tracking record has `EntityState != Unchanged`
- `List<T> DirtyItems` — all items with `Modified` or `Added` state
- `int DirtyCount` — count of dirty items
- `int AddedCount`, `int ModifiedCount`, `int DeletedCount` — per-state counts
- `IReadOnlyList<T> AddedItems` — items with `EntityState.Added`
- `IReadOnlyList<T> DeletedItems` — items in `DeletedList`

Enhance `DataManagementModelsStandard/Editor/ObservableChanges.cs`:

```csharp
public class ObservableChanges<T>
{
    public List<T> Added { get; set; } = new();
    public List<T> Modified { get; set; } = new();
    public List<T> Deleted { get; set; } = new();
    public bool HasChanges => Added.Count > 0 || Modified.Count > 0 || Deleted.Count > 0;
    public int TotalCount => Added.Count + Modified.Count + Deleted.Count;
}
```

### 1D. AcceptChanges / RejectChanges

New methods on ObservableBindingList:

- **`AcceptChanges()`** — marks all items as `Unchanged`, clears `OriginalValues`, clears `ModifiedProperties`, clears `DeletedList`, clears `UpdateLog`, resets `Version` to 0
- **`RejectChanges()`** — for each `Modified` item: restore properties from `OriginalValues`. For each `Added` item: remove from list. For each `Deleted` item: restore to list. Reset all states to `Unchanged`
- **`AcceptChanges(T item)`** — per-item accept
- **`RejectChanges(T item)`** — per-item revert

---

## Phase 2: Fix 12 Critical Bugs (Surgical Fixes — No RefreshView Replacement)

**Approach**: Keep all existing `ApplyFilter`, `ApplySortCore`, `ApplySort`, `ApplyPaging` methods. Fix bugs **within** each method's existing structure. Add state-tracking fields so methods become aware of each other.

**Target partial files**: `ObservableBindingList.Sort.cs`, `ObservableBindingList.Filter.cs`, `ObservableBindingList.Pagination.cs`, `ObservableBindingList.ListChanges.cs`, `ObservableBindingList.Logging.cs`

### New Private State Fields

Add alongside existing fields in the core partial file:

```csharp
private List<T> _insertionOrderList = new List<T>();   // immutable insertion-order backup
private Func<T, bool> _activeFilterPredicate;           // stores last predicate-based filter
private PropertyDescriptor _activeSortProperty;         // mirrors sortProperty for cross-method awareness
private ListSortDirection _activeSortDirection;          // mirrors sortDirection for cross-method awareness
private bool _isPagingActive;                            // whether pagination is engaged
private int _filteredCount;                              // count after filter, before pagination
private List<T> _currentWorkingSet;                      // filtered+sorted result before paging
```

### New Public Properties

- `bool IsFiltered` → `_activeFilterPredicate != null || !string.IsNullOrEmpty(filterString)`
- `bool IsPaged` → `_isPagingActive`
- `int TotalCount` → `originalList.Count`
- `int FilteredCount` → `_filteredCount` (set by filter methods)

### New Events

- `FilterApplied` / `FilterRemoved`
- `SortApplied` / `SortRemoved`

### Private Helper Methods

```csharp
/// <summary>
/// After filtering from originalList, re-applies the active sort if one is set.
/// Called by all ApplyFilter methods before ResetItems.
/// </summary>
private List<T> ApplyActiveSortIfNeeded(List<T> items)
{
    if (isSorted && _activeSortProperty != null)
    {
        var comparer = new PropertyComparer<T>(_activeSortProperty, _activeSortDirection);
        items.Sort(comparer);
    }
    return items;
}

/// <summary>
/// Re-derives the view from originalList by calling existing filter/sort/page methods
/// in sequence based on which state fields are set. Called by InsertItem, RemoveItem,
/// AddRange, RemoveRange after data mutations to keep the view consistent.
/// Does NOT replace existing methods — just calls them.
/// </summary>
private void ReapplyActiveTransformations()
{
    SuppressNotification = true;
    RaiseListChangedEvents = false;

    // Start from originalList
    List<T> workingSet = new List<T>(originalList);

    // Re-apply filter if active
    if (_activeFilterPredicate != null)
    {
        workingSet = workingSet.Where(_activeFilterPredicate).ToList();
    }
    else if (!string.IsNullOrEmpty(filterString))
    {
        var fil = ParseFilter(filterString);
        if (fil != null)
            workingSet = originalList.AsQueryable().Where(fil).ToList();
    }

    _filteredCount = workingSet.Count;

    // Re-apply sort if active
    workingSet = ApplyActiveSortIfNeeded(workingSet);

    // Store for pagination
    _currentWorkingSet = workingSet;

    // Re-apply pagination if active
    if (_isPagingActive && PageSize > 0)
    {
        int totalPages = (int)Math.Ceiling((double)workingSet.Count / PageSize);
        if (CurrentPage > totalPages) CurrentPage = Math.Max(1, totalPages);
        workingSet = workingSet.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();
    }

    ResetItems(workingSet);

    // Fix stale _currentIndex
    if (Items.Count == 0) _currentIndex = -1;
    else if (_currentIndex >= Items.Count) _currentIndex = 0;
    else if (_currentIndex < 0 && Items.Count > 0) _currentIndex = 0;

    ResetBindings();
    SuppressNotification = false;
    RaiseListChangedEvents = true;
}
```

### The 12 Bugs and Their Fixes

---

**BUG 1: Sort then Filter loses the sort**

`ApplySortCore` sorts only `Items`. Then `ApplyFilter` reads from unsorted `originalList`, so the sort vanishes.

**Fix in `ObservableBindingList.Filter.cs`**: In both `ApplyFilter(Func<T, bool>)` and `private ApplyFilter()`, after filtering from `originalList`, call `ApplyActiveSortIfNeeded(filteredItems)` before `ResetItems()`. Also store the predicate: `_activeFilterPredicate = predicate`.

```csharp
// In ApplyFilter(Func<T, bool> predicate):
var filteredItems = originalList.Where(predicate).ToList();
filteredItems = ApplyActiveSortIfNeeded(filteredItems);  // <-- ADD
_activeFilterPredicate = predicate;                      // <-- ADD
_filteredCount = filteredItems.Count;                    // <-- ADD
_currentWorkingSet = filteredItems;                      // <-- ADD
ResetItems(filteredItems);
```

---

**BUG 2: `ApplySort(string)` permanently destroys insertion order**

`originalList.Sort(comparer)` mutates `originalList` in place. After this, `RemoveSortCore()` restores from `originalList` but it's already sorted — insertion order lost forever.

**Fix in `ObservableBindingList.Sort.cs`**: 
- Populate `_insertionOrderList` in all constructors (copy of `originalList` at construction time).
- Keep `_insertionOrderList` in sync when items are added/removed.
- In `ApplySort(string, dir)`: sort a **copy** of `originalList` instead of mutating it. Store sort state.
- In `RemoveSortCore()`: restore from `_insertionOrderList` instead of `originalList`.

```csharp
// ApplySort(string, dir) — changed:
public void ApplySort(string propertyName, ListSortDirection direction)
{
    // ... existing validation ...
    var comparer = new PropertyComparer<T>(propDesc, direction);

    // Sort a COPY, not originalList itself
    var sortedList = new List<T>(originalList);
    sortedList.Sort(comparer);

    // Store sort state for cross-method awareness
    _activeSortProperty = propDesc;
    _activeSortDirection = direction;
    isSorted = true;
    sortProperty = propDesc;
    sortDirection = direction;

    _currentWorkingSet = sortedList;
    ResetItems(sortedList);
    ResetBindings();
}

// RemoveSortCore() — changed:
protected override void RemoveSortCore()
{
    SuppressNotification = true;
    RaiseListChangedEvents = false;
    isSorted = false;
    sortProperty = null;
    _activeSortProperty = null;
    sortDirection = ListSortDirection.Ascending;
    ResetItems(_insertionOrderList);  // <-- restore from backup, not originalList
    SuppressNotification = false;
    RaiseListChangedEvents = true;
}
```

---

**BUG 3: Pagination ignores active filter**

`ApplyPaging()` does `originalList.Skip().Take()`. If a filter is active, pagination pages across all unfiltered items.

**Fix in `ObservableBindingList.Pagination.cs`**: `ApplyPaging()` pages from `_currentWorkingSet` (which holds the filtered+sorted result) instead of `originalList`.

```csharp
private void ApplyPaging()
{
    _isPagingActive = true;
    var source = _currentWorkingSet ?? originalList;
    var pagedItems = source.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();
    ResetItems(pagedItems);
    ResetBindings();
}
```

---

**BUG 4: `_currentIndex` goes stale after filter/sort/page**

`ResetItems()` never touches `_currentIndex`. After filtering 100 items to 5, if `_currentIndex` was 75, `Current` returns `default(T)` (null).

**Fix in `ObservableBindingList.Filter.cs`**: At the end of `ResetItems()`, after `UpdateIndexTrackingAfterFilterorSort()`, clamp `_currentIndex`:

```csharp
private void ResetItems(List<T> items)
{
    // ... existing logic (unhook, clear, add, rehook) ...

    UpdateIndexTrackingAfterFilterorSort();

    // FIX: Clamp _currentIndex to valid range
    if (Items.Count == 0)
        _currentIndex = -1;
    else if (_currentIndex >= Items.Count)
        _currentIndex = 0;
    else if (_currentIndex < 0 && Items.Count > 0)
        _currentIndex = 0;
}
```

---

**BUG 5: Three sort methods behave inconsistently**

- `ApplySort(string, dir)` — mutates `originalList`, updates tracking
- `ApplySortCore(prop, dir)` — sorts `Items` only, no `originalList` change
- `Sort(string)` — sorts `Items` only, no `ResetItems`, no tracking update

**Fix in `ObservableBindingList.Sort.cs`**: Make all three consistent about sort state and tracking:

1. `ApplySort(string, dir)` — stop mutating `originalList` (see BUG 2 fix). Store `_activeSortProperty`/`_activeSortDirection`, set `isSorted = true`.
2. `ApplySortCore(prop, dir)` — keep existing logic, also store `_activeSortProperty = prop`, `_activeSortDirection = direction`.
3. `Sort(string)` — add `ResetItems(items)` call at the end so tracking gets updated. Store sort state in `_activeSortProperty`/`_activeSortDirection`.
4. `ApplySort(ListSortDescriptionCollection)` — keep existing logic, also store active state.

---

**BUG 6: No pipeline state — operations unaware of each other**

No `_activeFilter`, `_activeSort`, or `_activePaging` state is stored. Each operation runs independently.

**Fix**: The new state fields listed above (`_activeFilterPredicate`, `_activeSortProperty`, `_activeSortDirection`, `_isPagingActive`, `_currentWorkingSet`, `_filteredCount`) solve this. Each method records its state. `ReapplyActiveTransformations()` can re-derive the view after data mutations.

---

**BUG 7 (NEW): `TotalPages` uses `originalList.Count` instead of filtered count**

`TotalPages => Math.Ceiling((double)originalList.Count / PageSize)` — wrong when filter is active.

**Fix in core file**: Change `TotalPages` property getter:

```csharp
public int TotalPages => (int)Math.Ceiling((double)(_currentWorkingSet?.Count ?? originalList.Count) / PageSize);
```

---

**BUG 8 (NEW): `InsertItem` originalList positioning is flawed under filter/sort**

When filter or sort is active, `originalList.Insert(index, item)` uses the **view index** as the position in `originalList`. But view indices don't correspond to `originalList` positions when filter/sort is active, corrupting master data order.

**Fix in `ObservableBindingList.ListChanges.cs`**: When filter or sort is active, always **append** to `originalList` (end of insertion order). Also append to `_insertionOrderList`. Then optionally call `ReapplyActiveTransformations()` so the item appears at the correct view position.

```csharp
// InsertItem — change the originalList logic:
if (IsFiltered || isSorted)
{
    originalList.Add(item);
    _insertionOrderList.Add(item);
    tr.OriginalIndex = originalList.Count - 1;
}
else
{
    originalList.Insert(index, item);
    _insertionOrderList.Insert(index, item);
}
```

---

**BUG 9 (NEW): `RemoveItem` by `OriginalIndex` can remove wrong item after prior removals**

`originalList.RemoveAt(removedOriginalIndex)` uses index-based removal. If multiple sequential removals shift indices, this can remove the wrong item.

**Fix in `ObservableBindingList.ListChanges.cs`**: Change to reference-based removal:

```csharp
// RemoveItem — change from:
//   originalList.RemoveAt(removedOriginalIndex);
// to:
int actualIndex = originalList.IndexOf(removedItem);
if (actualIndex >= 0)
{
    removedOriginalIndex = actualIndex;
    originalList.RemoveAt(actualIndex);
    _insertionOrderList.Remove(removedItem);
}
```

---

**BUG 10 (NEW): `OnListChanged` silently moves cursor on every `ItemChanged` event**

At line 1474: `_currentIndex = e.NewIndex` fires on every property change. Editing a cell in row 5 while cursor is on row 20 silently jumps the cursor to row 5.

**Fix in `ObservableBindingList.ListChanges.cs`**: Remove the `_currentIndex = e.NewIndex` line from `OnListChanged`. The cursor should only move via explicit navigation (`MoveNext`, `MoveTo`, etc.) or `SetPosition`.

```csharp
protected override void OnListChanged(ListChangedEventArgs e)
{
    if (SuppressNotification || _isPositionChanging)
        return;

    if (!_isPositionChanging)
    {
        // REMOVED: _currentIndex = e.NewIndex on ItemChanged
        // Property edits must NOT move the cursor
        base.OnListChanged(e);
    }
}
```

---

**BUG 11 (NEW): `Item_PropertyChanged` never sets `EntityState = Modified`**

The handler validates and fires events but never looks up tracking or sets state. `GetPendingChanges()` returns empty Modified list for property-level edits.

**Fix**: Already addressed in Phase 1B above. Listed here for completeness as a Phase 2 bug that Phase 1 resolves.

---

**BUG 12 (NEW): `UpdateLog` keyed by `DateTime` — collision risk**

`Dictionary<DateTime, EntityUpdateInsertLog>` — if two operations occur in the same tick, the second overwrites the first.

**Fix in `ObservableBindingList.Logging.cs`**: Change to `Dictionary<Guid, EntityUpdateInsertLog>`. Add `LogId` (`Guid`) and keep `LogDateandTime` as a queryable property on `EntityUpdateInsertLog`. Update `CreateLogEntry`, `CommitItemAsync`, `ResetAfterCommit`, and `MarkAsCommitted` to use `Guid` key.

```csharp
public Dictionary<Guid, EntityUpdateInsertLog> UpdateLog { get; set; }

// In CreateLogEntry:
var logId = Guid.NewGuid();
UpdateLog[logId] = new EntityUpdateInsertLog { LogId = logId, LogDateandTime = DateTime.UtcNow, ... };
```

---

### Constructor Updates for Phase 2

All constructors must initialize the new state fields:

```csharp
// Add to ClearAll() or each constructor:
_insertionOrderList = new List<T>();
_currentWorkingSet = null;
_activeFilterPredicate = null;
_activeSortProperty = null;
_isPagingActive = false;
_filteredCount = 0;

// After populating originalList in each constructor:
_insertionOrderList = new List<T>(originalList);
```

---

## Phase 3: Current/Index Position Management and Events

**Target partial files**: `ObservableBindingList.CurrentAndMovement.cs`, `ObservableBindingList.ListChanges.cs`

### 3A. Fix Current/Index

**Design Rule**: Index is the single source of truth. `Current` is ALWAYS read-only, derived from `_currentIndex`.

```
CurrentIndex (settable) --> _currentIndex (backing field) --> Current (read-only, derived)
MoveToItem(T item)      --> finds index via IndexOf         --> calls MoveTo(index)
```

Changes:

- **Keep `Current` read-only** — no setter
- **Make `CurrentIndex` settable** — `set => MoveTo(value)`
- **Add `MoveToItem(T item)`** — `int idx = Items.IndexOf(item); if (idx >= 0) MoveTo(idx);`
- **Remove silent cursor-jump** from `OnListChanged` — already done in BUG 10 fix (Phase 2). Property edits must NOT move `_currentIndex`.

### 3B. BOF/EOF Cursor Semantics

- `bool IsAtBOF` — `_currentIndex <= 0 || Count == 0`
- `bool IsAtEOF` — `_currentIndex >= Count - 1 || Count == 0`
- `bool IsEmpty` — `Count == 0`
- `bool IsPositionValid` — `_currentIndex >= 0 && _currentIndex < Count`

### 3C. CurrentChanging Event

New event that fires BEFORE position changes, with cancel support:

```csharp
public event EventHandler<CurrentChangingEventArgs> CurrentChanging;
```

`CurrentChangingEventArgs`: `OldIndex`, `NewIndex`, `OldItem`, `NewItem`, `Cancel`

All Move methods and `SetPosition` check `Cancel` before applying.

### 3D. Enhanced Event Model

New events:

- `BatchOperationStarted` / `BatchOperationCompleted` — for `AddRange` / `RemoveRange`
- `BeforeSave` / `AfterSave` — around commit operations
- `FilterApplied` / `FilterRemoved` (also in Phase 2)
- `SortApplied` / `SortRemoved` (also in Phase 2)

New EventArgs in `DataManagementModelsStandard/Editor/ObservableBindingListEventArgs.cs`:

- `CurrentChangingEventArgs` — `OldIndex`, `NewIndex`, `OldItem`, `NewItem`, `Cancel`
- `BatchOperationEventArgs` — `OperationType` (AddRange/RemoveRange), `ItemCount`
- `CommitEventArgs<T>` — `Item`, `EntityState`, `Cancel`

---

## Phase 4: Validation Framework

**Target partial files**: New `ObservableBindingList.Validation.cs`

### 4A. Data Annotations Support

Read standard .NET Data Annotations from `T` properties at startup (cached):

- `[Required]` — field must not be null/empty
- `[MaxLength(n)]` / `[MinLength(n)]` — string length
- `[Range(min, max)]` — numeric range
- `[RegularExpression(pattern)]` — regex match
- `[EmailAddress]`, `[Phone]`, `[Url]` — format validators
- `[CustomValidation]` — custom method

Auto-validate on property change (opt-in via `bool IsAutoValidateEnabled`). Block commit for invalid items.

### 4B. Validation Result Framework

New classes (new file `DataManagementModelsStandard/Editor/ValidationResult.cs`):

```csharp
public enum ValidationSeverity { Error, Warning, Info }

public class ValidationError
{
    public string PropertyName { get; set; }
    public string ErrorMessage { get; set; }
    public ValidationSeverity Severity { get; set; }
}

public class ValidationResult
{
    public bool IsValid => !Errors.Any(e => e.Severity == ValidationSeverity.Error);
    public List<ValidationError> Errors { get; set; } = new();
}
```

New members on ObservableBindingList:

- `Func<T, ValidationResult> CustomValidator { get; set; }` — pluggable custom validation
- `ValidationResult Validate(T item)` — runs Data Annotations + CustomValidator
- `ValidationResult ValidateAll()` — validates every item
- `List<ValidationError> GetErrors(T item)` — errors for specific item
- `bool IsValid` — true when all items pass validation
- `event EventHandler<ValidationEventArgs<T>> ValidationFailed`

### 4C. IDataErrorInfo / INotifyDataErrorInfo Bridge

Consider implementing `IDataErrorInfo` on entity level or providing a bridge so WinForms `DataGridView` and other controls can natively consume per-row validation errors. The `ValidationResult` framework feeds into this for free UI error indicators.

---

## Phase 5: Commit and Master-Detail

**Target partial files**: `ObservableBindingList.Tracking.cs`, New `ObservableBindingList.MasterDetail.cs`

### 5A. Batch Commit

- `CommitAllAsync(insertAsync, updateAsync, deleteAsync)` — iterates all pending changes in order: **Deletes first, then Updates, then Inserts** (default order avoids FK constraint violations)
- `CommitAllAsync(..., CommitOrder order)` — configurable order enum
- Returns `CommitResult` with `List<CommitItemResult>` (per-item success/failure/error)
- Fires `BeforeSave` / `AfterSave` events
- On success, calls `AcceptChanges()` for committed items
- Batch-commits and calls tracking rebuild **once** at the end, not per-item

**Fix for existing `CommitItemAsync`**: Currently removes items from `originalList` and `Items` for deletes but doesn't call index-shift rebuild. `CommitAllAsync` handles this correctly by deferring rebuild.

### 5B. Master-Detail Relationship Support

Oracle Forms-style parent-child navigation. When parent cursor moves, child list auto-filters.

```csharp
// On parent list:
parentList.RegisterDetail(childList, foreignKeyProperty: "ParentId", masterKeyProperty: "Id");

// When parentList.CurrentChanged fires:
//   childList auto-applies filter: item.ParentId == parentList.Current.Id
//   childList._currentIndex resets to 0
//   childList fires CurrentChanged
```

New members:

- `void RegisterDetail<TChild>(ObservableBindingList<TChild> childList, string foreignKeyProp, string masterKeyProp)`
- `void UnregisterDetail<TChild>(ObservableBindingList<TChild> childList)`
- `List<object> DetailLists` — registered child lists
- Internal: on `CurrentChanged`, iterate detail lists and apply master-key filter via their existing `ApplyFilter()` method

---

## Phase 6: Computed Columns, Bookmarks, Lazy Loading

**Target partial files**: New `ObservableBindingList.Computed.cs`, `ObservableBindingList.Bookmarks.cs`, `ObservableBindingList.VirtualLoading.cs`

### 6A. Computed / Virtual Columns

Register derived values that auto-recalculate on property change:

```csharp
list.RegisterComputed("FullName", item => $"{item.FirstName} {item.LastName}");
list.RegisterComputed("TotalPrice", item => item.Quantity * item.UnitPrice);

object val = list.GetComputed(item, "FullName"); // "John Doe"
```

- `void RegisterComputed(string name, Func<T, object> computation)`
- `void UnregisterComputed(string name)`
- `object GetComputed(T item, string name)`
- `Dictionary<string, object> GetAllComputed(T item)`
- Auto-recalculate on `Item_PropertyChanged` for registered computations
- Fire `PropertyChanged` with the computed column name so UI can bind

### 6B. Bookmarks / Named Positions

Save and restore cursor positions by name:

```csharp
list.SetBookmark("beforeEdit");
// ... navigate around, edit ...
list.GoToBookmark("beforeEdit"); // jumps back
```

- `void SetBookmark(string name)` — saves `_currentIndex` under the name
- `bool GoToBookmark(string name)` — restores position, returns false if bookmark invalid
- `void RemoveBookmark(string name)`
- `IReadOnlyDictionary<string, int> Bookmarks` — all saved bookmarks
- `void ClearBookmarks()`
- Bookmarks auto-invalidate after filter/sort/page (position may have changed)

### 6C. Lazy / Virtual Loading

For large datasets (100K+ rows), load pages on demand instead of all at once:

```csharp
list.SetDataProvider(async (pageIndex, pageSize) =>
{
    return await myDataSource.GetPageAsync(pageIndex, pageSize);
});
list.SetTotalItemCount(100000);
list.SetPageSize(50);
// Now GoToPage(n) calls the provider instead of reading from originalList
```

- `void SetDataProvider(Func<int, int, Task<List<T>>> provider)` — page loader callback
- `void SetTotalItemCount(int count)` — total rows available server-side
- `bool IsVirtualMode` — true when a data provider is set
- When `GoToPage(n)` is called in virtual mode, it calls the provider instead of `originalList.Skip().Take()`
- Caching: keep a configurable number of pages in memory (default: 3 — current, previous, next)
- When virtual mode is enabled, `originalList` only holds currently-loaded pages. `_insertionOrderList` is disabled.

---

## Phase 7: Undo/Redo, Thread Safety, Freeze, Aggregates

**Target partial files**: New `ObservableBindingList.UndoRedo.cs`, `ObservableBindingList.ThreadSafety.cs`, `ObservableBindingList.Aggregates.cs`

### 7A. Undo/Redo

New internal class `UndoRedoManager<T>` (new file `DataManagementModelsStandard/Editor/UndoRedoManager.cs`):

- Stores `UndoAction` records: `ActionType` (PropertyChange/Insert/Remove), `Item`, `PropertyName`, `OldValue`, `NewValue`, `Index`
- `Undo()` / `Redo()` methods
- `bool CanUndo`, `bool CanRedo` properties
- Configurable `int MaxUndoDepth` (default 50)
- Opt-in: `bool IsUndoEnabled` (default false, zero cost when off)

### 7B. Thread Safety

- `private readonly ReaderWriterLockSlim _rwLock`
- `bool IsThreadSafe { get; set; }` — opt-in (default false)
- When enabled, wrap mutations in `_rwLock.EnterWriteLock()`, reads in `_rwLock.EnterReadLock()`
- Zero overhead when `IsThreadSafe == false` (no lock calls)

### 7C. Read-Only / Freeze Mode

- `bool IsFrozen` — when true, all mutations throw `InvalidOperationException`
- `void Freeze()` / `void Unfreeze()`
- `IDisposable BeginBatchUpdate()` — suppresses all notifications during batch, fires single `Reset` on dispose. Stores and restores `SuppressNotification` and `RaiseListChangedEvents` state so nested batch updates work correctly.

### 7D. Aggregate / Summary Support

```csharp
decimal total = list.Sum("Price");
decimal avg = list.Average("Rating");
object earliest = list.Min("CreatedDate");
object latest = list.Max("CreatedDate");
int active = list.CountWhere(x => x.IsActive);
var groups = list.GroupBy("Category");

// Filtered aggregation
decimal filteredTotal = list.SumWhere("Price", x => x.IsActive);
decimal filteredAvg = list.AverageWhere("Rating", x => x.Category == "Electronics");
```

All aggregates operate on the current `Items` view (respecting active filter/sort/page).

---

## File Change Summary

### Existing Files Modified

| File | Changes |
|------|---------|
| `ObservableBindingList.cs` (core partial) | New state fields, `TotalPages` fix, `IsFiltered`/`IsPaged`/`TotalCount`/`FilteredCount` properties |
| `ObservableBindingList.Sort.cs` | BUG 2 fix (stop mutating originalList), BUG 5 fix (consistent sort state), store `_activeSortProperty`/`_activeSortDirection` |
| `ObservableBindingList.Filter.cs` | BUG 1 fix (re-sort after filter), BUG 4 fix (clamp `_currentIndex` in ResetItems), store `_activeFilterPredicate`/`_filteredCount`/`_currentWorkingSet` |
| `ObservableBindingList.Pagination.cs` | BUG 3 fix (page from `_currentWorkingSet`), BUG 7 fix (`TotalPages` uses filtered count) |
| `ObservableBindingList.ListChanges.cs` | BUG 8 fix (InsertItem append under filter/sort), BUG 9 fix (RemoveItem by reference), BUG 10 fix (remove cursor-jump), Phase 1B (Item_PropertyChanged sets Modified) |
| `ObservableBindingList.Logging.cs` | BUG 12 fix (UpdateLog keyed by Guid) |
| `ObservableBindingList.Tracking.cs` | Phase 1 snapshots, AcceptChanges/RejectChanges, Phase 5 batch commit |
| `ObservableBindingList.CurrentAndMovement.cs` | Phase 3 BOF/EOF, CurrentChanging, MoveToItem, settable CurrentIndex |
| `ObservableBindingList.Constructors.cs` | Initialize `_insertionOrderList` and new state fields |
| `Tracking.cs` | Add OriginalValues, ModifiedAt, ModifiedBy, Version, ModifiedProperties, IsDirty |
| `EntityState.cs` | Add `Detached` state |
| `ObservableBindingListEventArgs.cs` | Add CurrentChangingEventArgs, BatchOperationEventArgs, CommitEventArgs, ValidationEventArgs |
| `ObservableChanges.cs` | Add HasChanges, TotalCount properties |
| `EntityUpdateInsertLog` | Add `LogId` (Guid) property |

### New Files

| File | Content |
|------|---------|
| `ObservableBindingList.Validation.cs` | Phase 4 — Data Annotations, custom validators, ValidationResult |
| `ObservableBindingList.MasterDetail.cs` | Phase 5B — RegisterDetail, UnregisterDetail, auto-filter on CurrentChanged |
| `ObservableBindingList.Computed.cs` | Phase 6A — RegisterComputed, GetComputed, auto-recalculate |
| `ObservableBindingList.Bookmarks.cs` | Phase 6B — SetBookmark, GoToBookmark, auto-invalidation |
| `ObservableBindingList.VirtualLoading.cs` | Phase 6C — SetDataProvider, virtual mode, page caching |
| `ObservableBindingList.UndoRedo.cs` | Phase 7A — UndoRedoManager integration |
| `ObservableBindingList.ThreadSafety.cs` | Phase 7B — ReaderWriterLockSlim wrapper |
| `ObservableBindingList.Aggregates.cs` | Phase 7D — Sum, Average, Min, Max, CountWhere, GroupBy, SumWhere, AverageWhere |
| `ValidationResult.cs` | ValidationResult, ValidationError, ValidationSeverity classes |
| `UndoRedoManager.cs` | UndoRedoManager<T>, UndoAction classes |

---

## Implementation Priority Order

1. **Phase 0: Partial Class Split** — Execute [observablebindinglist_partialclasses.plan.md](observablebindinglist_partialclasses.plan.md) first
2. **Phase 1: Tracking and Dirty State** (1A → 1B → 1C → 1D) — Foundation for everything
3. **Phase 2: Fix 12 Bugs** — Surgical fixes in each method, state fields, `ReapplyActiveTransformations()`
4. **Phase 3: Current/Index + Events** — Position management, BOF/EOF, CurrentChanging
5. **Phase 4: Validation** — Data Annotations + custom validators + IDataErrorInfo bridge
6. **Phase 5: Commit + Master-Detail** — Batch operations and parent-child
7. **Phase 6: Computed Columns, Bookmarks, Lazy Loading** — Advanced features
8. **Phase 7: Undo/Redo, Thread Safety, Freeze, Aggregates** — Final polish

---

## Verification Checklist

After each phase, verify:

| Phase | Verification |
|-------|-------------|
| Phase 1 | `GetPendingChanges()` correctly returns Modified items when properties change in-place. `AcceptChanges()`/`RejectChanges()` work per-item and bulk. `IsDirty(item)` returns true after edit. |
| Phase 2 | Sort → Filter preserves sort order. Filter → Paginate pages filtered data only. `RemoveSort` restores true insertion order. `_currentIndex` never goes out of bounds. `InsertItem` under filter/sort doesn't corrupt `originalList`. `RemoveItem` removes correct item. Property edit doesn't jump cursor. |
| Phase 3 | `MoveToItem()` works. `CurrentChanging` can cancel navigation. `IsAtBOF`/`IsAtEOF` are accurate. |
| Phase 4 | `[Required]` attributes block commit. `ValidateAll()` reports all violations. Custom validators fire. |
| Phase 5 | `CommitAllAsync` processes Delete → Update → Insert by default. Master-detail child auto-filters on parent move. |
| Phase 6 | Computed columns recalculate on property change. Bookmarks save/restore correctly. Virtual mode loads pages on demand. |
| Phase 7 | Undo/Redo reverts property changes. Freeze blocks mutations. Aggregates respect active filter. |

