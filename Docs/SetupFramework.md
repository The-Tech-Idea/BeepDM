# Setup Framework Guide

Wizard-based first-run initialization for a BeepDM application across Desktop, Console, Web API,
Blazor (Server + WASM), and MAUI.

**Canonical reference:** [`DataManagementEngineStandard/SetUp/README.md`](../DataManagementEngineStandard/SetUp/README.md)
— it documents the contracts, the real step/adapter inventory, and the current gaps.
**Roadmap:** [`.plans/setup/MASTER-SETUP-TRACKER.md`](../.plans/setup/MASTER-SETUP-TRACKER.md).

> Scope today is **one app, one connection, local disk, forward-only**. No solution/app aggregate,
> no multi-app, no identity/RBAC, no remote state, no rollback execution.

## Entry point

```csharp
var wizard = new SetupWizardBuilder()
    .WithId("app-setup")
    .AddStep(new DriverProvisionStep(driverOpts))
    .AddStep(new ConnectionConfigStep(connOpts))
    .AddStep(new SchemaSetupStep(schemaOpts))
    .AddStep(new SeedingStep(new SeedingStepOptions { Registry = registry }))
    .Build();

var context = new SetupContext { Editor = dmeEditor, Options = options, State = new SetupState() };
var adapter = new ConsoleSetupWizardAdapter();
SetupReport report = await adapter.RunAsync(wizard, context, ct);
```

Pass the **same `SetupOptions` instance** to the builder and the context — `SetupWizard.Run` uses
`context.Options ?? Options`, so a context with default options silently overrides `DryRun`.

`SeedingStepOptions.Registry` is required; `SeedingStep.Validate` fails without it.

## Steps

Six steps, in dependency order:

| Step | `StepId` | Depends on |
|---|---|---|
| `DriverProvisionStep` | `driver-provision` | — |
| `ConnectionConfigStep` | `connection-config` | `driver-provision` |
| `SchemaSetupStep` | `schema-setup` | `connection-config` |
| `DefaultsSetupStep` | `defaults-setup` | `schema-setup` |
| `SeedingStep` | `seeding` | `schema-setup` |
| `DataImportStep` | `data-import` | `defaults-setup`, `seeding` |

`DataImportStep` verifies and counts rows — it does **not** import. Use `DataImportManager` for that.

## Adapters

All six live in `SetUp/Adapters/` and are platform-agnostic (no WinForms/WPF/MAUI/JSInterop
references):

- `DesktopSetupWizardAdapter` — WinForms/WPF
- `ConsoleSetupWizardAdapter` — CLI output (not an arg-parsing CLI)
- `WebApiSetupWizardAdapter` — ASP.NET Core, exposes `SetupAdapterStatus`
- `BlazorServerSetupWizardAdapter` — Blazor Server
- `BlazorWasmSetupWizardAdapter` — Blazor WASM, resumes state via JSON
- `MauiSetupWizardAdapter` — .NET MAUI

## Options

`SetupOptions` (all `init`-only): `DryRun`, `SkipSeeding`, `SkipSchema`, `Environment`
(default `"Development"`, a free-form label), `StrictPolicyMode`, `StateFilePath`, `ReportOutputPath`.

Checkpointing is **off unless `StateFilePath` is set** — it silently no-ops otherwise.
`ReportOutputPath` is currently never read.

Only `SchemaSetupStep` performs real dry-run work; other steps skip when `DryRun` is set.

## DI

```csharp
services.AddSetupWizard();       // one ISetupWizard, singleton
services.AddBeepBootstrapper();  // first-run detection + wizard + adapter
```

`BeepBootstrapper` runs the wizard only when `IFirstRunDetector` reports a first run, then writes a
`.setup_complete` marker under `ConfigEditor.ConfigPath`.

## File locations

- Contracts — `DataManagementModelsStandard/SetUp/`
- Implementation — `DataManagementEngineStandard/SetUp/`
- Steps — `DataManagementEngineStandard/SetUp/Steps/`
- Adapters — `DataManagementEngineStandard/SetUp/Adapters/`
- Seeding — `DataManagementEngineStandard/SetUp/Seeding/`

## Related

- [Core Architecture](CoreArchitecture.md) · [Service Registration](ServiceRegistration.md)
