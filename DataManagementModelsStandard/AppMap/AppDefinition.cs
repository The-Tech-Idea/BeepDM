using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.AppMap;

/// <summary>
/// A managed application — the top-level unit of the Studio. An App models a
/// <b>solution</b>: it owns a set of <see cref="AppProject"/>s (Web, API, Data,
/// Identity, …), a set of <see cref="AppEnv"/>vironments, each owning its
/// <see cref="AppDataSource"/>s. This is the App → Projects + App → Environment →
/// Datasource hierarchy the Studio is built on.
/// </summary>
public sealed class AppDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..12];
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Color { get; set; }

    /// <summary>Optional path to the .sln this app was registered from.</summary>
    public string? SolutionPath { get; set; }

    /// <summary>The projects that compose this app/solution. The schema comes from the
    /// <see cref="AppProject.IsDataProject"/> entries (their entity assemblies).</summary>
    public List<AppProject> Projects { get; set; } = new();

    /// <summary>Back-compat: the single entity assembly path, mirrored to the first data
    /// project for older callers. Prefer <see cref="Projects"/>.</summary>
    public string? AssemblyPath
    {
        get => Projects.FirstOrDefault(p => p.IsDataProject)?.AssemblyPath;
        set
        {
            var dp = Projects.FirstOrDefault(p => p.IsDataProject);
            if (dp != null) dp.AssemblyPath = value;
            else if (!string.IsNullOrWhiteSpace(value))
                Projects.Add(new AppProject { Name = Name + ".Data", Role = ProjectRole.Data, AssemblyPath = value, IsDataProject = true });
        }
    }

    public int EntityCount { get; set; }
    public List<string> ModuleNames { get; set; } = new();
    public List<AppEnv> Environments { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Projects flagged as data projects (the source of the app's entity schema).</summary>
    public List<AppProject> DataProjects => Projects.Where(p => p.IsDataProject).ToList();
    public int ProjectCount => Projects.Count;

    /// <summary>The baseline environment (the source of truth schema others promote from).
    /// Defaults to the first env marked <see cref="AppEnv.IsBaseline"/>, else the first env.</summary>
    public AppEnv? Baseline =>
        Environments.FirstOrDefault(e => e.IsBaseline) ?? Environments.FirstOrDefault();

    public AppEnv? GetEnvironment(string envId) =>
        Environments.FirstOrDefault(e =>
            e.EnvironmentId.Equals(envId, StringComparison.OrdinalIgnoreCase));

    public AppProject? GetProject(string name) =>
        Projects.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
}

/// <summary>
/// One project inside an app/solution (e.g. the Web, API, Data, or Identity project).
/// Persistable and lighter than the discovery-time <c>ProjectInfo</c>; populated by
/// "register from solution" (<c>IAppStudioService.RegisterFromSolutionAsync</c>).
/// </summary>
public sealed class AppProject
{
    public string Name { get; set; } = string.Empty;
    /// <summary>Detected/assigned role (Data, Api, Web, IdentityServer, Service, …).</summary>
    public ProjectRole Role { get; set; } = ProjectRole.Unknown;
    public string? CsprojPath { get; set; }
    /// <summary>Built assembly path (for data projects, the entity assembly).</summary>
    public string? AssemblyPath { get; set; }
    public string? OutputType { get; set; }
    public string? TargetFramework { get; set; }
    public string? RootNamespace { get; set; }
    public bool IsDataProject { get; set; }
    public bool HasDbContext { get; set; }
    public int EntityCount { get; set; }
    public List<string> ModuleNames { get; set; } = new();
    public List<string> ProjectReferences { get; set; } = new();
}

/// <summary>
/// An environment inside an App (e.g. dev, staging, production). Carries its
/// tier metadata (order, colour, approval policy) inline so the Studio can render
/// a pipeline without a separate lookup. Tier templates still live in the Studio's
/// <c>EnvironmentProfile</c>; an <see cref="AppEnv"/> is a tier *instantiated* for one App.
/// </summary>
public sealed class AppEnv
{
    public string EnvironmentId { get; set; } = string.Empty;
    public string Tier { get; set; } = "dev";            // dev | test | staging | production | custom
    public string? DisplayName { get; set; }
    public int Order { get; set; }
    public string? Color { get; set; }
    public bool IsBaseline { get; set; }
    public bool IsProduction { get; set; }
    public bool RequiresApproval { get; set; }
    public string? SchemaVersion { get; set; }
    public string? PromotedFrom { get; set; }             // env id this one was promoted from
    public DateTimeOffset? PromotedAt { get; set; }
    public List<AppDataSource> Datasources { get; set; } = new();
    public List<string> ServiceEndpoints { get; set; } = new();

    /// <summary>Per-project bindings for THIS environment: where each project runs, which
    /// datasource it uses, and which code version is deployed here. This is the
    /// Project ↔ Environment relation — the missing link between code composition and
    /// environments.</summary>
    public List<ProjectEnvBinding> ProjectBindings { get; set; } = new();

    public int DatasourceCount => Datasources.Count;
    public int HealthyDatasources => Datasources.Count(d => d.IsConnected);
    public string Label => !string.IsNullOrWhiteSpace(DisplayName) ? DisplayName : EnvironmentId;

    /// <summary>Binding for a project in this env, if any.</summary>
    public ProjectEnvBinding? GetBinding(string projectName) =>
        ProjectBindings.FirstOrDefault(b => b.ProjectName.Equals(projectName, StringComparison.OrdinalIgnoreCase));
}

/// <summary>
/// How one project is realised in one environment: its endpoint, its datasource,
/// and the code version deployed there. Drives code promotion (advance the version)
/// and environment switching (change the endpoint) separately from schema migration.
/// </summary>
public sealed class ProjectEnvBinding
{
    public string ProjectName { get; set; } = string.Empty;
    /// <summary>URL the project serves at in this env (Web/API/Identity). Null for non-hosted projects.</summary>
    public string? EndpointUrl { get; set; }
    /// <summary>Name of the <see cref="AppDataSource"/> this project uses in this env (data projects).</summary>
    public string? DatasourceName { get; set; }
    /// <summary>Code version/build deployed to this env (e.g. git sha or semver). Advanced by code promotion.</summary>
    public string? DeployedVersion { get; set; }
    public DateTimeOffset? DeployedAt { get; set; }
    public bool IsEnabled { get; set; } = true;
    /// <summary>Free-form env-specific config overrides (feature flags, keys, …).</summary>
    public Dictionary<string, string?> ConfigOverrides { get; set; } = new();
}

/// <summary>
/// A datasource bound to one App×Environment. <see cref="ProjectName"/> links it to the
/// owning project (a multi-project app may have several datasources per env — app DB,
/// identity DB, logs DB — each owned by a different data project).
/// </summary>
public sealed class AppDataSource
{
    public string Name { get; set; } = string.Empty;
    /// <summary>The project that owns this datasource in this env (usually a data project). When null, the datasource is app-wide.</summary>
    public string? ProjectName { get; set; }
    public string? ConnectionString { get; set; }
    public string Type { get; set; } = "SqlServer";       // DataSourceType name
    public string? Category { get; set; }                 // DatasourceCategory name
    public bool IsPrimary { get; set; }
    public bool IsConnected { get; set; }
    public string? ErrorMessage { get; set; }
}

public interface IAppRegistry
{
    AppDefinition RegisterApp(AppDefinition app);
    AppDefinition? GetApp(string appId);
    List<AppDefinition> GetAllApps();
    AppDefinition? AddEnvironment(string appId, AppEnv env);
    AppDefinition? SetDatasource(string appId, string envId, AppDataSource ds);
    bool RemoveApp(string appId);

    /// <summary>Full-app upsert: replace the stored app (matched by id or name) wholesale,
    /// preserving the original <see cref="AppDefinition.CreatedAt"/>. This is the persistence
    /// primitive the Studio's <c>IAppStudioService</c> uses after mutating an app's
    /// environments / datasources.</summary>
    AppDefinition? SaveApp(AppDefinition app);
}
