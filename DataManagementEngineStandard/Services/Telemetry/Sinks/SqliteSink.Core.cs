using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace TheTechIdea.Beep.Services.Telemetry.Sinks
{
    /// <summary>
    /// Cross-platform SQLite sink for the unified telemetry pipeline.
    /// Backed by <c>Microsoft.Data.Sqlite</c> so the same code runs on
    /// Windows, Linux, macOS, and MAUI without an OS-specific provider.
    /// </summary>
    /// <remarks>
    /// Split into four partial files:
    /// <list type="bullet">
    ///   <item><c>.Core</c> — fields, ctor, connection lifecycle, health.</item>
    ///   <item><c>.Schema</c> — PRAGMAs, CREATE TABLE / INDEX bootstrap.</item>
    ///   <item><c>.Write</c> — batched insert in a single transaction.</item>
    ///   <item><c>.Query</c> — diagnostic read helpers. Full query API lands in Phase 10.</item>
    /// </list>
    /// One open connection per sink instance keeps the file-handle and
    /// memory cost predictable; concurrent writers are serialized through
    /// <c>_writeGate</c>. WAL mode is enabled so the database survives
    /// process kill mid-batch.
    /// </remarks>
    public sealed partial class SqliteSink : ITelemetrySink
    {
        /// <summary>SQLite synchronous=NORMAL — high throughput, recoverable via WAL.</summary>
        public const string SyncNormal = "NORMAL";

        /// <summary>SQLite synchronous=FULL — strict durability for audit-sensitive deployments.</summary>
        public const string SyncFull = "FULL";

        private readonly SemaphoreSlim _writeGate = new(initialCount: 1, maxCount: 1);
        private readonly string _databasePath;
        private readonly string _connectionString;
        private readonly string _synchronous;
        private SqliteConnection _connection;
        private bool _schemaReady;
        private bool _healthy = true;
        private string _lastError;
        private long _writtenCount;
        private int _disposed;

        /// <summary>
        /// Creates a sink that persists to <paramref name="databasePath"/>.
        /// Pass <see cref="SyncFull"/> for audit-strict mode.
        /// </summary>
        public SqliteSink(
            string databasePath,
            string synchronous = SyncNormal,
            string name = "sqlite")
        {
            if (string.IsNullOrWhiteSpace(databasePath))
            {
                throw new ArgumentException("Database path must be supplied.", nameof(databasePath));
            }

            Name = string.IsNullOrWhiteSpace(name) ? "sqlite" : name;
            _databasePath = databasePath;
            _synchronous = string.Equals(synchronous, SyncFull, StringComparison.OrdinalIgnoreCase) ? SyncFull : SyncNormal;

            try
            {
                string dir = Path.GetDirectoryName(databasePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }
            catch (Exception ex)
            {
                Volatile.Write(ref _healthy, false);
                Volatile.Write(ref _lastError, ex.Message);
            }

            SqliteConnectionStringBuilder builder = new SqliteConnectionStringBuilder
            {
                DataSource = databasePath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Default,
                Pooling = false
            };
            _connectionString = builder.ToString();
        }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public bool IsHealthy => Volatile.Read(ref _healthy);

        /// <summary>Configured database file path.</summary>
        public string DatabasePath => _databasePath;

        /// <summary>Configured synchronous mode (<see cref="SyncNormal"/> or <see cref="SyncFull"/>).</summary>
        public string Synchronous => _synchronous;

        /// <summary>Total envelopes inserted since startup.</summary>
        public long WrittenCount => Interlocked.Read(ref _writtenCount);

        /// <summary>Most recent error message, or <c>null</c>.</summary>
        public string LastError => Volatile.Read(ref _lastError);

        private SqliteConnection EnsureOpenUnderLock()
        {
            if (_connection is { State: System.Data.ConnectionState.Open })
            {
                return _connection;
            }

            _connection?.Dispose();
            _connection = new SqliteConnection(_connectionString);
            _connection.Open();

            if (!_schemaReady)
            {
                ApplyPragmas(_connection);
                EnsureSchema(_connection);
                _schemaReady = true;
            }

            Volatile.Write(ref _healthy, true);
            return _connection;
        }

        private void MarkUnhealthy(Exception ex)
        {
            Volatile.Write(ref _healthy, false);
            Volatile.Write(ref _lastError, ex.Message);
            RecordWriteError();
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            await _writeGate.WaitAsync().ConfigureAwait(false);
            try
            {
                try
                {
                    if (_connection is { State: System.Data.ConnectionState.Open })
                    {
                        using (SqliteCommand cmd = _connection.CreateCommand())
                        {
                            cmd.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Volatile.Write(ref _lastError, ex.Message);
                }
                _connection?.Dispose();
                _connection = null;
            }
            finally
            {
                _writeGate.Release();
                _writeGate.Dispose();
            }
        }
    }
}
