// Copyright (c) The Tech Idea. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Studio.Deployment;
using TheTechIdea.Beep.Studio.Driver;
using TheTechIdea.Beep.Studio.Governance;
using TheTechIdea.Beep.Studio.Manifest;
using TheTechIdea.Beep.Studio.Migration;
using TheTechIdea.Beep.Studio.Schema;
using TheTechIdea.Beep.Studio.Source;
using TheTechIdea.Beep.Studio.Sync;

namespace TheTechIdea.Beep.Studio.Contracts;

/// <summary>
/// Top-level facade for the Beep Studio. Composes every lower-level service
/// (driver, source, schema, migration, sync, governance, manifest, deployment)
/// behind a single async, platform-agnostic API. UI hosts (Blazor, WinForms,
/// WPF, Maui) call into this; engine primitives (IMigrationManager, BeepSyncManager,
/// IBeepAudit, IDMEEditor) are never touched directly from the host.
/// </summary>
/// <remarks>
/// This is the **only** type the host UI is expected to depend on. Sub-service
/// facades (e.g. <see cref="Migrations"/>, <see cref="Sources"/>) are exposed as
/// properties so the host can take a single dependency in DI and still reach
/// into any sub-service.
/// </remarks>
public interface IStudioService
{
    /// <summary>Environment profiles (Dev / Test / Staging / Live / Custom).</summary>
    IEnvironmentProfileService Environments { get; }

    /// <summary>Data-source driver provisioning (Phase 2).</summary>
    IDriverService Drivers { get; }

    /// <summary>Source (connection) registry + configuration (Phase 3).</summary>
    ISourceService Sources { get; }

    /// <summary>Schema discovery + design (Phase 4).</summary>
    ISchemaService Schemas { get; }

    /// <summary>Migration orchestration (Phase 5).</summary>
    IMigrationStudioService Migrations { get; }

    /// <summary>Data sync orchestration (Phase 6).</summary>
    ISyncStudioService Sync { get; }

    /// <summary>Governance: policies, approvals, audit (Phase 7).</summary>
    IGovernanceService Governance { get; }

    /// <summary>Data lifecycle manifest — the link between data and code (Phase 9).</summary>
    IDataLifecycleManifestService Manifest { get; }

    /// <summary>Deployment metadata + HMAC approval tokens (Phase 10).</summary>
    IDeploymentMetadataService Deployment { get; }

    /// <summary>
    /// Read the Studio's runtime info (version, supported types, capabilities).
    /// Synchronous because it is a pure metadata read backed by in-memory state.
    /// </summary>
    StudioInfo GetInfo();

    /// <summary>Async version of <see cref="GetInfo"/>.</summary>
    Task<StudioResult<StudioInfo>> GetInfoAsync(CancellationToken ct = default);
}
