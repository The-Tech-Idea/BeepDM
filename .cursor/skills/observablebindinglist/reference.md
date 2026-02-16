````markdown
# ObservableBindingList<T> Complete Reference

## Constructors

```csharp
var list = new ObservableBindingList<Customer>();                         // Empty
var list = new ObservableBindingList<Customer>(enumerable);               // From IEnumerable<T>
var list = new ObservableBindingList<Customer>(iList);                    // From IList<T>
var list = new ObservableBindingList<Customer>(bindingListView);          // From IBindingListView
var list = new ObservableBindingList<Customer>(dataTable);                // From DataTable
var list = new ObservableBindingList<Customer>(objectList);               // From List<object>
```

## Properties — Core / Pipeline

| Property | Type | Description |
|----------|------|-------------|
| `SuppressNotification` | `bool` | Suppress change notifications |
| `IsSorted` | `bool` (get) | Whether list is sorted |
| `IsSynchronized` | `bool` (get) | Always false |
| `IsFiltered` | `bool` (get) | Whether filter is active |
| `IsPaged` | `bool` (get) | Whether pagination is active |
| `TotalCount` | `int` (get) | Total items in master list |
| `FilteredCount` | `int` (get) | Count after filtering |
| `IsLogging` | `bool` | Enable audit logging |
| `CurrentUser` | `string` | User identity for tracking |
| `Trackings` | `List<Tracking>` | All tracking records |
| `UpdateLog` | `Dictionary<Guid, EntityUpdateInsertLog>` | Change audit log |

## Properties — Navigation

| Property | Type | Description |
|----------|------|-------------|
| `CurrentIndex` | `int` | Current position (get/set) |
| `Current` | `T` (get) | Item at current position |
| `IsAtBOF` | `bool` (get) | At beginning |
| `IsAtEOF` | `bool` (get) | At end |
| `IsEmpty` | `bool` (get) | No items |
| `IsPositionValid` | `bool` (get) | Valid position |

## Properties — Tracking

| Property | Type | Description |
|----------|------|-------------|
| `HasChanges` | `bool` (get) | Any tracked changes |
| `DirtyItems` | `List<T>` (get) | Modified or Added items |
| `DirtyCount` | `int` (get) | Count of dirty items |
| `AddedCount` | `int` (get) | Added item count |
| `ModifiedCount` | `int` (get) | Modified item count |
| `DeletedCount` | `int` (get) | Deleted item count |
| `AddedItems` | `IReadOnlyList<T>` (get) | Items with Added state |
| `DeletedItems` | `IReadOnlyList<T>` (get) | Items in DeletedList |

## Properties — Validation

| Property | Type | Description |
|----------|------|-------------|
| `IsAutoValidateEnabled` | `bool` | Auto-validate on change |
| `BlockCommitOnValidationError` | `bool` | Block commit if errors |
| `CustomValidator` | `Func<T, ValidationResult>` | Custom validation function |
| `IsAllValid` | `bool` (get) | All items pass validation |

## Properties — Undo/Redo

| Property | Type | Description |
|----------|------|-------------|
| `IsUndoEnabled` | `bool` | Enable undo/redo |
| `MaxUndoDepth` | `int` | Max undo actions (default: 50) |
| `CanUndo` | `bool` (get) | Undo available |
| `CanRedo` | `bool` (get) | Redo available |

## Properties — Computed

| Property | Type | Description |
|----------|------|-------------|
| `ComputedColumnNames` | `IReadOnlyCollection<string>` (get) | Registered columns |

## Properties — Bookmarks

| Property | Type | Description |
|----------|------|-------------|
| `Bookmarks` | `IReadOnlyDictionary<string, int>` (get) | Saved bookmarks |

## Properties — Thread Safety / Freeze

| Property | Type | Description |
|----------|------|-------------|
| `IsThreadSafe` | `bool` | Enable reader-writer locks |
| `IsFrozen` | `bool` (get) | Whether list is frozen |

## Properties — Virtual Loading

| Property | Type | Description |
|----------|------|-------------|
| `IsVirtualMode` | `bool` (get) | Data provider set |
| `PageCacheSize` | `int` | Pages to cache (default: 3) |
| `VirtualTotalPages` | `int` (get) | Total pages in virtual mode |

## Properties — Master-Detail

| Property | Type | Description |
|----------|------|-------------|
| `DetailLists` | `IReadOnlyList<object>` (get) | Child lists |

## Properties — Pagination

| Property | Type | Description |
|----------|------|-------------|
| `PageSize` | `int` (get) | Items per page (default: 20) |
| `CurrentPage` | `int` (get) | Current page (1-based) |
| `TotalPages` | `int` (get) | Total pages |

