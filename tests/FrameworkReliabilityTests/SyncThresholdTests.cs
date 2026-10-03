using Moq;
using TheTechIdea.Beep;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.BeepSync;
using TheTechIdea.Beep.Editor.BeepSync.Helpers;
using TheTechIdea.Beep.Editor.BeepSync.Interfaces;
using TheTechIdea.Beep.Editor.Importing;
using TheTechIdea.Beep.Rules;
using Xunit;

namespace TheTechIdea.Beep.Framework.Tests;

public partial class SyncOutcomeTests
{
    private static Mock<IRuleEngine> ThresholdEngine(Func<Dictionary<string, object>, Dictionary<string, object>> evaluate)
    {
        var engine = new Mock<IRuleEngine>();
        engine.Setup(x => x.HasRule(It.IsAny<string>())).Returns((string key) => key == "threshold");
        engine.Setup(x => x.SolveRule("threshold", It.IsAny<Dictionary<string, object>>(), It.IsAny<RuleExecutionPolicy>()))
            .Returns((string _, Dictionary<string, object> args, RuleExecutionPolicy _) => (evaluate(args), null!));
        return engine;
    }

    private static void EnableThreshold(BeepSyncManager manager, DataSyncSchema schema, Mock<IRuleEngine>? engine)
    {
        schema.DqPolicy = new DqPolicy { BatchThresholdRuleKey = "threshold" };
        manager.IntegrationContext = engine == null ? null : new SyncIntegrationContext { RuleEngine = engine.Object };
    }

    [Fact]
    public async Task SyncThreshold_MissingRequiredEngineCannotAdmitWrites()
    {
        var (manager, schema, destination, source, _, _) = MappedHarness();
        EnableThreshold(manager, schema, null);
        var result = await manager.SyncDataAsync(schema);
        Assert.Equal(Errors.Failed, result.Flag);
        destination.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
        source.Verify(x => x.GetEntity(It.IsAny<string>(), It.IsAny<List<TheTechIdea.Beep.Report.AppFilter>>()), Times.Never);
    }

