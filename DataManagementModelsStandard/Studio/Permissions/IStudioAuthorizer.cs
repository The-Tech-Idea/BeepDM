// Copyright (c) The Tech Idea. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.SetUp.Security;
using TheTechIdea.Beep.Studio.Apps.Workflows;

namespace TheTechIdea.Beep.Studio.Permissions;

/// <summary>
/// Stage 4: every operation a Studio user can be granted or denied. A superset of
/// <see cref="SetupPermission"/> (the first 8 values are identical, so a setup authorizer can be
/// adapted with a one-line map).
/// </summary>
/// <remarks>
/// Numeric values are stable. Do not renumber. Add new values at the end.
/// The first 8 (0-7) deliberately match <see cref="SetupPermission"/> so the same principal/authorizer
/// surface serves both setup-time and runtime — Stage 4.1's "unified surface" decision.
/// </remarks>
public enum StudioPermission
{
    // ── Setup-inherited (values match SetupPermission) ───────────────────────
    RunSetup = 0,
    ProvisionDriver = 1,
    ConfigureConnection = 2,
    ApplySchema = 3,
    ApproveMigration = 4,
    Seed = 5,
    Rollback = 6,
    ViewState = 7,

    // ── Runtime — App scope ──────────────────────────────────────────────────
    ViewApp = 100,
    EditApp = 101,
    DeleteApp = 102,

    ViewEnvironment = 110,
    EditEnvironment = 111,

    AddDatasource = 120,
    EditDatasource = 121,
    DeleteDatasource = 122,
    TestDatasource = 123,

    BuildMigrationPlan = 130,
    ApplyMigration = 131,
    RollbackMigration = 132,

    CopyData = 140,
    SubsetData = 141,
    MaskedCopyData = 142,

    PromoteCode = 150,
    PromoteSchema = 151,
    PromoteData = 152,

    /// <summary>Runtime governance approval. Distinct from <see cref="ApproveMigration"/> (setup-time).</summary>
    ApproveRequest = 160,
    ManageMembers = 161,
    ManagePolicies = 162,
    GenerateAppConfig = 163,
}

/// <summary>Allow or deny a permission. Default allow (most grants).</summary>
public enum PermissionEffect
{
    Allow = 0,
    Deny = 1,
}

/// <summary>
/// A single grant: user X may (or may not) do Y in scope (AppId, EnvId, DatasourceName — any null
/// means "any" at that level). Scopes form a specificity hierarchy: Datasource &gt; Env &gt; App &gt; global.
/// </summary>
public sealed class PermissionGrant
{
    public string UserId { get; set; } = string.Empty;
    public string? AppId { get; set; }
    public string? EnvId { get; set; }
    public string? DatasourceName { get; set; }
    public StudioPermission Action { get; set; }
    public PermissionEffect Effect { get; set; } = PermissionEffect.Allow;
    public DateTimeOffset GrantedAt { get; set; } = DateTimeOffset.UtcNow;
    public string GrantedBy { get; set; } = "system";
}

/// <summary>
/// The result of a permission evaluation. Carries enough detail for the UI to explain the decision
/// ("Denied because explicit Deny at env=prod beats role Allow at app scope").
/// </summary>
public sealed class PermissionDecision
{
    public bool Allowed { get; init; }
    public IReadOnlyList<string> Reasons { get; init; } = Array.Empty<string>();
    public IReadOnlyList<PermissionGrant> Matched { get; init; } = Array.Empty<PermissionGrant>();

    public static PermissionDecision Allow(IReadOnlyList<PermissionGrant>? matched = null, IReadOnlyList<string>? reasons = null)
        => new() { Allowed = true, Matched = matched ?? Array.Empty<PermissionGrant>(), Reasons = reasons ?? Array.Empty<string>() };

    public static PermissionDecision Deny(string reason, IReadOnlyList<PermissionGrant>? matched = null, IReadOnlyList<string>? extraReasons = null)
    {
        var reasons = new List<string> { reason };
        if (extraReasons != null) reasons.AddRange(extraReasons);
        return new() { Allowed = false, Matched = matched ?? Array.Empty<PermissionGrant>(), Reasons = reasons };
    }
}

/// <summary>
/// Stage 4: the unified permission evaluator for both setup-time and runtime.
/// </summary>
/// <remarks>
/// <para>
/// Extends <see cref="ISetupAuthorizer"/> so a single DI registration covers both surfaces. The
/// setup-time <c>AuthorizeAsync(principal, SetupPermission, SetupContext)</c> delegates to
/// <see cref="EvaluateAsync(string, StudioPermission, string?, string?, string?, CancellationToken)"/>
/// with no scope (null app/env/datasource).
/// </para>
/// <para>
/// <b>Evaluation algorithm</b> (explicit-deny-wins — the AWS IAM / GCP IAM model):
/// <list type="number">
/// <item>Collect all grants for <c>userId</c> matching <c>(action, appId, envId, datasourceName)</c>.</item>
/// <item>If ANY matching grant is <c>Deny</c> → <c>Deny</c>. Denials are absolute — a more-specific
/// <c>Allow</c> cannot punch through any matching <c>Deny</c> (privilege-escalation prevention).</item>
/// <item>Else if any matching grant is <c>Allow</c> → <c>Allow</c>.</item>
/// <item>Else <c>Deny</c> (default-deny).</item>
/// </list>
/// Specificity only narrows the <c>Allow</c> set — an app-wide Allow does not grant on unrelated
/// apps. But a single Deny at any matching scope is final.
/// </para>
/// </remarks>
public interface IStudioAuthorizer : ISetupAuthorizer
{
    // Note: ISetupAuthorizer.AuthorizeAsync(principal, permission, context, ct) is inherited
    // unchanged. Implementations route it to EvaluateAsync with no scope.

