# Phase 1 — Wizard Contract and Architecture Foundation

## Objective

Establish the core contracts and base classes that every other phase builds upon.  
This phase defines the "spine" of the setup wizard system: how steps are declared, how context flows between them, how state is checkpointed, and how the wizard is composed and executed.

No database calls occur in this phase. All types are pure contracts or base implementations testable against `InMemory` datasources.

---

## Scope

- `ISetupStep` — single unit of work within a wizard
- `ISetupWizard` — orchestration contract for running a sequence of steps
- `SetupContext` — shared mutable runtime state passed through all steps
- `SetupState` — serializable checkpoint (which steps are done, plan hashes)
- `SetupReport` — immutable outcome record produced after a wizard run
- `ISetupProgressReporter` — unified progress surface for all platforms
- `SetupOptions` — configuration flags (dry-run, skip-seed, target environment)
- `SetupWizardBuilder` — fluent API for composing wizards
- `SetupWizardBase` — abstract base implementing the step loop and checkpoint logic

---

## Type Specifications

### 1. `ISetupStep`

```csharp
namespace TheTechIdea.Beep.SetUp
{
    public interface ISetupStep
    {
        /// <summary>Stable, unique identifier for this step (e.g. "connection-config").</summary>
        string StepId { get; }

        /// <summary>Human-readable display name shown in progress UI.</summary>
        string StepName { get; }

        /// <summary>Optional description shown in wizard UI and reports.</summary>
        string Description { get; }

        /// <summary>Ordered list of StepIds that must complete before this step runs.</summary>
        IReadOnlyList<string> DependsOn { get; }

        /// <summary>
        /// True if this step can be safely skipped when it detects it has already
        /// been applied (idempotency guard).
        /// </summary>
        bool CanSkip(SetupContext context);

        /// <summary>Validate pre-conditions before Execute is called.</summary>
        IErrorsInfo Validate(SetupContext context);

        /// <summary>
        /// Execute the step. Must return Errors.Ok on success.
        /// Must NOT throw; surface errors via IErrorsInfo.
        /// </summary>
        IErrorsInfo Execute(SetupContext context, IProgress<PassedArgs> progress = null);
    }
}
```

### 2. `ISetupWizard`

```csharp
namespace TheTechIdea.Beep.SetUp
{
    public interface ISetupWizard
    {
        /// <summary>Ordered list of steps to execute.</summary>
        IReadOnlyList<ISetupStep> Steps { get; }

        /// <summary>Current runtime state (may be resumed from a saved checkpoint).</summary>
        SetupState State { get; }

        /// <summary>Options controlling dry-run, skip-seed, environment, etc.</summary>
        SetupOptions Options { get; }

        /// <summary>
        /// Run all pending steps in order. Idempotent — already-done steps are skipped.
        /// Returns Errors.Ok when all steps succeed.
        /// </summary>
        IErrorsInfo Run(SetupContext context, IProgress<PassedArgs> progress = null);

        /// <summary>
        /// Resume from the last persisted checkpoint.
        /// Equivalent to Run but skips steps recorded as done in State.
        /// </summary>
        IErrorsInfo Resume(SetupContext context, IProgress<PassedArgs> progress = null);

        /// <summary>Returns the report for the most recent Run/Resume call.</summary>
        SetupReport GetReport();
    }
}
```

### 3. `SetupContext`

```csharp
namespace TheTechIdea.Beep.SetUp
{
    /// <summary>
    /// Mutable shared state flowing through all wizard steps.
    /// Steps read from and write to this context.
    /// </summary>
    public class SetupContext
    {
        public IDMEEditor Editor { get; set; }
        public IDataSource DataSource { get; set; }
        public SetupOptions Options { get; set; } = new SetupOptions();
        public ISetupProgressReporter ProgressReporter { get; set; }

        // Populated by Phase 2 (ConnectionConfigStep)
        public ConnectionProperties ConnectionProperties { get; set; }

        // Populated by Phase 3 (SchemaSetupStep)
        public MigrationPlanArtifact MigrationPlan { get; set; }
        public MigrationExecutionResult MigrationResult { get; set; }

        // Populated by Phase 4 (SeedingStep)
        public IReadOnlyList<string> CompletedSeederIds { get; set; }

        // Bag for steps to pass custom context forward
        public Dictionary<string, object> Properties { get; } = new Dictionary<string, object>();
    }
}
```

