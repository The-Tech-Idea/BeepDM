# Phase 6 — Pre-flight, Validation, and Rollback

## Objective

Add three production-readiness layers to the setup wizard:

1. **Pre-flight gates** — validate preconditions *before* schema or seeding executes.
2. **Post-execution health checks** — confirm schema and seed data are in the expected state *after* each step.
3. **Wizard-level rollback orchestration** — when a step fails mid-run, undo changes using `MigrationManager` compensation and seeder undo, restoring the system to the last known-good checkpoint.

---

## Scope

- `PreflightValidationStep : ISetupStep`
- `SchemaHealthCheckStep : ISetupStep`
- `SeedHealthCheckStep : ISetupStep`
- `RollbackOrchestrator` — wizard-level compensation coordinator
- `RollbackReport` — outcome of a rollback attempt
- `SetupState.FailedStepId` utilization (declared in Phase 1)
- Integration with `MigrationManager.RollbackFailedExecution` and `BuildCompensationPlan`

---

## Step 1: `PreflightValidationStep`

Runs before schema creation. Validates connectivity, provider capability, required config keys, and disk/network conditions.

```csharp
namespace TheTechIdea.Beep.SetUp.Steps
{
    public class PreflightValidationStep : ISetupStep
    {
        public string StepId => "preflight-validation";
        public string StepName => "Pre-flight Validation";
        public string Description =>
            "Validates connectivity, provider capabilities, and preconditions before schema apply.";
        public IReadOnlyList<string> DependsOn => new[] { "connection-config" };

        private readonly PreflightValidationStepOptions _opts;

        public PreflightValidationStep(PreflightValidationStepOptions opts)
        {
            _opts = opts ?? new PreflightValidationStepOptions();
        }

        public bool CanSkip(SetupContext context) => false; // always run preflight

        public IErrorsInfo Validate(SetupContext context)
        {
            if (context.DataSource == null || context.DataSource.ConnectionStatus != ConnectionState.Open)
                return Fail("DataSource must be open for PreflightValidationStep.");
            return Ok();
        }

        public IErrorsInfo Execute(SetupContext context, IProgress<PassedArgs> progress = null)
        {
            var ds = context.DataSource;
            var editor = context.Editor;
            var failures = new List<string>();

            // A. Connectivity ping
            Report(progress, 10, "Checking datasource connectivity...");
            if (ds.ConnectionStatus != ConnectionState.Open)
                return Fail("Datasource is not open. Cannot proceed with preflight.");

            // B. Provider capability matrix
            Report(progress, 25, "Validating provider capabilities...");
            if (_opts.RequireCreateTable)
            {
                var caps = new MigrationManager(editor, ds).GetProviderCapabilityProfile();
                if (!caps.SupportsCreateTable)
                    failures.Add("Provider does not support CREATE TABLE operations.");
                if (_opts.RequireAlterColumn && !caps.SupportsAlterColumn)
                    failures.Add("Provider does not support ALTER COLUMN operations.");
                if (_opts.RequireIndexCreation && !caps.SupportsCreateIndex)
                    failures.Add("Provider does not support CREATE INDEX operations.");
            }

            // C. Required config keys
            Report(progress, 50, "Checking required configuration...");
            foreach (var key in _opts.RequiredConfigKeys)
            {
                var val = editor.ConfigEditor.GetConfigValue(key);
                if (string.IsNullOrEmpty(val))
                    failures.Add($"Required config key '{key}' is missing.");
            }

            // D. Entity list non-empty
            if (_opts.EntityTypes?.Count == 0)
                failures.Add("No entity types registered for schema creation.");

            // E. Custom validations
            foreach (var customCheck in _opts.CustomChecks)
            {
                var result = customCheck(context);
                if (result.Flag != Errors.Ok)
                    failures.Add(result.Message);
            }

            Report(progress, 90, "Compiling preflight results...");

            if (failures.Count > 0)
            {
                var msg = "Preflight failed:\n" + string.Join("\n", failures.Select(f => $"  • {f}"));
                return Fail(msg);
            }

            Report(progress, 100, "Pre-flight checks passed.");
            return Ok($"All {3 + _opts.RequiredConfigKeys.Count + _opts.CustomChecks.Count} " +
                      "preflight checks passed.");
        }

        private static IErrorsInfo Ok(string msg = "Ok") =>
            new ErrorsInfo { Flag = Errors.Ok, Message = msg };
        private static IErrorsInfo Fail(string msg, Exception ex = null) =>
            new ErrorsInfo { Flag = Errors.Failed, Message = msg, Ex = ex };
        private static void Report(IProgress<PassedArgs> p, int pct, string msg) =>
            p?.Report(new PassedArgs { ParameterInt1 = pct, Messege = msg });
    }

    public class PreflightValidationStepOptions
    {
        public bool RequireCreateTable { get; set; } = true;
        public bool RequireAlterColumn { get; set; } = false;
        public bool RequireIndexCreation { get; set; } = true;
        public IReadOnlyList<Type> EntityTypes { get; set; }
        public IReadOnlyList<string> RequiredConfigKeys { get; set; } = new List<string>();
        public IReadOnlyList<Func<SetupContext, IErrorsInfo>> CustomChecks { get; set; } =
            new List<Func<SetupContext, IErrorsInfo>>();
    }
}
```

