# Phase 3 — Schema Creation, DDL, and Migration Step

## Objective

Implement `SchemaSetupStep`, the wizard step that creates all required tables, columns, and indexes for a given entity list by driving the full `MigrationManager` plan → policy → dry-run → preflight → approve → execute pipeline.

This step is datasource-agnostic: it calls `IDataSource.CreateEntityAs()` and `MigrationManager` only; no raw SQL is generated in wizard code.

---

## Scope

- `SchemaSetupStep : ISetupStep`
- `SchemaSetupStepOptions` — entity list, type assembly hints, policy flags
- Full integration with `MigrationManager` phases: Planning → Policy → DryRun → Preflight → Compensation → Approve → Execute
- Schema hash computation for idempotency and upgrade detection
- Checkpoint persistence into `SetupState`

---

## Background: MigrationManager API Used

| Method | Phase purpose |
|---|---|
| `BuildMigrationPlanForTypes(types, opts)` | Build immutable plan artifact from .NET entity types |
| `EvaluateMigrationPlanPolicy(plan, opts)` | Policy gate — block destructive/unsafe changes |
| `GenerateDryRunReport(plan)` | DDL preview; stored in `SetupReport.DryRunReportJson` |
| `RunPreflightChecks(plan)` | Provider-specific health checks before apply |
| `BuildImpactReport(plan)` | Quantify row count / lock impact |
| `BuildCompensationPlan(plan)` | Build rollback artifact before execution |
| `CheckRollbackReadiness(compensationPlan)` | Verify rollback evidence is present |
| `ApproveMigrationPlan(plan, approver, notes)` | Stamp plan as approved (required before Execute) |
| `ExecuteMigrationPlan(plan, checkpoint, progress)` | Apply schema changes with retries + checkpointing |
| `GetMigrationTelemetrySnapshot()` | Observability post-execute |
| `GetMigrationAuditEvents()` | Audit trail for `SetupReport` |

---

## Step Contract

