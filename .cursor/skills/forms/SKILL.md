---
name: forms
description: Expert guidance for Oracle Forms-compatible form management using FormsManager (UnitofWorksManager). Use when implementing master-detail forms, managing form blocks, handling mode transitions, or simulating Oracle Forms behavior in .NET applications.
---

# Forms Management Guide

Expert guidance for Oracle Forms-compatible form management using FormsManager (UnitofWorksManager), providing master-detail relationships, mode transitions, and complete form-level operations.

## Overview

**FormsManager** (formerly UnitofWorksManager) provides Oracle Forms-compatible data management:
- **Master-Detail Relationships**: Automatic coordination between parent and child blocks
- **Mode Transitions**: Query â†” CRUD mode transitions with validation
- **Form Operations**: COMMIT_FORM, CLEAR_FORM, EXECUTE_QUERY, etc.
- **Navigation**: FIRST_RECORD, LAST_RECORD, NEXT_RECORD, PREVIOUS_RECORD
- **Unsaved Changes Handling**: Intelligent handling of dirty state across blocks
- **Event System**: Comprehensive trigger system (pre/post operations)

## Architecture

### Modular Design

```
FormsManager (Main Coordinator)
â”œâ”€â”€ Partial Classes
â”‚   â”œâ”€â”€ FormsManager.cs - Core coordination & block management
â”‚   â”œâ”€â”€ FormsManager.EnhancedOperations.cs - Type-safe CRUD operations
â”‚   â”œâ”€â”€ FormsManager.FormOperations.cs - Form-level operations
â”‚   â”œâ”€â”€ FormsManager.Navigation.cs - Record navigation
â”‚   â””â”€â”€ FormsManager.ModeTransitions.cs - Mode transition validation
â”œâ”€â”€ Helper Managers
â”‚   â”œâ”€â”€ RelationshipManager - Master-detail relationships
â”‚   â”œâ”€â”€ DirtyStateManager - Unsaved changes handling
â”‚   â”œâ”€â”€ EventManager - Event coordination & triggers
â”‚   â”œâ”€â”€ FormsSimulationHelper - Oracle Forms simulation
â”‚   â”œâ”€â”€ PerformanceManager - Caching & optimization
â”‚   â””â”€â”€ ConfigurationManager - Configuration management
â””â”€â”€ Models
    â”œâ”€â”€ DataBlockInfo - Block metadata
    â”œâ”€â”€ DataBlockRelationship - Relationship definitions
    â””â”€â”€ UnitofWorksManagerConfiguration - Configuration settings
```

## Initialization

### Basic Initialization

```csharp
var formsManager = new FormsManager(dmeEditor);
```

### With Dependency Injection

```csharp
var formsManager = new FormsManager(
    dmeEditor,
    relationshipManager: customRelationshipManager,
    dirtyStateManager: customDirtyStateManager,
    eventManager: customEventManager,
    formsSimulationHelper: customFormsHelper,
    performanceManager: customPerformanceManager,
    configurationManager: customConfigManager
);
```

## Block Registration

### Register Block

```csharp
// Create UnitOfWork for block
using var customerUow = new UnitofWork<Customer>(
    dmeEditor, 
    "MyDatabase", 
    "Customers", 
    "Id"
);

// Get entity structure
var customerStructure = dmeEditor.GetDataSource("MyDatabase")
    .GetEntityStructure("Customers", true);

// Register block
formsManager.RegisterBlock(
    blockName: "CUSTOMERS",
    unitOfWork: customerUow,
    entityStructure: customerStructure,
    dataSourceName: "MyDatabase",
    isMasterBlock: true
);
```

### Register Detail Block

```csharp
// Register master block
formsManager.RegisterBlock("CUSTOMERS", customerUow, customerStructure, "MyDatabase", isMasterBlock: true);

// Register detail block
using var ordersUow = new UnitofWork<Order>(dmeEditor, "MyDatabase", "Orders", "Id");
var ordersStructure = dmeEditor.GetDataSource("MyDatabase").GetEntityStructure("Orders", true);

formsManager.RegisterBlock("ORDERS", ordersUow, ordersStructure, "MyDatabase", isMasterBlock: false);

// Create master-detail relationship
formsManager.CreateRelationship(
    masterBlockName: "CUSTOMERS",
    detailBlockName: "ORDERS",
    masterKeyField: "Id",
    detailForeignKeyField: "CustomerId"
);
```

## Mode Transitions

### Enter Query Mode

```csharp
// Enter query mode for block
var result = await formsManager.EnterQueryModeAsync("CUSTOMERS");

if (result.Flag == Errors.Ok)
{
    // Block is now in query mode, ready for filter input
}
```

