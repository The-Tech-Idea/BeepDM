// Copyright (c) The Tech Idea. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.SetUp.Security;
using TheTechIdea.Beep.Studio.Apps.Workflows;
using TheTechIdea.Beep.Studio.Identity;
using TheTechIdea.Beep.Studio.Permissions;

namespace TheTechIdea.Beep.Services.Studio.Identity;

/// <summary>
/// Stage 4 solo-mode <see cref="IIdentityStore"/>: a single implicit local admin, no password.
/// </summary>
/// <remarks>
/// <para>
/// This is the Studio-scoped counterpart of <see cref="TheTechIdea.Beep.SetUp.Security.AnonymousSetupPrincipal"/>:
/// when no real identity is configured (solo hosts), the operating user is treated as the implicit
/// admin on everything. This preserves today's behavior — the WPF <c>AppStudioSession.CurrentUser</c>
/// defaults to <c>Environment.UserName</c> and that user is allowed to do anything.
/// </para>
/// <para>
/// <b>Validation always succeeds.</b> Solo mode has no password gate. The local admin is identified
/// by user name (case-insensitive), and <see cref="ValidateCredentialsAsync"/> returns the user for
/// any password (including null). Enterprise mode uses <see cref="DatabaseIdentityStore"/>.
/// </para>
/// </remarks>
public sealed class LocalIdentityStore : IIdentityStore
{
    /// <summary>Stable id for the solo admin. Matches <c>AnonymousSetupPrincipal.Id</c>.</summary>
    public const string LocalAdminId = "local-admin";

    /// <summary>
    /// The singleton local admin user. <see cref="StudioUser.UserName"/> is the OS user name, so
    /// audit entries read identically to the pre-Stage-4 behavior.
    /// </summary>
    public static StudioUser LocalAdmin { get; } = new()
    {
        Id = LocalAdminId,
        UserName = Environment.UserName,
        DisplayName = Environment.UserName,
        Email = null,
        IsActive = true,
        CreatedAt = DateTimeOffset.UtcNow,
        Roles = new List<string> { nameof(AppMemberRole.Admin) },
    };

    /// <inheritdoc />
    public Task<StudioUser?> FindByIdAsync(string userId, CancellationToken ct = default)
        => Task.FromResult<StudioUser?>(
            string.Equals(userId, LocalAdminId, StringComparison.OrdinalIgnoreCase)
                ? LocalAdmin
                : null);

    /// <inheritdoc />
    public Task<StudioUser?> FindByNameAsync(string userName, CancellationToken ct = default)
        => Task.FromResult<StudioUser?>(
            string.Equals(userName, LocalAdmin.UserName, StringComparison.OrdinalIgnoreCase)
                ? LocalAdmin
                : null);

