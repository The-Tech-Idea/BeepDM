---
name: forms
description: Entry-point guidance for FormsManager orchestration in BeepDM. Use when implementing Oracle Forms style behavior with block registration, master-detail coordination, mode transitions, navigation, dirty-state handling, trigger/event flow, and performance/configuration policies.
---

# Forms Manager Guide

Use this skill as the top-level entry point for `FormsManager` orchestration.

## Use this skill when
- Wiring a new block-based form over `UnitofWork` instances
- Coordinating master-detail behavior, form lifecycle, and navigation
- Deciding whether a change belongs in mode transitions, helpers, enhanced CRUD, or performance/configuration

## Do not use this skill when
- The task is only about direct `UnitofWork` behavior outside forms orchestration. Use [`unitofwork`](../unitofwork/SKILL.md).
- The task is only about import/sync/ETL pipelines. Use [`importing`](../importing/SKILL.md), [`beepsync`](../beepsync/SKILL.md), or [`etl`](../etl/SKILL.md).

## Architecture
- `FormsManager` is the coordinator class in `TheTechIdea.Beep.Editor.UOWManager`.
- Core responsibilities are split across partial classes:
  - registration and coordination in `FormsManager.cs`
  - lifecycle in `FormsManager.FormOperations.cs`
  - navigation in `FormsManager.Navigation.cs`
  - mode flow in `FormsManager.ModeTransitions.cs`
  - CRUD/query enhancements in `FormsManager.EnhancedOperations.cs`
- Helper managers own focused behavior:
  - `RelationshipManager`
  - `DirtyStateManager`
  - `EventManager`
  - `FormsSimulationHelper`
  - `PerformanceManager`
  - `ConfigurationManager`

## File Locations
- `DataManagementEngineStandard/Editor/Forms/FormsManager.Core.cs` (main coordinator, split across 30+ partial files)
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

## Specialized Skills
- [`forms-mode-transitions`](../forms-mode-transitions/SKILL.md)
- [`forms-operations-navigation`](../forms-operations-navigation/SKILL.md)
- [`forms-enhanced-data-operations`](../forms-enhanced-data-operations/SKILL.md)
- [`forms-helper-managers`](../forms-helper-managers/SKILL.md)
- [`forms-performance-configuration`](../forms-performance-configuration/SKILL.md)

## Integration with the data-management layer

`FormsManager` is the **runtime UI** of BeepDM. Every save flows down to a transactional layer; every read comes from already-configured state:

| Direction | Layer | What flows |
|---|---|---|
| → **unitofwork** | `UnitofWork<T>` | All form saves flow through UoW. Forms does not write to the datasource directly. |
| ← **configeditor** | `ConfigEditor` façade | Reads entity structure from config cache; falls back to `IDataSource.GetEntityStructure`. |
| → **migration** | `MigrationManager` | Schema drift detected by Forms is reported, not auto-migrated. |
| ← **setup** | Setup Framework | Setup is invisible; Forms is visible. After setup runs, Forms is what the user sees. |
| ↔ **etl** | Pipeline engine | Forms displays ETL output; Forms does not trigger ETL. |

The Mavis cross-project equivalent of this skill lives at `.harness/skills/beepdm-forms/SKILL.md`.

## Detailed Reference
Use [`reference.md`](./reference.md) for scenarios and examples.