---

## Step 2: `SchemaHealthCheckStep`

Runs after `SchemaSetupStep`. Confirms that every entity type in the list corresponds to an existing table/collection in the datasource.

```csharp
namespace TheTechIdea.Beep.SetUp.Steps
{
    public class SchemaHealthCheckStep : ISetupStep
    {
        public string StepId => "schema-health-check";
        public string StepName => "Schema Health Check";
        public string Description =>
            "Verifies all expected entities exist in the datasource after schema creation.";
        public IReadOnlyList<string> DependsOn => new[] { "schema-setup" };

        private readonly IReadOnlyList<Type> _entityTypes;

        public SchemaHealthCheckStep(IReadOnlyList<Type> entityTypes)
        {
            _entityTypes = entityTypes;
        }

        public bool CanSkip(SetupContext ctx) => false;

        public IErrorsInfo Validate(SetupContext ctx)
        {
            if (ctx.DataSource == null) return Fail("DataSource must be set.");
            return Ok();
        }

        public IErrorsInfo Execute(SetupContext ctx, IProgress<PassedArgs> progress = null)
        {
            var ds = ctx.DataSource;
            var missing = new List<string>();
            int total = _entityTypes.Count;

            // Refresh metadata before checking
            ds.GetEntitesList();

            for (int i = 0; i < total; i++)
            {
                var typeName = _entityTypes[i].Name;
                Report(progress, (int)((i + 1) * 100.0 / total),
                    $"Checking entity: {typeName}...");
                if (!ds.CheckEntityExist(typeName))
                    missing.Add(typeName);
            }

            if (missing.Count > 0)
                return Fail($"Schema health check failed. Missing entities: " +
                    string.Join(", ", missing));

            Report(progress, 100, $"Schema health check passed. {total} entities verified.");
            return Ok($"All {total} entities exist in the datasource.");
        }

        private static IErrorsInfo Ok(string msg = "Ok") =>
            new ErrorsInfo { Flag = Errors.Ok, Message = msg };
        private static IErrorsInfo Fail(string msg, Exception ex = null) =>
            new ErrorsInfo { Flag = Errors.Failed, Message = msg, Ex = ex };
        private static void Report(IProgress<PassedArgs> p, int pct, string msg) =>
            p?.Report(new PassedArgs { ParameterInt1 = pct, Messege = msg });
    }
}
```

---

## Step 3: `SeedHealthCheckStep`

Runs after `SeedingStep`. Verifies expected minimum row counts per seeder.

