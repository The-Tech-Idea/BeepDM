# Phase 7 — Solution Aggregate & Multi-App

**Goal:** The "single simple place to manage a solution and its parts." One solution → N apps → each
app's environments, datasources, schema, and setup state, manageable from one surface.

**Pre-condition:** Phase 2 (definitions are data — N apps means N definitions) and Phase 3
(`SetupStateKey` already carries `AppId`).

**Files touched:** `Services/AppMap/`, `DataManagementEngineStandard/SetUp/`,
`DataManagementModelsStandard/`

---

## ✅ Status: complete (orchestrator scope)

Items P7-01/02/04/05/06 landed; P7-03 partial and the Environment rename deferred (per-item summary
in the master tracker). 206/206 tests green; `SolutionSetupTests.cs` (7) covers it.

**The decision (P7-01), on evidence:** a sub-agent mapped AppMap. `AppDefinition` is already a full
solution model (App → Projects → Environments → Datasources) with `IAppRegistry`; AppMap and SetUp
are fully decoupled; AppMap has no setup concept. So AppMap owns the aggregate, and SetUp added a
solution orchestrator that references AppMap **read-only**, plus one additive field
(`AppDefinition.SetupDefinitionPath`). The direction of dependency is SetUp → AppMap, never the
reverse — which is why P7-05's status lives on the orchestrator, not on AppMap's `SolutionSnapshot`.

**Two honest limits, not shortcuts:**

- **Inter-app order is supplied, not derived.** AppMap's `ProjectDependencyGraph` is project-level and
  has no topological sort; apps have no inter-app dependency field. So `SolutionSetupOptions.Dependencies`
  is an explicit map, topo-sorted by the orchestrator. Pretending to derive an order the data doesn't
  contain would be worse.
- **No cross-app transaction.** One app's failure skips only its dependents; the rest proceed. The
  report says which apps need attention. A distributed rollback across apps is explicitly out of scope.

**Deferred** (user chose orchestrator-only scope): the 4-way `Environment` naming collision and the
`Services/EnvironmentService` → `BeepFolderService` rename — ~40 call sites, on `BeepService`'s
startup path, and the class also seeds configs so the new name would misfit. Its own change if pursued.

---

## Read this first: most of this already exists

**Do not write a solution model until 7-A is decided.** `Services/AppMap/` already has:

| Type | Location | What it holds |
|---|---|---|
| `AppDefinition` | `Models/AppMap/AppDefinition.cs` | `Id`, `Name`, `SolutionPath`, `Projects`, `Environments`, `EntityCount`, `ModuleNames`, `Baseline` |
| `AppProject` | same | per-project, incl. `IsDataProject` |
| `AppEnv` | same | per-app environment |
| `SolutionInfo` | `Models/AppMap/SolutionInfo.cs` | `.sln` discovery: `Name`, `SlnPath`, `Projects`, `Configurations` |
| `ProjectDependencyGraph` | `Models/AppMap/` | project dependency edges |
| `ISolutionControlService` | `Services/AppMap/ControlPanel/` | `GetSnapshotAsync`, `HealthCheckAllAsync`, `SwitchEnvironmentAsync`, `GetDependencyMapAsync` |
| `IAppRegistry`, `IAppMapService`, `IEnvironmentManagementService`, `IIdentityManagementService`, `IVersionManagementService`, `IMultiProjectSyncService`, `IAppRelationshipService` | `Services/AppMap/` | registry, envs, identity, versions, cross-project sync |

All are already exposed on `DMEEditor.Services`. **This phase should extend AppMap, not clone it.**
Building a parallel `SetupSolution` model would create a fourth overlapping subsystem — the exact
problem this plan exists to end.

## 7-A  Decision: who owns the aggregate

**Recommendation:** **AppMap owns the aggregate. SetUp owns per-app provisioning.**

| Layer | Owns |
|---|---|
| `Services/AppMap/` | the solution: apps, projects, environments, dependency graph, health, snapshots |
| `SetUp/` | provisioning **one app** into **one environment** (a lifecycle operation AppMap invokes) |
| `Services/Studio/` | data-lifecycle ops (migration, sync, governance) on an app's datasources |

Rationale: AppMap's `AppDefinition` already *is* "a solution and its parts", and
`ISolutionControlService` already *is* a control panel. SetUp is a step in an app's life, not the
place a solution lives. Inverting this (SetUp growing a solution model) means reimplementing
discovery, environments, identity, and health that already work.

**This decision must be made explicitly before any code.** If it lands the other way, 7-B..7-E change
shape entirely.

## 7-B  Resolve the `Environment` collision *(prerequisite for everything else)*

**Four unrelated things are called "Environment" today:**

| Symbol | Location | Means |
|---|---|---|
| `SetupOptions.Environment` | `SetUp/` | free-form `string`, default `"Development"` — a **label**; nothing resolves config from it |
| `AppEnv` / `IEnvironmentManagementService` | `Services/AppMap/` | a real per-app environment with a baseline |
| `EnvironmentProfile` / `IEnvironmentProfileService` | `Services/Studio/` | Studio's environment profile |
| `EnvironmentService` | `Services/` | **unrelated** — creates data folders on disk |

