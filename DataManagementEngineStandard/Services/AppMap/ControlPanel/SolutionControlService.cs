using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.AppMap;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Environments;
using TheTechIdea.Beep.Utilities;
using AppMapModel = TheTechIdea.Beep.AppMap.AppMap;

namespace TheTechIdea.Beep.Services.AppMap.ControlPanel;

/// <summary>
/// Aggregates IAppMapService + IEnvironmentManagementService into
/// a unified solution control panel: snapshots, health checks,
/// environment-wide switching, and dependency maps.
/// </summary>
public sealed class SolutionControlService : ISolutionControlService
{
    private readonly IDMEEditor _editor;
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(5) };

    public SolutionControlService(IDMEEditor editor)
    {
        _editor = editor ?? throw new ArgumentNullException(nameof(editor));
    }

    public Task<SolutionSnapshot> GetSnapshotAsync(AppMapModel appMap, string environmentId, CancellationToken token = default)
    {
        var snapshot = new SolutionSnapshot
        {
            AppMapId = appMap.Id,
            SolutionName = appMap.Solution.Name,
            ActiveEnvironmentId = environmentId,
            DependencyMap = appMap.Solution.Projects.ToDictionary(
                p => p.Name,
                p => p.ProjectReferences ?? new List<string>()),
        };

        var envSvc = _editor.Environment;
        foreach (var proj in appMap.Projects)
        {
            var profile = envSvc?.GetProjectProfile(proj.Project.Name, environmentId);
            if (profile == null) continue;

            foreach (var server in profile.Servers)
            {
                snapshot.Projects.Add(new ProjectRuntimeStatus
                {
                    ProjectName = proj.Project.Name,
                    ServerType = server.ServerType,
                    Url = server.Url,
                    LastCheckedAt = DateTime.UtcNow,
                });
            }

            // Collect DB versions
            if (proj.Project.DataFolders.Any(f => f.HasDbContext))
            {
                var versions = _editor.Version?.GetVersionHistory(proj.Project.Name);
                var latest = versions?.FirstOrDefault();
                if (latest != null)
                    snapshot.DatabaseVersions[proj.Project.Name] = $"v{latest.Major}.{latest.Minor}.{latest.Patch}";
            }
        }

        return Task.FromResult(snapshot);
    }

    public async Task<SolutionSnapshot> HealthCheckAllAsync(AppMapModel appMap, string environmentId, CancellationToken token = default)
    {
        var snapshot = await GetSnapshotAsync(appMap, environmentId, token);

        foreach (var proj in snapshot.Projects)
        {
            token.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(proj.Url)) continue;

            var result = new HealthCheckResult { CheckedAt = DateTime.UtcNow };
            try
            {
                var healthUrl = proj.Url.TrimEnd('/') + "/health";
                var response = await _http.GetAsync(healthUrl, token);
                result.StatusCode = (int)response.StatusCode;
                result.Status = response.IsSuccessStatusCode ? HealthStatus.Healthy : HealthStatus.Unhealthy;
            }
            catch (Exception ex)
            {
                result.Status = HealthStatus.Unhealthy;
                result.ErrorMessage = ex.Message;
            }
            proj.Health = result;
            proj.LastCheckedAt = result.CheckedAt;
        }

        return snapshot;
    }

    public async Task<EnvironmentSwitchPlan> SwitchEnvironmentAsync(AppMapModel appMap, string fromEnv, string toEnv, CancellationToken token = default)
    {
        var plan = new EnvironmentSwitchPlan
        {
            FromEnvironment = fromEnv,
            ToEnvironment = toEnv,
            InvolvesProduction = appMap.Environments.Any(e =>
                e.Id == toEnv && (e.Tier == EnvironmentTier.Production || e.Tier == EnvironmentTier.Live)),
        };

        var envSvc = _editor.Environment;
        if (envSvc == null) return plan;

        foreach (var proj in appMap.Projects)
        {
            var change = new EnvironmentSwitchChange { ProjectName = proj.Project.Name };
            var fromProfile = envSvc.GetProjectProfile(proj.Project.Name, fromEnv);
            var toProfile = envSvc.GetProjectProfile(proj.Project.Name, toEnv);

            if (fromProfile != null && toProfile != null)
            {
                foreach (var fromServer in fromProfile.Servers)
                {
                    var toServer = toProfile.Servers.FirstOrDefault(s => s.ServerType == fromServer.ServerType);
                    change.ServerChanges.Add(new ServerUrlChange
                    {
                        ServerType = fromServer.ServerType.ToString(),
                        OldUrl = fromServer.Url,
                        NewUrl = toServer?.Url ?? fromServer.Url,
                    });
                }
            }
            plan.Changes.Add(change);
        }

        return plan;
    }

    public Task<Dictionary<string, List<string>>> GetDependencyMapAsync(AppMapModel appMap, CancellationToken token = default)
    {
        return Task.FromResult(appMap.Solution.Projects.ToDictionary(
            p => p.Name, p => p.ProjectReferences ?? new List<string>()));
    }
}
