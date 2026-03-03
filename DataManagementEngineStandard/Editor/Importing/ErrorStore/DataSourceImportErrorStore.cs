using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Editor.Importing.ErrorStore
{
    /// <summary>
    /// Error store backed by any <see cref="IDataSource"/> (SQLite, LiteDB, etc.).
    /// Uses the <see cref="IDMEEditor"/> to resolve the connection and leverages
    /// <see cref="ILocalDB"/>.CreateDB() to initialise the target database on first use.
    ///
    /// Entity name used: <c>import_errors</c>.
    /// </summary>
    public sealed class DataSourceImportErrorStore : IImportErrorStore
    {
        private const string EntityName = "import_errors";

        private readonly IDMEEditor _editor;
        private readonly string     _connectionName;
        private          bool       _initialised;

        public DataSourceImportErrorStore(IDMEEditor editor, string connectionName)
        {
            _editor         = editor ?? throw new ArgumentNullException(nameof(editor));
            _connectionName = connectionName ?? throw new ArgumentNullException(nameof(connectionName));
        }

        public async Task SaveAsync(ImportErrorRecord record, CancellationToken token = default)
        {
            var ds = await GetDataSourceAsync().ConfigureAwait(false);
            await Task.Run(() => ds.InsertEntity(EntityName, record), token).ConfigureAwait(false);
        }

        public async Task<IReadOnlyList<ImportErrorRecord>> LoadAsync(string contextKey, CancellationToken token = default)
        {
            var ds = await GetDataSourceAsync().ConfigureAwait(false);
            var query = $"SELECT * FROM {EntityName} WHERE ContextKey = '{Escape(contextKey)}'";
            var result = await Task.Run(() => ds.RunQuery(query), token).ConfigureAwait(false);
            return MapRows(result);
        }

        public async Task<IReadOnlyList<ImportErrorRecord>> LoadPendingAsync(string contextKey, CancellationToken token = default)
        {
            var ds = await GetDataSourceAsync().ConfigureAwait(false);
            var query = $"SELECT * FROM {EntityName} WHERE ContextKey = '{Escape(contextKey)}' AND Replayed = 0";
            var result = await Task.Run(() => ds.RunQuery(query), token).ConfigureAwait(false);
            return MapRows(result);
        }

        public async Task MarkReplayedAsync(string contextKey, int batchNumber, int recordIndex, CancellationToken token = default)
        {
            var ds = await GetDataSourceAsync().ConfigureAwait(false);
            var sql = $"UPDATE {EntityName} SET Replayed=1, ReplayedAt='{DateTime.UtcNow:O}' " +
                      $"WHERE ContextKey='{Escape(contextKey)}' AND BatchNumber={batchNumber} AND RecordIndex={recordIndex}";
            await Task.Run(() => ds.ExecuteSql(sql), token).ConfigureAwait(false);
        }

        public async Task ClearAsync(string contextKey, CancellationToken token = default)
        {
            var ds = await GetDataSourceAsync().ConfigureAwait(false);
            var sql = $"DELETE FROM {EntityName} WHERE ContextKey='{Escape(contextKey)}'";
            await Task.Run(() => ds.ExecuteSql(sql), token).ConfigureAwait(false);
        }

        // ------------------------------------------------------------------
        private async Task<IDataSource> GetDataSourceAsync()
        {
            var ds = _editor.GetDataSource(_connectionName);
            if (ds == null)
                throw new InvalidOperationException($"Data source '{_connectionName}' not found.");

            if (!_initialised)
            {
                if (ds is ILocalDB localDb) localDb.CreateDB();
                ds.Openconnection();
                _initialised = true;
            }

            return await Task.FromResult(ds).ConfigureAwait(false);
        }

        private static IReadOnlyList<ImportErrorRecord> MapRows(IEnumerable<object> rows)
        {
            if (rows == null) return Array.Empty<ImportErrorRecord>();
            var list = new List<ImportErrorRecord>();
            foreach (var row in rows)
            {
                if (row == null) continue;
                var t = row.GetType();
                list.Add(new ImportErrorRecord
                {
                    ContextKey  = GetProp<string>(t, row, nameof(ImportErrorRecord.ContextKey))  ?? string.Empty,
                    BatchNumber = GetProp<int>(t, row, nameof(ImportErrorRecord.BatchNumber)),
                    RecordIndex = GetProp<int>(t, row, nameof(ImportErrorRecord.RecordIndex)),
                    Reason      = GetProp<string>(t, row, nameof(ImportErrorRecord.Reason))      ?? string.Empty,
                    RuleName    = GetProp<string>(t, row, nameof(ImportErrorRecord.RuleName)),
                    Replayed    = GetProp<bool>(t, row, nameof(ImportErrorRecord.Replayed)),
                    ReplayedAt  = GetProp<DateTime?>(t, row, nameof(ImportErrorRecord.ReplayedAt)),
                    TriageNote  = GetProp<string>(t, row, nameof(ImportErrorRecord.TriageNote))
                });
            }
            return list;
        }

        private static T? GetProp<T>(Type t, object obj, string name)
        {
            var p = t.GetProperty(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
            if (p == null) return default;
            var val = p.GetValue(obj);
            if (val == null || val == DBNull.Value) return default;
            try { return (T)Convert.ChangeType(val, typeof(T).IsGenericType ? Nullable.GetUnderlyingType(typeof(T))! : typeof(T)); }
            catch { return default; }
        }

        private static string Escape(string s) => s.Replace("'", "''");
    }
}