## Properties — Sort

| Property | Type | Description |
|----------|------|-------------|
| `SortDescriptions` | `ListSortDescriptionCollection` (get) | Multi-column sorts |
| `SupportsAdvancedSorting` | `bool` (get) | Always true |
| `SortDirection` | `ListSortDirection` | Current sort direction |
| `SupportsFiltering` | `bool` (get) | Always true |
| `Filter` | `string` | IBindingListView string filter |

---

## Methods — CRUD

```csharp
list.AddNew(item);                    // Add existing item
list.AddNew();                         // Add new default item
list.AddRange(items);                  // Add multiple (single notification)
list.RemoveRange(items);               // Remove multiple (single notification)
list.RemoveAll(c => c.Inactive);       // Remove all matching
```

## Methods — Navigation

```csharp
bool moved = list.MoveNext();
bool moved = list.MovePrevious();
bool moved = list.MoveFirst();
bool moved = list.MoveLast();
bool moved = list.MoveTo(5);
bool moved = list.MoveToItem(entity);
list.SetPosition(10);                  // No CurrentChanging event
```

## Methods — Tracking

```csharp
// Query state
int origIdx = list.GetOriginalIndex(item);
T item = list.GetItem();                          // At current position
T item = list.GetItemFromOriginalList(index);     // From master list
T item = list.GetItemFromCurrentList(index);      // From current view
Tracking t = list.GetTrackingItem(item);

// Dirty queries
bool dirty = list.IsDirty(item);
ObservableChanges<T> pending = list.GetPendingChanges();
Dictionary<string, (object, object)> changes = list.GetChanges(item);
object orig = list.GetOriginalValue(item, "Name");
T origItem = list.GetOriginalItem(item);

// Accept / Reject
list.AcceptChanges();                   // All
list.AcceptChanges(item);               // Single
list.RejectChanges();                   // All
list.RejectChanges(item);              // Single

// Commit helpers
list.MarkAsCommitted(item);            // Mark single as saved
list.ResetAfterCommit();               // Reset all tracking
```

## Methods — Commit

```csharp
// Single item
var result = await list.CommitItemAsync(item, insertFn, updateFn, deleteFn);

// All items (default: DeletesFirst)
var commitResult = await list.CommitAllAsync(insertFn, updateFn, deleteFn);

// With explicit order
var commitResult = await list.CommitAllAsync(insertFn, updateFn, deleteFn, CommitOrder.InsertsFirst);
```

## Methods — Validation

```csharp
ValidationResult r = list.Validate(item);
ValidationResult r = list.ValidateProperty(item, "Name");
ValidationResult r = list.ValidateAll();
List<ValidationError> e = list.GetErrors(item);
List<ValidationError> e = list.GetErrors(item, "Name");
List<T> invalid = list.GetInvalidItems();
list.ClearValidationCache();
list.ClearValidationCache(item);
```

## Methods — Undo/Redo

```csharp
bool undone = list.Undo();
bool redone = list.Redo();
list.ClearUndoHistory();
```

## Methods — Computed Columns

```csharp
list.RegisterComputed("FullName", c => $"{c.First} {c.Last}");
list.UnregisterComputed("FullName");
object val = list.GetComputed(item, "FullName");
Dictionary<string, object> all = list.GetAllComputed(item);
list.ClearComputedCache();
```

## Methods — Bookmarks

```csharp
list.SetBookmark("start");
list.SetBookmark("section2", 42);
bool found = list.GoToBookmark("start");
list.RemoveBookmark("start");
list.ClearBookmarks();
```

## Methods — Thread Safety

```csharp
using (list.EnterReadLock()) { /* read ops */ }
using (list.EnterWriteLock()) { /* write ops */ }
TResult r = list.ReadLocked(() => list.Count);
list.WriteLocked(() => list.Clear());
```

## Methods — Freeze / Batch

```csharp
list.Freeze();
list.Unfreeze();
using (list.BeginBatchUpdate()) { /* bulk ops, single notification */ }
```

## Methods — Aggregates

```csharp
decimal sum = list.Sum("Amount");
decimal cSum = list.SumWhere("Amount", c => c.Paid);
decimal avg = list.Average("Price");
decimal cAvg = list.AverageWhere("Price", c => c.Category == "A");
object min = list.Min("Price");
object max = list.Max("Price");
int count = list.CountWhere(c => c.Active);
Dictionary<object, List<T>> groups = list.GroupBy("Category");
Dictionary<object, List<T>> cGroups = list.GroupByWhere("Category", c => c.Active);
List<object> distinct = list.DistinctValues("City");
```

## Methods — Virtual Loading

