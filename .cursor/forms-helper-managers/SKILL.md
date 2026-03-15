---
name: forms-helper-managers
description: Detailed guidance for FormsManager helper architecture in BeepDM. Use when extending RelationshipManager, DirtyStateManager, EventManager, FormsSimulationHelper, or related helper-driven behavior behind forms orchestration.
---

# Forms Helper Managers

Use this skill when touching helper classes under `Editor/Forms/Helpers`.

## File Locations
- `DataManagementEngineStandard/Editor/Forms/Helpers/RelationshipManager.cs`
- `DataManagementEngineStandard/Editor/Forms/Helpers/DirtyStateManager.cs`
- `DataManagementEngineStandard/Editor/Forms/Helpers/EventManager.cs`
- `DataManagementEngineStandard/Editor/Forms/Helpers/FormsSimulationHelper.cs`
- `DataManagementEngineStandard/Editor/Forms/Helpers/PerformanceManager.cs`

## Responsibilities
- `RelationshipManager`: master-detail relation registration and synchronization
- `DirtyStateManager`: unsaved-change analysis and save/rollback decisions
- `EventManager`: trigger/event pipeline and unit-of-work subscriptions
- `FormsSimulationHelper`: reflection-based field access, audit defaults, sequences, and system variables
- `PerformanceManager`: cache and metrics support for block access

## Working Rules
1. Register blocks before relationship creation.
2. Preserve centralized event subscription/unsubscription paths.
3. Keep dirty-state checks in navigation and mode transitions.
4. Reuse simulation helpers instead of duplicating reflection logic in callers.

## Related Skills
- [`forms`](../forms/SKILL.md)
- [`forms-operations-navigation`](../forms-operations-navigation/SKILL.md)
- [`forms-performance-configuration`](../forms-performance-configuration/SKILL.md)

## Detailed Reference
Use [`reference.md`](./reference.md) for helper responsibilities, trigger patterns, and pitfalls.
