// Copyright (c) The Tech Idea. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TheTechIdea.Beep.Studio.Contracts;
using TheTechIdea.Beep.Studio.Deployment;
using TheTechIdea.Beep.Studio.Driver;
using TheTechIdea.Beep.Studio.Governance;
using TheTechIdea.Beep.Studio.Manifest;
using TheTechIdea.Beep.Studio.Migration;
using TheTechIdea.Beep.Studio.Schema;
using TheTechIdea.Beep.Studio.Source;
using TheTechIdea.Beep.Studio.Stubs;
using TheTechIdea.Beep.Studio.Sync;

namespace TheTechIdea.Beep.Studio;

/// <summary>
/// DI registration helpers for the Beep Studio. The host calls
/// <see cref="AddBeepStudio"/> next to the existing
/// <c>RegisterBeepApp</c> call.
/// </summary>
public static class BeepServiceExtensions
{
    /// <summary>The DI registration method name (kept as a nested static for discoverability).</summary>
    public static IServiceCollection AddBeepStudio(
        this IServiceCollection services,
        Action<StudioOptions>? configure = null)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        // 1. Configure options. Use a default instance if the host did not supply one.
        var options = new StudioOptions();
        configure?.Invoke(options);
        services.TryAddSingleton(options);

        // 2. Register the top-level facade. TryAdd lets the host override the
        //    implementation for testing.
        services.TryAddSingleton<IStudioService, StudioService>();

        // 3. Register the sub-service facades. Each one defaults to a stub that
        //    returns HostNotSupported; the real implementations land in their
        //    respective phases (2-7, 9, 10). The sub-service accessors on
        //    StudioService resolve from this same DI container.
        //
        //    PR 2: ISourceService → real SourceService (wraps DatasourceManagementService).
        //          The SourceService needs the host's IDMEEditor; we resolve it lazily
        //          via a factory so the registration is order-independent.
        services.TryAddSingleton<IEnvironmentProfileService, EnvironmentProfileServiceStub>();
        // PR 8: IDriverService → real DriverService (wraps the engine's
        // DriverProvisionStep + ConnectionDriversConfig registry).
        services.TryAddSingleton<IDriverService>(sp => new DriverService(
            sp.GetRequiredService<TheTechIdea.Beep.Services.IBeepService>().DMEEditor));
        services.TryAddSingleton<ISourceService>(sp => new SourceService(
            sp.GetRequiredService<TheTechIdea.Beep.Services.IBeepService>().DMEEditor));
        // PR 3: ISchemaService → real SchemaService (wraps EntityDiscoveryService).
        services.TryAddSingleton<ISchemaService>(sp => new SchemaService(
            sp.GetRequiredService<TheTechIdea.Beep.Services.IBeepService>().DMEEditor));
        // PR 4: IMigrationStudioService → real MigrationStudioService (wraps IMigrationManager).
        services.TryAddSingleton<IMigrationStudioService>(sp => new MigrationStudioService(
            sp.GetRequiredService<TheTechIdea.Beep.Services.IBeepService>().DMEEditor));
        // PR 5: ISyncStudioService → real SyncStudioService (wraps BeepSyncManager).
        services.TryAddSingleton<ISyncStudioService>(sp => new SyncStudioService(
            sp.GetRequiredService<TheTechIdea.Beep.Services.IBeepService>().DMEEditor));
        // PR 6: IGovernanceService → real GovernanceService. Wraps IBeepAudit (optional;
        // falls back to NullBeepAudit when the engine's audit feature is disabled).
        services.TryAddSingleton<IGovernanceService>(sp =>
        {
            var audit = sp.GetService<TheTechIdea.Beep.Services.Audit.IBeepAudit>();
            return new GovernanceService(audit);
        });
        // PR 7: IDataLifecycleManifestService → real DataLifecycleManifestService
        // (reads/writes beep/data-lifecycle-manifest.json).
        services.TryAddSingleton<IDataLifecycleManifestService>(_ => new DataLifecycleManifestService());
        // PR 7: IDeploymentMetadataService → real DeploymentMetadataService
        // (resolves code revision, mints/verifies HMAC approval tokens).
        services.TryAddSingleton<IDeploymentMetadataService>(_ => new DeploymentMetadataService());

        return services;
    }
}
