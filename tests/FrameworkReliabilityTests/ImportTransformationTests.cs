using Moq;
using System.Data;
using System.Dynamic;
using TheTechIdea.Beep;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Importing;
using TheTechIdea.Beep.Editor.Importing.Helpers;
using TheTechIdea.Beep.Editor.Importing.Interfaces;
using TheTechIdea.Beep.Editor.Mapping;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Workflow.Mapping;
using TheTechIdea.Beep.Tools;
using Xunit;
using DefaultValue = TheTechIdea.Beep.ConfigUtil.DefaultValue;

namespace TheTechIdea.Beep.Framework.Tests;

public class ImportTransformationTests
{
    private const string Secret = "sensitive-row-value-do-not-log";

    private static DataImportBatchHelper Helper(IDMEEditor? editor = null)
    {
        if (editor == null)
        {
            var concreteEditor = new Mock<IDMEEditor>();
            concreteEditor.SetupGet(value => value.classCreator).Returns(new ClassCreator(concreteEditor.Object));
            editor = concreteEditor.Object;
        }
        return new(editor, new DataImportTransformationHelper(editor), Mock.Of<IDataImportProgressHelper>());
    }

    private static Mock<IDataSource> Destination()
    {
        var destination = new Mock<IDataSource>();
        destination.Setup(x => x.InsertEntity("target", It.IsAny<object>()))
            .Returns(new ErrorsInfo { Flag = Errors.Ok });
        return destination;
    }

