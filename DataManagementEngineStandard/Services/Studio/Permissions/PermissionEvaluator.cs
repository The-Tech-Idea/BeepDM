// Copyright (c) The Tech Idea. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Studio.Permissions;

namespace TheTechIdea.Beep.Services.Studio.Permissions;

/// <summary>
/// Stage 4: the deny-wins evaluation algorithm, extracted as a pure function so it can be
/// unit-tested in isolation and reused by any <see cref="IStudioAuthorizer"/> impl.
/// </summary>
/// <remarks>
/// <para>
/// <b>Algorithm</b> (explicit-deny-wins — the AWS IAM / GCP IAM model):
/// <list type="number">
/// <item>Filter grants by <c>(userId, action)</c> AND scope match. A grant matches scope when each
/// grant scope field is null OR equals the requested value. So a global grant (all-null scope)
/// matches any request; an app-scoped grant matches only that app; etc.</item>
/// <item>If ANY matching grant is <c>Deny</c> → <c>Deny</c>. Denials are absolute — a more-specific
/// <c>Allow</c> cannot punch through any matching <c>Deny</c>. This is the privilege-escalation
/// prevention rule.</item>
/// <item>Else if ANY matching grant is <c>Allow</c> → <c>Allow</c>.</item>
/// <item>Else <c>Deny</c> (default-deny).</item>
/// </list>
/// </para>
/// <para>
/// <b>Why not "most-specific-wins".</b> A "most-specific grant wins" rule would let an app-scoped
/// Allow override a global Deny — a privilege escalation. The standard model in production RBAC
/// systems (AWS IAM, GCP IAM, Kubernetes RBAC) is "explicit deny is absolute", and that is what
/// admins expect: a regulator's prod-scoped Deny can never be overridden, period. Specificity only
/// matters for narrowing <c>Allow</c> (an app-wide Allow doesn't grant access on unrelated apps).
/// </para>
/// </remarks>
public static class PermissionEvaluator
{
    /// <summary>Evaluate the canonical algorithm against a grant set.</summary>
    public static PermissionDecision Evaluate(
        IEnumerable<PermissionGrant> grants,
        string userId,
        StudioPermission action,
        string? appId,
        string? envId,
        string? datasourceName)
    {
        if (string.IsNullOrEmpty(userId))
            return PermissionDecision.Deny("userId is required");

        // Filter: same user, same action, scope matches.
        var matching = grants
            .Where(g => string.Equals(g.UserId, userId, StringComparison.OrdinalIgnoreCase))
            .Where(g => g.Action == action)
            .Where(g => ScopeMatches(g, appId, envId, datasourceName))
            .ToList();

        if (matching.Count == 0)
            return PermissionDecision.Deny($"No {action} grant for user '{userId}' in scope");

        // Explicit Deny is absolute — any matching Deny wins, regardless of specificity.
        var denies = matching.Where(g => g.Effect == PermissionEffect.Deny).ToList();
        if (denies.Count > 0)
        {
            return PermissionDecision.Deny(
                $"Denied by {denies.Count} explicit Deny grant(s)",
                matched: matching,
                extraReasons: denies.Select(d => $"  Denied: {d.Action} at {DescribeScope(d)} granted by {d.GrantedBy}").ToList());
        }

        // No denies — allow iff any matching grant is Allow.
        var allows = matching.Where(g => g.Effect == PermissionEffect.Allow).ToList();
        if (allows.Count > 0)
        {
            return PermissionDecision.Allow(
                matched: matching,
                reasons: allows.Select(g => $"Allowed: {g.Action} at {DescribeScope(g)} granted by {g.GrantedBy}").ToList());
        }

        // Only neutral / unknown-effect grants present. Default-deny.
        return PermissionDecision.Deny($"No Allow grant for {action} (only neutral grants matched)");
    }

    /// <summary>Specificity rank: more non-null scope fields = more specific.</summary>
    internal static int SpecificityOf(PermissionGrant g)
    {
        var n = 0;
        if (!string.IsNullOrEmpty(g.AppId)) n++;
        if (!string.IsNullOrEmpty(g.EnvId)) n++;
        if (!string.IsNullOrEmpty(g.DatasourceName)) n++;
        return n;
    }

    /// <summary>
    /// A grant matches the requested scope when every grant scope field is null/empty (wildcard) OR
    /// equals the requested value. This makes global grants match any request.
    /// </summary>
    internal static bool ScopeMatches(PermissionGrant g, string? appId, string? envId, string? datasourceName)
    {
        if (!string.IsNullOrEmpty(g.AppId) && !string.Equals(g.AppId, appId, StringComparison.OrdinalIgnoreCase))
            return false;
        if (!string.IsNullOrEmpty(g.EnvId) && !string.Equals(g.EnvId, envId, StringComparison.OrdinalIgnoreCase))
            return false;
        if (!string.IsNullOrEmpty(g.DatasourceName) && !string.Equals(g.DatasourceName, datasourceName, StringComparison.OrdinalIgnoreCase))
            return false;
        return true;
    }

    private static string DescribeScope(PermissionGrant g)
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(g.AppId)) parts.Add($"app={g.AppId}");
        if (!string.IsNullOrEmpty(g.EnvId)) parts.Add($"env={g.EnvId}");
        if (!string.IsNullOrEmpty(g.DatasourceName)) parts.Add($"ds={g.DatasourceName}");
        return parts.Count == 0 ? "global scope" : string.Join(", ", parts);
    }
}
