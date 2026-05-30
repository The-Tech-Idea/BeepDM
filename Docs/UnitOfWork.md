# UnitOfWork Pattern Guide

## Overview

`UnitofWork<T>` is a thin orchestrator that owns DataSource I/O and delegates all collection/tracking/state logic to its `Units` property (`ObservableBindingList<T>`).

## Architecture

```
Consumer (UI / Service)
        |
   IUnitofWork<T>
        |
   UnitofWork<T>
   +- DataSource I/O (Insert/Update/Delete)
   +- DefaultsManager integration
   +- EntityStructure validation bridge
   +- PrimaryKey / Identity / GuidKey management
   +- Event relay (Pre/Post hooks)
   +- ObservableBindingList<T> (Units)
      +- SINGLE SOURCE OF TRUTH for:
         Change tracking, Dirty state, Navigation,
         Undo/Redo, Validation, Filter/Sort/Page,
         Computed columns, Master-detail, Thread safety,
         Aggregates, Virtual/lazy loading
```

## Class Declaration

```csharp
public partial class UnitofWork<T> : IUnitofWork<T>, INotifyPropertyChanged, IDisposable
    where T : Entity, new()
```

## Core Capabilities

- Change tracking (delegates to OBL's Tracking system)
- Transaction handling via `Commit()` using OBL's `CommitAllAsync()`
- `CommitOrder` control: `DeletesFirst`, `InsertsFirst`, `AsTracked`
- DefaultsManager integration for insert/update defaults
- Validation (Data Annotations + EntityStructure-based via CustomValidator bridge)
- Granular Undo/Redo (per-action stack, replaces old snapshot approach)
- Computed columns, Bookmarks, Aggregates
- Virtual/lazy loading with DataSource-powered page provider
- Master-detail auto-sync between parent/child UOWs
- Thread safety, Freeze (read-only mode), Batch update scoping
- Event hooks for pre/post operations (cancellable)
- Dynamic runtime creation through `UnitOfWorkFactory` and wrappers when entity types are not known at compile time

## File Locations

- `DataManagementEngineStandard/Editor/UOW/UnitofWork.Core.cs` - Fields, constructors, properties, SetUnits, Dispose
- `DataManagementEngineStandard/Editor/UOW/UnitofWork.CRUD.cs` - Create, Read, Update operations
- `DataManagementEngineStandard/Editor/UOW/UnitofWork.Core.Extensions.cs` - Delete, Commit, Rollback, batch ops, events, logging
- `DataManagementEngineStandard/Editor/UOW/UnitofWork.Core.Utilities.cs` - Navigation, tracking queries, filtering, helpers
- `DataManagementEngineStandard/Editor/UOW/UnitofWork.OBLIntegration.cs` - All OBL feature passthroughs
- `DataManagementEngineStandard/Editor/UOW/UnitOfWorkFactory.cs` - Runtime type creation and wrappers
- `DataManagementEngineStandard/Editor/UOW/UnitOfWorkWrapper.cs` - Late-bound access wrapper
- `DataManagementModelsStandard/Editor/IUnitofWork.cs` - Generic interface
- `DataManagementModelsStandard/Editor/IUnitofWorkNonGeneric.cs` - Non-generic interface

## Basic CRUD and Commit

```csharp
using var uow = new UnitofWork<Customer>(editor, "MyDb", "Customers", "Id");

// Create
uow.New();
uow.CurrentItem.Name = "Jane";

// Read
var all = await uow.Get();
var customer = uow.Get("123");

// Update
customer.Name = "Updated";
uow.Update(customer);

// Delete
uow.Delete(customer);

// Commit all changes
var result = await uow.Commit();
if (result.Flag != Errors.Ok)
    Console.WriteLine(result.Message);
```

## Undo/Redo

```csharp
uow.IsUndoEnabled = true;
uow.MaxUndoDepth = 50;

customer.Name = "Changed";
uow.Undo();  // Name reverts to original
uow.Redo();  // Name goes back to "Changed"
```

## Validation

```csharp
uow.IsAutoValidateEnabled = true;
uow.BlockCommitOnValidationError = true;

var result = uow.ValidateAll();
if (!result.IsValid)
{
    foreach (var error in result.Errors)
        Console.WriteLine($"{error.PropertyName}: {error.Message}");
}

var invalidItems = uow.GetInvalidItems();
```

## Aggregates

```csharp
decimal total = uow.Sum("Amount");
decimal avg = uow.Average("Price");
int activeCount = uow.CountWhere(c => c.IsActive);
var grouped = uow.GroupBy("Category");
var distinctCities = uow.DistinctValues("City");
```

## Virtual/Lazy Loading

```csharp
uow.EnableVirtualMode(totalCount: 10000);
await uow.GoToPageAsync(5);
await uow.PrefetchAdjacentPagesAsync();
uow.InvalidatePageCache();
uow.DisableVirtualMode();
```

## Master-Detail

```csharp
var masterUow = new UnitofWork<Order>(editor, "MyDb", "Orders", "OrderId");
var detailUow = new UnitofWork<OrderLine>(editor, "MyDb", "OrderLines", "LineId");

var lines = await detailUow.Get();
masterUow.RegisterDetail(detailUow.Units, "OrderId", "OrderId");
// Moving master auto-filters detail by FK
```

## Batch Update (suppress notifications)

```csharp
using (uow.BeginBatchUpdate())
{
    for (int i = 0; i < 1000; i++)
        uow.Add(new Customer { Name = $"Customer {i}" });
}
// Single notification fires after batch
```

## Thread Safety and Freeze

```csharp
uow.IsThreadSafe = true;   // Enable reader-writer locks
uow.Freeze();               // Collection becomes read-only
uow.Unfreeze();             // Mutations allowed again
```

## Bookmarks

```csharp
uow.SetBookmark("checkpoint1");
// ... navigate around ...
uow.GoToBookmark("checkpoint1");  // Returns to saved position
```

## Computed Columns

```csharp
uow.RegisterComputed("FullName", c => $"{c.FirstName} {c.LastName}");
var fullName = uow.GetComputed(customer, "FullName");
```

## CommitOrder Control

```csharp
uow.CommitOrder = CommitOrder.InsertsFirst;  // Parent records first
// or
uow.CommitOrder = CommitOrder.DeletesFirst;  // Clean up first (default)
```

## Rollback

```csharp
uow.Add(new Customer { Name = "Temp" });
if (uow.IsDirty)
    await uow.Rollback();  // Reverts all pending changes via OBL's RejectChanges()
```

## Service Pattern

```csharp
public class CustomerService
{
    private readonly IDMEEditor _editor;

    public async Task<List<Customer>> GetAllAsync()
    {
        using var uow = new UnitofWork<Customer>(_editor, "MyDb", "Customers", "Id");
        return (await uow.Get()).ToList();
    }

    public async Task<IErrorsInfo> CreateAsync(Customer customer)
    {
        using var uow = new UnitofWork<Customer>(_editor, "MyDb", "Customers", "Id");
        uow.Add(customer);
        return await uow.Commit();
    }
}
```

## Runtime Type Creation With Factory

```csharp
var wrapper = UnitOfWorkFactory.CreateUnitOfWork(
    typeof(Customer),
    editor,
    "MyDb",
    "Customers",
    primaryKey: "Id");

var result = await wrapper.CommitAsync();
```

## Pitfalls

- Changes are not persisted until `Commit()`.
- Forgetting `using` can leak resources - always use `using var uow = ...`.
- Using wrong primary key breaks Update/Delete by ID.
- `UndoLastChange()` is `[Obsolete]` - use `Undo()` instead.
- `CommitOrder` defaults to `DeletesFirst` - change to `InsertsFirst` if foreign keys require parent records first.
- Virtual mode requires calling `EnableVirtualMode(totalCount)` before `GoToPageAsync()`.
- Validation is wired automatically via `CustomValidator` bridge in `SetUnits()` - no manual setup needed.

## Related Documentation

- [Core Architecture](CoreArchitecture.md) - IDMEEditor overview
- [Data Source Implementation](HowToCreateNewDataSource.md) - Building custom data sources
- [Forms Manager](FormsManager.md) - Oracle Forms-style UI orchestration
