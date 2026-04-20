using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using TheTechIdea.Beep.Services.Audit.Models;
using TheTechIdea.Beep.Services.Audit.Query;

namespace TheTechIdea.Beep.Services.Telemetry.Sinks
{
    /// <summary>
    /// Phase 10 read partial of <see cref="SqliteSink"/>. Translates an
    /// <see cref="AuditQuery"/> into a parameterized SQL statement over
    /// the <c>telemetry</c> table and re-hydrates audit events from the
    /// stored <c>audit_json</c> column.
    /// </summary>
    /// <remarks>
    /// All filter fields with their own column (timestamp, user, entity,
    /// record key) are translated to native predicates so the partial
    /// indexes from Phase 03 stay effective. Fields that only exist
    /// inside the JSON envelope (category, operation, outcome, tenant,
    /// chain id, properties) are filtered post-hydration via
    /// <see cref="AuditQuery.Matches"/> — the query still pushes a
    /// reasonable upper bound on rows fetched (<c>Take * 4</c>) to
    /// keep result-set size bounded for huge datasets.
    /// </remarks>
    public sealed partial class SqliteSink : IAuditQueryEngine
    {
        /// <inheritdoc />
        public async Task<IReadOnlyList<AuditEvent>> ExecuteAsync(
            AuditQuery query,
            CancellationToken cancellationToken = default)
        {
            if (query is null)
            {
                return Array.Empty<AuditEvent>();
            }

            await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (Volatile.Read(ref _disposed) != 0)
                {
                    return Array.Empty<AuditEvent>();
                }

                SqliteConnection connection;
                try
                {
                    connection = EnsureOpenUnderLock();
                }
                catch (Exception ex)
                {
                    MarkUnhealthy(ex);
                    return Array.Empty<AuditEvent>();
                }

                var results = new List<AuditEvent>(Math.Max(16, query.Take));
                using SqliteCommand cmd = connection.CreateCommand();
                BuildSelect(cmd, query);

                using SqliteDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                    string json = reader.IsDBNull(0) ? null : reader.GetString(0);
                    AuditEvent ev = NdjsonAuditDeserializer.TryParse(json);
                    if (ev is null)
                    {
                        continue;
                    }
                    if (!query.Matches(ev))
                    {
                        continue;
                    }
                    results.Add(ev);
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

        private static void BuildSelect(SqliteCommand cmd, AuditQuery query)
        {
            var sql = new StringBuilder("SELECT audit_json FROM telemetry WHERE kind = 1");

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
            if (!string.IsNullOrEmpty(query.UserId))
            {
                sql.Append(" AND audit_user = $user");
                cmd.Parameters.AddWithValue("$user", query.UserId);
            }
            if (!string.IsNullOrEmpty(query.EntityName))
            {
                sql.Append(" AND audit_entity = $entity");
                cmd.Parameters.AddWithValue("$entity", query.EntityName);
            }
            if (!string.IsNullOrEmpty(query.RecordKey))
            {
                sql.Append(" AND audit_record_key = $record");
                cmd.Parameters.AddWithValue("$record", query.RecordKey);
            }

            sql.Append(" ORDER BY ").Append(MapOrderColumn(query.OrderByField));
            sql.Append(query.OrderDescending ? " DESC" : " ASC");

            int hardLimit = query.Take > 0 ? Math.Min(query.Take * 4, 1_000_000) : 1_000_000;
            sql.Append(" LIMIT ").Append(hardLimit.ToString(CultureInfo.InvariantCulture));
            sql.Append(';');

            cmd.CommandText = sql.ToString();
        }

        private static string MapOrderColumn(string field)
        {
            if (string.IsNullOrEmpty(field))
            {
                return "ts";
            }
            switch (field.ToLowerInvariant())
            {
                case "user":
                case "user_id":
                    return "audit_user";
                case "entity":
                case "entity_name":
                    return "audit_entity";
                case "sequence":
                case "seq":
                    return "id";
                default:
                    return "ts";
            }
        }
    }
}
