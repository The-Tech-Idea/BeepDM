// Copyright (c) The Tech Idea. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.AppMap;
using TheTechIdea.Beep.Studio.Apps;
using TheTechIdea.Beep.Studio.Apps.Workflows;
using TheTechIdea.Beep.Studio.Contracts;
using TheTechIdea.Beep.Studio.Deployment;
using TheTechIdea.Beep.Studio.Driver;
using TheTechIdea.Beep.Studio.Governance;
using TheTechIdea.Beep.Studio.Manifest;
using TheTechIdea.Beep.Studio.Migration;
using TheTechIdea.Beep.Studio.Schema;
using TheTechIdea.Beep.Studio.Source;
using TheTechIdea.Beep.Studio.Sync;
// PR 17: alias the engine's MigrationExecutionPolicy (class) since the
// Studio's local record was removed; we keep the Studio's record shapes
// for MigrationDryRunReport / MigrationPreflightReport / MigrationImpactReport
// (they don't have engine equivalents with the right Studio-friendly fields).
using EngineMigrationExecutionPolicy = TheTechIdea.Beep.Editor.Migration.MigrationExecutionPolicy;

namespace TheTechIdea.Beep.Studio;

/// <summary>
/// No-op implementation of <see cref="Contracts.IStudioService"/>. Every call
/// returns <c>StudioResult&lt;T&gt;.Fail(StudioErrorCode.HostNotSupported, ...)</c>.
/// Use in tests and in hosts that have not yet wired the Studio.
/// </summary>
public sealed class NullStudioService : IStudioService
{
    /// <summary>Singleton instance — safe to share across all threads and circuits.</summary>
    public static NullStudioService Instance { get; } = new();

    private NullStudioService() { }

    /// <inheritdoc />
    public IAppStudioService Apps => NullAppStudioService.Instance;

    /// <inheritdoc />
    public IEnvironmentProfileService Environments => NullEnvironmentProfileService.Instance;

    /// <inheritdoc />
    public IDriverService Drivers => NullDriverService.Instance;

    /// <inheritdoc />
    public ISourceService Sources => NullSourceService.Instance;

    /// <inheritdoc />
    public ISchemaService Schemas => NullSchemaService.Instance;

    /// <inheritdoc />
    public IMigrationStudioService Migrations => NullMigrationStudioService.Instance;

    /// <inheritdoc />
    public ISyncStudioService Sync => NullSyncStudioService.Instance;

    /// <inheritdoc />
    public IGovernanceService Governance => NullGovernanceService.Instance;

    /// <inheritdoc />
    public IDataLifecycleManifestService Manifest => NullDataLifecycleManifestService.Instance;

    /// <inheritdoc />
    public IDeploymentMetadataService Deployment => NullDeploymentMetadataService.Instance;

    /// <inheritdoc />
    public StudioInfo GetInfo() => new(
        Version: "0.0.0-null",
        EngineVersion: "0.0.0",
        SupportedDataSourceTypes: Array.Empty<string>(),
        SupportedDataSourceCategories: Array.Empty<string>(),
        SupportedTiers: Array.Empty<RolloutTier>(),
        AuditEnabled: false,
        HostedServicesEnabled: false,
        ManifestLoaded: false,
        ManifestVersion: null,
        Capabilities: new Dictionary<string, object?>());

    /// <inheritdoc />
    public Task<StudioResult<StudioInfo>> GetInfoAsync(CancellationToken ct = default)
        => Task.FromResult(StudioResult<StudioInfo>.Ok(GetInfo()));
}

// ─────────────────────────────────────────────────────────────────────────────
// Per-sub-service null implementations. All return HostNotSupported for
// mutation methods; all return empty results for read methods.
// ─────────────────────────────────────────────────────────────────────────────

