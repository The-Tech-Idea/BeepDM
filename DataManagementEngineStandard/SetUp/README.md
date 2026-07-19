# SetUp — First-Run Setup Framework

Wizard-based initialization for a BeepDM application: provision drivers → configure a connection →
create schema → set defaults → seed → verify. Contracts live in
`DataManagementModelsStandard/SetUp/`; implementation lives here.

This document describes **what the code does today**, including its gaps. For where it's going, see
[`.plans/setup/MASTER-SETUP-TRACKER.md`](../../.plans/setup/MASTER-SETUP-TRACKER.md).

> **Scope today: one app, one connection, local disk, forward-only.** There is no solution/app
> aggregate, no multi-app support, no identity/RBAC, no remote state, and no rollback execution.
> Don't infer those from the type names — see [Known gaps](#known-gaps).

## Quick start

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

**`SetupOptions` must be the same instance on the wizard and the context.** `SetupWizard.Run` does
`var runOptions = context.Options ?? Options;` — the context wins outright. Set `DryRun` on the
builder but pass a context with a default `SetupOptions` and you get a **silent live run**.
`DefaultSetupWizardFactory` passes one instance to both; nothing in the type system enforces it.

## Contracts

`ISetupStep` (`DataManagementModelsStandard/SetUp/ISetupStep.cs`):

```csharp
string StepId { get; }
string StepName { get; }
string Description { get; }
IReadOnlyList<string> DependsOn { get; }
bool CanSkip(SetupContext context);
IErrorsInfo Validate(SetupContext context);
IErrorsInfo Execute(SetupContext context, IProgress<PassedArgs> progress = null);
Task<IErrorsInfo> ValidateAsync(SetupContext, CancellationToken);   // DIM → wraps Validate
Task<IErrorsInfo> ExecuteAsync(SetupContext, IProgress<PassedArgs>, CancellationToken); // DIM → Task.Run(Execute)
```

There is **no `Order` property and no rollback member**. Ordering is the `List<ISetupStep>`
insertion order; `DependsOn` is a validation constraint, never a sort key — nothing topologically
reorders steps.

`SetupWizard.Run` per step: skip if `State.IsStepCompleted` → `DependsOn` guard → `Validate` →
`CanSkip` → `Execute` → mark complete → persist.

`ExecuteAsync` is never called by the wizard: `RunAsync` wraps the whole **synchronous** `Run` in one
`Task.Run`. Cancellation is therefore only observed *between* steps — a long `ExecuteMigrationPlan`
cannot be interrupted. `ISetupStep.Execute` takes no `CancellationToken`.

## The steps

| Class | `StepId` | `DependsOn` | What it actually does |
|---|---|---|---|
| `DriverProvisionStep` | `driver-provision` | — | Loads a driver from a local package or NuGet via `IAssemblyHandler`; persists driver config. |
| `ConnectionConfigStep` | `connection-config` | `driver-provision` | Matches a driver, normalizes/validates the connection string, writes it through `ConfigEditor`, optionally opens the connection. |
| `SchemaSetupStep` | `schema-setup` | `connection-config` | Drives `MigrationManager` end-to-end: plan → policy → dry-run → preflight → impact → compensation → approve → checkpoint → execute. |
| `DefaultsSetupStep` | `defaults-setup` | `schema-setup` | Adds `DateEntered`/`DateModified`/`CreatedAt`/`UpdatedAt` defaults via `ConfigEditor.Savedefaults`. |
| `SeedingStep` | `seeding` | `schema-setup` | Runs `ISeeder`s from `ISeederRegistry` in topological order. |
| `DataImportStep` | `data-import` | `defaults-setup`, `seeding` | **Imports nothing** — reads entity structures and counts rows. Use `DataImportManager` for real imports. |

The six interfaces in `Steps/IStepInterfaces.cs` (`IDriverProvisionStep`, …) are **empty markers**
used only for DI keying.

## Definitions — a wizard as data

