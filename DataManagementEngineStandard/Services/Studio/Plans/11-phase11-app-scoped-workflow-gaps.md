# Phase 11 — App-Scoped Workflow Gaps

> **Scope:** close the gaps that surfaced when the WPF AppStudio was re-pointed from the flat
> Studio services (`IMigrationStudioService`, `IGovernanceService`) onto the App-scoped workflow
> tree (`IAppStudioService.Apps.*`). Every item here is **engine/contract work in BeepDM**. The
> WPF host is already done and needs no further change except where noted.

> Legend: `[ ]` open · `[~]` in-progress · `[x]` done · `[!]` blocked

---

## Why this phase

The App-scoped tree (`Studio/Apps/Workflows/`) is the App → Environment → Datasource model and is
now what the host binds to. Re-pointing exposed four kinds of gap:

1. **Capability the flat tree has and the App tree does not** — migration *planning*
   (BuildPlan/Preflight) and *policy authoring*. One consumer (`PromotionPipeline`) is still
   stranded on the flat tree because of it.
2. **A workflow that reports success it did not achieve** — `RunSoloDevAsync` "seeds" by checking
   a path exists.
3. **A catalog that promises schema it does not ship** — every QuickStart template has an empty
   `EntityTypeNames`, so `SchemaApplied` can never be true.
4. **A contract whose doc and implementation disagree** — `SeedAsync` documents folders/CSV/
   assemblies and implements a single JSON file.

Items 1 and 2 are the blockers. Items 3–6 are correctness bugs of decreasing blast radius.

---

## P11-A — App-scoped migration planning (unblocks PromotionPipeline)

**Priority: high.** This is the only item with a stranded consumer.

### The gap

The flat service exposes a plan-handle lifecycle:

```csharp
// Studio/Migration/IMigrationStudioService.cs
Task<StudioResult<MigrationPlanHandle>>      BuildPlanAsync(MigrationRequest, IStudioProgress?, ct);   // :21
Task<StudioResult<MigrationDryRunReport>>    DryRunAsync(MigrationPlanHandle, ct);                     // :24
Task<StudioResult<MigrationPreflightReport>> PreflightAsync(MigrationPlanHandle, ct);                  // :25
Task<StudioResult<MigrationImpactReport>>    ImpactAsync(MigrationPlanHandle, ct);                     // :26
Task<StudioResult<MigrationExecutionHandle>> ApplyAsync(MigrationPlanHandle, MigrationExecutionPolicy, IStudioProgress?, ct); // :31
Task<StudioResult<MigrationRollbackReport>>  RollbackAsync(string executionToken, RollbackPolicy, IStudioProgress?, ct);      // :34
```

The App-scoped workflow has **no plan concept at all**. `AppMigrationWorkflow.MigrateAsync` and
`DryRunAsync` both funnel into one `RunMigrationAsync(appId, envId, dryRun, options, ct)`
(`AppMigrationWorkflow.cs:27-31`), and `RollbackAsync` returns a bare `bool` (`:33-44`).

So an App-scoped caller cannot: build a plan once and inspect it, run preflight checks, see
per-operation risk/DDL, apply a *previously reviewed* plan, or roll back by execution token.

### Stranded consumer

`PromotionPipeline` is a staged review gate — build → dry-run → preflight → apply → rollback per
stage — which is exactly the plan-handle lifecycle. It still calls the flat tree:

- `Beep.WPF/Beep.AppStudio/ViewModels/PromotionPipelineViewModel.cs:99,125,133`
- `Beep.WPF/Beep.AppStudio/Views/PromotionPipelineView.xaml.cs:97,121,147,172,195`

It is the **last** flat-tree consumer in AppStudio. Until this lands, the flat tree cannot be
marked `[Obsolete]` and the deprecation is incomplete.

### Work

- [ ] P11-A-01 Add `EnvMigrationPlan` to `Studio/Apps/Workflows/IAppMigrationWorkflow.cs` — an
      app×env-scoped plan handle. Carries `PlanId`, `AppId`, `EnvId`, `DatasourceName`, `BuiltAt`,
      and `IReadOnlyList<EnvMigrationOperation>` (`EntityName`, `Operation`, `RiskLevel`,
      `DdlPreview`). Mirror `MigrationPlanHandle`'s shape so the concepts stay recognisable.
- [ ] P11-A-02 Add `EnvPreflightReport` (`Checks[]` of `Name`/`Status`/`Message`) and
      `EnvExecutionHandle` (`ExecutionToken`, `AppId`, `EnvId`, `AppliedAt`).
