using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using TheTechIdea.Beep.Services.Audit.Models;

namespace TheTechIdea.Beep.Services.Telemetry.Sinks
{
    /// <summary>
    /// Insert + flush half of <see cref="SqliteSink"/>. Each
    /// <see cref="WriteBatchAsync"/> call wraps the entire batch in a
    /// single transaction so the database round-trip cost is O(1) per
    /// batch, not O(envelope).
    /// </summary>
    public sealed partial class SqliteSink
    {
        private const string InsertSql =
            "INSERT INTO telemetry (kind, ts, level, category, msg, props_json, trace_id, corr_id, " +
            "audit_user, audit_entity, audit_record_key, audit_json) " +
            "VALUES ($kind, $ts, $level, $category, $msg, $props, $trace, $corr, " +
            "$au_user, $au_entity, $au_record, $au_json);";

        /// <inheritdoc />
        public async Task WriteBatchAsync(IReadOnlyList<TelemetryEnvelope> batch, CancellationToken cancellationToken)
        {
            if (batch is null || batch.Count == 0)
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

                SqliteConnection connection;
                try
                {
                    connection = EnsureOpenUnderLock();
                }
                catch (Exception ex)
                {
                    MarkUnhealthy(ex);
                    return;
                }

                using SqliteTransaction tx = connection.BeginTransaction();
                using SqliteCommand cmd = connection.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = InsertSql;

                SqliteParameter pKind = cmd.Parameters.Add("$kind", SqliteType.Integer);
                SqliteParameter pTs = cmd.Parameters.Add("$ts", SqliteType.Text);
                SqliteParameter pLevel = cmd.Parameters.Add("$level", SqliteType.Integer);
                SqliteParameter pCategory = cmd.Parameters.Add("$category", SqliteType.Text);
                SqliteParameter pMsg = cmd.Parameters.Add("$msg", SqliteType.Text);
                SqliteParameter pProps = cmd.Parameters.Add("$props", SqliteType.Text);
                SqliteParameter pTrace = cmd.Parameters.Add("$trace", SqliteType.Text);
                SqliteParameter pCorr = cmd.Parameters.Add("$corr", SqliteType.Text);
                SqliteParameter pAuUser = cmd.Parameters.Add("$au_user", SqliteType.Text);
                SqliteParameter pAuEntity = cmd.Parameters.Add("$au_entity", SqliteType.Text);
                SqliteParameter pAuRecord = cmd.Parameters.Add("$au_record", SqliteType.Text);
                SqliteParameter pAuJson = cmd.Parameters.Add("$au_json", SqliteType.Text);

                int inserted = 0;
                try
                {
                    for (int i = 0; i < batch.Count; i++)
                    {
                        TelemetryEnvelope env = batch[i];
                        if (env is null)
                        {
                            continue;
                        }

                        pKind.Value = (int)env.Kind;
                        pTs.Value = env.TimestampUtc.ToString("O");
                        pLevel.Value = (int)env.Level;
                        pCategory.Value = AsDbString(env.Category);
                        pMsg.Value = AsDbString(env.Message);
                        pProps.Value = AsDbString(SerializeProperties(env));
                        pTrace.Value = AsDbString(env.TraceId);
                        pCorr.Value = AsDbString(env.CorrelationId);

                        AuditEvent audit = env.Audit;
                        if (audit is not null)
                        {
                            pAuUser.Value = AsDbString(audit.UserId);
                            pAuEntity.Value = AsDbString(audit.EntityName);
                            pAuRecord.Value = AsDbString(audit.RecordKey);
                            pAuJson.Value = AsDbString(NdjsonSerializer.SerializeText(env));
                        }
                        else
                        {
                            pAuUser.Value = DBNull.Value;
                            pAuEntity.Value = DBNull.Value;
                            pAuRecord.Value = DBNull.Value;
                            pAuJson.Value = DBNull.Value;
                        }

                        cmd.ExecuteNonQuery();
                        inserted++;
                    }

                    tx.Commit();
                    Interlocked.Add(ref _writtenCount, inserted);
                    if (inserted > 0)
                    {
                        RecordWriteSuccess();
                    }
                }
                catch (Exception ex)
                {
                    try
                    {
                        tx.Rollback();
                    }
                    catch
                    {
                        // best-effort rollback
                    }
                    MarkUnhealthy(ex);
                }
            }
            finally
            {
                _writeGate.Release();
            }
        }

        /// <inheritdoc />
        public async Task FlushAsync(CancellationToken cancellationToken)
        {
            await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (Volatile.Read(ref _disposed) != 0 || _connection is null)
                {
                    return;
                }
                try
                {
                    using SqliteCommand cmd = _connection.CreateCommand();
                    cmd.CommandText = "PRAGMA wal_checkpoint(PASSIVE);";
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    MarkUnhealthy(ex);
                }
            }
            finally
            {
                _writeGate.Release();
            }
        }

        private static object AsDbString(string value)
            => string.IsNullOrEmpty(value) ? (object)DBNull.Value : value;

        private static string SerializeProperties(TelemetryEnvelope env)
        {
            if (env.Properties is null || env.Properties.Count == 0)
            {
                return null;
            }
            try
            {
                // Reuse the NDJSON serializer to keep one canonical shape
                // for property bags across file + sqlite sinks.
                TelemetryEnvelope shell = new TelemetryEnvelope
                {
                    Kind = env.Kind,
                    Level = env.Level,
                    TimestampUtc = env.TimestampUtc,
                    Properties = env.Properties
                };
                string json = NdjsonSerializer.SerializeText(shell);
                int idx = json.IndexOf("\"properties\":", StringComparison.Ordinal);
                if (idx < 0)
                {
                    return null;
                }
                int start = json.IndexOf('{', idx);
                if (start < 0)
                {
                    return null;
                }
                int depth = 0;
                for (int i = start; i < json.Length; i++)
                {
                    if (json[i] == '{')
                    {
                        depth++;
                    }
                    else if (json[i] == '}')
                    {
                        depth--;
                        if (depth == 0)
                        {
                            return json.Substring(start, i - start + 1);
                        }
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
