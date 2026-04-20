using System.Collections.Generic;

namespace TheTechIdea.Beep.Services.Telemetry
{
    /// <summary>
    /// Cloning support for <see cref="TelemetryEnvelope"/>. Used by the
    /// per-sink redaction path so a sink-local mutation cannot leak into
    /// other sinks running in the same fan-out.
    /// </summary>
    public sealed partial class TelemetryEnvelope
    {
        /// <summary>
        /// Returns a shallow envelope copy with the property bag duplicated.
        /// The <see cref="Audit"/> reference and <see cref="Exception"/>
        /// instance are shared because both are treated as immutable past
        /// envelope creation.
        /// </summary>
        public TelemetryEnvelope Clone()
        {
            IDictionary<string, object> propertiesCopy = null;
            if (Properties != null)
            {
                propertiesCopy = new Dictionary<string, object>(Properties);
            }

            return new TelemetryEnvelope
            {
                Kind = Kind,
                TimestampUtc = TimestampUtc,
                Category = Category,
                Level = Level,
                Message = Message,
                Exception = Exception,
                Properties = propertiesCopy,
                Audit = Audit,
                TraceId = TraceId,
                CorrelationId = CorrelationId
            };
        }
    }
}
