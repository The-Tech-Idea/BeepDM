// Copyright (c) The Tech Idea. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Studio;

/// <summary>
/// Engine-wide constants for the Studio. Used for file names, environment names,
/// and well-known keys the engine looks up in the manifest.
/// </summary>
public static class StudioConstants
{
    /// <summary>The Studio's sub-folder under the platform-appropriate <c>Beep</c> data folder.</summary>
    public const string StudioSubFolder = "Studio";

    /// <summary>The default manifest filename, relative to the project repo root.</summary>
    public const string DefaultManifestFileName = "data-lifecycle-manifest.json";

    /// <summary>The default manifest sub-folder, relative to the project repo root.</summary>
    public const string DefaultManifestSubFolder = "beep";

    /// <summary>The current supported manifest version.</summary>
    public const int CurrentManifestVersion = 1;

    /// <summary>The environment variable that CI sets with the deployment metadata (engine Phase 10).</summary>
    public const string DeploymentMetadataEnvVar = "BEEP_DEPLOYMENT_METADATA_JSON";

    /// <summary>The environment variable that holds the approval-token HMAC key.</summary>
    public const string ApprovalHmacKeyEnvVar = "BEEP_APPROVAL_HMAC_KEY";

    /// <summary>The environment variable that points at a manifest file (overrides the auto-discover).</summary>
    public const string ManifestPathEnvVar = "BEEP_MANIFEST_PATH";

    /// <summary>The default env profile id, used when no profile is specified.</summary>
    public const string DefaultEnvironmentId = "dev";

    /// <summary>All well-known environment ids, in display order.</summary>
    public static IReadOnlyList<string> WellKnownEnvironmentIds { get; } = new[]
    {
        "dev",
        "test",
        "staging",
        "live"
    };

    /// <summary>The default cooldown between two applies in the Live tier (5 minutes).</summary>
    public static TimeSpan DefaultLiveCooldown { get; } = TimeSpan.FromMinutes(5);
}
