// Copyright (c) The Tech Idea. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.AppMap;
using TheTechIdea.Beep.Studio.Apps.Workflows;

namespace TheTechIdea.Beep.Studio.Apps;

/// <summary>
/// The App-centric root of the Studio. Everything in the Studio is organised around
/// an <see cref="AppDefinition"/>: an App owns <see cref="AppEnv"/>vironments, each of
/// which owns its <see cref="AppDataSource"/>s. This is the single App → Environment →
/// Datasource hierarchy the host UI builds on (replaces the old flat environment model).
/// </summary>
/// <remarks>
/// Every method returns a <see cref="StudioResult{T}"/> — business-level failures are
/// reported, never thrown. The implementation wraps the engine's <c>IAppRegistry</c>
/// (persistence) and <c>DatasourceManagementService</c> (live health), so the host never
/// touches those primitives directly.
/// </remarks>
public interface IAppStudioService
{
    /// <summary>List all apps, ordered by name.</summary>
    Task<StudioResult<IReadOnlyList<AppDefinition>>> ListAsync(CancellationToken ct = default);

    /// <summary>Get a single app by id or name.</summary>
    Task<StudioResult<AppDefinition>> GetAsync(string appId, CancellationToken ct = default);

    /// <summary>Create or update an app. Standard environments are auto-seeded when none are supplied.</summary>
    Task<StudioResult<AppDefinition>> SaveAsync(AppDefinition app, CancellationToken ct = default);

    /// <summary>
    /// Register an app from a solution (.sln). Discovers the solution's projects via the
    /// engine's <c>IAppMapService</c>, stamps each with its detected role, and stores the
    /// project composition on the app. Data projects become the source of the app's schema.
    /// </summary>
    Task<StudioResult<AppDefinition>> RegisterFromSolutionAsync(string appName, string solutionPath, CancellationToken ct = default);

    /// <summary>Register a single project (.csproj) into an existing app. Discovers the
    /// project metadata and appends it to the app's <c>Projects</c> list.</summary>
    Task<StudioResult<AppProject>> RegisterProjectAsync(string appId, string csprojPath, CancellationToken ct = default);

    /// <summary>Scan a folder or .sln for projects and add them ALL to an existing app.
    /// When a .sln is found, its projects are discovered via IAppMapService and merged
    /// (replacing any existing projects with the same name). Web, API, Identity, Service,
    /// and Test projects are included — not just Data.</summary>
    Task<StudioResult<List<AppProject>>> ScanAndAddProjectsAsync(string appId, string folderOrSlnPath, CancellationToken ct = default);

    /// <summary>Discover entity types (data classes) from a project's assembly.
    /// Populates <c>AppProject.EntityCount</c> and <c>ModuleNames</c> on the project.
    /// Uses the engine's <c>EntityDiscoveryService</c>.</summary>
    Task<StudioResult<AppProject>> DiscoverEntitiesForProjectAsync(string appId, string projectName, CancellationToken ct = default);

    /// <summary>Discover entity types for all data projects in the app.</summary>
    Task<StudioResult<List<AppProject>>> DiscoverAllEntitiesAsync(string appId, CancellationToken ct = default);

    /// <summary>Re-scan the app's solution path and refresh its project list — adds new,
    /// updates existing, removes deleted projects. Use after changing the .sln.</summary>
    Task<StudioResult<List<AppProject>>> ReScanSolutionAsync(string appId, CancellationToken ct = default);

    /// <summary>Clone an entire solution (app + envs + datasources + projects) under a new name.</summary>
    Task<StudioResult<AppDefinition>> CloneSolutionAsync(string appId, string newName, CancellationToken ct = default);

    /// <summary>Auto-find the nearest .sln file walking up from the given start path.
    /// Returns the solution path, or a clean NotFound error when none is found.</summary>
    Task<StudioResult<string>> FindNearestSolutionAsync(string startPath, CancellationToken ct = default);

    /// <summary>
    /// Detect changes across every environment of an app: schema drift
    /// (model vs DB), code versions behind the baseline, and data
    /// staleness. Returns one status per env.
    /// </summary>
    Task<StudioResult<ChangeDetection>> DetectChangesAsync(string appId, CancellationToken ct = default);

    /// <summary>List datasources registered in the engine's connection pool (available to bind to any app×env).</summary>
    Task<StudioResult<IReadOnlyList<AvailableDatasource>>> ListAvailableDatasourcesAsync(CancellationToken ct = default);

    /// <summary>
    /// Generate <c>appsettings.{env}.json</c> for one environment. Bakes every
    /// project's endpoint, every datasource's connection string, and the env metadata
    /// into a standard .NET config file the host app consumes via <c>IConfiguration</c>.
    /// This is the runtime bridge — the Studio is the source of truth for per-env config.
    /// </summary>
    Task<StudioResult<AppConfigOutput>> GenerateAppConfigAsync(string appId, string envId, CancellationToken ct = default);

    /// <summary>Generate configs for every environment of an app.</summary>
    Task<StudioResult<IReadOnlyList<AppConfigOutput>>> GenerateAllAppConfigsAsync(string appId, CancellationToken ct = default);

    /// <summary>Delete an app by id or name.</summary>
    Task<StudioResult<bool>> DeleteAsync(string appId, CancellationToken ct = default);

    /// <summary>Add an environment to an app.</summary>
    Task<StudioResult<AppDefinition>> AddEnvironmentAsync(string appId, AppEnv env, CancellationToken ct = default);

    /// <summary>Remove an environment from an app.</summary>
    Task<StudioResult<AppDefinition>> RemoveEnvironmentAsync(string appId, string envId, CancellationToken ct = default);

