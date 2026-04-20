using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace TheTechIdea.Beep.Services.Telemetry
{
    /// <summary>
    /// UTF-8 newline-delimited-JSON writer used by file and SQLite sinks
    /// to render <see cref="TelemetryEnvelope"/> instances. Built on
    /// <see cref="System.Text.Json"/> so it works on every supported
    /// .NET target without per-OS branches.
    /// </summary>
    /// <remarks>
    /// Exception payloads are flattened to a small property bag (type,
    /// message, stack) rather than reflected through the full type tree.
    /// This keeps output compact, avoids reentrancy on logger frames, and
    /// sidesteps serializer cycles on common framework exceptions.
    /// </remarks>
    public static class NdjsonSerializer
    {
        private static readonly JsonWriterOptions WriterOptions = new JsonWriterOptions
        {
            Indented = false,
            SkipValidation = false,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        /// <summary>
        /// Serializes a single envelope to UTF-8 bytes terminated with
        /// <c>\n</c>. Returns an array sized exactly to the produced
        /// payload to keep the file writer allocation pattern predictable.
        /// </summary>
        public static byte[] SerializeLine(TelemetryEnvelope envelope)
        {
            if (envelope is null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            using MemoryStream buffer = new MemoryStream(256);
            using (Utf8JsonWriter writer = new Utf8JsonWriter(buffer, WriterOptions))
            {
                WriteEnvelope(writer, envelope);
            }
            buffer.WriteByte((byte)'\n');
            return buffer.ToArray();
        }

        /// <summary>
        /// Serializes a single envelope as a UTF-8 string (no trailing
        /// newline). Useful for sinks that store one envelope per row
        /// (SQLite, IndexedDB) instead of per file line.
        /// </summary>
        public static string SerializeText(TelemetryEnvelope envelope)
        {
            byte[] bytes = SerializeLine(envelope);
            // Drop the trailing newline.
            int length = bytes.Length > 0 && bytes[bytes.Length - 1] == (byte)'\n'
                ? bytes.Length - 1
                : bytes.Length;
            return Utf8NoBom.GetString(bytes, 0, length);
        }

        private static void WriteEnvelope(Utf8JsonWriter writer, TelemetryEnvelope envelope)
        {
            writer.WriteStartObject();

            writer.WriteString("ts", envelope.TimestampUtc.ToString("O"));
            writer.WriteString("kind", envelope.Kind.ToString());
            writer.WriteString("level", envelope.Level.ToString());

            WriteOptional(writer, "category", envelope.Category);
            WriteOptional(writer, "message", envelope.Message);
            WriteOptional(writer, "trace_id", envelope.TraceId);
            WriteOptional(writer, "correlation_id", envelope.CorrelationId);

            if (envelope.Exception is not null)
            {
                writer.WritePropertyName("exception");
                WriteException(writer, envelope.Exception);
            }

            if (envelope.Properties is not null && envelope.Properties.Count > 0)
            {
                writer.WritePropertyName("properties");
                WriteProperties(writer, envelope.Properties);
            }

            if (envelope.Audit is not null)
            {
                writer.WritePropertyName("audit");
                WriteAudit(writer, envelope.Audit);
            }

            writer.WriteEndObject();
        }

        private static void WriteOptional(Utf8JsonWriter writer, string name, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                writer.WriteString(name, value);
            }
        }

        private static void WriteException(Utf8JsonWriter writer, Exception ex)
        {
            writer.WriteStartObject();
            writer.WriteString("type", ex.GetType().FullName);
            writer.WriteString("message", ex.Message);
            if (!string.IsNullOrEmpty(ex.StackTrace))
            {
                writer.WriteString("stack", ex.StackTrace);
            }
            if (ex.InnerException is not null)
            {
                writer.WritePropertyName("inner");
                WriteException(writer, ex.InnerException);
            }
            writer.WriteEndObject();
        }

        private static void WriteProperties(Utf8JsonWriter writer, IDictionary<string, object> bag)
        {
            writer.WriteStartObject();
            foreach (KeyValuePair<string, object> kvp in bag)
            {
                writer.WritePropertyName(string.IsNullOrEmpty(kvp.Key) ? "_" : kvp.Key);
                WriteScalar(writer, kvp.Value);
            }
            writer.WriteEndObject();
        }

        private static void WriteAudit(Utf8JsonWriter writer, Audit.Models.AuditEvent audit)
        {
            writer.WriteStartObject();
            writer.WriteString("event_id", audit.EventId.ToString("D"));
            writer.WriteString("ts", audit.TimestampUtc.ToString("O"));
            WriteOptional(writer, "source", audit.Source);
            WriteOptional(writer, "entity", audit.EntityName);
            WriteOptional(writer, "record_key", audit.RecordKey);
            WriteOptional(writer, "user_id", audit.UserId);
            WriteOptional(writer, "user_name", audit.UserName);
            WriteOptional(writer, "tenant", audit.Tenant);
            WriteOptional(writer, "correlation_id", audit.CorrelationId);
            WriteOptional(writer, "trace_id", audit.TraceId);

            // Phase 08 fields — emitted additively so existing readers
            // ignore them while Phase 10 query / verifier paths can
            // round-trip the full canonical envelope.
            writer.WriteString("category", audit.Category.ToString());
            WriteOptional(writer, "operation", audit.Operation);
            writer.WriteString("outcome", audit.Outcome.ToString());
            WriteOptional(writer, "reason", audit.Reason);
            WriteOptional(writer, "chain_id", audit.ChainId);
            writer.WriteNumber("sequence", audit.Sequence);
            WriteOptional(writer, "prev_hash", audit.PrevHash);
            WriteOptional(writer, "hash", audit.Hash);

            if (audit.FieldChanges is not null && audit.FieldChanges.Count > 0)
            {
                writer.WritePropertyName("field_changes");
                WriteFieldChanges(writer, audit.FieldChanges);
            }

            if (audit.Properties is not null && audit.Properties.Count > 0)
            {
                writer.WritePropertyName("properties");
                WriteProperties(writer, audit.Properties);
            }
            writer.WriteEndObject();
        }

        private static void WriteFieldChanges(Utf8JsonWriter writer, System.Collections.Generic.IList<Audit.Models.AuditFieldChange> changes)
        {
            writer.WriteStartArray();
            for (int i = 0; i < changes.Count; i++)
            {
                Audit.Models.AuditFieldChange change = changes[i];
                if (change is null)
                {
                    continue;
                }
                writer.WriteStartObject();
                writer.WriteString("field", change.Field ?? string.Empty);
                writer.WritePropertyName("old_value");
                WriteScalar(writer, change.OldValue);
                writer.WritePropertyName("new_value");
                WriteScalar(writer, change.NewValue);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }

        private static void WriteScalar(Utf8JsonWriter writer, object value)
        {
            switch (value)
            {
                case null:
                    writer.WriteNullValue();
                    return;
                case string s:
                    writer.WriteStringValue(s);
                    return;
                case bool b:
                    writer.WriteBooleanValue(b);
                    return;
                case int i:
                    writer.WriteNumberValue(i);
                    return;
                case long l:
                    writer.WriteNumberValue(l);
                    return;
                case double d:
                    writer.WriteNumberValue(d);
                    return;
                case decimal m:
                    writer.WriteNumberValue(m);
                    return;
                case float f:
                    writer.WriteNumberValue(f);
                    return;
                case DateTime dt:
                    writer.WriteStringValue(dt.ToString("O"));
                    return;
                case DateTimeOffset dto:
                    writer.WriteStringValue(dto.ToString("O"));
                    return;
                case Guid g:
                    writer.WriteStringValue(g.ToString("D"));
                    return;
                case TimeSpan ts:
                    writer.WriteStringValue(ts.ToString("c"));
                    return;
                default:
                    writer.WriteStringValue(value.ToString());
                    return;
            }
        }
    }
}
