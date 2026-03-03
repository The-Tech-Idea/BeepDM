using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Importing.Interfaces;

namespace TheTechIdea.Beep.Editor.Importing.History
{
    /// <summary>
    /// Import run history store backed by any <see cref="IDataSource"/> (SQLite, LiteDB, etc.).
    /// Uses the <see cref="IDMEEditor"/> to resolve the connection.
    /// Entity name: <c>import_run_history</c>.
    /// </summary>
    public sealed class DataSourceImportRunHistoryStore : IImportRunHistoryStore
    {
        private const string EntityName = "import_run_history";

        private readonly IDMEEditor _editor;
        private readonly string     _connectionName;
        private          bool       _initialised;

        public DataSourceImportRunHistoryStore(IDMEEditor editor, string connectionName)
        {
            _editor         = editor ?? throw new ArgumentNullException(nameof(editor));
            _connectionName = connectionName ?? throw new ArgumentNullException(nameof(connectionName));
        }

        public async Task SaveRunAsync(ImportRunRecord record, CancellationToken token = default)
        {
            var ds = await GetDataSourceAsync().ConfigureAwait(false);
            await Task.Run(() => ds.InsertEntity(EntityName, record), token).ConfigureAwait(false);
        }

        public async Task<IReadOnlyList<ImportRunRecord>> GetRunsAsync(string contextKey, CancellationToken token = default)
        {
            var ds    = await GetDataSourceAsync().ConfigureAwait(false);
            var query = $"SELECT * FROM {EntityName} WHERE ContextKey='{Escape(contextKey)}' ORDER BY StartedAt DESC";
            var result = await Task.Run(() => ds.RunQuery(query), token).ConfigureAwait(false);
            return MapRows(result);
        }

        public async Task<ImportRunRecord?> GetLastSuccessfulRunAsync(string contextKey, CancellationToken token = default)
        {
            var ds    = await GetDataSourceAsync().ConfigureAwait(false);
            var query = $"SELECT * FROM {EntityName} WHERE ContextKey='{Escape(contextKey)}' " +
                        $"AND FinalState='{ImportState.Completed}' ORDER BY StartedAt DESC LIMIT 1";
            var result = await Task.Run(() => ds.RunQuery(query), token).ConfigureAwait(false);
            return MapRows(result).FirstOrDefault();
        }

        public async Task ClearAsync(string contextKey, CancellationToken token = default)
        {
            var ds  = await GetDataSourceAsync().ConfigureAwait(false);
            var sql = $"DELETE FROM {EntityName} WHERE ContextKey='{Escape(contextKey)}'";
            await Task.Run(() => ds.ExecuteSql(sql), token).ConfigureAwait(false);
        }

        // ------------------------------------------------------------------
        private async Task<IDataSource> GetDataSourceAsync()
        {
            var ds = _editor.GetDataSource(_connectionName)
                ?? throw new InvalidOperationException($"Data source '{_connectionName}' not found.");

            if (!_initialised)
            {
                if (ds is ILocalDB localDb) localDb.CreateDB();
                ds.Openconnection();
                _initialised = true;
            }

            return await Task.FromResult(ds).ConfigureAwait(false);
        }

        private static IReadOnlyList<ImportRunRecord> MapRows(IEnumerable<object> rows)
        {
            if (rows == null) return Array.Empty<ImportRunRecord>();
            var list = new List<ImportRunRecord>();
            foreach (var row in rows)
            {
                if (row == null) continue;
                var t = row.GetType();
                list.Add(new ImportRunRecord
                {
                    ContextKey          = GetProp<string>(t, row, nameof(ImportRunRecord.ContextKey))          ?? string.Empty,
                    RunId               = GetProp<string>(t, row, nameof(ImportRunRecord.RunId))               ?? Guid.NewGuid().ToString(),
                    StartedAt           = GetProp<DateTime>(t, row, nameof(ImportRunRecord.StartedAt)),
                    FinishedAt          = GetProp<DateTime?>(t, row, nameof(ImportRunRecord.FinishedAt)),
                    FinalState          = Enum.TryParse<ImportState>(GetProp<string>(t, row, nameof(ImportRunRecord.FinalState)), out var s) ? s : ImportState.Idle,
                    SyncMode            = Enum.TryParse<SyncMode>(GetProp<string>(t, row, nameof(ImportRunRecord.SyncMode)), out var m) ? m : SyncMode.FullRefresh,
                    RecordsRead         = GetProp<long>(t, row, nameof(ImportRunRecord.RecordsRead)),
                    RecordsWritten      = GetProp<long>(t, row, nameof(ImportRunRecord.RecordsWritten)),
                    RecordsBlocked      = GetProp<long>(t, row, nameof(ImportRunRecord.RecordsBlocked)),
                    RecordsQuarantined  = GetProp<long>(t, row, nameof(ImportRunRecord.RecordsQuarantined)),
                    RecordsWarned       = GetProp<long>(t, row, nameof(ImportRunRecord.RecordsWarned)),
                    BatchesProcessed    = GetProp<int>(t, row, nameof(ImportRunRecord.BatchesProcessed)),
                    SchemaDriftDetected = GetProp<bool>(t, row, nameof(ImportRunRecord.SchemaDriftDetected)),
                    FinalWatermark      = GetProp<string>(t, row, nameof(ImportRunRecord.FinalWatermark)),
                    Summary             = GetProp<string>(t, row, nameof(ImportRunRecord.Summary))
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
