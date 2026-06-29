using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.AppMap;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Studio.Apps.Workflows;

namespace TheTechIdea.Beep.Studio.Apps;

/// <summary>
/// CI/CD + deploy-to-env: generate a migration-aware pipeline (rendered YAML),
/// run migrate-on-deploy against an env, and stand up disposable preview
/// databases for pull-request validation. Preview DBs are created as local
/// datasources named <c>{appId}-pr-{prId}</c>.
/// </summary>
internal sealed class AppCicdWorkflow : IAppCicdWorkflow
{
    private readonly IDMEEditor _editor;
    public AppCicdWorkflow(IDMEEditor editor) => _editor = editor;

    public Task<StudioResult<PipelineDescriptor>> GeneratePipelineAsync(string appId, CicdProvider provider, CancellationToken ct = default)
    {
        var app = _editor.AppRegistry?.GetApp(appId);
        if (app == null) return Task.FromResult(StudioResult<PipelineDescriptor>.Fail(StudioErrorCode.NotFound, $"App '{appId}' not found."));

        var envIds = app.Environments.OrderBy(e => e.Order).Select(e => e.EnvironmentId).ToList();
        var stages = new List<string> { "build", "test" };
        foreach (var env in envIds) stages.Add($"migrate:{env}");

        var yaml = new StringBuilder();
        if (provider == CicdProvider.GitHubActions)
        {
            yaml.AppendLine("name: " + Safe(app.Name));
            yaml.AppendLine("on: [push, pull_request]");
            yaml.AppendLine("jobs:");
            yaml.AppendLine("  migrate:");
            yaml.AppendLine("    runs-on: ubuntu-latest");
            yaml.AppendLine("    steps:");
            yaml.AppendLine("      - uses: actions/checkout@v4");
            yaml.AppendLine("      - name: Install Beep toolchain");
            yaml.AppendLine("        run: dotnet tool install --global TheTechIdea.Beep");
            foreach (var env in envIds)
            {
                yaml.AppendLine($"      - name: Migrate {env}");
                yaml.AppendLine($"        run: beep migrate --app \"{app.Name}\" --env {env}");
                yaml.AppendLine($"        env:");
                yaml.AppendLine($"          BEEP_{env.ToUpper()}_CONN: ${{{{ secrets.BEEP_{env.ToUpper()}_CONN }}}}");
            }
        }
        else if (provider == CicdProvider.AzureDevOps)
        {
            yaml.AppendLine("trigger: [main, develop]");
            yaml.AppendLine("stages:");
            foreach (var env in envIds)
            {
                yaml.AppendLine($"- stage: Migrate_{env}");
                yaml.AppendLine("  jobs:");
                yaml.AppendLine("  - job: Migrate");
                yaml.AppendLine("    steps:");
                yaml.AppendLine($"    - script: beep migrate --app \"{app.Name}\" --env {env}");
                yaml.AppendLine("      displayName: 'Migrate " + env + "'");
            }
        }
        else
        {
            yaml.AppendLine("# Generic pipeline — migrate each environment in order:");
            foreach (var env in envIds) yaml.AppendLine($"beep migrate --app \"{app.Name}\" --env {env}");
        }

        return Task.FromResult(StudioResult<PipelineDescriptor>.Ok(new PipelineDescriptor
        {
            AppId = appId, Provider = provider, Yaml = yaml.ToString(), Stages = stages
        }));
    }

    public async Task<StudioResult<EnvMigrationReport>> MigrateOnDeployAsync(string appId, string envId, CancellationToken ct = default)
        => await new AppMigrationWorkflow(_editor).MigrateAsync(appId, envId, null, ct);

