using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor.BeepSync;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Editor
{
    public partial class BeepSyncManager
    {
        private static IErrorsInfo ValidateWatermarkPolicy(DataSyncSchema schema)
        {
            var policy = schema?.WatermarkPolicy;
            if (policy == null) return new ErrorsInfo { Flag = Errors.Ok };
            string failure = null;
            if (!string.Equals(policy.WatermarkMode, "Timestamp", StringComparison.OrdinalIgnoreCase))
                failure = $"Watermark mode '{policy.WatermarkMode}' is not supported by this sync executor. Only Timestamp is currently supported.";
            else if (string.IsNullOrWhiteSpace(policy.WatermarkField))
                failure = "Timestamp watermark requires a source field.";
            else if (policy.LastWatermarkValue != null && policy.LastWatermarkValue is not DateTime && policy.LastWatermarkValue is not DateTimeOffset)
                failure = "Timestamp watermark must be a DateTime or DateTimeOffset value.";
            else if (policy.OverlapWindowSeconds < 0)
                failure = "Watermark overlap cannot be negative.";
            return new ErrorsInfo { Flag = failure == null ? Errors.Ok : Errors.Failed, Message = failure };
        }
        /// <summary>
        /// Builds a <see cref="CdcFilterContext"/> from the schema's <see cref="WatermarkPolicy"/>.
        /// Returns <c>null</c> when no watermark policy is set (full-load mode).
        /// </summary>
        private CdcFilterContext BuildCdcFilterContext(DataSyncSchema schema, CancellationToken token)
        {
            var wp = schema?.WatermarkPolicy;
            if (wp == null) return null;
            token.ThrowIfCancellationRequested();
            var validation = ValidateWatermarkPolicy(schema);
            if (validation.Flag != Errors.Ok) throw new NotSupportedException(validation.Message);

            var ctx = new CdcFilterContext
            {
                SchemaId       = schema.Id,
                WatermarkField = wp.WatermarkField,
                WindowEnd      = DateTime.UtcNow
            };

            // Lower bound with overlap window
            var lastValue = wp.LastWatermarkValue is DateTimeOffset offset ? offset.UtcDateTime : wp.LastWatermarkValue;
            ctx.WindowStart = lastValue is DateTime lastDt
                ? lastDt.AddSeconds(-wp.OverlapWindowSeconds)
                : lastValue;

            // Default range filter
            if (!string.IsNullOrWhiteSpace(wp.WatermarkField) && ctx.WindowStart != null)
            {
                ctx.ResolvedFilters.Add(new AppFilter
                {
                    FieldName   = wp.WatermarkField,
                    Operator    = ">",
                    FilterValue = ((DateTime)ctx.WindowStart).ToString("O", CultureInfo.InvariantCulture)
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

            // Keep the read window bounded; rows newer than this run belong to the next run.
            ctx.ResolvedFilters.Add(new AppFilter
            {
                FieldName = wp.WatermarkField,
                Operator = "<=",
                FilterValue = ((DateTime)ctx.WindowEnd).ToString("O", CultureInfo.InvariantCulture)
            });
            ctx.NewWatermarkValue = ctx.WindowEnd;
            return ctx;
        }
    }
}
