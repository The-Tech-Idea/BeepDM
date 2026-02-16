````skill
---
name: observablebindinglist
description: Guidance for ObservableBindingList<T> — the unified collection engine powering change tracking, validation, undo/redo, filtering, sorting, pagination, virtual loading, master-detail, and more in BeepDM.
---

# ObservableBindingList<T> Pattern Guide

Use this skill when working with ObservableBindingList<T> directly, or when understanding the collection engine that backs UnitofWork<T>.

## Architecture

ObservableBindingList<T> extends BindingList<T> with a rich feature set organized as 24 partial class files. It is the **single source of truth** for all tracked collection state in BeepDM.

```
BindingList<T>
      |
ObservableBindingList<T>
  +- Core / Pipeline State
  +- CRUD (AddNew, AddRange, RemoveRange, RemoveAll)
  +- Navigation (Current, Move*, SetPosition)
  +- Tracking (Dirty, Added/Modified/Deleted, Snapshots)
  +- Commit (CommitItemAsync, CommitAllAsync)
  +- Validation (Annotations + CustomValidator)
  +- Undo/Redo (per-action stack)
  +- Computed Columns
  +- Bookmarks
  +- Thread Safety / Freeze / Batch Update
  +- Aggregates (Sum, Average, Min, Max, GroupBy, etc.)
  +- Virtual Loading (paged data provider)
  +- Master-Detail (FK-linked child lists)
  +- Filter / Sort / Pagination
  +- Find / Search (multi-strategy)
  +- Export (ToDataTable)
  +- Logging
```

## Class Declaration

```csharp
public partial class ObservableBindingList<T> : BindingList<T>,
    IBindingListView, INotifyCollectionChanged, IDisposable
    where T : class, INotifyPropertyChanged, new()
```

## Core Capabilities
- Full change tracking with per-item state (Added, Modified, Deleted, Unchanged)
- Original value snapshots for RejectChanges/diff
- Async commit engine with configurable commit order (DeletesFirst, InsertsFirst, AsTracked)
- Data Annotations + custom validation with auto-validate option
- Per-action undo/redo stacks
- Computed columns with caching
- Position bookmarks
- Thread-safe reader-writer locks
- Freeze (read-only) mode
- Batch update with notification suppression
- Aggregate functions (Sum, Average, Min, Max, Count, GroupBy, Distinct)
- Virtual/lazy loading via async data provider
- Master-detail FK synchronization
- Predicate/property/string-based filtering
- Multi-column sorting
- Server-side and client-side pagination
- Multi-strategy search (Contains, StartsWith, EndsWith, multi-property, AdvancedSearch)
- DataTable export
- Audit logging with DateTime or Guid keys

## File Locations (24 partial files)

All in `DataManagementModelsStandard/Editor/`:
- `ObservableBindingList.cs` — Core fields, base overrides, pipeline
- `ObservableBindingList.Constructors.cs` — 6 constructors
- `ObservableBindingList.CRUD.cs` — AddNew, AddRange, RemoveRange, RemoveAll
- `ObservableBindingList.CurrentAndMovement.cs` — Navigation, Current, Move*
- `ObservableBindingList.Tracking.cs` — Dirty state, snapshots, Accept/Reject
- `ObservableBindingList.Validation.cs` — Validate, GetErrors, CustomValidator
- `ObservableBindingList.UndoRedo.cs` — Undo/Redo stacks
- `ObservableBindingList.Computed.cs` — Computed columns
- `ObservableBindingList.Bookmarks.cs` — Position bookmarks
- `ObservableBindingList.ThreadSafety.cs` — Locks, Freeze, Batch
- `ObservableBindingList.Aggregates.cs` — Sum, Average, Min, Max, GroupBy, etc.
- `ObservableBindingList.VirtualLoading.cs` — Paged data provider
- `ObservableBindingList.MasterDetail.cs` — FK-linked detail lists
- `ObservableBindingList.Filter.cs` — Predicate/property filtering
- `ObservableBindingList.Sort.cs` — Single/multi-column sorting
- `ObservableBindingList.Pagination.cs` — Client-side paging
- `ObservableBindingList.Find.cs` — Find by predicate/property
- `ObservableBindingList.Search.cs` — Multi-strategy search
- `ObservableBindingList.ListChanges.cs` — List change event handling
- `ObservableBindingList.Logging.cs` — Audit logging
- `ObservableBindingList.Export.cs` — DataTable export
- `ObservableBindingList.Utilities.cs` — Helper methods
- `ObservableBindingListEventArgs.cs` — Custom EventArgs types

