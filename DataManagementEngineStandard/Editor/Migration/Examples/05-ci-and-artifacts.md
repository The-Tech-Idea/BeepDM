# Example 5: CI Validation and Artifact Export

Run CI checks and publish evidence bundle.

```csharp
var ciReport = migrationManager.ValidatePlanForCi(plan, new MigrationPolicyOptions
{
    EnvironmentTier = MigrationEnvironmentTier.Test
});

if (!ciReport.CanMerge)
    throw new InvalidOperationException("CI migration gates failed.");

var artifacts = migrationManager.ExportMigrationArtifacts(plan, ciReport);

File.WriteAllText("migration-plan.json", artifacts.PlanJson);
File.WriteAllText("migration-dryrun.json", artifacts.DryRunJson);
File.WriteAllText("migration-ci-report.json", artifacts.CiValidationJson);
File.WriteAllText("migration-approval.md", artifacts.ApprovalReportMarkdown);
```
