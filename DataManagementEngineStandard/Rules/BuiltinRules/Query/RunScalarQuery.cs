using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Rules.BuiltinRules.Query
{
    /// <summary>
    /// Runs a filtered query on a registered data source and returns the
    /// first field of the first matching row as a scalar value.
    /// Parameters:
    ///   <c>IDMEEditor</c>     — injected <see cref="IDMEEditor"/> instance.
    ///   <c>DataSourceName</c> — registered data source name.
    ///   <c>EntityName</c>     — entity/table to query.
    ///   <c>Filters</c>        — (optional) semicolon-separated <c>"field:op:value"</c> triples,
    ///                           e.g. <c>"Status:=:Active;Score:&gt;:50"</c>.
    ///   <c>OutputField</c>    — (optional) specific field name to return; defaults to first field.
    /// Outputs: <c>Result</c> (scalar), <c>RowCount</c>.
    /// </summary>
    [Rule(ruleKey: "Query.RunScalarQuery", ParserKey = "RulesParser", RuleName = "RunScalarQuery")]
    public sealed class RunScalarQuery : IRule
    {
        private readonly IDMEEditor _editor;

        public string RuleText { get; set; } = "Query.RunScalarQuery";
        public IRuleStructure Structure { get; set; } = new RuleStructure();

        public RunScalarQuery() { }
        public RunScalarQuery(IDMEEditor editor) { _editor = editor; }

        public (Dictionary<string, object> outputs, object result) SolveRule(
            Dictionary<string, object> parameters = null)
        {
            var output = new Dictionary<string, object>();
            if (parameters == null) { output["Error"] = "No parameters."; return (output, null); }

            var dm = ResolveEditor(parameters, output);
            if (dm == null) return (output, null);

            parameters.TryGetValue("DataSourceName", out var dsRaw);
            parameters.TryGetValue("EntityName",     out var enRaw);
            parameters.TryGetValue("Filters",        out var filRaw);
            parameters.TryGetValue("OutputField",    out var ofRaw);

            if (dsRaw == null || enRaw == null)
            { output["Error"] = "DataSourceName and EntityName are required."; return (output, null); }

            try
            {
                var ds = dm.GetDataSource(dsRaw.ToString());
                if (ds == null) { output["Error"] = $"DataSource '{dsRaw}' not found."; return (output, null); }

                var filters = ParseFilters(filRaw?.ToString());
                var task    = ds.GetEntityAsync(enRaw.ToString(), filters);
                task.Wait();
                var rows = (task.Result as IEnumerable<object>)?.ToList() ?? new List<object>();

                output["RowCount"] = rows.Count;
                if (rows.Count == 0) { output["Result"] = null; return (output, null); }

                var scalar = ExtractField(rows[0], ofRaw?.ToString());
                output["Result"] = scalar;
                return (output, scalar);
            }
            catch (Exception ex)
            {
                output["Error"] = ex.Message;
                return (output, null);
            }
        }

        private IDMEEditor ResolveEditor(Dictionary<string, object> p, Dictionary<string, object> output)
        {
            if (_editor != null) return _editor;
            if (p.TryGetValue("IDMEEditor", out var raw) && raw is IDMEEditor dm) return dm;
            output["Error"] = "IDMEEditor is required";
            return null;
        }

        private static object ExtractField(object row, string fieldName)
        {
            if (row is IDictionary<string, object> d)
                return (fieldName != null && d.TryGetValue(fieldName, out var fv))
                    ? fv : d.Values.FirstOrDefault();

            var prop = fieldName != null
                ? row?.GetType().GetProperty(fieldName)
                : row?.GetType().GetProperties().FirstOrDefault();
            return prop?.GetValue(row);
        }

        private static List<AppFilter> ParseFilters(string raw)
        {
            var list = new List<AppFilter>();
            if (string.IsNullOrWhiteSpace(raw)) return list;
            foreach (var part in raw.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var bits = part.Split(':', 3);
                if (bits.Length < 3) continue;
                list.Add(new AppFilter { FieldName = bits[0].Trim(), Operator = bits[1].Trim(), FilterValue = bits[2].Trim() });
            }
            return list;
        }
    }
}
