using System;
using TheTechIdea.Beep.Editor.Forms.Helpers;
using Xunit;

namespace TheTechIdea.Beep.Editor.UOWManager.Tests;

public class OracleFormatMaskTranslatorTests
{
    [Theory]
    [InlineData("MM/DD/YYYY", "MM\\/dd\\/yyyy")]
    [InlineData("DD-MON-YYYY", "dd-MMM-yyyy")]
    [InlineData("YYYY-MM-DD", "yyyy-MM-dd")]
    [InlineData("DD/MM/YYYY HH24:MI:SS", "dd\\/MM\\/yyyy HH\\:mm\\:ss")]
    [InlineData("HH:MI:SS AM", "hh\\:mm\\:ss tt")]
    [InlineData("Month DD, YYYY", "MMMM dd, yyyy")]
    public void TryTranslateDate_KnownMask_ProducesExpectedNetFormat(string oracleMask, string expectedNetFormat)
    {
        Assert.True(OracleFormatMaskTranslator.TryTranslateDate(oracleMask, out var netFormat));
        Assert.Equal(expectedNetFormat, netFormat);
    }

    [Theory]
    [InlineData("MM/DD/YYYY")]
    [InlineData("YYYY-MM-DD HH24:MI:SS")]
    public void TryTranslateDate_KnownMask_RoundTripsARealDate(string oracleMask)
    {
        var sample = new DateTime(2026, 3, 7, 14, 5, 9);
        Assert.True(OracleFormatMaskTranslator.TryTranslateDate(oracleMask, out var netFormat));

        // Must not throw FormatException, and the separators authored in the
        // mask must appear literally regardless of the current culture.
        var formatted = sample.ToString(netFormat);
        Assert.Contains("2026", formatted);
        Assert.Contains("03", formatted);
        Assert.Contains("07", formatted);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("SSSSS")]
    [InlineData("IYYY-IW")]
    [InlineData("RN")]
    [InlineData("DD FMMONTH YYYY")] // FM prefix is not a recognised token
    public void TryTranslateDate_UnsupportedMask_Refuses(string? oracleMask)
    {
        Assert.False(OracleFormatMaskTranslator.TryTranslateDate(oracleMask, out var netFormat));
        Assert.Null(netFormat);
    }

    [Fact]
    public void TryTranslateNumeric_ThousandsAndDecimals_ProducesGroupedFormat()
    {
        Assert.True(OracleFormatMaskTranslator.TryTranslateNumeric("999,999.99", out var spec));
        Assert.Equal("#,##0.00", spec!.NetFormatString);
        Assert.Equal(2, spec.DecimalPlaces);
        Assert.True(spec.GroupingEnabled);
        Assert.Equal("", spec.Prefix);

        Assert.Equal("1,234.50", (1234.5m).ToString(spec.NetFormatString));
    }

    [Fact]
    public void TryTranslateNumeric_LeadingCurrencySymbol_CapturedAsPrefix()
    {
        Assert.True(OracleFormatMaskTranslator.TryTranslateNumeric("$999,999.00", out var spec));
        Assert.Equal("$#,##0.00", spec!.NetFormatString);
        Assert.Equal("$", spec.Prefix);
        Assert.True(spec.GroupingEnabled);

        Assert.Equal("$1,234.50", (1234.5m).ToString(spec.NetFormatString));
    }

    [Fact]
    public void TryTranslateNumeric_ZeroPaddedInteger_RequiresLeadingZeros()
    {
        Assert.True(OracleFormatMaskTranslator.TryTranslateNumeric("0000", out var spec));
        Assert.Equal("0000", spec!.NetFormatString);
        Assert.Equal(0, spec.DecimalPlaces);
        Assert.False(spec.GroupingEnabled);

        Assert.Equal("0042", (42m).ToString(spec.NetFormatString));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("999.99MI")]
    [InlineData("999.99PR")]
    [InlineData("FM999.99")]
    [InlineData("9V99")]
    [InlineData("999.99.99")]
    public void TryTranslateNumeric_UnsupportedMask_Refuses(string? oracleMask)
    {
        Assert.False(OracleFormatMaskTranslator.TryTranslateNumeric(oracleMask, out var spec));
        Assert.Null(spec);
    }
}
