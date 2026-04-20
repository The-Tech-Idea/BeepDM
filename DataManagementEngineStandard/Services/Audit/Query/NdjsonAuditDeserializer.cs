using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using TheTechIdea.Beep.Services.Audit.Models;

namespace TheTechIdea.Beep.Services.Audit.Query
{
    /// <summary>
    /// Reads a single NDJSON envelope line written by
    /// <c>NdjsonSerializer</c> (Phase 03) and reconstructs the
    /// embedded <see cref="AuditEvent"/>. Used by the file-scan query
    /// engine and the GDPR purge re-seal path so we can verify and
    /// rewrite chain hashes from disk without spinning up a SQLite sink.
    /// </summary>
    /// <remarks>
    /// The deserializer is strict about field shape but tolerant of
    /// missing / unknown fields so older log files (pre-Phase 08, no
    /// chain fields) round-trip cleanly with <c>Sequence = 0</c>,
    /// <c>Hash = null</c>. Properties are projected to
    /// <see cref="object"/>; numeric / bool / null primitives are kept
    /// as-is so equality comparisons in <see cref="AuditQuery.Matches"/>
    /// behave the way operators expect.
    /// </remarks>
    public static class NdjsonAuditDeserializer
    {
        /// <summary>
        /// Attempts to parse <paramref name="line"/> as an NDJSON envelope
        /// and extract the audit event. Returns <c>null</c> when the line
        /// is not a JSON object, when it has no <c>"audit"</c> object, or
        /// when parsing fails for any reason.
        /// </summary>
        public static AuditEvent TryParse(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return null;
            }

            try
            {
                using JsonDocument doc = JsonDocument.Parse(line);
                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                {
                    return null;
                }
                if (!doc.RootElement.TryGetProperty("audit", out JsonElement audit))
                {
                    return null;
                }
                if (audit.ValueKind != JsonValueKind.Object)
                {
                    return null;
                }
                return ReadAudit(audit);
            }
            catch
            {
                return null;
            }
        }

        private static AuditEvent ReadAudit(JsonElement element)
        {
            var ev = new AuditEvent();

            if (element.TryGetProperty("event_id", out JsonElement evtId)
                && evtId.ValueKind == JsonValueKind.String
                && Guid.TryParse(evtId.GetString(), out Guid g))
            {
                ev.EventId = g;
            }
            if (element.TryGetProperty("ts", out JsonElement ts)
                && ts.ValueKind == JsonValueKind.String
                && DateTime.TryParse(ts.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime when))
            {
                ev.TimestampUtc = when;
            }

            ev.Source = ReadString(element, "source");
            ev.EntityName = ReadString(element, "entity");
            ev.RecordKey = ReadString(element, "record_key");
            ev.UserId = ReadString(element, "user_id");
            ev.UserName = ReadString(element, "user_name");
            ev.Tenant = ReadString(element, "tenant");
            ev.CorrelationId = ReadString(element, "correlation_id");
            ev.TraceId = ReadString(element, "trace_id");
            ev.Operation = ReadString(element, "operation");
            ev.Reason = ReadString(element, "reason");
            ev.PrevHash = ReadString(element, "prev_hash");
            ev.Hash = ReadString(element, "hash");

            string chainId = ReadString(element, "chain_id");
            if (!string.IsNullOrEmpty(chainId))
            {
                ev.ChainId = chainId;
            }

            if (element.TryGetProperty("category", out JsonElement cat)
                && cat.ValueKind == JsonValueKind.String
                && Enum.TryParse(cat.GetString(), ignoreCase: true, out AuditCategory category))
            {
                ev.Category = category;
            }

            if (element.TryGetProperty("outcome", out JsonElement outcome)
                && outcome.ValueKind == JsonValueKind.String
                && Enum.TryParse(outcome.GetString(), ignoreCase: true, out AuditOutcome o))
            {
                ev.Outcome = o;
            }

            if (element.TryGetProperty("sequence", out JsonElement seq)
                && seq.ValueKind == JsonValueKind.Number
                && seq.TryGetInt64(out long seqValue))
            {
                ev.Sequence = seqValue;
            }

            if (element.TryGetProperty("field_changes", out JsonElement changes)
                && changes.ValueKind == JsonValueKind.Array)
            {
                ev.FieldChanges = ReadFieldChanges(changes);
            }

            if (element.TryGetProperty("properties", out JsonElement props)
                && props.ValueKind == JsonValueKind.Object)
            {
                ev.Properties = ReadProperties(props);
            }

            return ev;
        }

        private static string ReadString(JsonElement parent, string name)
        {
            if (parent.TryGetProperty(name, out JsonElement v) && v.ValueKind == JsonValueKind.String)
            {
                return v.GetString();
            }
            return null;
        }

        private static IList<AuditFieldChange> ReadFieldChanges(JsonElement array)
        {
            var list = new List<AuditFieldChange>(array.GetArrayLength());
            foreach (JsonElement item in array.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }
                var change = new AuditFieldChange
                {
                    Field = ReadString(item, "field"),
                    OldValue = item.TryGetProperty("old_value", out JsonElement ov) ? ReadScalar(ov) : null,
                    NewValue = item.TryGetProperty("new_value", out JsonElement nv) ? ReadScalar(nv) : null
                };
                list.Add(change);
            }
            return list;
        }

        private static IDictionary<string, object> ReadProperties(JsonElement obj)
        {
            var dict = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (JsonProperty prop in obj.EnumerateObject())
            {
                dict[prop.Name] = ReadScalar(prop.Value);
            }
            return dict;
        }

        private static object ReadScalar(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    return element.GetString();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Number:
                    if (element.TryGetInt64(out long l))
                    {
                        return l;
                    }
                    if (element.TryGetDouble(out double d))
                    {
                        return d;
                    }
                    return element.GetRawText();
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    return null;
                default:
                    return element.GetRawText();
            }
        }
    }
}