    /// <summary>
    /// Evaluate whether <paramref name="userId"/> may take <paramref name="action"/> in the given
    /// scope. Implementations SHOULD populate <see cref="PermissionDecision.Reasons"/> so the UI can
    /// explain denials.
    /// </summary>
    Task<PermissionDecision> EvaluateAsync(
        string userId,
        StudioPermission action,
        string? appId = null,
        string? envId = null,
        string? datasourceName = null,
        CancellationToken ct = default);

    /// <summary>Grant a permission (allow or deny). Idempotent: replaces any existing grant with the same (user, action, scope).</summary>
    Task GrantAsync(PermissionGrant grant, CancellationToken ct = default);

    /// <summary>Remove a grant. No-op if it doesn't exist.</summary>
    Task RevokeAsync(PermissionGrant grant, CancellationToken ct = default);

    Task<IReadOnlyList<PermissionGrant>> ListGrantsAsync(string? userId = null, string? appId = null, CancellationToken ct = default);

    /// <summary>
    /// Resolve the users who can take <paramref name="action"/> in scope — used by governance to
    /// fan out approval notifications ("who has ApproveRequest on env=prod?") and by the host to
    /// populate @-mention suggestions.
    /// </summary>
    Task<IReadOnlyList<string>> ResolveActorsAsync(StudioPermission action, string? appId, string? envId = null, CancellationToken ct = default);
}

/// <summary>
/// Role templates — the canned grant sets applied when <c>AssignRoleAsync(role)</c> is called.
/// </summary>
/// <remarks>
/// Mirrors <c>RoleBasedSetupAuthorizer</c>'s permission→roles map but inverted (role→permissions) and
/// scoped to <see cref="AppMemberRole"/> (Viewer/Contributor/Operator/Admin). The template applies
/// the role's grant set at the given scope (AppId, EnvId, DatasourceName) so e.g. assigning
/// "Operator on env=prod" only grants Operator permissions on prod.
/// </remarks>
public static class RoleTemplates
{
    /// <summary>Map a role to the permission set that role grants. Used by <c>IIdentityStore.AssignRoleAsync</c>.</summary>
    public static IReadOnlyCollection<StudioPermission> For(AppMemberRole role) => role switch
    {
        AppMemberRole.Viewer => Viewer,
        AppMemberRole.Contributor => Contributor,
        AppMemberRole.Operator => Operator,
        AppMemberRole.Admin => Admin,
        _ => Array.Empty<StudioPermission>(),
    };

    // Read-only, no mutations. Carefully ordered by capability so the inheritance reads cleanly.
    public static readonly StudioPermission[] Viewer =
    {
        StudioPermission.ViewApp, StudioPermission.ViewEnvironment, StudioPermission.ViewState,
    };

    public static readonly StudioPermission[] Contributor =
    {
        StudioPermission.ViewApp, StudioPermission.ViewEnvironment, StudioPermission.ViewState,
        StudioPermission.EditApp,
        StudioPermission.AddDatasource, StudioPermission.EditDatasource, StudioPermission.TestDatasource,
        StudioPermission.BuildMigrationPlan,
        StudioPermission.CopyData, StudioPermission.SubsetData,
        StudioPermission.GenerateAppConfig,
    };

    public static readonly StudioPermission[] Operator =
    {
        // Contributor set +
        StudioPermission.ViewApp, StudioPermission.ViewEnvironment, StudioPermission.ViewState,
        StudioPermission.EditApp,
        StudioPermission.AddDatasource, StudioPermission.EditDatasource, StudioPermission.TestDatasource,
        StudioPermission.BuildMigrationPlan,
        StudioPermission.CopyData, StudioPermission.SubsetData,
        StudioPermission.GenerateAppConfig,
        // Operator-only:
        StudioPermission.ApplyMigration, StudioPermission.RollbackMigration,
        StudioPermission.PromoteCode, StudioPermission.PromoteSchema, StudioPermission.PromoteData,
    };

    public static readonly StudioPermission[] Admin =
    {
        // Operator set +
        StudioPermission.ViewApp, StudioPermission.ViewEnvironment, StudioPermission.ViewState,
        StudioPermission.EditApp,
        StudioPermission.AddDatasource, StudioPermission.EditDatasource, StudioPermission.TestDatasource,
        StudioPermission.BuildMigrationPlan,
        StudioPermission.CopyData, StudioPermission.SubsetData,
        StudioPermission.GenerateAppConfig,
        StudioPermission.ApplyMigration, StudioPermission.RollbackMigration,
        StudioPermission.PromoteCode, StudioPermission.PromoteSchema, StudioPermission.PromoteData,
        // Admin-only:
        StudioPermission.DeleteApp, StudioPermission.DeleteDatasource,
        StudioPermission.ApproveRequest, StudioPermission.ManageMembers, StudioPermission.ManagePolicies,
        StudioPermission.MaskedCopyData,
        // All setup permissions (admin can run setup end-to-end):
        StudioPermission.RunSetup, StudioPermission.ProvisionDriver, StudioPermission.ConfigureConnection,
        StudioPermission.ApplySchema, StudioPermission.ApproveMigration, StudioPermission.Seed,
        StudioPermission.Rollback,
    };
}
