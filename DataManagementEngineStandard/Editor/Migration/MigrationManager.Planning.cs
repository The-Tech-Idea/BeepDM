using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.Migration
{
    public partial class MigrationManager
    {
        /// <summary>
        /// Builds a non-destructive migration plan using discovery-based entity resolution.
        /// </summary>
        public MigrationPlanArtifact BuildMigrationPlan(string namespaceName = null, Assembly assembly = null, bool detectRelationships = true)
        {
            if (MigrateDataSource == null)
                return CreatePlanArtifactWithError(usesDiscovery: true, "datasource-not-set", "Migration data source is not set", "Configure MigrateDataSource before generating a migration plan.");

            List<Type> entityTypes;
            try
            {
                entityTypes = DiscoverEntityTypes(namespaceName, assembly, includeSubNamespaces: true);
            }
            catch (Exception ex)
            {
                return CreatePlanArtifactWithError(
                    usesDiscovery: true,
                    "plan-discovery-failed",
                    $"Could not discover entity types: {ex.Message}",
                    "Register the required assemblies explicitly or use BuildMigrationPlanForTypes.");
            }

            return BuildMigrationPlanInternal(entityTypes, usesDiscovery: true);
        }

        /// <summary>
        /// Builds a non-destructive migration plan using explicit entity types.
        /// </summary>
        public MigrationPlanArtifact BuildMigrationPlanForTypes(IEnumerable<Type> entityTypes, bool detectRelationships = true)
        {
            if (MigrateDataSource == null)
                return CreatePlanArtifactWithError(usesDiscovery: false, "datasource-not-set", "Migration data source is not set", "Configure MigrateDataSource before generating a migration plan.");

            if (entityTypes == null)
                return CreatePlanArtifactWithError(usesDiscovery: false, "entity-types-null", "Entity types collection cannot be null", "Pass explicit entity types or use discovery-based planning.");

            return BuildMigrationPlanInternal(entityTypes, usesDiscovery: false);
        }

        private MigrationPlanArtifact BuildMigrationPlanInternal(IEnumerable<Type> entityTypes, bool usesDiscovery)
        {
            var plan = CreateBasePlanArtifact(usesDiscovery);
            var typeList = entityTypes
                .Where(type => type != null)
                .Distinct()
                .ToList();

            plan.EntityTypeCount = typeList.Count;

            var readiness = BuildReadinessReport(typeList, usesDiscovery);
            plan.ReadinessIssues.AddRange(readiness.Issues);
            plan.ProviderAssumptions = readiness.MigrationBestPractices
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            plan.ProviderCapabilities = BuildProviderCapabilityProfile(plan.DataSourceType, plan.DataSourceCategory);
            if (!string.IsNullOrWhiteSpace(plan.ProviderCapabilities.PortabilityWarning))
            {
                plan.ReadinessIssues.Add(new MigrationReadinessIssue
                {
                    Code = "provider-portability-warning",
                    Severity = MigrationIssueSeverity.Warning,
                    Message = plan.ProviderCapabilities.PortabilityWarning,
                    Recommendation = "Review provider-specific capability constraints and fallback tasks before execution."
                });
            }

            if (typeList.Count == 0)
            {
                plan.ReadinessIssues.Add(new MigrationReadinessIssue
                {
                    Code = "entity-types-empty",
                    Severity = MigrationIssueSeverity.Warning,
                    Message = "Migration plan generation found zero entity types.",
                    Recommendation = usesDiscovery
                        ? "Register assemblies or use explicit-type planning."
                        : "Pass a non-empty type collection."
                });
            }
            else
            {
                foreach (var entityType in typeList)
                {
                    plan.Operations.Add(BuildPlanOperation(entityType, plan.DataSourceCategory, plan.DataSourceType, plan.ProviderCapabilities));
                }
            }

            plan.PolicyEvaluation = EvaluateMigrationPlanPolicy(plan, CreateDefaultPolicyOptions());
            plan.PlanHash = ComputePlanHash(plan);
            plan.ImpactReport = BuildImpactReport(plan);
            plan.DryRunReport = GenerateDryRunReport(plan);
            plan.PreflightReport = new MigrationPreflightReport
            {
                PlanId = plan.PlanId,
                PlanHash = plan.PlanHash,
                CheckedOnUtc = DateTime.UtcNow,
                CanApply = false
            };
            plan.CompensationPlan = BuildCompensationPlan(plan);
            plan.RollbackReadinessReport = CheckRollbackReadiness(
                plan,
                backupConfirmed: false,
                restoreTestEvidenceProvided: false,
                restoreTestEvidence: "Not provided at planning time.");
            plan.PerformancePlan = BuildPerformancePlan(plan);
            plan.CiValidationReport = ValidatePlanForCi(plan);
            plan.RolloutGovernanceReport = EvaluateRolloutGovernance(plan);
            plan.ExecutionCheckpoint = CreateExecutionCheckpoint(plan);
            RecordPlanCreated(plan);
            TryTrackMigrationPlan(plan, usesDiscovery ? nameof(BuildMigrationPlan) : nameof(BuildMigrationPlanForTypes));
            return plan;
        }

        private MigrationPlanArtifact CreateBasePlanArtifact(bool usesDiscovery)
        {
            return new MigrationPlanArtifact
            {
                PlanId = Guid.NewGuid().ToString("N"),
                CreatedOnUtc = DateTime.UtcNow,
                LifecycleState = MigrationPlanLifecycleState.Draft,
                DataSourceName = MigrateDataSource?.DatasourceName ?? string.Empty,
                DataSourceType = MigrateDataSource?.DatasourceType ?? DataSourceType.Unknown,
                DataSourceCategory = MigrateDataSource?.Category ?? DatasourceCategory.NONE,
                UsesDiscovery = usesDiscovery
            };
        }

        private MigrationPlanArtifact CreatePlanArtifactWithError(bool usesDiscovery, string code, string message, string recommendation)
        {
            var artifact = CreateBasePlanArtifact(usesDiscovery);
            artifact.ReadinessIssues.Add(new MigrationReadinessIssue
            {
                Code = code,
                Severity = MigrationIssueSeverity.Error,
                Message = message,
                Recommendation = recommendation
            });
            artifact.PlanHash = ComputePlanHash(artifact);
            return artifact;
        }

        private MigrationPlanOperation BuildPlanOperation(Type entityType, DatasourceCategory category, DataSourceType type, MigrationProviderCapabilityProfile providerProfile)
        {
            var operation = new MigrationPlanOperation
            {
                EntityTypeName = entityType.FullName ?? entityType.Name
            };

            try
            {
                var desired = TryGetEntityStructure(entityType);
                var entityName = GetEntityName(entityType, desired);
                operation.EntityName = entityName;

                if (desired == null)
                {
                    operation.Kind = MigrationPlanOperationKind.Error;
                    operation.RiskLevel = MigrationPlanRiskLevel.High;
                    operation.Note = $"Could not convert '{entityType.Name}' to EntityStructure.";
                    operation.ProviderAssumptions.Add("Verify classCreator availability and entity metadata conversion before apply.");
                    return operation;
                }

                if (string.IsNullOrWhiteSpace(entityName))
                {
                    operation.Kind = MigrationPlanOperationKind.Error;
                    operation.RiskLevel = MigrationPlanRiskLevel.High;
                    operation.Note = $"Entity name resolution failed for '{entityType.Name}'.";
                    operation.ProviderAssumptions.Add("Add a stable class name or [Table] attribute.");
                    return operation;
                }

                var exists = MigrateDataSource.CheckEntityExist(entityName);
                if (!exists)
                {
                    operation.Kind = MigrationPlanOperationKind.CreateEntity;
                    operation.RiskLevel = ClassifyCreateEntityRisk(category, type);
                    operation.Note = $"Entity '{entityName}' does not exist and will be created.";
                    operation.ProviderAssumptions.AddRange(GetOperationAssumptions(category, type, operation.Kind));
                    operation.FallbackTasks.AddRange(GetFallbackTasks(operation.Kind, entityName, providerProfile));
                    return operation;
                }

                var current = MigrateDataSource.GetEntityStructure(entityName, true);
                if (current == null)
                {
                    operation.Kind = MigrationPlanOperationKind.Error;
                    operation.RiskLevel = MigrationPlanRiskLevel.High;
                    operation.Note = $"Current entity structure could not be read for '{entityName}'.";
                    operation.ProviderAssumptions.Add("Validate datasource connectivity and metadata permissions before apply.");
                    return operation;
                }

                var missingColumns = GetMissingColumns(current, desired);
                if (missingColumns.Count > 0)
                {
                    operation.Kind = MigrationPlanOperationKind.AddMissingColumns;
                    operation.MissingColumns = missingColumns
                        .Where(column => column != null && !string.IsNullOrWhiteSpace(column.FieldName))
                        .Select(column => column.FieldName)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(columnName => columnName)
                        .ToList();
                    operation.RiskLevel = ClassifyAddColumnsRisk(category, type, operation.MissingColumns.Count);
                    operation.Note = $"Entity '{entityName}' requires {operation.MissingColumns.Count} missing column(s).";
                    operation.ProviderAssumptions.AddRange(GetOperationAssumptions(category, type, operation.Kind));
                    operation.FallbackTasks.AddRange(GetFallbackTasks(operation.Kind, entityName, providerProfile));
                }
                else
                {
                    operation.Kind = MigrationPlanOperationKind.UpToDate;
                    operation.RiskLevel = MigrationPlanRiskLevel.Low;
                    operation.Note = $"Entity '{entityName}' is up to date.";
                    operation.ProviderAssumptions.Add("No structural action required.");
                }
            }
            catch (Exception ex)
            {
                operation.Kind = MigrationPlanOperationKind.Error;
                operation.RiskLevel = MigrationPlanRiskLevel.Critical;
                operation.Note = $"Planning error for '{entityType.Name}': {ex.Message}";
                operation.ProviderAssumptions.Add("Resolve planning-time exceptions before executing migration operations.");
            }

            return operation;
        }

        private static MigrationPlanRiskLevel ClassifyCreateEntityRisk(DatasourceCategory category, DataSourceType type)
        {
            if (category == DatasourceCategory.RDBMS)
                return type == DataSourceType.Oracle ? MigrationPlanRiskLevel.High : MigrationPlanRiskLevel.Medium;

            if (category == DatasourceCategory.FILE || category == DatasourceCategory.NOSQL)
                return MigrationPlanRiskLevel.Low;

            return MigrationPlanRiskLevel.Medium;
        }

        private static MigrationPlanRiskLevel ClassifyAddColumnsRisk(DatasourceCategory category, DataSourceType type, int missingColumnCount)
        {
            if (missingColumnCount >= 10)
                return MigrationPlanRiskLevel.High;

            if (category == DatasourceCategory.RDBMS && type == DataSourceType.Oracle)
                return missingColumnCount > 0 ? MigrationPlanRiskLevel.High : MigrationPlanRiskLevel.Medium;

            if (missingColumnCount >= 4)
                return MigrationPlanRiskLevel.Medium;

            return MigrationPlanRiskLevel.Low;
        }

        private static IEnumerable<string> GetOperationAssumptions(DatasourceCategory category, DataSourceType type, MigrationPlanOperationKind operationKind)
        {
            if (operationKind == MigrationPlanOperationKind.CreateEntity)
            {
                yield return "Creation will use IDataSource.CreateEntityAs with .NET type metadata.";
                if (category == DatasourceCategory.RDBMS)
                    yield return "Validate naming conventions and helper capabilities before production rollout.";
            }
            else if (operationKind == MigrationPlanOperationKind.AddMissingColumns)
            {
                yield return "Column adds depend on datasource DDL capability and helper support.";
                if (category == DatasourceCategory.RDBMS)
                    yield return "Review lock/maintenance impact for additive schema changes.";
                if (type == DataSourceType.Oracle)
                    yield return "Oracle estates may require staged rollout for large table changes.";
            }
        }

        private static IEnumerable<string> GetFallbackTasks(MigrationPlanOperationKind kind, string entityName, MigrationProviderCapabilityProfile profile)
        {
            if (profile == null)
                yield break;

            if (kind == MigrationPlanOperationKind.AddMissingColumns && !profile.SupportsAlterColumn)
            {
                yield return $"Prepare fallback table-copy strategy for '{entityName}' because alter-column style DDL is limited on this provider.";
                yield return "Generate create-new + data-copy + swap execution runbook.";
            }

            if (kind == MigrationPlanOperationKind.CreateEntity && profile.RequiresOfflineWindowForSchemaChanges)
            {
                yield return $"Schedule offline/maintenance window validation before creating '{entityName}'.";
            }

            if (!profile.SupportsTransactionalDdl)
            {
                yield return "Enable checkpointed apply mode and explicit compensation steps due to non-transactional DDL behavior.";
            }
        }

        private static MigrationPolicyOptions CreateDefaultPolicyOptions()
        {
            return new MigrationPolicyOptions
            {
                EnvironmentTier = ResolveEnvironmentTierFromEnvironment(),
                RequireApprovalForHighRisk = true,
                RequireApprovalForCriticalRisk = true,
                BlockDestructiveInProtectedEnvironments = true,
                AllowDestructiveOverrideInProtectedEnvironments = false
            };
        }

        private static MigrationEnvironmentTier ResolveEnvironmentTierFromEnvironment()
        {
            var raw = Environment.GetEnvironmentVariable("BEEP_MIGRATION_ENVIRONMENT");
            if (string.IsNullOrWhiteSpace(raw))
                return MigrationEnvironmentTier.Development;

            switch (raw.Trim().ToLowerInvariant())
            {
                case "prod":
                case "production":
                    return MigrationEnvironmentTier.Production;
                case "stage":
                case "staging":
                case "preprod":
                    return MigrationEnvironmentTier.Staging;
                case "test":
                case "qa":
                case "uat":
                    return MigrationEnvironmentTier.Test;
                default:
                    return MigrationEnvironmentTier.Development;
            }
        }

        private static string ComputePlanHash(MigrationPlanArtifact plan)
        {
            var builder = new StringBuilder();
            builder.Append(plan.DataSourceName).Append('|')
                .Append(plan.DataSourceType).Append('|')
                .Append(plan.DataSourceCategory).Append('|')
                .Append(plan.UsesDiscovery).Append('|')
                .Append(plan.EntityTypeCount).Append('|')
                .Append(plan.LifecycleState);

            foreach (var issue in plan.ReadinessIssues
                .OrderBy(issue => issue.Code, StringComparer.OrdinalIgnoreCase)
                .ThenBy(issue => issue.EntityName, StringComparer.OrdinalIgnoreCase))
            {
                builder.Append("|I:")
                    .Append(issue.Code).Append(':')
                    .Append(issue.Severity).Append(':')
                    .Append(issue.EntityName).Append(':')
                    .Append(issue.Message);
            }

            foreach (var operation in plan.Operations
                .OrderBy(op => op.EntityName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(op => op.EntityTypeName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(op => op.Kind))
            {
                builder.Append("|O:")
                    .Append(operation.EntityName).Append(':')
                    .Append(operation.EntityTypeName).Append(':')
                    .Append(operation.Kind).Append(':')
                    .Append(operation.RiskLevel).Append(':')
                    .Append(operation.Note);

                foreach (var column in operation.MissingColumns.OrderBy(name => name, StringComparer.OrdinalIgnoreCase))
                {
                    builder.Append(":C:").Append(column);
                }

                foreach (var fallback in operation.FallbackTasks.OrderBy(task => task, StringComparer.OrdinalIgnoreCase))
                {
                    builder.Append(":F:").Append(fallback);
                }
            }

            if (plan.ProviderCapabilities != null)
            {
                builder.Append("|P:")
                    .Append(plan.ProviderCapabilities.DataSourceType).Append(':')
                    .Append(plan.ProviderCapabilities.DataSourceCategory).Append(':')
                    .Append(plan.ProviderCapabilities.SupportsAlterColumn).Append(':')
                    .Append(plan.ProviderCapabilities.SupportsRenameEntity).Append(':')
                    .Append(plan.ProviderCapabilities.SupportsRenameColumn).Append(':')
                    .Append(plan.ProviderCapabilities.SupportsTransactionalDdl).Append(':')
                    .Append(plan.ProviderCapabilities.RequiresOfflineWindowForSchemaChanges).Append(':')
                    .Append(plan.ProviderCapabilities.PortabilityWarning);
            }

            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(builder.ToString());
            var hash = sha.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", string.Empty);
        }

        private void TryTrackMigrationPlan(MigrationPlanArtifact plan, string operationName)
        {
            try
            {
                var configEditor = _editor?.ConfigEditor as ConfigEditor;
                if (configEditor == null)
                    return;

                var record = new MigrationRecord
                {
                    MigrationId = plan.PlanId,
                    Name = operationName,
                    AppliedOnUtc = DateTime.UtcNow,
                    Success = !plan.ReadinessIssues.Any(issue => issue.Severity == MigrationIssueSeverity.Error),
                    Notes = $"planHash={plan.PlanHash}; lifecycle={plan.LifecycleState}; pending={plan.PendingOperationCount}; usesDiscovery={plan.UsesDiscovery}",
                    Steps = plan.Operations.Select(operation => new MigrationStep
                    {
                        Operation = operation.Kind.ToString(),
                        EntityName = operation.EntityName,
                        ColumnName = operation.MissingColumns.Count > 0 ? string.Join(",", operation.MissingColumns) : string.Empty,
                        Sql = string.Empty,
                        Success = operation.Kind != MigrationPlanOperationKind.Error,
                        Message = operation.Note
                    }).ToList()
                };

                configEditor.AppendMigrationRecord(
                    plan.DataSourceName ?? string.Empty,
                    plan.DataSourceType,
                    record);
            }
            catch (Exception ex)
            {
                _editor?.AddLogMessage("Beep", $"Failed to track migration plan artifact: {ex.Message}", DateTime.Now, 0, null, Errors.Warning);
            }
        }

        // ────────────────────────────────────────────────────────────────────────────────
        // Phase 1 — Shared entity migration pipeline
        // Both ApplyMigrations (discovery) and ApplyMigrationsForTypes (explicit) delegate here.
        // ────────────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Shared internal pipeline used by all migration-apply paths (explicit and discovery).
        /// Emits a <see cref="EntityPipelineResult"/> with normalized scanned/created/updated/skipped/error counts
        /// and per-entity <see cref="EntityMigrationResultEntry"/> for CI consumption.
        /// </summary>
        internal EntityPipelineResult ExecuteEntityMigrationPipeline(
            IReadOnlyList<Type> typeList,
            bool usesDiscovery,
            EntityMigrationSource source,
            bool addMissingColumns,
            IProgress<PassedArgs> progress = null)
        {
            var pipelineResult = new EntityPipelineResult
            {
                Scanned = typeList.Count
            };

            foreach (var entityType in typeList)
            {
                pipelineResult.Processed++;
                var entry = new EntityMigrationResultEntry
                {
                    TypeFullName = entityType.FullName ?? entityType.Name,
                    Source = source,
                    AssemblyName = entityType.Assembly?.GetName().Name ?? string.Empty
                };

                try
                {
                    progress?.Report(new PassedArgs { Messege = $"Migrating: {entityType.Name}" });

                    var entityStructure = _editor?.classCreator?.ConvertToEntityStructure(entityType);
                    if (entityStructure == null)
                    {
                        entry.Decision = EntityMigrationDecision.Error;
                        entry.DecisionReasonCode = "ENTITY-CONVERT-FAILED";
                        entry.Success = false;
                        entry.Message = $"Failed to convert '{entityType.Name}' to EntityStructure";
                        pipelineResult.ErrorCount++;
                        pipelineResult.Entries.Add(entry);
                        continue;
                    }

                    var tableAttr = entityType.GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.TableAttribute>();
                    if (tableAttr != null && !string.IsNullOrWhiteSpace(tableAttr.Name))
                        entityStructure.EntityName = tableAttr.Name;

                    entry.EntityName = entityStructure.EntityName;

                    bool existed;
                    try { existed = MigrateDataSource.CheckEntityExist(entityStructure.EntityName); }
                    catch { existed = false; }

                    var result = EnsureEntity(entityStructure, createIfMissing: true, addMissingColumns: addMissingColumns);

                    if (result.Flag == Errors.Ok)
                    {
                        if (!existed)
                        {
                            entry.Decision = EntityMigrationDecision.Create;
                            entry.DecisionReasonCode = "ENTITY-CREATED";
                            pipelineResult.Created++;
                        }
                        else if (result.Message != null &&
                                 (result.Message.Contains("Added") || result.Message.Contains("column")))
                        {
                            entry.Decision = EntityMigrationDecision.Update;
                            entry.DecisionReasonCode = "COLUMNS-ADDED";
                            pipelineResult.Updated++;
                        }
                        else
                        {
                            entry.Decision = EntityMigrationDecision.NoChange;
                            entry.DecisionReasonCode = "ENTITY-UP-TO-DATE";
                            pipelineResult.Skipped++;
                        }
                        entry.Success = true;
                        entry.Message = result.Message;
                        progress?.Report(new PassedArgs { Messege = $"[{entry.DecisionReasonCode}] {entry.EntityName}" });
                    }
                    else
                    {
                        entry.Decision = EntityMigrationDecision.Error;
                        entry.DecisionReasonCode = "ENTITY-ERROR";
                        entry.Success = false;
                        entry.Message = result.Message;
                        pipelineResult.ErrorCount++;
                        progress?.Report(new PassedArgs { Messege = $"[ERROR] {entry.EntityName}: {result.Message}" });
                    }
                }
                catch (Exception ex)
                {
                    entry.Decision = EntityMigrationDecision.Error;
                    entry.DecisionReasonCode = "ENTITY-EXCEPTION";
                    entry.Success = false;
                    entry.Message = ex.Message;
                    pipelineResult.ErrorCount++;
                    progress?.Report(new PassedArgs { Messege = $"[EXCEPTION] {entityType.Name}: {ex.Message}" });
                }

                pipelineResult.Entries.Add(entry);
            }

            return pipelineResult;
        }

        /// <summary>
        /// Computes a stable SHA-256 hash string for use as a readiness report fingerprint.
        /// Enables diff between runs without string parsing.
        /// </summary>
        internal static string ComputeReadinessReportHash(
            string dataSourceName,
            DataSourceType dataSourceType,
            DatasourceCategory dataSourceCategory,
            bool usesDiscovery,
            int entityTypeCount,
            IEnumerable<MigrationReadinessIssue> issues)
        {
            var builder = new StringBuilder();
            builder.Append(dataSourceName).Append('|')
                .Append(dataSourceType).Append('|')
                .Append(dataSourceCategory).Append('|')
                .Append(usesDiscovery).Append('|')
                .Append(entityTypeCount);

            foreach (var issue in (issues ?? Enumerable.Empty<MigrationReadinessIssue>())
                .OrderBy(i => i.Code, StringComparer.OrdinalIgnoreCase)
                .ThenBy(i => i.EntityName, StringComparer.OrdinalIgnoreCase))
            {
                builder.Append("|").Append(issue.Code).Append(':').Append(issue.Severity).Append(':').Append(issue.EntityName);
            }

            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(builder.ToString());
            var hash = sha.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", string.Empty).Substring(0, 16); // 16-char prefix is enough for diffing
        }
    }
}
