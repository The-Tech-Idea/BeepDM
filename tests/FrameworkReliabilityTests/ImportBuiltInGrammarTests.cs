using Moq;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Defaults;
using TheTechIdea.Beep.Editor.Importing;
using Xunit;

namespace TheTechIdea.Beep.Framework.Tests;

public partial class ImportRequiredResolverTests
{
    [Theory]
    [InlineData(":NOT_A_GUID")]
    [InlineData(":NOT_A_USERNAME")]
    [InlineData(":NOT_A_MACHINENAME")]
    [InlineData(":NEWGUID(ignored)")]
    [InlineData(":USERNAME(ignored)")]
    [InlineData(":MACHINENAME(ignored)")]
    [InlineData(":GUID(N,ignored)")]
    [InlineData(":ADD(2,3,)")]
    [InlineData(":ADDDAYS(TODAY,2,)")]
    [InlineData(":IF(true,'yes','no','ignored')")]
    [InlineData(":CONDITIONAL(true,'yes','no','ignored')")]
    [InlineData(":TERNARY(true,'yes','no','ignored')")]
    [InlineData(":NOT(true,false)")]
    [InlineData(":EQ(1,1,ignored)")]
    [InlineData(":NE(1,2,ignored)")]
    [InlineData(":GT(2,1,ignored)")]
    [InlineData(":GTE(2,1,ignored)")]
    [InlineData(":LT(1,2,ignored)")]
    [InlineData(":LTE(1,2,ignored)")]
    [InlineData(":ISNULL(null,'fallback','ignored')")]
    [InlineData(":COALESCE('winner',)")]
    [InlineData(":MATH(PI,ignored)")]
    [InlineData(":MATH(SQRT,16,ignored)")]
    [InlineData(":SEQUENCE(1)")]
    [InlineData(":SEQUENCE(orders,1)")]
    [InlineData(":INCREMENT()")]
    [InlineData(":INCREMENT(Id)")]
    [InlineData(":AUTOINCREMENT(1)")]
    [InlineData(":GUID('N' 'D')")]
    [InlineData(":FORMAT(TODAY,'yyyy' 'MM')")]
    [InlineData(":ADDDAYS(ADDMONTHS('2026-01-01',1,),2)")]
    [InlineData(":ADDDAYS(NEWGUID(),2)")]
    public async Task RequiredBuiltInGrammarRejectsIgnoredArgumentsAndPlaceholderValues(string rule)
    {
        var editor = new Mock<IDMEEditor>();
        var destination = new Mock<IDataSource>();
        var row = new Dictionary<string, object>();
        var result = await Run(editor.Object, Config(destination, rule), row);
        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Equal(1, result.RecordsTransformationFailed);
        Assert.Equal(0, result.WriteAttempts);
        Assert.False(result.HasUncertainWrites);
        Assert.False(row.ContainsKey("Value"));
        destination.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
        editor.Verify(x => x.AddLogMessage(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<Errors>()), Times.Never);
    }

    [Theory]
    [InlineData(":COALESCE('NOW winner','fallback')", "NOW winner")]
    [InlineData(":IF(true,'NEWGUID','fallback')", "NEWGUID")]
    [InlineData(":IF('same'='same','yes','no')", "yes")]
    [InlineData(":PROPERTY(USERNAME)", "row-owned-user")]
    [InlineData(":PROPERTY(MACHINENAME)", "row-owned-host")]
    [InlineData(":PROPERTY(NEWGUID)", "row-owned-guid")]
    public async Task RequiredRoutingUsesOperatorNotWordsInsideArguments(string rule, string expected)
    {
        var destination = new Mock<IDataSource>();
        var row = new Dictionary<string, object>
        {
            ["USERNAME"] = "row-owned-user", ["MACHINENAME"] = "row-owned-host", ["NEWGUID"] = "row-owned-guid"
        };
        var result = await Run(Mock.Of<IDMEEditor>(), Config(destination, rule), row);
        Assert.Equal(Errors.Ok, result.Flag);
        Assert.Equal(expected, row["Value"]);
        destination.Verify(x => x.InsertEntity("target", row), Times.Once);
    }

