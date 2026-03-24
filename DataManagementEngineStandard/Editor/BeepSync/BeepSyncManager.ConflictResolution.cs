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
        /// Evaluates whether the bidirectional reverse-import path should be blocked based on
        /// <see cref="ConflictPolicy"/> and the Rule Engine.
        /// </summary>
        private (bool Quarantine, string Reason) TryEvaluateConflictGate(DataSyncSchema schema)
        {
            var cp  = schema?.ConflictPolicy;
            var ctx = IntegrationContext;

            if (cp == null || string.IsNullOrWhiteSpace(cp.ResolutionRuleKey))
                return (false, null);

            if (ctx?.RuleEngine != null && ctx.RuleEngine.HasRule(cp.ResolutionRuleKey))
            {
                try
                {
                    var started = DateTime.UtcNow;
                    var (outputs, result) = ctx.RuleEngine.SolveRule(
                        cp.ResolutionRuleKey,
                        new Dictionary<string, object>
                        {
                            ["schemaId"]   = schema.Id,
                            ["entityName"] = schema.SourceEntityName,
                            ["direction"]  = schema.SyncDirection
                        },
                        BuildRulePolicy(schema));

                    var winner     = outputs?.TryGetValue("winner",     out var w)  == true ? w?.ToString()  : result?.ToString();
                    var reasonCode = outputs?.TryGetValue("reasonCode", out var rc) == true ? rc?.ToString() : "RULE";
                    var elapsed    = DateTime.UtcNow - started;

                    if (cp.CaptureEvidence)
                    {
                        LastRunConflicts.Add(new ConflictEvidence
                        {
                            SchemaId    = schema.Id,
                            EntityName  = schema.SourceEntityName,
                            RuleKey     = cp.ResolutionRuleKey,
                            Winner      = winner ?? "fallback",
                            ReasonCode  = reasonCode,
                            RuleElapsed = elapsed,
                            DetectedAt  = DateTime.UtcNow
                        });
                    }

                    bool quarantine = string.Equals(winner, "quarantine", StringComparison.OrdinalIgnoreCase)
                                   || string.Equals(winner, "false",      StringComparison.OrdinalIgnoreCase);
                    return (quarantine, reasonCode);
                }
                catch (Exception ex)
                {
                    _editor.AddLogMessage("BeepSync",
                        $"Conflict rule '{cp.ResolutionRuleKey}' threw: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                }
            }

            // Built-in fallback: DestinationWins blocks reverse import
            bool blockReverse = string.Equals(schema.ConflictResolutionStrategy, "DestinationWins",
                StringComparison.OrdinalIgnoreCase);
            return (blockReverse, blockReverse ? "DestinationWins-NoReverseImport" : null);
        }
    }
}