    [Fact]
    public async Task ThrowingCustomTransformationDoesNotWriteInputOrExposeException()
    {
        var destination = Destination();
        var config = ImportWriteTests.Config(destination);
        config.CustomTransformation = _ => throw new InvalidOperationException(Secret);
        var result = await Helper().ProcessBatchDetailedAsync(new object[] { Secret }, config, null!, default, 3);

        Assert.Equal(ImportOutcome.Failed, result.Outcome);
        Assert.Equal(1, result.RecordsFailed);
        Assert.Equal(1, result.RecordsTransformationFailed);
        Assert.Equal(0, result.WriteAttempts);
        Assert.False(result.HasUncertainWrites);
        Assert.DoesNotContain(Secret, result.Message);
        Assert.All(result.Errors, error =>
        {
            Assert.DoesNotContain(Secret, error.Message);
            Assert.Null(error.Ex);
        });
        destination.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task ThrowingProjectionGetterDoesNotWriteUnfilteredRecord()
    {
        var destination = Destination();
        var config = ImportWriteTests.Config(destination);
        config.SelectedFields = new() { nameof(ThrowingRecord.Selected) };
        var result = await Helper().ProcessBatchDetailedAsync(new object[] { new ThrowingRecord() }, config, null!, default);

        Assert.Equal(ImportOutcome.Failed, result.Outcome);
        Assert.Equal(0, result.WriteAttempts);
        Assert.DoesNotContain(Secret, string.Join(";", result.Errors.Select(error => error.Message)));
        destination.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task ConfiguredMappingWithoutMatchingDestinationDoesNotWriteInput()
    {
        var destination = Destination();
        var config = ImportWriteTests.Config(destination);
        config.Mapping = new EntityDataMap { MappedEntities = new() };
        var result = await Helper().ProcessBatchDetailedAsync(new object[] { Secret }, config, null!, default);

        Assert.Equal(ImportOutcome.Failed, result.Outcome);
        Assert.Equal(0, result.WriteAttempts);
        destination.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task RequiredDefaultWithoutStructureDoesNotWriteInput()
    {
        var destination = Destination();
        var config = ImportWriteTests.Config(destination);
        config.ApplyDefaults = true;
        config.DefaultValues = new() { new DefaultValue { PropertyName = "Id", PropertyValue = "1" } };
        var result = await Helper().ProcessBatchDetailedAsync(new object[] { new { Id = (int?)null } }, config, null!, default);

        Assert.Equal(ImportOutcome.Failed, result.Outcome);
        Assert.Equal(0, result.WriteAttempts);
        destination.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task LaterTransformationFailurePreservesEarlierAcknowledgementWithoutReplay()
    {
        var destination = Destination();
        var config = ImportWriteTests.Config(destination);
        int transformations = 0;
        config.CustomTransformation = row => ++transformations == 2 ? throw new IOException(Secret) : row;
        var result = await Helper().ProcessBatchDetailedAsync(new object[] { 1, 2 }, config, null!, default, 3);

        Assert.Equal(ImportOutcome.Partial, result.Outcome);
        Assert.Equal(1, result.RecordsSucceeded);
        Assert.Equal(1, result.RecordsFailed);
        Assert.Equal(1, result.WriteAttempts);
        Assert.False(result.HasUncertainWrites);
        Assert.Equal(2, transformations);
        destination.Verify(x => x.InsertEntity("target", 1), Times.Once);
        destination.Verify(x => x.InsertEntity("target", 2), Times.Never);
    }

    [Fact]
    public async Task ManagerCannotReportTransformationFailureAsCompleted()
    {
        var destination = Destination();
        var config = ImportWriteTests.Config(destination, Secret);
        config.CustomTransformation = _ => throw new InvalidOperationException(Secret);
        using var manager = new DataImportManager(Mock.Of<IDMEEditor>());
        var result = Assert.IsType<ImportExecutionResult>(await manager.RunImportAsync(config, null!, default));

        Assert.Equal(ImportOutcome.Failed, result.Outcome);
        Assert.Equal(0, manager.GetImportStatus().RecordsProcessed);
        Assert.Equal(0, result.WriteAttempts);
        Assert.Equal(1, result.RecordsTransformationFailed);
        destination.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

    [Theory]
    [InlineData("poco")]
    [InlineData("dictionary")]
    [InlineData("dynamic")]
    [InlineData("data-row")]
    public async Task ConfiguredStaticDefaultIsAppliedWithoutUtilityOrCatalogFallback(string kind)
    {
        var destination = Destination();
        var config = DefaultConfig(destination);
        object row = new DefaultRecord();
        if (kind == "dictionary") row = new Dictionary<string, object>();
        if (kind == "dynamic") row = new ExpandoObject();
        if (kind == "data-row")
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(int));
            row = table.Rows.Add(DBNull.Value);
        }
        var result = await Helper().ProcessBatchDetailedAsync(new[] { row }, config, null!, default);

        Assert.Equal(ImportOutcome.Completed, result.Outcome);
        Assert.Equal(1, result.RecordsSucceeded);
        Assert.Equal(0, result.RecordsTransformationFailed);
        Assert.Equal(kind switch
        {
            "poco" => ((DefaultRecord)row).Id,
            "data-row" => ((DataRow)row).Field<int>("Id"),
            _ => Convert.ToInt32(((IDictionary<string, object>)row)["Id"])
        }, 7);
    }

    [Theory]
    [InlineData("getter")]
    [InlineData("setter")]
    [InlineData("readonly")]
    [InlineData("conversion")]
    [InlineData("unknown-rule")]
    [InlineData("unknown-field")]
    public async Task DefaultStageErrorsDenyWrites(string error)
    {
        var destination = Destination();
        var config = DefaultConfig(destination);
        object row = error switch
        {
            "getter" => new ThrowingRecord(),
            "setter" => new ThrowingDefaultSetter(),
            "readonly" => new { Id = (int?)null },
            _ => new DefaultRecord()
        };
        if (error == "getter")
        {
            config.DefaultValues[0].PropertyName = "Selected";
            config.DestEntityStructure!.Fields[0].FieldName = "Selected";
        }
        if (error == "conversion") config.DefaultValues[0].PropertyValue = Secret;
        if (error == "unknown-rule") config.DefaultValues[0].Rule = ":THIS_RULE_IS_NOT_REGISTERED()";
        if (error == "unknown-field") config.DefaultValues[0].PropertyName = "Missing";
        var transformation = new DataImportTransformationHelper(Mock.Of<IDMEEditor>());
        var detailed = transformation.TransformRecord(row, config, default);
        Assert.False(detailed.Succeeded);
        Assert.Equal(ImportTransformationStage.Defaults, detailed.Stage);
        Assert.Null(detailed.Record);
        Assert.DoesNotContain(Secret, detailed.Message);
        var result = await Helper().ProcessBatchDetailedAsync(new[] { row }, config, null!, default);

        Assert.Equal(1, result.RecordsTransformationFailed);
        Assert.Equal(0, result.WriteAttempts);
        Assert.False(result.HasUncertainWrites);
        destination.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task DisabledDefaultDoesNotRequireMetadataOrApplyValue()
    {
        var destination = Destination();
        var config = DefaultConfig(destination);
        config.DefaultValues[0].IsEnabled = false;
        config.DestEntityStructure = null;
        var row = new DefaultRecord();
        var result = await Helper().ProcessBatchDetailedAsync(new object[] { row }, config, null!, default);
        Assert.Equal(ImportOutcome.Completed, result.Outcome);
        Assert.Null(row.Id);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancellationInsideTransformationIsNotAnUncertainWrite(bool throwAfterCancel)
    {
        using var cancellation = new CancellationTokenSource();
        var destination = Destination();
        var config = ImportWriteTests.Config(destination);
        config.CustomTransformation = row =>
        {
            cancellation.Cancel();
            if (throwAfterCancel) throw new OperationCanceledException(Secret, cancellation.Token);
            return row;
        };
        var result = await Helper().ProcessBatchDetailedAsync(new object[] { 1 }, config, null!, cancellation.Token);
        Assert.Equal(ImportOutcome.Cancelled, result.Outcome);
        Assert.Equal(0, result.WriteAttempts);
        Assert.Equal(0, result.RecordsTransformationFailed);
        Assert.False(result.HasUncertainWrites);
        destination.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public void LegacyPipelineSurfacesSafeStageFailureInsteadOfReturningInput()
    {
        var config = ImportWriteTests.Config(Destination());
        config.CustomTransformation = _ => throw new IOException(Secret);
        var exception = Assert.Throws<ImportTransformationException>(() =>
            new DataImportTransformationHelper(Mock.Of<IDMEEditor>()).ApplyTransformationPipeline(Secret, config));
        Assert.Equal(ImportTransformationStage.Custom, exception.Stage);
        Assert.Equal(nameof(IOException), exception.ExceptionType);
        Assert.Null(exception.InnerException);
        Assert.DoesNotContain(Secret, exception.ToString());
    }

    [Fact]
    public async Task NullTypedOutcomeFailsClosedWithoutLegacyFallback()
    {
        var destination = Destination();
        var transformation = new Mock<IDataImportTransformationHelper>();
        transformation.As<IDataImportTransformationOutcome>().Setup(x => x.TransformRecord(
            It.IsAny<object>(), It.IsAny<DataImportConfiguration>(), It.IsAny<CancellationToken>())).Returns((ImportTransformationResult)null!);
        var helper = new DataImportBatchHelper(Mock.Of<IDMEEditor>(), transformation.Object, Mock.Of<IDataImportProgressHelper>());
        var result = await helper.ProcessBatchDetailedAsync(new object[] { 1 }, ImportWriteTests.Config(destination), null!, default);
        Assert.Equal(1, result.RecordsTransformationFailed);
        Assert.Equal(0, result.WriteAttempts);
        transformation.Verify(x => x.ApplyTransformationPipeline(It.IsAny<object>(), It.IsAny<DataImportConfiguration>()), Times.Never);
    }

    [Fact]
    public async Task LegacyCustomHelperExceptionIsSanitizedAndNotRetried()
    {
        var destination = Destination();
        var transformation = new Mock<IDataImportTransformationHelper>();
        transformation.Setup(x => x.ApplyTransformationPipeline(It.IsAny<object>(), It.IsAny<DataImportConfiguration>())).Throws(new IOException(Secret));
        var helper = new DataImportBatchHelper(Mock.Of<IDMEEditor>(), transformation.Object, Mock.Of<IDataImportProgressHelper>());
        var result = await helper.ProcessBatchDetailedAsync(new object[] { 1 }, ImportWriteTests.Config(destination), null!, default, 3);
        Assert.Equal(1, result.RecordsTransformationFailed);
        Assert.Equal(0, result.WriteAttempts);
        var error = Assert.Single(result.Errors);
        Assert.Null(error.Ex);
        Assert.DoesNotContain(Secret, error.Message);
        transformation.Verify(x => x.ApplyTransformationPipeline(It.IsAny<object>(), It.IsAny<DataImportConfiguration>()), Times.Once);
    }

    [Theory]
    [InlineData("getter")]
    [InlineData("conversion")]
    [InlineData("missing-destination")]
    [InlineData("missing-transform")]
    public async Task StrictMappingFieldFailuresCannotBecomePartialObjects(string error)
    {
        var destination = Destination();
        var config = MappingConfig(destination);
        if (error == "missing-destination") config.Mapping!.MappedEntities[0].FieldMapping[0].ToFieldName = "Missing";
        if (error == "missing-transform")
            MappingManager.SetFieldTransformChain(config.DestDataSourceName, config.DestEntityName, "Id", new MappingFieldTransformChain
            {
                DestinationFieldName = "Id", Steps = new() { new() { Name = "unregistered-transform" } }
            });
        try
        {
            object row = error == "getter" ? new ThrowingIdRecord() : new { Id = error == "conversion" ? Secret : "3" };
            var result = await Helper().ProcessBatchDetailedAsync(new[] { row }, config, null!, default);
            Assert.Equal(ImportOutcome.Failed, result.Outcome);
            Assert.Equal(1, result.RecordsTransformationFailed);
            Assert.Equal(0, result.WriteAttempts);
            Assert.False(result.HasUncertainWrites);
            Assert.DoesNotContain(Secret, string.Join(";", result.Errors.Select(item => item.Message)));
        }
        finally
        {
            MappingManager.SetFieldTransformChain(config.DestDataSourceName, config.DestEntityName, "Id", null!);
        }
        destination.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task StrictMappingUsesConfiguredDefaultsStageOnly()
    {
        var destination = Destination();
        destination.Setup(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>())).Returns(new ErrorsInfo { Flag = Errors.Ok });
        var config = MappingConfig(destination);
        try
        {
            var result = await Helper().ProcessBatchDetailedAsync(new object[] { new { Id = "3" } }, config, null!, default);
            Assert.Equal(ImportOutcome.Completed, result.Outcome);
            Assert.Equal(1, result.RecordsSucceeded);
            Assert.Equal(0, result.RecordsTransformationFailed);
            destination.Verify(x => x.InsertEntity(config.DestEntityName,
                It.Is<object>(record => (int?)record.GetType().GetProperty("Id")!.GetValue(record) == 3)), Times.Once);
        }
        finally { MappingManager.InvalidateMappingCaches(config.DestDataSourceName, config.DestEntityName); }
    }

    private static DataImportConfiguration MappingConfig(Mock<IDataSource> destination)
    {
        var config = ImportWriteTests.Config(destination);
        config.DestEntityName = "Mapped" + Guid.NewGuid().ToString("N");
        config.Mapping = new()
        {
            MappedEntities = new()
            {
                new()
                {
                    EntityName = config.DestEntityName, EntityDataSource = config.DestDataSourceName,
                    SelectedDestFields = new() { new() { FieldName = "Id", Fieldtype = "System.Int32" } },
                    EntityFields = new() { new() { FieldName = "Id", Fieldtype = "System.Int32" } },
                    FieldMapping = new() { new() { FromFieldName = "Id", ToFieldName = "Id" } }
                }
            }
        };
        return config;
    }

    private static DataImportConfiguration DefaultConfig(Mock<IDataSource> destination)
    {
        var config = ImportWriteTests.Config(destination);
        config.ApplyDefaults = true;
        config.DefaultValues = new() { new DefaultValue { PropertyName = "Id", PropertyValue = "7" } };
        config.DestEntityStructure = new() { Fields = new() { new() { FieldName = "Id", Fieldtype = "System.Int32" } } };
        return config;
    }

    public sealed class DefaultRecord { public int? Id { get; set; } }
    public sealed class ThrowingIdRecord { public int? Id => throw new InvalidOperationException(Secret); }
    public sealed class ThrowingDefaultSetter
    {
        public int? Id { get => null; set => throw new InvalidOperationException(Secret); }
    }

    public sealed class ThrowingRecord
    {
        public string Selected => throw new InvalidOperationException(Secret);
        public string PrivateValue => Secret;
    }
}
