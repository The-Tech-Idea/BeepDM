# ObservableBindingList Partial Class Split Plan

**Objective**: Split `ObservableBindingList.cs` (2,348 lines) into organized partial classes for easier maintenance and future enhancement  
**Risk Level**: Low (code organization only, zero logic changes)  
**Status**: Ready to Execute  
**Related Plan**: [observablebindinglist_enterprise_enhancement.plan.md](observablebindinglist_enterprise_enhancement.plan.md) — future enhancements will target individual partial files instead of one monolithic file

---

## Step 1: Create Backup

### Actions
1. Copy `ObservableBindingList.cs` → `ObservableBindingList.cs.backup`
2. Commit to git with message: "Backup before ObservableBindingList partial class split"

### Validation
- Verify backup exists
- Confirm solution still compiles before any changes

---

## Step 2: Current File Structure Analysis

**File:** `DataManagementModelsStandard/Editor/ObservableBindingList.cs`  
**Total Lines:** 2,348  
**Namespace:** `TheTechIdea.Beep.Editor`  
**Class Signature:** `public class ObservableBindingList<T> : BindingList<T>, IBindingListView, INotifyCollectionChanged, IDisposable where T : class, INotifyPropertyChanged, new()`

### Current Regions and Line Ranges

| Region | Lines | Approx Size | Description |
|--------|-------|-------------|-------------|
| *(pre-region)* | 1–116 | 116 | Usings, class decl, static cache, fields, events, tracking dictionaries |
| `"Current and Movement"` | 117–201 | 85 | `_currentIndex`, `Current`, `MoveNext/Previous/First/Last/To`, `OnCurrentChanged` |
| `"Sort"` | 203–368 | 165 | `isSorted`, sort fields, `ApplySort` (3 overloads), `ApplySortCore`, `RemoveSort/Core`, `Sort`, `ParallelQuickSort`, `InsertionSort`, `Partition`, `Swap`, `Compare` |
| `"Find"` | 370–573 | 203 | `WhereContains`, `FirstOrDefault`, `Any`, `SearchWithProgress`, `FindIndex`, `Search`, `Find` (2 overloads), nested `SearchCompletedEventArgs<TItem>` |
| `"Enhanced Search Functionality"` | 574–855 | 282 | `SearchByProperties`, `SearchByText`, `SearchAllProperties`, `AdvancedSearch`, `EvaluateCondition`, `CompareValues`, `FindAndFilter` |
| `"Filter"` | 856–1101 | 246 | `filterString`, `originalList`, `DeletedList`, `ApplyFilter` (3 overloads), `Filter` property, `RemoveFilter`, `ParseFilter`, `ParseSingleCondition`, `ResetItems`, `ResetBindings` |
| `"Constructor"` | 1103–1271 | 169 | `ClearAll`, `ClearItems`, `Dispose`, 6 constructors |
| `"Util Methods"` | 1274–1346 | 73 | `GetItem<T>`, `ConvertToNumericType`, `IsNumericType` |
| `"Logging"` | 1348–1424 | 77 | `TrackChanges`, `CreateLogEntry`, `GetChangedFields`, `ChangedValues`, `IsLogging`, `UpdateLog` |
| `"List and Item Change"` | 1426–1702 | 277 | `OnListChanged`, `Item_PropertyChanged`, `RemoveItem`, `InsertItem`, `SetPosition`, `SetItem`, `CollectionChanged` event, `OnCollectionChanged` |
| `"ID Generations"` | 1704–2057 | 354 | `UpdateIndexTrackingAfterFilterorSort`, `EnsureTrackingConsistency`, `UpdateLogEntries`, `ResettoOriginal`, `UpdateItemIndexMapping`, `GetOriginalIndex`, `GetItem`, `GetItemFromOriginalList/CurrentList`, `GetTrackingItem`, `MarkAsCommitted`, `ResetAfterCommit`, `CommitItemAsync`, `GetPendingChanges` |
| `"Export"` | 2059–2101 | 43 | `ToDataTable` |
| `"Pagination"` | 2103–2128 | 26 | `SetPageSize`, `GoToPage`, `ApplyPaging` |
| `"CRUD"` | 2130–2348 | 219 | `AddNewCore`, `AddNew` (2 overloads), `AddRange`, `RemoveRange`, `RemoveAll` |

