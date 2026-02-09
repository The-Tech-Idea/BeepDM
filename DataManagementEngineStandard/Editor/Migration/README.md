# Editor Migration

## Purpose
This folder contains migration orchestration for schema and data transitions between BeepDM versions or provider models.

## Key Files
- `IMigrationManager.cs`: Migration contract and migration summary output model.
- `MigrationManager.cs`: Concrete migration planning and execution pipeline.

## Runtime Flow
1. Build a migration plan from source and target metadata.
2. Validate compatibility and required transformation steps.
3. Execute migration operations and collect `MigrationSummary` diagnostics.

## Extension Guidelines
- Keep migration operations idempotent where possible.
- Surface partial-failure details clearly in summaries.
- Reuse existing helper abstractions for provider-specific SQL/script generation.

## Testing Focus
- Backward-compatible migrations with additive schema changes.
- Conflict handling for incompatible field types and key changes.
- Rollback/compensation behavior for mid-run failures.