    [Fact]
    public async Task SyncThreshold_ThrowAfterWriteCannotPublishSuccess()
    {
        var (manager, schema, destination, _, _, _) = MappedHarness();
        EnableThreshold(manager, schema, ThresholdEngine(_ => throw new InvalidOperationException("secret-threshold")));
        var previous = schema.WatermarkPolicy.LastWatermarkValue;
        var result = await manager.SyncDataAsync(schema);
        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Equal("Failed", schema.SyncStatus);
        Assert.Equal(previous, schema.WatermarkPolicy.LastWatermarkValue);
        Assert.DoesNotContain("secret", result.Message);
        destination.Verify(x => x.InsertEntity("target", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task SyncThreshold_AbortRetainsAcknowledgedWrites()
    {
        var (manager, schema, destination, _, _, _) = MappedHarness();
        EnableThreshold(manager, schema, ThresholdEngine(_ => new() { ["action"] = "AbortRun" }));
        var result = await manager.SyncDataAsync(schema);
        var imports = Assert.IsType<ImportExecutionResult>(result);
        Assert.Equal(1, imports.RecordsSucceeded);
        Assert.True(imports.RequiresReconciliation);
        destination.Verify(x => x.InsertEntity("target", It.IsAny<object>()), Times.Once);
    }

    [Theory]
    [InlineData("missing-rule")]
    [InlineData("lookup-throw")]
    [InlineData("lookup-timeout")]
    public async Task SyncThreshold_RequiredLookupFailuresStopBeforeReadsOrDdl(string defect)
    {
        var (manager, schema, destination, source, _, _) = MappedHarness();
        var engine = ThresholdEngine(_ => new() { ["action"] = "ContinueRun" });
        EnableThreshold(manager, schema, engine);
        if (defect == "missing-rule") engine.Setup(x => x.HasRule("threshold")).Returns(false);
        if (defect == "lookup-throw") engine.Setup(x => x.HasRule("threshold")).Throws(new InvalidOperationException("secret-lookup"));
        if (defect == "lookup-timeout")
        {
            schema.RulePolicy = new SyncRulePolicy { MaxExecutionMs = 1 };
            engine.Setup(x => x.HasRule("threshold")).Returns(() => { BusyWait(15); return true; });
        }
        var result = await manager.SyncDataAsync(schema);
        Assert.Equal(Errors.Failed, result.Flag); Assert.DoesNotContain("secret", result.Message);
        destination.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
        destination.Verify(x => x.CreateEntityAs(It.IsAny<TheTechIdea.Beep.DataBase.EntityStructure>()), Times.Never);
        source.Verify(x => x.GetEntity(It.IsAny<string>(), It.IsAny<List<TheTechIdea.Beep.Report.AppFilter>>()), Times.Never);
    }

    private static void BusyWait(int milliseconds)
    {
        // Match the production monotonic duration, not SpinUntil's coarse timeout clock.
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        while (elapsed.ElapsedMilliseconds < milliseconds) Thread.SpinWait(64);
    }

    [Theory]
    [InlineData("absent")]
    [InlineData("null")]
    [InlineData("unknown")]
    [InlineData("object")]
    [InlineData("boolean")]
    [InlineData("outputs-null")]
    public async Task SyncThreshold_MalformedOutputsCannotCoerceIntoPermission(string defect)
    {
        var (manager, schema, destination, _, _, _) = MappedHarness();
        EnableThreshold(manager, schema, ThresholdEngine(_ => defect switch
        {
            "absent" => new(), "null" => new() { ["action"] = null! },
            "unknown" => new() { ["action"] = "Continue" }, "object" => new() { ["action"] = new CoercedAction() },
            "boolean" => new() { ["action"] = true }, _ => null!
        }));
        var result = Assert.IsType<ImportExecutionResult>(await manager.SyncDataAsync(schema));
        Assert.Equal(ImportOutcome.Partial, result.Outcome); Assert.Equal(1, result.RecordsSucceeded);
        Assert.Equal(SyncBatchThresholdOutcome.EvaluationFailed, manager.LastRunBatchThresholdResult.Outcome);
        Assert.True(manager.LastRunBatchThresholdResult.BlocksCompletion);
        destination.Verify(x => x.InsertEntity("target", It.IsAny<object>()), Times.Once);
    }

    private sealed class CoercedAction { public override string ToString() => throw new InvalidOperationException("secret-coercion"); }

    [Theory]
    [InlineData("missing-engine")]
    [InlineData("lookup-throw")]
    [InlineData("solve-throw")]
    [InlineData("abort")]
    public async Task SyncThreshold_AdvisoryErrorsAndDecisionsHaveExplicitEvidence(string defect)
    {
        var (manager, schema, destination, _, _, _) = MappedHarness();
        var engine = ThresholdEngine(_ => defect == "solve-throw" ? throw new InvalidOperationException("secret-advisory") : new() { ["action"] = "AbortRun" });
        EnableThreshold(manager, schema, defect == "missing-engine" ? null : engine);
        schema.DqPolicy.ThresholdFailureMode = QualityFailureMode.Advisory;
        if (defect == "lookup-throw") engine.Setup(x => x.HasRule("threshold")).Throws(new InvalidOperationException("secret-advisory"));
        var result = Assert.IsType<ImportExecutionResult>(await manager.SyncDataAsync(schema));
        Assert.Equal(Errors.Ok, result.Flag); Assert.Equal(1, result.RecordsSucceeded);
        Assert.True(manager.LastRunBatchThresholdResult.HasWarning); Assert.False(manager.LastRunBatchThresholdResult.BlocksCompletion);
        Assert.Equal(defect == "abort" ? SyncBatchThresholdOutcome.Rejected : SyncBatchThresholdOutcome.EvaluationFailed, manager.LastRunBatchThresholdResult.Outcome);
        Assert.DoesNotContain("secret", result.Message);
        destination.Verify(x => x.InsertEntity("target", It.IsAny<object>()), Times.Once);
    }

    [Theory]
    [InlineData("ContinueRun", true)]
    [InlineData("AbortRun", false)]
    public async Task SyncThreshold_EmptyAttemptsHaveZeroRateAndStillEvaluate(string action, bool passed)
    {
        var (manager, schema, destination, source, _, _) = MappedHarness();
        source.Setup(x => x.GetEntity("source", It.IsAny<List<TheTechIdea.Beep.Report.AppFilter>>())).Returns(Array.Empty<object>());
        var engine = ThresholdEngine(args =>
        {
            Assert.Equal(0, args["recordCount"]); Assert.Equal(0, args["rejectCount"]); Assert.Equal(0.0, args["rejectRate"]);
            return new() { ["action"] = action };
        });
        EnableThreshold(manager, schema, engine);
        var result = await manager.SyncDataAsync(schema);
        Assert.Equal(passed ? Errors.Ok : Errors.Failed, result.Flag);
        Assert.Equal(0, manager.LastRunBatchThresholdResult.RecordsAttempted);
        engine.Verify(x => x.SolveRule("threshold", It.IsAny<Dictionary<string, object>>(), It.IsAny<RuleExecutionPolicy>()), Times.Once);
        destination.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

    [Theory]
    [InlineData(50.0, SyncBatchThresholdOutcome.Passed)]
    [InlineData(49.0, SyncBatchThresholdOutcome.Rejected)]
    public async Task SyncThreshold_RealDenominatorIncludesRejectedRowsAndCannotOverrideRecordFailure(double limit, SyncBatchThresholdOutcome outcome)
    {
        var (manager, schema, destination, source, _, _) = MappedHarness();
        source.Setup(x => x.GetEntity("source", It.IsAny<List<TheTechIdea.Beep.Report.AppFilter>>()))
            .Returns(new object[] { new { SourceId = 7, SourceName = "allowed" }, new { SourceId = -1, SourceName = "denied" } });
        var engine = ThresholdEngine(args =>
        {
            Assert.Equal(2, args["recordCount"]); Assert.Equal(1, args["rejectCount"]); Assert.Equal(0.5, args["rejectRate"]);
            return new() { ["action"] = "ContinueRun" };
        });
        engine.Setup(x => x.HasRule("quality")).Returns(true);
        engine.Setup(x => x.SolveRule("quality", It.IsAny<Dictionary<string, object>>(), It.IsAny<RuleExecutionPolicy>()))
            .Returns((string _, Dictionary<string, object> args, RuleExecutionPolicy _) =>
                (new Dictionary<string, object>(), (object)((int?)args["record"].GetType().GetProperty("TargetId")!.GetValue(args["record"]) > 0)));
        EnableThreshold(manager, schema, engine); schema.DqPolicy.RuleKeys.Add("quality"); schema.DqPolicy.MaxRejectRatePercent = limit;
        var result = Assert.IsType<ImportExecutionResult>(await manager.SyncDataAsync(schema));
        Assert.Equal(Errors.Failed, result.Flag); Assert.Equal(1, result.RecordsSucceeded); Assert.Equal(1, result.RecordsQualityRejected);
        Assert.Equal(outcome, manager.LastRunBatchThresholdResult.Outcome);
        destination.Verify(x => x.InsertEntity("target", It.IsAny<object>()), Times.Once);
        engine.Verify(x => x.SolveRule("threshold", It.IsAny<Dictionary<string, object>>(), It.IsAny<RuleExecutionPolicy>()), Times.Once);
    }

    [Fact]
    public async Task SyncThreshold_BidirectionalCountsIncludeActualReversePayloadWrites()
    {
        var (manager, schema, destination, source, _, _) = MappedHarness();
        schema.SyncDirection = "Bidirectional";
        destination.Setup(x => x.GetEntity("target", It.IsAny<List<TheTechIdea.Beep.Report.AppFilter>>()))
            .Returns(new object[] { new { TargetId = 9, TargetName = "reverse" } });
        source.Setup(x => x.InsertEntity("source", It.IsAny<object>())).Returns(new ErrorsInfo { Flag = Errors.Ok });
        var engine = ThresholdEngine(args => { Assert.Equal(2, args["recordCount"]); return new() { ["action"] = "AbortRun" }; });
        EnableThreshold(manager, schema, engine);
        var result = Assert.IsType<ImportExecutionResult>(await manager.SyncDataAsync(schema));
        Assert.Equal(2, result.RecordsSucceeded); Assert.Equal(2, manager.LastRunBatchThresholdResult.RecordsAttempted);
        Assert.Equal(SyncBatchThresholdOutcome.Rejected, manager.LastRunBatchThresholdResult.Outcome);
        Assert.True(result.RequiresReconciliation);
        engine.Verify(x => x.SolveRule("threshold", It.IsAny<Dictionary<string, object>>(), It.IsAny<RuleExecutionPolicy>()), Times.Once);
        source.Verify(x => x.InsertEntity("source", It.Is<object>(row => (int?)row.GetType().GetProperty("SourceId")!.GetValue(row) == 9)), Times.Once);
    }

    [Fact]
    public async Task SyncThreshold_CapturesIntentAcrossCallerRebindAndPersistsOriginalFingerprint()
    {
        var (manager, schema, destination, source, _, _) = MappedHarness();
        schema.RetryPolicy.CheckpointEnabled = true;
        var engine = ThresholdEngine(args => { Assert.Equal(0.05, args["maxRejectRate"]); return new() { ["action"] = "AbortRun" }; });
        EnableThreshold(manager, schema, engine);
        source.Setup(x => x.GetEntity("source", It.IsAny<List<TheTechIdea.Beep.Report.AppFilter>>())).Returns(() =>
        {
            schema.DqPolicy.Enabled = false; schema.DqPolicy.BatchThresholdRuleKey = "other";
            schema.DqPolicy.ThresholdFailureMode = QualityFailureMode.Advisory; schema.DqPolicy.MaxRejectRatePercent = 100;
            schema.RetryPolicy.CheckpointEnabled = false;
            manager.IntegrationContext = new SyncIntegrationContext { RuleEngine = ThresholdEngine(_ => new() { ["action"] = "ContinueRun" }).Object };
            return new object[] { new { SourceId = 7, SourceName = "captured" } };
        });
        var result = Assert.IsType<ImportExecutionResult>(await manager.SyncDataAsync(schema));
        Assert.Equal(Errors.Failed, result.Flag); Assert.Equal(1, result.RecordsSucceeded);
        Assert.Equal(PersistenceWriteStatus.Saved, manager.LastRunFailureCheckpointStatus);
        var checkpoint = await new SchemaPersistenceHelper(manager.Editor, _folder).LoadCheckpointAsync(schema.Id);
        Assert.Equal("Failed", checkpoint.Status); Assert.Equal(1, checkpoint.ProcessedOffset);
        Assert.Equal(SyncBatchThresholdOutcome.Rejected, checkpoint.FailureEvidence.Threshold.Outcome);
        Assert.Equal(QualityFailureMode.Required, checkpoint.FailureEvidence.Threshold.FailureMode);
        destination.Verify(x => x.InsertEntity("target", It.IsAny<object>()), Times.Once);
    }

    [Theory]
    [InlineData("timeout")]
    [InlineData("cancel")]
    public async Task SyncThreshold_ObservedLimitsAfterWritesRetainDurableFailureEvidence(string defect)
    {
        var (manager, schema, destination, _, _, _) = MappedHarness();
        schema.RetryPolicy.CheckpointEnabled = true;
        using var cancellation = new CancellationTokenSource();
        var engine = ThresholdEngine(_ =>
        {
            if (defect == "timeout") BusyWait(15); else cancellation.Cancel();
            return new() { ["action"] = "ContinueRun" };
        });
        EnableThreshold(manager, schema, engine); schema.RulePolicy = new SyncRulePolicy { MaxExecutionMs = 1 };
        var previous = schema.WatermarkPolicy.LastWatermarkValue;
        var result = Assert.IsType<ImportExecutionResult>(await manager.SyncDataAsync(schema, cancellation.Token));
        Assert.Equal(Errors.Failed, result.Flag); Assert.Equal(1, result.RecordsSucceeded);
        Assert.Equal(previous, schema.WatermarkPolicy.LastWatermarkValue);
        Assert.Equal(defect == "cancel" ? "Cancelled" : "Failed", schema.SyncStatus);
        var checkpoint = await new SchemaPersistenceHelper(manager.Editor, _folder).LoadCheckpointAsync(schema.Id);
        Assert.Equal("Failed", checkpoint.Status); Assert.Equal(1, checkpoint.FailureEvidence.RecordsAcknowledged);
        Assert.Equal(defect == "cancel" ? SyncRunFailureKind.Cancelled : SyncRunFailureKind.QualityThreshold, checkpoint.FailureEvidence.Kind);
        destination.Verify(x => x.InsertEntity("target", It.IsAny<object>()), Times.Once);
    }

    [Theory]
    [InlineData("key")]
    [InlineData("rate-negative")]
    [InlineData("rate-large")]
    [InlineData("rate-nan")]
    [InlineData("rate-infinity")]
    [InlineData("mode")]
    [InlineData("depth")]
    [InlineData("timeout")]
    public async Task SyncThreshold_InvalidPolicyFailsAdmissionEvenInAdvisoryMode(string defect)
    {
        var (manager, schema, destination, _, _, _) = MappedHarness();
        EnableThreshold(manager, schema, ThresholdEngine(_ => new() { ["action"] = "ContinueRun" }));
        schema.DqPolicy.ThresholdFailureMode = QualityFailureMode.Advisory;
        switch (defect)
        {
            case "key": schema.DqPolicy.BatchThresholdRuleKey = " "; break;
            case "rate-negative": schema.DqPolicy.MaxRejectRatePercent = -1; break;
            case "rate-large": schema.DqPolicy.MaxRejectRatePercent = 101; break;
            case "rate-nan": schema.DqPolicy.MaxRejectRatePercent = double.NaN; break;
            case "rate-infinity": schema.DqPolicy.MaxRejectRatePercent = double.PositiveInfinity; break;
            case "mode": schema.DqPolicy.ThresholdFailureMode = (QualityFailureMode)99; break;
            case "depth": schema.RulePolicy = new SyncRulePolicy { MaxDepth = 65 }; break;
            case "timeout": schema.RulePolicy = new SyncRulePolicy { MaxExecutionMs = 60001 }; break;
        }
        Assert.Equal(Errors.Failed, (await manager.SyncDataAsync(schema)).Flag);
        destination.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

    [Theory]
    [InlineData(PersistenceWriteStatus.Failed)]
    [InlineData(PersistenceWriteStatus.Cancelled)]
    [InlineData(PersistenceWriteStatus.Unsupported)]
    [InlineData(PersistenceWriteStatus.Saved)]
    public async Task SyncThreshold_FailedCheckpointAcknowledgementRetainsOriginalCounts(PersistenceWriteStatus status)
    {
        var storage = new Mock<ISchemaPersistenceHelper>();
        storage.Setup(x => x.LoadSchemasAsync()).ReturnsAsync(new ObservableBindingList<DataSyncSchema>());
        var saved = new List<SyncCheckpoint>();
        storage.As<ISyncPersistenceAcknowledgement>().Setup(x => x.SaveCheckpointAcknowledgedAsync(It.IsAny<SyncCheckpoint>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SyncCheckpoint checkpoint, CancellationToken _) =>
            {
                saved.Add(checkpoint);
                return new PersistenceWriteResult(checkpoint.Status == "Running" ? PersistenceWriteStatus.Saved : status);
            });
        var (manager, schema, destination, _) = Harness(storage.Object);
        schema.RetryPolicy = new RetryPolicy { MaxAttempts = 3, BaseDelayMs = 1 };
        destination.Setup(x => x.InsertEntity("target", It.IsAny<object>())).Returns(new ErrorsInfo { Flag = Errors.Ok });
        EnableThreshold(manager, schema, ThresholdEngine(_ => new() { ["action"] = "AbortRun" }));
        var result = await manager.SyncDataAsync(schema);
        Assert.Equal(Errors.Failed, result.Flag); Assert.Equal(status, manager.LastRunFailureCheckpointStatus);
        Assert.Equal(new[] { "Running", "Failed" }, saved.Select(x => x.Status));
        var failed = saved[1]; Assert.Equal(saved[0].RunId, failed.RunId); Assert.Equal(saved[0].SchemaFingerprint, failed.SchemaFingerprint);
        Assert.Equal(1, failed.ProcessedOffset); Assert.Equal(1, failed.FailureEvidence.RecordsAcknowledged);
        Assert.Equal(1, failed.FailureEvidence.RecordsAttempted); Assert.True(failed.RequiresReconciliation);
        if (status != PersistenceWriteStatus.Saved)
        {
            var failure = Assert.IsType<SyncCheckpointFailureResult>(result);
            Assert.Equal("Failure", failure.CheckpointStage); Assert.Equal(1, failure.RecordsAcknowledged);
            Assert.Equal(1, failure.ImportResult.RecordsSucceeded); Assert.True(failure.RequiresReconciliation);
        }
        else Assert.Equal(1, Assert.IsType<ImportExecutionResult>(result).RecordsSucceeded);
        destination.Verify(x => x.InsertEntity("target", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task SyncThreshold_RestartPreservesFailedEvidenceAndBlocksReplay()
    {
        var (manager, schema, destination, _, _, _) = MappedHarness();
        schema.RetryPolicy.CheckpointEnabled = true;
        EnableThreshold(manager, schema, ThresholdEngine(_ => new() { ["action"] = "AbortRun" }));
        var result = Assert.IsType<ImportExecutionResult>(await manager.SyncDataAsync(schema));
        Assert.Equal(1, result.RecordsSucceeded);
        var store = new SchemaPersistenceHelper(manager.Editor, _folder);
        var bytes = File.ReadAllBytes(store.GetCheckpointFilePath(schema.Id));
        using var restart = new BeepSyncManager(manager.Editor, manager.IntegrationContext, _folder);
        Assert.Equal(Errors.Failed, (await restart.SyncDataAsync(schema)).Flag);
        Assert.Equal(bytes, File.ReadAllBytes(store.GetCheckpointFilePath(schema.Id)));
        Assert.True((await store.LoadCheckpointAsync(schema.Id)).RequiresReconciliation);
        Assert.Equal(1, manager.LastRunReconciliationReport.DestRowsWritten);
        Assert.Equal(1, manager.LastRunReconciliationReport.SourceRowsScanned);
        destination.Verify(x => x.InsertEntity("target", It.IsAny<object>()), Times.Once);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("throw")]
    public async Task SyncThreshold_RuleDisappearingAfterWritesCannotBecomePass(string defect)
    {
        var (manager, schema, destination, _, _, _) = MappedHarness();
        var engine = ThresholdEngine(_ => new() { ["action"] = "ContinueRun" });
        int lookups = 0;
        engine.Setup(x => x.HasRule("threshold")).Returns(() => ++lookups == 1 ? true :
            defect == "missing" ? false : throw new InvalidOperationException("secret-lookup"));
        EnableThreshold(manager, schema, engine);
        var result = Assert.IsType<ImportExecutionResult>(await manager.SyncDataAsync(schema));
        Assert.Equal(1, result.RecordsSucceeded); Assert.Equal(Errors.Failed, result.Flag);
        Assert.Equal(SyncBatchThresholdOutcome.EvaluationFailed, manager.LastRunBatchThresholdResult.Outcome);
        engine.Verify(x => x.SolveRule("threshold", It.IsAny<Dictionary<string, object>>(), It.IsAny<RuleExecutionPolicy>()), Times.Never);
        destination.Verify(x => x.InsertEntity("target", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task SyncThreshold_FreshCapturedRulePolicyIsUsedForEveryAttempt()
    {
        var (manager, schema, destination, _, _, _) = MappedHarness();
        var policies = new List<RuleExecutionPolicy>();
        var engine = ThresholdEngine(_ => new() { ["action"] = "ContinueRun" });
        schema.RetryPolicy.ErrorCategoryRuleKey = "triage";
        engine.Setup(x => x.HasRule("triage")).Returns(true);
        engine.Setup(x => x.SolveRule("triage", It.IsAny<Dictionary<string, object>>(), It.IsAny<RuleExecutionPolicy>()))
            .Returns((new Dictionary<string, object> { ["category"] = "Transient", ["action"] = "Retry" }, null!));
        EnableThreshold(manager, schema, engine); schema.RulePolicy = new SyncRulePolicy { MaxDepth = 7, MaxExecutionMs = 250 };
        var capturedDepths = new List<int>();
        engine.Setup(x => x.SolveRule("threshold", It.IsAny<Dictionary<string, object>>(), It.IsAny<RuleExecutionPolicy>()))
            .Returns((string _, Dictionary<string, object> args, RuleExecutionPolicy policy) =>
            {
                capturedDepths.Add(policy.MaxDepth); policies.Add(policy); policy.MaxDepth = 64;
                return (new Dictionary<string, object> { ["action"] = "ContinueRun" }, null!);
            });
        destination.SetupSequence(x => x.InsertEntity("target", It.IsAny<object>()))
            .Returns(new ErrorsInfo { Flag = Errors.Failed }).Returns(new ErrorsInfo { Flag = Errors.Ok });
        Assert.Equal(Errors.Ok, (await manager.SyncDataAsync(schema)).Flag);
        Assert.Equal(new[] { 7, 7 }, capturedDepths); Assert.Equal(2, policies.Count); Assert.NotSame(policies[0], policies[1]);
        Assert.All(policies, policy => Assert.Equal(250, policy.MaxExecutionMs));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("throw")]
    public async Task SyncThreshold_FailedPublicationAdapterErrorsCannotConcealAcknowledgements(string defect)
    {
        var storage = new Mock<ISchemaPersistenceHelper>();
        storage.Setup(x => x.LoadSchemasAsync()).ReturnsAsync(new ObservableBindingList<DataSyncSchema>());
        storage.As<ISyncPersistenceAcknowledgement>().Setup(x => x.SaveCheckpointAcknowledgedAsync(It.IsAny<SyncCheckpoint>(), It.IsAny<CancellationToken>()))
            .Returns((SyncCheckpoint checkpoint, CancellationToken _) => checkpoint.Status == "Running"
                ? Task.FromResult(new PersistenceWriteResult(PersistenceWriteStatus.Saved))
                : defect == "null" ? Task.FromResult<PersistenceWriteResult>(null!) : Task.FromException<PersistenceWriteResult>(new IOException("secret-store")));
        var (manager, schema, destination, _) = Harness(storage.Object);
        schema.RetryPolicy = new RetryPolicy { BaseDelayMs = 1 };
        destination.Setup(x => x.InsertEntity("target", It.IsAny<object>())).Returns(new ErrorsInfo { Flag = Errors.Ok });
        EnableThreshold(manager, schema, ThresholdEngine(_ => new() { ["action"] = "AbortRun" }));
        var result = Assert.IsType<SyncCheckpointFailureResult>(await manager.SyncDataAsync(schema));
        Assert.Equal(1, result.RecordsAcknowledged); Assert.Equal(1, result.ImportResult.RecordsSucceeded);
        Assert.Equal(PersistenceWriteStatus.Failed, result.CheckpointPersistenceStatus);
        Assert.DoesNotContain("secret", result.Message);
        destination.Verify(x => x.InsertEntity("target", It.IsAny<object>()), Times.Once);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SyncThreshold_ReverseRejectOrCancellationPersistsBothDirectionEvidence(bool cancel)
    {
        var (manager, schema, destination, source, _, _) = MappedHarness();
        schema.SyncDirection = "Bidirectional"; schema.RetryPolicy.CheckpointEnabled = true;
        destination.Setup(x => x.GetEntity("target", It.IsAny<List<TheTechIdea.Beep.Report.AppFilter>>()))
            .Returns(new object[] { new { TargetId = 9, TargetName = "reverse" } });
        using var cancellation = new CancellationTokenSource();
        var engine = ThresholdEngine(_ => new() { ["action"] = "ContinueRun" });
        engine.Setup(x => x.HasRule("quality")).Returns(true);
        engine.Setup(x => x.SolveRule("quality", It.IsAny<Dictionary<string, object>>(), It.IsAny<RuleExecutionPolicy>()))
            .Returns((string _, Dictionary<string, object> args, RuleExecutionPolicy _) =>
            {
                bool forward = (string)args["entityName"] == "target";
                if (!forward && cancel) cancellation.Cancel();
                return (new Dictionary<string, object>(), (object)forward);
            });
        EnableThreshold(manager, schema, engine); schema.DqPolicy.RuleKeys.Add("quality");
        var result = Assert.IsType<ImportExecutionResult>(await manager.SyncDataAsync(schema, cancellation.Token));
        Assert.Equal(1, result.RecordsSucceeded); Assert.Equal(Errors.Failed, result.Flag);
        var checkpoint = await new SchemaPersistenceHelper(manager.Editor, _folder).LoadCheckpointAsync(schema.Id);
        Assert.Equal("Failed", checkpoint.Status); Assert.Equal(1, checkpoint.ProcessedOffset);
        Assert.Equal(2, checkpoint.TotalExpected); Assert.Equal(2, checkpoint.FailureEvidence.RecordsQualityEvaluated);
        Assert.Equal(cancel ? 0 : 1, checkpoint.FailureEvidence.RecordsQualityRejected);
        Assert.Equal(cancel ? SyncRunFailureKind.Cancelled : SyncRunFailureKind.QualityThreshold, checkpoint.FailureEvidence.Kind);
        source.Verify(x => x.InsertEntity("source", It.IsAny<object>()), Times.Never);
        destination.Verify(x => x.InsertEntity("target", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task SyncThreshold_WindowsFailureSnapshotDenialPreservesRunningEvidenceAndCounts()
    {
        if (!OperatingSystem.IsWindows()) return;
        var (manager, schema, destination, _, _, _) = MappedHarness();
        schema.RetryPolicy.CheckpointEnabled = true;
        EnableThreshold(manager, schema, ThresholdEngine(_ => new() { ["action"] = "AbortRun" }));
        var store = new SchemaPersistenceHelper(manager.Editor, _folder);
        var path = store.GetCheckpointFilePath(schema.Id);
        FileStream? held = null; byte[]? running = null;
        destination.Setup(x => x.InsertEntity("target", It.IsAny<object>())).Returns(() =>
        {
            running = File.ReadAllBytes(path);
            held = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            return new ErrorsInfo { Flag = Errors.Ok };
        });
        try
        {
            var result = Assert.IsType<SyncCheckpointFailureResult>(await manager.SyncDataAsync(schema));
            Assert.Equal("Failure", result.CheckpointStage); Assert.Equal(1, result.RecordsAcknowledged);
            Assert.NotEqual(PersistenceWriteStatus.Saved, result.CheckpointPersistenceStatus);
            Assert.Equal(running, File.ReadAllBytes(path));
            Assert.Equal("Running", (await store.LoadCheckpointAsync(schema.Id)).Status);
            destination.Verify(x => x.InsertEntity("target", It.IsAny<object>()), Times.Once);
        }
        finally { held?.Dispose(); }
    }
}