    [Theory]
    [InlineData(":ADDDAYS(ADDMONTHS('2026-01-01',1),2)")]
    [InlineData(":ADDDAYS(ADDDAYS('2026-01-01',30),3)")]
    public async Task RequiredDateArgumentsRespectNestedCalls(string rule)
    {
        var destination = new Mock<IDataSource>(); var row = new Dictionary<string, object>();
        var result = await Run(Mock.Of<IDMEEditor>(), Config(destination, rule), row);
        Assert.Equal(Errors.Ok, result.Flag);
        Assert.Equal(new DateTime(2026, 2, 3), row["Value"]);
    }

    [Theory]
    [InlineData("invalid-scope")]
    [InlineData("Process,ignored")]
    [InlineData("Process,")]
    public async Task RequiredEnvironmentRuleCannotSilentlySelectFallbackScope(string suffix)
    {
        var key = "BEEP_GRAMMAR_" + Guid.NewGuid().ToString("N");
        Environment.SetEnvironmentVariable(key, Secret);
        try
        {
            var destination = new Mock<IDataSource>(); var row = new Dictionary<string, object>();
            var result = await Run(Mock.Of<IDMEEditor>(), Config(destination, $":ENV({key},{suffix})"), row);
            Assert.Equal(Errors.Failed, result.Flag); Assert.Equal(0, result.WriteAttempts);
            Assert.False(row.ContainsKey("Value"));
            destination.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
        }
        finally { Environment.SetEnvironmentVariable(key, null); }
    }

    [Fact]
    public async Task QuotedConfigurationKeyRoutesToConfigurationNotEmbeddedGuidToken()
    {
        var key = "BEEP_NEWGUID_" + Guid.NewGuid().ToString("N");
        Environment.SetEnvironmentVariable("APPSETTING_" + key, "owned-config-value");
        try
        {
            var destination = new Mock<IDataSource>(); var row = new Dictionary<string, object>();
            var result = await Run(Mock.Of<IDMEEditor>(), Config(destination, $":CONFIG('{key}')"), row);
            Assert.Equal(Errors.Ok, result.Flag); Assert.Equal("owned-config-value", row["Value"]);
        }
        finally { Environment.SetEnvironmentVariable("APPSETTING_" + key, null); }
    }

    [Fact]
    public async Task RequiredConfigurationGrammarDoesNotTreatMultipleArgumentsAsOneKey()
    {
        var key = "BEEP_GRAMMAR_" + Guid.NewGuid().ToString("N") + ",ignored";
        Environment.SetEnvironmentVariable("APPSETTING_" + key, Secret);
        try
        {
            var destination = new Mock<IDataSource>();
            var result = await Run(Mock.Of<IDMEEditor>(), Config(destination, $":CONFIG({key})"), new Dictionary<string, object>());
            Assert.Equal(Errors.Failed, result.Flag); Assert.Equal(0, result.WriteAttempts);
        }
        finally { Environment.SetEnvironmentVariable("APPSETTING_" + key, null); }
    }

    [Fact]
    public async Task CustomRequiredResolverRetainsItsOwnArgumentContract()
    {
        var editor = Mock.Of<IDMEEditor>();
        DefaultsManager.RegisterCustomResolver(editor, new ProbeResolver(editor, _ => 17));
        var destination = new Mock<IDataSource>(); var row = new Dictionary<string, object>();
        Assert.Equal(Errors.Ok, (await Run(editor, Config(destination, ":PROBE(1,2,3)"), row)).Flag);
        Assert.Equal(17, row["Value"]);
    }

