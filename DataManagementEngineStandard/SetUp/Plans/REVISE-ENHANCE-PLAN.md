# BeepDM Setup Wizard — Revise & Enhance Plan

> Generated: 2026-06-12 | Source: Full audit of 42 files across `SetUp/`

---

## Executive Summary

The setup wizard system (Phases 1-5, 9) is **structurally sound** and production-grade. 
The 6-step pipeline, 6 platform adapters, seeding subsystem, bootstrapper, and DI layer 
are all implemented. The remaining work splits into three tiers:

| Tier | Scope | Effort Est. | Impact |
|------|-------|-------------|--------|
| **A – Revise existing** | Fix bugs, harden edge cases, add tests, tracker cleanup | 2-3 weeks | Medium |
| **B – Production readiness** | Phase 6 (preflight/rollback) + Phase 7 (observability/audit) | 3-4 weeks | High |
| **C – Enterprise features** | Phase 8 (multi-tenant, upgrade, CI/CD) | 4-6 weeks | Low-Medium |

---

## Tier A — Revise & Harden Existing Code

### A.1 Critical Bug Fixes & Safety Gaps

| # | Issue | File(s) | Severity |
|---|-------|---------|----------|
| A.1.1 | `SetupContext` is not thread-safe — steps read/write mutable state on a thread-pool thread while platform adapters may read context concurrently | `SetupContext.cs` | **High** |
| A.1.2 | `SetupWizard.Run()` catches generic `Exception` and wraps it in `IErrorsInfo`, which hides stack traces. Should also log via `ILogger`/`Debug` before swallowing | `SetupWizard.cs:around-500` | **Medium** |
| A.1.3 | Checkpoint atomic file move has retry logic (5 attempts, 30ms) but no backoff — collisions on multi-process access will retry 5×120ms = fast-fail instead of waiting | `SetupWizard.cs:PersistState` | **Low** |
| A.1.4 | `SchemaSetupStep.CanSkip` compares entity list hash but does NOT detect when entities changed (added/removed types). The hash covers type names only — property changes silently skip re-migration | `Steps/SchemaSetupStep.cs:CanSkip` | **High** |
| A.1.5 | `ConnectionConfigStep` normalizes file paths but does not validate the path exists on disk for file-based datasources (SQLite/LiteDB) — fails at schema step instead | `Steps/ConnectionConfigStep.cs` | **Medium** |
| A.1.6 | `DriverProvisionStep` calls `SaveConnectionDriversConfigValues()` after NuGet download but does not call `ReloadDrivers()` — may leave stale in-memory driver list | `Steps/DriverProvisionStep.cs` | **Medium** |
| A.1.7 | `SeederBase` catches all exceptions and returns `ErrorsInfo` but never logs the exception — silent failures in seeders | `Seeding/SeederBase.cs` | **Medium** |
| A.1.8 | `ApplicationBootstrapper.BootstrapAsync` calls `_editorAccessor()` every call — if the editor was disposed between calls, it may throw `ObjectDisposedException` instead of a helpful message | `ApplicationBootstrapper.cs:129` | **Low** |
| A.1.9 | `DataImportStep` verifies entity presence but does not import data. The class doc says "delegated to a separate `DataImportManager`" but no such manager is referenced or wired | `Steps/DataImportStep.cs` | **Low** |

### A.2 Idempotency & Resume Hardening

| # | Task | Priority |
|---|------|----------|
| A.2.1 | Add a `RunId` (GUID) to `SetupState` so two concurrent runs cannot corrupt each other's checkpoint. The wizard should validate the `RunId` matches on resume | **High** |
| A.2.2 | Schema hash in `CanSkip` should include column-level schema inspection via `SchemaManager` to detect property changes, not just type-name changes | **High** |
| A.2.3 | `SeedingStep` tracks `CompletedSeederIds` but does NOT verify the seeder's `IsAlreadySeeded` still returns true on resume — a seeder could have been partially executed and resume may re-run rows | **Medium** |
| A.2.4 | `ConnectionConfigStep` has no idempotency guard. If the step is re-run (e.g. wizard resumed after connection-fail), it will duplicate/re-add the connection configuration | **Medium** |

### A.3 Missing Tests (Zero Coverage)

