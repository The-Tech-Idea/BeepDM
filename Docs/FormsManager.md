# Forms Manager Guide

## Overview

`FormsManager` provides Oracle Forms-style behavior with block registration, master-detail coordination, mode transitions, navigation, dirty-state handling, trigger/event flow, and performance/configuration policies.

## Architecture

- `FormsManager` is the coordinator class in `TheTechIdea.Beep.Editor.UOWManager`.
- Core responsibilities are split across partial classes:
  - Registration and coordination in `FormsManager.cs`
  - Lifecycle in `FormsManager.FormOperations.cs`
  - Navigation in `FormsManager.Navigation.cs`
  - Mode flow in `FormsManager.ModeTransitions.cs`
  - CRUD/query enhancements in `FormsManager.EnhancedOperations.cs`
- Helper managers own focused behavior:
  - `RelationshipManager`
  - `DirtyStateManager`
  - `EventManager`
  - `FormsSimulationHelper`
  - `PerformanceManager`
  - `ConfigurationManager`

## File Locations

- `DataManagementEngineStandard/Editor/Forms/FormsManager.cs`
- `DataManagementEngineStandard/Editor/Forms/FormsManager.FormOperations.cs`
- `DataManagementEngineStandard/Editor/Forms/FormsManager.Navigation.cs`
- `DataManagementEngineStandard/Editor/Forms/FormsManager.ModeTransitions.cs`
- `DataManagementEngineStandard/Editor/Forms/FormsManager.EnhancedOperations.cs`
- `DataManagementEngineStandard/Editor/Forms/Helpers/`
- `DataManagementEngineStandard/Editor/Forms/Configuration/`
- `DataManagementEngineStandard/Editor/Forms/Models/`

## Fast Workflow

1. Create `FormsManager(editor)`.
2. Register each block with `RegisterBlock(...)`.
3. Create relationships with `CreateMasterDetailRelation(...)`.
4. Open the form and enter the right mode for the target block.
5. Use form/navigation/CRUD APIs through `FormsManager`, not direct ad-hoc block mutations.
6. Commit or roll back through form-level APIs.

## Basic Setup

```csharp
// Create forms manager
var formsManager = new FormsManager(editor);

// Register blocks
formsManager.RegisterBlock(
    blockName: "Orders",
    dataSourceName: "MyDb",
    entityName: "Orders",
    primaryKey: "OrderId");

formsManager.RegisterBlock(
    blockName: "OrderLines",
    dataSourceName: "MyDb",
    entityName: "OrderLines",
    primaryKey: "LineId");

// Create master-detail relationship
formsManager.CreateMasterDetailRelation(
    masterBlock: "Orders",
    detailBlock: "OrderLines",
    masterField: "OrderId",
    detailField: "OrderId");

// Open form
formsManager.OpenForm("OrderEntryForm");
```

## Mode Transitions

```csharp
// Enter query mode
formsManager.EnterQuery("Orders");

// Execute query
formsManager.ExecuteQuery("Orders");

// Enter insert mode
formsManager.EnterInsert("Orders");

// Enter update mode
formsManager.EnterUpdate("Orders");

// Enter delete mode
formsManager.EnterDelete("Orders");

// Commit changes
var result = formsManager.Commit("Orders");
if (result.Flag != Errors.Ok)
    Console.WriteLine(result.Message);

// Rollback changes
formsManager.Rollback("Orders");
```

## Navigation

```csharp
// Navigate records
formsManager.FirstRecord("Orders");
formsManager.PreviousRecord("Orders");
formsManager.NextRecord("Orders");
formsManager.LastRecord("Orders");

// Go to specific record
formsManager.GoToRecord("Orders", 5);

// Check navigation state
bool canGoNext = formsManager.CanGoNext("Orders");
bool canGoPrevious = formsManager.CanGoPrevious("Orders");
```

## Dirty State Handling

```csharp
// Check if block has changes
bool isDirty = formsManager.IsDirty("Orders");

// Check if specific field is dirty
bool fieldChanged = formsManager.IsFieldDirty("Orders", "CustomerName");

// Clear dirty state
formsManager.ClearDirty("Orders");

// Get changed fields
var changedFields = formsManager.GetChangedFields("Orders");
```

## Event Handling

```csharp
// Register pre-event handlers
formsManager.RegisterPreEvent("Orders", FormEvent.Insert, args =>
{
    // Validate before insert
    if (string.IsNullOrEmpty(args.CurrentRecord["CustomerName"]?.ToString()))
    {
        args.Cancel = true;
        args.ErrorMessage = "CustomerName is required";
    }
});

// Register post-event handlers
formsManager.RegisterPostEvent("Orders", FormEvent.Commit, args =>
{
    Console.WriteLine($"Committed {args.RecordsAffected} records");
});

// Trigger events manually
formsManager.TriggerEvent("Orders", FormEvent.Validate);
```

## Master-Detail Coordination

```csharp
// Auto-sync detail when master changes
formsManager.EnableAutoSync("Orders", "OrderLines");

// Manually refresh detail
formsManager.RefreshDetail("Orders", "OrderLines");

// Check if detail has unsaved changes
bool detailDirty = formsManager.IsDetailDirty("Orders", "OrderLines");
```

## Performance Configuration

```csharp
// Configure fetch size
formsManager.SetFetchSize("Orders", 100);

// Enable/disable lazy loading
formsManager.EnableLazyLoading("Orders", true);

// Set query timeout
formsManager.SetQueryTimeout("Orders", TimeSpan.FromSeconds(30));

// Configure caching
formsManager.EnableBlockCache("Orders", true);
formsManager.SetCacheSize("Orders", 50);
```

## Validation

```csharp
// Validate current record
var validationResult = formsManager.ValidateRecord("Orders");
if (!validationResult.IsValid)
{
    foreach (var error in validationResult.Errors)
    {
        Console.WriteLine($"{error.FieldName}: {error.Message}");
    }
}

// Validate all records in block
var blockValidation = formsManager.ValidateBlock("Orders");
```

## Block State Management

```csharp
// Save block state
formsManager.SaveBlockState("Orders", "checkpoint1");

// Restore block state
formsManager.RestoreBlockState("Orders", "checkpoint1");

// Reset block to initial state
formsManager.ResetBlock("Orders");
```

## Working with Multiple Forms

```csharp
// Create multiple forms
formsManager.CreateForm("OrderEntry");
formsManager.CreateForm("CustomerLookup");

// Switch between forms
formsManager.ActivateForm("OrderEntry");

// Close form
formsManager.CloseForm("CustomerLookup");
```

## Integration with UnitOfWork

```csharp
// Get UnitOfWork for a block
var uow = formsManager.GetUnitOfWork("Orders");

// Use UOW features through FormsManager
formsManager.AddNew("Orders");
formsManager.DeleteCurrent("Orders");
formsManager.UpdateCurrent("Orders");
```

## Pitfalls

- Always use `FormsManager` APIs rather than manipulating blocks directly.
- Call `Commit()` through `FormsManager` to ensure master-detail consistency.
- Register events before opening the form to avoid missing initial triggers.
- Master-detail relations must be created before opening the form.
- Navigation state may be stale after query execution - re-check `CanGoNext`/`CanGoPrevious`.

## Related Documentation

- [Core Architecture](CoreArchitecture.md) - IDMEEditor overview
- [Unit of Work Pattern](UnitOfWork.md) - Transactional operations
- [ETL Operations](ETL.md) - Data migration
