// Copyright (c) The Tech Idea. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.SetUp.Security;
using TheTechIdea.Beep.Studio.Permissions;

namespace TheTechIdea.Beep.Studio.Identity;

/// <summary>
/// Stage 4: a Studio user. The user-store counterpart to <see cref="ISetupPrincipal"/> — same shape
/// (Id, DisplayName, Roles, IsAuthenticated) plus the credentials/profile fields the host needs for
/// login UI, audit, and account management.
/// </summary>
public sealed class StudioUser
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..12];
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastLoginAt { get; set; }
    /// <summary>Roles assigned at the global scope. App-scoped role assignments live on <c>AppRoleAssignment</c>.</summary>
    public List<string> Roles { get; set; } = new();
}

/// <summary>
/// Stage 4: the current user, scoped service. Solo hosts sign in <c>LocalIdentityStore.LocalAdmin</c>
/// at startup (zero-friction); enterprise hosts call <see cref="SignIn"/> from a login dialog.
/// Mirrors <see cref="ISetupPrincipal"/> and the WPF host's <c>AppStudioSession.CurrentUser</c>.
/// </summary>
public interface IStudioPrincipal : ISetupPrincipal
{
    /// <summary>The full <see cref="StudioUser"/> record (null before sign-in).</summary>
    StudioUser? User { get; }

    /// <summary>Whether <see cref="User"/> is set. Equivalent to <c>User != null</c>.</summary>
    new bool IsAuthenticated { get; }

    void SignIn(StudioUser user);
    void SignOut();

    /// <summary>Convenience: evaluate a permission for the current user at a global scope.</summary>
    Task<bool> CanAsync(StudioPermission action, string? appId = null, string? envId = null, string? datasourceName = null, CancellationToken ct = default);
}

/// <summary>
/// Stage 4: the user/role/credential store — the persistence-layer sibling of
/// <see cref="IStudioAuthorizer"/>. Solo impl returns the implicit local admin; enterprise impl
/// reads/writes a user table backed by <c>IStudioRepository</c> (Stage 3).
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this is separate from <c>IIdentityManagementService</c>.</b> The latter (in
/// <c>Services/AppMap/</c>) is a per-<i>datasource</i> identity probe — it inspects whether a
/// <i>customer's</i> database has ASP.NET Identity tables. <c>IIdentityStore</c> is the Studio's
/// own user directory — the people who operate the Studio (developers, DBAs, admins). Different
/// data, different lifetime, different consumers.
/// </para>
/// <para>
/// <b>Password storage.</b> Solo impl has no passwords (the local admin is implicit and unauthenticated).
/// Enterprise impl MUST hash with PBKDF2/Argon2 — never store plaintext. <see cref="ValidateCredentialsAsync"/>
/// runs in constant time after the user lookup to avoid user-exists timing oracles.
/// </para>
/// </remarks>
public interface IIdentityStore
{
    Task<StudioUser?> FindByIdAsync(string userId, CancellationToken ct = default);
    Task<StudioUser?> FindByNameAsync(string userName, CancellationToken ct = default);
    Task<IReadOnlyList<StudioUser>> ListUsersAsync(string? appId = null, CancellationToken ct = default);
    Task<StudioUser> CreateAsync(StudioUser user, string? password = null, CancellationToken ct = default);
    Task<StudioUser> UpdateAsync(StudioUser user, CancellationToken ct = default);
    Task<bool> DeleteAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Verify a credential pair. Returns the signed-in user on success, null on mismatch or locked-out
    /// account. Implementations MUST take roughly constant time after the lookup to avoid leaking
    /// which usernames exist.
    /// </summary>
    Task<StudioUser?> ValidateCredentialsAsync(string userName, string password, CancellationToken ct = default);

    Task<IReadOnlyList<string>> GetRolesAsync(string userId, string? appId = null, CancellationToken ct = default);
    Task AssignRoleAsync(string userId, string role, string? appId = null, CancellationToken ct = default);
    Task RevokeRoleAsync(string userId, string role, string? appId = null, CancellationToken ct = default);
}
