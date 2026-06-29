// Copyright (c) The Tech Idea. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
/// Real implementation of <see cref="IAppStudioService"/>. Composes the engine's
/// <c>IAppRegistry</c> (the App → Environment → Datasource persistence aggregate) with
/// <c>DatasourceManagementService</c> (live connection health). The host UI depends on
/// this facade only; it never reads <c>ConfigEditor.DataConnections</c> or calls
/// <c>IAppRegistry</c> directly.
/// </summary>
public sealed class AppStudioService : IAppStudioService
{
    private readonly IDMEEditor _editor;

    public AppStudioService(IDMEEditor editor)
    {
        _editor = editor ?? throw new ArgumentNullException(nameof(editor));
    }

    private IAppRegistry? Registry => _editor.AppRegistry;

    // ── App-scoped workflows (lazy, all backed by the same editor) ───────────
    private IAppMigrationWorkflow? _migrations;
    private IAppDataWorkflow? _data;
    private IAppGovernanceWorkflow? _governance;
    private IAppQuickStartWorkflow? _quickStart;
    private IAppDeployWorkflow? _deploy;
    private IAppCicdWorkflow? _cicd;
    private IAppCloudWorkflow? _cloud;
    private IScenarioWorkflow? _scenarios;

    /// <inheritdoc />
    public IAppMigrationWorkflow Migrations => _migrations ??= new AppMigrationWorkflow(_editor);
    /// <inheritdoc />
    public IAppDataWorkflow Data => _data ??= new AppDataWorkflow(_editor);
    /// <inheritdoc />
    public IAppGovernanceWorkflow Governance => _governance ??= new AppGovernanceWorkflow(_editor);
    /// <inheritdoc />
    public IAppQuickStartWorkflow QuickStart => _quickStart ??= new AppQuickStartWorkflow(_editor);
    /// <inheritdoc />
    public IAppDeployWorkflow Deploy => _deploy ??= new AppDeployWorkflow(_editor);
    /// <inheritdoc />
    public IAppCicdWorkflow Cicd => _cicd ??= new AppCicdWorkflow(_editor);
    /// <inheritdoc />
    public IAppCloudWorkflow Cloud => _cloud ??= new AppCloudWorkflow(_editor);
    /// <inheritdoc />
    public IScenarioWorkflow Scenarios => _scenarios ??= new ScenarioWorkflow(_editor);

    /// <inheritdoc />
    public Task<StudioResult<IReadOnlyList<AppDefinition>>> ListAsync(CancellationToken ct = default)
    {
        var registry = Registry;
        if (registry == null)
            return Task.FromResult(StudioResult<IReadOnlyList<AppDefinition>>.Fail(
                StudioErrorCode.HostNotSupported, "App registry is not available."));

        return Task.FromResult(StudioResult<IReadOnlyList<AppDefinition>>.Ok(
            registry.GetAllApps()));
    }

    /// <inheritdoc />
    public Task<StudioResult<AppDefinition>> GetAsync(string appId, CancellationToken ct = default)
    {
        var registry = Registry;
        if (registry == null)
            return NotFound<AppDefinition>("App registry is not available.");

        var app = registry.GetApp(appId);
        return app == null
            ? NotFound<AppDefinition>($"App '{appId}' not found.")
            : Task.FromResult(StudioResult<AppDefinition>.Ok(app));
    }

    /// <inheritdoc />
    public Task<StudioResult<AppDefinition>> SaveAsync(AppDefinition app, CancellationToken ct = default)
    {
        var registry = Registry;
        if (registry == null) return NotFound<AppDefinition>("App registry is not available.");
        if (app == null || string.IsNullOrWhiteSpace(app.Name))
            return Invalid<AppDefinition>("App with a valid Name is required.");

        var saved = registry.SaveApp(app);
        return saved == null
            ? NotFound<AppDefinition>($"Could not save app '{app?.Name}'.")
            : Task.FromResult(StudioResult<AppDefinition>.Ok(saved));
    }

