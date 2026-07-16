# Phase 8 — CLI, Unattended & CI

**Goal:** Drive setup without a UI — from a terminal, a container entrypoint, or a CI job — and gate
definitions on pull requests.

**Pre-condition:** Phase 2. A CLI cannot construct `SchemaSetupStepOptions { EntityTypes = ... }`;
it needs a definition file.

**Files touched:** new `SetUp/Cli/`, `DataManagementEngineStandard/SetUp/`

---

## What's wrong today

- **`ConsoleSetupWizardAdapter` is not a CLI.** It's an output surface — `Console.WriteLine` table
  rendering. No arg parsing, no exit codes, no unattended mode.
- **Cancellation is cosmetic.** `ISetupStep.Execute` takes no `CancellationToken`; `RunAsync` wraps
  the whole synchronous `Run` in one `Task.Run`, so a token only prevents *starting* a step. Ctrl-C
  during `ExecuteMigrationPlan` does nothing.
- **No CI gate.** A broken definition is only discovered at run time, against a live datasource.

---

## 8-A  Verbs

```
beep setup validate  --definition <path> [--strict]
beep setup plan      --definition <path> --env <id>          # dry-run; prints the report
beep setup apply     --definition <path> --env <id> [--yes] [--auto-rollback]
beep setup status    --app <id> --env <id>
beep setup rollback  --app <id> --env <id> --run <runId>
```

`validate` is the CI gate: **structural only, no `IDMEEditor`, no datasource** (P2-G). That's what
makes it runnable on a PR.

`plan` = `apply --dry-run` and must **prove** it mutates nothing (8-E).

## 8-B  Exit codes

Scripts branch on these; they're part of the contract.

| Code | Meaning |
|---|---|
| 0 | success (or `validate` passed) |
| 1 | unexpected error |
| 2 | definition invalid (`validate` failed) |
| 3 | setup failed; rollback succeeded |
| 4 | setup failed; **rollback also failed** — needs a human |
| 5 | not authorized (P5) |
| 6 | lease held by another runner (P3) |

**3 vs 4 is the important distinction** — 4 means the system is in a partial state and nothing
automated will fix it.

## 8-C  Unattended mode

```csharp
public bool NonInteractive { get; init; }   // SetupOptions
```

No prompts; any input requirement is an error, not a hang. `--yes` is required for `apply` when
`NonInteractive` — refuse to mutate a database from a script without explicit intent.

Solo default stays interactive. Detect CI via the conventional `CI` env var and default
`NonInteractive = true` there, but never override an explicit flag.

## 8-D  Real cancellation

This is the substantive engineering item in the phase, and it's a **contract change**:

```csharp
// ISetupStep — new DIM; existing steps keep compiling
Task<IErrorsInfo> ExecuteAsync(SetupContext context, IProgress<PassedArgs> progress,
                               CancellationToken token)
    => Task.Run(() => Execute(context, progress), token);   // current default: still non-interruptible
```

`SetupWizard.RunAsync` must call `step.ExecuteAsync(...)` **per step** rather than wrapping the whole
sync `Run` in one `Task.Run`. Then steps that can honour a token override it and thread it into the
call they already make:

- `SchemaSetupStep` → `ExecuteMigrationPlan(..., token)`
- `SeedingStep` → check between seeders
- `DriverProvisionStep` → `LoadNuggetFromNuGetAsync(..., token)` (it currently does
  `Task.Run(...).GetAwaiter().GetResult()` to dodge a sync-over-async deadlock — real async removes
  the need)

Steps that don't override stay non-interruptible; that's honest and back-compatible. A cancelled run
must leave a **valid checkpoint** so `apply` can resume — cancellation is not failure.

## 8-E  CI gate

```yaml
- run: dotnet beep setup validate --definition ./setup/app-setup.json --strict
```

Fails the PR on: unknown step type, cycle, duplicate `StepId`, unresolvable `EntityTypeNames`
(when the assembly is available), unsupported `SchemaVersion`.

Pairs with P2's diff-stable serializer — reviewers see a meaningful diff, CI proves it still loads.

## 8-F  Tests

| Test | Guards |
|---|---|
| `Validate_Returns2_OnInvalidDefinition` | 8-B |
| `Validate_RunsWithout_DataSource` | 8-A CI gate |
| `Apply_DryRun_MutatesNothing` | 8-A |
| `Apply_NonInteractive_Without_Yes_Refuses` | 8-C |
| `RollbackFailure_Returns4_Not3` | 8-B |
| `Cancel_MidStep_LeavesResumableCheckpoint` | 8-D |
| `UnauthorizedRun_Returns5` | 8-B + P5 |

## Files summary

| Action | File | Est. |
|---|---|---|
| New | `Engine/SetUp/Cli/SetupCliCommands.cs` | ~220 |
| New | `Engine/SetUp/Cli/SetupExitCode.cs` | ~30 |
| Modify | `Models/SetUp/ISetupStep.cs` (cancellable DIM) | ~8 |
| Modify | `Models/SetUp/SetupOptions.cs` (+`NonInteractive`) | ~3 |
| Modify | `Engine/SetUp/SetupWizard.cs` (per-step async) | ~60 |
| Modify | `Engine/SetUp/Steps/SchemaSetupStep.cs` | ~25 |
| Modify | `Engine/SetUp/Steps/SeedingStep.cs` | ~15 |
| Modify | `Engine/SetUp/Steps/DriverProvisionStep.cs` | ~25 |
| New | `tests/SetupWizardTests/CliTests.cs` | ~200 |

---

## Note on hosting the CLI

`System.CommandLine` is the house style for Beep CLIs (BeepShell uses it), but adding it to
`DataManagementEngine.csproj` puts a CLI parser in a library every consumer app references.

**Recommendation:** put the *verbs* in the engine as plain methods (`SetupCliCommands`) taking parsed
arguments, and host the parser in a separate `Beep.SetUp.Cli` tool project. The engine stays
dependency-clean; the tool project is where `System.CommandLine` lives. `beep setup` then wires these
verbs, and `dotnet beep` ships as a `PackAsTool` package.
