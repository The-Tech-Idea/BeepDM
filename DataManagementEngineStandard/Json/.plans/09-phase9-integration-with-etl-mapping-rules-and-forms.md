# Phase 9 - Integration with ETL, Mapping, Rules, and Forms

## Objective
Formalize Json integration contracts across dependent BeepDM modules.

## Scope
- ETL ingestion/output contracts.
- Mapping alignment for inferred schema and path fields.
- Rules and Forms query/update integration touchpoints.

## File Targets
- `Json/JsonDataSource.cs`
- `Json/JsonDataSourceAdvanced.cs`
- `Json/Helpers/*`

## Planned Enhancements
- Shared context envelope for downstream modules.
- Mapping-ready path metadata export.
- Rule-evaluation and form-state hooks for JSON entities.

## Acceptance Criteria
- ETL/Mapping/Rules/Forms integration tests pass end-to-end.
- Context contracts are documented and versioned.
- Cross-module diagnostics are correlated by operation id.
