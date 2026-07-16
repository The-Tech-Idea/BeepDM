# Phase 1 — Stabilize & Correctness

**Goal:** Make the existing framework behave as documented before building on it. Three of these bugs
make the *default* wizard unusable, and one is silent data corruption.

**Pre-condition:** none. This is the entry point.

**Files touched:** `DataManagementEngineStandard/SetUp/`, `DataManagementModelsStandard/SetUp/`,
`tests/SetupWizardTests/`

**Exit criteria:** `dotnet test BeepDM.sln` fully green (55/55 on SetupWizardTests — today's single
failure is resolved by 1-F).

---

## 1-A  `DriverProvisionStep.StepId` — duplicate key crashes the default wizard

**Files:** `SetUp/Steps/DriverProvisionStep.cs`, `SetUp/DefaultSetupWizardFactory.cs`

`StepId` is a hardcoded constant:

```csharp
// DriverProvisionStep.cs:47  — current
public string StepId => "driver-provision";
```

`DefaultSetupWizardFactory.BuildDriverSteps` emits **one step per distinct AutoLoad package**, so all
instances share one id. `SetupWizardBuilder.cs:167` then does:

```csharp
var stepById = _steps.ToDictionary(s => s.StepId, StringComparer.Ordinal);  // throws ArgumentException
```

**Result:** any stock config with 2+ AutoLoad drivers throws at `AddSetupWizard()` / `BootstrapAsync`
— i.e. at DI resolution, before any user code runs.

### Fix

```csharp
private readonly string _stepId;

public DriverProvisionStep(DriverProvisionStepOptions options)
{
    _options = options ?? throw new ArgumentNullException(nameof(options));
    _stepId = string.IsNullOrWhiteSpace(options.PackageName)
        ? "driver-provision"
        : $"driver-provision:{options.PackageName}";
}

public string StepId => _stepId;
```

Bare `"driver-provision"` is preserved when there's no package name, so existing single-driver
`DependsOn` references keep resolving. **`ConnectionConfigStep.DependsOn` currently hardcodes
`"driver-provision"`** — with N parameterized driver steps it must depend on all of them:

```csharp
// ConnectionConfigStep — DependsOn becomes constructor-injected rather than hardcoded
public IReadOnlyList<string> DependsOn => _options.DependsOnStepIds ?? new[] { "driver-provision" };
```

`DefaultSetupWizardFactory` passes the generated driver step ids in.

**Test:** `CreateDefault_WithTwoAutoLoadDrivers_DoesNotThrow`.

---

## 1-B  Default wizard cannot seed — null `Registry`

**File:** `SetUp/DefaultSetupWizardFactory.cs:59`

```csharp
.AddStep(new SeedingStep(new SeedingStepOptions()))   // Registry == null
```

`SeedingStep.Validate` (`SeedingStep.cs:74`) then always fails with
`"SeedingStepOptions.Registry must be set."` The factory's own XML doc admits the caller must patch
`SeedingStepOptions.Registry` before running — so the default wizard is not runnable as returned.

### Fix

Accept an optional registry and omit the step when absent, rather than shipping a step that always
fails:

```csharp
public static ISetupWizard CreateDefault(IDMEEditor editor, ISeederRegistry seeders = null)
{
    ...
    if (seeders != null && seeders.GetOrderedSeeders().Any())
        builder.AddStep(new SeedingStep(new SeedingStepOptions { Registry = seeders }));
    ...
}
```

`SetupWizardServiceExtensions.AddSetupWizard()` resolves `ISeederRegistry` from the container
(`GetService`, not `GetRequiredService` — solo apps may have none) and passes it through.

**Test:** `CreateDefault_WithoutSeeders_OmitsSeedingStep`; `CreateDefault_WithSeeders_ValidatesOk`.

---

## 1-C  `DefaultsSetupStep` writes a frozen timestamp *(silent data corruption)*

**File:** `SetUp/Steps/DefaultsSetupStep.cs:73-78`

```csharp
existing.Add(new DefaultValue
{
    PropertyName  = fieldName,
    PropertyValue = DateTime.UtcNow,   // ← evaluated ONCE, at setup time
    IsEnabled     = true
});
```

