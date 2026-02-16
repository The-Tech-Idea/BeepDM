````markdown
# UnitOfWork<T> Complete Reference

## Initialization

```csharp
// Standard with primary key
using var uow = new UnitofWork<Customer>(editor, "MyDatabase", "Customers", "Id");

// With EntityStructure
using var uow = new UnitofWork<Customer>(editor, "MyDatabase", "Customers", entityStructure, "Id");

// List mode (in-memory only, no DataSource)
using var uow = new UnitofWork<Customer>(editor, isInListMode: true, initialData, "Id");

// Without primary key (auto-detected from EntityStructure)
using var uow = new UnitofWork<Customer>(editor, "MyDatabase", "Customers");
```

## CRUD Operations

### Create
```csharp
uow.New();                          // Create new entity with defaults
uow.CurrentItem.Name = "John";
uow.Add(customer);                  // Add existing entity
await uow.AddRange(customers);      // Add multiple entities
```

### Read
```csharp
var all = await uow.Get();                            // All entities
var filtered = await uow.Get(filters);                 // With AppFilter list
var byQuery = await uow.GetQuery("SELECT ...");        // Raw query
var byId = uow.Get("123");                             // By PK string
var byIndex = uow.Get(0);                              // By index
var byKey = uow.Read("123");                           // By PK string
var byPred = uow.Read(c => c.Email == "j@mail.com");   // By predicate
var multi = await uow.MultiRead(c => c.Active);        // Multiple by predicate
```

### Update
```csharp
uow.Update(customer);                                 // By PK match
uow.Update("123", updatedCustomer);                   // By ID
uow.Update(c => c.Email == "j@mail.com", updated);    // By predicate
await uow.UpdateAsync(customer);                       // Async to DataSource
await uow.UpdateRange(customers);                      // Multiple
uow.UpdateDoc(customer);                               // Direct to DataSource
```

### Delete
```csharp
uow.Delete(customer);                     // Entity
uow.Delete("123");                         // By ID
uow.Delete();                              // Current item
uow.Delete(c => c.Status == "Inactive");   // By predicate
await uow.DeleteAsync(customer);           // Async
await uow.DeleteRange(customers);          // Multiple
uow.DeleteDoc(customer);                   // Direct to DataSource
```

### Direct DataSource Insert
```csharp
await uow.InsertAsync(customer);    // Insert async to DataSource
uow.InsertDoc(customer);            // Insert sync to DataSource
```

## Transaction Management

```csharp
// Commit all tracked changes
var result = await uow.Commit();
if (result.Flag != Errors.Ok)
    Console.WriteLine(result.Message);

// Commit with progress and cancellation
await uow.Commit(progress, cancellationToken);

// Control commit order
uow.CommitOrder = CommitOrder.DeletesFirst;   // Default
uow.CommitOrder = CommitOrder.InsertsFirst;   // FK dependencies
uow.CommitOrder = CommitOrder.AsTracked;       // In tracking order

// Rollback all pending changes
await uow.Rollback();

// Check dirty state
if (uow.IsDirty)
    await uow.Commit();
```

## Change Tracking

```csharp
bool dirty = uow.IsDirty;
var inserted = uow.InsertedKeys;         // Dictionary<int, string>
var updated = uow.UpdatedKeys;           // Dictionary<int, string>
var deleted = uow.DeletedKeys;           // Dictionary<int, string>
var deletedList = uow.DeletedUnits;      // List<T>

var added = uow.GetAddedEntities();      // IEnumerable<int> indices
var modified = uow.GetModifiedEntities(); // IEnumerable<int> indices
var deletedItems = uow.GetDeletedEntities(); // IEnumerable<T>

var log = uow.GetChangeLog();            // List<ChangeRecord>
uow.SaveLog("path/to/log.json");        // Persist audit log
```

## Navigation

```csharp
uow.MoveFirst();
uow.MoveNext();
uow.MovePrevious();
uow.MoveLast();
uow.MoveTo(5);                           // By index
uow.MoveToItem(entity);                  // By reference (OBL passthrough)

var current = uow.CurrentItem;           // Current entity
bool atStart = uow.IsAtBOF;
bool atEnd = uow.IsAtEOF;
bool empty = uow.IsEmpty;
```

## Events

```csharp
// Pre-events (cancel via e.Cancel = true on UnitofWorkParams)
uow.PreCreate += (s, e) => { };
uow.PreInsert += (s, e) => { };
uow.PreUpdate += (s, e) => { };
uow.PreDelete += (s, e) => { };
uow.PreCommit += (s, e) => { };
uow.PreQuery += (s, e) => { };

// Post-events
uow.PostCreate += (s, e) => { };
uow.PostInsert += (s, e) => { };
uow.PostUpdate += (s, e) => { };
uow.PostDelete += (s, e) => { };
uow.PostCommit += (s, e) => { };
uow.PostQuery += (s, e) => { };
uow.PostEdit += (s, e) => { };

// INotifyPropertyChanged
uow.PropertyChanged += (s, e) => { };
```

## Filtering and Paging

```csharp
var filters = new List<AppFilter>
{
    new AppFilter { FieldName = "Status", Operator = "=", FilterValue = "Active" },
    new AppFilter { FieldName = "City", Operator = "=", FilterValue = "NYC" }
};
var filtered = await uow.Get(filters);

uow.PageIndex = 0;
uow.PageSize = 10;
int total = uow.TotalItemCount;
```

