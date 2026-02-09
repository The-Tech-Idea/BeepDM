# Forms Interfaces

## Purpose
This folder defines contracts for form-oriented unit-of-work orchestration, including dirty-state tracking, event dispatch, relationship handling, simulation, and configuration.

## Key Interfaces
- `IUnitofWorksManager`: Coordinator for form block and record operations.
- `IRelationshipManager`: Master/detail and block-relationship management.
- `IDirtyStateManager`: Unsaved-change detection and save/rollback orchestration.
- `IEventManager`: UI-facing trigger and validation event contract.
- `IFormsSimulationHelper`: Runtime variable and field behavior simulation.
- `IPerformanceManager`: Block cache and performance metrics contract.
- `IConfigurationManager`: Form runtime configuration lifecycle.

## Integration Notes
- Interfaces are designed to keep UI framework code separate from data logic.
- Preserve async entry points for save/rollback and synchronization operations.
- Keep validation hooks consistent so forms and APIs can share rule logic.