```csharp
list.SetDataProvider(async (page, size) => await LoadPageAsync(page, size));
list.SetTotalItemCount(50000);
await list.GoToPageAsync(10);
await list.PrefetchAdjacentPagesAsync();
list.InvalidatePageCache();
list.ClearDataProvider();
```

## Methods — Master-Detail

```csharp
list.RegisterDetail(childList, "OrderId", "OrderId");
list.UnregisterDetail(childList);
list.UnregisterAllDetails();
```

## Methods — Filter

```csharp
list.ApplyFilter(c => c.Active);                       // Predicate
list.ApplyFilter("Status", "Active", "==");            // Property-value
list.RemoveFilter();
```

## Methods — Sort

```csharp
list.ApplySort("Name", ListSortDirection.Ascending);   // Single column
list.ApplySort(sortDescriptions);                       // Multi-column
list.Sort("Name");                                      // Using current direction
list.RemoveSort();
```

## Methods — Pagination

```csharp
list.SetPageSize(25);
list.GoToPage(3);
```

## Methods — Find / Search

```csharp
T item = list.FirstOrDefault(c => c.Id == 42);
bool exists = list.Any(c => c.Active);
int idx = list.FindIndex(c => c.Name == "Alice");

List<T> results = list.Search(c => c.Active);
List<T> results = list.SearchWithProgress(c => c.Active, progress);

T found = list.Find(c => c.Name == "Alice");          // Expression
T found = list.Find("Name", "Alice");                  // Property-value

List<T> contains = list.WhereContains("Name", "ali", ignoreCase: true);

List<T> multi = list.SearchByProperties(
    new Dictionary<string, object> { ["City"] = "NYC" }, "AND"
);

List<T> text = list.SearchByText("Name", "ali", "Contains", true);
List<T> allProps = list.SearchAllProperties("alice");
List<T> adv = list.AdvancedSearch("Name=Alice;City=NYC", ';', "AND");

int matchCount = list.FindAndFilter(c => c.Active);   // Filter + return count
```

## Methods — Export

```csharp
DataTable dt = list.ToDataTable("Customers");
```

## Methods — Bindings

```csharp
list.ResetBindings();   // Fire ListChanged Reset
```

## Methods — Dispose

```csharp
list.Dispose();         // Unhook all handlers, clear state
```

---

## Events

| Event | Type | Category |
|-------|------|----------|
| `PropertyChanged` | `PropertyChangedEventHandler` | Core |
| `CollectionChanged` | `NotifyCollectionChangedEventHandler` | Core |
| `ItemAdded` | `EventHandler<ItemAddedEventArgs<T>>` | CRUD |
| `ItemRemoved` | `EventHandler<ItemRemovedEventArgs<T>>` | CRUD |
| `ItemChanged` | `EventHandler<ItemChangedEventArgs<T>>` | CRUD |
| `ItemValidating` | `EventHandler<ItemValidatingEventArgs<T>>` | Validation |
| `ItemDeleting` | `EventHandler<ItemValidatingEventArgs<T>>` | Validation |
| `ValidationFailed` | `EventHandler<ValidationEventArgs<T>>` | Validation |
| `CurrentChanging` | `EventHandler<CurrentChangingEventArgs>` | Navigation |
| `CurrentChanged` | `EventHandler` | Navigation |
| `SearchCompleted` | `EventHandler<SearchCompletedEventArgs<T>>` | Search |
| `FilterApplied` | `EventHandler` | Pipeline |
| `FilterRemoved` | `EventHandler` | Pipeline |
| `SortApplied` | `EventHandler` | Pipeline |
| `SortRemoved` | `EventHandler` | Pipeline |
| `BatchOperationStarted` | `EventHandler<BatchOperationEventArgs>` | Batch |
| `BatchOperationCompleted` | `EventHandler<BatchOperationEventArgs>` | Batch |
| `BeforeSave` | `EventHandler<CommitEventArgs<T>>` | Commit |
| `AfterSave` | `EventHandler<CommitEventArgs<T>>` | Commit |

---

## Best Practices

1. **Always use `using` with Dispose** — unhooks all handlers
2. **BeginBatchUpdate for bulk ops** — avoids O(n) notifications
3. **Check HasChanges before commit** — avoid empty commits
4. **Use AcceptChanges after successful commit** — clears tracking
5. **Use RejectChanges to rollback** — reverts to original snapshots
6. **Set IsThreadSafe only when needed** — adds lock overhead
7. **Freeze for read-only views** — prevents accidental mutations
8. **Use AdvancedSearch for complex queries** — supports nested property paths
9. **Set PageCacheSize for virtual mode** — balances memory vs. latency
10. **Register CustomValidator early** — before adding items to get immediate feedback
````
