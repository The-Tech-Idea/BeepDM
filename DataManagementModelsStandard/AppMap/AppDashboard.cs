using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.AppMap;

/// <summary>
/// Composite read model for one App's dashboard: the app, a per-environment
/// card list (with live datasource health), and the promotion status (which envs
/// are behind the baseline). Returned by <c>IAppStudioService.GetDashboardAsync</c>.
/// </summary>
public sealed class AppDashboard
{
    public required AppDefinition App { get; set; }
    public List<AppEnvDashboard> Environments { get; set; } = new();
    public string? BaselineEnvId { get; set; }
    public List<string> EnvsBehindBaseline { get; set; } = new();
    public int TotalDatasources { get; set; }
    public int HealthyDatasources { get; set; }
    public bool AnyBehind => EnvsBehindBaseline.Count > 0;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>One environment's card in the dashboard pipeline.</summary>
public sealed class AppEnvDashboard
{
    public required string EnvironmentId { get; set; }
    public required string Tier { get; set; }
    public string Label { get; set; } = string.Empty;
    public int Order { get; set; }
    public string? Color { get; set; }
    public bool IsBaseline { get; set; }
    public bool IsProduction { get; set; }
    public bool RequiresApproval { get; set; }
    public string? SchemaVersion { get; set; }
    public string? PromotedFrom { get; set; }
    public DateTimeOffset? PromotedAt { get; set; }
    public List<AppDataSource> Datasources { get; set; } = new();
    public int DatasourceCount => Datasources.Count;
    public int HealthyDatasourceCount => Datasources.FindAll(d => d.IsConnected).Count;
    public bool IsBehindBaseline { get; set; }
}

/// <summary>Outcome of promoting schema/config from one environment to another.</summary>
public sealed class PromotionResult
{
    public required string AppId { get; set; }
    public required string AppName { get; set; }
    public required string FromEnv { get; set; }
    public required string ToEnv { get; set; }
    public bool Succeeded { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset PromotedAt { get; set; } = DateTimeOffset.UtcNow;
}
