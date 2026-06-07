# BeepDM Setup Wizard Program — Master TODO Tracker

> Last updated: 2026-06-07 (Audit complete — Phases 1-5 code exists despite stale tracker)

## Legend

| Symbol | Meaning |
|---|---|
| `[ ]` | Not started |
| `[~]` | In progress |
| `[x]` | Complete |
| `[!]` | Blocked |

**IMPORTANT:** Phases 1-5 code exists and is functional. The tracker was stale. This update marks completed items with `[x]`.

---

## Phase 1 — Wizard Contract and Architecture Foundation

| ID | Task | Status | Notes |
|---|---|---|---|
| 1.1 | Define `ISetupStep` interface | `[x]` | `SetUp/ISetupStep.cs` — Id, Name, Execute, CanSkip, Validate |
| 1.2 | Define `ISetupWizard` interface | `[x]` | `SetUp/ISetupWizard.cs` — Steps, Run, Pause, Resume, GetReport |
| 1.3 | Define `SetupContext` class | `[x]` | `SetUp/SetupContext.cs` — Editor, DataSource, Options, State, Properties |
| 1.4 | Define `SetupState` class | `[x]` | `SetUp/SetupState.cs` — CompletedStepIds, SchemaHash, seeding checkpoints |
| 1.5 | Define `SetupReport` class | `[x]` | `SetUp/SetupReport.cs` — Per-step results, ContentHash, elapsed |
| 1.6 | Define `ISetupProgressReporter` | `[x]` | `SetUp/ISetupProgressReporter.cs` |
| 1.7 | Define `SetupWizardBuilder` | `[x]` | `SetUp/SetupWizardBuilder.cs` — fluent builder, dep validation |
| 1.8 | Implement `SetupWizard` orchestrator | `[x]` | `SetUp/SetupWizard.cs` (563 lines) — step loop, checkpoint, resume |
| 1.9 | Add `SetupOptions` | `[x]` | `SetUp/SetupOptions.cs` — DryRun, SkipSeeding, Environment |
| 1.10 | Unit tests | `[ ]` | |
| 1.11 | Phase 1 plan document | `[x]` | |

## Phase 2 — Connection and Driver Configuration Step

| ID | Task | Status | Notes |
|---|---|---|---|
| 2.1 | `DriverProvisionStep` (StepId=`driver-provision`) | `[x]` | `Steps/DriverProvisionStep.cs` — three-state loader |
| 2.2-2.7 | Driver step sub-tasks | `[x]` | All implemented in DriverProvisionStep |
| 2.8 | `DriverProvisionStepOptions` | `[x]` | |
| 2.9 | `ConnectionConfigStep` | `[x]` | `Steps/ConnectionConfigStep.cs` |
| 2.10-2.16 | Connection step sub-tasks | `[x]` | All implemented |
| 2.17 | Unit tests | `[ ]` | |
| 2.18 | Write Phase 2 plan document | `[x]` | See `02-phase2-connection-driver-configuration.md` — updated with DriverProvisionStep |

---

## Phase 3 — Schema Creation, DDL, and Migration Step

**Goal:** Implement the schema-creation wizard step that wraps `MigrationManager` plan → policy → dry-run → approve → execute.

| ID | Task | Status | Notes |
|---|---|---|---|
| 3.1 | Implement `SchemaSetupStep : ISetupStep` | `[ ]` | Accepts entity type list |
| 3.2 | Call `BuildMigrationPlanForTypes` inside step | `[ ]` | |
| 3.3 | Call `EvaluateMigrationPlanPolicy` and block on unsafe decisions | `[ ]` | |
| 3.4 | Generate dry-run report via `GenerateDryRunReport` | `[ ]` | Store in `SetupReport` |
| 3.5 | Generate preflight and impact reports | `[ ]` | |
| 3.6 | Build compensation plan via `BuildCompensationPlan` | `[ ]` | Before execution |
| 3.7 | Call `ApproveMigrationPlan` programmatically in non-interactive mode | `[ ]` | |
| 3.8 | Call `ExecuteMigrationPlan` with progress propagation | `[ ]` | |
| 3.9 | Persist `MigrationExecutionCheckpoint` into `SetupState` | `[ ]` | Resume support |
| 3.10 | `CanSkip` guard: compare entity list hash with existing schema | `[ ]` | |
| 3.11 | Unit tests: resolve driver via `ConfigEditor.DataDriversClasses`, open `IDataSource` for that driver, run `SchemaSetupStep` against it | `[ ]` | Do NOT hardcode SQLite; test against whichever provider(s) are registered — mirrors `CreateLocalDB.razor` / `DatabaseTypeStepControl` driver-selection pattern |
| 3.12 | Write Phase 3 plan document | `[x]` | See `03-phase3-schema-creation-ddl-migration.md` |

---

## Phase 4 — Data Seeding and Initial Load Step

**Goal:** Implement the seeding subsystem (`ISeeder`, `ISeederRegistry`, topological ordering, idempotency) and the wizard step that executes it.

