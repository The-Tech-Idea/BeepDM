// Copyright (c) The Tech Idea. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Studio.Driver;

/// <summary>
/// Provisioning of a data-source driver (a NuGet package, a local DLL, or a
/// plugin assembly). Implemented in Phase 2 — this PR ships the stub.
/// </summary>
public interface IDriverService
{
    /// <summary>List every driver registered with the engine (built-in + provisioned).</summary>
    Task<StudioResult<IReadOnlyList<DriverInfo>>> ListAsync(CancellationToken ct = default);

    /// <summary>Get a single driver by its package name (e.g. <c>Beep.DataSource.SqlServer</c>).</summary>
    Task<StudioResult<DriverInfo>> GetAsync(string packageName, CancellationToken ct = default);

    /// <summary>Provision a driver. The default implementation supports NuGet, local-folder, and plugin-assembly sources.</summary>
    Task<StudioResult<DriverProvisionResult>> ProvisionAsync(
        DriverProvisionRequest request,
        IStudioProgress? progress = null,
        CancellationToken ct = default);

    /// <summary>Unload a previously-provisioned driver. The engine keeps the package on disk.</summary>
    Task<StudioResult<bool>> UnloadAsync(string packageName, CancellationToken ct = default);

    /// <summary>
    /// Stage 6.5: list the assemblies currently loaded into the engine's assembly handler. Used by
    /// the Schema Browser's "Discover from assembly" picker — kills the
    /// <c>IBeepService.LLoader.Assemblies</c> bypass in the AppStudio view.
    /// </summary>
    Task<StudioResult<IReadOnlyList<LoadedAssemblyInfo>>> ListLoadedAssembliesAsync(CancellationToken ct = default);
}

/// <summary>
/// Stage 6.5: a loaded-assembly entry — name + path + version, what a UI picker needs to display
/// and what the engine needs to reload it for entity discovery.
/// </summary>
public sealed record LoadedAssemblyInfo(
    string Name,
    string? Path,
    string? Version,
    string? FullName);

/// <summary>A registered data-source driver.</summary>
public sealed record DriverInfo(
    string PackageName,
    string ClassName,
    string Version,
    string DataSourceType,
    string Category,
    string Source,
    string Location,
    bool IsLoaded,
    bool IsAutoLoad,
    string? IconName,
    IReadOnlyList<string> ExtensionsHandled,
    IReadOnlyList<string> FileExtensions);

/// <summary>A request to provision a driver.</summary>
public sealed record DriverProvisionRequest(
    string PackageName,
    string? Version,
    DriverSource Source,
    string? LocalPath = null,
    string? PluginAssemblyPath = null,
    bool AutoLoad = true);

/// <summary>The result of a provisioning attempt.</summary>
public sealed record DriverProvisionResult(
    bool Success,
    string VersionResolved,
    string Location,
    bool Loaded,
    IReadOnlyList<string> ClassesRegistered,
    string? ErrorMessage = null);

/// <summary>Where the driver comes from.</summary>
public enum DriverSource
{
    /// <summary>NuGet package — fetched from a configured feed (default: nuget.org).</summary>
    NuGet = 0,

    /// <summary>Local file / folder — copied from a user-supplied path.</summary>
    Local = 1,

    /// <summary>Plugin assembly — loaded from an already-registered plugin.</summary>
    Plugin = 2
}
