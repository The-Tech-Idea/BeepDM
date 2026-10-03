using System.Globalization;
using Moq;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
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
    [Theory]
    [InlineData(":IF(1,'yes','no')")]
    [InlineData(":IF('false','yes','no')")]
    [InlineData(":NOT(1)")]
    [InlineData(":AND(true,1)")]
    [InlineData(":OR(false,'value')")]
    [InlineData(":EXPRESSION(unknownIdentifier)")]
    [InlineData(":EXPRESSION(1 2)")]
    [InlineData(":EXPRESSION(2 @ 3)")]
    [InlineData(":EXPRESSION('2'+3)")]
    [InlineData(":EQ('1',1)")]
    [InlineData(":GTE(null,null)")]
    [InlineData(":EXPRESSION(1<2<3)")]
    [InlineData(":IF(true,'yes',1 @ 2)")]
    [InlineData(":ISNULL(Missing,'fallback')")]
    [InlineData(":EQ(NonFinite,NonFinite)")]
    [InlineData(":NOT(ObjectValue)")]
    [InlineData(":MATH(SQRT,-1)")]
    [InlineData(":EXPRESSION(1e309)")]
    [InlineData(":EXPRESSION(1e-400)")]
    [InlineData(":EXPRESSION(1.23456789012345678901234567890123456789)")]
    [InlineData(":EXPRESSION(79228162514264337593543950335+1)")]
    [InlineData(":ROUND(1,1.5)")]
    [InlineData(":ROUND(1,16)")]
    [InlineData(":RANDOM(2,1)")]
    [InlineData(":RANDOM(2147483648)")]
    [InlineData(":EXPRESSION(1%0)")]
    [InlineData(":GT(true,false)")]
    public async Task StrictRequiredExpressionDeniesInvalidTypesOrIncompleteSyntax(string rule)
    {
        var editor = new Mock<IDMEEditor>(); var destination = new Mock<IDataSource>();
        var row = new Dictionary<string, object> { ["NonFinite"] = double.NaN, ["ObjectValue"] = new object() };
        var result = await Run(editor.Object, Config(destination, rule), row);
        Assert.Equal(Errors.Failed, result.Flag); Assert.Equal(0, result.WriteAttempts);
        Assert.Equal(1, result.RecordsTransformationFailed); Assert.False(result.HasUncertainWrites);
        Assert.False(row.ContainsKey("Value"));
        destination.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
        editor.Verify(x => x.AddLogMessage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(),
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<Errors>()), Times.Never);
    }

    [Theory]
    [InlineData(":EXPRESSION(2+3*4)", 14d)]
    [InlineData(":EXPRESSION((2+3)*4)", 20d)]
    [InlineData(":EXPRESSION(1-2-3)", -4d)]
    [InlineData(":EXPRESSION(-2+3)", 1d)]
    [InlineData(":EXPRESSION(1e-3+2)", 2.001d)]
    [InlineData(":EXPRESSION(Amount*Quantity+2)", 4.5d)]
    [InlineData(":EXPRESSION(ADD(Amount,2)*Quantity)", 6.5d)]
    [InlineData(":CALCULATE((2+3)*4)", 20d)]
    [InlineData(":COMPUTE(Amount*Quantity+2)", 4.5d)]
    [InlineData(":ROUND(Amount,1)", 1.3d)]
    [InlineData(":ROUND(1.005,2)", 1.01d)]
    [InlineData(":ROUND(-1.005,2)", -1.01d)]
    [InlineData(":MATH('ROUND',2.5)", 2d)]
    [InlineData(":MATH('PI')", Math.PI)]
    [InlineData(":ADD.'1.25'.'2.5'", 3.75d)]
    [InlineData(":RANDOM(2147483647,2147483647)", 2147483647)]
    public async Task StrictRequiredMathUsesPrecedenceAndActualTypedRowValues(string rule, double expected)
    {
        var destination = new Mock<IDataSource>();
        var row = new Dictionary<string, object> { ["Amount"] = 1.25m, ["Quantity"] = 2 };
        Assert.Equal(Errors.Ok, (await Run(Mock.Of<IDMEEditor>(), Config(destination, rule), row)).Flag);
        Assert.Equal(expected, Convert.ToDouble(row["Value"], CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData(":EXPRESSION(true OR false AND false)", true)]
    [InlineData(":EVAL(NOT false AND true)", true)]
    [InlineData(":NOT(EQ(Amount,1.25))", false)]
    [InlineData(":AND(GTE(Age,18),NOT(false))", true)]
    [InlineData(":EQ(Left,Right)", false)]
    [InlineData(":LT(Left,Right)", true)]
    [InlineData(":EQ(1.00001,1.00002)", false)]
    [InlineData(":LT('10','2')", true)]
    [InlineData(":EQ('Active','active')", true)]
    [InlineData(":EXPRESSION(null=null)", true)]
    [InlineData(":EQ(18446744073709551615,18446744073709551614)", false)]
    [InlineData(":EQ(1e-29,2e-29)", false)]
    [InlineData(":LT(0.1,0.1000000000000000000000000001)", true)]
    public async Task StrictRequiredComparisonAndBooleanValuesHaveExactMeaning(string rule, bool expected)
    {
        var destination = new Mock<IDataSource>();
        var row = new Dictionary<string, object>
        {
            ["Amount"] = 1.25m, ["Age"] = 21,
            ["Left"] = 9007199254740992L, ["Right"] = 9007199254740993L
        };
        Assert.Equal(Errors.Ok, (await Run(Mock.Of<IDMEEditor>(), Config(destination, rule), row)).Flag);
        Assert.Equal(expected, Assert.IsType<bool>(row["Value"]));
    }

    [Theory]
    [InlineData(":IF(GTE(Age,18),'adult','minor')", "adult")]
    [InlineData(":IF(false,1/0,'no')", "no")]
    [InlineData(":CASE(Status,'Active','yes','no')", "yes")]
    [InlineData(":COALESCE(Input,'fallback')", "fallback")]
    public async Task StrictRequiredConditionalsReadRowsAndOnlyEvaluateSelectedValues(string rule, string expected)
    {
        var destination = new Mock<IDataSource>();
        var row = new Dictionary<string, object> { ["Age"] = 21, ["Status"] = "Active", ["Input"] = null! };
        Assert.Equal(Errors.Ok, (await Run(Mock.Of<IDMEEditor>(), Config(destination, rule), row)).Flag);
        Assert.Equal(expected, row["Value"]);
    }

    [Theory]
    [InlineData(":AND(false,Thrower)", false)]
    [InlineData(":OR(true,Thrower)", true)]
    public async Task StrictRequiredLogicalShortCircuitDoesNotInvokeUnusedGetter(string rule, bool expected)
    {
        var destination = new Mock<IDataSource>(); var row = new ShortCircuitRow();
        Assert.Equal(Errors.Ok, (await Run(Mock.Of<IDMEEditor>(), Config(destination, rule), row)).Flag);
        Assert.Equal(expected, row.Value); Assert.Equal(0, row.Reads);
    }

    private sealed class ShortCircuitRow
    {
        public object? Value { get; set; }
        public int Reads { get; private set; }
        public bool Thrower { get { Reads++; throw new IOException(Secret); } }
    }

    [Theory]
    [InlineData("fr-FR")]
    [InlineData("ar-KW")]
    public async Task StrictRequiredNumbersDoNotDependOnCurrentCulture(string culture)
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
        try
        {
            var destination = new Mock<IDataSource>();
            var row = new Dictionary<string, object> { ["Amount"] = 1.25m, ["Quantity"] = 2 };
            Assert.Equal(Errors.Ok, (await Run(Mock.Of<IDMEEditor>(), Config(destination,
                ":EXPRESSION(Amount*Quantity+0.5)"), row)).Flag);
            Assert.Equal(3d, row["Value"]);
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [Fact]
    public async Task NestedRequiredMathHonorsCapturedCustomResolverInsteadOfBypassingIt()
    {
        var editor = Mock.Of<IDMEEditor>();
        DefaultsManager.RegisterCustomResolver(editor, new ExpressionMathOverride(editor));
        var destination = new Mock<IDataSource>(); var row = new Dictionary<string, object>();
        Assert.Equal(Errors.Ok, (await Run(editor, Config(destination, ":EXPRESSION(ADD(2,3)+1)"), row)).Flag);
        Assert.Equal(18d, row["Value"]);
    }

    private sealed class ExpressionMathOverride(IDMEEditor editor) : BaseDefaultValueResolver(editor)
    {
        public override string ResolverName => "Formula";
        public override IEnumerable<string> SupportedRuleTypes => new[] { "ADD" };
        public override bool CanHandle(string rule) => rule.StartsWith("ADD(", StringComparison.Ordinal);
        public override object ResolveValue(string rule, IPassedArgs parameters) => 17;
        public override IEnumerable<string> GetExamples() => Array.Empty<string>();
    }

    [Theory]
    [InlineData("nesting")]
    [InlineData("depth")]
    [InlineData("nodes")]
    [InlineData("tokens")]
    [InlineData("syntax")]
    public async Task RequiredExpressionParsesAndBoundsEntireTreeBeforeReadingAnyField(string kind)
    {
        var expression = kind switch
        {
            "nesting" => new string('(', 33) + "Thrower" + new string(')', 33),
            "depth" => "Thrower" + string.Concat(Enumerable.Repeat("+1", 33)),
            "nodes" => "COALESCE(Thrower," + string.Join(",", Enumerable.Repeat("0", 1024)) + ")",
            "tokens" => "COALESCE(Thrower," + string.Join(",", Enumerable.Repeat("0", 2050)) + ")",
            _ => "IF(Thrower,'yes',1 @ 2)"
        };
        var row = new ShortCircuitRow(); var destination = new Mock<IDataSource>();
        var result = await Run(Mock.Of<IDMEEditor>(), Config(destination, ":EXPRESSION(" + expression + ")"), row);
        Assert.Equal(Errors.Failed, result.Flag); Assert.Equal(0, result.WriteAttempts);
        Assert.Equal(0, row.Reads); Assert.Null(row.Value);
    }

    [Theory]
    [InlineData(":EQ(FloatValue,FloatValue)", true)]
    [InlineData(":EQ(FloatValue,0.1)", true)]
    [InlineData(":LT(LargeDouble,LargeInteger)", true)]
    [InlineData(":EQ(DateValue,DateValue)", true)]
    [InlineData(":EQ(GuidValue,GuidValue)", true)]
    public async Task RequiredExpressionComparisonsAdmitTypedScalarsWithoutObserverConversion(string rule, bool expected)
    {
        var row = new Dictionary<string, object>
        {
            ["FloatValue"] = 0.1f, ["LargeDouble"] = 9007199254740992d,
            ["LargeInteger"] = 9007199254740993L, ["DateValue"] = new DateOnly(2026, 10, 3),
            ["GuidValue"] = Guid.Empty
        };
        var destination = new Mock<IDataSource>();
        Assert.Equal(Errors.Ok, (await Run(Mock.Of<IDMEEditor>(), Config(destination, rule), row)).Flag);
        Assert.Equal(expected, row["Value"]);
    }

    [Theory]
    [InlineData(":EQ(Tiny,0)")]
    [InlineData(":IF(NumericText,'yes','no')")]
    [InlineData(":EXPRESSION(NumericText+1)")]
    [InlineData(":MATH(ABS,NumericText)")]
    [InlineData(":EXPRESSION(Ambiguous+1)")]
    public async Task RequiredExpressionRejectsLossyMixedComparisonStringCoercionAndAmbiguousFields(string rule)
    {
        var row = new Dictionary<string, object>
        {
            ["Tiny"] = double.Epsilon, ["NumericText"] = "2", ["Ambiguous"] = 1, ["ambiguous"] = 2
        };
        var destination = new Mock<IDataSource>();
        var result = await Run(Mock.Of<IDMEEditor>(), Config(destination, rule), row);
        Assert.Equal(Errors.Failed, result.Flag); Assert.Equal(0, result.WriteAttempts);
        Assert.False(row.ContainsKey("Value"));
    }

    [Fact]
    public async Task RequiredExpressionCancellationAfterGetterPreventsAssignmentAndProviderWrite()
    {
        using var cancellation = new CancellationTokenSource();
        var row = new CancellingExpressionRow(cancellation); var destination = new Mock<IDataSource>();
        var editor = Mock.Of<IDMEEditor>();
        var batch = new DataImportBatchHelper(editor, new DataImportTransformationHelper(editor), Mock.Of<IDataImportProgressHelper>());
        var result = await batch.ProcessBatchDetailedAsync(new object[] { row },
            Config(destination, ":EXPRESSION(Input+1)"), null!, cancellation.Token);
        Assert.Equal(ImportOutcome.Cancelled, result.Outcome); Assert.Equal(0, result.WriteAttempts);
        Assert.Null(row.Value); Assert.Equal(1, row.Reads); Assert.False(result.HasUncertainWrites);
    }

    private sealed class CancellingExpressionRow(CancellationTokenSource cancellation)
    {
        public object? Value { get; set; }
        public int Reads { get; private set; }
        public int Input { get { Reads++; cancellation.Cancel(); return 1; } }
    }

    [Fact]
    public void LegacyExpressionFallbackIsUnchangedOutsideRequiredScope()
    {
        var editor = Mock.Of<IDMEEditor>();
        Assert.Equal("no", DefaultsManager.Resolve(editor, ":IF(1,'yes','no')"));
        Assert.Equal(true, DefaultsManager.Resolve(editor, ":NOT(1)"));
    }
}
