# Phase 8 - Integration with Defaults, Mapping, ETL, and Forms

## Objective
Formalize cross-module integration contracts so rules can be reused safely across BeepDM components.

## Scope
- Define integration APIs and expected rule contexts.
- Standardize parameter naming and output contracts.
- Add reference examples for each consuming module.

## File Targets
- `DataManagementEngineStandard/Rules/RulesEngine.cs`
- `DataManagementEngineStandard/Editor/Defaults/*` (touchpoint)
- `DataManagementEngineStandard/Editor/Mapping/*` (touchpoint)
- `DataManagementEngineStandard/Editor/ETL/*` (touchpoint)
- `DataManagementEngineStandard/Editor/Forms/*` (touchpoint)

## Planned Enhancements
- Shared context envelope (entity, datasource, user/session, execution mode).
- Contract adapters for module-specific rule invocation.
- Integration tests for realistic end-to-end scenarios.

## Acceptance Criteria
- Defaults/Mapping/ETL/Forms can evaluate rules through a stable contract.
- Rule invocation context is consistent across modules.
- Integration examples compile and pass regression tests.
