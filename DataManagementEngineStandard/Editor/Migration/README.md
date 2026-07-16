# Migration Manager

> **Read this first — verified status (code + tests, not aspiration).** `MigrationManager` is real,
> sophisticated code (~10.8k LOC, 18 partials) — but until recently it had **zero tests**. A test
> harness now exists (`tests/MigrationManagerTests`, a Moq recording fake that drives the real manager
> with no live DB). Verification confirmed the additive path is correct and consistent, and pinned
> several **structural gaps where a documented capability is unreachable via the normal planning path**.
> The rest of this document describes the intended design; the gaps below are what actually holds today.
>
> **Governed destructive migration — NOW SUPPORTED (was gap #1):** `BuildMigrationPlan*` accepts
> `includeDestructive: true`, which emits `DropColumn` operations for columns the live datasource still
> has but the model dropped (marked `IsDestructive`/high-risk; **off by default** — dropping is data loss).
> `ExecuteStep` now runs `DropColumn`/`AlterColumn`/`DropEntity`/`TruncateEntity`/`RenameEntity`/`RenameColumn`
> through the per-datasource `ISchemaMigrationProvider` (no raw DDL). The destructive-change **policy gate,
> compensation, and rollback-readiness are now reachable and enforced**: a destructive plan is blocked at
> preflight unless the caller passes approval (`ExecuteMigrationPlan(plan, policyOptions: <approver+override>)`)
> and supplies backup/restore evidence on the readiness report. Proven end-to-end in `DestructiveExecutionTests`.
> *Column type changes (`AlterColumn`) are still not auto-planned* — a reflected .NET type vs a live DB
> type is not a reliable equality signal, so it would false-positive on every column; use the imperative
> `AlterColumn(...)` (the executor runs it either way).
>
> **Durable resume + idempotency — NOW SUPPORTED:** execution checkpoints are re-hydrated from the
> persisted history JSON, so `ResumeMigrationPlan`/`GetExecutionCheckpoint` survive a process restart
> (`TryLoadPersistedCheckpoint`, exercised in `ExecutionCheckpointResumeTests`). `IsMigrationApplied(name)`
> and `RecordMigration(name, success, notes)` now exist (`MigrationManager.Idempotency.cs`): a named
> migration is recorded once and gated on re-run (only *successful* records count). Both read/write
> through `IConfigEditor` (which now declares `LoadMigrationHistory`/`AppendMigrationRecord`/`SaveMigrationHistory`).
>
> **Verified structural gaps (pinned by `tests/MigrationManagerTests`):**
> 1. **Compensation/rollback is inert for *additive* plans.** `BuildCompensationPlan` only emits actions
>    for high-risk **or relational (FK/index)** ops; a plain `CreateEntity`/`AddMissingColumns` plan
>    gets an **empty** compensation plan, so `RollbackFailedExecution` has nothing to undo — even
>    though `CreateEntity` is *declared* reversible. This is what `SchemaSetupStep.RollbackAsync`
>    (Setup framework) hits. The compensation engine itself is correct when fed a qualifying op
>    (a destructive `DropColumn` plan **does** get compensation actions — that path is now exercised).
> 2. **`AddMissingColumns` has no auto-rollback** (forward-fix/manual only).
> 3. **Impact/performance analysis is hardcoded heuristics** — no row-count/size probing.
> 4. **The plan hash doesn't fingerprint a `CreateEntity`'s column set** — two different schemas for
>    the same new entity hash identically.
>
> **Hygiene — resolved:** the `MigrationHistory`/`MigrationRecord`/`MigrationStep` POCOs were defined
> identically in **both** shipped assemblies under the same namespace (a latent CS0433 for any consumer
> referencing both packages). The `DataManagementEngine` copy has been removed; the canonical types now
> live only in `DataManagementModels`, with `[assembly: TypeForwardedTo]` (`DataManagementEngineStandard/TypeForwards.cs`)
> preserving binary compat. The 13 previously-silent `catch{}` in `Discovery.cs`/`ManifestParser.cs`/`MigrationTrackingService.cs`
> now **report through `IDMEEditor.AddLogMessage` at `Errors.Warning`** with the assembly/type context and
> the exception message — a real load fault (missing/locked/bad-image/version-conflict assembly,
> `TypeLoadException`, transient connection failure) is surfaced instead of vanishing, while the flow
> still falls through to the next candidate. (The manifest type-probes use `GetType(throwOnError:false)`,
> which returns null for a merely-absent type, so these logs fire only on genuine faults, not on every miss.)
>
> **Hygiene — resolved:** `SchemaManager` has been **dissolved** — `MigrationManager` is the single
> schema drift+change authority. Its dead DDL surface was deleted; its **full-drift detection** moved in
> as `MigrationManager.InspectDrift(...)` (`MigrationManager.Drift.cs`, using the shared `SchemaComparator`
> — so drift now sees drops/alters, closing the *detection* half of the additive-only gap and ending the
> double-diff). Cross-datasource **sync preflight/draft** moved to the stateless `Editor/Schema/SyncSchemaPreflight`
> helper (shared by `DataImportManager` + `BeepSyncManager`). `SchemaComparator`/`SchemaFingerprinter`/snapshot
> models remain as shared primitives; the unused snapshot store + baseline methods were removed.
>
> **Known hygiene items (tracked, architectural — not resolved):** a second `MigrationProviderRegistry`
> on `ConfigEditor` is dead (no fallbacks, never read — annotated in-code); `CreateEntity`/`EnsureEntity`
> bypass the provider that alter/drop use. See `.plans/migration/`.
>
> Gaps 1–4 above remain opt-in future work.

## Purpose
`MigrationManager` is the schema-migration orchestration layer for BeepDM.  
It supports:
- classic entity-level schema operations
- discovery and explicit-type migration workflows
- planning/policy/dry-run/preflight/impact analysis
- resumable execution with checkpoints
- rollback/compensation readiness and actions
- observability, CI validation, and rollout governance gates

## Architecture
Implementation uses partial classes to keep responsibilities isolated:
- `MigrationManager.cs`: core shell, shared helpers, migration tracking
- `MigrationManager.AssemblyRegistration.cs`: assembly registration APIs
- `MigrationManager.Discovery.cs`: discovery-based migration and readiness
- `MigrationManager.ReadinessAndExplicit.cs`: explicit-type readiness and migration
- `MigrationManager.EntityOperations.cs`: schema operations (create/alter/drop/rename/index)
- `MigrationManager.Capabilities.cs`: provider capability profile generation
- `MigrationManager.Policy.cs`: compatibility and safety policy engine
- `MigrationManager.Planning.cs`: plan artifact generation and orchestration
- `MigrationManager.DryRunAndPreflight.cs`: dry-run SQL preview, preflight, impact
- `MigrationManager.ExecutionOrchestration.cs`: execution, retries, checkpoints, resume
- `MigrationManager.RollbackCompensation.cs`: compensation plans and rollback flow
- `MigrationManager.Observability.cs`: telemetry, diagnostics, audit trail
- `MigrationManager.PerformanceScale.cs`: lock/runtime strategy and performance plan
- `MigrationManager.DevExAutomation.cs`: CI gates, plan diff, artifact export
- `MigrationManager.RolloutGovernance.cs`: wave promotion and KPI hard-stop controls

## Core Models
Primary contracts live in `IMigrationManager.cs`:
- planning: `MigrationPlanArtifact`, `MigrationPlanOperation`
- policy: `MigrationPolicyOptions`, `MigrationPolicyEvaluation`
- safety reports: `MigrationDryRunReport`, `MigrationPreflightReport`, `MigrationImpactReport`
- execution: `MigrationExecutionCheckpoint`, `MigrationExecutionResult`, `MigrationExecutionPolicy`
- rollback: `MigrationCompensationPlan`, `MigrationRollbackReadinessReport`, `MigrationRollbackResult`
- observability: `MigrationTelemetrySnapshot`, `MigrationDiagnosticEntry`, `MigrationAuditEvent`
- CI/devex: `MigrationCiValidationReport`, `MigrationDevExArtifactBundle`
- rollout: `MigrationRolloutGovernanceRequest`, `MigrationRolloutGovernanceReport`

## API Surface Summary
- entity operations:
  - `EnsureEntity(...)`, `CreateEntity(...)`, `AddColumn(...)`, `DropColumn(...)`, `AlterColumn(...)`
  - `CreateIndex(...)`, `AddForeignKey(...)`, `DropForeignKey(...)` (opt-in relational DDL)
- discovery and explicit migration:
  - `DiscoverEntityTypes(...)`, `EnsureDatabaseCreated(...)`, `ApplyMigrations(...)`
  - `EnsureDatabaseCreatedForTypes(...)`, `ApplyMigrationsForTypes(...)`
  - `ApplyMigrationsFromManifest(...)`, `EnsureDatabaseCreatedFromManifest(...)` (file-based discovery)
- ORM/model interop (EF Core, NHibernate, hand-rolled — ORM-agnostic POCO model):
  - `BuildMigrationPlanForModel(...)`, `BuildMigrationPlanForTypesAnnotated(...)`
  - `EnsureDatabaseCreatedForModel(..., applyForeignKeys, applyIndexes)`
  - `ApplyMigrationsForModel(..., applyForeignKeys, applyIndexes)` — topologically orders by FK dependencies
  - `GetMigrationReadinessForModel(...)`, `GetMigrationSummaryForModel(...)`
  - `GetMigrationModelEvidence()` (stable hash + per-entity reflection records)
- planning and policy:
  - `BuildMigrationPlan(..., applyForeignKeys, applyIndexes)`, `BuildMigrationPlanForTypes(..., applyForeignKeys, applyIndexes)`
  - `EvaluateMigrationPlanPolicy(...)`
  - `GenerateDryRunReport(...)`, `RunPreflightChecks(...)`, `BuildImpactReport(...)`
  - `BuildPerformancePlan(...)`
- entity operations:
  - `CreateIndex(...)`, `DropIndex(...)`, `AddForeignKey(...)`, `DropForeignKey(...)`
- execution:
  - `CreateExecutionCheckpoint(...)`, `ExecuteMigrationPlan(...)`, `ResumeMigrationPlan(...)`
  - `GetExecutionCheckpoint(...)`
- rollback and approval:
  - `BuildCompensationPlan(...)`, `CheckRollbackReadiness(...)`, `RollbackFailedExecution(...)`
  - `ApproveMigrationPlan(...)`
- observability:
  - `GetMigrationTelemetrySnapshot(...)`, `GetMigrationDiagnostics(...)`, `GetMigrationAuditEvents(...)`
- CI and governance:
  - `ValidatePlanForCi(...)`, `BuildMigrationPlanDiff(...)`, `ExportMigrationArtifacts(...)`
  - `EvaluateRolloutGovernance(...)`

## Recommended Execution Flow
1. Build plan (`BuildMigrationPlanForTypes` preferred for known schemas).
2. Evaluate policy (`EvaluateMigrationPlanPolicy`) and review warnings/blocks.
3. Generate dry-run/preflight/impact/performance reports.
4. Build compensation + rollback readiness evidence.
5. Run CI validation gates (`ValidatePlanForCi`) and export artifacts.
6. Evaluate rollout governance wave (`EvaluateRolloutGovernance`).
7. Approve plan (`ApproveMigrationPlan`) with audit notes.
8. Execute (`ExecuteMigrationPlan`) and monitor telemetry/diagnostics.
9. Resume from checkpoint when needed, or rollback with compensation on failure.

## CI/CD and Release Evidence
CI gate coverage includes:
- `plan-lint`
- `policy-check`
- `dry-run-validation`
- `portability-warning`

Artifact export (`ExportMigrationArtifacts`) includes:
- full plan JSON
- dry-run JSON
- CI validation JSON
- markdown approval report

## Rollout Wave Profiles (Example)
- Wave 1 non-critical:
  - success >= `0.90`
  - mean duration <= `180000 ms`
  - rollback rate <= `0.20`
  - policy-block ratio <= `0.40`
- Wave 2 standard production:
  - success >= `0.95`
  - mean duration <= `120000 ms`
  - rollback rate <= `0.10`
  - policy-block ratio <= `0.25`
- Wave 3 critical:
  - success >= `0.99`
  - mean duration <= `90000 ms`
  - rollback rate <= `0.02`
  - policy-block ratio <= `0.10`
  - hard-stop on critical diagnostics, rollback-in-critical-wave, or high failure rate

## Provider Guidance
- Use additive schema changes where possible.
- Treat destructive/type-narrowing/nullability-tightening operations as high-risk.
- Always rely on helper-driven capability profiles for portability decisions.
- Validate on real target provider before production rollout.

## Examples
Practical examples are under `Editor/Migration/Examples`:
- `00-overview.md`
- `01-plan-and-policy.md`
- `02-dryrun-preflight-impact.md`
- `03-execution-checkpoint-resume.md`
- `04-rollback-compensation.md`
- `05-ci-and-artifacts.md`
- `06-rollout-governance.md`
