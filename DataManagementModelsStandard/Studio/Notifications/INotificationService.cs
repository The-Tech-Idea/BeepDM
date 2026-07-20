// Copyright (c) The Tech Idea. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Studio.Notifications;

/// <summary>Where a notification should be delivered.</summary>
public enum NotificationChannel
{
    /// <summary>Persisted inbox (always on — every notification is recorded here regardless of other channels).</summary>
    Inbox = 0,
    Email = 1,
    Webhook = 2,
    /// <summary>Real-time push via SignalR (host-optional).</summary>
    SignalR = 3,
}

/// <summary>How loud the notification should be in the UI.</summary>
public enum NotificationSeverity
{
    Info = 0,
    Success = 1,
    Warning = 2,
    Error = 3,
}

/// <summary>
/// Why the notification exists. Drives the icon, the default severity, the deep-link kind, and
/// subscription filtering. Add new values at the end.
/// </summary>
public enum NotificationCategory
{
    ApprovalRequested = 0,
    ApprovalDecided = 1,
    ApprovalExpired = 2,

    MigrationCompleted = 10,
    MigrationFailed = 11,
    DataCopyCompleted = 12,
    DataCopyFailed = 13,
    PromotionCompleted = 14,
    PromotionFailed = 15,

    MembershipChanged = 20,
    PolicyChanged = 21,
    SetupLifecycle = 22,
    System = 99,
}

/// <summary>
/// A single notification addressed to one user (or a role broadcast). Always persisted to the inbox;
/// other channels are added by the service based on the recipient's <see cref="NotificationSubscription"/>s.
/// </summary>
public sealed class NotificationMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..16];
    public NotificationCategory Category { get; set; }
    public NotificationSeverity Severity { get; set; } = NotificationSeverity.Info;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    /// <summary>Recipient user id. Null when <see cref="RecipientRole"/> is set (role broadcast).</summary>
    public string? RecipientUserId { get; set; }
    /// <summary>Recipient role name (broadcast to all users with this role). Null for direct messages.</summary>
    public string? RecipientRole { get; set; }

    public string? AppId { get; set; }
    public string? EnvId { get; set; }

    /// <summary>What the notification links to — used by the host router (Stage 6.10) to deep-link.</summary>
    public string? DeepLinkKind { get; set; }
    public string? DeepLinkId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsRead { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
}

/// <summary>
/// Per-user subscription: "I want <c>MigrationCompleted</c> for app <c>X</c> delivered to <c>Email</c>."
/// The inbox is always written; subscriptions only add extra channels.
/// </summary>
public sealed class NotificationSubscription
{
    public string UserId { get; set; } = string.Empty;
    public NotificationCategory Category { get; set; }
    public string? AppId { get; set; }
    public NotificationChannel Channel { get; set; }
}

/// <summary>
/// Stage 5: the unified notification service. Persists every message to an inbox, then fans out to
/// the channels the recipient has subscribed to. Solo mode: inbox only (the local admin sees everything).
/// </summary>
/// <remarks>
/// <para>
/// <b>Always-on inbox.</b> Regardless of subscriptions, every notification is written to the recipient's
/// inbox. Subscriptions only add extra channels. This means a fresh host with no subscriptions still
/// surfaces everything in the inbox UI — no "why didn't anyone tell me?" surprises.
/// </para>
/// <para>
/// <b>Best-effort fan-out.</b> Channel adapters MUST NOT throw — failures are logged and the next
/// channel proceeds. The inbox write is the only one whose failure aborts the send.
/// </para>
/// <para>
/// <b>Recipients.</b> <see cref="SendAsync"/> resolves <see cref="NotificationMessage.RecipientRole"/>
/// broadcasts via the optional <c>IIdentityStore</c>/<c>IStudioAuthorizer</c> (provided at impl
/// construction time) into per-user inbox writes. Direct messages (<see cref="RecipientUserId"/> set)
/// skip the lookup.
/// </para>
/// </remarks>
public interface INotificationService
{
    /// <summary>
    /// Send a message. Inbox is always written; other channels are added based on the recipient's
    /// subscriptions. <paramref name="channels"/> overrides subscriptions (explicit one-off send).
    /// </summary>
    Task SendAsync(NotificationMessage message, IReadOnlyList<NotificationChannel>? channels = null, CancellationToken ct = default);

    /// <summary>Inbox for a user. Newest first. <paramref name="unreadOnly"/> filters unread.</summary>
    Task<IReadOnlyList<NotificationMessage>> GetInboxAsync(string userId, bool unreadOnly = false, CancellationToken ct = default);

    Task MarkReadAsync(string userId, IReadOnlyCollection<string> messageIds, CancellationToken ct = default);
    Task MarkAllReadAsync(string userId, CancellationToken ct = default);

    Task SubscribeAsync(string userId, NotificationSubscription subscription, CancellationToken ct = default);
    Task UnsubscribeAsync(string userId, NotificationSubscription subscription, CancellationToken ct = default);
    Task<IReadOnlyList<NotificationSubscription>> GetSubscriptionsAsync(string userId, CancellationToken ct = default);
}