- [ ] P11-A-03 Extend `IAppMigrationWorkflow`:
      ```csharp
      Task<StudioResult<EnvMigrationPlan>>    BuildPlanAsync(string appId, string envId, MigrationOptions? options = null, IStudioProgress? progress = null, CancellationToken ct = default);
      Task<StudioResult<EnvMigrationReport>>  DryRunPlanAsync(EnvMigrationPlan plan, CancellationToken ct = default);
      Task<StudioResult<EnvPreflightReport>>  PreflightAsync(EnvMigrationPlan plan, CancellationToken ct = default);
      Task<StudioResult<EnvExecutionHandle>>  ApplyPlanAsync(EnvMigrationPlan plan, MigrationExecutionPolicy policy, IStudioProgress? progress = null, CancellationToken ct = default);
      Task<StudioResult<bool>>                RollbackExecutionAsync(string executionToken, CancellationToken ct = default);
      ```
      Keep the existing `MigrateAsync`/`DryRunAsync` one-shot methods — MigrationStudioView uses
      them and does not want a plan handle.
- [ ] P11-A-04 Implement in `AppMigrationWorkflow`. It already resolves app×env → datasource
      (`ResolveDatasource`) and entity types (`ResolveEntityTypes`); reuse both and delegate to the
      same engine the flat `MigrationStudioService` wraps rather than writing a second engine path.
- [ ] P11-A-05 **Thread `IStudioProgress` through.** The App-scoped methods currently accept a
      `CancellationToken` but no progress, which is why MigrationStudioView can only show an
      indeterminate bar. Plan/apply are the long operations; they need real progress.
- [ ] P11-A-06 Re-point `PromotionPipelineViewModel` + `PromotionPipelineView` onto
      `Apps.Migrations`. Drop `using TheTechIdea.Beep.Studio.Migration` from both —
      `MigrationOptions` exists in both trees and the ambiguity is a compile error (CS0104).
- [ ] P11-A-07 Migrate `PromotionPipelineViewModel` onto `StudioViewModelBase` so its failures
      reach the output pane like every other view.
- [ ] P11-A-08 Mark the flat `IStudioService.Migrations` `[Obsolete]` once no consumer remains.

**Verification:** open Promotion Pipeline, run a stage end-to-end against a dev env, confirm the
plan/preflight/apply/rollback buttons each act and the progress bar advances by percentage rather
than spinning.

---

## P11-B — App-scoped governance policy (restores configurable approvals)

**Priority: high.** This is a capability regression that was accepted knowingly; this item pays it back.

### The gap

`AppGovernanceWorkflow` hardcodes the approval count:

```csharp
// AppGovernanceWorkflow.cs:59
var required = env?.RequiresApproval == true || env?.IsProduction == true ? 2 : 1;
```

and `EvaluateAsync` (`:86-101`) derives everything from `AppEnv.RequiresApproval` /
`IsProduction` plus its own member and ticket lists. There is no policy store, so none of these
are expressible:

| Flat `GovernancePolicy` field | App-scoped equivalent |
|---|---|
| `RequiredApproverCount` | hardcoded `2` |
| `RequireApprover` | inferred from env flags |
| `AllowedApproverRoles` | — |
| `BlockedOperations` | — |
| `CooldownBetweenRuns` | — |
| `RequireDryRunOnApply` | — |
| `RequirePreflightOnApply` | — |
| `MaxRowsAffectedPerRun` | — |

The flat policy CRUD (`IGovernanceService.cs:21-25`) is now orphaned: the WPF Policy tab was
rewritten as an *evaluate* surface because the App tree only evaluates.

### Work

- [ ] P11-B-01 Add `AppGovernancePolicy` to `Studio/Apps/Workflows/IAppGovernanceWorkflow.cs`,
      scoped to app + env **tier** (not env id — a policy should cover "production" generally):
      `PolicyId`, `AppId`, `Tier`, `RequireApprover`, `RequiredApproverCount`,
      `AllowedApproverRoles`, `BlockedOperations`, `CooldownBetweenRuns`, `RequireDryRunOnApply`,
      `MaxRowsAffectedPerRun`, `CreatedAt`, `UpdatedAt`.
- [ ] P11-B-02 Extend `IAppGovernanceWorkflow`:
      ```csharp
      Task<StudioResult<IReadOnlyList<AppGovernancePolicy>>> ListPoliciesAsync(string appId, CancellationToken ct = default);
      Task<StudioResult<AppGovernancePolicy>>                UpsertPolicyAsync(string appId, AppGovernancePolicy policy, CancellationToken ct = default);
      Task<StudioResult<bool>>                               DeletePolicyAsync(string appId, string policyId, CancellationToken ct = default);
      ```
