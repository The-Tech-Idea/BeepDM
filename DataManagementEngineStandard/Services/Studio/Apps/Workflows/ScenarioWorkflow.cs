using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.AppMap;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Studio.Apps.Workflows;

namespace TheTechIdea.Beep.Studio.Apps;

/// <summary>
/// Composes the micro-workflows into the two canonical Studio scenarios.
/// </summary>
internal sealed class ScenarioWorkflow : IScenarioWorkflow
{
    private readonly IDMEEditor _editor;
    public ScenarioWorkflow(IDMEEditor editor) => _editor = editor;

    public async Task<StudioResult<SoloDevResult>> RunSoloDevAsync(SoloDevRequest request, CancellationToken ct)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.AppName))
            return StudioResult<SoloDevResult>.Fail(StudioErrorCode.InvalidArgument, "AppName is required.");
        if (request.Datasources == null || request.Datasources.Count == 0)
            return StudioResult<SoloDevResult>.Fail(StudioErrorCode.InvalidArgument, "At least one datasource spec is required.");

        var registry = _editor.AppRegistry;
        if (registry == null) return StudioResult<SoloDevResult>.Fail(StudioErrorCode.HostNotSupported, "App registry unavailable.");

        try
        {
            // 1. Register app (dev-only)
            var app = registry.RegisterApp(new AppDefinition { Name = request.AppName });
            // Keep only the dev env
            var devEnv = app.Environments.FirstOrDefault(e => e.Tier == "dev") ?? app.Environments.FirstOrDefault() ?? new AppEnv { EnvironmentId = "dev", Tier = "dev", IsBaseline = true };
            app.Environments.Clear();
            app.Environments.Add(devEnv);
            var baselineName = devEnv.EnvironmentId;

            var dsNames = new List<string>();
            var mgmt = new TheTechIdea.Beep.Services.DatasourceManagement.DatasourceManagementService(_editor);

            // 2. Provision each datasource + bind to baseline
            foreach (var spec in request.Datasources)
            {
                var dsName = string.IsNullOrWhiteSpace(spec.Name) ? $"{request.AppName}-{Guid.NewGuid():N}"[..12] : spec.Name;
                var connStr = spec.ConnectionString ?? DefaultLocalConnectionString(spec.Type, request.AppName);
                var dt = Enum.TryParse<TheTechIdea.Beep.Utilities.DataSourceType>(spec.Type, true, out var t) ? t : TheTechIdea.Beep.Utilities.DataSourceType.SqlLite;
                mgmt.AddDatasource(new ConnectionProperties { ConnectionName = dsName, DatabaseType = dt, ConnectionString = connStr });
                registry.SetDatasource(app.Id, baselineName, new AppDataSource { Name = dsName, ConnectionString = connStr, Type = spec.Type, IsPrimary = spec.IsPrimary });
                dsNames.Add(dsName);
            }
            mgmt.SaveConfiguration();

            // 3. Migrate each datasource in the baseline env
            bool schemaApplied = false;
            var migration = new AppMigrationWorkflow(_editor);
            foreach (var dsName in dsNames)
            {
                var mr = await migration.MigrateAsync(app.Id, baselineName, new MigrationOptions { DatasourceName = dsName }, ct);
                if (mr.IsSuccess && mr.Value!.Succeeded) schemaApplied = true;
            }

            // 4. Seed
            bool seeded = false;
            if (request.Seed && !string.IsNullOrWhiteSpace(request.SeedSource))
            {
                var seedOk = System.IO.File.Exists(request.SeedSource) || System.IO.Directory.Exists(request.SeedSource);
                seeded = seedOk;
            }

            return StudioResult<SoloDevResult>.Ok(new SoloDevResult
            {
                AppId = app.Id, BaselineEnvId = baselineName, DatasourceNames = dsNames,
                SchemaApplied = schemaApplied, Seeded = seeded,
                Message = $"Ready: {dsNames.Count} datasource(s) provisioned" + (schemaApplied ? ", schema applied." : ".")
            });
        }
        catch (Exception ex) { return StudioResult<SoloDevResult>.Fail(StudioErrorCode.HostNotSupported, ex.Message); }
    }

    public async Task<StudioResult<EnterpriseResult>> RunEnterpriseAsync(EnterpriseRequest request, CancellationToken ct)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.AppName))
            return StudioResult<EnterpriseResult>.Fail(StudioErrorCode.InvalidArgument, "AppName is required.");

        var registry = _editor.AppRegistry;
        if (registry == null) return StudioResult<EnterpriseResult>.Fail(StudioErrorCode.HostNotSupported, "App registry unavailable.");

        try
        {
            // 1. Register app
            var app = registry.RegisterApp(new AppDefinition { Name = request.AppName });

            // 2. Register from solution (populate projects)
            int projectCount = 0;
            if (!string.IsNullOrWhiteSpace(request.SolutionPath) && System.IO.File.Exists(request.SolutionPath))
            {
                var facade = new AppStudioService(_editor);
                var slnRes = await facade.RegisterFromSolutionAsync(request.AppName, request.SolutionPath, ct);
                if (slnRes.IsSuccess) { app = slnRes.Value!; projectCount = app.ProjectCount; }
            }

            // 3. Replace the seeded environments with the enterprise env layout
            app.Environments.Clear();
            var envIds = new List<string>();
            foreach (var eSpec in request.Environments)
            {
                app.Environments.Add(new AppEnv
                {
                    EnvironmentId = eSpec.EnvironmentId, Tier = eSpec.Tier,
                    IsBaseline = eSpec.IsBaseline, IsProduction = eSpec.IsProduction,
                    RequiresApproval = eSpec.RequiresApproval,
                    Order = request.Environments.IndexOf(eSpec)
                });
                envIds.Add(eSpec.EnvironmentId);
            }
            registry.SaveApp(app);
            var baseline = app.Environments.FirstOrDefault(e => e.IsBaseline) ?? app.Environments.FirstOrDefault();
            var baselineName = baseline?.EnvironmentId ?? "dev";

            var mgmt = new TheTechIdea.Beep.Services.DatasourceManagement.DatasourceManagementService(_editor);
            var migration = new AppMigrationWorkflow(_editor);
            var deploy = new AppDeployWorkflow(_editor);
            var cicd = new AppCicdWorkflow(_editor);

            // 4. Create datasources per env
            foreach (var eSpec in request.Environments)
            {
                foreach (var dsSpec in eSpec.Datasources)
                {
                    var dsName = string.IsNullOrWhiteSpace(dsSpec.Name) ? $"{request.AppName}-{eSpec.EnvironmentId}" : dsSpec.Name;
                    var dt = Enum.TryParse<TheTechIdea.Beep.Utilities.DataSourceType>(dsSpec.Type, true, out var t) ? t : TheTechIdea.Beep.Utilities.DataSourceType.SqlLite;
                    var connStr = dsSpec.ConnectionString ?? DefaultLocalConnectionString(dsSpec.Type, $"{request.AppName}-{eSpec.EnvironmentId}");
                    if (!mgmt.DatasourceExists(dsName))
                    {
                        mgmt.AddDatasource(new ConnectionProperties { ConnectionName = dsName, DatabaseType = dt, ConnectionString = connStr });
                    }
                    registry.SetDatasource(app.Id, eSpec.EnvironmentId, new AppDataSource { Name = dsName, ConnectionString = connStr, Type = dsSpec.Type, IsPrimary = dsSpec.IsPrimary });
                }
            }
            mgmt.SaveConfiguration();
            registry.SaveApp(app);

            // 5. Migrate baseline
            bool schemaApplied = false;
            if (baselineName != null)
            {
                var mr = await migration.MigrateAsync(app.Id, baselineName, ct: ct);
                schemaApplied = mr.IsSuccess && mr.Value!.Succeeded;
            }

            // 6. RBAC
            bool rbacSetup = false;
            if (!string.IsNullOrWhiteSpace(request.InitialAdminUser))
            {
                var gov = new AppGovernanceWorkflow(_editor);
                var roleRes = await gov.AssignRoleAsync(app.Id, request.InitialAdminUser, AppMemberRole.Admin, ct);
                rbacSetup = roleRes.IsSuccess;
            }

            // 7. Deploy code to baseline
            bool codeDeployed = false;
            if (baselineName != null && app.Projects.Any(p => p.IsDataProject))
            {
                var dp = app.Projects.First(p => p.IsDataProject);
                var depRes = await deploy.DeployAsync(app.Id, baselineName, dp.Name, "1.0.0", ct);
                codeDeployed = depRes.IsSuccess;
            }

            // 8. CI/CD pipeline
            bool pipelineGenerated = false;
            string? pipelineYaml = null;
            var provider = request.CicdProvider ?? (request.SolutionPath != null ? CicdProvider.GitHubActions : (CicdProvider?)null);
            if (provider != null)
            {
                var pipeRes = await cicd.GeneratePipelineAsync(app.Id, provider.Value, ct);
                pipelineGenerated = pipeRes.IsSuccess;
                pipelineYaml = pipeRes.Value?.Yaml;
            }

            // 9. Audit
            var govAudit = new AppGovernanceWorkflow(_editor);
            await govAudit.RecordAuditAsync(app.Id, baselineName ?? "system", "enterprise-setup", request.InitialAdminUser ?? "system", "Enterprise scenario complete", ct);

            return StudioResult<EnterpriseResult>.Ok(new EnterpriseResult
            {
                AppId = app.Id, AppName = app.Name, ProjectCount = projectCount,
                EnvironmentIds = envIds, SchemaApplied = schemaApplied, CodeDeployed = codeDeployed,
                PipelineGenerated = pipelineGenerated, PipelineYaml = pipelineYaml,
                Message = $"Enterprise setup complete. {envIds.Count} env(s), {projectCount} project(s)" + (rbacSetup ? ", RBAC set" : "")
            });
        }
        catch (Exception ex) { return StudioResult<EnterpriseResult>.Fail(StudioErrorCode.HostNotSupported, ex.Message); }
    }

    private static string DefaultLocalConnectionString(string dsType, string appName)
    {
        var safe = string.Concat(appName.Where(char.IsLetterOrDigit));
        return dsType.Equals("SqlServer", StringComparison.OrdinalIgnoreCase)
            ? $"Server=(localdb)\\MSSQLLocalDB;Database={safe};Trusted_Connection=True;TrustServerCertificate=True;"
            : $"Data Source={safe}.db";
    }
}
