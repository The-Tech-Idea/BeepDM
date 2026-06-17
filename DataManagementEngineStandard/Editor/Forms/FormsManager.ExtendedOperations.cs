using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Editor.Forms.Models;

namespace TheTechIdea.Beep.Editor.UOWManager
{
    public partial class FormsManager
    {
        private static readonly Lazy<Type> _passedArgsType = new(() =>
            Type.GetType("TheTechIdea.Beep.ConfigUtil.PassedArgs, TheTechIdea.Beep") ?? typeof(object),
            System.Threading.LazyThreadSafetyMode.PublicationOnly);
        #region Settings (SET_APPLICATION_PROPERTY equivalents)

        private readonly ConcurrentDictionary<string, object> _applicationProperties = new(StringComparer.OrdinalIgnoreCase);

        public void SetApplicationProperty(string name, object value) =>
            _applicationProperties[name] = value;

        public object GetApplicationProperty(string name) =>
            _applicationProperties.TryGetValue(name, out var v) ? v : null;

        public T GetApplicationProperty<T>(string name) =>
            _applicationProperties.TryGetValue(name, out var v) && v is T t ? t : default;

        public bool HasApplicationProperty(string name) =>
            _applicationProperties.ContainsKey(name);

        public void RemoveApplicationProperty(string name) =>
            _applicationProperties.TryRemove(name, out _);

        #endregion

        #region TEXT_IO / Editor built-ins (G2.2)

        public async Task<string> ReadTextFileAsync(string path, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentNullException(nameof(path));
            if (!File.Exists(path)) throw new FileNotFoundException("Text file not found", path);
            return await Task.Run(() => File.ReadAllText(path), ct);
        }

        public async Task WriteTextFileAsync(string path, string content, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentNullException(nameof(path));
            await Task.Run(() => File.WriteAllText(path, content ?? string.Empty), ct);
        }

        public async Task AppendTextFileAsync(string path, string content, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentNullException(nameof(path));
            await Task.Run(() => File.AppendAllText(path, content ?? string.Empty), ct);
        }

        public async Task<string[]> ReadTextLinesAsync(string path, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentNullException(nameof(path));
            if (!File.Exists(path)) throw new FileNotFoundException("Text file not found", path);
            return await Task.Run(() => File.ReadAllLines(path), ct);
        }

        // Editor built-in — the host renders the editor; the engine just surfaces the text content.
        // The host (WinForms/WPF/Blazor) provides the multi-line editing surface.
        public string GetMultiLineText(string blockName, string fieldName)
        {
            var block = GetBlock(blockName);
            if (block == null) return null;
            var uow = block.UnitOfWork;
            return uow?.CurrentItem is object record
                ? GetFieldValue(record, fieldName)?.ToString()
                : null;
        }

        public bool SetMultiLineText(string blockName, string fieldName, string text)
        {
            var block = GetBlock(blockName);
            if (block == null) return false;
            var uow = block.UnitOfWork;
            if (uow?.CurrentItem is object record)
            {
                return SetFieldValue(record, fieldName, text);
            }
            return false;
        }

        #endregion

        #region UoW Surfacing — Bookmarks (G3.1)

        public void SetBlockBookmark(string blockName, string bookmarkName)
        {
            var uow = GetUnitOfWork(blockName);
            if (uow == null) return;
            try
            {
                var method = uow.GetType().GetMethod("SetBookmark");
                if (method != null)
                    method.Invoke(uow, new object[] { bookmarkName });
            }
            catch (Exception ex)
            {
                LogError($"SetBlockBookmark '{bookmarkName}' failed for block '{blockName}'", ex, blockName);
            }
        }

