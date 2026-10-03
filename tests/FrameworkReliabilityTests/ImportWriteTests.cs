using System.Data;
using Moq;
using TheTechIdea.Beep;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Importing;
using TheTechIdea.Beep.Editor.Importing.Helpers;
using TheTechIdea.Beep.Editor.Importing.Interfaces;
using Xunit;

namespace TheTechIdea.Beep.Framework.Tests;

public class ImportWriteTests
{
    private static DataImportBatchHelper Helper()
    {
        var transformation = new Mock<IDataImportTransformationHelper>();
        transformation.Setup(x => x.ApplyTransformationPipeline(It.IsAny<object>(), It.IsAny<DataImportConfiguration>()))
            .Returns((object record, DataImportConfiguration _) => record);
        return new DataImportBatchHelper(Mock.Of<IDMEEditor>(), transformation.Object,
            Mock.Of<IDataImportProgressHelper>());
    }

    internal static DataImportConfiguration Config(Mock<IDataSource> destination, params object[] rows)
    {
        var source = new Mock<IDataSource>();
        source.SetupGet(x => x.ConnectionStatus).Returns(ConnectionState.Open);
        destination.SetupGet(x => x.ConnectionStatus).Returns(ConnectionState.Open);
        source.Setup(x => x.GetEntity("source", It.IsAny<List<TheTechIdea.Beep.Report.AppFilter>>())).Returns(rows);
        return new DataImportConfiguration
        {
            SourceData = source.Object, DestData = destination.Object,
            SourceEntityName = "source", DestEntityName = "target",
            SourceDataSourceName = "source-connection", DestDataSourceName = "target-connection",
            CreateDestinationIfNotExists = false, ApplyDefaults = false, BatchSize = 2
        };
    }