    public async Task<StudioResult<PrPreviewResult>> CreatePreviewDatabaseAsync(string appId, string prId, CancellationToken ct = default)
    {
        var app = _editor.AppRegistry?.GetApp(appId);
        if (app == null) return StudioResult<PrPreviewResult>.Fail(StudioErrorCode.NotFound, $"App '{appId}' not found.");
        var baseline = app.Baseline;
        if (baseline?.Datasources.Any() != true)
            return StudioResult<PrPreviewResult>.Fail(StudioErrorCode.InvalidArgument, "Baseline environment has no datasource to clone.");

        var previewName = $"{app.Id}-pr-{Safe(prId)}";
        var connStr = $"Data Source={previewName}.db";
        try
        {
            var mgmt = new TheTechIdea.Beep.Services.DatasourceManagement.DatasourceManagementService(_editor);
            if (!mgmt.DatasourceExists(previewName))
            {
                mgmt.AddDatasource(new TheTechIdea.Beep.ConfigUtil.ConnectionProperties
                {
                    ConnectionName = previewName,
                    DatabaseType = TheTechIdea.Beep.Utilities.DataSourceType.SqlLite,
                    ConnectionString = connStr
                });
                mgmt.SaveConfiguration();
            }
            // Bind a transient app×preview env so the migration workflow targets it.
            var previewEnv = $"{prId}-preview";
            _editor.AppRegistry?.AddEnvironment(app.Id, new AppEnv { EnvironmentId = previewEnv, Tier = "dev", Order = 99 });
            _editor.AppRegistry?.SetDatasource(app.Id, previewEnv, new AppDataSource { Name = previewName, ConnectionString = connStr, Type = "SqlLite", IsPrimary = true });

            var migrated = await new AppMigrationWorkflow(_editor).MigrateAsync(app.Id, previewEnv, null, ct);
            return StudioResult<PrPreviewResult>.Ok(new PrPreviewResult
            {
                AppId = appId, PrId = prId, PreviewDatasourceName = previewName, ConnectionString = connStr,
                SchemaApplied = migrated.IsSuccess, CreatedAt = DateTimeOffset.UtcNow
            });
        }
        catch (Exception ex) { return StudioResult<PrPreviewResult>.Fail(StudioErrorCode.HostNotSupported, ex.Message); }
    }

    public Task<StudioResult<bool>> DropPreviewDatabaseAsync(string appId, string prId, CancellationToken ct = default)
    {
        try
        {
            var previewName = $"{appId}-pr-{Safe(prId)}";
            var mgmt = new TheTechIdea.Beep.Services.DatasourceManagement.DatasourceManagementService(_editor);
            if (mgmt.DatasourceExists(previewName)) { mgmt.RemoveDatasource(previewName); mgmt.SaveConfiguration(); }
            // Remove the preview environment
            var app = _editor.AppRegistry?.GetApp(appId);
            var previewEnv = $"{prId}-preview";
            app?.Environments.RemoveAll(e => e.EnvironmentId.Equals(previewEnv, StringComparison.OrdinalIgnoreCase));
            _editor.AppRegistry?.SaveApp(app);
            return Task.FromResult(StudioResult<bool>.Ok(true));
        }
        catch (Exception ex) { return Task.FromResult(StudioResult<bool>.Fail(StudioErrorCode.HostNotSupported, ex.Message)); }
    }

    public Task<StudioResult<IReadOnlyList<PrPreviewResult>>> ListPreviewsAsync(string appId, CancellationToken ct = default)
    {
        try
        {
            var mgmt = new TheTechIdea.Beep.Services.DatasourceManagement.DatasourceManagementService(_editor);
            var prefix = $"{appId}-pr-";
            var previews = mgmt.GetAllDatasources()
                .Where(c => c.ConnectionName.StartsWith(prefix, StringComparison.Ordinal))
                .Select(c => new PrPreviewResult
                {
                    AppId = appId,
                    PrId = c.ConnectionName.Substring(prefix.Length),
                    PreviewDatasourceName = c.ConnectionName,
                    ConnectionString = c.ConnectionString,
                    SchemaApplied = true
                }).ToList();
            return Task.FromResult(StudioResult<IReadOnlyList<PrPreviewResult>>.Ok(previews));
        }
        catch (Exception ex) { return Task.FromResult(StudioResult<IReadOnlyList<PrPreviewResult>>.Fail(StudioErrorCode.HostNotSupported, ex.Message)); }
    }

    private static string Safe(string? s) => new string((s ?? "").Where(char.IsLetterOrDigit).ToArray());
}