`DefaultValue`'s ctor sets `propertyType = DefaultValueType.Static`
(`DataManagementModelsStandard/ConfigUtil/DefaultValue.cs:17`) and `Rule` stays null. The step never
overrides either.

**Result:** `DateEntered`/`DateModified`/`CreatedAt`/`UpdatedAt` are stamped with **the setup run's
timestamp for the life of the app** — every row gets the same value. The step's own description says
"audit timestamps," which is what makes the intent unambiguous and the implementation wrong.

### Fix

Make them rule-based (`DefaultValue.IsRuleBased` is `!string.IsNullOrEmpty(Rule) && ...`):

```csharp
existing.Add(new DefaultValue
{
    PropertyName = fieldName,
    propertyType = DefaultValueType.Rule,
    Rule         = "UtcNow",     // resolved per-insert by DefaultsManager's rule engine
    IsEnabled    = true
});
```

**Confirm the exact rule token** against `Editor/Defaults/Resolvers/` before writing — `"UtcNow"` is
the intended semantic, not a verified literal. If no now-resolver exists, add one in this phase;
that's the whole point of the step.

**Test:** `DefaultsSetupStep_WritesRuleBasedTimestamp_NotStatic` — assert `IsRuleBased == true`.

---

## 1-D  `ReferenceDataSeederBase<T>` — partial failure masquerades as success

**File:** `SetUp/Seeding/ReferenceDataSeederBase.cs:20-40`

Row-by-row `InsertEntity` with no transaction and no batching. Combined with `SeederBase`'s coarse
default `IsAlreadySeeded` (`CheckEntityExist` + "any rows at all"), a failure at record 50/100 leaves
49 rows committed — and the **next run reports "already seeded" and silently skips records 50–100**.
That's a permanent, invisible data hole in reference data.

### Fix

Two parts:

1. Wrap the insert loop in a transaction when `IDataSource` supports one
   (`BeginTransaction`/`Commit`/`EndTransaction`), so a partial batch rolls back.
2. Override `IsAlreadySeeded` to be **per-record**, not "any rows at all":

```csharp
protected override bool IsAlreadySeeded(SetupContext ctx)
{
    var expected = GetRecords();                       // already abstract on the base
    var existing = CountExisting(ctx);
    return existing >= expected.Count;                 // not "existing > 0"
}
```

For datasources without transactions, insert-if-missing per record keeps it idempotent.

**Test:** `ReferenceDataSeeder_PartialFailure_DoesNotReportAlreadySeeded` — fail at record 50, re-run,
assert records 50–100 are inserted.

---

## 1-E  `DriverProvisionStep` fakes verification

**File:** `SetUp/Steps/DriverProvisionStep.cs:154-157`

When `IsDriverClassLoaded` returns false, the step sets `driver.IsMissing = false` and reports
success — defeating the check it just ran and letting `ConnectionConfigStep` proceed against a driver
that isn't loaded.

### Fix

```csharp
if (!assemblyHandler.IsDriverClassLoaded(driver.classHandler, driver.dllname))
    return StepErrorHelpers.Fail(
        $"Driver '{driver.PackageName}' installed but class '{driver.classHandler}' did not load.");
driver.IsMissing = false;
```

**Test:** `DriverProvisionStep_Fails_WhenClassNotLoaded`.

---

## 1-F  Decide the step-order rule *(product decision — resolves the failing test)*

Three places disagree today:

| Where | Behavior |
|---|---|
| `SetupWizard.ValidateStepDefinitions` (`:336-342`) | Compares registration indices; **returns `IErrorsInfo`** at `Run()` |
| `SetupWizardBuilder.Build()` XML doc (`:145-150`) | *Claims* it throws "if a dependency is missing **or declared out of order**" |
| `SetupWizardBuilder.ValidateDependencyOrder` (`:165-229`) | Kahn's over a **reversed** graph — order-independent, so it **cannot** detect out-of-order |
| `SetupWizardBuilderTests.Build_Throws_WhenStepsOutOfOrder` (`:54-70`) | Asserts `Build()` **throws** → **fails today** |

The builder's Kahn result (`sorted`) is computed and then **discarded**; `_steps` is never reordered.

### Options

- **(a) Throw early.** Add an index check to `ValidateDependencyOrder` mirroring
  `SetupWizard.cs:336-342`. Test passes unchanged; matches the existing XML doc. Registration order
  stays meaningful.