### 4. `SetupState` (Serializable Checkpoint)

```csharp
namespace TheTechIdea.Beep.SetUp
{
    /// <summary>
    /// Serializable snapshot of wizard progress.
    /// Persist to JSON so runs can resume after crash or user interruption.
    /// </summary>
    public class SetupState
    {
        /// <summary>Set of step IDs that have completed successfully.</summary>
        public HashSet<string> CompletedStepIds { get; set; } = new HashSet<string>();

        /// <summary>Set of step IDs that were skipped (CanSkip returned true).</summary>
        public HashSet<string> SkippedStepIds { get; set; } = new HashSet<string>();

        /// <summary>Step ID of the last failed step, or null if no failure.</summary>
        public string FailedStepId { get; set; }

        /// <summary>Content hash of the entity list used for schema creation.</summary>
        public string SchemaHash { get; set; }

        /// <summary>Seeder IDs that have already been applied.</summary>
        public HashSet<string> CompletedSeederIds { get; set; } = new HashSet<string>();

        /// <summary>UTC timestamp of the first successful step in this run.</summary>
        public DateTimeOffset? StartedAt { get; set; }

        /// <summary>UTC timestamp of the last completed step.</summary>
        public DateTimeOffset? LastUpdatedAt { get; set; }

        /// <summary>Arbitrary key-value bag for step-specific state.</summary>
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

        public bool IsStepCompleted(string stepId) =>
            CompletedStepIds.Contains(stepId) || SkippedStepIds.Contains(stepId);
    }
}
```

### 5. `SetupReport` (Immutable Outcome)

```csharp
namespace TheTechIdea.Beep.SetUp
{
    public class SetupStepResult
    {
        public string StepId { get; set; }
        public string StepName { get; set; }
        public bool Succeeded { get; set; }
        public bool Skipped { get; set; }
        public string Message { get; set; }
        public TimeSpan Elapsed { get; set; }
        public DateTimeOffset ExecutedAt { get; set; }
    }

    public class SetupReport
    {
        public string WizardId { get; set; }
        public string RunId { get; set; } = Guid.NewGuid().ToString("N");
        public bool Succeeded { get; set; }
        public IReadOnlyList<SetupStepResult> StepResults { get; set; }
        public DateTimeOffset StartedAt { get; set; }
        public DateTimeOffset FinishedAt { get; set; }
        public TimeSpan TotalElapsed => FinishedAt - StartedAt;
        public string ContentHash { get; set; }      // SHA-256 of StepResults JSON
        public string RollbackReportJson { get; set; } // populated on rollback
        public string DryRunReportJson { get; set; }   // populated in dry-run mode
    }
}
```

### 6. `ISetupProgressReporter`

```csharp
namespace TheTechIdea.Beep.SetUp
{
    public interface ISetupProgressReporter
    {
        void ReportStepStart(string stepId, string stepName, int stepIndex, int totalSteps);
        void ReportStepProgress(string stepId, int percentComplete, string message);
        void ReportStepComplete(string stepId, bool succeeded, string message);
        void ReportWizardComplete(SetupReport report);
    }
}
```

### 7. `SetupOptions`

```csharp
namespace TheTechIdea.Beep.SetUp
{
    public class SetupOptions
    {
        /// <summary>If true, build plans and dry-runs but do not apply changes.</summary>
        public bool DryRun { get; set; } = false;

        /// <summary>If true, skip all seeding steps.</summary>
        public bool SkipSeeding { get; set; } = false;

        /// <summary>If true, skip schema-creation steps (assume schema already exists).</summary>
        public bool SkipSchema { get; set; } = false;

        /// <summary>Target environment label used by policy gates (e.g. "Development", "Production").</summary>
        public string Environment { get; set; } = "Development";

        /// <summary>If true, fail on any policy warning (not just blocks).</summary>
        public bool StrictPolicyMode { get; set; } = false;

        /// <summary>Path for persisting SetupState JSON. Null = in-memory only.</summary>
        public string StateFilePath { get; set; }

        /// <summary>Path for writing SetupReport JSON/Markdown artifacts.</summary>
        public string ReportOutputPath { get; set; }
    }
}
```