    /// <inheritdoc />
    public async Task<StudioResult<AppDefinition>> RegisterFromSolutionAsync(string appName, string solutionPath, CancellationToken ct = default)
    {
        var registry = Registry;
        if (registry == null) return StudioResult<AppDefinition>.Fail(StudioErrorCode.HostNotSupported, "App registry is not available.");
        if (string.IsNullOrWhiteSpace(appName))
            return StudioResult<AppDefinition>.Fail(StudioErrorCode.InvalidArgument, "appName is required.");
        if (string.IsNullOrWhiteSpace(solutionPath) || !System.IO.File.Exists(solutionPath))
            return StudioResult<AppDefinition>.Fail(StudioErrorCode.InvalidArgument, $"Solution not found: '{solutionPath}'.");

        try
        {
            var appMap = await _editor.AppMap.CreateAppMapAsync(solutionPath, null, ct);
            if (appMap == null)
                return StudioResult<AppDefinition>.Fail(StudioErrorCode.InvalidArgument, $"Could not discover a solution at '{solutionPath}'.");

            var app = new AppDefinition
            {
                Name = appName,
                SolutionPath = solutionPath,
                Description = $"Registered from {System.IO.Path.GetFileName(solutionPath)}"
            };

            foreach (var assignment in appMap.Projects)
            {
                var p = assignment.Project;
                var isData = assignment.Role == TheTechIdea.Beep.Utilities.ProjectRole.Data
                             || p.DataFolders.Any(f => f.HasDbContext);
                var project = new AppProject
                {
                    Name = p.Name,
                    Role = assignment.Role,
                    CsprojPath = p.CsprojPath,
                    OutputType = p.OutputType,
                    TargetFramework = p.TargetFramework,
                    RootNamespace = p.RootNamespace,
                    IsDataProject = isData,
                    HasDbContext = p.DataFolders.Any(f => f.HasDbContext),
                    AssemblyPath = ResolveAssemblyPath(p),
                    ProjectReferences = p.ProjectReferences ?? new()
                };
                app.Projects.Add(project);
            }

            var saved = registry.SaveApp(app);
            if (saved == null)
                return StudioResult<AppDefinition>.Fail(StudioErrorCode.NotFound, "Discovered projects but could not persist the app.");
            return StudioResult<AppDefinition>.Ok(saved);
        }
        catch (Exception ex)
        {
            return StudioResult<AppDefinition>.Fail(StudioErrorCode.HostNotSupported, ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<StudioResult<AppProject>> RegisterProjectAsync(string appId, string csprojPath, CancellationToken ct = default)
    {
        var registry = Registry;
        if (registry == null) return StudioResult<AppProject>.Fail(StudioErrorCode.HostNotSupported, "App registry unavailable.");
        var app = registry.GetApp(appId);
        if (app == null) return StudioResult<AppProject>.Fail(StudioErrorCode.NotFound, "App not found.");
        if (string.IsNullOrWhiteSpace(csprojPath) || !System.IO.File.Exists(csprojPath))
            return StudioResult<AppProject>.Fail(StudioErrorCode.InvalidArgument, $"Project file not found: '{csprojPath}'.");
        try
        {
            var discovery = new TheTechIdea.Beep.Services.AppMap.SolutionDiscoveryService(_editor);
            var info = await discovery.DiscoverProjectAsync(csprojPath, ct);
            if (info == null) return StudioResult<AppProject>.Fail(StudioErrorCode.InvalidArgument, $"Could not parse '{csprojPath}'.");
            var (role, _, _) = TheTechIdea.Beep.Services.AppMap.RoleDetectionHeuristics.Detect(info);
            var project = new AppProject
            {
                Name = info.Name, Role = role, CsprojPath = info.CsprojPath,
                OutputType = info.OutputType, TargetFramework = info.TargetFramework,
                RootNamespace = info.RootNamespace,
                IsDataProject = role == TheTechIdea.Beep.Utilities.ProjectRole.Data || info.DataFolders.Any(f => f.HasDbContext),
                HasDbContext = info.DataFolders.Any(f => f.HasDbContext),
                AssemblyPath = ResolveAssemblyPath(info),
                ProjectReferences = info.ProjectReferences ?? new()
            };
            app.Projects.Add(project);
            registry.SaveApp(app);
            return StudioResult<AppProject>.Ok(project);
        }
        catch (Exception ex) { return StudioResult<AppProject>.Fail(StudioErrorCode.HostNotSupported, ex.Message); }
    }

    /// <inheritdoc />
    public async Task<StudioResult<List<AppProject>>> ScanAndAddProjectsAsync(string appId, string folderOrSlnPath, CancellationToken ct = default)
    {
        var registry = Registry;
        if (registry == null) return StudioResult<List<AppProject>>.Fail(StudioErrorCode.HostNotSupported, "App registry unavailable.");
        var app = registry.GetApp(appId);
        if (app == null) return StudioResult<List<AppProject>>.Fail(StudioErrorCode.NotFound, "App not found.");
        if (string.IsNullOrWhiteSpace(folderOrSlnPath) || !(System.IO.File.Exists(folderOrSlnPath) || System.IO.Directory.Exists(folderOrSlnPath)))
            return StudioResult<List<AppProject>>.Fail(StudioErrorCode.InvalidArgument, $"Path not found: '{folderOrSlnPath}'.");

        var results = new List<AppProject>();

        // Resolve to a .sln if it's a folder
        string path = folderOrSlnPath;
        if (System.IO.Directory.Exists(path))
        {
            var slnFiles = System.IO.Directory.GetFiles(path, "*.sln", System.IO.SearchOption.TopDirectoryOnly);
            if (slnFiles.Length == 1) path = slnFiles[0];
            else if (slnFiles.Length > 1) path = slnFiles.OrderBy(f => System.IO.File.GetLastWriteTime(f)).Last();
            // if no .sln, we'll scan .csproj files directly
        }

        // 1. Try solution discovery
        if (path.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) && System.IO.File.Exists(path))
        {
            try
            {
                var appMap = await _editor.AppMap.CreateAppMapAsync(path, null, ct);
                if (appMap != null)
                {
                    foreach (var assignment in appMap.Projects)
                    {
                        var p = assignment.Project;
                        var existing = app.Projects.FirstOrDefault(ep => ep.Name.Equals(p.Name, StringComparison.OrdinalIgnoreCase));
                        if (existing != null) app.Projects.Remove(existing);
                        var isData = assignment.Role == TheTechIdea.Beep.Utilities.ProjectRole.Data || p.DataFolders.Any(f => f.HasDbContext);
                        var project = new AppProject
                        {
                            Name = p.Name, Role = assignment.Role, CsprojPath = p.CsprojPath,
                            OutputType = p.OutputType, TargetFramework = p.TargetFramework, RootNamespace = p.RootNamespace,
                            IsDataProject = isData, HasDbContext = p.DataFolders.Any(f => f.HasDbContext),
                            AssemblyPath = ResolveAssemblyPath(p),
                            ProjectReferences = p.ProjectReferences ?? new()
                        };
                        app.Projects.Add(project);
                        results.Add(project);
                    }
                    app.SolutionPath = path;
                    registry.SaveApp(app);
                    return StudioResult<List<AppProject>>.Ok(results);
                }
            }
            catch { /* fall through to individual .csproj scan */ }
        }

        // 2. Fallback: scan for individual .csproj files in the folder
        var rootDir = System.IO.Directory.Exists(folderOrSlnPath) ? folderOrSlnPath : System.IO.Path.GetDirectoryName(folderOrSlnPath) ?? folderOrSlnPath;
        if (System.IO.Directory.Exists(rootDir))
        {
            var csprojFiles = System.IO.Directory.GetFiles(rootDir, "*.csproj", System.IO.SearchOption.AllDirectories)
                .Take(50).ToList();
            var discovery = new TheTechIdea.Beep.Services.AppMap.SolutionDiscoveryService(_editor);
            foreach (var csproj in csprojFiles)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    var info = await discovery.DiscoverProjectAsync(csproj, ct);
                    if (info == null) continue;
                    var existing = app.Projects.FirstOrDefault(p => p.Name.Equals(info.Name, StringComparison.OrdinalIgnoreCase));
                    if (existing != null) app.Projects.Remove(existing);
                    var (role, _, _) = TheTechIdea.Beep.Services.AppMap.RoleDetectionHeuristics.Detect(info);
                    var project = new AppProject
                    {
                        Name = info.Name, Role = role, CsprojPath = info.CsprojPath,
                        OutputType = info.OutputType, TargetFramework = info.TargetFramework, RootNamespace = info.RootNamespace,
                        IsDataProject = role == TheTechIdea.Beep.Utilities.ProjectRole.Data || info.DataFolders.Any(f => f.HasDbContext),
                        HasDbContext = info.DataFolders.Any(f => f.HasDbContext),
                        AssemblyPath = ResolveAssemblyPath(info),
                        ProjectReferences = info.ProjectReferences ?? new()
                    };
                    app.Projects.Add(project);
                    results.Add(project);
                }
                catch { /* skip unparseable csproj */ }
            }
            registry.SaveApp(app);
        }

        return results.Count > 0
            ? StudioResult<List<AppProject>>.Ok(results)
            : StudioResult<List<AppProject>>.Fail(StudioErrorCode.NotFound, "No projects found at the given path.");
    }

