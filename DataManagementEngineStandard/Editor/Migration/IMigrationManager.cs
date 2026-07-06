using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.UOW.Helpers;
using TheTechIdea.Beep.Editor.UOW.Interfaces;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.Migration
{
    /// <summary>
    /// Summary of migration status comparing Entity classes with database state.
    /// </summary>
    public class MigrationSummary
    {
        /// <summary>
        /// List of entity names that need to be created in the database.
        /// </summary>
        public List<string> EntitiesToCreate { get; set; } = new List<string>();
        
        /// <summary>
        /// List of entity names that need updates (missing columns, etc.).
        /// </summary>
        public List<string> EntitiesToUpdate { get; set; } = new List<string>();
        
        /// <summary>
        /// List of entity names that are up-to-date with their Entity classes.
        /// </summary>
        public List<string> EntitiesUpToDate { get; set; } = new List<string>();
        
        /// <summary>
        /// List of errors encountered during migration summary generation.
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Non-error diagnostics and intent annotations recorded alongside the
        /// summary (e.g. <c>applyForeignKeys=true</c> when the caller opted in).
        /// </summary>
        public List<string> Diagnostics { get; set; } = new List<string>();

        /// <summary>
        /// Total count of entities that need migration.
        /// </summary>
        public int TotalPendingMigrations => EntitiesToCreate.Count + EntitiesToUpdate.Count;

        /// <summary>
        /// Number of foreign-key relations discovered across the surveyed
        /// entities. Only meaningful when the caller opted into FK
        /// generation (see <see cref="GetMigrationSummary(string, Assembly, bool, bool, bool)"/>).
        /// Counts both AddForeignKey and DropForeignKey operations.
        /// </summary>
        public int ForeignKeyCount { get; set; }

        /// <summary>
        /// Number of indexes discovered across the surveyed entities. Only
        /// meaningful when the caller opted into index generation.
        /// Counts both CreateIndex and DropIndex operations.
        /// </summary>
        public int IndexCount { get; set; }

        /// <summary>
        /// True when the active provider can express AddForeignKey/DropForeignKey
        /// DDL. False for file-based and NoSQL datasources. Operators should
        /// inspect this together with <see cref="ForeignKeyCount"/> before
        /// relying on a plan to apply constraints.
        /// </summary>
        public bool ProviderSupportsForeignKeys { get; set; } = true;

        /// <summary>
        /// True when the active provider can express CreateIndex/DropIndex DDL.
        /// False for file-based datasources and some embedded engines that
        /// emulate index DDL via table copy.
        /// </summary>
        public bool ProviderSupportsIndexes { get; set; } = true;

        /// <summary>
        /// Indicates if there are any pending migrations.
        /// </summary>
        public bool HasPendingMigrations => TotalPendingMigrations > 0;

        // Phase 2 additions
        /// <summary>Per-entity machine-readable decision records for CI consumption.</summary>
        public List<EntityDecisionRecord> EntityDecisions { get; set; } = new List<EntityDecisionRecord>();
        /// <summary>Stable hash of the summary inputs for run-to-run diffing.</summary>
        public string ReportHash { get; set; } = string.Empty;
        public DataSourceType DataSourceType { get; set; } = DataSourceType.Unknown;
        public DatasourceCategory DataSourceCategory { get; set; } = DatasourceCategory.NONE;
    }

    /// <summary>
    /// Severity levels used by migration readiness diagnostics.
    /// </summary>
    public enum MigrationIssueSeverity
    {
        Info,
        Warning,
        Error
    }

    /// <summary>
    /// Structured migration-readiness finding with datasource and operational guidance.
    /// </summary>
    public class MigrationReadinessIssue
    {
        public string Code { get; set; } = string.Empty;
        public MigrationIssueSeverity Severity { get; set; } = MigrationIssueSeverity.Info;
        public string Message { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        // Phase 2 additions
        /// <summary>Log channel — enables CI to filter issues by type without parsing Message text.</summary>
        public MigrationReportChannel Channel { get; set; } = MigrationReportChannel.ReadinessIssue;
        /// <summary>Stable recommendation id (e.g. "REC-RDBMS-001") to link to a <see cref="RecommendationEntry"/>.</summary>
        public string RecommendationId { get; set; } = string.Empty;
        /// <summary>Key capability facts that influenced this finding (e.g. "SupportsSchemaEvolution=false").</summary>
        public Dictionary<string, string> CapabilityContextSnapshot { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Datasource-aware readiness report for enterprise migration planning.
    /// </summary>
    public class MigrationReadinessReport
    {
        public string DataSourceName { get; set; } = string.Empty;
        public DataSourceType DataSourceType { get; set; } = DataSourceType.Unknown;
        public DatasourceCategory DataSourceCategory { get; set; } = DatasourceCategory.NONE;
        public bool UsesDiscovery { get; set; }
        public bool HelperAvailable { get; set; }
        public bool SupportsSchemaEvolution { get; set; }
        public bool IsSchemaEnforced { get; set; }
        public bool SupportsTransactions { get; set; }
        public string CapabilityNotes { get; set; } = string.Empty;
        public List<string> MigrationBestPractices { get; set; } = new List<string>();
        [Obsolete("Use MigrationBestPractices instead. This alias remains for backward compatibility.")]
        public List<string> ProviderBestPractices
        {
            get => MigrationBestPractices;
            set => MigrationBestPractices = value ?? new List<string>();
        }
        public List<MigrationReadinessIssue> Issues { get; set; } = new List<MigrationReadinessIssue>();
        public int EntityTypeCount { get; set; }
        public bool HasBlockingIssues => Issues.Exists(issue => issue.Severity == MigrationIssueSeverity.Error);
        public MigrationPolicyEvaluation PolicyEvaluation { get; set; } = new MigrationPolicyEvaluation();
        // Phase 2 additions
        /// <summary>
        /// Deterministic hash of the report inputs. Stable across repeated runs with the same
        /// datasource type/category and entity set — enables diff between runs without string parsing.
        /// </summary>
        public string ReportHash { get; set; } = string.Empty;
        /// <summary>Per-entity migration decisions for CI gate consumption.</summary>
        public List<EntityDecisionRecord> EntityDecisions { get; set; } = new List<EntityDecisionRecord>();
        /// <summary>Versioned recommendation profile applied to this report.</summary>
        public RecommendationProfile AppliedProfile { get; set; }
    }

    /// <summary>
    /// Lifecycle states for a migration plan artifact.
    /// </summary>
    public enum MigrationPlanLifecycleState
    {
        Draft,
        Reviewed,
        Approved,
        Applied,
        Verified
    }

    /// <summary>
    /// Operation categories used in migration plan preview artifacts.
    /// </summary>
    public enum MigrationPlanOperationKind
    {
        None,
        CreateEntity,
        DropEntity,
        AddMissingColumns,
        DropColumn,
        AlterColumn,
        RenameEntity,
        RenameColumn,
        TruncateEntity,
        AddForeignKey,
        DropForeignKey,
        CreateIndex,
        DropIndex,
        UpToDate,
        Error
    }

    /// <summary>
    /// Risk level classification for migration plan operations.
    /// </summary>
    public enum MigrationPlanRiskLevel
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// A single planned operation generated during migration planning.
    /// </summary>
    public class MigrationPlanOperation
    {
        public string EntityName { get; set; } = string.Empty;
        public string EntityTypeName { get; set; } = string.Empty;
        public MigrationPlanOperationKind Kind { get; set; } = MigrationPlanOperationKind.None;
        public MigrationPlanRiskLevel RiskLevel { get; set; } = MigrationPlanRiskLevel.Low;
        public bool IsDestructive { get; set; }
        public bool IsTypeNarrowing { get; set; }
        public bool HasNullabilityTightening { get; set; }
        public List<string> ProviderAssumptions { get; set; } = new List<string>();
        public List<string> FallbackTasks { get; set; } = new List<string>();
        public List<string> MissingColumns { get; set; } = new List<string>();
        public string Note { get; set; } = string.Empty;
        /// <summary>
        /// Stable identifier for the target object of this operation (e.g. constraint
        /// name for AddForeignKey/DropForeignKey, index name for CreateIndex/DropIndex).
        /// Threaded through to <see cref="MigrationExecutionStep.TargetName"/> so the
        /// execution step can run the right drop/add DDL without re-deriving it from
        /// the desired structure's metadata (which may not round-trip cleanly for
        /// indexes, where the plan emitter also synthesizes the name).
        /// </summary>
        public string TargetName { get; set; } = string.Empty;
    }

    public class MigrationProviderCapabilityProfile
    {
        public DataSourceType DataSourceType { get; set; } = DataSourceType.Unknown;
        public DatasourceCategory DataSourceCategory { get; set; } = DatasourceCategory.NONE;
        public bool SupportsAlterColumn { get; set; }
        public bool SupportsRenameEntity { get; set; }
        public bool SupportsRenameColumn { get; set; }
        public bool SupportsTransactionalDdl { get; set; }
        public bool SupportsForeignKeys { get; set; }
        public bool SupportsIndexes { get; set; }
        public bool RequiresOfflineWindowForSchemaChanges { get; set; }
        public string PortabilityWarning { get; set; } = string.Empty;
        public List<string> Constraints { get; set; } = new List<string>();
    }

    /// <summary>
    /// First-class migration plan artifact generated before applying schema changes.
    /// </summary>
    public class MigrationPlanArtifact : IMigrationPlanArtifact
    {
        public string PlanId { get; set; } = Guid.NewGuid().ToString("N");
        public string PlanHash { get; set; } = string.Empty;
        public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;
        public MigrationPlanLifecycleState LifecycleState { get; set; } = MigrationPlanLifecycleState.Draft;
        public string DataSourceName { get; set; } = string.Empty;
        public DataSourceType DataSourceType { get; set; } = DataSourceType.Unknown;
        public DatasourceCategory DataSourceCategory { get; set; } = DatasourceCategory.NONE;
        public bool UsesDiscovery { get; set; }
        public int EntityTypeCount { get; set; }
        public MigrationProviderCapabilityProfile ProviderCapabilities { get; set; } = new MigrationProviderCapabilityProfile();
        public List<string> ProviderAssumptions { get; set; } = new List<string>();
        public List<MigrationReadinessIssue> ReadinessIssues { get; set; } = new List<MigrationReadinessIssue>();
        public List<MigrationPlanOperation> Operations { get; set; } = new List<MigrationPlanOperation>();
        public MigrationPolicyEvaluation PolicyEvaluation { get; set; } = new MigrationPolicyEvaluation();
        public MigrationDryRunReport DryRunReport { get; set; } = new MigrationDryRunReport();
        public MigrationPreflightReport PreflightReport { get; set; } = new MigrationPreflightReport();
        public MigrationImpactReport ImpactReport { get; set; } = new MigrationImpactReport();
        public MigrationExecutionCheckpoint ExecutionCheckpoint { get; set; } = new MigrationExecutionCheckpoint();
        public MigrationCompensationPlan CompensationPlan { get; set; } = new MigrationCompensationPlan();
        public MigrationRollbackReadinessReport RollbackReadinessReport { get; set; } = new MigrationRollbackReadinessReport();
        public List<MigrationAuditEvent> AuditTrail { get; set; } = new List<MigrationAuditEvent>();
        public List<MigrationDiagnosticEntry> Diagnostics { get; set; } = new List<MigrationDiagnosticEntry>();
        public MigrationPerformancePlan PerformancePlan { get; set; } = new MigrationPerformancePlan();
        public MigrationCiValidationReport CiValidationReport { get; set; } = new MigrationCiValidationReport();
        public MigrationRolloutGovernanceReport RolloutGovernanceReport { get; set; } = new MigrationRolloutGovernanceReport();
        public int PendingOperationCount => Operations.FindAll(operation =>
            operation.Kind == MigrationPlanOperationKind.CreateEntity ||
            operation.Kind == MigrationPlanOperationKind.AddMissingColumns ||
            operation.Kind == MigrationPlanOperationKind.AddForeignKey ||
            operation.Kind == MigrationPlanOperationKind.DropForeignKey ||
            operation.Kind == MigrationPlanOperationKind.CreateIndex ||
            operation.Kind == MigrationPlanOperationKind.DropIndex ||
            operation.Kind == MigrationPlanOperationKind.AlterColumn ||
            operation.Kind == MigrationPlanOperationKind.DropColumn ||
            operation.Kind == MigrationPlanOperationKind.RenameEntity ||
            operation.Kind == MigrationPlanOperationKind.RenameColumn ||
            operation.Kind == MigrationPlanOperationKind.TruncateEntity).Count;
    }

    public enum MigrationPolicyDecision
    {
        Pass,
        Warn,
        Block
    }

    public enum MigrationEnvironmentTier
    {
        Development,
        Test,
        Staging,
        Production
    }

    public class MigrationPolicyOptions
    {
        public MigrationEnvironmentTier EnvironmentTier { get; set; } = MigrationEnvironmentTier.Development;
        public bool RequireApprovalForHighRisk { get; set; } = true;
        public bool RequireApprovalForCriticalRisk { get; set; } = true;
        public bool BlockDestructiveInProtectedEnvironments { get; set; } = true;
        public bool AllowDestructiveOverrideInProtectedEnvironments { get; set; } = false;
        public string OverrideReason { get; set; } = string.Empty;
        public string Approver { get; set; } = string.Empty;
    }

    public class MigrationPolicyFinding
    {
        public string RuleId { get; set; } = string.Empty;
        public MigrationPolicyDecision Decision { get; set; } = MigrationPolicyDecision.Pass;
        public string Message { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public MigrationPlanOperationKind OperationKind { get; set; } = MigrationPlanOperationKind.None;
        public MigrationPlanRiskLevel RiskLevel { get; set; } = MigrationPlanRiskLevel.Low;
    }

    public class MigrationPolicyEvaluation
    {
        public MigrationEnvironmentTier EnvironmentTier { get; set; } = MigrationEnvironmentTier.Development;
        public bool IsProtectedEnvironment { get; set; }
        public bool RequiresManualApproval { get; set; }
        public MigrationPolicyDecision Decision { get; set; } = MigrationPolicyDecision.Pass;
        public List<MigrationPolicyFinding> Findings { get; set; } = new List<MigrationPolicyFinding>();
        public bool HasBlockingFindings => Findings.Exists(finding => finding.Decision == MigrationPolicyDecision.Block);
    }

    public class MigrationDryRunOperation
    {
        public string EntityName { get; set; } = string.Empty;
        public MigrationPlanOperationKind Kind { get; set; } = MigrationPlanOperationKind.None;
        public MigrationPlanRiskLevel RiskLevel { get; set; } = MigrationPlanRiskLevel.Low;
        /// <summary>
        /// For AddForeignKey / DropForeignKey, the constraint name. For
        /// CreateIndex / DropIndex, the index name. Empty for non-relational
        /// op kinds. Mirrors <see cref="MigrationPlanOperation.TargetName"/>.
        /// </summary>
        public string TargetName { get; set; } = string.Empty;
        public List<string> DdlPreview { get; set; } = new List<string>();
        public List<string> RiskTags { get; set; } = new List<string>();
        public List<string> Diagnostics { get; set; } = new List<string>();
    }

    public class MigrationDryRunReport
    {
        public DateTime GeneratedOnUtc { get; set; } = DateTime.UtcNow;
        public string PlanId { get; set; } = string.Empty;
        public string PlanHash { get; set; } = string.Empty;
        public List<MigrationDryRunOperation> Operations { get; set; } = new List<MigrationDryRunOperation>();
        public bool HasBlockingIssues { get; set; }
        public List<string> Diagnostics { get; set; } = new List<string>();
    }

    public class MigrationPreflightCheck
    {
        public string Code { get; set; } = string.Empty;
        public MigrationPolicyDecision Decision { get; set; } = MigrationPolicyDecision.Pass;
        public string Message { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
    }

    public class MigrationPreflightReport
    {
        public DateTime CheckedOnUtc { get; set; } = DateTime.UtcNow;
        public string PlanId { get; set; } = string.Empty;
        public string PlanHash { get; set; } = string.Empty;
        public bool CanApply { get; set; }
        public bool SchemaDriftDetected { get; set; }
        public List<MigrationPreflightCheck> Checks { get; set; } = new List<MigrationPreflightCheck>();
    }

    public enum MigrationImpactSensitivity
    {
        Low,
        Medium,
        High
    }

    public class MigrationImpactEntry
    {
        public string EntityName { get; set; } = string.Empty;
        public MigrationPlanOperationKind Kind { get; set; } = MigrationPlanOperationKind.None;
        /// <summary>
        /// For AddForeignKey / DropForeignKey, the constraint name. For
        /// CreateIndex / DropIndex, the index name. Empty for non-relational
        /// op kinds. Mirrors <see cref="MigrationPlanOperation.TargetName"/>.
        /// </summary>
        public string TargetName { get; set; } = string.Empty;
        public MigrationImpactSensitivity Sensitivity { get; set; } = MigrationImpactSensitivity.Low;
        public List<string> UsageHints { get; set; } = new List<string>();
        public List<string> DataVolumeIndicators { get; set; } = new List<string>();
    }

    public class MigrationImpactReport
    {
        public DateTime GeneratedOnUtc { get; set; } = DateTime.UtcNow;
        public string PlanId { get; set; } = string.Empty;
        public string PlanHash { get; set; } = string.Empty;
        public List<MigrationImpactEntry> Entries { get; set; } = new List<MigrationImpactEntry>();
    }

    public enum MigrationExecutionStepStatus
    {
        Pending,
        Running,
        Completed,
        Failed,
        Skipped
    }

    public class MigrationExecutionStep
    {
        public int Sequence { get; set; }
        public string StepId { get; set; } = string.Empty;
        public List<int> DependsOn { get; set; } = new List<int>();
        public string EntityName { get; set; } = string.Empty;
        public string EntityTypeName { get; set; } = string.Empty;
        public MigrationPlanOperationKind OperationKind { get; set; } = MigrationPlanOperationKind.None;
        public List<string> MissingColumns { get; set; } = new List<string>();
        public MigrationExecutionStepStatus Status { get; set; } = MigrationExecutionStepStatus.Pending;
        public int AttemptCount { get; set; }
        public long ElapsedMilliseconds { get; set; }
        public string Message { get; set; } = string.Empty;
        /// <summary>
        /// Stable identifier for the target object of this step (constraint name for
        /// AddForeignKey/DropForeignKey, index name for CreateIndex/DropIndex). When
        /// empty the executor falls back to <see cref="StepId"/>, which is the
        /// synthetic "step-N" identifier.
        /// </summary>
        public string TargetName { get; set; } = string.Empty;
    }

    public class MigrationExecutionCheckpoint
    {
        public string ExecutionToken { get; set; } = Guid.NewGuid().ToString("N");
        public string CorrelationId { get; set; } = Guid.NewGuid().ToString("N");
        public string PlanId { get; set; } = string.Empty;
        public string PlanHash { get; set; } = string.Empty;
        public DateTime StartedOnUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedOnUtc { get; set; } = DateTime.UtcNow;
        public int LastCompletedStep { get; set; } = -1;
        public long ElapsedMilliseconds { get; set; }
        public bool IsCompleted { get; set; }
        public bool HasFailed { get; set; }
        public string FailureCategory { get; set; } = string.Empty;
        public string FailureReason { get; set; } = string.Empty;
        public List<MigrationExecutionStep> Steps { get; set; } = new List<MigrationExecutionStep>();
    }

    public class MigrationExecutionResult : IMigrationExecutionResult
    {
        public string ExecutionToken { get; set; } = string.Empty;
        public bool ResumedFromCheckpoint { get; set; }
        public bool Success { get; set; }
        public bool RequiresOperatorIntervention { get; set; }
        public string Message { get; set; } = string.Empty;
        public string RollbackOutcome { get; set; } = string.Empty;
        public string CompensationOutcome { get; set; } = string.Empty;
        public MigrationExecutionCheckpoint Checkpoint { get; set; } = new MigrationExecutionCheckpoint();
        public int AppliedCount { get; internal set; }

        /// <summary>
        /// Sequence numbers of steps that failed (after exhausting their retry budget).
        /// Empty when <c>Success = true</c>. Populated when the plan continues past
        /// failures (<c>policy.AbortOnStepFailure = false</c>); in the default
        /// "abort on first failure" policy, this list contains at most one entry.
        /// </summary>
        public List<int> FailedSteps { get; internal set; } = new List<int>();
    }

    public enum MigrationRollbackMode
    {
        ReversibleDdl,
        ForwardFixWithCompensation,
        ManualOnly
    }

    public class MigrationCompensationAction
    {
        public string ActionId { get; set; } = string.Empty;
        public int Sequence { get; set; }
        public string EntityName { get; set; } = string.Empty;
        public MigrationPlanOperationKind OperationKind { get; set; } = MigrationPlanOperationKind.None;
        public MigrationRollbackMode RollbackMode { get; set; } = MigrationRollbackMode.ForwardFixWithCompensation;
        public bool IsHighRisk { get; set; }
        public bool IsRequiredBeforeApply { get; set; }
        public string RollbackSqlPreview { get; set; } = string.Empty;
        public string CompensationPlaybook { get; set; } = string.Empty;
        /// <summary>
        /// Stable identifier for the target object of this compensation (constraint
        /// name for AddForeignKey/DropForeignKey, index name for CreateIndex/DropIndex).
        /// Mirrors <see cref="MigrationPlanOperation.TargetName"/>; populated when
        /// the action wraps a FK/Index op so the executor can run the right DDL
        /// without re-deriving the name from the original plan.
        /// </summary>
        public string TargetName { get; set; } = string.Empty;
    }

    public class MigrationCompensationPlan
    {
        public DateTime GeneratedOnUtc { get; set; } = DateTime.UtcNow;
        public string PlanId { get; set; } = string.Empty;
        public string PlanHash { get; set; } = string.Empty;
        public List<MigrationCompensationAction> Actions { get; set; } = new List<MigrationCompensationAction>();
    }

    public class MigrationRollbackReadinessCheck
    {
        public string Code { get; set; } = string.Empty;
        public MigrationPolicyDecision Decision { get; set; } = MigrationPolicyDecision.Pass;
        public string Message { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
    }

    public class MigrationRollbackReadinessReport
    {
        public DateTime CheckedOnUtc { get; set; } = DateTime.UtcNow;
        public string PlanId { get; set; } = string.Empty;
        public string PlanHash { get; set; } = string.Empty;
        public bool BackupConfirmed { get; set; }
        public bool RestoreTestEvidenceProvided { get; set; }
        public string RestoreTestEvidence { get; set; } = string.Empty;
        public bool IsReady { get; set; }
        public List<MigrationRollbackReadinessCheck> Checks { get; set; } = new List<MigrationRollbackReadinessCheck>();
    }

    public class MigrationRollbackResult
    {
        public string ExecutionToken { get; set; } = string.Empty;
        public bool DryRun { get; set; } = true;
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> ExecutedActions { get; set; } = new List<string>();
    }

    public enum MigrationDiagnosticSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }

    public class MigrationDiagnosticEntry
    {
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
        public string ExecutionToken { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = string.Empty;
        public string OperationCode { get; set; } = string.Empty;
        public MigrationDiagnosticSeverity Severity { get; set; } = MigrationDiagnosticSeverity.Info;
        public string EntityName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
    }

    public class MigrationAuditEvent
    {
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
        public string ExecutionToken { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = string.Empty;
        public string PlanId { get; set; } = string.Empty;
        public string PlanHash { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string ApprovedBy { get; set; } = string.Empty;
        public string ExecutedBy { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public MigrationPlanOperationKind OperationKind { get; set; } = MigrationPlanOperationKind.None;
        public string TargetName { get; set; } = string.Empty;
    }

    public class MigrationTelemetryMetrics
    {
        public long PlanCount { get; set; }
        public long ExecutionCount { get; set; }
        public long SuccessCount { get; set; }
        public long FailureCount { get; set; }
        public long RetryCount { get; set; }
        public long RollbackCount { get; set; }
        public long PolicyBlockCount { get; set; }
        public long TotalStepDurationMilliseconds { get; set; }
        public long StepDurationSamples { get; set; }
        public double AverageStepDurationMilliseconds => StepDurationSamples <= 0 ? 0 : (double)TotalStepDurationMilliseconds / StepDurationSamples;
        public double PolicyBlockRatio => PlanCount <= 0 ? 0 : (double)PolicyBlockCount / PlanCount;
        /// <summary>
        /// Per-operation-kind completion counts since process start. Keys are
        /// the <see cref="MigrationPlanOperationKind"/> string (e.g. "AddForeignKey",
        /// "CreateIndex"). Values are total completions (success + failure).
        /// Operators can correlate spike in CreateIndex failures with a recent
        /// platform upgrade or a large table.
        /// </summary>
        public Dictionary<string, long> OperationKindCounts { get; set; } = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        /// Per-operation-kind failure counts since process start. Same key
        /// shape as <see cref="OperationKindCounts"/>.
        /// </summary>
        public Dictionary<string, long> OperationKindFailureCounts { get; set; } = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        /// Per-operation-kind total execution duration in milliseconds since
        /// process start. Keyed by <see cref="MigrationPlanOperationKind"/>
        /// string. Operators can identify whether AddForeignKey steps
        /// average 18ms or 18000ms, and compare against baseline estimates.
        /// </summary>
        public Dictionary<string, long> OperationKindTotalDurationMilliseconds { get; set; } = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
    }

    public class MigrationTelemetrySnapshot
    {
        public DateTime CapturedOnUtc { get; set; } = DateTime.UtcNow;
        public MigrationTelemetryMetrics Metrics { get; set; } = new MigrationTelemetryMetrics();
        public double SuccessRate { get; set; }
        public double FailureRate { get; set; }
        public Dictionary<string, int> DiagnosticsBySeverity { get; set; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        public List<MigrationDiagnosticEntry> Diagnostics { get; set; } = new List<MigrationDiagnosticEntry>();
        public List<MigrationAuditEvent> AuditEvents { get; set; } = new List<MigrationAuditEvent>();
    }

    public enum MigrationExecutionWindowMode
    {
        OnlinePreferred,
        MaintenanceWindowRequired
    }

    public class MigrationOperationScaleAnnotation
    {
        public string EntityName { get; set; } = string.Empty;
        public MigrationPlanOperationKind OperationKind { get; set; } = MigrationPlanOperationKind.None;
        /// <summary>
        /// For AddForeignKey / DropForeignKey, the constraint name. For
        /// CreateIndex / DropIndex, the index name. Empty for non-relational
        /// op kinds. Mirrors <see cref="MigrationPlanOperation.TargetName"/>.
        /// </summary>
        public string TargetName { get; set; } = string.Empty;
        /// <summary>
        /// Risk level copied from <see cref="MigrationPlanOperation.RiskLevel"/>.
        /// Useful when correlating a high lock score with the operation's
        /// intrinsic risk classification.
        /// </summary>
        public MigrationPlanRiskLevel RiskLevel { get; set; } = MigrationPlanRiskLevel.Low;
        public MigrationExecutionWindowMode WindowMode { get; set; } = MigrationExecutionWindowMode.OnlinePreferred;
        public int EstimatedRuntimeSeconds { get; set; }
        public int EstimatedLockImpactScore { get; set; }
        public string Note { get; set; } = string.Empty;
    }

    public class MigrationPerformancePolicy
    {
        public int BatchSize { get; set; } = 10;
        public int ThrottleDelayMilliseconds { get; set; } = 0;
        public int LockTimeoutMilliseconds { get; set; } = 30000;
        public bool EnableThrottledMode { get; set; } = false;
        public MigrationExecutionWindowMode PreferredWindowMode { get; set; } = MigrationExecutionWindowMode.OnlinePreferred;
    }

    public class MigrationPerformanceKpi
    {
        public int PlannedMigrationWindowMinutes { get; set; } = 30;
        public int MaxAllowedLockWaitMilliseconds { get; set; } = 5000;
        public int TargetOperationsPerMinute { get; set; } = 20;
    }

    public class MigrationPerformancePlan
    {
        public DateTime GeneratedOnUtc { get; set; } = DateTime.UtcNow;
        public string PlanId { get; set; } = string.Empty;
        public string PlanHash { get; set; } = string.Empty;
        public MigrationPerformancePolicy Policy { get; set; } = new MigrationPerformancePolicy();
        public MigrationPerformanceKpi Kpis { get; set; } = new MigrationPerformanceKpi();
        public List<MigrationOperationScaleAnnotation> OperationAnnotations { get; set; } = new List<MigrationOperationScaleAnnotation>();
        public List<string> MaintenanceWindowGuidance { get; set; } = new List<string>();
        public List<string> TimeoutGuidance { get; set; } = new List<string>();
    }

    public class MigrationCiGateResult
    {
        public string Gate { get; set; } = string.Empty;
        public MigrationPolicyDecision Decision { get; set; } = MigrationPolicyDecision.Pass;
        public string Message { get; set; } = string.Empty;
    }

    public class MigrationCiValidationReport
    {
        public DateTime GeneratedOnUtc { get; set; } = DateTime.UtcNow;
        public string PlanId { get; set; } = string.Empty;
        public string PlanHash { get; set; } = string.Empty;
        public bool CanMerge { get; set; }
        public List<MigrationCiGateResult> Gates { get; set; } = new List<MigrationCiGateResult>();
    }

    public class MigrationDevExArtifactBundle
    {
        public DateTime GeneratedOnUtc { get; set; } = DateTime.UtcNow;
        public string PlanId { get; set; } = string.Empty;
        public string PlanHash { get; set; } = string.Empty;
        public string PlanJson { get; set; } = string.Empty;
        public string DryRunJson { get; set; } = string.Empty;
        public string CiValidationJson { get; set; } = string.Empty;
        public string ApprovalReportMarkdown { get; set; } = string.Empty;
        public string PerformancePlanJson { get; set; } = string.Empty;
        public string CompensationPlanJson { get; set; } = string.Empty;
        public string RollbackReadinessJson { get; set; } = string.Empty;
    }

    public enum MigrationRolloutWave
    {
        Wave1NonCritical,
        Wave2StandardProduction,
        Wave3Critical
    }

    public class MigrationRolloutKpiThresholds
    {
        public double MinSuccessRate { get; set; } = 0.95;
        public double MaxMeanExecutionDurationMilliseconds { get; set; } = 120000;
        public double MaxRollbackInvocationRate { get; set; } = 0.10;
        public double MaxPolicyBlockRatio { get; set; } = 0.25;
    }

    public class MigrationRolloutHardStopPolicy
    {
        public bool StopOnAnyCriticalDiagnostic { get; set; } = true;
        public bool StopOnAnyRollbackForCriticalWave { get; set; } = true;
        public double MaxFailureRate { get; set; } = 0.10;
    }

    public class MigrationRolloutGovernanceRequest
    {
        public MigrationRolloutWave Wave { get; set; } = MigrationRolloutWave.Wave1NonCritical;
        public bool IsCriticalDataSource { get; set; }
        public string ReviewedBy { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public MigrationRolloutKpiThresholds Thresholds { get; set; } = new MigrationRolloutKpiThresholds();
        public MigrationRolloutHardStopPolicy HardStopPolicy { get; set; } = new MigrationRolloutHardStopPolicy();
    }

    public class MigrationRolloutKpiSnapshot
    {
        public double SuccessRate { get; set; }
        public double MeanExecutionDurationMilliseconds { get; set; }
        public double RollbackInvocationRate { get; set; }
        public double PolicyBlockRatio { get; set; }
    }

    public class MigrationRolloutGateResult
    {
        public string Gate { get; set; } = string.Empty;
        public MigrationPolicyDecision Decision { get; set; } = MigrationPolicyDecision.Pass;
        public string Observed { get; set; } = string.Empty;
        public string Threshold { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class MigrationRolloutGovernanceReport
    {
        public DateTime GeneratedOnUtc { get; set; } = DateTime.UtcNow;
        public string PlanId { get; set; } = string.Empty;
        public string PlanHash { get; set; } = string.Empty;
        public MigrationRolloutWave Wave { get; set; } = MigrationRolloutWave.Wave1NonCritical;
        public bool CanPromote { get; set; }
        public bool HardStopTriggered { get; set; }
        public string HardStopReason { get; set; } = string.Empty;
        public string ReviewedBy { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public MigrationRolloutKpiSnapshot Kpis { get; set; } = new MigrationRolloutKpiSnapshot();
        public List<MigrationRolloutGateResult> Gates { get; set; } = new List<MigrationRolloutGateResult>();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Phase 1 – Entity Migration Pipeline (source provenance + shared pipeline)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Which source channel supplied a particular entity type to the pipeline.</summary>
    public enum EntityMigrationSource
    {
        Explicit,
        DiscoveryAssembly,
        DiscoveryFileSystem,
        /// <summary>
        /// Entity types resolved from an Entity Framework Core <see cref="Microsoft.EntityFrameworkCore.Metadata.IModel"/>
        /// (typically via a <see cref="Microsoft.EntityFrameworkCore.DbContext"/>).
        /// </summary>
        DiscoveryEFCoreModel
    }

    /// <summary>Per-entity outcome from the migration pipeline.</summary>
    public enum EntityMigrationDecision
    {
        Create,
        Update,
        NoChange,
        Error,
        Skipped
    }

    /// <summary>Metadata recorded per entity as it enters the migration pipeline.</summary>
    public class EntityMigrationMetadata
    {
        public string TypeFullName { get; set; } = string.Empty;
        public string AssemblyName { get; set; } = string.Empty;
        public EntityMigrationSource Source { get; set; } = EntityMigrationSource.Explicit;
        /// <summary>"Exact" | "SubNamespace" | "None" — namespace scope used during assembly discovery.</summary>
        public string NamespaceMatchMode { get; set; } = string.Empty;
        /// <summary>Namespace prefix declared in the manifest entry (manifest-sourced entities only).</summary>
        public string NamespacePrefix { get; set; } = string.Empty;
    }

    /// <summary>Per-entity result produced by the shared pipeline.</summary>
    public class EntityMigrationResultEntry
    {
        public string EntityName { get; set; } = string.Empty;
        public string TypeFullName { get; set; } = string.Empty;
        public EntityMigrationDecision Decision { get; set; } = EntityMigrationDecision.NoChange;
        /// <summary>Stable code string, e.g. "ENTITY-CREATED", "ENTITY-UPDATED", "ENTITY-UP-TO-DATE", "ENTITY-ERROR".</summary>
        public string DecisionReasonCode { get; set; } = string.Empty;
        public EntityMigrationSource Source { get; set; } = EntityMigrationSource.Explicit;
        public string AssemblyName { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Normalized result produced by the shared entity migration pipeline used by both
    /// explicit-type and discovery-based migration paths.
    /// </summary>
    public class EntityPipelineResult
    {
        public int Scanned { get; set; }
        public int Processed { get; set; }
        public int Created { get; set; }
        public int Updated { get; set; }
        public int Skipped { get; set; }
        public int ErrorCount { get; set; }
        public List<string> BlockingReasons { get; set; } = new List<string>();
        public List<EntityMigrationResultEntry> Entries { get; set; } = new List<EntityMigrationResultEntry>();
        public bool HasBlockingReasons => BlockingReasons.Count > 0;

        public string ToSummaryString()
        {
            var s = $"Created {Created}, updated {Updated}, skipped {Skipped}";
            if (ErrorCount > 0) s += $", {ErrorCount} error(s)";
            return s;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Phase 1 – Manifest Parser (file-based entity discovery)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A single parsed record from a migration manifest .txt file.
    /// Format per line: TypeFullName[|AssemblyHint[|NamespacePrefix]]
    /// </summary>
    public class MigrationManifestEntry
    {
        public int LineNumber { get; set; }
        public string TypeFullName { get; set; } = string.Empty;
        /// <summary>Optional assembly simple name or full name hint.</summary>
        public string AssemblyHint { get; set; } = string.Empty;
        /// <summary>Optional namespace prefix to restrict resolution scope.</summary>
        public string NamespacePrefix { get; set; } = string.Empty;
    }

    /// <summary>Parse error for a manifest line. Error codes: MIG-MANIFEST-001..004.</summary>
    public class MigrationManifestParseError
    {
        /// <summary>
        /// MIG-MANIFEST-001 = invalid segment count (not 1–3).
        /// MIG-MANIFEST-002 = empty type name segment.
        /// MIG-MANIFEST-003 = unresolved type after resolution pipeline.
        /// MIG-MANIFEST-004 = duplicate entity declaration.
        /// </summary>
        public string Code { get; set; } = string.Empty;
        public int LineNumber { get; set; }
        public string LineContent { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>Result of parsing a migration manifest file.</summary>
    public class MigrationManifestParseResult
    {
        public string FilePath { get; set; } = string.Empty;
        public List<MigrationManifestEntry> Entries { get; set; } = new List<MigrationManifestEntry>();
        public List<MigrationManifestParseError> Errors { get; set; } = new List<MigrationManifestParseError>();
        public bool HasErrors => Errors.Count > 0;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Phase 2 – Readiness / Summary Reporting (machine-grade contracts)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Log channel for a <see cref="MigrationReadinessIssue"/> — enables CI to filter by type.</summary>
    public enum MigrationReportChannel
    {
        ReadinessIssue,
        BestPractice,
        MigrationDecision
    }

    /// <summary>Per-entity migration decision recorded in the summary for machine consumption.</summary>
    public class EntityDecisionRecord
    {
        public string EntityName { get; set; } = string.Empty;
        public EntityMigrationDecision Decision { get; set; } = EntityMigrationDecision.NoChange;
        /// <summary>Stable reason code, e.g. "ENTITY-CREATED", "COLUMNS-ADDED", "ENTITY-UP-TO-DATE".</summary>
        public string DecisionReasonCode { get; set; } = string.Empty;
        /// <summary>Key capability facts used to reach this decision (e.g. SupportsSchemaEvolution=true).</summary>
        public Dictionary<string, string> CapabilityContextSnapshot { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public EntityMigrationSource Source { get; set; } = EntityMigrationSource.Explicit;
        public string AssemblyName { get; set; } = string.Empty;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Phase 3 – DDL Operation Outcomes (classified DDL evidence)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Classified outcome of a single DDL operation.</summary>
    public enum DdlOperationOutcome
    {
        /// <summary>DDL was generated and executed successfully.</summary>
        Executed,
        /// <summary>No DDL was needed (entity/column already correct).</summary>
        NoOp,
        /// <summary>The provider does not support this DDL operation.</summary>
        Unsupported,
        /// <summary>DDL was emulated via non-DDL means (e.g. file mutation, table rebuild).</summary>
        Emulated,
        /// <summary>DDL failed.</summary>
        Failed
    }

    /// <summary>Which helper stack generated or executed the DDL.</summary>
    public enum DdlHelperSource
    {
        /// <summary>Universal RDBMS helper (RdbmsHelper in UniversalDataSourceHelpers).</summary>
        UniversalRdbmsHelper,
        /// <summary>Legacy RDBMS facade (RDBMSHelper).</summary>
        LegacyRdbmsFacade,
        /// <summary>File-mutation path for file-based providers.</summary>
        FileMutation,
        /// <summary>Direct datasource API — no helper DDL generated.</summary>
        Direct
    }

    /// <summary>
    /// Audit evidence emitted per DDL operation. Enables distinguishing helper SQL execution
    /// from file emulation paths and no-op outcomes.
    /// </summary>
    public class DdlOperationEvidence
    {
        public string OperationId { get; set; } = Guid.NewGuid().ToString("N");
        public string OperationName { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string ColumnName { get; set; } = string.Empty;
        public string IndexName { get; set; } = string.Empty;
        public DdlOperationOutcome Outcome { get; set; } = DdlOperationOutcome.NoOp;
        public DdlHelperSource HelperSource { get; set; } = DdlHelperSource.Direct;
        /// <summary>SHA256 prefix of the generated SQL (empty when no SQL was produced).</summary>
        public string SqlHash { get; set; } = string.Empty;
        /// <summary>Stable reason code, e.g. "DDL-UNSUPPORTED-001", "DDL-NOOP-NO-HELPER".</summary>
        public string ReasonCode { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Phase 4 – Assembly Discovery Evidence (deterministic scan ordering)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Ranked source category for an assembly contributed to entity discovery.</summary>
    public enum AssemblySourceKind
    {
        /// <summary>Explicitly registered via RegisterAssembly.</summary>
        ManualRegistered,
        /// <summary>Entry assembly or one of its static references.</summary>
        EntryReference,
        /// <summary>Calling assembly or one of its static references.</summary>
        CallingReference,
        /// <summary>Already loaded in AppDomain.CurrentDomain.</summary>
        AppDomainLoaded,
        /// <summary>Loaded via DMEEditor.assemblyHandler (plugins).</summary>
        AssemblyHandlerPlugin
    }

    /// <summary>Per-assembly record produced during entity discovery.</summary>
    public class AssemblyDiscoveryRecord
    {
        public string AssemblyFullName { get; set; } = string.Empty;
        public string AssemblyName { get; set; } = string.Empty;
        public AssemblySourceKind Source { get; set; } = AssemblySourceKind.AppDomainLoaded;
        public bool Scanned { get; set; }
        public bool Skipped { get; set; }
        /// <summary>Why the assembly was excluded from scanning (non-null when Skipped = true).</summary>
        public string SkipReason { get; set; } = string.Empty;
        public List<string> LoaderExceptions { get; set; } = new List<string>();
        public List<string> FoundEntityTypes { get; set; } = new List<string>();
    }

    /// <summary>
    /// Discovery evidence snapshot produced by <see cref="IMigrationManager.DiscoverEntityTypes"/>.
    /// Supports troubleshooting "no entities found" without enabling ad-hoc debug logging.
    /// </summary>
    public class AssemblyDiscoveryEvidence
    {
        public int TotalAssembliesConsidered { get; set; }
        public int Scanned { get; set; }
        public int Skipped { get; set; }
        public List<AssemblyDiscoveryRecord> Records { get; set; } = new List<AssemblyDiscoveryRecord>();
        /// <summary>TypeFullName → AssemblyName mapping for all discovered entity types.</summary>
        public Dictionary<string, string> EntityTypeOriginMap { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        /// <summary>Diagnostics produced when zero entities are found for a non-null namespace filter.</summary>
        public List<string> NamespaceScopeDiagnostics { get; set; } = new List<string>();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Phase 6 – ORM-shaped MigrationModel POCO
    // ─────────────────────────────────────────────────────────────────────────
    //
    // BeepDM does not depend on any ORM package. Instead it accepts a populated
    // MigrationModel POCO from the caller. The caller is free to populate it from
    // EF Core, NHibernate, Dapper, hand-rolled code, or a JSON file — BeepDM only
    // sees the POCO.

    /// <summary>
    /// BeepDM-owned, ORM-agnostic description of a model shape that an external
    /// framework (typically an ORM such as EF Core) has produced. Callers populate
    /// this POCO themselves and hand it to MigrationManager; the engine does not
    /// import or reference any ORM package.
    /// </summary>
    public class MigrationModel
    {
        /// <summary>Logical name of the originating framework, e.g. "EntityFrameworkCore", "NHibernate", "Manual".</summary>
        public string Source { get; set; } = "Manual";
        /// <summary>Semantic version of the originating framework, if known (e.g. "9.0.0").</summary>
        public string SourceVersion { get; set; } = string.Empty;
        /// <summary>Caller-defined opaque identifier, e.g. a DbContext type name.</summary>
        public string SourceId { get; set; } = string.Empty;
        /// <summary>Entities in the model, keyed by CLR type full name for fast lookup.</summary>
        public Dictionary<string, MigrationModelEntity> Entities { get; set; }
            = new Dictionary<string, MigrationModelEntity>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// A single entity inside a <see cref="MigrationModel"/>. Maps to one logical
    /// table on the target datasource.
    /// </summary>
    public class MigrationModelEntity
    {
        /// <summary>CLR type full name (used as the stable lookup key).</summary>
        public string ClrTypeFullName { get; set; } = string.Empty;
        /// <summary>Target table name (e.g. "Customers").</summary>
        public string TableName { get; set; } = string.Empty;
        /// <summary>Target schema/owner/database; empty when the default applies.</summary>
        public string Schema { get; set; } = string.Empty;
        /// <summary>True for query / keyless types that have no table backing them.</summary>
        public bool IsKeyless { get; set; }
        /// <summary>True for abstract types that are not migrated directly.</summary>
        public bool IsAbstract { get; set; }
        /// <summary>Properties in declaration order (the order callers intend to map).</summary>
        public List<MigrationModelProperty> Properties { get; set; } = new List<MigrationModelProperty>();
        /// <summary>Indexes declared on this entity.</summary>
        public List<MigrationModelIndex> Indexes { get; set; } = new List<MigrationModelIndex>();
        /// <summary>Foreign keys declared on this entity.</summary>
        public List<MigrationModelForeignKey> ForeignKeys { get; set; } = new List<MigrationModelForeignKey>();
    }

    /// <summary>
    /// A single property / column on a <see cref="MigrationModelEntity"/>.
    /// </summary>
    public class MigrationModelProperty
    {
        /// <summary>CLR property name (used as the stable lookup key on the entity).</summary>
        public string PropertyName { get; set; } = string.Empty;
        /// <summary>Target column name (defaults to PropertyName when blank).</summary>
        public string ColumnName { get; set; } = string.Empty;
        /// <summary>.NET type full name (e.g. "System.String", "System.Int32", "System.DateTime").</summary>
        public string FieldType { get; set; } = string.Empty;
        /// <summary>Native SQL type hint (e.g. "nvarchar(255)", "decimal(18,2)").</summary>
        public string ColumnType { get; set; } = string.Empty;
        public bool IsNullable { get; set; } = true;
        public int? MaxLength { get; set; }
        public byte? Precision { get; set; }
        public byte? Scale { get; set; }
        public bool IsPrimaryKey { get; set; }
        public bool IsIdentity { get; set; }
        public bool IsRowVersion { get; set; }
        public bool IsUnique { get; set; }
        public bool IsIndexed { get; set; }
        public string DefaultValueSql { get; set; } = string.Empty;
        public string ComputedColumnSql { get; set; } = string.Empty;
        /// <summary>Optional BeepDM DbFieldCategory override. When zero, BeepDM infers it from FieldType.</summary>
        public DbFieldCategory FieldCategoryHint { get; set; }
    }

    /// <summary>Index descriptor inside a <see cref="MigrationModelEntity"/>.</summary>
    public class MigrationModelIndex
    {
        public string Name { get; set; } = string.Empty;
        public List<string> Columns { get; set; } = new List<string>();
        public bool IsUnique { get; set; }
    }

    /// <summary>Foreign key descriptor inside a <see cref="MigrationModelEntity"/>.</summary>
    public class MigrationModelForeignKey
    {
        public string ConstraintName { get; set; } = string.Empty;
        public List<string> Columns { get; set; } = new List<string>();
        public string PrincipalTable { get; set; } = string.Empty;
        public string PrincipalSchema { get; set; } = string.Empty;
        public List<string> PrincipalColumns { get; set; } = new List<string>();
        /// <summary>String form of the on-delete behaviour ("Cascade", "Restrict", "SetNull", "NoAction", "ClientCascade", "ClientNoAction", "ClientSetNull").</summary>
        public string OnDeleteBehavior { get; set; } = string.Empty;
        /// <summary>String form of the on-update behaviour (same vocabulary as <see cref="OnDeleteBehavior"/>).</summary>
        public string OnUpdateBehavior { get; set; } = string.Empty;
    }

    /// <summary>
    /// Evidence captured during the most recent
    /// <see cref="IMigrationManager.BuildMigrationPlanForModel"/> call. Includes the
    /// per-entity reflection record, the model fingerprint, and any cross-cutting diagnostics.
    /// </summary>
    public class MigrationModelEvidence
    {
        public int EntityTypeCount { get; set; }
        public int KeylessTypeCount { get; set; }
        public int ForeignKeyCount { get; set; }
        public int IndexCount { get; set; }
        public string Source { get; set; } = string.Empty;
        public string SourceVersion { get; set; } = string.Empty;
        public string SourceId { get; set; } = string.Empty;
        /// <summary>Entity CLR type name → record map for the model that was reflected.</summary>
        public Dictionary<string, MigrationModelEntityRecord> Entities { get; set; }
            = new Dictionary<string, MigrationModelEntityRecord>(StringComparer.OrdinalIgnoreCase);
        /// <summary>Stable hash of the entire model evidence (table+schema+column shape across all entities).</summary>
        public string ModelHash { get; set; } = string.Empty;
        /// <summary>Cross-cutting warnings emitted while reflecting the model (e.g. duplicate table mapping).</summary>
        public List<string> Diagnostics { get; set; } = new List<string>();
    }

    /// <summary>Per-entity record produced while reflecting a <see cref="MigrationModelEntity"/>.</summary>
    public class MigrationModelEntityRecord
    {
        public string ClrTypeFullName { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public string Schema { get; set; } = string.Empty;
        public List<string> PropertyNames { get; set; } = new List<string>();
        public List<string> PrimaryKey { get; set; } = new List<string>();
        public List<MigrationModelIndex> Indexes { get; set; } = new List<MigrationModelIndex>();
        public List<MigrationModelForeignKey> ForeignKeys { get; set; } = new List<MigrationModelForeignKey>();
        public string RecordHash { get; set; } = string.Empty;
        public List<string> Warnings { get; set; } = new List<string>();
    }


    // ─────────────────────────────────────────────────────────────────────────
    // Phase 5 – Recommendation Profiles (versioned best-practice intelligence)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Severity level for a structured recommendation entry.</summary>
    public enum RecommendationSeverity
    {
        Info,
        Warning,
        Critical
    }

    /// <summary>A single structured recommendation with stable id, rationale and capability provenance.</summary>
    public class RecommendationEntry
    {
        /// <summary>Stable id, e.g. "REC-RDBMS-001". Referenced from readiness issues via RecommendationId.</summary>
        public string Id { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public RecommendationSeverity Severity { get; set; } = RecommendationSeverity.Info;
        public string Rationale { get; set; } = string.Empty;
        /// <summary>Capability probe names that influence whether this recommendation applies.</summary>
        public List<string> CapabilityDependencies { get; set; } = new List<string>();
        /// <summary>
        /// Which helper/manager produced the capability evidence for this recommendation.
        /// Values: "UniversalDataSourceHelpers" | "RDBMSHelpers" | "MigrationManager"
        /// </summary>
        public string CapabilitySource { get; set; } = string.Empty;
    }

    /// <summary>Versioned recommendation profile scoped to a datasource type/category pair.</summary>
    public class RecommendationProfile
    {
        public string ProfileId { get; set; } = string.Empty;
        public string ProfileVersion { get; set; } = "1.0";
        public DataSourceType DataSourceType { get; set; } = DataSourceType.Unknown;
        public DatasourceCategory DataSourceCategory { get; set; } = DatasourceCategory.NONE;
        public List<RecommendationEntry> Recommendations { get; set; } = new List<RecommendationEntry>();
        /// <summary>Organization-specific overrides: recommendation id → override text.</summary>
        public Dictionary<string, string> PolicyOverlays { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Augmented existing DTOs (additions only — backward compatible)
    // ─────────────────────────────────────────────────────────────────────────

    public interface IMigrationManager
    {
        IDMEEditor DMEEditor { get; }
        IDataSource MigrateDataSource { get; set; }

        // ── Entity-level operations ──

        IErrorsInfo EnsureEntity(EntityStructure entity, bool createIfMissing = true, bool addMissingColumns = true, bool applyForeignKeys = false, bool applyIndexes = false);
        IErrorsInfo EnsureEntity(Type pocoType, bool createIfMissing = true, bool addMissingColumns = true, bool detectRelationships = true, bool applyForeignKeys = false, bool applyIndexes = false);
        IReadOnlyList<EntityField> GetMissingColumns(EntityStructure current, EntityStructure desired);

        IErrorsInfo CreateEntity(EntityStructure entity);
        IErrorsInfo DropEntity(string entityName);
        IErrorsInfo TruncateEntity(string entityName);
        IErrorsInfo RenameEntity(string oldName, string newName);
        IErrorsInfo AlterColumn(string entityName, string columnName, EntityField newColumn);
        IErrorsInfo DropColumn(string entityName, string columnName);
        IErrorsInfo RenameColumn(string entityName, string oldColumnName, string newColumnName);
        IErrorsInfo CreateIndex(string entityName, string indexName, string[] columns, Dictionary<string, object> options = null);

        /// <summary>
        /// Drops an index by name. Mirrors <see cref="CreateIndex"/> for the
        /// <see cref="MigrationPlanOperationKind.DropIndex"/> plan operation.
        /// </summary>
        IErrorsInfo DropIndex(string entityName, string indexName);

        /// <summary>
        /// Adds a foreign key constraint between two entities on the migration datasource.
        /// </summary>
        /// <param name="entityName">Dependent (child) table.</param>
        /// <param name="columnNames">Foreign-key column(s) on the dependent table.</param>
        /// <param name="referencedEntityName">Referenced (principal) table.</param>
        /// <param name="referencedColumnNames">Referenced column(s) on the principal table.</param>
        /// <param name="onDeleteBehavior">One of "Cascade", "Restrict", "SetNull", "NoAction". Default: "Cascade".</param>
        /// <param name="onUpdateBehavior">One of "Cascade", "Restrict", "SetNull", "NoAction". Default: "Cascade".</param>
        /// <param name="constraintName">Optional explicit constraint name.</param>
        IErrorsInfo AddForeignKey(
            string entityName,
            string[] columnNames,
            string referencedEntityName,
            string[] referencedColumnNames,
            string onDeleteBehavior = "Cascade",
            string onUpdateBehavior = "Cascade",
            string constraintName = null);

        /// <summary>
        /// Drops a foreign key constraint by name.
        /// </summary>
        IErrorsInfo DropForeignKey(string entityName, string constraintName);

        // ── Assembly registration for cross-project discovery ──

        /// <summary>
        /// Register additional assemblies for entity type discovery.
        /// Use this when entity classes live in separate projects/DLLs that may not be
        /// automatically found by AppDomain scanning (e.g., lazily-loaded assemblies).
        /// </summary>
        void RegisterAssembly(Assembly assembly);

        /// <summary>
        /// Register multiple assemblies for entity type discovery.
        /// </summary>
        void RegisterAssemblies(IEnumerable<Assembly> assemblies);

        /// <summary>
        /// Gets all currently registered assemblies (manual + auto-discovered).
        /// Useful for diagnostics when entity types are not being found.
        /// </summary>
        IReadOnlyList<Assembly> GetRegisteredAssemblies();

        // ── Entity type discovery ──

        /// <summary>
        /// Discovers all types that inherit from Entity in the specified namespace(s).
        /// Searches in the given assembly, registered assemblies, AppDomain assemblies,
        /// the entry assembly and its referenced assemblies, and DMEEditor's assembly handler.
        /// </summary>
        List<Type> DiscoverEntityTypes(string namespaceName = null, Assembly assembly = null, bool includeSubNamespaces = true);

        /// <summary>
        /// Discovers all types that inherit from Entity in all searchable assemblies.
        /// Scans registered assemblies, AppDomain, entry assembly references, and DMEEditor's assembly handler.
        /// </summary>
        List<Type> DiscoverAllEntityTypes(bool includeSubNamespaces = true);

        // ── Database-level migration (discovery-based) ──

        /// <summary>
        /// Ensures database is created with all discovered Entity types.
        /// Similar to EF Core's Database.EnsureCreated().
        /// Creates entities for all classes that inherit from Entity in the given namespace/assembly.
        /// Existing entities are counted as skipped; missing columns are not added by this ensure-created path.
        /// </summary>
        IErrorsInfo EnsureDatabaseCreated(string namespaceName = null, Assembly assembly = null, bool detectRelationships = true, IProgress<PassedArgs> progress = null, bool applyForeignKeys = false, bool applyIndexes = false);

        /// <summary>
        /// Applies migrations for all discovered Entity types.
        /// Compares Entity classes with database schema and applies changes.
        /// Similar to EF Core's Database.Migrate().
        /// </summary>
        IErrorsInfo ApplyMigrations(string namespaceName = null, Assembly assembly = null, bool detectRelationships = true, bool addMissingColumns = true, IProgress<PassedArgs> progress = null, bool applyForeignKeys = false, bool applyIndexes = false);

        /// <summary>
        /// Gets migration summary comparing Entity classes with current database state.
        /// Returns list of entities that need creation or updates.
        /// <para>
        /// <paramref name="applyForeignKeys"/> and <paramref name="applyIndexes"/> are
        /// stamped on the returned <see cref="MigrationSummary.Diagnostics"/> as
        /// informational entries (the summary itself never applies schema changes).
        /// </para>
        /// </summary>
        MigrationSummary GetMigrationSummary(string namespaceName = null, Assembly assembly = null, bool detectRelationships = true, bool applyForeignKeys = false, bool applyIndexes = false);

        /// <summary>
        /// Gets migration summary comparing the supplied entity types with the current
        /// database state. Bypasses discovery — this is the type-driven counterpart of
        /// <see cref="GetMigrationSummary(string, Assembly, bool, bool, bool)"/>.
        /// </summary>
        /// <param name="applyForeignKeys">Record the intended apply-foreign-keys flag on the summary for downstream consumers.</param>
        /// <param name="applyIndexes">Record the intended apply-indexes flag on the summary for downstream consumers.</param>
        MigrationSummary GetMigrationSummaryForTypes(IEnumerable<Type> entityTypes, bool detectRelationships = true, bool applyForeignKeys = false, bool applyIndexes = false);

        /// <summary>
        /// Builds a datasource-aware readiness report for discovery-based migrations.
        /// Use this before running migrations in enterprise environments where platform behavior,
        /// naming standards, and operational risk need explicit review.
        /// <para>
        /// <paramref name="applyForeignKeys"/> and <paramref name="applyIndexes"/> are
        /// stamped on the returned <see cref="MigrationReadinessReport.Issues"/> as
        /// <see cref="MigrationIssueSeverity.Info"/> entries so consumers can audit the
        /// intended apply behavior. The report itself never applies schema changes.
        /// </para>
        /// </summary>
        MigrationReadinessReport GetMigrationReadiness(string namespaceName = null, Assembly assembly = null, bool detectRelationships = true, bool applyForeignKeys = false, bool applyIndexes = false);

        // ── Database-level migration (explicit types — no discovery needed) ──

        /// <summary>
        /// Ensures database is created for the given entity types.
        /// Use this when you know exactly which types to create — bypasses assembly discovery entirely.
        /// This is the most reliable approach for cross-project scenarios.
        /// </summary>
        /// <example>
        /// migrationManager.EnsureDatabaseCreatedForTypes(
        ///     new[] { typeof(Customer), typeof(Product), typeof(Invoice) },
        ///     progress: progressReporter);
        /// </example>
        IErrorsInfo EnsureDatabaseCreatedForTypes(IEnumerable<Type> entityTypes, bool detectRelationships = true, IProgress<PassedArgs> progress = null, bool applyForeignKeys = false, bool applyIndexes = false);

        /// <summary>
        /// Applies migrations for the given entity types.
        /// Use this when you know exactly which types to migrate — bypasses assembly discovery entirely.
        /// </summary>
        IErrorsInfo ApplyMigrationsForTypes(IEnumerable<Type> entityTypes, bool detectRelationships = true, bool addMissingColumns = true, IProgress<PassedArgs> progress = null, bool applyForeignKeys = false, bool applyIndexes = false);

        /// <summary>
        /// Builds a datasource-aware readiness report for explicit-type migrations.
        /// <para>
        /// <paramref name="applyForeignKeys"/> and <paramref name="applyIndexes"/> are
        /// stamped on the returned <see cref="MigrationReadinessReport.Issues"/> as
        /// <see cref="MigrationIssueSeverity.Info"/> entries so consumers can audit the
        /// intended apply behavior. The report itself never applies schema changes.
        /// </para>
        /// </summary>
        MigrationReadinessReport GetMigrationReadinessForTypes(IEnumerable<Type> entityTypes, bool detectRelationships = true, bool applyForeignKeys = false, bool applyIndexes = false);

        /// <summary>
        /// Returns datasource-aware migration best-practice guidance for the current migration datasource
        /// or for an explicitly requested datasource type/category.
        /// </summary>
        IReadOnlyList<string> GetMigrationBestPractices(DataSourceType? dataSourceType = null, DatasourceCategory? dataSourceCategory = null);

        /// <summary>
        /// Backward-compatible alias for older provider-named guidance calls.
        /// </summary>
        [Obsolete("Use GetMigrationBestPractices instead. This alias remains for backward compatibility.")]
        IReadOnlyList<string> GetProviderBestPractices(DataSourceType? dataSourceType = null);

        /// <summary>
        /// Builds a migration plan artifact (preview only) from discovery-based entity resolution.
        /// This method does not apply schema changes.
        /// <para>
        /// <paramref name="applyForeignKeys"/> and <paramref name="applyIndexes"/> are recorded on
        /// the plan as informational AddForeignKey / CreateIndex operations alongside the entity
        /// operations, so dry-run, policy, and preflight reports preview them. The plan itself
        /// remains read-only; the flags take effect on
        /// <see cref="ApplyMigrations(string, Assembly, bool, bool, IProgress{PassedArgs}, bool, bool)"/>.
        /// </para>
        /// </summary>
        MigrationPlanArtifact BuildMigrationPlan(string namespaceName = null, Assembly assembly = null, bool detectRelationships = true, bool applyForeignKeys = false, bool applyIndexes = false);

        /// <summary>
        /// Builds a migration plan artifact (preview only) from explicit entity types.
        /// This method does not apply schema changes.
        /// <para>
        /// <paramref name="applyForeignKeys"/> and <paramref name="applyIndexes"/> are recorded on
        /// the plan as informational AddForeignKey / CreateIndex operations alongside the entity
        /// operations, so dry-run, policy, and preflight reports preview them. The plan itself
        /// remains read-only; the flags take effect on
        /// <see cref="ApplyMigrationsForTypes(IEnumerable{Type}, bool, bool, IProgress{PassedArgs}, bool, bool)"/>.
        /// </para>
        /// </summary>
        MigrationPlanArtifact BuildMigrationPlanForTypes(IEnumerable<Type> entityTypes, bool detectRelationships = true, bool applyForeignKeys = false, bool applyIndexes = false);

        /// <summary>
        /// Evaluates a migration plan against compatibility and safety policies.
        /// Emits pass/warn/block verdicts and enforces protected-environment defaults.
        /// </summary>
        MigrationPolicyEvaluation EvaluateMigrationPlanPolicy(MigrationPlanArtifact plan, MigrationPolicyOptions options = null);

        /// <summary>
        /// Generates dry-run output including operation list, DDL preview, and risk tags.
        /// This method does not apply schema changes.
        /// </summary>
        MigrationDryRunReport GenerateDryRunReport(MigrationPlanArtifact plan);

        /// <summary>
        /// Runs preflight checks before apply (connection, drift, policy, operational heuristics).
        /// </summary>
        MigrationPreflightReport RunPreflightChecks(MigrationPlanArtifact plan, MigrationPolicyOptions options = null);

        /// <summary>
        /// Builds impact analysis with entity usage hints and data-volume sensitivity indicators.
        /// </summary>
        MigrationImpactReport BuildImpactReport(MigrationPlanArtifact plan);

        /// <summary>
        /// Creates (or refreshes) a migration execution checkpoint for a plan.
        /// </summary>
        MigrationExecutionCheckpoint CreateExecutionCheckpoint(MigrationPlanArtifact plan, string executionToken = null);

        /// <summary>
        /// Executes a migration plan with deterministic retry and checkpoint tracking.
        /// Supports resumable execution via execution token.
        /// </summary>
        MigrationExecutionResult ExecuteMigrationPlan(MigrationPlanArtifact plan, MigrationExecutionPolicy policy = null, string executionToken = null, IProgress<PassedArgs> progress = null);

        /// <summary>
        /// Async overload of <see cref="ExecuteMigrationPlan"/>. Required by the
        /// shared <c>IRetryPipeline</c> for per-step retry. New code should prefer
        /// this method; the sync overload is a thin wrapper for back-compat.
        /// </summary>
        Task<MigrationExecutionResult> ExecuteMigrationPlanAsync(MigrationPlanArtifact plan, MigrationExecutionPolicy policy = null, string executionToken = null, IProgress<PassedArgs> progress = null, CancellationToken token = default);

        /// <summary>
        /// Resumes a previously checkpointed execution by token.
        /// </summary>
        MigrationExecutionResult ResumeMigrationPlan(string executionToken, MigrationExecutionPolicy policy = null, IProgress<PassedArgs> progress = null);

        /// <summary>
        /// Gets a stored execution checkpoint by token.
        /// </summary>
        MigrationExecutionCheckpoint GetExecutionCheckpoint(string executionToken);

        /// <summary>
        /// Builds rollback and compensation actions for high-risk migration operations.
        /// </summary>
        MigrationCompensationPlan BuildCompensationPlan(MigrationPlanArtifact plan);

        /// <summary>
        /// Validates rollback readiness before apply (backup/snapshot and restore evidence).
        /// </summary>
        MigrationRollbackReadinessReport CheckRollbackReadiness(MigrationPlanArtifact plan, bool backupConfirmed, bool restoreTestEvidenceProvided, string restoreTestEvidence = null);

        /// <summary>
        /// Executes (or simulates) rollback and compensation for a failed checkpoint execution.
        /// </summary>
        MigrationRollbackResult RollbackFailedExecution(string executionToken, bool dryRun = true);

        /// <summary>
        /// Marks a migration plan as approved and records approval audit evidence.
        /// </summary>
        MigrationPlanArtifact ApproveMigrationPlan(MigrationPlanArtifact plan, string approvedBy, string notes = null);

        /// <summary>
        /// Gets telemetry snapshot including metrics, diagnostics, and audit lifecycle events.
        /// </summary>
        MigrationTelemetrySnapshot GetMigrationTelemetrySnapshot(string executionToken = null);

        /// <summary>
        /// Gets structured diagnostics, optionally filtered by execution token and minimum severity.
        /// </summary>
        IReadOnlyList<MigrationDiagnosticEntry> GetMigrationDiagnostics(string executionToken = null, MigrationDiagnosticSeverity? minimumSeverity = null);

        /// <summary>
        /// Gets audit events, optionally filtered by execution token.
        /// </summary>
        IReadOnlyList<MigrationAuditEvent> GetMigrationAuditEvents(string executionToken = null);

        /// <summary>
        /// Builds lock/runtime impact estimates and performance execution guidance for a migration plan.
        /// </summary>
        MigrationPerformancePlan BuildPerformancePlan(MigrationPlanArtifact plan, MigrationPerformancePolicy policy = null);

        /// <summary>
        /// Runs CI/CD validation gates for plan lint, policy, dry-run, and portability checks.
        /// </summary>
        MigrationCiValidationReport ValidatePlanForCi(MigrationPlanArtifact plan, MigrationPolicyOptions options = null);

        /// <summary>
        /// Builds a lightweight textual diff between two plans for developer review.
        /// </summary>
        string BuildMigrationPlanDiff(MigrationPlanArtifact previousPlan, MigrationPlanArtifact currentPlan);

        /// <summary>
        /// Exports approval-ready artifacts (plan, dry-run, CI report, markdown summary).
        /// </summary>
        MigrationDevExArtifactBundle ExportMigrationArtifacts(MigrationPlanArtifact plan, MigrationCiValidationReport ciReport = null);

        /// <summary>
        /// Evaluates rollout wave governance using KPI thresholds and hard-stop controls.
        /// </summary>
        MigrationRolloutGovernanceReport EvaluateRolloutGovernance(MigrationPlanArtifact plan, MigrationRolloutGovernanceRequest request = null);

        // ── Phase 1 – Filesystem manifest discovery ──

        /// <summary>
        /// Parses a migration manifest .txt file and returns typed entries plus structured parse errors.
        /// Line format: TypeFullName[|AssemblyHint[|NamespacePrefix]]
        /// Error codes: MIG-MANIFEST-001..MIG-MANIFEST-004.
        /// </summary>
        MigrationManifestParseResult ParseManifestFile(string filePath);

        // ── Phase 4 – Assembly discovery evidence ──

        /// <summary>
        /// Returns the discovery evidence snapshot produced by the most recent call to
        /// <see cref="DiscoverEntityTypes"/>. Returns null if no discovery has run yet.
        /// Enables troubleshooting of "no entities found" scenarios without ad-hoc debug logging.
        /// </summary>
        AssemblyDiscoveryEvidence GetDiscoveryEvidence();

        // ── Phase 3 – DDL operation evidence ──

        /// <summary>
        /// Returns the DDL operation evidence collected during the current session.
        /// Entries are appended after each DDL operation (DropEntity, RenameEntity, AlterColumn, etc.).
        /// </summary>
        IReadOnlyList<DdlOperationEvidence> GetDdlEvidence();

        // ── Phase 5 – Recommendation profiles ──

        /// <summary>
        /// Returns the versioned recommendation profile for the given datasource type/category.
        /// Each recommendation has a stable id, rationale, and capability provenance markers.
        /// </summary>
        RecommendationProfile GetRecommendationProfile(DataSourceType? dataSourceType = null, DatasourceCategory? dataSourceCategory = null);

        // ── Phase 6 – ORM interop (POCO model abstraction) ──
        //
        // These methods bridge ORM-shaped models (e.g. EF Core IModel, NHibernate model,
        // a hand-rolled dictionary, or a snapshot loaded from disk) into the existing
        // migration plan / readiness / apply pipeline. The bridge is intentionally
        // framework-agnostic: callers populate a BeepDM-owned MigrationModel POCO and
        // BeepDM does the rest.
        //
        // The engine ships with a built-in MigrationModelBuilder that reads data
        // annotations ([Table], [Column], [Key], [ForeignKey], [Index], [Required],
        // [MaxLength], [StringLength], etc.) from CLR types — no ORM package required.
        // A separate companion package (or user code) can translate an EF Core IModel
        // into the same MigrationModel shape without BeepDM taking a hard EF Core dep.
        //
        // Retrieved entity types are tagged with EntityMigrationSource.DiscoveryEFCoreModel
        // for provenance and benefit from the same plan / policy / dry-run / preflight /
        // compensation / governance machinery as the explicit and discovery paths.
        //
        // Implementations are sourced from MigrationManager.EFCore.cs.

        /// <summary>
        /// Builds a non-destructive migration plan from a pre-built
        /// <see cref="MigrationModel"/>. Use this overload when an ORM (or any other
        /// source) has already produced the model — BeepDM does not need to know about
        /// the originating ORM.
        /// <para>
        /// <paramref name="applyForeignKeys"/> and <paramref name="applyIndexes"/> are
        /// recorded on the plan's operations as a hint to consumers about whether the
        /// apply pipeline should generate FK / index DDL. The plan itself remains
        /// read-only; the flags take effect on <see cref="ApplyMigrationsForModel"/>.
        /// </para>
        /// </summary>
        MigrationPlanArtifact BuildMigrationPlanForModel(MigrationModel model, bool detectRelationships = true, bool applyForeignKeys = false, bool applyIndexes = false);

        /// <summary>
        /// Builds a non-destructive migration plan from a set of CLR types whose
        /// migration shape is read via data annotations
        /// (<c>System.ComponentModel.DataAnnotations.Schema.TableAttribute</c> and friends).
        /// This is the no-ORM package path: pure reflection, no hard dependencies.
        /// </summary>
        MigrationPlanArtifact BuildMigrationPlanForTypesAnnotated(IEnumerable<Type> entityTypes, bool detectRelationships = true, bool applyForeignKeys = false, bool applyIndexes = false);

        /// <summary>
        /// Ensures that the connected migration datasource contains every entity described
        /// by the supplied <see cref="MigrationModel"/>.
        /// Existing entities are left as-is; use <see cref="ApplyMigrationsForModel"/> to add missing columns.
        /// </summary>
        IErrorsInfo EnsureDatabaseCreatedForModel(MigrationModel model, bool detectRelationships = true, IProgress<PassedArgs> progress = null, bool applyForeignKeys = false, bool applyIndexes = false);

        /// <summary>
        /// Applies schema changes for every entity described by the supplied
        /// <see cref="MigrationModel"/>.
        /// <para>
        /// When <paramref name="applyForeignKeys"/> is true, foreign keys discovered in
        /// the model are created via <c>AddForeignKey</c> after the dependent table is
        /// created. When <paramref name="applyIndexes"/> is true, indexes are created
        /// via <c>CreateIndex</c>. Both default to false, preserving prior behavior of
        /// leaving relational artifacts to the ORM or to manual DDL.
        /// </para>
        /// </summary>
        IErrorsInfo ApplyMigrationsForModel(MigrationModel model, bool detectRelationships = true, bool addMissingColumns = true, IProgress<PassedArgs> progress = null, bool applyForeignKeys = false, bool applyIndexes = false);

        /// <summary>
        /// Builds a datasource-aware readiness report for the supplied
        /// <see cref="MigrationModel"/>.
        /// </summary>
        /// <param name="model">ORM-supplied migration model.</param>
        /// <param name="detectRelationships">When true, foreign-key and index shapes are reflected.</param>
        /// <param name="applyForeignKeys">Record the intended apply-foreign-keys flag on the report for downstream consumers (no schema changes here).</param>
        /// <param name="applyIndexes">Record the intended apply-indexes flag on the report for downstream consumers (no schema changes here).</param>
        MigrationReadinessReport GetMigrationReadinessForModel(MigrationModel model, bool detectRelationships = true, bool applyForeignKeys = false, bool applyIndexes = false);

        /// <summary>
        /// Returns a migration summary comparing the supplied <see cref="MigrationModel"/>
        /// with the current database state.
        /// </summary>
        /// <param name="model">ORM-supplied migration model.</param>
        /// <param name="detectRelationships">When true, foreign-key and index shapes are reflected.</param>
        /// <param name="applyForeignKeys">Record the intended apply-foreign-keys flag on the summary for downstream consumers.</param>
        /// <param name="applyIndexes">Record the intended apply-indexes flag on the summary for downstream consumers.</param>
        MigrationSummary GetMigrationSummaryForModel(MigrationModel model, bool detectRelationships = true, bool applyForeignKeys = false, bool applyIndexes = false);

        /// <summary>
        /// Returns the model evidence captured during the most recent
        /// <see cref="BuildMigrationPlanForModel"/> call.
        /// </summary>
        MigrationModelEvidence GetMigrationModelEvidence();
    }
}
