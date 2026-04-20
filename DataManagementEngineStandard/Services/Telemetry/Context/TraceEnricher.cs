using System.Collections.Generic;
using System.Diagnostics;

namespace TheTechIdea.Beep.Services.Telemetry.Context
{
    /// <summary>
    /// Stamps trace context (<c>traceId</c>, <c>spanId</c>,
    /// <c>parentSpanId</c>) onto every envelope. Source preference order:
    /// <list type="number">
    ///   <item><description>The producer-supplied <see cref="TelemetryEnvelope.TraceId"/>.</description></item>
    ///   <item><description><see cref="Activity.Current"/> when an OTel-compatible
    ///   ambient activity exists (covers ASP.NET Core, gRPC, HttpClient, EF, etc.).</description></item>
    ///   <item><description><see cref="BeepActivityScope.Current"/>.</description></item>
    /// </list>
    /// When all three are empty the enricher is a no-op so envelopes outside
    /// any traced operation are not tagged with synthetic ids.
    /// </summary>
    public sealed class TraceEnricher : IEnricher
    {
        /// <inheritdoc/>
        public string Name => "trace";

        /// <inheritdoc/>
        public void Enrich(TelemetryEnvelope envelope)
        {
            if (envelope is null)
            {
                return;
            }

            string traceId;
            string spanId;
            string parentSpanId;
            if (!TryReadAmbient(out traceId, out spanId, out parentSpanId))
            {
                return;
            }

            if (string.IsNullOrEmpty(envelope.TraceId))
            {
                envelope.TraceId = traceId;
            }

            if (envelope.Properties is null)
            {
                envelope.Properties = new Dictionary<string, object>();
            }

            WriteIfMissing(envelope.Properties, EnrichmentProperties.TraceId, traceId);
            WriteIfMissing(envelope.Properties, EnrichmentProperties.SpanId, spanId);
            WriteIfMissing(envelope.Properties, EnrichmentProperties.ParentSpanId, parentSpanId);
        }

        private static bool TryReadAmbient(out string traceId, out string spanId, out string parentSpanId)
        {
            Activity activity = Activity.Current;
            if (activity != null && activity.IdFormat == ActivityIdFormat.W3C)
            {
                traceId = activity.TraceId.ToHexString();
                spanId = activity.SpanId.ToHexString();
                parentSpanId = activity.ParentSpanId == default
                    ? null
                    : activity.ParentSpanId.ToHexString();
                return true;
            }

            BeepActivity scope = BeepActivityScope.Current;
            if (scope != null)
            {
                traceId = scope.TraceId;
                spanId = scope.SpanId;
                parentSpanId = scope.ParentSpanId;
                return true;
            }

            traceId = null;
            spanId = null;
            parentSpanId = null;
            return false;
        }

        private static void WriteIfMissing(IDictionary<string, object> bag, string key, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }
            if (!bag.ContainsKey(key))
            {
                bag[key] = value;
            }
        }
    }
}
