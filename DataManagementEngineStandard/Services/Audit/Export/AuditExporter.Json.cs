using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using TheTechIdea.Beep.Services.Audit.Models;

namespace TheTechIdea.Beep.Services.Audit.Export
{
    /// <summary>
    /// JSON renderer for <see cref="AuditExporter"/>. Produces a single
    /// document <c>{ "events": [...] }</c> containing every event in
    /// canonical form. The matching <see cref="ExportManifest"/> ships
    /// in a sibling <c>.manifest.json</c> file so payload and manifest
    /// can rotate independently.
    /// </summary>
    public sealed partial class AuditExporter
    {
        private static readonly JsonWriterOptions JsonOptions = new JsonWriterOptions
        {
            Indented = false,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private static byte[] WriteJson(IReadOnlyList<AuditEvent> events)
        {
            using var ms = new MemoryStream();
            using (var writer = new Utf8JsonWriter(ms, JsonOptions))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("events");
                writer.WriteStartArray();
                for (int i = 0; i < events.Count; i++)
                {
                    AuditEvent ev = events[i];
                    if (ev is null) { continue; }
                    WriteAuditEvent(writer, ev);
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
            }
            return ms.ToArray();
        }

        internal static byte[] WriteManifestJson(ExportManifest manifest)
        {
            using var ms = new MemoryStream();
            using (var writer = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
            {
                writer.WriteStartObject();
                writer.WriteNumber("version", manifest.Version);
                writer.WriteString("createdUtc", manifest.CreatedUtc.ToString("O", CultureInfo.InvariantCulture));
                WriteOptionalString(writer, "operatorId", manifest.OperatorId);
                WriteOptionalString(writer, "format", manifest.Format);
                writer.WriteNumber("eventCount", manifest.EventCount);
                if (manifest.FromUtc.HasValue)
                {
                    writer.WriteString("fromUtc", manifest.FromUtc.Value.ToString("O", CultureInfo.InvariantCulture));
                }
                if (manifest.ToUtc.HasValue)
                {
                    writer.WriteString("toUtc", manifest.ToUtc.Value.ToString("O", CultureInfo.InvariantCulture));
                }
                writer.WritePropertyName("chainIds");
                writer.WriteStartArray();
                if (manifest.ChainIds is not null)
                {
                    for (int i = 0; i < manifest.ChainIds.Count; i++)
                    {
                        writer.WriteStringValue(manifest.ChainIds[i] ?? string.Empty);
                    }
                }
                writer.WriteEndArray();
                WriteOptionalString(writer, "payloadSha256", manifest.PayloadSha256);
                WriteOptionalString(writer, "manifestHmac", manifest.ManifestHmac);
                WriteOptionalString(writer, "notes", manifest.Notes);
                writer.WriteEndObject();
            }
            return ms.ToArray();
        }

        private static void WriteAuditEvent(Utf8JsonWriter writer, AuditEvent ev)
        {
            writer.WriteStartObject();
            writer.WriteString("event_id", ev.EventId.ToString("D"));
            writer.WriteString("ts", ev.TimestampUtc.ToString("O", CultureInfo.InvariantCulture));
            WriteOptionalString(writer, "source", ev.Source);
            WriteOptionalString(writer, "entity", ev.EntityName);
            WriteOptionalString(writer, "record_key", ev.RecordKey);
            WriteOptionalString(writer, "user_id", ev.UserId);
            WriteOptionalString(writer, "user_name", ev.UserName);
            WriteOptionalString(writer, "tenant", ev.Tenant);
            WriteOptionalString(writer, "correlation_id", ev.CorrelationId);
            WriteOptionalString(writer, "trace_id", ev.TraceId);
            writer.WriteString("category", ev.Category.ToString());
            WriteOptionalString(writer, "operation", ev.Operation);
            writer.WriteString("outcome", ev.Outcome.ToString());
            WriteOptionalString(writer, "reason", ev.Reason);
            WriteOptionalString(writer, "chain_id", ev.ChainId);
            writer.WriteNumber("sequence", ev.Sequence);
            WriteOptionalString(writer, "prev_hash", ev.PrevHash);
            WriteOptionalString(writer, "hash", ev.Hash);

            if (ev.FieldChanges is not null && ev.FieldChanges.Count > 0)
            {
                writer.WritePropertyName("field_changes");
                writer.WriteStartArray();
                for (int i = 0; i < ev.FieldChanges.Count; i++)
                {
                    AuditFieldChange ch = ev.FieldChanges[i];
                    if (ch is null) { continue; }
                    writer.WriteStartObject();
                    writer.WriteString("field", ch.Field ?? string.Empty);
                    writer.WritePropertyName("old_value");
                    WriteScalar(writer, ch.OldValue);
                    writer.WritePropertyName("new_value");
                    WriteScalar(writer, ch.NewValue);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
            }

            if (ev.Properties is not null && ev.Properties.Count > 0)
            {
                writer.WritePropertyName("properties");
                writer.WriteStartObject();
                foreach (KeyValuePair<string, object> kvp in ev.Properties)
                {
                    writer.WritePropertyName(string.IsNullOrEmpty(kvp.Key) ? "_" : kvp.Key);
                    WriteScalar(writer, kvp.Value);
                }
                writer.WriteEndObject();
            }
            writer.WriteEndObject();
        }

        private static void WriteOptionalString(Utf8JsonWriter writer, string name, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                writer.WriteString(name, value);
            }
        }

        private static void WriteScalar(Utf8JsonWriter writer, object value)
        {
            switch (value)
            {
                case null: writer.WriteNullValue(); return;
                case string s: writer.WriteStringValue(s); return;
                case bool b: writer.WriteBooleanValue(b); return;
                case int i: writer.WriteNumberValue(i); return;
                case long l: writer.WriteNumberValue(l); return;
                case double d: writer.WriteNumberValue(d); return;
                case decimal m: writer.WriteNumberValue(m); return;
                case float f: writer.WriteNumberValue(f); return;
                case System.DateTime dt: writer.WriteStringValue(dt.ToString("O", CultureInfo.InvariantCulture)); return;
                case System.DateTimeOffset dto: writer.WriteStringValue(dto.ToString("O", CultureInfo.InvariantCulture)); return;
                case System.Guid g: writer.WriteStringValue(g.ToString("D")); return;
                default: writer.WriteStringValue(value.ToString()); return;
            }
        }
    }
}