    /// <inheritdoc />
    public Task<StudioResult<string>> FindNearestSolutionAsync(string startPath, CancellationToken ct = default)
    {
        try
        {
            var dir = System.IO.Directory.Exists(startPath) ? startPath : System.IO.Path.GetDirectoryName(startPath) ?? startPath;
            for (int level = 0; level < 5; level++)
            {
                var slnFiles = System.IO.Directory.GetFiles(dir, "*.sln", System.IO.SearchOption.TopDirectoryOnly);
                if (slnFiles.Length == 1) return Task.FromResult(StudioResult<string>.Ok(slnFiles[0]));
                if (slnFiles.Length > 1)
                {
                    var dirName = new System.IO.DirectoryInfo(dir).Name;
                    var best = slnFiles.FirstOrDefault(f => System.IO.Path.GetFileNameWithoutExtension(f).Equals(dirName, StringComparison.OrdinalIgnoreCase)) ?? slnFiles[0];
                    return Task.FromResult(StudioResult<string>.Ok(best));
                }
                var parent = System.IO.Directory.GetParent(dir);
                if (parent == null) break;
                dir = parent.FullName;
            }
            return Task.FromResult(StudioResult<string>.Fail(StudioErrorCode.NotFound, "No .sln found within 5 parent directories."));
        }
        catch (Exception ex) { return Task.FromResult(StudioResult<string>.Fail(StudioErrorCode.HostNotSupported, ex.Message)); }
    }

