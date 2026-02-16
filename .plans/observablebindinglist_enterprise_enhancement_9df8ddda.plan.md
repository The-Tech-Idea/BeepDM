
# ObservableBindingList Enterprise Enhancement Plan

---

## Phase 1: Tracking and Dirty State (HIGHEST PRIORITY)

The foundation for everything else. Without proper tracking, AcceptChanges/RejectChanges, undo/redo, and commit operations cannot work correctly.

### 1A. Enhance Tracking Class

**File:** [Tracking.cs](c:\Users\f_ald\source\repos\The-Tech-Idea\BeepDM\DataManagementModelsStandard\Editor\Tracking.cs)

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

**File:** [EntityState.cs](c:\Users\f_ald\source\repos\The-Tech-Idea\BeepDM\DataManagementModelsStandard\Editor\EntityState.cs)

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

### 1B. Original Value Snapshots in ObservableBindingList

**File:** [ObservableBindingList.cs](c:\Users\f_ald\source\repos\The-Tech-Idea\BeepDM\DataManagementModelsStandard\Editor\ObservableBindingList.cs)

In `Item_PropertyChanged`: when entity is `Unchanged`, snapshot ALL current property values into `Tracking.OriginalValues` BEFORE changing state to `Modified`. This only happens once (first edit).

```csharp
void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
{
    T item = (T)sender;
    var tracking = GetTrackingItem(item);
    if (tracking != null && tracking.EntityState == EntityState.Unchanged)
    {
        // First modification -- snapshot original values
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
    // ... rest of existing logic
}
```

New methods:

- `string CurrentUser { get; set; }` -- configurable, defaults to `Environment.UserName`
- `Dictionary<string, object> SnapshotValues(T item)` -- reads all properties via cached reflection
- `object GetOriginalValue(T item, string propertyName)` -- returns value from snapshot
- `T GetOriginalItem(T item)` -- reconstructs a copy with original values
- `bool IsDirty(T item)` -- checks `tracking.EntityState != Unchanged`
- `Dictionary<string, (object Original, object Current)> GetChanges(T item)` -- per-field diff

### 1C. HasChanges and DirtyItems

New properties on ObservableBindingList:

- `bool HasChanges` -- true if any tracking record has `EntityState != Unchanged`
- `List<T> DirtyItems` -- all items with `Modified` or `Added` state
- `int DirtyCount` -- count of dirty items
- `int AddedCount`, `int ModifiedCount`, `int DeletedCount` -- per-state counts

Enhance [ObservableChanges.cs](c:\Users\f_ald\source\repos\The-Tech-Idea\BeepDM\DataManagementModelsStandard\Editor\ObservableChanges.cs):

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

- `**AcceptChanges()**` -- marks all items as `Unchanged`, clears `OriginalValues`, clears `ModifiedProperties`, clears `DeletedList`, clears `UpdateLog`, resets `Version` to 0
- `**RejectChanges()**` -- for each `Modified` item: restore properties from `OriginalValues`. For each `Added` item: remove from list. For each `Deleted` item: restore to list. Reset all states to `Unchanged`
- `**AcceptChanges(T item)**` -- per-item accept
- `**RejectChanges(T item)**` -- per-item revert

---

## Phase 2: Fix the 6 Critical Filter/Sort/Pagination Bugs

### The 6 Bugs

**BUG 1: Sort then Filter loses the sort**
`ApplySortCore` sorts only `Items`. Then `ApplyFilter` reads from unsorted `originalList`. The sort vanishes. Example: sort by Name ascending, then filter by City -- Items comes back in insertion order, not sorted by Name.

**BUG 2: `ApplySort(string)` permanently destroys insertion order**
Line 320: `originalList.Sort(comparer)` mutates `originalList` in place. After this, `RemoveSortCore()` calls `ResetItems(originalList)` but `originalList` is already sorted. The original insertion order is gone forever.

**BUG 3: Pagination ignores active filter**
`ApplyPaging()` does `originalList.Skip().Take()`. If a filter reduced 1000 items to 50, pagination still pages across all 1000. The user sees unfiltered pages.

**BUG 4: `_currentIndex` goes stale**
`ResetItems()` never touches `_currentIndex`. After filtering 100 items to 5, if `_currentIndex` was 75, `Current` returns `default(T)` (null) silently because `75 >= Items.Count(5)`.

**BUG 5: Three Sort methods behave inconsistently**

- `ApplySort(string, dir)` -- mutates `originalList`, updates tracking via ResetItems
- `ApplySortCore(prop, dir)` -- sorts `Items` in-place, does NOT touch `originalList`
- `Sort(string)` -- sorts `Items` in-place, does NOT call ResetItems, does NOT update tracking

