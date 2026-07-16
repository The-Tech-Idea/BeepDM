# Phase 4 — Rollback & Compensation

**Goal:** Make a failed setup undo itself. Today a failure at step N leaves steps 1..N-1 applied with
no automated undo.

**Pre-condition:** Phase 1 (1-G bridges the compensation plan out of `context.Properties`).

**Files touched:** `DataManagementModelsStandard/SetUp/`, `DataManagementEngineStandard/SetUp/`

---

## ✅ Status: complete

All items P4-01..07 landed (per-item summary in the master tracker). 183/183 tests green;
`RollbackTests.cs` (9) covers it.

**One deliberate deviation from the plan.** 4-C said "execute the compensation plan `SchemaSetupStep`
already builds" and referenced an `ExecuteCompensationPlanAsync` — which the plan itself flagged to
*verify before writing*. It doesn't exist. The real API is
`MigrationManager.RollbackFailedExecution(executionToken)`, which undoes what was **actually
executed**, keyed by the token already recorded on the state. That's strictly safer than replaying a
serialized plan: the live schema may have drifted since the plan was built, and re-deriving could
compute a different, wrong compensation. So the compensation-plan JSON (`context.SetCompensationPlan`)
remains an advisory record; the token-based rollback is what runs. If a token wasn't recorded, the
schema step **fails loudly** rather than reporting a phantom undo.

The `IUndoableSeeder` / `IBackupConfirmationProvider` interfaces landed as specified.

---

## What's wrong today

The design exists; the execution doesn't.

| Hook | Reality |
|---|---|
| `SchemaSetupStep` `BuildCompensationPlan` + `CheckRollbackReadiness` | plan is **built and serialized** into `context.Properties["CompensationPlanJson"]` — **nothing ever executes it** |
| `SetupContext.SetCompensationPlan` | write-only; **no getter exists** |
| `SetupReport.RollbackReportJson` | never populated; always `null` |
| `ISetupStep` | **no rollback member at all** |
| `CheckRollbackReadiness(backupConfirmed: !strict)` | in non-strict mode it **asserts a backup exists without checking** |

Concretely: a run that fails at `seeding` leaves the schema created and reference data half-inserted
(and per P1-1D, the coarse `IsAlreadySeeded` then reports "already seeded" on retry).

---

## 4-A  `ISetupStep.RollbackAsync`

Add as a **default interface method** so all six existing steps compile unchanged:

```csharp
// ISetupStep
/// <summary>Undo this step's effects. Default: no-op (step declares itself non-compensating).</summary>
Task<IErrorsInfo> RollbackAsync(SetupContext context, IProgress<PassedArgs> progress = null,
                                CancellationToken token = default)
    => Task.FromResult<IErrorsInfo>(new ErrorsInfo { Flag = Errors.Ok, Message = "No rollback defined." });

/// <summary>False when rollback is a no-op, so the orchestrator can report honestly.</summary>
bool SupportsRollback => false;
```

`SupportsRollback` matters: without it the orchestrator can't distinguish "undone" from "nothing to
undo", and would report a clean rollback that never happened.

## 4-B  `IRollbackOrchestrator`

**New:** `DataManagementModelsStandard/SetUp/Rollback/IRollbackOrchestrator.cs`
**New:** `DataManagementEngineStandard/SetUp/Rollback/RollbackOrchestrator.cs`

```csharp
public interface IRollbackOrchestrator
{
    Task<RollbackReport> RollbackAsync(ISetupWizard wizard, SetupContext context,
                                       IProgress<PassedArgs> progress = null,
                                       CancellationToken token = default);
}

public sealed class RollbackReport
{
    public string RunId { get; set; }
    public bool Succeeded { get; set; }
    public List<RollbackStepResult> StepResults { get; set; } = new();
    public DateTime StartedAt { get; set; }
    public DateTime FinishedAt { get; set; }
}

public sealed class RollbackStepResult
{
    public string StepId { get; set; }
    public bool Succeeded { get; set; }
    public bool Skipped { get; set; }          // SupportsRollback == false
    public string Message { get; set; }
    public TimeSpan Elapsed { get; set; }
}
```

Semantics:

