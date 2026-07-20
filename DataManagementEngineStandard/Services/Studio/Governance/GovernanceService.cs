// Copyright (c) The Tech Idea. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Services.Audit;
using TheTechIdea.Beep.Services.Audit.Models;
using TheTechIdea.Beep.Studio.Governance;
using TheTechIdea.Beep.Studio.Migration;
// PR 17: disambiguate the engine's AuditQuery (class) from the Studio's
// AuditQuery (record, defined in this namespace). The mapper code expects
// the engine shape; without this alias the compiler resolves to the Studio
// record and the property names don't match.
using EngineAuditQuery = TheTechIdea.Beep.Services.Audit.Models.AuditQuery;

namespace TheTechIdea.Beep.Studio.Governance;

/// <summary>
/// Default implementation of <see cref="IGovernanceService"/>. Persists
/// policies to <c>%ProgramData%\TheTechIdea\BeepDMS\governance-policies.json</c>
/// and approvals to <c>%ProgramData%\TheTechIdea\BeepDMS\governance-approvals.json</c>.
/// Wraps the engine's <see cref="IBeepAudit"/> for the audit surface.
/// </summary>
/// <remarks>
/// The engine's <c>AuditEvent</c> is more general than the Studio's
/// <c>StudioAuditEvent</c>; the mapping goes both ways via
/// <see cref="MapFromAuditEvent"/> and <see cref="MapToAuditEvent"/>.
/// The Studio carries extra business-friendly fields (<c>Category</c>,
/// <c>Action</c>, <c>Subject</c>, <c>BeforeJson</c>, <c>AfterJson</c>) that the
/// engine stores in the <c>Properties</c> dictionary.
/// </remarks>
public sealed class GovernanceService : IGovernanceService
{
    private readonly IBeepAudit _audit;
    private readonly string _policiesPath;
    private readonly string _approvalsPath;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    /// <summary>
    /// Stage 5.5/5.6: optional notification fan-out for approval lifecycle events. Null (solo default)
    /// preserves today's behavior — no notifications. Enterprise hosts inject an
    /// <see cref="INotificationService"/> + <see cref="IStudioAuthorizer"/> so approvals reach real users.
    /// </summary>
    private readonly TheTechIdea.Beep.Studio.Notifications.INotificationService? _notifications;
    private readonly TheTechIdea.Beep.Studio.Permissions.IStudioAuthorizer? _authorizer;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public GovernanceService(IBeepAudit? audit = null, string? dataRoot = null)
        : this(audit, dataRoot, notifications: null, authorizer: null) { }

    /// <summary>
    /// Stage 5.5/5.6: enterprise constructor. Notifications and authorizer are optional; when both
    /// are present, approval events fan out to the resolved approvers.
    /// </summary>
    public GovernanceService(
        IBeepAudit? audit,
        string? dataRoot,
        TheTechIdea.Beep.Studio.Notifications.INotificationService? notifications,
        TheTechIdea.Beep.Studio.Permissions.IStudioAuthorizer? authorizer)
    {
        _audit = audit ?? new TheTechIdea.Beep.Services.Audit.NullBeepAudit();
        var root = dataRoot ?? TheTechIdea.Beep.Services.EnvironmentService.CreateAppfolder("BeepDMS");
        _policiesPath = Path.Combine(root, "governance-policies.json");
        _approvalsPath = Path.Combine(root, "governance-approvals.json");
        Directory.CreateDirectory(root);
        _notifications = notifications;
        _authorizer = authorizer;
    }

    // ── Policies ─────────────────────────────────────────────────────────────

    public Task<StudioResult<IReadOnlyList<GovernancePolicy>>> ListPoliciesAsync(CancellationToken ct = default)
    {
        return Task.FromResult(StudioResult<IReadOnlyList<GovernancePolicy>>.Ok(LoadPolicies()));
    }

