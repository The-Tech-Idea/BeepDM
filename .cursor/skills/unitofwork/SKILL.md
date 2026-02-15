---
name: unitofwork
description: Guidance for UnitofWork<T> usage for CRUD operations, change tracking, and transactions in BeepDM.
---

# UnitOfWork Pattern Guide

Use this skill when implementing CRUD operations through UnitofWork or building service layers on top of BeepDM.

## Core Capabilities
- Change tracking for inserts, updates, deletes
- Transaction handling via `Commit()`
- DefaultsManager integration
- Event hooks for pre and post operations

## Workflow
1. Create `UnitofWork<T>` for entity and datasource.
2. Use `New`, `Add`, `Update`, or `Delete`.
3. Call `Commit()` to persist changes.

## Validation
- Check `IErrorsInfo.Flag == Errors.Ok` from `Commit()`.
- Check `IsDirty` before committing.
- Ensure correct primary key field name.

## Pitfalls
- Changes are not persisted until `Commit()`.
- Forgetting `using` can leak resources.
- Using wrong primary key breaks Update/Delete by ID.

## File Locations
- DataManagementEngineStandard/Editor/UOW/

## Example
```csharp
using var uow = new UnitofWork<Customer>(editor, "MyDb", "Customers", "Id");

uow.New();
uow.CurrentItem.Name = "Jane";

var result = await uow.Commit();
if (result.Flag != Errors.Ok)
{
    Console.WriteLine(result.Message);
}
```

## Task-Specific Examples

### Update And Commit
```csharp
var customers = await uow.Get();
var customer = customers.FirstOrDefault();
if (customer != null)
{
    customer.Name = "Updated";
    uow.Update(customer);
    await uow.Commit();
}
```

### Rollback Changes
```csharp
uow.Add(new Customer { Name = "Temp" });
if (uow.IsDirty)
{
    await uow.Rollback();
}
```