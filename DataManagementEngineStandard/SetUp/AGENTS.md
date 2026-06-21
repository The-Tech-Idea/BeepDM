# AGENTS.md — BeepDM Setup Wizard Framework

> Guide for AI coding agents working with the setup wizard system.
> Last updated: 2026-06-12

---

## Namespace Map

**IMPORTANT: File-system folder names match namespaces here, but ALWAYS verify by reading the namespace declaration inside the file — folders can be misleading.**

| Folder | Namespace |
|--------|-----------|
| `SetUp/` | `TheTechIdea.Beep.SetUp` |
| `SetUp/Adapters/` | `TheTechIdea.Beep.SetUp.Adapters` |
| `SetUp/Seeding/` | `TheTechIdea.Beep.SetUp.Seeding` |
| `SetUp/Steps/` | `TheTechIdea.Beep.SetUp.Steps` |
| *(planned)* | `TheTechIdea.Beep.SetUp.Ci` |
| *(planned)* | `TheTechIdea.Beep.SetUp.MultiTenant` |
| *(planned)* | `TheTechIdea.Beep.SetUp.Upgrade` |

Cross-project types referenced by this framework:

| Type | Namespace | Project |
|------|-----------|---------|
| `IErrorsInfo`, `ErrorsInfo` | `TheTechIdea.Beep.ConfigUtil` | `DataManagementModelsStandard` |
| `PassedArgs` | `TheTechIdea.Beep.Addin` | `DataManagementModelsStandard` |
| `Errors` (enum) | `TheTechIdea.Beep.ConfigUtil` | `DataManagementModelsStandard` |
| `IDMEEditor` | `TheTechIdea.Beep.Editor` | `DataManagementEngineStandard` |
| `IDataSource` | `TheTechIdea.Beep.DataBase` | `DataManagementEngineStandard` |
| `ConnectionProperties` | `TheTechIdea.Beep.ConfigUtil` | `DataManagementModelsStandard` |

---

## Architecture Overview

```
ApplicationBootstrapper     (lifecycle: FirstRun → Wizard → MarkComplete)
        │
ISetupWizardAdapter        (platform seam — 6 adapters)
        │
ISetupWizard (SetupWizard) (orchestration engine — runs steps sequentially)
        │
ISetupStep (6 concrete)    (individual units of work)
        │
SetupContext / SetupState  (shared mutable state + checkpoint)
```

See `Plans/REVISE-ENHANCE-PLAN.md` for the full enhancement roadmap.

---

## Core Interfaces

### ISetupStep
The fundamental building block. Every step has:
- `StepId` — stable unique string (e.g. `"driver-provision"`)
- `StepName` — human-readable label
- `DependsOn` — ordered list of prerequisite StepIds
- `CanSkip(SetupContext)` — idempotency guard
- `Validate(SetupContext)` — pre-condition check
- `Execute(SetupContext, IProgress<PassedArgs>)` — the work; **must not throw, return IErrorsInfo**

### ISetupWizard
Orchestrator. Exposes `Steps`, `State`, `Options`, `Run()`, `Resume()`, `GetReport()`.

### ISetupWizardAdapter
Platform bridge. `RunAsync()` invokes the wizard within the platform's execution model.

### ISetupWizardFactory
Creates pre-configured wizards. Each call rebuilds the wizard fresh — host UIs stage user selections between calls.

### ISeeder
Seed-data unit. `SeederId`, `DependsOn`, `IsAlreadySeeded()`, `Seed()`.

