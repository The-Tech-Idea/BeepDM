using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.AppMap;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Studio.Apps.Workflows;
using TheTechIdea.Beep.Studio.Migration.Ledger;

namespace TheTechIdea.Beep.Studio.Apps;

/// <summary>
/// App-scoped schema migration. Resolves an app×env's primary datasource, gathers
/// the app's entity types, and delegates to the engine's
/// <c>MigrationTrackingService</c> / <c>DatasourceManagementService</c>.
/// </summary>
internal sealed class AppMigrationWorkflow : IAppMigrationWorkflow
{
    private readonly IDMEEditor _editor;
    private readonly TheTechIdea.Beep.Studio.Migration.Ledger.IMigrationLedger? _ledger;
    public AppMigrationWorkflow(IDMEEditor editor, TheTechIdea.Beep.Studio.Migration.Ledger.IMigrationLedger? ledger = null)
    { _editor = editor; _ledger = ledger; }

    public Task<StudioResult<EnvMigrationReport>> MigrateAsync(string appId, string envId, MigrationOptions? options = null, CancellationToken ct = default)
        => RunMigrationAsync(appId, envId, dryRun: false, options, ct);

    public Task<StudioResult<EnvMigrationReport>> DryRunAsync(string appId, string envId, CancellationToken ct = default)
        => RunMigrationAsync(appId, envId, dryRun: true, null, ct);

    public async Task<StudioResult<bool>> RollbackAsync(string appId, string envId, string? datasourceName = null, CancellationToken ct = default)
    {
        var ds = ResolveDatasource(appId, envId, datasourceName);
        if (ds.Error is { } err) return StudioResult<bool>.Fail(err);
        try
        {
            var tracking = new TheTechIdea.Beep.Editor.Migration.MigrationTrackingService(_editor);
            var ok = tracking.UndoLastMigration(ds.Name);
            return StudioResult<bool>.Ok(ok?.Flag == Errors.Ok);
        }
        catch (Exception ex) { return StudioResult<bool>.Fail(StudioErrorCode.HostNotSupported, ex.Message); }
    }

    public Task<StudioResult<IReadOnlyList<EnvMigrationHistoryItem>>> GetHistoryAsync(string appId, string envId, string? datasourceName = null, CancellationToken ct = default)
    {
        var ds = ResolveDatasource(appId, envId, datasourceName);
        if (ds.Error is { } err) return Task.FromResult(StudioResult<IReadOnlyList<EnvMigrationHistoryItem>>.Fail(err));
        try
        {
            var history = new TheTechIdea.Beep.Editor.Migration.MigrationTrackingService(_editor).GetMigrationHistory(ds.Name);
            var items = (history?.Migrations ?? new List<MigrationRecord>()).Select(h => new EnvMigrationHistoryItem
            {
                DatasourceName = ds.Name,
                Version = h.MigrationId ?? h.Name ?? string.Empty,
                AppliedAt = h.AppliedOnUtc,
                Status = h.Success ? "Applied" : "Failed"
            }).ToList();
            return Task.FromResult(StudioResult<IReadOnlyList<EnvMigrationHistoryItem>>.Ok(items));
        }
        catch (Exception ex) { return Task.FromResult(StudioResult<IReadOnlyList<EnvMigrationHistoryItem>>.Fail(StudioErrorCode.HostNotSupported, ex.Message)); }
    }

    public async Task<StudioResult<SchemaCompareResult>> CompareEnvironmentsAsync(string appId, string sourceEnv, string targetEnv, string? datasourceName = null, CancellationToken ct = default)
    {
        // When a specific datasource is named, compare only that one across envs.
        // When null, iterate ALL datasource pairs matched by ProjectName (multi-datasource).
        var app = _editor.AppRegistry?.GetApp(appId);
        if (app == null) return StudioResult<SchemaCompareResult>.Fail(StudioErrorCode.NotFound, $"App '{appId}' not found.");
        var srcEnv = app.GetEnvironment(sourceEnv);
        var tgtEnv = app.GetEnvironment(targetEnv);
        if (srcEnv == null || tgtEnv == null) return StudioResult<SchemaCompareResult>.Fail(StudioErrorCode.InvalidArgument, "Both environments must exist.");

        var result = new SchemaCompareResult { AppId = appId, SourceEnv = sourceEnv, TargetEnv = targetEnv };

        if (!string.IsNullOrWhiteSpace(datasourceName))
        {
            var src = srcEnv.Datasources.FirstOrDefault(d => d.Name.Equals(datasourceName, StringComparison.OrdinalIgnoreCase));
            var tgt = tgtEnv.Datasources.FirstOrDefault(d => d.Name.Equals(datasourceName, StringComparison.OrdinalIgnoreCase));
            if (src == null || tgt == null) return StudioResult<SchemaCompareResult>.Fail(StudioErrorCode.NotFound, $"Datasource '{datasourceName}' not found in both envs.");
            return await CompareOneAsync(src.Name, tgt.Name, app, result, ct);
        }

        // Multi-datasource: pair by ProjectName
        var types = ResolveEntityTypes(app, null);
        foreach (var srcDs in srcEnv.Datasources)
        {
            var tgtDs = string.IsNullOrWhiteSpace(srcDs.ProjectName)
                ? tgtEnv.Datasources.FirstOrDefault()
                : tgtEnv.Datasources.FirstOrDefault(d => string.Equals(d.ProjectName, srcDs.ProjectName, StringComparison.OrdinalIgnoreCase));
            if (tgtDs == null) { result.MissingInTarget.Add(srcDs.Name); continue; }
            var pairResult = await CompareOneAsync(srcDs.Name, tgtDs.Name, app, null, ct);
            foreach (var diff in pairResult.Different) result.Different.Add($"{srcDs.Name}: {diff}");
            foreach (var diff in pairResult.MissingInTarget) result.MissingInTarget.Add($"{srcDs.Name}: {diff}");
        }
        result.AreEqual = result.Different.Count == 0 && result.MissingInTarget.Count == 0 && result.MissingInSource.Count == 0;
        return StudioResult<SchemaCompareResult>.Ok(result);
    }

