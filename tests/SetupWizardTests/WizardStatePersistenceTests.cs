using TheTechIdea.Beep.SetUp.State;

namespace TheTechIdea.Beep.SetUp.Tests;

/// <summary>
/// Phase 3 at the wizard level: state persists and resumes through the store, and a second runner
/// is refused while the lease is held.
/// </summary>
public class WizardStatePersistenceTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(), "beep-wizard-tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        try { if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true); } catch { }
    }

    /// <summary>A step that records how many times it actually executed, so we can prove resume skips it.</summary>
    private sealed class CountingStep : ISetupStep
    {
        private readonly string _id;
        public int ExecuteCount { get; private set; }
        public CountingStep(string id) => _id = id;

        public string StepId => _id;
        public string StepName => _id;
        public string Description => _id;
        public IReadOnlyList<string> DependsOn => Array.Empty<string>();
        public bool CanSkip(SetupContext context) => false;
        public IErrorsInfo Validate(SetupContext context) => new ErrorsInfo { Flag = Errors.Ok };
        public IErrorsInfo Execute(SetupContext context, IProgress<PassedArgs> progress = null)
        {
            ExecuteCount++;
            return new ErrorsInfo { Flag = Errors.Ok, Message = "done" };
        }
    }

    private SetupContext NewContext() => new()
    {
        Options = new SetupOptions { Environment = "Development" },
        State = new SetupState()
    };

    [Fact]
    public void CompletedSteps_Persist_AndResumeSkipsThem()
    {
        var store = new LocalJsonSetupStateStore(_root);
        var stepA = new CountingStep("a");
        var stepB = new CountingStep("b");

        var wizard1 = new SetupWizardBuilder()
            .WithId("persist-wiz")
            .WithStateStore(store)
            .AddStep(stepA).AddStep(stepB)
            .Build();

        var r1 = wizard1.Run(NewContext());
        Assert.Equal(Errors.Ok, r1.Flag);
        Assert.Equal(1, stepA.ExecuteCount);
        Assert.Equal(1, stepB.ExecuteCount);

        // A brand-new wizard instance (as after a restart) with fresh steps, same store + id.
        var stepA2 = new CountingStep("a");
        var stepB2 = new CountingStep("b");
        var wizard2 = new SetupWizardBuilder()
            .WithId("persist-wiz")
            .WithStateStore(store)
            .AddStep(stepA2).AddStep(stepB2)
            .Build();

        var r2 = wizard2.Run(NewContext());
        Assert.Equal(Errors.Ok, r2.Flag);

        // Already-completed steps must NOT run again — that's the whole point of the checkpoint.
        Assert.Equal(0, stepA2.ExecuteCount);
        Assert.Equal(0, stepB2.ExecuteCount);
    }

    [Fact]
    public async Task SecondWizard_IsRefused_WhileFirstHoldsLease()
    {
        var store = new LocalJsonSetupStateStore(_root);
        var key = new SetupStateKey("locked-wiz", "Development");

        // Simulate a run in progress by holding the lease out-of-band.
        var held = await store.TryAcquireLeaseAsync(key, TimeSpan.FromMinutes(5));
        Assert.NotNull(held);

        var wizard = new SetupWizardBuilder()
            .WithId("locked-wiz")
            .WithStateStore(store)
            .AddStep(new CountingStep("a"))
            .Build();

        var result = wizard.Run(NewContext());

        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Contains("holds the setup lock", result.Message);

        await held!.DisposeAsync();

        // Once released, the wizard runs.
        Assert.Equal(Errors.Ok, wizard.Run(NewContext()).Flag);
    }

    [Fact]
    public void NoStore_StillRuns_WithoutCheckpointing()
    {
        // Back-compat: no store, no StateFilePath → runs, just isn't resumable.
        var step = new CountingStep("a");
        var wizard = new SetupWizardBuilder().WithId("no-store").AddStep(step).Build();

        Assert.Equal(Errors.Ok, wizard.Run(NewContext()).Flag);
        Assert.Equal(1, step.ExecuteCount);
    }

    [Fact]
    public void TwoWizards_DifferentIds_DoNotCollide_OnSharedStore()
    {
        var store = new LocalJsonSetupStateStore(_root);

        var a = new SetupWizardBuilder().WithId("wiz-a").WithStateStore(store)
            .AddStep(new CountingStep("only-a")).Build();
        var b = new SetupWizardBuilder().WithId("wiz-b").WithStateStore(store)
            .AddStep(new CountingStep("only-b")).Build();

        Assert.Equal(Errors.Ok, a.Run(NewContext()).Flag);
        Assert.Equal(Errors.Ok, b.Run(NewContext()).Flag);

        // Each wizard's checkpoint records only its own step — no cross-contamination.
        Assert.Contains("only-a", a.State.CompletedStepIds);
        Assert.DoesNotContain("only-b", a.State.CompletedStepIds);
        Assert.Contains("only-b", b.State.CompletedStepIds);
        Assert.DoesNotContain("only-a", b.State.CompletedStepIds);
    }
}