### ISeederRegistry
Manages seeder collection with topological sort (Kahn's algorithm).

---

## Standard 6-Step Pipeline

```
1. driver-provision    (DriverProvisionStep)     — ensure DB driver loaded
2. connection-config   (ConnectionConfigStep)    — build/open connection
3. schema-setup        (SchemaSetupStep)         — create tables via MigrationManager
4. defaults-setup      (DefaultsSetupStep)       — audit timestamp defaults
5. seeding             (SeedingStep)             — seed reference data
6. data-import         (DataImportStep)          — verify entities exist
```

---

## How to Add a New Step

1. Create options class for the step if needed
2. Implement `ISetupStep`:
   ```csharp
   public class MyStep : ISetupStep
   {
       public string StepId => "my-step";
       public string StepName => "My Step";
       public string Description => "Does something.";
       public IReadOnlyList<string> DependsOn => new[] { "connection-config" };

       public bool CanSkip(SetupContext context) => false;
       public IErrorsInfo Validate(SetupContext context) => Ok();
       public IErrorsInfo Execute(SetupContext context, IProgress<PassedArgs> progress = null) { ... }
   }
   ```
3. **Always return `IErrorsInfo` from `Execute()` and `Validate()` — never throw.**
4. **Always implement `CanSkip()` with a proper idempotency check.**
5. Register the step via `SetupWizardBuilder.AddStep()` in `DefaultSetupWizardFactory`.

---

## How to Add a New Seeder

1. Extend `SeederBase` (or implement `ISeeder` directly):
   ```csharp
   public class MySeeder : SeederBase
   {
       public override string SeederId => "my-seeder";
       public override string SeederName => "My Seeder";
       public override IReadOnlyList<string> DependsOn => new[] { "roles-seeder" };
       protected override string TargetEntityName => "MyEntity";
       protected override IErrorsInfo SeedCore(IDataSource ds, IDMEEditor editor, IProgress<PassedArgs> progress) { ... }
   }
   ```
2. For fixed lookup tables, use `ReferenceDataSeederBase<T>`.
3. Override `IsAlreadySeeded()` for custom idempotency checks beyond row-count.
4. Register via `ISeederRegistry.Register()` or assembly discovery.

---

## DI Wiring (Microsoft.Extensions.DependencyInjection)

```csharp
services.AddFirstRunDetector();                          // IFirstRunDetector singleton
services.AddSetupWizard();                               // ISetupWizardFactory + SetupContext
services.AddSetupWizardAdapter<ConsoleSetupWizardAdapter>(); // platform adapter
services.AddApplicationBootstrapper();                   // bootstrapper singleton
```

For hosts where `IDMEEditor` isn't directly in DI (e.g. BeepWeb):
```csharp
services.AddApplicationBootstrapper(sp => sp.GetRequiredService<IBeepService>().DMEEditor);
```

---

## Key Conventions & Gotchas

### Do NOT:
- Throw exceptions from `Execute()`, `Validate()`, or `Seed()`. Return `IErrorsInfo` with `Errors.Failed`.
- Modify `SetupContext.State` without calling `SyncFromContext` / `SyncToContext` in the wizard.
- Hardcode SQL — all DDL flows through `MigrationManager` and `IDataSource`.
- Assume a specific database provider — the wizard is datasource-agnostic.

### DO:
- Use `CanSkip()` for idempotency on every step and seeder.
- Use `IProgress<PassedArgs>` for reporting — `PassedArgs.ParameterInt1` is percentage (0-100), `Messege` is the message.
- Pass `ILogger<T>?` as an optional constructor parameter (defaults to null for back-compat).
- Write checkpoint state via `context.State` properties — the wizard persists it automatically.
- Use `ConnectionProperties` for connection details — never store raw connection strings.

### Schema Hash (Critical)
`SchemaSetupStep.ComputeEntityListHash()` includes type names + public instance property names + property types. Changing a property type, renaming a property, or adding/removing properties WILL change the hash and trigger re-migration.

### State Lifecycle
- `SetupState.RunId` is assigned by `SetupWizard` on fresh runs; preserved on resume.
- `SetupState.SchemaHash` is computed after successful schema creation.
- `SetupState.CompletedSeederIds` tracks partial seeder progress for resume-after-failure.
- Checkpoint is persisted after every completed step via atomic temp-file + move.

### Error Pattern
```csharp
// For steps:
private static IErrorsInfo Ok(string msg = "Ok") =>
    new ErrorsInfo { Flag = Errors.Ok, Message = msg };
private static IErrorsInfo Fail(string msg, Exception ex = null) =>
    new ErrorsInfo { Flag = Errors.Failed, Message = msg, Ex = ex };
```

### Progress Pattern
```csharp
private static void Report(IProgress<PassedArgs> p, int pct, string msg) =>
    p?.Report(new PassedArgs { ParameterInt1 = pct, Messege = msg });
```

---

## Platform Adapters

All 6 adapters live in `SetUp/Adapters/` and share the namespace `TheTechIdea.Beep.SetUp.Adapters`:

| Adapter | Key Feature |
|---------|-------------|
| `DesktopSetupWizardAdapter` | Runs on thread-pool via `Task.Run()`, progress callback |
| `ConsoleSetupWizardAdapter` | `Console.WriteLine` table output, extensible via subclass |
| `WebApiSetupWizardAdapter` | Background thread + `SetupAdapterStatus` for polling |
| `BlazorServerSetupWizardAdapter` | Virtual hooks for SignalR (`IHubContext<SetupProgressHub>`) |
| `BlazorWasmSetupWizardAdapter` | Virtual hooks for localStorage (`IJSRuntime`) |
| `MauiSetupWizardAdapter` | Virtual `InvokeOnMainThreadAsync` for UI dispatch |

All adapters use virtual extension points — they compile without platform-specific references. The platform-specific binding happens in subclass overrides in the platform project.

---

## Test Project

Test project: `tests/SetupWizardTests/SetupWizardTests.csproj` (xUnit + Moq, net9.0)

Key test patterns:
- Use `DelegateStep` helper to create inline `ISetupStep` implementations for orchestration tests.
- Use `TestSeeder` / `TestSeederWithDeps` helpers for seeder registry tests.
- Schema hash tests validate property-level change detection.
- Import these namespaces: `TheTechIdea.Beep.Addin`, `TheTechIdea.Beep.ConfigUtil`, `TheTechIdea.Beep.DataBase`, `TheTechIdea.Beep.Editor`, `TheTechIdea.Beep.SetUp`, `TheTechIdea.Beep.SetUp.Seeding`, `Xunit`.

---

## Forms Engine — Architecture Rules (2026-06-18)

### Three Object Types Only
The UI emulates Oracle Forms with exactly 3 object types. Only 3 interfaces should exist:

| Interface | File | Purpose |
|---|---|---|
| `IBeepFormsHost` | `Hosts/IBeepFormsHost.cs` | Form host — block registration, navigation, CRUD, LOV, validation, messaging |
| `IBlockView` | `Hosts/IBlockView.cs` | Block view — Bind/Unbind, navigation, CRUD, events, field presenters |
| `IFieldPresenter` | `Hosts/IFieldPresenter.cs` | Field presenter — Value, Validate, Clear, factory methods |

**No other interfaces should exist.** Removed: `IBuiltinHost`, `IBeepBuiltins`, `IBlockNavigationBar`, `IFormsNotificationService`, `IFormsBootstrapper`,
`IBeepFieldPresenter`, `IBeepWpfFieldPresenter`, `IBeepWpfBlockView`, `IBeepBlockView`, `IBeepWpfBlockNavigationBar`.

### Architecture
```
Engine IUnitofWorksManager (~285 methods)
    ▲                               ▲
    │ _formsManager (direct)        │ IBeepFormsHost (45 methods)
    │                               │
BeepForms layer                   BeepBlock layer
(all engine methods)              (only what Blocks need)
```

### Interface Ownership
- ALL interfaces live in `Editor/Forms/Hosts/` ONLY
- NO interfaces in WPF or WinForms `Contracts/` directories
- Both WPF and WinForms implement engine interfaces directly — no extensions

### No Builtins
- `IBeepBuiltins`, `IBuiltinHost`, all builtin classes DELETED
- Engine operations (SetMessage, ShowAlert, Multi-form, etc.) accessed through `_formsManager` directly
- No adapters, no bridges, no wrapping layers

### Engine Helpers (authoritative, shared)
- `Helpers/RecordPropertyAccessor` — SINGLE reflection authority
- `Helpers/MessageClassifier` — SINGLE severity authority
- `Helpers/FieldTypeMapper` — SINGLE field type mapping
- `Helpers/FieldFormulaEvaluator` — SINGLE formula evaluator

### UI Layer Rules
- NO business logic — no DataTable, DataView, DataRowView
- NO raw reflection — use `RecordPropertyAccessor`
- NO severity duplication — use `MessageClassifier`
- NO `_boundUnitOfWork` or direct `IUnitOfWork` in Blocks
- Both WPF and WinForms implement engine interfaces directly
- Form-level operations use `_formsManager.*` directly

