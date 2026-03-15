# Forms Management Quick Reference

## Initialization

```csharp
var formsManager = new FormsManager(dmeEditor);
```

## Block Registration

```csharp
// Register block
formsManager.RegisterBlock("CUSTOMERS", customerUow, customerStructure, "MyDatabase", isMasterBlock: true);

// Create relationship
formsManager.CreateRelationship("CUSTOMERS", "ORDERS", "Id", "CustomerId");
```

## Mode Transitions

```csharp
// Enter query mode
await formsManager.EnterQueryModeAsync("CUSTOMERS");

// Execute query and enter CRUD mode
await formsManager.ExecuteQueryAndEnterCrudModeAsync("CUSTOMERS", filters);

// Create new record (handles unsaved changes)
await formsManager.CreateNewRecordInMasterBlockAsync("CUSTOMERS");
```

## Form Operations

```csharp
await formsManager.CommitFormAsync();        // COMMIT_FORM
await formsManager.ClearFormAsync();         // CLEAR_FORM
await formsManager.ExecuteQueryAsync(...);    // EXECUTE_QUERY
await formsManager.DeleteRecordAsync(...);    // DELETE_RECORD
```

## Navigation

```csharp
await formsManager.FirstRecordAsync("CUSTOMERS");
await formsManager.LastRecordAsync("CUSTOMERS");
await formsManager.NextRecordAsync("CUSTOMERS");
await formsManager.PreviousRecordAsync("CUSTOMERS");
var record = formsManager.GetCurrentRecord("CUSTOMERS");
```

## CRUD Operations

```csharp
await formsManager.CreateRecordAsync<Customer>("CUSTOMERS", c => { c.Name = "John"; });
await formsManager.UpdateRecordAsync<Customer>("CUSTOMERS", c => { c.Name = "Updated"; });
await formsManager.DeleteRecordAsync("CUSTOMERS");
```

## Unsaved Changes

```csharp
bool isDirty = formsManager.IsDirty;
await formsManager.HandleUnsavedChangesAsync("CUSTOMERS", UnsavedChangesAction.Save);
```
