// Copyright (c) The Tech Idea. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Studio.Migration;

namespace TheTechIdea.Beep.Studio.Governance;

/// <summary>
/// Governance: policies, approvals, and audit. Implemented in Phase 7.
/// Wraps the engine's existing <c>IBeepAudit</c> pipeline (hash-chained,
/// tamper-evident) with a <see cref="StudioResult{T}"/>-shaped surface, an
/// approval workflow, and a redaction policy for sensitive fields.
/// </summary>
public interface IGovernanceService
{
    // ---- policies ----
    Task<StudioResult<IReadOnlyList<GovernancePolicy>>> ListPoliciesAsync(CancellationToken ct = default);
    Task<StudioResult<GovernancePolicy>> GetPolicyAsync(string policyId, CancellationToken ct = default);
    Task<StudioResult<GovernancePolicy>> GetPolicyForTierAsync(RolloutTier tier, CancellationToken ct = default);
    Task<StudioResult<GovernancePolicy>> UpsertPolicyAsync(GovernancePolicy policy, CancellationToken ct = default);
    Task<StudioResult<bool>> DeletePolicyAsync(string policyId, CancellationToken ct = default);

    // ---- approval workflow ----
    Task<StudioResult<PolicyEvaluationResult>> EvaluateRequestAsync(ApprovalRequest request, CancellationToken ct = default);
    Task<StudioResult<ApprovalRequest>> RequestApprovalAsync(ApprovalRequest request, CancellationToken ct = default);
    Task<StudioResult<ApprovalRequest>> DecideApprovalAsync(string approvalId, ApprovalDecision decision, string decider, string? comment = null, CancellationToken ct = default);
    Task<StudioResult<IReadOnlyList<ApprovalRequest>>> ListApprovalsAsync(ApprovalListFilter? filter = null, CancellationToken ct = default);

    // ---- audit ----
    Task<StudioResult<string>> RecordAuditAsync(StudioAuditEvent evt, CancellationToken ct = default);
    Task<StudioResult<IReadOnlyList<StudioAuditEvent>>> QueryAuditAsync(AuditQuery query, CancellationToken ct = default);
    Task<StudioResult<AuditIntegrityReport>> VerifyAuditIntegrityAsync(CancellationToken ct = default);
}

/// <summary>A governance policy. Drives the approval workflow and the audit policy.</summary>
public sealed record GovernancePolicy(
    string PolicyId,
    string Name,
    RolloutTier Tier,
    bool RequireApprover,
    int RequiredApproverCount,
    IReadOnlyList<string> AllowedApproverRoles,
    IReadOnlyList<string> BlockedOperations,
    TimeSpan? CooldownBetweenRuns,
    bool RequireDryRunOnApply,
    bool RequirePreflightOnApply,
    int MaxRowsAffectedPerRun,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>An approval request.</summary>
public sealed record ApprovalRequest(
    string ApprovalId,
    string OperationType,
    string OperationSubjectId,
    string OperationSubjectJson,
    string PlanHash,
    RolloutTier Tier,
    string RequestedBy,
    DateTimeOffset RequestedAt,
    IReadOnlyList<ApprovalDecision> Decisions,
    ApprovalState State,
    DateTimeOffset? DecidedAt);

/// <summary>A single approver's decision.</summary>
public sealed record ApprovalDecision(
    string Decider,
    DateTimeOffset DecidedAt,
    bool Approved,
    string? Comment);

/// <summary>The state of an approval request.</summary>
public enum ApprovalState
{
    /// <summary>Pending. Awaiting one or more approvers.</summary>
    Pending = 0,

    /// <summary>Approved. The required approver count was met.</summary>
    Approved = 1,

    /// <summary>Rejected. At least one approver rejected the request.</summary>
    Rejected = 2,

    /// <summary>Withdrawn by the requestor before the decision was made.</summary>
    Withdrawn = 3,

    /// <summary>Expired without a decision.</summary>
    Expired = 4
}

/// <summary>Filter for <see cref="IGovernanceService.ListApprovalsAsync"/>.</summary>
public sealed record ApprovalListFilter(
    ApprovalState? State = null,
    RolloutTier? Tier = null,
    string? OperationType = null,
    string? RequestedBy = null,
    string? Decider = null,
    DateTimeOffset? Since = null,
    DateTimeOffset? Until = null,
    int Skip = 0,
    int Take = 100);

/// <summary>An audit event recorded by the Studio. Wraps the engine's <c>TelemetryEnvelope</c>.</summary>
public sealed record StudioAuditEvent(
    long? Seq,
    DateTimeOffset At,
    string Actor,
    string Category,
    string Action,
    string Subject,
    string? BeforeJson,
    string? AfterJson,
    string? CorrelationId,
    string? Notes);

/// <summary>Query for the audit log.</summary>
public sealed record AuditQuery(
    string? Actor = null,
    string? Category = null,
    string? Action = null,
    string? Subject = null,
    string? CorrelationId = null,
    DateTimeOffset? Since = null,
    DateTimeOffset? Until = null,
    int Skip = 0,
    int Take = 100);

/// <summary>The result of an integrity check on the audit log.</summary>
public sealed record AuditIntegrityReport(
    bool IsIntact,
    int VerifiedEvents,
    IReadOnlyList<AuditIntegrityIssue> Issues);

/// <summary>A single integrity issue.</summary>
public sealed record AuditIntegrityIssue(
    long Seq,
    string Reason);
