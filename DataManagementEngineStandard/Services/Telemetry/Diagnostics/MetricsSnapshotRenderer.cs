using System;
using System.Globalization;
using System.Text;

namespace TheTechIdea.Beep.Services.Telemetry.Diagnostics
{
    /// <summary>
    /// Renders <see cref="MetricsSnapshot"/> instances to text or JSON.
    /// Kept in its own helper so the hosted service stays small and the
    /// renderer is reusable from ad-hoc diagnostics tools.
    /// </summary>
    internal static class MetricsSnapshotRenderer
    {
        public static string Render(MetricsSnapshot snapshot, MetricsSnapshotFormat format)
        {
            if (snapshot is null)
            {
                return string.Empty;
            }
            return format == MetricsSnapshotFormat.Json
                ? RenderJson(snapshot)
                : RenderText(snapshot);
        }

        private static string RenderText(MetricsSnapshot s)
        {
            var sb = new StringBuilder(512);
            sb.Append("# ").Append(s.PipelineName).Append(' ')
              .Append(s.CapturedUtc.ToString("O", CultureInfo.InvariantCulture)).AppendLine();
            sb.Append("queue.depth=").Append(s.QueueDepthCurrent)
              .Append(" queue.capacity=").Append(s.QueueCapacity)
              .Append(" backpressure=").Append(s.BackpressureMode).AppendLine();
            sb.Append("log.enqueued=").Append(s.LogEnqueuedTotal)
              .Append(" audit.enqueued=").Append(s.AuditEnqueuedTotal).AppendLine();
            sb.Append("dropped.sampled=").Append(s.DroppedSampledTotal)
              .Append(" dropped.deduped=").Append(s.DroppedDedupedTotal)
              .Append(" dropped.rateLimited=").Append(s.DroppedRateLimitedTotal)
              .Append(" dropped.queueFull=").Append(s.DroppedQueueFullTotal).AppendLine();
            sb.Append("sink.errors=").Append(s.SinkErrorsTotal)
              .Append(" flush.lastMs=").Append(s.LastFlushLatencyMs).AppendLine();
            sb.Append("sweeper.deleted=").Append(s.SweeperDeletesTotal)
              .Append(" sweeper.compressed=").Append(s.SweeperCompressTotal)
              .Append(" budget.breaches=").Append(s.BudgetBreachesTotal)
              .Append(" budget.blocking=").Append(s.IsBlockingWrites).AppendLine();
            sb.Append("chain.signed=").Append(s.ChainSignedTotal)
              .Append(" chain.verified=").Append(s.ChainVerifiedTotal)
              .Append(" chain.divergence=").Append(s.ChainDivergenceTotal).AppendLine();
            sb.Append("self.emitted=").Append(s.SelfEventsEmittedTotal)
              .Append(" self.deduped=").Append(s.SelfEventsDedupedTotal).AppendLine();
            sb.Append("sinks.allHealthy=").Append(s.AllSinksHealthy).AppendLine();
            if (s.Sinks is not null)
            {
                for (int i = 0; i < s.Sinks.Count; i++)
                {
                    SinkHealth h = s.Sinks[i];
                    if (h is null) { continue; }
                    sb.Append("sink[").Append(h.Name).Append("] healthy=").Append(h.IsHealthy)
                      .Append(" written=").Append(h.WrittenCount)
                      .Append(" failures=").Append(h.ConsecutiveFailures).AppendLine();
                }
            }
            return sb.ToString();
        }

