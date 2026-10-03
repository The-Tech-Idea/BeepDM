using Moq;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Defaults;
using TheTechIdea.Beep.Editor.Defaults.Resolvers;
using TheTechIdea.Beep.Editor.Defaults.RuleParsing;
using Xunit;

namespace TheTechIdea.Beep.Framework.Tests;

public partial class ImportRequiredResolverTests
{
    [Theory]
    [InlineData(":GUID.")]
    [InlineData(":GUID..N")]
    [InlineData("GUID.")]
    [InlineData("GUID.'unterminated")]
    [InlineData(":ADD..'2'.'3'")]
    [InlineData(":ADD.'2'.'3'.")]
    [InlineData(":IF.true..'yes'.'no'")]
    [InlineData(":IF.true.'yes'.'no'.")]
    [InlineData(":IF.true.'yes'ignored.'no'")]
    [InlineData(":IF.true.'yes' 'other'.'no'")]
    [InlineData(":IF.true.'yes'.'no'.'extra'")]
    [InlineData(":PROPERTY..'Input'")]
    [InlineData(":PROPERTY.'Input'.")]
    [InlineData(":COALESCE..'Input'.'fallback'")]
    [InlineData(":COALESCE.'Input'.'fallback'.")]
    [InlineData(":PROPERTY.Input,ignored")]
    [InlineData(":IF.true.'unterminated")]
    public async Task RequiredDotGrammarDoesNotEraseMissingSegmentsOrMalformedQuotes(string rule)
    {
        var editor = new Mock<IDMEEditor>(); var destination = new Mock<IDataSource>();
        var row = new Dictionary<string, object> { ["Input"] = 7 };
        var result = await Run(editor.Object, Config(destination, rule), row);
        Assert.Equal(Errors.Failed, result.Flag); Assert.Equal(1, result.RecordsTransformationFailed);
        Assert.Equal(0, result.WriteAttempts); Assert.False(result.HasUncertainWrites);
        Assert.False(row.ContainsKey("Value"));
        destination.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
        editor.Verify(x => x.AddLogMessage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(),
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<Errors>()), Times.Never);
    }

    [Theory]
    [InlineData(":COALESCE.'Input'.'fallback'", "Input")]
    [InlineData(":ISNULL.'Input'.'fallback'", "Input")]
    [InlineData(":COALESCE.'123'.'fallback'", "123")]
    [InlineData(":COALESCE.'true'.'fallback'", "true")]
    [InlineData(":COALESCE.'false'.'fallback'", "false")]
    [InlineData(":COALESCE.'null'.'fallback'", "null")]
    [InlineData(":COALESCE.null.'fallback'", "fallback")]
    [InlineData(":COALESCE.\"a.b,c(d)\".'fallback'", "a.b,c(d)")]
    [InlineData(":IF.true.''.'else'", "")]
    [InlineData(":CASE.'a'.'a'.''.'else'", "")]
    [InlineData(":EXPRESSION.'A+B'", "A+B")]
    [InlineData(":EXPRESSION(\"A+B\")", "A+B")]
    public async Task RequiredDotLiteralArgumentsPreserveTheirStringMeaning(string rule, string expected)
    {
        var destination = new Mock<IDataSource>();
        var row = new Dictionary<string, object> { ["Input"] = 7, ["A"] = 2, ["B"] = 3 };
        var result = await Run(Mock.Of<IDMEEditor>(), Config(destination, rule), row);
        Assert.Equal(Errors.Ok, result.Flag);
        Assert.Equal(expected, Assert.IsType<string>(row["Value"]));
        destination.Verify(x => x.InsertEntity("target", row), Times.Once);
    }

    [Theory]
    [InlineData(":ADD.2.3", 5d)]
    [InlineData(":ADD.'1.5'.'2.5'", 4d)]
    [InlineData(":SUB.'5'.'2'", 3d)]
    [InlineData(":MUL.'2'.'3'", 6d)]
    [InlineData(":DIV.'6'.'2'", 3d)]
    [InlineData(":ROUND.'1.25'.1", 1.3d)]
    [InlineData(":CALCULATE.'2 + 3'", 5d)]
    [InlineData(":EXPRESSION.2+3", 5d)]
    public async Task RequiredDotMathUsesDelimitersAndQuotedDecimalNumbers(string rule, double expected)
    {
        var destination = new Mock<IDataSource>(); var row = new Dictionary<string, object>();
        Assert.Equal(Errors.Ok, (await Run(Mock.Of<IDMEEditor>(), Config(destination, rule), row)).Flag);
        Assert.Equal(expected, row["Value"]);
    }

    [Fact]
    public async Task RequiredDotConditionGroupingRetainsDecimalComparison()
    {
        var destination = new Mock<IDataSource>(); var row = new Dictionary<string, object> { ["Score"] = 2d };
        Assert.Equal(Errors.Ok, (await Run(Mock.Of<IDMEEditor>(), Config(destination,
            ":IF.'Score >= 1.5'.'yes'.'no'"), row)).Flag);
        Assert.Equal("yes", row["Value"]);
    }

    [Fact]
    public async Task RequiredDotDateGroupingRetainsNestedCallAndFormatLiteral()
    {
        var destination = new Mock<IDataSource>(); var row = new Dictionary<string, object>();
        Assert.Equal(Errors.Ok, (await Run(Mock.Of<IDMEEditor>(), Config(destination,
            ":FORMAT.'ADDDAYS(\"2026-01-01\",2)'.'yyyy,MM,dd'"), row)).Flag);
        Assert.Equal("2026,01,03", row["Value"]);
    }

    [Fact]
    public async Task RequiredDotConfigurationRetainsQuotedKeyPunctuation()
    {
        var key = "BEEP_DOT_" + Guid.NewGuid().ToString("N") + ".comma,(parenthesis)";
        Environment.SetEnvironmentVariable("APPSETTING_" + key, "actual-key-value");
        try
        {
            var destination = new Mock<IDataSource>(); var row = new Dictionary<string, object>();
            Assert.Equal(Errors.Ok, (await Run(Mock.Of<IDMEEditor>(), Config(destination, $":CONFIG.'{key}'"), row)).Flag);
            Assert.Equal("actual-key-value", row["Value"]);
        }
        finally { Environment.SetEnvironmentVariable("APPSETTING_" + key, null); }
    }

    [Fact]
    public async Task RequiredDotCustomOverrideReceivesPreservedLiterals()
    {
        var editor = Mock.Of<IDMEEditor>(); string? observed = null;
        DefaultsManager.RegisterCustomResolver(editor, new DotOverrideResolver(editor, rule => { observed = rule; return 17; }));
        var destination = new Mock<IDataSource>(); var row = new Dictionary<string, object>();
        Assert.Equal(Errors.Ok, (await Run(editor, Config(destination, ":COALESCE.'Input'.'false'"), row)).Flag);
        Assert.Equal("COALESCE('Input','false')", observed); Assert.Equal(17, row["Value"]);
    }

    [Theory]
    [InlineData(":LOOKUP.'Users'.'Email'.\"Name='a.b,c'\"")]
    [InlineData(":QUERY.'scalar'.'Users'.'Email'.\"Name='a.b,c'\"")]
    public async Task RequiredDotQueryGroupingPreservesActualProviderFilter(string rule)
    {
        var editor = Mock.Of<IDMEEditor>(); var source = new Mock<IDataSource>();
        List<TheTechIdea.Beep.Report.AppFilter>? observed = null;
        source.Setup(x => x.GetEntity("Users", It.IsAny<List<TheTechIdea.Beep.Report.AppFilter>>()))
            .Callback((string _, List<TheTechIdea.Beep.Report.AppFilter> filters) => observed = filters)
            .Returns(new object[] { new Dictionary<string, object> { ["Email"] = "actual-lookup-value" } });
        DefaultsManager.RegisterCustomResolver(editor, new ProbeResolver(editor, _ =>
            DefaultsManager.Resolve(editor, rule, new PassedArgs { DataSource = source.Object })));
        var destination = new Mock<IDataSource>(); var row = new Dictionary<string, object>();
        Assert.Equal(Errors.Ok, (await Run(editor, Config(destination, ":PROBE()"), row)).Flag);
        Assert.Equal("actual-lookup-value", row["Value"]);
        var filter = Assert.Single(observed!);
        Assert.Equal("Name", filter.FieldName); Assert.Equal("=", filter.Operator); Assert.Equal("a.b,c", filter.FilterValue);
        source.Verify(x => x.GetEntity("Users", It.IsAny<List<TheTechIdea.Beep.Report.AppFilter>>()), Times.Once);
    }

    [Theory]
    [InlineData(":LOOKUP.'Users'..'Email'")]
    [InlineData(":QUERY.'scalar'.'Users'.'Email'.")]
    public async Task MalformedRequiredDotQueryStopsBeforeProviderRead(string rule)
    {
        var editor = Mock.Of<IDMEEditor>(); var source = new Mock<IDataSource>();
        DefaultsManager.RegisterCustomResolver(editor, new ProbeResolver(editor, _ =>
            DefaultsManager.Resolve(editor, rule, new PassedArgs { DataSource = source.Object })));
        var destination = new Mock<IDataSource>(); var row = new Dictionary<string, object>();
        Assert.Equal(Errors.Failed, (await Run(editor, Config(destination, ":PROBE()"), row)).Flag);
        source.Verify(x => x.GetEntity(It.IsAny<string>(), It.IsAny<List<TheTechIdea.Beep.Report.AppFilter>>()), Times.Never);
        destination.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task UnknownCustomDotDialectIsNotRewrittenByShippedParser()
    {
        var editor = Mock.Of<IDMEEditor>(); string? observed = null;
        DefaultsManager.RegisterCustomResolver(editor, new UnknownDotResolver(editor, rule => { observed = rule; return 17; }));
        var destination = new Mock<IDataSource>(); var row = new Dictionary<string, object>();
        Assert.Equal(Errors.Ok, (await Run(editor, Config(destination, ":PLUGIN.'Input'.''"), row)).Flag);
        Assert.Equal("PLUGIN.'Input'.''", observed); Assert.Equal(17, row["Value"]);
    }

    private sealed class UnknownDotResolver(IDMEEditor editor, Func<string, object> resolve) : BaseDefaultValueResolver(editor)
    {
        public override string ResolverName => "PluginDot";
        public override IEnumerable<string> SupportedRuleTypes => new[] { "PLUGIN" };
        public override bool CanHandle(string rule) => rule.StartsWith("PLUGIN.", StringComparison.Ordinal);
        public override object ResolveValue(string rule, IPassedArgs parameters) => resolve(rule);
        public override IEnumerable<string> GetExamples() => Array.Empty<string>();
    }

    private sealed class DotOverrideResolver(IDMEEditor editor, Func<string, object> resolve) : BaseDefaultValueResolver(editor)
    {
        public override string ResolverName => "Expression";
        public override IEnumerable<string> SupportedRuleTypes => new[] { "COALESCE" };
        public override bool CanHandle(string rule) => rule.StartsWith("COALESCE(", StringComparison.Ordinal);
        public override object ResolveValue(string rule, IPassedArgs parameters) => resolve(rule);
        public override IEnumerable<string> GetExamples() => Array.Empty<string>();
    }

    [Fact]
    public void PublicLegacyDotNormalizationRetainsItsCompatibilityBehavior()
    {
        Assert.Equal("COALESCE(false,fallback)", RuleNormalizer.Normalize(":COALESCE.'false'.'fallback'").NormalizedRule);
        Assert.Equal("IF(true,yes,no)", RuleNormalizer.Normalize(":IF.true..yes.no").NormalizedRule);
    }
}
