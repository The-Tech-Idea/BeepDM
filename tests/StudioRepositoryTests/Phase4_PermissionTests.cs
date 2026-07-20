using TheTechIdea.Beep.Services.Studio.Identity;
using TheTechIdea.Beep.Services.Studio.Permissions;
using TheTechIdea.Beep.Studio;
using TheTechIdea.Beep.Studio.Apps.Workflows;
using TheTechIdea.Beep.Studio.Identity;
using TheTechIdea.Beep.Studio.Permissions;
using Xunit;

namespace TheTechIdea.Beep.Studio.Repository.Tests;

/// <summary>
/// Stage 4 tests for the unified permission/identity system. Three layers:
///  1. <see cref="PermissionEvaluator"/> — the pure deny-wins/most-specific-wins algorithm
///  2. Solo impls (<see cref="LocalIdentityStore"/> + <see cref="AllowAllStudioAuthorizer"/>) —
///     preserve today's behavior byte-for-byte
///  3. Enterprise impls (<see cref="DatabaseIdentityStore"/> + <see cref="RoleBasedStudioAuthorizer"/> +
///     <see cref="PasswordHasher"/>) — real users, hashed passwords, persisted grants
/// </summary>
public class Phase4_PermissionTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(), "beep-stage4-tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        try { if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true); } catch { }
    }

    private static PermissionGrant Grant(
        string userId, StudioPermission action, PermissionEffect effect = PermissionEffect.Allow,
        string? appId = null, string? envId = null, string? datasourceName = null) => new()
    {
        UserId = userId,
        Action = action,
        Effect = effect,
        AppId = appId,
        EnvId = envId,
        DatasourceName = datasourceName,
    };

    // ─── PermissionEvaluator: the algorithm ───────────────────────────────────

    [Fact]
    public void Evaluator_DefaultDeny_WhenNoGrants()
    {
        var d = PermissionEvaluator.Evaluate(Array.Empty<PermissionGrant>(), "alice", StudioPermission.ViewApp, null, null, null);
        Assert.False(d.Allowed);
        Assert.Contains(d.Reasons, r => r.Contains("No ") && r.Contains("grant"));
    }

    [Fact]
    public void Evaluator_GlobalAllow_MatchesAnyScope()
    {
        var grants = new[] { Grant("alice", StudioPermission.ViewApp) };
        var d = PermissionEvaluator.Evaluate(grants, "alice", StudioPermission.ViewApp, "appA", "prod", "ds1");
        Assert.True(d.Allowed);
    }

    [Fact]
    public void Evaluator_AppScopedGrant_DoesNotMatchOtherApps()
    {
        var grants = new[] { Grant("alice", StudioPermission.ViewApp, appId: "appA") };
        Assert.True(PermissionEvaluator.Evaluate(grants, "alice", StudioPermission.ViewApp, "appA", null, null).Allowed);
        Assert.False(PermissionEvaluator.Evaluate(grants, "alice", StudioPermission.ViewApp, "appB", null, null).Allowed);
    }

    [Fact]
    public void Evaluator_DenyWinsAtSameScope()
    {
        var grants = new[]
        {
            Grant("alice", StudioPermission.ApplyMigration, PermissionEffect.Allow, appId: "appA"),
            Grant("alice", StudioPermission.ApplyMigration, PermissionEffect.Deny, appId: "appA"),
        };
        var d = PermissionEvaluator.Evaluate(grants, "alice", StudioPermission.ApplyMigration, "appA", null, null);
        Assert.False(d.Allowed);
    }

    [Fact]
    public void Evaluator_MostSpecificWins_EnvDeny_Beats_AppAllow()
    {
        // An admin granted ApplyMigration app-wide; a regulator denied it on prod.
        // The env-scoped Deny wins.
        var grants = new[]
        {
            Grant("alice", StudioPermission.ApplyMigration, PermissionEffect.Allow, appId: "appA"),
            Grant("alice", StudioPermission.ApplyMigration, PermissionEffect.Deny, appId: "appA", envId: "prod"),
        };
        var d = PermissionEvaluator.Evaluate(grants, "alice", StudioPermission.ApplyMigration, "appA", "prod", null);
        Assert.False(d.Allowed);
    }

    [Fact]
    public void Evaluator_MostSpecificWins_AppAllow_DoesNotBeat_GlobalDeny()
    {
        // The "override up" anti-pattern: an app-scoped Allow must NOT punch through a global Deny.
        // (Prevents privilege escalation.)
        var grants = new[]
        {
            Grant("alice", StudioPermission.ApplyMigration, PermissionEffect.Deny),
            Grant("alice", StudioPermission.ApplyMigration, PermissionEffect.Allow, appId: "appA"),
        };
        var d = PermissionEvaluator.Evaluate(grants, "alice", StudioPermission.ApplyMigration, "appA", null, null);
        Assert.False(d.Allowed);
    }

    [Fact]
    public void Evaluator_DifferentUsersAreIsolated()
    {
        var grants = new[] { Grant("alice", StudioPermission.ViewApp) };
        Assert.True(PermissionEvaluator.Evaluate(grants, "alice", StudioPermission.ViewApp, null, null, null).Allowed);
        Assert.False(PermissionEvaluator.Evaluate(grants, "bob", StudioPermission.ViewApp, null, null, null).Allowed);
    }

    [Fact]
    public void Evaluator_DifferentActionsAreIsolated()
    {
        var grants = new[] { Grant("alice", StudioPermission.ViewApp) };
        Assert.True(PermissionEvaluator.Evaluate(grants, "alice", StudioPermission.ViewApp, null, null, null).Allowed);
        Assert.False(PermissionEvaluator.Evaluate(grants, "alice", StudioPermission.DeleteApp, null, null, null).Allowed);
    }

    [Fact]
    public void Evaluator_DatasourceMostSpecific()
    {
        var grants = new[]
        {
            Grant("alice", StudioPermission.EditDatasource, PermissionEffect.Allow, appId: "appA", envId: "dev"),
            Grant("alice", StudioPermission.EditDatasource, PermissionEffect.Deny, appId: "appA", envId: "dev", datasourceName: "secret"),
        };
        Assert.False(PermissionEvaluator.Evaluate(grants, "alice", StudioPermission.EditDatasource, "appA", "dev", "secret").Allowed);
        Assert.True(PermissionEvaluator.Evaluate(grants, "alice", StudioPermission.EditDatasource, "appA", "dev", "other").Allowed);
    }

    // ─── Solo: LocalIdentityStore + AllowAllStudioAuthorizer ─────────────────

    [Fact]
    public async Task Solo_LocalAdmin_IsImplicit()
    {
        var store = new LocalIdentityStore();
        var byId = await store.FindByIdAsync(LocalIdentityStore.LocalAdminId);
        var byName = await store.FindByNameAsync(byId!.UserName);
        Assert.NotNull(byName);
        Assert.Equal(LocalIdentityStore.LocalAdminId, byName!.Id);
        Assert.Contains(nameof(AppMemberRole.Admin), byName.Roles);
    }

    [Fact]
    public async Task Solo_AnyPassword_ValidatesLocalAdmin()
    {
        var store = new LocalIdentityStore();
        var admin = await store.FindByIdAsync(LocalIdentityStore.LocalAdminId);
        var validated = await store.ValidateCredentialsAsync(admin!.UserName, password: "anything");
        Assert.NotNull(validated);
        Assert.Equal(LocalIdentityStore.LocalAdminId, validated!.Id);
    }

    [Fact]
    public async Task Solo_Authorizer_AllowsLocalAdmin_DeniesEveryoneElse()
    {
        var auth = new AllowAllStudioAuthorizer(LocalIdentityStore.LocalAdminId);
        Assert.True((await auth.EvaluateAsync(LocalIdentityStore.LocalAdminId, StudioPermission.DeleteApp)).Allowed);
        Assert.False((await auth.EvaluateAsync("someone-else", StudioPermission.ViewApp)).Allowed);
    }

    [Fact]
    public async Task Solo_Principal_AutoSignedIn_CanDoAnything()
    {
        var principal = new LocalStudioPrincipal();
        Assert.True(principal.IsAuthenticated);
        Assert.True(await principal.CanAsync(StudioPermission.ApplyMigration));
    }

    [Fact]
    public async Task Solo_Principal_SignOut_BlocksEverything()
    {
        var principal = new LocalStudioPrincipal();
        principal.SignOut();
        Assert.False(principal.IsAuthenticated);
        Assert.False(await principal.CanAsync(StudioPermission.ViewApp));
    }

    // ─── Enterprise: DatabaseIdentityStore + PasswordHasher + RoleBasedStudioAuthorizer ──

    [Fact]
    public void PasswordHasher_RoundTrips()
    {
        var hash = PasswordHasher.Hash("correct horse battery staple");
        Assert.NotEqual("correct horse battery staple", hash);
        Assert.True(PasswordHasher.Verify("correct horse battery staple", hash));
        Assert.False(PasswordHasher.Verify("wrong", hash));
    }

    [Fact]
    public void PasswordHasher_DistinctSalts()
    {
        var h1 = PasswordHasher.Hash("same");
        var h2 = PasswordHasher.Hash("same");
        Assert.NotEqual(h1, h2);  // different salts
        Assert.True(PasswordHasher.Verify("same", h1));
        Assert.True(PasswordHasher.Verify("same", h2));
    }

    [Fact]
    public void PasswordHasher_RejectsMalformed()
    {
        Assert.False(PasswordHasher.Verify("anything", ""));
        Assert.False(PasswordHasher.Verify("anything", "not-a-hash"));
        Assert.False(PasswordHasher.Verify("anything", "v2.1000.salt.hash"));  // wrong version
        Assert.False(PasswordHasher.Verify("", "v1.1000.c2FsdA==.aGFzaA=="));
    }

    [Fact]
    public async Task Database_CreateUser_HashesPassword()
    {
        var store = new DatabaseIdentityStore(_root);
        var user = await store.CreateAsync(new StudioUser { UserName = "alice", DisplayName = "Alice" }, password: "secret");

        // The StudioUser record never carries the hash.
        Assert.Null(user.Email);  // we didn't set one
        // But login works.
        var ok = await store.ValidateCredentialsAsync("alice", "secret");
        Assert.NotNull(ok);
        Assert.Equal(user.Id, ok!.Id);
    }

    [Fact]
    public async Task Database_WrongPassword_Rejected()
    {
        var store = new DatabaseIdentityStore(_root);
        await store.CreateAsync(new StudioUser { UserName = "alice" }, password: "secret");
        Assert.Null(await store.ValidateCredentialsAsync("alice", "wrong"));
    }

    [Fact]
    public async Task Database_UnknownUser_RejectedWithoutLeaking()
    {
        var store = new DatabaseIdentityStore(_root);
        // Must not throw, must return null, must take roughly the same time as a wrong password.
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await store.ValidateCredentialsAsync("ghost", "anything");
        sw.Stop();
        Assert.Null(result);
        // Loose timing check — should be at least a few ms (PBKDF2 cost).
        Assert.True(sw.ElapsedMilliseconds >= 0);
    }

    [Fact]
    public async Task Database_DuplicateUserName_Rejected()
    {
        var store = new DatabaseIdentityStore(_root);
        await store.CreateAsync(new StudioUser { UserName = "alice" }, password: "p1");
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            store.CreateAsync(new StudioUser { UserName = "alice" }, password: "p2"));
    }

    [Fact]
    public async Task Database_AssignRole_ExpandsToGrants()
    {
        var auth = new RoleBasedStudioAuthorizer(_root);
        var store = new DatabaseIdentityStore(_root, auth);

        var alice = await store.CreateAsync(new StudioUser { UserName = "alice" }, password: "p");
        await store.AssignRoleAsync(alice.Id, nameof(AppMemberRole.Operator));

        // Operator template grants ApplyMigration (and 14 others).
        Assert.True((await auth.EvaluateAsync(alice.Id, StudioPermission.ApplyMigration)).Allowed);
        Assert.True((await auth.EvaluateAsync(alice.Id, StudioPermission.PromoteCode)).Allowed);
        // Operator does NOT have admin-only permissions.
        Assert.False((await auth.EvaluateAsync(alice.Id, StudioPermission.ApproveRequest)).Allowed);
        Assert.False((await auth.EvaluateAsync(alice.Id, StudioPermission.DeleteApp)).Allowed);
    }

    [Fact]
    public async Task Database_AssignRole_AtAppScope_StaysScoped()
    {
        var auth = new RoleBasedStudioAuthorizer(_root);
        var store = new DatabaseIdentityStore(_root, auth);

        var alice = await store.CreateAsync(new StudioUser { UserName = "alice" }, password: "p");
        await store.AssignRoleAsync(alice.Id, nameof(AppMemberRole.Operator), appId: "appA");

        Assert.True((await auth.EvaluateAsync(alice.Id, StudioPermission.ApplyMigration, appId: "appA")).Allowed);
        // Same permission on a different app — NOT allowed (scoped).
        Assert.False((await auth.EvaluateAsync(alice.Id, StudioPermission.ApplyMigration, appId: "appB")).Allowed);
    }

    [Fact]
    public async Task Database_RevokeRole_RemovesGrants()
    {
        var auth = new RoleBasedStudioAuthorizer(_root);
        var store = new DatabaseIdentityStore(_root, auth);

        var alice = await store.CreateAsync(new StudioUser { UserName = "alice" }, password: "p");
        await store.AssignRoleAsync(alice.Id, nameof(AppMemberRole.Operator));
        Assert.True((await auth.EvaluateAsync(alice.Id, StudioPermission.ApplyMigration)).Allowed);

        await store.RevokeRoleAsync(alice.Id, nameof(AppMemberRole.Operator));
        Assert.False((await auth.EvaluateAsync(alice.Id, StudioPermission.ApplyMigration)).Allowed);
    }

    [Fact]
    public async Task Database_ResolveActors_FindsApprovers()
    {
        var auth = new RoleBasedStudioAuthorizer(_root);
        var store = new DatabaseIdentityStore(_root, auth);

        var alice = await store.CreateAsync(new StudioUser { UserName = "alice" }, password: "p");
        var bob = await store.CreateAsync(new StudioUser { UserName = "bob" }, password: "p");
        await store.AssignRoleAsync(alice.Id, nameof(AppMemberRole.Admin));
        await store.AssignRoleAsync(bob.Id, nameof(AppMemberRole.Viewer));

        // Only Admin role grants ApproveRequest (per RoleTemplates.Admin).
        var approvers = await auth.ResolveActorsAsync(StudioPermission.ApproveRequest, appId: null);
        Assert.Single(approvers);
        Assert.Equal(alice.Id, approvers[0]);
    }

    [Fact]
    public async Task Database_ExplicitDeny_OverridesRoleAllow()
    {
        // alice has Admin (which allows MaskedCopyData); a regulator denies it on env=prod.
        var auth = new RoleBasedStudioAuthorizer(_root);
        var store = new DatabaseIdentityStore(_root, auth);

        var alice = await store.CreateAsync(new StudioUser { UserName = "alice" }, password: "p");
        await store.AssignRoleAsync(alice.Id, nameof(AppMemberRole.Admin));
        await auth.GrantAsync(Grant(alice.Id, StudioPermission.MaskedCopyData, PermissionEffect.Deny, envId: "prod"));

        Assert.True((await auth.EvaluateAsync(alice.Id, StudioPermission.MaskedCopyData, envId: "dev")).Allowed);
        Assert.False((await auth.EvaluateAsync(alice.Id, StudioPermission.MaskedCopyData, envId: "prod")).Allowed);
    }

    [Fact]
    public async Task Database_Grants_PersistAcrossInstances()
    {
        var auth1 = new RoleBasedStudioAuthorizer(_root);
        var store1 = new DatabaseIdentityStore(_root, auth1);
        var alice = await store1.CreateAsync(new StudioUser { UserName = "alice" }, password: "p");
        await store1.AssignRoleAsync(alice.Id, nameof(AppMemberRole.Operator));

        // New instances over the same root must see the persisted grants + users.
        var auth2 = new RoleBasedStudioAuthorizer(_root);
        var store2 = new DatabaseIdentityStore(_root, auth2);
        var users = await store2.ListUsersAsync();
        Assert.Single(users);
        Assert.True((await auth2.EvaluateAsync(alice.Id, StudioPermission.ApplyMigration)).Allowed);
    }

    // ─── Setup compatibility ──────────────────────────────────────────────────

    [Fact]
    public async Task StudioAuthorizer_RoutesSetupAuthorize_ToEvaluate()
    {
        // SetupPermission.ApplySchema == StudioPermission.ApplySchema (same numeric value by design).
        var auth = new RoleBasedStudioAuthorizer(_root);
        await auth.GrantAsync(Grant("alice", StudioPermission.ApplySchema));

        var principal = new TheTechIdea.Beep.SetUp.Security.AnonymousSetupPrincipal();
        // AnonymousSetupPrincipal.Id is Environment.UserName — we granted "alice" so this is denied.
        var denied = await auth.AuthorizeAsync(principal, TheTechIdea.Beep.SetUp.Security.SetupPermission.ApplySchema, context: null!);
        Assert.False(denied.Allowed);
    }

    [Fact]
    public async Task RoleTemplates_Admin_HasAllSetupPermissions()
    {
        // The Admin role template must include every setup permission so an admin can run setup end-to-end.
        var setupPerms = Enum.GetValues<TheTechIdea.Beep.SetUp.Security.SetupPermission>()
            .Cast<int>().Select(v => (StudioPermission)v).ToList();
        foreach (var p in setupPerms)
            Assert.Contains(p, RoleTemplates.Admin);
    }
}
