// Copyright (c) The Tech Idea. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TheTechIdea.Beep.Studio.Governance;
using TheTechIdea.Beep.Studio.Notifications;

namespace TheTechIdea.Beep.Services.Studio.Notifications;

/// <summary>
/// Stage 5.7: background sweep that expires stale <see cref="ApprovalRequest"/>s and notifies the
/// requester + approvers. Mirrors the BeepBootstrapper hosted-service pattern.
/// </summary>
/// <remarks>
/// <para>
/// Runs every 5 minutes by default. An approval is expired when its age exceeds
/// <see cref="ApprovalTtl"/> (default 7 days). When an approval expires:
/// <list type="bullet">
/// <item>State flips to <see cref="ApprovalState.Expired"/> via <see cref="IGovernanceService"/>.</item>
/// <item>A <see cref="NotificationCategory.ApprovalExpired"/> notification is sent to the requester.</item>
/// </list>
/// </para>
/// <para>
/// <b>Solo mode</b>: when <see cref="INotificationService"/> or <see cref="IGovernanceService"/> is
/// not wired, the service is a no-op (enterprise feature).
/// </para>
/// </remarks>
public sealed class ApprovalExpiryService : BackgroundService
{
    private readonly IGovernanceService _governance;
    private readonly INotificationService _notifications;
    private readonly ILogger<ApprovalExpiryService>? _logger;
    private readonly TimeSpan _sweepInterval;
    private readonly TimeSpan _approvalTtl;

    /// <param name="governance">Required — service is a no-op when not wired (passed as null-safe via the no-op factory).</param>
    /// <param name="notifications">Required — service is a no-op when not wired.</param>
    /// <param name="logger">Optional logger.</param>
    /// <param name="sweepInterval">How often to sweep. Default 5 minutes.</param>
    /// <param name="approvalTtl">When an approval is considered stale. Default 7 days.</param>
    public ApprovalExpiryService(
        IGovernanceService governance,
        INotificationService notifications,
        ILogger<ApprovalExpiryService>? logger = null,
        TimeSpan? sweepInterval = null,
        TimeSpan? approvalTtl = null)
    {
        _governance = governance ?? throw new ArgumentNullException(nameof(governance));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        _logger = logger;
        _sweepInterval = sweepInterval ?? TimeSpan.FromMinutes(5);
        _approvalTtl = approvalTtl ?? TimeSpan.FromDays(7);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger?.LogInformation("ApprovalExpiryService started (sweep={Sweep}, ttl={Ttl}).", _sweepInterval, _approvalTtl);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SweepOnceAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger?.LogWarning(ex, "ApprovalExpiryService sweep failed; will retry next interval.");
            }
            try { await Task.Delay(_sweepInterval, stoppingToken).ConfigureAwait(false); }
            catch (OperationCanceledException) { break; }
        }
        _logger?.LogInformation("ApprovalExpiryService stopped.");
    }

    /// <summary>One sweep pass — public so tests can drive it deterministically without waiting.</summary>
    public async Task SweepOnceAsync(CancellationToken ct = default)
    {
        var pending = await _governance.ListApprovalsAsync(filter: new ApprovalListFilter { State = ApprovalState.Pending }, ct: ct).ConfigureAwait(false);
        if (!pending.IsSuccess || pending.Value == null || pending.Value.Count == 0) return;

        var cutoff = DateTimeOffset.UtcNow - _approvalTtl;
        foreach (var approval in pending.Value.Where(a => a.RequestedAt < cutoff))
        {
            try
            {
                // System-driven expiry via the dedicated ExpireApprovalAsync path — bypasses the
                // user-only validation in DecideApprovalAsync (self-approval check, approver-roles
                // check). This is a system action, not a user decision.
                var expiryComment = $"Automatically expired after {_approvalTtl.TotalDays:0} days with no decision.";
                var result = await _governance.ExpireApprovalAsync(approval.ApprovalId, actor: "system-expiry",
                    comment: expiryComment, ct: ct).ConfigureAwait(false);

                // Notify requester of the expiry only if the expiry actually happened.
                if (result.IsSuccess)
                {
                    await _notifications.SendAsync(new NotificationMessage
                    {
                        Category = NotificationCategory.ApprovalExpired,
                        Severity = NotificationSeverity.Warning,
                        Title = $"Approval expired: {approval.OperationType}",
                        Body = $"Request {approval.ApprovalId} for {approval.OperationType} (plan {approval.PlanHash}) expired without a decision.",
                        RecipientUserId = approval.RequestedBy,
                        DeepLinkKind = "approval",
                        DeepLinkId = approval.ApprovalId,
                    }, ct: ct).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to expire approval {Id}.", approval.ApprovalId);
            }
        }
    }
}
