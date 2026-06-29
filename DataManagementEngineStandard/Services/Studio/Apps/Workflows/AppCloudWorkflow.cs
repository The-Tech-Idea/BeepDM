using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.AppMap;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Studio.Apps.Workflows;

namespace TheTechIdea.Beep.Studio.Apps;

/// <summary>
/// Cloud integration. Resolves connection strings (expanding <c>vault:Name</c>
/// references), surfaces managed-identity context, and estimates per-env cost.
/// When no vault is configured, resolution falls back to the locally-stored
/// connection string and reports the gap — never invents a secret.
/// </summary>
internal sealed class AppCloudWorkflow : IAppCloudWorkflow
{
    private readonly IDMEEditor _editor;
    public AppCloudWorkflow(IDMEEditor editor) => _editor = editor;

    public Task<StudioResult<ResolvedConnection>> ResolveConnectionAsync(string appId, string envId, string datasourceName, CancellationToken ct = default)
    {
        var ds = ResolveDatasource(appId, envId, datasourceName);
        if (ds.Error is { } err) return Task.FromResult(StudioResult<ResolvedConnection>.Fail(err));

        var connStr = ds.DataSource!.ConnectionString;
        var resolved = new ResolvedConnection { AppId = appId, EnvId = envId, DatasourceName = datasourceName };

        if (!string.IsNullOrWhiteSpace(connStr) && connStr.StartsWith("vault:", StringComparison.OrdinalIgnoreCase))
        {
            // Vault reference — we cannot resolve without a configured client. Report honestly.
            var parts = connStr.Substring("vault:".Length).Split('-', 2);
            resolved.Vault = parts.Length > 0 && Enum.TryParse<CloudVaultProvider>(parts[0], true, out var v) ? v : CloudVaultProvider.None;
            resolved.FromVault = false;
            resolved.ResolutionNote = "Vault reference present but no vault client is configured. Configure StudioOptions/CloudVault to resolve.";
        }
        else
        {
            resolved.ConnectionString = connStr;
            resolved.FromVault = false;
            resolved.ResolutionNote = "Resolved from local config.";
        }
        return Task.FromResult(StudioResult<ResolvedConnection>.Ok(resolved));
    }

    public Task<StudioResult<bool>> BindSecretAsync(string appId, string envId, string datasourceName, CloudSecretRef secretRef, CancellationToken ct = default)
    {
        var app = _editor.AppRegistry?.GetApp(appId);
        var env = app?.GetEnvironment(envId);
        var ds = env?.Datasources.FirstOrDefault(d => d.Name.Equals(datasourceName, StringComparison.OrdinalIgnoreCase));
        if (ds == null) return Task.FromResult(StudioResult<bool>.Fail(StudioErrorCode.NotFound, "Datasource not found on app/env."));
        // Record the secret reference as the connection string placeholder.
        ds.ConnectionString = $"vault:{secretRef.Provider}-{secretRef.SecretName}" + (secretRef.Version != null ? $"?version={secretRef.Version}" : "");
        _editor.AppRegistry?.SaveApp(app!);
        return Task.FromResult(StudioResult<bool>.Ok(true));
    }

    public Task<StudioResult<CloudIdentityContext>> GetIdentityContextAsync(CancellationToken ct = default)
    {
        var ctx = new CloudIdentityContext
        {
            // The editor does not expose managed-identity state; the host overrides this
            // when running on Azure/AWS. Default = local, no vault.
            IsManagedIdentity = false,
            VaultConfigured = false,
            DefaultVault = CloudVaultProvider.None
        };
        return Task.FromResult(StudioResult<CloudIdentityContext>.Ok(ctx));
    }

    public Task<StudioResult<EnvCostEstimate>> EstimateEnvCostAsync(string appId, string envId, CancellationToken ct = default)
    {
        var app = _editor.AppRegistry?.GetApp(appId);
        var env = app?.GetEnvironment(envId);
        if (env == null) return Task.FromResult(StudioResult<EnvCostEstimate>.Fail(StudioErrorCode.NotFound, $"Environment '{envId}' not found."));

        // Honest zero estimate with the env's datasources enumerated. The host
        // (which knows its cloud pricing) refines these lines.
        var estimate = new EnvCostEstimate
        {
            AppId = appId, EnvId = envId, EstimatedUsdPerMonth = 0, IsEstimated = true
        };
        foreach (var ds in env.Datasources)
            estimate.Lines.Add(new EnvCostLine { DatasourceName = ds.Name, Tier = ds.Type, UsdPerMonth = 0 });
        return Task.FromResult(StudioResult<EnvCostEstimate>.Ok(estimate));
    }

    private (AppDataSource? DataSource, StudioError? Error) ResolveDatasource(string appId, string envId, string datasourceName)
    {
        var app = _editor.AppRegistry?.GetApp(appId);
        if (app == null) return (null, new StudioError(StudioErrorCode.NotFound, $"App '{appId}' not found.", null, null));
        var env = app.GetEnvironment(envId);
        if (env == null) return (null, new StudioError(StudioErrorCode.NotFound, $"Environment '{envId}' not found.", null, null));
        var ds = env.Datasources.FirstOrDefault(d => d.Name.Equals(datasourceName, StringComparison.OrdinalIgnoreCase));
        if (ds == null) return (null, new StudioError(StudioErrorCode.NotFound, $"Datasource '{datasourceName}' not found.", null, null));
        return (ds, null);
    }
}