### All Shared State (Fields/Properties That Cross Partial Boundaries)

These fields are accessed by multiple regions and must remain in the core file. All partials can access them because they are in the same `partial class`.

| Field | Type | Used By |
|-------|------|---------|
| `_isDisposed` | `bool` | Constructor, Dispose |
| `_propertyInfoCache` | `static ConcurrentDictionary<string, PropertyInfo>` | Sort, Filter, Find, Logging, Util, CRUD |
| `_trackingsByGuid` | `Dictionary<Guid, Tracking>` | Tracking, ListChanges, CRUD, Commit |
| `_trackingsByOriginalIndex` | `Dictionary<int, Tracking>` | Tracking, ListChanges, CRUD, Commit |
| `_currentIndex` | `int` | Current/Movement, ListChanges, Filter, Pagination |
| `_isPositionChanging` | `bool` | Current/Movement, ListChanges |
| `SuppressNotification` | `bool` | Sort, Filter, ListChanges, CRUD, Tracking, Pagination |
| `isSorted` | `bool` | Sort, Filter, ListChanges |
| `sortProperty` | `PropertyDescriptor` | Sort |
| `sortDirection` | `ListSortDirection` | Sort |
| `filterString` | `string` | Filter, ListChanges |
| `originalList` | `List<T>` | Filter, Sort, ListChanges, Tracking, Pagination, CRUD, Export |
| `DeletedList` | `List<T>` | Filter, ListChanges, Tracking, Commit |
| `ChangedValues` | `Dictionary<T, Dictionary<string, object>>` | Logging, ListChanges, Tracking |
| `UpdateLog` | `Dictionary<DateTime, EntityUpdateInsertLog>` | Logging, Tracking, Commit |
| `IsLogging` | `bool` | Logging, ListChanges |
| `PageSize` | `int` | Pagination |
| `CurrentPage` | `int` | Pagination |

---

## Step 3: Partial Class Split Plan

### 3A. File Layout

All files go in: `DataManagementModelsStandard/Editor/`

