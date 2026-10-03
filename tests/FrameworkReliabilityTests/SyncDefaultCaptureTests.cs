using Moq;
using TheTechIdea.Beep;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor.BeepSync;
using TheTechIdea.Beep.Editor.Importing;
using TheTechIdea.Beep.Report;
using Xunit;
using DefaultValue = TheTechIdea.Beep.ConfigUtil.DefaultValue;

namespace TheTechIdea.Beep.Framework.Tests;

public partial class SyncOutcomeTests
{
    [Theory]
    [InlineData("missing")]
    [InlineData("duplicate")]
    [InlineData("literal")]
    public async Task ReverseDefaultCatalogDenialPrecedesBothDirections(string defect)
    {
        var (manager, schema, destination, source) = Harness();
        schema.SyncDirection = "Bidirectional";
        schema.RetryPolicy = new RetryPolicy { MaxAttempts = 3, BaseDelayMs = 1, CheckpointEnabled = false };
        var connections = new List<ConnectionProperties>
        { new() { ConnectionName = "target-ds", DatasourceDefaults = new() } };
        if (defect != "missing")
            connections.Add(new() { ConnectionName = "source-ds", DatasourceDefaults = new()
                { new() { PropertyName = "Id", PropertyValue = defect == "literal" ? new object() : 7 } } });
        if (defect == "duplicate") connections.Add(new() { ConnectionName = "SOURCE-DS" });
        var catalog = new Mock<IConfigEditor>();
        catalog.SetupGet(x => x.DataConnections).Returns(connections);
        Mock.Get(manager.Editor).SetupGet(x => x.ConfigEditor).Returns(catalog.Object);
        destination.Setup(x => x.InsertEntity("target", It.IsAny<object>())).Returns(new ErrorsInfo { Flag = Errors.Ok });
        var cursor = schema.WatermarkPolicy.LastWatermarkValue; var date = schema.LastSyncDate;
        var result = Assert.IsType<ImportExecutionResult>(await manager.SyncDataAsync(schema));
        Assert.True(result.TransformationAdmissionFailed);
        Assert.Equal(ImportOutcome.Failed, result.Outcome);
        Assert.Equal(0, result.RecordsAttempted);
        Assert.Equal(0, result.RecordsSucceeded);
        Assert.Equal(0, result.WriteAttempts);
        Assert.Null(result.Ex);
        Assert.Equal(cursor, schema.WatermarkPolicy.LastWatermarkValue); Assert.Equal(date, schema.LastSyncDate);
        source.Verify(x => x.GetEntity(It.IsAny<string>(), It.IsAny<List<AppFilter>>()), Times.Never);
        source.Verify(x => x.Openconnection(), Times.Never);
        destination.Verify(x => x.Openconnection(), Times.Never);
        source.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
        destination.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
        destination.Verify(x => x.CreateEntityAs(It.IsAny<TheTechIdea.Beep.DataBase.EntityStructure>()), Times.Never);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task BothDirectionsUseInitiallyCapturedCatalogEvenWhenForwardChangesIt(bool initiallyEmpty, bool changeDirection)
    {
        var (manager, schema, destination, source) = Harness();
        schema.SyncDirection = "Bidirectional";
        var forward = new DefaultValue { PropertyName = "Id", PropertyValue = 7 };
        var reverse = new DefaultValue { PropertyName = "Id", PropertyValue = 9 };
        var connections = new List<ConnectionProperties>
        {
            new() { ConnectionName = "source-ds", DatasourceDefaults = initiallyEmpty ? new() : new() { reverse } },
            new() { ConnectionName = "target-ds", DatasourceDefaults = initiallyEmpty ? new() : new() { forward } }
        };
        var catalog = new Mock<IConfigEditor>(); catalog.SetupGet(x => x.DataConnections).Returns(connections);
        Mock.Get(manager.Editor).SetupGet(x => x.ConfigEditor).Returns(catalog.Object);
        source.Setup(x => x.GetEntity("source", It.IsAny<List<AppFilter>>())).Returns(() =>
        {
            connections.Clear(); forward.PropertyValue = 70; reverse.PropertyValue = 90;
            if (changeDirection) schema.SyncDirection = "SourceToDestination";
            return new object[] { new Dictionary<string, object>() };
        });
        destination.Setup(x => x.GetEntity("target", It.IsAny<List<AppFilter>>()))
            .Returns(new object[] { new Dictionary<string, object>() });
        object? forwardRow = null, reverseRow = null;
        destination.Setup(x => x.InsertEntity("target", It.IsAny<object>())).Returns((string _, object row) =>
        { forwardRow = row; return new ErrorsInfo { Flag = Errors.Ok }; });
        source.Setup(x => x.InsertEntity("source", It.IsAny<object>())).Returns((string _, object row) =>
        { reverseRow = row; return new ErrorsInfo { Flag = Errors.Ok }; });
        var result = Assert.IsType<ImportExecutionResult>(await manager.SyncDataAsync(schema));
        Assert.Equal(ImportOutcome.Completed, result.Outcome);
        Assert.Equal(2, result.RecordsSucceeded);
        var actualForward = Assert.IsType<Dictionary<string, object>>(forwardRow);
        var actualReverse = Assert.IsType<Dictionary<string, object>>(reverseRow);
        if (initiallyEmpty) { Assert.Empty(actualForward); Assert.Empty(actualReverse); }
        else { Assert.Equal(7, actualForward["Id"]); Assert.Equal(9, actualReverse["Id"]); }
        // Admission reads each direction exactly once, including genuinely empty catalogs.
        catalog.VerifyGet(x => x.DataConnections, Times.Exactly(2));
    }
}
