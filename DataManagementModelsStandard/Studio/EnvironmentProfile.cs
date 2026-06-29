// Copyright (c) The Tech Idea. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Studio;

/// <summary>
/// An environment profile (Dev, Test, Staging, Live, or a custom tier).
/// Profiles drive the approval workflow — a profile with <see cref="RequiresApproval"/>
/// = true requires an approval token before any migration or sync can run.
/// </summary>
public sealed record EnvironmentProfile(
    /// <summary>Stable id (e.g. <c>dev</c>, <c>live</c>). Unique within the Studio.</summary>
    string Id,

    /// <summary>Human-readable display name (e.g. "Development", "Live (Production)").</summary>
    string Name,

    /// <summary>The rollout tier this profile represents.</summary>
    RolloutTier Tier,

    /// <summary>Display order in pickers. Lower = earlier.</summary>
    int Order,

    /// <summary>Optional UI hint (a hex colour, e.g. <c>#22C55E</c>). <c>null</c> = use the theme default.</summary>
    string? Color,

    /// <summary>True when every mutation targeting this profile requires an approval token.</summary>
    bool RequiresApproval,

    /// <summary>How many distinct approvers must sign off before an approval is granted.</summary>
    int RequiredApproverCount,

    /// <summary>True for any profile that touches production data (Live tier).</summary>
    bool IsProduction,

    /// <summary>Free-form tags for filtering (e.g. <c>["pii", "regulated"]</c>).</summary>
    IReadOnlyList<string> Tags,

    /// <summary>When the profile was created.</summary>
    DateTimeOffset CreatedAt,

    /// <summary>When the profile was last updated.</summary>
    DateTimeOffset UpdatedAt);

/// <summary>The rollout tier a profile represents. Drives the default approval policy.</summary>
public enum RolloutTier
{
    /// <summary>Development. No approval required by default.</summary>
    Dev = 0,

    /// <summary>Test / CI. Approval optional, depends on policy.</summary>
    Test = 1,

    /// <summary>Staging. Approval required.</summary>
    Staging = 2,

    /// <summary>Live / production. Approval required with multiple approvers.</summary>
    Live = 3,

    /// <summary>Custom tier (e.g. "sandbox", "dr"). The default policy is host-defined.</summary>
    Custom = 99
}
