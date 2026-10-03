using System.Collections;
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
using TheTechIdea.Beep.Report;
using Xunit;

namespace TheTechIdea.Beep.Framework.Tests;

public partial class ImportRequiredResolverTests
{
    private static Task<ImportExecutionResult> QueryRun(string rule, Mock<IDataSource> source,
        Mock<IDataSource> destination, object row, List<ObjectItem>? objects = null,
        IDMEEditor? editor = null, CancellationToken token = default)
    {
        editor ??= Mock.Of<IDMEEditor>();
        var owner = editor;
        DefaultsManager.RegisterCustomResolver(owner, new ProbeResolver(owner, args =>
            DefaultsManager.Resolve(owner, rule, new PassedArgs
            { DataSource = source.Object, ReturnData = args.ReturnData, Objects = objects ?? new() })));
        Mock.Get(owner).Invocations.Clear();
        return new DataImportBatchHelper(owner, new DataImportTransformationHelper(owner), Mock.Of<IDataImportProgressHelper>())
            .ProcessBatchDetailedAsync(new object[] { row }, Config(destination, ":PROBE()"), null!, token);
    }

    [Theory]
    [InlineData(":COUNT(Users,garbage)")]
    [InlineData(":QUERY(count,Users,garbage)")]
    [InlineData(":LOOKUP(Users,Email,Age=>2)")]
    [InlineData(":LOOKUP(Users,Email,Age==2)")]
    [InlineData(":LOOKUP(Users,Email,Name=)")]
    [InlineData(":LOOKUP(Users,Email,=1)")]
    [InlineData(":LOOKUP(Users,Email,Age=1 AND Name='x')")]
    [InlineData(":LOOKUP(Users,Email,Age>=)")]
    [InlineData(":LOOKUP(Users,Email,Name=@Missing)")]
    [InlineData(":LOOKUP(Users,Email,Name='a' 'b')")]
    [InlineData(":MAX(Users)")]
    [InlineData(":MIN(Users)")]
    [InlineData(":SUM(Users)")]
    [InlineData(":AVG(Users)")]
    [InlineData(":QUERY(aggregate,BOGUS,Users,Email)")]
    [InlineData(":LOOKUP('',Email)")]
    [InlineData(":LOOKUP(Users,'')")]
    [InlineData(":QUERY('bogus',Users)")]
    public async Task RequiredQueryValidatesEveryArgumentBeforeProviderRead(string rule)
    {
        var source = new Mock<IDataSource>(); var destination = new Mock<IDataSource>();
        source.Setup(x => x.GetEntity(It.IsAny<string>(), It.IsAny<List<AppFilter>>()))
            .Returns(new object[] { new Dictionary<string, object> { ["Email"] = 7 } });
        var editor = new Mock<IDMEEditor>(); var row = new Dictionary<string, object>();
        var result = await QueryRun(rule, source, destination, row, editor: editor.Object);
        Assert.Equal(Errors.Failed, result.Flag); Assert.Equal(0, result.WriteAttempts);
        Assert.Equal(1, result.RecordsTransformationFailed); Assert.False(row.ContainsKey("Value"));
        source.Verify(x => x.GetEntity(It.IsAny<string>(), It.IsAny<List<AppFilter>>()), Times.Never);
        editor.Verify(x => x.AddLogMessage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(),
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<Errors>()), Times.Never);
    }

    [Theory]
    [InlineData(":COUNT(Users)")]
    [InlineData(":QUERY(count,Users)")]
    [InlineData(":QUERY(exists,Users)")]
    public async Task NullRequiredQueryResponseCannotMeanEmptyOrFalse(string rule)
    {
        var source = new Mock<IDataSource>(); var destination = new Mock<IDataSource>();
        source.Setup(x => x.GetEntity("Users", It.IsAny<List<AppFilter>>())).Returns((IEnumerable<object>)null!);
        var result = await QueryRun(rule, source, destination, new Dictionary<string, object>());
        Assert.Equal(Errors.Failed, result.Flag); Assert.Equal(0, result.WriteAttempts);
        Assert.False(result.HasUncertainWrites);
    }

    [Theory]
    [InlineData(":COUNT(Users)", 0)]
    [InlineData(":QUERY('count',Users)", 0)]
    [InlineData(":QUERY(exists,Users)", false)]
    [InlineData(":QUERY('exists',Users)", false)]
    public async Task ActualEmptyRequiredQueryHasDefinedCountOrExistsResult(string rule, object expected)
    {
        var source = new Mock<IDataSource>(); var destination = new Mock<IDataSource>();
        source.Setup(x => x.GetEntity("Users", It.IsAny<List<AppFilter>>())).Returns(Array.Empty<object>());
        var row = new Dictionary<string, object>();
        Assert.Equal(Errors.Ok, (await QueryRun(rule, source, destination, row)).Flag);
        Assert.Equal(expected, row["Value"]);
    }

    [Theory]
    [InlineData(Errors.Failed)]
    [InlineData(Errors.Warning)]
    [InlineData(Errors.Information)]
    [InlineData(Errors.Unknown)]
    public async Task RequiredQueryDoesNotIgnoreReportedProviderFailure(Errors flag)
    {
        var source = new Mock<IDataSource>(); var destination = new Mock<IDataSource>();
        source.Setup(x => x.GetEntity("Users", It.IsAny<List<AppFilter>>())).Returns(Array.Empty<object>());
        source.SetupGet(x => x.ErrorObject).Returns(new ErrorsInfo { Flag = flag, Message = Secret });
        var result = await QueryRun(":COUNT(Users)", source, destination, new Dictionary<string, object>());
        Assert.Equal(Errors.Failed, result.Flag); Assert.Equal(0, result.WriteAttempts);
    }

    [Theory]
    [InlineData("string")]
    [InlineData("bool")]
    [InlineData("object")]
    [InlineData("nan")]
    [InlineData("infinity")]
    [InlineData("missing")]
    [InlineData("ambiguous")]
    [InlineData("nullrow")]
    public async Task RequiredAggregateDoesNotSilentlySkipInvalidRowsOrValues(string kind)
    {
        var bad = kind switch
        {
            "missing" => new Dictionary<string, object>(),
            "ambiguous" => new Dictionary<string, object> { ["Amount"] = 3, ["amount"] = 4 },
            "nullrow" => null,
            _ => new Dictionary<string, object> { ["Amount"] = kind switch
            { "string" => "bad", "bool" => true, "nan" => double.NaN, "infinity" => double.PositiveInfinity, _ => new object() } }
        };
        var source = new Mock<IDataSource>(); var destination = new Mock<IDataSource>();
        source.Setup(x => x.GetEntity("Users", It.IsAny<List<AppFilter>>())).Returns(new object[]
            { new Dictionary<string, object> { ["Amount"] = 2 }, bad! });
        var result = await QueryRun(":SUM(Users,Amount)", source, destination, new Dictionary<string, object>());
        Assert.Equal(Errors.Failed, result.Flag); Assert.Equal(0, result.WriteAttempts);
    }

    [Theory]
    [InlineData(":MAX(Users,Amount)", 9007199254740993L)]
    [InlineData(":MIN(Users,Amount)", 9007199254740992L)]
    public async Task RequiredExtremaRetainExactIntegralFieldTypes(string rule, long expected)
    {
        var source = new Mock<IDataSource>(); var destination = new Mock<IDataSource>();
        source.Setup(x => x.GetEntity("Users", It.IsAny<List<AppFilter>>())).Returns(new object[]
        {
            new Dictionary<string, object> { ["Amount"] = 9007199254740992L },
            new Dictionary<string, object> { ["Amount"] = 9007199254740993L }
        });
        var row = new Dictionary<string, object>();
        Assert.Equal(Errors.Ok, (await QueryRun(rule, source, destination, row)).Flag);
        Assert.Equal(expected, Assert.IsType<long>(row["Value"]));
    }

    [Theory]
    [InlineData(":MAX(Users,Date)", 3)]
    [InlineData(":QUERY(aggregate,'MIN',Users,Date)", 1)]
    public async Task RequiredExtremaSupportActualDateFieldValues(string rule, int expectedDay)
    {
        var source = new Mock<IDataSource>(); var destination = new Mock<IDataSource>();
        source.Setup(x => x.GetEntity("Users", It.IsAny<List<AppFilter>>())).Returns(new object[]
        {
            new Dictionary<string, object> { ["Date"] = new DateTime(2026, 10, 3) },
            new Dictionary<string, object> { ["Date"] = new DateTime(2026, 10, 1) }
        });
        var row = new Dictionary<string, object>();
        Assert.Equal(Errors.Ok, (await QueryRun(rule, source, destination, row)).Flag);
        Assert.Equal(new DateTime(2026, 10, expectedDay), row["Value"]);
    }

    [Theory]
    [InlineData("fr-FR")]
    [InlineData("ar-KW")]
    public async Task RequiredFilterBindsActualRowInvariantlyAndPreservesQuotedParameterLiteral(string culture)
    {
        var previous = CultureInfo.CurrentCulture; CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
        try
        {
            var source = new Mock<IDataSource>(); var destination = new Mock<IDataSource>();
            List<AppFilter>? seen = null;
            source.Setup(x => x.GetEntity("Users", It.IsAny<List<AppFilter>>()))
                .Callback((string _, List<AppFilter> filters) => seen = filters)
                .Returns(new object[] { new Dictionary<string, object> { ["Email"] = "actual-email" } });
            var row = new Dictionary<string, object> { ["Amount"] = 1.25m };
            Assert.Equal(Errors.Ok, (await QueryRun(":LOOKUP(Users,Email,Amount>=@Amount,Name='@Missing',Tag='a>=b,c')",
                source, destination, row)).Flag);
            Assert.Equal("actual-email", row["Value"]);
            Assert.Equal(new[] { "Amount", "Name", "Tag" }, seen!.Select(x => x.FieldName));
            Assert.Equal(new[] { ">=", "=", "=" }, seen.Select(x => x.Operator));
            Assert.Equal(new[] { "1.25", "@Missing", "a>=b,c" }, seen.Select(x => x.FilterValue));
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [Fact]
    public async Task RequiredQueryParsesAllFiltersBeforeContextConversionCallbacks()
    {
        var value = new QueryObserverValue(); var source = new Mock<IDataSource>(); var destination = new Mock<IDataSource>();
        var result = await QueryRun(":LOOKUP(Users,Email,Age=@Input,garbage)", source, destination,
            new Dictionary<string, object>(), new() { new() { Name = "Input", obj = value } });
        Assert.Equal(Errors.Failed, result.Flag); Assert.Equal(0, value.Reads);
        source.Verify(x => x.GetEntity(It.IsAny<string>(), It.IsAny<List<AppFilter>>()), Times.Never);
    }

    private sealed class QueryObserverValue
    {
        public int Reads { get; private set; }
        public override string ToString() { Reads++; throw new IOException(Secret); }
    }

    [Theory]
    [InlineData(":SUM(Users,Amount)", 3.75d)]
    [InlineData(":AVG(Users,Amount)", 1.875d)]
    public async Task RequiredNumericAggregateUsesActualDecimalsAndOnlySkipsRealNullFields(string rule, double expected)
    {
        var source = new Mock<IDataSource>(); var destination = new Mock<IDataSource>();
        source.Setup(x => x.GetEntity("Users", It.IsAny<List<AppFilter>>())).Returns(new object[]
        {
            new Dictionary<string, object> { ["Amount"] = 1.25m },
            new Dictionary<string, object> { ["Amount"] = null! },
            new Dictionary<string, object> { ["Amount"] = 2.5m }
        });
        var row = new Dictionary<string, object>();
        Assert.Equal(Errors.Ok, (await QueryRun(rule, source, destination, row)).Flag);
        Assert.Equal(expected, row["Value"]);
    }

    [Theory]
    [InlineData(":MAX(Users,Amount)")]
    [InlineData(":MIN(Users,Amount)")]
    [InlineData(":SUM(Users,Amount)")]
    [InlineData(":AVG(Users,Amount)")]
    [InlineData(":LOOKUP(Users,Amount)")]
    [InlineData(":GETENTITY(Users)")]
    public async Task RequiredValueQueryDoesNotInventAValueFromEmptyResults(string rule)
    {
        var source = new Mock<IDataSource>(); var destination = new Mock<IDataSource>();
        source.Setup(x => x.GetEntity("Users", It.IsAny<List<AppFilter>>())).Returns(Array.Empty<object>());
        var result = await QueryRun(rule, source, destination, new Dictionary<string, object>());
        Assert.Equal(Errors.Failed, result.Flag); Assert.Equal(0, result.WriteAttempts);
    }

    [Theory]
    [InlineData("decimaloverflow")]
    [InlineData("floatoverflow")]
    [InlineData("numericstring")]
    [InlineData("incompatibleextrema")]
    public async Task RequiredAggregateRejectsOverflowCoercionAndIncompatibleOrdering(string kind)
    {
        var first = kind switch { "decimaloverflow" => (object)decimal.MaxValue, "floatoverflow" => double.MaxValue, _ => 1 };
        var second = kind switch { "floatoverflow" => (object)double.MaxValue, "numericstring" or "incompatibleextrema" => "2", _ => 1 };
        var source = new Mock<IDataSource>(); var destination = new Mock<IDataSource>();
        source.Setup(x => x.GetEntity("Users", It.IsAny<List<AppFilter>>())).Returns(new object[]
        { new Dictionary<string, object> { ["Amount"] = first }, new Dictionary<string, object> { ["Amount"] = second } });
        var result = await QueryRun(kind == "incompatibleextrema" ? ":MAX(Users,Amount)" : ":SUM(Users,Amount)",
            source, destination, new Dictionary<string, object>());
        Assert.Equal(Errors.Failed, result.Flag); Assert.Equal(0, result.WriteAttempts);
    }

    [Theory]
    [InlineData("ambiguous")]
    [InlineData("null")]
    [InlineData("object")]
    public async Task RequiredBindingRejectsAmbiguousNullAndObserverValuesBeforeProviderRead(string kind)
    {
        var observer = new QueryObserverValue();
        var objects = new List<ObjectItem> { new() { Name = "Input", obj = kind switch { "object" => observer, "null" => null!, _ => 1 } } };
        if (kind == "ambiguous") objects.Add(new() { Name = "input", obj = 2 });
        var source = new Mock<IDataSource>(); var destination = new Mock<IDataSource>();
        var result = await QueryRun(":COUNT(Users,Id=@Input)", source, destination, new Dictionary<string, object>(), objects);
        Assert.Equal(Errors.Failed, result.Flag); Assert.Equal(0, observer.Reads);
        source.Verify(x => x.GetEntity(It.IsAny<string>(), It.IsAny<List<AppFilter>>()), Times.Never);
    }

    [Fact]
    public async Task RequiredFilterOperatorScanningRespectsQuotedFieldAndValuePunctuation()
    {
        var source = new Mock<IDataSource>(); var destination = new Mock<IDataSource>(); List<AppFilter>? seen = null;
        source.Setup(x => x.GetEntity("Users", It.IsAny<List<AppFilter>>()))
            .Callback((string _, List<AppFilter> filters) => seen = filters).Returns(Array.Empty<object>());
        var row = new Dictionary<string, object>();
        Assert.Equal(Errors.Ok, (await QueryRun(":COUNT(Users,'Name=Code'='a!=b',Blank='',Code!=@Code)", source, destination,
            row, new() { new() { Name = "Code", obj = 17 } })).Flag);
        Assert.Equal(new[] { "Name=Code", "Blank", "Code" }, seen!.Select(x => x.FieldName));
        Assert.Equal(new[] { "a!=b", "", "17" }, seen.Select(x => x.FilterValue));
        Assert.Equal(new[] { "=", "=", "!=" }, seen.Select(x => x.Operator));
    }

    [Fact]
    public async Task RequiredQueryFilterCountLimitDeniesBeforeProviderRead()
    {
        var source = new Mock<IDataSource>(); var destination = new Mock<IDataSource>();
        var result = await QueryRun(":COUNT(Users," + string.Join(",", Enumerable.Repeat("Id=1", 257)) + ")",
            source, destination, new Dictionary<string, object>());
        Assert.Equal(Errors.Failed, result.Flag);
        source.Verify(x => x.GetEntity(It.IsAny<string>(), It.IsAny<List<AppFilter>>()), Times.Never);
    }

    [Theory]
    [InlineData(100000, true)]
    [InlineData(100001, false)]
    public async Task RequiredQueryBoundsConsumedRowsWithoutMaterializingResults(int count, bool succeeds)
    {
        var source = new Mock<IDataSource>(); var destination = new Mock<IDataSource>(); var reads = 0;
        IEnumerable<object> Rows()
        {
            for (var i = 0; i < count; i++)
            {
                reads++;
                // Keep the recording mock's call history out of the consumer-memory bound.
                if (i % 1000 == 0) source.Invocations.Clear();
                yield return new object();
            }
        }
        source.Setup(x => x.GetEntity("Users", It.IsAny<List<AppFilter>>())).Returns(Rows());
        var row = new Dictionary<string, object>();
        var result = await QueryRun(":COUNT(Users)", source, destination, row);
        Assert.Equal(succeeds ? Errors.Ok : Errors.Failed, result.Flag);
        Assert.Equal(count, reads);
        if (succeeds) Assert.Equal(count, row["Value"]);
        else { Assert.False(row.ContainsKey("Value")); Assert.Equal(0, result.WriteAttempts); }
    }

    [Theory]
    [InlineData("move")]
    [InlineData("current")]
    [InlineData("dispose")]
    [InlineData("status")]
    public async Task RequiredQueryEnumerationAndDisposeFailuresNeverBecomeAcknowledgedValues(string kind)
    {
        var source = new Mock<IDataSource>(); var destination = new Mock<IDataSource>();
        var status = new ErrorsInfo(); source.SetupGet(x => x.ErrorObject).Returns(status);
        var cursor = new QueryCursor(kind, kind == "status" ? () => status.Flag = Errors.Failed : null);
        source.Setup(x => x.GetEntity("Users", It.IsAny<List<AppFilter>>())).Returns(cursor);
        var result = await QueryRun(":COUNT(Users)", source, destination, new Dictionary<string, object>());
        Assert.Equal(Errors.Failed, result.Flag); Assert.Equal(0, result.WriteAttempts);
        Assert.Equal(1, cursor.Disposals);
        if (kind == "status") Assert.Equal(1, cursor.Moves);
    }

    private sealed class QueryCursor(string kind, Action? afterCurrent = null) : IEnumerable<object>, IEnumerator<object>
    {
        public int Moves { get; private set; }
        public int Reads { get; private set; }
        public int Disposals { get; private set; }
        public object Current
        {
            get
            {
                Reads++;
                if (kind == "current") throw new IOException(Secret);
                afterCurrent?.Invoke();
                return new Dictionary<string, object> { ["Amount"] = 2 };
            }
        }
        public bool MoveNext() { Moves++; if (kind == "move" && Moves == 2) throw new IOException(Secret); return Moves <= 2; }
        public void Dispose() { Disposals++; if (kind == "dispose") throw new IOException(Secret); }
        public void Reset() => throw new NotSupportedException();
        public IEnumerator<object> GetEnumerator() => this;
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [Fact]
    public async Task RequiredQueryProviderDenialStopsSubsequentDiagnosticGetters()
    {
        var source = new Mock<IDataSource>(); var destination = new Mock<IDataSource>();
        var status = new Mock<IErrorsInfo>(); status.SetupGet(x => x.Flag).Returns(Errors.Failed);
        status.SetupGet(x => x.Ex).Throws(new IOException(Secret));
        source.SetupGet(x => x.ErrorObject).Returns(status.Object);
        source.Setup(x => x.GetEntity("Users", It.IsAny<List<AppFilter>>())).Returns(Array.Empty<object>());
        var result = await QueryRun(":COUNT(Users)", source, destination, new Dictionary<string, object>());
        Assert.Equal(Errors.Failed, result.Flag);
        status.VerifyGet(x => x.Ex, Times.Never);
    }

    [Fact]
    public async Task RequiredLookupCopiesBinaryScalarAndUsesDataRowFieldAccess()
    {
        var table = new System.Data.DataTable(); table.Columns.Add("Blob", typeof(byte[]));
        var bytes = new byte[] { 1, 2 }; var record = table.Rows.Add(bytes);
        var source = new Mock<IDataSource>(); var destination = new Mock<IDataSource>();
        source.Setup(x => x.GetEntity("Users", It.IsAny<List<AppFilter>>())).Returns(new object[] { record });
        var row = new Dictionary<string, object>();
        Assert.Equal(Errors.Ok, (await QueryRun(":LOOKUP(Users,Blob)", source, destination, row)).Flag);
        var actual = Assert.IsType<byte[]>(row["Value"]); Assert.Equal(bytes, actual); Assert.NotSame(bytes, actual);
    }

    [Fact]
    public async Task RequiredQueryCanComposeWithTheCapturedExpressionResolver()
    {
        var source = new Mock<IDataSource>(); var destination = new Mock<IDataSource>(); List<AppFilter>? seen = null;
        source.Setup(x => x.GetEntity("Users", It.IsAny<List<AppFilter>>()))
            .Callback((string _, List<AppFilter> filters) => seen = filters)
            .Returns(new object[] { new Dictionary<string, object> { ["Amount"] = 17 } });
        var row = new Dictionary<string, object> { ["Id"] = 1 };
        Assert.Equal(Errors.Ok, (await QueryRun(":EXPRESSION(LOOKUP('Users','Amount','Id=@Id')+1)", source, destination, row)).Flag);
        Assert.Equal(18d, row["Value"]); Assert.Equal("1", Assert.Single(seen!).FilterValue);
    }

    [Fact]
    public async Task RequiredCustomQueryOverrideRetainsItsOwnSemanticContract()
    {
        var editor = Mock.Of<IDMEEditor>(); DefaultsManager.RegisterCustomResolver(editor, new CustomQueryResolver(editor));
        var row = new Dictionary<string, object>(); var source = new Mock<IDataSource>(); var destination = new Mock<IDataSource>();
        Assert.Equal(Errors.Ok, (await QueryRun(":LOOKUP(Users,Email,plugin-specific-filter)", source, destination, row, editor: editor)).Flag);
        Assert.Equal(17, row["Value"]);
    }

    private sealed class CustomQueryResolver(IDMEEditor editor) : DataSourceResolver(editor)
    {
        public override object ResolveValue(string rule, IPassedArgs parameters) => 17;
    }

    [Fact]
    public async Task RequiredQueryChecksCancellationImmediatelyAfterProviderCallback()
    {
        using var cancellation = new CancellationTokenSource();
        var source = new Mock<IDataSource>(); var destination = new Mock<IDataSource>(); var reads = 0;
        IEnumerable<object> Rows() { reads++; yield return new Dictionary<string, object> { ["Amount"] = 2 }; }
        source.Setup(x => x.GetEntity("Users", It.IsAny<List<AppFilter>>()))
            .Callback(() => cancellation.Cancel()).Returns(Rows());
        var result = await QueryRun(":COUNT(Users)", source, destination, new Dictionary<string, object>(), token: cancellation.Token);
        Assert.Equal(ImportOutcome.Cancelled, result.Outcome); Assert.Equal(0, result.WriteAttempts); Assert.Equal(0, reads);
    }

    [Fact]
    public async Task RequiredQueryCancellationDuringEnumerationStopsBeforeAnotherProviderCallback()
    {
        using var cancellation = new CancellationTokenSource();
        var cursor = new QueryCursor("cancel", cancellation.Cancel);
        var source = new Mock<IDataSource>(); var destination = new Mock<IDataSource>();
        source.Setup(x => x.GetEntity("Users", It.IsAny<List<AppFilter>>())).Returns(cursor);
        var row = new Dictionary<string, object>();
        var result = await QueryRun(":COUNT(Users)", source, destination, row, token: cancellation.Token);
        Assert.Equal(ImportOutcome.Cancelled, result.Outcome); Assert.Equal(0, result.WriteAttempts);
        Assert.Equal(1, cursor.Moves); Assert.Equal(1, cursor.Reads); Assert.Equal(1, cursor.Disposals);
        Assert.False(row.ContainsKey("Value"));
    }

    [Fact]
    public async Task RequiredQueryDoesNotIgnoreProviderExceptionBehindOkFlag()
    {
        var source = new Mock<IDataSource>(); var destination = new Mock<IDataSource>();
        source.Setup(x => x.GetEntity("Users", It.IsAny<List<AppFilter>>())).Returns(Array.Empty<object>());
        source.SetupGet(x => x.ErrorObject).Returns(new ErrorsInfo { Flag = Errors.Ok, Ex = new IOException(Secret) });
        var result = await QueryRun(":COUNT(Users)", source, destination, new Dictionary<string, object>());
        Assert.Equal(Errors.Failed, result.Flag); Assert.Equal(0, result.WriteAttempts);
    }

    [Fact]
    public async Task RequiredQueryBindingRetainsExplicitNamedRecordCompatibility()
    {
        var editor = Mock.Of<IDMEEditor>(); var source = new Mock<IDataSource>(); var destination = new Mock<IDataSource>();
        List<AppFilter>? seen = null;
        source.Setup(x => x.GetEntity("Users", It.IsAny<List<AppFilter>>()))
            .Callback((string _, List<AppFilter> filters) => seen = filters).Returns(Array.Empty<object>());
        DefaultsManager.RegisterCustomResolver(editor, new ProbeResolver(editor, _ =>
            DefaultsManager.Resolve(editor, ":COUNT(Users,Id=@Id)", new PassedArgs
            {
                DataSource = source.Object,
                Objects = new() { new() { Name = "Record", obj = new Dictionary<string, object> { ["Id"] = 17 } } }
            })));
        var row = new Dictionary<string, object>();
        Assert.Equal(Errors.Ok, (await Run(editor, Config(destination, ":PROBE()"), row)).Flag);
        Assert.Equal(0, row["Value"]); Assert.Equal("17", Assert.Single(seen!).FilterValue);
    }

    [Theory]
    [InlineData(":GETENTITY(Users)")]
    [InlineData(":QUERY(exists,Users)")]
    [InlineData(":LOOKUP(Users,Amount)")]
    public async Task RequiredFirstExistsAndScalarConsumeOnlyTheFirstRowAndDispose(string rule)
    {
        var cursor = new QueryCursor("move");
        var source = new Mock<IDataSource>(); var destination = new Mock<IDataSource>();
        source.Setup(x => x.GetEntity("Users", It.IsAny<List<AppFilter>>())).Returns(cursor);
        var row = new Dictionary<string, object>();
        Assert.Equal(Errors.Ok, (await QueryRun(rule, source, destination, row)).Flag);
        Assert.Equal(1, cursor.Moves); Assert.Equal(1, cursor.Reads); Assert.Equal(1, cursor.Disposals);
        if (rule.Contains("exists", StringComparison.Ordinal)) Assert.Equal(true, row["Value"]);
        else if (rule.Contains("LOOKUP", StringComparison.Ordinal)) Assert.Equal(2, row["Value"]);
        else Assert.Equal(2, Assert.IsType<Dictionary<string, object>>(row["Value"])["Amount"]);
    }

    [Fact]
    public async Task RequiredQueryFailureAfterAcknowledgedRowKeepsPartialCountsWithoutReplay()
    {
        var editor = Mock.Of<IDMEEditor>(); var source = new Mock<IDataSource>(); var destination = new Mock<IDataSource>();
        source.SetupSequence(x => x.GetEntity("Users", It.IsAny<List<AppFilter>>()))
            .Returns(Array.Empty<object>()).Throws(new IOException(Secret));
        DefaultsManager.RegisterCustomResolver(editor, new ProbeResolver(editor, _ =>
            DefaultsManager.Resolve(editor, ":COUNT(Users)", new PassedArgs { DataSource = source.Object })));
        var first = new Dictionary<string, object>(); var second = new Dictionary<string, object>();
        var result = await Run(editor, Config(destination, ":PROBE()"), first, second);
        Assert.Equal(ImportOutcome.Partial, result.Outcome); Assert.Equal(1, result.RecordsSucceeded);
        Assert.Equal(1, result.RecordsTransformationFailed); Assert.Equal(1, result.WriteAttempts);
        Assert.False(result.HasUncertainWrites); Assert.Equal(0, first["Value"]); Assert.False(second.ContainsKey("Value"));
        destination.Verify(x => x.InsertEntity("target", first), Times.Once);
        destination.Verify(x => x.InsertEntity("target", second), Times.Never);
    }

    [Fact]
    public void LegacyQueryCountStillTreatsNullAsZeroOutsideRequiredScope()
    {
        var source = new Mock<IDataSource>();
        source.Setup(x => x.GetEntity("Users", It.IsAny<List<AppFilter>>())).Returns((IEnumerable<object>)null!);
        Assert.Equal(0, DefaultsManager.Resolve(Mock.Of<IDMEEditor>(), ":QUERY(count,Users)",
            new PassedArgs { DataSource = source.Object }));
    }
}
