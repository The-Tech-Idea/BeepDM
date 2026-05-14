# Setup Wizard Plans — README

This folder contains the phased design plan for the **BeepDM Setup Wizard** system.

The wizard provides a structured, multi-step, platform-agnostic mechanism to bootstrap any supported database: create schema, seed reference data, validate, rollback on failure, emit audit reports, and run headlessly in CI/CD pipelines.

---

## Document Index

| Document | Phase | Topic | Status |
|---|---|---|---|
| [00-overview-setup-wizard-gap-matrix.md](00-overview-setup-wizard-gap-matrix.md) | Overview | Architecture overview, gap matrix, design principles | Planning |
| [MASTER-TODO-TRACKER.md](MASTER-TODO-TRACKER.md) | All | Central task tracker with phase-by-phase checklist | Planning |
| [01-phase1-contract-and-wizard-architecture.md](01-phase1-contract-and-wizard-architecture.md) | Phase 1 | Core contracts, base wizard, step model | Planning |
| [02-phase2-connection-driver-configuration.md](02-phase2-connection-driver-configuration.md) | Phase 2 | Connection builder step, driver resolution | Planning |
| [03-phase3-schema-creation-ddl-migration.md](03-phase3-schema-creation-ddl-migration.md) | Phase 3 | Schema creation via MigrationManager | Planning |
| [04-phase4-seeding-and-data-load.md](04-phase4-seeding-and-data-load.md) | Phase 4 | Seeder registry, topological ordering, idempotency | Planning |
| [05-phase5-platform-adapters.md](05-phase5-platform-adapters.md) | Phase 5 | Desktop, CLI, Web API, Blazor Server/WASM, MAUI adapters | Planning |
| [06-phase6-preflight-validation-rollback.md](06-phase6-preflight-validation-rollback.md) | Phase 6 | Pre-flight checks, health checks, rollback orchestration | Planning |
| [07-phase7-observability-reporting-audit.md](07-phase7-observability-reporting-audit.md) | Phase 7 | Audit trail, telemetry events, report export (JSON + Markdown) | Planning |
| [08-phase8-advanced-multitenant-upgrade-cicd.md](08-phase8-advanced-multitenant-upgrade-cicd.md) | Phase 8 | Multi-tenant orchestration, upgrade wizard, CI/CD headless mode | Planning |

---

## Phase Summary

### Phase 1 — Contract and Wizard Architecture
Defines the minimal contracts that all other phases build on:
`ISetupStep`, `ISetupWizard`, `SetupContext`, `SetupState`, `SetupOptions`, `SetupReport`, `SetupWizardBuilder`.

**Key types:** `ISetupStep`, `ISetupWizard`, `SetupContext`, `SetupState`, `SetupWizardBuilder`, `SetupWizard`

### Phase 2 — Connection and Driver Configuration
Guided step for building and persisting a `ConnectionProperties` using `ConnectionHelper`. Handles all provider families (SQLite, SQL Server, PostgreSQL, Oracle, In-Memory, CSV/JSON).

**Key types:** `ConnectionConfigStep`, `ConnectionConfigStepOptions`

### Phase 3 — Schema Creation and DDL Migration
Wraps the full `MigrationManager` pipeline — plan → policy → dry-run → preflight → impact → compensation → execute. Idempotency via SHA-256 schema hash.

**Key types:** `SchemaSetupStep`, `SchemaSetupStepOptions`

### Phase 4 — Seeding and Data Load
`ISeeder` / `ISeederRegistry` with Kahn topological sort, `DefaultsManager` integration, resumable partial-run tracking, `SeederBase` / `ReferenceDataSeederBase<T>`.

**Key types:** `ISeeder`, `ISeederRegistry`, `SeederRegistry`, `SeederBase`, `SeedingStep`, `SeedingStepOptions`

### Phase 5 — Platform Adapters
Bridge implementations for Desktop WinForms/WPF, Console/CLI, ASP.NET Core Web API, Blazor Server (SignalR), Blazor WASM (localStorage), and MAUI (MainThread).

**Key types:** `ISetupWizardAdapter`, `ISetupWizardFactory`, `DesktopSetupWizardAdapter`, `ConsoleSetupWizardAdapter`, `WebApiSetupWizardAdapter`, `BlazorServerSetupWizardAdapter`, `BlazorWasmSetupWizardAdapter`, `MauiSetupWizardAdapter`

### Phase 6 — Pre-flight, Validation, and Rollback
Provider capability gates before schema changes, entity existence checks after schema, minimum row-count checks after seeding, and `RollbackOrchestrator` using `MigrationManager` compensation.

**Key types:** `PreflightValidationStep`, `SchemaHealthCheckStep`, `SeedHealthCheckStep`, `RollbackOrchestrator`, `RollbackReport`, `IUndoableSeeder`

### Phase 7 — Observability, Reporting, and Audit
Structured `SetupTelemetryEvent` enum, `SetupAuditCollector`, SHA-256 `ContentHash` for tamper detection, `SetupReportExporter` (JSON + Markdown), and `IDMLogger` integration.

**Key types:** `SetupTelemetryEvent`, `SetupAuditCollector`, `SetupAuditEntry`, `SetupReportExporter`, `SeederAuditEntry`

### Phase 8 — Advanced Features
- **Multi-tenant:** `TenantSetupContext`, `ITenantResolver`, `MultiTenantSetupOrchestrator` — fan-out wizard runs per tenant with aggregated report.
- **Upgrade Wizard:** `SchemaVersionTracker`, `UpgradeWizard` — detect and apply only schema/seed deltas.
- **CI/CD:** `CiSetupRunner` — headless, exit-code-aware, report-emitting runner with GitHub Actions / Azure Pipelines integration guide.

---

## Recommended Step Order (Full Pipeline)

```
PreflightValidationStep       (Phase 6)
ConnectionConfigStep          (Phase 2)
SchemaSetupStep               (Phase 3)
SchemaHealthCheckStep         (Phase 6)
SeedingStep                   (Phase 4)
SeedHealthCheckStep           (Phase 6)
```

Rollback (`RollbackOrchestrator`) triggers automatically on any step failure between `ConnectionConfigStep` and `SeedHealthCheckStep`.

---

## Delivery Priority

| Priority | Phases | Justification |
|---|---|---|
| **MVP** | 1 → 2 → 3 → 4 → 5 | Core wizard is functional on all platforms |
| **Production-ready** | + 6 → 7 | Add safety gates, audit trail, report export |
| **Enterprise** | + 8 | Multi-tenant, upgrade workflows, CI/CD gates |

---

## Key Conventions

- Return `IErrorsInfo` from all step Execute methods — never throw from orchestration code.
- Report progress via `IProgress<PassedArgs>` where `ParameterInt1` is percentage and `Messege` is description.
- Log every significant event via `IDMLogger.WriteLog`.
- Never log raw `ConnectionString` values — use `ConnectionStringSecurityHelper.SecureConnectionString` first.
- Platform-specific adapter implementations belong in their platform project, not in `DataManagementEngineStandard`. Only `ISetupWizardAdapter` and `ISetupWizardFactory` contracts live in the engine.
