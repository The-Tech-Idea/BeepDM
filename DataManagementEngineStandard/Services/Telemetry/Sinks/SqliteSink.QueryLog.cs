using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using TheTechIdea.Beep.Services.Logging;
using TheTechIdea.Beep.Services.Logging.Query;

namespace TheTechIdea.Beep.Services.Telemetry.Sinks
{
    /// <summary>
    /// Phase 10 log-read partial of <see cref="SqliteSink"/>. Translates
    /// a <see cref="LogQuery"/> into a parameterized SQL statement over
    /// the <c>telemetry</c> table where <c>kind = 0</c> and rehydrates
    /// <see cref="LogRecord"/> instances. Audit rows are excluded so a
    /// single sink can serve both engines without bleeding rows from
    /// one channel into the other.
    /// </summary>
    public sealed partial class SqliteSink : ILogQueryEngine
    {
        /// <inheritdoc />
        async Task<IReadOnlyList<LogRecord>> ILogQueryEngine.ExecuteAsync(LogQuery query, CancellationToken cancellationToken)
        {
            if (query is null)
            {
                return Array.Empty<LogRecord>();
            }

            await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (Volatile.Read(ref _disposed) != 0)
                {
                    return Array.Empty<LogRecord>();
                }

                SqliteConnection connection;
                try
                {
                    connection = EnsureOpenUnderLock();
                }
                catch (Exception ex)
                {
                    MarkUnhealthy(ex);
                    return Array.Empty<LogRecord>();
                }

                var results = new List<LogRecord>(Math.Max(16, query.Take));
                using SqliteCommand cmd = connection.CreateCommand();
                BuildLogSelect(cmd, query);

                using SqliteDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    if (cancellationToken.IsCancellationRequested) { break; }
                    LogRecord record = MapLogRow(reader);
                    if (record is null) { continue; }
                    if (!query.Matches(record)) { continue; }
                    results.Add(record);
                    if (query.Take > 0 && results.Count >= query.Take)
                    {
                        break;
                    }
                }
                return results;
            }
            finally
            {
                _writeGate.Release();
            }
        }

        private static void BuildLogSelect(SqliteCommand cmd, LogQuery query)
        {
            var sql = new StringBuilder(
                "SELECT ts, level, category, msg, props_json, trace_id, corr_id FROM telemetry WHERE kind = 0");

            if (query.FromUtc.HasValue)
            {
                sql.Append(" AND ts >= $from");
                cmd.Parameters.AddWithValue("$from", query.FromUtc.Value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
            }
            if (query.ToUtc.HasValue)
            {
                sql.Append(" AND ts <= $to");
                cmd.Parameters.AddWithValue("$to", query.ToUtc.Value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
            }
            if (query.MinLevel.HasValue)
            {
                sql.Append(" AND level >= $level");
                cmd.Parameters.AddWithValue("$level", (int)query.MinLevel.Value);
            }
            if (!string.IsNullOrEmpty(query.Category))
            {
                sql.Append(" AND category = $cat");
                cmd.Parameters.AddWithValue("$cat", query.Category);
            }
            if (!string.IsNullOrEmpty(query.CorrelationId))
            {
                sql.Append(" AND corr_id = $corr");
                cmd.Parameters.AddWithValue("$corr", query.CorrelationId);
            }
            if (!string.IsNullOrEmpty(query.TraceId))
            {
                sql.Append(" AND trace_id = $trace");
                cmd.Parameters.AddWithValue("$trace", query.TraceId);
            }

            sql.Append(" ORDER BY ts ").Append(query.OrderDescending ? "DESC" : "ASC");

            int hardLimit = query.Take > 0 ? Math.Min(query.Take * 4, 1_000_000) : 1_000_000;
            sql.Append(" LIMIT ").Append(hardLimit.ToString(CultureInfo.InvariantCulture));
            sql.Append(';');

            cmd.CommandText = sql.ToString();
        }

        private static LogRecord MapLogRow(SqliteDataReader reader)
        {
            try
            {
                string tsStr = reader.IsDBNull(0) ? null : reader.GetString(0);
                if (string.IsNullOrEmpty(tsStr)) { return null; }
                if (!DateTime.TryParse(tsStr, CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind, out DateTime ts))
                {
                    return null;
                }

                int levelOrdinal = reader.IsDBNull(1) ? (int)BeepLogLevel.Information : reader.GetInt32(1);
                string category = reader.IsDBNull(2) ? null : reader.GetString(2);
                string message = reader.IsDBNull(3) ? null : reader.GetString(3);
                string propsJson = reader.IsDBNull(4) ? null : reader.GetString(4);
                string traceId = reader.IsDBNull(5) ? null : reader.GetString(5);
                string corrId = reader.IsDBNull(6) ? null : reader.GetString(6);

                return new LogRecord
                {
                    TimestampUtc = ts.ToUniversalTime(),
                    Level = (BeepLogLevel)levelOrdinal,
                    Category = category,
                    Message = message,
                    CorrelationId = corrId,
                    TraceId = traceId,
                    Properties = ParseProps(propsJson)
                };
            }
            catch
            {
                return null;
            }
        }

        private static IDictionary<string, object> ParseProps(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                {
                    return null;
                }
                var bag = new Dictionary<string, object>(StringComparer.Ordinal);
                foreach (JsonProperty p in doc.RootElement.EnumerateObject())
                {
                    bag[p.Name] = ScalarFromJson(p.Value);
                }
                return bag;
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