A wizard can be authored, versioned, diffed, and validated as JSON instead of C#. See
`SetUp/Definition/`.

```csharp
var serializer = new JsonSetupDefinitionSerializer();
var factory    = new SetupStepFactory(seederRegistry);   // registry from DI, never from the file

SetupDefinition def = serializer.Deserialize(File.ReadAllText("app-setup.json"));

// CI gate — structural only, no IDMEEditor, no datasource:
IErrorsInfo check = new SetupDefinitionValidator(factory).Validate(def);

ISetupWizard wizard = SetupWizardBuilder.FromDefinition(def, factory).Build();
```

```json
{
  "schemaVersion": 1,
  "id": "northwind-setup",
  "environment": "Development",
  "steps": [
    { "stepId": "driver-provision", "type": "driver-provision",
      "options": { "packageName": "SQLite", "version": "1.0.118" } },
    { "stepId": "connection-config", "type": "connection-config", "dependsOn": ["driver-provision"],
      "options": { "connectionProperties": { "connectionName": "northwind.db", "databaseType": "SqlLite" } } },
    { "stepId": "schema-setup", "type": "schema-setup", "dependsOn": ["connection-config"],
      "options": { "entityTypeNames": ["MyApp.Models.Product"] } }
  ]
}
```

Rules that are load-bearing, not stylistic:

- **`type` is a registry key** (`SetupStepFactory.TypeKeys`), never an assembly-qualified type name.
  Unknown keys are rejected — a definition from a shared store can't instantiate arbitrary types.
- **All JSON goes through `SetupJson.Options`**: camelCase properties, **named** enum values
  (`"SqlLite"`, never `27` — numeric enum values are positional and would silently misbind if the
  enum is reordered). Add a step type or serializer path? Route it through `SetupJson`.
- **`SchemaSetupStepOptions.EntityTypeNames`** (strings, resolved via `IAssemblyHandler`) is the
  portable replacement for the `[Obsolete]` `EntityTypes` (`IReadOnlyList<Type>`). An unresolvable
  name fails loudly — it is never silently dropped.
- **`SeedingStepOptions.Registry` is `[JsonIgnore]`d** and injected from DI by the factory — a live
  object graph is never named in a file.
- **`SchemaVersion`** gates loading: an older document is upgraded, a newer/unknown one is refused
  (not silently reset). Same for `SetupState`'s checkpoint version.

Only `SchemaSetupStep` does real dry-run work — it plans, calls `GenerateDryRunReport`, stores JSON
in `context.Properties["DryRunReportJson"]`, then returns before mutating. Every other step simply
returns `true` from `CanSkip` when `DryRun` is set.

Idempotency is by convention, not contract: `State.IsStepCompleted` short-circuits re-runs, and
`SchemaSetupStep.CanSkip` compares `State.SchemaHash` to a SHA-256 of the entity list.

## Seeding

`ISeeder` implementations are registered **manually** via `SeederRegistry.Register(...)`. Ordering is
a topological sort over `DependsOn`; unknown deps and cycles throw. Idempotency is
`ISeeder.IsAlreadySeeded` plus `SetupState.CompletedSeederIds` for resume.

There is no `[SeederAttribute]`, and seeders are **not** discovered by `[AddinAttribute]`.
`SeederRegistry.DiscoverFromAssemblies(...)` exists but has **zero callers** — nothing auto-registers
seeders.

## Rollback

A failed run can undo its completed steps. Steps opt in via `ISetupStep.SupportsRollback` +
`RollbackAsync` (both default no-op DIMs). `IRollbackOrchestrator` walks completed steps in **reverse**
and is **best-effort** — a step whose rollback fails or throws is recorded, and the rest still run,
because stopping halfway strands more state. Steps that don't support rollback are reported *skipped*,
never as a clean undo that didn't happen.

- `SchemaSetupStep` rolls back via `MigrationManager.RollbackFailedExecution(executionToken)` — it
  undoes what was actually executed (token from `State.Metadata`), not a replayed plan. No token →
  fails loudly.
