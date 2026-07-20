// Copyright (c) The Tech Idea. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.SetUp;
using TheTechIdea.Beep.SetUp.Steps;
using TheTechIdea.Beep.Studio.Driver;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Studio.Driver;

/// <summary>
/// Default implementation of <see cref="IDriverService"/>. Lists registered
/// drivers from <see cref="IConfigEditor.DataDriversClasses"/>; provisions
/// a driver via the engine's <see cref="DriverProvisionStep"/>.
/// </summary>
public sealed class DriverService : IDriverService
{
    private readonly IDMEEditor _editor;

    public DriverService(IDMEEditor editor)
    {
        _editor = editor ?? throw new ArgumentNullException(nameof(editor));
    }

    public Task<StudioResult<IReadOnlyList<DriverInfo>>> ListAsync(CancellationToken ct = default)
    {
        try
        {
            var drivers = _editor.ConfigEditor?.DataDriversClasses ?? new List<ConnectionDriversConfig>();
            var list = drivers
                .Select(d => new DriverInfo(
                    PackageName: d.PackageName ?? string.Empty,
                    ClassName: d.DriverClass ?? string.Empty,
                    Version: d.version ?? string.Empty,
                    DataSourceType: d.DatasourceType.ToString() ?? string.Empty,
                    Category: d.DatasourceCategory.ToString() ?? string.Empty,
                    Source: d.IsMissing ? "Missing" : "Registered",
                    Location: string.Empty,
                    IsLoaded: !d.IsMissing,
                    IsAutoLoad: d.AutoLoad,
                    IconName: d.iconname,
                    ExtensionsHandled: new List<string>(),
                    // PR 17: extensionstoHandle is a comma-separated string on
                    // the engine type, not a string[]. Split on commas when
                    // present.
                    FileExtensions: SplitExtensions(d.extensionstoHandle)))
                .ToList();
            return Task.FromResult(StudioResult<IReadOnlyList<DriverInfo>>.Ok(list));
        }
        catch (Exception ex)
        {
            return Task.FromResult(StudioResult<IReadOnlyList<DriverInfo>>.Fail(StudioErrorCode.InternalError, ex.Message, ex));
        }
    }