| # | File Name | Content (from regions) | Approx Lines | Enhancement Plan Phase |
|---|-----------|----------------------|-------------|----------------------|
| 1 | `ObservableBindingList.cs` | Class declaration, usings, all shared fields, events, static reflection cache, tracking dictionary helpers, `IDisposable` | ~180 | — |
| 2 | `ObservableBindingList.Constructors.cs` | All 6 constructors, `ClearAll`, `ClearItems`, `Dispose(bool)` | ~170 | — |
| 3 | `ObservableBindingList.CurrentAndMovement.cs` | `_currentIndex`, `Current`, `CurrentIndex`, `CurrentChanged`, `OnCurrentChanged`, `MoveNext/Previous/First/Last/To`, `SetPosition` | ~100 | Phase 3 |
| 4 | `ObservableBindingList.Sort.cs` | Sort fields, `ApplySort` (3 overloads), `ApplySortCore`, `RemoveSort`, `RemoveSortCore`, `Sort(string)`, `ParallelQuickSort`, `InsertionSort`, `Partition`, `Swap`, `Compare`, sort properties | ~165 | Phase 2 (Bug 1,2,5) |
| 5 | `ObservableBindingList.Find.cs` | `WhereContains`, `FirstOrDefault`, `Any`, `SearchWithProgress`, `FindIndex`, `Search`, `Find` (2 overloads), `SearchCompletedEventArgs<TItem>` nested class, `SearchCompleted` event | ~205 | — |
| 6 | `ObservableBindingList.Search.cs` | `SearchByProperties`, `SearchByText`, `SearchAllProperties`, `AdvancedSearch`, `EvaluateCondition`, `CompareValues`, `FindAndFilter` | ~280 | — |
| 7 | `ObservableBindingList.Filter.cs` | `filterString`, `originalList`, `DeletedList`, `ApplyFilter` (3 overloads), `Filter` property, `RemoveFilter`, `ParseFilter`, `ParseSingleCondition`, `ResetItems`, `ResetBindings` | ~250 | Phase 2 (Bug 1,3,6) |
| 8 | `ObservableBindingList.Logging.cs` | `TrackChanges`, `CreateLogEntry`, `GetChangedFields`, `ChangedValues`, `IsLogging`, `IsLoggin` (obsolete), `UpdateLog` | ~80 | Phase 2 (Bug 12) |
| 9 | `ObservableBindingList.ListChanges.cs` | `OnListChanged`, `ObservableBindingList_AddingNew`, `Item_PropertyChanged`, `RemoveItem`, `InsertItem`, `SetItem`, `CollectionChanged` event, `OnCollectionChanged` | ~280 | Phase 1B, Phase 2 (Bug 8,9,10), Phase 3 |
| 10 | `ObservableBindingList.Tracking.cs` | `UpdateIndexTrackingAfterFilterorSort`, `EnsureTrackingConsistency`, `UpdateLogEntries`, `ResettoOriginal`, `UpdateItemIndexMapping`, `GetOriginalIndex`, `GetItem`, `GetItemFromOriginalList`, `GetItemFromCurrentList`, `GetItemFroCurrentList` (obsolete), `GetTrackingItem`, `MarkAsCommitted`, `ResetAfterCommit`, `CommitItemAsync`, `GetPendingChanges` | ~355 | Phase 1 (all), Phase 5 |
| 11 | `ObservableBindingList.Export.cs` | `ToDataTable` | ~45 | — |
| 12 | `ObservableBindingList.Pagination.cs` | `SetPageSize`, `GoToPage`, `ApplyPaging`, `TotalPages` | ~30 | Phase 2 (Bug 3,7) |
| 13 | `ObservableBindingList.CRUD.cs` | `AddNewCore`, `AddNew` (2 overloads), `AddRange`, `RemoveRange`, `RemoveAll` | ~220 | — |
| 14 | `ObservableBindingList.Utilities.cs` | `GetItem<T>(DataRow)`, `ConvertToNumericType`, `IsNumericType` | ~75 | — |

**Total: 14 files** (1 core + 13 partials)

---

### 3B. Core File: `ObservableBindingList.cs`

This file keeps the class declaration, all shared state, and the static reflection cache. Every other partial file depends on these.

```
Content:
├── using directives (all — shared across partials via partial class)
├── namespace TheTechIdea.Beep.Editor
├── public partial class ObservableBindingList<T> : BindingList<T>, IBindingListView, INotifyCollectionChanged, IDisposable
│   ├── _isDisposed field
│   ├── _propertyInfoCache (static ConcurrentDictionary)
│   ├── GetCachedProperty(string) — static helper
│   ├── GetCachedProperties() — static helper
│   ├── PageSize, CurrentPage, TotalPages properties
│   ├── ItemAdded, ItemRemoved, ItemChanged events
│   ├── ItemValidating, ItemDeleting events
│   ├── _trackingsByGuid, _trackingsByOriginalIndex dictionaries
│   ├── Trackings property (backward-compat list view)
│   ├── AddTracking(Tracking) helper
│   ├── RemoveTracking(Tracking) helper
│   ├── ClearTrackings() helper
│   ├── FindTrackingByOriginalIndex(int) helper
│   ├── TrackingsCount property
│   ├── SuppressNotification property
│   ├── IsSorted, IsSynchronized properties
│   ├── PropertyChanged event
│   └── OnPropertyChanged(string) method
```

**Source lines:** 1–116 (pre-region block), plus `PageSize`/`CurrentPage`/`TotalPages` from line 63–65

---

### 3C. `ObservableBindingList.Constructors.cs`

