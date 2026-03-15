---
name: forms-enhanced-data-operations
description: Detailed guidance for FormsManager enhanced CRUD and query operations in BeepDM. Use when implementing CreateNewRecord, InsertRecordEnhancedAsync, UpdateCurrentRecordAsync, or ExecuteQueryEnhancedAsync with validation, relationship sync, and audit/default handling.
---

# Forms Enhanced Data Operations

Use this skill when operating on data through `FormsManager.EnhancedOperations`.

## File Locations
- `DataManagementEngineStandard/Editor/Forms/FormsManager.EnhancedOperations.cs`
- `DataManagementEngineStandard/Editor/Forms/Helpers/FormsSimulationHelper.cs`
- `DataManagementEngineStandard/Editor/Forms/Helpers/RelationshipManager.cs`

## Core APIs
- `CreateNewRecord(...)`
- `InsertRecordEnhancedAsync(...)`
- `UpdateCurrentRecordAsync(...)`
- `ExecuteQueryEnhancedAsync(...)`
- `GetCurrentRecord(...)`
- `GetRecordCount(...)`
- `CopyFields(...)`
- `ApplyAuditDefaults(...)`

## Working Rules
1. Prefer enhanced methods over ad-hoc reflection in callers.
2. Keep operations mode-aware; enter query/CRUD via `FormsManager` first.
3. Preserve validation and relationship synchronization after successful DML.
4. Treat warnings distinctly from failures after query execution.

## Related Skills
- [`forms`](../forms/SKILL.md)
- [`forms-mode-transitions`](../forms-mode-transitions/SKILL.md)
- [`forms-helper-managers`](../forms-helper-managers/SKILL.md)

## Detailed Reference
Use [`reference.md`](./reference.md) for flow examples, pitfalls, and verification checks.
