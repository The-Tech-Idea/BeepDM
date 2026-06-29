// Copyright (c) The Tech Idea. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using TheTechIdea.Beep.AppMap;
using TheTechIdea.Beep.Studio.Apps;
using TheTechIdea.Beep.Studio.Contracts;
using TheTechIdea.Beep.Studio.Deployment;
using TheTechIdea.Beep.Studio.Driver;
using TheTechIdea.Beep.Studio.Governance;
using TheTechIdea.Beep.Studio.Manifest;
using TheTechIdea.Beep.Studio.Migration;
using TheTechIdea.Beep.Studio.Schema;
using TheTechIdea.Beep.Studio.Source;
using TheTechIdea.Beep.Studio.Sync;

namespace TheTechIdea.Beep.Studio;

/// <summary>
/// Top-level facade for the Beep Studio. Composes every lower-level service
/// (driver, source, schema, migration, sync, governance, manifest,
/// deployment) behind a single async, platform-agnostic API. The host UI
/// depends on <see cref="Contracts.IStudioService"/> only; this class
/// resolves the sub-services from <see cref="IServiceProvider"/>.
/// </summary>
public sealed class StudioService : IStudioService
{
    private readonly IServiceProvider _services;

    /// <summary>Construct the facade. The DI container supplies the sub-services.</summary>
    public StudioService(IServiceProvider services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <inheritdoc />
    public IAppStudioService Apps =>
        _services.GetRequiredService<IAppStudioService>();

    /// <inheritdoc />
    public IEnvironmentProfileService Environments =>
        _services.GetRequiredService<IEnvironmentProfileService>();

    /// <inheritdoc />
    public IDriverService Drivers =>
        _services.GetRequiredService<IDriverService>();

    /// <inheritdoc />
    public ISourceService Sources =>
        _services.GetRequiredService<ISourceService>();

    /// <inheritdoc />
    public ISchemaService Schemas =>
        _services.GetRequiredService<ISchemaService>();

    /// <inheritdoc />
    public IMigrationStudioService Migrations =>
        _services.GetRequiredService<IMigrationStudioService>();

    /// <inheritdoc />
    public ISyncStudioService Sync =>
        _services.GetRequiredService<ISyncStudioService>();

    /// <inheritdoc />
    public IGovernanceService Governance =>
        _services.GetRequiredService<IGovernanceService>();

    /// <inheritdoc />
    public IDataLifecycleManifestService Manifest =>
        _services.GetRequiredService<IDataLifecycleManifestService>();

    /// <inheritdoc />
    public IDeploymentMetadataService Deployment =>
        _services.GetRequiredService<IDeploymentMetadataService>();

    /// <inheritdoc />
    public StudioInfo GetInfo()
    {
        return new StudioInfo(
            Version: "0.1.0",
            EngineVersion: "3.0.0",
            SupportedDataSourceTypes: Array.Empty<string>(),
            SupportedDataSourceCategories: Array.Empty<string>(),
            SupportedTiers: new[] { RolloutTier.Dev, RolloutTier.Test, RolloutTier.Staging, RolloutTier.Live, RolloutTier.Custom },
            AuditEnabled: true,
            HostedServicesEnabled: true,
            ManifestLoaded: Manifest.Current != null,
            ManifestVersion: Manifest.Current?.ManifestVersion.ToString(),
            Capabilities: new Dictionary<string, object?>
            {
                ["efCoreAdapter"] = false,
                ["hmacApprovalTokens"] = true,
                ["fileScanAuditQuery"] = true
            });
    }

    /// <inheritdoc />
    public Task<StudioResult<StudioInfo>> GetInfoAsync(CancellationToken ct = default)
        => Task.FromResult(StudioResult<StudioInfo>.Ok(GetInfo()));
}