Depending on which sort you call, subsequent filter/RemoveSort behave completely differently.

**BUG 6: No pipeline state**
No `_activeFilter`, `_activeSort`, or `_activePaging` state is stored. Each operation runs independently against `originalList`, unaware of the others. Operations cannot be reapplied after data changes.

### The Fix: `RefreshView()` Pipeline

**Principle**: `originalList` is NEVER sorted or filtered. It holds the true insertion-order master data. A single `RefreshView()` applies all active stages in sequence:

```
originalList (immutable insertion order)
    |
    |  Stage 1: Apply _activeFilterPredicate
    v
filteredList (intermediate, not stored)
    |
    |  Stage 2: Apply _activeSortProperty / _activeSortDirection
    v
sortedFilteredList (intermediate, not stored)
    |
    |  Stage 3: Apply _activePaging (Skip/Take)
    v
Items (the view consumers see via ResetItems)
    |
    |  Stage 4: Reset _currentIndex if stale
    v
CurrentChanged event fires
```

### New private state fields

```csharp
private Func<T, bool> _activeFilterPredicate;
private string _activeFilterString;
private PropertyDescriptor _activeSortProperty;
private ListSortDirection _activeSortDirection;
private ListSortDescriptionCollection _activeSortDescriptions;
private bool _isPagingActive;
```

### `RefreshView()` implementation

```csharp
private void RefreshView()
{
    SuppressNotification = true;
    RaiseListChangedEvents = false;

    IEnumerable<T> view = originalList;

    // Stage 1: Filter
    if (_activeFilterPredicate != null)
        view = view.Where(_activeFilterPredicate);

    var filteredList = view.ToList();
    FilteredCount = filteredList.Count;

    // Stage 2: Sort (single or multi-column)
    if (_activeSortDescriptions != null && _activeSortDescriptions.Count > 0)
    {
        // Multi-column sort via LINQ expressions
        filteredList = ApplyMultiSort(filteredList, _activeSortDescriptions);
    }
    else if (_activeSortProperty != null)
    {
        var comparer = new PropertyComparer<T>(_activeSortProperty, _activeSortDirection);
        filteredList.Sort(comparer);
        isSorted = true;
    }
    else
    {
        isSorted = false;
    }

    // Stage 3: Page
    IEnumerable<T> finalView = filteredList;
    if (_isPagingActive && PageSize > 0)
    {
        TotalPages = (int)Math.Ceiling((double)filteredList.Count / PageSize);
        if (CurrentPage > TotalPages) CurrentPage = Math.Max(1, TotalPages);
        finalView = filteredList.Skip((CurrentPage - 1) * PageSize).Take(PageSize);
    }

    ResetItems(finalView.ToList());

    // Stage 4: Fix CurrentIndex
    if (Items.Count == 0)
        _currentIndex = -1;
    else if (_currentIndex >= Items.Count)
        _currentIndex = 0;
    else if (_currentIndex < 0)
        _currentIndex = 0;

    ResetBindings();
    SuppressNotification = false;
    RaiseListChangedEvents = true;
    OnCurrentChanged();
}
```

### Refactored methods (all delegate to RefreshView)

- `ApplyFilter(predicate)` -- store `_activeFilterPredicate = predicate`, call `RefreshView()`
- `ApplyFilter()` (string) -- parse filterString into predicate, store, call `RefreshView()`
- `RemoveFilter()` -- clear `_activeFilterPredicate` and `_activeFilterString`, call `RefreshView()`
- `ApplySortCore(prop, dir)` -- store `_activeSortProperty` + `_activeSortDirection`, call `RefreshView()`
- `ApplySort(string, dir)` -- same as above, NO LONGER mutates originalList
- `ApplySort(ListSortDescriptionCollection)` -- store `_activeSortDescriptions`, call `RefreshView()`
- `Sort(string)` -- store sort state, call `RefreshView()`
- `RemoveSortCore()` -- clear all sort state, call `RefreshView()`
- `ApplyPaging()` -- set `_isPagingActive = true`, call `RefreshView()`
- `GoToPage(n)` -- update `CurrentPage`, call `RefreshView()`
- `SetPageSize(n)` -- update `PageSize`, reset to page 1, call `RefreshView()`

### New public properties

- `bool IsFiltered` -- `_activeFilterPredicate != null`
- `bool IsPaged` -- `_isPagingActive`
- `int TotalCount` -- `originalList.Count`
- `int FilteredCount` -- count after filter, before pagination

### Events

- `FilterApplied` / `FilterRemoved`
- `SortApplied` / `SortRemoved`

