using Moq;
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
    [InlineData(false)]
    [InlineData(true)]
    public async Task RequiredResolverFailurePreservesCursorAndDoesNotRetry(bool firstRowAcknowledged)
    {
        var (manager, schema, destination, source) = Harness();
        var editor = Mock.Get(manager.Editor);
        var catalog = new Mock<IConfigEditor>();
        catalog.SetupGet(x => x.DataConnections).Returns(new List<ConnectionProperties>
        {
            new() { ConnectionName = "target-ds", DatasourceDefaults = new()
                { new() { PropertyName = "Id", Rule = ":ADD(1,not-a-number)", IsEnabled = true } } }
        });
        editor.SetupGet(x => x.ConfigEditor).Returns(catalog.Object);
        var failedRow = new Dictionary<string, object>();
        object[] rows = firstRowAcknowledged
            ? new object[] { new Dictionary<string, object> { ["Id"] = 1 }, failedRow }
            : new object[] { failedRow };
        source.Setup(x => x.GetEntity("source", It.IsAny<List<AppFilter>>())).Returns(rows);
        destination.Setup(x => x.InsertEntity("target", It.IsAny<object>())).Returns(new ErrorsInfo { Flag = Errors.Ok });
        schema.RetryPolicy = new RetryPolicy { MaxAttempts = 3, BaseDelayMs = 1, CheckpointEnabled = false };
        var cursor = schema.WatermarkPolicy.LastWatermarkValue; var date = schema.LastSyncDate;
        var result = Assert.IsType<ImportExecutionResult>(await manager.SyncDataAsync(schema));
        Assert.Equal(firstRowAcknowledged ? ImportOutcome.Partial : ImportOutcome.Failed, result.Outcome);
        Assert.Equal(1, result.RecordsTransformationFailed);
        Assert.Equal(firstRowAcknowledged ? 1 : 0, result.RecordsSucceeded);
        Assert.Equal(cursor, schema.WatermarkPolicy.LastWatermarkValue); Assert.Equal(date, schema.LastSyncDate);
        source.Verify(x => x.GetEntity("source", It.IsAny<List<AppFilter>>()), Times.Once);
        destination.Verify(x => x.InsertEntity("target", failedRow), Times.Never);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RequiredTransformationFailurePreservesCursorAndStopsWholeRunRetry(bool firstRowAcknowledged)
    {
        var (manager, schema, destination, source) = Harness();
        var editor = Mock.Get(manager.Editor);
        var config = new Mock<IConfigEditor>();
        config.SetupGet(x => x.DataConnections).Returns(new List<ConnectionProperties>
        {
            new()
            {
                ConnectionName = "target-ds",
                DatasourceDefaults = new() { new DefaultValue { PropertyName = "Id", PropertyValue = "not-an-integer" } }
            }
        });
        editor.SetupGet(x => x.ConfigEditor).Returns(config.Object);
        object[] rows = firstRowAcknowledged
            ? new object[] { new ImportTransformationTests.DefaultRecord { Id = 1 }, new ImportTransformationTests.DefaultRecord() }
            : new object[] { new ImportTransformationTests.DefaultRecord() };
        source.Setup(x => x.GetEntity("source", It.IsAny<List<AppFilter>>())).Returns(rows);
        destination.Setup(x => x.InsertEntity("target", It.IsAny<object>())).Returns(new ErrorsInfo { Flag = Errors.Ok });
        schema.RetryPolicy = new RetryPolicy { MaxAttempts = 3, BaseDelayMs = 1, CheckpointEnabled = false };
        var previousCursor = schema.WatermarkPolicy.LastWatermarkValue;
        var previousDate = schema.LastSyncDate;

        var result = Assert.IsType<ImportExecutionResult>(await manager.SyncDataAsync(schema));

        Assert.Equal(firstRowAcknowledged ? ImportOutcome.Partial : ImportOutcome.Failed, result.Outcome);
        Assert.Equal(1, result.RecordsTransformationFailed);
        Assert.Equal(firstRowAcknowledged ? 1 : 0, result.RecordsSucceeded);
        Assert.False(result.HasUncertainWrites);
        Assert.Equal("Failed", schema.SyncStatus);
        Assert.Equal(previousCursor, schema.WatermarkPolicy.LastWatermarkValue);
        Assert.Equal(previousDate, schema.LastSyncDate);
        source.Verify(x => x.GetEntity("source", It.IsAny<List<AppFilter>>()), Times.Once);
        destination.Verify(x => x.InsertEntity("target", It.IsAny<object>()), firstRowAcknowledged ? Times.Once() : Times.Never());
    }
}