### 8. `SetupWizardBuilder` (Fluent Builder)

```csharp
namespace TheTechIdea.Beep.SetUp
{
    public class SetupWizardBuilder
    {
        private readonly List<ISetupStep> _steps = new();
        private SetupOptions _options = new SetupOptions();
        private string _wizardId = "default-setup";

        public SetupWizardBuilder WithId(string wizardId)
        {
            _wizardId = wizardId;
            return this;
        }

        public SetupWizardBuilder AddStep(ISetupStep step)
        {
            _steps.Add(step);
            return this;
        }

        public SetupWizardBuilder WithOptions(SetupOptions options)
        {
            _options = options;
            return this;
        }

        public SetupWizardBuilder WithDryRun(bool dryRun = true)
        {
            _options.DryRun = dryRun;
            return this;
        }

        public SetupWizardBuilder WithEnvironment(string env)
        {
            _options.Environment = env;
            return this;
        }

        public ISetupWizard Build()
        {
            return new SetupWizard(_wizardId, _steps, _options);
        }
    }
}
```

### 9. `SetupWizardBase` / `SetupWizard` (Abstract Base + Default Implementation)

```csharp
namespace TheTechIdea.Beep.SetUp
{
    public class SetupWizard : ISetupWizard
    {
        private readonly string _wizardId;
        private readonly List<ISetupStep> _steps;
        private SetupReport _lastReport;

        public IReadOnlyList<ISetupStep> Steps => _steps.AsReadOnly();
        public SetupState State { get; private set; } = new SetupState();
        public SetupOptions Options { get; }

        public SetupWizard(string wizardId, IEnumerable<ISetupStep> steps, SetupOptions options)
        {
            _wizardId = wizardId;
            _steps = new List<ISetupStep>(steps);
            Options = options;
        }

        public IErrorsInfo Run(SetupContext context, IProgress<PassedArgs> progress = null)
        {
            State.StartedAt ??= DateTimeOffset.UtcNow;
            var results = new List<SetupStepResult>();
            var started = DateTimeOffset.UtcNow;

            foreach (var step in _steps)
            {
                if (State.IsStepCompleted(step.StepId)) continue;

                // Validate pre-conditions
                var validation = step.Validate(context);
                if (validation.Flag == Errors.Failed)
                {
                    State.FailedStepId = step.StepId;
                    _lastReport = BuildReport(results, false, started);
                    return validation;
                }

                // Idempotency guard
                if (step.CanSkip(context))
                {
                    State.SkippedStepIds.Add(step.StepId);
                    results.Add(new SetupStepResult
                    {
                        StepId = step.StepId, StepName = step.StepName,
                        Succeeded = true, Skipped = true,
                        ExecutedAt = DateTimeOffset.UtcNow
                    });
                    continue;
                }

                var sw = System.Diagnostics.Stopwatch.StartNew();
                var result = step.Execute(context, progress);
                sw.Stop();

                var stepResult = new SetupStepResult
                {
                    StepId = step.StepId,
                    StepName = step.StepName,
                    Succeeded = result.Flag == Errors.Ok,
                    Message = result.Message,
                    Elapsed = sw.Elapsed,
                    ExecutedAt = DateTimeOffset.UtcNow
                };
                results.Add(stepResult);

                if (result.Flag != Errors.Ok)
                {
                    State.FailedStepId = step.StepId;
                    _lastReport = BuildReport(results, false, started);
                    return result;
                }

                State.CompletedStepIds.Add(step.StepId);
                State.LastUpdatedAt = DateTimeOffset.UtcNow;
                PersistState(context.Options);
            }

            _lastReport = BuildReport(results, true, started);
            return new ErrorsInfo { Flag = Errors.Ok, Message = "Setup completed successfully." };
        }

        public IErrorsInfo Resume(SetupContext context, IProgress<PassedArgs> progress = null)
            => Run(context, progress); // Run already skips completed steps

        public SetupReport GetReport() => _lastReport;

        private SetupReport BuildReport(List<SetupStepResult> results, bool succeeded, DateTimeOffset started)
        {
            var finished = DateTimeOffset.UtcNow;
            var json = System.Text.Json.JsonSerializer.Serialize(results);
            var hash = Convert.ToHexString(
                System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(json)));
            return new SetupReport
            {
                WizardId = _wizardId,
                Succeeded = succeeded,
                StepResults = results.AsReadOnly(),
                StartedAt = started,
                FinishedAt = finished,
                ContentHash = hash
            };
        }

        private void PersistState(SetupOptions opts)
        {
            if (string.IsNullOrEmpty(opts?.StateFilePath)) return;
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(State);
                System.IO.File.WriteAllText(opts.StateFilePath, json);
            }
            catch { /* state persistence is best-effort */ }
        }
    }
}
```

