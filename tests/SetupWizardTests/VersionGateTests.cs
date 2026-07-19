using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using TheTechIdea.Beep.Editor.Migration;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Services.AppMap;
using TheTechIdea.Beep.SetUp.Steps;

namespace TheTechIdea.Beep.SetUp.Tests;

// Phase 9 — App/DB versioning & migrate-on-startup.

public class SemVerTests
{
    [Theory]
    [InlineData("2.3.1", 2, 3, 1)]
    [InlineData("2.3", 2, 3, 0)]
    [InlineData("2", 2, 0, 0)]
    [InlineData("2.3.1-beta.4", 2, 3, 1)]
    [InlineData("1.0.0+build7", 1, 0, 0)]
    public void TryParse_ParsesCoreComponents(string v, int maj, int min, int pat)
    {
        Assert.True(SemVer.TryParse(v, out var a, out var b, out var c));
        Assert.Equal((maj, min, pat), (a, b, c));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-version")]
    public void TryParse_FailsOnGarbage(string? v) => Assert.False(SemVer.TryParse(v, out _, out _, out _));

    [Fact]
    public void Compare_OrdersByMajorThenMinorThenPatch()
    {
        Assert.True(SemVer.Compare("2.0.0", "1.9.9") > 0);
        Assert.True(SemVer.Compare("1.2.0", "1.1.9") > 0);
        Assert.True(SemVer.Compare("1.1.2", "1.1.1") > 0);
        Assert.Equal(0, SemVer.Compare("1.1.1", "1.1.1"));
    }
}

public class CompareVersionsTests
{
    private static VersionManagementService NewService() =>
        new(new Mock<IDMEEditor>().Object);

    [Fact]
    public void ReportsBreaking_OnMajorBump()
    {
        var v1 = new DatabaseVersion { Major = 1, Minor = 0, Patch = 0, EntityCount = 5, SchemaHash = "a" };
        var v2 = new DatabaseVersion { Major = 2, Minor = 0, Patch = 0, EntityCount = 5, SchemaHash = "b" };

        var cmp = NewService().CompareVersions(v1, v2);

        Assert.True(cmp.BreakingChangesCount >= 1);
    }

    [Fact]
    public void ReportsBreaking_WhenEntitiesRemoved()
    {
        var v1 = new DatabaseVersion { Major = 1, Minor = 0, Patch = 0, EntityCount = 8, SchemaHash = "a" };
        var v2 = new DatabaseVersion { Major = 1, Minor = 0, Patch = 1, EntityCount = 5, SchemaHash = "b" };

        var cmp = NewService().CompareVersions(v1, v2);

        Assert.True(cmp.BreakingChangesCount >= 1);
        Assert.Equal(1, cmp.RemovedCount);
    }

    [Fact]
    public void NoChanges_WhenIdenticalVersionAndSchema()
    {
        var v = new DatabaseVersion { Major = 1, Minor = 2, Patch = 3, EntityCount = 4, SchemaHash = "same" };
        var same = new DatabaseVersion { Major = 1, Minor = 2, Patch = 3, EntityCount = 4, SchemaHash = "same" };

        var cmp = NewService().CompareVersions(v, same);

        Assert.Empty(cmp.Changes);
    }
}

public class DbSchemaVersionStoreTests
{
    /// <summary>
    /// A stateful IDataSource stand-in backed by an in-memory row list. <paramref name="markerExists"/>
    /// pre-creates the marker so <c>EnsureMarker</c> early-returns without the MigrationManager create
    /// path (that path is exercised by the real-datasource integration test, not here).
    /// </summary>
    private static Mock<IDataSource> FakeDataSource(List<object> rows, bool markerExists = false, string name = "test")
    {
        var ds = new Mock<IDataSource>();
        ds.SetupGet(d => d.ConnectionStatus).Returns(ConnectionState.Open);
        ds.Setup(d => d.Openconnection()).Returns(ConnectionState.Open);
        ds.SetupGet(d => d.DatasourceName).Returns(name);
        ds.Setup(d => d.CheckEntityExist(It.IsAny<string>())).Returns(markerExists);
        ds.Setup(d => d.InsertEntity(It.IsAny<string>(), It.IsAny<object>()))
            .Returns((string _, object o) => { rows.Add(o); return new ErrorsInfo { Flag = Errors.Ok }; });
        ds.Setup(d => d.UpdateEntity(It.IsAny<string>(), It.IsAny<object>()))
            .Returns((string _, object o) => { rows.Clear(); rows.Add(o); return new ErrorsInfo { Flag = Errors.Ok }; });
        ds.Setup(d => d.GetEntity(It.IsAny<string>(), It.IsAny<List<AppFilter>>()))
            .Returns(() => rows.ToList());
        return ds;
    }

    private static Mock<IDMEEditor> EditorFor(IDataSource ds, string name = "test")
    {
        var editor = new Mock<IDMEEditor>();
        editor.Setup(e => e.GetDataSource(name)).Returns(ds);
        return editor;
    }

    [Fact]
    public void Read_ReturnsNull_WhenMarkerAbsent()
    {
        var rows = new List<object>();
        var ds = FakeDataSource(rows, markerExists: false);
        var store = new DbSchemaVersionStore(EditorFor(ds.Object).Object);

        Assert.Null(store.Read("test"));
    }