```
Content:
├── public partial class ObservableBindingList<T>
│   ├── ClearAll()
│   ├── ClearItems() override
│   ├── Dispose() (public)
│   ├── Dispose(bool disposing) (protected virtual)
│   ├── ObservableBindingList() — default ctor
│   ├── ObservableBindingList(IEnumerable<T>)
│   ├── ObservableBindingList(IList<T>)
│   ├── ObservableBindingList(IBindingListView)
│   ├── ObservableBindingList(DataTable)
│   └── ObservableBindingList(List<object>)
```

**Source lines:** 1098–1271

---

### 3D. `ObservableBindingList.CurrentAndMovement.cs`

```
Content:
├── public partial class ObservableBindingList<T>
│   ├── _currentIndex field
│   ├── _isPositionChanging field
│   ├── CurrentIndex property (read-only)
│   ├── Current property (read-only)
│   ├── CurrentChanged event
│   ├── OnCurrentChanged()
│   ├── MoveNext()
│   ├── MovePrevious()
│   ├── MoveFirst()
│   ├── MoveLast()
│   ├── MoveTo(int)
│   └── SetPosition(int) — moved from "List and Item Change" region
```

**Source lines:** 117–201 + `SetPosition` from 1638–1655

**Note:** `SetPosition` logically belongs with navigation. Moving it here groups all cursor-related code together.

---

### 3E. `ObservableBindingList.Sort.cs`

```
Content:
├── public partial class ObservableBindingList<T>
│   ├── isSorted field
│   ├── sortProperty field
│   ├── sortDirection field
│   ├── SupportsSortingCore override
│   ├── IsSortedCore override
│   ├── SortPropertyCore override
│   ├── SortDirectionCore override
│   ├── SortDescriptions property
│   ├── SupportsAdvancedSorting property
│   ├── SortDirection property (get/set)
│   ├── InsertionSort(...)
│   ├── Compare(T, T, PropertyInfo, ListSortDirection)
│   ├── ParallelQuickSort(...)
│   ├── Partition(...)
│   ├── Swap(...)
│   ├── ApplySort(string, ListSortDirection)
│   ├── ApplySortCore(PropertyDescriptor, ListSortDirection) override
│   ├── RemoveSort()
│   ├── RemoveSortCore() override
│   ├── ApplySort(ListSortDescriptionCollection)
│   └── Sort(string)
```

**Source lines:** 203–368

---

### 3F. `ObservableBindingList.Find.cs`

```
Content:
├── public partial class ObservableBindingList<T>
│   ├── SearchCompleted event
│   ├── WhereContains(string, string, bool)
│   ├── FirstOrDefault(Func<T, bool>)
│   ├── Any(Func<T, bool>)
│   ├── SearchWithProgress(Func<T, bool>, IProgress<int>)
│   ├── FindIndex(Predicate<T>)
│   ├── Search(Func<T, bool>)
│   ├── Find(Expression<Func<T, bool>>)
│   ├── Find(string, object)
│   └── nested class SearchCompletedEventArgs<TItem>
```

**Source lines:** 370–573

---

### 3G. `ObservableBindingList.Search.cs`

```
Content:
├── public partial class ObservableBindingList<T>
│   ├── SearchByProperties(Dictionary<string, object>, string)
│   ├── SearchByText(string, string, string, bool)
│   ├── SearchAllProperties(string, IEnumerable<string>, bool)
│   ├── AdvancedSearch(string, char, string)
│   ├── EvaluateCondition(T, string, string, string) — private
│   ├── CompareValues(object, string, Func) — private
│   └── FindAndFilter(Func<T, bool>)
```

**Source lines:** 574–855

---

### 3H. `ObservableBindingList.Filter.cs`

```
Content:
├── public partial class ObservableBindingList<T>
│   ├── filterString field
│   ├── originalList field
│   ├── DeletedList field
│   ├── SupportsFiltering property
│   ├── Filter property (get/set)
│   ├── ApplyFilter(Func<T, bool>)
│   ├── ApplyFilter(string, object, string)
│   ├── RemoveFilter()
│   ├── ApplyFilter() — private, string-based
│   ├── ParseFilter(string) — private
│   ├── ParseSingleCondition(string, ParameterExpression) — private
│   ├── ResetItems(List<T>) — private
│   └── ResetBindings() — public new
```

