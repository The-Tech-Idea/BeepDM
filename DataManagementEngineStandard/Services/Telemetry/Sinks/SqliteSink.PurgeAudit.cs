using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using TheTechIdea.Beep.Services.Audit.Models;
using TheTechIdea.Beep.Services.Audit.Purge;
using TheTechIdea.Beep.Services.Audit.Query;

namespace TheTechIdea.Beep.Services.Telemetry.Sinks
{
    /// <summary>
    /// Phase 10 purge partial of <see cref="SqliteSink"/>. Implements
    /// <see cref="IAuditPurgeStore"/> so the GDPR purge service can run
    /// over a SQLite-backed audit store. Every operation is performed
    /// inside a single transaction and uses the same write gate as
    /// inserts so concurrent producers stay correct.
    /// </summary>
    public sealed partial class SqliteSink : IAuditPurgeStore
    {
        /// <inheritdoc />
        async Task<PurgeImpact> IAuditPurgeStore.DeleteByUserAsync(string userId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return PurgeImpact.Empty;
            }
            return await DeleteWhereAsync(
                "audit_user = $u",
                cmd => cmd.Parameters.AddWithValue("$u", userId),
                cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        async Task<PurgeImpact> IAuditPurgeStore.DeleteByEntityAsync(string entityName, string recordKey, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(entityName))
            {
                return PurgeImpact.Empty;
            }
            if (string.IsNullOrEmpty(recordKey))
            {
                return await DeleteWhereAsync(
                    "audit_entity = $e",
                    cmd => cmd.Parameters.AddWithValue("$e", entityName),
                    cancellationToken).ConfigureAwait(false);
            }
            return await DeleteWhereAsync(
                "audit_entity = $e AND audit_record_key = $r",
                cmd =>
                {
                    cmd.Parameters.AddWithValue("$e", entityName);
                    cmd.Parameters.AddWithValue("$r", recordKey);
                },
                cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        async Task<IReadOnlyList<AuditEvent>> IAuditPurgeStore.ReadChainAsync(string chainId, CancellationToken cancellationToken)
        {
            await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (Volatile.Read(ref _disposed) != 0)
                {
                    return Array.Empty<AuditEvent>();
                }
                SqliteConnection connection = EnsureOpenUnderLock();
                using SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText =
                    "SELECT id, audit_json FROM telemetry " +
                    "WHERE kind = 1 AND json_extract(audit_json, '$.audit.chain_id') = $c " +
                    "ORDER BY json_extract(audit_json, '$.audit.sequence') ASC, id ASC;";
                cmd.Parameters.AddWithValue("$c", chainId ?? AuditEvent.DefaultChainId);

                var list = new List<AuditEvent>();
                using SqliteDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string payload = reader.IsDBNull(1) ? null : reader.GetString(1);
                    AuditEvent ev = NdjsonAuditDeserializer.TryParse(payload);
                    if (ev is null)
                    {
                        continue;
                    }
                    list.Add(ev);
                }
                return list;
            }
            finally
            {
                _writeGate.Release();
            }
        }

        /// <inheritdoc />
        async Task IAuditPurgeStore.UpdateChainAsync(string chainId, IReadOnlyList<AuditEvent> resealed, CancellationToken cancellationToken)
        {
            if (resealed is null || resealed.Count == 0)
            {
                return;
            }

            await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (Volatile.Read(ref _disposed) != 0)
                {
                    return;
                }
                SqliteConnection connection = EnsureOpenUnderLock();
                using SqliteTransaction tx = connection.BeginTransaction();

                using (SqliteCommand findCmd = connection.CreateCommand())
                {
                    findCmd.Transaction = tx;
                    findCmd.CommandText =
                        "SELECT id, audit_json FROM telemetry " +
                        "WHERE kind = 1 AND json_extract(audit_json, '$.audit.event_id') = $eid;";
                    SqliteParameter pEid = findCmd.Parameters.Add("$eid", SqliteType.Text);

                    using SqliteCommand updateCmd = connection.CreateCommand();
                    updateCmd.Transaction = tx;
                    updateCmd.CommandText = "UPDATE telemetry SET audit_json = $json WHERE id = $id;";
                    SqliteParameter pJson = updateCmd.Parameters.Add("$json", SqliteType.Text);
                    SqliteParameter pId = updateCmd.Parameters.Add("$id", SqliteType.Integer);

                    for (int i = 0; i < resealed.Count; i++)
                    {
                        AuditEvent ev = resealed[i];
                        if (ev is null)
                        {
                            continue;
                        }
                        pEid.Value = ev.EventId.ToString("D");
                        long rowId = -1;
                        using (SqliteDataReader reader = findCmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                rowId = reader.GetInt64(0);
                            }
                        }
                        if (rowId < 0)
                        {
                            continue;
                        }
                        TelemetryEnvelope env = new TelemetryEnvelope
                        {
                            Kind = TelemetryKind.Audit,
                            TimestampUtc = ev.TimestampUtc,
                            Category = ev.Source,
                            Audit = ev
                        };
                        pJson.Value = NdjsonSerializer.SerializeText(env);
                        pId.Value = rowId;
                        updateCmd.ExecuteNonQuery();
                    }
                }

                tx.Commit();
            }
            finally
            {
                _writeGate.Release();
            }
        }

        string IAuditPurgeStore.Name => Name;

        private async Task<PurgeImpact> DeleteWhereAsync(string whereClause, Action<SqliteCommand> bind, CancellationToken cancellationToken)
        {
            await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (Volatile.Read(ref _disposed) != 0)
                {
                    return PurgeImpact.Empty;
                }

                SqliteConnection connection = EnsureOpenUnderLock();
                var impact = new PurgeImpact();

                using SqliteTransaction tx = connection.BeginTransaction();

                using (SqliteCommand readCmd = connection.CreateCommand())
                {
                    readCmd.Transaction = tx;
                    readCmd.CommandText =
                        "SELECT json_extract(audit_json, '$.audit.chain_id') AS cid " +
                        "FROM telemetry WHERE kind = 1 AND " + whereClause + " AND audit_json IS NOT NULL;";
                    bind(readCmd);
                    using SqliteDataReader reader = readCmd.ExecuteReader();
                    while (reader.Read())
                    {
                        string cid = reader.IsDBNull(0) ? AuditEvent.DefaultChainId : reader.GetString(0);
                        impact.AffectedChains.Add(string.IsNullOrEmpty(cid) ? AuditEvent.DefaultChainId : cid);
                    }
                }

                using (SqliteCommand deleteCmd = connection.CreateCommand())
                {
                    deleteCmd.Transaction = tx;
                    deleteCmd.CommandText = "DELETE FROM telemetry WHERE kind = 1 AND " + whereClause + ";";
                    bind(deleteCmd);
                    impact.RemovedCount = deleteCmd.ExecuteNonQuery();
                }

                tx.Commit();
                return impact;
            }
            finally
            {
                _writeGate.Release();
            }
        }
    }
}
