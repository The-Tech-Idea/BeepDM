# MASTER MIGRATION TRACKER
# MigrationManager — Verify + Hygiene

**Goal:** Prove the ~10.8k-LOC, 0%-tested `MigrationManager` subsystem actually works, fix the bugs
testing surfaces, and clean up — **without** adding new engine semantics. Structural gaps are *pinned
as documented characterization tests*, not closed. (Full plan:
`C:\Users\f_ald\.claude\plans\now-revise-migrationmanager-and-ticklish-wall.md`.)

**Legend:** `[ ]` open · `[~]` in progress · `[x]` done · `[!]` blocked

## The through-line

Migration has **zero tests** across ~60 public methods, yet the docs claim a complete enterprise
engine. Same pattern as Setup — testing is what surfaces the real bugs. A recording fake
`IDataSource` + `IDataSourceHelper` (Moq) exercises planning → policy → dry-run → preflight →
execution → rollback **without a live DB**. Build the harness first; everything else hangs off it.

## Verified structural gaps (pinned, not closed in this plan)

1. Governed pipeline is **additive-only** — `BuildPlanOperation` never emits Alter/Drop/Rename/Truncate;
   `ExecuteStep` `default:`-errors them; the `Policy.cs:256-258` destructive gate is **unreachable**.
2. **Resume doesn't survive a restart** — checkpoints are process-static; JSON is written but
   `ResumeMigrationPlan` never reads it back.
3. **No idempotency** — `IsMigrationApplied`/`RecordMigration` don't exist; history is write-only.
4. No transaction wrapping (`SupportsTransactionalDdl` is advisory).
5. `AddMissingColumns` has no auto-rollback; `RollbackFailedExecution` undoes only ReversibleDdl.
6. Impact/performance estimates are hardcoded heuristics.
7. Two unlinked apply paths (imperative vs governed).

## Phases

| # | Phase | Document | Status |
|---|---|---|---|
| 1 | Test harness + characterization (keystone) | [PHASE-01](PHASE-01-Test-Harness.md) | [x] |
| 2 | Planning, policy & CI-gate verification | [PHASE-02](PHASE-02-Planning-Policy.md) | [x] |
| 3 | Dry-run / preflight / impact verification | [PHASE-03](PHASE-03-DryRun-Preflight-Impact.md) | [x] |
| 4 | Execution, checkpoint & resume verification | [PHASE-04](PHASE-04-Execution-Checkpoint-Resume.md) | [x] |
| 5 | Rollback & compensation verification | [PHASE-05](PHASE-05-Rollback-Compensation.md) | [x] |
| 6 | Hygiene: catches, dedup, doc drift, honest README | [PHASE-06](PHASE-06-Hygiene-Docs.md) | [x] |

## Not in this plan (future, opt-in)

Idempotency; close the additive-only pipeline (4a plan-only → 4b execute); transaction wrapping;
durable checkpoints; architectural reconciliation (SchemaManager vs MigrationManager preflight/drift;
imperative-vs-governed apply paths; CreateEntity provider bypass).

## Verification

Every phase ends green: `dotnet build BeepDM.sln` (0 errors) + `dotnet test BeepDM.sln`.
Baseline at plan start: **206/206** (SetupWizardTests 152, FormsManager.Tests 54). Migration adds a
new `tests/MigrationManagerTests` project.

Contract note: `IMigrationManager` + DTOs ship as NuGet `TheTechIdea.Beep.DataManagementEngine` 3.1.1
— additive members + `[Obsolete]` over signature edits.

## Progress log