```csharp
namespace TheTechIdea.Beep.SetUp.Steps
{
    public class SchemaSetupStep : ISetupStep
    {
        public string StepId => "schema-setup";
        public string StepName => "Create Database Schema";
        public string Description =>
            "Plans, validates, and applies schema creation for all registered entity types.";
        public IReadOnlyList<string> DependsOn =>
            new[] { "connection-config" }; // must have open DataSource

        private readonly SchemaSetupStepOptions _opts;

        public SchemaSetupStep(SchemaSetupStepOptions options)
        {
            _opts = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Skip if the schema hash recorded in SetupState matches the current entity list hash.
        /// This means schema was already applied and no entities have changed.
        /// </summary>
        public bool CanSkip(SetupContext context)
        {
            if (context.DataSource == null) return false;
            var currentHash = ComputeEntityListHash(_opts.EntityTypes);
            return context.State != null &&
                   context.State.SchemaHash == currentHash;
        }

        public IErrorsInfo Validate(SetupContext context)
        {
            if (context.DataSource == null || context.DataSource.ConnectionStatus != ConnectionState.Open)
                return Fail("DataSource must be open before SchemaSetupStep. " +
                            "Ensure ConnectionConfigStep ran successfully.");
            if (_opts.EntityTypes == null || _opts.EntityTypes.Count == 0)
                return Fail("SchemaSetupStepOptions.EntityTypes must contain at least one type.");
            return Ok();
        }

        public IErrorsInfo Execute(SetupContext context, IProgress<PassedArgs> progress = null)
        {
            var editor = context.Editor;
            var ds = context.DataSource;
            var migration = new MigrationManager(editor, ds);

            // Register extra assemblies if provided
            if (_opts.ExtraAssemblies?.Count > 0)
            {
                foreach (var asm in _opts.ExtraAssemblies)
                    migration.RegisterAssembly(asm);
            }

            // Phase A: Build plan
            Report(progress, 5, "Building migration plan...");
            var planResult = migration.BuildMigrationPlanForTypes(
                _opts.EntityTypes,
                new MigrationPlanOptions
                {
                    IncludeIndexes = _opts.IncludeIndexes,
                    IncludeConstraints = _opts.IncludeConstraints
                });
            if (planResult == null)
                return Fail("BuildMigrationPlanForTypes returned null.");

            context.MigrationPlan = planResult;
            var planJson = System.Text.Json.JsonSerializer.Serialize(planResult);

            // Phase B: Policy evaluation
            Report(progress, 15, "Evaluating migration policy...");
            var policyEval = migration.EvaluateMigrationPlanPolicy(
                planResult, BuildPolicyOptions(context));
            if (policyEval.IsBlocked)
                return Fail("Migration plan blocked by policy: " +
                    string.Join("; ", policyEval.BlockedReasons));

            // Phase C: Dry-run DDL preview
            Report(progress, 25, "Generating dry-run DDL preview...");
            var dryRun = migration.GenerateDryRunReport(planResult);
            if (dryRun != null)
            {
                context.Properties["DryRunReportJson"] =
                    System.Text.Json.JsonSerializer.Serialize(dryRun);
            }

            // If dry-run only mode, stop here
            if (context.Options.DryRun)
            {
                return Ok("Dry-run complete. Schema not modified (DryRun=true).");
            }

            // Phase D: Preflight checks
            Report(progress, 35, "Running preflight checks...");
            var preflight = migration.RunPreflightChecks(planResult);
            if (preflight.HasCriticalFailures)
                return Fail("Preflight failed: " +
                    string.Join("; ", preflight.CriticalFailureMessages));

            // Phase E: Impact report
            Report(progress, 45, "Building impact report...");
            var impact = migration.BuildImpactReport(planResult);
            // Warn on high row-impact operations but do not block
            if (impact?.HighImpactOperationCount > 0)
            {
                editor.Logger?.WriteLog(
                    $"[SchemaSetupStep] {impact.HighImpactOperationCount} high-impact " +
                    $"operations detected. Review impact report before production use.");
            }

            // Phase F: Compensation plan (before execution)
            Report(progress, 55, "Building compensation/rollback plan...");
            var compensation = migration.BuildCompensationPlan(planResult);
            var readiness = migration.CheckRollbackReadiness(compensation);
            if (!readiness.IsReady && context.Options.StrictPolicyMode)
                return Fail("Rollback readiness check failed in StrictPolicyMode.");

            // Phase G: Approve plan
            Report(progress, 65, "Approving migration plan...");
            migration.ApproveMigrationPlan(planResult, approver: "SetupWizard",
                notes: $"Auto-approved by setup wizard. Environment={context.Options.Environment}");

            // Phase H: Execute
            Report(progress, 70, "Executing migration plan...");
            var checkpoint = migration.CreateExecutionCheckpoint(planResult);
            var execResult = migration.ExecuteMigrationPlan(planResult, checkpoint,
                new Progress<PassedArgs>(a =>
                    Report(progress, 70 + (a.ParameterInt1 / 4), a.Messege)));

            if (execResult.Flag != Errors.Ok)
            {
                // Persist checkpoint for resume
                if (context.State != null)
                    context.State.Metadata["LastCheckpointId"] =
                        checkpoint?.CheckpointId ?? string.Empty;
                return Fail("Migration execution failed: " + execResult.Message, execResult.Ex);
            }

            context.MigrationResult = execResult;

            // Phase I: Record schema hash in state
            var hash = ComputeEntityListHash(_opts.EntityTypes);
            if (context.State != null)
            {
                context.State.SchemaHash = hash;
                context.State.Metadata["MigrationPlanId"] = planResult.PlanId ?? string.Empty;
            }

            // Refresh datasource metadata
            ds.GetEntitesList();

            Report(progress, 100, "Schema creation complete.");
            return Ok($"Schema applied. {execResult.AppliedOperationCount} operations executed.");
        }

        // --- helpers ---
        private MigrationPolicyOptions BuildPolicyOptions(SetupContext ctx) =>
            new MigrationPolicyOptions
            {
                Environment = ctx.Options.Environment,
                BlockDestructiveChanges = ctx.Options.StrictPolicyMode ||
                                          ctx.Options.Environment == "Production",
                BlockNullabilityTightening = ctx.Options.StrictPolicyMode
            };

        private static string ComputeEntityListHash(IEnumerable<Type> types)
        {
            var names = string.Join(",", types.Select(t => t.FullName).OrderBy(n => n));
            var bytes = System.Text.Encoding.UTF8.GetBytes(names);
            var hash = System.Security.Cryptography.SHA256.HashData(bytes);
            return Convert.ToHexString(hash);
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

### `SchemaSetupStepOptions`

```csharp
namespace TheTechIdea.Beep.SetUp.Steps
{
    public class SchemaSetupStepOptions
    {
        /// <summary>Entity .NET types to create in the target datasource.</summary>
        public IReadOnlyList<Type> EntityTypes { get; set; }

        /// <summary>Extra assemblies to register in MigrationManager for type discovery.</summary>
        public IReadOnlyList<Assembly> ExtraAssemblies { get; set; }

        /// <summary>Include index creation operations in the migration plan.</summary>
        public bool IncludeIndexes { get; set; } = true;

        /// <summary>Include constraint creation in the migration plan.</summary>
        public bool IncludeConstraints { get; set; } = true;
    }
}
```

---

## Schema Hash and Idempotency

```
Hash = SHA-256( sorted FullName list of all EntityTypes )

CanSkip returns true when:
  SetupState.SchemaHash == current hash

This means:
  - If entity list is unchanged since last run → step skips (schema already applied).
  - If a new entity type is added → hash differs → step re-runs (additive migration only).
  - If production env + BlockDestructiveChanges → policy blocks type-narrowing operations.
