using System.Collections.Generic;

namespace TheTechIdea.Beep.Services.Telemetry.Context
{
    /// <summary>
    /// Stamps every envelope with a <c>correlationId</c>. Reuses the producer-
    /// supplied <see cref="TelemetryEnvelope.CorrelationId"/> when present, then
    /// the current <see cref="BeepActivityScope.Current"/> trace id, and only
    /// generates a fresh GUID-N when no scope or upstream id exists.
    /// </summary>
    /// <remarks>
    /// Always writes the chosen value back to
    /// <see cref="TelemetryEnvelope.CorrelationId"/> so downstream enrichers
    /// and sinks see a single canonical field, and mirrors it into
    /// <see cref="TelemetryEnvelope.Properties"/> for sinks that flatten the
    /// property bag (e.g. the NDJSON file sink).
    /// </remarks>
    public sealed class CorrelationEnricher : IEnricher
    {
        /// <inheritdoc/>
        public string Name => "correlation";

        /// <inheritdoc/>
        public void Enrich(TelemetryEnvelope envelope)
        {
            if (envelope is null)
            {
                return;
            }

            string correlationId = envelope.CorrelationId;
            if (string.IsNullOrEmpty(correlationId))
            {
                BeepActivity current = BeepActivityScope.Current;
                correlationId = current?.TraceId ?? IdGenerators.NewCorrelationId();
                envelope.CorrelationId = correlationId;
            }

            if (envelope.Properties is null)
            {
                envelope.Properties = new Dictionary<string, object>();
            }
            if (!envelope.Properties.ContainsKey(EnrichmentProperties.CorrelationId))
            {
                envelope.Properties[EnrichmentProperties.CorrelationId] = correlationId;
            }
        }
    }
}
