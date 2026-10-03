using System.Globalization;
using Moq;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Defaults;
using TheTechIdea.Beep.Editor.Defaults.Resolvers;
using TheTechIdea.Beep.Editor.Importing;
using Xunit;

namespace TheTechIdea.Beep.Framework.Tests;

public partial class ImportRequiredResolverTests
{
    [Theory]
    [InlineData("en-US", "01/02/2026")]
    [InlineData("fr-FR", "01/02/2026")]
    [InlineData("ar-KW", "2026/01/02")]
    [InlineData("en-US", "January 2, 2026")]
    [InlineData("fr-FR", "2 janvier 2026")]
    [InlineData("en-US", "2026-1-2")]
    [InlineData("en-US", "2026-01-02 12:30:00")]
    [InlineData("en-US", "2026-01-02T12:30")]
    [InlineData("en-US", "2026-01-02T12:30:00.12345678")]
    [InlineData("en-US", "2026-01-02T12:30:00+15:00")]
    [InlineData("en-US", "2026-02-29")]
    [InlineData("en-US", "2026-01-02T24:00:00")]
    public async Task RequiredDatesDenyNonIsoOrInvalidLiteralWithoutCultureFallback(string culture, string literal)
    {
        var old = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            var destination = new Mock<IDataSource>();
            var result = await Run(Mock.Of<IDMEEditor>(), Config(destination, $":ADDDAYS('{literal}',1)"), new Dictionary<string, object>());
            Assert.Equal(Errors.Failed, result.Flag); Assert.Equal(0, result.WriteAttempts);
            Assert.Equal(1, result.RecordsTransformationFailed); Assert.False(result.HasUncertainWrites);
        }
        finally { CultureInfo.CurrentCulture = old; }
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("fr-FR")]
    [InlineData("ar-KW")]
    public async Task RequiredDateFormattingIsInvariant(string culture)
    {
        var old = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            var row = new Dictionary<string, object>();
            var result = await Run(Mock.Of<IDMEEditor>(), Config(new Mock<IDataSource>(),
                ":DATEFORMAT('2026-01-02','dddd, MMMM dd yyyy')"), row);
            Assert.Equal(Errors.Ok, result.Flag); Assert.Equal("Friday, January 02 2026", row["Value"]);
        }
        finally { CultureInfo.CurrentCulture = old; }
    }

    [Theory]
    [InlineData(":ADDDAYS('2026-01-02',0.5)", "2026-01-02T12:00:00")]
    [InlineData(":ADDHOURS('2026-01-02',1.25)", "2026-01-02T01:15:00")]
    [InlineData(":ADDMINUTES('2026-01-02',-0.5)", "2026-01-01T23:59:30")]
    [InlineData(":ADDDAYS('2024-02-28',1)", "2024-02-29T00:00:00")]
    [InlineData(":ADDMONTHS('2026-01-31',1)", "2026-02-28T00:00:00")]
    [InlineData(":ADDYEARS('2024-02-29',1)", "2025-02-28T00:00:00")]
    [InlineData(":ADDDAYS(ADDMONTHS('2026-01-31',1),1)", "2026-03-01T00:00:00")]
    [InlineData(":ADDHOURS('2026-01-02T12:30:45.1234567',0)", "2026-01-02T12:30:45.1234567")]
    public async Task RequiredDateOffsetsPreserveExactCalendarAndFractionalMeaning(string rule, string expected)
    {
        var row = new Dictionary<string, object>();
        Assert.Equal(Errors.Ok, (await Run(Mock.Of<IDMEEditor>(), Config(new Mock<IDataSource>(), rule), row)).Flag);
        var value = Assert.IsType<DateTime>(row["Value"]);
        Assert.Equal(DateTime.Parse(expected, CultureInfo.InvariantCulture), value);
        Assert.Equal(DateTimeKind.Unspecified, value.Kind);
    }

    [Fact]
    public async Task RequiredDateUtcLiteralRetainsUtcKindAndTicks()
    {
        var row = new Dictionary<string, object>();
        Assert.Equal(Errors.Ok, (await Run(Mock.Of<IDMEEditor>(), Config(new Mock<IDataSource>(),
            ":ADDMINUTES('2026-01-02T12:30:45.1234567Z',1)"), row)).Flag);
        var value = Assert.IsType<DateTime>(row["Value"]);
        Assert.Equal(new DateTime(2026, 1, 2, 12, 31, 45, DateTimeKind.Utc).AddTicks(1234567), value);
        Assert.Equal(DateTimeKind.Utc, value.Kind);
    }

    [Theory]
    [InlineData("+03:00")]
    [InlineData("-05:30")]
    [InlineData("+14:00")]
    public async Task RequiredDateOffsetLiteralPreservesOffsetRatherThanHostConversion(string offset)
    {
        var row = new Dictionary<string, object>();
        Assert.Equal(Errors.Ok, (await Run(Mock.Of<IDMEEditor>(), Config(new Mock<IDataSource>(),
            $":ADDHOURS('2026-01-02T12:30:45.1234567{offset}',1)"), row)).Flag);
        var value = Assert.IsType<DateTimeOffset>(row["Value"]);
        Assert.Equal(DateTimeOffset.Parse("2026-01-02T13:30:45.1234567" + offset, CultureInfo.InvariantCulture), value);
        Assert.Equal(DateTimeOffset.Parse("2026-01-02T12:30:45" + offset, CultureInfo.InvariantCulture).Offset, value.Offset);
    }

    [Theory]
    [InlineData(":ADDDAYS('9999-12-31',1)")]
    [InlineData(":ADDMONTHS('0001-01-01',-1)")]
    [InlineData(":ADDYEARS('2026-01-01',2147483648)")]
    [InlineData(":ADDMONTHS('2026-01-01',1.5)")]
    [InlineData(":ADDDAYS('2026-01-01',NaN)")]
    [InlineData(":ADDDAYS('2026-01-01',Infinity)")]
    [InlineData(":ADDDAYS('2026-01-01',1e3)")]
    [InlineData(":ADDDAYS('2026-01-01',0.00000000000000000000000000001)")]
    [InlineData(":ADDHOURS('2026-01-01',0.000000000001)")]
    [InlineData(":ADDMINUTES('2026-01-01',0.0000000001)")]
    [InlineData(":FORMAT('2026-01-01','Q')")]
    [InlineData(":FORMAT('2026-01-01',\"yyyy'unclosed\")")]
    public async Task RequiredDateArithmeticAndFormatDenyUnrepresentableMeaning(string rule)
    {
        var result = await Run(Mock.Of<IDMEEditor>(), Config(new Mock<IDataSource>(), rule), new Dictionary<string, object>());
        Assert.Equal(Errors.Failed, result.Flag); Assert.Equal(0, result.WriteAttempts);
    }

    [Theory]
    [InlineData(":ADDDAYS(DATESTAMP,not-a-number)")]
    [InlineData(":FORMAT(DATESTAMP,'Q')")]
    [InlineData(":ADDDAYS(ADDHOURS(DATESTAMP,bad),1)")]
    [InlineData(":FORMAT(ADDMONTHS(DATESTAMP,1.5),'O')")]
    public async Task RequiredDatePlanDeniesKnownArgumentsBeforeNestedCallbacks(string rule)
    {
        var reads = 0; var editor = Mock.Of<IDMEEditor>();
        DefaultsManager.RegisterCustomResolver(editor, new DateStampResolver(editor, _ => { reads++; return new DateTime(2026, 1, 2); }));
        var result = await Run(editor, Config(new Mock<IDataSource>(), rule), new Dictionary<string, object>());
        Assert.Equal(Errors.Failed, result.Flag); Assert.Equal(0, result.WriteAttempts); Assert.Equal(0, reads);
    }

    [Fact]
    public async Task RequiredDateFormatLimitDeniesBeforeNestedCallbacks()
    {
        var reads = 0; var editor = Mock.Of<IDMEEditor>();
        DefaultsManager.RegisterCustomResolver(editor, new DateStampResolver(editor, _ => { reads++; return DateTime.Today; }));
        var result = await Run(editor, Config(new Mock<IDataSource>(), ":FORMAT(DATESTAMP,'" + new string('y', 1025) + "')"), new Dictionary<string, object>());
        Assert.Equal(Errors.Failed, result.Flag); Assert.Equal(0, result.WriteAttempts); Assert.Equal(0, reads);
    }

    [Theory]
    [InlineData("date")]
    [InlineData("offset")]
    public async Task RequiredDateNestedResolverUsesActualTypedValueAndRowContext(string kind)
    {
        var editor = Mock.Of<IDMEEditor>(); var row = new Dictionary<string, object> { ["Input"] = kind == "date"
            ? (object)new DateTime(2026, 1, 2, 12, 0, 0, DateTimeKind.Utc)
            : new DateTimeOffset(2026, 1, 2, 12, 0, 0, TimeSpan.FromHours(3)) };
        DefaultsManager.RegisterCustomResolver(editor, new DateStampResolver(editor, args =>
            ((Dictionary<string, object>)args.ReturnData)["Input"]));
        if (kind == "date") Assert.IsType<DateTime>(row["Input"]);
        else Assert.IsType<DateTimeOffset>(row["Input"]);
        Assert.Equal(Errors.Ok, (await Run(editor, Config(new Mock<IDataSource>(), ":ADDDAYS(DATESTAMP,1)"), row)).Flag);
        if (kind == "date") { var value = Assert.IsType<DateTime>(row["Value"]); Assert.Equal(DateTimeKind.Utc, value.Kind); Assert.Equal(3, value.Day); }
        else { var value = Assert.IsType<DateTimeOffset>(row["Value"]); Assert.Equal(TimeSpan.FromHours(3), value.Offset); Assert.Equal(3, value.Day); }
    }

    [Theory]
    [InlineData("string")]
    [InlineData("number")]
    [InlineData("observer")]
    [InlineData("time")]
    [InlineData("dateonly")]
    public async Task RequiredDateNestedValueCannotBeReparsedOrObserverConverted(string kind)
    {
        var editor = Mock.Of<IDMEEditor>(); var observer = new DateObserverValue();
        object value = kind switch { "string" => "2026-01-02", "number" => 1, "observer" => observer,
            "time" => TimeSpan.FromHours(1), _ => new DateOnly(2026, 1, 2) };
        DefaultsManager.RegisterCustomResolver(editor, new DateStampResolver(editor, _ => value));
        var result = await Run(editor, Config(new Mock<IDataSource>(), ":ADDDAYS(DATESTAMP,1)"), new Dictionary<string, object>());
        Assert.Equal(Errors.Failed, result.Flag); Assert.Equal(0, result.WriteAttempts); Assert.Equal(0, observer.Reads);
    }

    [Fact]
    public async Task RequiredDateNestedTokenOverrideRetainsPinnedRosterAcrossRows()
    {
        var editor = Mock.Of<IDMEEditor>(); var reads = 0; var replacementReads = 0;
        DefaultsManager.RegisterCustomResolver(editor, new DateStampResolver(editor, _ =>
        {
            reads++;
            DefaultsManager.RegisterCustomResolver(editor, new DateStampResolver(editor, _ => { replacementReads++; return new DateTime(2040, 1, 1); }, "TODAY"));
            return new DateTime(2026, 1, 2);
        }, "TODAY"));
        var first = new Dictionary<string, object>(); var second = new Dictionary<string, object>();
        Assert.Equal(Errors.Ok, (await Run(editor, Config(new Mock<IDataSource>(), ":ADDDAYS(TODAY,1)"), first, second)).Flag);
        Assert.Equal(new DateTime(2026, 1, 3), first["Value"]); Assert.Equal(first["Value"], second["Value"]);
        Assert.Equal(2, reads); Assert.Equal(0, replacementReads);
    }

    [Fact]
    public async Task RequiredDateNestedReportedFailureKeepsAcknowledgedPrefixPartial()
    {
        var editor = Mock.Of<IDMEEditor>(); var first = new Dictionary<string, object> { ["Value"] = "already supplied" };
        DefaultsManager.RegisterCustomResolver(editor, new DateStampResolver(editor, _ => new DateTime(2026, 1, 2), warning: true));
        Mock.Get(editor).Invocations.Clear();
        var destination = new Mock<IDataSource>();
        var result = await Run(editor, Config(destination, ":ADDDAYS(DATESTAMP,1)"), first, new Dictionary<string, object>());
        Assert.Equal(ImportOutcome.Partial, result.Outcome); Assert.Equal(1, result.RecordsSucceeded);
        Assert.Equal(1, result.RecordsTransformationFailed); Assert.Equal(1, result.WriteAttempts);
        destination.Verify(x => x.InsertEntity("target", first), Times.Once);
        Mock.Get(editor).Verify(x => x.AddLogMessage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(),
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<Errors>()), Times.Never);
    }

    [Fact]
    public async Task RequiredDateNestedCancellationPreventsDestinationWrite()
    {
        using var cancellation = new CancellationTokenSource(); var editor = Mock.Of<IDMEEditor>();
        DefaultsManager.RegisterCustomResolver(editor, new DateStampResolver(editor, _ => { cancellation.Cancel(); return DateTime.Today; }));
        var destination = new Mock<IDataSource>(); var configuration = Config(destination, ":ADDDAYS(DATESTAMP,1)");
        var result = await new TheTechIdea.Beep.Editor.Importing.Helpers.DataImportBatchHelper(editor,
            new TheTechIdea.Beep.Editor.Importing.Helpers.DataImportTransformationHelper(editor),
            Mock.Of<TheTechIdea.Beep.Editor.Importing.Interfaces.IDataImportProgressHelper>())
            .ProcessBatchDetailedAsync(new object[] { new Dictionary<string, object>() }, configuration, null!, cancellation.Token);
        Assert.Equal(ImportOutcome.Cancelled, result.Outcome); Assert.Equal(0, result.WriteAttempts);
    }

    [Fact]
    public void LegacyDateDirectCallRetainsCultureParsing()
    {
        var old = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            Assert.Equal(new DateTime(2026, 1, 3), new DateTimeResolver(Mock.Of<IDMEEditor>()).ResolveValue("ADDDAYS('01/02/2026',1)", new PassedArgs()));
        }
        finally { CultureInfo.CurrentCulture = old; }
    }

    private sealed class DateObserverValue
    {
        public int Reads { get; private set; }
        public override string ToString() { Reads++; throw new IOException(Secret); }
    }

    [Theory]
    [InlineData("ADDDAYS", "0.000000025", 21600L)]
    [InlineData("ADDHOURS", "0.000000001", 36L)]
    [InlineData("ADDMINUTES", "0.000000005", 3L)]
    public async Task RequiredDateFractionsRetainExactSubmillisecondTicks(string operation, string amount, long ticks)
    {
        var row = new Dictionary<string, object>();
        Assert.Equal(Errors.Ok, (await Run(Mock.Of<IDMEEditor>(), Config(new Mock<IDataSource>(),
            $":{operation}('2026-01-02T00:00:00Z',{amount})"), row)).Flag);
        var actual = Assert.IsType<DateTime>(row["Value"]);
        Assert.Equal(new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc).AddTicks(ticks), actual);
        Assert.Equal(DateTimeKind.Utc, actual.Kind);
    }

    [Theory]
    [InlineData("NOW")]
    [InlineData("CURRENTDATETIME")]
    [InlineData("UTCNOW")]
    [InlineData("CURRENTUTCDATETIME")]
    [InlineData("UTCTODAY")]
    [InlineData("TODAY")]
    [InlineData("CURRENTDATE")]
    [InlineData("YESTERDAY")]
    [InlineData("TOMORROW")]
    [InlineData("CURRENTTIME")]
    [InlineData("STARTOFMONTH")]
    [InlineData("ENDOFMONTH")]
    [InlineData("STARTOFYEAR")]
    [InlineData("ENDOFYEAR")]
    [InlineData("STARTOFWEEK")]
    [InlineData("ENDOFWEEK")]
    public async Task RequiredDateClockAliasesHaveActualTimeAndExplicitKind(string operation)
    {
        var before = DateTimeOffset.UtcNow; var row = new Dictionary<string, object>();
        Assert.Equal(Errors.Ok, (await Run(Mock.Of<IDMEEditor>(), Config(new Mock<IDataSource>(), ":" + operation), row)).Flag);
        var after = DateTimeOffset.UtcNow;
        if (operation == "CURRENTTIME")
        {
            var time = Assert.IsType<TimeSpan>(row["Value"]);
            Assert.InRange(time, TimeSpan.Zero, TimeSpan.FromDays(1));
            Assert.True(time >= before.LocalDateTime.TimeOfDay && time <= after.LocalDateTime.TimeOfDay || before.LocalDateTime.Date != after.LocalDateTime.Date);
            return;
        }
        var date = Assert.IsType<DateTime>(row["Value"]);
        var utc = operation is "UTCNOW" or "CURRENTUTCDATETIME" or "UTCTODAY";
        Assert.Equal(utc ? DateTimeKind.Utc : DateTimeKind.Local, date.Kind);
        if (operation is "NOW" or "CURRENTDATETIME" or "UTCNOW" or "CURRENTUTCDATETIME")
        { Assert.InRange(date.ToUniversalTime(), before.UtcDateTime, after.UtcDateTime); return; }
        DateTime Anchor(DateTime now)
        {
            var today = now.Date;
            return operation switch
            {
                "YESTERDAY" => today.AddDays(-1), "TOMORROW" => today.AddDays(1),
                "STARTOFMONTH" => new DateTime(today.Year, today.Month, 1),
                "ENDOFMONTH" => new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month)),
                "STARTOFYEAR" => new DateTime(today.Year, 1, 1), "ENDOFYEAR" => new DateTime(today.Year, 12, 31),
                "STARTOFWEEK" => today.AddDays(-((7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7)),
                "ENDOFWEEK" => today.AddDays(6 - ((7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7)),
                _ => today
            };
        }
        Assert.True(date == Anchor(utc ? before.UtcDateTime : before.LocalDateTime) || date == Anchor(utc ? after.UtcDateTime : after.LocalDateTime));
    }

    [Fact]
    public async Task RequiredDateCanConsumeActualRowPropertyWithoutStringConversion()
    {
        var row = new Dictionary<string, object> { ["Input"] = new DateTime(2026, 1, 2, 3, 0, 0, DateTimeKind.Local) };
        Assert.Equal(Errors.Ok, (await Run(Mock.Of<IDMEEditor>(), Config(new Mock<IDataSource>(), ":ADDDAYS(PROPERTY(Input),1)"), row)).Flag);
        var date = Assert.IsType<DateTime>(row["Value"]);
        Assert.Equal(new DateTime(2026, 1, 3, 3, 0, 0, DateTimeKind.Local), date); Assert.Equal(DateTimeKind.Local, date.Kind);
    }

    [Theory]
    [InlineData("2026-01-02T12:30:45.1234567Z", "2026-01-02T12:30:45.1234567Z")]
    [InlineData("2026-01-02T12:30:45.1234567+03:00", "2026-01-02T12:30:45.1234567+03:00")]
    [InlineData("2026-01-02T12:30:45.1234567", "2026-01-02T12:30:45.1234567")]
    public async Task RequiredDateRoundtripFormattingRetainsKindAndOffset(string literal, string expected)
    {
        var row = new Dictionary<string, object>();
        Assert.Equal(Errors.Ok, (await Run(Mock.Of<IDMEEditor>(), Config(new Mock<IDataSource>(), $":FORMAT('{literal}','O')"), row)).Flag);
        Assert.Equal(expected, row["Value"]);
    }

    [Fact]
    public async Task RequiredDateKnownBareTokenArgumentsDenyBeforeNestedCallback()
    {
        var reads = 0; var editor = Mock.Of<IDMEEditor>();
        DefaultsManager.RegisterCustomResolver(editor, new DateStampResolver(editor, _ => { reads++; return DateTime.Today; }, "TODAY(bad)"));
        var result = await Run(editor, Config(new Mock<IDataSource>(), ":ADDDAYS(TODAY(bad),1)"), new Dictionary<string, object>());
        Assert.Equal(Errors.Failed, result.Flag); Assert.Equal(0, result.WriteAttempts); Assert.Equal(0, reads);
    }

    [Fact]
    public async Task RequiredDateNestedExceptionDoesNotExposeRuleOrExceptionSecret()
    {
        var editor = Mock.Of<IDMEEditor>();
        DefaultsManager.RegisterCustomResolver(editor, new DateStampResolver(editor, _ => throw new IOException(Secret)));
        Mock.Get(editor).Invocations.Clear();
        var result = await Run(editor, Config(new Mock<IDataSource>(), ":ADDDAYS(DATESTAMP,1)"), new Dictionary<string, object>());
        Assert.Equal(Errors.Failed, result.Flag); Assert.Equal(0, result.WriteAttempts);
        Assert.DoesNotContain(Secret, result.Message ?? "");
        Mock.Get(editor).Verify(x => x.AddLogMessage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(),
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<Errors>()), Times.Never);
    }

    [Fact]
    public async Task RequiredDateSubclassRetainsItsExplicitSemanticContract()
    {
        var editor = Mock.Of<IDMEEditor>(); var row = new Dictionary<string, object>();
        DefaultsManager.RegisterCustomResolver(editor, new CustomDateResolver(editor));
        Assert.Equal(Errors.Ok, (await Run(editor, Config(new Mock<IDataSource>(), ":TODAY"), row)).Flag);
        Assert.Equal("custom-date-contract", row["Value"]);
    }

    private sealed class CustomDateResolver(IDMEEditor editor) : DateTimeResolver(editor)
    {
        public override int Priority => 0;
        public override string ResolverName => "CustomDate";
        public override object ResolveValue(string rule, IPassedArgs parameters) => "custom-date-contract";
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task RequiredDateDirectEmptyRuleCannotReturnClockFallback(string? rule)
    {
        var editor = Mock.Of<IDMEEditor>();
        DefaultsManager.RegisterCustomResolver(editor, new ProbeResolver(editor, args => new DateTimeResolver(editor).ResolveValue(rule, args)));
        Mock.Get(editor).Invocations.Clear();
        var result = await Run(editor, Config(new Mock<IDataSource>(), ":PROBE()"), new Dictionary<string, object>());
        Assert.Equal(Errors.Failed, result.Flag); Assert.Equal(0, result.WriteAttempts);
        Assert.Equal(1, result.RecordsTransformationFailed);
        Mock.Get(editor).Verify(x => x.AddLogMessage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(),
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<Errors>()), Times.Never);
    }

    private sealed class DateStampResolver(IDMEEditor editor, Func<IPassedArgs, object> resolve, string token = "DATESTAMP", bool warning = false)
        : BaseDefaultValueResolver(editor)
    {
        public override string ResolverName => "DateStamp";
        public override int Priority => 0;
        public override IEnumerable<string> SupportedRuleTypes => new[] { token };
        public override bool CanHandle(string rule) => rule == token;
        public override IEnumerable<string> GetExamples() => Array.Empty<string>();
        public override object ResolveValue(string rule, IPassedArgs parameters)
        { if (warning) LogWarning(Secret); return resolve(parameters); }
    }
}
