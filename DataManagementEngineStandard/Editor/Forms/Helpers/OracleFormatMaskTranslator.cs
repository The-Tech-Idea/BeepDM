using System;
using System.Linq;
using System.Text;

namespace TheTechIdea.Beep.Editor.Forms.Helpers;

/// <summary>
/// Translates an Oracle Forms FORMAT_MASK (<see cref="TheTechIdea.Beep.Editor.Forms.Models.BlockFieldDefinition.FormatMask"/>)
/// into the .NET-native shapes the runtime hosts' presenter controls actually
/// consume. Oracle format masks use their own token vocabulary (<c>YYYY</c>,
/// <c>MON</c>, <c>HH24</c>, <c>9</c>, <c>0</c>, ...), which is not
/// interchangeable with .NET custom format strings — passing an authored mask
/// straight into <see cref="DateTime.ToString(string)"/> either throws
/// (uppercase <c>D</c> is not a recognised custom specifier) or silently
/// renders wrong (Oracle's <c>/</c> and <c>:</c> are literals; .NET's are
/// culture-dependent separators unless escaped).
/// <para>
/// Only the commonly-authored subset of each spec is supported. An
/// unsupported token — anything outside the tables below — fails the
/// translation rather than emit a best-effort, possibly-wrong format string;
/// callers must leave the control's own default format in place when a
/// translation is refused, never guess.
/// </para>
/// </summary>
public static class OracleFormatMaskTranslator
{
    // Longest-match-first: "MONTH" must be tried before "MON", "HH24"/"HH12"
    // before "HH", etc. Values are .NET custom date/time format specifiers.
    private static readonly (string Oracle, string Net)[] DateTokens =
    {
        ("MONTH", "MMMM"),
        ("A.M.", "tt"),
        ("P.M.", "tt"),
        ("HH24", "HH"),
        ("HH12", "hh"),
        ("RRRR", "yyyy"),
        ("YYYY", "yyyy"),
        ("DAY", "dddd"),
        ("MON", "MMM"),
        ("DY", "ddd"),
        ("YY", "yy"),
        ("MM", "MM"),
        ("DD", "dd"),
        ("HH", "hh"),
        ("MI", "mm"),
        ("SS", "ss"),
        ("AM", "tt"),
        ("PM", "tt"),
    };

    /// <summary>
    /// Translates an Oracle date/time format mask (e.g. <c>"MM/DD/YYYY"</c>,
    /// <c>"YYYY-MM-DD HH24:MI:SS"</c>) into a .NET custom date/time format
    /// string. Returns false — leaving <paramref name="netFormat"/> null —
    /// when the mask is blank or contains a token this translator does not
    /// recognise (e.g. Oracle's <c>SSSSS</c>, <c>IYYY</c>, <c>RN</c>).
    /// </summary>
    public static bool TryTranslateDate(string? oracleMask, out string? netFormat)
    {
        netFormat = null;
        if (string.IsNullOrWhiteSpace(oracleMask)) return false;

        var mask = oracleMask.Trim();
        var upper = mask.ToUpperInvariant();
        var result = new StringBuilder(mask.Length + 4);
        var i = 0;
        while (i < mask.Length)
        {
            var matched = false;
            foreach (var (oracle, net) in DateTokens)
            {
                if (i + oracle.Length <= upper.Length &&
                    string.CompareOrdinal(upper, i, oracle, 0, oracle.Length) == 0)
                {
                    result.Append(net);
                    i += oracle.Length;
                    matched = true;
                    break;
                }
            }
            if (matched) continue;

            var c = mask[i];
            if (char.IsLetter(c))
            {
                // An alphabetic run this translator does not recognise —
                // refuse rather than emit a literal that would collide with
                // a .NET custom specifier (e.g. a stray "T", "S", "N").
                netFormat = null;
                return false;
            }

            // '/' and ':' are culture-dependent separators in .NET custom
            // format strings unless escaped; every other separator Oracle
            // allows (space, '-', '.', ',') is already literal in .NET.
            if (c == '/' || c == ':') result.Append('\\');
            result.Append(c);
            i++;
        }

        if (result.Length == 0) return false;
        netFormat = result.ToString();
        return true;
    }

    /// <summary>
    /// Translates an Oracle numeric format mask (e.g. <c>"999,999.99"</c>,
    /// <c>"$999,999.00"</c>, <c>"0000"</c>) into the pieces the runtime hosts
    /// need: a .NET numeric format string for the grid's
    /// <c>IFormattable.ToString(format)</c> path, plus the decomposed
    /// decimal-places / grouping / prefix a single-record numeric control
    /// configures itself with rather than a free-form format string. Returns
    /// false when the mask contains anything beyond digit placeholders
    /// (<c>9</c>/<c>0</c>), a single decimal point, group separators, and an
    /// optional leading currency symbol — Oracle's <c>MI</c>, <c>PR</c>,
    /// <c>FM</c>, <c>V</c>, <c>EEEE</c> and similar suffixes are not
    /// supported.
    /// </summary>
    public static bool TryTranslateNumeric(string? oracleMask, out NumericFormatSpec? spec)
    {
        spec = null;
        if (string.IsNullOrWhiteSpace(oracleMask)) return false;

        var mask = oracleMask.Trim();

        string prefix = "";
        if (mask.Length > 0 && mask[0] == '$')
        {
            prefix = "$";
            mask = mask[1..];
        }

        if (mask.Length == 0) return false;

        var parts = mask.Split('.');
        if (parts.Length > 2) return false;

        var intPart = parts[0];
        var decPart = parts.Length == 2 ? parts[1] : string.Empty;

        if (!intPart.All(c => c is '9' or '0' or ',')) return false;
        if (!decPart.All(c => c is '9' or '0')) return false;
        if (intPart.Length == 0 && decPart.Length == 0) return false;

        var groupingEnabled = intPart.Contains(',');
        var decimalPlaces = decPart.Length;
        var requiredIntDigits = Math.Max(1, intPart.Count(c => c == '0'));

        var netIntFormat = groupingEnabled
            ? (requiredIntDigits <= 1 ? "#,##0" : "#,##" + new string('0', requiredIntDigits))
            : (requiredIntDigits <= 1 ? "0" : new string('0', requiredIntDigits));
        var netDecFormat = decimalPlaces > 0 ? "." + new string('0', decimalPlaces) : "";

        spec = new NumericFormatSpec
        {
            NetFormatString = prefix + netIntFormat + netDecFormat,
            DecimalPlaces = decimalPlaces,
            GroupingEnabled = groupingEnabled,
            Prefix = prefix,
        };
        return true;
    }
}

/// <summary>
/// The decomposed result of translating an Oracle numeric FORMAT_MASK — see
/// <see cref="OracleFormatMaskTranslator.TryTranslateNumeric"/>.
/// </summary>
public sealed class NumericFormatSpec
{
    /// <summary>A .NET custom numeric format string, e.g. "#,##0.00".</summary>
    public string NetFormatString { get; set; } = "";
    public int DecimalPlaces { get; set; }
    public bool GroupingEnabled { get; set; }
    public string Prefix { get; set; } = "";
}
