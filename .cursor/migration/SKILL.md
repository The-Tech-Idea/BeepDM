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
- Treat `EnsureDatabaseCreated*` as create-if-missing only: existing entities are skipped and missing columns are not added. Use `ApplyMigrations*` when column evolution is required.
- Resolve manifest types deterministically: prefer `AssemblyHint`, then registered assemblies, then assembly-handler plugin assemblies (`Assemblies[].DllLib` and `LoadedAssemblies`), then broad AppDomain fallback.
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
- Count existing entities from `EnsureDatabaseCreated*` as skipped, not created; check existence before calling `EnsureEntity` when writing new ensure-created paths.
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
- Manifest type resolution can bind the wrong version if broad AppDomain scanning is preferred over `AssemblyHint` or `RegisterAssembly`; keep the deterministic resolution order intact.
- Broad assembly scanning can surface unrelated loader/version conflicts; prefer explicit-type migration to reduce that blast radius.
- File-based or schema-limited datasources may not support full DDL operations.
- Assuming every provider supports add/alter/drop column operations creates false confidence.
- Reusing a migration plan validated on SQL Server for Oracle, SQLite, or another provider without rerunning summary/validation is unsafe.
- Skipping CI/governance gates causes unsafe promotions and weak release evidence.
- Executing without rollback readiness makes incident recovery slower and riskier.
- Default `applyForeignKeys`/`applyIndexes` is `false`; an empty constraint list after a build call usually means the flags were not passed.
- Resume from checkpoint preserves `step.TargetName` -> `operation.TargetName`; rebuilding the plan without the checkpoint reuses the saved target. Without it, the executor falls back to a synthetic `step-N` identifier and rollback cannot drop the constraint by name.
- Lock-impact estimates for FK/Index ops (AddForeignKey +3 score, 18s baseline; CreateIndex +4 score, 25s baseline; DropForeignKey +6s; DropIndex +4s) are baseline assumptions; production windows must be sized with row counts.
- The model-interop cache is NOT cleared after `BuildMigrationPlanForModel` returns. The executor needs the ORM-shaped `EntityStructure` (with Relations/Indexes) for FK/Index step execution; clearing the cache would cause the executor to fall back to the classCreator view, silently applying no FKs or indexes.
- FK/Index execution is scoped by TargetName: `ApplyForeignKeysForEntity(desired, targetName)` and `ApplyIndexesForEntity(desired, targetName)` only apply the one FK/index the plan step targets, not every relation on the entity. The full-apply overloads still exist for callers that opt to apply all at once.
- Type-based plans (`BuildMigrationPlanForTypes`) now topologically sort operations so that a CreateEntity for entity A appears before any AddForeignKey that references A. The model-interop path already sorted by entity structure order; this brings the type-discovery path to parity.
- DropForeignKey/DropIndex steps without a TargetName now fail with a clear diagnostic rather than silently falling back to the synthetic `step-N` identifier, which was never a valid database constraint/index name.
- Telemetry includes per-operation-kind duration totals (`OperationKindTotalDurationMilliseconds`) in addition to the existing completion/failure counts, so operators can see "CreateIndex steps average 4500ms" without computing from audit trails.
- `PendingOperationCount` now includes all migration operation kinds (AddForeignKey, DropForeignKey, CreateIndex, DropIndex, AlterColumn, DropColumn, RenameEntity, RenameColumn, TruncateEntity), not just CreateEntity and AddMissingColumns. Approval reports and CI dashboards now show accurate pending-op counts for FK/Index-heavy plans.
- SelectModelInterop methods now have `detectRelationships` wired through rather than dead; the parameter was accepted but never passed.
- Rollout governance now detects first-run plans (no execution history) and skips KPI gates with a Warn instead of silently passing all of them. A hash-stability gate also blocks plans whose hash has diverged from the checkpointed hash.
- Rollout wave classification distinguishes Add (non-destructive: Wave2) from Drop (destructive: Wave3) FK/Index ops, rather than lumping all relational ops into Wave3Critical.
- Self-referencing FKs and excessive FKs per entity (>8) produce policy warnings. FK references to entities not in the plan produce Block-level findings.
- The readiness report now surfaces the FK and index count per entity, and warns about self-referencing FKs.
- The approval report shows the operation Note for every kind of operation (not just FK/Index with TargetName), so a `DropEntity` annotation like "archive data first" is visible to reviewers.
- The plan diff signature excludes RiskLevel, so a risk reclassification (Low→Medium) doesn't cause both an "added" and "removed" entry for the same operation.
- `ExportMigrationArtifacts` now includes PerformancePlan, CompensationPlan, and RollbackReadiness JSON alongside Plan/DryRun/CI artifacts.

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

## Integration with the data-management layer

`MigrationManager` does not run in isolation. Treat it as one node in a chain of responsibilities:

| Direction | Layer | What flows |
|---|---|---|
| ← **setup** | Setup Framework (Phase 4, `SchemaSetupStep`) | First-run schema creation. Setup composes migration; it does not duplicate it. |
| ↔ **configeditor** | `ConfigEditor.MigrationHistoryManager` | `IsMigrationApplied(ds, name)` gates re-runs; `RecordMigration(...)` records success. The persisted history is the source of truth. |
| ↔ **schema** | `ISchemaManager` | **Distinct service**: `MigrationManager` does DDL on a single datasource; `ISchemaManager` does cross-datasource preflight + sync-draft. They are different jobs. The preflight service may surface the need to run `MigrationManager` when destination columns are missing. |
| → **etl** | ETL pipelines | Pipelines assume schema exists. If a target entity is missing, ETL fails fast and reports back to migration. |
| ← **unitofwork** | `UnitofWork<T>` | UoW asserts the entity exists. UoW does not migrate; migration is upstream. |
| ← **forms** | `FormsManager` | Forms bind to migrated entities. Schema drift detected by Forms is reported, not auto-migrated. |

The Mavis cross-project equivalent of this skill lives at `.harness/skills/beepdm-migration/SKILL.md`.

## Detailed Reference
Use [`reference.md`](./reference.md) for API-level call patterns by lifecycle stage.
