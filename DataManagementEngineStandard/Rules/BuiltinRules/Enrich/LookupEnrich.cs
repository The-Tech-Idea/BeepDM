using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Rules.BuiltinRules.Enrich
{
    /// <summary>
    /// Looks up a record in <c>LookupEntityName</c> by <c>LookupField==Value</c>
    /// and copies the value of <c>ReturnField</c> into the output.
    /// Parameters: <c>DataSourceName</c>, <c>LookupEntityName</c>, <c>LookupField</c>,
    /// <c>Value</c>, <c>ReturnField</c>, <c>IDMEEditor</c>.
    /// Optional <c>DefaultValue</c> returned when no match found.
    /// </summary>
    [Rule(ruleKey: "Enrich.LookupEnrich", ParserKey = "RulesParser", RuleName = "LookupEnrich")]
    public sealed class LookupEnrich : IRule
    {
        private readonly IDMEEditor _editor;

        public string RuleText { get; set; } = "Enrich.LookupEnrich";
        public IRuleStructure Structure { get; set; } = new RuleStructure();

        public LookupEnrich() { }
        public LookupEnrich(IDMEEditor editor) { _editor = editor; }

        public (Dictionary<string, object> outputs, object result) SolveRule(
            Dictionary<string, object> parameters = null)
        {
            var output = new Dictionary<string, object>();
            if (parameters == null)
            { output["Error"] = "Parameters cannot be null"; return (output, null); }

            var dm = ResolveEditor(parameters, output);
            if (dm == null) return (output, null);

            if (!parameters.TryGetValue("DataSourceName",   out var dsRaw)    ||
                !parameters.TryGetValue("LookupEntityName", out var entityRaw) ||
                !parameters.TryGetValue("LookupField",      out var fieldRaw)  ||
                !parameters.TryGetValue("Value",            out var valueRaw)  ||
                !parameters.TryGetValue("ReturnField",      out var retRaw))
            {
                output["Error"] = "Missing required parameters: DataSourceName, LookupEntityName, LookupField, Value, ReturnField";
                return (output, null);
            }

            object? defaultValue = null;
            if (parameters.TryGetValue("DefaultValue", out var defRaw))
                defaultValue = defRaw;

            try
            {
                var ds = dm.GetDataSource(dsRaw.ToString());
                if (ds == null) { output["Error"] = $"DataSource '{dsRaw}' not found"; return (output, null); }

                var filters = new List<AppFilter>
                {
                    new AppFilter { FieldName = fieldRaw.ToString(), Operator = "=", FilterValue = valueRaw?.ToString() }
                };

                var task = ds.GetEntityAsync(entityRaw.ToString(), filters);
                task.Wait();
                var rows = (task.Result as IEnumerable<object>)?.ToList();

                object? res = defaultValue;
                if (rows != null && rows.Count > 0)
                {
                    var first = rows[0];
                    var prop  = first?.GetType().GetProperty(retRaw.ToString());
                    if (prop != null) res = prop.GetValue(first);
                }

                output["Result"]  = res;
                output["Found"]   = rows != null && rows.Count > 0;
                return (output, res);
            }
            catch (Exception ex)
            {
                output["Error"] = ex.Message;
                return (output, defaultValue);
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