**Phase 1 (done):** `tests/MigrationManagerTests` project created + added to `BeepDM.sln`.
`MigrationTestHarness` (Moq recording fake `IDataSource` + `IDMEEditor` + `IClassCreator` + a
recording `ISchemaMigrationProvider`) drives a real `MigrationManager` with **no live DB** — the
first time this subsystem has been executable under test. `HarnessSmokeTests` (2) + `CharacterizationTests`
(3) + `ExecutionCharacterizationTests` (4): pin gap #1 (planner additive-only, both planning + execution
sides), that the imperative API DOES route destructive ops through the provider, that the governed
path executes an additive plan, and the no-idempotency re-plan (gap #2/#3).

**Phase 2 (done):** `PlanningPolicyTests` (6): deterministic plan hash, missing-column detection,
additive plan not policy-blocking, CI `CanMerge`. **Key precise finding:** the policy engine is
*correct* — fed a hand-built `DropColumn` plan it blocks in a Production tier — so gap #1 is isolated
to the **planner**, which never emits column/table-destructive ops (it can emit Drop FK/Index).

**Phase 3 (done):** `DryRunPreflightImpactTests` (4): dry-run report for an additive plan; the
built-plan `PreflightReport.CanApply=false`-by-design → `RunPreflightChecks` clears it to true;
impact produces entries with static sensitivity (pins gap #6 — no real row-count probing).

**Phase 4 (done):** `ExecutionCheckpointResumeTests` (6): execute produces a checkpoint with steps;
`GetExecutionCheckpoint` works in-process; plan-hash-mismatch on a reused token is rejected (self-
validated the premise); **gap #2 pinned** — reflectively clearing the static checkpoint store (== a
restart) makes `ResumeMigrationPlan` return "No checkpoint found". **New finding:** the plan hash is
insensitive to a `CreateEntity`'s column set (two schemas hash identically).

**Phase 5 (done):** `RollbackCompensationTests` (7): **cross-subsystem finding** — `BuildCompensationPlan`
emits actions only for high-risk/relational ops, so a plain `CreateEntity`/`AddMissingColumns` plan
gets an **empty** compensation plan and `RollbackFailedExecution` undoes nothing — the exact path
`SchemaSetupStep.RollbackAsync` (Setup framework) hits. The compensation engine itself is *correct*
(proven with hand-built high-risk plans: CreateEntity→ReversibleDdl, AddColumn→ForwardFix). Readiness
+ dry-run verified.

**Phase 6 (in progress):** Honest `Editor/Migration/README.md` header written (documents real
capabilities + all pinned gaps + known hygiene items). Doc drift fixed (`SKILL.md` nonexistent
`Checkpoints.cs` reference). Dead `ConfigEditor.MigrationProviders` registry annotated in-code.
All **13 previously-silent `catch{}`** (6 in `Discovery.cs`, 5 in `ManifestParser.cs`, 2 in
`MigrationTrackingService.cs`) now **report through `IDMEEditor.AddLogMessage(..., Errors.Warning)`**
with assembly/type context + the exception message (per user direction — don't swallow, route to the
editor's error management). They still fall through to the next candidate (best-effort resolution/
retry), but a genuine load fault (bad-image/version-conflict assembly, `TypeLoadException`, transient
connection failure) is now visible. Manifest probes use `GetType(throwOnError:false)`, so these logs
fire only on real faults, not on every "type absent here" miss. `OpenWithRetrySync` was made an
instance method (was `static`) so it can reach `_editor`; single caller, instance context. **236/236 green.**
**POCO dedup — DONE (user-approved go).** Deleted the `DataManagementEngine` copy of
`MigrationHistory.cs`; the canonical `MigrationHistory`/`MigrationRecord`/`MigrationStep` now live only
in `DataManagementModels`, with `[assembly: TypeForwardedTo]` in new `DataManagementEngineStandard/TypeForwards.cs`
preserving binary compat. This removed the latent **CS0433** for consumers referencing both packages
and cleared ~62 in-tree **CS0436** warnings (3829 → 3767). Verified: 0 errors, **236/236 green**, no
CS0433/CS0436 remain for these types.

**Phase 6 CLOSED.** Verify+hygiene plan complete (Phases 1–6 all `[x]`). Remaining architectural
hygiene (dead `ConfigEditor` registry — annotated; `CreateEntity`/`EnsureEntity` provider bypass;
`SchemaManager` vs `MigrationManager` dual stacks) is catalogued below as opt-in future work, not part
of this scope.

**236/236 green** (migration 30, setup 152, forms 54); solution builds 0 errors.
