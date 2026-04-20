using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;

namespace TheTechIdea.Beep.Services.Logging.Query
{
    /// <summary>
    /// Reads a single NDJSON line written by <c>NdjsonSerializer</c>
    /// and projects it onto a <see cref="LogRecord"/>. Audit envelopes
    /// (lines that carry an <c>"audit"</c> object) are skipped so the
    /// log query path never accidentally surfaces audit data.
    /// </summary>
    public static class NdjsonLogDeserializer
    {
        /// <summary>
        /// Attempts to parse <paramref name="line"/>. Returns <c>null</c>
        /// when the line is not a log envelope.
        /// </summary>
        public static LogRecord TryParse(string line)
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
                if (doc.RootElement.TryGetProperty("audit", out _))
                {
                    return null;
                }
                if (doc.RootElement.TryGetProperty("kind", out JsonElement kindEl)
                    && kindEl.ValueKind == JsonValueKind.String
                    && string.Equals(kindEl.GetString(), "Audit", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                var record = new LogRecord();
                if (doc.RootElement.TryGetProperty("ts", out JsonElement tsEl)
                    && tsEl.ValueKind == JsonValueKind.String
                    && DateTime.TryParse(tsEl.GetString(), CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind, out DateTime ts))
                {
                    record.TimestampUtc = ts.ToUniversalTime();
                }

                if (doc.RootElement.TryGetProperty("level", out JsonElement levelEl)
                    && levelEl.ValueKind == JsonValueKind.String
                    && Enum.TryParse(levelEl.GetString(), true, out BeepLogLevel level))
                {
                    record.Level = level;
                }

                if (doc.RootElement.TryGetProperty("category", out JsonElement catEl)
                    && catEl.ValueKind == JsonValueKind.String)
                {
                    record.Category = catEl.GetString();
                }

                if (doc.RootElement.TryGetProperty("message", out JsonElement msgEl)
                    && msgEl.ValueKind == JsonValueKind.String)
                {
                    record.Message = msgEl.GetString();
                }

                if (doc.RootElement.TryGetProperty("trace_id", out JsonElement traceEl)
                    && traceEl.ValueKind == JsonValueKind.String)
                {
                    record.TraceId = traceEl.GetString();
                }

                if (doc.RootElement.TryGetProperty("correlation_id", out JsonElement corrEl)
                    && corrEl.ValueKind == JsonValueKind.String)
                {
                    record.CorrelationId = corrEl.GetString();
                }

                if (doc.RootElement.TryGetProperty("properties", out JsonElement propsEl)
                    && propsEl.ValueKind == JsonValueKind.Object)
                {
                    var bag = new Dictionary<string, object>(StringComparer.Ordinal);
                    foreach (JsonProperty p in propsEl.EnumerateObject())
                    {
                        bag[p.Name] = ScalarFromJson(p.Value);
                    }
                    record.Properties = bag;
                }

                return record;
            }
            catch
            {
                return null;
            }
        }

        private static object ScalarFromJson(JsonElement el)
        {
            switch (el.ValueKind)
            {
                case JsonValueKind.String:
                    return el.GetString();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Number:
                    if (el.TryGetInt64(out long l)) { return l; }
                    if (el.TryGetDouble(out double d)) { return d; }
                    return el.GetRawText();
                case JsonValueKind.Null:
                    return null;
                default:
                    return el.GetRawText();
            }
        }
    }
}