**Source lines:** 856–1101

**Note:** `originalList`, `filterString`, and `DeletedList` are declared here because the Filter region is where they are first declared in the original file. All partials access them as class-level fields.

---

### 3I. `ObservableBindingList.Logging.cs`

```
Content:
├── public partial class ObservableBindingList<T>
│   ├── TrackChanges(T, T) — private
│   ├── CreateLogEntry(T, LogAction, Tracking, Dictionary) — private
│   ├── ChangedValues field
│   ├── IsLoggin property (obsolete)
│   ├── IsLogging property
│   ├── UpdateLog property
│   └── GetChangedFields(T, T) — private
```

**Source lines:** 1348–1424

---

### 3J. `ObservableBindingList.ListChanges.cs`

```
Content:
├── public partial class ObservableBindingList<T>
│   ├── OnListChanged(ListChangedEventArgs) override
│   ├── ObservableBindingList_AddingNew(object, AddingNewEventArgs) — private
│   ├── Item_PropertyChanged(object, PropertyChangedEventArgs) — private
│   ├── RemoveItem(int) override
│   ├── InsertItem(int, T) override
│   ├── SetItem(int, T) override
│   ├── CollectionChanged event
│   └── OnCollectionChanged(NotifyCollectionChangedEventArgs)
```

**Source lines:** 1426–1702

---

### 3K. `ObservableBindingList.Tracking.cs`

```
Content:
├── public partial class ObservableBindingList<T>
│   ├── UpdateIndexTrackingAfterFilterorSort() — private
│   ├── EnsureTrackingConsistency() — private
│   ├── UpdateLogEntries(Tracking, int) — private
│   ├── ResettoOriginal(List<T>) — private
│   ├── UpdateItemIndexMapping(int, bool) — private
│   ├── GetOriginalIndex(T)
│   ├── GetItem()
│   ├── GetItemFromOriginalList(int)
│   ├── GetItemFromCurrentList(int)
│   ├── GetItemFroCurrentList(int) — obsolete
│   ├── GetTrackingItem(T)
│   ├── MarkAsCommitted(T)
│   ├── ResetAfterCommit()
│   ├── CommitItemAsync(T, Func, Func, Func) — async
│   └── GetPendingChanges()
```

**Source lines:** 1704–2057

---

### 3L. `ObservableBindingList.Export.cs`

```
Content:
├── public partial class ObservableBindingList<T>
│   └── ToDataTable(string)
```

**Source lines:** 2059–2101

---

### 3M. `ObservableBindingList.Pagination.cs`

```
Content:
├── public partial class ObservableBindingList<T>
│   ├── SetPageSize(int)
│   ├── GoToPage(int)
│   └── ApplyPaging() — private
```

**Source lines:** 2103–2128

**Note:** `PageSize`, `CurrentPage`, `TotalPages` stay in the core file since they are top-level properties used across multiple partials.

---

### 3N. `ObservableBindingList.CRUD.cs`

```
Content:
├── public partial class ObservableBindingList<T>
│   ├── AddNewCore() override
│   ├── AddNew(T)
│   ├── AddNew()
│   ├── AddRange(IEnumerable<T>)
│   ├── RemoveRange(IEnumerable<T>)
│   └── RemoveAll(Func<T, bool>)
```

**Source lines:** 2130–2348

---

### 3O. `ObservableBindingList.Utilities.cs`

```
Content:
├── public partial class ObservableBindingList<T>
│   ├── GetItem<T>(DataRow) — private
│   ├── ConvertToNumericType(object, Type) — private
│   └── IsNumericType(Type) — private static
```

**Source lines:** 1274–1346

---

## Step 4: Execution Order

Each step must compile before proceeding to the next. The order is designed to extract the simplest, most self-contained partials first.