    public Task<StudioResult<DriverInfo>> GetAsync(string packageName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(packageName))
            return Task.FromResult(StudioResult<DriverInfo>.Fail(StudioErrorCode.InvalidArgument, "packageName is required."));
        var d = (_editor.ConfigEditor?.DataDriversClasses ?? new List<ConnectionDriversConfig>())
            .FirstOrDefault(x => string.Equals(x.PackageName, packageName, StringComparison.OrdinalIgnoreCase));
        if (d == null)
            return Task.FromResult(StudioResult<DriverInfo>.Fail(StudioErrorCode.NotFound, $"Driver '{packageName}' not found."));
        return Task.FromResult(StudioResult<DriverInfo>.Ok(new DriverInfo(
            PackageName: d.PackageName ?? string.Empty,
            ClassName: d.DriverClass ?? string.Empty,
            Version: d.version ?? string.Empty,
            DataSourceType: d.DatasourceType.ToString() ?? string.Empty,
            Category: d.DatasourceCategory.ToString() ?? string.Empty,
            Source: d.IsMissing ? "Missing" : "Registered",
            Location: string.Empty,
            IsLoaded: !d.IsMissing,
            IsAutoLoad: d.AutoLoad,
            IconName: d.iconname,
            ExtensionsHandled: new List<string>(),
            FileExtensions: SplitExtensions(d.extensionstoHandle))));
    }

    public async Task<StudioResult<DriverProvisionResult>> ProvisionAsync(
        DriverProvisionRequest request,
        IStudioProgress? progress = null,
        CancellationToken ct = default)
    {
        if (request == null)
            return StudioResult<DriverProvisionResult>.Fail(StudioErrorCode.InvalidArgument, "request is required.");
        if (string.IsNullOrWhiteSpace(request.PackageName))
            return StudioResult<DriverProvisionResult>.Fail(StudioErrorCode.InvalidArgument, "request.PackageName is required.");

        progress?.Report(new StudioProgressUpdate(
            OperationId: Guid.NewGuid().ToString("N"),
            OperationName: $"Provisioning driver {request.PackageName}",
            Stage: StudioProgressStage.Begin,
            CurrentStep: "Locating driver",
            Percent: 0,
            Severity: StudioProgressSeverity.Info,
            Timestamp: DateTimeOffset.UtcNow,
            Payload: new Dictionary<string, object?> { ["source"] = request.Source.ToString() }));

        try
        {
            // Local source: copy the DLL to the host's drivers folder and register.
            if (request.Source == DriverSource.Local)
            {
                if (string.IsNullOrWhiteSpace(request.LocalPath) || !File.Exists(request.LocalPath))
                    return StudioResult<DriverProvisionResult>.Fail(StudioErrorCode.NotFound,
                        $"Local driver file not found: {request.LocalPath}");

                var asm = Assembly.LoadFrom(request.LocalPath);
                var asmName = asm.GetName();
                var (dataSourceTypeName, dataSourceCategoryName) = InspectTypes(asm);

                var cfg = new ConnectionDriversConfig
                {
                    PackageName = request.PackageName,
                    DriverClass = asmName.Name,
                    version = asmName.Version?.ToString() ?? "1.0.0",
                    DatasourceType = ParseEnumOrDefault<DataSourceType>(dataSourceTypeName),
                    DatasourceCategory = ParseEnumOrDefault<DatasourceCategory>(dataSourceCategoryName),
                    AutoLoad = request.AutoLoad,
                    IsMissing = false,
                    NuggetMissing = true,         // not a NuGet package
                    extensionstoHandle = string.Empty
                };

                _editor.ConfigEditor?.DataDriversClasses.Add(cfg);
                _editor.ConfigEditor?.SaveConnectionDriversConfigValues();

                progress?.Report(new StudioProgressUpdate(
                    OperationId: Guid.NewGuid().ToString("N"),
                    OperationName: $"Provisioning driver {request.PackageName}",
                    Stage: StudioProgressStage.Complete,
                    CurrentStep: "Registered",
                    Percent: 100,
                    Severity: StudioProgressSeverity.Info,
                    Timestamp: DateTimeOffset.UtcNow,
                    Payload: null));

                return StudioResult<DriverProvisionResult>.Ok(new DriverProvisionResult(
                    Success: true, VersionResolved: cfg.version ?? "1.0.0", Location: request.LocalPath!,
                    Loaded: false, ClassesRegistered: new List<string> { cfg.DriverClass ?? asmName.Name }));
            }

            // NuGet / Plugin paths: defer to the engine's DriverProvisionStep.
            var stepOpts = new DriverProvisionStepOptions
            {
                PackageName = request.PackageName,
                Version = request.Version
            };
            var step = new DriverProvisionStep(stepOpts);
            // PR 17: SetupContext.DataSource expects an IDataSource, not a
            // SetupState (those are different concerns). We pass null! and
            // the engine's DriverProvisionStep is responsible for finding
            // or constructing a real IDataSource from the Editor when it
            // needs one. The Studio doesn't manage raw IDataSource
            // instances — that's the host's IDMEEditor's job.
            var ctx = new SetupContext
            {
                Editor = _editor,
                DataSource = null!
            };
            // Drive the step through its public Execute path with a progress adapter.
            var adapter = progress != null ? new Migration.StudioProgressToEngineAdapter(progress, "driver-provision") : null;
            var result = step.Execute(ctx, adapter);
            var success = result?.Flag != Errors.Failed;
            return success
                ? StudioResult<DriverProvisionResult>.Ok(new DriverProvisionResult(
                    Success: true, VersionResolved: request.Version ?? "latest", Location: request.LocalPath ?? request.PluginAssemblyPath ?? "(engine-managed)",
                    Loaded: true, ClassesRegistered: new List<string> { request.PackageName }))
                : StudioResult<DriverProvisionResult>.Fail(StudioErrorCode.DriverLoadFailed,
                    result?.Message ?? "Driver provisioning failed.");
        }
        catch (Exception ex)
        {
            progress?.Report(new StudioProgressUpdate(
                OperationId: Guid.NewGuid().ToString("N"),
                OperationName: $"Provisioning driver {request.PackageName}",
                Stage: StudioProgressStage.Failed,
                CurrentStep: ex.Message,
                Percent: 0,
                Severity: StudioProgressSeverity.Error,
                Timestamp: DateTimeOffset.UtcNow,
                Payload: null));
            return StudioResult<DriverProvisionResult>.Fail(StudioErrorCode.DriverLoadFailed, ex.Message, ex);
        }
    }

    public Task<StudioResult<bool>> UnloadAsync(string packageName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(packageName))
            return Task.FromResult(StudioResult<bool>.Fail(StudioErrorCode.InvalidArgument, "packageName is required."));
        try
        {
            var drivers = _editor.ConfigEditor?.DataDriversClasses;
            var d = drivers?.FirstOrDefault(x => string.Equals(x.PackageName, packageName, StringComparison.OrdinalIgnoreCase));
            if (d == null) return Task.FromResult(StudioResult<bool>.Fail(StudioErrorCode.NotFound, "Driver not found."));
            drivers!.Remove(d);
            _editor.ConfigEditor?.SaveConnectionDriversConfigValues();
            return Task.FromResult(StudioResult<bool>.Ok(true));
        }
        catch (Exception ex)
        {
            return Task.FromResult(StudioResult<bool>.Fail(StudioErrorCode.InternalError, ex.Message, ex));
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Stage 6.5: projects <c>_editor.assemblyHandler.Assemblies</c> (a list of <c>assemblies_rep</c>)
    /// into the Studio DTO. This is the single sanctioned path for AppStudio to learn what's loaded
    /// — killing the <c>IBeepService.LLoader.Assemblies</c> bypass in SchemaBrowserView.
    /// </remarks>
    public Task<StudioResult<IReadOnlyList<LoadedAssemblyInfo>>> ListLoadedAssembliesAsync(CancellationToken ct = default)
    {
        try
        {
            var assemblies = _editor.assemblyHandler?.Assemblies ?? new List<TheTechIdea.Beep.Tools.assemblies_rep>();
            var list = assemblies
                .Where(a => a?.DllLib != null)
                .Select(a =>
                {
                    var name = a.DllLib!.GetName().Name ?? a.DllName ?? a.DllLib.FullName ?? string.Empty;
                    return new LoadedAssemblyInfo(
                        Name: name,
                        Path: !string.IsNullOrEmpty(a.DllLibPath) ? a.DllLibPath : a.DllLib.Location,
                        Version: a.DllLib.GetName().Version?.ToString(),
                        FullName: a.DllLib.FullName);
                })
                .ToList();
            return Task.FromResult(StudioResult<IReadOnlyList<LoadedAssemblyInfo>>.Ok(list));
        }
        catch (Exception ex)
        {
            return Task.FromResult(StudioResult<IReadOnlyList<LoadedAssemblyInfo>>.Fail(StudioErrorCode.InternalError, ex.Message, ex));
        }
    }

    // ── Private helpers ─────────────────────────────────────────────────────

    private static (string DataSourceType, string Category) InspectTypes(Assembly asm)
    {
        // v1 best-effort: scan the assembly for IDataSource implementations and
        // pick the first concrete one. A future PR can let the caller declare
        // type/category explicitly.
        try
        {
            var dataSourceType = asm.GetTypes()
                .FirstOrDefault(t => !t.IsAbstract && !t.IsInterface && typeof(IDataSource).IsAssignableFrom(t));
            return (dataSourceType?.Name ?? "Unknown", "RDBMS");
        }
        catch
        {
            return ("Unknown", "RDBMS");
        }
    }

    private static T ParseEnumOrDefault<T>(string? name) where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(name)) return default;
        return Enum.TryParse<T>(name, ignoreCase: true, out var v) ? v : default;
    }

    /// <summary>
    /// Split a comma-separated extension string into a list. Returns an
    /// empty list when the input is null or whitespace. Used to convert
    /// <c>ConnectionDriversConfig.extensionstoHandle</c> (a single string)
    /// into the <c>List&lt;string&gt;</c> shape the Studio's
    /// <see cref="DriverInfo"/> expects.
    /// </summary>
    private static List<string> SplitExtensions(string? extensions)
    {
        if (string.IsNullOrWhiteSpace(extensions)) return new List<string>();
        return extensions
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }
}