    /// <inheritdoc />
    public async Task<StudioResult<ChangeDetection>> DetectChangesAsync(string appId, CancellationToken ct = default)
    {
        var registry = Registry;
        if (registry == null) return StudioResult<ChangeDetection>.Fail(StudioErrorCode.HostNotSupported, "App registry unavailable.");
        var app = registry.GetApp(appId);
        if (app == null) return StudioResult<ChangeDetection>.Fail(StudioErrorCode.NotFound, "App not found.");
        var baseline = app.Baseline;
        var cd = new ChangeDetection { AppId = appId, BaselineEnvId = baseline?.EnvironmentId };
        var migration = new AppMigrationWorkflow(_editor);
        foreach (var env in app.Environments)
        {
            var status = new EnvChangeStatus { EnvId = env.EnvironmentId };
            try
            {
                var mr = await migration.DryRunAsync(appId, env.EnvironmentId, ct);
                status.SchemaDrifted = mr.IsSuccess && mr.Value != null && mr.Value.OperationsApplied > 0;
                status.SchemaNote = status.SchemaDrifted ? $"{mr.Value!.OperationsApplied} pending op(s)" : "Up to date";
            }
            catch { status.SchemaNote = "Could not check."; }
            if (baseline != null && !string.Equals(env.EnvironmentId, baseline.EnvironmentId, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var baselineBinding in baseline.ProjectBindings)
                {
                    if (string.IsNullOrWhiteSpace(baselineBinding.DeployedVersion)) continue;
                    var envBinding = env.ProjectBindings.FirstOrDefault(b => b.ProjectName.Equals(baselineBinding.ProjectName, StringComparison.OrdinalIgnoreCase));
                    if (envBinding == null || !string.Equals(envBinding.DeployedVersion, baselineBinding.DeployedVersion, StringComparison.OrdinalIgnoreCase))
                    { status.CodeBehind = true; status.CodeNote = $"Behind baseline ({baselineBinding.ProjectName})"; break; }
                }
            }
            if (!status.CodeBehind) status.CodeNote = "Code matches baseline";
            // Data staleness: dry-run the migration on this env to detect pending schema changes
            if (baseline != null && !string.Equals(env.EnvironmentId, baseline.EnvironmentId, StringComparison.OrdinalIgnoreCase))
            {
                try { var mr = await migration.DryRunAsync(appId, env.EnvironmentId, ct); status.DataStale = mr.IsSuccess && mr.Value is { OperationsApplied: > 0 }; }
                catch { /* best-effort */ }
            }
            cd.Environments.Add(status);
        }
        return StudioResult<ChangeDetection>.Ok(cd);
    }

    /// <inheritdoc />
    public Task<StudioResult<IReadOnlyList<AvailableDatasource>>> ListAvailableDatasourcesAsync(CancellationToken ct = default)
    {
        try
        {
            var mgmt = new TheTechIdea.Beep.Services.DatasourceManagement.DatasourceManagementService(_editor);
            var all = mgmt.GetAllDatasources();
            var list = all.Where(c => c != null && !string.IsNullOrWhiteSpace(c.ConnectionName))
                .Select(c =>
                {
                    var status = mgmt.GetDatasourceStatus(c.ConnectionName);
                    return new AvailableDatasource
                    {
                        Name = c.ConnectionName,
                        Type = c.DatabaseType.ToString(),
                        IsConnected = status?.IsConnected ?? false,
                        Category = status?.Category,
                        ErrorMessage = status?.ErrorMessage
                    };
                }).ToList();
            return Task.FromResult(StudioResult<IReadOnlyList<AvailableDatasource>>.Ok(list));
        }
        catch (Exception ex) { return Task.FromResult(StudioResult<IReadOnlyList<AvailableDatasource>>.Fail(StudioErrorCode.HostNotSupported, ex.Message)); }
    }

    /// <inheritdoc />
    public Task<StudioResult<bool>> DeleteAsync(string appId, CancellationToken ct = default)
    {
        var registry = Registry;
        if (registry == null)
            return Task.FromResult(StudioResult<bool>.Fail(
                StudioErrorCode.HostNotSupported, "App registry is not available."));

        return Task.FromResult(registry.RemoveApp(appId)
            ? StudioResult<bool>.Ok(true)
            : StudioResult<bool>.Fail(StudioErrorCode.NotFound, $"App '{appId}' not found."));
    }

    /// <inheritdoc />
    public Task<StudioResult<AppDefinition>> AddEnvironmentAsync(string appId, AppEnv env, CancellationToken ct = default)
    {
        var registry = Registry;
        if (registry == null) return NotFound<AppDefinition>("App registry is not available.");
        if (env == null || string.IsNullOrWhiteSpace(env.EnvironmentId))
            return Invalid<AppDefinition>("Environment with a valid EnvironmentId is required.");

        var updated = registry.AddEnvironment(appId, env);
        return updated == null
            ? NotFound<AppDefinition>($"App '{appId}' not found, or environment '{env.EnvironmentId}' already exists.")
            : Task.FromResult(StudioResult<AppDefinition>.Ok(updated));
    }

    /// <inheritdoc />
    public Task<StudioResult<AppDefinition>> RemoveEnvironmentAsync(string appId, string envId, CancellationToken ct = default)
    {
        var (app, err) = Mutate(appId);
        if (err != null) return Task.FromResult(StudioResult<AppDefinition>.Fail(err.Value));
        var env = app!.GetEnvironment(envId);
        if (env == null)
            return NotFound<AppDefinition>($"Environment '{envId}' not found on app '{appId}'.");
        if (env.IsBaseline && app.Environments.Count(e => e.IsBaseline) <= 1)
            return Invalid<AppDefinition>("Cannot remove the baseline environment.");
        app.Environments.Remove(env);
        return Persist(app);
    }

    /// <inheritdoc />
    public Task<StudioResult<AppEnv>> UpdateEnvironmentAsync(string appId, string envId, AppEnv updated, CancellationToken ct = default)
    {
        var (app, err) = Mutate(appId);
        if (err != null) return Task.FromResult(StudioResult<AppEnv>.Fail(err.Value));
        if (updated == null) return Invalid<AppEnv>("updated is required.");
        var env = app!.GetEnvironment(envId);
        if (env == null) return NotFound<AppEnv>($"Environment '{envId}' not found.");

        if (!string.IsNullOrWhiteSpace(updated.DisplayName)) env.DisplayName = updated.DisplayName;
        if (!string.IsNullOrWhiteSpace(updated.Tier)) env.Tier = updated.Tier;
        if (updated.Order != default) env.Order = updated.Order;
        if (!string.IsNullOrWhiteSpace(updated.Color)) env.Color = updated.Color;
        env.RequiresApproval = updated.RequiresApproval;
        if (updated.IsBaseline != env.IsBaseline)
        {
            // Unset baseline on others if setting a new one
            if (updated.IsBaseline) foreach (var e in app.Environments.Where(e => !e.EnvironmentId.Equals(envId, StringComparison.OrdinalIgnoreCase))) e.IsBaseline = false;
            env.IsBaseline = updated.IsBaseline;
        }

        Registry?.SaveApp(app);
        return Task.FromResult(StudioResult<AppEnv>.Ok(env));
    }

