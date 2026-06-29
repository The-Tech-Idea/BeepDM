using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Studio.Apps.Workflows;

/// <summary>
/// Solo-developer / first-run quick start: one-call flows that create an app,
/// provision a local datasource, apply schema, and (optionally) seed — plus the
/// catalog of app templates. These collapse the multi-step wizard into a single
/// decision for the solo path.
/// </summary>
public interface IAppQuickStartWorkflow
{
    /// <summary>Available app templates (Web, Microservice, Data Warehouse, Blank, …).</summary>
    Task<StudioResult<IReadOnlyList<AppTemplate>>> ListTemplatesAsync(CancellationToken ct = default);

    /// <summary>
    /// One-call solo flow: register the app, create a local datasource (SQLite by default),
    /// bind it to the app's baseline environment, apply the template's entity schema, and
    /// optionally seed. Returns the ready-to-use app.
    /// </summary>
    Task<StudioResult<QuickStartResult>> StartAsync(QuickStartRequest request, CancellationToken ct = default);

    /// <summary>Seed an app's baseline env from a seed source (JSON/CSV folder or a seed assembly).</summary>
    Task<StudioResult<bool>> SeedAsync(string appId, string envId, string seedSource, CancellationToken ct = default);
}

public sealed class AppTemplate
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    /// <summary>Default local datasource type for the template (e.g. "SqlLite", "SqlServer").</summary>
    public string DefaultDatasourceType { get; set; } = "SqlLite";
    /// <summary>Assembly-qualified entity type names the template ships schema for.</summary>
    public List<string> EntityTypeNames { get; set; } = new();
    public bool IsBlittableOnLocal { get; set; } = true;
}

public sealed class QuickStartRequest
{
    public string AppName { get; set; } = string.Empty;
    /// <summary>Template id from <see cref="IAppQuickStartWorkflow.ListTemplatesAsync"/>. "blank" for no entities.</summary>
    public string TemplateId { get; set; } = "blank";
    /// <summary>Override the local datasource type. Default = template's default.</summary>
    public string? DatasourceType { get; set; }
    /// <summary>Optional connection string. When null, a local file DB is created under the app's data folder.</summary>
    public string? ConnectionString { get; set; }
    /// <summary>When true, run <see cref="IAppQuickStartWorkflow.SeedAsync"/> after schema apply.</summary>
    public bool Seed { get; set; }
    public string? SeedSource { get; set; }
}

public sealed class QuickStartResult
{
    public required string AppId { get; set; }
    public required string BaselineEnvId { get; set; }
    public required string DatasourceName { get; set; }
    public bool SchemaApplied { get; set; }
    public bool Seeded { get; set; }
    public string Message { get; set; } = string.Empty;
}
