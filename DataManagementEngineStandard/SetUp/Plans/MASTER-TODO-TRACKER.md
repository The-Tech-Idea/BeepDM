# BeepDM Setup Wizard Program — Master TODO Tracker

> Last updated: 2026-05-13

---

## Legend

| Symbol | Meaning |
|---|---|
| `[ ]` | Not started |
| `[~]` | In progress |
| `[x]` | Complete |
| `[!]` | Blocked |

---

## Phase 1 — Wizard Contract and Architecture Foundation

**Goal:** Define the core contracts (`ISetupWizard`, `ISetupStep`, `SetupContext`, `SetupState`, `SetupReport`) that all other phases build on.

| ID | Task | Status | Notes |
|---|---|---|---|
| 1.1 | Define `ISetupStep` interface | `[ ]` | Id, Name, Execute, CanSkip, Validate |
| 1.2 | Define `ISetupWizard` interface | `[ ]` | Steps, Run, Pause, Resume, GetReport |
| 1.3 | Define `SetupContext` class | `[ ]` | Carries editor, dataSource, options, progress |
| 1.4 | Define `SetupState` class (serializable checkpoint) | `[ ]` | Completed step IDs, plan hashes, timestamps |
| 1.5 | Define `SetupReport` class (immutable outcome) | `[ ]` | Per-step results, content hash, elapsed |
| 1.6 | Define `ISetupProgressReporter` interface | `[ ]` | Unified progress surface |
| 1.7 | Define `SetupWizardBuilder` fluent builder | `[ ]` | Compose steps, set options, build wizard |
| 1.8 | Implement `SetupWizardBase` abstract class | `[ ]` | Step loop, checkpoint save, error propagation |
| 1.9 | Add `SetupOptions` (dry-run, skip-seed, environment flags) | `[ ]` | |
| 1.10 | Unit tests: step ordering, checkpoint save/resume | `[ ]` | Use InMemory datasource |
| 1.11 | Write Phase 1 plan document | `[x]` | See `01-phase1-contract-and-wizard-architecture.md` |

---

## Phase 2 — Connection and Driver Configuration Step

**Goal:** Implement two cooperating steps: `DriverProvisionStep` (three-state driver loader — checks loaded/cached/missing and installs via NuGet if needed) followed by `ConnectionConfigStep` (validates, resolves, and persists `ConnectionProperties` via `ConfigEditor` and `ConnectionHelper`). Based on `ConnectionDrivers.razor`, `NuggetManager.razor`, and `LocalNuggetManager.razor` three-state patterns.

| ID | Task | Status | Notes |
|---|---|---|---|
| 2.1 | Implement `DriverProvisionStep : ISetupStep` (StepId = `driver-provision`) | `[ ]` | Prerequisite for ConnectionConfigStep — must run first |
| 2.2 | Implement State 1 (Loaded): `CanSkip` returns true when `!IsMissing` | `[ ]` | No assemblyHandler calls needed |
| 2.3 | Implement State 2 (Cached): `assemblyHandler.LoadDriverFromLocalPackage` | `[ ]` | Follow `ConnectionDrivers.razor` LoadDriverFromCacheAsync pattern |
| 2.4 | Implement State 2 fallthrough: if disk load fails and `NuggetMissing`, fall to State 3 | `[ ]` | |
| 2.5 | Implement State 3 (Missing): `assemblyHandler.LoadNuggetFromNuGetAsync` | `[ ]` | Follow `NuggetManager.razor` RunInstallAsync pattern |
| 2.6 | After any State 2/3 load: call `ConfigEditor.SaveConnectionDriversConfigValues` | `[ ]` | Persist updated IsMissing / NuggetMissing flags |
| 2.7 | After State 3: verify `DataDriversClasses` contains entry with `!IsMissing`; return `Errors.Failed` if `[AddinAttribute]` type not found | `[ ]` | |
| 2.8 | Define `DriverProvisionStepOptions` (DataSourceType, PackageId, Version, NuGetSources, InstallPath, UseSingleSharedContext, UseProcessHost) | `[ ]` | |
| 2.9 | Implement `ConnectionConfigStep : ISetupStep` | `[ ]` | Accepts `ConnectionProperties` draft; depends on DriverProvisionStep having run |
| 2.10 | Integrate `ConnectionDriverLinkingHelper.GetBestMatchingDriver` | `[ ]` | Now guaranteed to succeed |
| 2.11 | Integrate `ConnectionStringProcessingHelper.ReplaceValueFromConnectionString` | `[ ]` | Template placeholder fill |
| 2.12 | Integrate `ConnectionStringValidationHelper.IsConnectionStringValid` | `[ ]` | Structural check |
| 2.13 | Integrate `ConnectionStringSecurityHelper.SecureConnectionString` | `[ ]` | Mask before log |
| 2.14 | Persist validated connection via `ConfigEditor.AddDataConnection` | `[ ]` | |
| 2.15 | Update `SetupContext.DataSource` on successful open | `[ ]` | Call `editor.OpenDataSource` |
| 2.16 | Implement `CanSkip` guard on `ConnectionConfigStep` (connection already exists + open) | `[ ]` | Idempotency |
| 2.17 | Unit tests: all three driver states (loaded/cached/missing) + all ConnectionConfigStep validation branches | `[ ]` | Use mock assemblyHandler for State 2/3; see Phase 2 plan Testing Approach |
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
