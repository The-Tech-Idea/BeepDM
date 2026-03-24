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
