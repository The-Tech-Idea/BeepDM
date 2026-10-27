using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Extensions
{
    /// <summary>Shared bounded-read/conditional-cohort compiler. Text never enters generated syntax or native linguistic case conversion.</summary>
    public static class OrdinalIgnoreCaseTextQuery
    {
        /// <summary>Builds one exact predicate from a metadata-owned quoted column and a caller-owned integer parameter binder.</summary>
        public static string Build(DataSourceType provider, string quotedColumn, OrdinalIgnoreCaseTextEquality comparison, Func<int, string> parameter)
        {
            ArgumentNullException.ThrowIfNull(comparison); ArgumentNullException.ThrowIfNull(parameter);
            if (provider is not (DataSourceType.SqlLite or DataSourceType.SqlServer or DataSourceType.AzureSQL))
                throw new NotSupportedException("Ordinal case-insensitive text predicates are not qualified for this provider.");
            var sqlite = provider == DataSourceType.SqlLite;
            var column = sqlite ? quotedColumn : "CONVERT(nvarchar(max)," + quotedColumn + ") COLLATE Latin1_General_100_BIN2";
            var terms = new List<string> { quotedColumn + " IS NOT NULL" };
            if (sqlite)
            {
                // SQLite length stops at NUL; exclude such stored values rather than matching an invisible suffix.
                terms.Add("instr(" + column + ",char(0)) = 0");
                terms.Add("length(" + column + ") = " + parameter(comparison.Groups.Count));
            }
            else terms.Add("DATALENGTH(" + column + ") = " + parameter(comparison.Value.Length * 2));
            var offset = 1;
            foreach (var group in comparison.Groups)
            {
                var position = offset.ToString(CultureInfo.InvariantCulture);
                if (sqlite)
                {
                    terms.Add("unicode(substr(" + column + "," + position + ",1)) IN (" + string.Join(",", group.Select(text => parameter(Rune.GetRuneAt(text, 0).Value))) + ")");
                    offset++;
                }
                else
                {
                    // The explicit non-SC binary collation makes positions UTF-16 code units.
                    // Correlate each surrogate pair: independent high/low sets could admit a different scalar.
                    var alternatives = group.Select(text => "(" + string.Join(" AND ", text.Select((unit, index) =>
                        "UNICODE(SUBSTRING(" + column + "," + (offset + index).ToString(CultureInfo.InvariantCulture) + ",1)) = " + parameter(unit))) + ")");
                    terms.Add("(" + string.Join(" OR ", alternatives) + ")"); offset += group[0].Length;
                }
            }
            return "(" + string.Join(" AND ", terms) + ")";
        }
    }
}
