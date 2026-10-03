using Moq;
using System.Data;
using System.Diagnostics;
using System.Text.Json.Nodes;
using TheTechIdea.Beep;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Importing;
using TheTechIdea.Beep.Editor.Importing.ErrorStore;
using TheTechIdea.Beep.Editor.Importing.Helpers;
using TheTechIdea.Beep.Editor.Importing.Interfaces;
using TheTechIdea.Beep.Tools;
using Xunit;

namespace TheTechIdea.Beep.Framework.Tests;

public sealed class ImportRejectRecoveryTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "BeepDM-RejectRecovery", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Replay_LegacyRecordsCannotBeMarkedWithoutProviderAcknowledgement()
    {
        var destination = new Mock<IDataSource>();
        var config = ImportWriteTests.Config(destination);
        var store = new Mock<IImportErrorStore>();
        config.ErrorStore = store.Object;
        var context = "source-connection/source/target-connection/target";
        store.Setup(x => x.LoadPendingAsync(context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new ImportErrorRecord { ContextKey = context, RawRecord = new { Id = 7 } } });
        var editor = new Mock<IDMEEditor>(); editor.SetupGet(x => x.ErrorObject).Returns(new ErrorsInfo());
        using var manager = new DataImportManager(editor.Object);
        Assert.Equal(Errors.Ok, manager.SetImportConfiguration(config).Flag);
        var result = await manager.ReplayFailedRecordsAsync(context);
        Assert.Equal(Errors.Failed, result.Flag);
        store.Verify(x => x.MarkReplayedAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        destination.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task Replay_MissingStoreCannotReportSuccessfulRecovery()
    {
        var editor = new Mock<IDMEEditor>(); editor.SetupGet(x => x.ErrorObject).Returns(new ErrorsInfo());
        using var manager = new DataImportManager(editor.Object);
        Assert.Equal(Errors.Ok, manager.SetImportConfiguration(ImportWriteTests.Config(new Mock<IDataSource>())).Flag);
        Assert.Equal(Errors.Failed, (await manager.ReplayFailedRecordsAsync("source-connection/source/target-connection/target")).Flag);
    }

    [Fact]
    public async Task LegacyIndexMarking_RejectsAmbiguousCrossRunCoordinates()
    {
        var store = new JsonFileImportErrorStore(_folder);
        await store.SaveAsync(new ImportErrorRecord { ContextKey = "context", BatchNumber = 0, RecordIndex = 1 });
        await store.SaveAsync(new ImportErrorRecord { ContextKey = "context", BatchNumber = 0, RecordIndex = 1 });
        await Assert.ThrowsAnyAsync<Exception>(() => store.MarkReplayedAsync("context", 0, 1));
        Assert.All(await store.LoadAsync("context"), record => Assert.False(record.Replayed));
    }

    private sealed class Rule : IDataQualityRule
    {
        public string RuleName => "positive";
        public string FieldName => "Id";
        public DataQualityAction OnFailure => DataQualityAction.Quarantine;
        public bool Allow { get; set; }
        public int Calls { get; private set; }
        public bool Evaluate(object? value, object record) { Calls++; return Allow && Convert.ToInt32(value) > 0; }
        public string FailureMessage(object? value) => "not used";
    }

    private async Task<(Mock<IDataSource> Destination, Mock<IDMEEditor> Editor, DataImportConfiguration Config,
        JsonFileImportErrorStore Store, Rule Rule, ImportErrorRecord Reject)> RejectAsync(bool keyed = false)
    {
        var destination = new Mock<IDataSource>();
        if (keyed) destination.As<IImportReplayDataSource>();
        var config = ImportWriteTests.Config(destination);
        destination.SetupGet(x => x.DatasourceName).Returns(config.DestDataSourceName);
        destination.SetupGet(x => x.GuidID).Returns("provider-target-identity");
        destination.Setup(x => x.CheckEntityExist("target")).Returns(true);
        destination.Setup(x => x.GetEntityStructure("target", false)).Returns(new EntityStructure
        {
            EntityName = "target", Fields = new List<EntityField>
            { new() { FieldName = "Id", Fieldtype = "System.Int32", IsRequired = true } }
        });
        destination.Setup(x => x.InsertEntity("target", It.IsAny<object>())).Returns(new ErrorsInfo { Flag = Errors.Ok });
        var editor = new Mock<IDMEEditor>();
        editor.SetupGet(x => x.ErrorObject).Returns(new ErrorsInfo());
        editor.SetupGet(x => x.classCreator).Returns(new ClassCreator(editor.Object));
        var store = new JsonFileImportErrorStore(_folder);
        var rule = new Rule();
        config.ErrorStore = store;
        config.QualityRules.Add(rule);
        var helper = new DataImportBatchHelper(editor.Object, new DataImportTransformationHelper(editor.Object), Mock.Of<IDataImportProgressHelper>());
        var result = await helper.ProcessBatchDetailedAsync(new object[] { new { Id = 1 } }, config, null!, default);
        Assert.Equal(1, result.RecordsQuarantined);
        Assert.Equal(0, result.WriteAttempts);
        var rejected = Assert.Single(await store.LoadAsync(DataImportManager.GetRejectContextKey(config)),
            record => record.Recovery?.RunId == result.RunId);
        Assert.Equal(result.RunId, rejected.Recovery!.RunId);
        Assert.Null(rejected.RawRecord);
        return (destination, editor, config, store, rule, rejected);
    }

    [Fact]
    public async Task Replay_WritesPreparedDestinationAndAcknowledgesOnlyAfterProvider()
    {
        var fixture = await RejectAsync();
        var context = DataImportManager.GetRejectContextKey(fixture.Config);
        var prepared = await fixture.Store.PrepareReplayAsync(context, fixture.Reject.Recovery!.RejectId, 0, "operator", new { Id = 7 });
        fixture.Rule.Allow = true;
        fixture.Config.CustomTransformation = _ => throw new InvalidOperationException("must not transform twice");
        fixture.Destination.Setup(x => x.InsertEntity("target", It.IsAny<object>())).Returns((string _, object row) =>
        {
            Assert.Equal(7, row.GetType().GetProperty("Id")!.GetValue(row));
            var persisted = fixture.Store.LoadRejectAsync(context, prepared.Recovery!.RejectId).GetAwaiter().GetResult();
            Assert.Equal(ImportRejectState.Claimed, persisted.Recovery!.State);
            Assert.False(persisted.Replayed);
            return new ErrorsInfo { Flag = Errors.Ok };
        });
        using var manager = new DataImportManager(fixture.Editor.Object);
        var result = await manager.ReplayRejectedRecordAsync(fixture.Config, context, prepared.Recovery!.RejectId, 1, "worker");
        Assert.Equal(Errors.Ok, result.Flag);
        Assert.Equal(1, result.RecordsAcknowledged);
        Assert.Equal(1, result.WriteAttempts);
        Assert.Equal(2, fixture.Rule.Calls);
        var reloaded = await new JsonFileImportErrorStore(_folder).LoadRejectAsync(context, prepared.Recovery.RejectId);
        Assert.Equal(ImportRejectState.Acknowledged, reloaded.Recovery!.State);
        Assert.Equal(3, reloaded.Recovery.Revision);
        Assert.True(reloaded.Replayed);
        Assert.NotNull(reloaded.ReplayedAt);
        Assert.Equal(fixture.Reject.Recovery!.OriginalDestinationPayload, reloaded.Recovery.OriginalDestinationPayload);
        fixture.Destination.Verify(x => x.InsertEntity("target", It.IsAny<object>()), Times.Once);
        Assert.Equal(Errors.Failed, (await manager.ReplayRejectedRecordAsync(fixture.Config, context, prepared.Recovery.RejectId, 1, "second")).Flag);
        fixture.Destination.Verify(x => x.InsertEntity("target", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task Replay_QualityDenialDoesNotWriteOrCreateAnotherReject()
    {
        var fixture = await RejectAsync();
        var context = DataImportManager.GetRejectContextKey(fixture.Config);
        var id = fixture.Reject.Recovery!.RejectId;
        await fixture.Store.PrepareReplayAsync(context, id, 0, "operator");
        using var manager = new DataImportManager(fixture.Editor.Object);
        var result = await manager.ReplayRejectedRecordAsync(fixture.Config, context, id, 1, "worker");
        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Equal(1, result.RecordsDenied);
        Assert.Equal(0, result.WriteAttempts);
        var only = Assert.Single(await fixture.Store.LoadAsync(context));
        Assert.Equal(ImportRejectState.Pending, only.Recovery!.State);
        Assert.False(only.Replayed);
        fixture.Destination.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

    [Theory]
    [InlineData("throw")]
    [InlineData("null")]
    [InlineData("warning")]
    [InlineData("unknown")]
    public async Task Replay_UncertainWritesRemainBlockedAcrossRestart(string failure)
    {
        var fixture = await RejectAsync();
        var context = DataImportManager.GetRejectContextKey(fixture.Config);
        var id = fixture.Reject.Recovery!.RejectId;
        await fixture.Store.PrepareReplayAsync(context, id, 0, "operator"); fixture.Rule.Allow = true;
        fixture.Destination.Setup(x => x.InsertEntity("target", It.IsAny<object>())).Returns(() => failure switch
        {
            "throw" => throw new IOException("secret-provider"), "null" => null!,
            "warning" => new ErrorsInfo { Flag = Errors.Warning }, _ => new ErrorsInfo { Flag = Errors.Unknown }
        });
        using var manager = new DataImportManager(fixture.Editor.Object);
        var result = await manager.ReplayRejectedRecordAsync(fixture.Config, context, id, 1, "worker");
        Assert.True(result.HasUncertainWrites);
        Assert.True(result.RequiresReconciliation);
        Assert.Null(result.Ex);
        Assert.DoesNotContain("secret-provider", result.Message);
        var restarted = new JsonFileImportErrorStore(_folder);
        Assert.Empty(await restarted.LoadPendingAsync(context));
        var blocked = await restarted.LoadRejectAsync(context, id);
        Assert.Equal(ImportRejectState.ReconciliationRequired, blocked.Recovery!.State);
        await Assert.ThrowsAnyAsync<Exception>(() => restarted.PrepareReplayAsync(context, id, 3, "operator"));
        await Assert.ThrowsAnyAsync<Exception>(() => restarted.ClearAsync(context));
        fixture.Destination.Verify(x => x.InsertEntity("target", It.IsAny<object>()), Times.Once);
    }

    [Theory]
    [InlineData("unprepared")]
    [InlineData("no-rules")]
    [InlineData("wrong-guid")]
    [InlineData("wrong-name")]
    [InlineData("closed")]
    [InlineData("missing-entity")]
    [InlineData("unknown-field")]
    [InlineData("null-required")]
    [InlineData("missing-required")]
    public async Task Replay_UnsupportedPreparationPolicyOrTargetNeverWrites(string fault)
    {
        var fixture = await RejectAsync();
        var context = DataImportManager.GetRejectContextKey(fixture.Config);
        var id = fixture.Reject.Recovery!.RejectId;
        object corrected = fault == "unknown-field" ? new { Other = 7 } : fault == "null-required"
            ? new Dictionary<string, object?> { ["Id"] = null } : fault == "missing-required" ? new { Other = 1 } : new { Id = 7 };
        if (fault != "unprepared") await fixture.Store.PrepareReplayAsync(context, id, 0, "operator", corrected);
        fixture.Rule.Allow = true;
        if (fault == "no-rules") fixture.Config.QualityRules.Clear();
        if (fault == "wrong-guid") fixture.Destination.SetupGet(x => x.GuidID).Returns("other-target");
        if (fault == "wrong-name") fixture.Destination.SetupGet(x => x.DatasourceName).Returns("other-connection");
        if (fault == "closed") fixture.Destination.SetupGet(x => x.ConnectionStatus).Returns(ConnectionState.Closed);
        if (fault == "missing-entity") fixture.Destination.Setup(x => x.CheckEntityExist("target")).Returns(false);
        using var manager = new DataImportManager(fixture.Editor.Object);
        var result = await manager.ReplayRejectedRecordAsync(fixture.Config, context, id, 1, "worker");
        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Equal(0, result.WriteAttempts);
        fixture.Destination.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
        Assert.Equal(fault == "unprepared" ? ImportRejectState.Pending : ImportRejectState.Prepared,
            (await fixture.Store.LoadRejectAsync(context, id)).Recovery!.State);
    }

    [Fact]
    public async Task Replay_ConcurrentManagersCannotWriteTheSameRejectTwice()
    {
        var fixture = await RejectAsync();
        var context = DataImportManager.GetRejectContextKey(fixture.Config);
        var id = fixture.Reject.Recovery!.RejectId;
        await fixture.Store.PrepareReplayAsync(context, id, 0, "operator"); fixture.Rule.Allow = true;
        using var entered = new ManualResetEventSlim(); using var release = new ManualResetEventSlim();
        fixture.Destination.Setup(x => x.InsertEntity("target", It.IsAny<object>())).Returns(() =>
        { entered.Set(); Assert.True(release.Wait(TimeSpan.FromSeconds(10))); return new ErrorsInfo { Flag = Errors.Ok }; });
        using var first = new DataImportManager(fixture.Editor.Object);
        using var second = new DataImportManager(fixture.Editor.Object);
        var running = first.ReplayRejectedRecordAsync(fixture.Config, context, id, 1, "first");
        try
        {
            Assert.True(entered.Wait(TimeSpan.FromSeconds(10)));
            var denied = await second.ReplayRejectedRecordAsync(fixture.Config, context, id, 1, "second");
            Assert.Equal(Errors.Failed, denied.Flag);
            Assert.Equal(0, denied.WriteAttempts);
        }
        finally { release.Set(); }
        Assert.Equal(Errors.Ok, (await running).Flag);
        fixture.Destination.Verify(x => x.InsertEntity("target", It.IsAny<object>()), Times.Once);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Replay_CancellationKeepsActualProviderEvidence(bool acknowledged)
    {
        var fixture = await RejectAsync();
        var context = DataImportManager.GetRejectContextKey(fixture.Config);
        var id = fixture.Reject.Recovery!.RejectId;
        await fixture.Store.PrepareReplayAsync(context, id, 0, "operator"); fixture.Rule.Allow = true;
        using var cancellation = new CancellationTokenSource();
        fixture.Destination.Setup(x => x.InsertEntity("target", It.IsAny<object>())).Returns(() =>
        {
            cancellation.Cancel();
            if (!acknowledged) throw new OperationCanceledException(cancellation.Token);
            return new ErrorsInfo { Flag = Errors.Ok };
        });
        using var manager = new DataImportManager(fixture.Editor.Object);
        var result = await manager.ReplayRejectedRecordAsync(fixture.Config, context, id, 1, "worker", cancellation.Token);
        Assert.True(result.Cancelled);
        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Equal(acknowledged ? 1 : 0, result.RecordsAcknowledged);
        Assert.Equal(!acknowledged, result.HasUncertainWrites);
        var persisted = await fixture.Store.LoadRejectAsync(context, id);
        Assert.Equal(acknowledged ? ImportRejectState.Acknowledged : ImportRejectState.ReconciliationRequired, persisted.Recovery!.State);
    }

    [Theory]
    [InlineData(ImportReplayProviderConfirmation.NotApplied)]
    [InlineData(ImportReplayProviderConfirmation.Applied)]
    public async Task Store_ReconciliationRequiresRevisionOwnershipAndExplicitEvidence(ImportReplayProviderConfirmation confirmation)
    {
        var fixture = await RejectAsync();
        var context = DataImportManager.GetRejectContextKey(fixture.Config);
        var id = fixture.Reject.Recovery!.RejectId;
        await fixture.Store.PrepareReplayAsync(context, id, 0, "operator");
        var claimed = await fixture.Store.ClaimReplayAsync(context, id, 1, "worker");
        await Assert.ThrowsAnyAsync<Exception>(() => fixture.Store.ReconcileReplayAsync(context, id, 2, "wrong-owner", "operator", confirmation, "evidence"));
        await Assert.ThrowsAnyAsync<Exception>(() => fixture.Store.ReconcileReplayAsync(context, id, 2, claimed.Recovery!.ClaimId!, "operator", confirmation, ""));
        await fixture.Store.ReconcileReplayAsync(context, id, 2, claimed.Recovery!.ClaimId!, "operator", confirmation, "provider-proof/123");
        var persisted = await new JsonFileImportErrorStore(_folder).LoadRejectAsync(context, id);
        Assert.Equal(confirmation == ImportReplayProviderConfirmation.Applied ? ImportRejectState.ReconciledApplied : ImportRejectState.Pending,
            persisted.Recovery!.State);
        Assert.Equal(confirmation == ImportReplayProviderConfirmation.Applied, persisted.Replayed);
    }

    [Fact]
    public async Task Store_DurableIdsSeparateRepeatedRunLocalCoordinatesAndDismissal()
    {
        var first = await RejectAsync(); var second = await RejectAsync();
        var context = DataImportManager.GetRejectContextKey(first.Config);
        Assert.NotEqual(first.Reject.Recovery!.RejectId, second.Reject.Recovery!.RejectId);
        Assert.NotEqual(first.Reject.Recovery.RunId, second.Reject.Recovery.RunId);
        Assert.Equal(first.Reject.RecordIndex, second.Reject.RecordIndex);
        await Assert.ThrowsAnyAsync<Exception>(() => first.Store.MarkReplayedAsync(context, 0, first.Reject.RecordIndex));
        await first.Store.DismissRejectAsync(context, first.Reject.Recovery.RejectId, 0, "operator", "triage/123");
        var dismissed = await first.Store.LoadRejectAsync(context, first.Reject.Recovery.RejectId);
        Assert.Equal(ImportRejectState.Dismissed, dismissed.Recovery!.State);
        Assert.False(dismissed.Replayed);
        await Assert.ThrowsAnyAsync<Exception>(() => first.Store.ClearAsync(context));
        Assert.Equal(second.Reject.Recovery.RejectId, Assert.Single(await first.Store.LoadPendingAsync(context)).Recovery!.RejectId);
    }

    [Theory]
    [InlineData("missing-id")]
    [InlineData("missing-version")]
    [InlineData("uppercase-id")]
    [InlineData("duplicate-id")]
    [InlineData("pending-claim")]
    [InlineData("malformed-payload")]
    [InlineData("foreign-payload-member")]
    [InlineData("duplicate-payload-member")]
    public async Task Store_CorruptDurableEvidenceCannotBeMutatedOrCleared(string corruption)
    {
        var fixture = await RejectAsync();
        var context = DataImportManager.GetRejectContextKey(fixture.Config);
        var path = Assert.Single(Directory.GetFiles(_folder, "*.errors.jsonl"));
        var json = JsonNode.Parse(await File.ReadAllTextAsync(path))!.AsObject();
        var recovery = json["Recovery"]!.AsObject();
        switch (corruption)
        {
            case "missing-id": recovery.Remove("RejectId"); break;
            case "missing-version": recovery.Remove("FormatVersion"); break;
            case "uppercase-id": recovery["RejectId"] = fixture.Reject.Recovery!.RejectId.ToUpperInvariant(); break;
            case "pending-claim": recovery["ClaimId"] = Guid.NewGuid().ToString("N"); break;
            case "malformed-payload": recovery["OriginalDestinationPayload"] = "secret-corrupt-payload"; break;
            case "foreign-payload-member":
                var payload = JsonNode.Parse(recovery["OriginalDestinationPayload"]!.GetValue<string>())!.AsObject();
                payload["$type"] = "foreign-clr-type"; recovery["OriginalDestinationPayload"] = payload.ToJsonString(); break;
            case "duplicate-payload-member":
                var original = recovery["OriginalDestinationPayload"]!.GetValue<string>();
                recovery["OriginalDestinationPayload"] = "{\"Kind\":\"array\"," + original[1..]; break;
        }
        var bytes = json.ToJsonString() + "\n";
        if (corruption == "duplicate-id") bytes += bytes;
        await File.WriteAllTextAsync(path, bytes);
        await Assert.ThrowsAnyAsync<Exception>(() => fixture.Store.PrepareReplayAsync(context, fixture.Reject.Recovery!.RejectId, 0, "operator"));
        await Assert.ThrowsAnyAsync<Exception>(() => fixture.Store.ClearAsync(context));
        Assert.Equal(bytes, await File.ReadAllTextAsync(path));
    }

    [Fact]
    public async Task Replay_UsesProviderReplayKeyCapabilityRatherThanOrdinaryInsert()
    {
        var fixture = await RejectAsync(keyed: true);
        var context = DataImportManager.GetRejectContextKey(fixture.Config);
        var id = fixture.Reject.Recovery!.RejectId;
        await fixture.Store.PrepareReplayAsync(context, id, 0, "operator"); fixture.Rule.Allow = true;
        fixture.Destination.As<IImportReplayDataSource>().Setup(x => x.InsertReplay("target", It.IsAny<object>(), id))
            .Returns(new ErrorsInfo { Flag = Errors.Ok });
        using var manager = new DataImportManager(fixture.Editor.Object);
        Assert.Equal(Errors.Ok, (await manager.ReplayRejectedRecordAsync(fixture.Config, context, id, 1, "worker")).Flag);
        fixture.Destination.As<IImportReplayDataSource>().Verify(x => x.InsertReplay("target", It.IsAny<object>(), id), Times.Once);
        fixture.Destination.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Replay_CompletionSaveFailurePreservesAcknowledgementAndBlocksBlindRetry(bool persistedBeforeThrow)
    {
        var fixture = await RejectAsync();
        var context = DataImportManager.GetRejectContextKey(fixture.Config);
        var id = fixture.Reject.Recovery!.RejectId;
        await fixture.Store.PrepareReplayAsync(context, id, 0, "operator"); fixture.Rule.Allow = true;
        var adapter = new Mock<IImportErrorStore>();
        var recovery = adapter.As<IImportRejectRecoveryStore>();
        recovery.Setup(x => x.ValidateRecoveryAsync(context, It.IsAny<CancellationToken>()))
            .Returns((string key, CancellationToken token) => fixture.Store.ValidateRecoveryAsync(key, token));
        recovery.Setup(x => x.LoadRejectAsync(context, id, It.IsAny<CancellationToken>()))
            .Returns((string key, string reject, CancellationToken token) => fixture.Store.LoadRejectAsync(key, reject, token));
        recovery.Setup(x => x.ClaimReplayAsync(context, id, 1, "worker", It.IsAny<CancellationToken>()))
            .Returns((string key, string reject, int revision, string owner, CancellationToken token) =>
                fixture.Store.ClaimReplayAsync(key, reject, revision, owner, token));
        recovery.Setup(x => x.CompleteReplayAsync(context, id, 2, It.IsAny<string>(), ImportReplayDisposition.Acknowledged, It.IsAny<CancellationToken>()))
            .Returns(async (string key, string reject, int revision, string claim, ImportReplayDisposition disposition, CancellationToken token) =>
            {
                if (persistedBeforeThrow) await fixture.Store.CompleteReplayAsync(key, reject, revision, claim, disposition, token);
                throw new IOException("secret-completion");
            });
        fixture.Config.ErrorStore = adapter.Object;
        using var manager = new DataImportManager(fixture.Editor.Object);
        var result = await manager.ReplayRejectedRecordAsync(fixture.Config, context, id, 1, "worker");
        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Equal(1, result.RecordsAcknowledged);
        Assert.Equal(1, result.RecoveryPersistenceFailures);
        Assert.True(result.RequiresReconciliation);
        Assert.False(result.HasUncertainWrites);
        var persisted = await new JsonFileImportErrorStore(_folder).LoadRejectAsync(context, id);
        Assert.Equal(persistedBeforeThrow ? ImportRejectState.Acknowledged : ImportRejectState.Claimed, persisted.Recovery!.State);
        Assert.Empty(await fixture.Store.LoadPendingAsync(context));
        fixture.Config.ErrorStore = fixture.Store;
        Assert.Equal(Errors.Failed, (await manager.ReplayRejectedRecordAsync(fixture.Config, context, id, 1, "second")).Flag);
        fixture.Destination.Verify(x => x.InsertEntity("target", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task Replay_ExplicitFailureRequiresNewOperatorPreparationBeforeRetry()
    {
        var fixture = await RejectAsync();
        var context = DataImportManager.GetRejectContextKey(fixture.Config);
        var id = fixture.Reject.Recovery!.RejectId;
        await fixture.Store.PrepareReplayAsync(context, id, 0, "operator"); fixture.Rule.Allow = true;
        fixture.Destination.SetupSequence(x => x.InsertEntity("target", It.IsAny<object>()))
            .Returns(new ErrorsInfo { Flag = Errors.Failed }).Returns(new ErrorsInfo { Flag = Errors.Ok });
        using var manager = new DataImportManager(fixture.Editor.Object);
        var failed = await manager.ReplayRejectedRecordAsync(fixture.Config, context, id, 1, "worker");
        Assert.Equal(1, failed.RecordsFailed);
        Assert.False(failed.HasUncertainWrites);
        Assert.Equal(ImportRejectState.Pending, (await fixture.Store.LoadRejectAsync(context, id)).Recovery!.State);
        Assert.Equal(Errors.Failed, (await manager.ReplayRejectedRecordAsync(fixture.Config, context, id, 3, "worker")).Flag);
        await fixture.Store.PrepareReplayAsync(context, id, 3, "operator");
        Assert.Equal(Errors.Ok, (await manager.ReplayRejectedRecordAsync(fixture.Config, context, id, 4, "worker")).Flag);
        fixture.Destination.Verify(x => x.InsertEntity("target", It.IsAny<object>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Replay_CancelledBeforeAdmissionLeavesPreparationAndNoProviderAttempt()
    {
        var fixture = await RejectAsync();
        var context = DataImportManager.GetRejectContextKey(fixture.Config);
        var id = fixture.Reject.Recovery!.RejectId;
        await fixture.Store.PrepareReplayAsync(context, id, 0, "operator"); fixture.Rule.Allow = true;
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        using var manager = new DataImportManager(fixture.Editor.Object);
        var result = await manager.ReplayRejectedRecordAsync(fixture.Config, context, id, 1, "worker", cancellation.Token);
        Assert.True(result.Cancelled); Assert.False(result.RequiresReconciliation); Assert.Equal(0, result.WriteAttempts);
        Assert.Equal(ImportRejectState.Prepared, (await fixture.Store.LoadRejectAsync(context, id)).Recovery!.State);
    }

    [Theory]
    [InlineData("unsupported")]
    [InlineData("cycle")]
    [InlineData("too-large")]
    [InlineData("oversized-name")]
    [InlineData("aggregate-budget")]
    public async Task Store_UnsupportedCorrectionsDoNotReplaceOriginalEvidence(string kind)
    {
        var fixture = await RejectAsync();
        var context = DataImportManager.GetRejectContextKey(fixture.Config);
        var id = fixture.Reject.Recovery!.RejectId;
        var correction = new Dictionary<string, object>();
        if (kind == "cycle") correction["Id"] = correction;
        else if (kind == "unsupported") correction["Id"] = new Version(1, 2);
        else if (kind == "too-large") correction["Id"] = new string('x', 1048577);
        else if (kind == "oversized-name") correction[new string('x', 1025)] = 1;
        else { correction["Id"] = new string('x', 100000); correction["Name"] = new string('x', 100000); }
        await Assert.ThrowsAnyAsync<Exception>(() => fixture.Store.PrepareReplayAsync(context, id, 0, "operator", correction));
        var persisted = await fixture.Store.LoadRejectAsync(context, id);
        Assert.Equal(ImportRejectState.Pending, persisted.Recovery!.State);
        Assert.Equal(0, persisted.Recovery.Revision);
        Assert.Equal(fixture.Reject.Recovery.OriginalDestinationPayload, persisted.Recovery.OriginalDestinationPayload);
    }

    [Fact]
    public async Task Store_SeparateProcessesCanClaimPreparedRevisionOnlyOnce()
    {
        var fixture = await RejectAsync();
        var context = DataImportManager.GetRejectContextKey(fixture.Config);
        var id = fixture.Reject.Recovery!.RejectId;
        await fixture.Store.PrepareReplayAsync(context, id, 0, "operator");
        using var first = Worker("reject-claim", _folder, context, id, "first");
        using var second = Worker("reject-claim", _folder, context, id, "second");
        var outputs = await Task.WhenAll(ReadWorker(first), ReadWorker(second));
        Assert.Single(outputs, output => output.Trim() == "claimed");
        Assert.Single(outputs, output => output.Trim() == "denied");
        var claimed = await new JsonFileImportErrorStore(_folder).LoadRejectAsync(context, id);
        Assert.Equal(ImportRejectState.Claimed, claimed.Recovery!.State);
        Assert.Equal(2, claimed.Recovery.Revision);
        using var reload = Worker("reject-reload", _folder, context, id);
        Assert.Equal("reloaded", (await ReadWorker(reload)).Trim());
    }

    private static Process Worker(params string[] arguments)
    {
        var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true };
        var worker = Path.Combine(AppContext.BaseDirectory, "persistence-worker", "FrameworkPersistenceWorker.dll");
        Assert.True(File.Exists(worker)); start.ArgumentList.Add(worker);
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        return Process.Start(start)!;
    }

    [Fact]
    public async Task Replay_BulkRequiresPreparationAndCannotHideBlockedClaims()
    {
        var fixture = await RejectAsync();
        var context = DataImportManager.GetRejectContextKey(fixture.Config);
        var id = fixture.Reject.Recovery!.RejectId;
        using var manager = new DataImportManager(fixture.Editor.Object);
        Assert.Equal(Errors.Ok, manager.SetImportConfiguration(fixture.Config).Flag);
        Assert.Equal(Errors.Failed, (await manager.ReplayFailedRecordsAsync(context)).Flag);
        await fixture.Store.PrepareReplayAsync(context, id, 0, "operator");
        await fixture.Store.ClaimReplayAsync(context, id, 1, "worker");
        var blocked = Assert.IsType<ImportRejectReplayResult>(await manager.ReplayFailedRecordsAsync(context));
        Assert.Equal(Errors.Failed, blocked.Flag);
        Assert.True(blocked.RequiresReconciliation);
        fixture.Destination.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task Replay_BulkObserverFailureCannotReclassifyAcknowledgedWrite()
    {
        var fixture = await RejectAsync();
        var context = DataImportManager.GetRejectContextKey(fixture.Config);
        var id = fixture.Reject.Recovery!.RejectId;
        await fixture.Store.PrepareReplayAsync(context, id, 0, "operator"); fixture.Rule.Allow = true;
        using var manager = new DataImportManager(fixture.Editor.Object);
        Assert.Equal(Errors.Ok, manager.SetImportConfiguration(fixture.Config).Flag);
        var progress = new Mock<IProgress<TheTechIdea.Beep.Addin.IPassedArgs>>();
        progress.Setup(x => x.Report(It.IsAny<TheTechIdea.Beep.Addin.IPassedArgs>())).Throws(new Exception("secret-observer"));
        var result = Assert.IsType<ImportRejectReplayResult>(await manager.ReplayFailedRecordsAsync(context, progress.Object));
        Assert.Equal(Errors.Ok, result.Flag); Assert.Equal(1, result.RecordsAcknowledged); Assert.Equal(1, result.DiagnosticFailures);
        Assert.True((await fixture.Store.LoadRejectAsync(context, id)).Replayed);
    }

    [Theory]
    [InlineData("long")]
    [InlineData("decimal")]
    [InlineData("datetime")]
    [InlineData("datetimeoffset")]
    [InlineData("guid")]
    [InlineData("bytes")]
    public async Task Replay_TypedPreparedPayloadRehydratesActualGeneratedProviderRow(string kind)
    {
        var fixture = await RejectAsync();
        var context = DataImportManager.GetRejectContextKey(fixture.Config);
        var id = fixture.Reject.Recovery!.RejectId;
        object expected = kind switch
        {
            "long" => 9007199254740993L, "decimal" => 123.456m,
            "datetime" => new DateTime(2026, 10, 3, 7, 8, 9, DateTimeKind.Utc),
            "datetimeoffset" => new DateTimeOffset(2026, 10, 3, 7, 8, 9, TimeSpan.FromHours(3)),
            "guid" => Guid.Parse("4123770a-8c41-4e83-8211-1d1e23f85399"), _ => new byte[] { 0, 127, 255 }
        };
        fixture.Destination.Setup(x => x.GetEntityStructure("target", false)).Returns(new EntityStructure
        {
            EntityName = "target", Fields = new List<EntityField>
            {
                new() { FieldName = "Id", Fieldtype = "System.Int32", IsRequired = true },
                new() { FieldName = "Value", Fieldtype = expected.GetType().FullName!, IsRequired = true }
            }
        });
        await fixture.Store.PrepareReplayAsync(context, id, 0, "operator", new Dictionary<string, object> { ["Id"] = 7, ["Value"] = expected });
        fixture.Rule.Allow = true;
        fixture.Destination.Setup(x => x.InsertEntity("target", It.IsAny<object>())).Returns((string _, object row) =>
        {
            var actual = row.GetType().GetProperty("Value")!.GetValue(row);
            Assert.Equal(expected.GetType(), actual!.GetType());
            if (expected is byte[] bytes) Assert.Equal(bytes, Assert.IsType<byte[]>(actual));
            else Assert.Equal(expected, actual);
            if (expected is DateTime date) Assert.Equal(date.Kind, Assert.IsType<DateTime>(actual).Kind);
            if (expected is DateTimeOffset offset) Assert.Equal(offset.Offset, Assert.IsType<DateTimeOffset>(actual).Offset);
            return new ErrorsInfo { Flag = Errors.Ok };
        });
        using var manager = new DataImportManager(fixture.Editor.Object);
        Assert.Equal(Errors.Ok, (await manager.ReplayRejectedRecordAsync(fixture.Config, context, id, 1, "worker")).Flag);
    }

    private static async Task<string> ReadWorker(Process child)
    {
        var output = child.StandardOutput.ReadToEndAsync(); var errors = child.StandardError.ReadToEndAsync();
        try
        {
            await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(45));
            Assert.True(child.ExitCode == 0, await errors); return await output;
        }
        finally { if (!child.HasExited) { child.Kill(entireProcessTree: true); await child.WaitForExitAsync(); } }
    }

    public void Dispose() { if (Directory.Exists(_folder)) Directory.Delete(_folder, recursive: true); }
}