    public Task<StudioResult<GovernancePolicy>> GetPolicyAsync(string policyId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(policyId))
            return Task.FromResult(StudioResult<GovernancePolicy>.Fail(StudioErrorCode.InvalidArgument, "policyId is required."));
        var p = LoadPolicies().FirstOrDefault(p => p.PolicyId == policyId);
        return Task.FromResult(p == null
            ? StudioResult<GovernancePolicy>.Fail(StudioErrorCode.NotFound, $"Policy '{policyId}' not found.")
            : StudioResult<GovernancePolicy>.Ok(p));
    }

    public Task<StudioResult<GovernancePolicy>> GetPolicyForTierAsync(RolloutTier tier, CancellationToken ct = default)
    {
        // Pick the first policy that matches the tier; otherwise return a sensible default.
        var p = LoadPolicies().FirstOrDefault(p => p.Tier == tier);
        if (p != null) return Task.FromResult(StudioResult<GovernancePolicy>.Ok(p));

        // Fall back to a built-in default.
        var defaultPolicy = tier switch
        {
            RolloutTier.Live => DefaultPolicyForLive(),
            RolloutTier.Staging => DefaultPolicyForStaging(),
            RolloutTier.Test => DefaultPolicyForTest(),
            _ => DefaultPolicyForDev()
        };
        return Task.FromResult(StudioResult<GovernancePolicy>.Ok(defaultPolicy));
    }

    public async Task<StudioResult<GovernancePolicy>> UpsertPolicyAsync(GovernancePolicy policy, CancellationToken ct = default)
    {
        if (policy == null)
            return StudioResult<GovernancePolicy>.Fail(StudioErrorCode.InvalidArgument, "policy is required.");
        if (string.IsNullOrWhiteSpace(policy.PolicyId))
            return StudioResult<GovernancePolicy>.Fail(StudioErrorCode.InvalidArgument, "policy.PolicyId is required.");

        await _writeLock.WaitAsync(ct);
        try
        {
            var policies = LoadPolicies();
            var idx = policies.FindIndex(p => p.PolicyId == policy.PolicyId);
            // Records use init-only setters, so update via `with` expressions.
            var now = DateTimeOffset.UtcNow;
            if (idx < 0)
            {
                policy = policy with { CreatedAt = now, UpdatedAt = now };
                policies.Add(policy);
            }
            else
            {
                policy = policy with { CreatedAt = policies[idx].CreatedAt, UpdatedAt = now };
                policies[idx] = policy;
            }
            await File.WriteAllTextAsync(_policiesPath, JsonSerializer.Serialize(policies, JsonOpts), ct);
            await RecordAuditAsync("Policy", idx < 0 ? "Create" : "Update", $"Policy:{policy.PolicyId}", null, JsonSerializer.Serialize(policy, JsonOpts), ct);
            return StudioResult<GovernancePolicy>.Ok(policy);
        }
        catch (Exception ex)
        {
            return StudioResult<GovernancePolicy>.Fail(StudioErrorCode.InternalError, ex.Message, ex);
        }
        finally { _writeLock.Release(); }
    }

    public async Task<StudioResult<bool>> DeletePolicyAsync(string policyId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(policyId))
            return StudioResult<bool>.Fail(StudioErrorCode.InvalidArgument, "policyId is required.");

        await _writeLock.WaitAsync(ct);
        try
        {
            var policies = LoadPolicies();
            var p = policies.FirstOrDefault(x => x.PolicyId == policyId);
            if (p == null) return StudioResult<bool>.Fail(StudioErrorCode.NotFound, "Policy not found.");
            policies.Remove(p);
            await File.WriteAllTextAsync(_policiesPath, JsonSerializer.Serialize(policies, JsonOpts), ct);
            await RecordAuditAsync("Policy", "Delete", $"Policy:{policyId}", JsonSerializer.Serialize(p, JsonOpts), null, ct);
            return StudioResult<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            return StudioResult<bool>.Fail(StudioErrorCode.InternalError, ex.Message, ex);
        }
        finally { _writeLock.Release(); }
    }

    // ── Approval workflow ───────────────────────────────────────────────────

    public Task<StudioResult<PolicyEvaluationResult>> EvaluateRequestAsync(ApprovalRequest request, CancellationToken ct = default)
    {
        if (request == null) return Task.FromResult(StudioResult<PolicyEvaluationResult>.Fail(StudioErrorCode.InvalidArgument, "request is required."));

        return GetPolicyForTierAsync(request.Tier).ContinueWith(policyTask =>
        {
            if (!policyTask.Result.IsSuccess)
                return StudioResult<PolicyEvaluationResult>.Fail(policyTask.Result.Error.Code, policyTask.Result.Error.Message);

            var policy = policyTask.Result.Value!;
            var violations = new List<PolicyViolation>();

            if (policy.RequireApprover && string.IsNullOrWhiteSpace(request.RequestedBy))
                violations.Add(new PolicyViolation("ApproverRequired", "Approval requires a requestor.", "Block"));

            if (policy.BlockedOperations?.Count > 0 && policy.BlockedOperations.Contains(request.OperationType))
                violations.Add(new PolicyViolation("OperationBlocked", $"Operation '{request.OperationType}' is blocked by the {policy.Name} policy.", "Block"));

            if (request.Tier == RolloutTier.Live && !policy.RequireApprover)
                violations.Add(new PolicyViolation("LiveRequiresApprover", "Live tier requires an approver.", "Warn"));

            return StudioResult<PolicyEvaluationResult>.Ok(new PolicyEvaluationResult(
                IsAllowed: violations.All(v => v.Severity != "Block"),
                Violations: violations));
        }, ct);
    }

    public async Task<StudioResult<ApprovalRequest>> RequestApprovalAsync(ApprovalRequest request, CancellationToken ct = default)
    {
        if (request == null) return StudioResult<ApprovalRequest>.Fail(StudioErrorCode.InvalidArgument, "request is required.");
        if (string.IsNullOrWhiteSpace(request.OperationType))
            return StudioResult<ApprovalRequest>.Fail(StudioErrorCode.InvalidArgument, "request.OperationType is required.");
        if (string.IsNullOrWhiteSpace(request.PlanHash))
            return StudioResult<ApprovalRequest>.Fail(StudioErrorCode.InvalidArgument, "request.PlanHash is required.");

        var evaluation = await EvaluateRequestAsync(request, ct);
        if (!evaluation.IsSuccess)
            return StudioResult<ApprovalRequest>.Fail(evaluation.Error.Code, evaluation.Error.Message);
        if (!evaluation.Value!.IsAllowed)
            return StudioResult<ApprovalRequest>.Fail(StudioErrorCode.PolicyViolation, "Request is blocked by policy.", null, BuildDetails(evaluation.Value));

        await _writeLock.WaitAsync(ct);
        try
        {
            var approvals = LoadApprovals();
            var id = string.IsNullOrWhiteSpace(request.ApprovalId) ? Guid.NewGuid().ToString("N") : request.ApprovalId;
            var withId = request with { ApprovalId = id, State = ApprovalState.Pending, DecidedAt = null };
            approvals.Add(withId);
            await File.WriteAllTextAsync(_approvalsPath, JsonSerializer.Serialize(approvals, JsonOpts), ct);
            await RecordAuditAsync("Approval", "Request", $"Approval:{id}", null, JsonSerializer.Serialize(withId, JsonOpts), ct);

            // Stage 5.5: fan-out to approvers (enterprise only). Best-effort — never fails the request.
            await NotifyApproversAsync(withId, ct).ConfigureAwait(false);

            return StudioResult<ApprovalRequest>.Ok(withId);
        }
        catch (Exception ex)
        {
            return StudioResult<ApprovalRequest>.Fail(StudioErrorCode.InternalError, ex.Message, ex);
        }
        finally { _writeLock.Release(); }
    }

    public async Task<StudioResult<ApprovalRequest>> DecideApprovalAsync(string approvalId, ApprovalDecision decision, string decider, string? comment = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(approvalId)) return StudioResult<ApprovalRequest>.Fail(StudioErrorCode.InvalidArgument, "approvalId is required.");
        if (string.IsNullOrWhiteSpace(decider)) return StudioResult<ApprovalRequest>.Fail(StudioErrorCode.InvalidArgument, "decider is required.");

        await _writeLock.WaitAsync(ct);
        try
        {
            var approvals = LoadApprovals();
            var idx = approvals.FindIndex(a => a.ApprovalId == approvalId);
            if (idx < 0) return StudioResult<ApprovalRequest>.Fail(StudioErrorCode.NotFound, "Approval not found.");

            var current = approvals[idx];
            if (current.RequestedBy == decider)
                return StudioResult<ApprovalRequest>.Fail(StudioErrorCode.PermissionDenied, "A user cannot decide on their own approval request.");

            var policyResult = await GetPolicyForTierAsync(current.Tier, ct);
            if (!policyResult.IsSuccess) return StudioResult<ApprovalRequest>.Fail(policyResult.Error.Code, policyResult.Error.Message);
            var policy = policyResult.Value!;

            // Check approver role (v1: any non-empty decider passes; a future PR can wire the role list)
            if (policy.AllowedApproverRoles?.Count > 0 && decider.Contains("@") == false)
                return StudioResult<ApprovalRequest>.Fail(StudioErrorCode.PermissionDenied, "Decider is not in any of the allowed approver roles.");

            var decisions = (current.Decisions ?? new List<ApprovalDecision>()).ToList();
            decisions.Add(new ApprovalDecision(decider, DateTimeOffset.UtcNow, decision.Approved, comment));

            // Aggregate: if any decision rejects, the request is Rejected.
            // If all required approvers have approved, the request is Approved.
            ApprovalState newState;
            if (decisions.Any(d => !d.Approved))
                newState = ApprovalState.Rejected;
            else if (decisions.Count(d => d.Approved) >= policy.RequiredApproverCount)
                newState = ApprovalState.Approved;
            else
                newState = ApprovalState.Pending;

            var updated = current with
            {
                Decisions = decisions,
                State = newState,
                DecidedAt = newState != ApprovalState.Pending ? DateTimeOffset.UtcNow : null
            };
            approvals[idx] = updated;
            await File.WriteAllTextAsync(_approvalsPath, JsonSerializer.Serialize(approvals, JsonOpts), ct);
            await RecordAuditAsync("Approval", newState.ToString(), $"Approval:{approvalId}", JsonSerializer.Serialize(current, JsonOpts), JsonSerializer.Serialize(updated, JsonOpts), ct);

            // Stage 5.6: notify the requester of the decision (enterprise only). Best-effort.
            await NotifyRequesterOfDecisionAsync(updated, ct).ConfigureAwait(false);

            return StudioResult<ApprovalRequest>.Ok(updated);
        }
        catch (Exception ex)
        {
            return StudioResult<ApprovalRequest>.Fail(StudioErrorCode.InternalError, ex.Message, ex);
        }
        finally { _writeLock.Release(); }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Stage 5.7: bypasses the user-only validation paths in <see cref="DecideApprovalAsync"/> — this
    /// is a system action (expiry sweep or admin force-decide), not a user decision. Self-approval
    /// check and allowed-approver-roles check are skipped; the state flips to <see cref="ApprovalState.Expired"/>.
    /// </remarks>
    public async Task<StudioResult<ApprovalRequest>> ExpireApprovalAsync(string approvalId, string actor, string? comment = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(approvalId)) return StudioResult<ApprovalRequest>.Fail(StudioErrorCode.InvalidArgument, "approvalId is required.");
        if (string.IsNullOrWhiteSpace(actor)) return StudioResult<ApprovalRequest>.Fail(StudioErrorCode.InvalidArgument, "actor is required.");

        await _writeLock.WaitAsync(ct);
        try
        {
            var approvals = LoadApprovals();
            var idx = approvals.FindIndex(a => a.ApprovalId == approvalId);
            if (idx < 0) return StudioResult<ApprovalRequest>.Fail(StudioErrorCode.NotFound, "Approval not found.");

            var current = approvals[idx];
            if (current.State != ApprovalState.Pending)
                return StudioResult<ApprovalRequest>.Fail(StudioErrorCode.InvalidArgument, $"Ticket already {current.State}.");

            var decisions = (current.Decisions ?? new List<ApprovalDecision>()).ToList();
            decisions.Add(new ApprovalDecision(actor, DateTimeOffset.UtcNow, Approved: false, Comment: comment ?? "Expired."));

            var updated = current with
            {
                Decisions = decisions,
                State = ApprovalState.Expired,
                DecidedAt = DateTimeOffset.UtcNow,
            };
            approvals[idx] = updated;
            await File.WriteAllTextAsync(_approvalsPath, JsonSerializer.Serialize(approvals, JsonOpts), ct);
            await RecordAuditAsync("Approval", "Expired", $"Approval:{approvalId}", JsonSerializer.Serialize(current, JsonOpts), JsonSerializer.Serialize(updated, JsonOpts), ct);

            // Stage 5.6: notify the requester of the expiry (enterprise only). Best-effort.
            await NotifyRequesterOfDecisionAsync(updated, ct).ConfigureAwait(false);

            return StudioResult<ApprovalRequest>.Ok(updated);
        }
        catch (Exception ex)
        {
            return StudioResult<ApprovalRequest>.Fail(StudioErrorCode.InternalError, ex.Message, ex);
        }
        finally { _writeLock.Release(); }
    }

    public Task<StudioResult<IReadOnlyList<ApprovalRequest>>> ListApprovalsAsync(ApprovalListFilter? filter = null, CancellationToken ct = default)
    {
        var all = LoadApprovals();
        IEnumerable<ApprovalRequest> q = all;
        if (filter?.State.HasValue == true) q = q.Where(a => a.State == filter.State.Value);
        if (filter?.Tier.HasValue == true) q = q.Where(a => a.Tier == filter.Tier.Value);
        if (!string.IsNullOrWhiteSpace(filter?.OperationType)) q = q.Where(a => a.OperationType == filter.OperationType);
        if (!string.IsNullOrWhiteSpace(filter?.RequestedBy)) q = q.Where(a => a.RequestedBy == filter.RequestedBy);
        if (!string.IsNullOrWhiteSpace(filter?.Decider)) q = q.Where(a => a.Decisions?.Any(d => d.Decider == filter.Decider) == true);
        if (filter?.Since.HasValue == true) q = q.Where(a => a.RequestedAt >= filter.Since.Value);
        if (filter?.Until.HasValue == true) q = q.Where(a => a.RequestedAt <= filter.Until.Value);
        var list = q.OrderByDescending(a => a.RequestedAt).Skip(filter?.Skip ?? 0).Take(filter?.Take ?? 100).ToList();
        return Task.FromResult(StudioResult<IReadOnlyList<ApprovalRequest>>.Ok(list));
    }

    // ── Audit ────────────────────────────────────────────────────────────────

    public async Task<StudioResult<string>> RecordAuditAsync(StudioAuditEvent evt, CancellationToken ct = default)
    {
        if (evt == null) return StudioResult<string>.Fail(StudioErrorCode.InvalidArgument, "evt is required.");
        if (!_audit.IsEnabled) return StudioResult<string>.Ok("(audit disabled)");

        try
        {
            var engineEvent = MapToAuditEvent(evt);
            await _audit.RecordAsync(engineEvent, ct);
            return StudioResult<string>.Ok(evt.CorrelationId ?? evt.Subject ?? Guid.NewGuid().ToString("N"));
        }
        catch (Exception ex)
        {
            return StudioResult<string>.Fail(StudioErrorCode.AuditFailed, ex.Message, ex);
        }
    }

    public async Task<StudioResult<IReadOnlyList<StudioAuditEvent>>> QueryAuditAsync(AuditQuery query, CancellationToken ct = default)
    {
        if (!_audit.IsEnabled) return StudioResult<IReadOnlyList<StudioAuditEvent>>.Ok(Array.Empty<StudioAuditEvent>());
        try
        {
            var engineQuery = new EngineAuditQuery
            {
                FromUtc = query.Since?.UtcDateTime,
                ToUtc = query.Until?.UtcDateTime,
                Source = query.Category,
                EntityName = query.Subject,
                RecordKey = query.CorrelationId,
                UserId = query.Actor,
                Take = query.Take
            };
            var results = await _audit.QueryAsync(engineQuery, ct);
            var vms = results.Select(MapFromAuditEvent).ToList();
            return StudioResult<IReadOnlyList<StudioAuditEvent>>.Ok(vms);
        }
        catch (Exception ex)
        {
            return StudioResult<IReadOnlyList<StudioAuditEvent>>.Fail(StudioErrorCode.AuditFailed, ex.Message, ex);
        }
    }

    public async Task<StudioResult<AuditIntegrityReport>> VerifyAuditIntegrityAsync(CancellationToken ct = default)
    {
        if (!_audit.IsEnabled)
            return StudioResult<AuditIntegrityReport>.Ok(new AuditIntegrityReport(true, 0, Array.Empty<AuditIntegrityIssue>()));
        try
        {
            var intact = await _audit.VerifyIntegrityAsync(ct);
            return StudioResult<AuditIntegrityReport>.Ok(new AuditIntegrityReport(intact, 0, Array.Empty<AuditIntegrityIssue>()));
        }
        catch (Exception ex)
        {
            return StudioResult<AuditIntegrityReport>.Fail(StudioErrorCode.AuditFailed, ex.Message, ex);
        }
    }

    // ── Private helpers ─────────────────────────────────────────────────────

    private List<GovernancePolicy> LoadPolicies()
    {
        if (!File.Exists(_policiesPath)) return new List<GovernancePolicy>();
        try
        {
            var json = File.ReadAllText(_policiesPath);
            return JsonSerializer.Deserialize<List<GovernancePolicy>>(json, JsonOpts) ?? new List<GovernancePolicy>();
        }
        catch
        {
            return new List<GovernancePolicy>();
        }
    }

    private List<ApprovalRequest> LoadApprovals()
    {
        if (!File.Exists(_approvalsPath)) return new List<ApprovalRequest>();
        try
        {
            var json = File.ReadAllText(_approvalsPath);
            return JsonSerializer.Deserialize<List<ApprovalRequest>>(json, JsonOpts) ?? new List<ApprovalRequest>();
        }
        catch
        {
            return new List<ApprovalRequest>();
        }
    }

    private Task RecordAuditAsync(string category, string action, string subject, string? beforeJson, string? afterJson, CancellationToken ct)
    {
        var evt = new StudioAuditEvent(
            Seq: null,
            At: DateTimeOffset.UtcNow,
            Actor: Environment.UserName,
            Category: category,
            Action: action,
            Subject: subject,
            BeforeJson: beforeJson,
            AfterJson: afterJson,
            CorrelationId: null,
            Notes: null);
        return RecordAuditAsync(evt, ct);
    }

    private static AuditEvent MapToAuditEvent(StudioAuditEvent e)
    {
        return new AuditEvent
        {
            Source = e.Category,
            EntityName = e.Subject,
            RecordKey = e.CorrelationId,
            UserId = e.Actor,
            UserName = e.Actor,
            TimestampUtc = e.At.UtcDateTime,
            CorrelationId = e.CorrelationId,
            Properties = new Dictionary<string, object>
            {
                ["category"] = e.Category,
                ["action"] = e.Action,
                ["subject"] = e.Subject,
                ["notes"] = e.Notes ?? string.Empty,
                ["before"] = e.BeforeJson ?? string.Empty,
                ["after"] = e.AfterJson ?? string.Empty
            }
        };
    }

    private static StudioAuditEvent MapFromAuditEvent(AuditEvent e)
    {
        string category = e.Source ?? string.Empty;
        string action = string.Empty;
        string subject = e.EntityName ?? string.Empty;
        string? beforeJson = null, afterJson = null, notes = null;
        if (e.Properties != null)
        {
            if (e.Properties.TryGetValue("category", out var cat)) category = cat?.ToString() ?? category;
            if (e.Properties.TryGetValue("action", out var act)) action = act?.ToString() ?? string.Empty;
            if (e.Properties.TryGetValue("subject", out var sub)) subject = sub?.ToString() ?? subject;
            if (e.Properties.TryGetValue("before", out var bj)) beforeJson = bj?.ToString();
            if (e.Properties.TryGetValue("after", out var aj)) afterJson = aj?.ToString();
            if (e.Properties.TryGetValue("notes", out var n)) notes = n?.ToString();
        }
        return new StudioAuditEvent(
            Seq: null,
            At: new DateTimeOffset(e.TimestampUtc, TimeSpan.Zero),
            Actor: e.UserId ?? e.UserName ?? string.Empty,
            Category: category,
            Action: action,
            Subject: subject,
            BeforeJson: beforeJson,
            AfterJson: afterJson,
            CorrelationId: e.CorrelationId,
            Notes: notes);
    }

    private static IReadOnlyDictionary<string, object?>? BuildDetails(PolicyEvaluationResult r)
    {
        return new Dictionary<string, object?>
        {
            ["violations"] = r.Violations.Select(v => new { v.Code, v.Message, v.Severity }).ToList()
        };
    }

    // Built-in defaults (returned when no policy file exists for the requested tier)

    private static GovernancePolicy DefaultPolicyForDev() => new(
        PolicyId: "default-dev", Name: "Default (Dev)", Tier: RolloutTier.Dev,
        RequireApprover: false, RequiredApproverCount: 0,
        AllowedApproverRoles: new List<string>(), BlockedOperations: new List<string>(),
        CooldownBetweenRuns: null, RequireDryRunOnApply: false, RequirePreflightOnApply: false,
        MaxRowsAffectedPerRun: 1_000_000,
        CreatedAt: DateTimeOffset.UtcNow, UpdatedAt: DateTimeOffset.UtcNow);

    private static GovernancePolicy DefaultPolicyForTest() => new(
        PolicyId: "default-test", Name: "Default (Test)", Tier: RolloutTier.Test,
        RequireApprover: false, RequiredApproverCount: 0,
        AllowedApproverRoles: new List<string>(), BlockedOperations: new List<string>(),
        CooldownBetweenRuns: null, RequireDryRunOnApply: true, RequirePreflightOnApply: true,
        MaxRowsAffectedPerRun: 1_000_000,
        CreatedAt: DateTimeOffset.UtcNow, UpdatedAt: DateTimeOffset.UtcNow);

    private static GovernancePolicy DefaultPolicyForStaging() => new(
        PolicyId: "default-staging", Name: "Default (Staging)", Tier: RolloutTier.Staging,
        RequireApprover: true, RequiredApproverCount: 1,
        AllowedApproverRoles: new List<string> { "DBA", "Architect" }, BlockedOperations: new List<string> { "TruncateEntity" },
        CooldownBetweenRuns: TimeSpan.FromMinutes(5), RequireDryRunOnApply: true, RequirePreflightOnApply: true,
        MaxRowsAffectedPerRun: 1_000_000,
        CreatedAt: DateTimeOffset.UtcNow, UpdatedAt: DateTimeOffset.UtcNow);

    private static GovernancePolicy DefaultPolicyForLive() => new(
        PolicyId: "default-live", Name: "Default (Live-Locked)", Tier: RolloutTier.Live,
        RequireApprover: true, RequiredApproverCount: 2,
        AllowedApproverRoles: new List<string> { "DBA", "Architect" },
        BlockedOperations: new List<string> { "DropEntity", "TruncateEntity", "DropColumn" },
        CooldownBetweenRuns: TimeSpan.FromMinutes(30), RequireDryRunOnApply: true, RequirePreflightOnApply: true,
        MaxRowsAffectedPerRun: 1_000_000,
        CreatedAt: DateTimeOffset.UtcNow, UpdatedAt: DateTimeOffset.UtcNow);

    // ─── Stage 5.5/5.6 notification fan-out (best-effort) ──────────────────────

    /// <summary>
    /// Stage 5.5: notify everyone who can approve the operation. Uses
    /// <see cref="IStudioAuthorizer.ResolveActorsAsync"/> when an authorizer is wired (enterprise);
    /// otherwise no-op (solo default — the local admin sees the request in their own inbox anyway).
    /// </summary>
    private async Task NotifyApproversAsync(ApprovalRequest approval, CancellationToken ct)
    {
        if (_notifications == null) return;
        try
        {
            IReadOnlyList<string> approverIds = _authorizer != null
                ? await _authorizer.ResolveActorsAsync(
                    TheTechIdea.Beep.Studio.Permissions.StudioPermission.ApproveRequest,
                    appId: null, envId: null, ct: ct).ConfigureAwait(false)
                : Array.Empty<string>();

            foreach (var approverId in approverIds)
            {
                // Don't notify the requester of their own request — they already know.
                if (string.Equals(approverId, approval.RequestedBy, StringComparison.OrdinalIgnoreCase)) continue;

                await _notifications.SendAsync(new TheTechIdea.Beep.Studio.Notifications.NotificationMessage
                {
                    Category = TheTechIdea.Beep.Studio.Notifications.NotificationCategory.ApprovalRequested,
                    Severity = TheTechIdea.Beep.Studio.Notifications.NotificationSeverity.Warning,
                    Title = $"Approval needed: {approval.OperationType} ({approval.Tier})",
                    Body = $"PlanHash: {approval.PlanHash}. Requested by {approval.RequestedBy}.",
                    RecipientUserId = approverId,
                    DeepLinkKind = "approval",
                    DeepLinkId = approval.ApprovalId,
                }, ct: ct).ConfigureAwait(false);
            }
        }
        catch { /* best-effort — never fail an approval request on a notification problem */ }
    }

    /// <summary>Stage 5.6: notify the requester that a decision has been made.</summary>
    private async Task NotifyRequesterOfDecisionAsync(ApprovalRequest updated, CancellationToken ct)
    {
        if (_notifications == null) return;
        if (updated.State == ApprovalState.Pending) return;  // no terminal decision yet
        try
        {
            await _notifications.SendAsync(new TheTechIdea.Beep.Studio.Notifications.NotificationMessage
            {
                Category = TheTechIdea.Beep.Studio.Notifications.NotificationCategory.ApprovalDecided,
                Severity = updated.State == ApprovalState.Approved
                    ? TheTechIdea.Beep.Studio.Notifications.NotificationSeverity.Success
                    : TheTechIdea.Beep.Studio.Notifications.NotificationSeverity.Warning,
                Title = $"Approval {updated.State}: {updated.OperationType}",
                Body = $"Your request for {updated.OperationType} (plan {updated.PlanHash}) was {updated.State.ToString().ToLowerInvariant()}.",
                RecipientUserId = updated.RequestedBy,
                DeepLinkKind = "approval",
                DeepLinkId = updated.ApprovalId,
            }, ct: ct).ConfigureAwait(false);
        }
        catch { /* best-effort */ }
    }
}
