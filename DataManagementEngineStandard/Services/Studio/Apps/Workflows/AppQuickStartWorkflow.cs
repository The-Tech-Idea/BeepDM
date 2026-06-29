using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using TheTechIdea.Beep.AppMap;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Studio.Apps.Workflows;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Studio.Apps;

/// <summary>
/// Solo-developer quick start: register an app, provision a local datasource,
/// bind it to the baseline env, and apply schema in one call. Ships a small
/// template catalog.
/// </summary>
internal sealed class AppQuickStartWorkflow : IAppQuickStartWorkflow
{
    private readonly IDMEEditor _editor;
    public AppQuickStartWorkflow(IDMEEditor editor) => _editor = editor;

    private static readonly List<AppTemplate> Templates = new()
    {
        new() { Id = "blank", Name = "Blank", Description = "No entities. Add your own." },
        new() { Id = "web", Name = "Web App", DefaultDatasourceType = "SqlLite", Description = "Typical web app data model." },
        new() { Id = "microservice", Name = "Microservice", DefaultDatasourceType = "SqlLite", Description = "Service-bound schema." },
        new() { Id = "warehouse", Name = "Data Warehouse", DefaultDatasourceType = "SqlServer", Description = "Analytical schema." },
    };

    public Task<StudioResult<IReadOnlyList<AppTemplate>>> ListTemplatesAsync(CancellationToken ct = default)
        => Task.FromResult(StudioResult<IReadOnlyList<AppTemplate>>.Ok(Templates));

    public async Task<StudioResult<QuickStartResult>> StartAsync(QuickStartRequest request, CancellationToken ct = default)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.AppName))
            return StudioResult<QuickStartResult>.Fail(StudioErrorCode.InvalidArgument, "AppName is required.");

        var registry = _editor.AppRegistry;
        if (registry == null) return StudioResult<QuickStartResult>.Fail(StudioErrorCode.HostNotSupported, "App registry unavailable.");

        var app = registry.RegisterApp(new AppDefinition { Name = request.AppName });
        var baseline = app.Baseline ?? app.Environments.FirstOrDefault();
        if (baseline == null) return StudioResult<QuickStartResult>.Fail(StudioErrorCode.InvalidArgument, "App has no baseline environment.");

        var dsType = request.DatasourceType ?? Templates.FirstOrDefault(t => t.Id == request.TemplateId)?.DefaultDatasourceType ?? "SqlLite";
        var dsName = $"{request.AppName}-dev";
        var connStr = request.ConnectionString ?? DefaultLocalConnectionString(dsType, request.AppName);

        try
        {
            var mgmt = new TheTechIdea.Beep.Services.DatasourceManagement.DatasourceManagementService(_editor);
            var conn = new ConnectionProperties
            {
                ConnectionName = dsName,
                DatabaseType = ParseDataSourceType(dsType),
                ConnectionString = connStr,
                FilePath = connStr
            };
            mgmt.AddDatasource(conn);
            mgmt.SaveConfiguration();

            registry.SetDatasource(app.Id, baseline.EnvironmentId, new AppDataSource { Name = dsName, ConnectionString = connStr, Type = dsType, IsPrimary = true });

            bool applied = false, seeded = false;
            var migration = new AppMigrationWorkflow(_editor);
            var template = Templates.FirstOrDefault(t => t.Id == request.TemplateId);
            if (template is { EntityTypeNames.Count: > 0 })
            {
                var res = await migration.MigrateAsync(app.Id, baseline.EnvironmentId, new MigrationOptions { EntityTypeNames = template.EntityTypeNames }, ct);
                applied = res.IsSuccess;
            }

            if (request.Seed && !string.IsNullOrWhiteSpace(request.SeedSource))
            {
                var seedRes = await SeedAsync(app.Id, baseline.EnvironmentId, request.SeedSource, ct);
                seeded = seedRes.IsSuccess;
            }

            return StudioResult<QuickStartResult>.Ok(new QuickStartResult
            {
                AppId = app.Id,
                BaselineEnvId = baseline.EnvironmentId,
                DatasourceName = dsName,
                SchemaApplied = applied,
                Seeded = seeded,
                Message = applied ? "App ready: schema applied." : "App created. Bind entities to apply schema."
            });
        }
        catch (Exception ex)
        {
            return StudioResult<QuickStartResult>.Fail(StudioErrorCode.HostNotSupported, ex.Message);
        }
    }

    public async Task<StudioResult<bool>> SeedAsync(string appId, string envId, string seedSource, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(seedSource))
            return StudioResult<bool>.Fail(StudioErrorCode.InvalidArgument, "seedSource is required.");
        if (!File.Exists(seedSource))
            return StudioResult<bool>.Fail(StudioErrorCode.NotFound, $"Seed source '{seedSource}' not found.");

        try
        {
            var app = _editor.AppRegistry?.GetApp(appId);
            if (app == null) return StudioResult<bool>.Fail(StudioErrorCode.NotFound, $"App '{appId}' not found.");
            var env = app.GetEnvironment(envId);
            if (env == null) return StudioResult<bool>.Fail(StudioErrorCode.NotFound, $"Environment '{envId}' not found.");
            var primary = env.Datasources.FirstOrDefault(d => d.IsPrimary) ?? env.Datasources.FirstOrDefault();
            if (primary == null) return StudioResult<bool>.Fail(StudioErrorCode.InvalidArgument, "No datasource to seed.");

            var ds = _editor.GetDataSource(primary.Name);
            if (ds == null) return StudioResult<bool>.Fail(StudioErrorCode.NotFound, $"Datasource '{primary.Name}' not registered.");

            var json = await File.ReadAllTextAsync(seedSource, ct);
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            int seeded = 0;
            foreach (var entityProp in doc.RootElement.EnumerateObject())
            {
                var entityName = entityProp.Name;
                if (entityProp.Value.ValueKind != System.Text.Json.JsonValueKind.Array) continue;
                foreach (var row in entityProp.Value.EnumerateArray())
                {
                    var obj = System.Text.Json.JsonSerializer.Deserialize<object>(row.GetRawText());
                    if (obj != null) { ds.InsertEntity(entityName, obj); seeded++; }
                }
            }
            return StudioResult<bool>.Ok(true);
        }
        catch (Exception ex) { return StudioResult<bool>.Fail(StudioErrorCode.HostNotSupported, ex.Message); }
    }

    private static string DefaultLocalConnectionString(string dsType, string appName)
    {
        var safe = string.Concat(appName.Where(char.IsLetterOrDigit));
        return dsType.Equals("SqlServer", StringComparison.OrdinalIgnoreCase)
            ? $"Server=(localdb)\\MSSQLLocalDB;Database={safe};Trusted_Connection=True;TrustServerCertificate=True;"
            : $"Data Source={safe}.db";
    }

    private static DataSourceType ParseDataSourceType(string s) =>
        Enum.TryParse<DataSourceType>(s, true, out var t) ? t : DataSourceType.SqlLite;
}
