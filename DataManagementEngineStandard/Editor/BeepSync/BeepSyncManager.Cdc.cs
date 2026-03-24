using System;
using System.Collections.Generic;
using System.Threading;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor.BeepSync;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Editor
{
    public partial class BeepSyncManager
    {
        /// <summary>
        /// Builds a <see cref="CdcFilterContext"/> from the schema's <see cref="WatermarkPolicy"/>.
        /// Returns <c>null</c> when no watermark policy is set (full-load mode).
        /// </summary>
        private CdcFilterContext BuildCdcFilterContext(DataSyncSchema schema, CancellationToken token)
        {
            var wp = schema?.WatermarkPolicy;
            if (wp == null) return null;

            var ctx = new CdcFilterContext
            {
                SchemaId       = schema.Id,
                WatermarkField = wp.WatermarkField,
                WindowEnd      = DateTime.UtcNow
            };

            // Lower bound with overlap window
            ctx.WindowStart = wp.LastWatermarkValue is DateTime lastDt
                ? lastDt.AddSeconds(-wp.OverlapWindowSeconds)
                : wp.LastWatermarkValue;

            // Default range filter
            if (!string.IsNullOrWhiteSpace(wp.WatermarkField) && ctx.WindowStart != null)
            {
                ctx.ResolvedFilters.Add(new AppFilter
                {
                    FieldName   = wp.WatermarkField,
                    Operator    = ">",
                    FilterValue = ctx.WindowStart?.ToString() ?? string.Empty
                });
            }

            var ictx = IntegrationContext;

            // Custom CDC filter rule
            if (ictx?.RuleEngine != null && !string.IsNullOrWhiteSpace(wp.FilterRuleKey)
                && ictx.RuleEngine.HasRule(wp.FilterRuleKey))
            {
                try
                {
                    var (outputs, _) = ictx.RuleEngine.SolveRule(
                        wp.FilterRuleKey,
                        new Dictionary<string, object>
                        {
                            ["watermarkField"] = wp.WatermarkField,
                            ["lastWatermark"]  = wp.LastWatermarkValue,
                            ["overlapSeconds"] = wp.OverlapWindowSeconds,
                            ["sourceDs"]       = schema.SourceDataSourceName
                        });

                    if (outputs?.TryGetValue("filters", out var ruleFilters) == true
                        && ruleFilters is List<AppFilter> filterList)
                        ctx.ResolvedFilters = filterList;
                }
                catch (Exception ex)
                {
                    _editor.AddLogMessage("BeepSync",
                        $"CDC filter rule '{wp.FilterRuleKey}' threw: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                }
            }

            // Late-arrival rule
            if (ictx?.RuleEngine != null && !string.IsNullOrWhiteSpace(wp.LateArrivalRuleKey)
                && ictx.RuleEngine.HasRule(wp.LateArrivalRuleKey))
            {
                try
                {
                    var (outputs, _) = ictx.RuleEngine.SolveRule(
                        wp.LateArrivalRuleKey,
                        new Dictionary<string, object>
                        {
                            ["windowClose"]    = ctx.WindowEnd,
                            ["overlapSeconds"] = wp.OverlapWindowSeconds
                        });

                    if (outputs?.TryGetValue("action", out var action) == true)
                        ctx.LateArrivalAction = action?.ToString() ?? "include";
                }
                catch (Exception ex)
                {
                    _editor.AddLogMessage("BeepSync",
                        $"Late-arrival rule '{wp.LateArrivalRuleKey}' threw: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                }
            }

            ctx.NewWatermarkValue = ctx.WindowEnd;
            return ctx;
        }
    }
}