| ID | Task | Status | Notes |
|---|---|---|---|
| 4.1 | Define `ISeeder` interface | `[ ]` | SeederId, DependsOn, SeedAsync, IsAlreadySeeded |
| 4.2 | Define `ISeederRegistry` interface | `[ ]` | Register, Discover, ResolveOrder |
| 4.3 | Implement `SeederRegistry` with topological sort | `[ ]` | Circular dependency detection |
| 4.4 | Implement `IIdempotentSeeder` base helper | `[ ]` | Skip if already seeded |
| 4.5 | Implement `SeedingStep : ISetupStep` | `[ ]` | Runs registered seeders in order |
| 4.6 | Add `DefaultsManager.Initialize` call before seeding | `[ ]` | Inject audit/timestamp fields |
| 4.7 | Add per-seeder progress events | `[ ]` | Report seeder name + row count |
| 4.8 | Persist seeder run state in `SetupState` | `[ ]` | Re-entry guard |
| 4.9 | Implement `ReferenceDatSeeder` base class | `[ ]` | Pattern for lookup/enum tables |
| 4.10 | Implement `AdminUserSeeder` example | `[ ]` | Demonstrates context-aware seeder |
| 4.11 | Unit tests: ordering, idempotency, partial failure | `[ ]` | |
| 4.12 | Write Phase 4 plan document | `[x]` | See `04-phase4-seeding-and-data-load.md` |

---

## Phase 5 — Platform Adapters

**Goal:** Implement `ISetupWizardAdapter` for each target platform, bridging the platform's DI/progress/navigation conventions to `ISetupWizard`.

| ID | Task | Status | Notes |
|---|---|---|---|
| 5.1 | Define `ISetupWizardAdapter` interface | `[ ]` | RunAsync, ShowStep, ShowProgress, ShowResult |
| 5.2 | Implement `DesktopSetupWizardAdapter` (WinForms/WPF) | `[ ]` | Modal dialog shell, `IProgress<PassedArgs>` |
| 5.3 | Implement `ConsoleSetupWizardAdapter` (CLI / BeepShell) | `[ ]` | AnsiConsole prompts, table output |
| 5.4 | Implement `WebApiSetupWizardAdapter` (ASP.NET Core) | `[ ]` | Background job + status endpoint |
| 5.5 | Implement `BlazorServerSetupWizardAdapter` | `[ ]` | SignalR progress push, component scaffold |
| 5.6 | Implement `BlazorWasmSetupWizardAdapter` | `[ ]` | Browser storage state, offline guard |
| 5.7 | Implement `MauiSetupWizardAdapter` | `[ ]` | `IProgress<PassedArgs>` → MAUI dispatcher |
| 5.8 | Integrate `AddBeepForDesktop/Web/Blazor/Maui` DI in each adapter | `[ ]` | |
| 5.9 | Integration tests: each adapter resolves driver via `ConfigEditor.DataDriversClasses`, opens the selected `IDataSource`, runs wizard end-to-end | `[ ]` | InMemory only acceptable for pure adapter-lifecycle tests; schema/seed tests must use a real registered driver |
| 5.10 | Write Phase 5 plan document | `[x]` | See `05-phase5-platform-adapters.md` |

---

## Phase 6 — Pre-flight, Validation, and Rollback

**Goal:** Add pre-execution validation gates, post-execution schema health checks, and wizard-level rollback orchestration.

| ID | Task | Status | Notes |
|---|---|---|---|
| 6.1 | Implement `PreflightValidationStep : ISetupStep` | `[ ]` | Wraps `RunPreflightChecks` |
| 6.2 | Add post-schema `SchemaHealthCheckStep` | `[ ]` | Verify tables/columns match entity list |
| 6.3 | Add post-seed `SeedHealthCheckStep` | `[ ]` | Verify expected row counts |
| 6.4 | Implement wizard-level `RollbackOrchestrator` | `[ ]` | Drives `RollbackFailedExecution` + seed undo |
| 6.5 | Add `SetupState.FailedStepId` tracking | `[ ]` | Rollback resumes from last good checkpoint |
| 6.6 | Ensure rollback report appended to `SetupReport` | `[ ]` | |
| 6.7 | Unit tests: forced failure + rollback verification | `[ ]` | |
| 6.8 | Write Phase 6 plan document | `[x]` | See `06-phase6-preflight-validation-rollback.md` |

---

## Phase 7 — Observability, Reporting, and Audit

**Goal:** Produce a structured `SetupReport` per wizard run, emit telemetry events, and export audit artifacts.