        public bool GoToBlockBookmark(string blockName, string bookmarkName)
        {
            var uow = GetUnitOfWork(blockName);
            if (uow == null) return false;
            try
            {
                var method = uow.GetType().GetMethod("GoToBookmark");
                if (method != null)
                {
                    method.Invoke(uow, new object[] { bookmarkName });
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogError($"GoToBlockBookmark '{bookmarkName}' failed for block '{blockName}'", ex, blockName);
            }
            return false;
        }

        public void RemoveBlockBookmark(string blockName, string bookmarkName)
        {
            var uow = GetUnitOfWork(blockName);
            if (uow == null) return;
            try
            {
                var method = uow.GetType().GetMethod("RemoveBookmark");
                method?.Invoke(uow, new object[] { bookmarkName });
            }
            catch (Exception ex)
            {
                LogError($"RemoveBlockBookmark failed for block '{blockName}'", ex, blockName);
            }
        }

        public void ClearBlockBookmarks(string blockName)
        {
            var uow = GetUnitOfWork(blockName);
            if (uow == null) return;
            try
            {
                var method = uow.GetType().GetMethod("ClearBookmarks");
                method?.Invoke(uow, null);
            }
            catch (Exception ex)
            {
                LogError($"ClearBlockBookmarks failed for block '{blockName}'", ex, blockName);
            }
        }

        #endregion

        #region UoW Surfacing — Computed Columns (G3.2)

        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, Func<object, object>>> _computedColumns
            = new(StringComparer.OrdinalIgnoreCase);

        public void RegisterBlockComputed(string blockName, string columnName, Func<object, object> computation)
        {
            if (string.IsNullOrWhiteSpace(blockName)) throw new ArgumentNullException(nameof(blockName));
            if (string.IsNullOrWhiteSpace(columnName)) throw new ArgumentNullException(nameof(columnName));
            if (computation == null) throw new ArgumentNullException(nameof(computation));

            var dict = _computedColumns.GetOrAdd(blockName, _ =>
                new ConcurrentDictionary<string, Func<object, object>>(StringComparer.OrdinalIgnoreCase));
            dict[columnName] = computation;
        }

        public void UnregisterBlockComputed(string blockName, string columnName)
        {
            if (_computedColumns.TryGetValue(blockName, out var dict))
                dict.TryRemove(columnName, out _);
        }

        public object GetBlockComputedValue(string blockName, string columnName)
        {
            var uow = GetUnitOfWork(blockName);
            if (uow == null) return null;
            var current = uow.CurrentItem;
            if (current == null) return null;

            if (_computedColumns.TryGetValue(blockName, out var dict) &&
                dict.TryGetValue(columnName, out var computation))
            {
                try { return computation(current); }
                catch (Exception ex)
                {
                    LogError($"Computed column '{columnName}' failed for block '{blockName}'", ex, blockName);
                    return null;
                }
            }
            return null;
        }

        public IReadOnlyList<string> GetBlockComputedColumnNames(string blockName)
        {
            if (_computedColumns.TryGetValue(blockName, out var dict))
                return dict.Keys.ToList().AsReadOnly();
            return Array.Empty<string>();
        }

        public Dictionary<string, object> GetAllBlockComputedValues(string blockName)
        {
            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (_computedColumns.TryGetValue(blockName, out var dict))
            {
                foreach (var kvp in dict)
                    result[kvp.Key] = GetBlockComputedValue(blockName, kvp.Key) ?? "error";
            }
            return result;
        }

        #endregion

        #region UoW Surfacing — Freeze / Batch Update (G3.3)

        public void FreezeBlock(string blockName)
        {
            var uow = GetUnitOfWork(blockName);
            if (uow == null) return;
            try
            {
                var method = uow.GetType().GetMethod("Freeze");
                method?.Invoke(uow, null);
            }
            catch (Exception ex)
            {
                LogError($"FreezeBlock failed for block '{blockName}'", ex, blockName);
            }
        }

        public void UnfreezeBlock(string blockName)
        {
            var uow = GetUnitOfWork(blockName);
            if (uow == null) return;
            try
            {
                var method = uow.GetType().GetMethod("Unfreeze");
                method?.Invoke(uow, null);
            }
            catch (Exception ex)
            {
                LogError($"UnfreezeBlock failed for block '{blockName}'", ex, blockName);
            }
        }

        public void BeginBlockBatchUpdate(string blockName)
        {
            var uow = GetUnitOfWork(blockName);
            if (uow == null) return;
            try
            {
                var method = uow.GetType().GetMethod("BeginBatchUpdate");
                method?.Invoke(uow, null);
            }
            catch (Exception ex)
            {
                LogError($"BeginBlockBatchUpdate failed for block '{blockName}'", ex, blockName);
            }
        }

        #endregion

        #region UoW Surfacing — Entity Search / Clone (G3.4)

        public async Task<object> FindBlockRecordAsync(string blockName, Func<object, bool> predicate, CancellationToken ct = default)
        {
            var uow = GetUnitOfWork(blockName);
            if (uow == null) return null;
            try
            {
                var method = uow.GetType().GetMethod("FindAsync");
                if (method != null)
                {
                    var task = (Task)method.Invoke(uow, new object[] { predicate, ct });
                    await task.ConfigureAwait(false);
                    var resultProp = task.GetType().GetProperty("Result");
                    return resultProp?.GetValue(task);
                }
            }
            catch (Exception ex)
            {
                LogError($"FindBlockRecordAsync failed for block '{blockName}'", ex, blockName);
            }
            return null;
        }

        public async Task<List<object>> FindBlockRecordsAsync(string blockName, Func<object, bool> predicate, CancellationToken ct = default)
        {
            var uow = GetUnitOfWork(blockName);
            if (uow == null) return new List<object>();
            try
            {
                var method = uow.GetType().GetMethod("FindManyAsync");
                if (method != null)
                {
                    var task = (Task)method.Invoke(uow, new object[] { predicate, ct });
                    await task.ConfigureAwait(false);
                    var resultProp = task.GetType().GetProperty("Result");
                    if (resultProp?.GetValue(task) is System.Collections.IEnumerable enumerable)
                        return enumerable.Cast<object>().ToList();
                }
            }
            catch (Exception ex)
            {
                LogError($"FindBlockRecordsAsync failed for block '{blockName}'", ex, blockName);
            }
            return new List<object>();
        }

        public async Task<object> CloneBlockRecordAsync(string blockName, bool deepCopy = false, CancellationToken ct = default)
        {
            var uow = GetUnitOfWork(blockName);
            if (uow == null) return null;
            try
            {
                var method = uow.GetType().GetMethod("CloneItem");
                if (method != null)
                {
                    var task = (Task)method.Invoke(uow, new object[] { uow.CurrentItem, deepCopy });
                    await task.ConfigureAwait(false);
                    var resultProp = task.GetType().GetProperty("Result");
                    return resultProp?.GetValue(task);
                }
            }
            catch (Exception ex)
            {
                LogError($"CloneBlockRecordAsync failed for block '{blockName}'", ex, blockName);
            }
            return null;
        }

        #endregion

        #region UoW Surfacing — Change Log (G3.5)

        public IReadOnlyList<object> GetBlockDetailedChangeLog(string blockName)
        {
            var uow = GetUnitOfWork(blockName);
            if (uow == null) return Array.Empty<object>();
            try
            {
                var method = uow.GetType().GetMethod("GetChangeLog");
                if (method?.Invoke(uow, null) is System.Collections.IEnumerable enumerable)
                    return enumerable.Cast<object>().ToList().AsReadOnly();
            }
            catch (Exception ex)
            {
                LogError($"GetBlockDetailedChangeLog failed for block '{blockName}'", ex, blockName);
            }
            return Array.Empty<object>();
        }

        #endregion

        #region UoW Surfacing — Virtual/Lazy Loading Alignment (G3.7)

        public void EnableBlockVirtualMode(string blockName, long totalRecordCount)
        {
            var uow = GetUnitOfWork(blockName);
            if (uow == null) return;
            try
            {
                var method = uow.GetType().GetMethod("EnableVirtualMode");
                method?.Invoke(uow, new object[] { totalRecordCount });
            }
            catch (Exception ex)
            {
                LogError($"EnableBlockVirtualMode failed for block '{blockName}'", ex, blockName);
            }
        }

        public void DisableBlockVirtualMode(string blockName)
        {
            var uow = GetUnitOfWork(blockName);
            if (uow == null) return;
            try
            {
                var method = uow.GetType().GetMethod("DisableVirtualMode");
                method?.Invoke(uow, null);
            }
            catch (Exception ex)
            {
                LogError($"DisableBlockVirtualMode failed for block '{blockName}'", ex, blockName);
            }
        }

        public async Task GoToBlockPageAsync(string blockName, int page, CancellationToken ct = default)
        {
            var uow = GetUnitOfWork(blockName);
            if (uow == null) return;
            try
            {
                var method = uow.GetType().GetMethod("GoToPageAsync");
                if (method != null)
                {
                    var task = (Task)method.Invoke(uow, new object[] { page, ct });
                    await task.ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                LogError($"GoToBlockPageAsync failed for block '{blockName}'", ex, blockName);
            }
        }

        public async Task PrefetchBlockAdjacentPagesAsync(string blockName, CancellationToken ct = default)
        {
            var uow = GetUnitOfWork(blockName);
            if (uow == null) return;
            try
            {
                var method = uow.GetType().GetMethod("PrefetchAdjacentPagesAsync");
                if (method != null)
                {
                    var task = (Task)method.Invoke(uow, new object[] { ct });
                    await task.ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                LogError($"PrefetchBlockAdjacentPagesAsync failed for block '{blockName}'", ex, blockName);
            }
        }

        #endregion

        #region Source-Level Aggregate Queries (G3.10)

        public async Task<double> GetBlockAggregateScalarAsync(string blockName, string aggregateExpression, CancellationToken ct = default)
        {
            var block = GetBlock(blockName);
            if (block == null) return 0;
            try
            {
                var ds = block.DataSource ?? _dmeEditor.GetDataSource(block.DataSourceName);
                if (ds == null) return 0;

                var method = ds.GetType().GetMethod("GetScalarAsync");
                if (method == null) return 0;

                var task = (Task)method.Invoke(ds, new object[] { aggregateExpression });
                if (task == null) return 0;

                await task.ConfigureAwait(false);
                var resultProp = task.GetType().GetProperty("Result");
                var raw = resultProp?.GetValue(task);
                return raw is double d ? d : Convert.ToDouble(raw ?? 0);
            }
            catch (Exception ex)
            {
                LogError($"GetBlockAggregateScalarAsync failed for block '{blockName}'", ex, blockName);
                return 0;
            }
        }

        #endregion

        #region Source-Level Transactions (G3.11)

        public bool BeginFormTransaction()
        {
            try
            {
                foreach (var kvp in _blocks)
                {
                    var ds = kvp.Value.DataSource ?? _dmeEditor.GetDataSource(kvp.Value.DataSourceName);
                    if (ds == null) continue;
                    try
                    {
                        var method = ds.GetType().GetMethod("BeginTransaction");
                        if (method != null)
                        {
                            var args = Activator.CreateInstance(_passedArgsType.Value);
                            method?.Invoke(ds, new[] { args });
                        }
                    }
                    catch (Exception ex) { LogDebugStructured($"Tx begin skipped for '{kvp.Key}': {ex.Message}", kvp.Key); }
                }
                return true;
            }
            catch (Exception ex)
            {
                LogError("BeginFormTransaction failed", ex, null);
                return false;
            }
        }

        public void EndFormTransaction()
        {
            foreach (var kvp in _blocks)
            {
                var ds = kvp.Value.DataSource ?? _dmeEditor.GetDataSource(kvp.Value.DataSourceName);
                if (ds == null) continue;
                try
                {
                    var method = ds.GetType().GetMethod("EndTransaction");
                    method?.Invoke(ds, null);
                }
                catch (Exception ex) { LogDebugStructured($"Tx end skipped for '{kvp.Key}': {ex.Message}", kvp.Key); }
            }
        }

        public bool CommitFormTransaction()
        {
            try
            {
                foreach (var kvp in _blocks)
                {
                    var ds = kvp.Value.DataSource ?? _dmeEditor.GetDataSource(kvp.Value.DataSourceName);
                    if (ds == null) continue;
                    try
                    {
                        var method = ds.GetType().GetMethod("Commit");
                        if (method != null)
                        {
                            var args = Activator.CreateInstance(_passedArgsType.Value);
                            method?.Invoke(ds, new[] { args });
                        }
                    }
                    catch (Exception ex) { LogDebugStructured($"Tx commit skipped for '{kvp.Key}': {ex.Message}", kvp.Key); }
                }
                return true;
            }
            catch (Exception ex)
            {
                LogError("CommitFormTransaction failed", ex, null);
                return false;
            }
        }

        #endregion
    }
}