    private async Task<SchemaCompareResult> CompareOneAsync(string srcDsName, string tgtDsName, AppDefinition app, SchemaCompareResult? sharedResult, CancellationToken ct)
    {
        var result = sharedResult ?? new SchemaCompareResult { AppId = app.Id, SourceEnv = srcDsName, TargetEnv = tgtDsName };
        var types = ResolveEntityTypes(app, null);
        if (types.Count == 0) { result.AreEqual = true; return result; }
        try
        {
            var mgmt = new TheTechIdea.Beep.Services.DatasourceManagement.DatasourceManagementService(_editor);
            var drift = await mgmt.InspectSchemaAsync(tgtDsName, types, ct);
            if (drift == null || drift.Count == 0) { result.AreEqual = true; return result; }
            foreach (var kv in drift)
                if (kv.Value != null) result.Different.Add(kv.Key);
            result.AreEqual = result.Different.Count == 0;
            return result;
        }
        catch (Exception ex) { result.AreEqual = false; result.Different.Add(ex.Message); return result; }
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private async Task<StudioResult<EnvMigrationReport>> RunMigrationAsync(string appId, string envId, bool dryRun, MigrationOptions? options, CancellationToken ct)
    {
        var app = _editor.AppRegistry?.GetApp(appId);
        if (app == null) return StudioResult<EnvMigrationReport>.Fail(StudioErrorCode.NotFound, $"App '{appId}' not found.");
        var env = app.GetEnvironment(envId);
        if (env == null) return StudioResult<EnvMigrationReport>.Fail(StudioErrorCode.NotFound, $"Environment '{envId}' not found.");

        // Resolve which datasources to migrate: named one, or every datasource in the env.
        var targets = new List<AppDataSource>();
        if (!string.IsNullOrWhiteSpace(options?.DatasourceName))
        {
            var named = env.Datasources.FirstOrDefault(d => d.Name.Equals(options.DatasourceName, StringComparison.OrdinalIgnoreCase));
            if (named == null) return StudioResult<EnvMigrationReport>.Fail(StudioErrorCode.NotFound, $"Datasource '{options.DatasourceName}' not found on '{envId}'.");
            targets.Add(named);
        }
        else
        {
            targets = env.Datasources.ToList();
        }
        if (targets.Count == 0) return StudioResult<EnvMigrationReport>.Fail(StudioErrorCode.InvalidArgument, $"Environment '{envId}' has no datasource to migrate.");

        var types = ResolveEntityTypes(app, options);
        if (types.Count == 0) return StudioResult<EnvMigrationReport>.Fail(StudioErrorCode.InvalidArgument, "No entity types found for the app. Set app projects with data-project flags or pass EntityTypeNames.");

        var report = new EnvMigrationReport { AppId = appId, EnvId = envId, DatasourceName = targets.Count == 1 ? targets[0].Name : $"{targets.Count} datasource(s)", Succeeded = true };

        try
        {
            foreach (var dsDef in targets)
            {
                ct.ThrowIfCancellationRequested();
                if (dryRun)
                {
                    var ds = _editor.GetDataSource(dsDef.Name);
                    if (ds == null) { report.Succeeded = false; report.Message += $"[{dsDef.Name}] not registered; "; continue; }
                    var mgmt = new TheTechIdea.Beep.Editor.Migration.MigrationManager(_editor, ds) { MigrateDataSource = ds };
                    var plan = mgmt.BuildMigrationPlanForTypes(types, options?.DetectRelationships ?? true, options?.ApplyForeignKeys ?? false, options?.ApplyIndexes ?? false);
                    var ops = plan?.Operations?.Count ?? 0;
                    report.OperationsApplied += ops;
                    if (ops == 0) report.Message += $"[{dsDef.Name}] up to date; ";
                    else report.Message += $"[{dsDef.Name}] {ops} pending; ";
                    continue;
                }

                var tracking = new TheTechIdea.Beep.Editor.Migration.MigrationTrackingService(_editor);
                var res = tracking.ExecuteTrackedMigration(dsDef.Name, types,
                    detectRelationships: options?.DetectRelationships ?? true,
                    applyForeignKeys: options?.ApplyForeignKeys ?? false,
                    applyIndexes: options?.ApplyIndexes ?? false);

                var ok = res?.Flag == Errors.Ok;
                if (!ok) report.Succeeded = false;
                if (ok && res?.Message?.Contains("up to date", StringComparison.OrdinalIgnoreCase) == true)
                    report.Message += $"[{dsDef.Name}] up to date; ";
                else
                    report.Message += $"[{dsDef.Name}] {(ok ? "applied " + types.Count : "FAILED: " + res?.Message)}; ";
                if (ok) report.OperationsApplied += types.Count;

                var latest = _editor.Version?.GetLatestVersion(dsDef.Name);
                if (latest != null) report.SchemaVersion = $"{latest.Major}.{latest.Minor}.{latest.Patch}";
            }

            if (report.OperationsApplied == 0 && report.Succeeded) report.WasUpToDate = true;
            if (string.IsNullOrWhiteSpace(report.Message)) report.Message = "Ok.";
            return StudioResult<EnvMigrationReport>.Ok(report);
        }
        catch (Exception ex) { return StudioResult<EnvMigrationReport>.Fail(StudioErrorCode.HostNotSupported, ex.Message); }
    }

    private (string Name, StudioError? Error) ResolveDatasource(string appId, string envId, string? datasourceName = null)
    {
        var app = _editor.AppRegistry?.GetApp(appId);
        if (app == null) return ("", new StudioError(StudioErrorCode.NotFound, $"App '{appId}' not found.", null, null));
        var env = app.GetEnvironment(envId);
        if (env == null) return ("", new StudioError(StudioErrorCode.NotFound, $"Environment '{envId}' not found.", null, null));
        AppDataSource? ds = null;
        if (!string.IsNullOrWhiteSpace(datasourceName))
            ds = env.Datasources.FirstOrDefault(d => d.Name.Equals(datasourceName, StringComparison.OrdinalIgnoreCase));
        if (ds == null)
            ds = env.Datasources.FirstOrDefault(d => d.IsPrimary) ?? env.Datasources.FirstOrDefault();
        if (ds == null) return ("", new StudioError(StudioErrorCode.InvalidArgument, $"Environment '{envId}' has no datasource.", null, null));
        return (ds.Name, null);
    }

    private static List<Type> ResolveEntityTypes(AppDefinition app, MigrationOptions? options)
    {
        if (options?.EntityTypeNames?.Count > 0)
        {
            var byName = new List<Type>();
            foreach (var name in options.EntityTypeNames)
            {
                var t = Type.GetType(name) ?? AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).FirstOrDefault(x => x != null);
                if (t != null) byName.Add(t);
            }
            return byName;
        }
        var types = new List<Type>();

        // 1) Gather entity types from the app's data project assemblies (the multi-project case).
        var dataAssemblyPaths = app.Projects
            .Where(p => p.IsDataProject && !string.IsNullOrWhiteSpace(p.AssemblyPath) && System.IO.File.Exists(p.AssemblyPath!))
            .Select(p => p.AssemblyPath!)
            .Distinct(StringComparer.OrdinalIgnoreCase);
        foreach (var path in dataAssemblyPaths)
        {
            try
            {
                var asm = Assembly.LoadFrom(path);
                foreach (var t in asm.GetExportedTypes().Where(t => t.IsClass && !t.IsAbstract))
                    types.Add(t);
            }
            catch { /* skip unloadable assembly */ }
        }

        // 2) Back-compat: single AssemblyPath on the app (now a computed view of the data project).
        if (types.Count == 0 && !string.IsNullOrWhiteSpace(app.AssemblyPath) && System.IO.File.Exists(app.AssemblyPath))
        {
            try
            {
                var asm = Assembly.LoadFrom(app.AssemblyPath);
                types = asm.GetExportedTypes().Where(t => t.IsClass && !t.IsAbstract).ToList();
            }
            catch { /* fall through */ }
        }

        // 3) Last resort: namespace heuristic from module names.
        if (types.Count == 0 && app.ModuleNames.Count > 0)
        {
            types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetExportedTypes())
                .Where(t => t.IsClass && !t.IsAbstract && app.ModuleNames.Any(m => t.Namespace?.StartsWith(m, StringComparison.OrdinalIgnoreCase) == true))
                .ToList();
        }
        return types;
    }
}