### Execute Query and Enter CRUD Mode

```csharp
// Define filters
var filters = new List<AppFilter>
{
    new AppFilter { FieldName = "Status", Operator = "=", FilterValue = "Active" }
};

// Execute query and transition to CRUD mode
var result = await formsManager.ExecuteQueryAndEnterCrudModeAsync("CUSTOMERS", filters);

if (result.Flag == Errors.Ok)
{
    // Block is now in CRUD mode with filtered data
    var currentRecord = formsManager.GetCurrentRecord("CUSTOMERS");
}
```

### Create New Record (with Unsaved Changes Handling)

```csharp
// This handles the critical scenario: creating new record when parent/child have unsaved changes
var result = await formsManager.CreateNewRecordInMasterBlockAsync("CUSTOMERS");

// What happens automatically:
// 1. Detects unsaved changes in parent block (CUSTOMERS)
// 2. Detects unsaved changes in ALL child blocks (ORDERS, ORDER_ITEMS)
// 3. Prompts user: "You have unsaved changes. Save/Discard/Cancel?"
// 4. Handles user response appropriately
// 5. Creates new parent record if approved
// 6. Automatically clears and coordinates ALL child blocks
```

## Form Operations

### COMMIT_FORM

```csharp
// Commit all changes across all blocks
var result = await formsManager.CommitFormAsync();

if (result.Flag == Errors.Ok)
{
    Console.WriteLine("All changes committed successfully");
}
```

### CLEAR_FORM

```csharp
// Clear all blocks
var result = await formsManager.ClearFormAsync();

// Or clear specific block
var result = await formsManager.ClearBlockAsync("CUSTOMERS");
```

### EXECUTE_QUERY

```csharp
// Execute query with filters
var filters = new List<AppFilter>
{
    new AppFilter { FieldName = "City", Operator = "=", FilterValue = "New York" }
};

var result = await formsManager.ExecuteQueryAsync("CUSTOMERS", filters);
```

### DELETE_RECORD

```csharp
// Delete current record in block
var result = await formsManager.DeleteRecordAsync("CUSTOMERS");

if (result.Flag == Errors.Ok)
{
    Console.WriteLine("Record deleted successfully");
}
```

## Navigation

### Record Navigation

```csharp
// First record
var result = await formsManager.FirstRecordAsync("CUSTOMERS");

// Last record
var result = await formsManager.LastRecordAsync("CUSTOMERS");

// Next record
var result = await formsManager.NextRecordAsync("CUSTOMERS");

// Previous record
var result = await formsManager.PreviousRecordAsync("CUSTOMERS");

// Go to specific record
var result = await formsManager.GoToRecordAsync("CUSTOMERS", recordIndex: 5);
```

### Get Current Record

```csharp
// Get current record from block
var currentRecord = formsManager.GetCurrentRecord("CUSTOMERS");

if (currentRecord is Customer customer)
{
    Console.WriteLine($"Current customer: {customer.Name}");
}
```

## Master-Detail Coordination

### Create Relationship

```csharp
// Create master-detail relationship
formsManager.CreateRelationship(
    masterBlockName: "CUSTOMERS",
    detailBlockName: "ORDERS",
    masterKeyField: "Id",
    detailForeignKeyField: "CustomerId"
);

// Multi-level relationships
formsManager.CreateRelationship("ORDERS", "ORDER_ITEMS", "Id", "OrderId");
```

### Automatic Detail Synchronization

```csharp
// When master record changes, detail blocks are automatically synchronized
await formsManager.NextRecordAsync("CUSTOMERS");

// Detail block "ORDERS" automatically filters to show orders for current customer
// Detail block "ORDER_ITEMS" automatically filters to show items for current order
```

## Unsaved Changes Handling

### Check Dirty State

```csharp
// Check if any block has unsaved changes
bool hasUnsavedChanges = formsManager.IsDirty;

// Check specific block
var blockInfo = formsManager.GetBlock("CUSTOMERS");
bool blockIsDirty = blockInfo?.UnitOfWork?.IsDirty ?? false;
```

### Handle Unsaved Changes

```csharp
// When navigating away with unsaved changes
var result = await formsManager.HandleUnsavedChangesAsync(
    blockName: "CUSTOMERS",
    action: UnsavedChangesAction.Save  // Save, Discard, or Cancel
);

if (result.Flag == Errors.Ok)
{
    // Proceed with navigation
    await formsManager.NextRecordAsync("CUSTOMERS");
}
```

## Event System

### Block Events