```csharp
namespace TheTechIdea.Beep.SetUp.Steps
{
    public class SeedHealthCheckStep : ISetupStep
    {
        public string StepId => "seed-health-check";
        public string StepName => "Seed Health Check";
        public string Description =>
            "Verifies expected minimum row counts in seeded entities.";
        public IReadOnlyList<string> DependsOn => new[] { "seeding" };

        private readonly IReadOnlyList<SeedHealthCheckRule> _rules;

        public SeedHealthCheckStep(IReadOnlyList<SeedHealthCheckRule> rules)
        {
            _rules = rules;
        }

        public bool CanSkip(SetupContext ctx) => false;

        public IErrorsInfo Validate(SetupContext ctx)
        {
            if (ctx.DataSource == null) return Fail("DataSource must be set.");
            return Ok();
        }

        public IErrorsInfo Execute(SetupContext ctx, IProgress<PassedArgs> progress = null)
        {
            var ds = ctx.DataSource;
            var failures = new List<string>();
            int total = _rules.Count;

            for (int i = 0; i < total; i++)
            {
                var rule = _rules[i];
                Report(progress, (int)((i + 1) * 100.0 / total),
                    $"Checking seed: {rule.EntityName}...");

                var data = ds.GetEntity(rule.EntityName, null) as System.Collections.IEnumerable;
                var count = data?.Cast<object>().Count() ?? 0;

                if (count < rule.MinExpectedRowCount)
                    failures.Add($"'{rule.EntityName}': expected >= {rule.MinExpectedRowCount} " +
                                 $"rows, found {count}.");
            }

            if (failures.Count > 0)
                return Fail("Seed health check failed:\n" +
                    string.Join("\n", failures.Select(f => $"  • {f}")));

            Report(progress, 100, $"Seed health check passed. {total} rules verified.");
            return Ok($"All {total} seed checks passed.");
        }

        private static IErrorsInfo Ok(string msg = "Ok") =>
            new ErrorsInfo { Flag = Errors.Ok, Message = msg };
        private static IErrorsInfo Fail(string msg, Exception ex = null) =>
            new ErrorsInfo { Flag = Errors.Failed, Message = msg, Ex = ex };
        private static void Report(IProgress<PassedArgs> p, int pct, string msg) =>
            p?.Report(new PassedArgs { ParameterInt1 = pct, Messege = msg });
    }

    public class SeedHealthCheckRule
    {
        public string EntityName { get; set; }
        public int MinExpectedRowCount { get; set; } = 1;
    }
}
```

---

## Rollback Orchestrator

When a wizard step fails, `RollbackOrchestrator` drives the compensation sequence using whatever phase-appropriate undo mechanism is available.

```csharp
namespace TheTechIdea.Beep.SetUp
{
    public class RollbackOrchestrator
    {
        private readonly IDMEEditor _editor;
        private readonly IDataSource _dataSource;

        public RollbackOrchestrator(IDMEEditor editor, IDataSource dataSource)
        {
            _editor = editor;
            _dataSource = dataSource;
        }

        /// <summary>
        /// Attempt to roll back wizard changes after a step failure.
        /// Strategy is determined by which steps completed (recorded in SetupState).
        /// </summary>
        public RollbackReport Rollback(SetupState state, SetupContext context)
        {
            var report = new RollbackReport
            {
                FailedStepId = state.FailedStepId,
                AttemptedAt = DateTimeOffset.UtcNow
            };

            var steps = new List<RollbackStepResult>();

            // Roll back schema if it was applied
            if (state.CompletedStepIds.Contains("schema-setup") &&
                context.MigrationPlan != null)
            {
                steps.Add(RollbackSchema(context));
            }

            // Roll back seeding if it was partially or fully applied
            if (state.CompletedSeederIds?.Count > 0 &&
                context.Options?.SkipSeeding == false)
            {
                steps.Add(RollbackSeeds(state, context));
            }

            report.StepResults = steps;
            report.Succeeded = steps.All(s => s.Succeeded);
            return report;
        }

        private RollbackStepResult RollbackSchema(SetupContext context)
        {
            try
            {
                var migration = new MigrationManager(_editor, _dataSource);
                var compensation = migration.BuildCompensationPlan(context.MigrationPlan);
                var readiness = migration.CheckRollbackReadiness(compensation);
                if (!readiness.IsReady)
                {
                    return new RollbackStepResult
                    {
                        StepId = "schema-setup",
                        Succeeded = false,
                        Message = "Rollback readiness check failed; manual intervention required."
                    };
                }
                var result = migration.RollbackFailedExecution(
                    context.MigrationResult, compensation);
                return new RollbackStepResult
                {
                    StepId = "schema-setup",
                    Succeeded = result.Flag == Errors.Ok,
                    Message = result.Message
                };
            }
            catch (Exception ex)
            {
                return new RollbackStepResult
                {
                    StepId = "schema-setup",
                    Succeeded = false,
                    Message = $"Schema rollback threw: {ex.Message}"
                };
            }
        }

        private RollbackStepResult RollbackSeeds(SetupState state, SetupContext context)
        {
            // Seed rollback: delete inserted records for each completed seeder
            // This requires seeders to implement IUndoableSeeder (optional extension)
            // For seeders without undo: log a warning and mark as manual
            return new RollbackStepResult
            {
                StepId = "seeding",
                Succeeded = false,
                Message = "Seed rollback requires manual intervention. " +
                    $"Completed seeders: {string.Join(", ", state.CompletedSeederIds)}. " +
                    "Delete inserted records or restore from backup."
            };
        }
    }

    public class RollbackReport
    {
        public string FailedStepId { get; set; }
        public DateTimeOffset AttemptedAt { get; set; }
        public bool Succeeded { get; set; }
        public IReadOnlyList<RollbackStepResult> StepResults { get; set; }
    }

    public class RollbackStepResult
    {
        public string StepId { get; set; }
        public bool Succeeded { get; set; }
        public string Message { get; set; }
    }
}
```

