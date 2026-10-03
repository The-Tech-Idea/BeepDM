using System.Globalization;
using TheTechIdea.Beep.Rules;
using TheTechIdea.Beep.Rules.BuiltinParsers;
using Xunit;

namespace TheTechIdea.Beep.Framework.Tests;

public sealed class NfelAdmissionTests
{
    [Theory]
    [InlineData("1 + 2 @")]
    [InlineData("1 @ 2")]
    [InlineData(")(")]
    [InlineData("()")]
    [InlineData("1 2")]
    [InlineData("a..b")]
    [InlineData("a.")]
    [InlineData("'unterminated")]
    [InlineData("1 +")]
    [InlineData("true ? 1")]
    [InlineData("1,2")]
    [InlineData("f(1)")]
    [InlineData("1.")]
    [InlineData("1e")]
    [InlineData("1e+")]
    [InlineData("1e999")]
    [InlineData("1.2.3")]
    [InlineData("a&&")]
    [InlineData("a ? b :")]
    [InlineData("'bad\\q'")]
    [InlineData("'bad\\u00Q0'")]
    [InlineData("false ? 1 @ : 2")]
    [InlineData("@other")]
    [InlineData(":field")]
    public void MalformedInputIsRejectedBeforeExecution(string text)
    {
        var parser = new NfelParser();
        var parsed = parser.ParseRule(text);
        Assert.False(parsed.Success);
        Assert.True(parsed.HasErrors);
        Assert.Empty(((IRuleParser)parser).RuleStructures);
        var engine = new RuleEngine(parser);
        engine.RegisterRule(new ExpressionRule(text));
        Assert.Throws<RuleParseException>(() => engine.SolveRule(text, new()));
    }

    [Theory]
    [InlineData("1-2", -1d)]
    [InlineData("1 - 2", -1d)]
    [InlineData("1--2", 3d)]
    [InlineData("-(1+2)*2", -6d)]
    [InlineData("true ? 1 : 2", 1d)]
    [InlineData("false ? 1 : 2", 2d)]
    [InlineData("2^3^2", 512d)]
    [InlineData("-2^2", -4d)]
    [InlineData("2^-2", .25d)]
    [InlineData(".5 + 1e2", 100.5d)]
    [InlineData("1+2*3", 7d)]
    [InlineData("(1+2)*3", 9d)]
    [InlineData("10%3", 1d)]
    [InlineData("true ? false ? 2 : 3 : 4", 3d)]
    public void NumericGrammarHasActualExecutionResults(string text, double expected)
    {
        Assert.Equal(expected, Evaluate(text));
        Assert.Equal(expected, Solve(text));
    }

    [Fact]
    public void QuotedStringsAreDecodedAndComparedToActualValues()
    {
        Assert.Equal(true, Evaluate("name == 'Alice'", new() { ["name"] = "Alice" }));
        Assert.Equal("a'b", Evaluate("'a\\'b'"));
        Assert.Equal("", Solve("''"));
        Assert.Equal("line\nend", Evaluate("'line\\nend'"));
    }

    [Theory]
    [InlineData("true ? 7 : missing", 7d)]
    [InlineData("false ? missing : 8", 8d)]
    [InlineData("false && missing", false)]
    [InlineData("true || missing", true)]
    [InlineData("false && (1/0>0)", false)]
    public void UnusedBranchesAreLazy(string text, object expected)
    {
        Assert.Equal(expected, Evaluate(text));
        Assert.Equal(expected, Solve(text));
    }

    [Fact]
    public void SuppliedTokensCannotOverrideRegisteredExpression()
    {
        var parser = new NfelParser();
        var engine = new RuleEngine(parser);
        var rule = new ExpressionRule("1+2")
        {
            Structure = new RuleStructure { Tokens = new() { new(TokenType.NumericLiteral, "99") } }
        };
        engine.RegisterRule(rule);
        Assert.Throws<RuleParseException>(() => engine.SolveRule("1+2", new()));
    }

    [Fact]
    public void ReturnedHistoryCannotModifyParserOwnership()
    {
        var parser = new NfelParser();
        parser.ParseRule("1+2");
        var history = ((IRuleParser)parser).RuleStructures;
        history[0].Tokens[0].Value = "99";
        history.Clear();
        var fresh = ((IRuleParser)parser).RuleStructures;
        Assert.Single(fresh);
        Assert.Equal("1", fresh[0].Tokens[0].Value);
    }

