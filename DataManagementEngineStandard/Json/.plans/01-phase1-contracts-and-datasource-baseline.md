# Phase 1 - Contracts and Datasource Baseline

## Objective
Stabilize Json datasource contracts and clarify baseline responsibilities between datasource and helpers.

## Scope
- Unify core/advanced datasource surface.
- Define capability metadata and execution options.

## File Targets
- `Json/JsonDataSource.cs`
- `Json/JsonDataSourceAdvanced.cs`
- `Json/README.md`

## Planned Enhancements
- Capability profile (supports filtering, deep graph hydration, async).
- Explicit options model (path mode, null semantics, relation loading behavior).
- Lean partial-class split by concern (query, CRUD, schema, graph).

## Acceptance Criteria
- Datasource behavior is deterministic and discoverable by capability.
- Public methods map to clear responsibility boundaries.
- Baseline contract tests pass.
