using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.AppMap;
using TheTechIdea.Beep.Studio.Apps.Workflows;
using TheTechIdea.Beep.Studio.Migration.Ledger;

namespace TheTechIdea.Beep.Studio;

// Null implementations of the six app-scoped workflows. All mutations return
// HostNotSupported; reads return empty. Used by NullStudioService when the host
// has not wired the Studio (tests / minimal hosts).

internal sealed class NullAppMigrationWorkflow : IAppMigrationWorkflow
{
    public static NullAppMigrationWorkflow Instance { get; } = new();
    private NullAppMigrationWorkflow() { }
    public Task<StudioResult<EnvMigrationReport>> MigrateAsync(string appId, string envId, MigrationOptions? options = null, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<EnvMigrationReport>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<EnvMigrationReport>> DryRunAsync(string appId, string envId, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<EnvMigrationReport>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<bool>> RollbackAsync(string appId, string envId, string? datasourceName = null, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<bool>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<IReadOnlyList<EnvMigrationHistoryItem>>> GetHistoryAsync(string appId, string envId, string? datasourceName = null, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<IReadOnlyList<EnvMigrationHistoryItem>>.Ok(System.Array.Empty<EnvMigrationHistoryItem>()));
    public Task<StudioResult<SchemaCompareResult>> CompareEnvironmentsAsync(string appId, string sourceEnv, string targetEnv, string? datasourceName = null, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<SchemaCompareResult>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
}

internal sealed class NullAppDataWorkflow : IAppDataWorkflow
{
    public static NullAppDataWorkflow Instance { get; } = new();
    private NullAppDataWorkflow() { }
    public Task<StudioResult<DataSyncReport>> CopyAsync(string appId, string fromEnv, string toEnv, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<DataSyncReport>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<DataSyncReport>> SubsetAsync(string appId, string fromEnv, string toEnv, DataSubsetOptions options, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<DataSyncReport>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<DataSyncReport>> MaskedCopyAsync(string appId, string fromEnv, string toEnv, IReadOnlyCollection<MaskingRule> masking, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<DataSyncReport>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<IReadOnlyList<MaskingRule>>> GetMaskingRulesAsync(string appId, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<IReadOnlyList<MaskingRule>>.Ok(System.Array.Empty<MaskingRule>()));
    public Task<StudioResult<IReadOnlyList<MaskingRule>>> SetMaskingRulesAsync(string appId, IEnumerable<MaskingRule> rules, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<IReadOnlyList<MaskingRule>>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<IReadOnlyList<MigrationLedgerEntry>>> GetHistoryAsync(string appId, string? envId = null, string? datasourceName = null, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<IReadOnlyList<MigrationLedgerEntry>>.Ok(System.Array.Empty<MigrationLedgerEntry>()));
}

internal sealed class NullAppGovernanceWorkflow : IAppGovernanceWorkflow
{
    public static NullAppGovernanceWorkflow Instance { get; } = new();
    private NullAppGovernanceWorkflow() { }
    public Task<StudioResult<IReadOnlyList<AppRoleAssignment>>> ListMembersAsync(string appId, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<IReadOnlyList<AppRoleAssignment>>.Ok(System.Array.Empty<AppRoleAssignment>()));
    public Task<StudioResult<AppRoleAssignment>> AssignRoleAsync(string appId, string userId, AppMemberRole role, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<AppRoleAssignment>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<bool>> RevokeRoleAsync(string appId, string userId, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<bool>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<bool>> CanUserAsync(string appId, string userId, AppMemberRole required, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<bool>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<ApprovalTicket>> RequestApprovalAsync(string appId, string envId, string action, string requestedBy, string? reason = null, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<ApprovalTicket>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<ApprovalTicket>> DecideAsync(string appId, string ticketId, bool approved, string decidedBy, string? comment = null, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<ApprovalTicket>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<IReadOnlyList<ApprovalTicket>>> ListApprovalsAsync(string appId, string? envId = null, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<IReadOnlyList<ApprovalTicket>>.Ok(System.Array.Empty<ApprovalTicket>()));
    public Task<StudioResult<PolicyDecision>> EvaluateAsync(string appId, string envId, string action, string userId, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<PolicyDecision>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<int>> RecordAuditAsync(string appId, string envId, string action, string userId, string? detail = null, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<int>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<IReadOnlyList<AppAuditEntry>>> QueryAuditAsync(string appId, string? envId = null, int skip = 0, int take = 100, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<IReadOnlyList<AppAuditEntry>>.Ok(System.Array.Empty<AppAuditEntry>()));
}

internal sealed class NullAppQuickStartWorkflow : IAppQuickStartWorkflow
{
    public static NullAppQuickStartWorkflow Instance { get; } = new();
    private NullAppQuickStartWorkflow() { }
    public Task<StudioResult<IReadOnlyList<AppTemplate>>> ListTemplatesAsync(CancellationToken ct = default) =>
        Task.FromResult(StudioResult<IReadOnlyList<AppTemplate>>.Ok(System.Array.Empty<AppTemplate>()));
    public Task<StudioResult<QuickStartResult>> StartAsync(QuickStartRequest request, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<QuickStartResult>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<bool>> SeedAsync(string appId, string envId, string seedSource, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<bool>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
}

internal sealed class NullAppCicdWorkflow : IAppCicdWorkflow
{
    public static NullAppCicdWorkflow Instance { get; } = new();
    private NullAppCicdWorkflow() { }
    public Task<StudioResult<PipelineDescriptor>> GeneratePipelineAsync(string appId, CicdProvider provider, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<PipelineDescriptor>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<EnvMigrationReport>> MigrateOnDeployAsync(string appId, string envId, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<EnvMigrationReport>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<PrPreviewResult>> CreatePreviewDatabaseAsync(string appId, string prId, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<PrPreviewResult>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<bool>> DropPreviewDatabaseAsync(string appId, string prId, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<bool>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<IReadOnlyList<PrPreviewResult>>> ListPreviewsAsync(string appId, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<IReadOnlyList<PrPreviewResult>>.Ok(System.Array.Empty<PrPreviewResult>()));
}

internal sealed class NullAppCloudWorkflow : IAppCloudWorkflow
{
    public static NullAppCloudWorkflow Instance { get; } = new();
    private NullAppCloudWorkflow() { }
    public Task<StudioResult<ResolvedConnection>> ResolveConnectionAsync(string appId, string envId, string datasourceName, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<ResolvedConnection>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<bool>> BindSecretAsync(string appId, string envId, string datasourceName, CloudSecretRef secretRef, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<bool>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<CloudIdentityContext>> GetIdentityContextAsync(CancellationToken ct = default) =>
        Task.FromResult(StudioResult<CloudIdentityContext>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<EnvCostEstimate>> EstimateEnvCostAsync(string appId, string envId, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<EnvCostEstimate>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
}

internal sealed class NullAppDeployWorkflow : IAppDeployWorkflow
{
    public static NullAppDeployWorkflow Instance { get; } = new();
    private NullAppDeployWorkflow() { }
    public Task<StudioResult<ProjectDeployment>> DeployAsync(string appId, string envId, string projectName, string version, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<ProjectDeployment>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<PromotionResult>> PromoteCodeAsync(string appId, string toEnv, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<PromotionResult>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<IReadOnlyList<ProjectDeployment>>> GetDeploymentsAsync(string appId, string? envId = null, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<IReadOnlyList<ProjectDeployment>>.Ok(System.Array.Empty<ProjectDeployment>()));
    public Task<StudioResult<bool>> ConfigureBindingAsync(string appId, string envId, string projectName, string? endpointUrl, string? datasourceName, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<bool>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
}

internal sealed class NullAppScenarioWorkflow : IScenarioWorkflow
{
    public static NullAppScenarioWorkflow Instance { get; } = new();
    private NullAppScenarioWorkflow() { }
    public Task<StudioResult<SoloDevResult>> RunSoloDevAsync(SoloDevRequest request, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<SoloDevResult>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
    public Task<StudioResult<EnterpriseResult>> RunEnterpriseAsync(EnterpriseRequest request, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<EnterpriseResult>.Fail(StudioErrorCode.HostNotSupported, "NullStudioService"));
}