internal sealed class NullEnvironmentProfileService : IEnvironmentProfileService
{
    public static NullEnvironmentProfileService Instance { get; } = new();
    private NullEnvironmentProfileService() { }
    public Task<StudioResult<IReadOnlyList<EnvironmentProfile>>> ListAsync(CancellationToken ct = default) =>
        Task.FromResult(StudioResult<IReadOnlyList<EnvironmentProfile>>.Ok(Array.Empty<EnvironmentProfile>()));
    public Task<StudioResult<EnvironmentProfile>> GetAsync(string id, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<EnvironmentProfile>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<EnvironmentProfile>> SaveAsync(EnvironmentProfile p, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<EnvironmentProfile>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<bool>> DeleteAsync(string id, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<bool>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<EnvironmentProfile>> GetDefaultAsync(CancellationToken ct = default) =>
        Task.FromResult(StudioResult<EnvironmentProfile>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
}

internal sealed class NullAppStudioService : IAppStudioService
{
    public static NullAppStudioService Instance { get; } = new();
    private NullAppStudioService() { }
    public IAppMigrationWorkflow Migrations => NullAppMigrationWorkflow.Instance;
    public IAppDataWorkflow Data => NullAppDataWorkflow.Instance;
    public IAppGovernanceWorkflow Governance => NullAppGovernanceWorkflow.Instance;
    public IAppQuickStartWorkflow QuickStart => NullAppQuickStartWorkflow.Instance;
    public IAppDeployWorkflow Deploy => NullAppDeployWorkflow.Instance;
    public IAppCicdWorkflow Cicd => NullAppCicdWorkflow.Instance;
    public IAppCloudWorkflow Cloud => NullAppCloudWorkflow.Instance;
    public IScenarioWorkflow Scenarios => NullAppScenarioWorkflow.Instance;
    public Task<StudioResult<IReadOnlyList<AppDefinition>>> ListAsync(CancellationToken ct = default) =>
        Task.FromResult(StudioResult<IReadOnlyList<AppDefinition>>.Ok(Array.Empty<AppDefinition>()));
    public Task<StudioResult<AppDefinition>> GetAsync(string appId, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<AppDefinition>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<AppDefinition>> SaveAsync(AppDefinition app, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<AppDefinition>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<AppDefinition>> RegisterFromSolutionAsync(string appName, string solutionPath, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<AppDefinition>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<bool>> DeleteAsync(string appId, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<bool>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<AppDefinition>> AddEnvironmentAsync(string appId, AppEnv env, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<AppDefinition>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<AppDefinition>> RemoveEnvironmentAsync(string appId, string envId, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<AppDefinition>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<AppEnv>> UpdateEnvironmentAsync(string appId, string envId, AppEnv updated, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<AppEnv>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<AppDefinition>> SetDatasourceAsync(string appId, string envId, AppDataSource ds, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<AppDefinition>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<AppDefinition>> RemoveDatasourceAsync(string appId, string envId, string dsName, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<AppDefinition>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<bool>> TestDatasourceAsync(string appId, string envId, string dsName, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<bool>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<AppDashboard>> GetDashboardAsync(string appId, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<AppDashboard>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<PromotionResult>> PromoteAsync(string appId, string toEnv, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<PromotionResult>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<EnvironmentPromotion>> PromoteEnvironmentAsync(string appId, string toEnv, PromoteOptions? options = null, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<EnvironmentPromotion>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<AppProject>> RegisterProjectAsync(string appId, string csprojPath, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<AppProject>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<List<AppProject>>> ScanAndAddProjectsAsync(string appId, string folderOrSlnPath, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<List<AppProject>>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<AppProject>> DiscoverEntitiesForProjectAsync(string appId, string projectName, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<AppProject>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<List<AppProject>>> DiscoverAllEntitiesAsync(string appId, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<List<AppProject>>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<List<AppProject>>> ReScanSolutionAsync(string appId, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<List<AppProject>>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<AppDefinition>> CloneSolutionAsync(string appId, string newName, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<AppDefinition>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<string>> FindNearestSolutionAsync(string startPath, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<string>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<ChangeDetection>> DetectChangesAsync(string appId, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<ChangeDetection>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<IReadOnlyList<AvailableDatasource>>> ListAvailableDatasourcesAsync(CancellationToken ct = default) =>
        Task.FromResult(StudioResult<IReadOnlyList<AvailableDatasource>>.Ok(System.Array.Empty<AvailableDatasource>()));
    public Task<StudioResult<AppConfigOutput>> GenerateAppConfigAsync(string appId, string envId, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<AppConfigOutput>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<IReadOnlyList<AppConfigOutput>>> GenerateAllAppConfigsAsync(string appId, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<IReadOnlyList<AppConfigOutput>>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
}

internal sealed class NullDriverService : IDriverService
{
    public static NullDriverService Instance { get; } = new();
    private NullDriverService() { }
    public Task<StudioResult<IReadOnlyList<DriverInfo>>> ListAsync(CancellationToken ct = default) =>
        Task.FromResult(StudioResult<IReadOnlyList<DriverInfo>>.Ok(Array.Empty<DriverInfo>()));
    public Task<StudioResult<DriverInfo>> GetAsync(string n, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<DriverInfo>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<DriverProvisionResult>> ProvisionAsync(DriverProvisionRequest r, IStudioProgress? p = null, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<DriverProvisionResult>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<bool>> UnloadAsync(string n, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<bool>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
}

internal sealed class NullSourceService : ISourceService
{
    public static NullSourceService Instance { get; } = new();
    private NullSourceService() { }
    public Task<StudioResult<IReadOnlyList<SourceInfo>>> ListAsync(SourceListFilter? f = null, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<IReadOnlyList<SourceInfo>>.Ok(Array.Empty<SourceInfo>()));
    public Task<StudioResult<SourceInfo>> GetAsync(string n, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<SourceInfo>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<SourceInfo>> SaveAsync(SourceConfigurationRequest r, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<SourceInfo>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<bool>> DeleteAsync(string n, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<bool>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<SourceTestResult>> TestAsync(string n, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<SourceTestResult>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<IReadOnlyList<EntityDescriptor>>> BrowseAsync(string n, string? e = null, int r = 0, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<IReadOnlyList<EntityDescriptor>>.Ok(Array.Empty<EntityDescriptor>()));
}

internal sealed class NullSchemaService : ISchemaService
{
    public static NullSchemaService Instance { get; } = new();
    private NullSchemaService() { }
    public Task<StudioResult<IReadOnlyList<EntityDescriptor>>> DiscoverAsync(EntityDiscoveryRequest r, IStudioProgress? p = null, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<IReadOnlyList<EntityDescriptor>>.Ok(Array.Empty<EntityDescriptor>()));
    public Task<StudioResult<EntityDescriptor>> DescribeAsync(string a, string e, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<EntityDescriptor>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
}

internal sealed class NullMigrationStudioService : IMigrationStudioService
{
    public static NullMigrationStudioService Instance { get; } = new();
    private NullMigrationStudioService() { }
    public Task<StudioResult<MigrationPlanHandle>> BuildPlanAsync(MigrationRequest r, IStudioProgress? p = null, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<MigrationPlanHandle>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<MigrationDryRunReport>> DryRunAsync(MigrationPlanHandle h, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<MigrationDryRunReport>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<MigrationPreflightReport>> PreflightAsync(MigrationPlanHandle h, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<MigrationPreflightReport>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<MigrationImpactReport>> ImpactAsync(MigrationPlanHandle h, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<MigrationImpactReport>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<CiValidationReport>> ValidateForCiAsync(MigrationPlanHandle h, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<CiValidationReport>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<PolicyEvaluationResult>> EvaluatePolicyAsync(MigrationPlanHandle h, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<PolicyEvaluationResult>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<MigrationExecutionHandle>> ApplyAsync(MigrationPlanHandle h, EngineMigrationExecutionPolicy p, IStudioProgress? pr = null, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<MigrationExecutionHandle>.Fail(StudioErrorCode.HostNotSupported, "No Studio is wired into this host."));
    public Task<StudioResult<MigrationExecutionHandle>> ResumeAsync(string t, EngineMigrationExecutionPolicy? p = null, IStudioProgress? pr = null, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<MigrationExecutionHandle>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<bool>> CancelAsync(string t, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<bool>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<MigrationRollbackReport>> RollbackAsync(string t, RollbackPolicy p, IStudioProgress? pr = null, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<MigrationRollbackReport>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<IReadOnlyList<MigrationHistoryItem>>> GetHistoryAsync(string? s = null, int sk = 0, int t = 100, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<IReadOnlyList<MigrationHistoryItem>>.Ok(Array.Empty<MigrationHistoryItem>()));
    public Task<StudioResult<MigrationExecutionState>> GetExecutionStateAsync(string t, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<MigrationExecutionState>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Migration.Ledger.IMigrationLedger Ledger => new TheTechIdea.Beep.Services.Studio.Migration.Ledger.JsonMigrationLedger(
        System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BeepDM", "Studio"));
}

internal sealed class NullSyncStudioService : ISyncStudioService
{
    public static NullSyncStudioService Instance { get; } = new();
    private NullSyncStudioService() { }
    public Task<StudioResult<IReadOnlyList<SyncSchemaSummary>>> ListSchemasAsync(SyncListFilter? f = null, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<IReadOnlyList<SyncSchemaSummary>>.Ok(Array.Empty<SyncSchemaSummary>()));
    public Task<StudioResult<SyncSchemaVm>> GetSchemaAsync(string id, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<SyncSchemaVm>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<SyncSchemaVm>> SaveSchemaAsync(SyncSchemaVm s, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<SyncSchemaVm>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<bool>> DeleteSchemaAsync(string id, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<bool>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<ValidationReport>> ValidateSchemaAsync(string id, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<ValidationReport>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<SyncPreflightReport>> RunPreflightAsync(string id, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<SyncPreflightReport>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<SyncRunHandle>> EnqueueRunAsync(string id, SyncRunOptions? o = null, IStudioProgress? p = null, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<SyncRunHandle>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<bool>> StopRunAsync(string r, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<bool>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<SyncRunStatus>> GetRunStatusAsync(string r, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<SyncRunStatus>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<SyncReconciliationVm>> GetReconciliationAsync(string r, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<SyncReconciliationVm>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<IReadOnlyList<ConflictEvidenceVm>>> ListConflictsAsync(string id, int sk = 0, int t = 100, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<IReadOnlyList<ConflictEvidenceVm>>.Ok(Array.Empty<ConflictEvidenceVm>()));
    public Task<StudioResult<ConflictResolutionResult>> ResolveConflictAsync(string id, string cid, ConflictResolutionAction a, string? d = null, string? c = null, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<ConflictResolutionResult>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<IReadOnlyList<SyncRunHistoryItem>>> GetRunHistoryAsync(string id, int sk = 0, int t = 100, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<IReadOnlyList<SyncRunHistoryItem>>.Ok(Array.Empty<SyncRunHistoryItem>()));
}

internal sealed class NullGovernanceService : IGovernanceService
{
    public static NullGovernanceService Instance { get; } = new();
    private NullGovernanceService() { }
    public Task<StudioResult<IReadOnlyList<GovernancePolicy>>> ListPoliciesAsync(CancellationToken ct = default) =>
        Task.FromResult(StudioResult<IReadOnlyList<GovernancePolicy>>.Ok(Array.Empty<GovernancePolicy>()));
    public Task<StudioResult<GovernancePolicy>> GetPolicyAsync(string id, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<GovernancePolicy>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<GovernancePolicy>> GetPolicyForTierAsync(RolloutTier tier, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<GovernancePolicy>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<GovernancePolicy>> UpsertPolicyAsync(GovernancePolicy p, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<GovernancePolicy>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<bool>> DeletePolicyAsync(string id, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<bool>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<PolicyEvaluationResult>> EvaluateRequestAsync(ApprovalRequest r, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<PolicyEvaluationResult>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<ApprovalRequest>> RequestApprovalAsync(ApprovalRequest r, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<ApprovalRequest>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<ApprovalRequest>> DecideApprovalAsync(string id, ApprovalDecision d, string dec, string? c = null, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<ApprovalRequest>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<IReadOnlyList<ApprovalRequest>>> ListApprovalsAsync(ApprovalListFilter? f = null, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<IReadOnlyList<ApprovalRequest>>.Ok(Array.Empty<ApprovalRequest>()));
    public Task<StudioResult<string>> RecordAuditAsync(StudioAuditEvent e, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<string>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<IReadOnlyList<StudioAuditEvent>>> QueryAuditAsync(AuditQuery q, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<IReadOnlyList<StudioAuditEvent>>.Ok(Array.Empty<StudioAuditEvent>()));
    public Task<StudioResult<AuditIntegrityReport>> VerifyAuditIntegrityAsync(CancellationToken ct = default) =>
        Task.FromResult(StudioResult<AuditIntegrityReport>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
}

internal sealed class NullDataLifecycleManifestService : IDataLifecycleManifestService
{
    public static NullDataLifecycleManifestService Instance { get; } = new();
    private NullDataLifecycleManifestService() { }
    public DataLifecycleManifest? Current => null;
    public Task<StudioResult<DataLifecycleManifest>> LoadAsync(string? p = null, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<DataLifecycleManifest>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<bool>> SaveAsync(DataLifecycleManifest m, string p, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<bool>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<ManifestValidationReport>> ValidateAsync(DataLifecycleManifest m, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<ManifestValidationReport>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public string? ResolveManifestPath(string? s = null) => null;
}

internal sealed class NullDeploymentMetadataService : IDeploymentMetadataService
{
    public static NullDeploymentMetadataService Instance { get; } = new();
    private NullDeploymentMetadataService() { }
    public Task<StudioResult<DeploymentMetadata>> GetCurrentAsync(CancellationToken ct = default) =>
        Task.FromResult(StudioResult<DeploymentMetadata>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public void Override(DeploymentMetadata? m) { /* no-op */ }
    public Task<StudioResult<ApprovalToken>> IssueApprovalTokenAsync(ApprovalTokenRequest r, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<ApprovalToken>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<ApprovalTokenClaims>> VerifyApprovalTokenAsync(string t, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<ApprovalTokenClaims>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
}
