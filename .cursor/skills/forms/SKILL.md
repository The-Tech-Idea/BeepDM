---
name: forms
description: Entry-point guidance for FormsManager orchestration in DataManagementEngineStandard/Editor/Forms. Use when implementing Oracle Forms style behavior, master-detail blocks, mode transitions, navigation, dirty-state handling, event triggers, and performance/configuration patterns.
---

# Forms Manager Guide

Use this skill as the top-level entry point for `FormsManager` orchestration.  
For deeper workflows, read the specialized skills listed below.

## What This Module Is
- `FormsManager` is the coordinator for block registration, relationships, form lifecycle, navigation, mode transitions, and commit/rollback orchestration.
- It delegates core behavior to helpers: `RelationshipManager`, `DirtyStateManager`, `EventManager`, `FormsSimulationHelper`, `PerformanceManager`, `ConfigurationManager`.
- It is split into partial classes for maintainability: core registration, form operations, navigation, mode transitions, and enhanced operations.

## Use This Skill When
- You need to wire a new master-detail form with `UnitOfWork` blocks.
- You need safe transitions between Query and CRUD behavior.
- You need cross-block commit/rollback with dirty-state management.
- You are debugging trigger/event behavior or navigation side effects.
- You are tuning cache/performance and form-level configuration defaults.

## Fast Workflow
1. Create `FormsManager(editor)`.
2. Register all blocks with `RegisterBlock(...)` and valid `IEntityStructure`.
3. Define parent-child links with `CreateMasterDetailRelation(...)`.
4. Open form and run mode transitions (`EnterQueryModeAsync` -> `ExecuteQueryAndEnterCrudModeAsync`).
5. Use navigation/data operations and always inspect returned `IErrorsInfo` or `bool`.
6. Commit with `CommitFormAsync()` or rollback with `RollbackFormAsync()`.

## Specialized Skills
- Mode transitions and CRUD/query flow: [forms-mode-transitions](../forms-mode-transitions/SKILL.md)
- Form lifecycle and navigation: [forms-operations-navigation](../forms-operations-navigation/SKILL.md)
- Enhanced CRUD and query operations: [forms-enhanced-data-operations](../forms-enhanced-data-operations/SKILL.md)
- Helpers and trigger pipeline: [forms-helper-managers](../forms-helper-managers/SKILL.md)
- Performance/cache/config tuning: [forms-performance-configuration](../forms-performance-configuration/SKILL.md)

## Key Files
- `DataManagementEngineStandard/Editor/Forms/FormsManager.cs`
- `DataManagementEngineStandard/Editor/Forms/FormsManager.FormOperations.cs`
- `DataManagementEngineStandard/Editor/Forms/FormsManager.Navigation.cs`
- `DataManagementEngineStandard/Editor/Forms/FormsManager.ModeTransitions.cs`
- `DataManagementEngineStandard/Editor/Forms/FormsManager.EnhancedOperations.cs`
- `DataManagementEngineStandard/Editor/Forms/Helpers/*.cs`

## Baseline Example
```csharp
var forms = new FormsManager(editor);

using var customerUow = new UnitofWork<Customer>(editor, "MyDb", "Customers", "Id");
var customerStructure = editor.GetDataSource("MyDb").GetEntityStructure("Customers", true);
forms.RegisterBlock("CUSTOMERS", customerUow, customerStructure, "MyDb", isMasterBlock: true);

using var orderUow = new UnitofWork<Order>(editor, "MyDb", "Orders", "Id");
var orderStructure = editor.GetDataSource("MyDb").GetEntityStructure("Orders", true);
forms.RegisterBlock("ORDERS", orderUow, orderStructure, "MyDb", isMasterBlock: false);

forms.CreateMasterDetailRelation("CUSTOMERS", "ORDERS", "Id", "CustomerId");
await forms.OpenFormAsync("CustomerOrderForm");
await forms.EnterQueryModeAsync("CUSTOMERS");
await forms.ExecuteQueryAndEnterCrudModeAsync("CUSTOMERS");
```