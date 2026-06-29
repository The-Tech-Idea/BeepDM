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
/// Code deployment &amp; promotion — operates on an app's
/// <see cref="ProjectEnvBinding"/> records within each <see cref="AppEnv"/>.
/// </summary>
internal sealed class AppDeployWorkflow : IAppDeployWorkflow
{
    private readonly IDMEEditor _editor;
    public AppDeployWorkflow(IDMEEditor editor) => _editor = editor;

    public Task<StudioResult<ProjectDeployment>> DeployAsync(string appId, string envId, string projectName, string version, CancellationToken ct = default)
    {
        var app = _editor.AppRegistry?.GetApp(appId);
        if (app == null) return NotFound<ProjectDeployment>("App");
        var env = app.GetEnvironment(envId);
        if (env == null) return NotFound<ProjectDeployment>("Environment");
        var project = app.GetProject(projectName);
        if (project == null) return NotFound<ProjectDeployment>("Project");

        var binding = env.ProjectBindings.FirstOrDefault(b => b.ProjectName.Equals(projectName, StringComparison.OrdinalIgnoreCase));
        if (binding == null)
        {
            binding = new ProjectEnvBinding { ProjectName = projectName };
            env.ProjectBindings.Add(binding);
        }
        binding.DeployedVersion = version;
        binding.DeployedAt = DateTimeOffset.UtcNow;
        _editor.AppRegistry?.SaveApp(app);

        var result = new ProjectDeployment { AppId = appId, EnvId = envId, ProjectName = projectName, Version = version, DeployedAt = binding.DeployedAt, EndpointUrl = binding.EndpointUrl, DatasourceName = binding.DatasourceName };
        return Task.FromResult(StudioResult<ProjectDeployment>.Ok(result));
    }

    public Task<StudioResult<PromotionResult>> PromoteCodeAsync(string appId, string toEnv, CancellationToken ct = default)
    {
        var app = _editor.AppRegistry?.GetApp(appId);
        if (app == null) return NotFound<PromotionResult>("App");
        var baseline = app.Baseline;
        if (baseline == null) return Invalid<PromotionResult>("App has no baseline env.");
        var target = app.GetEnvironment(toEnv);
        if (target == null) return NotFound<PromotionResult>("Target environment");
        if (string.Equals(target.EnvironmentId, baseline.EnvironmentId, StringComparison.OrdinalIgnoreCase))
            return Invalid<PromotionResult>("Cannot promote code from an env to itself.");

        int copied = 0;
        foreach (var baselineBinding in baseline.ProjectBindings)
        {
            if (string.IsNullOrWhiteSpace(baselineBinding.DeployedVersion)) continue;
            var targetBinding = target.ProjectBindings.FirstOrDefault(b => b.ProjectName.Equals(baselineBinding.ProjectName, StringComparison.OrdinalIgnoreCase));
            if (targetBinding == null)
            {
                targetBinding = new ProjectEnvBinding { ProjectName = baselineBinding.ProjectName, EndpointUrl = baselineBinding.EndpointUrl, DatasourceName = baselineBinding.DatasourceName };
                target.ProjectBindings.Add(targetBinding);
            }
            targetBinding.DeployedVersion = baselineBinding.DeployedVersion;
            targetBinding.DeployedAt = DateTimeOffset.UtcNow;
            copied++;
        }
        _editor.AppRegistry?.SaveApp(app);
        var result = new PromotionResult { AppId = appId, AppName = app.Name, FromEnv = baseline.EnvironmentId, ToEnv = toEnv, Succeeded = true, Message = $"Code promoted for {copied} project(s): {baseline.EnvironmentId} → {toEnv}." };
        return Task.FromResult(StudioResult<PromotionResult>.Ok(result));
    }

    public Task<StudioResult<IReadOnlyList<ProjectDeployment>>> GetDeploymentsAsync(string appId, string? envId = null, CancellationToken ct = default)
    {
        var app = _editor.AppRegistry?.GetApp(appId);
        if (app == null) return Task.FromResult(StudioResult<IReadOnlyList<ProjectDeployment>>.Ok(Array.Empty<ProjectDeployment>()));
        var envs = envId != null ? new[] { app.GetEnvironment(envId) }.Where(e => e != null).ToList() : app.Environments;
        var list = envs.SelectMany(e => e.ProjectBindings.Select(b => new ProjectDeployment { AppId = appId, EnvId = e!.EnvironmentId, ProjectName = b.ProjectName, Version = b.DeployedVersion, DeployedAt = b.DeployedAt, EndpointUrl = b.EndpointUrl, DatasourceName = b.DatasourceName })).ToList();
        return Task.FromResult(StudioResult<IReadOnlyList<ProjectDeployment>>.Ok(list));
    }

    public Task<StudioResult<bool>> ConfigureBindingAsync(string appId, string envId, string projectName, string? endpointUrl, string? datasourceName, CancellationToken ct = default)
    {
        var app = _editor.AppRegistry?.GetApp(appId);
        if (app == null) return Task.FromResult(StudioResult<bool>.Fail(StudioErrorCode.NotFound, "App not found."));
        var env = app.GetEnvironment(envId);
        if (env == null) return Task.FromResult(StudioResult<bool>.Fail(StudioErrorCode.NotFound, "Environment not found."));
        var binding = env.ProjectBindings.FirstOrDefault(b => b.ProjectName.Equals(projectName, StringComparison.OrdinalIgnoreCase));
        if (binding == null) { binding = new ProjectEnvBinding { ProjectName = projectName }; env.ProjectBindings.Add(binding); }
        if (endpointUrl != null) binding.EndpointUrl = endpointUrl;
        if (datasourceName != null) binding.DatasourceName = datasourceName;
        _editor.AppRegistry?.SaveApp(app);
        return Task.FromResult(StudioResult<bool>.Ok(true));
    }

    private static Task<StudioResult<T>> NotFound<T>(string what) => Task.FromResult(StudioResult<T>.Fail(StudioErrorCode.NotFound, $"{what} not found."));
    private static Task<StudioResult<T>> Invalid<T>(string msg) => Task.FromResult(StudioResult<T>.Fail(StudioErrorCode.InvalidArgument, msg));
}
