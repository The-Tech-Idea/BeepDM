---
name: beepdm-setup
description: Use when building or running the BeepDM Setup Framework wizard — first-run initialization (driver provisioning, connection setup, schema creation, seeding) across Desktop, Blazor Server/WASM, MAUI, Web API, and Console adapters. Hands off to Migration (schema), Configuration (persistence), and ETL (initial load) skills.
---

# beepdm-setup

The Setup Framework is a **wizard-based** automated initializer for BeepDM applications. It sequences platform-neutral steps (driver → connection → schema → seed) and exposes them through platform-specific adapters so the same wizard runs unchanged on Desktop, Blazor, MAUI, Web API, or CLI.

## When to use this skill

- First-run application initialization on a new environment.
- Adding a new platform adapter (Blazor, MAUI, etc.).
- Implementing a custom setup step (e.g. tenant provisioning, license check).
- Debugging setup state, idempotency, checkpoint, or resume issues.
- Extending seeding with new `ISeeder` implementations.

## Do NOT use this skill for

- Schema evolution after initial setup → use **beepdm-migration**.
- CRUD / transactional app logic → use **beepdm-unitofwork**.
- Persisted connection / query / driver management → use **beepdm-configuration**.

## File Locations

**Core (`DataManagementEngineStandard/SetUp/`):**
- `SetupWizard.cs` — main orchestrator
- `ISetupStep.cs` — step contract
- `SetupContext.cs` — shared per-run context
- `SetupState.cs` — checkpoint state
- `SetupOptions.cs` — wizard options
- `SetupReport.cs` — final report
- `SetupWizardBuilder.cs` — fluent builder
- `SetupWizardServiceExtensions.cs` — DI helpers

**Steps (`SetUp/Steps/`):**
- `DriverProvisionStep` — Phase 2: load driver
- `ConnectionConfigStep` — Phase 2: configure connection
- `SchemaSetupStep` — Phase 3: apply schema
- `DefaultsSetupStep` — Phase 3: apply defaults

**Seeding (`SetUp/Seeding/`):**
- `ISeeder.cs`, `SeederBase.cs`, `ReferenceDataSeederBase.cs`
- `SeederRegistry.cs` — registry + topological sort

**Adapters (`SetUp/Adapters/`):**
- `DesktopSetupWizardAdapter` — WinForms/WPF
- `WebApiSetupWizardAdapter` — ASP.NET Core
- `BlazorServerSetupWizardAdapter`, `BlazorWasmSetupWizardAdapter`
- `MauiSetupWizardAdapter` — .NET MAUI
- `ConsoleSetupWizardAdapter` — CLI / BeepShell

**Factory:** `DefaultSetupWizardFactory.cs`

## Quick Start

```csharp
var wizard = new SetupWizardBuilder()
    .WithId("app-setup")
    .AddStep(new DriverProvisionStep(driverOpts))
    .AddStep(new ConnectionConfigStep(connOpts))
    .AddStep(new SchemaSetupStep(schemaOpts))
    .AddStep(new SeedingStep(seedingOpts))
    .Build();

var adapter = new DesktopSetupWizardAdapter(progressCallback, completeCallback);
var report = await adapter.RunAsync(wizard, context);
```

## Key Responsibilities

- **Orchestration**: `SetupWizard` runs steps in sequence, checkpoints after each, supports Resume.
- **State sync**: `SyncFromContext` (pulls fresh step writes) and `SyncToContext` (pushes bookkeeping) keep steps consistent.
- **Idempotency**: every step implements `CanSkip` based on deterministic checks (hash, entity count, etc.). Re-running the wizard must be safe.
- **Progress**: unified `IProgress<PassedArgs>` per-step with percentage + message.
- **Adapters**: bridge the core wizard to platform-specific UIs / runtimes. They own the UX, not the wizard.

## 8-Phase Mental Model

| Phase | What | Where |
|---|---|---|
| 1 | Wizard composition | `SetupWizardBuilder` |
| 2 | Driver provisioning | `DriverProvisionStep` |
| 3 | Connection configuration | `ConnectionConfigStep` |
| 4 | Schema creation | `SchemaSetupStep` (+ `MigrationManager`) |
| 5 | Defaults | `DefaultsSetupStep` |
| 6 | Seeding | `SeedingStep` (ordered by `SeederRegistry`) |
| 7 | Adapter execution | one of the 6 adapters |
| 8 | Reporting | `SetupReport` |

## How this skill works with the rest of the data-management layer

| Handoff | Direction | What flows |
|---|---|---|
| **beepdm-migration** | → Migration | Phase 4 (`SchemaSetupStep`) calls into `MigrationManager` to create the initial schema. Setup does not duplicate migration logic; it composes migration. |
| **beepdm-configuration** | ↔ Config | Phase 3 (`ConnectionConfigStep`) reads/writes through `ConfigEditor` — never ad-hoc JSON. New connection persists in `DataConnections.json`. |
| **beepdm-etl** | → ETL | Phase 6 (seeding) can hand off to ETL for non-trivial initial loads. The wizard's job is "make the system usable"; ETL's job is "move data." |
| **beepdm-unitofwork** | ← UoW | UoW is the runtime API the app uses **after** setup completes. Setup guarantees schema + seed; UoW is what runs CRUD against that schema. |
| **beepdm-forms** | ← Forms | Forms bind to entities created during setup. No special wiring needed — they use the same `IDataSource` that setup configured. |

## Design Rules

- Steps must be **idempotent** — `CanSkip` returns true if a deterministic check (hash, row count, schema fingerprint) shows the step is already done.
- Adapters own **presentation only**. Do not embed UI logic in steps.
- Driver provisioning is **step 1** — connection config needs the driver to validate.
- Persist checkpoint after each step so a crash resumes from the right place.
- Use `IProgress<PassedArgs>` for UI updates; do not couple to a specific adapter.

## Cross-references

- See **beepdm-migration** for the schema-creation work step 4 calls into.
- See **beepdm-configuration** for the persisted connection + driver store.
- See **beepdm-etl** for optional initial data load.
- See `.cursor/setup/SKILL.md` for the deep-dive implementation details.
