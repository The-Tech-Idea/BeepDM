using TheTechIdea.Beep.SetUp.Security;

namespace TheTechIdea.Beep.SetUp.Tests;

/// <summary>
/// Guards for Phase 5 (.plans/setup/PHASE-05-Identity-RBAC-Approvals.md): identity + RBAC + real
/// approvals for enterprise, with a zero-ceremony solo default that must never break.
/// </summary>
public class SecurityTests
{
    private sealed class TestStep : ISetupStep
    {
        private readonly SetupPermission _perm;
        public bool Ran { get; private set; }
        public TestStep(string id, SetupPermission perm) { StepId = id; _perm = perm; }

        public string StepId { get; }
        public string StepName => StepId;
        public string Description => StepId;
        public IReadOnlyList<string> DependsOn => Array.Empty<string>();
        public SetupPermission RequiredPermission => _perm;
        public bool CanSkip(SetupContext context) => false;
        public IErrorsInfo Validate(SetupContext context) => new ErrorsInfo { Flag = Errors.Ok };
        public IErrorsInfo Execute(SetupContext context, IProgress<PassedArgs> progress = null)
        {
            Ran = true;
            return new ErrorsInfo { Flag = Errors.Ok };
        }
    }

    private sealed class FakePrincipal : ISetupPrincipal
    {
        public FakePrincipal(string id, bool authed, params string[] roles)
        { Id = id; IsAuthenticated = authed; Roles = roles; }
        public string Id { get; }
        public string DisplayName => Id;
        public IReadOnlyCollection<string> Roles { get; }
        public bool IsAuthenticated { get; }
    }

    private static SetupContext NewContext() =>
        new() { Options = new SetupOptions(), State = new SetupState() };

    // ── the design rule: solo must not break ─────────────────────────────────

    [Fact]
    public void Solo_NoSecurityConfig_StillRuns()
    {
        var step = new TestStep("a", SetupPermission.ApplySchema);
        var wizard = new SetupWizardBuilder().WithId("solo").AddStep(step).Build();

        Assert.Equal(Errors.Ok, wizard.Run(NewContext()).Flag);
        Assert.True(step.Ran);
    }

    [Fact]
    public void Solo_RecordsAnonymousActor_NotAuthenticated()
    {
        var wizard = new SetupWizardBuilder().WithId("solo")
            .AddStep(new TestStep("a", SetupPermission.Seed)).Build();

        wizard.Run(NewContext());

        Assert.False(wizard.GetReport().ActorAuthenticated);   // never claim solo was authenticated
        Assert.False(string.IsNullOrEmpty(wizard.GetReport().ActorId));
    }

    // ── RBAC enforcement ─────────────────────────────────────────────────────

    [Fact]
    public void DeniedPermission_FailsStep_DoesNotThrow_AndStepNeverRuns()
    {
        var step = new TestStep("schema", SetupPermission.ApplySchema);
        var authorizer = new RoleBasedSetupAuthorizer(new Dictionary<SetupPermission, string[]>
        {
            [SetupPermission.RunSetup] = new[] { "operator" },
            [SetupPermission.ApplySchema] = new[] { "dba" }   // operator lacks this
        });
        var wizard = new SetupWizardBuilder().WithId("rbac")
            .WithSecurity(new FakePrincipal("alice", authed: true, "operator"), authorizer)
            .AddStep(step).Build();

        var result = wizard.Run(NewContext());   // must not throw

        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Contains("Not authorized", result.Message);
        Assert.False(step.Ran);   // denial happens before Execute
    }

    [Fact]
    public void GrantedPermission_Runs()
    {
        var step = new TestStep("schema", SetupPermission.ApplySchema);
        var authorizer = new RoleBasedSetupAuthorizer(new Dictionary<SetupPermission, string[]>
        {
            [SetupPermission.RunSetup] = new[] { "dba" },
            [SetupPermission.ApplySchema] = new[] { "dba" }
        });
        var wizard = new SetupWizardBuilder().WithId("rbac")
            .WithSecurity(new FakePrincipal("bob", authed: true, "dba"), authorizer)
            .AddStep(step).Build();

        Assert.Equal(Errors.Ok, wizard.Run(NewContext()).Flag);
        Assert.True(step.Ran);
    }

    [Fact]
    public void UnauthenticatedPrincipal_IsDenied_ByRoleAuthorizer()
    {
        var authorizer = new RoleBasedSetupAuthorizer(new Dictionary<SetupPermission, string[]>
        {
            [SetupPermission.RunSetup] = new[] { "dba" }
        });
        var wizard = new SetupWizardBuilder().WithId("rbac")
            .WithSecurity(new FakePrincipal("anon", authed: false, "dba"), authorizer)
            .AddStep(new TestStep("a", SetupPermission.RunSetup)).Build();

        Assert.Equal(Errors.Failed, wizard.Run(NewContext()).Flag);
    }

    [Fact]
    public void State_Records_AuthenticatedActor()
    {
        var authorizer = new AllowAllAuthorizer();
        var wizard = new SetupWizardBuilder().WithId("actor")
            .WithSecurity(new FakePrincipal("carol", authed: true, "dba"), authorizer)
            .AddStep(new TestStep("a", SetupPermission.Seed)).Build();

        wizard.Run(NewContext());

        Assert.Equal("carol", wizard.GetReport().ActorId);
        Assert.True(wizard.GetReport().ActorAuthenticated);
    }

    // ── approvals ────────────────────────────────────────────────────────────

    [Fact]
    public async Task AutoApproval_RecordsSelfApproved_True()
    {
        var approval = await new AutoApprovalProvider()
            .RequestApprovalAsync(NewContext(), new FakePrincipal("dev", authed: false), "plan-1");

        Assert.True(approval.Granted);
        Assert.True(approval.IsSelfApproved);   // honest, not laundered
        Assert.Equal("plan-1", approval.PlanHash);
    }

    [Fact]
    public async Task Enterprise_RejectsSelfApproval()
    {
        // Approver lookup returns the SAME id as the requester.
        var provider = new SeparationOfDutyApprovalProvider((_, _, _) => Task.FromResult("alice"));

        var approval = await provider.RequestApprovalAsync(
            NewContext(), new FakePrincipal("alice", authed: true, "dba"), "plan-1");

        Assert.False(approval.Granted);
        Assert.True(approval.IsSelfApproved);
        Assert.Contains("Self-approval rejected", approval.Note);
    }

    [Fact]
    public async Task Enterprise_AllowsDistinctApprover()
    {
        var provider = new SeparationOfDutyApprovalProvider((_, _, _) => Task.FromResult("bob"));

        var approval = await provider.RequestApprovalAsync(
            NewContext(), new FakePrincipal("alice", authed: true, "dba"), "plan-1");

        Assert.True(approval.Granted);
        Assert.False(approval.IsSelfApproved);
        Assert.Equal("bob", approval.ApproverId);
    }

    [Fact]
    public async Task Enterprise_Denies_WhenNoApproverSignedOff()
    {
        var provider = new SeparationOfDutyApprovalProvider((_, _, _) => Task.FromResult<string>(null));

        var approval = await provider.RequestApprovalAsync(
            NewContext(), new FakePrincipal("alice", authed: true), "plan-1");

        Assert.False(approval.Granted);
    }
}