    [Fact]
    public void InvariantNumbersDoNotDependOnHostCulture()
    {
        var saved = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            Assert.Equal(3.75d, Evaluate("1.25+2.5"));
        }
        finally { CultureInfo.CurrentCulture = saved; }
    }

    [Theory]
    [InlineData("1 ? 2 : 3")]
    [InlineData("'false' && true")]
    [InlineData("!null")]
    [InlineData("'1' == 1")]
    [InlineData("true == 1")]
    [InlineData("null + 1")]
    [InlineData("null < 1")]
    [InlineData("'text' + 1")]
    public void InvalidTypeCoercionsAreNotFabricated(string text)
    {
        Assert.Equal(DiagnosticCode.TypeCoercionFailed, Assert.Throws<RuleEvaluationException>(() => Evaluate(text)).Code);
        Assert.Equal(DiagnosticCode.TypeCoercionFailed, Assert.Throws<RuleEvaluationException>(() => Solve(text)).Code);
    }

    [Theory]
    [InlineData("1/0", DiagnosticCode.DivisionByZero)]
    [InlineData("1%0", DiagnosticCode.DivisionByZero)]
    [InlineData("1e308*10", DiagnosticCode.InvalidNumeric)]
    [InlineData("(-1)^.5", DiagnosticCode.InvalidNumeric)]
    public void NonfiniteResultsAndZeroDivisionAreTypedFailures(string text, DiagnosticCode code)
    {
        Assert.Equal(code, Assert.Throws<RuleEvaluationException>(() => Evaluate(text)).Code);
    }

    [Theory]
    [InlineData("true AND NOT false", true)]
    [InlineData("false OR true", true)]
    [InlineData("null == null", true)]
    [InlineData("null != 'a'", true)]
    [InlineData("'Alice' == 'ALICE'", true)]
    [InlineData("'' == ''", true)]
    [InlineData("'a' < 'b'", true)]
    public void BooleanNullAndStringSemanticsAreExplicit(string text, bool expected)
        => Assert.Equal(expected, Evaluate(text));

    [Fact]
    public void DottedIdentifiersAreLiteralDictionaryKeysNotObjectTraversal()
    {
        Assert.Equal(12d, Evaluate("order.total*2", new() { ["order.total"] = 6 }));
        var observer = new ObservedValue();
        Assert.Throws<RuleEvaluationException>(() => Evaluate("order.total", new() { ["order"] = observer }));
        Assert.Equal(0, observer.Reads);
        Assert.Equal("A", Evaluate("'\\u0041'"));
    }

    [Fact]
    public void IntegralAndDecimalParameterComparisonsDoNotLosePrecision()
    {
        Assert.Equal(false, Evaluate("a==b", new() { ["a"] = 9007199254740993L, ["b"] = 9007199254740992L }));
        Assert.Equal(true, Evaluate("a>b", new() { ["a"] = decimal.MaxValue, ["b"] = decimal.MaxValue - 1 }));
        Assert.Equal(false, Evaluate("a==b", new() { ["a"] = ulong.MaxValue, ["b"] = ulong.MaxValue - 1 }));
    }

    [Fact]
    public void ArbitraryObjectsAreNotConvertedOrEchoed()
    {
        var observed = new ObservedValue();
        var failure = Assert.Throws<RuleEvaluationException>(() => Evaluate("value+1", new() { ["value"] = observed }));
        Assert.Equal(0, observed.Reads);
        Assert.DoesNotContain("SECRET", failure.ToString());
        Assert.Equal(DiagnosticCode.InvalidNumeric,
            Assert.Throws<RuleEvaluationException>(() => Evaluate("value", new() { ["value"] = double.NaN })).Code);
    }

    [Fact]
    public void SourceAndDecodedStringBoundsAreEnforcedAtTheirBoundary()
    {
        var parser = new NfelParser();
        Assert.True(parser.ParseRule("1" + new string(' ', 65535)).Success);
        Assert.False(parser.ParseRule("1" + new string(' ', 65536)).Success);
        Assert.True(parser.ParseRule("'" + new string('a', 16384) + "'").Success);
        Assert.False(parser.ParseRule("'" + new string('a', 16385) + "'").Success);
        Assert.False(parser.ParseRule(new string('a', 257)).Success);
        Assert.True(parser.ParseRule(new string('a', 256)).Success);
    }

    [Fact]
    public void DepthAndNodeLimitsCoverGroupsPrefixAndOwnedTreeHeight()
    {
        var parser = new NfelParser();
        Assert.True(parser.ParseRule(new string('(', 63) + "1" + new string(')', 63)).Success);
        Assert.False(parser.ParseRule(new string('(', 64) + "1" + new string(')', 64)).Success);
        Assert.True(parser.ParseRule(new string('-', 63) + "1").Success);
        Assert.False(parser.ParseRule(new string('-', 64) + "1").Success);
        Assert.True(parser.ParseRule(string.Join("+", Enumerable.Repeat("1", 64))).Success);
        Assert.False(parser.ParseRule(string.Join("+", Enumerable.Repeat("1", 65))).Success);
        string balanced = Balanced(1024);
        Assert.True(parser.ParseRule("-" + balanced).Success);
        var rejected = parser.ParseRule("--" + balanced);
        Assert.False(rejected.Success);
        Assert.Equal(DiagnosticCode.PolicyViolation, Assert.Single(rejected.Diagnostics).Code);
    }

    [Fact]
    public void HistoryIsBoundedByCountCharactersAndTokens()
    {
        var parser = new NfelParser();
        for (int i = 0; i < 140; i++) parser.ParseRule(i.ToString(CultureInfo.InvariantCulture));
        Assert.Equal(128, ((IRuleParser)parser).RuleStructures.Count);
        Assert.Equal("12", ((IRuleParser)parser).RuleStructures[0].Expression);
        parser.Clear();
        Assert.Empty(((IRuleParser)parser).RuleStructures);
        var padded = "1" + new string(' ', 65535);
        for (int i = 0; i < 20; i++) parser.ParseRule(padded);
        Assert.Equal(16, ((IRuleParser)parser).RuleStructures.Count);
        parser.Clear();
        var large = Balanced(1024);
        for (int i = 0; i < 10; i++) Assert.True(parser.ParseRule(large).Success);
        Assert.Equal(8, ((IRuleParser)parser).RuleStructures.Count);
        Assert.True(((IRuleParser)parser).RuleStructures.Sum(s => s.Tokens.Count) <= 32768);
    }

    [Fact]
    public void ParseResultsAndConcurrentClearDoNotOwnHistory()
    {
        var parser = new NfelParser();
        var first = parser.ParseRule("1+2");
        first.Structure.Expression = "SECRET";
        first.Structure.Tokens[0].Value = "99";
        Assert.Equal("1+2", ((IRuleParser)parser).RuleStructures[0].Expression);
        Assert.Equal("1", ((IRuleParser)parser).RuleStructures[0].Tokens[0].Value);
        Parallel.For(0, 300, i =>
        {
            Assert.True(parser.ParseRule(i + "+1").Success);
            if (i % 5 == 0) parser.Clear();
            var snapshot = ((IRuleParser)parser).RuleStructures;
            Assert.True(snapshot.Count <= 128);
            snapshot.Clear();
        });
        Assert.True(((IRuleParser)parser).RuleStructures.Count <= 128);
    }

    [Theory]
    [InlineData(TokenType.Unknown, "@")]
    [InlineData(TokenType.Comma, ",")]
    [InlineData(TokenType.Identifier, "a..b")]
    [InlineData(TokenType.Plus, "*")]
    [InlineData(TokenType.NumericLiteral, "NaN")]
    [InlineData(TokenType.RuleReference, "other")]
    public void InvalidDirectTokensDenyBeforeParameterLookup(TokenType type, string value)
    {
        var comparer = new LookupComparer();
        var parameters = new Dictionary<string, object>(comparer) { ["name"] = 1 };
        comparer.OnLookup = () => throw new Exception("SECRET");
        var tokens = new List<Token> { new(type, value) };
        Assert.Throws<RuleParseException>(() => new RuleEngine(new NfelParser()).EvaluateExpression(tokens, parameters));
        Assert.Equal(0, comparer.Reads);
    }

    [Fact]
    public void DirectTokenCountAndStringLimitsDenyBeforeLookup()
    {
        var engine = new RuleEngine(new NfelParser());
        Assert.Throws<RuleParseException>(() => engine.EvaluateExpression(
            Enumerable.Repeat(new Token(TokenType.NumericLiteral, "1"), 4097).ToList(), new()));
        Assert.Throws<RuleParseException>(() => engine.EvaluateExpression(
            new List<Token> { new(TokenType.StringLiteral, new string('a', 16385)) }, new()));
        Assert.Throws<RuleParseException>(() => engine.EvaluateExpression(new List<Token> { null! }, new()));
    }

    [Fact]
    public void AllTokensIncludingUnusedBranchesMustPassCapturedPolicy()
    {
        var parser = new NfelParser();
        var tokens = parser.ParseRule("true ? 1 : 2^3").Structure.Tokens;
        var policy = new RuleExecutionPolicy { AllowedTokenTypes = new() { TokenType.BooleanLiteral, TokenType.NumericLiteral, TokenType.Question, TokenType.Colon } };
        var engine = new RuleEngine(parser);
        Assert.Equal(DiagnosticCode.OperatorNotAllowed, Assert.Throws<RuleEvaluationException>(() => engine.EvaluateExpression(tokens, new(), policy)).Code);
        engine.RegisterRule(new ExpressionRule("true ? 1 : 2^3"));
        Assert.Equal(DiagnosticCode.OperatorNotAllowed, Assert.Throws<RuleEvaluationException>(() => engine.SolveRule("true ? 1 : 2^3", new(), policy)).Code);
    }

    [Fact]
    public void TokenAndPolicyMutationDuringLookupCannotChangeAdmittedExecution()
    {
        var parser = new NfelParser();
        var tokens = parser.ParseRule("value+2").Structure.Tokens;
        var policy = new RuleExecutionPolicy { AllowedTokenTypes = new() { TokenType.Identifier, TokenType.Plus, TokenType.NumericLiteral } };
        var comparer = new LookupComparer();
        var parameters = new Dictionary<string, object>(comparer) { ["value"] = 1 };
        comparer.OnLookup = () => { tokens[2].Value = "99"; policy.AllowedTokenTypes.Clear(); };
        Assert.Equal(3d, new RuleEngine(parser).EvaluateExpression(tokens, parameters, policy));
        Assert.Equal(1, comparer.Reads);
    }

    [Fact]
    public void LookupFailuresAreSanitizedEvenWhenObserverThrowsRuleException()
    {
        var comparer = new LookupComparer();
        var parameters = new Dictionary<string, object>(comparer) { ["value"] = 1 };
        comparer.OnLookup = () => throw new RuleEvaluationException(DiagnosticCode.DivisionByZero, "SECRET");
        var failure = Assert.Throws<RuleEvaluationException>(() => Evaluate("value", parameters));
        Assert.DoesNotContain("SECRET", failure.ToString());
        Assert.Equal(DiagnosticCode.PolicyViolation, failure.Code);
    }

    [Fact]
    public void LookupTimeoutCannotBeDisabledByCallerPolicyMutation()
    {
        var parser = new NfelParser();
        var tokens = parser.ParseRule("value").Structure.Tokens;
        var policy = new RuleExecutionPolicy { MaxExecutionMs = 100 };
        var comparer = new LookupComparer();
        var parameters = new Dictionary<string, object>(comparer) { ["value"] = 1 };
        comparer.OnLookup = () =>
        {
            policy.MaxExecutionMs = 0;
            var clock = System.Diagnostics.Stopwatch.StartNew();
            SpinWait.SpinUntil(() => clock.ElapsedMilliseconds >= 150);
        };
        Assert.Equal(DiagnosticCode.TimeoutExceeded,
            Assert.Throws<RuleEvaluationException>(() => new RuleEngine(parser).EvaluateExpression(tokens, parameters, policy)).Code);
        Assert.Equal(1, comparer.Reads);
    }

    [Fact]
    public void FreshAndLoadedRulesRespectLifecycleBeforeLookup()
    {
        var policy = new RuleExecutionPolicy { MinimumLifecycleState = RuleLifecycleState.Approved };
        var engine = new RuleEngine(new NfelParser());
        var rule = new ExpressionRule("value");
        engine.RegisterRule(rule);
        Assert.Equal(DiagnosticCode.LifecycleStateViolation, Assert.Throws<RuleEvaluationException>(() => engine.SolveRule("value", new(), policy)).Code);
        rule.Structure = new RuleStructure { Expression = "value", LifecycleState = RuleLifecycleState.Approved };
        Assert.Equal(7, engine.SolveRule("value", new() { ["value"] = 7 }, policy).result);
        rule.Structure.LifecycleState = RuleLifecycleState.Deprecated;
        Assert.Equal(DiagnosticCode.LifecycleStateViolation, Assert.Throws<RuleEvaluationException>(() => engine.SolveRule("value", new(), policy)).Code);
    }

    [Fact]
    public void LaterMutationIsRejectedButCannotChangeAlreadyAdmittedRun()
    {
        var parser = new NfelParser();
        var rule = new ExpressionRule("value+2") { Structure = parser.ParseRule("value+2").Structure };
        var engine = new RuleEngine(parser);
        engine.RegisterRule(rule);
        var comparer = new LookupComparer();
        var parameters = new Dictionary<string, object>(comparer) { ["value"] = 1 };
        comparer.OnLookup = () => rule.Structure.Tokens[2].Value = "99";
        Assert.Equal(3d, engine.SolveRule("value+2", parameters).result);
        Assert.Throws<RuleParseException>(() => engine.SolveRule("value+2", parameters));
        Assert.Equal(1, comparer.Reads);
    }

    [Fact]
    public void DiagnosticsDoNotEchoInvalidSourceOrPayload()
    {
        var parser = new NfelParser();
        var invalid = parser.ParseRule("'SECRET' @");
        var diag = Assert.Single(invalid.Diagnostics);
        Assert.Equal(9, diag.Start);
        Assert.Equal(1, diag.Length);
        Assert.DoesNotContain("SECRET", diag.Message);
        var engine = new RuleEngine(parser);
        engine.RegisterRule(new ExpressionRule("'SECRET' @"));
        Assert.DoesNotContain("SECRET", Assert.Throws<RuleParseException>(() => engine.SolveRule("'SECRET' @", new())).ToString());
    }

    [Fact]
    public void SerializedTokenOrdinalsAndOtherParserProfilesStayCompatible()
    {
        Assert.Equal(24, (int)TokenType.Unknown);
        Assert.Equal(26, (int)TokenType.CloseParen);
        var legacy = new RuleEngine(new RuleParser());
        Assert.Equal(3d, legacy.EvaluateExpression(new List<Token> {
            new(TokenType.NumericLiteral, "1"), new(TokenType.Plus, "+"), new(TokenType.NumericLiteral, "2") }, new()));
        var formula = new FormulaParser().ParseRule("1+2");
        Assert.True(formula.Success);
        Assert.Equal(TokenType.Unknown, formula.Structure.Tokens[1].Type);
    }

    private static string Balanced(int leaves) => leaves == 1 ? "1" :
        "(" + Balanced(leaves / 2) + "+" + Balanced(leaves - leaves / 2) + ")";

    [Fact]
    public void RegistrationLookupIsCaselessButAdmittedSourceCannotChange()
    {
        var engine = new RuleEngine(new NfelParser());
        var rule = new ExpressionRule("Value+2");
        engine.RegisterRule(rule);
        Assert.Equal(3d, engine.SolveRule("VALUE+2", new() { ["Value"] = 1 }).result);
        rule.RuleText = "value+2";
        Assert.Throws<RuleParseException>(() => engine.SolveRule("Value+2", new() { ["value"] = 99 }));
        Assert.True(engine.UnregisterRule("VALUE+2"));
        engine.RegisterRule(new ExpressionRule("value+2"));
        Assert.Equal(3d, engine.SolveRule("value+2", new() { ["value"] = 1 }).result);
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(1, -1)]
    public void InvalidPolicyLimitsFailBeforeLookup(int depth, int timeout)
    {
        var policy = new RuleExecutionPolicy { MaxDepth = depth, MaxExecutionMs = timeout };
        var parser = new NfelParser();
        var tokens = parser.ParseRule("value").Structure.Tokens;
        Assert.Equal(DiagnosticCode.PolicyViolation,
            Assert.Throws<RuleEvaluationException>(() => new RuleEngine(parser).EvaluateExpression(tokens, new(), policy)).Code);
    }

    [Fact]
    public void InvalidSpansAndChangedExpressionDenyBeforeLookup()
    {
        var parser = new NfelParser();
        var tokens = parser.ParseRule("1").Structure.Tokens;
        tokens[0].Start = int.MaxValue;
        Assert.Throws<RuleParseException>(() => new RuleEngine(parser).EvaluateExpression(tokens, new()));
        var rule = new ExpressionRule("1") { Structure = new RuleStructure { Expression = "2" } };
        var engine = new RuleEngine(parser);
        engine.RegisterRule(rule);
        Assert.Throws<RuleParseException>(() => engine.SolveRule("1", new()));
    }

    [Fact]
    public void NullEmptyAndWhitespaceInputsAreSafeFailures()
    {
        var parser = new NfelParser();
        Assert.False(parser.ParseRule((string)null!).Success);
        Assert.False(parser.ParseRule("").Success);
        Assert.False(parser.ParseRule(" \t\n").Success);
        Assert.Throws<ArgumentNullException>(() => parser.ParseRule((IRule)null!));
        Assert.Empty(((IRuleParser)parser).RuleStructures);
        Assert.Equal(1d, new RuleEngine(parser).EvaluateExpression(parser.ParseRule("1").Structure.Tokens, new(), new RuleExecutionPolicy { MaxDepth = 0, MaxExecutionMs = 0 }));
    }

    [Fact]
    public void ConcatenationAndCallerStringBoundsAreEnforced()
    {
        var allowed = new string('x', 16384);
        Assert.Equal(allowed, Evaluate("value+''", new() { ["value"] = allowed }));
        Assert.Equal(DiagnosticCode.PolicyViolation,
            Assert.Throws<RuleEvaluationException>(() => Evaluate("value+'x'", new() { ["value"] = allowed })).Code);
        Assert.Equal(DiagnosticCode.PolicyViolation,
            Assert.Throws<RuleEvaluationException>(() => Evaluate("value", new() { ["value"] = allowed + "x" })).Code);
    }

    private sealed class ObservedValue
    {
        internal int Reads;
        public override string ToString() { Reads++; throw new Exception("SECRET"); }
    }

    [Theory]
    [InlineData("2.0")]
    [InlineData("1.99")]
    [InlineData(null)]
    public void UnknownTransportVersionsDenyDirectAndRegisteredEvaluation(string? version)
    {
        var parser = new NfelParser();
        var structure = parser.ParseRule("1").Structure;
        structure.Tokens[0].SchemaVersion = version!;
        var engine = new RuleEngine(parser);
        Assert.Throws<RuleParseException>(() => engine.EvaluateExpression(structure.Tokens, new()));
        structure.Tokens[0].SchemaVersion = RuleStructure.CurrentSchemaVersion;
        structure.SchemaVersion = version!;
        var rule = new ExpressionRule("1") { Structure = structure };
        engine.RegisterRule(rule);
        Assert.Throws<RuleParseException>(() => engine.SolveRule("1", new()));
    }

    private sealed class LookupComparer : IEqualityComparer<string>
    {
        internal Action? OnLookup;
        internal int Reads;
        public bool Equals(string? left, string? right) => StringComparer.Ordinal.Equals(left, right);
        public int GetHashCode(string value)
        {
            if (OnLookup != null) { Reads++; OnLookup(); }
            return StringComparer.Ordinal.GetHashCode(value);
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void UnknownOrOversizedTokenPolicyIsDenied(int count)
    {
        var policy = new RuleExecutionPolicy { AllowedTokenTypes = Enumerable.Range(1000, count).Select(v => (TokenType)v).ToHashSet() };
        var parser = new NfelParser();
        Assert.Equal(DiagnosticCode.PolicyViolation,
            Assert.Throws<RuleEvaluationException>(() => new RuleEngine(parser).EvaluateExpression(parser.ParseRule("1").Structure.Tokens, new(), policy)).Code);
    }

    private static object Evaluate(string text, Dictionary<string, object>? parameters = null)
    {
        var parser = new NfelParser();
        var parsed = parser.ParseRule(text);
        Assert.True(parsed.Success);
        return new RuleEngine(parser).EvaluateExpression(parsed.Structure.Tokens, parameters ?? new());
    }

    private static object Solve(string text)
    {
        var engine = new RuleEngine(new NfelParser());
        engine.RegisterRule(new ExpressionRule(text));
        return engine.SolveRule(text, new()).result;
    }

    private sealed class ExpressionRule(string text) : IRule
    {
        public string RuleText { get; set; } = text;
        public IRuleStructure Structure { get; set; } = null!;
        public (Dictionary<string, object> outputs, object result) SolveRule(Dictionary<string, object>? parameters = null)
            => throw new InvalidOperationException("RuleEngine must evaluate the admitted expression.");
    }
}