| Order | Action | Validation |
|-------|--------|------------|
| 1 | Create backup `ObservableBindingList.cs.backup` | File exists |
| 2 | Add `partial` keyword to class declaration | Compiles |
| 3 | Extract `ObservableBindingList.Utilities.cs` | Compiles — no dependencies on other partials |
| 4 | Extract `ObservableBindingList.Export.cs` | Compiles — only reads `Items` and `GetCachedProperties()` |
| 5 | Extract `ObservableBindingList.Pagination.cs` | Compiles — uses `originalList`, `PageSize`, `CurrentPage` |
| 6 | Extract `ObservableBindingList.Logging.cs` | Compiles — uses `GetCachedProperties()`, `UpdateLog` |
| 7 | Extract `ObservableBindingList.Find.cs` | Compiles — uses `Items`, `originalList`, `GetCachedProperty()` |
| 8 | Extract `ObservableBindingList.Search.cs` | Compiles — uses `Items`, `originalList`, `GetCachedProperty()` |
| 9 | Extract `ObservableBindingList.CurrentAndMovement.cs` | Compiles — move `SetPosition` here from ListChanges region |
| 10 | Extract `ObservableBindingList.Sort.cs` | Compiles — uses `GetCachedProperty()`, `originalList`, `ResetItems`, `ResetBindings` |
| 11 | Extract `ObservableBindingList.Filter.cs` | Compiles — move `originalList`, `filterString`, `DeletedList` field declarations to core file first |
| 12 | Extract `ObservableBindingList.CRUD.cs` | Compiles — uses `InsertItem` (in ListChanges), `originalList`, tracking |
| 13 | Extract `ObservableBindingList.Tracking.cs` | Compiles — uses `originalList`, tracking dictionaries, `Items`, `UpdateLog` |
| 14 | Extract `ObservableBindingList.ListChanges.cs` | Compiles — uses tracking, `originalList`, `filterString`, logging |
| 15 | Extract `ObservableBindingList.Constructors.cs` | Compiles — everything else already extracted |
| 16 | Final cleanup of core `ObservableBindingList.cs` | Compiles — only shared state remains |

---

## Step 5: Field Relocation Rules

Some fields are declared in the middle of the original file (inside regions) but are accessed by multiple partials. These must be **relocated to the core file** during extraction.

| Field | Current Location | Move To |
|-------|------------------|---------|
| `filterString` | Filter region (L857) | Core file |
| `originalList` | Filter region (L858) | Core file |
| `DeletedList` | Filter region (L859) | Core file |
| `isSorted` | Sort region (L204) | Core file |
| `sortProperty` | Sort region (L205) | Core file |
| `sortDirection` | Sort region (L206) | Core file |
| `_currentIndex` | Current/Movement region (L118) | Core file |
| `_isPositionChanging` | Current/Movement region (L126) | Core file |
| `ChangedValues` | Logging region (L1408) | Core file |
| `UpdateLog` | Logging region (L1411) | Core file |
| `IsLogging` | Logging region (L1410) | Core file |

---

## Step 6: Using Directives Strategy

Each partial file needs its own `using` block. The full set of usings from the original file:

```csharp
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
```

**Rule:** Each partial file includes only the usings it actually needs. The minimum set for every partial is:

```csharp
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
```

Additional usings per file:

| File | Extra Usings Needed |
|------|-------------------|
| Core | `System.Collections.Concurrent`, `System.Reflection`, `System.Collections.Specialized` |
| Constructors | `System.Data`, `System.Reflection`, `TheTechIdea.Beep.Utilities` |
| CurrentAndMovement | *(none beyond minimum)* |
| Sort | `System.Collections`, `System.Reflection`, `System.Linq.Expressions`, `System.Threading.Tasks` |
| Find | `System.Linq.Expressions`, `System.Reflection` |
| Search | `System.Reflection` |
| Filter | `System.Linq.Expressions`, `System.Reflection` |
| Logging | `TheTechIdea.Beep.Utilities`, `TheTechIdea.Beep.ConfigUtil` |
| ListChanges | `System.Collections.Specialized`, `TheTechIdea.Beep.Utilities`, `TheTechIdea.Beep.ConfigUtil` |
| Tracking | `System.Threading.Tasks`, `TheTechIdea.Beep.Utilities`, `TheTechIdea.Beep.ConfigUtil` |
| Export | `System.Data`, `System.Reflection` |
| Pagination | *(none beyond minimum)* |
| CRUD | `System.Collections.Specialized`, `TheTechIdea.Beep.Utilities` |
| Utilities | `System.Data`, `System.Reflection` |

