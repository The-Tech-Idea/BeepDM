# Example 4: Compensation and Rollback

Prepare rollback evidence before execution, and run rollback when needed.

```csharp
var compensationPlan = migrationManager.BuildCompensationPlan(plan);

var readiness = migrationManager.CheckRollbackReadiness(
    plan,
    backupConfirmed: true,
    restoreTestEvidenceProvided: true,
    restoreTestEvidence: "Restore test run id: restore-2026-03-16-01");

if (!readiness.IsReady)
    throw new InvalidOperationException("Rollback readiness failed.");

// rollback flow after failed run
var rollbackResult = migrationManager.RollbackFailedExecution(
    executionToken: failedExecutionToken,
    dryRun: true);

Console.WriteLine($"Rollback simulation success: {rollbackResult.Success}");
```