- `SeedingStep` unseeds seeders implementing `IUndoableSeeder`, in reverse; others are left in place
  and logged.
- `IBackupConfirmationProvider` answers whether a backup is confirmed; the default
  (`NoBackupConfirmationProvider`) returns false and warns, replacing the old inverted
  `backupConfirmed: !strict`.

Rollback is **opt-in** via `SetupOptions.AutoRollbackOnFailure` (undoing a partial setup can destroy
diagnostic state). Its outcome is always written to `SetupReport.RollbackReportJson`.

## Solution — setting up many apps

Provision a whole solution, not just one app (`Solution/`, references `AppMap` read-only).

- **`AppDefinition`** (in `AppMap`) is the solution aggregate — App → Projects → Environments →
  Datasources. Setup added one field: `SetupDefinitionPath` (path to the app's `SetupDefinition` JSON).
- **`ISetupWizardResolver`** builds a per-app wizard from that path, keyed by
  `SetupStateKey(definitionId, environment, appId)` — so apps sharing a state store don't collide
  (the `AppId` slot). `SetupWizardBuilder.WithAppId(...)` threads it in.
- **`ISolutionSetupOrchestrator`** sets up N apps in dependency order (an explicit
  `SolutionSetupOptions.Dependencies` map, topo-sorted — AppMap doesn't model inter-app order). One
  app's failure skips only its dependents; the rest run. `SolutionSetupReport` gives a per-app
  outcome, and `GetStatusAsync` reads persisted state into a NotSetUp/InProgress/Failed/Complete view.

There is deliberately no cross-app transaction, and the status lives on the orchestrator (not on
AppMap's `SolutionSnapshot`) to keep the SetUp → AppMap dependency one-directional.

## Security — identity, RBAC, approvals

Enterprise concepts, each with a **no-op solo default so zero-config setup is unchanged** (`Security/`):

- **`ISetupPrincipal`** — who is running. Default `AnonymousSetupPrincipal` (local OS user,
  `IsAuthenticated == false`). Recorded on `SetupState`/`SetupReport` (`ActorId`,
  `ActorAuthenticated`) — never inferred.
- **`ISetupAuthorizer`** — `AllowAllAuthorizer` (solo) or `RoleBasedSetupAuthorizer` (enterprise,
  fail-closed). The wizard checks `RunSetup` up front and each step's `RequiredPermission` before
  `Execute`; a denial is `Errors.Failed` (never a throw) and the step doesn't run.
- **`ISetupApprovalProvider`** — `AutoApprovalProvider` (solo: grants, records `IsSelfApproved=true`)
  or `SeparationOfDutyApprovalProvider` (enterprise: rejects self-approval, requires a distinct
  approver). `SchemaSetupStep` binds approval to the plan id.

Wire via `SetupWizardBuilder.WithSecurity(principal, authorizer)`; the approval provider and backup
provider are `SchemaSetupStep` ctor args.

## Audit & telemetry

Every run is answerable — who ran what, against which environment, when, with what result (`Audit/`,
`Telemetry/`).

- **`ISetupAuditSink`** — the wizard emits `RunStarted` → per-step `StepStarted`/`StepCompleted`/
  `StepSkipped`/`StepFailed`/`Denied` → `RunCompleted`/`RunFailed` (plus `Rollback*`). Every event
  carries the actor, `ActorAuthenticated`, the definition `ContentHash` ("what was applied"),
  environment, step, and elapsed.
- **Sinks:** `JsonlSetupAuditSink` (solo, **append-only** — each run's record survives),
  `BeepAuditSetupSink` (enterprise, onto the tamper-evident `IBeepAudit` chain), `NullSetupAuditSink`
  (no-op). Default: JSONL under `ReportOutputPath` when set, else no-op. Wire with
  `SetupWizardBuilder.WithAudit(...)`.
- **Auditing never fails the run** — a sink error is logged and swallowed. Enterprise audit-or-abort
  wraps the sink.
- **Telemetry:** `SetupActivitySource` (`TheTechIdea.Beep.SetUp`) emits one span per step, tagged
  wizard/step/environment/definition-hash/actor/outcome. Zero-cost when nothing listens; subscribe via
  an `ActivityListener` or OpenTelemetry `AddSource`.

## Adapters

Six adapters in `Adapters/`, all platform-agnostic by design — none reference WinForms, WPF, MAUI,
SignalR, or JSInterop. All derive from **`SetupWizardAdapterBase`**, which owns the run loop
(progress plumbing, `Task.Run`, uniform cancellation/exception handling, `GetReport()`). An
unexpected exception never escapes `RunAsync` — the partial report is the result.

Override only what differs. Hooks are `Task`-returning so an adapter can await UI marshalling:

| Hook | Purpose |
|---|---|
| `OnRunStartingAsync(wizard, context)` | before the run (resume state, seed status) |
| `ReportProgress(wizard, context, args)` | attribute progress; default resolves the active step |
| `OnCancelledAsync(context)` / `OnFailedAsync(ex, context)` | terminal-state handling |
| `OnCompletedAsync(report, context)` | after the report exists, before `RunAsync` returns |
| `ShowStep` / `ShowProgress` / `ShowResult` | `virtual`; the display surface |

| Adapter | Distinct behavior |
|---|---|
| `ConsoleSetupWizardAdapter` | Renders a `Console.WriteLine` table; prints step 1 on start. |
| `DesktopSetupWizardAdapter` | `Action<PassedArgs>` callbacks + events. Overrides `ReportProgress` to pass the **raw** `PassedArgs` through (the default would drop `Flag`/`ErrorObject`). |
| `WebApiSetupWizardAdapter` | `SetupAdapterStatus` state machine; overrides `RunAsync` to guarantee a non-null report for polling clients. |
| `BlazorServerSetupWizardAdapter` | Routes to `OnProgress`/`OnComplete` extension points. |
| `BlazorWasmSetupWizardAdapter` | Loads state on start; persists on cancel, failure, and completion. |
| `MauiSetupWizardAdapter` | `InvokeOnMainThreadAsync` hook (inline by default). Completion is **awaited**; progress is intentionally fire-and-forget so a UI tick can't block the run. |

## State, reports, first-run

`SetupState`: `SchemaVersion`, `RunId`, `Revision`, `CompletedStepIds`, `SkippedStepIds`,
`FailedStepId`, `SchemaHash`, `CompletedSeederIds`, timestamps, `Metadata`.

State is loaded and saved through **`ISetupStateStore`** (`SetUp/State/`), selected by the wizard:

- **Injected store** (`SetupWizardBuilder.WithStateStore(...)`) wins.
- Else a legacy **`SetupOptions.StateFilePath`** maps to `LocalJsonSetupStateStore.ForExplicitFile(...)`.
- Else **no store** — checkpointing is disabled (and warned), matching the historical no-path behaviour.

`LocalJsonSetupStateStore` (solo default) writes one JSON file per key at
`{root}/setup/{appId|_}/{environment}/{wizardId}.state.json` via temp + `File.Move(overwrite)` with
retries, plus a sibling `.lock` file for a **cross-process** lease. `RemoteSetupStateStore`
(enterprise) puts state behind an ETag-versioned `ISetupStateTransport` with optimistic concurrency —
a stale save throws `SetupStateConflictException`.

**Concurrency:** `SetupWizard.Run` acquires an exclusive lease (`TryAcquireLeaseAsync`, 30-min TTL)
before the first step and **refuses to run** if another runner holds it; the lease is released in a
`finally`. An expired lease is reclaimable, so a crashed run doesn't wedge the key forever. The key is
`SetupStateKey(wizardId, environment, appId)`, so two wizards on one store no longer collide.

`SchemaVersion` gates loading: an older document is accepted, a newer/unknown one is refused (never
silently reset — a re-run against a live DB is a re-migration, not a reset).

`FileBasedFirstRunDetector` decides first-run purely by existence of a `.setup_complete` marker under
`ConfigEditor.ConfigPath`. The file's content is a timestamp that nothing ever reads. On any
exception `IsFirstRunAsync` returns `true` (fails open → re-runs setup).

`BeepBootstrapper` composes first-run detection + wizard + adapter. It does **not** reference
`BeepService`; it takes a `Func<IDMEEditor>` accessor so resolution is deferred until a run is
actually detected.

On a **non-first run** it can now run a **version-gate upgrade pass** (Phase 9) instead of returning
immediately: pass the optional `upgradeWizardFactory` delegate (e.g.
`e => factory.CreateUpgrade(e, options, gate)`), which builds a `"standard-upgrade"` wizard — its own
state key, so its state starts empty each launch and the wizard's skip-completed-steps guard can't
suppress the gate. The gate compares the declared schema version and the entity model against the
version recorded **in the target database** (`__BeepSchemaVersion`, via `DbSchemaVersionStore`) and
applies pending migrations, surfacing movement on `BootstrapResult.MigratedFrom`/`MigratedTo` under
`BootstrapPhase.VersionCheck`. Omit the delegate and the historical first-run-only behaviour is
unchanged. See [`.plans/setup/PHASE-09`](../../.plans/setup/PHASE-09-Versioned-Migrate-On-Startup.md).

## DI

```csharp
services.AddSetupWizard();       // registers one ISetupWizard as a SINGLETON
services.AddBeepBootstrapper();  // resolves IDMEEditor from the container
```

`AddSetupWizard()` builds from `DefaultSetupWizardFactory.CreateDefault(editor)` — see the bugs
below before relying on it.

## Known gaps

Verified against the code. Load-bearing ones first.

**Dead / unwired API**

- `SetupState.RunId` is documented as the stale/concurrent-checkpoint detector, but **nothing ever
  compares RunIds**. (Phase 3.)
- `SeederRegistry.DiscoverFromAssemblies` — zero callers; nothing auto-registers seeders.

**Recently fixed** (Phase 1 — if you see these described elsewhere, that doc is stale):

- `DefaultSetupWizardFactory` no longer throws with 2+ AutoLoad drivers. Driver step ids are
  qualified per package (`driver-provision:SQLite`) **by the factory** when it emits more than one;
  a single driver keeps the bare `driver-provision` id.
- The factory takes an optional `ISeederRegistry` and omits `SeedingStep` when there isn't one,
  instead of shipping a step whose `Validate` always failed.
- `DefaultsSetupStep` now writes rule-based (`UTCNOW`) audit defaults rather than a literal captured
  at setup time. `DateTimeResolver` gained `UTCNOW`/`UTCTODAY`/`CURRENTUTCDATETIME`.
- `ReferenceDataSeederBase<T>` wraps its batch in a transaction where supported and uses count-based
  idempotency, so a partial failure can no longer masquerade as "already seeded".
- `DriverProvisionStep` verifies the driver class exists in the loaded assemblies before clearing
  `IsMissing`, instead of asserting success.
- Step order is enforced at `Build()` (throws), matching the XML doc and the wizard's `Run()` check.
  Duplicate step ids are reported by name.
- `SetupReport.DryRunReportJson`/`.RollbackReportJson` are populated, and `ReportOutputPath` is
  honoured (timestamped, never overwritten).

**Structural**

- **No CLI / unattended mode.** (Phase 8.)
- **Environment naming is still 4-way ambiguous** (`SetupOptions.Environment` label vs `AppMap.AppEnv`
  vs `Studio.EnvironmentProfile` vs the folder-helper `Services/EnvironmentService`). A dedicated
  cleanup was deferred out of Phase 7 — it's ~40 call sites and orthogonal to orchestration.
