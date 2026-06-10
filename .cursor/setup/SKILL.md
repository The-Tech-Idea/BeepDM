---
name: setup
description: End-to-end guidance for the 8-phase Setup Framework. Covers wizard orchestration, step implementation, state management, checkpointing/resume, and platform adapters (Desktop/Blazor/MAUI/CLI/Web). Use when implementing application initialization workflows, creating custom setup steps, or debugging setup state/idempotency issues.
---

# Setup Framework Skill

Unified entry point for all Setup Framework development in BeepDM.

## File Locations

**Core:**
- `DataManagementEngineStandard/SetUp/SetupWizard.cs` — Main orchestrator
- `DataManagementEngineStandard/SetUp/ISetupStep.cs` — Step contract
- `DataManagementEngineStandard/SetUp/SetupContext.cs` — Shared context
- `DataManagementEngineStandard/SetUp/SetupState.cs` — Checkpoint state
- `DataManagementEngineStandard/SetUp/SetupWizardBuilder.cs` — Fluent builder

**Steps (Phases 2–4):**
- `DataManagementEngineStandard/SetUp/Steps/DriverProvisionStep.cs` — Phase 2: Load driver
- `DataManagementEngineStandard/SetUp/Steps/ConnectionConfigStep.cs` — Phase 2: Configure connection
- `DataManagementEngineStandard/SetUp/Steps/SchemaSetupStep.cs` — Phase 3: Apply schema
- `DataManagementEngineStandard/SetUp/Steps/SeedingStep.cs` — Phase 4: Run seeders

**Seeding (Phase 4):**
- `DataManagementEngineStandard/SetUp/Seeding/ISeeder.cs` — Seeder contract
- `DataManagementEngineStandard/SetUp/Seeding/SeederBase.cs` — Abstract base
- `DataManagementEngineStandard/SetUp/Seeding/ReferenceDataSeederBase.cs` — Generic reference data
- `DataManagementEngineStandard/SetUp/Seeding/SeederRegistry.cs` — Registry + topological sort
- `DataManagementEngineStandard/SetUp/Seeding/ISeederRegistry.cs` — Registry contract

**Adapters (Phase 5):**
- `DataManagementEngineStandard/SetUp/Adapters/DesktopSetupWizardAdapter.cs` — WinForms/WPF
- `DataManagementEngineStandard/SetUp/Adapters/ConsoleSetupWizardAdapter.cs` — CLI/BeepShell
- `DataManagementEngineStandard/SetUp/Adapters/BlazorWasmSetupWizardAdapter.cs` — Blazor WASM
- `DataManagementEngineStandard/SetUp/Adapters/BlazorServerSetupWizardAdapter.cs` — Blazor Server
- `DataManagementEngineStandard/SetUp/Adapters/WebApiSetupWizardAdapter.cs` — ASP.NET Core
- `DataManagementEngineStandard/SetUp/Adapters/MauiSetupWizardAdapter.cs` — .NET MAUI

**Factory:**
- `DataManagementEngineStandard/SetUp/DefaultSetupWizardFactory.cs` — Default factory

## Key Responsibilities

**Orchestration:** SetupWizard runs steps in sequence, checkpoints after each, supports Resume.

**State Sync:** 
- `SyncFromContext` pulls fresh step writes before pushing back
- `SyncToContext` ensures all steps see current bookkeeping
- `PersistState` atomically saves checkpoint file

**Idempotency:** Every step implements CanSkip based on deterministic checks (hash, entity count, etc.).

**Progress:** Unified IProgress<PassedArgs> per-step with percentage + message.

**Adapters:** Bridge between core SetupWizard and platform-specific UIs/runtimes.

## Common Tasks

### Create a New Setup Step

```csharp
public class MyCustomStep : ISetupStep
{
    private readonly MyCustomStepOptions _opts;
    
    public string StepId => "my-custom";
    public string StepName => "My Custom Step";
    public IReadOnlyList<string> DependsOn => new[] { "connection-config" };
    
    public bool CanSkip(SetupContext context)
    {
        // Return true if work is already done
        return context?.State?.CompletedStepIds.Contains(StepId) == true;
    }
    
    public IErrorsInfo Validate(SetupContext context)
    {
        if (context?.Editor == null) return Fail("Editor required");
        return Ok();
    }
    
    public IErrorsInfo Execute(SetupContext context, IProgress<PassedArgs> progress = null)
    {
        Report(progress, 10, "Starting…");
        // ... work
        Report(progress, 100, "Done");
        return Ok("Completed successfully");
    }
}
```

