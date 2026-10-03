using Moq;
using TheTechIdea.Beep;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.BeepSync;
using TheTechIdea.Beep.Editor.Importing;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Rules;
using Xunit;

namespace TheTechIdea.Beep.Framework.Tests;

public partial class SyncOutcomeTests
{
    private static Mock<IRuleEngine> QualityEngine(Func<Dictionary<string, object>, object> evaluate)
    {
        var engine = new Mock<IRuleEngine>();
        engine.Setup(x => x.HasRule(It.IsAny<string>())).Returns((string key) => key == "quality");
        engine.Setup(x => x.SolveRule("quality", It.IsAny<Dictionary<string, object>>(), It.IsAny<RuleExecutionPolicy>()))
            .Returns((string _, Dictionary<string, object> args, RuleExecutionPolicy _) =>
                (new Dictionary<string, object>(), evaluate(args)));
        return engine;
    }

    private static void EnableRecordQuality(BeepSyncManager manager, DataSyncSchema schema, Mock<IRuleEngine> engine)
    {
        schema.DqPolicy = new DqPolicy { RuleKeys = new() { "quality" }, BatchThresholdEnabled = false };
        manager.IntegrationContext = new SyncIntegrationContext { RuleEngine = engine.Object };
    }

    [Fact]
    public async Task SyncQuality_MappedMixedRowsEvaluateGeneratedPayloadOnceAndPreserveAcknowledgements()
    {
        var (manager, schema, destination, source, _, _) = MappedHarness();
        source.Setup(x => x.GetEntity("source", It.IsAny<List<AppFilter>>()))
            .Returns(new object[] { new { SourceId = 7, SourceName = "allowed" }, new { SourceId = -1, SourceName = "rejected" } });
        var engine = QualityEngine(args => (int?)args["record"].GetType().GetProperty("TargetId")!.GetValue(args["record"]) > 0);
        EnableRecordQuality(manager, schema, engine);
        var previous = schema.WatermarkPolicy.LastWatermarkValue;
        var result = Assert.IsType<ImportExecutionResult>(await manager.SyncDataAsync(schema));
        Assert.Equal(ImportOutcome.Partial, result.Outcome);
        Assert.Equal(1, result.RecordsSucceeded);
        Assert.Equal(2, result.RecordsQualityEvaluated);
        Assert.Equal(1, result.RecordsQualityRejected);
        Assert.Equal(previous, schema.WatermarkPolicy.LastWatermarkValue);
        Assert.Equal("Failed", schema.SyncStatus);
        destination.Verify(x => x.InsertEntity("target", It.IsAny<object>()), Times.Once);
        engine.Verify(x => x.SolveRule("quality", It.IsAny<Dictionary<string, object>>(), It.IsAny<RuleExecutionPolicy>()), Times.Exactly(2));
    }

    [Fact]
    public async Task SyncQuality_BidirectionalGateSeesActualReverseEntityAndPayload()
    {
        var (manager, schema, destination, source, _, _) = MappedHarness();
        schema.SyncDirection = "Bidirectional";
        destination.Setup(x => x.GetEntity("target", It.IsAny<List<AppFilter>>()))
            .Returns(new object[] { new { TargetId = -1, TargetName = "reverse" } });
        var seen = new List<string>();
        var engine = QualityEngine(args =>
        {
            var entity = (string)args["entityName"]; seen.Add(entity);
            var property = entity == "target" ? "TargetId" : "SourceId";
            return (int?)args["record"].GetType().GetProperty(property)!.GetValue(args["record"]) > 0;
        });
        EnableRecordQuality(manager, schema, engine);
        var result = Assert.IsType<ImportExecutionResult>(await manager.SyncDataAsync(schema));
        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Equal(1, result.RecordsQualityRejected);
        Assert.Equal(1, result.RecordsSucceeded);
        Assert.True(result.RequiresReconciliation);
        Assert.Equal(new[] { "target", "source" }, seen);
        destination.Verify(x => x.InsertEntity("target", It.IsAny<object>()), Times.Once);
        source.Verify(x => x.InsertEntity("source", It.IsAny<object>()), Times.Never);
    }

