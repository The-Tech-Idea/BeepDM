using System.Data;
using System.ComponentModel;
using Moq;
using TheTechIdea.Beep;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.BeepSync;
using TheTechIdea.Beep.Editor.BeepSync.Helpers;
using TheTechIdea.Beep.Editor.BeepSync.Interfaces;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Rules;
using Xunit;

namespace TheTechIdea.Beep.Framework.Tests;

public partial class SyncOutcomeTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "BeepDM-SyncOutcome", Guid.NewGuid().ToString("N"));
    private (BeepSyncManager Manager, DataSyncSchema Schema, Mock<IDataSource> Destination, Mock<IDataSource> Source) Harness(ISchemaPersistenceHelper? persistence = null)
    {
        var editor = new Mock<IDMEEditor>();
        var catalog = new Mock<IConfigEditor>();
        catalog.SetupGet(x => x.DataConnections).Returns(new List<ConnectionProperties>
        { new() { ConnectionName = "source-ds" }, new() { ConnectionName = "target-ds" } });
        editor.SetupGet(x => x.ConfigEditor).Returns(catalog.Object);
        var source = Source("source");
        var destination = Source("target");
        editor.Setup(x => x.GetDataSource("source-ds")).Returns(source.Object);
        editor.Setup(x => x.GetDataSource("target-ds")).Returns(destination.Object);
        editor.Setup(x => x.CheckDataSourceExist("source-ds")).Returns(true);
        editor.Setup(x => x.CheckDataSourceExist("target-ds")).Returns(true);
        source.Setup(x => x.GetEntity("source", It.IsAny<List<AppFilter>>())).Returns(new object[] { new { Id = 1 } });
        var schema = new DataSyncSchema
        {
            Id = Guid.NewGuid().ToString("N"), SourceDataSourceName = "source-ds", DestinationDataSourceName = "target-ds",
            SourceEntityName = "source", DestinationEntityName = "target",
            SourceSyncDataField = "Id", DestinationSyncDataField = "Id", SyncType = "Incremental",
            SyncDirection = "SourceToDestination", BatchSize = 2,
            WatermarkPolicy = new WatermarkPolicy { WatermarkField = "UpdatedAt", LastWatermarkValue = DateTime.UtcNow.AddHours(-1) }
        };
        return (persistence == null ? new BeepSyncManager(editor.Object, null, _folder) : BeepSyncManager.CreateWithPersistence(editor.Object, persistence), schema, destination, source);
    }

    private static Mock<IDataSource> Source(string name)
    {
        var source = new Mock<IDataSource>();
        source.SetupGet(x => x.ConnectionStatus).Returns(ConnectionState.Open);
        source.Setup(x => x.Openconnection()).Returns(ConnectionState.Open);
        source.Setup(x => x.GetEntitesList()).Returns(new List<string> { name });
        source.Setup(x => x.CheckEntityExist(name)).Returns(true);
        source.Setup(x => x.GetEntityStructure(name, false)).Returns(new EntityStructure
        {
            EntityName = name, Fields = new List<EntityField>
            {
                new() { FieldName = "Id", Fieldtype = "System.Int32", IsKey = true },
                new() { FieldName = "UpdatedAt", Fieldtype = "System.DateTime" }
            }
        });
        return source;
    }

    [Theory]
    [InlineData("Sequence")]
    [InlineData("CompositeKey")]
    [InlineData("invalid")]
    public async Task UnsupportedCursorModeFailsBeforeTargetWrites(string mode)
    {
        var (manager, schema, destination, _) = Harness();
        schema.WatermarkPolicy.WatermarkMode = mode;
        var previous = schema.WatermarkPolicy.LastWatermarkValue;
        var result = await manager.SyncDataAsync(schema);
        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Contains("not supported", result.Message);
        Assert.Equal(previous, schema.WatermarkPolicy.LastWatermarkValue);
        destination.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task FailedImportDoesNotAdvanceCursorOrPublishSuccess()
    {
        var (manager, schema, destination, _) = Harness();
        destination.Setup(x => x.InsertEntity("target", It.IsAny<object>())).Returns(new ErrorsInfo { Flag = Errors.Failed });
        var previous = schema.WatermarkPolicy.LastWatermarkValue;
        var result = await manager.SyncDataAsync(schema);
        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Equal("Failed", schema.SyncStatus);
        Assert.Equal(previous, schema.WatermarkPolicy.LastWatermarkValue);
        destination.Verify(x => x.InsertEntity("target", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task CancellationAfterAcknowledgedWriteDoesNotAdvanceCursor()
    {
        var (manager, schema, destination, _) = Harness();
        using var cancellation = new CancellationTokenSource();
        destination.Setup(x => x.InsertEntity("target", It.IsAny<object>())).Returns(() =>
        {
            cancellation.Cancel();
            return new ErrorsInfo { Flag = Errors.Ok };
        });
        var previous = schema.WatermarkPolicy.LastWatermarkValue;
        var result = await manager.SyncDataAsync(schema, cancellation.Token);
        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Equal("Cancelled", schema.SyncStatus);
        Assert.Equal(previous, schema.WatermarkPolicy.LastWatermarkValue);
        destination.Verify(x => x.InsertEntity("target", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task TimestampReadHasAnInvariantUpperBoundBeforeCursorAdvances()
    {
        var (manager, schema, destination, source) = Harness();
        destination.Setup(x => x.InsertEntity("target", It.IsAny<object>())).Returns(new ErrorsInfo { Flag = Errors.Ok });
        List<AppFilter>? filters = null;
        source.Setup(x => x.GetEntity("source", It.IsAny<List<AppFilter>>())).Callback((string _, List<AppFilter> f) => filters = f)
            .Returns(new object[] { new { Id = 1 } });
        var result = await manager.SyncDataAsync(schema);
        Assert.True(result.Flag == Errors.Ok, result.Message);
        var upper = Assert.Single(filters!, f => f.FieldName == "UpdatedAt" && f.Operator == "<=");
        var value = Assert.IsType<DateTime>(schema.WatermarkPolicy.LastWatermarkValue);
        Assert.Equal(value.ToString("O", System.Globalization.CultureInfo.InvariantCulture), upper.FilterValue);
    }

    [Fact]
    public async Task ReverseFailureDoesNotReplaySuccessfulForwardWrites()
    {
        var (manager, schema, destination, source) = Harness();
        schema.SyncDirection = "Bidirectional";
        schema.RetryPolicy = new RetryPolicy { MaxAttempts = 3, BaseDelayMs = 1 };
        destination.Setup(x => x.InsertEntity("target", It.IsAny<object>())).Returns(new ErrorsInfo { Flag = Errors.Ok });
        destination.Setup(x => x.GetEntity("target", It.IsAny<List<AppFilter>>())).Returns(new object[] { new { Id = 1 } });
        source.Setup(x => x.InsertEntity("source", It.IsAny<object>())).Returns(new ErrorsInfo { Flag = Errors.Failed, Message = "connection timeout" });
        var previous = schema.WatermarkPolicy.LastWatermarkValue;
        var result = await manager.SyncDataAsync(schema);
        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Equal(previous, schema.WatermarkPolicy.LastWatermarkValue);
        destination.Verify(x => x.InsertEntity("target", It.IsAny<object>()), Times.Once);
        source.Verify(x => x.InsertEntity("source", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task RuleAuditSubscriptionIsRemovedOnCancellation()
    {
        var (manager, schema, destination, _) = Harness();
        var rules = new Mock<IRuleEngine>();
        manager.IntegrationContext = new SyncIntegrationContext { RuleEngine = rules.Object };
        using var cancellation = new CancellationTokenSource();
        destination.Setup(x => x.InsertEntity("target", It.IsAny<object>())).Returns(() =>
        {
            cancellation.Cancel();
            return new ErrorsInfo { Flag = Errors.Ok };
        });
        await manager.SyncDataAsync(schema, cancellation.Token);
        rules.VerifyAdd(x => x.RuleEvaluated += It.IsAny<EventHandler<RuleAuditEventArgs>>(), Times.Once);
        rules.VerifyRemove(x => x.RuleEvaluated -= It.IsAny<EventHandler<RuleAuditEventArgs>>(), Times.Once);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MandatoryStartCheckpointFailure_IsNotSwallowedByRetryHook(bool throws)
    {
        var storage = new Mock<ISchemaPersistenceHelper>();
        storage.Setup(s => s.LoadSchemasAsync()).ReturnsAsync(new ObservableBindingList<DataSyncSchema>());
        var acknowledgement = storage.As<ISyncPersistenceAcknowledgement>();
        var write = acknowledgement.Setup(s => s.SaveCheckpointAcknowledgedAsync(It.IsAny<SyncCheckpoint>(), It.IsAny<CancellationToken>()));
        if (throws) write.ThrowsAsync(new IOException("save failed"));
        else write.ReturnsAsync(new PersistenceWriteResult(PersistenceWriteStatus.Failed, new IOException("save failed")));
        var (manager, schema, destination, _) = Harness(storage.Object);
        schema.RetryPolicy = new RetryPolicy { MaxAttempts = 3, BaseDelayMs = 1 };
        var result = await manager.SyncDataAsync(schema);
        Assert.Equal(Errors.Failed, result.Flag); Assert.Contains("not acknowledged", result.Message);
        destination.Verify(s => s.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
        acknowledgement.Verify(s => s.SaveCheckpointAcknowledgedAsync(It.IsAny<SyncCheckpoint>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LegacyStorageWithoutAcknowledgement_CannotAdmitCheckpointedImport()
    {
        var storage = new Mock<ISchemaPersistenceHelper>();
        storage.Setup(s => s.LoadSchemasAsync()).ReturnsAsync(new ObservableBindingList<DataSyncSchema>());
        var (manager, schema, destination, _) = Harness(storage.Object);
        schema.RetryPolicy = new RetryPolicy { MaxAttempts = 2, BaseDelayMs = 1 };
        Assert.Equal(Errors.Failed, (await manager.SyncDataAsync(schema)).Flag);
        destination.Verify(s => s.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task TerminalCheckpointFailure_AfterAcknowledgedWrite_DoesNotPublishSuccessOrReplay()
    {
        var storage = new Mock<ISchemaPersistenceHelper>();
        storage.Setup(s => s.LoadSchemasAsync()).ReturnsAsync(new ObservableBindingList<DataSyncSchema>());
        storage.As<ISyncPersistenceAcknowledgement>().Setup(s => s.SaveCheckpointAcknowledgedAsync(It.IsAny<SyncCheckpoint>(), It.IsAny<CancellationToken>()))
            .Returns((SyncCheckpoint cp, CancellationToken _) => cp.Status == "Running"
                ? Task.FromResult(new PersistenceWriteResult(PersistenceWriteStatus.Saved))
                : Task.FromException<PersistenceWriteResult>(new IOException("terminal checkpoint failed")));
        var (manager, schema, destination, _) = Harness(storage.Object);
        schema.RetryPolicy = new RetryPolicy { MaxAttempts = 3, BaseDelayMs = 1 };
        destination.Setup(s => s.InsertEntity("target", It.IsAny<object>())).Returns(new ErrorsInfo { Flag = Errors.Ok });
        var previous = schema.WatermarkPolicy.LastWatermarkValue; var date = schema.LastSyncDate;
        Assert.Equal(Errors.Failed, (await manager.SyncDataAsync(schema)).Flag);
        Assert.Equal("Failed", schema.SyncStatus); Assert.Equal(date, schema.LastSyncDate); Assert.Equal(previous, schema.WatermarkPolicy.LastWatermarkValue);
        destination.Verify(s => s.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task RestartedFailedCheckpoint_BlocksReplayAndPreservesEvidence()
    {
        var (manager, schema, destination, _) = Harness();
        schema.RetryPolicy = new RetryPolicy { MaxAttempts = 1 };
        destination.Setup(s => s.InsertEntity("target", It.IsAny<object>())).Returns(new ErrorsInfo { Flag = Errors.Failed });
        Assert.Equal(Errors.Failed, (await manager.SyncDataAsync(schema)).Flag);
        var store = new SchemaPersistenceHelper(manager.Editor, _folder); var path = store.GetCheckpointFilePath(schema.Id);
        var saved = File.ReadAllBytes(path); var checkpoint = await store.LoadCheckpointAsync(schema.Id);
        Assert.Equal("Failed", checkpoint.Status); Assert.True(checkpoint.RequiresReconciliation);
        Assert.Equal(1, checkpoint.FailureEvidence.RecordsAttempted); Assert.Equal(1, checkpoint.FailureEvidence.RecordsFailed);
        using var restart = new BeepSyncManager(manager.Editor, null, _folder);
        Assert.Equal(Errors.Failed, (await restart.SyncDataAsync(schema)).Flag);
        Assert.Equal(saved, File.ReadAllBytes(path));
        destination.Verify(s => s.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task CompletedCheckpoint_PersistsCountsAndRejectsChangedExecutionContext()
    {
        var (manager, schema, destination, _) = Harness();
        schema.RetryPolicy = new RetryPolicy { MaxAttempts = 1 };
        destination.Setup(s => s.InsertEntity("target", It.IsAny<object>())).Returns(new ErrorsInfo { Flag = Errors.Ok });
        Assert.Equal(Errors.Ok, (await manager.SyncDataAsync(schema)).Flag);
        var store = new SchemaPersistenceHelper(manager.Editor, _folder); var cp = await store.LoadCheckpointAsync(schema.Id);
        Assert.Equal("Completed", cp.Status); Assert.Equal(1, cp.ProcessedOffset); Assert.Equal(64, cp.SchemaFingerprint.Length);
        schema.SourceKeyField = "changed-key";
        Assert.Equal(Errors.Failed, (await manager.SyncDataAsync(schema)).Flag);
        destination.Verify(s => s.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Once);
        Assert.Equal(cp.RunId, (await store.LoadCheckpointAsync(schema.Id)).RunId);
    }

    [Theory]
    [InlineData(PersistenceWriteStatus.Failed)]
    [InlineData(PersistenceWriteStatus.Cancelled)]
    [InlineData(PersistenceWriteStatus.Unsupported)]
    public async Task TerminalAcknowledgementFailure_IsNotReplacedBySuccessfulLegacyTask(PersistenceWriteStatus status)
    {
        var storage = new Mock<ISchemaPersistenceHelper>();
        storage.Setup(s => s.LoadSchemasAsync()).ReturnsAsync(new ObservableBindingList<DataSyncSchema>());
        var acknowledged = storage.As<ISyncPersistenceAcknowledgement>();
        acknowledged.Setup(s => s.SaveCheckpointAcknowledgedAsync(It.IsAny<SyncCheckpoint>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SyncCheckpoint cp, CancellationToken _) => new PersistenceWriteResult(cp.Status == "Running" ? PersistenceWriteStatus.Saved : status));
        storage.Setup(s => s.SaveCheckpointAsync(It.IsAny<SyncCheckpoint>())).Returns(Task.CompletedTask);
        var (manager, schema, destination, _) = Harness(storage.Object);
        schema.RetryPolicy = new RetryPolicy { MaxAttempts = 3, BaseDelayMs = 1 };
        destination.Setup(s => s.InsertEntity("target", It.IsAny<object>())).Returns(new ErrorsInfo { Flag = Errors.Ok });
        var previous = schema.WatermarkPolicy.LastWatermarkValue;

        var result = await manager.SyncDataAsync(schema);

        Assert.Equal(Errors.Failed, result.Flag);
        var failure = Assert.IsType<SyncCheckpointFailureResult>(result);
        Assert.Equal(status, failure.CheckpointPersistenceStatus);
        Assert.Equal(1, failure.RecordsAcknowledged);
        Assert.True(failure.RequiresReconciliation);
        Assert.Equal(manager.LastRunCheckpoint.RunId, failure.RunId);
        Assert.Equal("Failed", schema.SyncStatus);
        Assert.Equal(previous, schema.WatermarkPolicy.LastWatermarkValue);
        Assert.Equal("Running", manager.LastRunCheckpoint.Status);
        destination.Verify(s => s.InsertEntity("target", It.IsAny<object>()), Times.Once);
        acknowledged.Verify(s => s.SaveCheckpointAcknowledgedAsync(It.Is<SyncCheckpoint>(cp => cp.Status == "Completed" && cp.ProcessedOffset == 1), It.IsAny<CancellationToken>()), Times.Once);
        storage.Verify(s => s.SaveCheckpointAsync(It.IsAny<SyncCheckpoint>()), Times.Never);
    }

    [Theory]
    [InlineData("sync.slo.classify-run")]
    [InlineData("sync.alert.test")]
    public async Task PostCommitRuleLookupFailure_CannotReclassifySuccess(string failingRule)
    {
        var (manager, schema, destination, _) = Harness();
        schema.RetryPolicy = new RetryPolicy { MaxAttempts = 3, BaseDelayMs = 1 };
        schema.SloProfile = new SloProfile { AlertRuleKeys = new List<string> { "sync.alert.test" } };
        var rules = new Mock<IRuleEngine>();
        rules.Setup(s => s.HasRule(failingRule)).Throws(new InvalidOperationException("diagnostic-secret"));
        manager.IntegrationContext = new SyncIntegrationContext { RuleEngine = rules.Object };
        destination.Setup(s => s.InsertEntity("target", It.IsAny<object>())).Returns(new ErrorsInfo { Flag = Errors.Ok });
        var previous = schema.WatermarkPolicy.LastWatermarkValue;

        var result = await manager.SyncDataAsync(schema);

        Assert.Equal(Errors.Ok, result.Flag);
        Assert.Equal("Success", schema.SyncStatus);
        Assert.NotEqual(previous, schema.WatermarkPolicy.LastWatermarkValue);
        Assert.Equal("Completed", (await new SchemaPersistenceHelper(manager.Editor, _folder).LoadCheckpointAsync(schema.Id)).Status);
        destination.Verify(s => s.InsertEntity("target", It.IsAny<object>()), Times.Once);
        Assert.Single(schema.SyncRuns);
        Assert.Single(manager.LastRunDiagnosticFailures);
        Assert.Equal("InvalidOperationException", manager.LastRunDiagnosticFailures[0].ExceptionType);
    }

    [Fact]
    public async Task PostCommitRunHistoryObserverFailure_CannotReclassifySuccess()
    {
        var (manager, schema, destination, _) = Harness();
        schema.RetryPolicy = new RetryPolicy { MaxAttempts = 3, BaseDelayMs = 1 };
        destination.Setup(s => s.InsertEntity("target", It.IsAny<object>())).Returns(new ErrorsInfo { Flag = Errors.Ok });
        schema.SyncRuns.ListChanged += (_, args) =>
        {
            if (args.ListChangedType == ListChangedType.ItemAdded) throw new InvalidOperationException("history observer");
        };

        var result = await manager.SyncDataAsync(schema);

        Assert.Equal(Errors.Ok, result.Flag);
        Assert.Equal("Success", schema.SyncStatus);
        Assert.Equal("Completed", (await new SchemaPersistenceHelper(manager.Editor, _folder).LoadCheckpointAsync(schema.Id)).Status);
        destination.Verify(s => s.InsertEntity("target", It.IsAny<object>()), Times.Once);
        Assert.Contains(manager.LastRunDiagnosticFailures, f => f.Operation == "RunHistory");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MissingTerminalAcknowledgementOrEmptyRunFailure_CannotRetryCompletion(bool empty)
    {
        var storage = new Mock<ISchemaPersistenceHelper>();
        storage.Setup(s => s.LoadSchemasAsync()).ReturnsAsync(new ObservableBindingList<DataSyncSchema>());
        storage.As<ISyncPersistenceAcknowledgement>().Setup(s => s.SaveCheckpointAcknowledgedAsync(It.IsAny<SyncCheckpoint>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SyncCheckpoint cp, CancellationToken _) => cp.Status == "Running"
                ? new PersistenceWriteResult(PersistenceWriteStatus.Saved)
                : empty ? new PersistenceWriteResult(PersistenceWriteStatus.Failed) : null!);
        var (manager, schema, destination, source) = Harness(storage.Object);
        schema.RetryPolicy = new RetryPolicy { MaxAttempts = 3, BaseDelayMs = 1 };
        if (empty) source.Setup(s => s.GetEntity("source", It.IsAny<List<AppFilter>>())).Returns(Array.Empty<object>());
        destination.Setup(s => s.InsertEntity("target", It.IsAny<object>())).Returns(new ErrorsInfo { Flag = Errors.Ok });

        var failure = Assert.IsType<SyncCheckpointFailureResult>(await manager.SyncDataAsync(schema));

        Assert.Equal(PersistenceWriteStatus.Failed, failure.CheckpointPersistenceStatus);
        Assert.Equal(empty ? 0 : 1, failure.RecordsAcknowledged);
        storage.As<ISyncPersistenceAcknowledgement>().Verify(s => s.SaveCheckpointAcknowledgedAsync(It.IsAny<SyncCheckpoint>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        destination.Verify(s => s.InsertEntity("target", It.IsAny<object>()), empty ? Times.Never() : Times.Once());
    }

    [Fact]
    public async Task SuccessfulTerminalAcknowledgement_UsesCallerTokenAndIsNotUndoneByLaterCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        var storage = new Mock<ISchemaPersistenceHelper>();
        storage.Setup(s => s.LoadSchemasAsync()).ReturnsAsync(new ObservableBindingList<DataSyncSchema>());
        storage.As<ISyncPersistenceAcknowledgement>().Setup(s => s.SaveCheckpointAcknowledgedAsync(It.IsAny<SyncCheckpoint>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SyncCheckpoint cp, CancellationToken token) =>
            {
                Assert.Equal(cancellation.Token, token);
                if (cp.Status == "Completed") cancellation.Cancel();
                return new PersistenceWriteResult(PersistenceWriteStatus.Saved);
            });
        var (manager, schema, destination, _) = Harness(storage.Object);
        schema.RetryPolicy = new RetryPolicy { MaxAttempts = 1 };
        destination.Setup(s => s.InsertEntity("target", It.IsAny<object>())).Returns(new ErrorsInfo { Flag = Errors.Ok });

        Assert.Equal(Errors.Ok, (await manager.SyncDataAsync(schema, cancellation.Token)).Flag);
        Assert.Equal("Success", schema.SyncStatus);
        Assert.Equal("Completed", manager.LastRunCheckpoint.Status);
    }

    [Theory]
    [InlineData(nameof(DataSyncSchema.ActiveCheckpoint))]
    [InlineData(nameof(DataSyncSchema.LastSyncDate))]
    [InlineData(nameof(DataSyncSchema.SyncStatus))]
    [InlineData(nameof(DataSyncSchema.SyncStatusMessage))]
    public async Task SuccessPropertyNotifications_CannotUndoAcknowledgedCompletion(string property)
    {
        var (manager, schema, destination, _) = Harness();
        schema.RetryPolicy = new RetryPolicy { MaxAttempts = 3, BaseDelayMs = 1 };
        schema.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == property) throw new InvalidOperationException("property observer");
        };
        destination.Setup(s => s.InsertEntity("target", It.IsAny<object>())).Returns(new ErrorsInfo { Flag = Errors.Ok });

        Assert.Equal(Errors.Ok, (await manager.SyncDataAsync(schema)).Flag);
        Assert.Equal("Success", schema.SyncStatus);
        Assert.Equal("Completed", schema.ActiveCheckpoint.Status);
        Assert.NotEqual(default, schema.LastSyncDate);
        Assert.NotEmpty(manager.LastRunDiagnosticFailures);
        destination.Verify(s => s.InsertEntity("target", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task DiagnosticObserversAndLoggerFailures_AreIsolatedAndSanitized()
    {
        var (manager, schema, destination, _) = Harness();
        schema.RetryPolicy = new RetryPolicy { MaxAttempts = 1 };
        var messages = new List<string>();
        Mock.Get(manager.Editor).Setup(s => s.AddLogMessage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<Errors>()))
            .Callback((string _, string message, DateTime _, int _, string _, Errors _) =>
            {
                messages.Add(message);
                if (message.Contains("diagnostic failed")) throw new IOException("logger-secret");
            });
        manager.DiagnosticFailed += (_, _) => throw new InvalidOperationException("observer-secret");
        var reported = new List<SyncDiagnosticFailureEventArgs>();
        manager.DiagnosticFailed += (_, failure) => reported.Add(failure);
        var rules = new Mock<IRuleEngine>();
        rules.Setup(s => s.HasRule("sync.slo.classify-run")).Returns(true);
        rules.Setup(s => s.SolveRule("sync.slo.classify-run", It.IsAny<Dictionary<string, object>>(), It.IsAny<RuleExecutionPolicy>()))
            .Throws(new InvalidOperationException("rule-secret"));
        manager.IntegrationContext = new SyncIntegrationContext { RuleEngine = rules.Object };
        destination.Setup(s => s.InsertEntity("target", It.IsAny<object>())).Returns(new ErrorsInfo { Flag = Errors.Ok });

        Assert.Equal(Errors.Ok, (await manager.SyncDataAsync(schema)).Flag);
        var failure = Assert.Single(reported);
        Assert.Equal("SloRule", failure.Operation);
        Assert.Equal("InvalidOperationException", failure.ExceptionType);
        Assert.DoesNotContain(messages, m => m.Contains("rule-secret") || m.Contains("observer-secret") || m.Contains("logger-secret"));
        Assert.Single(schema.SyncRuns);
        Assert.Equal("Completed", (await new SchemaPersistenceHelper(manager.Editor, _folder).LoadCheckpointAsync(schema.Id)).Status);
    }

    [Fact]
    public async Task TerminalReplacementFailure_PreservesActualRunningFileAndAcknowledgedCount()
    {
        if (!OperatingSystem.IsWindows()) return; // File-share replacement denial is a Windows fixture.
        var (manager, schema, destination, _) = Harness();
        schema.RetryPolicy = new RetryPolicy { MaxAttempts = 3, BaseDelayMs = 1 };
        var store = new SchemaPersistenceHelper(manager.Editor, _folder);
        var path = store.GetCheckpointFilePath(schema.Id);
        byte[]? runningBytes = null;
        FileStream? blocker = null;
        destination.Setup(s => s.InsertEntity("target", It.IsAny<object>())).Returns(() =>
        {
            runningBytes = File.ReadAllBytes(path);
            blocker = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            return new ErrorsInfo { Flag = Errors.Ok };
        });
        SyncCheckpointFailureResult failure;
        try { failure = Assert.IsType<SyncCheckpointFailureResult>(await manager.SyncDataAsync(schema)); }
        finally { blocker?.Dispose(); }

        Assert.Equal(1, failure.RecordsAcknowledged);
        Assert.Equal(PersistenceWriteStatus.Failed, failure.CheckpointPersistenceStatus);
        Assert.True(failure.RequiresReconciliation);
        Assert.Equal(runningBytes, File.ReadAllBytes(path));
        Assert.Equal("Running", (await store.LoadCheckpointAsync(schema.Id)).Status);
        using var restart = new BeepSyncManager(manager.Editor, null, _folder);
        Assert.Equal(Errors.Failed, (await restart.SyncDataAsync(schema)).Flag);
        destination.Verify(s => s.InsertEntity("target", It.IsAny<object>()), Times.Once);
        Assert.Empty(Directory.EnumerateFiles(_folder, "*.tmp", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task AuditUnsubscribeFailure_CannotOverrideSavedCompletion()
    {
        var (manager, schema, destination, _) = Harness();
        schema.RetryPolicy = new RetryPolicy { MaxAttempts = 1 };
        var rules = new Mock<IRuleEngine>();
        rules.SetupRemove(r => r.RuleEvaluated -= It.IsAny<EventHandler<RuleAuditEventArgs>>()).Throws(new InvalidOperationException("unsubscribe failure"));
        manager.IntegrationContext = new SyncIntegrationContext { RuleEngine = rules.Object };
        destination.Setup(s => s.InsertEntity("target", It.IsAny<object>())).Returns(new ErrorsInfo { Flag = Errors.Ok });

        Assert.Equal(Errors.Ok, (await manager.SyncDataAsync(schema)).Flag);
        Assert.Equal("Success", schema.SyncStatus);
        Assert.Contains(manager.LastRunDiagnosticFailures, f => f.Operation == "RuleAuditUnsubscribe");
        Assert.Equal("Completed", (await new SchemaPersistenceHelper(manager.Editor, _folder).LoadCheckpointAsync(schema.Id)).Status);
    }

    public void Dispose() { if (Directory.Exists(_folder)) Directory.Delete(_folder, recursive: true); }
}