    /// <inheritdoc />
    public Task<StudioResult<AppDefinition>> SetDatasourceAsync(string appId, string envId, AppDataSource ds, CancellationToken ct = default)
    {
        var registry = Registry;
        if (registry == null) return NotFound<AppDefinition>("App registry is not available.");
        if (ds == null || string.IsNullOrWhiteSpace(ds.Name))
            return Invalid<AppDefinition>("Datasource with a valid Name is required.");

        var updated = registry.SetDatasource(appId, envId, ds);
        return updated == null
            ? NotFound<AppDefinition>($"App '{appId}' or environment '{envId}' not found.")
            : Task.FromResult(StudioResult<AppDefinition>.Ok(updated));
    }

    /// <inheritdoc />
    public Task<StudioResult<AppDefinition>> RemoveDatasourceAsync(string appId, string envId, string dsName, CancellationToken ct = default)
    {
        var (app, err) = Mutate(appId);
        if (err != null) return Task.FromResult(StudioResult<AppDefinition>.Fail(err.Value));
        var env = app!.GetEnvironment(envId);
        if (env == null)
            return NotFound<AppDefinition>($"Environment '{envId}' not found on app '{appId}'.");
        var ds = env.Datasources.FirstOrDefault(d =>
            d.Name.Equals(dsName, StringComparison.OrdinalIgnoreCase));
        if (ds == null)
            return NotFound<AppDefinition>($"Datasource '{dsName}' not found on '{appId}/{envId}'.");
        env.Datasources.Remove(ds);
        return Persist(app);
    }

    /// <inheritdoc />
    public Task<StudioResult<bool>> TestDatasourceAsync(string appId, string envId, string dsName, CancellationToken ct = default)
    {
        var (app, err) = Mutate(appId);
        if (err != null)
            return Task.FromResult(StudioResult<bool>.Fail(err.Value));
        var env = app!.GetEnvironment(envId);
        var ds = env?.Datasources.FirstOrDefault(d =>
            d.Name.Equals(dsName, StringComparison.OrdinalIgnoreCase));
        if (ds == null)
            return Task.FromResult(StudioResult<bool>.Fail(
                StudioErrorCode.NotFound, $"Datasource '{dsName}' not found on '{appId}/{envId}'."));

        try
        {
            var mgmt = new TheTechIdea.Beep.Services.DatasourceManagement.DatasourceManagementService(_editor);
            var conn = mgmt.GetDatasource(ds.Name);
            if (conn == null)
            {
                // No matching registered connection to test — mark the app datasource as unknown.
                ds.IsConnected = false;
                ds.ErrorMessage = "Datasource is not registered in the connection list.";
                _ = Persist(app);
                return Task.FromResult(StudioResult<bool>.Ok(false));
            }
            var state = mgmt.TestConnection(conn);
            ds.IsConnected = state == System.Data.ConnectionState.Open;
            ds.ErrorMessage = ds.IsConnected ? null : state.ToString();
            _ = Persist(app);
            return Task.FromResult(StudioResult<bool>.Ok(ds.IsConnected));
        }
        catch (Exception ex)
        {
            ds.IsConnected = false;
            ds.ErrorMessage = ex.Message;
            _ = Persist(app);
            return Task.FromResult(StudioResult<bool>.Fail(StudioErrorCode.HostNotSupported, ex.Message));
        }
    }

