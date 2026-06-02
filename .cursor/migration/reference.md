# Migration Quick Reference

## 1) Plan
```csharp
var migration = new MigrationManager(editor, dataSource);
var types = new[] { typeof(Customer), typeof(Order) };
var plan = migration.BuildMigrationPlanForTypes(types, detectRelationships: true);
```

## 2) Policy + Safety Reports
```csharp
var policy = migration.EvaluateMigrationPlanPolicy(plan, new MigrationPolicyOptions
{
    EnvironmentTier = MigrationEnvironmentTier.Staging
});
var dryRun = migration.GenerateDryRunReport(plan);
var preflight = migration.RunPreflightChecks(plan);
var impact = migration.BuildImpactReport(plan);
var performance = migration.BuildPerformancePlan(plan);
```

## 3) CI and Artifacts
```csharp
var ci = migration.ValidatePlanForCi(plan);
if (!ci.CanMerge)
    throw new InvalidOperationException("CI migration gates failed.");

var artifacts = migration.ExportMigrationArtifacts(plan, ci);
```

## 4) Rollout Governance
```csharp
var governance = migration.EvaluateRolloutGovernance(plan, new MigrationRolloutGovernanceRequest
{
    Wave = MigrationRolloutWave.Wave2StandardProduction,
    IsCriticalDataSource = false,
    ReviewedBy = "release-manager"
});
if (!governance.CanPromote)
    throw new InvalidOperationException("Governance blocked promotion.");
```

## 5) Execute / Resume
```csharp
var exec = migration.ExecuteMigrationPlan(plan, new MigrationExecutionPolicy
{
    MaxTransientRetries = 3,
    RetryDelayMilliseconds = 500
});

if (!exec.Success && !string.IsNullOrWhiteSpace(exec.ExecutionToken))
{
    var resumed = migration.ResumeMigrationPlan(exec.ExecutionToken);
}
```

## 6) Rollback / Compensation
```csharp
var compensation = migration.BuildCompensationPlan(plan);
var readiness = migration.CheckRollbackReadiness(plan, true, true, "restore-test-id-001");
if (!readiness.IsReady)
    throw new InvalidOperationException("Rollback readiness failed.");

var rollback = migration.RollbackFailedExecution(exec.ExecutionToken, dryRun: true);
```

## 7) Telemetry / Diagnostics / Audit
```csharp
var telemetry = migration.GetMigrationTelemetrySnapshot(exec.ExecutionToken);
var diagnostics = migration.GetMigrationDiagnostics(exec.ExecutionToken, MigrationDiagnosticSeverity.Warning);
var audits = migration.GetMigrationAuditEvents(exec.ExecutionToken);
```

## Discovery-Based Path (When Entity Set Is Dynamic)
```csharp
migration.RegisterAssembly(typeof(Customer).Assembly);
var summary = migration.GetMigrationSummary("MyApp.Entities");
var apply = migration.ApplyMigrations("MyApp.Entities", null, true, true, null);
```

## ClassCreator-Driven Type Discovery

The discovery pipeline uses `IClassCreator` to recognise EF Core POCOs and
plain POCOs alongside `Entity`/`IEntity` types. Add new discoverable type
kinds by:

1. Implementing the recognition method on the appropriate helper in
   `DataManagementEngineStandard/Tools/Helpers/`.
2. Adding the public method to `IClassCreator`.
3. Adding a delegation in `ClassCreator.PocoToEntity.cs`.

```csharp
// ClassCreator.PocoToEntity.cs (existing)
public bool IsEfDecoratedType(Type type)
    => _efCoreToEntityHelper.IsEfDecoratedType(type);

public bool IsDiscoverablePoco(Type type)
    => _pocoToEntityHelper.IsDiscoverablePoco(type);
```

## Foreign Keys and Indexes (Opt-In)

FK and Index generation default to **off** so pre-existing callers see no behavior change.
Pass the opt-in flags explicitly when you want the planner to emit the operations.

```csharp
var plan = migration.BuildMigrationPlanForTypes(
    types,
    detectRelationships: true,
    applyForeignKeys: true,   // emits AddForeignKey / DropForeignKey operations
    applyIndexes: true);      // emits CreateIndex / DropIndex operations
```

The same flags exist on:
- `EnsureDatabaseCreatedForTypes(types, ..., applyForeignKeys, applyIndexes)`
- `ApplyMigrationsForTypes(types, ..., applyForeignKeys, applyIndexes)`
- `BuildMigrationPlan(model, applyForeignKeys, applyIndexes)` and the variant that takes an explicit `MigrationModel`
- `ApplyMigrationsForModel(model, applyForeignKeys, applyIndexes)`
- `EnsureDatabaseCreatedForModel(model, applyForeignKeys, applyIndexes)`
- `BuildMigrationPlanForModel(...)` honors FK/Index flags through the model-interop cache