---

## Phase 3: Current/Index Position Management and Events

### 3A. Fix Current/Index

**Design Rule**: Index is the single source of truth. `Current` is ALWAYS read-only, derived from `_currentIndex`.

```
CurrentIndex (settable) --> _currentIndex (backing field) --> Current (read-only, derived)
MoveToItem(T item)      --> finds index via IndexOf         --> calls MoveTo(index)
```

Changes:

- **Keep `Current` read-only** -- no setter
- **Make `CurrentIndex` settable** -- `set => MoveTo(value)`
- **Add `MoveToItem(T item)**` -- `int idx = Items.IndexOf(item); if (idx >= 0) MoveTo(idx);`
- **Remove silent cursor-jump** from `OnListChanged` (line 1473-1476). Property edits must NOT move `_currentIndex`.

### 3B. BOF/EOF Cursor Semantics

- `bool IsAtBOF` -- `_currentIndex <= 0 || Count == 0`
- `bool IsAtEOF` -- `_currentIndex >= Count - 1 || Count == 0`
- `bool IsEmpty` -- `Count == 0`
- `bool IsPositionValid` -- `_currentIndex >= 0 && _currentIndex < Count`

### 3C. CurrentChanging Event

New event that fires BEFORE position changes, with cancel support:

```csharp
public event EventHandler<CurrentChangingEventArgs> CurrentChanging;
```

`CurrentChangingEventArgs`: `OldIndex`, `NewIndex`, `OldItem`, `NewItem`, `Cancel`

All Move methods and `SetPosition` check `Cancel` before applying.

### 3D. Enhanced Event Model

New events:

- `BatchOperationStarted` / `BatchOperationCompleted` -- for `AddRange` / `RemoveRange`
- `BeforeSave` / `AfterSave` -- around commit operations
- `FilterApplied` / `FilterRemoved` (also in Phase 2)
- `SortApplied` / `SortRemoved` (also in Phase 2)

New EventArgs in [ObservableBindingListEventArgs.cs](c:\Users\f_ald\source\repos\The-Tech-Idea\BeepDM\DataManagementModelsStandard\Editor\ObservableBindingListEventArgs.cs):

- `CurrentChangingEventArgs` -- `OldIndex`, `NewIndex`, `OldItem`, `NewItem`, `Cancel`
- `BatchOperationEventArgs` -- `OperationType` (AddRange/RemoveRange), `ItemCount`
- `CommitEventArgs<T>` -- `Item`, `EntityState`, `Cancel`

---

## Phase 4: Validation Framework

### 4A. Data Annotations Support

Read standard .NET Data Annotations from `T` properties at startup (cached):

- `[Required]` -- field must not be null/empty
- `[MaxLength(n)]` / `[MinLength(n)]` -- string length
- `[Range(min, max)]` -- numeric range
- `[RegularExpression(pattern)]` -- regex match
- `[EmailAddress]`, `[Phone]`, `[Url]` -- format validators
- `[CustomValidation]` -- custom method

Auto-validate on property change (opt-in via `bool IsAutoValidateEnabled`). Block commit for invalid items.

### 4B. Validation Result Framework

New classes (add to existing or new file in Editor/):

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

- `Func<T, ValidationResult> CustomValidator { get; set; }` -- pluggable custom validation
- `ValidationResult Validate(T item)` -- runs Data Annotations + CustomValidator
- `ValidationResult ValidateAll()` -- validates every item
- `List<ValidationError> GetErrors(T item)` -- errors for specific item
- `bool IsValid` -- true when all items pass validation
- `event EventHandler<ValidationEventArgs<T>> ValidationFailed`

---

## Phase 5: Commit and Master-Detail

### 5A. Batch Commit

- `CommitAllAsync(insertAsync, updateAsync, deleteAsync)` -- iterates all pending changes in order: Deletes first, then Updates, then Inserts
- `CommitAllAsync(..., CommitOrder order)` -- configurable order enum
- Returns `CommitResult` with `List<CommitItemResult>` (per-item success/failure/error)
- Fires `BeforeSave` / `AfterSave` events
- On success, calls `AcceptChanges()` for committed items

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
- `List<object> DetailLists` -- registered child lists
- Internal: on `CurrentChanged`, iterate detail lists and apply master-key filter via their `RefreshView()`

This integrates with the Phase 2 `RefreshView()` pipeline -- master-detail is just another filter source.

---

## Phase 6: Computed Columns, Bookmarks, Lazy Loading

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

- `void SetBookmark(string name)` -- saves `_currentIndex` under the name
- `bool GoToBookmark(string name)` -- restores position, returns false if bookmark invalid
- `void RemoveBookmark(string name)`
- `IReadOnlyDictionary<string, int> Bookmarks` -- all saved bookmarks
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

