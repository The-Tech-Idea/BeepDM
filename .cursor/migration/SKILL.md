---
name: migration
description: Guidance for MigrationManager planning, safety policy, dry-run/preflight, execution checkpoints, rollback/compensation, CI validation, and rollout governance in BeepDM. Use when designing or running datasource-agnostic schema creation/upgrade flows from Entity types and EntityStructure metadata.
---

# Migration Guide

Use this skill when planning, validating, executing, or governing schema migrations with `MigrationManager`.

## Use this skill when
- Creating databases from Entity/POCO types.
- Adding missing tables or columns through datasource-agnostic migration flows.
- Building migration plans before apply (`BuildMigrationPlan*`).
- Running safety checks (policy, dry-run, preflight, impact, performance).
- Executing migrations with checkpoints and resume support.
- Preparing rollback/compensation and readiness evidence.
- Running CI gates and rollout wave promotion checks.
- Troubleshooting discovery, capability, portability, or governance failures.

## Do not use this skill when
- The task is only about CRUD or transactional app logic. Use [`unitofwork`](../unitofwork/SKILL.md) or [`idatasource`](../idatasource/SKILL.md).
- The task is only about connection definition or config persistence. Use [`connection`](../connection/SKILL.md) and [`configeditor`](../configeditor/SKILL.md).

## Core Capabilities
- Discover entity types across assemblies.
- Register assemblies explicitly for discovery.
- Ensure a database/schema exists from Entity types or `EntityStructure`.
- Apply migrations for missing entities or columns.
- Optionally add foreign keys and indexes on RDBMS targets via opt-in flags.
- Build immutable migration plan artifacts and hashes.
- Evaluate plan policy and protected-environment safety decisions.
- Generate dry-run DDL previews, preflight checks, and impact reports.
- Execute with retries, deterministic step state, checkpoint persistence, and resume.
- Build compensation plans, rollback-readiness checks, and rollback simulations.
- Capture telemetry, diagnostics, and auditable lifecycle events.
- Run CI validation gates and export approval-ready artifacts.
- Evaluate rollout wave promotion with KPI thresholds and hard-stop rules.
- Honor ORM/POCO-shaped `EntityStructure` via the model-interop cache for EF Core and other ORM callers.
- Surface FK/Index target names and counts throughout the artifact chain (plan op, step, dry-run, impact, perf annotation, summary, approval report).

## ClassCreator Integration for Discovery

`IsEntityType` in `MigrationManager.Discovery.cs` now delegates to
`IClassCreator.IsEfDecoratedType` and `IClassCreator.IsDiscoverablePoco` when a
type does not inherit from `Entity` or implement `IEntity`. This means the
discovery pipeline can pick up:

- EF Core POCOs decorated with `Table`, `Column`, `Key`, `ForeignKey`, etc.
- Plain POCOs that are concrete, public, non-generic, and have a parameterless
  constructor or are record types

without the user subclassing `Entity`. Both methods are implemented on the helpers
(`EfCoreToEntityGeneratorHelper` / `PocoToEntityGeneratorHelper`) and surfaced
through `IClassCreator`.

To add a new kind of discoverable type without touching the MigrationManager,
implement a new helper method and add a delegation to `ClassCreator.PocoToEntity.cs`
(which already delegates `ScanNamespaceForEfCoreClasses`, `ConvertEfCoreTypeToEntityStructure`, etc.).