| # | Test Suite | Effort | Priority |
|---|-----------|--------|----------|
| A.3.1 | **Unit tests for orchestration core**: `SetupWizard` state machine, checkpoint persistence, resume, graph validation, builder validation | 3-4 days | **P0** |
| A.3.2 | **Unit tests for steps**: Each of the 6 concrete steps with mocked `IDMEEditor`/`IDataSource` | 4-5 days | **P0** |
| A.3.3 | **Unit tests for seeding**: `SeederRegistry` topological sort, circular dependency detection, idempotency, partial failure resume | 2-3 days | **P0** |
| A.3.4 | **Unit tests for adapters**: Each adapter lifecycle (RunAsync → progress → completion), cancellation, error paths | 3-4 days | **P0** |
| A.3.5 | **Unit tests for bootstrapper**: `ApplicationBootstrapper` first-run/no-first-run/cancel/error paths, `FileBasedFirstRunDetector` | 2 days | **P0** |
| A.3.6 | **Integration tests**: End-to-end with InMemory datasource, end-to-end with real SQLite driver | 3-4 days | **P1** |
| A.3.7 | **Create test project**: Add `tests/SetupWizardTests/` with `.csproj` referencing xUnit + Moq (or the project's existing test framework) | 1 day | **P0** |

### A.4 API Polish & Doc Comments

| # | Task | Priority |
|---|------|----------|
| A.4.1 | Add XML doc comments (`/// <summary>`) to all public types lacking them (check: `ISetupProgressReporter`, `SetupOptions`, `SetupState`, `SetupReport`, `SetupWizardBuilder`, `DefaultSetupWizardFactory`, all adapters) | **Medium** |
| A.4.2 | Add `[EditorBrowsable(EditorBrowsableState.Never)]` to `SingleWizardFactory` (internal implementation detail) | **Low** |
| A.4.3 | Consider `record` for `SetupReport`, `SetupStepResult`, `BootstrapResult` — they are immutable results | **Low** |
| A.4.4 | `ISetupWizardAdapter.ShowStep()`, `ShowProgress()`, `ShowResult()` are declared but never called from `SetupWizard`. Either wire them or mark obsolete | **Medium** |

### A.5 DI & Configuration Improvements

| # | Task | Priority |
|---|------|----------|
| A.5.1 | `SetupWizardServiceExtensions.AddSetupWizard()` creates a wizard via `DefaultSetupWizardFactory` but doesn't allow configuring `SetupOptions` — add an overload accepting `Action<SetupOptions>` | **Medium** |
| A.5.2 | Support `IOptions<SetupOptions>` pattern so options can be bound from `appsettings.json` | **Low** |
| A.5.3 | The `SetupWizardServices` holder class leaks an `ISetupWizard` singleton from DI but wizards should be transient — the bootstrapper rebuilds them anyway. Remove the singleton registration or document the intent | **Medium** |

### A.6 Tracker & Documentation Cleanup

| # | Task | Priority |
|---|------|----------|
| A.6.1 | Update `MASTER-TODO-TRACKER.md` — Phase 3 tasks 3.1-3.11, Phase 4 tasks 4.1-4.9, Phase 5 tasks 5.1-5.7 all show `[ ]` but code exists. Mark them `[x]` | **High** |
| A.6.2 | Cross-cutting X.2: Review all plans against `migration` and `idatasource` SKILL docs for API accuracy | **Medium** |
| A.6.3 | Cross-cutting X.5: Audit all step `Execute()` returns — ensure every code path returns `IErrorsInfo` and never throws (exception: `OperationCanceledException`) | **Medium** |
| A.6.4 | Apply conventions consistently: plan documents say `SeedAsync` but interface uses `Seed`; plan says `ISetupWizard.Pause()` exists but it was removed. Sync docs to code | **Low** |

---

## Tier B — Phase 6 & 7: Production Readiness

### B.1 Phase 6 — Pre-flight, Validation, and Rollback

*(Plans exist in `06-phase6-preflight-validation-rollback.md` — zero code implemented)*

| # | Task | Effort |
|---|------|--------|
| B.1.1 | Implement `PreflightValidationStep : ISetupStep` — wraps `MigrationManager.RunPreflightChecks`, runs connectivity check, provider capability matrix, config existence, entity list sanity. Insert between driver-provision and connection-config | 2 days |
| B.1.2 | Implement `SchemaHealthCheckStep : ISetupStep` — post-schema verification that all entities in the type list have corresponding tables/columns via `SchemaManager.InspectManyAsync`. Insert after schema-setup | 1 day |
| B.1.3 | Implement `SeedHealthCheckStep : ISetupStep` — post-seed verification that expected row counts exist. Insert after seeding | 1 day |
| B.1.4 | Implement `RollbackOrchestrator` — coordinates `MigrationManager.RollbackFailedExecution` + seed undo via `IUndoableSeeder` contract. Driven by `SetupWizard` when any step fails after schema-setup | 3-4 days |
| B.1.5 | Add `IUndoableSeeder` interface — `Undo(IDataSource, IDMEEditor)` for seeders that can reverse their inserts | 1 day |
| B.1.6 | Wire `SetupState.FailedStepId` tracking into rollback decision matrix | 1 day |
| B.1.7 | Append `RollbackReport` to `SetupReport` | 1 day |
| B.1.8 | Update `DefaultSetupWizardFactory` to insert new validation steps in the standard pipeline | 0.5 day |
| B.1.9 | Write unit + integration tests | 3 days |

### B.2 Phase 7 — Observability, Reporting, and Audit

*(Plans exist in `07-phase7-observability-reporting-audit.md` — zero code implemented)*

| # | Task | Effort |
|---|------|--------|
| B.2.1 | Extend `SetupReport` schema: add `MigrationAuditEvents`, `SeederAuditEntries`, `AuditTrail` (list of `SetupAuditEntry`), `FailureReason` | 1 day |
| B.2.2 | Implement `SetupAuditCollector` — hooks into `SetupWizard` step lifecycle to collect per-step timing, decisions, entity manifests, and error details into typed audit entries | 2 days |
| B.2.3 | Integrate `MigrationManager.GetMigrationAuditEvents()` into report | 1 day |
| B.2.4 | Add `SetupTelemetryEvent` enum (27 event types from plan) and emit events from `SetupWizard` via `IObservable<SetupTelemetryEvent>` or `Action<SetupTelemetryEvent>` callback | 2 days |
| B.2.5 | Implement `SetupReportExporter` — JSON output + Markdown table output to directory. Validate plan content for CI artifact production | 1.5 days |
| B.2.6 | Wire `ILogger`/`BeepLog` calls throughout `SetupWizard`, all steps, `SeederBase`, `ApplicationBootstrapper`. Add `ILogger?` parameter to step constructors (backward-compat: default null, fall back to Debug) | 2 days |
| B.2.7 | Verify `ContentHash` computation covers all report fields (currently only `StepResults`). Add hash of audit events and seeder events | 0.5 day |
| B.2.8 | Write unit + integration tests | 2 days |

---

## Tier C — Phase 8: Enterprise Features

*(Plans exist in `08-phase8-advanced-multitenant-upgrade-cicd.md` — zero code implemented)*

### C.1 Multi-Tenant Setup

| # | Task | Effort |
|---|------|--------|
| C.1.1 | Define `ITenantResolver` interface — resolves tenant ID from current context (claims, header, connection string suffix) | 1 day |
| C.1.2 | Implement `TenantSetupContext : SetupContext` — adds `TenantId`, `TenantConnectionString`, schema prefix | 1 day |
| C.1.3 | Implement `MultiTenantSetupOrchestrator` — iterates `ITenantResolver.GetTenants()`, runs `ISetupWizard` per tenant, aggregates `MultiTenantSetupReport` | 3 days |
| C.1.4 | Write tests | 2 days |

### C.2 Upgrade Wizard

| # | Task | Effort |
|---|------|--------|
| C.2.1 | Implement `SchemaVersionTracker` — persists schema versions in a `__BeepSetupVersion` table; writes hash+timestamp on each successful schema run | 2 days |
| C.2.2 | Implement `UpgradeWizard : ISetupWizard` — compares current entity hash against `SchemaVersionTracker`, detects delta, builds migration plan only for changed entities, re-seeds changed entities | 4 days |
| C.2.3 | Implement `UpgradeSeederResolver` — only runs seeders for entities whose schema changed (avoids re-seeding unchanged tables) | 1.5 days |
| C.2.4 | Write tests | 3 days |

### C.3 CI/CD Headless Mode

| # | Task | Effort |
|---|------|--------|
| C.3.1 | Implement `CiSetupRunner` — non-interactive, exit-code-aware, writes JSON artifacts, validates plans via `ValidatePlanForCi`. Uses `ConsoleSetupWizardAdapter` internally | 2 days |
| C.3.2 | Wire `SetupOptions.DryRun` through entire pipeline — ensure all steps respect dry-run (plan only, no execute) | 1.5 days |
| C.3.3 | Implement `CiValidationResult` — structured result with plan validation errors, exit code mapping | 1 day |
| C.3.4 | Add GitHub Actions workflow template example | 1 day |
| C.3.5 | Write tests | 2 days |

---

## Recommended Execution Order

```
1.  A.6.1 — Fix stale tracker                    (0.5 day)
2.  A.3.7 — Create test project                  (1 day)
3.  A.1.4 — Fix schema hash detection            (1 day)   ← critical bug
4.  A.1.1 — Thread safety for SetupContext       (1 day)   ← critical bug
5.  A.2.1 — RunId for checkpoint isolation       (0.5 day)
6.  A.1.2 — Wire ILogger to wizard/steps         (1 day)
7.  A.3.1-3.2 — Core + step unit tests           (8 days)
8.  A.1.5-1.9 — Remaining bug fixes              (3 days)
9.  A.3.3-3.7 — Remaining tests                  (9 days)
10. A.2.2-2.4 — Idempotency hardening            (2 days)
11. A.4, A.5, A.6.2-6.4 — Polish & docs          (3 days)
    ───────────────── TIER A COMPLETE ────────────
12. B.1.1-1.3 — Preflight + health checks        (4 days)
13. B.1.4-1.6 — Rollback orchestrator            (5 days)
14. B.1.7-1.9 — Rollback wiring + tests          (4.5 days)
15. B.2.1-2.5 — Observability + report exporter  (6.5 days)
16. B.2.6-2.8 — Logger + content hash + tests    (4.5 days)
    ───────────────── TIER B COMPLETE ────────────
17. C.1 — Multi-tenant                           (7 days)
18. C.2 — Upgrade wizard                         (10.5 days)
19. C.3 — CI/CD headless mode                    (7.5 days)
    ───────────────── TIER C COMPLETE ────────────
```

**Total estimated effort: ~77 working days** (roughly 3-4 months for a single developer)

### Quick Wins (1 Week)

If time is limited, prioritize these for maximum impact with minimal effort:

1. **Fix tracker** (`MASTER-TODO-TRACKER.md`) — 0.5 day
2. **Fix schema hash bug** (A.1.4) — 1 day — prevents silent data loss on entity changes
3. **Add thread safety** (A.1.1) — 1 day — prevents heisenbugs
4. **Wire ILogger** (A.1.2, B.2.6) — 1 day — makes debugging possible
5. **Create test project + 5 smoke tests** (A.3.7, partial A.3.1) — 2 days
6. **Update diagrams in README.md** — 0.5 day

---

## Architectural Notes for Implementation

### DI & Lifetime Decisions

- `ISetupWizard` instances should be **transient**, not singleton. The bootstrapper already rebuilds wizards per `BootstrapAsync()` call. Remove the singleton holder in `SetupWizardServiceExtensions`.
- `SetupContext` should be **scoped** to a wizard run, not a DI singleton. Have `ISetupWizardFactory.CreateDefault()` return a new context each time.
- Platform adapters remain **singleton** (they are stateless bridges).

### Adapter Location Decision

The plan documents suggest moving Blazor/MAUI adapters to platform projects. **Current approach (keep in engine, use virtual extension points) is the better pattern** — it avoids NuGet package split and allows all platforms to share a single `TheTechIdea.Beep.DataManagementEngine` package. Keep adapters in the engine and mark extension points as `protected virtual`.

### Step Injection Pattern

Steps currently take their `*StepOptions` via constructor. For loggable steps (Phase 7), add an optional `ILogger?` parameter:

```csharp
public SchemaSetupStep(SchemaSetupStepOptions opts, ILogger<SchemaSetupStep>? logger = null)
```

This is backward-compatible and enables structured logging without forcing a DI dependency.

### Testing Strategy

- **Unit tests**: Mock `IDMEEditor`, `IDataSource`, `MigrationManager`, `ConnectionHelper`. Use xUnit + Moq.
- **Integration tests**: Use SQLite InMemory (`DataSource=:memory:`) for real driver testing. Tests should live in a separate `tests/SetupWizardTests/` project that references `DataManagementEngine.csproj`.
- **Smoke test**: Create default wizard → run end-to-end against InMemory → assert report succeeded.
