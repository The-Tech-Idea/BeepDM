using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Rules.BuiltinRules.DQ
{
    /// <summary>
    /// Checks referential integrity: verifies that <c>Value</c> exists in <c>LookupEntityName</c>.<c>LookupField</c>.
    /// Parameters: <c>DataSourceName</c>, <c>LookupEntityName</c>, <c>LookupField</c>,
    /// <c>Value</c>, <c>IDMEEditor</c>.
    /// Output: <c>bool</c> + <c>MatchedRecord</c> (first matched row as dict).
    /// </summary>
    [Rule(ruleKey: "DQ.ReferentialIntegrity", ParserKey = "RulesParser", RuleName = "ReferentialIntegrity")]
    public sealed class ReferentialIntegrity : IRule
    {
        private readonly IDMEEditor _editor;

        public string RuleText { get; set; } = "DQ.ReferentialIntegrity";
        public IRuleStructure Structure { get; set; } = new RuleStructure();

        public ReferentialIntegrity() { }
        public ReferentialIntegrity(IDMEEditor editor) { _editor = editor; }

        public (Dictionary<string, object> outputs, object result) SolveRule(
            Dictionary<string, object> parameters = null)
        {
            var output = new Dictionary<string, object>();
            if (parameters == null)
            { output["Error"] = "Parameters cannot be null"; return (output, false); }

            var dm = ResolveEditor(parameters, output);
            if (dm == null) return (output, false);

            if (!parameters.TryGetValue("DataSourceName",    out var dsRaw) ||
                !parameters.TryGetValue("LookupEntityName",  out var enRaw) ||
                !parameters.TryGetValue("LookupField",       out var lfRaw) ||
                !parameters.TryGetValue("Value",             out var valRaw))
            {
                output["Error"] = "Missing required parameters: DataSourceName, LookupEntityName, LookupField, Value";
                return (output, false);
            }

            try
            {
                var ds = dm.GetDataSource(dsRaw.ToString());
                if (ds == null) { output["Error"] = $"DataSource '{dsRaw}' not found"; return (output, false); }

                var filters = new List<AppFilter>
                {
                    new AppFilter { FieldName = lfRaw.ToString(), Operator = "=", FilterValue = valRaw?.ToString() }
                };

                var task = ds.GetEntityAsync(enRaw.ToString(), filters);
                task.Wait();
                var rows = task.Result as IList<object>;
                bool exists = rows != null && rows.Any();

                output["Result"] = exists;
                if (exists && rows![0] is Dictionary<string, object> matched)
                    output["MatchedRecord"] = matched;

                return (output, exists);
            }
            catch (Exception ex)
            {
                output["Error"] = ex.Message;
                return (output, false);
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