    /// <inheritdoc />
    public Task<IReadOnlyList<StudioUser>> ListUsersAsync(string? appId = null, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<StudioUser>>(new[] { LocalAdmin });

    /// <inheritdoc />
    public Task<StudioUser> CreateAsync(StudioUser user, string? password = null, CancellationToken ct = default)
        => throw new InvalidOperationException("LocalIdentityStore does not support creating users. Use DatabaseIdentityStore for multi-user mode.");

    /// <inheritdoc />
    public Task<StudioUser> UpdateAsync(StudioUser user, CancellationToken ct = default)
        => throw new InvalidOperationException("LocalIdentityStore does not support updating users. Use DatabaseIdentityStore for multi-user mode.");

    /// <inheritdoc />
    public Task<bool> DeleteAsync(string userId, CancellationToken ct = default)
        => throw new InvalidOperationException("LocalIdentityStore does not support deleting users. Use DatabaseIdentityStore for multi-user mode.");

    /// <inheritdoc />
    /// <remarks>Solo mode has no password — any credential pair identifying the local admin succeeds.</remarks>
    public Task<StudioUser?> ValidateCredentialsAsync(string userName, string password, CancellationToken ct = default)
        => FindByNameAsync(userName, ct);

    /// <inheritdoc />
    /// <remarks>The local admin implicitly has every role. Assignment is a no-op.</remarks>
    public Task<IReadOnlyList<string>> GetRolesAsync(string userId, string? appId = null, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<string>>(LocalAdmin.Roles);

    public Task AssignRoleAsync(string userId, string role, string? appId = null, CancellationToken ct = default)
        => Task.CompletedTask;  // no-op: local admin has every role implicitly

    public Task RevokeRoleAsync(string userId, string role, string? appId = null, CancellationToken ct = default)
        => Task.CompletedTask;  // no-op: cannot revoke the implicit-admin role
}

/// <summary>
/// Stage 4 solo-mode <see cref="IStudioAuthorizer"/>: grants the local admin everything, denies
/// everything else. Mirrors <see cref="TheTechIdea.Beep.SetUp.Security.AllowAllAuthorizer"/> at
/// the App scope.
/// </summary>
public sealed class AllowAllStudioAuthorizer : IStudioAuthorizer
{
    private readonly string _localUserId;

    /// <param name="localUserId">The user id treated as the implicit admin (typically
    /// <see cref="LocalIdentityStore.LocalAdminId"/>). Other users are deny-by-default.</param>
    public AllowAllStudioAuthorizer(string localUserId)
    {
        _localUserId = localUserId ?? throw new ArgumentNullException(nameof(localUserId));
    }

    /// <inheritdoc />
    public Task<PermissionDecision> EvaluateAsync(
        string userId, StudioPermission action,
        string? appId = null, string? envId = null, string? datasourceName = null,
        CancellationToken ct = default)
    {
        if (string.Equals(userId, _localUserId, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(PermissionDecision.Allow(
                reasons: new[] { $"Solo mode: local admin '{userId}' implicitly granted {action}" }));
        }
        return Task.FromResult(PermissionDecision.Deny(
            $"Solo mode: only the local admin (id={_localUserId}) is granted permissions; '{userId}' is not recognized"));
    }

    /// <inheritdoc />
    /// <remarks>Setup-inherited: any principal is allowed in solo mode (matches AllowAllAuthorizer).</remarks>
    public Task<SetupAuthorizationResult> AuthorizeAsync(
        ISetupPrincipal principal,
        SetupPermission permission,
        TheTechIdea.Beep.SetUp.SetupContext context,
        CancellationToken token = default)
        => Task.FromResult(SetupAuthorizationResult.Allow());

    public Task GrantAsync(PermissionGrant grant, CancellationToken ct = default)
        => Task.CompletedTask;  // no-op: solo mode ignores grants

    public Task RevokeAsync(PermissionGrant grant, CancellationToken ct = default)
        => Task.CompletedTask;  // no-op

    public Task<IReadOnlyList<PermissionGrant>> ListGrantsAsync(string? userId = null, string? appId = null, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<PermissionGrant>>(Array.Empty<PermissionGrant>());

    public Task<IReadOnlyList<string>> ResolveActorsAsync(StudioPermission action, string? appId, string? envId = null, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<string>>(new[] { _localUserId });
}

/// <summary>
/// Stage 4 solo-mode <see cref="IStudioPrincipal"/>: auto-signed-in local admin at construction.
/// Mirrors <see cref="TheTechIdea.Beep.SetUp.Security.AnonymousSetupPrincipal"/> with
/// <c>IsAuthenticated = true</c> (the solo admin IS the signed-in user, even though they didn't
/// authenticate with a credential).
/// </summary>
public sealed class LocalStudioPrincipal : IStudioPrincipal
{
    /// <summary>Initial state: signed in as <see cref="LocalIdentityStore.LocalAdmin"/>.</summary>
    public LocalStudioPrincipal()
    {
        User = LocalIdentityStore.LocalAdmin;
    }

    public StudioUser? User { get; private set; }

    // ISetupPrincipal surface — delegates to User.
    string TheTechIdea.Beep.SetUp.Security.ISetupPrincipal.Id => User?.Id ?? string.Empty;
    string TheTechIdea.Beep.SetUp.Security.ISetupPrincipal.DisplayName => User?.DisplayName ?? string.Empty;
    IReadOnlyCollection<string> TheTechIdea.Beep.SetUp.Security.ISetupPrincipal.Roles => (IReadOnlyCollection<string>?)User?.Roles ?? Array.Empty<string>();
    bool TheTechIdea.Beep.SetUp.Security.ISetupPrincipal.IsAuthenticated => User != null;

    // IStudioPrincipal surface.
    public bool IsAuthenticated => User != null;

    public void SignIn(StudioUser user) => User = user ?? throw new ArgumentNullException(nameof(user));
    public void SignOut() => User = null;

    /// <inheritdoc />
    /// <remarks>The solo principal has no authorizer — it allows everything while signed in. Use
    /// <see cref="DatabaseStudioPrincipal"/> for evaluated permissions.</remarks>
    public Task<bool> CanAsync(StudioPermission action, string? appId = null, string? envId = null, string? datasourceName = null, CancellationToken ct = default)
        => Task.FromResult(User != null);
}
