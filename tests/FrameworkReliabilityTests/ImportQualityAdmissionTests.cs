using Moq;
using System.Data;
using System.Diagnostics;
using System.Dynamic;
using TheTechIdea.Beep;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Importing;
using TheTechIdea.Beep.Editor.Importing.Helpers;
using TheTechIdea.Beep.Editor.Importing.Interfaces;
using Xunit;

namespace TheTechIdea.Beep.Framework.Tests;

public class ImportQualityAdmissionTests
{
    private static DataImportBatchHelper Helper() => new(Mock.Of<IDMEEditor>(),
        new DataImportTransformationHelper(Mock.Of<IDMEEditor>()), Mock.Of<IDataImportProgressHelper>());

    private sealed class Rule : IDataQualityRule
    {
        public string RuleName { get; set; } = "rule";
        public string FieldName { get; set; } = "Id";
        public DataQualityAction OnFailure { get; set; } = DataQualityAction.Block;
        public int Calls { get; private set; }
        public Func<object?, bool> Predicate { get; set; } = value => Convert.ToInt32(value) > 0;
        public bool Evaluate(object? value, object record) { Calls++; return Predicate(value); }
        public string FailureMessage(object? value) => throw new InvalidOperationException("secret-message");
    }

    [Fact]
    public async Task QualityRules_BlockInvalidTransformedRowBeforeWrite()
    {
        var destination = new Mock<IDataSource>();
        destination.Setup(x => x.InsertEntity("target", It.IsAny<object>())).Returns(new ErrorsInfo { Flag = Errors.Ok });
        var config = ImportWriteTests.Config(destination);
        var rule = new Rule();
        config.QualityRules.Add(rule);
        var result = await Helper().ProcessBatchDetailedAsync(new object[] { new { Id = 1 }, new { Id = -1 } }, config, null!, default);
        Assert.Equal(ImportOutcome.Partial, result.Outcome);
        Assert.Equal(1, result.RecordsSucceeded);
        Assert.Equal(1, result.WriteAttempts);
        Assert.Equal(2, rule.Calls);
    }

