# BeepDM Setup Wizard Program — Overview and Gap Matrix

## Objective

Define a phased, platform-aware **Setup Wizard** system that guides applications through:

1. **Database schema creation** (tables, indexes, constraints, views)
2. **Data seeding and initial loading** (defaults, reference data, admin seeds)
3. **Post-install validation** (schema health, seed verification)
4. **Environment-specific wiring** (Desktop WinForms/WPF, Web API, Blazor Server, Blazor WASM, MAUI, CLI)

All wizard logic is built on the existing `IDataSource`, `MigrationManager`, `ConfigEditor`, `BeepService`, and `ConnectionHelper` foundations. No new database drivers are introduced — the wizard orchestrates existing primitives.

---

## Platforms in Scope

| Platform | Service Lifetime | Progress Reporting | DI Pattern |
|---|---|---|---|
| Desktop WinForms / WPF | Singleton | `IProgress<PassedArgs>` → UI callback | `AddBeepForDesktop()` |
| Console / CLI (BeepShell) | Singleton | Console write-through | `ShellServiceProvider` |
| ASP.NET Core Web API | Scoped | Background task + endpoint | `AddBeepForWeb()` |
| Blazor Server | Scoped | SignalR push | `AddBeepForBlazorServer()` |
| Blazor WASM | Singleton | Browser Storage | `AddBeepForBlazorWasm()` |
| MAUI | Singleton | `IProgress<PassedArgs>` → UI | `AddBeepForMaui()` |

---

## Current Baseline

- `MigrationManager` covers schema planning, dry-run, execution, rollback, and governance.
- `IDataSource.CreateEntityAs()` handles provider-agnostic table creation.
- `ConfigEditor.AddDataConnection()` / `UpdateDataConnection()` manage connection persistence.
- `ConnectionHelper` resolves drivers, normalizes paths, and validates strings.
- `BeepService` / `IBeepService` provides registered service access for all platforms.
- `DefaultsManager` injects field defaults before save.
- No unified **setup wizard orchestration layer** exists that combines these into a step-by-step, platform-adaptive, seedable, resumable flow.

---

## Gap Matrix

| Area | Current | Target | Priority |
|---|---|---|---|
| Wizard Contract | None | `ISetupWizard`, `ISetupStep`, `SetupContext` | P0 |
| Connection Configuration Step | Manual `ConnectionHelper` calls | Guided, validated connection builder step | P0 |
| Schema Creation Step | Manual `MigrationManager` calls | Wizard-driven schema apply with policy checks | P0 |
| Seeding / Data Load Step | None formal | `ISeeder`, `ISeederRegistry`, seed ordering, idempotency | P0 |
| Platform Adapters | None formal | Per-platform adapter: Desktop, Web, Blazor, MAUI, CLI | P0 |
| Pre-flight Validation | `MigrationManager.RunPreflightChecks` | Integrated wizard pre-flight gate step | P1 |
| Wizard State / Resume | None | Serializable `SetupState` with step checkpointing | P1 |
| Rollback on Partial Failure | `MigrationManager.RollbackFailedExecution` | Wizard-level rollback orchestration | P1 |
| Progress Reporting | Platform-specific | Unified `ISetupProgressReporter` surface + adapters | P1 |
| Seed Idempotency | None formal | `IIdempotentSeeder` check before re-seeding | P1 |
| Multi-Tenant Setup | None | Tenant-scoped setup context and routing | P2 |
| Upgrade Wizard | None | Delta-detect, migrate, re-seed changed entities | P2 |
| CI/CD Integration | `MigrationManager.ValidatePlanForCi` | Wizard headless/dry-run mode for pipelines | P2 |
| Setup Report / Audit | None formal | `SetupReport` with step results and hash | P2 |
| UI Shell Components | None | Platform-specific shell: modal, page, CLI menus | P3 |

---

## Planned Phases

| Phase | Title | Priority |
|---|---|---|
| 1 | Wizard Contract and Architecture Foundation | P0 |
| 2 | Connection and Driver Configuration Step | P0 |
| 3 | Schema Creation, DDL, and Migration Step | P0 |
| 4 | Data Seeding and Initial Load Step | P0 |
| 5 | Platform Adapters (Desktop / Web / Blazor / MAUI / CLI) | P0 |
| 6 | Pre-flight, Validation, and Rollback | P1 |
| 7 | Observability, Reporting, and Audit | P1 |
| 8 | Advanced Features (Multi-Tenant, Upgrade Wizard, CI/CD) | P2 |

---

## Core Design Principles

1. **Datasource-agnostic** — all schema steps flow through `IDataSource.CreateEntityAs()` and `MigrationManager`; no direct SQL generation in wizard code.
2. **Step-based, resumable** — each wizard step is an `ISetupStep` with a deterministic ID; a `SetupState` snapshot allows resume after crash or user pause.
3. **Platform-adaptive, not platform-specific** — a single `ISetupWizard` contract; platform adapters wrap it for UI surface differences.
4. **Idempotent by default** — every step is guarded by a "has this already been done?" check before re-applying.
5. **Non-destructive planning** — reuse `MigrationManager` plan-before-apply contract; setup never modifies schema without a generated plan artifact.
6. **Progress-first** — all long-running operations surface `IProgress<PassedArgs>` compatible notifications; platform adapters translate to native UI.
7. **Error-info propagation** — return `IErrorsInfo` from every step; never throw from wizard orchestration code.
8. **Seed ordering** — seeders declare dependencies so the engine can topologically sort execution order.
9. **CI/CD headless mode** — wizards can run in non-interactive dry-run or apply mode for automated pipelines.

---

## Key Types to Introduce (Summary)

```
ISetupWizard          — top-level wizard orchestration contract
ISetupStep            — single step contract (Id, Name, Execute, CanSkip, Validate)
SetupContext          — shared runtime state (editor, dataSource, options, progress)
SetupState            — serializable checkpoint (completed steps, plan hashes, timestamps)
SetupReport           — immutable outcome record per wizard run
ISeeder               — contract for a single data seed unit
ISeederRegistry       — registry for seeder discovery and ordering
ISetupProgressReporter — unified progress surface; adapters for each platform
ISetupWizardAdapter   — platform-specific wrapper around ISetupWizard
SetupWizardBuilder    — fluent builder for constructing a wizard from steps
```

---

## Success Criteria

- Any supported platform can bootstrap a full database from zero connection to seeded data in one `wizard.Run()` call.
- Every setup run produces a `SetupReport` with a content hash, per-step results, and elapsed time.
- Partial failures do not leave orphaned schema; rollback compensation is always available.
- Adding a new seeder requires only implementing `ISeeder` and registering it — no wizard code changes.
- The entire setup flow is testable in a headless `InMemory` datasource with no external infrastructure.