Generated operations carry the constraint or index name on `MigrationPlanOperation.TargetName`:
```csharp
foreach (var op in plan.Operations.Where(o => o.TargetName != null))
    Console.WriteLine($"{op.Kind} target={op.TargetName} on {op.EntityName}");
```

## Provider Capability for FK / Index

Check whether the active provider can actually emit the DDL:
```csharp
var profile = migration.GetProviderCapabilityProfile(plan);
if (!profile.SupportsForeignKeys)
    // provider is SQL/no-SQL hybrid or file-backed - skip FK ops or pick a fallback
if (!profile.SupportsIndexes)
    // provider cannot create indexes - do not emit CreateIndex ops
```

Policy blocks FK/Index ops whose provider reports `!Supports*` and emits a
`provider-fallback-missing-foreign-key` or `provider-fallback-missing-index` rule.

## Rollback Drops FK and Index

`RollbackFailedExecution(token, dryRun: false)` will issue
`ALTER TABLE ... DROP CONSTRAINT <name>` for `ReversibleDdl` FK ops and
`DROP INDEX <name>` for `ReversibleDdl` Index ops when the rollback has the
target name. The names come from `MigrationPlanOperation.TargetName`, threaded
through `MigrationExecutionStep.TargetName` and `MigrationCompensationAction.TargetName`.

## Telemetry Per Operation Kind

`MigrationTelemetryMetrics.OperationKindCounts` and `OperationKindFailureCounts`
break completion and failure counts down by `MigrationPlanOperationKind` so you
can see whether `AddForeignKey` and `CreateIndex` are succeeding as often as
`CreateTable`. Updated automatically by the executor; read via
`GetMigrationTelemetrySnapshot(executionToken)`.

## Migration Summary FK/Index Counts

`MigrationSummary` now surfaces the FK/Index picture directly so a UI does not
have to walk the entity decisions or parse diagnostics strings:

```csharp
var summary = migration.GetMigrationSummary("MyApp.Entities",
    applyForeignKeys: true, applyIndexes: true);
Console.WriteLine($"FKs: {summary.ForeignKeyCount}, Indexes: {summary.IndexCount}");
Console.WriteLine($"Provider supports FK: {summary.ProviderSupportsForeignKeys}");
Console.WriteLine($"Provider supports Index: {summary.ProviderSupportsIndexes}");
```

The counts are only populated when the corresponding opt-in flag is set.
When the flag is set but the surveyed entities have no relations or indexes,
a diagnostic is added (`applyForeignKeys=true: no relations found...`).
When the provider does not support the operation, a different diagnostic is
added so the caller can decide whether to target a different provider or
strip the flag.

## Constraint/Index Names on Every Artifact

The constraint or index name now flows through every artifact in the chain so
you can correlate across plan, step, dry-run, impact, perf, summary, and
approval reports:

```csharp
// Plan op carries the name
foreach (var op in plan.Operations.Where(o => !string.IsNullOrEmpty(o.TargetName)))
    Console.WriteLine($"{op.Kind} {op.EntityName}.{op.TargetName}");

// Dry-run entry carries the name and DDL preview
foreach (var dro in plan.DryRunReport.Operations.Where(o => !string.IsNullOrEmpty(o.TargetName)))
    Console.WriteLine($"{dro.Kind} {dro.EntityName}.{dro.TargetName} -> {string.Join("; ", dro.DdlPreview)}");

// Impact entry carries the name
foreach (var entry in plan.ImpactReport.Entries.Where(e => !string.IsNullOrEmpty(e.TargetName)))
    Console.WriteLine($"{entry.Kind} {entry.EntityName}.{entry.TargetName} sensitivity={entry.Sensitivity}");

// Performance annotation carries the name + risk level
foreach (var ann in plan.PerformancePlan.OperationAnnotations.Where(a => !string.IsNullOrEmpty(a.TargetName)))
    Console.WriteLine($"{ann.OperationKind} {ann.EntityName}.{ann.TargetName} score={ann.EstimatedLockImpactScore} risk={ann.RiskLevel}");

// Approval report shows a per-kind count summary at the top
var bundle = migration.ExportMigrationArtifacts(plan, ciReport);
Console.WriteLine(bundle.ApprovalReportMarkdown);
// "## Operation Counts\n- AddForeignKey=3, CreateIndex=5, CreateEntity=12, ..."
```
