---
name: beepdm-migration
description: Use when designing, running, or governing schema migrations with BeepDM's MigrationManager — planning, dry-run/preflight, execution checkpoints, rollback/compensation, CI gates, and rollout waves across datasource types. Hands off to the Setup Framework (first-run) and Configuration (history) skills.
---

# beepdm-migration

`MigrationManager` is the central authority for evolving a datasource's schema in BeepDM. It is **datasource-agnostic** — it produces plan artifacts, then dispatches to the per-dialect `IDataSourceHelper` for actual DDL.

## When to use this skill

- Creating databases from `Entity` POCO types or `EntityStructure` metadata.
- Adding missing tables/columns/FKs/indexes on an existing datasource.
- Building a migration plan **before** applying it (`BuildMigrationPlan*`).
- Running safety gates: policy, dry-run, preflight, impact, performance.
- Executing migrations with retries, checkpoints, and resume.
- Preparing rollback/compensation plans and readiness evidence.
- Running CI validation gates and producing approval artifacts.
- Evaluating rollout wave promotion against KPI thresholds.

## Do NOT use this skill for

- First-run database creation with seeding → use **beepdm-setup**.
- CRUD / transactional app logic → use **beepdm-unitofwork**.
- Connection-string definition → use **beepdm-configuration**.
- Bulk cross-system data movement → use **beepdm-etl**.

## File Locations

`DataManagementEngineStandard/Editor/Migration/` is split by responsibility (one partial per concern). The relevant files:

- `MigrationManager.cs` — façade
- `MigrationManager.Discovery.cs` — entity/POCO/EF discovery via `IClassCreator`
- `MigrationManager.AssemblyRegistration.cs` — explicit assembly registration
- `MigrationManager.Planning.cs` — `BuildMigrationPlan*` (immutable plan + hash)
- `MigrationManager.Policy.cs` — protected-environment policy & safety decisions
- `MigrationManager.DryRunAndPreflight.cs` — DDL previews + impact reports
- `MigrationManager.PerformanceScale.cs` — perf annotations and sizing
- `MigrationManager.ExecutionOrchestration.cs` — retries, deterministic step state
- `MigrationManager.Checkpoints.cs` — checkpoint persistence + resume
- `MigrationManager.RollbackCompensation.cs` — compensation plans + rollback simulation
- `MigrationManager.RolloutGovernance.cs` — wave promotion + KPI/hard-stop rules
- `MigrationManager.Observability.cs` — telemetry, audit, lifecycle events
- `MigrationManager.ModelInterop.cs` — EF Core / ORM interop cache
- `MigrationManager.DevExAutomation.cs` — approval reports, CI artifacts
- `MigrationManager.ManifestParser.cs` — manifest → plan

## Core Capabilities

- Discover entity types across assemblies (incl. EF-decorated POCOs and plain POCOs).
- Build immutable migration plan artifacts + hashes.
- Evaluate policy + protected-environment safety decisions.
- Generate dry-run DDL previews, preflight checks, impact reports.
- Execute with retries, deterministic step state, checkpoint persistence, resume.
- Build compensation plans, rollback-readiness checks, rollback simulations.
- Capture telemetry, diagnostics, auditable lifecycle events.
- Run CI validation gates; export approval-ready artifacts.
- Evaluate rollout wave promotion with KPI thresholds + hard-stop rules.
- Surface FK/Index target names and counts across the whole artifact chain.

## Discovery via ClassCreator

`IsEntityType` in `MigrationManager.Discovery.cs` delegates to `IClassCreator.IsEfDecoratedType` and `IClassCreator.IsDiscoverablePoco`, so the discovery pipeline picks up:

- EF Core POCOs decorated with `[Table]`, `[Column]`, `[Key]`, `[ForeignKey]`, etc.
- Plain POCOs that are concrete, public, non-generic, and have a parameterless ctor (or are records)

without users having to subclass `Entity`. To add a new discoverable type, implement a helper method and add a delegation in `ClassCreator.PocoToEntity.cs`.

## Typical Workflow

1. **Plan**: `BuildMigrationPlan(entities, target)` → immutable plan + hash.
2. **Gate**: `EvaluatePolicy(plan)` → `Approve | Reject | NeedsReview` for protected envs.
3. **Preview**: `GenerateDryRun(plan)` → DDL preview; `Preflight(plan)` → capability / data-impact checks.
4. **Execute**: `Execute(plan)` with retries + checkpoint persistence; resumable via `Resume(checkpointId)`.
5. **Compensate**: `BuildCompensationPlan(plan)` for rollback; `SimulateRollback(...)` before destructive ops.
6. **Govern**: `EvaluateRolloutWave(wave, kpis)` → `Promote | Hold | Rollback` based on thresholds.

## Execution surface (sync + async)

`MigrationManager` exposes both signatures for the execute step:

- `MigrationExecutionResult ExecuteMigrationPlan(plan, policy?, executionToken?, progress?)` — sync. Thin wrapper that calls `ExecuteMigrationPlanAsync(...).GetAwaiter().GetResult()`. **Safe for sync callers** that aren't running on a captured sync context (WinForms message loops, WPF dispatcher). The original sync surface; preserved for back-compat.
- `Task<MigrationExecutionResult> ExecuteMigrationPlanAsync(plan, policy?, executionToken?, progress?, CancellationToken token = default)` — canonical. New code should use this. Per-step retry is delegated to `IRetryPipeline`; the outer foreach over steps owns step-to-step control flow and the abort-vs-continue decision (see `policy.AbortOnStepFailure` below).

