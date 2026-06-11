// Copyright (c) The Tech Idea. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Studio;

/// <summary>
/// DI options for the Studio. Passed to <see cref="BeepServiceExtensions.Studio.AddBeepStudio"/>.
/// </summary>
public sealed class StudioOptions
{
    /// <summary>The Studio's data folder. Defaults to the platform-appropriate path from <c>EnvironmentService</c>.</summary>
    public string DataRoot { get; set; } = TheTechIdea.Beep.Services.EnvironmentService.CreateAppfolder("Studio");

    /// <summary>Persistence layout. Default: JSON.</summary>
    public StudioPersistenceMode Persistence { get; set; } = StudioPersistenceMode.Json;

    /// <summary>Disable the audit hook. Default: <c>false</c> (audit ON).</summary>
    public bool DisableAudit { get; set; } = false;

    /// <summary>Disable the built-in hosted services (sync runner etc.). Default: <c>false</c>.</summary>
    public bool DisableHostedServices { get; set; } = false;

    /// <summary>Disable the auto-load of plug-in assemblies on startup. Default: <c>false</c>.</summary>
    public bool DisableAssemblyLoad { get; set; } = false;

    /// <summary>Default tier applied to environments created via the API when the caller does not specify one. Default: <see cref="RolloutTier.Dev"/>.</summary>
    public RolloutTier DefaultEnvironmentTier { get; set; } = RolloutTier.Dev;

    /// <summary>How many days of audit history to retain on the local file sink. 0 = forever. Default: 365.</summary>
    public int AuditRetentionDays { get; set; } = 365;

    /// <summary>Path to the project's <c>DataLifecycleManifest</c>. If <c>null</c>, the Studio walks up from CWD looking for <c>beep/data-lifecycle-manifest.json</c>.</summary>
    public string? ManifestPath { get; set; }

    /// <summary>When <c>true</c> (default), the Studio refuses to apply a migration or run a sync that does not match the manifest's expected source list.</summary>
    public bool EnforceManifestOnApply { get; set; } = true;

    /// <summary>When <c>true</c> (default), every audit event is enriched with <see cref="DeploymentMetadata"/> (code revision + build id).</summary>
    public bool EnrichAuditWithDeploymentMetadata { get; set; } = true;

    /// <summary>Override the HMAC key used to sign approval tokens. <c>null</c> means the DI extension will read from <c>BEEP_APPROVAL_HMAC_KEY</c> (or generate an ephemeral key in dev).</summary>
    public string? ApprovalHmacKey { get; set; }
}

/// <summary>The persistence layout for the Studio's JSON state files.</summary>
public enum StudioPersistenceMode
{
    /// <summary>JSON files only.</summary>
    Json = 0,

    /// <summary>SQLite only.</summary>
    Sqlite = 1,

    /// <summary>JSON for sources / schemas / manifest, SQLite for audit + history.</summary>
    Hybrid = 2
}