    [Theory]
    [InlineData(":NEWGUID", "D")]
    [InlineData(":NEWGUID()", "D")]
    [InlineData(":GUID()", "D")]
    [InlineData(":UUID()", "D")]
    [InlineData(":GENERATEUNIQUEID()", "D")]
    [InlineData(":GUID(N)", "N")]
    [InlineData(":GUID('B')", "B")]
    [InlineData(":UUID(P)", "P")]
    public async Task RequiredGuidAliasesAndFormatsStillProduceGuidStrings(string rule, string format)
    {
        var destination = new Mock<IDataSource>(); var row = new Dictionary<string, object>();
        Assert.Equal(Errors.Ok, (await Run(Mock.Of<IDMEEditor>(), Config(destination, rule), row)).Flag);
        Assert.True(Guid.TryParseExact(Assert.IsType<string>(row["Value"]), format, out var guid));
        Assert.NotEqual(Guid.Empty, guid);
    }

    [Fact]
    public async Task RequiredDateFormatRetainsQuotedCommaAndNestedDate()
    {
        var destination = new Mock<IDataSource>(); var row = new Dictionary<string, object>();
        var result = await Run(Mock.Of<IDMEEditor>(), Config(destination, ":FORMAT(ADDDAYS('2026-01-01',2),'yyyy,MM,dd')"), row);
        Assert.Equal(Errors.Ok, result.Flag); Assert.Equal("2026,01,03", row["Value"]);
    }

    [Fact]
    public async Task RequiredConfigurationStillAcceptsOneQuotedKeyContainingComma()
    {
        var key = "BEEP_GRAMMAR_" + Guid.NewGuid().ToString("N") + ",one-key";
        Environment.SetEnvironmentVariable("APPSETTING_" + key, "one-key-value");
        try
        {
            var destination = new Mock<IDataSource>(); var row = new Dictionary<string, object>();
            var result = await Run(Mock.Of<IDMEEditor>(), Config(destination, $":CONFIG('{key}')"), row);
            Assert.Equal(Errors.Ok, result.Flag); Assert.Equal("one-key-value", row["Value"]);
        }
        finally { Environment.SetEnvironmentVariable("APPSETTING_" + key, null); }
    }

    [Fact]
    public async Task ValidRequiredEnvironmentProcessScopeReadsActualValue()
    {
        var key = "BEEP_GRAMMAR_" + Guid.NewGuid().ToString("N");
        Environment.SetEnvironmentVariable(key, "process-value");
        try
        {
            var destination = new Mock<IDataSource>(); var row = new Dictionary<string, object>();
            var result = await Run(Mock.Of<IDMEEditor>(), Config(destination, $":ENV('{key}','Process')"), row);
            Assert.Equal(Errors.Ok, result.Flag); Assert.Equal("process-value", row["Value"]);
        }
        finally { Environment.SetEnvironmentVariable(key, null); }
    }

    [Fact]
    public async Task RequiredColonKeyCanContainQuotedParentheses()
    {
        var key = "BEEP_GRAMMAR_" + Guid.NewGuid().ToString("N") + "(one-key)";
        Environment.SetEnvironmentVariable("APPSETTING_" + key, "one-key-value");
        try
        {
            var destination = new Mock<IDataSource>(); var row = new Dictionary<string, object>();
            var result = await Run(Mock.Of<IDMEEditor>(), Config(destination, $":CONFIG:'{key}'"), row);
            Assert.Equal(Errors.Ok, result.Flag); Assert.Equal("one-key-value", row["Value"]);
        }
        finally { Environment.SetEnvironmentVariable("APPSETTING_" + key, null); }
    }

    [Fact]
    public void LegacyGuidAndSequenceFallbacksRemainOutsideRequiredFrame()
    {
        var editor = Mock.Of<IDMEEditor>();
        Assert.True(Guid.TryParse((string)DefaultsManager.Resolve(editor, ":NOT_A_GUID"), out _));
        Assert.IsType<int>(DefaultsManager.Resolve(editor, ":SEQUENCE(1)"));
    }
}
