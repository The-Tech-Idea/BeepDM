using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.SetUp.Steps;

namespace TheTechIdea.Beep.SetUp.Tests;

public class DryRunWorkflowTests
{
    [Fact]
    public void AllSteps_Skip_WhenDryRunTrue()
    {
        var context = new SetupContext
        {
            Options = new SetupOptions { DryRun = true, SkipSchema = false, SkipSeeding = false },
            Editor = null! // Skip the Editor check — DryRun takes priority
        };

        var driverStep = new DriverProvisionStep(new DriverProvisionStepOptions { PackageName = "test" });
        Assert.True(driverStep.CanSkip(context));

        var connStep = new ConnectionConfigStep(new ConnectionConfigStepOptions
        {
            ConnectionProperties = new ConnectionProperties { ConnectionName = "TestDB" }
        });
        Assert.True(connStep.CanSkip(context));

        var defaultsStep = new DefaultsSetupStep(new DefaultsSetupStepOptions { ApplyDefaults = true });
        Assert.True(defaultsStep.CanSkip(context));

        var dataImportStep = new DataImportStep(new DataImportStepOptions { EntityNames = ["Entity1"] });
        Assert.True(dataImportStep.CanSkip(context));
    }

    [Fact]
    public void SeedingStep_Skips_WhenDryRunTrue()
    {
        var registry = new SeederRegistry();
        registry.Register(new CountingSeeder("s1"));
        var step = new SeedingStep(new SeedingStepOptions { Registry = registry });
        var context = new SetupContext
        {
            Options = new SetupOptions { DryRun = true, SkipSeeding = false }
        };

        Assert.True(step.CanSkip(context));
    }

    [Fact]
    public void Steps_DoNotSkip_WhenDryRunFalse_AndPreconditionsMet()
    {
        var context = new SetupContext
        {
            Options = new SetupOptions { DryRun = false }
        };

        // DriverProvision with a valid package — CanSkip checks driver state, not just DryRun
        var driverStep = new DriverProvisionStep(new DriverProvisionStepOptions { PackageName = "test.pkg" });
        // Without a real editor with DataDriversClasses, FindDriver returns null → CanSkip returns false
        Assert.False(driverStep.CanSkip(context));
    }

    [Fact]
    public void DryRun_Prevents_Modification_Steps_FromRunning()
    {
        // Options MUST be shared between wizard and context for DryRun to propagate.
        // The factory (DefaultSetupWizardFactory) always does this.
        var options = new SetupOptions { DryRun = true };
        var step = new DryRunAwareStep("modify-step", "Modify", Array.Empty<string>(),
            _ => new ErrorsInfo { Flag = Errors.Failed, Message = "Should not be called" });

        var wizard = new SetupWizardBuilder()
            .WithOptions(options)
            .AddStep(step)
            .Build();

        var context = new SetupContext { Options = options };
        var result = wizard.Run(context);

        Assert.True(result.Flag == Errors.Ok);
        Assert.Contains("modify-step", wizard.State.SkippedStepIds);
    }

    private sealed class DryRunAwareStep : ISetupStep
    {
        private readonly string _id, _name;
        private readonly string[] _deps;
        private readonly Func<SetupContext, TheTechIdea.Beep.ConfigUtil.IErrorsInfo> _exec;
        public DryRunAwareStep(string id, string name, string[] deps, Func<SetupContext, TheTechIdea.Beep.ConfigUtil.IErrorsInfo> exec)
        { _id = id; _name = name; _deps = deps; _exec = exec; }
        public string StepId => _id;
        public string StepName => _name;
        public string Description => "";
        public IReadOnlyList<string> DependsOn => _deps;
        public bool CanSkip(SetupContext context) => context.Options?.DryRun == true;
        TheTechIdea.Beep.ConfigUtil.IErrorsInfo ISetupStep.Validate(SetupContext context)
            => new ErrorsInfo { Flag = Errors.Ok };
        TheTechIdea.Beep.ConfigUtil.IErrorsInfo ISetupStep.Execute(SetupContext context, IProgress<PassedArgs>? progress)
            => _exec(context);
    }

    private static IErrorsInfo Ok(string msg = "Ok") =>
        new ErrorsInfo { Flag = Errors.Ok, Message = msg };

    private sealed class CountingSeeder : ISeeder
    {
        public CountingSeeder(string id) { SeederId = id; SeederName = id; }
        public string SeederId { get; }
        public string SeederName { get; }
        public IReadOnlyList<string> DependsOn => Array.Empty<string>();
        bool ISeeder.IsAlreadySeeded(IDataSource dataSource, IDMEEditor editor) => false;
        TheTechIdea.Beep.ConfigUtil.IErrorsInfo ISeeder.Seed(IDataSource dataSource, IDMEEditor editor, IProgress<PassedArgs>? progress)
            => new ErrorsInfo { Flag = Errors.Ok };
    }
}
