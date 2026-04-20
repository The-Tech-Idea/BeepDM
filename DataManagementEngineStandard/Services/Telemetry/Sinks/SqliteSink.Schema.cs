using Microsoft.Data.Sqlite;

namespace TheTechIdea.Beep.Services.Telemetry.Sinks
{
    /// <summary>
    /// Schema bootstrap half of <see cref="SqliteSink"/>. Idempotent
    /// PRAGMA + CREATE statements applied once on first open of the
    /// connection.
    /// </summary>
    /// <remarks>
    /// Schema v1 stores both logs and audit in a single denormalized
    /// <c>telemetry</c> table. Audit-only fields (<c>audit_user</c>,
    /// <c>audit_entity</c>, <c>audit_record_key</c>) are populated only
    /// when <c>kind = 1</c> (Audit) and indexed via partial indexes so
    /// log inserts pay no index cost for fields they do not use.
    /// Phase 08 adds <c>chain_id</c>, <c>seq</c>, <c>prev_hash</c>,
    /// <c>hash</c> through an <c>ALTER TABLE</c> migration tracked in
    /// the <c>meta</c> table.
    /// </remarks>
    public sealed partial class SqliteSink
    {
        /// <summary>Logical schema version persisted in the <c>meta</c> table.</summary>
        public const int SchemaVersion = 1;

        private void ApplyPragmas(SqliteConnection connection)
        {
            using SqliteCommand cmd = connection.CreateCommand();
            cmd.CommandText = string.Concat(
                "PRAGMA journal_mode = WAL;",
                "PRAGMA synchronous = ", _synchronous, ";",
                "PRAGMA temp_store = MEMORY;",
                "PRAGMA foreign_keys = OFF;");
            cmd.ExecuteNonQuery();
        }

        private void EnsureSchema(SqliteConnection connection)
        {
            using (SqliteCommand cmd = connection.CreateCommand())
            {
                cmd.CommandText =
                    "CREATE TABLE IF NOT EXISTS telemetry (" +
                    "  id INTEGER PRIMARY KEY AUTOINCREMENT," +
                    "  kind INTEGER NOT NULL," +
                    "  ts TEXT NOT NULL," +
                    "  level INTEGER NOT NULL," +
                    "  category TEXT," +
                    "  msg TEXT," +
                    "  props_json TEXT," +
                    "  trace_id TEXT," +
                    "  corr_id TEXT," +
                    "  audit_user TEXT," +
                    "  audit_entity TEXT," +
                    "  audit_record_key TEXT," +
                    "  audit_json TEXT" +
                    ");" +
                    "CREATE INDEX IF NOT EXISTS ix_telemetry_kind_ts ON telemetry(kind, ts);" +
                    "CREATE INDEX IF NOT EXISTS ix_telemetry_cat_ts ON telemetry(category, ts);" +
                    "CREATE INDEX IF NOT EXISTS ix_telemetry_audit_user_ts ON telemetry(audit_user, ts) WHERE kind = 1;" +
                    "CREATE INDEX IF NOT EXISTS ix_telemetry_audit_entity_ts ON telemetry(audit_entity, ts) WHERE kind = 1;" +
                    "CREATE TABLE IF NOT EXISTS meta (" +
                    "  key TEXT PRIMARY KEY," +
                    "  value TEXT NOT NULL" +
                    ");";
                cmd.ExecuteNonQuery();
            }

            using (SqliteCommand cmd = connection.CreateCommand())
            {
                cmd.CommandText =
                    "INSERT INTO meta(key, value) VALUES ('schema_version', $v) " +
                    "ON CONFLICT(key) DO UPDATE SET value = excluded.value;";
                cmd.Parameters.AddWithValue("$v", SchemaVersion.ToString());
                cmd.ExecuteNonQuery();
            }
        }
    }
}