- **(b) Auto-sort.** Use the Kahn `sorted` output to reorder `_steps`; delete the test; ordering
  becomes declarative via `DependsOn` only. Friendliest for P2 (a serialized definition shouldn't
  depend on array order), but silently reorders what the caller wrote.
- **(c) Fail at run.** Retarget the test at `wizard.Run(...)` asserting `Errors.Failed`; delete the
  XML doc claim.

**Recommendation: (a) now, (b) in P2.** (a) is the smallest change that makes doc, code, and test
agree, and it keeps the failure at author-time. When P2 makes the definition data, revisit (b) —
a JSON definition's element order shouldn't be load-bearing, and `DependsOn` is already the truth.

Whichever is chosen, **all four** of code/doc/test/wizard must be updated together.

---

## 1-G  Bridge the report hooks

`SetupReport.DryRunReportJson` and `.RollbackReportJson` are **never populated** — `BuildReport`
(`SetupWizard.cs:264-285`) doesn't read `context.Properties`, where `SchemaSetupStep.cs:156` puts the
dry-run JSON. `SetupContext.SetDryRunReport` / `TryGetDryRunReport` / `SetCompensationPlan` have
**zero callers** (`SchemaSetupStep` writes the dictionary directly).

### Fix

- `SchemaSetupStep` → use `context.SetDryRunReport(json)` / `context.SetCompensationPlan(json)`.
- Add `SetupContext.TryGetCompensationPlan()` — currently write-only, which P4 needs.
- `BuildReport` → populate both fields from the context.

**Test:** `Report_Surfaces_DryRunJson_AfterDryRun`.

---

## 1-H  Make silent degradations loud

Two paths fail quietly and cost hours to diagnose:

1. **`StateFilePath == null` disables checkpointing** — `SetupCheckpointStore.LoadPersistedState` /
   `PersistState` both no-op with no warning. Log a warning once per run.
2. **`context.Options` silently overrides wizard `Options`** — `SetupWizard.Run:44` does
   `context.Options ?? Options`, so a caller who sets `DryRun` on the builder but passes a context
   with default options gets a **live run**. Log a warning when both are non-null and not reference-equal.

Neither should throw — existing callers depend on the current tolerance.

---

## 1-I  `ReportOutputPath` is never read

`SetupOptions.ReportOutputPath` (`SetupOptions.cs:19`) has no reader anywhere. Either write the
report there in `BuildReport` (preferred — P6 builds on it) or delete the property. Do **not** leave
it as a third dead hook.

---

## 1-J  Adapter base class

The `RunAsync` body is copy-pasted across all six adapters (`Console:39-51`, `Desktop:56-68`,
`BlazorServer:40-48`, `BlazorWasm:56-70`, `Maui:52-60`, `WebApi:35-50`) — identical
`Progress<PassedArgs>` → `try { await Task.Run(...) } catch (OperationCanceledException) { }` →
`GetReport()`. Only `WebApi` catches general exceptions, so **five of six adapters behave differently
on an unexpected throw**.

### Fix

```csharp
public abstract class SetupWizardAdapterBase : ISetupWizardAdapter
{
    public virtual async Task<SetupReport> RunAsync(ISetupWizard wizard, SetupContext ctx, CancellationToken token = default)
    {
        var progress = new Progress<PassedArgs>(OnProgress);
        try   { await Task.Run(() => wizard.Run(ctx, progress), token).ConfigureAwait(false); }
        catch (OperationCanceledException) { OnCancelled(); }
        catch (Exception ex)               { OnFailed(ex); }     // uniform, was WebApi-only
        var report = wizard.GetReport();
        OnCompleted(report);
        return report;
    }

    protected virtual void OnProgress(PassedArgs args) { }
    protected virtual void OnCancelled() { }
    protected virtual void OnFailed(Exception ex) { }
    protected virtual void OnCompleted(SetupReport report) { }
}
```

Keep `ISetupWizardAdapter` unchanged — it's a shipped contract.

Also fix while here: `ConsoleSetupWizardAdapter.cs:23` always prints step 1 regardless of resume
position, and `:31-33` infers the active step as "first not-completed", which is wrong once a step is
skipped.

### ✅ Done — and it was not the mechanical refactor it looked like

