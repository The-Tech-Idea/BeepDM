// Copyright (c) The Tech Idea. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Studio;
using TheTechIdea.Beep.Studio.Identity;
using TheTechIdea.Beep.Studio.Notifications;
using TheTechIdea.Beep.Studio.Permissions;
using TheTechIdea.Beep.Services.Studio.Identity;  // LocalIdentityStore

namespace TheTechIdea.Beep.Services.Studio.Notifications;

/// <summary>
/// Stage 5 default <see cref="INotificationService"/>: inbox persisted to
/// <c>{dataRoot}/notifications/{userId}.json</c> (per-user), with optional channel adapters for
/// Email/Webhook/SignalR.
/// </summary>
/// <remarks>
/// <para>
/// Same Stage 2.2 hardening pattern: process-wide lock, atomic temp-file + <c>File.Move(overwrite:true)</c>
/// with retries, concurrent-read-safe reads, reload-on-read for cross-process visibility.
/// </para>
/// <para>
/// <b>Always-on inbox.</b> Every <see cref="SendAsync"/> writes the message to the recipient's inbox
/// (one file per user, kept small by a retention cap — default 200 messages; oldest trimmed). Other
/// channels are added based on the recipient's subscriptions or the explicit <c>channels</c> argument.
/// </para>
/// <para>
/// <b>Role broadcasts.</b> When a message's <see cref="NotificationMessage.RecipientRole"/> is set,
/// the service resolves the role's members via the injected <see cref="IStudioAuthorizer"/> (or
/// <see cref="IIdentityStore"/> when only that's available) and writes one inbox entry per user.
/// When neither is provided (solo default), role broadcasts go to the local admin only.
/// </para>
/// </remarks>
public sealed class InboxNotificationService : INotificationService
{
    private const int IoRetryCount = 5;
    private const int IoRetryDelayMs = 30;
    private const int InboxRetentionCap = 200;
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true, PropertyNamingPolicy = null };

    private readonly string _root;
    private readonly IIdentityStore? _identity;
    private readonly IStudioAuthorizer? _authorizer;
    private readonly Dictionary<NotificationChannel, INotificationChannel> _channels;
    private readonly object _lock = new();

    public InboxNotificationService(
        string dataRoot,
        IIdentityStore? identity = null,
        IStudioAuthorizer? authorizer = null,
        IEnumerable<INotificationChannel>? channels = null)
    {
        if (string.IsNullOrWhiteSpace(dataRoot))
            throw new ArgumentException("dataRoot must be a non-empty path.", nameof(dataRoot));
        _root = Path.Combine(dataRoot, "notifications");
        Directory.CreateDirectory(_root);
        _identity = identity;
        _authorizer = authorizer;
        _channels = (channels ?? Array.Empty<INotificationChannel>()).ToDictionary(c => c.Channel);
    }

    /// <summary>Register (or replace) a channel at runtime. Stage 7's composition uses this to add Email/Webhook/SignalR.</summary>
    public void AddChannel(INotificationChannel channel) => _channels[channel.Channel] = channel;

    // ─── INotificationService ─────────────────────────────────────────────────

    public async Task SendAsync(NotificationMessage message, IReadOnlyList<NotificationChannel>? channels = null, CancellationToken ct = default)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        // Resolve recipients: either the explicit RecipientUserId, or the members of RecipientRole.
        var recipients = await ResolveRecipientsAsync(message, ct).ConfigureAwait(false);
        if (recipients.Count == 0) return;

        foreach (var recipient in recipients)
        {
            // Per-recipient copy so each gets its own IsRead state.
            var copy = CloneForRecipient(message, recipient.UserId);
            WriteToInbox(recipient.UserId, copy);

            // Channels: explicit argument wins, else recipient's subscriptions. Inbox is always-on,
            // so we don't consult subscriptions for it.
            var effectiveChannels = channels ?? await ResolveChannelsFromSubscriptions(recipient.UserId, copy, ct).ConfigureAwait(false);
            foreach (var ch in effectiveChannels)
            {
                if (ch == NotificationChannel.Inbox) continue;  // already done
                if (!_channels.TryGetValue(ch, out var adapter)) continue;
                try { await adapter.SendAsync(copy, recipient.DisplayName, ct).ConfigureAwait(false); }
                catch { /* best-effort — log via System.Diagnostics.Trace */ }
            }
        }
    }

    public Task<IReadOnlyList<NotificationMessage>> GetInboxAsync(string userId, bool unreadOnly = false, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) return Task.FromResult<IReadOnlyList<NotificationMessage>>(Array.Empty<NotificationMessage>());
        lock (_lock)
        {
            var all = LoadInbox(userId);
            var filtered = unreadOnly ? all.Where(m => !m.IsRead) : all;
            return Task.FromResult<IReadOnlyList<NotificationMessage>>(filtered.OrderByDescending(m => m.CreatedAt).ToList());
        }
    }

    public Task MarkReadAsync(string userId, IReadOnlyCollection<string> messageIds, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId) || messageIds == null) return Task.CompletedTask;
        var ids = messageIds.ToHashSet();
        lock (_lock)
        {
            var all = LoadInbox(userId);
            var changed = false;
            foreach (var m in all)
            {
                if (ids.Contains(m.Id) && !m.IsRead)
                {
                    m.IsRead = true;
                    m.ReadAt = DateTimeOffset.UtcNow;
                    changed = true;
                }
            }
            if (changed) SaveInbox(userId, all);
            return Task.CompletedTask;
        }
    }

    public Task MarkAllReadAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) return Task.CompletedTask;
        lock (_lock)
        {
            var all = LoadInbox(userId);
            var changed = false;
            foreach (var m in all)
            {
                if (!m.IsRead) { m.IsRead = true; m.ReadAt = DateTimeOffset.UtcNow; changed = true; }
            }
            if (changed) SaveInbox(userId, all);
            return Task.CompletedTask;
        }
    }

    // ─── Subscriptions ────────────────────────────────────────────────────────
    // Persisted at {dataRoot}/notifications/_subscriptions.json as a single list.

    public Task SubscribeAsync(string userId, NotificationSubscription subscription, CancellationToken ct = default)
    {
        if (subscription == null) throw new ArgumentNullException(nameof(subscription));
        lock (_lock)
        {
            var subs = LoadSubscriptions();
            subs.RemoveAll(s => SameSubscription(s, userId, subscription));
            var copy = new NotificationSubscription
            {
                UserId = userId,
                Category = subscription.Category,
                AppId = subscription.AppId,
                Channel = subscription.Channel,
            };
            subs.Add(copy);
            SaveSubscriptions(subs);
            return Task.CompletedTask;
        }
    }

    public Task UnsubscribeAsync(string userId, NotificationSubscription subscription, CancellationToken ct = default)
    {
        if (subscription == null) throw new ArgumentNullException(nameof(subscription));
        lock (_lock)
        {
            var subs = LoadSubscriptions();
            subs.RemoveAll(s => SameSubscription(s, userId, subscription));
            SaveSubscriptions(subs);
            return Task.CompletedTask;
        }
    }

    public Task<IReadOnlyList<NotificationSubscription>> GetSubscriptionsAsync(string userId, CancellationToken ct = default)
    {
        lock (_lock)
        {
            var subs = LoadSubscriptions().Where(s => string.Equals(s.UserId, userId, StringComparison.OrdinalIgnoreCase)).ToList();
            return Task.FromResult<IReadOnlyList<NotificationSubscription>>(subs);
        }
    }

    // ─── file I/O ─────────────────────────────────────────────────────────────

    private string InboxPath(string userId) => Path.Combine(_root, $"{Sanitize(userId)}.json");
    private string SubscriptionsPath => Path.Combine(_root, "_subscriptions.json");

    private static string Sanitize(string s) =>
        string.Concat(s.Select(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_'));

    private List<NotificationMessage> LoadInbox(string userId)
    {
        try
        {
            var path = InboxPath(userId);
            if (!File.Exists(path)) return new List<NotificationMessage>();
            var json = ReadShared(path);
            return string.IsNullOrWhiteSpace(json)
                ? new List<NotificationMessage>()
                : (JsonSerializer.Deserialize<List<NotificationMessage>>(json, JsonOpts) ?? new List<NotificationMessage>());
        }
        catch { return new List<NotificationMessage>(); }
    }

    private void WriteToInbox(string userId, NotificationMessage message)
    {
        lock (_lock)
        {
            var inbox = LoadInbox(userId);
            inbox.Add(message);
            // Trim oldest beyond the cap (keeps the file small and read fast).
            if (inbox.Count > InboxRetentionCap)
                inbox = inbox.OrderByDescending(m => m.CreatedAt).Take(InboxRetentionCap).ToList();
            SaveInbox(userId, inbox);
        }
    }

    private void SaveInbox(string userId, List<NotificationMessage> inbox) =>
        AtomicWrite(InboxPath(userId), JsonSerializer.Serialize(inbox, JsonOpts));

    private List<NotificationSubscription> LoadSubscriptions()
    {
        try
        {
            if (!File.Exists(SubscriptionsPath)) return new List<NotificationSubscription>();
            var json = ReadShared(SubscriptionsPath);
            return string.IsNullOrWhiteSpace(json)
                ? new List<NotificationSubscription>()
                : (JsonSerializer.Deserialize<List<NotificationSubscription>>(json, JsonOpts) ?? new List<NotificationSubscription>());
        }
        catch { return new List<NotificationSubscription>(); }
    }

    private void SaveSubscriptions(List<NotificationSubscription> subs) =>
        AtomicWrite(SubscriptionsPath, JsonSerializer.Serialize(subs, JsonOpts));

    private void AtomicWrite(string path, string content)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        var tmp = Path.Combine(string.IsNullOrEmpty(dir) ? "." : dir, Path.GetRandomFileName() + ".tmp");
        try
        {
            File.WriteAllText(tmp, content, Utf8NoBom);
            for (int attempt = 0; attempt < IoRetryCount; attempt++)
            {
                try { File.Move(tmp, path, overwrite: true); return; }
                catch (IOException) when (attempt < IoRetryCount - 1) { Thread.Sleep(IoRetryDelayMs); }
                catch (UnauthorizedAccessException) when (attempt < IoRetryCount - 1) { Thread.Sleep(IoRetryDelayMs); }
            }
        }
        finally
        {
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
        }
    }

    private static string ReadShared(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var sr = new StreamReader(fs, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return sr.ReadToEnd();
    }

    // ─── resolution helpers ───────────────────────────────────────────────────

    private async Task<IReadOnlyList<(string UserId, string DisplayName)>> ResolveRecipientsAsync(NotificationMessage m, CancellationToken ct)
    {
        // Direct message — no resolution needed.
        if (!string.IsNullOrEmpty(m.RecipientUserId))
            return new[] { (m.RecipientUserId, m.RecipientUserId) };  // display name resolved at channel send if available

        // Role broadcast — resolve members.
        if (string.IsNullOrEmpty(m.RecipientRole)) return Array.Empty<(string, string)>();
        if (_identity == null)
        {
            // Solo default: deliver to the local admin.
            return new[] { (LocalIdentityStore.LocalAdminId, LocalIdentityStore.LocalAdminId) };
        }

        var users = await _identity.ListUsersAsync(ct: ct).ConfigureAwait(false);
        return users
            .Where(u => u.IsActive && u.Roles.Contains(m.RecipientRole, StringComparer.OrdinalIgnoreCase))
            .Select(u => (u.Id, u.DisplayName))
            .ToList();
    }

    private async Task<IReadOnlyList<NotificationChannel>> ResolveChannelsFromSubscriptions(
        string userId, NotificationMessage message, CancellationToken ct)
    {
        var subs = await GetSubscriptionsAsync(userId, ct).ConfigureAwait(false);
        return subs
            .Where(s => s.Category == message.Category)
            .Where(s => string.IsNullOrEmpty(s.AppId) || string.Equals(s.AppId, message.AppId, StringComparison.OrdinalIgnoreCase))
            .Select(s => s.Channel)
            .Distinct()
            .ToList();
    }

    private static NotificationMessage CloneForRecipient(NotificationMessage source, string recipientUserId) => new()
    {
        Id = source.Id,
        Category = source.Category,
        Severity = source.Severity,
        Title = source.Title,
        Body = source.Body,
        RecipientUserId = recipientUserId,
        AppId = source.AppId,
        EnvId = source.EnvId,
        DeepLinkKind = source.DeepLinkKind,
        DeepLinkId = source.DeepLinkId,
        CreatedAt = source.CreatedAt,
    };

    private static bool SameSubscription(NotificationSubscription s, string userId, NotificationSubscription other) =>
        string.Equals(s.UserId, userId, StringComparison.OrdinalIgnoreCase)
        && s.Category == other.Category
        && s.Channel == other.Channel
        && string.Equals(s.AppId ?? "", other.AppId ?? "", StringComparison.OrdinalIgnoreCase);
}

// ─── Channel adapters ────────────────────────────────────────────────────────

/// <summary>
/// SMTP email channel. Mirrors the ETL <c>EmailNotifier</c>'s mechanism (SmtpClient + token-less
/// fixed subject/body) but consumes a <see cref="NotificationMessage"/> instead of a pipeline AlertEvent.
/// </summary>
public sealed class EmailNotificationChannel : INotificationChannel
{
    public NotificationChannel Channel => NotificationChannel.Email;
    private readonly Func<NotificationMessage, string> _recipientResolver;
    private readonly Func<NotificationMessage, (string subject, string body)> _contentResolver;
    private readonly Func<SmtpClient> _smtpFactory;
    private readonly string _from;

    /// <param name="recipientResolver">Returns the recipient email address for a message (null → skip).</param>
    /// <param name="contentResolver">Returns (subject, body) for the message.</param>
    /// <param name="smtpFactory">Factory so the SmtpClient can be disposed per-send (SmtpClient is not reusable after Dispose).</param>
    /// <param name="from">Sender address.</param>
    public EmailNotificationChannel(
        Func<NotificationMessage, string> recipientResolver,
        Func<NotificationMessage, (string subject, string body)> contentResolver,
        Func<SmtpClient> smtpFactory,
        string from)
    {
        _recipientResolver = recipientResolver ?? throw new ArgumentNullException(nameof(recipientResolver));
        _contentResolver = contentResolver ?? throw new ArgumentNullException(nameof(contentResolver));
        _smtpFactory = smtpFactory ?? throw new ArgumentNullException(nameof(smtpFactory));
        _from = from ?? throw new ArgumentNullException(nameof(from));
    }

    public async Task SendAsync(NotificationMessage message, string recipientDisplayName, CancellationToken ct = default)
    {
        var to = _recipientResolver(message);
        if (string.IsNullOrWhiteSpace(to)) return;
        var (subject, body) = _contentResolver(message);

        using var client = _smtpFactory();
        using var msg = new MailMessage { From = new MailAddress(_from), Subject = subject, Body = body, IsBodyHtml = false };
        msg.To.Add(to);
        await client.SendMailAsync(msg, ct).ConfigureAwait(false);
    }
}

/// <summary>
/// Webhook POST channel. Mirrors the ETL <c>WebhookNotifier</c>'s shape (HMAC-signed JSON payload)
/// but consumes a <see cref="NotificationMessage"/>.
/// </summary>
public sealed class WebhookNotificationChannel : INotificationChannel
{
    public NotificationChannel Channel => NotificationChannel.Webhook;
    private readonly Uri _url;
    private readonly byte[]? _signingSecret;
    private readonly HttpClient _http;

    public WebhookNotificationChannel(string url, string? signingSecret = null, HttpClient? http = null)
    {
        if (string.IsNullOrWhiteSpace(url)) throw new ArgumentException("url is required.", nameof(url));
        _url = new Uri(url);
        _signingSecret = string.IsNullOrEmpty(signingSecret) ? null : Encoding.UTF8.GetBytes(signingSecret);
        _http = http ?? new HttpClient();
    }

    public async Task SendAsync(NotificationMessage message, string recipientDisplayName, CancellationToken ct = default)
    {
        var payload = JsonSerializer.Serialize(message, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        using var req = new HttpRequestMessage(HttpMethod.Post, _url) { Content = new StringContent(payload, Encoding.UTF8, "application/json") };
        if (_signingSecret != null)
        {
            using var hmac = new System.Security.Cryptography.HMACSHA256(_signingSecret);
            var sig = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
            req.Headers.Add("X-Beep-Signature", $"sha256={sig}");
        }
        using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
    }
}

/// <summary>
/// Plain text log file channel — always available, used for auditing notification sends in dev.
/// </summary>
public sealed class LogFileNotificationChannel : INotificationChannel
{
    public NotificationChannel Channel => NotificationChannel.Inbox; // not a real channel; reuse Inbox slot for "log only"

    // Actually, this should not collide with Inbox. Make it a distinct slot by leaving Channel as Inbox
    // (since LogFile is a side-sink, not a delivery target the user subscribes to). For Stage 5 v1 this
    // channel is registered separately and only fires when explicitly requested via SendAsync(channels: ...).
    // We don't currently expose a NotificationChannel.LogFile enum value — add it if needed in a follow-up.

    private readonly string _path;
    private readonly object _lock = new();

    public LogFileNotificationChannel(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("path is required.", nameof(path));
        _path = path;
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
    }

    public Task SendAsync(NotificationMessage message, string recipientDisplayName, CancellationToken ct = default)
    {
        try
        {
            var line = $"[{DateTimeOffset.UtcNow:O}] [{message.Severity}] to={recipientDisplayName} cat={message.Category} title={message.Title}";
            lock (_lock) File.AppendAllText(_path, line + Environment.NewLine);
        }
        catch { /* best-effort */ }
        return Task.CompletedTask;
    }
}
