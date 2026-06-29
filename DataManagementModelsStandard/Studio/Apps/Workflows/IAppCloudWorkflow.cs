using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Studio.Apps.Workflows;

/// <summary>
/// Cloud integration: resolve connection strings / secrets from a cloud vault,
/// surface managed-identity context, and estimate per-environment cost. The
/// vault is configured via <c>StudioOptions</c> / host config; when no vault is
/// configured, the service resolves from local config and reports the gap.
/// </summary>
public interface IAppCloudWorkflow
{
    /// <summary>Resolve the live connection string for an app×env datasource, expanding secret references (e.g. <c>vault:MyDb-Prod</c>).</summary>
    Task<StudioResult<ResolvedConnection>> ResolveConnectionAsync(string appId, string envId, string datasourceName, CancellationToken ct = default);

    /// <summary>Bind a secret reference to a datasource so future resolutions pull from the vault.</summary>
    Task<StudioResult<bool>> BindSecretAsync(string appId, string envId, string datasourceName, CloudSecretRef secretRef, CancellationToken ct = default);

    /// <summary>The managed-identity / service-principal context the studio is running under (for IAM-aware datasources).</summary>
    Task<StudioResult<CloudIdentityContext>> GetIdentityContextAsync(CancellationToken ct = default);

    /// <summary>Estimate the monthly cost of an environment's datasources (cloud-managed DBs etc.). Best-effort when pricing is unavailable.</summary>
    Task<StudioResult<EnvCostEstimate>> EstimateEnvCostAsync(string appId, string envId, CancellationToken ct = default);
}

public sealed record CloudSecretRef(CloudVaultProvider Provider, string SecretName, string? Version = null);

public enum CloudVaultProvider
{
    None = 0,
    AzureKeyVault = 1,
    AwsSecretsManager = 2,
    HashiCorpVault = 3,
    Doppler = 4
}

public sealed class ResolvedConnection
{
    public required string AppId { get; set; }
    public required string EnvId { get; set; }
    public required string DatasourceName { get; set; }
    public string? ConnectionString { get; set; }
    /// <summary>True when the connection string was pulled from a vault; false when it came from local config.</summary>
    public bool FromVault { get; set; }
    public CloudVaultProvider Vault { get; set; }
    public string? ResolutionNote { get; set; }
}

public sealed class CloudIdentityContext
{
    public bool IsManagedIdentity { get; set; }
    public string? ClientId { get; set; }
    public string? TenantId { get; set; }
    public CloudVaultProvider DefaultVault { get; set; }
    public bool VaultConfigured { get; set; }
}

public sealed class EnvCostEstimate
{
    public required string AppId { get; set; }
    public required string EnvId { get; set; }
    /// <summary>Estimated monthly cost in USD. 0 when pricing is unavailable.</summary>
    public decimal EstimatedUsdPerMonth { get; set; }
    public string Currency { get; set; } = "USD";
    public List<EnvCostLine> Lines { get; set; } = new();
    public bool IsEstimated { get; set; }
}

public sealed class EnvCostLine
{
    public string DatasourceName { get; set; } = string.Empty;
    public string Tier { get; set; } = string.Empty;   // e.g. "General Purpose Gen5 2 vCore"
    public decimal UsdPerMonth { get; set; }
}
