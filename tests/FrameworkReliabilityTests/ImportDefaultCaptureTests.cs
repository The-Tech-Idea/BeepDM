using Moq;
using TheTechIdea.Beep;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor.Importing;
using TheTechIdea.Beep.Editor.Importing.Helpers;
using TheTechIdea.Beep.Editor.Importing.Interfaces;
using TheTechIdea.Beep.Report;
using Xunit;
using DefaultValue = TheTechIdea.Beep.ConfigUtil.DefaultValue;

namespace TheTechIdea.Beep.Framework.Tests;

public partial class ImportDefaultCatalogTests
{
    [Fact]
    public async Task ReusedConfigurationRefreshesImplicitCatalogWithoutReplacingCallerList()
    {
        var (editor, catalog, destination, config) = Harness();
        var callerDefaults = config.DefaultValues;
        var definition = new DefaultValue { PropertyName = "Id", PropertyValue = 7 };
        catalog.SetupGet(x => x.DataConnections).Returns(new List<ConnectionProperties>
        { new() { ConnectionName = "target-connection", DatasourceDefaults = new() { definition } } });
        Mock.Get(config.SourceData!).Setup(x => x.GetEntity("source", It.IsAny<List<AppFilter>>()))
            .Returns(() => new object[] { new Dictionary<string, object>() });
        var values = new List<int>();
        destination.Setup(x => x.InsertEntity("target", It.IsAny<object>())).Returns((string _, object row) =>
        {
            values.Add((int)((Dictionary<string, object>)row)["Id"]);
            return new ErrorsInfo { Flag = Errors.Ok };
        });
        using var manager = new DataImportManager(editor.Object);
        Assert.Equal(Errors.Ok, (await manager.RunImportAsync(config, null!, default)).Flag);
        Assert.Same(callerDefaults, config.DefaultValues);
        Assert.Empty(callerDefaults);
        definition.PropertyValue = 9;
        Assert.Equal(Errors.Ok, (await manager.RunImportAsync(config, null!, default)).Flag);
        Assert.Equal(new[] { 7, 9 }, values);
    }