- `void SetDataProvider(Func<int, int, Task<List<T>>> provider)` -- page loader callback
- `void SetTotalItemCount(int count)` -- total rows available server-side
- `bool IsVirtualMode` -- true when a data provider is set
- When `GoToPage(n)` is called in virtual mode, it calls the provider instead of `originalList.Skip().Take()`
- Caching: keep a configurable number of pages in memory (default: 3 -- current, previous, next)

---

## Phase 7: Undo/Redo, Thread Safety, Freeze, Aggregates

### 7A. Undo/Redo

New internal class `UndoRedoManager<T>`:

- Stores `UndoAction` records: `ActionType` (PropertyChange/Insert/Remove), `Item`, `PropertyName`, `OldValue`, `NewValue`, `Index`
- `Undo()` / `Redo()` methods
- `bool CanUndo`, `bool CanRedo` properties
- Configurable `int MaxUndoDepth` (default 50)
- Opt-in: `bool IsUndoEnabled` (default false, zero cost when off)

### 7B. Thread Safety

- `private readonly ReaderWriterLockSlim _rwLock`
- `bool IsThreadSafe { get; set; }` -- opt-in (default false)
- When enabled, wrap mutations in `_rwLock.EnterWriteLock()`, reads in `_rwLock.EnterReadLock()`
- Zero overhead when `IsThreadSafe == false` (no lock calls)

### 7C. Read-Only / Freeze Mode

- `bool IsFrozen` -- when true, all mutations throw `InvalidOperationException`
- `void Freeze()` / `void Unfreeze()`
- `IDisposable BeginBatchUpdate()` -- suppresses all notifications during batch, fires single `Reset` on dispose

### 7D. Aggregate / Summary Support

```csharp
decimal total = list.Sum("Price");
decimal avg = list.Average("Rating");
object earliest = list.Min("CreatedDate");
object latest = list.Max("CreatedDate");
int active = list.CountWhere(x => x.IsActive);
var groups = list.GroupBy("Category");
```

All aggregates operate on the current `Items` view (respecting active filter/sort/page).

---

## File Change Summary

- **[ObservableBindingList.cs](c:\Users\f_ald\source\repos\The-Tech-Idea\BeepDM\DataManagementModelsStandard\Editor\ObservableBindingList.cs)** -- Major: RefreshView pipeline, Current/Index fix, snapshots, AcceptChanges/RejectChanges, validation, master-detail, computed columns, bookmarks, lazy loading, undo/redo, thread safety, freeze, aggregates, batch commit, all new events
- **[Tracking.cs](c:\Users\f_ald\source\repos\The-Tech-Idea\BeepDM\DataManagementModelsStandard\Editor\Tracking.cs)** -- Add OriginalValues, ModifiedAt, ModifiedBy, Version, ModifiedProperties, IsDirty
- **[EntityState.cs](c:\Users\f_ald\source\repos\The-Tech-Idea\BeepDM\DataManagementModelsStandard\Editor\EntityState.cs)** -- Add `Detached` state
- **[ObservableBindingListEventArgs.cs](c:\Users\f_ald\source\repos\The-Tech-Idea\BeepDM\DataManagementModelsStandard\Editor\ObservableBindingListEventArgs.cs)** -- Add CurrentChangingEventArgs, BatchOperationEventArgs, CommitEventArgs, ValidationEventArgs
- **[ObservableChanges.cs](c:\Users\f_ald\source\repos\The-Tech-Idea\BeepDM\DataManagementModelsStandard\Editor\ObservableChanges.cs)** -- Add HasChanges, TotalCount properties
- **New: `ValidationResult.cs**` -- ValidationResult, ValidationError, ValidationSeverity
- **New: `UndoRedoManager.cs**` -- Undo/redo stack with UndoAction records

---

## Implementation Priority Order

1. **Phase 1: Tracking and Dirty State** (1A -> 1B -> 1C -> 1D) -- Foundation for everything
2. **Phase 2: Fix 6 Filter/Sort/Page Bugs** -- RefreshView() pipeline
3. **Phase 3: Current/Index + Events** -- Position management, BOF/EOF, CurrentChanging
4. **Phase 4: Validation** -- Data Annotations + custom validators
5. **Phase 5: Commit + Master-Detail** -- Batch operations and parent-child
6. **Phase 6: Computed Columns, Bookmarks, Lazy Loading** -- Advanced features
7. **Phase 7: Undo/Redo, Thread Safety, Freeze, Aggregates** -- Final polish

