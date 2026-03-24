# Phase 8 - Performance, Locking, and Scale Strategy

## Objective
Define large-scale migration execution patterns that minimize downtime and lock contention.

## Scope
- Performance strategy for large schemas and data volumes.
- Lock-aware execution windows and operation batching.

## File Targets
- `Migration/MigrationManager.cs`

## Planned Enhancements
- Batch and chunked operation execution for large migrations.
- Online/offline strategy annotations per operation.
- Lock/timeout policy configuration by datasource type.
- Maintenance-window and throttling guidance in plan metadata.

## Acceptance Criteria
- Large migration plans include lock and runtime impact estimates.
- Execution supports throttled mode for production safety.
- Performance KPIs are defined for migration windows.
