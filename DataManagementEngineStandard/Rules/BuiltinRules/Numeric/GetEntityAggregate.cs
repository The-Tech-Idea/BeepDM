using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Rules.BuiltinRules.Numeric
{
    /// <summary>
    /// Computes an aggregate (sum, avg, min, max, count) over <c>FieldName</c>
    /// in <c>EntityName</c> on <c>DataSourceName</c>.
    /// Parameters: <c>DataSourceName</c>, <c>EntityName</c>, <c>FieldName</c>,
    /// <c>Function</c> ("sum"|"avg"|"min"|"max"|"count"), <c>IDMEEditor</c>.
    /// Optional <c>FilterField</c>, <c>FilterOperator</c>, <c>FilterValue</c> for row pre-filtering.
    /// </summary>
    [Rule(ruleKey: "Numeric.GetEntityAggregate", ParserKey = "RulesParser", RuleName = "GetEntityAggregate")]
    public sealed class GetEntityAggregate : IRule
    {
        private readonly IDMEEditor _editor;

        public string RuleText { get; set; } = "Numeric.GetEntityAggregate";
        public IRuleStructure Structure { get; set; } = new RuleStructure();

        public GetEntityAggregate() { }
        public GetEntityAggregate(IDMEEditor editor) { _editor = editor; }

        public (Dictionary<string, object> outputs, object result) SolveRule(
            Dictionary<string, object> parameters = null)
        {
            var output = new Dictionary<string, object>();
            if (parameters == null)
            { output["Error"] = "Parameters cannot be null"; return (output, null); }

            var dm = ResolveEditor(parameters, output);
            if (dm == null) return (output, null);

            if (!parameters.TryGetValue("DataSourceName", out var dsRaw) ||
                !parameters.TryGetValue("EntityName",     out var enRaw) ||
                !parameters.TryGetValue("FieldName",      out var fnRaw) ||
                !parameters.TryGetValue("Function",       out var funcRaw))
            {
                output["Error"] = "Missing required parameters: DataSourceName, EntityName, FieldName, Function";
                return (output, null);
            }

            string func = funcRaw?.ToString()?.ToLowerInvariant() ?? string.Empty;
            if (!new[] { "sum", "avg", "min", "max", "count" }.Contains(func))
            {
                output["Error"] = "Function must be one of: sum, avg, min, max, count";
                return (output, null);
            }

            try
            {
                var ds = dm.GetDataSource(dsRaw.ToString());
                if (ds == null) { output["Error"] = $"DataSource '{dsRaw}' not found"; return (output, null); }

                var filters = new List<AppFilter>();
                if (parameters.TryGetValue("FilterField",    out var ff)  &&
                    parameters.TryGetValue("FilterOperator", out var fop) &&
                    parameters.TryGetValue("FilterValue",    out var fv)  &&
                    ff != null)
                {
                    filters.Add(new AppFilter
                    {
                        FieldName   = ff.ToString(),
                        Operator    = fop?.ToString() ?? "=",
                        FilterValue = fv?.ToString()
                    });
                }

                var task = ds.GetEntityAsync(enRaw.ToString(), filters);
                task.Wait();
                var rows = task.Result as IEnumerable<object>;

                if (rows == null)
                {
                    output["Result"] = func == "count" ? (object)0 : null;
                    return (output, output["Result"]);
                }

                string field = fnRaw.ToString();
                object res;

                if (func == "count")
                {
                    res = rows.Count();
                }
                else
                {
                    var values = rows
                        .Select(r => GetFieldValue(r, field))
                        .Where(v => v.HasValue)
                        .Select(v => v!.Value)
                        .ToList();

                    if (!values.Any())
                    {
                        output["Result"] = null;
                        output["RowCount"] = 0;
                        return (output, null);
                    }

                    res = func switch
                    {
                        "sum" => values.Sum(),
                        "avg" => values.Average(),
                        "min" => values.Min(),
                        "max" => values.Max(),
                        _     => null
                    };
                }

                output["Result"]   = res;
                output["Function"] = func;
                return (output, res);
            }
            catch (Exception ex)
            {
                output["Error"] = ex.Message;
                return (output, null);
            }
        }

        private static double? GetFieldValue(object row, string field)
        {
            if (row == null) return null;
            var type  = row.GetType();
            var prop  = type.GetProperty(field);
            var value = prop?.GetValue(row);
            if (value == null || value is DBNull) return null;
            return Convert.ToDouble(value);
        }

        private IDMEEditor ResolveEditor(Dictionary<string, object> p, Dictionary<string, object> output)
        {
            if (_editor != null) return _editor;
            if (p.TryGetValue("IDMEEditor", out var raw) && raw is IDMEEditor dm) return dm;
            output["Error"] = "IDMEEditor is required";
            return null;
        }
    }
}
