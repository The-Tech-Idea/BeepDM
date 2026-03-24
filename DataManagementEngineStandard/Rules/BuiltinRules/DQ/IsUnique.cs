using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Rules.BuiltinRules.DQ
{
    /// <summary>
    /// Checks whether <c>Value</c> is unique for <c>FieldName</c> in <c>EntityName</c>.
    /// Parameters: <c>DataSourceName</c>, <c>EntityName</c>, <c>FieldName</c>,
    /// <c>Value</c>, <c>IDMEEditor</c>.
    /// </summary>
    [Rule(ruleKey: "DQ.IsUnique", ParserKey = "RulesParser", RuleName = "IsUnique")]
    public sealed class IsUnique : IRule
    {
        private readonly IDMEEditor _editor;

        public string RuleText { get; set; } = "DQ.IsUnique";
        public IRuleStructure Structure { get; set; } = new RuleStructure();

        public IsUnique() { }
        public IsUnique(IDMEEditor editor) { _editor = editor; }

        public (Dictionary<string, object> outputs, object result) SolveRule(
            Dictionary<string, object> parameters = null)
        {
            var output = new Dictionary<string, object>();
            if (parameters == null)
            { output["Error"] = "Parameters cannot be null"; return (output, false); }

            var dm = ResolveEditor(parameters, output);
            if (dm == null) return (output, false);

            if (!parameters.TryGetValue("DataSourceName", out var dsRaw) ||
                !parameters.TryGetValue("EntityName",     out var enRaw) ||
                !parameters.TryGetValue("FieldName",      out var fnRaw) ||
                !parameters.TryGetValue("Value",          out var valRaw))
            {
                output["Error"] = "Missing required parameters: DataSourceName, EntityName, FieldName, Value";
                return (output, false);
            }

            try
            {
                var ds = dm.GetDataSource(dsRaw.ToString());
                if (ds == null) { output["Error"] = $"DataSource '{dsRaw}' not found"; return (output, false); }

                var filters = new List<AppFilter>
                {
                    new AppFilter { FieldName = fnRaw.ToString(), Operator = "=", FilterValue = valRaw?.ToString() }
                };

                var task  = ds.GetEntityAsync(enRaw.ToString(), filters);
                task.Wait();
                var rows  = task.Result as IList<object>;
                bool unique = rows == null || !rows.Any();

                output["Result"]     = unique;
                output["MatchCount"] = rows?.Count ?? 0;
                return (output, unique);
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
