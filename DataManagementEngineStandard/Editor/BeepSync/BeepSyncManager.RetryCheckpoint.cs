using System;
using System.Collections.Generic;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor.BeepSync;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Editor
{
    public partial class BeepSyncManager
    {
        /// <summary>
        /// Classifies an error into (category, action) to decide whether to retry.
        /// Evaluates <see cref="RetryPolicy.ErrorCategoryRuleKey"/> via the Rule Engine when
        /// available; falls back to built-in keyword heuristics.
        /// </summary>
        private (string category, string action) TryClassifyError(
            DataSyncSchema schema, string errorMessage, int attemptCount)
        {
            var rp  = schema?.RetryPolicy;
            var ctx = IntegrationContext;

            if (rp != null && !string.IsNullOrWhiteSpace(rp.ErrorCategoryRuleKey)
                && ctx?.RuleEngine != null && ctx.RuleEngine.HasRule(rp.ErrorCategoryRuleKey))
            {
                try
                {
                    var (outputs, _) = ctx.RuleEngine.SolveRule(
                        rp.ErrorCategoryRuleKey,
                        new Dictionary<string, object>
                        {
                            ["errorMessage"] = errorMessage ?? string.Empty,
                            ["attemptCount"] = attemptCount,
                            ["schemaId"]     = schema.Id
                        });

                    var cat    = outputs?.TryGetValue("category", out var c) == true ? c?.ToString() : "Transient";
                    var action = outputs?.TryGetValue("action",   out var a) == true ? a?.ToString() : "Retry";
                    return (cat ?? "Transient", action ?? "Retry");
                }
                catch (Exception ex)
                {
                    _editor.AddLogMessage("BeepSync",
                        $"Error-category rule '{rp.ErrorCategoryRuleKey}' threw: {ex.Message}",
                        DateTime.Now, -1, "", Errors.Failed);
                }
            }

            // Built-in heuristics
            var msg = errorMessage?.ToLowerInvariant() ?? string.Empty;
            if (msg.Contains("timeout")    || msg.Contains("network") || msg.Contains("connection"))
                return ("Transient",  "Retry");
            if (msg.Contains("validation") || msg.Contains("mapping") || msg.Contains("schema"))
                return ("Validation", "Quarantine");
            if (msg.Contains("conflict"))
                return ("Conflict",   "Retry");
            return ("Fatal", "Abort");
        }

        /// <summary>
        /// Decides whether a previously saved checkpoint is still safe to resume.
        /// Evaluates <c>sync.checkpoint.resume-safe</c> via the Rule Engine when available;
        /// falls back to the resume-window hours from <see cref="RetryPolicy"/>.
        /// </summary>
        private bool IsCheckpointResumeSafe(DataSyncSchema schema, SyncCheckpoint checkpoint)
        {
            if (checkpoint == null) return false;
            if (string.Equals(checkpoint.Status, "Completed", StringComparison.OrdinalIgnoreCase)) return false;

            var ctx = IntegrationContext;
            if (ctx?.RuleEngine != null && ctx.RuleEngine.HasRule("sync.checkpoint.resume-safe"))
            {
                try
                {
                    var (_, result) = ctx.RuleEngine.SolveRule(
                        "sync.checkpoint.resume-safe",
                        new Dictionary<string, object>
                        {
                            ["checkpoint"] = checkpoint,
                            ["schemaId"]   = schema?.Id,
                            ["savedAt"]    = checkpoint.SavedAt
                        });
                    return result is bool b ? b : result?.ToString() != "false";
                }
                catch { /* fall through */ }
            }

            int windowHours = schema?.RetryPolicy?.MaxResumeWindowHours > 0
                ? schema.RetryPolicy.MaxResumeWindowHours : 24;
            return (DateTime.UtcNow - checkpoint.SavedAt).TotalHours <= windowHours;
        }
    }
}