## Design Rules From Source
- Pass .NET type names through `EntityStructure`; do not pre-map to provider-native types.
- Prefer `IDataSource.CreateEntityAs(entity)` for datasource-agnostic entity creation.
- Use `IDataSourceHelper` for validation and column-level DDL when supported.
- Treat helper support and datasource capabilities as conditional, not guaranteed.
- Prefer explicit-type migration (`EnsureDatabaseCreatedForTypes` / `ApplyMigrationsForTypes`) for application-owned schemas where the entity list is known at compile time.
- Use discovery-based migration only when entity ownership is dynamic or plugin-driven, and register assemblies explicitly before relying on broad assembly scanning.
- Keep migration planning non-destructive: build/evaluate/approve plan artifacts before execution.
- Treat destructive, type-narrowing, and nullability-tightening operations as high risk requiring compensation/rollback planning.
- Use rollout governance for production promotion; do not promote a blocked or hard-stopped wave.
- Foreign key and index generation is opt-in: pass `applyForeignKeys: true` and/or `applyIndexes: true` on the build/apply API. Default is `false` to preserve pre-FK behavior.
- FK and index operations are gated by `MigrationProviderCapabilityProfile.SupportsForeignKeys` / `SupportsIndexes`. When the provider cannot express the constraint, policy raises `provider-fallback-missing-foreign-key` / `provider-fallback-missing-index` and the operation is dropped or flagged for fallback.
- FK and index names are threaded through `MigrationPlanOperation.TargetName` -> `MigrationExecutionStep.TargetName` -> `MigrationCompensationAction.TargetName` so the executor, dry-run DDL, and rollback all use the same identifier.
- The 4 new `MigrationPlanOperationKind` values are `AddForeignKey`, `DropForeignKey`, `CreateIndex`, `DropIndex`. Plan lint requires `TargetName` for all four. Rollout wave classification treats any plan containing FK/Index ops on RDBMS as `Wave3Critical` unless the caller overrides.
- The model-interop cache (`BuildMigrationPlanForModel` -> `TryGetEntityStructure`) lets EF Core and other ORMs get ORM-shaped `EntityStructure` (with `Indexes`, navigation FKs, `OnDeleteBehavior`, `OnUpdateBehavior`) instead of reflection-only shapes.

## Recommended Workflow
1. Create `MigrationManager(editor, dataSource)` and ensure `MigrateDataSource` is set.
2. Register extra assemblies if entity types live outside normal discovery paths.
3. Prefer explicit entity types for stable app schemas; use discovery only when entity set is unknown.
4. Build plan (`BuildMigrationPlanForTypes` or `BuildMigrationPlan`) and review operations.
5. Evaluate policy (`EvaluateMigrationPlanPolicy`) and block on unsafe decisions.
6. Generate dry-run/preflight/impact/performance reports.
7. Build compensation and rollback-readiness evidence.
8. Run CI gates (`ValidatePlanForCi`) and rollout governance (`EvaluateRolloutGovernance`).
9. Approve plan (`ApproveMigrationPlan`) and execute (`ExecuteMigrationPlan`).
10. On failure, inspect checkpoint/diagnostics, resume or rollback with compensation.

## Validation and Safety
- Ensure `MigrateDataSource` is not null before migration operations.
- Prefer `BuildMigrationPlan*` before applying changes.
- Use `ValidatePlanForCi` and `EvaluateRolloutGovernance` in pipeline-driven environments.
- Keep `EntityStructure.Fieldtype` values as .NET type names; let the datasource map them.
- Expect column-level DDL to vary by provider and helper capability.
- Treat loader/type mismatches during discovery as an assembly-registration or versioning problem first; inspect migration logs before retrying with broader scanning.
- Require backup/restore-test evidence for protected environments before execute.
- Plan lint blocks any FK or Index operation with empty `TargetName`; CI must fail the plan.
- For RDBMS plans containing FK/Index ops, treat the rollout as `Wave3Critical` by default; do not silently downgrade.
- Approval report must show `target=` and `note` for every FK/Index op so reviewers can audit constraint names.
- Preflight lock-impact-estimate counts `AddForeignKey` and `CreateIndex` as heavy ops; a plan with many of these will trigger the maintenance-window recommendation, not just plans with column alters.
- Dry-run and dry-run-rollback output includes the actual SQL preview (for FK/Index ops the constraint/index DDL), not just the playbook text.
- `MigrationPerformancePlan.MaintenanceWindowGuidance` explicitly calls out FK/Index op counts when they appear in a plan, so the reviewer sees *what* drove the recommendation, not just that one was made.

