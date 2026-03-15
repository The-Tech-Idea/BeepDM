# Editor Migration

## Purpose
This folder contains migration orchestration for schema and data transitions between BeepDM versions or provider models.

## Key Files
- `IMigrationManager.cs`: Migration contract and migration summary output model.
- `MigrationManager.cs`: Concrete migration planning and execution pipeline.

## Runtime Flow
1. Prefer explicit entity-type migration when the application owns a known schema.
2. Use discovery-based migration only when entity ownership is dynamic or plugin-driven.
3. Validate compatibility and required transformation steps.
4. Execute migration operations and collect `MigrationSummary` diagnostics.

## Extension Guidelines
- Keep migration operations idempotent where possible.
- Surface partial-failure details clearly in summaries.
- Reuse existing helper abstractions for provider-specific SQL/script generation.
- Log loader/type resolution failures clearly when discovery scans partially load assemblies.
- Keep the explicit-type path stable and simple; treat broad assembly scanning as a fallback, not the primary app path.

## Provider Best Practices
- Oracle: keep table and column names conservative, validate identity/sequence behavior explicitly, and prefer additive schema evolution over complex in-place rewrites.
- SQL Server: review lock impact, default constraints, and index rebuild implications before applying type changes or nullability changes on large tables.
- SQLite and other file-backed providers: treat destructive alters as exceptional, prefer create-missing/add-column workflows, and expect helper capability to be narrower than server RDBMS platforms.
- Cross-platform providers: use migration summary plus helper validation on the real target datasource before rollout; do not assume DDL that works on one provider is portable to another.
- Operationally: separate schema creation from seed/data bootstrap, and keep application seed flows from calling migration unless schema preparation is the explicit goal.

## Testing Focus
- Backward-compatible migrations with additive schema changes.
- Conflict handling for incompatible field types and key changes.
- Rollback/compensation behavior for mid-run failures.
- Assembly discovery resilience when unrelated loaded assemblies have version or loader issues.
- Provider-specific verification for Oracle, SQL Server, SQLite, and any other production target rather than relying on a single test database.