    [Theory]
    [InlineData("missing-engine")]
    [InlineData("missing-rule")]
    [InlineData("lookup-throw")]
    [InlineData("missing-store")]
    [InlineData("partial-channel")]
    public async Task SyncQuality_RequiredDependenciesFailBeforeAnyMutation(string defect)
    {
        var (manager, schema, destination, source, _, _) = MappedHarness();
        var engine = QualityEngine(_ => true);
        EnableRecordQuality(manager, schema, engine);
        schema.CreateDestinationIfNotExists = true;
        switch (defect)
        {
            case "missing-engine": manager.IntegrationContext = null; break;
            case "missing-rule": engine.Setup(x => x.HasRule("quality")).Returns(false); break;
            case "lookup-throw": engine.Setup(x => x.HasRule("quality")).Throws(new InvalidOperationException("secret-lookup")); break;
            case "missing-store": schema.DqPolicy.OnRecordFailure = DataQualityAction.Quarantine; break;
            case "partial-channel": schema.DqPolicy.RejectChannelDataSourceName = "reject"; break;
        }
        var result = await manager.SyncDataAsync(schema);
        Assert.Equal(Errors.Failed, result.Flag);
        Assert.DoesNotContain("secret", result.Message);
        destination.Verify(x => x.CreateEntityAs(It.IsAny<TheTechIdea.Beep.DataBase.EntityStructure>()), Times.Never);
        destination.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
        source.Verify(x => x.GetEntity(It.IsAny<string>(), It.IsAny<List<AppFilter>>()), Times.Never);
    }

    [Theory]
    [InlineData("throws")]
    [InlineData("string")]
    [InlineData("null")]
    public async Task SyncQuality_RequiredSolveFailuresNeverBecomePassOrWholeRunRetry(string failure)
    {
        var (manager, schema, destination, _, _, _) = MappedHarness();
        var engine = QualityEngine(_ => failure switch
        {
            "throws" => throw new InvalidOperationException("secret-solve"),
            "string" => "true", _ => null!
        });
        EnableRecordQuality(manager, schema, engine);
        var result = Assert.IsType<ImportExecutionResult>(await manager.SyncDataAsync(schema));
        Assert.Equal(1, result.RecordsQualityEvaluationFailed);
        Assert.Equal(0, result.WriteAttempts);
        Assert.Equal(Errors.Failed, result.Flag);
        Assert.False(result.HasUncertainWrites);
        destination.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
        engine.Verify(x => x.SolveRule("quality", It.IsAny<Dictionary<string, object>>(), It.IsAny<RuleExecutionPolicy>()), Times.Once);
    }

    [Fact]
    public async Task SyncQuality_CapturesEngineKeysAndModeBeforeCallerRebind()
    {
        var (manager, schema, destination, source, _, _) = MappedHarness();
        var engine = QualityEngine(_ => false);
        EnableRecordQuality(manager, schema, engine);
        source.Setup(x => x.GetEntity("source", It.IsAny<List<AppFilter>>())).Returns(() =>
        {
            schema.DqPolicy.RuleKeys.Clear(); schema.DqPolicy.RecordFailureMode = QualityFailureMode.Advisory;
            schema.DqPolicy.OnRecordFailure = DataQualityAction.Warn;
            manager.IntegrationContext = new SyncIntegrationContext { RuleEngine = QualityEngine(_ => true).Object };
            return new object[] { new { SourceId = 7, SourceName = "captured" } };
        });
        var result = Assert.IsType<ImportExecutionResult>(await manager.SyncDataAsync(schema));
        Assert.Equal(1, result.RecordsQualityRejected);
        Assert.Equal(Errors.Failed, result.Flag);
        destination.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
        engine.Verify(x => x.SolveRule("quality", It.IsAny<Dictionary<string, object>>(), It.IsAny<RuleExecutionPolicy>()), Times.Once);
    }

    [Fact]
    public async Task SyncQuality_AdvisoryLookupFailureIsCountedWithoutBlockingAcknowledgedWrite()
    {
        var (manager, schema, destination, _, _, _) = MappedHarness();
        var engine = QualityEngine(_ => true);
        EnableRecordQuality(manager, schema, engine);
        schema.DqPolicy.RecordFailureMode = QualityFailureMode.Advisory;
        engine.Setup(x => x.HasRule("quality")).Throws(new InvalidOperationException("secret-advisory"));
        var result = Assert.IsType<ImportExecutionResult>(await manager.SyncDataAsync(schema));
        Assert.Equal(Errors.Ok, result.Flag);
        Assert.Equal(1, result.RecordsWarned);
        Assert.Equal(1, result.RecordsQualityEvaluationFailed);
        Assert.Equal(1, result.RecordsSucceeded);
        destination.Verify(x => x.InsertEntity("target", It.IsAny<object>()), Times.Once);
    }

