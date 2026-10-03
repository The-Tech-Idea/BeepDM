using Moq;
using TheTechIdea.Beep;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.BeepSync;
using TheTechIdea.Beep.Editor.BeepSync.Helpers;
using TheTechIdea.Beep.Editor.Importing;
using TheTechIdea.Beep.Editor.Importing.Helpers;
using TheTechIdea.Beep.Editor.Schema;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Tools;
using Xunit;

namespace TheTechIdea.Beep.Framework.Tests;

public partial class SyncOutcomeTests
{
    private (BeepSyncManager Manager, DataSyncSchema Schema, Mock<IDataSource> Destination,
        Mock<IDataSource> Source, EntityStructure SourceMetadata, EntityStructure DestinationMetadata) MappedHarness()
    {
        var (manager, schema, destination, source) = Harness();
        var editor = Mock.Get(manager.Editor);
        editor.SetupGet(x => x.classCreator).Returns(new ClassCreator(editor.Object));
        schema.RetryPolicy = new RetryPolicy { MaxAttempts = 3, BaseDelayMs = 1, CheckpointEnabled = false };
        schema.SourceSyncDataField = "SourceId";
        schema.DestinationSyncDataField = "TargetId";
        schema.MappedFields.Add(new FieldSyncData
        {
            SourceField = "SourceName", DestinationField = "TargetName",
            SourceFieldType = "System.Boolean", DestinationFieldType = "System.Boolean"
        });
        var from = MappingMetadata("source", "SourceId", "SourceName");
        var to = MappingMetadata("target", "TargetId", "TargetName");
        source.Setup(x => x.GetEntityStructure("source", false)).Returns(from);
        destination.Setup(x => x.GetEntityStructure("target", false)).Returns(to);
        source.Setup(x => x.GetEntity("source", It.IsAny<List<AppFilter>>()))
            .Returns(new object[] { new { SourceId = 7, SourceName = "forward" } });
        destination.Setup(x => x.InsertEntity("target", It.IsAny<object>())).Returns(new ErrorsInfo { Flag = Errors.Ok });
        return (manager, schema, destination, source, from, to);
    }

    private static EntityStructure MappingMetadata(string entity, string key, string text) => new()
    {
        EntityName = entity,
        Fields = new()
        {
            new() { FieldName = key, Fieldtype = "System.Int32", IsKey = true, IsRequired = true },
            new() { FieldName = text, Fieldtype = "System.String", IsRequired = true },
            new() { FieldName = "UpdatedAt", Fieldtype = "System.DateTime" }
        }
    };

    [Fact]
    public async Task MappedRename_UsesProviderTypesAndActualGeneratedPayload()
    {
        var (manager, schema, destination, _, _, _) = MappedHarness();
        var result = await manager.SyncDataAsync(schema);
        Assert.True(result.Flag == Errors.Ok, result.Message);
        destination.Verify(x => x.InsertEntity("target", It.Is<object>(row =>
            (int?)row.GetType().GetProperty("TargetId")!.GetValue(row) == 7 &&
            (string?)row.GetType().GetProperty("TargetName")!.GetValue(row) == "forward" &&
            row.GetType().GetProperty("SourceId") == null)), Times.Once);
    }

