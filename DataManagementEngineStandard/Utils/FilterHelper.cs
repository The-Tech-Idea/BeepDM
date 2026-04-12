using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Report;

// Fully qualify System.Data types to avoid naming collisions with project-level
// types (e.g., custom DataView/DataTable classes or namespaces).
namespace TheTechIdea.Beep.Utils
{
    internal static class FilterHelper
    {
        internal static System.Data.DataView ApplyFilters(System.Data.DataTable records, List<AppFilter> filters)
        {
            var view = new System.Data.DataView(records);
            if (filters == null || filters.Count == 0) return view;

            foreach (var f in filters)
            {
                if (string.IsNullOrWhiteSpace(f.FieldName) || f.FilterValue == null) continue;
                var clause = BuildFilterClause(f);
                if (!string.IsNullOrEmpty(clause))
                {
                    view.RowFilter += (view.RowFilter.Length > 0 ? " AND " : "") + clause;
                }
            }
            return view;
        }

        internal static IEnumerable<System.Data.DataRow> ApplyFiltersEnumerable(System.Data.DataTable records, List<AppFilter> filters)
        {
            IEnumerable<System.Data.DataRow> q = records.AsEnumerable();
            if (filters == null || filters.Count == 0) return q;

            foreach (var f in filters)
            {
                q = q.Where(r => EvaluateRow(r, f));
            }
            return q;
        }

        private static string BuildFilterClause(AppFilter f) =>
            f.Operator switch
            {
                "equals" or "=" => $"{f.FieldName} = '{Escape(f.FilterValue)}'",
                "contains" => $"{f.FieldName} LIKE '%{Escape(f.FilterValue)}%'",
                "startswith" => $"{f.FieldName} LIKE '{Escape(f.FilterValue)}%'",
                "endswith" => $"{f.FieldName} LIKE '%{Escape(f.FilterValue)}'",
                "in" => BuildCollectionClause(f, negate: false),
                "not in" or "notin" => BuildCollectionClause(f, negate: true),
                ">" or "<" or ">=" or "<=" => $"{f.FieldName} {f.Operator} '{Escape(f.FilterValue)}'",
                "<>" or "!=" => $"{f.FieldName} <> '{Escape(f.FilterValue)}'",
                "between" => $"{f.FieldName} >= '{Escape(f.FilterValue)}' AND {f.FieldName} <= '{Escape(f.FilterValue1)}'",
                "not between" or "notbetween" => $"({f.FieldName} < '{Escape(f.FilterValue)}' OR {f.FieldName} > '{Escape(f.FilterValue1)}')",
                "is null" => $"{f.FieldName} IS NULL",
                "is not null" => $"{f.FieldName} IS NOT NULL",
                _ => null
            };

        private static string Escape(object val) => val?.ToString()?.Replace("'", "''") ?? string.Empty;

        private static bool EvaluateRow(System.Data.DataRow r, AppFilter f)
        {
            if (!r.Table.Columns.Contains(f.FieldName)) return false;
            var fieldVal = r[f.FieldName];
            if (fieldVal == DBNull.Value)
            {
                return f.Operator switch
                {
                    "is null" => true,
                    "is not null" => false,
                    _ => false
                };
            }

            var cmp = Comparer<object>.Default;
            return f.Operator switch
            {
                "equals" or "=" => fieldVal.Equals(f.FilterValue),
                "contains" => fieldVal.ToString().Contains(f.FilterValue?.ToString() ?? "", StringComparison.OrdinalIgnoreCase),
                "startswith" => fieldVal.ToString().StartsWith(f.FilterValue?.ToString() ?? "", StringComparison.OrdinalIgnoreCase),
                "endswith" => fieldVal.ToString().EndsWith(f.FilterValue?.ToString() ?? "", StringComparison.OrdinalIgnoreCase),
                "in" => MatchesCollection(fieldVal, f.FilterValue, negate: false),
                "not in" or "notin" => MatchesCollection(fieldVal, f.FilterValue, negate: true),
                ">" => cmp.Compare(fieldVal, f.FilterValue) > 0,
                "<" => cmp.Compare(fieldVal, f.FilterValue) < 0,
                ">=" => cmp.Compare(fieldVal, f.FilterValue) >= 0,
                "<=" => cmp.Compare(fieldVal, f.FilterValue) <= 0,
                "<>" or "!=" => !fieldVal.Equals(f.FilterValue),
                "between" => cmp.Compare(fieldVal, f.FilterValue) >= 0 && cmp.Compare(fieldVal, f.FilterValue1) <= 0,
                "not between" or "notbetween" => cmp.Compare(fieldVal, f.FilterValue) < 0 || cmp.Compare(fieldVal, f.FilterValue1) > 0,
                "is null" => false,
                "is not null" => true,
                _ => false
            };
        }

        private static string BuildCollectionClause(AppFilter filter, bool negate)
        {
            var values = SplitCollectionValues(filter.FilterValue).ToList();
            if (values.Count == 0)
            {
                return string.Empty;
            }

            string comparator = negate ? "<>" : "=";
            string joiner = negate ? " AND " : " OR ";
            return "(" + string.Join(joiner, values.Select(value => $"{filter.FieldName} {comparator} '{Escape(value)}'")) + ")";
        }

        private static IEnumerable<string> SplitCollectionValues(object rawValue)
        {
            if (rawValue == null)
            {
                return Enumerable.Empty<string>();
            }

            return rawValue.ToString()
                .Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(value => value.Trim())
                .Where(value => !string.IsNullOrWhiteSpace(value));
        }

        private static bool MatchesCollection(object fieldValue, object rawFilterValue, bool negate)
        {
            var values = SplitCollectionValues(rawFilterValue).ToList();
            if (values.Count == 0)
            {
                return false;
            }

            bool matched = values.Any(value => FilterValueMatches(fieldValue, value));
            return negate ? !matched : matched;
        }

        private static bool FilterValueMatches(object fieldValue, string rawValue)
        {
            if (fieldValue == null || fieldValue == DBNull.Value)
            {
                return false;
            }

            Type fieldType = Nullable.GetUnderlyingType(fieldValue.GetType()) ?? fieldValue.GetType();
            if (fieldType == typeof(DateTime))
            {
                return DateTime.TryParse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedDate)
                    ? ((DateTime)fieldValue) == parsedDate
                    : DateTime.TryParse(rawValue, out parsedDate) && ((DateTime)fieldValue) == parsedDate;
            }

            if (fieldType == typeof(bool))
            {
                return bool.TryParse(rawValue, out var parsedBool) && (bool)fieldValue == parsedBool;
            }

            if (IsNumericType(fieldType))
            {
                return decimal.TryParse(rawValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedNumber) &&
                       Convert.ToDecimal(fieldValue, CultureInfo.InvariantCulture) == parsedNumber;
            }

            return string.Equals(fieldValue.ToString(), rawValue, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsNumericType(Type type)
        {
            return type == typeof(byte) || type == typeof(short) || type == typeof(int) ||
                   type == typeof(long) || type == typeof(float) || type == typeof(double) ||
                   type == typeof(decimal);
        }
    }
}