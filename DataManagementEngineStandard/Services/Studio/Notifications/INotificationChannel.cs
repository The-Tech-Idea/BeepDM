// Copyright (c) The Tech Idea. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Studio.Notifications;

namespace TheTechIdea.Beep.Services.Studio.Notifications;

/// <summary>
/// Stage 5: a delivery channel for notifications (Email, Webhook, SignalR, …). Adapters MUST NOT
/// throw — failures are logged by the caller and the next channel proceeds.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why a new abstraction instead of wrapping the ETL notifiers.</b> The ETL notifiers
/// (<c>EmailNotifier</c>, <c>WebhookNotifier</c>, <c>LogFileNotifier</c> in
/// <c>Editor/ETL/Engine/BuiltIn/Notifiers/</c>) implement <c>IPipelineNotifier</c> and consume a
/// pipeline-specific <c>AlertEvent</c> with pipeline-specific tokens (<c>{PipelineName}</c>,
/// <c>{AlertRule}</c>, …). They are not a clean fit for user-facing notifications like "your approval
/// was requested". The SMTP/webhook *mechanism* is shared in spirit, but the contract is different
/// enough that a thin parallel channel surface is cleaner than wrapping. Future work can reconcile
/// the two under a shared base if the overlap grows.
/// </para>
/// </remarks>
public interface INotificationChannel
{
    NotificationChannel Channel { get; }

    /// <summary>Deliver the message. Must not throw — log failures and return.</summary>
    Task SendAsync(NotificationMessage message, string recipientDisplayName, CancellationToken ct = default);
}
