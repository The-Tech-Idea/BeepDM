using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.SetUp.Steps;

namespace TheTechIdea.Beep.SetUp.Tests;

public class SetupWizardBuilderTests
{
    [Fact]
    public void Build_WithNoSteps_ReturnsValidWizard()
    {
        var wizard = new SetupWizardBuilder()
            .WithId("test-wizard")
            .Build();

        Assert.NotNull(wizard);
        Assert.Empty(wizard.Steps);
    }

    [Fact]
    public void Build_WithValidSteps_ReturnsWizard()
    {
        var step1 = new DriverProvisionStep(new DriverProvisionStepOptions { PackageName = "test.package" });
        var step2 = new ConnectionConfigStep(new ConnectionConfigStepOptions
        {
            ConnectionProperties = new ConfigUtil.ConnectionProperties { ConnectionName = "TestDB" }
        });

        var wizard = new SetupWizardBuilder()
            .AddStep(step1)
            .AddStep(step2)
            .Build();

        Assert.Equal(2, wizard.Steps.Count);
        Assert.Equal("driver-provision", wizard.Steps[0].StepId);
        Assert.Equal("connection-config", wizard.Steps[1].StepId);
    }

    [Fact]
    public void Build_Throws_WhenDependencyMissing()
    {
        var step2 = new ConnectionConfigStep(new ConnectionConfigStepOptions
        {
            ConnectionProperties = new ConfigUtil.ConnectionProperties { ConnectionName = "TestDB" }
        });

        Assert.Throws<InvalidOperationException>(() =>
        {
            new SetupWizardBuilder()
                .AddStep(step2) // depends on driver-provision which is not registered
                .Build();
        });
    }

    [Fact]
    public void Build_Throws_WhenStepsOutOfOrder()
    {
        var step1 = new DriverProvisionStep(new DriverProvisionStepOptions { PackageName = "test.package" });
        var step2 = new ConnectionConfigStep(new ConnectionConfigStepOptions
        {
            ConnectionProperties = new ConfigUtil.ConnectionProperties { ConnectionName = "TestDB" }
        });

        Assert.Throws<InvalidOperationException>(() =>
        {
            new SetupWizardBuilder()
                .AddStep(step2) // depends on driver-provision, registered after
                .AddStep(step1)
                .Build();
        });
    }

    [Fact]
    public void WithOptions_AppliesConfiguration()
    {
        var options = new SetupOptions
        {
            DryRun = true,
            Environment = "Staging",
            SkipSeeding = true,
            StateFilePath = "/tmp/state.json"
        };

        var wizard = new SetupWizardBuilder()
            .WithOptions(options)
            .Build();

        Assert.True(wizard.Options.DryRun);
        Assert.Equal("Staging", wizard.Options.Environment);
        Assert.True(wizard.Options.SkipSeeding);
        Assert.Equal("/tmp/state.json", wizard.Options.StateFilePath);
    }

    [Fact]
    public void WithDryRun_SetsOption()
    {
        var wizard = new SetupWizardBuilder()
            .WithDryRun()
            .Build();

        Assert.True(wizard.Options.DryRun);
    }

    [Fact]
    public void WithEnvironment_SetsOption()
    {
        var wizard = new SetupWizardBuilder()
            .WithEnvironment("Production")
            .Build();

        Assert.Equal("Production", wizard.Options.Environment);
    }
}