- Walk `State.CompletedStepIds` in **reverse**, starting from `State.FailedStepId`.
- Skip steps in `SkippedStepIds` — they never applied anything.
- **A failing rollback does not abort the remaining rollbacks.** Continue, record each, report
  overall failure. Stopping halfway would strand *more* state than it cleans.
- Never throw — return `RollbackReport`, per repo convention.

## 4-C  Execute the compensation plan

`SchemaSetupStep.RollbackAsync` reads back the plan (1-G adds `TryGetCompensationPlan`) and executes
it through `MigrationManager` — the same manager that built it. It must **not** re-derive a plan at
rollback time: the schema may have drifted, and the plan recorded what was actually applied.

```csharp
public override async Task<IErrorsInfo> RollbackAsync(SetupContext ctx, IProgress<PassedArgs> p, CancellationToken ct)
{
    if (!ctx.TryGetCompensationPlan(out var json))
        return Fail("No compensation plan recorded; cannot roll back schema.");   // loud, not silent
    var plan = Deserialize(json);
    return await new MigrationManager(ctx.Editor, ctx.DataSource).ExecuteCompensationPlanAsync(plan, p, ct);
}

public override bool SupportsRollback => true;
```

Verify `ExecuteCompensationPlanAsync`'s real name against `Editor/Migration/IMigrationManager.cs`
before writing — `BuildCompensationPlan` and `CheckRollbackReadiness` are confirmed to exist; the
executor is not.

## 4-D  `IUndoableSeeder`

```csharp
public interface IUndoableSeeder : ISeeder
{
    Task<IErrorsInfo> UnseedAsync(SetupContext context, CancellationToken token = default);
}
```

`SeedingStep.RollbackAsync` unseeds `State.CompletedSeederIds` in reverse, skipping seeders that
don't implement it (recorded as `Skipped`, not `Succeeded`). Pairs with P1-1D: once seeding is
transactional and per-record idempotent, unseed is well-defined.

## 4-E  Fix `CheckRollbackReadiness`

`backupConfirmed: !strict` is backwards — it claims a backup exists precisely when nobody checked.
Replace with an explicit contract:

```csharp
public interface IBackupConfirmationProvider
{
    Task<bool> IsBackupConfirmedAsync(SetupContext context, CancellationToken token = default);
}
```

Solo default returns `false` and **logs a warning** (honest: no backup). Strict mode requires `true`.
Never claim a backup that wasn't verified.

## 4-F  Wire the report

Populate `SetupReport.RollbackReportJson` from `RollbackReport` (1-G opened the path). Auto-rollback
is **opt-in**, not default — silently undoing a partial setup can destroy diagnostic state:

```csharp
public bool AutoRollbackOnFailure { get; init; }   // SetupOptions; default false
```

## 4-G  Tests

| Test | Guards |
|---|---|
| `FailAtStepN_CompensatesStepsNMinus1_To_1_InReverse` | 4-B |
| `Rollback_Continues_WhenOneStepFails` | 4-B |
| `Rollback_SkipsSteps_ThatNeverRan` | 4-B |
| `NonCompensatingStep_ReportsSkipped_NotSucceeded` | 4-A |
| `SchemaRollback_Fails_Loudly_WhenNoPlanRecorded` | 4-C |
| `NonStrict_DoesNotClaimBackupConfirmed` | 4-E |
| `AutoRollback_Off_ByDefault` | 4-F |

## Files summary

| Action | File | Est. |
|---|---|---|
| Modify | `Models/SetUp/ISetupStep.cs` (2 DIMs) | ~10 |
| New | `Models/SetUp/Rollback/IRollbackOrchestrator.cs` + reports | ~70 |
| New | `Models/SetUp/Rollback/IUndoableSeeder.cs` | ~12 |
| New | `Models/SetUp/Rollback/IBackupConfirmationProvider.cs` | ~12 |
| New | `Engine/SetUp/Rollback/RollbackOrchestrator.cs` | ~150 |
| Modify | `Engine/SetUp/Steps/SchemaSetupStep.cs` | ~50 |
| Modify | `Engine/SetUp/Steps/SeedingStep.cs` | ~40 |
| Modify | `Engine/SetUp/Steps/ConnectionConfigStep.cs` | ~25 |
| Modify | `Models/SetUp/SetupOptions.cs` (+`AutoRollbackOnFailure`) | ~3 |
| New | `tests/SetupWizardTests/RollbackTests.cs` | ~250 |
