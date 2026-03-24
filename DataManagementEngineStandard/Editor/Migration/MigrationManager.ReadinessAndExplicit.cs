using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.Migration
{
    public partial class MigrationManager
    {
        private MigrationReadinessReport BuildReadinessReport(IEnumerable<Type> entityTypes, bool usesDiscovery)
        {
            var report = CreateBaseReadinessReport(usesDiscovery);

            if (entityTypes == null)
            {
                report.Issues.Add(new MigrationReadinessIssue
                {
                    Code = "entity-types-null",
                    Severity = MigrationIssueSeverity.Error,
                    Channel = MigrationReportChannel.ReadinessIssue,
                    Message = "No entity types were provided for readiness analysis.",
                    Recommendation = "Pass explicit entity types or fix discovery configuration before attempting migration."
                });
                return report;
            }

            var typeList = entityTypes.Where(type => type != null).Distinct().ToList();
            report.EntityTypeCount = typeList.Count;

            if (typeList.Count == 0)
            {
                report.Issues.Add(new MigrationReadinessIssue
                {
                    Code = "entity-types-empty",
                    Severity = MigrationIssueSeverity.Warning,
                    Channel = MigrationReportChannel.ReadinessIssue,
                    Message = "Readiness analysis found zero entity types.",
                    Recommendation = usesDiscovery
                        ? "Register the correct assemblies or use explicit-type migration."
                        : "Pass a non-empty entity type collection."
                });
                return report;
            }

            if (usesDiscovery)
            {
                report.Issues.Add(new MigrationReadinessIssue
                {
                    Code = "discovery-scope-review",
                    Severity = MigrationIssueSeverity.Info,
                    Channel = MigrationReportChannel.BestPractice,
                    RecommendationId = "REC-DISCOVERY-001",
                    Message = "Discovery-based migration is in use.",
                    Recommendation = "For enterprise application schemas, prefer explicit-type migration to reduce assembly/version noise."
                });
            }

            var entityNames = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var entityType in typeList)
            {
                var entityStructure = TryGetEntityStructure(entityType);
                var entityName = GetEntityName(entityType, entityStructure);

                if (string.IsNullOrWhiteSpace(entityName))
                {
                    report.Issues.Add(new MigrationReadinessIssue
                    {
                        Code = "entity-name-missing",
                        Severity = MigrationIssueSeverity.Error,
                        Channel = MigrationReportChannel.ReadinessIssue,
                        Message = $"Entity type '{entityType.FullName}' does not resolve to a valid entity name.",
                        Recommendation = "Add a stable class name or [Table] attribute before running migrations.",
                        EntityName = entityType.Name
                    });
                    continue;
                }

                if (!entityNames.TryGetValue(entityName, out var mappedTypes))
                {
                    mappedTypes = new List<string>();
                    entityNames[entityName] = mappedTypes;
                }
                mappedTypes.Add(entityType.FullName ?? entityType.Name);

                AnalyzeEntityName(report, entityName, entityType);
                AnalyzeEntityStructure(report, entityName, entityStructure);
            }

            foreach (var duplicate in entityNames.Where(item => item.Value.Count > 1))
            {
                report.Issues.Add(new MigrationReadinessIssue
                {
                    Code = "duplicate-entity-name",
                    Severity = MigrationIssueSeverity.Error,
                    Channel = MigrationReportChannel.ReadinessIssue,
                    Message = $"Multiple entity types map to the same entity name '{duplicate.Key}'.",
                    Recommendation = "Use distinct [Table] names or remove duplicate entity registrations before running migrations.",
                    EntityName = duplicate.Key
                });
            }

            report.PolicyEvaluation = BuildPolicyEvaluationFromReadiness(report);

            // Phase 2: compute stable report hash for CI diffing
            report.ReportHash = ComputeReadinessReportHash(
                report.DataSourceName,
                report.DataSourceType,
                report.DataSourceCategory,
                usesDiscovery,
                report.EntityTypeCount,
                report.Issues);

            // Phase 2: populate per-entity decision records from readiness analysis
            foreach (var type in typeList)
            {
                var structureSnapshot = TryGetEntityStructure(type);
                var entityName = GetEntityName(type, structureSnapshot);
                var hasIssue = report.Issues.Any(issue => string.Equals(issue.EntityName, entityName, StringComparison.OrdinalIgnoreCase) && issue.Severity == MigrationIssueSeverity.Error);
                var capabilitySnapshot = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["SupportsSchemaEvolution"] = report.SupportsSchemaEvolution.ToString(),
                    ["IsSchemaEnforced"]        = report.IsSchemaEnforced.ToString(),
                    ["SupportsTransactions"]    = report.SupportsTransactions.ToString(),
                    ["HelperAvailable"]         = report.HelperAvailable.ToString()
                };
                report.EntityDecisions.Add(new EntityDecisionRecord
                {
                    EntityName              = entityName,
                    Decision                = hasIssue ? EntityMigrationDecision.Skipped : EntityMigrationDecision.NoChange,
                    DecisionReasonCode      = hasIssue ? "READINESS-BLOCKED" : "READINESS-OK",
                    CapabilityContextSnapshot = capabilitySnapshot,
                    Source                  = EntityMigrationSource.Explicit,
                    AssemblyName            = type.Assembly?.GetName().Name ?? string.Empty
                });
            }

            return report;
        }

        private MigrationReadinessReport CreateBaseReadinessReport(bool usesDiscovery)
        {
            var dataSourceType = MigrateDataSource?.DatasourceType ?? DataSourceType.Unknown;
            var dataSourceCategory = MigrateDataSource?.Category ?? DatasourceCategory.NONE;
            var capabilities = GetCapabilities(dataSourceType, dataSourceCategory);
            var report = new MigrationReadinessReport
            {
                DataSourceName = MigrateDataSource?.DatasourceName ?? string.Empty,
                DataSourceType = dataSourceType,
                DataSourceCategory = dataSourceCategory,
                UsesDiscovery = usesDiscovery,
                HelperAvailable = TryHasHelper(dataSourceType),
                SupportsSchemaEvolution = capabilities.SupportsSchemaEvolution,
                IsSchemaEnforced = capabilities.IsSchemaEnforced,
                SupportsTransactions = capabilities.SupportsTransactions,
                CapabilityNotes = capabilities.Notes ?? string.Empty
            };

            report.MigrationBestPractices.AddRange(GetMigrationBestPractices(report.DataSourceType, report.DataSourceCategory));

            if (report.HelperAvailable)
            {
                report.MigrationBestPractices.Add("A specialized IDataSourceHelper is registered for this datasource type. Use it as the authoritative path for validation and platform-specific schema operations.");
            }
            else
            {
                report.MigrationBestPractices.Add("No specialized IDataSourceHelper was resolved. Prefer capability-driven create-if-missing flows and validate structural changes against the concrete datasource implementation.");
            }

            if (!report.HelperAvailable)
            {
                report.Issues.Add(new MigrationReadinessIssue
                {
                    Code = "helper-not-registered",
                    Severity = MigrationIssueSeverity.Warning,
                    Channel = MigrationReportChannel.ReadinessIssue,
                    RecommendationId = "REC-HELPER-001",
                    Message = $"No specialized helper is registered for datasource type '{report.DataSourceType}'.",
                    Recommendation = "Register or verify the appropriate helper if helper-driven DDL generation is required."
                });
            }

            if (MigrateDataSource?.Dataconnection?.ConnectionProp == null)
            {
                report.Issues.Add(new MigrationReadinessIssue
                {
                    Code = "connection-properties-missing",
                    Severity = MigrationIssueSeverity.Warning,
                    Channel = MigrationReportChannel.ReadinessIssue,
                    Message = "Migration datasource does not expose ConnectionProperties.",
                    Recommendation = "Persist and normalize connection metadata before migration so category, endpoint, and file settings remain consistent across helper resolution and runtime reconnects."
                });
            }
            else if (MigrateDataSource.Dataconnection.ConnectionProp.Category != DatasourceCategory.NONE &&
                     report.DataSourceCategory != DatasourceCategory.NONE &&
                     MigrateDataSource.Dataconnection.ConnectionProp.Category != report.DataSourceCategory)
            {
                report.Issues.Add(new MigrationReadinessIssue
                {
                    Code = "connection-category-mismatch",
                    Severity = MigrationIssueSeverity.Warning,
                    Channel = MigrationReportChannel.ReadinessIssue,
                    Message = $"Datasource category '{report.DataSourceCategory}' does not match connection category '{MigrateDataSource.Dataconnection.ConnectionProp.Category}'.",
                    Recommendation = "Reconcile datasource and ConnectionProperties category values before migration so helper and capability selection stay deterministic."
                });
            }

            if (!report.SupportsSchemaEvolution)
            {
                report.Issues.Add(new MigrationReadinessIssue
                {
                    Code = "schema-evolution-limited",
                    Severity = MigrationIssueSeverity.Warning,
                    Channel = MigrationReportChannel.BestPractice,
                    RecommendationId = "REC-SCHEMA-001",
                    Message = $"Datasource '{report.DataSourceName}' reports limited schema-evolution capability.",
                    Recommendation = "Prefer create-new-resource/additive migration patterns and validate helper behavior against the target datasource before altering existing structures."
                });
            }

            // Phase 5: attach recommendation profile for this datasource type/category
            report.AppliedProfile = GetRecommendationProfile(dataSourceType, dataSourceCategory);

            return report;
        }

        private MigrationReadinessReport CreateReadinessReportWithError(string code, string message, string recommendation)
        {
            var report = CreateBaseReadinessReport(usesDiscovery: false);
            report.Issues.Add(new MigrationReadinessIssue
            {
                Code = code,
                Severity = MigrationIssueSeverity.Error,
                Message = message,
                Recommendation = recommendation
            });
            return report;
        }

        private EntityStructure TryGetEntityStructure(Type entityType)
        {
            try
            {
                return _editor?.classCreator?.ConvertToEntityStructure(entityType);
            }
            catch
            {
                return null;
            }
        }

        private static string GetEntityName(Type entityType, EntityStructure entityStructure = null)
        {
            var tableAttr = entityType.GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.TableAttribute>();
            if (tableAttr != null && !string.IsNullOrWhiteSpace(tableAttr.Name))
                return tableAttr.Name;

            if (!string.IsNullOrWhiteSpace(entityStructure?.EntityName))
                return entityStructure.EntityName;

            return entityType.Name;
        }

        private void AnalyzeEntityName(MigrationReadinessReport report, string entityName, Type entityType)
        {
            if (entityName.IndexOfAny(new[] { ' ', '-', '.', '/' }) >= 0)
            {
                report.Issues.Add(new MigrationReadinessIssue
                {
                    Code = "identifier-portability",
                    Severity = MigrationIssueSeverity.Warning,
                    Message = $"Entity '{entityName}' uses characters that reduce provider portability.",
                    Recommendation = "Prefer simple alphanumeric and underscore naming for cross-platform migration safety.",
                    EntityName = entityName
                });
            }

            if (report.DataSourceCategory == DatasourceCategory.RDBMS &&
                report.DataSourceType == DataSourceType.Oracle &&
                entityName.Length > 30)
            {
                report.Issues.Add(new MigrationReadinessIssue
                {
                    Code = "oracle-identifier-length",
                    Severity = MigrationIssueSeverity.Warning,
                    Message = $"Entity '{entityName}' exceeds 30 characters, which is risky across Oracle estates and tooling.",
                    Recommendation = "Use shorter table names or stable aliases when targeting Oracle environments.",
                    EntityName = entityName
                });
            }
        }

        private void AnalyzeEntityStructure(MigrationReadinessReport report, string entityName, EntityStructure entityStructure)
        {
            if (entityStructure == null)
            {
                report.Issues.Add(new MigrationReadinessIssue
                {
                    Code = "entity-structure-unavailable",
                    Severity = MigrationIssueSeverity.Warning,
                    Message = $"Could not inspect structure for entity '{entityName}'.",
                    Recommendation = "Verify classCreator availability and entity metadata conversion before migration.",
                    EntityName = entityName
                });
                return;
            }

            if (report.IsSchemaEnforced &&
                report.DataSourceCategory == DatasourceCategory.RDBMS &&
                (entityStructure.PrimaryKeys == null || entityStructure.PrimaryKeys.Count == 0) &&
                (entityStructure.Fields == null || !entityStructure.Fields.Any(field => field.IsKey || field.IsIdentity || field.IsAutoIncrement)))
            {
                report.Issues.Add(new MigrationReadinessIssue
                {
                    Code = "primary-key-missing",
                    Severity = MigrationIssueSeverity.Warning,
                    Message = $"Entity '{entityName}' does not appear to define a primary key or identity field.",
                    Recommendation = "Add an explicit key definition for operationally safe migrations and downstream CRUD behavior.",
                    EntityName = entityName
                });
            }

            if (entityStructure.Fields == null)
                return;

            foreach (var field in entityStructure.Fields.Where(field => field != null && !string.IsNullOrWhiteSpace(field.FieldName)))
            {
                if (field.FieldName.IndexOfAny(new[] { ' ', '-', '.', '/' }) >= 0)
                {
                    report.Issues.Add(new MigrationReadinessIssue
                    {
                        Code = "column-portability",
                        Severity = MigrationIssueSeverity.Warning,
                        Message = $"Field '{field.FieldName}' in entity '{entityName}' uses characters that reduce provider portability.",
                        Recommendation = "Prefer simple alphanumeric and underscore column naming across providers.",
                        EntityName = entityName
                    });
                }

                if (report.DataSourceCategory == DatasourceCategory.RDBMS &&
                    report.DataSourceType == DataSourceType.Oracle &&
                    field.FieldName.Length > 30)
                {
                    report.Issues.Add(new MigrationReadinessIssue
                    {
                        Code = "oracle-column-length",
                        Severity = MigrationIssueSeverity.Warning,
                        Message = $"Field '{field.FieldName}' in entity '{entityName}' exceeds 30 characters, which is risky across Oracle estates and tooling.",
                        Recommendation = "Use shorter column names or stable aliases for Oracle portability.",
                        EntityName = entityName
                    });
                }
            }
        }

        private static DataSourceCapabilities GetCapabilities(DataSourceType dataSourceType, DatasourceCategory dataSourceCategory)
        {
            try
            {
                return DataSourceCapabilityMatrix.GetCapabilities(dataSourceType, dataSourceCategory) ?? new DataSourceCapabilities();
            }
            catch
            {
                return new DataSourceCapabilities();
            }
        }

        private bool TryHasHelper(DataSourceType dataSourceType)
        {
            try
            {
                return _editor?.GetDataSourceHelper(dataSourceType) != null;
            }
            catch
            {
                return false;
            }
        }

        private void LogReadinessReport(string operationName, MigrationReadinessReport report, IProgress<PassedArgs> progress = null)
        {
            if (report == null)
                return;

            // Best-practice channel — informational only
            foreach (var practice in report.MigrationBestPractices)
            {
                var message = $"{operationName} [BestPractice] [{report.DataSourceCategory}/{report.DataSourceType}]: {practice}";
                _editor?.AddLogMessage("Beep", message, DateTime.Now, 0, null, Errors.Ok);
                progress?.Report(new PassedArgs { Messege = message });
            }

            // Issue channels — use per-issue Channel for routing
            foreach (var issue in report.Issues)
            {
                var channelTag = issue.Channel switch
                {
                    MigrationReportChannel.BestPractice     => "BestPractice",
                    MigrationReportChannel.MigrationDecision => "Decision",
                    _                                        => "ReadinessIssue"
                };
                var message = $"{operationName} [{channelTag}][{issue.Severity}] {issue.Code}: {issue.Message}";
                var errorLevel = issue.Severity == MigrationIssueSeverity.Error
                    ? Errors.Failed
                    : issue.Severity == MigrationIssueSeverity.Warning
                        ? Errors.Warning
                        : Errors.Ok;

                _editor?.AddLogMessage("Beep", message, DateTime.Now, 0, null, errorLevel);
                progress?.Report(new PassedArgs { Messege = $"{message} — Recommendation: {issue.Recommendation}" });
            }
        }

        #region Explicit-Type Migration (bypasses discovery)

        /// <summary>
        /// Ensures database is created for the given entity types.
        /// Use this when you know exactly which types to create — bypasses assembly discovery entirely.
        /// This is the most reliable approach for cross-project scenarios where discovery might miss assemblies.
        /// </summary>
        /// <example>
        /// migrationManager.EnsureDatabaseCreatedForTypes(
        ///     new[] { typeof(Customer), typeof(Product), typeof(Invoice) },
        ///     progress: progressReporter);
        /// </example>
        public IErrorsInfo EnsureDatabaseCreatedForTypes(IEnumerable<Type> entityTypes, bool detectRelationships = true, IProgress<PassedArgs> progress = null)
        {
            if (MigrateDataSource == null)
                return CreateErrorsInfo(Errors.Failed, "Migration data source is not set");

            if (entityTypes == null)
                return CreateErrorsInfo(Errors.Failed, "Entity types collection cannot be null");

            var typeList = entityTypes.ToList();
            if (typeList.Count == 0)
                return CreateErrorsInfo(Errors.Warning, "No entity types provided");

            try
            {
                var readiness = BuildReadinessReport(typeList, usesDiscovery: false);
                LogReadinessReport(nameof(EnsureDatabaseCreatedForTypes), readiness, progress);
                if (readiness.HasBlockingIssues)
                {
                    var blocking = string.Join("; ", readiness.Issues
                        .Where(issue => issue.Severity == MigrationIssueSeverity.Error)
                        .Select(issue => issue.Message));
                    return CreateErrorsInfo(Errors.Failed, $"Migration readiness failed: {blocking}");
                }

                progress?.Report(new PassedArgs { Messege = $"EnsureDatabaseCreatedForTypes: Processing {typeList.Count} explicit type(s)" });

                _editor?.AddLogMessage("Beep",
                    $"MigrationManager.EnsureDatabaseCreatedForTypes: {typeList.Count} type(s): {string.Join(", ", typeList.Select(t => t.Name))}",
                    DateTime.Now, 0, null, Errors.Ok);

                var pipelineResult = ExecuteEntityMigrationPipeline(
                    typeList,
                    usesDiscovery: false,
                    source: EntityMigrationSource.Explicit,
                    addMissingColumns: false,
                    progress: progress);

                var summaryMsg = pipelineResult.ToSummaryString();
                progress?.Report(new PassedArgs { Messege = summaryMsg });

                if (pipelineResult.ErrorCount > 0)
                {
                    var errMsgs = pipelineResult.Entries.Where(e => !e.Success).Select(e => $"{e.EntityName ?? e.TypeFullName}: {e.Message}");
                    return CreateErrorsInfo(Errors.Failed, $"{summaryMsg}. Errors: {string.Join("; ", errMsgs)}");
                }

                return CreateErrorsInfo(Errors.Ok, summaryMsg);
            }
            catch (Exception ex)
            {
                return CreateErrorsInfo(Errors.Failed, $"Exception during EnsureDatabaseCreatedForTypes: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Applies migrations for the given entity types.
        /// Use this when you know exactly which types to migrate — bypasses assembly discovery entirely.
        /// </summary>
        public IErrorsInfo ApplyMigrationsForTypes(IEnumerable<Type> entityTypes, bool detectRelationships = true, bool addMissingColumns = true, IProgress<PassedArgs> progress = null)
        {
            if (MigrateDataSource == null)
                return CreateErrorsInfo(Errors.Failed, "Migration data source is not set");

            if (entityTypes == null)
                return CreateErrorsInfo(Errors.Failed, "Entity types collection cannot be null");

            var typeList = entityTypes.ToList();
            if (typeList.Count == 0)
                return CreateErrorsInfo(Errors.Warning, "No entity types provided");

            try
            {
                var readiness = BuildReadinessReport(typeList, usesDiscovery: false);
                LogReadinessReport(nameof(ApplyMigrationsForTypes), readiness, progress);
                if (readiness.HasBlockingIssues)
                {
                    var blocking = string.Join("; ", readiness.Issues
                        .Where(issue => issue.Severity == MigrationIssueSeverity.Error)
                        .Select(issue => issue.Message));
                    return CreateErrorsInfo(Errors.Failed, $"Migration readiness failed: {blocking}");
                }

                progress?.Report(new PassedArgs { Messege = $"ApplyMigrationsForTypes: Migrating {typeList.Count} explicit type(s)" });

                _editor?.AddLogMessage("Beep",
                    $"MigrationManager.ApplyMigrationsForTypes: {typeList.Count} type(s): {string.Join(", ", typeList.Select(t => t.Name))}",
                    DateTime.Now, 0, null, Errors.Ok);

                var pipelineResult = ExecuteEntityMigrationPipeline(
                    typeList,
                    usesDiscovery: false,
                    source: EntityMigrationSource.Explicit,
                    addMissingColumns: addMissingColumns,
                    progress: progress);

                var summaryMsg = pipelineResult.ToSummaryString();
                progress?.Report(new PassedArgs { Messege = summaryMsg });

                if (pipelineResult.ErrorCount > 0)
                {
                    var errMsgs = pipelineResult.Entries.Where(e => !e.Success).Select(e => $"{e.EntityName ?? e.TypeFullName}: {e.Message}");
                    return CreateErrorsInfo(Errors.Failed, $"{summaryMsg}. Errors: {string.Join("; ", errMsgs)}");
                }

                return CreateErrorsInfo(Errors.Ok, summaryMsg);
            }
            catch (Exception ex)
            {
                return CreateErrorsInfo(Errors.Failed, $"Exception during ApplyMigrationsForTypes: {ex.Message}", ex);
            }
        }

        #endregion
    }
}