    [Fact]
    public void Write_ThenRead_RoundTripsVersion()
    {
        var rows = new List<object>();
        var ds = FakeDataSource(rows, markerExists: true);
        var store = new DbSchemaVersionStore(EditorFor(ds.Object).Object);

        var written = new DatabaseVersion
        {
            DatasourceName = "test",
            Major = 2, Minor = 3, Patch = 1,
            Version = "2.3.1",
            SchemaHash = "hash-1",
            EntityCount = 7
        };

        var result = store.Write("test", written);
        Assert.Equal(Errors.Ok, result.Flag);

        var read = store.Read("test");
        Assert.NotNull(read);
        Assert.Equal("2.3.1", read!.VersionString);
        Assert.Equal(7, read.EntityCount);
    }

    [Fact]
    public void Write_Twice_KeepsSingleCurrentRow()
    {
        var rows = new List<object>();
        var ds = FakeDataSource(rows, markerExists: true);
        var store = new DbSchemaVersionStore(EditorFor(ds.Object).Object);

        store.Write("test", new DatabaseVersion { Major = 1, Minor = 0, Patch = 0, Version = "1.0.0" });
        store.Write("test", new DatabaseVersion { Major = 1, Minor = 0, Patch = 1, Version = "1.0.1" });

        Assert.Single(rows);                       // upsert, not append
        Assert.Equal("1.0.1", store.Read("test")!.VersionString);
    }
}

public class VersionGateStepGuardTests
{
    private class Sample { public int Id { get; set; } public string? Name { get; set; } }

    [Fact]
    public void CanSkip_WhenMigrateOnStartupFalse()
    {
        var step = new VersionGateStep(new VersionGateStepOptions());
        var ctx = new SetupContext { Options = new SetupOptions { MigrateOnStartup = false } };
        Assert.True(step.CanSkip(ctx));
    }

    [Fact]
    public void DoesNotSkip_ByDefault()
    {
        var step = new VersionGateStep(new VersionGateStepOptions());
        Assert.False(step.CanSkip(new SetupContext { Options = new SetupOptions() }));
    }

    [Fact]
    public void Validate_Fails_WithoutDatasource()
    {
        var step = new VersionGateStep(new VersionGateStepOptions { EntityTypes = new[] { typeof(Sample) } });
        var ctx = new SetupContext { Editor = new Mock<IDMEEditor>().Object, Options = new SetupOptions() };

        var result = step.Validate(ctx);

        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Contains("datasource", result.Message, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_Fails_WithoutEntityTypes()
    {
        var step = new VersionGateStep(new VersionGateStepOptions { DatasourceName = "test" });
        var ctx = new SetupContext { Editor = new Mock<IDMEEditor>().Object, Options = new SetupOptions() };

        var result = step.Validate(ctx);

        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Contains("entity types", result.Message, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_Passes_WithDatasourceAndTypes()
    {
        var step = new VersionGateStep(new VersionGateStepOptions
        {
            DatasourceName = "test",
            EntityTypes = new[] { typeof(Sample) }
        });
        var ctx = new SetupContext { Editor = new Mock<IDMEEditor>().Object, Options = new SetupOptions() };

        Assert.Equal(Errors.Ok, step.Validate(ctx).Flag);
    }
}

public class BootstrapperUpgradePassTests
{
    private static Mock<IFirstRunDetector> CompletedDetector()
    {
        var d = new Mock<IFirstRunDetector>();
        d.Setup(x => x.IsFirstRunAsync()).ReturnsAsync(false);
        d.SetupGet(x => x.WasSetupCompleted).Returns(true);
        return d;
    }

    [Fact]
    public async Task SecondRun_RunsUpgradePass_AndSurfacesVersionMovement()
    {
        var editor = new Mock<IDMEEditor>().Object;
        var wizard = new Mock<ISetupWizard>().Object;
        var upgradeCtx = new SetupContext { Editor = editor };

        var adapter = new Mock<ISetupWizardAdapter>();
        adapter.Setup(a => a.RunAsync(It.IsAny<ISetupWizard>(), It.IsAny<SetupContext>(), It.IsAny<CancellationToken>()))
            .Returns((ISetupWizard _, SetupContext c, CancellationToken _) =>
            {
                c.Properties[VersionGateStep.MigratedFromKey] = "1.0.0";
                c.Properties[VersionGateStep.MigratedToKey] = "1.0.1";
                return Task.FromResult(new SetupReport { Succeeded = true });
            });

        var boot = new BeepBootstrapper(
            CompletedDetector().Object,
            new Mock<ISetupWizardFactory>().Object,
            () => editor,
            adapter.Object,
            logger: null,
            upgradeWizardFactory: _ => (wizard, upgradeCtx));

        var result = await boot.BootstrapAsync();

        Assert.True(result.Succeeded);
        Assert.False(result.WasFirstRun);
        Assert.Equal(BootstrapPhase.Ready, result.CompletedPhase);
        Assert.Equal("1.0.0", result.MigratedFrom);
        Assert.Equal("1.0.1", result.MigratedTo);
    }

    [Fact]
    public async Task SecondRun_WithoutUpgradeFactory_KeepsLegacyEarlyReturn()
    {
        var editor = new Mock<IDMEEditor>().Object;
        var adapter = new Mock<ISetupWizardAdapter>();

        var boot = new BeepBootstrapper(
            CompletedDetector().Object,
            new Mock<ISetupWizardFactory>().Object,
            () => editor,
            adapter.Object);

        var result = await boot.BootstrapAsync();

        Assert.True(result.Succeeded);
        Assert.Null(result.MigratedTo);
        adapter.Verify(a => a.RunAsync(It.IsAny<ISetupWizard>(), It.IsAny<SetupContext>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
