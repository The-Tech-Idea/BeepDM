# Example 6: Rollout Governance Gates

Promote by wave with KPI and hard-stop policy.

```csharp
var governanceRequest = new MigrationRolloutGovernanceRequest
{
    Wave = MigrationRolloutWave.Wave2StandardProduction,
    IsCriticalDataSource = false,
    ReviewedBy = "release-manager",
    Notes = "Release train 2026.03.16",
    Thresholds = new MigrationRolloutKpiThresholds
    {
        MinSuccessRate = 0.95,
        MaxMeanExecutionDurationMilliseconds = 120000,
        MaxRollbackInvocationRate = 0.10,
        MaxPolicyBlockRatio = 0.25
    },
    HardStopPolicy = new MigrationRolloutHardStopPolicy
    {
        StopOnAnyCriticalDiagnostic = true,
        StopOnAnyRollbackForCriticalWave = true,
        MaxFailureRate = 0.10
    }
};

var governance = migrationManager.EvaluateRolloutGovernance(plan, governanceRequest);

if (!governance.CanPromote)
{
    Console.WriteLine($"Promotion blocked. Hard stop: {governance.HardStopTriggered}, reason: {governance.HardStopReason}");
    foreach (var gate in governance.Gates)
        Console.WriteLine($"{gate.Gate} => {gate.Decision} ({gate.Observed} vs {gate.Threshold})");
}
```
