using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.SetUp.Tests;

public class SetupWizardOrchestrationTests
{
    [Fact]
    public void Run_ExecutesSteps_InOrder()
    {
        var executed = new List<string>();
        var step1 = new DelegateStep("step-1", "Step 1", Array.Empty<string>(),
            _ => { executed.Add("step-1"); return Ok(); });
        var step2 = new DelegateStep("step-2", "Step 2", new[] { "step-1" },
            _ => { executed.Add("step-2"); return Ok(); });

        var wizard = new SetupWizardBuilder()
            .AddStep(step1)
            .AddStep(step2)
            .Build();

        var context = new SetupContext();
        var result = wizard.Run(context);

        Assert.True(result.Flag == Errors.Ok, result.Message);
        Assert.Equal(new[] { "step-1", "step-2" }, executed);
    }

    [Fact]
    public void Run_SkipsCompletedSteps()
    {
        var executed = new List<string>();
        var step1 = new DelegateStep("step-1", "Step 1", Array.Empty<string>(),
            _ => { executed.Add("step-1"); return Ok(); });
        var step2 = new DelegateStep("step-2", "Step 2", new[] { "step-1" },
            _ => { executed.Add("step-2"); return Ok(); });

        var wizard = new SetupWizardBuilder()
            .AddStep(step1)
            .AddStep(step2)
            .Build();

        var context = new SetupContext();
        context.State.CompletedStepIds.Add("step-1");

        var result = wizard.Run(context);

        Assert.True(result.Flag == Errors.Ok, result.Message);
        Assert.Equal(new[] { "step-2" }, executed);
    }

    [Fact]
    public void Run_AssignsRunId_OnFreshRun()
    {
        var wizard = new SetupWizardBuilder().Build();
        var context = new SetupContext();

        wizard.Run(context);

        Assert.NotNull(wizard.State.RunId);
        Assert.NotEmpty(wizard.State.RunId);
    }

    [Fact]
    public void Run_PreservesRunId_OnResume()
    {
        var wizard = new SetupWizardBuilder().Build();
        var context = new SetupContext { Options = new SetupOptions() };

        wizard.Run(context);
        var firstRunId = wizard.State.RunId;

        wizard.Run(context);

        Assert.Equal(firstRunId, wizard.State.RunId);
    }

    [Fact]
    public void Run_Stops_OnFirstFailure()
    {
        var executed = new List<string>();
        var step1 = new DelegateStep("step-1", "Step 1", Array.Empty<string>(),
            _ => { executed.Add("step-1"); return Ok(); });
        var step2 = new DelegateStep("step-2", "Step 2", new[] { "step-1" },
            _ => { executed.Add("step-2"); return Fail("step-2 failed"); });
        var step3 = new DelegateStep("step-3", "Step 3", new[] { "step-2" },
            _ => { executed.Add("step-3"); return Ok(); });

        var wizard = new SetupWizardBuilder()
            .AddStep(step1)
            .AddStep(step2)
            .AddStep(step3)
            .Build();

        var context = new SetupContext();
        var result = wizard.Run(context);

        Assert.True(result.Flag == Errors.Failed);
        Assert.Equal(new[] { "step-1", "step-2" }, executed);
        Assert.Equal("step-2", wizard.State.FailedStepId);
    }

    [Fact]
    public void Run_ReturnsError_WhenContextNull()
    {
        var wizard = new SetupWizardBuilder().Build();
        var result = wizard.Run(null!);

        Assert.True(result.Flag == Errors.Failed);
        Assert.Contains("must not be null", result.Message);
    }

    [Fact]
    public void Run_ValidatesStepGraph_BeforeExecution()
    {
        var step = new DelegateStep("bad-step", "Bad", new[] { "unknown" }, _ => Ok());

        var directWizard = new SetupWizard("test", new[] { step }, new SetupOptions());
        var context = new SetupContext();
        var result = directWizard.Run(context);

        Assert.True(result.Flag == Errors.Failed);
        Assert.Contains("unknown step", result.Message);
    }

    [Fact]
    public void Run_CanSkip_SkipsStep()
    {
        var step = new DelegateStep("skip-step", "Skip", Array.Empty<string>(),
            _ => Ok(), canSkip: _ => true);

        var wizard = new SetupWizardBuilder()
            .AddStep(step)
            .Build();

        var context = new SetupContext();
        var result = wizard.Run(context);

        Assert.True(result.Flag == Errors.Ok, result.Message);
        Assert.Contains("skip-step", wizard.State.SkippedStepIds);
    }

    [Fact]
    public void Report_ContainsContentHash_AfterRun()
    {
        var wizard = new SetupWizardBuilder()
            .AddStep(new DelegateStep("s1", "S1", Array.Empty<string>(), _ => Ok()))
            .Build();

        var context = new SetupContext();
        wizard.Run(context);

        var report = wizard.GetReport();
        Assert.NotNull(report);
        Assert.NotNull(report.ContentHash);
        Assert.NotEmpty(report.ContentHash);
    }

    private static IErrorsInfo Ok(string msg = "Ok") =>
        new ErrorsInfo { Flag = Errors.Ok, Message = msg };

    private static IErrorsInfo Fail(string msg) =>
        new ErrorsInfo { Flag = Errors.Failed, Message = msg };

    private sealed class DelegateStep : ISetupStep
    {
        private readonly string _stepId;
        private readonly string _stepName;
        private readonly string[] _dependsOn;
        private readonly Func<SetupContext, TheTechIdea.Beep.ConfigUtil.IErrorsInfo> _execute;
        private readonly Func<SetupContext, bool>? _canSkip;

        public DelegateStep(
            string stepId,
            string stepName,
            string[] dependsOn,
            Func<SetupContext, TheTechIdea.Beep.ConfigUtil.IErrorsInfo> execute,
            Func<SetupContext, bool>? canSkip = null)
        {
            _stepId = stepId;
            _stepName = stepName;
            _dependsOn = dependsOn;
            _execute = execute;
            _canSkip = canSkip;
        }

        public string StepId => _stepId;
        public string StepName => _stepName;
        public string Description => "";
        public IReadOnlyList<string> DependsOn => _dependsOn;

        public bool CanSkip(SetupContext context) => _canSkip?.Invoke(context) ?? false;

        TheTechIdea.Beep.ConfigUtil.IErrorsInfo ISetupStep.Validate(SetupContext context)
            => new ErrorsInfo { Flag = Errors.Ok };

        TheTechIdea.Beep.ConfigUtil.IErrorsInfo ISetupStep.Execute(SetupContext context, IProgress<PassedArgs>? progress)
            => _execute(context);
    }
}
