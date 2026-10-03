using Moq;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Defaults;
using TheTechIdea.Beep.Editor.Defaults.Resolvers;
using TheTechIdea.Beep.Editor.Importing;
using TheTechIdea.Beep.Editor.Importing.Helpers;
using TheTechIdea.Beep.Editor.Importing.Interfaces;
using Xunit;

namespace TheTechIdea.Beep.Framework.Tests;

public partial class ImportRequiredResolverTests
{
    private const string Secret = "sentinel-required-resolver-secret";

    private static DataImportConfiguration Config(Mock<IDataSource> destination, string rule)
    {
        destination.Setup(x => x.InsertEntity("target", It.IsAny<object>())).Returns(new ErrorsInfo { Flag = Errors.Ok });
        var config = ImportWriteTests.Config(destination);
        config.ApplyDefaults = true;
        config.DefaultValues = new() { new() { PropertyName = "Value", Rule = rule, IsEnabled = true } };
        config.DestEntityStructure = new EntityStructure
        { EntityName = "target", Fields = new() { new() { FieldName = "Value", Fieldtype = "System.Object" } } };
        return config;
    }

    private static Task<ImportExecutionResult> Run(IDMEEditor editor, DataImportConfiguration config, params object[] rows) =>
        new DataImportBatchHelper(editor, new DataImportTransformationHelper(editor), Mock.Of<IDataImportProgressHelper>())
            .ProcessBatchDetailedAsync(rows, config, null!, default);

