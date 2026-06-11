# Phase 06 — Migration Orchestration (`IMigrationStudioService`)

> **Scope:** implement `IMigrationStudioService` — the Studio's migration
> orchestration sub-service. Wraps the engine's `IMigrationManager` with a
> view-model surface, a `StudioResult<T>`-shaped API, an `IStudioProgress`
> reporter, and a `MigrationExecutionHandle` for resumable / cancellable
> runs. Also adds plan-diff, dry-run, preflight, impact, performance, and
> CI-validation helpers as first-class methods on the same service.

> Legend: `[ ]` open · `[~]` in-progress · `[x]` done · `[!]` blocked

---

## Why this phase

The engine's `IMigrationManager` is the right thing, but its surface is large
and uses `IErrorsInfo` (the engine's own error contract) plus raw
`IProgress<PassedArgs>` (the engine's progress type). For a Studio host
(Blazor, WinForms, WPF, Maui) we want:

- A `StudioResult<T>`-shaped API.
- A `MigrationPlanVm` view-model (not a raw `MigrationPlanArtifact`).
- A handle object (`MigrationExecutionHandle`) that can be passed around
  for resume / cancel / rollback.
- An `IStudioProgress` reporter (not `IProgress<PassedArgs>`).
- Built-in caching of plans by `(srcDs, dstDs, namespace, planHash)`.
- A first-class `ValidateForCiAsync` method that returns a `CiValidationReport`
  the host's CI integration can use as a pass/fail gate.

## Public surface (this phase fills in)

```csharp
// Contracts/IMigrationStudioService.cs
public interface IMigrationStudioService
{
    // ---- plan building ----
    Task<StudioResult<MigrationPlanVm>> BuildPlanAsync(MigrationRequest request, IStudioProgress? progress = null, CancellationToken ct = default);
    Task<StudioResult<MigrationPlanVm>> BuildPlanForTypesAsync(MigrationRequest request, IReadOnlyList<Type> entityTypes, IStudioProgress? progress = null, CancellationToken ct = default);
    Task<StudioResult<MigrationPlanVm>> BuildPlanForModelAsync(MigrationRequest request, object model, IStudioProgress? progress = null, CancellationToken ct = default);

    // ---- plan analysis ----
    Task<StudioResult<MigrationDryRunReport>> DryRunAsync(MigrationPlanHandle planHandle, CancellationToken ct = default);
    Task<StudioResult<MigrationPreflightReport>> PreflightAsync(MigrationPlanHandle planHandle, CancellationToken ct = default);
    Task<StudioResult<MigrationImpactReport>> ImpactAsync(MigrationPlanHandle planHandle, CancellationToken ct = default);
    Task<StudioResult<MigrationPerformancePlan>> PerformanceAsync(MigrationPlanHandle planHandle, CancellationToken ct = default);
    Task<StudioResult<CiValidationReport>> ValidateForCiAsync(MigrationPlanHandle planHandle, CancellationToken ct = default);
    Task<StudioResult<PolicyEvaluationResult>> EvaluatePolicyAsync(MigrationPlanHandle planHandle, CancellationToken ct = default);
    Task<StudioResult<BestPracticesVm>> GetBestPracticesAsync(string targetSourceName, CancellationToken ct = default);

    // ---- execution ----
    Task<StudioResult<MigrationExecutionHandle>> ApplyAsync(MigrationPlanHandle planHandle, MigrationExecutionPolicy policy, IStudioProgress? progress = null, CancellationToken ct = default);
    Task<StudioResult<MigrationExecutionHandle>> ResumeAsync(string executionToken, MigrationExecutionPolicy? policy = null, IStudioProgress? progress = null, CancellationToken ct = default);
    Task<StudioResult<bool>> CancelAsync(string executionToken, CancellationToken ct = default);
    Task<StudioResult<MigrationRollbackReport>> RollbackAsync(string executionToken, RollbackPolicy policy, IStudioProgress? progress = null, CancellationToken ct = default);

    // ---- history & state ----
    Task<StudioResult<IReadOnlyList<MigrationHistoryItem>>> GetHistoryAsync(string? sourceName = null, int skip = 0, int take = 100, CancellationToken ct = default);
    Task<StudioResult<MigrationExecutionState>> GetExecutionStateAsync(string executionToken, CancellationToken ct = default);
    Task<StudioResult<MigrationExecutionCheckpoint>> GetCheckpointAsync(string executionToken, CancellationToken ct = default);
    Task<StudioResult<AssemblyDiscoveryEvidence>> GetDiscoveryEvidenceAsync(CancellationToken ct = default);

    // ---- export ----
    Task<StudioResult<string>> ExportArtifactsAsync(MigrationPlanHandle planHandle, string outputDirectory, CancellationToken ct = default);
}
```

## Models

```csharp
public sealed record MigrationRequest(
    string SourceSourceName,                            // source source for diff
    string TargetSourceName,                            // target source for the plan
    string? NamespaceName = null,                        // for CLR-side discovery
    string? AssemblyPath = null,                          // for file-system discovery
    IReadOnlyList<string>? EntityNames = null,            // if null, scan namespace
    bool DetectRelationships = true,
    bool ApplyForeignKeys = false,
    bool ApplyIndexes = false);

public sealed record MigrationPlanHandle(
    string PlanId,                                       // unique plan id (UUID)
    string PlanHash,                                     // stable SHA-256 from the engine
    string SourceSourceName,
    string TargetSourceName,
    DateTimeOffset BuiltAt,
    IReadOnlyList<DdlOperationVm> Operations);

public sealed record DdlOperationVm(
    string EntityName,
    string Operation,                                    // "Create" | "Alter" | "Drop" | ...
    string DdlPreview,                                   // the actual SQL / file-mutation
    string RiskLevel,                                    // "Low" | "Medium" | "High" | "Critical"
    string DdlSource,                                    // "UniversalRdbmsHelper" | "FileMutation" | "Direct"
    string DdlHash,                                      // SHA-256 of the DDL string
    IReadOnlyList<string> Dependants,
    IReadOnlyList<string> Warnings);

public sealed record MigrationExecutionPolicy(
    int MaxTransientRetries = 2,
    bool RequireOperatorInterventionOnHardFail = true,
    bool RollbackOnFailure = true,
    TimeSpan? OperationTimeout = null,
    int BatchSize = 1000);

public sealed record MigrationExecutionHandle(
    string ExecutionToken,                              // engine's checkpoint token
    string PlanId,
    string PlanHash,
    DateTimeOffset StartedAt,
    MigrationExecutionState State);

public enum MigrationExecutionState { Queued, Running, Paused, Succeeded, Failed, Cancelled, RolledBack }

public sealed record MigrationExecutionCheckpoint(
    string ExecutionToken,
    string PlanId,
    string CurrentOperationIndex,
    int CompletedOperations,
    int TotalOperations,
    DateTimeOffset UpdatedAt);

public sealed record MigrationHistoryItem(
    string SourceName,
    DateTimeOffset AppliedAt,
    string PlanHash,
    int StepCount,
    bool Success,
    string? ErrorMessage,
    string? ApprovedBy);

public sealed record RollbackPolicy(
    bool UseCompensationPlan = true,
    bool RequireApproval = true,
    TimeSpan? OperationTimeout = null);

public sealed record MigrationRollbackReport(
    bool Success,
    int RolledBackOperations,
    int TotalOperations,
    IReadOnlyList<string> Warnings,
    string? ErrorMessage);

public sealed record MigrationDryRunReport(
    string PlanHash,
    IReadOnlyList<DryRunEntityReport> Entities);

public sealed record DryRunEntityReport(
    string EntityName,
    string TargetState,                                  // "Create" | "Alter" | "Drop" | "UpToDate"
    string DdlPreview,
    IReadOnlyList<string> Diffs);

public sealed record MigrationPreflightReport(
    string PlanHash,
    bool IsValid,
    IReadOnlyList<PreflightCheckResult> Checks);

public sealed record PreflightCheckResult(
    string Name,
    string Status,                                       // "Pass" | "Warn" | "Fail"
    string? Message);

public sealed record MigrationImpactReport(
    string PlanHash,
    IReadOnlyList<ImpactedEntity> Impacted);

public sealed record ImpactedEntity(
    string EntityName,
    IReadOnlyList<string> ReferencedBy,
    int RowCountEstimate);

public sealed record MigrationPerformancePlan(
    string PlanHash,
    TimeSpan EstimatedDuration,
    IReadOnlyList<PerformanceHint> Hints);

public sealed record PerformanceHint(
    string EntityName,
    string Hint,                                         // "Will lock Orders table for ~2m"
    string Severity);

public sealed record CiValidationReport(
    bool Pass,
    IReadOnlyList<CiCheckResult> Checks,
    string PlanHash);

public sealed record CiCheckResult(
    string Name,
    bool Pass,
    string? Message);

public sealed record PolicyEvaluationResult(
    bool IsAllowed,
    IReadOnlyList<PolicyViolation> Violations);

public sealed record PolicyViolation(
    string Code,
    string Message,
    string Severity);

public sealed record BestPracticesVm(
    string TargetType,                                  // e.g. "SqlServer"
    string TargetCategory,                              // e.g. "RDBMS"
    IReadOnlyList<BestPracticeItem> Items);

public sealed record BestPracticeItem(
    string Topic,
    string Advice,
    string Severity);                                   // "Info" | "Warn" | "Block"
```

## Folder layout (this phase creates)

```
Services/Studio/
├── Contracts/IMigrationStudioService.cs              ← DONE in Phase 1
├── Models/
│   ├── MigrationRequest.cs
│   ├── MigrationPlanHandle.cs
│   ├── DdlOperationVm.cs
│   ├── MigrationExecutionPolicy.cs
│   ├── MigrationExecutionHandle.cs
│   ├── MigrationExecutionState.cs
│   ├── MigrationExecutionCheckpoint.cs
│   ├── MigrationHistoryItem.cs
│   ├── RollbackPolicy.cs
│   ├── MigrationRollbackReport.cs
│   ├── MigrationDryRunReport.cs
│   ├── MigrationPreflightReport.cs
│   ├── MigrationImpactReport.cs
│   ├── MigrationPerformancePlan.cs
│   ├── CiValidationReport.cs
│   ├── PolicyEvaluationResult.cs
│   ├── BestPracticesVm.cs
│   └── AssemblyDiscoveryEvidence.cs
└── Migration/
    ├── MigrationStudioService.cs                      ← implements IMigrationStudioService
    ├── MigrationPlanBuilder.cs                        ← wraps BuildMigrationPlan*
    ├── MigrationPolicyEvaluator.cs                    ← wraps EvaluateMigrationPlanPolicy + EvaluateRolloutGovernance
    ├── MigrationRunner.cs                             ← wraps ExecuteMigrationPlan + ResumeMigrationPlan + RollbackFailedExecution
    ├── MigrationProgressStream.cs                     ← converts IProgress<PassedArgs> → IStudioProgress
    ├── MigrationArtifactExporter.cs                   ← wraps ExportMigrationArtifacts
    ├── MigrationPlanCache.cs                          ← in-memory cache keyed by (srcDs, dstDs, namespace, planHash)
    └── MigrationExecutionStateStore.cs                ← tracks active handles for cancel / resume
```

## Progress bridging

`MigrationProgressStream` is the adapter between the engine's
`IProgress<PassedArgs>` (the format the engine emits) and the Studio's
`IStudioProgress` (the format the host UI consumes). The bridge is implemented
in Phase 9 (`Adapters/StudioProgressBridge.cs`) — this phase declares the
intermediate type and the conversion logic.

## Execution lifecycle (host's view)

1. Host calls `BuildPlanAsync(request)` → `StudioResult<MigrationPlanVm>`.
2. Host calls `DryRunAsync(handle)` → renders a side-by-side diff.
3. Host calls `PreflightAsync(handle)` → renders pass/warn/fail chips.
4. Host calls `EvaluatePolicyAsync(handle)` → may block apply.
5. Host calls `ValidateForCiAsync(handle)` → boolean pass/fail (CI uses this).
6. Host calls `ApplyAsync(handle, policy, progress, ct)` → returns a
   `MigrationExecutionHandle` with a token.
7. Host streams `IStudioProgress` updates into the UI's progress bar.
8. Host can call `CancelAsync(token)` to abort.
9. Host can call `RollbackAsync(token, policy)` to undo a failed run.
10. Host can call `GetExecutionStateAsync(token)` from any tab to resume.

## Plan cache

`MigrationPlanCache` is a per-process cache of plans keyed by
`(srcDs, dstDs, namespace, planHash)`. The plan-hash comes from the
engine's `MigrationManager.Planning.cs:367-441` — the SHA-256 of all plan
inputs (datasource name, type, category, uses-discovery flag, entity count,
lifecycle state, sorted issues, sorted operations, etc.). This is the
fingerprint the host's CI uses to diff plans.

## Cross-cutting

- The Studio does **not** call `IMigrationManager.ApplyMigrations(...)` directly — it
  goes through `MigrationRunner` which uses `IMigrationManager.ExecuteMigrationPlan(...)`
  + checkpoint persistence. The studio benefits from the engine's retry pipeline +
  checkpoint + resume for free.
- The `MigrationExecutionHandle` is **stable across host restarts** — the engine's
  checkpoint is persisted in `IConfigEditor`, so closing the Blazor tab and reopening
  it can resume the run via `GetCheckpointAsync(token)`.
- Every `ApplyAsync` records an `IBeepAudit` event with the plan-hash, the policy,
  and the actor (Phase 8).
- The `MigrationPlanVm` is **read-only** from the host's perspective — the host can
  render it but not mutate it.

---

## Todo Tracker

| # | Task | Status | Notes |
|---|------|--------|-------|
| P06-01 | All `Models/*.cs` for this phase (~22 POCOs) | ⬜ | |
| P06-02 | `Migration/MigrationPlanBuilder.cs` — wraps `IMigrationManager.BuildMigrationPlan*` | ⬜ | |
| P06-03 | `Migration/MigrationPolicyEvaluator.cs` — wraps `EvaluateMigrationPlanPolicy` + `EvaluateRolloutGovernance` | ⬜ | |
| P06-04 | `Migration/MigrationRunner.cs` — wraps `ExecuteMigrationPlan` + `ResumeMigrationPlan` + `RollbackFailedExecution` | ⬜ | |
| P06-05 | `Migration/MigrationProgressStream.cs` — `IProgress<PassedArgs>` ↔ `IStudioProgress` bridge | ⬜ | |
| P06-06 | `Migration/MigrationArtifactExporter.cs` — wraps `ExportMigrationArtifacts`; writes to disk | ⬜ | |
| P06-07 | `Migration/MigrationPlanCache.cs` — in-memory cache keyed by plan-hash | ⬜ | |
| P06-08 | `Migration/MigrationExecutionStateStore.cs` — tracks active handles for cancel / resume | ⬜ | |
| P06-09 | `Migration/MigrationStudioService.cs` — implements `IMigrationStudioService` | ⬜ | |
| P06-10 | Wire `IMigrationStudioService` into `AddBeepStudio()` | ⬜ | |
| P06-11 | Tests: `MigrationPlanBuilderTests` (3+), `MigrationPolicyEvaluatorTests` (2+), `MigrationRunnerTests` (3+), `MigrationProgressStreamTests` (2+), `MigrationPlanCacheTests` (2+) | ⬜ | |
| P06-12 | Update `00-overview-and-scope.md` + `MASTER-TODO-TRACKER.md` to mark Phase 06 done | ⬜ | |

---

## Validation (definition of done)

- [ ] `dotnet build DataManagementEngineStandard` succeeds with **0 errors**.
- [ ] `BuildPlanAsync` on a sample 3-entity POCO namespace returns a plan with 3 create operations and a stable `PlanHash`.
- [ ] `DryRunAsync` returns a `MigrationDryRunReport` with the per-entity target state + diff.
- [ ] `PreflightAsync` returns a report with at least one Pass check.
- [ ] `EvaluatePolicyAsync` returns `IsAllowed = true` for a non-destructive plan; `IsAllowed = false` for a plan that drops a table.
- [ ] `ValidateForCiAsync` returns `Pass = true` for a clean plan; `Pass = false` for a plan that violates a CI check.
- [ ] `ApplyAsync` returns a `MigrationExecutionHandle` with a token; `GetExecutionStateAsync(token)` reports the state.
- [ ] `MigrationPlanCache` returns the same plan instance for the same `(srcDs, dstDs, namespace, planHash)`.
- [ ] All 12+ new tests pass.

---

## Pitfalls

1. **Don't call `ApplyMigrations(...)` directly** — it bypasses the engine's checkpoint + retry pipeline. Always go through `ExecuteMigrationPlan`.
2. **Don't store the `MigrationPlanArtifact` in the cache** — it's mutable. Cache the `MigrationPlanHandle` (immutable view-model) instead.
3. **Don't run the migration in the Blazor Server circuit** — `ApplyAsync` returns immediately with a handle; the actual run is on a background thread. The host UI subscribes to the `IStudioProgress` to render progress.
4. **Don't swallow the engine's `IErrorsInfo` errors** — wrap them in `StudioError` with the right `StudioErrorCode`. The host's UI will show a user-friendly message based on the code.
5. **Don't expose `IErrorsInfo` to the host** — it's an engine type. Convert to `StudioError`.
6. **Don't compute the plan-hash yourself** — use the engine's `Plan.Hash` from `MigrationPlanArtifact`. The hash includes inputs the host doesn't see.
7. **Don't allow a Live apply without an approval** — the policy check in `EvaluatePolicyAsync` is the gate. The host must call it before `ApplyAsync`.

---

## Related

- Phase 01 — contracts (this phase implements `IMigrationStudioService`)
- Phase 05 — schema discovery (provides the entities for the plan)
- Phase 07 — sync orchestration (parallel feature for data, not schema)
- Phase 08 — governance (every apply is gated by an approval + audited)
- Phase 09 — platform adapters (the progress bridge lives there)
- `BeepDM/DataManagementEngineStandard/Editor/Migration/IMigrationManager.cs` — the engine surface we wrap
- `BeepDM/DataManagementEngineStandard/Editor/Migration/MigrationManager.Planning.cs:367-441` — the plan-hash algorithm we re-use
- `BeepDM/DataManagementEngineStandard/Editor/Migration/MigrationManager.DevExAutomation.cs` — `ValidatePlanForCi`, `ExportMigrationArtifacts`
- `BeepDM/DataManagementEngineStandard/Editor/Migration/MigrationManager.RolloutGovernance.cs` — `EvaluateRolloutGovernance`
