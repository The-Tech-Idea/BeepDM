using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Rules.BuiltinRules.Notify
{
    /// <summary>
    /// Writes an audit trail record to the configured data source.
    /// Parameters:
    ///   <c>DataSourceName</c>  — target data source.
    ///   <c>EntityName</c>      — audit log entity/table name.
    ///   <c>Event</c>           — event label ("Validated", "Rejected", etc.).
    ///   <c>RecordId</c>        — identifier of the audited record.
    ///   <c>IDMEEditor</c>      — engine reference.
    /// Optional <c>Details</c>  — free-text description.
    /// Optional <c>ActorId</c>  — user/system that triggered the event.
    /// </summary>
    [Rule(ruleKey: "Notify.EmitAuditTrail", ParserKey = "RulesParser", RuleName = "EmitAuditTrail")]
    public sealed class EmitAuditTrail : IRule
    {
        private readonly IDMEEditor _editor;

        public string RuleText { get; set; } = "Notify.EmitAuditTrail";
        public IRuleStructure Structure { get; set; } = new RuleStructure();

        public EmitAuditTrail() { }
        public EmitAuditTrail(IDMEEditor editor) { _editor = editor; }

        public (Dictionary<string, object> outputs, object result) SolveRule(
            Dictionary<string, object> parameters = null)
        {
            var output = new Dictionary<string, object>();
            if (parameters == null)
            { output["Error"] = "Parameters cannot be null"; return (output, false); }

            var dm = ResolveEditor(parameters, output);
            if (dm == null) return (output, false);

            if (!parameters.TryGetValue("DataSourceName", out var dsRaw)     ||
                !parameters.TryGetValue("EntityName",     out var entityRaw)  ||
                !parameters.TryGetValue("Event",          out var eventRaw)   ||
                !parameters.TryGetValue("RecordId",       out var ridRaw))
            {
                output["Error"] = "Missing required parameters: DataSourceName, EntityName, Event, RecordId";
                return (output, false);
            }

            try
            {
                var ds = dm.GetDataSource(dsRaw.ToString());
                if (ds == null) { output["Error"] = $"DataSource '{dsRaw}' not found"; return (output, false); }

                string actorId = "system";
                if (parameters.TryGetValue("ActorId", out var actRaw) && actRaw != null)
                    actorId = actRaw.ToString()!;

                string details = string.Empty;
                if (parameters.TryGetValue("Details", out var detRaw) && detRaw != null)
                    details = detRaw.ToString()!;

                // Build a dynamic audit record using an anonymous dictionary
                var record = new Dictionary<string, object>
                {
                    ["RecordId"]   = ridRaw?.ToString() ?? string.Empty,
                    ["Event"]      = eventRaw?.ToString() ?? string.Empty,
                    ["ActorId"]    = actorId,
                    ["Details"]    = details,
                    ["OccurredAt"] = DateTime.UtcNow.ToString("o")
                };

                var err = ds.InsertEntity(entityRaw.ToString(), record);
                bool ok = err == null || err.Flag == Errors.Ok;

                output["Result"]  = ok;
                output["Audited"] = ok;
                return (output, ok);
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
