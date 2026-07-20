using TheTechIdea.Beep.AppMap;
using TheTechIdea.Beep.Services.Studio.Migration.Ledger;
using TheTechIdea.Beep.Services.Studio.Repository;
using TheTechIdea.Beep.Studio;
using TheTechIdea.Beep.Studio.Apps.Workflows;
using TheTechIdea.Beep.Studio.Governance;
using TheTechIdea.Beep.Studio.Repository;
using Xunit;
using GovernanceApprovalState = TheTechIdea.Beep.Studio.Governance.ApprovalState;

namespace TheTechIdea.Beep.Studio.Repository.Tests;

/// <summary>
/// Stage 3 tests for <see cref="IStudioRepository"/>. All tests run against the interface — the
/// <see cref="FileStudioRepository"/> is the v1 implementation; when a <c>DatabaseStudioRepository</c>
/// lands (deferred to Stage 7), the same tests apply by swapping the factory method.
/// </summary>
/// <remarks>
/// <para>
/// Pattern mirrors <c>tests/SetupWizardTests/StateStoreTests.cs</c>: temp root per fixture, IDisposable,
/// each test gets a fresh store. The parity claim from the Stage 3 plan ("same sequence of ops gives
/// identical reads") is realized by structuring each test against the interface, not the impl — so the
/// DB impl just slots in.
/// </para>
/// <para>
/// Coverage: Apps CRUD + dedupe-by-name + CreatedAt preservation; OCC refusal on stale writes;
/// cross-instance visibility (reload on read); Governance policy/approval persistence; Env-profile
/// CRUD; Masking rules per-app; lease acquire/renew/release; the migration-ledger aggregate is
/// reused (the underlying <see cref="JsonMigrationLedger"/> is covered by Phase2 tests).
/// </para>
/// </remarks>
public class Phase3_RepositoryTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(), "beep-studio-repo-tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        try { if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true); } catch { }
    }

    private IStudioRepository NewRepo() =>
        new FileStudioRepository(_root, new JsonMigrationLedger(_root));

    private static AppDefinition SampleApp(string id, string name) => new()
    {
        Id = id,
        Name = name,
        Description = $"desc for {name}",
        Projects = new List<AppProject>(),
        Environments = new List<AppEnv>(),
    };

    // ─── Apps aggregate ───────────────────────────────────────────────────────

    [Fact]
    public async Task Apps_SaveAndLoad_RoundTrips()
    {
        var repo = NewRepo();
        var app = SampleApp("app-1", "MyApp");

        await repo.Apps.SaveAsync(app);
        var loaded = await repo.Apps.LoadAsync("app-1");

        Assert.NotNull(loaded.App);
        Assert.Equal("MyApp", loaded.App!.Name);
        Assert.NotNull(loaded.RowVersion);
    }

    [Fact]
    public async Task Apps_DedupeByIdOrName_MatchesAppRegistrySemantics()
    {
        // Pre-Stage-3 AppRegistry deduped by Id OR Name. FileStudioRepository preserves that.
        var repo = NewRepo();
        await repo.Apps.SaveAsync(SampleApp("app-1", "MyApp"));
        await repo.Apps.SaveAsync(SampleApp("app-2", "MyApp"));  // same name, different id

        var all = await repo.Apps.LoadAllAsync();
        Assert.Single(all);  // upserted by name, not appended
    }

    [Fact]
    public async Task Apps_PreservesCreatedAt_OnUpsert()
    {
        var repo = NewRepo();
        var original = SampleApp("app-1", "MyApp");
        original.CreatedAt = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        await repo.Apps.SaveAsync(original);

        // Save again with the same id, fresh instance (CreatedAt == default).
        var update = SampleApp("app-1", "MyApp");
        update.Description = "updated";
        await repo.Apps.SaveAsync(update);

        var loaded = await repo.Apps.LoadAsync("app-1");
        Assert.Equal(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc), loaded.App!.CreatedAt);
        Assert.Equal("updated", loaded.App.Description);
    }

    [Fact]
    public async Task Apps_Delete_RemovesAndReportsTruth()
    {
        var repo = NewRepo();
        await repo.Apps.SaveAsync(SampleApp("app-1", "MyApp"));

        Assert.True(await repo.Apps.DeleteAsync("app-1"));
        Assert.False(await repo.Apps.DeleteAsync("app-1"));  // already gone
        Assert.Empty(await repo.Apps.LoadAllAsync());
    }

    [Fact]
    public async Task Apps_OCC_RefusesStaleWrite()
    {
        var repo = NewRepo();
        await repo.Apps.SaveAsync(SampleApp("app-1", "MyApp"));
        var loaded1 = await repo.Apps.LoadAsync("app-1");
        var staleVersion = loaded1.RowVersion;

        // Someone else writes between our read and write — version changes.
        await repo.Apps.SaveAsync(SampleApp("app-1", "MyApp-Edited"));

        // Now we try to save with the stale version. Must throw.
        var ex = await Assert.ThrowsAsync<StudioRepositoryConflictException>(() =>
            repo.Apps.SaveAsync(SampleApp("app-1", "MyApp-Late"), expectedRowVersion: staleVersion));
        Assert.Equal("apps", ex.ResourceKey);
        Assert.Equal(staleVersion, ex.ExpectedVersion);
        Assert.NotNull(ex.CurrentVersion);
        Assert.NotEqual(staleVersion, ex.CurrentVersion);
    }

    [Fact]
    public async Task Apps_OCC_AllowsSaveWithFreshVersion()
    {
        var repo = NewRepo();
        await repo.Apps.SaveAsync(SampleApp("app-1", "MyApp"));
        var loaded = await repo.Apps.LoadAsync("app-1");

        // Save with the fresh version — must succeed and return a new version.
        var newVersion = await repo.Apps.SaveAsync(
            SampleApp("app-1", "MyApp-Edited"), expectedRowVersion: loaded.RowVersion);
        Assert.NotNull(newVersion);
        Assert.NotEqual(loaded.RowVersion, newVersion);
    }

    [Fact]
    public async Task Apps_ReloadOnRead_SeesCrossInstanceWrites()
    {
        // Two repository instances over the same root — second must see first's writes without restart.
        var first = NewRepo();
        var second = NewRepo();

        await first.Apps.SaveAsync(SampleApp("app-1", "MyApp"));
        var seen = await second.Apps.LoadAllAsync();
        Assert.Single(seen);
    }

    // ─── Environment-profile aggregate ───────────────────────────────────────

    [Fact]
    public async Task EnvProfiles_SaveAndLoad_RoundTrips()
    {
        var repo = NewRepo();
        var now = DateTimeOffset.UtcNow;
        var profile = new EnvironmentProfile(
            Id: "staging", Name: "Staging", Tier: RolloutTier.Staging,
            Order: 2, Color: "#FFAA00",
            RequiresApproval: true, RequiredApproverCount: 1, IsProduction: false,
            Tags: Array.Empty<string>(), CreatedAt: now, UpdatedAt: now);

        await repo.EnvironmentProfiles.SaveAsync(profile);
        var all = await repo.EnvironmentProfiles.LoadAllAsync();
        Assert.Single(all);
        Assert.Equal("staging", all[0].Id);
        Assert.Equal(RolloutTier.Staging, all[0].Tier);
    }

    [Fact]
    public async Task EnvProfiles_Delete()
    {
        var repo = NewRepo();
        var now = DateTimeOffset.UtcNow;
        await repo.EnvironmentProfiles.SaveAsync(
            new EnvironmentProfile("dev", "Dev", RolloutTier.Dev, 0, null,
                RequiresApproval: false, RequiredApproverCount: 0, IsProduction: false,
                Tags: Array.Empty<string>(), CreatedAt: now, UpdatedAt: now));

        Assert.True(await repo.EnvironmentProfiles.DeleteAsync("dev"));
        Assert.Empty(await repo.EnvironmentProfiles.LoadAllAsync());
    }

    // ─── Governance aggregate ─────────────────────────────────────────────────

    [Fact]
    public async Task Governance_Policy_SaveAndLoad_RoundTrips()
    {
        var repo = NewRepo();
        var policy = new GovernancePolicy(
            PolicyId: "prod-policy", Name: "Prod gate", Tier: RolloutTier.Live,
            RequireApprover: true, RequiredApproverCount: 2,
            AllowedApproverRoles: new[] { "Admin" }, BlockedOperations: Array.Empty<string>(),
            CooldownBetweenRuns: TimeSpan.FromHours(1), RequireDryRunOnApply: true,
            RequirePreflightOnApply: false, MaxRowsAffectedPerRun: 100_000,
            CreatedAt: DateTimeOffset.UtcNow, UpdatedAt: DateTimeOffset.UtcNow);

        await repo.Governance.SavePolicyAsync(policy);
        var all = await repo.Governance.LoadPoliciesAsync();
        Assert.Single(all);
        Assert.Equal(2, all[0].RequiredApproverCount);
    }

    [Fact]
    public async Task Governance_Approval_SaveAndLoad_RoundTrips()
    {
        var repo = NewRepo();
        var approval = new ApprovalRequest(
            ApprovalId: "appr-1", OperationType: "ApplyMigration",
            OperationSubjectId: "ds1", OperationSubjectJson: "{}",
            PlanHash: "abc123", Tier: RolloutTier.Live,
            RequestedBy: "alice", RequestedAt: DateTimeOffset.UtcNow,
            Decisions: Array.Empty<ApprovalDecision>(), State: GovernanceApprovalState.Pending,
            DecidedAt: null);

        await repo.Governance.SaveApprovalAsync(approval);
        var all = await repo.Governance.LoadApprovalsAsync();
        Assert.Single(all);
        Assert.Equal(GovernanceApprovalState.Pending, all[0].State);
    }

    [Fact]
    public async Task Governance_Policy_Delete()
    {
        var repo = NewRepo();
        await repo.Governance.SavePolicyAsync(new GovernancePolicy(
            "p1", "p", RolloutTier.Dev, false, 0, Array.Empty<string>(), Array.Empty<string>(),
            null, false, false, 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));

        Assert.True(await repo.Governance.DeletePolicyAsync("p1"));
        Assert.Empty(await repo.Governance.LoadPoliciesAsync());
    }

    // ─── Masking-rules aggregate ──────────────────────────────────────────────

    [Fact]
    public async Task MaskingRules_PerApp_SaveAndLoad_RoundTrips()
    {
        var repo = NewRepo();
        var rules = new[]
        {
            new MaskingRule("Users", "Email", MaskingStrategy.Partial),
            new MaskingRule("Users", "Phone", MaskingStrategy.Hash),
        };

        await repo.MaskingRules.SaveAsync("app-1", rules);
        var loaded = await repo.MaskingRules.LoadAsync("app-1");
        Assert.Equal(2, loaded.Count);

        // Different app — empty (per-app isolation).
        var other = await repo.MaskingRules.LoadAsync("app-2");
        Assert.Empty(other);
    }

    [Fact]
    public async Task MaskingRules_OverwriteOnResave()
    {
        var repo = NewRepo();
        await repo.MaskingRules.SaveAsync("app-1", new[]
        {
            new MaskingRule("Users", "Email", MaskingStrategy.Partial),
        });
        await repo.MaskingRules.SaveAsync("app-1", new[]
        {
            new MaskingRule("Orders", "Total", MaskingStrategy.Constant, "0"),
        });

        var loaded = await repo.MaskingRules.LoadAsync("app-1");
        Assert.Single(loaded);
        Assert.Equal("Orders", loaded[0].EntityName);
    }

    // ─── MigrationLedger aggregate ────────────────────────────────────────────

    [Fact]
    public async Task MigrationLedger_Aggregate_ReusesInjectedLedger()
    {
        var ledger = new JsonMigrationLedger(_root);
        var repo = new FileStudioRepository(_root, ledger);

        Assert.Same(ledger, repo.MigrationLedger);
    }

    // ─── Lease ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Lease_AcquireExclusive_SecondAcquireRefused()
    {
        var repo = NewRepo();
        var lease1 = await repo.TryAcquireLeaseAsync("apps", TimeSpan.FromMinutes(5));
        Assert.NotNull(lease1);

        var lease2 = await repo.TryAcquireLeaseAsync("apps", TimeSpan.FromMinutes(5));
        Assert.Null(lease2);  // already held
    }

    [Fact]
    public async Task Lease_ReleaseAllowsReacquire()
    {
        var repo = NewRepo();
        var lease = await repo.TryAcquireLeaseAsync("apps", TimeSpan.FromMinutes(5));
        Assert.NotNull(lease);

        await lease!.DisposeAsync();

        var lease2 = await repo.TryAcquireLeaseAsync("apps", TimeSpan.FromMinutes(5));
        Assert.NotNull(lease2);
    }

    [Fact]
    public async Task Lease_Renew_ExtendsExpiry()
    {
        var repo = NewRepo();
        var lease = await repo.TryAcquireLeaseAsync("apps", TimeSpan.FromMinutes(1));
        Assert.NotNull(lease);

        var original = lease!.ExpiresAt;
        var renewed = await lease.RenewAsync();
        Assert.True(renewed);
        Assert.True(lease.ExpiresAt >= original);
    }

    [Fact]
    public async Task Lease_ReleaseDoesNotStompReclaimedLease()
    {
        // Acquire, let it expire (we simulate by writing an already-expired lease and reacquiring),
        // then dispose the original — must NOT delete the second holder's lease.
        var repo = NewRepo();
        var lease1 = await repo.TryAcquireLeaseAsync("apps", TimeSpan.FromMilliseconds(1));
        Assert.NotNull(lease1);

        await Task.Delay(50);  // let lease1 expire
        var lease2 = await repo.TryAcquireLeaseAsync("apps", TimeSpan.FromMinutes(5));
        Assert.NotNull(lease2);  // lease1 expired → reclaim succeeds

        await lease1!.DisposeAsync();  // must NOT delete lease2

        // lease2 still owns it — a third acquire must be refused.
        var lease3 = await repo.TryAcquireLeaseAsync("apps", TimeSpan.FromMinutes(5));
        Assert.Null(lease3);
    }

    // ─── Concurrency ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Concurrent_app_saves_all_land_uniquely()
    {
        var repo = NewRepo();
        var tasks = Enumerable.Range(0, 30)
            .Select(i => repo.Apps.SaveAsync(SampleApp($"app-{i}", $"App{i}")))
            .ToArray();
        await Task.WhenAll(tasks);

        var reopened = NewRepo();  // fresh instance reads from disk
        var all = await reopened.Apps.LoadAllAsync();
        Assert.Equal(30, all.Count);
        Assert.Equal(30, all.Select(a => a.Id).Distinct().Count());
    }
}