---

## Step 7: Partial File Template

Every partial file follows this template:

```csharp
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
// ... additional file-specific usings

namespace TheTechIdea.Beep.Editor
{
    public partial class ObservableBindingList<T>
    {
        #region "RegionName"
        
        // ... methods and members moved from original file
        
        #endregion
    }
}
```

---

## Step 8: Validation Checklist

After each extraction:

- [ ] Solution compiles with zero errors
- [ ] No duplicate member definitions
- [ ] All `private`/`protected` members accessible (they are, within `partial class`)
- [ ] No circular file dependencies
- [ ] Region tags preserved in each partial for readability
- [ ] Original line count ≈ sum of all partial file line counts

Final validation:

- [ ] All existing unit tests pass (if any)
- [ ] `ObservableBindingList<T>` can be instantiated and used identically to before
- [ ] No public API changes (all signatures identical)
- [ ] IntelliSense shows all members correctly

---

## Step 9: Enhancement Plan Alignment

This split directly supports the [enterprise enhancement plan](observablebindinglist_enterprise_enhancement.plan.md). Each enhancement phase now targets specific partial files:

| Enhancement Phase | Target Partial File(s) |
|------------------|----------------------|
| Phase 1: Tracking & Dirty State | `ObservableBindingList.Tracking.cs`, `ObservableBindingList.ListChanges.cs` (Item_PropertyChanged fix) |
| Phase 2: Filter/Sort/Page Bug Fixes | `ObservableBindingList.Sort.cs`, `ObservableBindingList.Filter.cs`, `ObservableBindingList.Pagination.cs`, `ObservableBindingList.ListChanges.cs` |
| Phase 3: Current/Index + Events | `ObservableBindingList.CurrentAndMovement.cs`, `ObservableBindingList.ListChanges.cs` (OnListChanged cursor-jump fix) |
| Phase 4: Validation Framework | New: `ObservableBindingList.Validation.cs` |
| Phase 5: Commit + Master-Detail | `ObservableBindingList.Tracking.cs`, New: `ObservableBindingList.MasterDetail.cs` |
| Phase 6: Computed, Bookmarks, Lazy | New: `ObservableBindingList.Computed.cs`, `ObservableBindingList.Bookmarks.cs`, `ObservableBindingList.VirtualLoading.cs` |
| Phase 7: Undo/Redo, Thread Safety | New: `ObservableBindingList.UndoRedo.cs`, `ObservableBindingList.ThreadSafety.cs`, `ObservableBindingList.Aggregates.cs` |

---

## File Summary

After split (14 files):

| File | Lines |
|------|-------|
| `ObservableBindingList.cs` (core) | ~180 |
| `ObservableBindingList.Constructors.cs` | ~170 |
| `ObservableBindingList.CurrentAndMovement.cs` | ~100 |
| `ObservableBindingList.Sort.cs` | ~165 |
| `ObservableBindingList.Find.cs` | ~205 |
| `ObservableBindingList.Search.cs` | ~280 |
| `ObservableBindingList.Filter.cs` | ~250 |
| `ObservableBindingList.Logging.cs` | ~80 |
| `ObservableBindingList.ListChanges.cs` | ~280 |
| `ObservableBindingList.Tracking.cs` | ~355 |
| `ObservableBindingList.Export.cs` | ~45 |
| `ObservableBindingList.Pagination.cs` | ~30 |
| `ObservableBindingList.CRUD.cs` | ~220 |
| `ObservableBindingList.Utilities.cs` | ~75 |
| **Total** | **~2,435** (slight increase from template/using overhead) |
