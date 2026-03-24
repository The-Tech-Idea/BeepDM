using System;
using System.Collections.Generic;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Rules.BuiltinRules.Flow
{
    /// <summary>
    /// Writes a failed record to a dead-letter entity table so it can be
    /// inspected and reprocessed later without losing data.
    /// Parameters:
    ///   <c>DataSourceName</c> — name of the registered data source.
    ///   <c>EntityName</c>     — target dead-letter table / collection name.
    ///   <c>RecordId</c>       — identifier of the original record.
    ///   <c>FailureReason</c>  — human-readable error description.
    ///   <c>Payload</c>        — original payload (string or object; stored as ToString()).
    ///   <c>IDMEEditor</c>     — injected <see cref="IDMEEditor"/> instance.
    /// Returns <c>true</c> on successful write, <c>false</c> on failure.
    /// </summary>
    [Rule(ruleKey: "Flow.DeadLetterQueue", ParserKey = "RulesParser", RuleName = "DeadLetterQueue")]
    public sealed class DeadLetterQueue : IRule
    {
        public string RuleText { get; set; } = "Flow.DeadLetterQueue";
        public IRuleStructure Structure { get; set; } = new RuleStructure();

        public (Dictionary<string, object> outputs, object result) SolveRule(
            Dictionary<string, object> parameters = null)
        {
            var output = new Dictionary<string, object>();

            if (parameters == null)
            {
                output["Error"] = "No parameters supplied.";
                return (output, false);
            }

            parameters.TryGetValue("IDMEEditor",     out var editorRaw);
            parameters.TryGetValue("DataSourceName", out var dsNameRaw);
            parameters.TryGetValue("EntityName",     out var entityRaw);
            parameters.TryGetValue("RecordId",       out var idRaw);
            parameters.TryGetValue("FailureReason",  out var reasonRaw);
            parameters.TryGetValue("Payload",        out var payloadRaw);

            var editor     = editorRaw    as IDMEEditor;
            var dsName     = dsNameRaw?.ToString();
            var entityName = entityRaw?.ToString();

            if (editor == null || string.IsNullOrWhiteSpace(dsName) || string.IsNullOrWhiteSpace(entityName))
            {
                output["Error"] = "IDMEEditor, DataSourceName and EntityName are required.";
                return (output, false);
            }

            try
            {
                var ds = editor.GetDataSource(dsName);
                if (ds == null)
                {
                    output["Error"] = $"DataSource '{dsName}' not found.";
                    return (output, false);
                }

                var entry = new Dictionary<string, object>
                {
                    ["RecordId"]      = idRaw?.ToString()      ?? string.Empty,
                    ["FailureReason"] = reasonRaw?.ToString()  ?? string.Empty,
                    ["Payload"]       = payloadRaw?.ToString() ?? string.Empty,
                    ["QueuedAt"]      = DateTime.UtcNow.ToString("o"),
                };

                var err = ds.InsertEntity(entityName, entry);
                bool ok = err?.Flag == Errors.Ok;

                output["Result"]  = ok;
                output["Queued"]  = ok;
                if (!ok)
                    output["InsertError"] = err?.Message ?? "Unknown error";

                return (output, ok);
            }
            catch (Exception ex)
            {
                output["Error"]  = ex.Message;
                output["Result"] = false;
                return (output, false);
            }
        }
    }
}