Implemented as `SetUp/Adapters/SetupWizardAdapterBase.cs`; all six adapters migrated.
Tests were written **first** (`tests/SetupWizardTests/AdapterBehaviorTests.cs`), which produced a
clean red state: 12 passing (pinning behavior worth keeping) and **5 failing** — exactly the five
adapters that leaked exceptions. What the attempt surfaced:

1. **Hooks must be `Task`-returning.** `MauiSetupWizardAdapter` *awaits*
   `InvokeOnMainThreadAsync(() => _completedAction?.Invoke(report))` before returning. A base with a
   sync `void ShowResult` silently turns that into fire-and-forget. The base exposes
   `OnRunStartingAsync` / `OnCancelledAsync` / `OnFailedAsync` / `OnCompletedAsync`, and
   `Maui_AwaitsMainThreadCompletion_BeforeRunAsyncReturns` guards it.
2. **`WebApi` is a state machine, not a shell.** It overrides `RunAsync` to keep its never-null
   return (`report ?? new SetupReport { Succeeded = false }`) and maps the rest onto hooks; its
   `Running → Completed/Failed/Cancelled` transitions are guarded by tests.
3. **`Desktop` must override `ReportProgress`.** The base default rebuilds a `PassedArgs` from
   (stepId, percent, message); Desktop's callback receives the wizard's **original** args and
   consumers may read `Flag`/`ErrorObject`. Passing the raw args through preserves that.
4. **`BlazorWasm` needs the context in its hooks** — it loads state on start and persists on cancel,
   failure, and completion. Hence `OnCompletedAsync(SetupReport, SetupContext)`.
5. **Watch for member hiding.** `Show*` were non-virtual; a plain migration leaves them *hiding* the
   base virtuals (CS0114), so a base-driven `ShowResult` would hit the empty base version. `WebApi`
   needed explicit `override`. **An incremental build masks these — verify with `--no-incremental`.**
6. **`Console.ShowResult` needed hardening** — it enumerates `StepResults` and slices `ContentHash`,
   and is now reachable on the failure path where a report may be partial.

The 3-then-1-by-1 split (Console/Desktop/BlazorServer, then Maui, WASM, WebApi) worked: failures went
5 → 2 → 0 with a build+test between each.

---

## 1-K  Tests

Add to `tests/SetupWizardTests/`:

| Test | Guards |
|---|---|
| `CreateDefault_WithTwoAutoLoadDrivers_DoesNotThrow` | 1-A |
| `CreateDefault_WithoutSeeders_OmitsSeedingStep` | 1-B |
| `DefaultsSetupStep_WritesRuleBasedTimestamp_NotStatic` | 1-C |
| `ReferenceDataSeeder_PartialFailure_DoesNotReportAlreadySeeded` | 1-D |
| `DriverProvisionStep_Fails_WhenClassNotLoaded` | 1-E |
| `Report_Surfaces_DryRunJson_AfterDryRun` | 1-G |
| `Adapters_AllSurfaceUnexpectedException` | 1-J |

`Build_Throws_WhenStepsOutOfOrder` is resolved by 1-F (kept, retargeted, or deleted per the decision).

## Files summary

| Action | File | Est. |
|---|---|---|
| Modify | `SetUp/Steps/DriverProvisionStep.cs` | ~15 |
| Modify | `SetUp/Steps/ConnectionConfigStep.cs` | ~5 |
| Modify | `SetUp/Steps/DefaultsSetupStep.cs` | ~10 |
| Modify | `SetUp/DefaultSetupWizardFactory.cs` | ~20 |
| Modify | `SetUp/SetupWizardBuilder.cs` | ~15 (1-F) |
| Modify | `SetUp/SetupWizard.cs` | ~20 (1-G, 1-H, 1-I) |
| Modify | `SetUp/SetupCheckpointStore.cs` | ~5 (1-H) |
| Modify | `SetUp/Seeding/ReferenceDataSeederBase.cs` | ~40 |
| Modify | `DataManagementModelsStandard/SetUp/SetupContext.cs` | ~6 (1-G) |
| New | `SetUp/Adapters/SetupWizardAdapterBase.cs` | ~50 |
| Modify | all 6 adapters | ~-40 net |
| New/Modify | `tests/SetupWizardTests/*` | ~200 |