---

## File Layout

```
DataManagementEngineStandard/
  SetUp/
    ISetupStep.cs
    ISetupWizard.cs
    ISetupWizardAdapter.cs         (placeholder, implemented Phase 5)
    ISetupProgressReporter.cs
    SetupContext.cs
    SetupOptions.cs
    SetupState.cs
    SetupReport.cs
    SetupWizard.cs
    SetupWizardBuilder.cs
    Plans/
      00-overview-setup-wizard-gap-matrix.md
      01-phase1-contract-and-wizard-architecture.md
      ...
```

---

## Dependencies

| Dependency | Source |
|---|---|
| `IDMEEditor` | `DataManagementModelsStandard/Editor/IDMEEditor.cs` |
| `IDataSource` | `DataManagementModelsStandard/IDataSource.cs` |
| `ConnectionProperties` | `DataManagementModelsStandard/ConfigUtil/ConnectionProperties.cs` |
| `IErrorsInfo` / `ErrorsInfo` | `DataManagementModelsStandard/` |
| `PassedArgs` | `DataManagementModelsStandard/StreamandEvents/` |
| `MigrationPlanArtifact` | `DataManagementEngineStandard/Editor/Migration/IMigrationManager.cs` |
| `MigrationExecutionResult` | same as above |

---

## Validation and Safety Rules

1. `ISetupStep.Execute` must never throw; return `Errors.Failed` with a descriptive message instead.
2. `SetupContext` must have a non-null `Editor` before any step runs; validate in `SetupWizard.Run`.
3. Steps must declare all `DependsOn` IDs that must complete first; the builder validates this graph is acyclic.
4. `SetupState` persistence is best-effort — a missing or corrupt state file triggers a clean run, not an error.
5. `SetupReport.ContentHash` must be SHA-256 of the serialized `StepResults`; recompute on deserialization if used for tamper detection.

---

## Testing Approach

- Test `SetupWizard.Run` with a list of stub `ISetupStep` implementations.
- Verify skipped steps are recorded in `State.SkippedStepIds`.
- Verify a failed step populates `State.FailedStepId` and stops the run.
- Verify `SetupReport.ContentHash` changes when any step result changes.
- Use an in-memory fake `ISetupProgressReporter` to verify events are raised.

---

## Acceptance Criteria

- [ ] All types listed above exist in `DataManagementEngineStandard/SetUp/`.
- [ ] `SetupWizardBuilder.Build()` produces a runnable `ISetupWizard`.
- [ ] A wizard with 3 stub steps runs, checkpoints, and produces a `SetupReport` with correct `ContentHash`.
- [ ] Resuming a wizard with step 2 already completed skips step 2 and executes only step 3.
- [ ] All new files compile with no warnings (treat warnings as errors for this folder).