    [Theory]
    [InlineData(":ADDDAYS(TODAY,not-a-number)")]
    [InlineData(":ADDHOURS(TODAY,not-a-number)")]
    [InlineData(":ADDMINUTES(TODAY,not-a-number)")]
    [InlineData(":ADDMONTHS(TODAY,not-a-number)")]
    [InlineData(":ADDYEARS(TODAY,not-a-number)")]
    [InlineData(":ADDDAYS(not-a-date,2)")]
    [InlineData(":FORMAT(TODAY)")]
    [InlineData(":ADD(1,not-a-number)")]
    [InlineData(":DIV(1,0)")]
    [InlineData(":RANDOM(not-a-number)")]
    [InlineData(":ROUND(1,not-a-number)")]
    [InlineData(":EXPRESSION(1+not-a-number)")]
    [InlineData(":EXPRESSION(1/0)")]
    [InlineData(":ADDDAYS(TODAY,2)trailing")]
    [InlineData(":ADDDAYS(TODAY,2")]
    [InlineData(":UNKNOWN()")]
    public async Task MalformedRequiredRulesCannotWriteNonNullFallback(string rule)
    {
        var editor = Mock.Of<IDMEEditor>(); var destination = new Mock<IDataSource>();
        var result = await Run(editor, Config(destination, rule), new Dictionary<string, object>());
        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Equal(1, result.RecordsTransformationFailed);
        Assert.Equal(0, result.WriteAttempts);
        Assert.False(result.HasUncertainWrites);
        destination.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

    [Theory]
    [InlineData(":ADD(2,3)", 5d)]
    [InlineData(":ADD.'2'.'3'", 5d)]
    [InlineData(":EXPRESSION(2+3)", 5d)]
    [InlineData(":SUB(2,2)", 0d)]
    [InlineData(":ROUND(1.25,1)", 1.3d)]
    public async Task RequiredExpressionIsNormalizedOnceAndWritesResolvedNumber(string rule, double expected)
    {
        var destination = new Mock<IDataSource>(); var row = new Dictionary<string, object>();
        var result = await Run(Mock.Of<IDMEEditor>(), Config(destination, rule), row);
        Assert.Equal(Errors.Ok, result.Flag);
        Assert.Equal(expected, row["Value"]);
    }

    [Fact]
    public async Task ArithmeticLiteralsWorkWithPocoRowContext()
    {
        var destination = new Mock<IDataSource>(); var row = new ValueRow();
        var result = await Run(Mock.Of<IDMEEditor>(), Config(destination, ":EXPRESSION(2+3)"), row);
        Assert.Equal(Errors.Ok, result.Flag); Assert.Equal(5d, row.Value);
    }

    private sealed class ValueRow { public object? Value { get; set; } }

    [Fact]
    public async Task ValidDateAndPlainLiteralRetainTheirMeaning()
    {
        var destination = new Mock<IDataSource>(); var editor = Mock.Of<IDMEEditor>();
        var row = new Dictionary<string, object>();
        Assert.Equal(Errors.Ok, (await Run(editor, Config(destination, ":ADDDAYS('2026-01-01',2)"), row)).Flag);
        Assert.Equal(new DateTime(2026, 1, 3), row["Value"]);
        row = new Dictionary<string, object>();
        Assert.Equal(Errors.Ok, (await Run(editor, Config(destination, "literal text"), row)).Flag);
        Assert.Equal("literal text", row["Value"]);
    }

    [Fact]
    public async Task BuiltInPropertyDefaultReadsEachActualRowWithoutCacheReuse()
    {
        var destination = new Mock<IDataSource>(); var first = new Dictionary<string, object> { ["Input"] = 1 };
        var second = new Dictionary<string, object> { ["Input"] = 2 };
        var result = await Run(Mock.Of<IDMEEditor>(), Config(destination, ":PROPERTY(Input)"), first, second);
        Assert.Equal(Errors.Ok, result.Flag);
        Assert.Equal(2, result.RecordsSucceeded);
        Assert.Equal(1, first["Value"]); Assert.Equal(2, second["Value"]);
    }

    [Fact]
    public async Task FailedDefaultAfterAcknowledgementNeverReplaysEarlierRow()
    {
        var editor = Mock.Of<IDMEEditor>(); var destination = new Mock<IDataSource>();
        var first = new Dictionary<string, object> { ["Value"] = DateTime.Today };
        var result = await Run(editor, Config(destination, ":ADDDAYS(TODAY,bad)"), first, new Dictionary<string, object>());
        Assert.Equal(ImportOutcome.Partial, result.Outcome);
        Assert.Equal(1, result.RecordsSucceeded); Assert.Equal(1, result.RecordsTransformationFailed);
        Assert.Equal(1, result.WriteAttempts);
        destination.Verify(x => x.InsertEntity("target", first), Times.Once);
    }

    [Fact]
    public async Task CancellationDuringResolverAdmitsNoProviderCall()
    {
        using var cancellation = new CancellationTokenSource();
        var editor = Mock.Of<IDMEEditor>();
        DefaultsManager.RegisterCustomResolver(editor, new ProbeResolver(editor, _ => { cancellation.Cancel(); return 17; }));
        var destination = new Mock<IDataSource>(); var config = Config(destination, ":PROBE()");
        var batch = new DataImportBatchHelper(editor, new DataImportTransformationHelper(editor), Mock.Of<IDataImportProgressHelper>());
        var result = await batch.ProcessBatchDetailedAsync(new object[] { new Dictionary<string, object>() }, config, null!, cancellation.Token);
        Assert.Equal(ImportOutcome.Cancelled, result.Outcome);
        Assert.Equal(0, result.WriteAttempts); Assert.False(result.HasUncertainWrites);
    }

    [Fact]
    public async Task ConcurrentEditorsKeepTheirRegisteredResolverAndWarningScope()
    {
        using var barrier = new Barrier(2);
        async Task Check(bool warning)
        {
            var editor = Mock.Of<IDMEEditor>();
            DefaultsManager.RegisterCustomResolver(editor, new ProbeResolver(editor, _ =>
            { if (!barrier.SignalAndWait(TimeSpan.FromSeconds(10))) throw new TimeoutException(); return warning ? 1 : 2; }, warning));
            var destination = new Mock<IDataSource>(); var row = new Dictionary<string, object>();
            var result = await Run(editor, Config(destination, ":PROBE()"), row);
            Assert.Equal(warning ? Errors.Failed : Errors.Ok, result.Flag);
            if (!warning) Assert.Equal(2, row["Value"]);
        }
        await Task.WhenAll(Task.Run(() => Check(true)), Task.Run(() => Check(false)));
    }

    [Fact]
    public async Task EditorRegistrationSurvivesOtherRuntimeInitialization()
    {
        var first = Mock.Of<IDMEEditor>(); var second = Mock.Of<IDMEEditor>();
        DefaultsManager.RegisterCustomResolver(first, new ProbeResolver(first, _ => 17));
        DefaultsManager.Initialize(second);
        var destination = new Mock<IDataSource>(); var row = new Dictionary<string, object>();
        var result = await Run(first, Config(destination, ":PROBE()"), row);
        Assert.Equal(Errors.Ok, result.Flag);
        Assert.Equal(17, row["Value"]);
    }

    [Fact]
    public void LegacyResolvePreservesExpressionPrefixAndEditorRegistration()
    {
        var first = Mock.Of<IDMEEditor>(); var second = Mock.Of<IDMEEditor>();
        DefaultsManager.RegisterCustomResolver(first, new ProbeResolver(first, _ => 17));
        DefaultsManager.RegisterCustomResolver(second, new ProbeResolver(second, _ => 23));
        Assert.Equal(17, DefaultsManager.Resolve(first, ":PROBE()"));
        Assert.Equal(23, DefaultsManager.Resolve(second, ":PROBE()"));
        Assert.Equal("literal", DefaultsManager.Resolve(first, "literal"));
        Assert.Equal(5d, DefaultsManager.Resolve(first, ":ADD(2,3)"));
    }

    [Fact]
    public async Task RequiredResolutionDoesNotUseLegacyMetadataOnlyValueCache()
    {
        var editor = Mock.Of<IDMEEditor>();
        DefaultsManager.RegisterCustomResolver(editor, new ProbeResolver(editor,
            args => ((Dictionary<string, object>)args.ReturnData)["Input"]));
        var first = new Dictionary<string, object> { ["Input"] = 1 };
        Assert.Equal(1, DefaultsManager.ResolverManager.ResolveValue(":PROBE()", new PassedArgs { ReturnData = first }));
        var destination = new Mock<IDataSource>(); var second = new Dictionary<string, object> { ["Input"] = 2 };
        var result = await Run(editor, Config(destination, ":PROBE()"), second);
        Assert.Equal(Errors.Ok, result.Flag);
        Assert.Equal(2, second["Value"]);
    }

    [Fact]
    public async Task ResolverWarningFallbackIsDeniedWithoutRawDiagnostics()
    {
        var editor = new Mock<IDMEEditor>();
        DefaultsManager.RegisterCustomResolver(editor.Object, new ProbeResolver(editor.Object, _ => 17, warn: true));
        var destination = new Mock<IDataSource>();
        var result = await Run(editor.Object, Config(destination, ":PROBE()"), new Dictionary<string, object>());
        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Equal(0, result.WriteAttempts);
        editor.Verify(x => x.AddLogMessage(It.IsAny<string>(), It.Is<string>(s => s.Contains(Secret)),
            It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<Errors>()), Times.Never);
    }

    [Fact]
    public async Task ResolverExceptionIsDeniedWithoutRawDiagnostics()
    {
        var editor = new Mock<IDMEEditor>();
        DefaultsManager.RegisterCustomResolver(editor.Object, new ProbeResolver(editor.Object, _ => throw new IOException(Secret)));
        var destination = new Mock<IDataSource>();
        var result = await Run(editor.Object, Config(destination, ":PROBE()"), new Dictionary<string, object>());
        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Equal(0, result.WriteAttempts);
        editor.Verify(x => x.AddLogMessage(It.IsAny<string>(), It.Is<string>(s => s.Contains(Secret)),
            It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<Errors>()), Times.Never);
    }

    private sealed class ProbeResolver(IDMEEditor editor, Func<IPassedArgs, object> resolve, bool warn = false)
        : BaseDefaultValueResolver(editor)
    {
        public override string ResolverName => "Probe";
        public override IEnumerable<string> SupportedRuleTypes => new[] { "PROBE" };
        public override bool SupportsCaching => true;
        public override bool CanHandle(string rule) => rule.StartsWith("PROBE(", StringComparison.Ordinal);
        public override IEnumerable<string> GetExamples() => Array.Empty<string>();
        public override object ResolveValue(string rule, IPassedArgs parameters)
        {
            if (warn) LogWarning(Secret);
            return resolve(parameters);
        }
    }
}
