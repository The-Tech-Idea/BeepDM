using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using TheTechIdea.Beep.Services.Audit.Models;

namespace TheTechIdea.Beep.Services.Audit.Export
{
    /// <summary>
    /// CSV renderer for <see cref="AuditExporter"/>. Emits a flat row
    /// per audit event with stable column ordering. Field changes and
    /// property bags are JSON-encoded into single columns so the CSV
    /// stays rectangular while still preserving the full payload for
    /// downstream tools that can re-parse the embedded JSON.
    /// </summary>
    public sealed partial class AuditExporter
    {
        private static readonly string[] CsvHeaders = new[]
        {
            "event_id","ts","source","entity","record_key","user_id","user_name","tenant",
            "correlation_id","trace_id","category","operation","outcome","reason",
            "chain_id","sequence","prev_hash","hash","field_changes_json","properties_json"
        };

        private static byte[] WriteCsv(IReadOnlyList<AuditEvent> events)
        {
            var sb = new StringBuilder(1024);
            for (int h = 0; h < CsvHeaders.Length; h++)
            {
                if (h > 0) { sb.Append(','); }
                sb.Append(CsvHeaders[h]);
            }
            sb.Append('\n');

            for (int i = 0; i < events.Count; i++)
            {
                AuditEvent ev = events[i];
                if (ev is null) { continue; }
                AppendCell(sb, ev.EventId.ToString("D")); sb.Append(',');
                AppendCell(sb, ev.TimestampUtc.ToString("O", CultureInfo.InvariantCulture)); sb.Append(',');
                AppendCell(sb, ev.Source); sb.Append(',');
                AppendCell(sb, ev.EntityName); sb.Append(',');
                AppendCell(sb, ev.RecordKey); sb.Append(',');
                AppendCell(sb, ev.UserId); sb.Append(',');
                AppendCell(sb, ev.UserName); sb.Append(',');
                AppendCell(sb, ev.Tenant); sb.Append(',');
                AppendCell(sb, ev.CorrelationId); sb.Append(',');
                AppendCell(sb, ev.TraceId); sb.Append(',');
                AppendCell(sb, ev.Category.ToString()); sb.Append(',');
                AppendCell(sb, ev.Operation); sb.Append(',');
                AppendCell(sb, ev.Outcome.ToString()); sb.Append(',');
                AppendCell(sb, ev.Reason); sb.Append(',');
                AppendCell(sb, ev.ChainId); sb.Append(',');
                AppendCell(sb, ev.Sequence.ToString(CultureInfo.InvariantCulture)); sb.Append(',');
                AppendCell(sb, ev.PrevHash); sb.Append(',');
                AppendCell(sb, ev.Hash); sb.Append(',');
                AppendCell(sb, FieldChangesAsJson(ev.FieldChanges)); sb.Append(',');
                AppendCell(sb, PropertiesAsJson(ev.Properties));
                sb.Append('\n');
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        private static void AppendCell(StringBuilder sb, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }
            bool needsQuotes =
                value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0;
            if (!needsQuotes)
            {
                sb.Append(value);
                return;
            }
            sb.Append('"');
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c == '"')
                {
                    sb.Append('"').Append('"');
                }
                else
                {
                    sb.Append(c);
                }
            }
            sb.Append('"');
        }

        private static string FieldChangesAsJson(IList<AuditFieldChange> changes)
        {
            if (changes is null || changes.Count == 0)
            {
                return string.Empty;
            }
            using var ms = new MemoryStream();
            using (var writer = new System.Text.Json.Utf8JsonWriter(ms))
            {
                writer.WriteStartArray();
                for (int i = 0; i < changes.Count; i++)
                {
                    AuditFieldChange ch = changes[i];
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
            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private static string PropertiesAsJson(IDictionary<string, object> bag)
        {
            if (bag is null || bag.Count == 0)
            {
                return string.Empty;
            }
            using var ms = new MemoryStream();
            using (var writer = new System.Text.Json.Utf8JsonWriter(ms))
            {
                writer.WriteStartObject();
                foreach (KeyValuePair<string, object> kvp in bag)
                {
                    writer.WritePropertyName(string.IsNullOrEmpty(kvp.Key) ? "_" : kvp.Key);
                    WriteScalar(writer, kvp.Value);
                }
                writer.WriteEndObject();
            }
            return Encoding.UTF8.GetString(ms.ToArray());
        }
    }
}