The `executionToken` is the lookup key for `ExecutionPlans[token]`; a fresh one is generated if you pass `null`. Resume semantics: pass the same token to get the same in-memory `MigrationExecutionCheckpoint` back.

## Abort vs continue on step failure

`MigrationExecutionPolicy.AbortOnStepFailure` (default `true`) controls the outer-loop behavior when a step's retry budget is exhausted:

- **`true` (default — preserves pre-refactor behavior):** the plan aborts on the first non-recoverable step failure. The `MigrationExecutionResult` has `Success = false` and a populated `Message` describing the failed step + rollback/compensation outcomes.
- **`false` (opt-in):** the plan continues to the next step. The failed step is recorded in `result.FailedSteps` (a `List<int>` of failed step sequences) and in the per-step checkpoint. The final result reflects it (`Success = false` if any step failed, but `AppliedCount` may be > 0 for the steps that succeeded).

Use `false` only when the plan is intentionally partitioned and a partial application is meaningful (e.g. each step operates on a different table and downstream consumers can tolerate one missing table). Dependency blocks and preflight failures always abort regardless of this flag.

## Per-step retry (canonical)

Per-step retry is delegated to the shared `IRetryPipeline` (see `beepdm-retry`). The pipeline runs the per-step retry loop; the outer foreach consults `AbortOnStepFailure` on giveup. Plan-level `CancellationToken` is checked at the start of each step iteration and is also forwarded to the per-step pipeline, so cancelling the token mid-retry will cancel the in-flight `Backoff` sleep.

## `MigrationRecord.MigrationId` conventions

The `MigrationRecord` history file uses three `MigrationId` shapes, each with a clear reason — see the class doc on `MigrationRecordWriter` (`Editor/Migration/MigrationRecordWriter.cs`) for the authoritative list. Quick reference:

- **Deterministic anchor (lookup key):** `WriteExecutionSnapshot` writes `MigrationId = checkpoint.ExecutionToken`; `WritePlanArtifact` writes `MigrationId = plan.PlanId`. These are the keys for "resume this execution" and "rerun this plan overwrites the previous snapshot."
- **Plan-prefixed summary:** `WritePlanExecution` and `WriteOperation(planId: ...)` write `MigrationId = "{plan.PlanId}-{rand8}"`. The prefix makes the record discoverable by its source plan in the history file.
- **Unprefixed summary:** `WriteOperation()` without a `planId` (the 38 in-tree call sites of `TrackMigration` that don't have a plan available) writes a full random Guid. This is the noise floor.

If you're writing a consumer that reads the `MigrationId` field, **don't** assume a fixed length or shape — the format is part of the writer's contract, not the record type's contract.

## Design Rules (from source)

- Plan artifacts are **immutable** — any change → new plan + new hash. Don't mutate plan ops after creation.
- FK/Index targets must be surfaced in plan op, step, dry-run, impact, perf annotation, summary, approval report. If you add a new artifact, propagate the same fields.
- Use `IDataSourceHelper` for dialect-specific DDL — never hardcode SQL.
- Always return `IErrorsInfo`; populate `Flag`/`Message`/`Ex` for expected failures. Don't throw for normal control flow.
- Honor ORM/POCO-shaped `EntityStructure` via the model-interop cache.

## How this skill works with the rest of the data-management layer

| Handoff | Direction | What flows |
|---|---|---|
| **beepdm-setup** | ← Setup | On first run, `SetupWizard` uses `MigrationManager` (typically via `SchemaSetupStep`) to create the initial schema. Migration history is then recorded. |
| **beepdm-configuration** | ↔ Config | `ConfigEditor.MigrationHistoryManager` is the persisted record of which migrations have been applied per datasource. `MigrationManager` reads/writes through it. `IsMigrationApplied(ds, name)` gates whether a migration runs again. |
| **beepdm-schema** | ↔ Schema migration | **`MigrationManager` does DDL on a single datasource; `ISchemaManager` does preflight + draft for cross-datasource alignment.** They are distinct services with different jobs — see `beepdm-schema` for the preflight / draft counterpart. |
| **beepdm-etl** | ← ETL | ETL pipelines run **after** schema is in place; if a target entity is missing, ETL should fail fast and call back into Migration, not invent columns. ETL also uses `ISchemaManager` for preflight (see `beepdm-schema`). |
| **beepdm-unitofwork** | ← UoW | UoW operates against an existing schema. UoW does **not** perform migrations — it asserts the entity exists. |
| **beepdm-forms** | ← Forms | Forms bind to entities produced by the migrated schema. Schema drift detected by Forms should be reported, not auto-migrated. |

## Cross-references

- See **beepdm-setup** for the wizard that calls into migration on first run.
- See **beepdm-configuration** for `MigrationHistoryManager` and the persisted history file.
- See **beepdm-etl** for what happens after the schema exists.
- See **beepdm-schema** for the cross-datasource preflight + sync-draft service (separate concern).
- See **beepdm-retry** for the shared `IRetryPipeline` primitive that `ExecuteMigrationPlanAsync` uses for per-step retry. The inner `while (!completed)` loop was migrated to a `RetryPlan<IErrorsInfo>`; the outer foreach still owns step-to-step control flow and the abort-vs-continue decision (driven by `MigrationExecutionPolicy.AbortOnStepFailure`).
- See `.cursor/migration/SKILL.md` for the deep-dive implementation details.
