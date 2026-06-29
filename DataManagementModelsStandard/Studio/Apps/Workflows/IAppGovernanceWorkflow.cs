using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Studio.Apps.Workflows;

/// <summary>
/// Per-app governance: role-based access control, approval gates for protected
/// (production) environments, policy enforcement, and an audit trail. Wraps the
/// engine's <c>IBeepAudit</c> and <c>IGovernanceService</c>, scoped to the app.
/// </summary>
public interface IAppGovernanceWorkflow
{
    // ── RBAC ───────────────────────────────────────────────────────────────
    Task<StudioResult<IReadOnlyList<AppRoleAssignment>>> ListMembersAsync(string appId, CancellationToken ct = default);
    Task<StudioResult<AppRoleAssignment>> AssignRoleAsync(string appId, string userId, AppMemberRole role, CancellationToken ct = default);
    Task<StudioResult<bool>> RevokeRoleAsync(string appId, string userId, CancellationToken ct = default);
    /// <summary>Does this user have the required role on the app? (Also true for admins.)</summary>
    Task<StudioResult<bool>> CanUserAsync(string appId, string userId, AppMemberRole required, CancellationToken ct = default);

    // ── Approval gates ─────────────────────────────────────────────────────
    /// <summary>Open an approval ticket for a protected action (e.g. migrate to prod). Returns the ticket id.</summary>
    Task<StudioResult<ApprovalTicket>> RequestApprovalAsync(string appId, string envId, string action, string requestedBy, string? reason = null, CancellationToken ct = default);
    /// <summary>Approve or deny a ticket. Enforces required approver count for the env tier.</summary>
    Task<StudioResult<ApprovalTicket>> DecideAsync(string appId, string ticketId, bool approved, string decidedBy, string? comment = null, CancellationToken ct = default);
    Task<StudioResult<IReadOnlyList<ApprovalTicket>>> ListApprovalsAsync(string appId, string? envId = null, CancellationToken ct = default);

    // ── Policy ─────────────────────────────────────────────────────────────
    /// <summary>Evaluate whether an action is allowed on an env by policy (tier gates, RBAC, open approvals).</summary>
    Task<StudioResult<PolicyDecision>> EvaluateAsync(string appId, string envId, string action, string userId, CancellationToken ct = default);

    // ── Audit ──────────────────────────────────────────────────────────────
    Task<StudioResult<int>> RecordAuditAsync(string appId, string envId, string action, string userId, string? detail = null, CancellationToken ct = default);
    Task<StudioResult<IReadOnlyList<AppAuditEntry>>> QueryAuditAsync(string appId, string? envId = null, int skip = 0, int take = 100, CancellationToken ct = default);
}

/// <summary>Coarse role for app membership. Maps to fine-grained permissions on the host side.</summary>
public enum AppMemberRole
{
    Viewer = 0,
    Contributor = 1,
    Operator = 2,
    Admin = 3
}

public sealed class AppRoleAssignment
{
    public required string AppId { get; set; }
    public required string UserId { get; set; }
    public AppMemberRole Role { get; set; }
    public DateTimeOffset AssignedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class ApprovalTicket
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..12];
    public required string AppId { get; set; }
    public string? EnvId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string RequestedBy { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public ApprovalState State { get; set; } = ApprovalState.Open;
    public List<string> Approvers { get; set; } = new();
    public int RequiredApprovals { get; set; } = 1;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DecidedAt { get; set; }
    public string? DecidedBy { get; set; }
    public string? Comment { get; set; }
}

public enum ApprovalState { Open = 0, Approved = 1, Denied = 2, Expired = 3 }

public sealed class PolicyDecision
{
    public bool Allowed { get; set; }
    public List<string> Reasons { get; set; } = new();
    public string? OpenTicketId { get; set; }
}

public sealed class AppAuditEntry
{
    public long Sequence { get; set; }
    public required string AppId { get; set; }
    public string? EnvId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public DateTimeOffset At { get; set; }
}
