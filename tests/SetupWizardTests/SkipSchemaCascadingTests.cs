using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.SetUp.Steps;

namespace TheTechIdea.Beep.SetUp.Tests;

public class SkipSchemaCascadingTests
{
    [Fact]
    public void DefaultsSetupStep_CanSkip_WhenSkipSchemaTrue()
    {
        var step = new DefaultsSetupStep(new DefaultsSetupStepOptions { ApplyDefaults = true });
        var context = new SetupContext
        {
            Options = new SetupOptions { SkipSchema = true },
            Editor = null! // Editor is null, but SkipSchema should take priority
        };

        Assert.True(step.CanSkip(context));
    }

    [Fact]
    public void DefaultsSetupStep_DoesNotSkip_WhenSkipSchemaFalse()
    {
        var step = new DefaultsSetupStep(new DefaultsSetupStepOptions { ApplyDefaults = true });
        var context = new SetupContext
        {
            Options = new SetupOptions { SkipSchema = false }
        };

        // Editor is null, so CanSkip returns true (no ConfigEditor)
        Assert.True(step.CanSkip(context));
    }

    [Fact]
    public void SeedingStep_CanSkip_WhenSkipSchemaTrue()
    {
        var registry = new SeederRegistry();
        registry.Register(new SimpleSeeder("s1"));
        var step = new SeedingStep(new SeedingStepOptions { Registry = registry });
        var context = new SetupContext
        {
            Options = new SetupOptions { SkipSchema = true, SkipSeeding = false }
        };

        Assert.True(step.CanSkip(context));
    }

    [Fact]
    public void SeedingStep_DoesNotSkip_WhenOnlySkipSeedingFalse()
    {
        var registry = new SeederRegistry();
        registry.Register(new SimpleSeeder("s1"));
        var step = new SeedingStep(new SeedingStepOptions { Registry = registry });
        var context = new SetupContext
        {
            Options = new SetupOptions { SkipSchema = false, SkipSeeding = false }
        };

        // No datasource means IsAlreadySeeded won't match, so CanSkip returns false
        Assert.False(step.CanSkip(context));
    }

    [Fact]
    public void DataImportStep_CanSkip_WhenSkipSchemaTrue()
    {
        var step = new DataImportStep(new DataImportStepOptions { EntityNames = new List<string> { "SomeEntity" } });
        var context = new SetupContext
        {
            Options = new SetupOptions { SkipSchema = true }
        };

        Assert.True(step.CanSkip(context));
    }

    [Fact]
    public void DataImportStep_DoesNotSkip_WhenSkipSchemaFalse()
    {
        var step = new DataImportStep(new DataImportStepOptions { EntityNames = new List<string> { "SomeEntity" } });
        var context = new SetupContext
        {
            Options = new SetupOptions { SkipSchema = false }
        };

        Assert.False(step.CanSkip(context));
    }

    [Fact]
    public void RunId_PropagatesToReport()
    {
        var wizard = new SetupWizardBuilder()
            .AddStep(new DelegateStep("s1", "S1", Array.Empty<string>(), _ => Ok()))
            .Build();

        var context = new SetupContext();
        wizard.Run(context);

        var report = wizard.GetReport();
        Assert.NotNull(report);
        Assert.Equal(wizard.State.RunId, report.RunId);
    }

    [Fact]
    public void RunId_PropagatesToContext()
    {
        var wizard = new SetupWizardBuilder()
            .AddStep(new DelegateStep("s1", "S1", Array.Empty<string>(), _ => Ok()))
            .Build();

        var context = new SetupContext();
        wizard.Run(context);

        Assert.Equal(wizard.State.RunId, context.State.RunId);
    }

    private static IErrorsInfo Ok(string msg = "Ok") =>
        new ErrorsInfo { Flag = Errors.Ok, Message = msg };

    private sealed class SimpleSeeder : ISeeder
    {
        public SimpleSeeder(string id) { SeederId = id; SeederName = id; }
        public string SeederId { get; }
        public string SeederName { get; }
        public IReadOnlyList<string> DependsOn => Array.Empty<string>();
        bool ISeeder.IsAlreadySeeded(IDataSource dataSource, IDMEEditor editor) => false;
        TheTechIdea.Beep.ConfigUtil.IErrorsInfo ISeeder.Seed(IDataSource dataSource, IDMEEditor editor, IProgress<PassedArgs>? progress)
            => new ErrorsInfo { Flag = Errors.Ok };
    }

    private sealed class DelegateStep : ISetupStep
    {
        private readonly string _id;
        private readonly string _name;
        private readonly string[] _deps;
        private readonly Func<SetupContext, TheTechIdea.Beep.ConfigUtil.IErrorsInfo> _exec;
        public DelegateStep(string id, string name, string[] deps, Func<SetupContext, TheTechIdea.Beep.ConfigUtil.IErrorsInfo> exec)
        { _id = id; _name = name; _deps = deps; _exec = exec; }
        public string StepId => _id;
        public string StepName => _name;
        public string Description => "";
        public IReadOnlyList<string> DependsOn => _deps;
        public bool CanSkip(SetupContext context) => false;
        TheTechIdea.Beep.ConfigUtil.IErrorsInfo ISetupStep.Validate(SetupContext context)
            => new ErrorsInfo { Flag = Errors.Ok };
        TheTechIdea.Beep.ConfigUtil.IErrorsInfo ISetupStep.Execute(SetupContext context, IProgress<PassedArgs>? progress)
            => _exec(context);
    }
}