        private static string RenderJson(MetricsSnapshot s)
        {
            var sb = new StringBuilder(512);
            sb.Append('{');
            AppendField(sb, "pipeline", s.PipelineName, first: true);
            AppendField(sb, "capturedUtc", s.CapturedUtc.ToString("O", CultureInfo.InvariantCulture));
            AppendField(sb, "queueDepth", s.QueueDepthCurrent);
            AppendField(sb, "queueCapacity", s.QueueCapacity);
            AppendField(sb, "backpressureMode", s.BackpressureMode.ToString());
            AppendField(sb, "logEnqueued", s.LogEnqueuedTotal);
            AppendField(sb, "auditEnqueued", s.AuditEnqueuedTotal);
            AppendField(sb, "droppedSampled", s.DroppedSampledTotal);
            AppendField(sb, "droppedDeduped", s.DroppedDedupedTotal);
            AppendField(sb, "droppedRateLimited", s.DroppedRateLimitedTotal);
            AppendField(sb, "droppedQueueFull", s.DroppedQueueFullTotal);
            AppendField(sb, "sinkErrors", s.SinkErrorsTotal);
            AppendField(sb, "lastFlushLatencyMs", s.LastFlushLatencyMs);
            AppendField(sb, "sweeperDeleted", s.SweeperDeletesTotal);
            AppendField(sb, "sweeperCompressed", s.SweeperCompressTotal);
            AppendField(sb, "budgetBreaches", s.BudgetBreachesTotal);
            AppendField(sb, "isBlockingWrites", s.IsBlockingWrites);
            AppendField(sb, "chainSigned", s.ChainSignedTotal);
            AppendField(sb, "chainVerified", s.ChainVerifiedTotal);
            AppendField(sb, "chainDivergence", s.ChainDivergenceTotal);
            AppendField(sb, "selfEmitted", s.SelfEventsEmittedTotal);
            AppendField(sb, "selfDeduped", s.SelfEventsDedupedTotal);
            AppendField(sb, "allSinksHealthy", s.AllSinksHealthy);
            sb.Append(",\"sinks\":[");
            if (s.Sinks is not null)
            {
                for (int i = 0; i < s.Sinks.Count; i++)
                {
                    SinkHealth h = s.Sinks[i];
                    if (h is null) { continue; }
                    if (i > 0) { sb.Append(','); }
                    sb.Append('{');
                    AppendField(sb, "name", h.Name, first: true);
                    AppendField(sb, "healthy", h.IsHealthy);
                    AppendField(sb, "written", h.WrittenCount);
                    AppendField(sb, "failures", h.ConsecutiveFailures);
                    if (h.LastSuccessUtc.HasValue)
                    {
                        AppendField(sb, "lastSuccessUtc",
                            h.LastSuccessUtc.Value.ToString("O", CultureInfo.InvariantCulture));
                    }
                    if (h.LastErrorUtc.HasValue)
                    {
                        AppendField(sb, "lastErrorUtc",
                            h.LastErrorUtc.Value.ToString("O", CultureInfo.InvariantCulture));
                    }
                    if (!string.IsNullOrEmpty(h.LastError))
                    {
                        AppendField(sb, "lastError", h.LastError);
                    }
                    sb.Append('}');
                }
            }
            sb.Append("]}");
            return sb.ToString();
        }

        private static void AppendField(StringBuilder sb, string name, string value, bool first = false)
        {
            if (!first) { sb.Append(','); }
            sb.Append('"').Append(name).Append("\":");
            if (value is null) { sb.Append("null"); return; }
            sb.Append('"');
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) { sb.Append("\\u").Append(((int)c).ToString("X4", CultureInfo.InvariantCulture)); }
                        else { sb.Append(c); }
                        break;
                }
            }
            sb.Append('"');
        }

        private static void AppendField(StringBuilder sb, string name, long value, bool first = false)
        {
            if (!first) { sb.Append(','); }
            sb.Append('"').Append(name).Append("\":")
              .Append(value.ToString(CultureInfo.InvariantCulture));
        }

        private static void AppendField(StringBuilder sb, string name, int value, bool first = false)
            => AppendField(sb, name, (long)value, first);

        private static void AppendField(StringBuilder sb, string name, bool value, bool first = false)
        {
            if (!first) { sb.Append(','); }
            sb.Append('"').Append(name).Append("\":")
              .Append(value ? "true" : "false");
        }
    }
}
