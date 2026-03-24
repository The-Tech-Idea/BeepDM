using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Rules.BuiltinRules.Query
{
    /// <summary>
    /// Performs a single-key lookup against a data source entity and returns a named field from the first match.
    /// Designed for common "lookup a label/value by ID" patterns.
    /// Parameters:
    ///   <c>IDMEEditor</c>     — injected <see cref="IDMEEditor"/> instance.
    ///   <c>DataSourceName</c> — registered data source name.
    ///   <c>EntityName</c>     — entity/table to query.
    ///   <c>LookupField</c>    — field name to match against (e.g. <c>CustomerId</c>).
    ///   <c>LookupValue</c>    — value used to match (e.g. the ID).
    ///   <c>ReturnField</c>    — field from the matching row to return.
    ///   <c>DefaultValue</c>   — (optional) value returned when no row is found.
    /// Outputs: <c>Result</c> (field value or default), <c>Found</c> (bool).
    /// </summary>
    [Rule(ruleKey: "Query.RunParameterizedLookup", ParserKey = "RulesParser", RuleName = "RunParameterizedLookup")]
    public sealed class RunParameterizedLookup : IRule
    {
        private readonly IDMEEditor _editor;

        public string RuleText { get; set; } = "Query.RunParameterizedLookup";
        public IRuleStructure Structure { get; set; } = new RuleStructure();

        public RunParameterizedLookup() { }
        public RunParameterizedLookup(IDMEEditor editor) { _editor = editor; }

        public (Dictionary<string, object> outputs, object result) SolveRule(
            Dictionary<string, object> parameters = null)
        {
            var output = new Dictionary<string, object>();
            if (parameters == null) { output["Error"] = "No parameters."; return (output, null); }

            var dm = ResolveEditor(parameters, output);
            if (dm == null) return (output, null);

            parameters.TryGetValue("DataSourceName", out var dsRaw);
            parameters.TryGetValue("EntityName",     out var enRaw);
            parameters.TryGetValue("LookupField",    out var lfRaw);
            parameters.TryGetValue("LookupValue",    out var lvRaw);
            parameters.TryGetValue("ReturnField",    out var rfRaw);
            parameters.TryGetValue("DefaultValue",   out var defRaw);

            if (dsRaw == null || enRaw == null || lfRaw == null || rfRaw == null)
            {
                output["Error"] = "DataSourceName, EntityName, LookupField, and ReturnField are required.";
                return (output, defRaw);
            }

            try
            {
                var ds = dm.GetDataSource(dsRaw.ToString());
                if (ds == null) { output["Error"] = $"DataSource '{dsRaw}' not found."; return (output, defRaw); }

                var filters = new List<AppFilter>
                {
                    new AppFilter { FieldName = lfRaw.ToString(), Operator = "=", FilterValue = lvRaw?.ToString() }
                };

                var task = ds.GetEntityAsync(enRaw.ToString(), filters);
                task.Wait();
                var rows = (task.Result as IEnumerable<object>)?.ToList() ?? new List<object>();

                bool found = rows.Count > 0;
                object res = defRaw;

                if (found)
                {
                    var first = rows[0];
                    if (first is IDictionary<string, object> d)
                        d.TryGetValue(rfRaw.ToString(), out res);
                    else
                    {
                        var prop = first?.GetType().GetProperty(rfRaw.ToString());
                        res = prop?.GetValue(first) ?? defRaw;
                    }
                }

                output["Result"] = res;
                output["Found"]  = found;
                return (output, res);
            }
            catch (Exception ex)
            {
                output["Error"] = ex.Message;
                return (output, defRaw);
            }
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
