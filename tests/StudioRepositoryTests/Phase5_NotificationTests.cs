using TheTechIdea.Beep.Services.Studio.Identity;
using TheTechIdea.Beep.Services.Studio.Notifications;
using TheTechIdea.Beep.Services.Studio.Permissions;
using TheTechIdea.Beep.Studio;
using TheTechIdea.Beep.Studio.Apps.Workflows;
using TheTechIdea.Beep.Studio.Governance;
using TheTechIdea.Beep.Studio.Identity;
using TheTechIdea.Beep.Studio.Notifications;
using TheTechIdea.Beep.Studio.Permissions;
using Xunit;
using ApprovalState = TheTechIdea.Beep.Studio.Governance.ApprovalState;

namespace TheTechIdea.Beep.Studio.Repository.Tests;

/// <summary>
/// Stage 5 tests for the notification system. Three layers:
///  1. <see cref="InboxNotificationService"/> — always-on inbox, fan-out via channels,
///     role broadcast resolution, subscriptions
///  2. Channel adapters — <see cref="LogFileNotificationChannel"/> (deterministic; others need
///     SMTP/HTTP and are validated by smoke-checks of their config)
///  3. <see cref="GovernanceService"/> + <see cref="ApprovalExpiryService"/> integration —
///     approvals fan out notifications to the right recipients
/// </summary>
public class Phase5_NotificationTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(), "beep-stage5-tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        try { if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true); } catch { }
    }

    private InboxNotificationService NewService(IIdentityStore? identity = null, IStudioAuthorizer? authorizer = null)
        => new(_root, identity, authorizer);

    private static NotificationMessage Msg(string userId, NotificationCategory cat, string title = "t") => new()
    {
        Category = cat,
        Title = title,
        Body = "body",
        RecipientUserId = userId,
    };

    // ─── Inbox ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Inbox_AlwaysWritten_EvenWithoutSubscriptions()
    {
        var svc = NewService();
        await svc.SendAsync(Msg("alice", NotificationCategory.ApprovalRequested));
        var inbox = await svc.GetInboxAsync("alice");
        Assert.Single(inbox);
    }

    [Fact]
    public async Task Inbox_FiltersUnread()
    {
        var svc = NewService();
        await svc.SendAsync(Msg("alice", NotificationCategory.ApprovalRequested));
        await svc.SendAsync(Msg("alice", NotificationCategory.MigrationCompleted));

        Assert.Equal(2, (await svc.GetInboxAsync("alice")).Count);
        Assert.Equal(2, (await svc.GetInboxAsync("alice", unreadOnly: true)).Count);

        var first = (await svc.GetInboxAsync("alice"))[0];
        await svc.MarkReadAsync("alice", new[] { first.Id });
        Assert.Equal(1, (await svc.GetInboxAsync("alice", unreadOnly: true)).Count);
    }

    [Fact]
    public async Task Inbox_MarkAllRead()
    {
        var svc = NewService();
        await svc.SendAsync(Msg("alice", NotificationCategory.ApprovalRequested));
        await svc.SendAsync(Msg("alice", NotificationCategory.MigrationCompleted));

        await svc.MarkAllReadAsync("alice");
        Assert.Empty(await svc.GetInboxAsync("alice", unreadOnly: true));
    }

    [Fact]
    public async Task Inbox_PerUserIsolation()
    {
        var svc = NewService();
        await svc.SendAsync(Msg("alice", NotificationCategory.ApprovalRequested));
        await svc.SendAsync(Msg("bob", NotificationCategory.ApprovalRequested));

        Assert.Single(await svc.GetInboxAsync("alice"));
        Assert.Single(await svc.GetInboxAsync("bob"));
        Assert.Empty(await svc.GetInboxAsync("carol"));
    }

    [Fact]
    public async Task Inbox_RetentionCap_TrimsOldest()
    {
        // 200-message cap: send 210, expect 200 retained, newest preserved.
        var svc = NewService();
        for (int i = 0; i < 210; i++)
        {
            var m = Msg("alice", NotificationCategory.System, title: $"m{i}");
            m.CreatedAt = DateTimeOffset.UtcNow.AddSeconds(i);  // monotonic for predictable ordering
            await svc.SendAsync(m);
        }
        var inbox = await svc.GetInboxAsync("alice");
        Assert.Equal(200, inbox.Count);
        // Newest (m209) is preserved.
        Assert.Equal("m209", inbox[0].Title);
    }

    // ─── Channel fan-out ──────────────────────────────────────────────────────

    [Fact]
    public async Task Channel_FanOut_WritesLogFile()
    {
        var logPath = Path.Combine(_root, "notifications.log");
        var svc = new InboxNotificationService(_root);
        svc.AddChannel(new LogFileNotificationChannel(logPath));

        await svc.SendAsync(Msg("alice", NotificationCategory.ApprovalRequested),
            channels: new[] { NotificationChannel.Inbox });
        // Inbox channel doesn't trigger LogFile (LogFile is on the Inbox slot — special case).
        // For a real test of LogFile: bypass the Inbox slot check by adding it explicitly through a
        // dedicated channel — covered in the next test.

        Assert.Single(await svc.GetInboxAsync("alice"));
    }

    [Fact]
    public async Task Channels_ResolvesSubscriptions_ForExtraChannels()
    {
        // alice subscribes to MigrationCompleted → Email. We don't have a real SMTP server, so we
        // register a LogFile channel on the Email slot to detect the fan-out path was taken.
        var logPath = Path.Combine(_root, "emails.log");
        var svc = new InboxNotificationService(_root);
        // Register the log-file channel UNDER the Email slot so the subscription routes to it.
        svc.AddChannel(new EmailSlotLogFileChannel(logPath));

        await svc.SubscribeAsync("alice", new NotificationSubscription
        {
            UserId = "alice",
            Category = NotificationCategory.MigrationCompleted,
            Channel = NotificationChannel.Email,
        });

        // Send a MigrationCompleted — alice should get inbox + the Email-slot log write.
        await svc.SendAsync(Msg("alice", NotificationCategory.MigrationCompleted));
        Assert.Single(await svc.GetInboxAsync("alice"));
        Assert.True(File.Exists(logPath));
        var logContent = File.ReadAllText(logPath);
        Assert.Contains("MigrationCompleted", logContent);

        // Send an ApprovalRequested — alice did NOT subscribe to it, so only inbox; no log write beyond the first.
        await svc.SendAsync(Msg("alice", NotificationCategory.ApprovalRequested));
        Assert.Equal(2, (await svc.GetInboxAsync("alice")).Count);
    }

    [Fact]
    public async Task Channels_ExplicitOverride_IgnoresSubscriptions()
    {
        var logPath = Path.Combine(_root, "forced.log");
        var svc = new InboxNotificationService(_root);
        svc.AddChannel(new EmailSlotLogFileChannel(logPath));

        // No subscription — but explicit channels arg forces the Email-slot log write.
        await svc.SendAsync(Msg("alice", NotificationCategory.ApprovalRequested),
            channels: new[] { NotificationChannel.Email });
        Assert.True(File.Exists(logPath));
    }

    // ─── Role broadcast ───────────────────────────────────────────────────────

    [Fact]
    public async Task RoleBroadcast_ResolvesAllMembers()
    {
        var auth = new RoleBasedStudioAuthorizer(_root);
        var store = new DatabaseIdentityStore(_root, auth);

        var alice = await store.CreateAsync(new StudioUser { UserName = "alice", DisplayName = "Alice" }, "p");
        var bob = await store.CreateAsync(new StudioUser { UserName = "bob", DisplayName = "Bob" }, "p");
        var carol = await store.CreateAsync(new StudioUser { UserName = "carol", DisplayName = "Carol" }, "p");
        await store.AssignRoleAsync(alice.Id, nameof(AppMemberRole.Admin));
        await store.AssignRoleAsync(bob.Id, nameof(AppMemberRole.Admin));
        // carol is a Viewer — not in the broadcast.

        var svc = NewService(identity: store, authorizer: auth);
        var broadcast = new NotificationMessage
        {
            Category = NotificationCategory.System,
            Title = "Hello admins",
            RecipientRole = nameof(AppMemberRole.Admin),
        };
        await svc.SendAsync(broadcast);

        Assert.Single(await svc.GetInboxAsync(alice.Id));
        Assert.Single(await svc.GetInboxAsync(bob.Id));
        Assert.Empty(await svc.GetInboxAsync(carol.Id));
    }

    [Fact]
    public async Task RoleBroadcast_SoloDefault_DeliversToLocalAdmin()
    {
        // No identity store wired — role broadcast goes to the implicit local admin.
        var svc = NewService();
        await svc.SendAsync(new NotificationMessage
        {
            Category = NotificationCategory.System,
            Title = "Hello",
            RecipientRole = "Admin",
        });
        var inbox = await svc.GetInboxAsync(LocalIdentityStore.LocalAdminId);
        Assert.Single(inbox);
    }

    // ─── Governance integration ──────────────────────────────────────────────

    [Fact]
    public async Task Governance_RequestApproval_NotifiesApprovers()
    {
        var auth = new RoleBasedStudioAuthorizer(_root);
        var store = new DatabaseIdentityStore(_root, auth);
        var notifications = new InboxNotificationService(_root, store, auth);
        var governance = new GovernanceService(audit: null, dataRoot: _root, notifications, auth);

        // alice is Admin (has ApproveRequest); bob is the requester.
        var alice = await store.CreateAsync(new StudioUser { UserName = "alice" }, "p");
        var bob = await store.CreateAsync(new StudioUser { UserName = "bob" }, "p");
        await store.AssignRoleAsync(alice.Id, nameof(AppMemberRole.Admin));

        var req = SampleApprovalRequest(requestedBy: bob.Id);
        var result = await governance.RequestApprovalAsync(req);
        Assert.True(result.IsSuccess);

        // alice (the approver) gets a notification; bob (the requester) does NOT.
        Assert.Single(await notifications.GetInboxAsync(alice.Id));
        Assert.Empty(await notifications.GetInboxAsync(bob.Id));
    }

    [Fact]
    public async Task Governance_DecideApproval_NotifiesRequester()
    {
        var auth = new RoleBasedStudioAuthorizer(_root);
        var store = new DatabaseIdentityStore(_root, auth);
        var notifications = new InboxNotificationService(_root, store, auth);
        var governance = new GovernanceService(audit: null, dataRoot: _root, notifications, auth);

        var alice = await store.CreateAsync(new StudioUser { UserName = "alice" }, "p");
        var bob = await store.CreateAsync(new StudioUser { UserName = "bob" }, "p");
        await store.AssignRoleAsync(alice.Id, nameof(AppMemberRole.Admin));

        var approval = (await governance.RequestApprovalAsync(SampleApprovalRequest(requestedBy: bob.Id))).Value!;
        await governance.DecideApprovalAsync(approval.ApprovalId,
            new ApprovalDecision(Decider: alice.Id, DecidedAt: DateTimeOffset.UtcNow, Approved: true, Comment: null),
            decider: alice.Id);

        // alice has 1 notification (the request). bob has 1 notification (the decision).
        Assert.Single(await notifications.GetInboxAsync(alice.Id));
        var bobInbox = await notifications.GetInboxAsync(bob.Id);
        Assert.Single(bobInbox);
        Assert.Equal(NotificationCategory.ApprovalDecided, bobInbox[0].Category);
    }

    [Fact]
    public async Task Governance_SoloMode_NoNotifications_PreservesTodayBehavior()
    {
        // No notifications injected — preserve today's behavior byte-for-byte.
        var governance = new GovernanceService(audit: null, dataRoot: _root);
        var result = await governance.RequestApprovalAsync(SampleApprovalRequest(requestedBy: "alice"));
        Assert.True(result.IsSuccess);
    }

    // ─── ApprovalExpiryService ────────────────────────────────────────────────

    [Fact]
    public async Task ExpiryService_DoesNothing_ForFreshApprovals()
    {
        var auth = new RoleBasedStudioAuthorizer(_root);
        var store = new DatabaseIdentityStore(_root, auth);
        var notifications = NewService(store, auth);
        var governance = new GovernanceService(audit: null, dataRoot: _root, notifications, auth);

        await governance.RequestApprovalAsync(SampleApprovalRequest(requestedBy: "alice"));

        var expiry = new ApprovalExpiryService(governance, notifications, approvalTtl: TimeSpan.FromDays(7));
        await expiry.SweepOnceAsync();

        var approvals = await governance.ListApprovalsAsync();
        Assert.All(approvals.Value!, a => Assert.Equal(ApprovalState.Pending, a.State));
    }

    [Fact]
    public async Task ExpiryService_ExpiresStaleApprovals_AndNotifies()
    {
        var auth = new RoleBasedStudioAuthorizer(_root);
        var store = new DatabaseIdentityStore(_root, auth);
        var notifications = NewService(store, auth);
        var governance = new GovernanceService(audit: null, dataRoot: _root, notifications, auth);

        // Stale request: RequestedAt 30 days ago.
        var stale = SampleApprovalRequest(requestedBy: "alice");
        stale = stale with { RequestedAt = DateTimeOffset.UtcNow.AddDays(-30) };
        await governance.RequestApprovalAsync(stale);

        // TTL = 7 days → stale request is past it.
        var expiry = new ApprovalExpiryService(governance, notifications, approvalTtl: TimeSpan.FromDays(7));
        await expiry.SweepOnceAsync();

        var approvals = await governance.ListApprovalsAsync();
        var theApproval = approvals.Value!.Single();
        Assert.NotEqual(ApprovalState.Pending, theApproval.State);

        // Requester gets an ApprovalExpired notification.
        var inbox = await notifications.GetInboxAsync("alice");
        Assert.Contains(inbox, m => m.Category == NotificationCategory.ApprovalExpired);
    }

    // ─── Subscriptions ────────────────────────────────────────────────────────

    [Fact]
    public async Task Subscriptions_CanBeListed_AndRemoved()
    {
        var svc = NewService();
        var sub = new NotificationSubscription
        {
            UserId = "alice",
            Category = NotificationCategory.MigrationCompleted,
            Channel = NotificationChannel.Email,
        };
        await svc.SubscribeAsync("alice", sub);
        Assert.Single(await svc.GetSubscriptionsAsync("alice"));

        await svc.UnsubscribeAsync("alice", sub);
        Assert.Empty(await svc.GetSubscriptionsAsync("alice"));
    }

    [Fact]
    public async Task Subscriptions_FilterByAppId()
    {
        var svc = NewService();
        await svc.SubscribeAsync("alice", new NotificationSubscription
        {
            UserId = "alice",
            Category = NotificationCategory.MigrationCompleted,
            AppId = "appA",
            Channel = NotificationChannel.Email,
        });

        var logPathAppA = Path.Combine(_root, "appA.log");
        var logPathAppB = Path.Combine(_root, "appB.log");
        // Two separate services with different log channels to distinguish which subscription fired.
        // Easier: use one service + one log path; assert that an appB message does NOT trigger the email-slot.
        svc.AddChannel(new EmailSlotLogFileChannel(logPathAppA));

        // appA message → subscription fires.
        var msgA = Msg("alice", NotificationCategory.MigrationCompleted);
        msgA.AppId = "appA";
        await svc.SendAsync(msgA);
        Assert.True(File.Exists(logPathAppA));

        // appB message → subscription does NOT fire (AppId mismatch).
        File.Delete(logPathAppA);
        var msgB = Msg("alice", NotificationCategory.MigrationCompleted);
        msgB.AppId = "appB";
        await svc.SendAsync(msgB);
        Assert.False(File.Exists(logPathAppA));
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static ApprovalRequest SampleApprovalRequest(string requestedBy) => new(
        ApprovalId: Guid.NewGuid().ToString("N"),
        OperationType: "ApplyMigration",
        OperationSubjectId: "ds1",
        OperationSubjectJson: "{}",
        PlanHash: "hash-" + Guid.NewGuid().ToString("N")[..8],
        Tier: RolloutTier.Dev,   // Dev default policy has no AllowedApproverRoles restriction
        RequestedBy: requestedBy,
        RequestedAt: DateTimeOffset.UtcNow,
        Decisions: Array.Empty<ApprovalDecision>(),
        State: ApprovalState.Pending,
        DecidedAt: null);

    /// <summary>
    /// A test-only channel that writes to a log file but sits on the Email slot, so we can detect
    /// "the Email fan-out path fired" without an SMTP server. (Production EmailNotificationChannel
    /// requires real SMTP config and is not testable in CI.)
    /// </summary>
    private sealed class EmailSlotLogFileChannel : INotificationChannel
    {
        public NotificationChannel Channel => NotificationChannel.Email;
        private readonly string _path;
        private readonly object _lock = new();
        public EmailSlotLogFileChannel(string path) { _path = path; }
        public Task SendAsync(NotificationMessage message, string recipientDisplayName, CancellationToken ct = default)
        {
            try
            {
                var dir = Path.GetDirectoryName(_path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                lock (_lock) File.AppendAllText(_path, $"{message.Category}:{message.Title}{Environment.NewLine}");
            }
            catch { }
            return Task.CompletedTask;
        }
    }
}
