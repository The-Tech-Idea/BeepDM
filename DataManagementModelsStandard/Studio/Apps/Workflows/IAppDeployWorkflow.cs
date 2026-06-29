using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.AppMap;

namespace TheTechIdea.Beep.Studio.Apps.Workflows;

/// <summary>
/// Code deployment &amp; promotion — the "code migration" track, kept separate from
/// schema migration. Deploys a project's code version to an environment and promotes
/// the baseline's code versions to a target env. Works on the app's
/// <see cref="TheTechIdea.Beep.AppMap.ProjectEnvBinding"/> records.
/// </summary>
public interface IAppDeployWorkflow
{
    /// <summary>Record a code deployment: stamp a project's version on its env binding.</summary>
    Task<StudioResult<ProjectDeployment>> DeployAsync(string appId, string envId, string projectName, string version, CancellationToken ct = default);

    /// <summary>Promote code from the baseline env to a target env: copy every project's
    /// <c>DeployedVersion</c> from baseline bindings to the target's bindings.</summary>
    Task<StudioResult<PromotionResult>> PromoteCodeAsync(string appId, string toEnv, CancellationToken ct = default);

    /// <summary>Current code deployments for an env (or the whole app when envId is null).</summary>
    Task<StudioResult<IReadOnlyList<ProjectDeployment>>> GetDeploymentsAsync(string appId, string? envId = null, CancellationToken ct = default);

    /// <summary>Bind/override a project's endpoint or datasource for a specific env (environment switching).</summary>
    Task<StudioResult<bool>> ConfigureBindingAsync(string appId, string envId, string projectName, string? endpointUrl, string? datasourceName, CancellationToken ct = default);
}

/// <summary>A code deployment recorded on an app×env×project binding.</summary>
public sealed class ProjectDeployment
{
    public required string AppId { get; set; }
    public required string EnvId { get; set; }
    public required string ProjectName { get; set; }
    public string? Version { get; set; }
    public DateTimeOffset? DeployedAt { get; set; }
    public string? EndpointUrl { get; set; }
    public string? DatasourceName { get; set; }
}

/// <summary>What a coordinated environment promotion should move. Defaults advance code + schema
/// (the safe, idempotent tracks) and leave data untouched (data moves are explicit).</summary>
public sealed class PromoteOptions
{
    public bool Code { get; set; } = true;
    public bool Schema { get; set; } = true;
    /// <summary>When true, also copy data from the baseline env (use carefully).</summary>
    public bool Data { get; set; } = false;
}

/// <summary>Outcome of promoting one environment across the code/schema/data tracks.</summary>
public sealed class EnvironmentPromotion
{
    public required string AppId { get; set; }
    public required string ToEnv { get; set; }
    public string? FromEnv { get; set; }
    public PromotionResult? Code { get; set; }
    public PromotionResult? Schema { get; set; }
    public DataSyncReport? Data { get; set; }
    public bool Succeeded { get; set; }
    public string Message { get; set; } = string.Empty;
}