    /// <summary>Update an environment's metadata (label, tier, colour, approval gate, …).</summary>
    Task<StudioResult<AppEnv>> UpdateEnvironmentAsync(string appId, string envId, AppEnv updated, CancellationToken ct = default);

    /// <summary>Set (upsert) a datasource on an app×environment.</summary>
    Task<StudioResult<AppDefinition>> SetDatasourceAsync(string appId, string envId, AppDataSource ds, CancellationToken ct = default);

    /// <summary>Remove a datasource from an app×environment.</summary>
    Task<StudioResult<AppDefinition>> RemoveDatasourceAsync(string appId, string envId, string dsName, CancellationToken ct = default);

    /// <summary>Live-test a datasource connection. Returns <c>true</c> when reachable.</summary>
    Task<StudioResult<bool>> TestDatasourceAsync(string appId, string envId, string dsName, CancellationToken ct = default);

    /// <summary>
    /// Build the composite dashboard view for one app: per-environment cards with live
    /// datasource health, plus the promotion status (which envs are behind the baseline).
    /// </summary>
    Task<StudioResult<AppDashboard>> GetDashboardAsync(string appId, CancellationToken ct = default);

    /// <summary>
    /// Promote the baseline's schema version (and optionally datasource config) to a target
    /// environment. Mirrors a Heroku-pipeline / Atlas promote flow: the target's
    /// <see cref="AppEnv.SchemaVersion"/> is advanced to the baseline's and its
    /// <see cref="AppEnv.PromotedFrom"/>/<see cref="AppEnv.PromotedAt"/> are stamped.
    /// Schema-only. Use <see cref="PromoteEnvironmentAsync"/> for coordinated code+schema+data.
    /// </summary>
    Task<StudioResult<PromotionResult>> PromoteAsync(string appId, string toEnv, CancellationToken ct = default);

    /// <summary>
    /// Coordinated environment promotion: advance code (project versions), schema (DB version),
    /// and optionally data from the baseline env to the target. This is the full
    /// "promote this environment" that ties projects, environments, and the code/schema/data
    /// migration tracks together.
    /// </summary>
    Task<StudioResult<EnvironmentPromotion>> PromoteEnvironmentAsync(string appId, string toEnv, PromoteOptions? options = null, CancellationToken ct = default);

    // ── App-scoped workflows ────────────────────────────────────────────────
    // Each names the app (and environment) it operates on. These are the
    // real "work" surfaces — the aggregate CRUD above just shapes the data.

    /// <summary>Schema migration against an app×environment datasource (apply/dry-run/rollback/history/compare).</summary>
    IAppMigrationWorkflow Migrations { get; }

    /// <summary>Data movement between environments: copy, subset, PII-masked copy.</summary>
    IAppDataWorkflow Data { get; }

    /// <summary>Per-app RBAC, approval gates, policy, and audit.</summary>
    IAppGovernanceWorkflow Governance { get; }

    /// <summary>Solo-dev quick start: one-call create + local DB + migrate + seed, and app templates.</summary>
    IAppQuickStartWorkflow QuickStart { get; }

    /// <summary>Code deployment &amp; promotion (the code-migration track), plus environment switching.</summary>
    IAppDeployWorkflow Deploy { get; }

    /// <summary>CI/CD + deploy-to-env: pipeline generation, migrate-on-deploy, PR preview DBs.</summary>
    IAppCicdWorkflow Cicd { get; }

    /// <summary>Scenario workflows: Solo Dev (local-first, multi-datasource) and Enterprise (solution-based, multi-env, RBAC, CI/CD).</summary>
    IScenarioWorkflow Scenarios { get; }

    /// <summary>Cloud integration: secret-vault resolution, managed identity, cost estimates.</summary>
    IAppCloudWorkflow Cloud { get; }
}

/// <summary>Per-environment change status — schema drift, code version, data staleness.</summary>
public sealed class EnvChangeStatus
{
    public required string EnvId { get; set; }
    public bool SchemaDrifted { get; set; }
    public string? SchemaNote { get; set; }
    public bool CodeBehind { get; set; }
    public string? CodeNote { get; set; }
    public bool DataStale { get; set; }
    public bool HasChanges => SchemaDrifted || CodeBehind || DataStale;
}

/// <summary>Aggregated change detection across all envs of one app.</summary>
public sealed class ChangeDetection
{
    public required string AppId { get; set; }
    public string? BaselineEnvId { get; set; }
    public List<EnvChangeStatus> Environments { get; set; } = new();
    public int EnvsWithChanges => Environments.Count(e => e.HasChanges);
    public DateTimeOffset CheckedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>Generated <c>appsettings.{EnvId}.json</c> for one environment of an app.</summary>
public sealed class AppConfigOutput
{
    public required string AppId { get; set; }
    public required string EnvId { get; set; }
    /// <summary>Full JSON content of the generated config file.</summary>
    public string Json { get; set; } = string.Empty;
    /// <summary>Suggested file name, e.g. <c>appsettings.Development.json</c>.</summary>
    public string FileName { get; set; } = string.Empty;
    /// <summary>Connection strings extracted from the app's datasources.</summary>
    public Dictionary<string, string?> ConnectionStrings { get; set; } = new();
    /// <summary>Project endpoint URLs per project.</summary>
    public Dictionary<string, string?> ProjectEndpoints { get; set; } = new();
}

/// <summary>A datasource that can be bound to an app's environment.</summary>
public sealed class AvailableDatasource
{
    public required string Name { get; set; }
    public string Type { get; set; } = "Unknown";
    public bool IsConnected { get; set; }
    public string? Category { get; set; }
    public string? ErrorMessage { get; set; }
}
