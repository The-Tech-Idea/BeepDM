using System;
using System.Collections.Generic;
using System.Data;
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
                ">" or "<" or ">=" or "<=" => $"{f.FieldName} {f.Operator} '{Escape(f.FilterValue)}'",
                "<>" or "!=" => $"{f.FieldName} <> '{Escape(f.FilterValue)}'",
                "between" => $"{f.FieldName} >= '{Escape(f.FilterValue)}' AND {f.FieldName} <= '{Escape(f.FilterValue1)}'",
                _ => null
            };

        private static string Escape(object val) => val?.ToString()?.Replace("'", "''") ?? string.Empty;

        private static bool EvaluateRow(System.Data.DataRow r, AppFilter f)
        {
            if (!r.Table.Columns.Contains(f.FieldName)) return false;
            var fieldVal = r[f.FieldName];
            if (fieldVal == DBNull.Value) return false;

            var cmp = Comparer<object>.Default;
            return f.Operator switch
            {
                "equals" or "=" => fieldVal.Equals(f.FilterValue),
                "contains" => fieldVal.ToString().Contains(f.FilterValue?.ToString() ?? "", StringComparison.OrdinalIgnoreCase),
                ">" => cmp.Compare(fieldVal, f.FilterValue) > 0,
                "<" => cmp.Compare(fieldVal, f.FilterValue) < 0,
                ">=" => cmp.Compare(fieldVal, f.FilterValue) >= 0,
                "<=" => cmp.Compare(fieldVal, f.FilterValue) <= 0,
                "<>" or "!=" => !fieldVal.Equals(f.FilterValue),
                "between" => cmp.Compare(fieldVal, f.FilterValue) >= 0 && cmp.Compare(fieldVal, f.FilterValue1) <= 0,
                _ => false
            };
        }
    }
}