### Optional: `IUndoableSeeder` for Seed Rollback

```csharp
namespace TheTechIdea.Beep.SetUp.Seeding
{
    /// <summary>
    /// Opt-in interface for seeders that can undo their inserted records.
    /// Implement this when rollback of seeded data is required.
    /// </summary>
    public interface IUndoableSeeder : ISeeder
    {
        /// <summary>Delete all records inserted by this seeder's Seed() call.</summary>
        IErrorsInfo Undo(IDataSource dataSource, IDMEEditor editor);
    }
}
```

---

## Recommended Wizard Step Order (With Validation Gates)

```
PreflightValidationStep          ← Phase 6: gate before any modification
ConnectionConfigStep             ← Phase 2
SchemaSetupStep                  ← Phase 3
SchemaHealthCheckStep            ← Phase 6: verify schema after creation
SeedingStep                      ← Phase 4
SeedHealthCheckStep              ← Phase 6: verify seeds after insertion
```

Rollback triggers automatically when any step between `ConnectionConfigStep` and `SeedHealthCheckStep` returns `Errors.Failed`.

---

## Rollback Strategy Decision Matrix

| Scenario | Rollback Action |
|---|---|
| `ConnectionConfigStep` fails | No schema/data written; remove persisted `ConnectionProperties` |
| `PreflightValidationStep` fails | No changes made; no rollback needed |
| `SchemaSetupStep` fails mid-execution | `MigrationManager.RollbackFailedExecution` + compensation plan |
| `SchemaHealthCheckStep` fails | Drop created tables via `MigrationManager` compensation |
| `SeedingStep` fails mid-run | Partial seed rollback using `IUndoableSeeder.Undo` where available |
| `SeedHealthCheckStep` fails | Warn + log; manual cleanup required |

---

## File Layout

```
DataManagementEngineStandard/
  SetUp/
    Steps/
      PreflightValidationStep.cs
      PreflightValidationStepOptions.cs
      SchemaHealthCheckStep.cs
      SeedHealthCheckStep.cs
      SeedHealthCheckRule.cs
    RollbackOrchestrator.cs
    RollbackReport.cs
    Seeding/
      IUndoableSeeder.cs
```

---

## Testing Approach

| Test | Description |
|---|---|
| `PreflightStep_ProviderLacksCreateTable_Fails` | Capability check → Errors.Failed |
| `PreflightStep_MissingConfigKey_Fails` | RequiredConfigKey absent → Errors.Failed |
| `PreflightStep_CustomCheckFails_Propagates` | Custom lambda failure → step fails |
| `SchemaHealthCheckStep_MissingEntity_Fails` | One entity absent → Errors.Failed |
| `SchemaHealthCheckStep_AllPresent_Passes` | All entities exist → Errors.Ok |
| `SeedHealthCheckStep_RowsBelowMin_Fails` | 0 rows when 1 expected → Errors.Failed |
| `RollbackOrchestrator_SchemaRollback_CallsCompensation` | Verify BuildCompensationPlan + RollbackFailedExecution called |
| `RollbackOrchestrator_SeedRollback_WithUndoableSeeder` | IUndoableSeeder.Undo called for completed seeders |
| `RollbackOrchestrator_SeedRollback_NoUndo_ReturnsManualWarning` | Non-undoable seeder → manual warning |

---

## Acceptance Criteria

- [ ] `PreflightValidationStep` blocks when provider lacks `SupportsCreateTable`.
- [ ] `PreflightValidationStep` blocks when any required config key is missing.
- [ ] `SchemaHealthCheckStep` fails if any expected entity does not exist post-schema-apply.
- [ ] `SeedHealthCheckStep` fails if minimum row count is not met.
- [ ] `RollbackOrchestrator.Rollback` calls `MigrationManager.RollbackFailedExecution` when schema step is in `CompletedStepIds`.
- [ ] `IUndoableSeeder.Undo` is called for each undoable seeder in `CompletedSeederIds`.
- [ ] `RollbackReport` is attached to `SetupReport.RollbackReportJson` after a failed run.
- [ ] All new types compile without warnings.
