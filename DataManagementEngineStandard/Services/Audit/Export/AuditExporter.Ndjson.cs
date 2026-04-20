using System.Collections.Generic;
using System.IO;
using TheTechIdea.Beep.Services.Audit.Models;
using TheTechIdea.Beep.Services.Logging;
using TheTechIdea.Beep.Services.Telemetry;

namespace TheTechIdea.Beep.Services.Audit.Export
{
    /// <summary>
    /// NDJSON renderer for <see cref="AuditExporter"/>. Wraps each
    /// audit event in a thin <see cref="TelemetryEnvelope"/> so the
    /// canonical line format matches what the file/SQLite sinks emit
    /// at write time. The output is therefore directly verifiable
    /// against the chain hashes already present in the store.
    /// </summary>
    public sealed partial class AuditExporter
    {
        private static byte[] WriteNdjson(IReadOnlyList<AuditEvent> events)
        {
            using var ms = new MemoryStream();
            for (int i = 0; i < events.Count; i++)
            {
                AuditEvent ev = events[i];
                if (ev is null)
                {
                    continue;
                }
                TelemetryEnvelope envelope = WrapEnvelope(ev);
                byte[] line = NdjsonSerializer.SerializeLine(envelope);
                ms.Write(line, 0, line.Length);
            }
            return ms.ToArray();
        }

        private static TelemetryEnvelope WrapEnvelope(AuditEvent ev)
        {
            return new TelemetryEnvelope
            {
                Kind = TelemetryKind.Audit,
                Level = BeepLogLevel.Information,
                TimestampUtc = ev.TimestampUtc,
                Category = ev.Source,
                Message = ev.EntityName,
                CorrelationId = ev.CorrelationId,
                TraceId = ev.TraceId,
                Audit = ev
            };
        }
    }
}