### Register a Custom Seeder

```csharp
public class MySeeder : SeederBase
{
    public override string SeederId => "my-seeder";
    public override string SeederName => "My Data Seeder";
    public override IReadOnlyList<string> DependsOn => new[] { "prerequisite-seeder" };
    
    protected override string TargetEntityName => "MyTable";
    
    protected override IErrorsInfo SeedCore(IDataSource ds, IDMEEditor editor, IProgress<PassedArgs> p)
    {
        // Insert data via ds.InsertEntity(...)
        return Ok("Seeding complete");
    }
}

// Register
var registry = new SeederRegistry();
registry.Register(new MySeeder());
registry.Register(new AnotherSeeder());

// Use in wizard
.AddStep(new SeedingStep(new SeedingStepOptions { Registry = registry }))
```

### Debug State Issues

**Problem: Step re-runs on resume**
- Check: Is `CompletedStepIds` being populated? Verify `SyncFromContext` → `SyncToContext` flow.
- Inspect: `setup-checkpoint.json` — verify step ID is recorded.

**Problem: Seeders re-run**
- Check: Is `CompletedSeederIds` being synced? Verify SyncToContext includes this line.
- Inspect: `setup-checkpoint.json` — verify seeder IDs persisted.

**Problem: Metadata (checkpoint token) stale**
- Check: Are you using dictionary indexer `[]` instead of `TryAdd`? Old values get overwritten correctly.
- Verify: `SyncFromContext` uses `[key] = value` and `SyncToContext` also uses indexer.

### Extend ConsoleSetupWizardAdapter for Spectre.Console

```csharp
using Spectre.Console;

public class SpectreSetupWizardAdapter : ConsoleSetupWizardAdapter
{
    public override void ShowStep(ISetupStep step, int stepIndex, int totalSteps)
    {
        AnsiConsole.MarkupLine($"[bold cyan]Step {stepIndex + 1}/{totalSteps}[/]: {step.StepName}");
    }
    
    public override void ShowProgress(string stepId, int percentComplete, string message)
    {
        AnsiConsole.MarkupLine($"[yellow][{percentComplete,3}%][/] {message}");
    }
    
    public override void ShowResult(SetupReport report)
    {
        if (report.Succeeded)
            AnsiConsole.MarkupLine("[green]✓ Setup succeeded[/]");
        else
            AnsiConsole.MarkupLine("[red]✗ Setup failed[/]");
    }
}
```

## 8-Phase Roadmap

| Phase | Scope | Status |
|-------|-------|--------|
| 1 | Contracts, wizard orchestrator | ✓ Complete |
| 2 | DriverProvisionStep, ConnectionConfigStep | ✓ Complete |
| 3 | SchemaSetupStep, dry-run | ✓ Complete |
| 4 | Seeders, SeedingStep, registry | ✓ Complete |
| 5 | All platform adapters (6 types) | ✓ Complete |
| 6 | Preflight validation, rollback | ⊘ Planned |
| 7 | Observability, audit, JSON export | ⊘ Planned |
| 8 | Multi-tenant, CI/CD, orchestration | ⊘ Planned |

## Detailed Reference

Use [`reference.md`](./reference.md) for complete examples and end-to-end scenarios.

## See Also

- [Setup Framework HTML Help](../../Help/setup-framework.html)
- [AssemblyHandler Skills](../shared-context-assemblyhandler/SKILL.md) — Driver loading
- [Migration Manager](../migration/SKILL.md) — Schema application

## Integration with the data-management layer

The Setup Framework is the **first-run orchestrator** for BeepDM apps. It sequences the steps and then hands off to the long-lived layers:

| Direction | Layer | What flows |
|---|---|---|
| → **migration** | `MigrationManager` (via `SchemaSetupStep`) | Phase 4 delegates schema creation. Setup does not embed migration logic. |
| ↔ **configeditor** | `ConfigEditor` façade | `ConnectionConfigStep` reads/writes connections through the façade — never ad-hoc JSON. |
| → **etl** | Pipeline engine | Phase 6 (seeding) may invoke a one-shot pipeline for non-trivial initial loads. |
| → **unitofwork** | `UnitofWork<T>` | Setup guarantees schema + seed; UoW is what runs CRUD against that schema at runtime. |
| → **forms** | `FormsManager` | After setup completes, Forms is the visible UI. Forms uses the same `IDataSource` that setup configured. |

The Mavis cross-project equivalent of this skill lives at `.harness/skills/beepdm-setup/SKILL.md`.
