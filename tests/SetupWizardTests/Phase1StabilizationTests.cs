using Moq;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.SetUp.Steps;
using TheTechIdea.Beep.Utilities;

// Moq also ships a DefaultValue type; alias the Beep one so the intent is unambiguous.
using DefaultValue = TheTechIdea.Beep.ConfigUtil.DefaultValue;

namespace TheTechIdea.Beep.SetUp.Tests;

/// <summary>
/// Regression guards for Phase 1 (.plans/setup/PHASE-01-Stabilize-Correctness.md).
/// Each test names the item it protects.
/// </summary>
public class Phase1StabilizationTests
{
    // ── helpers ──────────────────────────────────────────────────────────────

    private static Mock<IDMEEditor> EditorWithDrivers(params string[] packageNames)
    {
        var drivers = packageNames
            .Select(p => new ConnectionDriversConfig
            {
                PackageName = p,
                AutoLoad = true,
                NuggetVersion = "1.0.0",
                classHandler = $"{p}DataSource"
            })
            .ToList();

        var config = new Mock<IConfigEditor>();
        config.SetupGet(c => c.DataDriversClasses).Returns(drivers);

        var editor = new Mock<IDMEEditor>();
        editor.SetupGet(e => e.ConfigEditor).Returns(config.Object);
        return editor;
    }

    // ── 1-A  duplicate StepId crashed the default wizard ─────────────────────

    [Fact]
    public void CreateDefault_WithTwoAutoLoadDrivers_DoesNotThrow()
    {
        var editor = EditorWithDrivers("SQLite", "SqlServer");

        // Before 1-A both driver steps carried the constant id "driver-provision", so the
        // builder's step-id dictionary threw ArgumentException at DI-resolution time.
        var (wizard, _) = new DefaultSetupWizardFactory().CreateDefault(editor.Object);

        Assert.NotNull(wizard);
        var driverIds = wizard.Steps
            .Where(s => s.StepId.StartsWith(DriverProvisionStep.BaseStepId))
            .Select(s => s.StepId)
            .ToList();

        Assert.Equal(2, driverIds.Count);
        Assert.Equal(driverIds.Count, driverIds.Distinct().Count());
    }

    [Fact]
    public void CreateDefault_WithSingleDriver_KeepsBareStepId()
    {
        var editor = EditorWithDrivers("SQLite");

        var (wizard, _) = new DefaultSetupWizardFactory().CreateDefault(editor.Object);

        // Qualifying a lone driver would change the id every existing single-driver wizard
        // and its DependsOn references rely on.
        Assert.Contains(wizard.Steps, s => s.StepId == DriverProvisionStep.BaseStepId);
    }

    [Fact]
    public void Builder_Throws_WithNamedMessage_OnDuplicateStepId()
    {
        var a = new DriverProvisionStep(new DriverProvisionStepOptions { PackageName = "x" });
        var b = new DriverProvisionStep(new DriverProvisionStepOptions { PackageName = "y" });

        var ex = Assert.Throws<InvalidOperationException>(() =>
            new SetupWizardBuilder().AddStep(a).AddStep(b).Build());

        Assert.Contains("Duplicate step id", ex.Message);
        Assert.Contains(DriverProvisionStep.BaseStepId, ex.Message);
    }

    // ── 1-B  default wizard could never seed ─────────────────────────────────

    [Fact]
    public void CreateDefault_WithoutSeeders_OmitsSeedingStep()
    {
        var editor = EditorWithDrivers("SQLite");

        var (wizard, _) = new DefaultSetupWizardFactory().CreateDefault(editor.Object);

        // SeedingStep.Validate hard-fails on a null Registry, so shipping the step without one
        // produced a wizard that could never run.
        Assert.DoesNotContain(wizard.Steps, s => s.StepId == "seeding");
    }

    [Fact]
    public void CreateDefault_WithSeeders_IncludesSeedingStep_WithRegistrySet()
    {
        var editor = EditorWithDrivers("SQLite");
        var registry = new SeederRegistry();

        var (wizard, context) = new DefaultSetupWizardFactory(null, registry).CreateDefault(editor.Object);

        var seeding = wizard.Steps.SingleOrDefault(s => s.StepId == "seeding");
        Assert.NotNull(seeding);

        // Validate still reports a missing open DataSource here — the connection step opens it
        // later in the run. What 1-B guarantees is that it never fails for a null Registry.
        var result = seeding!.Validate(context);
        Assert.DoesNotContain("Registry", result.Message ?? string.Empty);
    }

