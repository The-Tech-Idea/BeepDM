# Example 3: Execute, Checkpoint, Resume

Use execution policy + resumable checkpoints for production safety.

```csharp
var execPolicy = new MigrationExecutionPolicy
{
    MaxTransientRetries = 3,
    RetryDelayMilliseconds = 500,
    RequireOperatorInterventionOnHardFail = true
};

var result = migrationManager.ExecuteMigrationPlan(plan, execPolicy);

if (!result.Success && !string.IsNullOrWhiteSpace(result.ExecutionToken))
{
    var checkpoint = migrationManager.GetExecutionCheckpoint(result.ExecutionToken);
    Console.WriteLine($"Last completed step: {checkpoint?.LastCompletedStep}");

    // after operator action/fix:
    var resumed = migrationManager.ResumeMigrationPlan(result.ExecutionToken, execPolicy);
    Console.WriteLine($"Resumed success: {resumed.Success}");
}
```