    [Fact]
    public async Task MappedBidirectional_UsesActualReversePayloadAndDeduplicatesKeyPair()
    {
        var (manager, schema, destination, source, _, _) = MappedHarness();
        schema.SyncDirection = "Bidirectional";
        schema.MappedFields.Add(new FieldSyncData { SourceField = "SourceId", DestinationField = "TargetId" });
        destination.Setup(x => x.GetEntity("target", It.IsAny<List<AppFilter>>()))
            .Returns(new object[] { new { TargetId = 9, TargetName = "reverse" } });
        source.Setup(x => x.InsertEntity("source", It.IsAny<object>())).Returns(new ErrorsInfo { Flag = Errors.Ok });
        Assert.Single(SyncSchemaTranslator.ToReverseImportConfiguration(schema).Mapping!.MappedEntities[0].FieldMapping,
            f => f.FromFieldName == "TargetId" && f.ToFieldName == "SourceId");
        var result = await manager.SyncDataAsync(schema);
        Assert.True(result.Flag == Errors.Ok, result.Message);
        source.Verify(x => x.InsertEntity("source", It.Is<object>(row =>
            (int?)row.GetType().GetProperty("SourceId")!.GetValue(row) == 9 &&
            (string?)row.GetType().GetProperty("SourceName")!.GetValue(row) == "reverse" &&
            row.GetType().GetProperty("TargetId") == null)), Times.Once);
        destination.Verify(x => x.InsertEntity("target", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public void MappedKey_HalfMatchCannotSuppressActualPair()
    {
        var (_, schema, _, _, _, _) = MappedHarness();
        schema.MappedFields.Add(new FieldSyncData { SourceField = "SourceId", DestinationField = "OtherId" });
        Assert.Single(SyncSchemaTranslator.ToImportConfiguration(schema).Mapping!.MappedEntities[0].FieldMapping,
            f => f.FromFieldName == "SourceId" && f.ToFieldName == "TargetId");
    }

    [Theory]
    [InlineData("unknown-source")]
    [InlineData("unknown-target")]
    [InlineData("ambiguous-source")]
    [InlineData("ambiguous-target")]
    [InlineData("duplicate-assignment")]
    [InlineData("missing-type")]
    [InlineData("required-target")]
    [InlineData("wrong-entity")]
    [InlineData("null-field")]
    public async Task MappedAdmission_InvalidMetadataOrPairsCannotWriteEitherDirection(string defect)
    {
        var (manager, schema, destination, source, from, to) = MappedHarness();
        schema.SyncDirection = "Bidirectional";
        switch (defect)
        {
            case "unknown-source": schema.MappedFields[0].SourceField = "Missing"; break;
            case "unknown-target": schema.MappedFields[0].DestinationField = "Missing"; break;
            case "ambiguous-source": from.Fields.Add(new() { FieldName = "sourcename", Fieldtype = "System.String" }); break;
            case "ambiguous-target": to.Fields.Add(new() { FieldName = "targetname", Fieldtype = "System.String" }); break;
            case "duplicate-assignment": schema.MappedFields.Add(new() { SourceField = "UpdatedAt", DestinationField = "targetname" }); break;
            case "missing-type": to.Fields[1].Fieldtype = ""; break;
            case "required-target": to.Fields.Add(new() { FieldName = "Required", Fieldtype = "System.String", IsRequired = true }); break;
            case "wrong-entity": to.EntityName = "foreign"; break;
            case "null-field": to.Fields.Add(null!); break;
        }
        var previous = schema.WatermarkPolicy.LastWatermarkValue;
        var result = await manager.SyncDataAsync(schema);
        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Equal(previous, schema.WatermarkPolicy.LastWatermarkValue);
        destination.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
        source.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
        destination.Verify(x => x.CreateEntityAs(It.IsAny<EntityStructure>()), Times.Never);
    }

    [Fact]
    public async Task MappedAdmission_ReverseRequiredCoverageIsCheckedBeforeForwardWrite()
    {
        var (manager, schema, destination, source, from, _) = MappedHarness();
        schema.SyncDirection = "Bidirectional";
        from.Fields.Add(new() { FieldName = "ReverseRequired", Fieldtype = "System.String", IsRequired = true });
        Assert.Equal(Errors.Failed, (await manager.SyncDataAsync(schema)).Flag);
        destination.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
        source.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task MappedAdmission_InvalidReverseGeneratedShapeCannotWriteForward()
    {
        var (manager, schema, destination, source, from, _) = MappedHarness();
        schema.SyncDirection = "Bidirectional";
        schema.SourceEntityName = "source-invalid";
        from.EntityName = schema.SourceEntityName;
        source.Setup(x => x.GetEntityStructure("source-invalid", false)).Returns(from);
        source.Setup(x => x.CheckEntityExist("source-invalid")).Returns(true);
        source.Setup(x => x.GetEntity("source-invalid", It.IsAny<List<AppFilter>>()))
            .Returns(new object[] { new { SourceId = 7, SourceName = "forward" } });
        destination.Setup(x => x.GetEntity("target", It.IsAny<List<AppFilter>>()))
            .Returns(new object[] { new { TargetId = 9, TargetName = "reverse" } });
        var result = await manager.SyncDataAsync(schema);
        Assert.Equal(Errors.Failed, result.Flag);
        destination.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task MappedAdmission_CapturedMetadataAndPairsSurviveLaterCallerEdits()
    {
        var (manager, schema, destination, source, from, to) = MappedHarness();
        source.Setup(x => x.GetEntity("source", It.IsAny<List<AppFilter>>())).Returns(() =>
        {
            from.Fields[1].FieldName = "Mutated";
            to.Fields[0].Fieldtype = "System.Boolean";
            to.Fields[1].FieldName = "Mutated";
            schema.MappedFields[0].DestinationField = "Mutated";
            return new object[] { new { SourceId = 7, SourceName = "captured" } };
        });
        var result = await manager.SyncDataAsync(schema);
        Assert.True(result.Flag == Errors.Ok, result.Message);
        destination.Verify(x => x.InsertEntity("target", It.Is<object>(row =>
            (int?)row.GetType().GetProperty("TargetId")!.GetValue(row) == 7 &&
            (string?)row.GetType().GetProperty("TargetName")!.GetValue(row) == "captured")), Times.Once);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MappedAdmission_MissingDestinationNeverInfersMappedDdl(bool allowCreation)
    {
        var (manager, schema, destination, _, _, _) = MappedHarness();
        schema.CreateDestinationIfNotExists = allowCreation;
        destination.Setup(x => x.GetEntitesList()).Returns(new List<string>());
        destination.Setup(x => x.CheckEntityExist("target")).Returns(false);
        Assert.Equal(Errors.Failed, (await manager.SyncDataAsync(schema)).Flag);
        destination.Verify(x => x.CreateEntityAs(It.IsAny<EntityStructure>()), Times.Never);
        destination.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MappedAdmission_ProviderExistenceDisagreementCannotAdmitForward(bool reverseMissing)
    {
        var (manager, schema, destination, source, _, _) = MappedHarness();
        schema.SyncDirection = "Bidirectional";
        schema.CreateDestinationIfNotExists = true;
        if (reverseMissing) source.Setup(x => x.CheckEntityExist("source")).Returns(false);
        else destination.Setup(x => x.CheckEntityExist("target")).Returns(false);
        Assert.Equal(Errors.Failed, (await manager.SyncDataAsync(schema)).Flag);
        destination.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
        destination.Verify(x => x.CreateEntityAs(It.IsAny<EntityStructure>()), Times.Never);
    }

    [Fact]
    public async Task MappedAdmission_CancellationCannotFallThroughSchemaPreflight()
    {
        var (manager, schema, destination, _, _, _) = MappedHarness();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.Equal(Errors.Failed, (await manager.SyncDataAsync(schema, cancellation.Token)).Flag);
        Assert.Equal("Cancelled", schema.SyncStatus);
        destination.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
        destination.Verify(x => x.CreateEntityAs(It.IsAny<EntityStructure>()), Times.Never);
    }

    [Fact]
    public void MappedBinding_BindsBothFieldListsAndIgnoresDeclaredTypeStrings()
    {
        var (manager, schema, _, _, from, to) = MappedHarness();
        var config = SyncSchemaTranslator.ToImportConfiguration(schema);
        SyncSchemaTranslator.BindEntityMetadata(config, from, to);
        var detail = Assert.Single(config.Mapping!.MappedEntities);
        Assert.NotSame(to.Fields, detail.SelectedDestFields);
        Assert.Same(config.DestEntityStructure!.Fields, detail.SelectedDestFields);
        Assert.Same(detail.EntityFields, detail.SelectedDestFields);
        Assert.Equal("System.String", detail.FieldMapping[0].FromFieldType);
        Assert.Equal("System.String", detail.FieldMapping[0].ToFieldType);
        Assert.True(config.RequireBoundMappingMetadata);
        Assert.Equal(Errors.Ok, new DataImportValidationHelper(manager.Editor).ValidateImportConfiguration(config).Flag);
        detail.SelectedDestFields = new();
        Assert.Equal(Errors.Failed, new DataImportValidationHelper(manager.Editor).ValidateImportConfiguration(config).Flag);
    }

    [Fact]
    public async Task MappedPreflight_ThrowingProviderFailsWithSafeDiagnostic()
    {
        var (manager, schema, destination, source, _, _) = MappedHarness();
        var mapping = SyncSchemaTranslator.ToImportConfiguration(schema).Mapping;
        source.Setup(x => x.GetEntityStructure("source", false)).Throws(new InvalidOperationException("secret-row-value"));
        var result = await SyncSchemaPreflight.RunPreflightAsync(manager.Editor, new SchemaRequest
        {
            SourceDataSourceName = schema.SourceDataSourceName, SourceEntityName = schema.SourceEntityName,
            DestinationDataSourceName = schema.DestinationDataSourceName, DestinationEntityName = schema.DestinationEntityName,
            Mapping = mapping
        });
        Assert.Equal(Errors.Failed, result.Status.Flag);
        Assert.DoesNotContain("secret-row-value", result.Status.Message);
        destination.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task MappedAdmission_PreflightObserverExceptionCannotFallThroughToImport()
    {
        var (manager, schema, destination, _, _, _) = MappedHarness();
        Mock.Get(manager.Editor).Setup(x => x.AddLogMessage("BeepSync", It.IsAny<string>(),
            It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<Errors>()))
            .Throws(new InvalidOperationException("secret-observer-value"));
        var result = await manager.SyncDataAsync(schema);
        Assert.Equal(Errors.Failed, result.Flag);
        Assert.DoesNotContain("secret-observer-value", result.Message);
        destination.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task MappedImport_DestinationDisappearingAfterBindingCannotAdmitDdlOrWrite()
    {
        var (manager, schema, destination, _, from, to) = MappedHarness();
        var config = SyncSchemaTranslator.ToImportConfiguration(schema);
        SyncSchemaTranslator.BindEntityMetadata(config, from, to);
        config.CreateDestinationIfNotExists = true;
        destination.Setup(x => x.CheckEntityExist("target")).Returns(false);
        using var import = new DataImportManager(manager.Editor);
        var result = Assert.IsType<ImportExecutionResult>(await import.RunImportAsync(config, null!, default));
        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Equal(0, result.WriteAttempts);
        Assert.False(result.HasUncertainWrites);
        destination.Verify(x => x.CreateEntityAs(It.IsAny<EntityStructure>()), Times.Never);
        destination.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task MappedAdmission_FieldNamesAreCanonicalizedFromProviderMetadata()
    {
        var (manager, schema, destination, _, _, _) = MappedHarness();
        schema.MappedFields[0].SourceField = "sourcename";
        schema.MappedFields[0].DestinationField = "targetname";
        schema.SourceSyncDataField = "sourceid";
        schema.DestinationSyncDataField = "targetid";
        var result = await manager.SyncDataAsync(schema);
        Assert.True(result.Flag == Errors.Ok, result.Message);
        destination.Verify(x => x.InsertEntity("target", It.Is<object>(row =>
            (int?)row.GetType().GetProperty("TargetId")!.GetValue(row) == 7 &&
            (string?)row.GetType().GetProperty("TargetName")!.GetValue(row) == "forward")), Times.Once);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MappedTranslation_IncompletePairsRejectInsteadOfBeingSilentlySkipped(bool reverse)
    {
        var (_, schema, _, _, _, _) = MappedHarness();
        schema.MappedFields[0].SourceField = "";
        Assert.Throws<InvalidOperationException>(() => reverse
            ? SyncSchemaTranslator.ToReverseImportConfiguration(schema)
            : SyncSchemaTranslator.ToImportConfiguration(schema));
    }
}
