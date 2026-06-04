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
                    RiskLevel = operation.RiskLevel,
                    // Surface the constraint/index name on the dry-run entry
                    // so reviewers can match the DDL preview back to a named
                    // constraint without re-walking the EntityStructure.
                    TargetName = operation.TargetName ?? string.Empty
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

            // Operational lock/session estimate heuristic. AddForeignKey and
            // CreateIndex are included because they can hold a SHARE/EXCLUSIVE
            // lock on the referencing table for the duration of the row scan
            // or index build. Without them, a plan with many FK/Index ops
            // would report "low lock/session impact" and skip the maintenance
            // window recommendation.
            var heavyOps = plan.Operations.Count(operation =>
                operation != null &&
                (operation.Kind == MigrationPlanOperationKind.AddMissingColumns ||
                 operation.Kind == MigrationPlanOperationKind.AlterColumn ||
                 operation.Kind == MigrationPlanOperationKind.DropColumn ||
                 operation.Kind == MigrationPlanOperationKind.DropEntity ||
                 operation.Kind == MigrationPlanOperationKind.AddForeignKey ||
                 operation.Kind == MigrationPlanOperationKind.CreateIndex));
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
                    // Surface the constraint/index name for FK/Index ops so
                    // a reviewer can tell which constraint "AddForeignKey on
                    // Orders" refers to without re-walking the EntityStructure.
                    TargetName = operation.TargetName ?? string.Empty,
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

            // Derive the intent flags from the original plan's operation list.
            // Without them the rebuilt plan would omit FK/Index ops and the
            // signature would always diverge from the original, producing a
            // perpetual false-positive drift that blocks every preflight run.
            var afk = plan.Operations.Any(op =>
                op != null && (op.Kind == MigrationPlanOperationKind.AddForeignKey ||
                               op.Kind == MigrationPlanOperationKind.DropForeignKey));
            var aix = plan.Operations.Any(op =>
                op != null && (op.Kind == MigrationPlanOperationKind.CreateIndex ||
                               op.Kind == MigrationPlanOperationKind.DropIndex));
            var currentPlan = BuildMigrationPlanForTypes(types, detectRelationships: true, applyForeignKeys: afk, applyIndexes: aix);
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
                .Select(operation =>
                {
                    // Include the constraint/index name for FK/Index ops so a rename
                    // produces a different signature. Without this, DetectSchemaDrift
                    // would miss a rename of FK_Orders_Customers -> FK_Orders_Customers_v2
                    // because the same EntityName+Kind+ColumnCount would still match.
                    var isRelationalOp = operation.Kind == MigrationPlanOperationKind.AddForeignKey ||
                                         operation.Kind == MigrationPlanOperationKind.DropForeignKey ||
                                         operation.Kind == MigrationPlanOperationKind.CreateIndex ||
                                         operation.Kind == MigrationPlanOperationKind.DropIndex;
                    var target = isRelationalOp ? (operation.TargetName ?? string.Empty) : string.Empty;
                    return $"{operation.EntityName}:{operation.Kind}:{operation.MissingColumns.Count}:{target}";
                }));
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
            else if (operation.Kind == MigrationPlanOperationKind.AddForeignKey)
            {
                var desired = ResolveDesiredEntityStructure(operation);
                if (desired?.Relations == null) return;

                var universalHelper = helper as TheTechIdea.Beep.Helpers.UniversalDataSourceHelpers.RdbmsHelpers.RdbmsHelper;
                // When TargetName is set, preview only the one FK the plan
                // operation targets. Without this, a plan with 5 FK ops on
                // the same entity previews the same 5 SQL lines 5 times,
                // each time under a different operation but with identical
                // DDL — confusing reviewers.
                string targetFk = !string.IsNullOrWhiteSpace(operation.TargetName)
                    ? operation.TargetName.Trim() : null;
                foreach (var fk in BuildForeignKeyDefinitions(desired))
                {
                    if (targetFk != null)
                    {
                        if (!string.Equals(fk.ConstraintName, targetFk, StringComparison.OrdinalIgnoreCase))
                            continue;
                    }

                    if (universalHelper != null)
                    {
                        var (sql, success, err) = universalHelper.GenerateAddForeignKeySql(
                            desired.EntityName,
                            fk.ColumnNames.ToArray(),
                            fk.ReferencedEntityName,
                            fk.ReferencedColumnNames.ToArray(),
                            fk.OnDeleteBehavior,
                            fk.OnUpdateBehavior,
                            fk.ConstraintName);
                        if (!string.IsNullOrWhiteSpace(sql))
                            dryRun.DdlPreview.Add(sql);
                        else if (!string.IsNullOrWhiteSpace(err))
                            dryRun.Diagnostics.Add($"FK '{fk.ConstraintName}': {err}");
                    }
                    else
                    {
                        dryRun.Diagnostics.Add($"FK preview unavailable for helper '{helper.GetType().Name}'.");
                    }
                }
            }
            else if (operation.Kind == MigrationPlanOperationKind.DropForeignKey)
            {
                // Prefer the actual FK name carried in TargetName (the modern
                // path that threads the name through plan op -> step ->
                // compensation action). Fall back to Note for older plans that
                // synthesized the name there.
                var fkName = !string.IsNullOrWhiteSpace(operation.TargetName)
                    ? operation.TargetName
                    : operation.Note;
                if (string.IsNullOrWhiteSpace(fkName))
                {
                    dryRun.Diagnostics.Add("DropForeignKey preview requires the constraint name to be carried in operation.TargetName (or operation.Note for older plans).");
                    return;
                }

                var universalHelper = helper as TheTechIdea.Beep.Helpers.UniversalDataSourceHelpers.RdbmsHelpers.RdbmsHelper;
                if (universalHelper != null)
                {
                    var (sql, success, err) = universalHelper.GenerateDropForeignKeySql(operation.EntityName, fkName);
                    if (!string.IsNullOrWhiteSpace(sql))
                        dryRun.DdlPreview.Add(sql);
                    else if (!string.IsNullOrWhiteSpace(err))
                        dryRun.Diagnostics.Add($"drop FK: {err}");
                }
            }
            else if (operation.Kind == MigrationPlanOperationKind.CreateIndex)
            {
                var desired = ResolveDesiredEntityStructure(operation);
                if (desired?.Indexes == null) return;

                // Same scoping as AddForeignKey: when TargetName is set,
                // preview only the index the plan operation targets.
                string targetIx = !string.IsNullOrWhiteSpace(operation.TargetName)
                    ? operation.TargetName.Trim() : null;
                foreach (var idx in desired.Indexes)
                {
                    if (idx == null || idx.Columns == null || idx.Columns.Count == 0) continue;
                    var name = string.IsNullOrWhiteSpace(idx.Name)
                        ? $"IX_{desired.EntityName}_{string.Join("_", idx.Columns)}"
                        : idx.Name;
                    if (targetIx != null && !string.Equals(name, targetIx, StringComparison.OrdinalIgnoreCase))
                        continue;
                    var (sql, success, err) = helper.GenerateCreateIndexSql(desired.EntityName, name, idx.Columns.ToArray(),
                        idx.Options != null && idx.Options.Count > 0 ? new Dictionary<string, object>(idx.Options) : null);
                    if (!string.IsNullOrWhiteSpace(sql))
                        dryRun.DdlPreview.Add(sql);
                    else if (!string.IsNullOrWhiteSpace(err))
                        dryRun.Diagnostics.Add($"index '{name}': {err}");
                }
            }
            else if (operation.Kind == MigrationPlanOperationKind.DropIndex)
            {
                // Prefer the actual index name carried in TargetName; fall back to
                // Note for older plans that synthesized the name there.
                var indexName = !string.IsNullOrWhiteSpace(operation.TargetName)
                    ? operation.TargetName
                    : operation.Note;
                if (string.IsNullOrWhiteSpace(indexName))
                {
                    dryRun.Diagnostics.Add("DropIndex preview requires the index name to be carried in operation.TargetName (or operation.Note for older plans).");
                    return;
                }

                var universalHelper = helper as TheTechIdea.Beep.Helpers.UniversalDataSourceHelpers.RdbmsHelpers.RdbmsHelper;
                if (universalHelper != null)
                {
                    var (sql, success, err) = universalHelper.GenerateDropIndexSql(operation.EntityName, indexName);
                    if (!string.IsNullOrWhiteSpace(sql))
                        dryRun.DdlPreview.Add(sql);
                    else if (!string.IsNullOrWhiteSpace(err))
                        dryRun.Diagnostics.Add($"drop index: {err}");
                }
                else
                {
                    dryRun.DdlPreview.Add($"DROP INDEX {indexName} ON {operation.EntityName}; -- helper-specific");
                }
            }
        }

        private EntityStructure ResolveDesiredEntityStructure(MigrationPlanOperation operation)
        {
            var entityType = ResolveType(operation.EntityTypeName);
            if (entityType != null)
                return TryGetEntityStructure(entityType);

            return ResolveCachedEntityStructure(operation.EntityTypeName, operation.EntityName);
        }

        private static MigrationImpactSensitivity ClassifyImpactSensitivity(MigrationPlanOperation operation)
        {
            if (operation.IsDestructive || operation.RiskLevel == MigrationPlanRiskLevel.Critical)
                return MigrationImpactSensitivity.High;

            if (operation.RiskLevel == MigrationPlanRiskLevel.High || operation.MissingColumns.Count >= 6)
                return MigrationImpactSensitivity.High;

            // FK/Index ops are RiskLevel.Low by default but still touch the
            // relational integrity surface. AddForeignKey validates every
            // existing row against the referenced PK; CreateIndex can lock
            // a large table for a long time. Surface them as Medium so the
            // operator review doesn't classify a 12-AddForeignKey plan as
            // "low impact" by default.
            var isRelationalOp = operation.Kind == MigrationPlanOperationKind.AddForeignKey ||
                                 operation.Kind == MigrationPlanOperationKind.CreateIndex ||
                                 operation.Kind == MigrationPlanOperationKind.DropForeignKey ||
                                 operation.Kind == MigrationPlanOperationKind.DropIndex;

            if (operation.RiskLevel == MigrationPlanRiskLevel.Medium ||
                operation.MissingColumns.Count >= 2 ||
                isRelationalOp)
                return MigrationImpactSensitivity.Medium;

            return MigrationImpactSensitivity.Low;
        }

        private static IEnumerable<string> GetImpactUsageHints(MigrationPlanOperation operation)
        {
            if (operation.Kind == MigrationPlanOperationKind.CreateEntity)
                yield return "New entity introduction may affect downstream reporting and ORM mappings.";

            if (operation.Kind == MigrationPlanOperationKind.AddMissingColumns)
                yield return "Added columns can require application serialization and projection updates.";

            if (operation.Kind == MigrationPlanOperationKind.AddForeignKey)
                yield return "New foreign keys may affect insert/update ordering and require data validation before constraint enforcement.";

            if (operation.Kind == MigrationPlanOperationKind.DropForeignKey)
                yield return "Dropping foreign keys can orphan dependent rows; verify referential integrity is enforced at the application layer.";

            if (operation.Kind == MigrationPlanOperationKind.CreateIndex)
                yield return "Index creation locks the affected table on most providers; consider CONCURRENTLY or scheduled windows for large tables.";

            if (operation.Kind == MigrationPlanOperationKind.DropIndex)
                yield return "Dropping indexes can regress query performance; capture EXPLAIN plans before and after.";

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

            // FK/Index impact scales with row count, not with the op itself.
            // Operators reading the impact report should not have to remember
            // that AddForeignKey does a full table scan on the referencing side.
            if (operation.Kind == MigrationPlanOperationKind.AddForeignKey)
                yield return "AddForeignKey validates every existing row in the referencing table against the referenced key; expect O(N) scan time on large tables.";

            if (operation.Kind == MigrationPlanOperationKind.CreateIndex)
                yield return "CreateIndex rebuilds or populates the index from existing rows; expect long lock window or background-rebuild time on large tables.";

            if (operation.Kind == MigrationPlanOperationKind.DropIndex)
                yield return "DropIndex invalidates dependent query plans; capture EXPLAIN baselines before execution to validate post-drop performance.";

            if (operation.Kind == MigrationPlanOperationKind.DropForeignKey)
                yield return "DropForeignKey can orphan dependent rows; ensure referential integrity is enforced at the application layer before the drop.";

            if (profile != null && !profile.SupportsIndexes && (
                operation.Kind == MigrationPlanOperationKind.CreateIndex ||
                operation.Kind == MigrationPlanOperationKind.DropIndex))
                yield return "Provider does not advertise index DDL support; this op will likely be a no-op or require a manual fallback.";

            if (profile != null && !profile.SupportsForeignKeys && (
                operation.Kind == MigrationPlanOperationKind.AddForeignKey ||
                operation.Kind == MigrationPlanOperationKind.DropForeignKey))
                yield return "Provider does not advertise foreign-key DDL support; this op will likely be a no-op or require a manual fallback.";
        }
    }
}