    [Fact]
    public async Task QualityRules_RequiredEvaluationExceptionCannotAdmitWrite()
    {
        var destination = new Mock<IDataSource>();
        destination.Setup(x => x.InsertEntity("target", It.IsAny<object>())).Returns(new ErrorsInfo { Flag = Errors.Ok });
        var config = ImportWriteTests.Config(destination);
        config.QualityRules.Add(new Rule { Predicate = _ => throw new InvalidOperationException("secret-record") });
        var result = await Helper().ProcessBatchDetailedAsync(new object[] { new { Id = 1 } }, config, null!, default);
        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Equal(0, result.WriteAttempts);
        Assert.DoesNotContain("secret-record", result.Message);
        destination.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task QualityRules_ReadActualTransformedDictionaryRatherThanInput()
    {
        var destination = new Mock<IDataSource>();
        destination.Setup(x => x.InsertEntity("target", It.IsAny<object>())).Returns(new ErrorsInfo { Flag = Errors.Ok });
        var config = ImportWriteTests.Config(destination);
        var rule = new Rule { Predicate = value => (int)value! == 7 };
        config.QualityRules.Add(rule);
        config.CustomTransformation = _ => new Dictionary<string, object> { ["id"] = 7 };
        var result = await Helper().ProcessBatchDetailedAsync(new object[] { new { Id = -1 } }, config, null!, default);
        Assert.Equal(Errors.Ok, result.Flag);
        Assert.Equal(1, rule.Calls);
        destination.Verify(x => x.InsertEntity("target", It.Is<object>(value =>
            ((Dictionary<string, object>)value)["id"].Equals(7))), Times.Once);
    }

    [Theory]
    [InlineData("dictionary")]
    [InlineData("expando")]
    [InlineData("data-row")]
    [InlineData("poco")]
    public async Task QualityRules_ReadSupportedRecordShapes(string shape)
    {
        object row;
        if (shape == "dictionary") row = new Dictionary<string, object> { ["id"] = -1 };
        else if (shape == "expando") { var values = (IDictionary<string, object?>)new ExpandoObject(); values["id"] = -1; row = values; }
        else if (shape == "data-row") { var table = new DataTable(); table.Columns.Add("id", typeof(int)); row = table.Rows.Add(-1); }
        else row = new { Id = -1 };
        var destination = new Mock<IDataSource>();
        var config = ImportWriteTests.Config(destination);
        config.QualityRules.Add(new Rule());
        var result = await Helper().ProcessBatchDetailedAsync(new[] { row }, config, null!, default);
        Assert.Equal(1, result.RecordsQualityEvaluated);
        Assert.Equal(1, result.RecordsQualityRejected);
        Assert.Equal(1, result.RecordsBlocked);
        Assert.Equal(0, result.RecordsQualityEvaluationFailed);
        Assert.Equal(0, result.WriteAttempts);
        Assert.False(result.HasUncertainWrites);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QualityRules_MissingOrAmbiguousFieldsAreEvaluationFailures(bool ambiguous)
    {
        var row = new Dictionary<string, object>();
        if (ambiguous) { row["id"] = 1; row["Id"] = 2; }
        var config = ImportWriteTests.Config(new Mock<IDataSource>());
        var rule = new Rule { Predicate = _ => true };
        config.QualityRules.Add(rule);
        var result = await Helper().ProcessBatchDetailedAsync(new[] { row }, config, null!, default);
        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Equal(1, result.RecordsQualityEvaluationFailed);
        Assert.Equal(0, result.RecordsQualityRejected);
        Assert.Equal(0, result.WriteAttempts);
        Assert.Equal(0, rule.Calls);
    }

    [Theory]
    [InlineData(QualityFailureMode.Required)]
    [InlineData(QualityFailureMode.Advisory)]
    public async Task QualityRules_EvaluationFailureUsesCapturedExplicitMode(QualityFailureMode mode)
    {
        var destination = new Mock<IDataSource>();
        destination.Setup(x => x.InsertEntity("target", It.IsAny<object>())).Returns(new ErrorsInfo { Flag = Errors.Ok });
        var config = ImportWriteTests.Config(destination);
        config.QualityFailureMode = mode;
        config.QualityRules.Add(new Rule { Predicate = _ => throw new InvalidOperationException("secret-rule") });
        var result = await Helper().ProcessBatchDetailedAsync(new object[] { new { Id = 1 } }, config, null!, default);
        Assert.Equal(1, result.RecordsQualityEvaluationFailed);
        Assert.Equal(mode == QualityFailureMode.Advisory ? 1 : 0, result.RecordsWarned);
        Assert.Equal(mode == QualityFailureMode.Advisory ? 1 : 0, result.RecordsSucceeded);
        Assert.Equal(mode == QualityFailureMode.Advisory ? Errors.Ok : Errors.Failed, result.Flag);
        Assert.Null(result.Ex);
        Assert.All(result.Errors, error => { Assert.Null(error.Ex); Assert.DoesNotContain("secret-rule", error.Message); });
    }

    [Fact]
    public async Task QualityRules_WarnAllowsWriteWithoutCountingRejection()
    {
        var destination = new Mock<IDataSource>();
        destination.Setup(x => x.InsertEntity("target", It.IsAny<object>())).Returns(new ErrorsInfo { Flag = Errors.Ok });
        var config = ImportWriteTests.Config(destination);
        config.QualityRules.Add(new Rule { OnFailure = DataQualityAction.Warn });
        var result = await Helper().ProcessBatchDetailedAsync(new object[] { new { Id = -1 } }, config, null!, default);
        Assert.Equal(Errors.Ok, result.Flag);
        Assert.Equal(1, result.RecordsWarned);
        Assert.Equal(0, result.RecordsQualityRejected);
        Assert.Equal(0, result.RecordsFailed);
    }

    [Fact]
    public async Task QualityRules_AreEvaluatedOnceNotPerProviderRetry()
    {
        var destination = new Mock<IDataSource>();
        destination.SetupSequence(x => x.InsertEntity("target", It.IsAny<object>()))
            .Returns(new ErrorsInfo { Flag = Errors.Failed }).Returns(new ErrorsInfo { Flag = Errors.Ok });
        var config = ImportWriteTests.Config(destination);
        var rule = new Rule(); config.QualityRules.Add(rule);
        var result = await Helper().ProcessBatchDetailedAsync(new object[] { new { Id = 1 } }, config, null!, default, 1);
        Assert.Equal(1, result.RecordsQualityEvaluated);
        Assert.Equal(1, rule.Calls);
        Assert.Equal(2, result.WriteAttempts);
        Assert.Equal(1, result.RecordsSucceeded);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QualityRules_QuarantineAcknowledgementAndFailureAreDistinct(bool storeFails)
    {
        var destination = new Mock<IDataSource>();
        var store = new Mock<IImportErrorStore>();
        ImportErrorRecord? saved = null;
        store.Setup(x => x.SaveAsync(It.IsAny<ImportErrorRecord>(), It.IsAny<CancellationToken>()))
            .Returns((ImportErrorRecord record, CancellationToken _) =>
            { saved = record; return storeFails ? Task.FromException(new IOException("secret-store")) : Task.CompletedTask; });
        var config = ImportWriteTests.Config(destination);
        config.ErrorStore = store.Object;
        config.QualityRules.Add(new Rule { OnFailure = DataQualityAction.Quarantine });
        var row = new { Id = -1 };
        var result = await Helper().ProcessBatchDetailedAsync(new object[] { row }, config, null!, default);
        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Equal(1, result.RecordsQualityRejected);
        Assert.Equal(storeFails ? 0 : 1, result.RecordsQuarantined);
        Assert.Equal(storeFails ? 1 : 0, result.RejectStoreFailures);
        Assert.Equal(0, result.WriteAttempts);
        Assert.Same(row, saved!.RawRecord);
        Assert.Equal("Record rejected by configured quality admission.", saved.Reason);
        Assert.All(result.Errors, error => Assert.DoesNotContain("secret", error.Message));
    }

    [Fact]
    public async Task QualityRules_MissingQuarantineStoreDeniesBeforeTargetCreation()
    {
        var destination = new Mock<IDataSource>();
        var config = ImportWriteTests.Config(destination, new { Id = -1 });
        config.CreateDestinationIfNotExists = true;
        config.QualityRules.Add(new Rule { OnFailure = DataQualityAction.Quarantine });
        using var manager = new DataImportManager(Mock.Of<IDMEEditor>());
        var result = Assert.IsType<ImportExecutionResult>(await manager.RunImportAsync(config, null!, default));
        Assert.True(result.QualityAdmissionFailed);
        Assert.Equal(0, result.RecordsAttempted);
        destination.Verify(x => x.CreateEntityAs(It.IsAny<TheTechIdea.Beep.DataBase.EntityStructure>()), Times.Never);
        destination.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task QualityRules_ManagerCapturesDescriptorsAcrossBatchesAndPublishesCounters()
    {
        var destination = new Mock<IDataSource>();
        var rule = new Rule();
        var config = ImportWriteTests.Config(destination, new { Id = 1 }, new { Id = -1 });
        config.QualityRules.Add(rule); config.BatchSize = 1;
        destination.Setup(x => x.InsertEntity("target", It.IsAny<object>())).Returns(() =>
        {
            rule.FieldName = "Missing"; rule.OnFailure = DataQualityAction.Warn;
            config.QualityRules.Clear(); config.QualityFailureMode = QualityFailureMode.Advisory;
            return new ErrorsInfo { Flag = Errors.Ok };
        });
        using var manager = new DataImportManager(Mock.Of<IDMEEditor>());
        var result = Assert.IsType<ImportExecutionResult>(await manager.RunImportAsync(config, null!, default));
        Assert.Equal(ImportOutcome.Partial, result.Outcome);
        Assert.Equal(2, result.RecordsQualityEvaluated);
        Assert.Equal(1, result.RecordsQualityRejected);
        Assert.Equal(1, result.RecordsSucceeded);
        Assert.Equal(1, manager.GetImportStatus().RecordsBlocked);
        Assert.Equal(2, rule.Calls);
        destination.Verify(x => x.InsertEntity("target", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task QualityRules_CancellationInsidePredicateIsNotAnUncertainWrite()
    {
        using var cancellation = new CancellationTokenSource();
        var config = ImportWriteTests.Config(new Mock<IDataSource>());
        config.QualityRules.Add(new Rule { Predicate = _ => { cancellation.Cancel(); return true; } });
        var result = await Helper().ProcessBatchDetailedAsync(new object[] { new { Id = 1 } }, config, null!, cancellation.Token);
        Assert.Equal(ImportOutcome.Cancelled, result.Outcome);
        Assert.Equal(0, result.WriteAttempts);
        Assert.False(result.HasUncertainWrites);
    }

    [Fact]
    public async Task QualityRules_ObservedTimeLimitDeniesReturnedResult()
    {
        var config = ImportWriteTests.Config(new Mock<IDataSource>());
        config.QualityRuleTimeoutMs = 1;
        config.QualityRules.Add(new Rule { Predicate = _ => { var clock = Stopwatch.StartNew(); while (clock.ElapsedMilliseconds < 3) Thread.SpinWait(20); return true; } });
        var result = await Helper().ProcessBatchDetailedAsync(new object[] { new { Id = 1 } }, config, null!, default);
        Assert.Equal(1, result.RecordsQualityEvaluationFailed);
        Assert.Equal(0, result.WriteAttempts);
    }

    [Fact]
    public async Task QualityRules_TransformationFailureNeverEvaluatesQuality()
    {
        var config = ImportWriteTests.Config(new Mock<IDataSource>());
        var rule = new Rule(); config.QualityRules.Add(rule);
        config.CustomTransformation = _ => throw new InvalidOperationException("secret-transform");
        var result = await Helper().ProcessBatchDetailedAsync(new object[] { new { Id = 1 } }, config, null!, default);
        Assert.Equal(0, rule.Calls);
        Assert.Equal(0, result.RecordsQualityEvaluated);
        Assert.Equal(1, result.RecordsTransformationFailed);
    }

    [Theory]
    [InlineData("action")]
    [InlineData("mode")]
    [InlineData("timeout")]
    [InlineData("null-rule")]
    [InlineData("rule-limit")]
    public async Task QualityRules_InvalidConfigurationFailsBeforeRows(string defect)
    {
        var config = ImportWriteTests.Config(new Mock<IDataSource>());
        var rule = new Rule(); config.QualityRules.Add(rule);
        switch (defect)
        {
            case "action": rule.OnFailure = (DataQualityAction)99; break;
            case "mode": config.QualityFailureMode = (QualityFailureMode)99; break;
            case "timeout": config.QualityRuleTimeoutMs = 0; break;
            case "null-rule": config.QualityRules.Add(null!); break;
            case "rule-limit": config.QualityRules.AddRange(Enumerable.Repeat<IDataQualityRule>(rule, 128)); break;
        }
        var result = await Helper().ProcessBatchDetailedAsync(new object[] { new { Id = 1 } }, config, null!, default);
        Assert.True(result.QualityAdmissionFailed);
        Assert.Equal(0, rule.Calls);
        Assert.Equal(0, result.WriteAttempts);
    }

    private sealed class Gate : IImportRecordAdmission
    {
        public bool RequiresRejectStore => false;
        public void Validate(CancellationToken token) { }
        public ImportRecordAdmissionResult Evaluate(object record, CancellationToken token) => null!;
    }

    [Fact]
    public async Task QualityRules_NullDedicatedGateOutcomeNeverFallsBackToPass()
    {
        var config = ImportWriteTests.Config(new Mock<IDataSource>());
        config.RecordAdmission = new Gate();
        var result = await Helper().ProcessBatchDetailedAsync(new object[] { new { Id = 1 } }, config, null!, default);
        Assert.Equal(1, result.RecordsQualityEvaluationFailed);
        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Equal(0, result.WriteAttempts);
    }

    private sealed class OpaqueBatchManager : DataImportManager
    {
        public OpaqueBatchManager(IDMEEditor editor, IDataImportBatchHelper batch) : base(editor) { _batchHelper = batch; }
    }

    [Fact]
    public async Task QualityRules_OpaqueCustomBatchCannotSilentlyBypassAdmission()
    {
        var batch = new Mock<IDataImportBatchHelper>();
        var config = ImportWriteTests.Config(new Mock<IDataSource>(), new { Id = 1 });
        config.QualityRules.Add(new Rule());
        using var manager = new OpaqueBatchManager(Mock.Of<IDMEEditor>(), batch.Object);
        var result = Assert.IsType<ImportExecutionResult>(await manager.RunImportAsync(config, null!, default));
        Assert.True(result.QualityAdmissionFailed);
        batch.Verify(x => x.ProcessBatchAsync(It.IsAny<IEnumerable<object>>(), It.IsAny<DataImportConfiguration>(),
            It.IsAny<IProgress<TheTechIdea.Beep.Addin.PassedArgs>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(Errors.Failed)]
    [InlineData(Errors.Warning)]
    [InlineData(Errors.Unknown)]
    public async Task QualityRules_DataSourceErrorStoreRequiresActualWriteAcknowledgement(Errors flag)
    {
        var data = new Mock<IDataSource>();
        data.Setup(x => x.InsertEntity("import_errors", It.IsAny<object>())).Returns(new ErrorsInfo { Flag = flag });
        var editor = new Mock<IDMEEditor>();
        editor.Setup(x => x.GetDataSource("rejects")).Returns(data.Object);
        var store = new TheTechIdea.Beep.Editor.Importing.ErrorStore.DataSourceImportErrorStore(editor.Object, "rejects");
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => store.SaveAsync(new ImportErrorRecord()));
        Assert.Equal("Import reject-store write was not acknowledged.", error.Message);
    }

    [Fact]
    public async Task QualityRules_RejectSaveCancellationIsRecordedWithoutTargetUncertainty()
    {
        using var cancellation = new CancellationTokenSource();
        var store = new Mock<IImportErrorStore>();
        store.Setup(x => x.SaveAsync(It.IsAny<ImportErrorRecord>(), It.IsAny<CancellationToken>()))
            .Returns(() => { cancellation.Cancel(); return Task.FromCanceled(cancellation.Token); });
        var config = ImportWriteTests.Config(new Mock<IDataSource>());
        config.ErrorStore = store.Object;
        config.QualityRules.Add(new Rule { OnFailure = DataQualityAction.Quarantine });
        var result = await Helper().ProcessBatchDetailedAsync(new object[] { new { Id = -1 } }, config, null!, cancellation.Token);
        Assert.Equal(ImportOutcome.Cancelled, result.Outcome);
        Assert.Equal(1, result.RejectStoreFailures);
        Assert.Equal(1, result.RecordsQualityRejected);
        Assert.Equal(0, result.WriteAttempts);
        Assert.False(result.HasUncertainWrites);
    }

    public sealed class DefaultedRow { public int? Id { get; set; } }

    [Fact]
    public async Task QualityRules_EvaluateAfterActualConfiguredDefaultsStage()
    {
        var destination = new Mock<IDataSource>();
        destination.Setup(x => x.InsertEntity("target", It.IsAny<object>())).Returns(new ErrorsInfo { Flag = Errors.Ok });
        var config = ImportWriteTests.Config(destination);
        config.ApplyDefaults = true;
        config.DefaultValues.Add(new TheTechIdea.Beep.ConfigUtil.DefaultValue { PropertyName = "Id", PropertyValue = "7" });
        config.DestEntityStructure = new() { Fields = new() { new() { FieldName = "Id", Fieldtype = "System.Int32" } } };
        var rule = new Rule { Predicate = value => (int?)value == 7 }; config.QualityRules.Add(rule);
        var result = await Helper().ProcessBatchDetailedAsync(new object[] { new DefaultedRow() }, config, null!, default);
        Assert.Equal(Errors.Ok, result.Flag);
        Assert.Equal(1, rule.Calls);
        Assert.Equal(1, result.RecordsSucceeded);
    }

    [Fact]
    public void QualityRules_RuntimeGateIsNotSerializedAsPersistedConfiguration()
    {
        var config = new DataImportConfiguration { RecordAdmission = new Gate() };
        Assert.DoesNotContain("RecordAdmission", System.Text.Json.JsonSerializer.Serialize(config));
        Assert.DoesNotContain("RecordAdmission", Newtonsoft.Json.JsonConvert.SerializeObject(config));
    }
}
