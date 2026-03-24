using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.Migration
{
    public partial class MigrationManager
    {
        public MigrationDryRunReport GenerateDryRunReport(MigrationPlanArtifact plan)
        {
            var report = new MigrationDryRunReport
            {
                GeneratedOnUtc = DateTime.UtcNow,
                PlanId = plan?.PlanId ?? string.Empty,
                PlanHash = plan?.PlanHash ?? string.Empty
            };

            if (plan == null)
            {
                report.HasBlockingIssues = true;
                report.Diagnostics.Add("Cannot generate dry-run report for a null plan.");
                return report;
            }

            var helper = ResolveHelper(plan.DataSourceType);
            foreach (var operation in plan.Operations)
            {
                if (operation == null)
                    continue;

                var dryRunOperation = new MigrationDryRunOperation
                {
                    EntityName = operation.EntityName,
                    Kind = operation.Kind,
                    RiskLevel = operation.RiskLevel
                };

                AddRiskTags(operation, dryRunOperation);
                AddDdlPreview(helper, operation, dryRunOperation);

                if (operation.Kind == MigrationPlanOperationKind.Error)
                {
                    dryRunOperation.Diagnostics.Add("Plan operation is already in error state.");
                    report.HasBlockingIssues = true;
                }

                report.Operations.Add(dryRunOperation);
            }

            return report;
        }

        public MigrationPreflightReport RunPreflightChecks(MigrationPlanArtifact plan, MigrationPolicyOptions options = null)
        {
            var report = new MigrationPreflightReport
            {
                CheckedOnUtc = DateTime.UtcNow,
                PlanId = plan?.PlanId ?? string.Empty,
                PlanHash = plan?.PlanHash ?? string.Empty,
                CanApply = false
            };

            if (plan == null)
            {
                report.Checks.Add(new MigrationPreflightCheck
                {
                    Code = "preflight-plan-null",
                    Decision = MigrationPolicyDecision.Block,
                    Message = "Migration plan is null.",
                    Recommendation = "Build or load a valid plan before running preflight."
                });
                return report;
            }

            // Connection and basic probe
            var connectivityOk = ProbeConnectivity();
            report.Checks.Add(new MigrationPreflightCheck
            {
                Code = "preflight-connectivity",
                Decision = connectivityOk ? MigrationPolicyDecision.Pass : MigrationPolicyDecision.Block,
                Message = connectivityOk ? "Datasource connectivity probe passed." : "Datasource connectivity probe failed.",
                Recommendation = connectivityOk ? "Proceed to policy and drift checks." : "Verify datasource configuration, credentials, and runtime availability."
            });

            // Policy gate
            var policy = EvaluateMigrationPlanPolicy(plan, options);
            report.Checks.Add(new MigrationPreflightCheck
            {
                Code = "preflight-policy",
                Decision = policy.Decision,
                Message = $"Policy decision is '{policy.Decision}'.",
                Recommendation = policy.Decision == MigrationPolicyDecision.Block
                    ? "Resolve blocking policy findings before apply."
                    : "Review warnings and approvals before apply."
            });

            // Schema drift gate
            var driftDetected = DetectSchemaDrift(plan);
            report.SchemaDriftDetected = driftDetected;
            report.Checks.Add(new MigrationPreflightCheck
            {
                Code = "preflight-schema-drift",
                Decision = driftDetected ? MigrationPolicyDecision.Block : MigrationPolicyDecision.Pass,
                Message = driftDetected
                    ? "Schema drift detected since plan creation."
                    : "No material schema drift detected since plan creation.",
                Recommendation = driftDetected
                    ? "Regenerate plan and re-run approvals/policy checks."
                    : "Current plan remains aligned with observed schema state."
            });

            // Operational lock/session estimate heuristic
            var heavyOps = plan.Operations.Count(operation =>
                operation != null &&
                (operation.Kind == MigrationPlanOperationKind.AddMissingColumns ||
                 operation.Kind == MigrationPlanOperationKind.AlterColumn ||
                 operation.Kind == MigrationPlanOperationKind.DropColumn ||
                 operation.Kind == MigrationPlanOperationKind.DropEntity));
            var lockDecision = heavyOps >= 5 || plan.ProviderCapabilities.RequiresOfflineWindowForSchemaChanges
                ? MigrationPolicyDecision.Warn
                : MigrationPolicyDecision.Pass;
            report.Checks.Add(new MigrationPreflightCheck
            {
                Code = "preflight-lock-impact-estimate",
                Decision = lockDecision,
                Message = lockDecision == MigrationPolicyDecision.Warn
                    ? "Estimated lock/session impact is elevated for this plan."
                    : "Estimated lock/session impact is low.",
                Recommendation = lockDecision == MigrationPolicyDecision.Warn
                    ? "Schedule maintenance window or phased rollout for high-impact operations."
                    : "Standard rollout window is likely sufficient."
            });

            plan.PerformancePlan ??= BuildPerformancePlan(plan);
            var requiresMaintenance = plan.PerformancePlan.OperationAnnotations.Any(annotation =>
                annotation.WindowMode == MigrationExecutionWindowMode.MaintenanceWindowRequired);
            report.Checks.Add(new MigrationPreflightCheck
            {
                Code = "preflight-performance-window",
                Decision = requiresMaintenance ? MigrationPolicyDecision.Warn : MigrationPolicyDecision.Pass,
                Message = requiresMaintenance
                    ? "Performance plan indicates maintenance-window operations."
                    : "Performance plan indicates online-preferred execution.",
                Recommendation = requiresMaintenance
                    ? "Schedule maintenance window and enable throttled execution."
                    : "Proceed with standard execution window and monitoring."
            });

            report.CanApply = report.Checks.All(check => check.Decision != MigrationPolicyDecision.Block);
            return report;
        }

        public MigrationImpactReport BuildImpactReport(MigrationPlanArtifact plan)
        {
            var report = new MigrationImpactReport
            {
                GeneratedOnUtc = DateTime.UtcNow,
                PlanId = plan?.PlanId ?? string.Empty,
                PlanHash = plan?.PlanHash ?? string.Empty
            };

            if (plan == null)
                return report;

            foreach (var operation in plan.Operations)
            {
                if (operation == null)
                    continue;

                var entry = new MigrationImpactEntry
                {
                    EntityName = operation.EntityName,
                    Kind = operation.Kind,
                    Sensitivity = ClassifyImpactSensitivity(operation)
                };

                entry.UsageHints.AddRange(GetImpactUsageHints(operation));
                entry.DataVolumeIndicators.AddRange(GetImpactDataVolumeIndicators(operation, plan.ProviderCapabilities));
                report.Entries.Add(entry);
            }

            return report;
        }

        private bool ProbeConnectivity()
        {
            if (MigrateDataSource == null)
                return false;

            try
            {
                MigrateDataSource.CheckEntityExist("__beep_preflight_probe__");
                if (MigrateDataSource.ErrorObject == null)
                    return true;

                return !string.Equals(
                    MigrateDataSource.ErrorObject.Flag.ToString(),
                    "Failed",
                    StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private bool DetectSchemaDrift(MigrationPlanArtifact plan)
        {
            var types = plan.Operations
                .Where(operation => operation != null && !string.IsNullOrWhiteSpace(operation.EntityTypeName))
                .Select(operation => ResolveType(operation.EntityTypeName))
                .Where(type => type != null)
                .Distinct()
                .ToList();

            if (types.Count == 0)
                return false;

            var currentPlan = BuildMigrationPlanForTypes(types);
            if (currentPlan == null)
                return true;

            var currentSignature = BuildOperationSignature(currentPlan.Operations);
            var existingSignature = BuildOperationSignature(plan.Operations);
            return !string.Equals(currentSignature, existingSignature, StringComparison.Ordinal);
        }

        private static string BuildOperationSignature(IEnumerable<MigrationPlanOperation> operations)
        {
            return string.Join("|", operations
                .Where(operation => operation != null)
                .OrderBy(operation => operation.EntityName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(operation => operation.Kind)
                .Select(operation => $"{operation.EntityName}:{operation.Kind}:{operation.MissingColumns.Count}"));
        }

        private Type ResolveType(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
                return null;

            var type = Type.GetType(typeName, throwOnError: false);
            if (type != null)
                return type;

            foreach (var assembly in GetSearchableAssemblies())
            {
                try
                {
                    type = assembly.GetType(typeName, throwOnError: false);
                    if (type != null)
                        return type;
                }
                catch
                {
                    // Ignore type-load failures during fallback resolution.
                }
            }

            return null;
        }

        private void AddRiskTags(MigrationPlanOperation operation, MigrationDryRunOperation dryRun)
        {
            dryRun.RiskTags.Add(operation.RiskLevel.ToString());
            if (operation.IsDestructive)
                dryRun.RiskTags.Add("Destructive");
            if (operation.IsTypeNarrowing)
                dryRun.RiskTags.Add("TypeNarrowing");
            if (operation.HasNullabilityTightening)
                dryRun.RiskTags.Add("NullabilityTightening");
            if (operation.FallbackTasks.Count > 0)
                dryRun.RiskTags.Add("HasFallback");
        }

        private void AddDdlPreview(IDataSourceHelper helper, MigrationPlanOperation operation, MigrationDryRunOperation dryRun)
        {
            if (helper == null)
            {
                dryRun.Diagnostics.Add("No helper available for DDL preview.");
                return;
            }

            if (operation.Kind == MigrationPlanOperationKind.CreateEntity)
            {
                var desired = ResolveDesiredEntityStructure(operation);
                if (desired == null)
                {
                    dryRun.Diagnostics.Add("Could not resolve entity structure for create preview.");
                    return;
                }

                var preview = helper.GenerateCreateTableSql(desired);
                if (!string.IsNullOrWhiteSpace(preview.Sql))
                    dryRun.DdlPreview.Add(preview.Sql);
                else
                    dryRun.Diagnostics.Add(preview.ErrorMessage ?? "Create-table preview unavailable.");
            }
            else if (operation.Kind == MigrationPlanOperationKind.AddMissingColumns)
            {
                var desired = ResolveDesiredEntityStructure(operation);
                if (desired?.Fields == null || desired.Fields.Count == 0)
                {
                    dryRun.Diagnostics.Add("Could not resolve desired fields for add-column preview.");
                    return;
                }

                foreach (var columnName in operation.MissingColumns)
                {
                    var field = desired.Fields.FirstOrDefault(candidate =>
                        candidate != null &&
                        string.Equals(candidate.FieldName, columnName, StringComparison.OrdinalIgnoreCase));
                    if (field == null)
                        continue;

                    var preview = helper.GenerateAddColumnSql(operation.EntityName, field);
                    if (!string.IsNullOrWhiteSpace(preview.Sql))
                        dryRun.DdlPreview.Add(preview.Sql);
                    else if (!string.IsNullOrWhiteSpace(preview.ErrorMessage))
                        dryRun.Diagnostics.Add($"{columnName}: {preview.ErrorMessage}");
                }
            }
        }

        private EntityStructure ResolveDesiredEntityStructure(MigrationPlanOperation operation)
        {
            var entityType = ResolveType(operation.EntityTypeName);
            if (entityType == null)
                return null;

            return TryGetEntityStructure(entityType);
        }

        private static MigrationImpactSensitivity ClassifyImpactSensitivity(MigrationPlanOperation operation)
        {
            if (operation.IsDestructive || operation.RiskLevel == MigrationPlanRiskLevel.Critical)
                return MigrationImpactSensitivity.High;

            if (operation.RiskLevel == MigrationPlanRiskLevel.High || operation.MissingColumns.Count >= 6)
                return MigrationImpactSensitivity.High;

            if (operation.RiskLevel == MigrationPlanRiskLevel.Medium || operation.MissingColumns.Count >= 2)
                return MigrationImpactSensitivity.Medium;

            return MigrationImpactSensitivity.Low;
        }

        private static IEnumerable<string> GetImpactUsageHints(MigrationPlanOperation operation)
        {
            if (operation.Kind == MigrationPlanOperationKind.CreateEntity)
                yield return "New entity introduction may affect downstream reporting and ORM mappings.";

            if (operation.Kind == MigrationPlanOperationKind.AddMissingColumns)
                yield return "Added columns can require application serialization and projection updates.";

            if (operation.IsDestructive)
                yield return "Destructive operation likely impacts dependent queries, views, and integration contracts.";

            if (operation.IsTypeNarrowing || operation.HasNullabilityTightening)
                yield return "Type/nullability tightening may break existing write paths and historical data assumptions.";
        }

        private static IEnumerable<string> GetImpactDataVolumeIndicators(MigrationPlanOperation operation, MigrationProviderCapabilityProfile profile)
        {
            if (operation.MissingColumns.Count >= 8)
                yield return "Large additive schema delta; expect longer metadata and backfill windows.";

            if (profile?.RequiresOfflineWindowForSchemaChanges == true)
                yield return "Provider profile suggests maintenance/offline window for schema operations.";

            if (profile != null && !profile.SupportsTransactionalDdl)
                yield return "Non-transactional DDL profile increases recovery complexity under load.";
        }
    }
}
