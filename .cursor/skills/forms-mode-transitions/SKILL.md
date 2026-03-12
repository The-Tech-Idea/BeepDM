---
name: forms-mode-transitions
description: Detailed guidance for FormsManager mode transitions in BeepDM. Use when working with EnterQueryModeAsync, ExecuteQueryAndEnterCrudModeAsync, EnterCrudModeForNewRecordAsync, and related readiness or validation checks across blocks.
---

# Forms Mode Transitions

Use this skill when implementing or debugging mode-aware behavior in `FormsManager`.

## File Locations
- `DataManagementEngineStandard/Editor/Forms/FormsManager.ModeTransitions.cs`
- `DataManagementEngineStandard/Editor/Forms/FormsManager.EnhancedOperations.cs`
- `DataManagementEngineStandard/Editor/Forms/Helpers/DirtyStateManager.cs`

## Core APIs
- `EnterQueryModeAsync(...)`
- `ExecuteQueryAndEnterCrudModeAsync(...)`
- `EnterCrudModeForNewRecordAsync(...)`
- `CreateNewRecordInMasterBlockAsync(...)`
- `ValidateAllBlocksForModeTransitionAsync()`
- `GetBlockMode(...)`
- `GetAllBlockModeInfo()`
- `IsFormReadyForModeTransitionAsync()`

## Working Rules
1. Never force mode changes by mutating block state externally.
2. Always inspect returned `IErrorsInfo`.
3. Resolve unsaved changes and parent context before entering detail CRUD flows.
4. Preserve status updates and event-trigger semantics around transitions.

## Related Skills
- [`forms`](../forms/SKILL.md)
- [`forms-enhanced-data-operations`](../forms-enhanced-data-operations/SKILL.md)
- [`forms-helper-managers`](../forms-helper-managers/SKILL.md)

## Detailed Reference
Use [`reference.md`](./reference.md) for transition flows, failure patterns, and debug checks.
