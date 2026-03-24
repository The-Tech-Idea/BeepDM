# Phase 3 - Expression Runtime and Type System

## Objective
Make expression evaluation deterministic through explicit type coercion, null semantics, and comparison rules.

## Scope
- Runtime evaluation path in `RulesEngine`.
- Type conversion contracts between tokens and runtime values.
- Rule-reference recursion and cycle detection behavior.

## File Targets
- `DataManagementEngineStandard/Rules/RulesEngine.cs`
- `DataManagementEngineStandard/Rules/GetSystemDate.cs`
- `DataManagementEngineStandard/Rules/GetRecordCount.cs`
- `DataManagementModelsStandard/Rules/RuleStructure.cs`

## Planned Enhancements
- Define coercion matrix (`string<->number`, `bool<->string`, date parsing policy).
- Introduce null propagation rules and explicit invalid-operation diagnostics.
- Add recursion depth and circular reference guard for rule references.

## Acceptance Criteria
- Runtime outputs are stable for equivalent inputs.
- Coercion edge cases are covered by test vectors.
- Cycle/reference overflow is reported with actionable diagnostics.