## Provider Best Practices
- Oracle: keep identifiers short and stable, avoid relying on case-sensitive quoted names, and validate sequence/identity expectations before assuming auto-number behavior.
- Oracle: prefer additive migrations and staged data moves for type changes; table rewrites and constrained `ALTER` operations are more operationally sensitive than in SQL Server.
- SQL Server: treat schema changes as online/offline decisions explicitly, especially for large tables and indexed columns; do not assume every `ALTER` is cheap in production.
- SQL Server: check default constraints, existing indexes, and nullability transitions before adding columns or changing types, because helper-generated DDL may still need operational review.
- SQLite and file-backed stores: prefer create-if-missing and additive patterns only; destructive alters, renames, and complex column changes are limited or emulated.
- PostgreSQL/MySQL/other RDBMS platforms: validate reserved words, casing, and helper capability before rollout; generated DDL is only safe when the target helper actually supports that operation.
- Cross-platform: keep entity names deterministic, avoid provider-specific type assumptions in entity classes, and test migrations against the real target engine rather than trusting one provider's behavior.

## Pitfalls
- Pre-mapping types breaks `CreateEntityAs` and corrupts datasource-agnostic behavior.
- Missing or unregistered assemblies cause discovery to return zero entities.
- Broad assembly scanning can surface unrelated loader/version conflicts; prefer explicit-type migration to reduce that blast radius.
- File-based or schema-limited datasources may not support full DDL operations.
- Assuming every provider supports add/alter/drop column operations creates false confidence.
- Reusing a migration plan validated on SQL Server for Oracle, SQLite, or another provider without rerunning summary/validation is unsafe.
- Skipping CI/governance gates causes unsafe promotions and weak release evidence.
- Executing without rollback readiness makes incident recovery slower and riskier.
- Default `applyForeignKeys`/`applyIndexes` is `false`; an empty constraint list after a build call usually means the flags were not passed.
- Resume from checkpoint preserves `step.TargetName` -> `operation.TargetName`; rebuilding the plan without the checkpoint reuses the saved target. Without it, the executor falls back to a synthetic `step-N` identifier and rollback cannot drop the constraint by name.
- Lock-impact estimates for FK/Index ops (AddForeignKey +3 score, 18s baseline; CreateIndex +4 score, 25s baseline; DropForeignKey +6s; DropIndex +4s) are baseline assumptions; production windows must be sized with row counts.

## File Locations
- `DataManagementEngineStandard/Editor/Migration/IMigrationManager.cs`
- `DataManagementEngineStandard/Editor/Migration/MigrationManager.cs`
- `DataManagementEngineStandard/Editor/Migration/README.md`
- `DataManagementEngineStandard/Editor/Migration/Examples/`
- `DataManagementModelsStandard/Editor/IDataSourceHelper.cs`

## Quick Example
```csharp
var migration = new MigrationManager(editor, dataSource);
var types = new[] { typeof(Customer), typeof(Order) };

var plan = migration.BuildMigrationPlanForTypes(types, detectRelationships: true);
var ci = migration.ValidatePlanForCi(plan);
var gov = migration.EvaluateRolloutGovernance(plan);
if (!ci.CanMerge || !gov.CanPromote)
    throw new InvalidOperationException("Migration blocked by CI/governance gates.");

var execute = migration.ExecuteMigrationPlan(plan);
if (!execute.Success)
    Console.WriteLine(execute.Message);
```

## Task-Specific Examples
- Full examples: `DataManagementEngineStandard/Editor/Migration/Examples/00-overview.md`
- Plan and policy: `Examples/01-plan-and-policy.md`
- Dry-run/preflight/impact: `Examples/02-dryrun-preflight-impact.md`
- Execution/checkpoint/resume: `Examples/03-execution-checkpoint-resume.md`
- Rollback/compensation: `Examples/04-rollback-compensation.md`
- CI/artifacts: `Examples/05-ci-and-artifacts.md`
- Rollout governance: `Examples/06-rollout-governance.md`
- EF Core / ORM interop and the model-interop cache: `Examples/07-efcore-interop.md`

## Related Skills
- [`beepdm`](../beepdm/SKILL.md)
- [`idatasource`](../idatasource/SKILL.md)
- [`configeditor`](../configeditor/SKILL.md)

## Detailed Reference
Use [`reference.md`](./reference.md) for API-level call patterns by lifecycle stage.