## Pitfalls
- `IsLoggin` property is `[Obsolete]` — use `IsLogging` instead.
- `GetItemFroCurrentList()` is `[Obsolete]` — use `GetItemFromCurrentList()`.
- Calling `AcceptChanges()` clears all tracking — call only after successful commit.
- `BeginBatchUpdate()` returns `IDisposable` — always use in a `using` block.
- `ApplyFilter(null)` removes the filter.
- Freeze prevents all mutations — `InvalidOperationException` on Add/Remove while frozen.
- Virtual mode requires `SetDataProvider()` and `SetTotalItemCount()` before `GoToPageAsync()`.
- Master-detail FK sync happens on `CurrentChanged` — ensure correct master position before querying detail.

## Examples

### Basic Usage
```csharp
var list = new ObservableBindingList<Customer>();
list.AddNew(new Customer { Name = "Alice" });
list.AddRange(customers);

var current = list.Current;
list.MoveNext();
list.MoveTo(5);
```

### Change Tracking
```csharp
list.AddNew(item);                     // Tracked as Added
item.Name = "Changed";                 // Tracked as Modified (via PropertyChanged)
list.Remove(item);                     // Tracked as Deleted

bool dirty = list.HasChanges;
int dirtyCount = list.DirtyCount;
var pending = list.GetPendingChanges();  // ObservableChanges<T>

// Diff a single item
var changes = list.GetChanges(item);    // Dict<string, (Original, Current)>
object orig = list.GetOriginalValue(item, "Name");
T origItem = list.GetOriginalItem(item);

// Accept or reject
list.AcceptChanges();                   // Mark all Unchanged
list.RejectChanges();                   // Revert all to original
list.AcceptChanges(singleItem);         // Accept one
list.RejectChanges(singleItem);         // Revert one
```

### Commit Engine
```csharp
var result = await list.CommitAllAsync(
    insertAsync: async item => await ds.InsertEntityAsync(...),
    updateAsync: async item => await ds.UpdateEntityAsync(...),
    deleteAsync: async item => await ds.DeleteEntityAsync(...)
);

if (result.AllSucceeded)
    list.ResetAfterCommit();
```

### Validation
```csharp
list.IsAutoValidateEnabled = true;
list.BlockCommitOnValidationError = true;

// Custom validator (plugged by UOW for EntityStructure-based validation)
list.CustomValidator = item =>
{
    if (string.IsNullOrEmpty(item.Name))
        return new ValidationResult(false, new[] { new ValidationError("Name", "Required") });
    return ValidationResult.Success;
};

var result = list.ValidateAll();
var errors = list.GetErrors(item);
var invalid = list.GetInvalidItems();
```

### Undo/Redo
```csharp
list.IsUndoEnabled = true;
list.MaxUndoDepth = 100;

item.Name = "Changed";
list.Undo();              // Reverts the property change
list.Redo();              // Re-applies it
list.ClearUndoHistory();
```

### Computed Columns
```csharp
list.RegisterComputed("FullName", c => $"{c.First} {c.Last}");
list.RegisterComputed("Total", c => c.Price * c.Qty);

var name = list.GetComputed(item, "FullName");
var all = list.GetAllComputed(item);
list.ClearComputedCache();
```

### Bookmarks
```csharp
list.SetBookmark("start");
list.SetBookmark("section2", 42);    // At specific index
list.MoveLast();
list.GoToBookmark("start");
list.RemoveBookmark("start");
list.ClearBookmarks();
```

### Thread Safety
```csharp
list.IsThreadSafe = true;

using (list.EnterReadLock())
    var item = list[0];

using (list.EnterWriteLock())
    list.Add(new Customer());

var result = list.ReadLocked(() => list.Count);
list.WriteLocked(() => list.Clear());
```

### Freeze / Batch
```csharp
list.Freeze();
// list.Add(...) would throw InvalidOperationException
list.Unfreeze();

using (list.BeginBatchUpdate())
{
    for (int i = 0; i < 10000; i++)
        list.AddNew(new Customer { Name = $"C{i}" });
}
// Single ListChanged notification after batch ends
```

