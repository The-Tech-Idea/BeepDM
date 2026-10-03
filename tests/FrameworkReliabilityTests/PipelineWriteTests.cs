using System.Data;
using Moq;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Pipelines.Engine;
using TheTechIdea.Beep.Pipelines.Engine.BuiltIn.Sinks;
using TheTechIdea.Beep.Pipelines.Models;
using TheTechIdea.Beep.Report;
using Xunit;

namespace TheTechIdea.Beep.Framework.Tests;

public class PipelineWriteTests
{
    private static readonly PipelineSchema Schema = new("target", new[] { new PipelineField("Id", typeof(int)) });
    private static PipelineRecord[] Records(params int[] ids) => ids.Select(id =>
    {
        var record = new PipelineRecord(Schema);
        record.Values[0] = id;
        return record;
    }).ToArray();

    private static (DataSinkPlugin Sink, PipelineRunContext Context) Sink(IDataSource source, string mode = "Insert", bool transaction = false)
    {
        var editor = new Mock<IDMEEditor>();
        editor.Setup(x => x.GetDataSource("destination")).Returns(source);
        var sink = new DataSinkPlugin();
        sink.Configure(new Dictionary<string, object>
        {
            ["DataSourceName"] = "destination", ["EntityName"] = "target", ["WriteMode"] = mode,
            ["UseTransaction"] = transaction
        });
        return (sink, new PipelineRunContext { DMEEditor = editor.Object });
    }

    private static Mock<IDataSource> Source()
    {
        var source = new Mock<IDataSource>();
        source.SetupGet(x => x.ConnectionStatus).Returns(ConnectionState.Open);
        return source;
    }

    [Theory]
    [InlineData(Errors.Failed)]
    [InlineData(Errors.Warning)]
    public async Task FailedWritesAreNotCounted(Errors flag)
    {
        var source = Source();
        source.Setup(x => x.InsertEntity("target", It.IsAny<object>())).Returns(new ErrorsInfo { Flag = flag });
        var (sink, ctx) = Sink(source.Object);
        await sink.BeginBatchAsync(ctx, Schema, default);
        await Assert.ThrowsAsync<PipelineWriteException>(() => sink.WriteBatchAsync(Records(1), ctx, default));
        Assert.Equal(0, ctx.TotalRecordsWritten);
    }

    [Fact]
    public async Task PartialBatchIsNotReplayedByRetryPolicy()
    {
        var source = Source();
        int writes = 0;
        source.Setup(x => x.InsertEntity("target", It.IsAny<object>())).Returns(() =>
            new ErrorsInfo { Flag = ++writes == 1 ? Errors.Ok : Errors.Failed });
        var (sink, ctx) = Sink(source.Object);
        await sink.BeginBatchAsync(ctx, Schema, default);
        var retry = new PipelineRetryPolicy(3, 0);
        var failure = await Assert.ThrowsAsync<PipelineWriteException>(() =>
            retry.ExecuteAsync(() => sink.WriteBatchAsync(Records(1, 2), ctx, default)));
        Assert.False(failure.CanRetry);
        Assert.Equal(1, failure.AcknowledgedRecords);
        Assert.Equal(2, writes);
        Assert.Equal(1, ctx.TotalRecordsWritten);
    }

    [Fact]
    public async Task UnknownWriteOutcomeIsNotRetried()
    {
        var source = Source();
        source.Setup(x => x.InsertEntity("target", It.IsAny<object>())).Throws(new IOException("connection lost"));
        var (sink, ctx) = Sink(source.Object);
        await sink.BeginBatchAsync(ctx, Schema, default);
        await Assert.ThrowsAsync<PipelineWriteException>(() => new PipelineRetryPolicy(3, 0)
            .ExecuteAsync(() => sink.WriteBatchAsync(Records(1), ctx, default)));
        source.Verify(x => x.InsertEntity("target", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task UpsertRequiresNativeCapabilityBeforeAnyWrites()
    {
        var source = Source();
        var (sink, ctx) = Sink(source.Object, "Upsert");
        await Assert.ThrowsAsync<NotSupportedException>(() => sink.BeginBatchAsync(ctx, Schema, default));
        source.Verify(x => x.UpdateEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
        source.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task NativeUpsertAcknowledgementIsChecked()
    {
        var source = Source();
        var native = source.As<IUpsertDataSource>();
        native.Setup(x => x.UpsertEntity("target", It.IsAny<object>())).Returns(new ErrorsInfo { Flag = Errors.Ok });
        var (sink, ctx) = Sink(source.Object, "Upsert");
        await sink.BeginBatchAsync(ctx, Schema, default);
        await sink.WriteBatchAsync(Records(1), ctx, default);
        Assert.Equal(1, ctx.TotalRecordsWritten);
        native.Verify(x => x.UpsertEntity("target", It.IsAny<object>()), Times.Once);
        source.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TransactionWritesCountOnlyAfterConfirmedCommit(bool commitSucceeded)
    {
        var source = new Mock<IRDBSource>();
        source.SetupGet(x => x.ConnectionStatus).Returns(ConnectionState.Open);
        source.Setup(x => x.BeginTransaction(It.IsAny<PassedArgs>())).Returns(new ErrorsInfo { Flag = Errors.Ok });
        source.Setup(x => x.InsertEntity("target", It.IsAny<object>())).Returns(new ErrorsInfo { Flag = Errors.Ok });
        source.Setup(x => x.Commit(It.IsAny<PassedArgs>())).Returns(new ErrorsInfo { Flag = commitSucceeded ? Errors.Ok : Errors.Failed });
        source.Setup(x => x.EndTransaction(It.IsAny<PassedArgs>())).Returns(new ErrorsInfo { Flag = Errors.Ok });
        var (sink, ctx) = Sink(source.Object, transaction: true);
        await sink.BeginBatchAsync(ctx, Schema, default);
        await sink.WriteBatchAsync(Records(1, 2), ctx, default);
        Assert.Equal(0, ctx.TotalRecordsWritten);
        if (commitSucceeded)
        {
            await sink.CommitAsync(ctx, default);
            Assert.Equal(2, ctx.TotalRecordsWritten);
            source.Verify(x => x.EndTransaction(It.IsAny<PassedArgs>()), Times.Never);
        }
        else
        {
            await Assert.ThrowsAsync<PipelineWriteException>(() => sink.CommitAsync(ctx, default));
            await sink.RollbackAsync(ctx, default);
            Assert.Equal(0, ctx.TotalRecordsWritten);
            source.Verify(x => x.EndTransaction(It.IsAny<PassedArgs>()), Times.Once);
        }
    }

    [Fact]
    public async Task CancellationIsNotRetried()
    {
        int attempts = 0;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new PipelineRetryPolicy(3, 0).ExecuteAsync(() =>
        {
            attempts++;
            throw new OperationCanceledException();
        }));
        Assert.Equal(1, attempts);
    }
}