    [Theory]
    [InlineData(Errors.Failed)]
    [InlineData(Errors.Warning)]
    [InlineData(Errors.Unknown)]
    public async Task UnacknowledgedRecordsAreNeverCountedAsSuccess(Errors flag)
    {
        var destination = new Mock<IDataSource>();
        destination.Setup(x => x.InsertEntity("target", It.IsAny<object>()))
            .Returns(new ErrorsInfo { Flag = flag, Message = "rejected" });
        var result = Assert.IsType<ImportExecutionResult>(await Helper().ProcessBatchAsync(
            new object[] { 1, 2 }, Config(destination), null!, default));
        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Equal(ImportOutcome.Failed, result.Outcome);
        Assert.Equal(2, result.RecordsAttempted);
        Assert.Equal(2, result.RecordsFailed);
        Assert.Equal(0, result.RecordsSucceeded);
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public async Task RetryNeverReplaysAcknowledgedRows()
    {
        var destination = new Mock<IDataSource>();
        var writes = new List<int>();
        int secondAttempts = 0;
        destination.Setup(x => x.InsertEntity("target", It.IsAny<object>())).Returns((string _, object row) =>
        {
            int value = (int)row;
            writes.Add(value);
            return new ErrorsInfo { Flag = value == 2 && secondAttempts++ == 0 ? Errors.Failed : Errors.Ok };
        });
        var config = Config(destination);
        config.OnBatchError = BatchErrorStrategy.Retry;
        config.MaxRetries = 1;
        var result = Assert.IsType<ImportExecutionResult>(await Helper().ProcessBatchAsync(
            new object[] { 1, 2, 3 }, config, null!, default));
        Assert.Equal(new[] { 1, 2, 2, 3 }, writes);
        Assert.Equal(3, result.RecordsSucceeded);
        Assert.Equal(4, result.WriteAttempts);
        Assert.Equal(ImportOutcome.Completed, result.Outcome);
    }

    [Fact]
    public async Task ProviderExceptionDoesNotBlindlyRetryAnIndeterminateWrite()
    {
        var destination = new Mock<IDataSource>();
        destination.Setup(x => x.InsertEntity("target", It.IsAny<object>())).Throws(new IOException("lost acknowledgement"));
        var config = Config(destination);
        config.OnBatchError = BatchErrorStrategy.Retry;
        config.MaxRetries = 5;
        var result = Assert.IsType<ImportExecutionResult>(await Helper().ProcessBatchAsync(new object[] { 1 }, config, null!, default));
        Assert.True(result.HasUncertainWrites);
        Assert.Equal(1, result.WriteAttempts);
        destination.Verify(x => x.InsertEntity("target", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task BatchCancellationPreservesAcknowledgedCount()
    {
        using var cancellation = new CancellationTokenSource();
        var destination = new Mock<IDataSource>();
        destination.Setup(x => x.InsertEntity("target", It.IsAny<object>())).Returns(() =>
        {
            cancellation.Cancel();
            return new ErrorsInfo { Flag = Errors.Ok };
        });
        var result = Assert.IsType<ImportExecutionResult>(await Helper().ProcessBatchAsync(
            new object[] { 1, 2 }, Config(destination), null!, cancellation.Token));
        Assert.Equal(ImportOutcome.Cancelled, result.Outcome);
        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Equal(1, result.RecordsSucceeded);
        destination.Verify(x => x.InsertEntity("target", It.IsAny<object>()), Times.Once);
    }

    [Theory]
    [InlineData(BatchErrorStrategy.Skip)]
    [InlineData(BatchErrorStrategy.Retry)]
    [InlineData(BatchErrorStrategy.Abort)]
    public async Task ManagerCannotReportAllFailedImportAsCompleted(BatchErrorStrategy strategy)
    {
        var destination = new Mock<IDataSource>();
        destination.Setup(x => x.InsertEntity("target", It.IsAny<object>())).Returns(new ErrorsInfo { Flag = Errors.Failed });
        var config = Config(destination, 1, 2, 3);
        config.OnBatchError = strategy;
        config.MaxRetries = 0;
        using var manager = new DataImportManager(Mock.Of<IDMEEditor>());
        var result = Assert.IsType<ImportExecutionResult>(await manager.RunImportAsync(config, null!, default));
        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Equal(0, result.RecordsSucceeded);
        Assert.Equal(0, manager.GetImportStatus().RecordsProcessed);
        Assert.Equal(ImportState.Faulted, manager.GetImportStatus().State);
        Assert.Equal(strategy == BatchErrorStrategy.Abort ? 2 : 3, result.RecordsFailed);
        Assert.Equal(strategy == BatchErrorStrategy.Skip ? 3 : 0, result.RecordsSkipped);
    }

    [Fact]
    public async Task ManagerReportsPartialWritesWithoutWholeBatchRetry()
    {
        var destination = new Mock<IDataSource>();
        destination.Setup(x => x.InsertEntity("target", It.IsAny<object>())).Returns((string _, object row) =>
            new ErrorsInfo { Flag = (int)row == 2 ? Errors.Failed : Errors.Ok });
        using var manager = new DataImportManager(Mock.Of<IDMEEditor>());
        var result = Assert.IsType<ImportExecutionResult>(await manager.RunImportAsync(Config(destination, 1, 2, 3), null!, default));
        Assert.Equal(ImportOutcome.Partial, result.Outcome);
        Assert.Equal(2, result.RecordsSucceeded);
        Assert.Equal(1, result.RecordsFailed);
        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Equal(2, manager.GetImportStatus().RecordsProcessed);
        destination.Verify(x => x.InsertEntity("target", 1), Times.Once);
    }

    [Fact]
    public async Task PreCancelledManagerDoesNotReadOrWrite()
    {
        var destination = new Mock<IDataSource>(MockBehavior.Strict);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        using var manager = new DataImportManager(Mock.Of<IDMEEditor>());
        var result = Assert.IsType<ImportExecutionResult>(await manager.RunImportAsync(Config(destination, 1), null!, cancellation.Token));
        Assert.Equal(ImportOutcome.Cancelled, result.Outcome);
        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Equal(ImportState.Cancelled, manager.GetImportStatus().State);
        destination.VerifyNoOtherCalls();
    }
}