- [ ] P11-B-03 Add `List<AppGovernancePolicy> Policies` to the JSON store record
      (`AppGovernanceWorkflow.cs:117`). The store already persists to
      `%LocalAppData%/BeepDM/Studio/governance/{appId}.json` and is versionless — adding a
      property is backward compatible (absent → empty list).
- [ ] P11-B-04 **Make `RequestApprovalAsync` read the policy** instead of the hardcoded `2`
      (`:59`). Fall back to the current env-flag behaviour only when no policy matches the tier,
      so existing apps keep working.
- [ ] P11-B-05 **Make `EvaluateAsync` consult the policy** (`:86-101`): deny when the action is in
      `BlockedOperations`, when the decider's role is not in `AllowedApproverRoles`, or when
      `CooldownBetweenRuns` has not elapsed since the last matching audit entry.
- [ ] P11-B-06 Restore a policy-authoring tab in `Beep.WPF/Beep.AppStudio/Views/GovernanceView.xaml`
      (it currently has Members | Approvals | Policy | Audit, where Policy is evaluate-only).
      Add the CRUD grid alongside the evaluator; gate it on `StudioModeConfig.CanManageUsers`.
- [ ] P11-B-07 Decide the fate of `VerifyAuditIntegrityAsync` — the flat tree has it
      (`IGovernanceService.cs:36`), the App tree does not, and the UI button was removed. The App
      store is a plain JSON file with **no hash chain**, so an App-scoped integrity check is
      meaningless until the store is chained. Either chain the store (real work) or record
      explicitly that App-scoped audit is not tamper-evident.

**Verification:** author a policy requiring 3 approvers on `production`, request approval on a prod
env, confirm the ticket reports `RequiredApprovals = 3` and stays `Open` until the third decision.

---

## P11-C — `RunSoloDevAsync` does not seed

**Priority: medium.** Small fix, but the workflow currently reports a success it never performed.

### The bug

```csharp
// ScenarioWorkflow.cs:66-71
bool seeded = false;
if (request.Seed && !string.IsNullOrWhiteSpace(request.SeedSource))
{
    var seedOk = System.IO.File.Exists(request.SeedSource) || System.IO.Directory.Exists(request.SeedSource);
    seeded = seedOk;              // <-- reports success for a path that merely exists
}
```

`SoloDevResult.Seeded` is therefore true whenever the path exists, and no rows are ever inserted.

The fix is small because **`AppQuickStartWorkflow.SeedAsync` is a real implementation**
(`AppQuickStartWorkflow.cs:99-134`): it resolves the env's primary datasource, parses the JSON, and
calls `ds.InsertEntity` per row.

### Work

- [ ] P11-C-01 Replace the existence check with a real call:
      ```csharp
      var seedRes = await new AppQuickStartWorkflow(_editor)
          .SeedAsync(app.Id, baselineName, request.SeedSource, ct);
      seeded = seedRes.IsSuccess;
      ```
      `RunEnterpriseAsync` composes sibling workflows the same way already (`:124-126`), so this
      matches the file's own pattern.
- [ ] P11-C-02 Surface the seed failure rather than swallowing it — `SoloDevResult.Message` should
      say why seeding failed, not just report `Seeded = false`.
- [ ] P11-C-03 **Verify `SeedAsync` actually inserts.** `:127` does
      `JsonSerializer.Deserialize<object>(row.GetRawText())`, which yields a boxed `JsonElement`,
      and passes it to `ds.InsertEntity(entityName, obj)`. If `InsertEntity` expects a POCO or
      `DataRow`, this inserts nothing while still returning `Ok`. Confirm against a real SQLite
      datasource before closing P11-C — this would be the same class of bug one layer down.

**Verification:** run Solo Dev with a seed file, then query the datasource and confirm the rows are
present. `Seeded = true` alone is not evidence.

---

## P11-D — QuickStart templates ship no entities, so schema is never applied

**Priority: medium.** The headline promise of QuickStart currently cannot fire.

### The bug

The template catalog sets `EntityTypeNames` on **no** template:

```csharp
// AppQuickStartWorkflow.cs:26-32
new() { Id = "blank",        Name = "Blank",          Description = "No entities. Add your own." },
new() { Id = "web",          Name = "Web App",        DefaultDatasourceType = "SqlLite",  Description = "Typical web app data model." },
new() { Id = "microservice", Name = "Microservice",   DefaultDatasourceType = "SqlLite",  Description = "Service-bound schema." },
new() { Id = "warehouse",    Name = "Data Warehouse", DefaultDatasourceType = "SqlServer", Description = "Analytical schema." },
```

`AppTemplate.EntityTypeNames` defaults to an empty list (`IAppQuickStartWorkflow.cs:38`), and
`StartAsync` gates the migration on it:

