# Migration Manager

> **Read this first — verified status (code + tests, not aspiration).** `MigrationManager` is real,
> sophisticated code (~10.8k LOC, 18 partials) — but until recently it had **zero tests**. A test
> harness now exists (`tests/MigrationManagerTests`, a Moq recording fake that drives the real manager
> with no live DB). Verification confirmed the additive path is correct and consistent, and pinned
> several **structural gaps where a documented capability is unreachable via the normal planning path**.
> The rest of this document describes the intended design; the gaps below are what actually holds today.
>
> **Verified structural gaps (pinned by `tests/MigrationManagerTests`):**
> 1. **The governed pipeline is additive-only.** `BuildMigrationPlan*` only ever emits
>    `CreateEntity`/`AddMissingColumns`/FK/Index/`UpToDate`/`Error`. It **never** emits
>    `AlterColumn`/`DropColumn`/`DropEntity`/`RenameEntity`/`RenameColumn`/`TruncateEntity` (it detects
>    *missing* columns to add, never diffs a *changed* or *removed* one). So the destructive-change
>    **policy gate, dry-run, compensation, and rollback are all guarding operations the planner can
>    never produce.** Destructive/alter ops work only via the ungoverned imperative API
>    (`DropEntity`, `AlterColumn`, …), which *does* route through the provider.
> 2. **Compensation/rollback is inert for normal plans.** `BuildCompensationPlan` only emits actions
>    for high-risk **or relational (FK/index)** ops; a plain `CreateEntity`/`AddMissingColumns` plan
>    gets an **empty** compensation plan, so `RollbackFailedExecution` has nothing to undo — even
>    though `CreateEntity` is *declared* reversible. This is what `SchemaSetupStep.RollbackAsync`
>    (Setup framework) hits. The compensation engine itself is correct when fed a qualifying op.
> 3. **Resume does not survive a process restart.** Checkpoints live in a `static` dictionary;
>    `ResumeMigrationPlan` reads only that dictionary and never loads the persisted JSON.
> 4. **No idempotency.** `IsMigrationApplied`/`RecordMigration` don't exist; history is write-only —
>    appended, never queried to skip an already-applied migration.
> 5. **`AddMissingColumns` has no auto-rollback** (forward-fix/manual only).
> 6. **Impact/performance analysis is hardcoded heuristics** — no row-count/size probing.
> 7. **The plan hash doesn't fingerprint a `CreateEntity`'s column set** — two different schemas for
>    the same new entity hash identically.
>
> **Known hygiene items (tracked, not yet resolved):** `MigrationHistory` POCO is defined twice
> (`DataManagementModelsStandard/ConfigUtil/MigrationHistory.cs` + `DataManagementEngineStandard/.../MigrationHistory.cs`,
> same namespace); a second `MigrationProviderRegistry` on `ConfigEditor` is dead (no fallbacks, never
> read); `CreateEntity`/`EnsureEntity` bypass the provider that alter/drop use; `SchemaManager` claims
> to "own ALL schema concerns" but `MigrationManager` never calls it (two independent, non-shared
> preflight/drift stacks). See `.plans/migration/`.
>
> Closing gaps 1–6 is catalogued as opt-in future work — the current effort is verify + hygiene.

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