    [Fact]
    public async Task ExplicitDefaultsAndBindingsAreOwnedBeforeSourceRead()
    {
        var (editor, _, destination, config) = Harness();
        var definition = new DefaultValue { PropertyName = "Id", PropertyValue = 7 };
        var callerDefaults = config.DefaultValues = new() { definition };
        Mock.Get(config.SourceData!).Setup(x => x.GetEntity("source", It.IsAny<List<AppFilter>>())).Returns(() =>
        {
            definition.PropertyValue = 9;
            config.ApplyDefaults = false;
            config.DestEntityName = "changed-target";
            return new object[] { new Dictionary<string, object>() };
        });
        object? written = null;
        destination.Setup(x => x.InsertEntity("target", It.IsAny<object>())).Returns((string _, object row) =>
        { written = row; return new ErrorsInfo { Flag = Errors.Ok }; });
        using var manager = new DataImportManager(editor.Object);
        Assert.Equal(Errors.Ok, (await manager.RunImportAsync(config, null!, default)).Flag);
        Assert.Equal(7, Assert.IsType<Dictionary<string, object>>(written)["Id"]);
        Assert.Same(callerDefaults, config.DefaultValues);
        Assert.Same(definition, callerDefaults[0]);
        destination.Verify(x => x.InsertEntity("changed-target", It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task DirectBatchCapturesBeforeEnumerationWithoutMutatingCallerDefaults()
    {
        var (editor, catalog, destination, config) = Harness();
        var callerDefaults = config.DefaultValues;
        var definition = new DefaultValue { PropertyName = "Id", PropertyValue = 7 };
        catalog.SetupGet(x => x.DataConnections).Returns(new List<ConnectionProperties>
        { new() { ConnectionName = "target-connection", DatasourceDefaults = new() { definition } } });
        IEnumerable<object> Rows()
        {
            definition.PropertyValue = 9;
            config.ApplyDefaults = false;
            yield return new Dictionary<string, object>();
        }
        object? written = null;
        destination.Setup(x => x.InsertEntity("target", It.IsAny<object>())).Returns((string _, object row) =>
        { written = row; return new ErrorsInfo { Flag = Errors.Ok }; });
        var batch = new DataImportBatchHelper(editor.Object, new DataImportTransformationHelper(editor.Object), Mock.Of<IDataImportProgressHelper>());
        Assert.Equal(Errors.Ok, (await batch.ProcessBatchDetailedAsync(Rows(), config, null!, default)).Flag);
        Assert.Equal(7, Assert.IsType<Dictionary<string, object>>(written)["Id"]);
        Assert.Same(callerDefaults, config.DefaultValues);
        Assert.Empty(callerDefaults);
    }

    [Fact]
    public async Task ByteLiteralIsIndependentForCallerAndEveryRow()
    {
        var (editor, _, destination, config) = Harness();
        var literal = new byte[] { 1, 2 };
        config.DefaultValues = new() { new() { PropertyName = "Id", PropertyValue = literal } };
        var observed = new List<byte>();
        destination.Setup(x => x.InsertEntity("target", It.IsAny<object>())).Returns((string _, object row) =>
        {
            var bytes = Assert.IsType<byte[]>(((Dictionary<string, object>)row)["Id"]);
            observed.Add(bytes[0]); bytes[0] = 99;
            return new ErrorsInfo { Flag = Errors.Ok };
        });
        var batch = new DataImportBatchHelper(editor.Object, new DataImportTransformationHelper(editor.Object), Mock.Of<IDataImportProgressHelper>());
        Assert.Equal(Errors.Ok, (await batch.ProcessBatchDetailedAsync(new object[]
            { new Dictionary<string, object>(), new Dictionary<string, object>() }, config, null!, default)).Flag);
        Assert.Equal(new byte[] { 1, 1 }, observed);
        Assert.Equal(new byte[] { 1, 2 }, literal);
    }

    private readonly record struct MutableLiteral(List<int> Values);

    public static IEnumerable<object[]> ClosedLiterals()
    {
        object[] values = { "text", 'x', true, (byte)1, (sbyte)-1, (short)-2, (ushort)2, 3, 4U, 5L, 6UL,
            1.25m, 2.5d, 3.5f, Guid.NewGuid(), new DateTime(2026, 10, 3, 0, 0, 0, DateTimeKind.Utc),
            new DateTimeOffset(2026, 10, 3, 0, 0, 0, TimeSpan.FromHours(3)), TimeSpan.FromSeconds(1),
            new DateOnly(2026, 10, 3), new TimeOnly(12, 30), new byte[] { 1, 2 } };
        return values.Select(value => new[] { value });
    }

    [Theory]
    [MemberData(nameof(ClosedLiterals))]
    public async Task ClosedLiteralsPreserveTheirActualTypeAndValue(object value)
    {
        var (editor, _, destination, config) = Harness();
        config.DefaultValues = new() { new() { PropertyName = "Id", PropertyValue = value } };
        object? written = null;
        destination.Setup(x => x.InsertEntity("target", It.IsAny<object>())).Returns((string _, object row) =>
        { written = ((Dictionary<string, object>)row)["Id"]; return new ErrorsInfo { Flag = Errors.Ok }; });
        var batch = new DataImportBatchHelper(editor.Object, new DataImportTransformationHelper(editor.Object), Mock.Of<IDataImportProgressHelper>());
        Assert.Equal(Errors.Ok, (await batch.ProcessBatchDetailedAsync(new object[]
            { new Dictionary<string, object>() }, config, null!, default)).Flag);
        Assert.Equal(value.GetType(), written!.GetType());
        if (value is byte[] bytes) { Assert.Equal(bytes, Assert.IsType<byte[]>(written)); Assert.NotSame(bytes, written); }
        else Assert.Equal(value, written);
    }

    [Fact]
    public async Task RuleIgnoresUnusedObjectFallbackWithoutRetainingIt()
    {
        var (editor, _, destination, config) = Harness();
        config.DefaultValues = new() { new() { PropertyName = "Id", Rule = ":ADD(1,2)", PropertyValue = new object() } };
        object? written = null;
        destination.Setup(x => x.InsertEntity("target", It.IsAny<object>())).Returns((string _, object row) =>
        { written = ((Dictionary<string, object>)row)["Id"]; return new ErrorsInfo { Flag = Errors.Ok }; });
        var batch = new DataImportBatchHelper(editor.Object, new DataImportTransformationHelper(editor.Object), Mock.Of<IDataImportProgressHelper>());
        Assert.Equal(Errors.Ok, (await batch.ProcessBatchDetailedAsync(new object[]
            { new Dictionary<string, object>() }, config, null!, default)).Flag);
        Assert.Equal(3d, written);
    }

    [Fact]
    public async Task DisabledDefaultsDoNotRequireCatalogOrLiteralAdmission()
    {
        var (editor, catalog, _, config) = Harness();
        config.ApplyDefaults = false;
        config.DefaultValues = new() { new() { PropertyName = "Id", PropertyValue = new MutableLiteral(new() { 1 }) } };
        catalog.SetupGet(x => x.DataConnections).Throws(new IOException("sentinel-catalog-secret"));
        using var manager = new DataImportManager(editor.Object);
        Assert.Equal(Errors.Ok, (await manager.RunImportAsync(config, null!, default)).Flag);
        catalog.VerifyGet(x => x.DataConnections, Times.Never);
        Assert.Single(config.DefaultValues);
    }

    [Fact]
    public async Task AggregateLiteralBudgetFailsBeforeProviderWork()
    {
        var (editor, _, destination, config) = Harness();
        config.DefaultValues = Enumerable.Range(0, 17).Select(index => new DefaultValue
            { PropertyName = $"Field{index}", PropertyValue = new byte[1048576] }).ToList();
        using var manager = new DataImportManager(editor.Object);
        var result = Assert.IsType<ImportExecutionResult>(await manager.RunImportAsync(config, null!, default));
        Assert.True(result.TransformationAdmissionFailed);
        Assert.Equal(0, result.RecordsAttempted);
        Assert.Equal(0, result.WriteAttempts);
        destination.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

    [Theory]
    [InlineData("struct")]
    [InlineData("nan")]
    [InlineData("infinity")]
    [InlineData("large-bytes")]
    [InlineData("large-string")]
    public async Task UnsupportedOrUnboundedLiteralDeniesAdmissionBeforeRows(string defect)
    {
        var (editor, _, destination, config) = Harness();
        object value = defect switch
        {
            "struct" => new MutableLiteral(new() { 1 }),
            "nan" => double.NaN,
            "infinity" => float.PositiveInfinity,
            "large-bytes" => new byte[1048577],
            _ => new string('x', 1048577)
        };
        config.DefaultValues = new() { new() { PropertyName = "Id", PropertyValue = value } };
        using var manager = new DataImportManager(editor.Object);
        var result = Assert.IsType<ImportExecutionResult>(await manager.RunImportAsync(config, null!, default));
        Assert.Equal(ImportOutcome.Failed, result.Outcome);
        Assert.True(result.TransformationAdmissionFailed);
        Assert.Equal(0, result.RecordsAttempted);
        Assert.Equal(0, result.WriteAttempts);
        Assert.Null(result.Ex);
        Mock.Get(config.SourceData!).Verify(x => x.GetEntity(It.IsAny<string>(), It.IsAny<List<AppFilter>>()), Times.Never);
        destination.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }
}
