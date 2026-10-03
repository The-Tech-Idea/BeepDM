using Moq;
using TheTechIdea.Beep;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Importing;
using TheTechIdea.Beep.Editor.Importing.Helpers;
using TheTechIdea.Beep.Editor.Importing.Interfaces;
using Xunit;

namespace TheTechIdea.Beep.Framework.Tests;

public partial class ImportDefaultCatalogTests
{
    private static (Mock<IDMEEditor> Editor, Mock<IConfigEditor> Catalog, Mock<IDataSource> Destination,
        DataImportConfiguration Config) Harness()
    {
        var destination = new Mock<IDataSource>();
        destination.Setup(x => x.InsertEntity("target", It.IsAny<object>())).Returns(new ErrorsInfo { Flag = Errors.Ok });
        destination.Setup(x => x.CreateEntityAs(It.IsAny<EntityStructure>())).Returns(true);
        var config = ImportWriteTests.Config(destination, new Dictionary<string, object> { ["Id"] = 1 });
        config.ApplyDefaults = true;
        config.SourceEntityStructure = config.DestEntityStructure = new EntityStructure
        { EntityName = "target", Fields = new List<EntityField> { new() { FieldName = "Id", Fieldtype = "System.Int32" } } };
        var catalog = new Mock<IConfigEditor>();
        catalog.SetupGet(x => x.DataConnections).Returns(new List<ConnectionProperties>
        { new() { ConnectionName = "target-connection", DatasourceDefaults = new() } });
        var editor = new Mock<IDMEEditor>(); editor.SetupGet(x => x.ConfigEditor).Returns(catalog.Object);
        return (editor, catalog, destination, config);
    }

    [Theory]
    [InlineData("missing-editor")]
    [InlineData("throwing-catalog")]
    [InlineData("null-catalog")]
    [InlineData("missing-connection")]
    [InlineData("duplicate-connection")]
    [InlineData("null-entry")]
    public async Task ImplicitCatalogFailureAdmitsNoWriteOrDDL(string defect)
    {
        var (editor, catalog, destination, config) = Harness();
        config.CreateDestinationIfNotExists = true;
        switch (defect)
        {
            case "missing-editor": editor.SetupGet(x => x.ConfigEditor).Returns((IConfigEditor)null!); break;
            case "throwing-catalog": catalog.SetupGet(x => x.DataConnections).Throws(new IOException("sentinel-catalog-secret")); break;
            case "null-catalog": catalog.SetupGet(x => x.DataConnections).Returns((List<ConnectionProperties>)null!); break;
            case "missing-connection": catalog.SetupGet(x => x.DataConnections).Returns(new List<ConnectionProperties>()); break;
            case "duplicate-connection": catalog.SetupGet(x => x.DataConnections).Returns(new List<ConnectionProperties>
                { new() { ConnectionName = "target-connection" }, new() { ConnectionName = "TARGET-CONNECTION" } }); break;
            case "null-entry": catalog.SetupGet(x => x.DataConnections).Returns(new List<ConnectionProperties> { null! }); break;
        }
        using var manager = new DataImportManager(editor.Object);
        var result = Assert.IsType<ImportExecutionResult>(await manager.RunImportAsync(config, null!, default));
        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Equal(0, result.WriteAttempts);
        Assert.Null(result.Ex);
        Assert.DoesNotContain("sentinel-catalog-secret", result.Message);
        destination.Verify(x => x.CreateEntityAs(It.IsAny<EntityStructure>()), Times.Never);
        destination.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task DirectBatchCannotBypassFailedImplicitCatalog()
    {
        var (editor, catalog, destination, config) = Harness();
        catalog.SetupGet(x => x.DataConnections).Throws(new IOException("sentinel-catalog-secret"));
        var batch = new DataImportBatchHelper(editor.Object, new DataImportTransformationHelper(editor.Object), Mock.Of<IDataImportProgressHelper>());
        var result = await batch.ProcessBatchDetailedAsync(new object[] { new { Id = 1 } }, config, null!, default);
        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Equal(0, result.WriteAttempts);
        Assert.DoesNotContain("sentinel-catalog-secret", result.Message);
        destination.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GenuineAbsentDefaultsOnKnownConnectionPermitImport(bool nullDefaults)
    {
        var (editor, catalog, _, config) = Harness();
        catalog.SetupGet(x => x.DataConnections).Returns(new List<ConnectionProperties>
        { new() { ConnectionName = "target-connection", DatasourceDefaults = nullDefaults ? null! : new() } });
        using var manager = new DataImportManager(editor.Object);
        Assert.Equal(Errors.Ok, (await manager.RunImportAsync(config, null!, default)).Flag);
    }
}