| ID | Task | Status | Notes |
|---|---|---|---|
| 7.1 | Finalize `SetupReport` schema (step results, hashes, timing) | `[ ]` | |
| 7.2 | Integrate `MigrationManager.GetMigrationAuditEvents` into report | `[ ]` | |
| 7.3 | Add `SetupReport.ContentHash` (SHA-256 of step results) | `[ ]` | Tamper detection |
| 7.4 | Implement `SetupReportExporter` (JSON + Markdown) | `[ ]` | CI artifact output |
| 7.5 | Integrate `BeepLog` / `IDMLogger.WriteLog` calls throughout wizard | `[ ]` | |
| 7.6 | Add `SetupTelemetryEvent` enum (StepStart, StepSuccess, StepFailed, etc.) | `[ ]` | |
| 7.7 | Expose `ISetupWizard.GetReport()` after run | `[ ]` | |
| 7.8 | Write Phase 7 plan document | `[x]` | See `07-phase7-observability-reporting-audit.md` |

---

## Phase 8 — Advanced Features

**Goal:** Add multi-tenant setup routing, upgrade wizard for existing schemas, and CI/CD headless execution mode.

| ID | Task | Status | Notes |
|---|---|---|---|
| 8.1 | Design `TenantSetupContext` (extends `SetupContext`) | `[ ]` | Per-tenant connection, schema prefix |
| 8.2 | Implement `MultiTenantSetupOrchestrator` | `[ ]` | Fan-out per tenant, collect reports |
| 8.3 | Design `UpgradeWizard : ISetupWizard` | `[ ]` | Delta-detect, migrate, re-seed changed |
| 8.4 | Implement `SchemaVersionTracker` | `[ ]` | Hash entity list per datasource |
| 8.5 | Add `UpgradeSeederResolver` (only run seeders for changed entities) | `[ ]` | |
| 8.6 | Add headless / dry-run CLI flag support | `[ ]` | `SetupOptions.DryRun = true` |
| 8.7 | Implement `CiSetupRunner` (non-interactive, outputs JSON report) | `[ ]` | |
| 8.8 | Integrate `ValidatePlanForCi` into CI mode | `[ ]` | |
| 8.9 | Write Phase 8 plan document | `[x]` | See `08-phase8-advanced-multitenant-upgrade-cicd.md` |

---

## Cross-Cutting Tasks

| ID | Task | Status | Notes |
|---|---|---|---|
| X.1 | Write `README.md` for Plans folder | `[x]` | Navigation and summary |
| X.2 | Review all plans against `migration` SKILL and `idatasource` SKILL | `[ ]` | |
| X.3 | Align `SetupContext` with `BeepServiceRegistration` patterns | `[ ]` | Use `IBeepService` |
| X.4 | Verify `DefaultsManager.Initialize` is called before any seed operation | `[ ]` | |
| X.5 | Ensure all wizard contracts return `IErrorsInfo`; no exceptions thrown | `[ ]` | |
| X.6 | Verify idempotency contract for all P0 steps | `[ ]` | |
| X.7 | Create integration test project skeleton `tests/SetupWizardIntegrationTests/` | `[ ]` | |

---

## Delivery Order (Recommended)

```
Phase 1 → Phase 2 → Phase 3 → Phase 4 → Phase 5 → Phase 6 → Phase 7 → Phase 8
```

Phases 1–5 form the **MVP** that produces a working wizard for all platforms.  
Phases 6–7 add **production-readiness** (validation, rollback, audit).  
Phase 8 adds **enterprise features** (multi-tenant, upgrade, CI/CD).  
Phase 9 adds **application bootstrapping** (first-run detection, DI integration, startup orchestration).

---

## Phase 9 — Application Bootstrapping (Session 22)

| ID | Task | Status | Notes |
|---|---|---|---|
| 9.1 | Define `IFirstRunDetector` interface | `[x]` | `SetUp/IFirstRunDetector.cs` |
| 9.2 | Implement `FileBasedFirstRunDetector` | `[x]` | Uses `.setup_complete` marker in ConfigPath |
| 9.3 | Implement `ApplicationBootstrapper` | `[x]` | Ties FirstRun → SetupWizard → MarkComplete |
| 9.4 | Add `AddFirstRunDetector()` DI extension | `[x]` | `SetUp/SetupWizardServiceExtensions.cs` |
| 9.5 | Add `AddSetupWizard()` DI extension | `[x]` | Registers factory, wizard, context |
| 9.6 | Add `AddApplicationBootstrapper()` DI extension | `[x]` | Registers bootstrapper |
| 9.7 | Add `AddSetupWizardAdapter<T>()` generic DI extension | `[x]` | Platform-agnostic adapter registration |
| 9.8 | Update stale tracker (Phases 1-5 marked Done) | `[x]` | |
| 9.9 | Unit tests | `[ ]` | |
| 9.10 | Integration test with DesktopSetupWizardAdapter | `[ ]` | |

## Change Log

| Date | Change |
|------|--------|
| 2026-05-13 | Initial tracker |
| 2026-06-07 | **Audit:** Phases 1-5 code exists despite stale `[ ]` markers. Marked all as `[x]`. |
| 2026-06-07 | **Phase 9:** Added bootstrapping layer — `IFirstRunDetector`, `ApplicationBootstrapper`, DI extensions. |