```

---

## Execution Phases Summary

```
SchemaSetupStep.Execute
  │
  ├─ A. BuildMigrationPlanForTypes   → MigrationPlanArtifact
  ├─ B. EvaluateMigrationPlanPolicy  → block on unsafe decisions
  ├─ C. GenerateDryRunReport         → DDL preview (stored in context.Properties)
  │       [DryRun=true → STOP here, return Ok]
  ├─ D. RunPreflightChecks           → critical failure check
  ├─ E. BuildImpactReport            → log high-impact warning
  ├─ F. BuildCompensationPlan        → rollback artifact
  │    CheckRollbackReadiness        → fail in StrictPolicyMode
  ├─ G. ApproveMigrationPlan         → auto-approve with wizard context
  ├─ H. ExecuteMigrationPlan         → apply, checkpoint, resume
  └─ I. Update SetupState.SchemaHash + RefreshMetadata
```

---

## Provider-Specific Best Practices

### SQLite
- Always additive: `CREATE TABLE IF NOT EXISTS`, `ALTER TABLE ADD COLUMN`.
- Column drops and renames are not natively supported; wizard emits a warning, not an error.
- Indexes via `CREATE INDEX IF NOT EXISTS`.

### SQL Server
- Use `IF NOT EXISTS` guards in generated DDL.
- Large table column additions (high row count) should warn about lock duration.
- `BlockDestructiveChanges = true` for Production environment.

### PostgreSQL
- Schema prefix (e.g., `public.TableName`) may be needed; validate via `IDataSourceHelper`.
- Case-sensitive quoted identifiers require `IncludeQuotedNames = true` if entities use camelCase.

### Oracle
- Identifier max length: 30 chars (legacy), 128 (18c+). Validate before plan execution.
- Auto-number uses sequences + triggers on older Oracle; plan generation must account for this.
- Prefer additive-only migrations; table rewrites are high-risk.

### MySQL / MariaDB
- `utf8mb4` charset must be set on new tables; validate encoding in connection string.
- `ALTER TABLE` for column-type changes may require table rebuild depending on storage engine.

---

## File Layout

```
DataManagementEngineStandard/
  SetUp/
    Steps/
      SchemaSetupStep.cs
      SchemaSetupStepOptions.cs
```

---

## Dependencies

| Type | Location |
|---|---|
| `MigrationManager` | `DataManagementEngineStandard/Editor/Migration/MigrationManager.cs` |
| `IMigrationManager` | same folder |
| `MigrationPlanArtifact`, `MigrationPlanOptions` | `IMigrationManager.cs` |
| `MigrationPolicyOptions`, `MigrationPolicyEvaluation` | same |
| `MigrationDryRunReport`, `MigrationPreflightReport` | same |
| `MigrationCompensationPlan` | same |
| `MigrationExecutionResult` | same |
| `ISetupStep` | Phase 1 |
| `SetupContext` | Phase 1 |

---

## Testing Approach

> **Driver selection rule:** Tests must NOT hardcode SQLite. Resolve the driver via
> `ConfigEditor.DataDriversClasses` (the same way `CreateLocalDB.razor` populates its dropdown
> and `DatabaseTypeStepControl` lets the user pick a provider). The test suite should be
> parametrized over all registered `IDataSource` drivers so every provider is exercised.

| Test | Description |
|---|---|
| `SchemaSetupStep_NewSchema_CreatesAllTables` | Resolve driver from `ConfigEditor.DataDriversClasses`, open that `IDataSource`, verify all entity types created |
| `SchemaSetupStep_UnchangedEntityList_Skips` | CanSkip returns true after first run for the selected provider |
| `SchemaSetupStep_PolicyBlocked_ReturnsFailed` | Destructive change in Prod → blocked, regardless of provider |
| `SchemaSetupStep_DryRunMode_NoSchemaChanges` | DryRun=true → no tables created on any provider |
| `SchemaSetupStep_PreflightCriticalFailure_Stops` | Preflight fail → Errors.Failed, no execute |
| `SchemaSetupStep_ExecutionFailed_PersistsCheckpoint` | Failed exec → checkpoint in SetupState |
| `SchemaSetupStep_AfterSuccess_EntityMetadataRefreshed` | GetEntitesList() called on datasource after execute |
| `SchemaSetupStep_AllRegisteredDrivers_CreateSchema` | Parametrized test: foreach driver in `ConfigEditor.DataDriversClasses` → schema round-trip succeeds |

---

## Acceptance Criteria

- [ ] `SchemaSetupStep` drives full MigrationManager pipeline (A through I).
- [ ] `DryRun = true` stops after dry-run report without modifying schema.
- [ ] Policy block (destructive change in Production) returns `Errors.Failed` without executing.
- [ ] `SetupState.SchemaHash` is updated on successful execution.
- [ ] `CanSkip` returns `true` when `SetupState.SchemaHash` matches current entity list hash.
- [ ] `GetEntitesList()` is called on the datasource after successful execution.
- [ ] Compensation plan is built before `ExecuteMigrationPlan` is called.
- [ ] Failed execution checkpoint is persisted to `SetupState.Metadata["LastCheckpointId"]`.
