using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor.BeepSync;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Rules;

namespace TheTechIdea.Beep.Editor
{
    public partial class BeepSyncManager
    {
        /// <summary>
        /// Runs all preflight checks against <paramref name="schema"/> without executing any data movement.
        /// </summary>
        public Task<SyncPreflightReport> RunPreflightAsync(
            DataSyncSchema schema,
            CancellationToken token = default)
        {
            var report = new SyncPreflightReport { PlanId = schema?.Id };

            if (schema == null)
            {
                report.AddError("SCHEMA-NULL", "Schema", "Schema cannot be null.");
                return Task.FromResult(report);
            }

            // 1. Structural validation
            var structuralResult = _validationHelper.ValidateSyncOperation(schema);
            if (structuralResult.Flag == Errors.Failed)
                report.AddError("SCHEMA-INVALID", "Schema", structuralResult.Message ?? "Schema structural validation failed.");

            // 2. Rule Engine preflight
            var ctx = IntegrationContext;
            if (ctx?.RuleEngine != null && schema.RulePolicy?.Enabled == true)
            {
                try
                {
                    var policy = BuildRulePolicy(schema, 5000);
                    if (ctx.RuleEngine.HasRule("sync.plan.validate"))
                    {
                        var (_, result) = ctx.RuleEngine.SolveRule(
                            "sync.plan.validate",
                            new Dictionary<string, object>
                            {
                                ["schema"]    = schema,
                                ["sourceDs"]  = schema.SourceDataSourceName,
                                ["destDs"]    = schema.DestinationDataSourceName,
                                ["direction"] = schema.SyncDirection
                            },
                            policy);

                        bool passed = result is bool b ? b : result?.ToString() != "false";
                        if (!passed)
                        {
                            report.RulesPassed = false;
                            report.AddError("RULE-PLAN-VALIDATE-FAILED", "Rules",
                                $"Rule 'sync.plan.validate' rejected schema '{schema.Id}'.");
                        }
                        else
                            report.AddInfo("RULE-PLAN-VALIDATE-PASSED", "Rules", "Plan validation rule passed.");
                    }
                }
                catch (Exception ex)
                {
                    report.RulesPassed = false;
                    report.AddError("RULE-PREFLIGHT-EXCEPTION", "Rules",
                        $"Rule Engine threw during preflight: {ex.Message}");
                }
            }

            // 3. Defaults Manager preflight
            if (ctx?.DefaultsManager != null && schema.DefaultsPolicy?.Enabled == true)
            {
                var profile = ctx.DefaultsManager.GetProfile(
                    schema.DestinationDataSourceName, schema.DestinationEntityName);
                if (profile == null || profile.Rules.Count == 0)
                {
                    report.DefaultsReady = false;
                    report.AddWarning("DEFAULTS-PROFILE-MISSING", "Defaults",
                        $"No defaults profile for '{schema.DestinationEntityName}' in '{schema.DestinationDataSourceName}'.");
                }
                else
                    report.AddInfo("DEFAULTS-PROFILE-READY", "Defaults", $"Defaults profile found ({profile.Rules.Count} rules).");
            }

            // 4. Watermark policy
            if (schema.WatermarkPolicy != null)
            {
                var wmResult = _validationHelper.ValidateWatermarkPolicy(schema);
                if (wmResult.Flag == Errors.Failed)
                    report.AddError("WATERMARK-INVALID", "Schema", wmResult.Message ?? "Watermark policy validation failed.");
                else
                    report.AddInfo("WATERMARK-READY", "Schema", wmResult.Message ?? "Watermark policy valid.");
            }

            // 5. Conflict policy rule existence check
            var cp = schema.ConflictPolicy;
            if (cp != null && !string.IsNullOrWhiteSpace(cp.ResolutionRuleKey))
            {
                if (ctx?.RuleEngine != null && !ctx.RuleEngine.HasRule(cp.ResolutionRuleKey))
                    report.AddWarning("CONFLICT-RULE-MISSING", "Schema",
                        $"Conflict rule '{cp.ResolutionRuleKey}' not registered. Falling back to string strategy.");
                else if (ctx?.RuleEngine != null)
                    report.AddInfo("CONFLICT-RULE-READY", "Schema", $"Conflict rule '{cp.ResolutionRuleKey}' registered.");
            }

            // 6. Retry error-category rule existence check
            var rp = schema.RetryPolicy;
            if (rp != null && !string.IsNullOrWhiteSpace(rp.ErrorCategoryRuleKey))
            {
                if (ctx?.RuleEngine != null && !ctx.RuleEngine.HasRule(rp.ErrorCategoryRuleKey))
                    report.AddWarning("RETRY-RULE-MISSING", "Schema",
                        $"Error-category rule '{rp.ErrorCategoryRuleKey}' not registered. Using built-in fallback.");
                else if (ctx?.RuleEngine != null)
                    report.AddInfo("RETRY-RULE-READY", "Schema", $"Error-category rule '{rp.ErrorCategoryRuleKey}' registered.");
            }

            // 7. Mapping quality gate
            if (schema.MappingPolicy?.Enabled == true && schema.MappingPolicy.MinQualityScore > 0)
            {
                var mqResult = _validationHelper.CheckMappingQualityGate(schema, out int mqScore, out string mqBand);
                report.MappingScore = mqScore;
                if (mqResult.Flag == Errors.Failed)
                    report.AddError("MAPPING-QUALITY-FAILED", "Mapping",
                        mqResult.Message ?? $"Mapping quality {mqScore} below threshold.");
                else
                    report.AddInfo("MAPPING-QUALITY-OK", "Mapping",
                        mqResult.Message ?? $"Mapping quality {mqScore} ({mqBand}) meets threshold.");
            }

            return Task.FromResult(report);
        }

        // ── Shared rule-policy builder ─────────────────────────────────────────────

        internal static RuleExecutionPolicy BuildRulePolicy(DataSyncSchema schema, int defaultMaxMs = 5000) =>
            new RuleExecutionPolicy
            {
                MaxDepth       = schema.RulePolicy?.MaxDepth > 0 ? schema.RulePolicy.MaxDepth : 10,
                MaxExecutionMs = schema.RulePolicy?.MaxExecutionMs > 0 ? schema.RulePolicy.MaxExecutionMs : defaultMaxMs
            };
    }
}