### Aggregates
```csharp
decimal sum = list.Sum("Amount");
decimal condSum = list.SumWhere("Amount", o => o.Paid);
decimal avg = list.Average("Price");
object min = list.Min("Price");
object max = list.Max("Price");
int count = list.CountWhere(c => c.Active);
var groups = list.GroupBy("Category");
var groupsFiltered = list.GroupByWhere("Category", c => c.Active);
var distinct = list.DistinctValues("City");
```

### Filter / Sort / Pagination
```csharp
// Predicate filter
list.ApplyFilter(c => c.Active && c.City == "NYC");
list.RemoveFilter();

// Property-value filter
list.ApplyFilter("Status", "Active", "==");

// Sort
list.ApplySort("Name", ListSortDirection.Ascending);
list.ApplySort(multiSortDescriptions);
list.Sort("Name");
list.RemoveSort();

// Pagination
list.SetPageSize(25);
list.GoToPage(3);
int totalPages = list.TotalPages;
int currentPage = list.CurrentPage;
```

### Find / Search
```csharp
var item = list.FirstOrDefault(c => c.Id == 42);
bool exists = list.Any(c => c.Email.Contains("@"));
int idx = list.FindIndex(c => c.Name == "Alice");

var results = list.Search(c => c.Active);
var withProgress = list.SearchWithProgress(c => c.Active, progress);

var found = list.Find("Name", "Alice");
var contains = list.WhereContains("Name", "ali", ignoreCase: true);

// Multi-property search
var multi = list.SearchByProperties(
    new Dictionary<string, object> { ["City"] = "NYC", ["Active"] = true },
    matchOperator: "AND"
);

// Text search
var text = list.SearchByText("Name", "ali", "Contains", ignoreCase: true);

// All string properties
var allProps = list.SearchAllProperties("alice");

// Advanced filter syntax
var adv = list.AdvancedSearch("Name=Alice;City=NYC", separator: ';', logicalOperator: "AND");

// Find and update visible view
int matchCount = list.FindAndFilter(c => c.Active);
```

### Virtual / Lazy Loading
```csharp
// Set provider
list.SetDataProvider(async (page, size) =>
{
    return await dataSource.GetPageAsync(page, size);
});
list.SetTotalItemCount(50000);
list.PageCacheSize = 5;

// Navigate
await list.GoToPageAsync(10);
await list.PrefetchAdjacentPagesAsync();
list.InvalidatePageCache();

// Clean up
list.ClearDataProvider();
```

### Master-Detail
```csharp
var orders = new ObservableBindingList<Order>(orderData);
var lines = new ObservableBindingList<OrderLine>(lineData);

orders.RegisterDetail(lines, "OrderId", "OrderId");
// Moving orders auto-filters lines by FK match

var details = orders.DetailLists;    // IReadOnlyList<object>
orders.UnregisterDetail(lines);
orders.UnregisterAllDetails();
```

### Export
```csharp
DataTable dt = list.ToDataTable("Customers");
```

### Events
```csharp
// CRUD events
list.ItemAdded += (s, e) => Console.WriteLine($"Added: {e.Item}");
list.ItemRemoved += (s, e) => Console.WriteLine($"Removed: {e.Item}");
list.ItemChanged += (s, e) => Console.WriteLine($"Changed: {e.PropertyName}");

// Navigation
list.CurrentChanging += (s, e) => { /* e.Cancel = true; */ };
list.CurrentChanged += (s, e) => { };

// Validation
list.ItemValidating += (s, e) => { };
list.ItemDeleting += (s, e) => { };
list.ValidationFailed += (s, e) => Console.WriteLine(e.Result);

// Pipeline
list.FilterApplied += (s, e) => { };
list.FilterRemoved += (s, e) => { };
list.SortApplied += (s, e) => { };
list.SortRemoved += (s, e) => { };

// Commit
list.BeforeSave += (s, e) => { };
list.AfterSave += (s, e) => { };
list.BatchOperationStarted += (s, e) => { };
list.BatchOperationCompleted += (s, e) => { };

// Search
list.SearchCompleted += (s, e) => Console.WriteLine($"Found {e.Results.Count}");

// Standard
list.CollectionChanged += (s, e) => { };
list.PropertyChanged += (s, e) => { };
```

### Logging
```csharp
list.IsLogging = true;
list.CurrentUser = "admin";
var log = list.UpdateLog;   // Dictionary<Guid, EntityUpdateInsertLog>
```
````