    /// <inheritdoc />
    public Task<StudioResult<AppDashboard>> GetDashboardAsync(string appId, CancellationToken ct = default)
    {
        var registry = Registry;
        if (registry == null)
            return Task.FromResult(StudioResult<AppDashboard>.Fail(
                StudioErrorCode.HostNotSupported, "App registry is not available."));

        var app = registry.GetApp(appId);
        if (app == null)
            return Task.FromResult(StudioResult<AppDashboard>.Fail(
                StudioErrorCode.NotFound, $"App '{appId}' not found."));

        // Optionally enrich datasource health from the live connection registry.
        try
        {
            var mgmt = new TheTechIdea.Beep.Services.DatasourceManagement.DatasourceManagementService(_editor);
            foreach (var env in app.Environments)
            {
                foreach (var ds in env.Datasources)
                {
                    var status = mgmt.GetDatasourceStatus(ds.Name);
                    if (status != null)
                    {
                        ds.IsConnected = status.IsConnected;
                        if (!string.IsNullOrWhiteSpace(status.Category)) ds.Category = status.Category;
                        ds.ErrorMessage = status.ErrorMessage;
                    }
                }
            }
        }
        catch { /* health enrichment is best-effort */ }

        var baseline = app.Baseline;
        var dashboard = new AppDashboard
        {
            App = app,
            BaselineEnvId = baseline?.EnvironmentId,
        };

        foreach (var env in app.Environments.OrderBy(e => e.Order).ThenBy(e => e.EnvironmentId))
        {
            var behind = baseline != null
                         && !env.IsBaseline
                         && !string.IsNullOrWhiteSpace(baseline.SchemaVersion)
                         && !string.Equals(env.SchemaVersion, baseline.SchemaVersion, StringComparison.OrdinalIgnoreCase);
            dashboard.Environments.Add(new AppEnvDashboard
            {
                EnvironmentId = env.EnvironmentId,
                Tier = env.Tier,
                Label = env.Label,
                Order = env.Order,
                Color = env.Color,
                IsBaseline = env.IsBaseline,
                IsProduction = env.IsProduction,
                RequiresApproval = env.RequiresApproval,
                SchemaVersion = env.SchemaVersion,
                PromotedFrom = env.PromotedFrom,
                PromotedAt = env.PromotedAt,
                Datasources = env.Datasources,
                IsBehindBaseline = behind,
            });
            if (behind) dashboard.EnvsBehindBaseline.Add(env.EnvironmentId);
        }

        dashboard.TotalDatasources = dashboard.Environments.Sum(e => e.DatasourceCount);
        dashboard.HealthyDatasources = dashboard.Environments.Sum(e => e.HealthyDatasourceCount);
        return Task.FromResult(StudioResult<AppDashboard>.Ok(dashboard));
    }