Plus `System.Environment` shadowing inside `SetUp` (see P5-A).

**Proposal:**

- `AppEnv` (AppMap) is **the** environment. One concept, one owner.
- Studio's `EnvironmentProfile` becomes a projection of `AppEnv`, or is deleted if redundant.
- `SetupOptions.Environment` (string) is `[Obsolete]`; add `EnvironmentId` referencing an `AppEnv`.
  Keep the string working — shipped contract.
- Rename `Services/EnvironmentService` → **`BeepFolderService`**. It creates folders; the name is
  pure collision. *(Breaking: it's public and used by `BeepService.LoadConfigurations`. Type-forward
  + `[Obsolete]`, remove next major.)*

This is the concrete answer to "are these naming confusion?" — for `Environment`, **yes**, and this
is the fix. For AppMap/Studio/SetUp as wholes, no: they're three real layers (7-A), just poorly
signposted.

## 7-C  Setup per app

DI registers **one singleton `ISetupWizard`** today (`SetupWizardServiceExtensions.cs:43`) built from
one `CreateDefault(editor)` — structurally single-app.

```csharp
public interface ISetupWizardResolver
{
    Task<ISetupWizard> ResolveAsync(string appId, string environmentId, CancellationToken token = default);
}
```

Resolves the app's `SetupDefinition` (P2) → `SetupWizardBuilder.FromDefinition` → wizard keyed by
`SetupStateKey(wizardId, environmentId, appId)` (P3 already carries `AppId`). The singleton
registration becomes `[Obsolete]`.

Where a definition lives: `AppDefinition.SetupDefinitionPath` (new, relative to `SolutionPath`), so
it sits in the repo next to the app and versions with it.

## 7-D  Solution-level orchestration

```csharp
public interface ISolutionSetupOrchestrator
{
    Task<SolutionSetupReport> SetupSolutionAsync(AppDefinition[] apps, string environmentId,
                                                 IProgress<PassedArgs> progress = null,
                                                 CancellationToken token = default);
}
```

- Order apps by `ProjectDependencyGraph` / `IAppRelationshipService` — reuse, don't re-derive.
- **One app's failure must not abort the others** unless they depend on it. Report per-app.
- Per-app rollback via P4; **no cross-app distributed transaction** — explicitly a non-goal. Report
  honestly which apps succeeded and which need attention.

## 7-E  Solution status

Extend `ISolutionControlService.GetSnapshotAsync` with setup state per app (`NotSetUp` / `InProgress`
/ `Complete` / `Failed`), read via `ISetupStateStore` (P3). Reuse `HealthCheckAllAsync` for
datasource health. This is the "single place" view — assembled from existing parts.

## 7-F  Tests

| Test | Guards |
|---|---|
| `TwoAppSolution_OneFails_OthersUnaffected` | 7-D |
| `Apps_SetUp_InDependencyOrder` | 7-D |
| `DependentApp_Skipped_WhenDependencyFails` | 7-D |
| `StateKey_Isolates_AppsAndEnvironments` | 7-C |
| `Snapshot_Reports_SetupState_PerApp` | 7-E |
| `LegacyStringEnvironment_StillResolves` | 7-B back-compat |

## Files summary

| Action | File | Est. |
|---|---|---|
| Modify | `Models/AppMap/AppDefinition.cs` (+`SetupDefinitionPath`) | ~6 |
| Modify | `Models/SetUp/SetupOptions.cs` (+`EnvironmentId`, obsolete string) | ~8 |
| New | `Models/SetUp/ISetupWizardResolver.cs` | ~15 |
| New | `Models/AppMap/ISolutionSetupOrchestrator.cs` + report | ~60 |
| New | `Engine/SetUp/SetupWizardResolver.cs` | ~90 |
| New | `Engine/Services/AppMap/SolutionSetupOrchestrator.cs` | ~170 |
| Modify | `Engine/Services/AppMap/ControlPanel/SolutionControlService.cs` | ~50 |
| Rename | `Services/EnvironmentService.cs` → `BeepFolderService.cs` (+forward) | ~20 |
| Modify | `Engine/SetUp/SetupWizardServiceExtensions.cs` | ~40 |
| New | `tests/SetupWizardTests/SolutionSetupTests.cs` | ~250 |

---

## Open question for 7-A

Studio's plan folder was deleted as stale, and its scope freeze ("**not** project scaffolding, **not**
deployment orchestration") is retired. Studio's *code* ships and compiles. So: does Studio remain the
data-lifecycle layer under AppMap (the 7-A recommendation), or does its facade become the single
entry point that absorbs AppMap?

The recommendation assumes the former — AppMap already owns the solution aggregate and the control
panel, and Studio's own services are datasource-scoped. **Confirm before 7-A ships.**
