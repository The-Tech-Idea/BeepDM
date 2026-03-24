using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Rules.BuiltinRules.Query
{
    /// <summary>
    /// Runs a parameterized query on a data source and writes the result value
    /// into a named field of the supplied record.
    /// Parameters:
    ///   <c>IDMEEditor</c>     — injected <see cref="IDMEEditor"/> instance.
    ///   <c>DataSourceName</c> — registered data source name.
    ///   <c>EntityName</c>     — entity/table to query.
    ///   <c>Filters</c>        — (optional) semicolon-separated <c>"field:op:value"</c> triples.
    ///   <c>OutputField</c>    — field in the result set to read (defaults to first field).
    ///   <c>TargetField</c>    — field name in <c>Record</c> to write the result into.
    ///   <c>Record</c>         — <see cref="IDictionary{TKey,TValue}"/> or <c>"field=val;…"</c> string.
    /// Outputs: <c>Record</c> (updated), <c>Result</c> (value written), <c>TargetField</c>.
    /// </summary>
    [Rule(ruleKey: "Query.SetFromQuery", ParserKey = "RulesParser", RuleName = "SetFromQuery")]
    public sealed class SetFromQuery : IRule
    {
        private readonly IDMEEditor _editor;

        public string RuleText { get; set; } = "Query.SetFromQuery";
        public IRuleStructure Structure { get; set; } = new RuleStructure();

        public SetFromQuery() { }
        public SetFromQuery(IDMEEditor editor) { _editor = editor; }

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
            parameters.TryGetValue("TargetField",    out var tfRaw);
            parameters.TryGetValue("Record",         out var recRaw);

            if (dsRaw == null || enRaw == null)
            { output["Error"] = "DataSourceName and EntityName are required."; return (output, null); }

            // Build/copy record
            var record = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (recRaw is IDictionary<string, object> d)
                foreach (var kv in d) record[kv.Key] = kv.Value;
            else if (recRaw is string s)
                foreach (var pair in s.Split(';', StringSplitOptions.RemoveEmptyEntries))
                { var idx = pair.IndexOf('='); if (idx > 0) record[pair[..idx].Trim()] = pair[(idx + 1)..].Trim(); }

            try
            {
                var ds = dm.GetDataSource(dsRaw.ToString());
                if (ds == null) { output["Error"] = $"DataSource '{dsRaw}' not found."; return (output, null); }

                var filters = ParseFilters(filRaw?.ToString());
                var task    = ds.GetEntityAsync(enRaw.ToString(), filters);
                task.Wait();
                var rows = (task.Result as IEnumerable<object>)?.ToList() ?? new List<object>();

                object scalar = null;
                if (rows.Count > 0)
                    scalar = ExtractField(rows[0], ofRaw?.ToString());

                var targetField = tfRaw?.ToString() ?? ofRaw?.ToString() ?? "QueryResult";
                record[targetField] = scalar;

                output["Record"]      = record;
                output["Result"]      = scalar;
                output["TargetField"] = targetField;
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