    /// <inheritdoc />
    public Task<StudioResult<PromotionResult>> PromoteAsync(string appId, string toEnv, CancellationToken ct = default)
    {
        var (app, err) = Mutate(appId);
        if (err != null)
            return Task.FromResult(StudioResult<PromotionResult>.Fail(err.Value));

        var baseline = app!.Baseline;
        var target = app.GetEnvironment(toEnv);
        if (baseline == null)
            return Task.FromResult(StudioResult<PromotionResult>.Fail(
                StudioErrorCode.InvalidArgument, "App has no baseline environment to promote from."));
        if (target == null)
            return Task.FromResult(StudioResult<PromotionResult>.Fail(
                StudioErrorCode.NotFound, $"Environment '{toEnv}' not found on app '{appId}'."));
        if (target.EnvironmentId.Equals(baseline.EnvironmentId, StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(StudioResult<PromotionResult>.Fail(
                StudioErrorCode.InvalidArgument, "Cannot promote an environment to itself."));

        target.SchemaVersion = baseline.SchemaVersion;
        target.PromotedFrom = baseline.EnvironmentId;
        target.PromotedAt = DateTimeOffset.UtcNow;
        var saved = Registry?.SaveApp(app);

        var result = new PromotionResult
        {
            AppId = app.Id,
            AppName = app.Name,
            FromEnv = baseline.EnvironmentId,
            ToEnv = target.EnvironmentId,
            Succeeded = saved != null,
            Message = saved == null
                ? "Promotion applied in memory but could not be persisted."
                : $"Promoted baseline '{baseline.EnvironmentId}' → '{target.EnvironmentId}'."
        };
        return Task.FromResult(StudioResult<PromotionResult>.Ok(result));
    }

    /// <inheritdoc />
    public async Task<StudioResult<EnvironmentPromotion>> PromoteEnvironmentAsync(string appId, string toEnv, PromoteOptions? options = null, CancellationToken ct = default)
    {
        options ??= new PromoteOptions();
        var (app, err) = Mutate(appId);
        if (err != null) return StudioResult<EnvironmentPromotion>.Fail(err.Value);
        var baseline = app!.Baseline;
        if (baseline == null) return StudioResult<EnvironmentPromotion>.Fail(StudioErrorCode.InvalidArgument, "No baseline environment.");
        if (string.Equals(toEnv, baseline.EnvironmentId, StringComparison.OrdinalIgnoreCase))
            return StudioResult<EnvironmentPromotion>.Fail(StudioErrorCode.InvalidArgument, "Cannot promote an environment to itself.");

        var ep = new EnvironmentPromotion { AppId = appId, ToEnv = toEnv, FromEnv = baseline.EnvironmentId };

        if (options.Code)
            ep.Code = (await ((IAppDeployWorkflow)new AppDeployWorkflow(_editor)).PromoteCodeAsync(appId, toEnv, ct)).Match(c => c, e => new PromotionResult { AppId = appId, AppName = app.Name, FromEnv = baseline.EnvironmentId, ToEnv = toEnv, Succeeded = false, Message = e.Message });

        if (options.Schema)
            ep.Schema = (await PromoteAsync(appId, toEnv, ct)).Match(s => s, e => new PromotionResult { AppId = appId, AppName = app.Name, FromEnv = baseline.EnvironmentId, ToEnv = toEnv, Succeeded = false, Message = e.Message });

        if (options.Data)
        {
            var dataRes = await ((IAppDataWorkflow)new AppDataWorkflow(_editor)).CopyAsync(appId, baseline.EnvironmentId, toEnv, ct);
            ep.Data = dataRes.Match(d => d, e => null);
        }

        ep.Succeeded = (ep.Code?.Succeeded ?? true) && (ep.Schema?.Succeeded ?? true) && (!options.Data || (ep.Data?.Succeeded ?? false));
        ep.Message = ep.Succeeded ? $"Environment '{toEnv}' promoted." : "One or more promotion tracks failed.";
        return StudioResult<EnvironmentPromotion>.Ok(ep);
    }

    // ── Entity discovery ────────────────────────────────────────────────────

    /// <inheritdoc />
    public Task<StudioResult<AppProject>> DiscoverEntitiesForProjectAsync(string appId, string projectName, CancellationToken ct = default)
    {
        var app = Registry?.GetApp(appId);
        if (app == null) return NotFound<AppProject>("App");
        var project = app.GetProject(projectName);
        if (project == null) return NotFound<AppProject>($"Project '{projectName}'");
        if (string.IsNullOrWhiteSpace(project.AssemblyPath) || !System.IO.File.Exists(project.AssemblyPath))
            return Invalid<AppProject>($"No assembly found for '{projectName}'. Build the project first.");

        try
        {
            var asm = System.Reflection.Assembly.LoadFrom(project.AssemblyPath);
            var discovery = new TheTechIdea.Beep.Editor.EntityDiscovery.EntityDiscoveryService(_editor);
            var opts = new TheTechIdea.Beep.Editor.EntityDiscovery.EntityDiscoveryOptions
            {
                Scope = TheTechIdea.Beep.Editor.EntityDiscovery.DiscoveryScope.Explicit,
                Assemblies = new[] { asm },
                ExcludeAbstract = true,
                ExcludeOpenGenerics = true
            };
            var entities = discovery.Discover(opts);
            if (entities != null)
            {
                project.EntityCount = entities.Count;
                project.ModuleNames = entities.Select(e => e.Namespace?.Split('.').FirstOrDefault() ?? "").Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().ToList();
                Registry?.SaveApp(app);
            }
            return Task.FromResult(StudioResult<AppProject>.Ok(project));
        }
        catch (Exception ex) { return Task.FromResult(StudioResult<AppProject>.Fail(StudioErrorCode.HostNotSupported, ex.Message)); }
    }

    /// <inheritdoc />
    public async Task<StudioResult<List<AppProject>>> DiscoverAllEntitiesAsync(string appId, CancellationToken ct = default)
    {
        var app = Registry?.GetApp(appId);
        if (app == null) return StudioResult<List<AppProject>>.Fail(StudioErrorCode.NotFound, "App not found.");
        var results = new List<AppProject>();
        foreach (var proj in app.Projects.Where(p => p.IsDataProject))
        {
            var res = await DiscoverEntitiesForProjectAsync(appId, proj.Name, ct);
            if (res.IsSuccess) results.Add(res.Value!);
        }
        return StudioResult<List<AppProject>>.Ok(results);
    }

    /// <inheritdoc />
    public async Task<StudioResult<List<AppProject>>> ReScanSolutionAsync(string appId, CancellationToken ct = default)
    {
        var app = Registry?.GetApp(appId);
        if (app == null) return StudioResult<List<AppProject>>.Fail(StudioErrorCode.NotFound, "App not found.");
        if (string.IsNullOrWhiteSpace(app.SolutionPath) || !System.IO.File.Exists(app.SolutionPath))
            return StudioResult<List<AppProject>>.Fail(StudioErrorCode.InvalidArgument, "No solution path on the app. Register it from a .sln first.");

        // Re-discover from the solution and replace all projects
        var slnRes = await ScanAndAddProjectsAsync(appId, app.SolutionPath, ct);
        if (slnRes.IsSuccess)
        {
            var added = slnRes.Value!;
            // Remove projects not in the new scan
            var scannedNames = new HashSet<string>(added.Select(p => p.Name), StringComparer.OrdinalIgnoreCase);
            app.Projects.RemoveAll(p => !scannedNames.Contains(p.Name));
            Registry?.SaveApp(app);
        }
        return slnRes;
    }

    /// <inheritdoc />
    public Task<StudioResult<AppDefinition>> CloneSolutionAsync(string appId, string newName, CancellationToken ct = default)
    {
        var app = Registry?.GetApp(appId);
        if (app == null) return NotFound<AppDefinition>("App");
        if (string.IsNullOrWhiteSpace(newName)) return Invalid<AppDefinition>("New name is required.");
        if (Registry?.GetApp(newName) != null) return Invalid<AppDefinition>($"A solution named '{newName}' already exists.");

        var clone = new AppDefinition
        {
            Name = newName,
            SolutionPath = app.SolutionPath,
            Description = $"Cloned from {app.Name}",
            Color = app.Color,
            Projects = app.Projects.Select(p => new AppProject
            {
                Name = p.Name, Role = p.Role, CsprojPath = p.CsprojPath, AssemblyPath = p.AssemblyPath,
                OutputType = p.OutputType, TargetFramework = p.TargetFramework, RootNamespace = p.RootNamespace,
                IsDataProject = p.IsDataProject, HasDbContext = p.HasDbContext, EntityCount = p.EntityCount,
                ModuleNames = new List<string>(p.ModuleNames), ProjectReferences = new List<string>(p.ProjectReferences)
            }).ToList(),
            Environments = app.Environments.Select(e => new AppEnv
            {
                EnvironmentId = e.EnvironmentId, Tier = e.Tier, DisplayName = e.DisplayName,
                Order = e.Order, Color = e.Color, IsBaseline = e.IsBaseline, IsProduction = e.IsProduction,
                RequiresApproval = e.RequiresApproval, SchemaVersion = null,
                Datasources = new List<AppDataSource>(),
                ProjectBindings = e.ProjectBindings.Select(b => new ProjectEnvBinding { ProjectName = b.ProjectName, EndpointUrl = b.EndpointUrl, DatasourceName = b.DatasourceName, IsEnabled = b.IsEnabled }).ToList()
            }).ToList(),
            ModuleNames = new List<string>(app.ModuleNames)
        };
        var saved = Registry!.SaveApp(clone);
        return saved != null ? Task.FromResult(StudioResult<AppDefinition>.Ok(saved)) : NotFound<AppDefinition>("Could not persist clone.");
    }

    // ── Config generation (runtime bridge: Studio → host .NET app) ──────────

    /// <inheritdoc />
    public Task<StudioResult<AppConfigOutput>> GenerateAppConfigAsync(string appId, string envId, CancellationToken ct = default)
    {
        var app = Registry?.GetApp(appId);
        if (app == null) return NotFound<AppConfigOutput>("App");
        var env = app.GetEnvironment(envId);
        if (env == null) return NotFound<AppConfigOutput>($"Environment '{envId}'");

        var connections = new Dictionary<string, string?>();
        var endpoints = new Dictionary<string, string?>();
        var cfg = new Dictionary<string, object?>
        {
            ["App"] = new { Name = app.Name, Id = app.Id, Environment = env.EnvironmentId, env.Tier }
        };

        // Connection strings per datasource
        foreach (var ds in env.Datasources)
        {
            var key = string.IsNullOrWhiteSpace(ds.ProjectName)
                ? $"ConnectionStrings:{ds.Name}" : $"ConnectionStrings:{ds.ProjectName}_{ds.Name}";
            connections[key] = ds.ConnectionString;
            cfg[key] = ds.ConnectionString;
        }

        // Project endpoints per environment binding
        foreach (var binding in env.ProjectBindings)
        {
            if (!string.IsNullOrWhiteSpace(binding.EndpointUrl))
            {
                var key = $"Endpoints:{binding.ProjectName}";
                endpoints[key] = binding.EndpointUrl;
                cfg[key] = binding.EndpointUrl;
            }
        }

        // Tier-level settings
        var tier = env.Tier?.ToLowerInvariant() switch
        {
            "dev" or "development" => "Development",
            "test" or "testing" => "Test",
            "staging" => "Staging",
            "prod" or "production" or "live" => "Production",
            _ => env.Tier ?? "Development"
        };

        var json = System.Text.Json.JsonSerializer.Serialize(cfg, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        var fileName = $"appsettings.{tier}.json";
        if (env.IsProduction) fileName = "appsettings.Production.json";

        return Task.FromResult(StudioResult<AppConfigOutput>.Ok(new AppConfigOutput
        {
            AppId = appId, EnvId = envId, Json = json, FileName = fileName,
            ConnectionStrings = connections, ProjectEndpoints = endpoints
        }));
    }

    /// <inheritdoc />
    public async Task<StudioResult<IReadOnlyList<AppConfigOutput>>> GenerateAllAppConfigsAsync(string appId, CancellationToken ct = default)
    {
        var app = Registry?.GetApp(appId);
        if (app == null) return StudioResult<IReadOnlyList<AppConfigOutput>>.Fail(StudioErrorCode.NotFound, "App not found.");
        var results = new List<AppConfigOutput>();
        foreach (var env in app.Environments)
        {
            var res = await GenerateAppConfigAsync(appId, env.EnvironmentId, ct);
            if (res.IsSuccess) results.Add(res.Value!);
        }
        return StudioResult<IReadOnlyList<AppConfigOutput>>.Ok(results);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    /// <summary>Resolve the built assembly path for a discovered project.</summary>
    private static string? ResolveAssemblyPath(TheTechIdea.Beep.AppMap.ProjectInfo info)
    {
        try
        {
            var dir = info.ProjectDirectory;
            var tmf = info.TargetFramework ?? "net10.0";
            var ext = (info.OutputType is "Exe" or "WinExe") ? ".exe" : ".dll";
            var path = System.IO.Path.Combine(dir, "bin", "Debug", tmf, info.Name + ext);
            return System.IO.File.Exists(path) ? path : null;
        }
        catch { return null; }
    }

    /// <summary>Fetch the app for an in-place mutation. Returns the app, or an error tuple.</summary>
    private (AppDefinition? app, StudioError? error) Mutate(string appId)
    {
        var registry = Registry;
        if (registry == null)
            return (null, new StudioError(StudioErrorCode.HostNotSupported, "App registry is not available.", null, null));
        var app = registry.GetApp(appId);
        if (app == null)
            return (null, new StudioError(StudioErrorCode.NotFound, $"App '{appId}' not found.", null, null));
        return (app, null);
    }

    private Task<StudioResult<AppDefinition>> Persist(AppDefinition app)
    {
        var saved = Registry?.SaveApp(app);
        return saved == null
            ? NotFound<AppDefinition>("Could not persist the app.")
            : Task.FromResult(StudioResult<AppDefinition>.Ok(saved));
    }

    private static Task<StudioResult<T>> NotFound<T>(string message) =>
        Task.FromResult(StudioResult<T>.Fail(StudioErrorCode.NotFound, message));
    private static Task<StudioResult<T>> Invalid<T>(string message) =>
        Task.FromResult(StudioResult<T>.Fail(StudioErrorCode.InvalidArgument, message));
}
