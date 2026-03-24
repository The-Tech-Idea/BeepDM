# Phase 5 - CRUD Consistency and Transaction-like Safety

## Objective
Improve write-path consistency, conflict handling, and failure recovery for JSON operations.

## Scope
- CRUD helper behavior and conflict policies.
- Partial failure handling and rollback/compensation patterns.
- Concurrency checks for multi-writer scenarios.

## File Targets
- `Json/Helpers/JsonCrudHelper.cs`
- `Json/Helpers/JsonDataHelper.cs`
- `Json/JsonDataSourceAdvanced.cs`

## Planned Enhancements
- Version/etag-like change tokens for optimistic concurrency.
- Batch write safety with compensation strategy.
- Clear conflict diagnostics and retry policy hooks.

## Acceptance Criteria
- CRUD operations provide consistent outcomes under concurrent updates.
- Partial failures produce deterministic recovery paths.
- Conflict cases are observable and actionable.
