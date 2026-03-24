# Example 2: Dry-Run, Preflight, and Impact

Generate safety evidence before approval/execution.

```csharp
var dryRun = migrationManager.GenerateDryRunReport(plan);
var preflight = migrationManager.RunPreflightChecks(plan);
var impact = migrationManager.BuildImpactReport(plan);
var performance = migrationManager.BuildPerformancePlan(plan);

if (dryRun.HasBlockingIssues)
    throw new InvalidOperationException("Dry-run has blocking issues.");

if (!preflight.CanApply)
    throw new InvalidOperationException("Preflight checks did not pass.");

Console.WriteLine($"Dry-run operations: {dryRun.Operations.Count}");
Console.WriteLine($"Impact entries: {impact.Entries.Count}");
Console.WriteLine($"Estimated window (min): {performance.Kpis.PlannedMigrationWindowMinutes}");
```
