---
name: forms-operations-navigation
description: Detailed guidance for FormsManager form lifecycle and navigation in BeepDM. Use when implementing OpenFormAsync, CloseFormAsync, CommitFormAsync, RollbackFormAsync, record movement, block switching, or navigation-driven synchronization behavior.
---

# Forms Operations And Navigation

Use this skill for end-user form lifecycle behavior and record navigation patterns.

## File Locations
- `DataManagementEngineStandard/Editor/Forms/FormsManager.FormOperations.cs`
- `DataManagementEngineStandard/Editor/Forms/FormsManager.Navigation.cs`
- `DataManagementEngineStandard/Editor/Forms/Helpers/RelationshipManager.cs`

## Core APIs
- form lifecycle: `OpenFormAsync`, `CloseFormAsync`, `CommitFormAsync`, `RollbackFormAsync`, `ClearAllBlocksAsync`, `ClearBlockAsync`, `ValidateForm`
- navigation: `FirstRecordAsync`, `NextRecordAsync`, `PreviousRecordAsync`, `LastRecordAsync`, `NavigateToRecordAsync`, `SwitchToBlockAsync`, `GetCurrentRecordInfo`, `GetAllNavigationInfo`

## Working Rules
1. Switch block explicitly before block-scoped actions.
2. Assume navigation can be cancelled by validation or unsaved-change policy.
3. Commit/rollback through form-level APIs, not manual per-block calls.
4. Preserve detail-block synchronization after navigation where configured.

## Related Skills
- [`forms`](../forms/SKILL.md)
- [`forms-helper-managers`](../forms-helper-managers/SKILL.md)
- [`forms-mode-transitions`](../forms-mode-transitions/SKILL.md)

## Detailed Reference
Use [`reference.md`](./reference.md) for safe lifecycle patterns, diagnostics, and pitfalls.