    [Fact]
    public void CreateDefault_WithoutSeeders_DataImportDoesNotDependOnSeeding()
    {
        var editor = EditorWithDrivers("SQLite");

        // Naming an unregistered step would fail the builder's unknown-dependency check.
        var (wizard, _) = new DefaultSetupWizardFactory().CreateDefault(editor.Object);

        var dataImport = wizard.Steps.Single(s => s.StepId == "data-import");
        Assert.DoesNotContain("seeding", dataImport.DependsOn);
    }

    // ── 1-C  frozen audit timestamp ──────────────────────────────────────────

    [Fact]
    public void DefaultsSetupStep_WritesRuleBasedTimestamp_NotStatic()
    {
        var saved = new List<DefaultValue>();

        var config = new Mock<IConfigEditor>();
        config.Setup(c => c.Getdefaults(It.IsAny<IDMEEditor>(), It.IsAny<string>()))
              .Returns(new List<DefaultValue>());
        config.Setup(c => c.Savedefaults(It.IsAny<IDMEEditor>(), It.IsAny<List<DefaultValue>>(), It.IsAny<string>()))
              .Callback<IDMEEditor, List<DefaultValue>, string>((_, list, _) => saved.AddRange(list))
              .Returns(new ErrorsInfo { Flag = Errors.Ok });

        var editor = new Mock<IDMEEditor>();
        editor.SetupGet(e => e.ConfigEditor).Returns(config.Object);

        var ds = new Mock<IDataSource>();
        ds.SetupGet(d => d.DatasourceName).Returns("testdb");

        var context = new SetupContext
        {
            Editor = editor.Object,
            DataSource = ds.Object,
            Options = new SetupOptions(),
            State = new SetupState()
        };

        new DefaultsSetupStep(new DefaultsSetupStepOptions { ApplyDefaults = true })
            .Execute(context);

        Assert.NotEmpty(saved);

        // A Static DefaultValue captures DateTime.UtcNow once, here — so every row inserted for
        // the life of the app would carry the setup run's timestamp.
        foreach (var dv in saved)
        {
            Assert.True(dv.IsRuleBased, $"'{dv.PropertyName}' must be rule-based, not a frozen literal.");
            Assert.Equal(DefaultValueType.Rule, dv.propertyType);
            Assert.False(dv.PropertyValue is DateTime, $"'{dv.PropertyName}' must not carry a captured DateTime.");
        }
    }

    // ── 1-F  the step-order rule ─────────────────────────────────────────────

    [Fact]
    public void OutOfOrder_Throws_ForOrderReason_NotUnknownDependency()
    {
        var driver = new DriverProvisionStep(new DriverProvisionStepOptions { PackageName = "test.package" });
        var conn = new ConnectionConfigStep(new ConnectionConfigStepOptions
        {
            ConnectionProperties = new ConnectionProperties { ConnectionName = "TestDB" }
        });

        var ex = Assert.Throws<InvalidOperationException>(() =>
            new SetupWizardBuilder().AddStep(conn).AddStep(driver).Build());

        // Guards against passing for the wrong reason: if the driver step's id drifted, this
        // would throw "unknown step" instead and the order rule would be untested.
        Assert.Contains("registered after it", ex.Message);
        Assert.DoesNotContain("unknown step", ex.Message);
    }

    [Fact]
    public void ConnectionConfigStep_CanDependOnQualifiedDriverIds()
    {
        var a = new DriverProvisionStep(new DriverProvisionStepOptions
        {
            StepId = DriverProvisionStep.BuildStepId("SQLite"),
            PackageName = "SQLite"
        });
        var b = new DriverProvisionStep(new DriverProvisionStepOptions
        {
            StepId = DriverProvisionStep.BuildStepId("SqlServer"),
            PackageName = "SqlServer"
        });

        var conn = new ConnectionConfigStep(new ConnectionConfigStepOptions
        {
            ConnectionProperties = new ConnectionProperties { ConnectionName = "TestDB" },
            DependsOnStepIds = new[] { a.StepId, b.StepId }
        });

        var wizard = new SetupWizardBuilder().AddStep(a).AddStep(b).AddStep(conn).Build();

        Assert.Equal(3, wizard.Steps.Count);
        Assert.Equal(2, conn.DependsOn.Count);
    }
}