    [Theory]
    [InlineData("ok")]
    [InlineData("failed")]
    [InlineData("null")]
    public async Task SyncQuality_RejectChannelRequiresActualAcknowledgementAndNeverAdmitsTarget(string acknowledgement)
    {
        var (manager, schema, destination, _, _, _) = MappedHarness();
        var engine = QualityEngine(_ => false);
        EnableRecordQuality(manager, schema, engine);
        schema.DqPolicy.OnRecordFailure = DataQualityAction.Quarantine;
        schema.DqPolicy.RejectChannelDataSourceName = "reject-ds";
        schema.DqPolicy.RejectChannelEntityName = "rejects";
        var rejects = Source("rejects");
        rejects.Setup(x => x.InsertEntity("rejects", It.IsAny<object>())).Returns(acknowledgement == "null" ? null! :
            new ErrorsInfo { Flag = acknowledgement == "ok" ? Errors.Ok : Errors.Failed });
        Mock.Get(manager.Editor).Setup(x => x.GetDataSource("reject-ds")).Returns(rejects.Object);
        var result = Assert.IsType<ImportExecutionResult>(await manager.SyncDataAsync(schema));
        Assert.Equal(1, result.RecordsQualityRejected);
        Assert.Equal(acknowledgement == "ok" ? 0 : 1, result.RejectStoreFailures);
        Assert.Equal(acknowledgement == "ok" ? 1 : 0, result.RecordsQuarantined);
        Assert.Equal(0, result.WriteAttempts);
        Assert.Equal(Errors.Failed, result.Flag);
        rejects.Verify(x => x.InsertEntity("rejects", It.IsAny<object>()), Times.Once);
        destination.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task SyncQuality_BidirectionalSuccessCombinesActualCounts()
    {
        var (manager, schema, destination, source, _, _) = MappedHarness();
        schema.SyncDirection = "Bidirectional";
        destination.Setup(x => x.GetEntity("target", It.IsAny<List<AppFilter>>()))
            .Returns(new object[] { new { TargetId = 9, TargetName = "reverse" } });
        source.Setup(x => x.InsertEntity("source", It.IsAny<object>())).Returns(new ErrorsInfo { Flag = Errors.Ok });
        EnableRecordQuality(manager, schema, QualityEngine(_ => true));
        var result = Assert.IsType<ImportExecutionResult>(await manager.SyncDataAsync(schema));
        Assert.Equal(Errors.Ok, result.Flag);
        Assert.Equal(2, result.RecordsQualityEvaluated);
        Assert.Equal(2, result.RecordsSucceeded);
        Assert.Equal(2, result.WriteAttempts);
        Assert.False(result.RequiresReconciliation);
    }

    [Fact]
    public async Task SyncQuality_CancelledReverseRetainsForwardAcknowledgementEvidence()
    {
        var (manager, schema, destination, _, _, _) = MappedHarness();
        schema.SyncDirection = "Bidirectional";
        using var cancellation = new CancellationTokenSource();
        destination.Setup(x => x.GetEntity("target", It.IsAny<List<AppFilter>>()))
            .Returns(new object[] { new { TargetId = 9, TargetName = "reverse" } });
        EnableRecordQuality(manager, schema, QualityEngine(args =>
        {
            if ((string)args["entityName"] == "source") cancellation.Cancel();
            return true;
        }));
        var result = Assert.IsType<ImportExecutionResult>(await manager.SyncDataAsync(schema, cancellation.Token));
        Assert.Equal(ImportOutcome.Cancelled, result.Outcome);
        Assert.Equal(1, result.RecordsSucceeded);
        Assert.Equal(2, result.RecordsQualityEvaluated);
        Assert.True(result.RequiresReconciliation);
        Assert.False(result.HasUncertainWrites);
    }

    [Fact]
    public async Task SyncQuality_CancellationInsideGateCannotAdvanceCursor()
    {
        var (manager, schema, destination, _, _, _) = MappedHarness();
        using var cancellation = new CancellationTokenSource();
        var engine = QualityEngine(_ => { cancellation.Cancel(); return true; });
        EnableRecordQuality(manager, schema, engine);
        var previous = schema.WatermarkPolicy.LastWatermarkValue;
        var result = await manager.SyncDataAsync(schema, cancellation.Token);
        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Equal("Cancelled", schema.SyncStatus);
        Assert.Equal(previous, schema.WatermarkPolicy.LastWatermarkValue);
        destination.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }
}
