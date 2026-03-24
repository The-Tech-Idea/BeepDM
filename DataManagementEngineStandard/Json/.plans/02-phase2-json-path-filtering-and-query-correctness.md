# Phase 2 - JSON Path, Filtering, and Query Correctness

## Objective
Harden query semantics for JSON path navigation and filtering.

## Scope
- Path traversal correctness.
- Predicate handling and filter edge cases.
- Deterministic result ordering and null behavior.

## File Targets
- `Json/Helpers/JsonPathNavigator.cs`
- `Json/Helpers/JsonFilterHelper.cs`
- `Json/JsonDataSource.cs`

## Planned Enhancements
- Standardize path syntax and unsupported construct diagnostics.
- Add strict/lenient query modes.
- Define deterministic behavior for missing paths and heterogeneous arrays.

## Acceptance Criteria
- Same query on same document returns deterministic output.
- Edge-case query suite passes (nulls, arrays, missing keys, mixed types).
- Diagnostics are structured and actionable.