```csharp
// AppQuickStartWorkflow.cs:71-75
if (template is { EntityTypeNames.Count: > 0 })   // never true for any built-in template
{
    var res = await migration.MigrateAsync(...);
    applied = res.IsSuccess;
}
```

So `QuickStartResult.SchemaApplied` is **always false** and the message is always
"App created. Bind entities to apply schema." — for `web`, `microservice`, and `warehouse`, whose
descriptions all promise a schema. `blank` is the only honest one.

### Work

- [ ] P11-D-01 Decide what the three non-blank templates should ship. Either populate
      `EntityTypeNames` with real assembly-qualified entity types, or delete the templates and keep
      only `blank` until there is a schema to ship. Shipping a "Data Warehouse — analytical schema"
      that creates nothing is worse than not offering it.
- [ ] P11-D-02 If templates stay, `AppTemplate.IsBlittableOnLocal` (`:39`) is declared and never
      read — either honour it (block SqlServer-only templates on a local SQLite default) or remove it.
- [ ] P11-D-03 Make the message honest when `applied` is false but the template *claimed* entities —
      that combination is a template-catalog bug, not a normal outcome.

**Verification:** run Quick Start with the `web` template against SQLite and confirm the tables
exist in the file. `SchemaApplied = true` alone is not evidence.

---

## P11-E — `SeedAsync` contract and implementation disagree

**Priority: low.** Documentation/behaviour mismatch, no data loss.

The contract:

```csharp
/// <summary>Seed an app's baseline env from a seed source (JSON/CSV folder or a seed assembly).</summary>
Task<StudioResult<bool>> SeedAsync(string appId, string envId, string seedSource, CancellationToken ct = default);
// IAppQuickStartWorkflow.cs:26-27
```

The implementation accepts **a single JSON file only**: `File.Exists(seedSource)` rejects folders
(`AppQuickStartWorkflow.cs:103`) and `JsonDocument.Parse` handles neither CSV nor an assembly
(`:119`).

This matters because `SoloDevRequest.SeedSource` and the WPF seed box both advertise the documented
behaviour, so a user pointing at a folder gets a `NotFound`.

- [ ] P11-E-01 Either widen the implementation (folder → every `*.json`/`*.csv` inside; assembly →
      reflect a seeder type) or narrow the doc to "a JSON file". Narrowing is the honest cheap fix;
      widening is the useful one.
- [ ] P11-E-02 Whichever way it goes, make the WPF tooltips match
      (`QuickStartView.xaml` `SeedSourceBox`, `ScenarioView.xaml` `SoloSeedSourceBox`).

---

## P11-F — Deprecation cleanup (after P11-A and P11-B)

- [ ] P11-F-01 Mark `IStudioService.Migrations` and `IStudioService.Governance` `[Obsolete]` with a
      message pointing at `Apps.Migrations` / `Apps.Governance`.
- [ ] P11-F-02 Delete the flat `GovernancePolicy` CRUD path once P11-B replaces it, or keep it and
      document that the two policy stores are independent — **do not leave both live and unexplained**.
      They persist to different places and nothing syncs them.
- [ ] P11-F-03 Re-check other hosts before removing anything: `BeepWeb/Beep.Razor.Components` and
      `Beep.Desktop` may still bind the flat tree. WPF AppStudio is clean as of Phase 11 except for
      PromotionPipeline (P11-A-06).

---

## Non-goals

- The ETL `WorkFlowEngine` (`TheTechIdea.Beep.Workflow`) is **out of scope**. It is a separate,
  fully-built step-graph engine (`WorkFlowDefinition` / `StepConnection` / `WorkFlowEngine` /
  `WorkFlowStorage`) with no WPF surface and no relationship to `Studio.Apps.Workflows` beyond the
  shared word. `IWorkFlowEditor` has no implementation and `DMEEditor.WorkFlowEditor` is always
  null; if a designer is wanted, that is its own phase.
- No changes to `Studio/Apps/Workflows/IAppDataWorkflow`, `IAppCloudWorkflow`, `IAppDeployWorkflow`,
  or `IAppCicdWorkflow` — all fully surfaced and behaving.

---

## Suggested order

1. **P11-C** (seed) — smallest, and it is an active correctness lie.
2. **P11-D** (templates) — same class, same file, cheap while in there.
3. **P11-A** (planning) — unblocks PromotionPipeline, the only stranded consumer.
4. **P11-B** (policy) — restores the accepted regression.
5. **P11-E**, **P11-F** — cleanup once the above settle.

> Phase 11 status: not started. Contracts unchanged, WPF host complete except P11-A-06/07.
