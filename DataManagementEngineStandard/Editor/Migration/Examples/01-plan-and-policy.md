# Example 1: Plan and Policy

Use explicit entity types when schema ownership is known.

```csharp
var entityTypes = new[]
{
    typeof(Customer),
    typeof(Order),
    typeof(OrderItem)
};

var plan = migrationManager.BuildMigrationPlanForTypes(entityTypes, detectRelationships: true);

var policyOptions = new MigrationPolicyOptions
{
    EnvironmentTier = MigrationEnvironmentTier.Staging,
    RequireApprovalForHighRisk = true,
    RequireApprovalForCriticalRisk = true,
    BlockDestructiveInProtectedEnvironments = true
};

var policy = migrationManager.EvaluateMigrationPlanPolicy(plan, policyOptions);

if (policy.Decision == MigrationPolicyDecision.Block)
{
    foreach (var finding in policy.Findings)
        Console.WriteLine($"{finding.RuleId}: {finding.Decision} - {finding.Message}");
    throw new InvalidOperationException("Migration plan blocked by policy.");
}
```