```csharp
// Pre-create event
formsManager.BlockPreCreate += (sender, args) =>
{
    Console.WriteLine($"Before creating record in {args.BlockName}");
    // args.Cancel = true; // Cancel operation
};

// Post-create event
formsManager.BlockPostCreate += (sender, args) =>
{
    Console.WriteLine($"After creating record in {args.BlockName}");
};

// Pre-update event
formsManager.BlockPreUpdate += (sender, args) =>
{
    Console.WriteLine($"Before updating record in {args.BlockName}");
};

// Pre-delete event
formsManager.BlockPreDelete += (sender, args) =>
{
    Console.WriteLine($"Before deleting record in {args.BlockName}");
};
```

### Form Events

```csharp
// Pre-commit event
formsManager.FormPreCommit += (sender, args) =>
{
    Console.WriteLine("Before committing form");
    // Validate all blocks before commit
};

// Post-commit event
formsManager.FormPostCommit += (sender, args) =>
{
    Console.WriteLine("After committing form");
};
```

### Navigation Events

```csharp
// Pre-navigation event
formsManager.PreNavigation += (sender, args) =>
{
    Console.WriteLine($"Navigating from {args.FromBlock} to {args.ToBlock}");
    // Handle unsaved changes before navigation
};
```

## CRUD Operations

### Create Record

```csharp
// Create new record in block
var result = await formsManager.CreateRecordAsync<Customer>("CUSTOMERS", customer =>
{
    customer.Name = "John Doe";
    customer.Email = "john@example.com";
});

if (result.Flag == Errors.Ok)
{
    Console.WriteLine("Record created successfully");
}
```

### Update Record

```csharp
// Update current record
var result = await formsManager.UpdateRecordAsync<Customer>("CUSTOMERS", customer =>
{
    customer.Name = "Updated Name";
    customer.Email = "updated@example.com";
});
```

### Delete Record

```csharp
// Delete current record
var result = await formsManager.DeleteRecordAsync("CUSTOMERS");
```

## Configuration

### Performance Configuration

```csharp
formsManager.Configuration.Performance.EnableCaching = true;
formsManager.Configuration.Performance.CacheSize = 1000;
formsManager.Configuration.Performance.EnablePerformanceMetrics = true;
```

### Validation Configuration

```csharp
formsManager.Configuration.Validation.ValidateOnNavigation = true;
formsManager.Configuration.Validation.ValidateOnCommit = true;
formsManager.Configuration.Validation.ShowValidationErrors = true;
```

## Best Practices

### 1. Always Register Blocks Before Use
```csharp
// Register all blocks before operations
formsManager.RegisterBlock("CUSTOMERS", customerUow, customerStructure, "MyDatabase", true);
formsManager.RegisterBlock("ORDERS", ordersUow, ordersStructure, "MyDatabase", false);

// Then create relationships
formsManager.CreateRelationship("CUSTOMERS", "ORDERS", "Id", "CustomerId");
```

### 2. Handle Unsaved Changes Before Navigation
```csharp
// Always check and handle unsaved changes
if (formsManager.IsDirty)
{
    var action = await PromptUserForUnsavedChangesAction();
    var result = await formsManager.HandleUnsavedChangesAsync("CUSTOMERS", action);
    
    if (result.Flag != Errors.Ok)
        return; // User cancelled
}

// Proceed with navigation
await formsManager.NextRecordAsync("CUSTOMERS");
```

### 3. Use Mode Transitions Properly
```csharp
// Enter query mode first
await formsManager.EnterQueryModeAsync("CUSTOMERS");

// Set filters (in UI)

// Execute query and enter CRUD mode
await formsManager.ExecuteQueryAndEnterCrudModeAsync("CUSTOMERS", filters);
```

### 4. Commit Changes Regularly
```csharp
// Commit after significant operations
await formsManager.CreateRecordAsync<Customer>("CUSTOMERS", ...);
await formsManager.CommitFormAsync(); // Commit all blocks
```

## Related Skills

- **@unitofwork** - UnitOfWork pattern for individual block operations
- **@beepdm** - Core BeepDM architecture
- **@connection** - Connection management

## Key Files

- `FormsManager.cs` - Main coordinator
- `FormsManager.EnhancedOperations.cs` - CRUD operations
- `FormsManager.FormOperations.cs` - Form-level operations
- `FormsManager.Navigation.cs` - Navigation
- `FormsManager.ModeTransitions.cs` - Mode transitions
- `Helpers/RelationshipManager.cs` - Relationship management
- `Helpers/DirtyStateManager.cs` - Dirty state handling
- `Helpers/EventManager.cs` - Event coordination


## Repo Documentation Anchors

- DataManagementEngineStandard/Editor/Forms/Interfaces/README.md
- DataManagementEngineStandard/Editor/Forms/Helpers/README.md
- DataManagementEngineStandard/Editor/Forms/Configuration/README.md

