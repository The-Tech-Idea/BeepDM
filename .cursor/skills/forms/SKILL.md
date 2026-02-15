---
name: forms
description: Guidance for FormsManager usage to provide Oracle Forms style behavior, including master-detail blocks and form operations.
---

# Forms Management Guide

Use this skill when implementing master-detail forms, handling mode transitions, and coordinating unit of work blocks.

## Core Concepts
- FormsManager coordinates blocks and navigation
- Blocks are backed by UnitofWork<T>
- Relationships drive detail filtering

## Workflow
1. Create `FormsManager(editor)`.
2. Register master and detail blocks with `UnitofWork<T>`.
3. Create relationships with master key and detail foreign key.
4. Use form operations like `CommitFormAsync`, `ClearFormAsync`.

## Validation
- Ensure entity structures exist for each block.
- Check `IErrorsInfo.Flag` on operations.
- Confirm relationships use correct key field names.

## Pitfalls
- Registering blocks before datasource is open causes null structures.
- Creating a new master record without handling dirty detail blocks can lose data.
- Using wrong key field names breaks relationship filtering.

## File Locations
- DataManagementEngineStandard/Editor/Forms/FormsManager.cs
- DataManagementEngineStandard/Editor/Forms/FormsManager.FormOperations.cs
- DataManagementEngineStandard/Editor/Forms/FormsManager.Navigation.cs

## Example
```csharp
var forms = new FormsManager(editor);

using var customerUow = new UnitofWork<Customer>(editor, "MyDb", "Customers", "Id");
var customerStruct = editor.GetDataSource("MyDb").GetEntityStructure("Customers", true);
forms.RegisterBlock("CUSTOMERS", customerUow, customerStruct, "MyDb", isMasterBlock: true);

using var orderUow = new UnitofWork<Order>(editor, "MyDb", "Orders", "Id");
var orderStruct = editor.GetDataSource("MyDb").GetEntityStructure("Orders", true);
forms.RegisterBlock("ORDERS", orderUow, orderStruct, "MyDb", isMasterBlock: false);

forms.CreateRelationship("CUSTOMERS", "ORDERS", "Id", "CustomerId");
await forms.ExecuteQueryAndEnterCrudModeAsync("CUSTOMERS", new List<AppFilter>());
```

## Task-Specific Examples

### Commit and Clear Form
```csharp
var commitResult = await forms.CommitFormAsync();
if (commitResult.Flag == Errors.Ok)
{
	await forms.ClearFormAsync();
}
```

### Handle Unsaved Changes Before Navigation
```csharp
if (forms.IsDirty)
{
	var result = await forms.HandleUnsavedChangesAsync(
		"CUSTOMERS",
		UnsavedChangesAction.Save);
	if (result.Flag != Errors.Ok)
	{
		return;
	}
}

await forms.NextRecordAsync("CUSTOMERS");
```