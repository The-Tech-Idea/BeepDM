// Copyright (c) The Tech Idea. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Studio.Contracts;

/// <summary>
/// CRUD over <see cref="EnvironmentProfile"/>s. The Studio uses these to drive
/// the approval workflow — every migration / sync declares which environment
/// it targets, and the engine looks up the profile to decide if an approval
/// is required.
/// </summary>
/// <remarks>
/// Implementations persist to <c>%ProgramData%\TheTechIdea\Beep\Studio\env-profiles.json</c>
/// (or the platform-equivalent path from <see cref="TheTechIdea.Beep.Services.EnvironmentService"/>).
/// </remarks>
public interface IEnvironmentProfileService
{
    /// <summary>List all profiles, ordered by <see cref="EnvironmentProfile.Order"/>.</summary>
    Task<StudioResult<IReadOnlyList<EnvironmentProfile>>> ListAsync(CancellationToken ct = default);

    /// <summary>Get a single profile by id.</summary>
    Task<StudioResult<EnvironmentProfile>> GetAsync(string environmentId, CancellationToken ct = default);

    /// <summary>Create or update a profile. The <see cref="EnvironmentProfile.UpdatedAt"/> is set by the engine.</summary>
    Task<StudioResult<EnvironmentProfile>> SaveAsync(EnvironmentProfile profile, CancellationToken ct = default);

    /// <summary>Delete a profile by id. Returns <see cref="StudioErrorCode.NotFound"/> if missing.</summary>
    Task<StudioResult<bool>> DeleteAsync(string environmentId, CancellationToken ct = default);

    /// <summary>Get the default profile (the first <see cref="RolloutTier.Dev"/> profile). Used when the caller does not specify one.</summary>
    Task<StudioResult<EnvironmentProfile>> GetDefaultAsync(CancellationToken ct = default);
}