## Validation (OBL Passthrough)

```csharp
// Enable auto-validation
uow.IsAutoValidateEnabled = true;
uow.BlockCommitOnValidationError = true;

// Validate
var result = uow.ValidateItem(entity);
var allResult = uow.ValidateAll();

// Query errors
var errors = uow.GetErrors(entity);       // List<ValidationError>
var invalid = uow.GetInvalidItems();      // List<T>
```

## Undo/Redo (OBL Passthrough)

```csharp
uow.IsUndoEnabled = true;
uow.MaxUndoDepth = 100;

bool undone = uow.Undo();
bool redone = uow.Redo();
uow.ClearUndoHistory();

bool canUndo = uow.CanUndo;
bool canRedo = uow.CanRedo;

// DEPRECATED: uow.UndoLastChange() - use Undo() instead
```

## Computed Columns (OBL Passthrough)

```csharp
uow.RegisterComputed("FullName", c => $"{c.First} {c.Last}");
uow.RegisterComputed("Total", c => c.Price * c.Qty);

object val = uow.GetComputed(entity, "FullName");
var allVals = uow.GetAllComputed(entity);

uow.UnregisterComputed("FullName");
var names = uow.ComputedColumnNames;  // IReadOnlyCollection<string>
```

## Bookmarks (OBL Passthrough)

```csharp
uow.SetBookmark("start");
uow.MoveLast();
bool found = uow.GoToBookmark("start");   // Returns to saved position
uow.RemoveBookmark("start");
uow.ClearBookmarks();
```

## Thread Safety / Freeze / Batch (OBL Passthrough)

```csharp
// Thread safety
uow.IsThreadSafe = true;

// Freeze (read-only mode)
uow.Freeze();
bool frozen = uow.IsFrozen;
uow.Unfreeze();

// Batch update (suppresses notifications)
using (uow.BeginBatchUpdate())
{
    // bulk operations here  single notification at end
}
```

## Aggregates (OBL Passthrough)

```csharp
decimal sum = uow.Sum("Amount");
decimal condSum = uow.SumWhere("Amount", o => o.Status == "Paid");

decimal avg = uow.Average("Price");
decimal condAvg = uow.AverageWhere("Price", p => p.Category == "A");

object min = uow.Min("Price");
object max = uow.Max("Price");

int count = uow.CountWhere(c => c.Active);

var groups = uow.GroupBy("Category");              // Dict<object, List<T>>
var distinct = uow.DistinctValues("City");         // List<object>
```

## Virtual / Lazy Loading (OBL Passthrough)

```csharp
uow.EnableVirtualMode(totalCount: 50000);

bool isVirtual = uow.IsVirtualMode;
int totalPages = uow.VirtualTotalPages;
uow.PageCacheSize = 5;

await uow.GoToPageAsync(10);
await uow.PrefetchAdjacentPagesAsync();
uow.InvalidatePageCache();

uow.DisableVirtualMode();
```

## Master-Detail (OBL Passthrough)

```csharp
// Register child list linked by FK
uow.RegisterDetail(childObl, "OrderId", "OrderId");

// Query registered details
var details = uow.DetailLists;    // IReadOnlyList<object>

// Unregister
uow.UnregisterDetail(childObl);
uow.UnregisterAllDetails();
```

## Soft Delete and Concurrency

```csharp
uow.SoftDeleteFieldName = "IsDeleted";
uow.IncludeDeleted = false;

uow.ConcurrencyMode = ConcurrencyMode.ThrowOnConflict;
uow.ConcurrencyFieldName = "RowVersion";
```

## PK / Identity / Sequence

```csharp
uow.IsIdentity = true;
uow.Sequencer = "seq_customers";
uow.GuidKey = "CustomerId";

uow.SetIDValue(entity, 42);
object id = uow.GetIDValue(entity);
int seq = uow.GetPrimaryKeySequence(entity);
int nextSeq = uow.GetSeq("seq_customers");
double lastId = uow.GetLastIdentity();
```

## Utility Methods

```csharp
uow.Clear();                              // Clear all data
int idx = uow.DocExist(entity);           // Check existence (returns index)
int idx2 = uow.DocExistByKey(entity);     // By PK
int idx3 = uow.FindDocIdx(entity);        // Find index
int idx4 = uow.Getindex("123");           // By PK string
int idx5 = uow.Getindex(entity);          // By entity ref
T item = uow.GetItemFromCurrentList(0);   // By index
var tracking = uow.GetTrackingItem(item);  // Tracking info
```

## Logging

```csharp
uow.IsLogging = true;
var log = uow.UpdateLog;   // Dictionary<DateTime, EntityUpdateInsertLog>
```

## Best Practices

1. **Always use `using`**  automatic disposal
2. **Commit after operations**  changes not persisted until `Commit()`
3. **Check errors**  always check `result.Flag == Errors.Ok`
4. **Use async methods**  prefer `await uow.Get()` over sync
5. **Enable validation**  set `IsAutoValidateEnabled = true` for data integrity
6. **Use Undo() not UndoLastChange()**  the old method is deprecated
7. **Set CommitOrder**  use `InsertsFirst` when FK dependencies require parent first
8. **Batch large operations**  use `BeginBatchUpdate()` for bulk inserts
9. **Thread safety**  set `IsThreadSafe = true` for concurrent access
10. **Validate before commit**  call `ValidateAll()` to catch errors early
````
