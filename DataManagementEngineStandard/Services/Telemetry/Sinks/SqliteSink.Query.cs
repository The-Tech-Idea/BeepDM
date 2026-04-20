using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace TheTechIdea.Beep.Services.Telemetry.Sinks
{
    /// <summary>
    /// Read-side helpers for <see cref="SqliteSink"/>. Phase 03 ships the
    /// minimum needed for diagnostics (row counts, oldest timestamp);
    /// the typed audit query API (<c>QueryAsync(AuditQuery)</c>) lands in
    /// Phase 10 and will reuse the same connection + gate.
    /// </summary>
    public sealed partial class SqliteSink
    {
        /// <summary>Total rows currently stored in the telemetry table.</summary>
        public async Task<long> CountAsync(CancellationToken cancellationToken = default)
            => await ExecuteScalarLongAsync("SELECT COUNT(1) FROM telemetry;", cancellationToken).ConfigureAwait(false);

        /// <summary>Total rows of the supplied <paramref name="kind"/>.</summary>
        public async Task<long> CountAsync(TelemetryKind kind, CancellationToken cancellationToken = default)
        {
            await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (Volatile.Read(ref _disposed) != 0)
                {
                    return 0;
                }
                SqliteConnection connection = EnsureOpenUnderLock();
                using SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT COUNT(1) FROM telemetry WHERE kind = $kind;";
                cmd.Parameters.AddWithValue("$kind", (int)kind);
                object scalar = cmd.ExecuteScalar();
                return scalar is null || scalar is DBNull ? 0L : Convert.ToInt64(scalar);
            }
            finally
            {
                _writeGate.Release();
            }
        }

        /// <summary>UTC timestamp of the oldest row, or <c>null</c> if empty.</summary>
        public async Task<DateTime?> GetOldestTimestampAsync(CancellationToken cancellationToken = default)
        {
            await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (Volatile.Read(ref _disposed) != 0)
                {
                    return null;
                }
                SqliteConnection connection = EnsureOpenUnderLock();
                using SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT ts FROM telemetry ORDER BY id ASC LIMIT 1;";
                object scalar = cmd.ExecuteScalar();
                if (scalar is null || scalar is DBNull)
                {
                    return null;
                }
                return DateTime.Parse(scalar.ToString(), null, System.Globalization.DateTimeStyles.RoundtripKind);
            }
            finally
            {
                _writeGate.Release();
            }
        }

        /// <summary>Returns the persisted schema version from the <c>meta</c> table.</summary>
        public async Task<int> GetSchemaVersionAsync(CancellationToken cancellationToken = default)
        {
            await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (Volatile.Read(ref _disposed) != 0)
                {
                    return 0;
                }
                SqliteConnection connection = EnsureOpenUnderLock();
                using SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT value FROM meta WHERE key = 'schema_version';";
                object scalar = cmd.ExecuteScalar();
                if (scalar is null || scalar is DBNull)
                {
                    return 0;
                }
                return int.TryParse(scalar.ToString(), out int v) ? v : 0;
            }
            finally
            {
                _writeGate.Release();
            }
        }

        private async Task<long> ExecuteScalarLongAsync(string sql, CancellationToken cancellationToken)
        {
            await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (Volatile.Read(ref _disposed) != 0)
                {
                    return 0;
                }
                SqliteConnection connection = EnsureOpenUnderLock();
                using SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText = sql;
                object scalar = cmd.ExecuteScalar();
                return scalar is null || scalar is DBNull ? 0L : Convert.ToInt64(scalar);
            }
            finally
            {
                _writeGate.Release();
            }
        }
    }
}
