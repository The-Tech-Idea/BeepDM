using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.UOW;
using TheTechIdea.Beep.Editor.UOW.Models;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Editor.UOWManager
{
    /// <summary>
    /// Phase 3 + 4 data-operation facade methods delegating to UOW marker interfaces.
    /// </summary>
    public partial class FormsManager
    {
        #region Undo / Redo

        public void SetBlockUndoEnabled(string blockName, bool enable, int maxDepth = 50)
            => (GetUnitOfWork(blockName) as IUndoable)?.EnableUndo(enable, maxDepth);

        public bool UndoBlock(string blockName)
            => (GetUnitOfWork(blockName) as IUndoable)?.UndoLastAction() ?? false;

        public bool RedoBlock(string blockName)
            => (GetUnitOfWork(blockName) as IUndoable)?.RedoLastAction() ?? false;

        public bool CanUndoBlock(string blockName)
            => (GetUnitOfWork(blockName) as IUndoable)?.CanUndo ?? false;

        public bool CanRedoBlock(string blockName)
            => (GetUnitOfWork(blockName) as IUndoable)?.CanRedo ?? false;

        #endregion

        #region Change Summaries

        public ChangeSummary GetBlockChangeSummary(string blockName)
        {
            var uow = GetUnitOfWork(blockName) as IUnitofWorkHistory;
            return uow?.GetChangeSummary() ?? new ChangeSummary();
        }

        public IReadOnlyDictionary<string, ChangeSummary> GetFormChangeSummary()
        {
            var result = new Dictionary<string, ChangeSummary>();
            foreach (var kv in _blocks)
                result[kv.Key] = GetBlockChangeSummary(kv.Key);
            return result;
        }

        #endregion

        #region Block Data Operations

        public async Task<bool> RefreshBlockAsync(
            string blockName,
            List<AppFilter> filters = null,
            ConflictMode conflictMode = ConflictMode.ServerWins,
            CancellationToken ct = default)
        {
            var uow = GetUnitOfWork(blockName);
            if (uow is IMergeable mergeable)
                return await mergeable.RefreshAsync(filters, conflictMode, ct);
            return false;
        }

        public bool RevertCurrentRecord(string blockName)
        {
            var block = GetBlock(blockName);
            var uow   = block?.UnitOfWork;
            if (uow is IRevertable r)
            {
                var item = uow.Units?.Current;
                if (item != null) return r.RevertItem(item);
            }
            return false;
        }

        public bool RevertRecord(string blockName, int recordIndex)
        {
            var uow = GetUnitOfWork(blockName);
            var revertable = uow as IRevertable;
            if (revertable == null || uow?.Units == null)
                return false;
            if (recordIndex < 0 || recordIndex >= uow.Units.Count)
                return false;

            return revertable.RevertItem(uow.Units[recordIndex]);
        }

        #endregion

        #region Query History

        public IReadOnlyList<QueryHistoryEntry> GetBlockQueryHistory(string blockName)
        {
            var uow = GetUnitOfWork(blockName) as IUnitofWorkHistory;
            return uow?.GetQueryHistory() ?? Array.Empty<QueryHistoryEntry>();
        }

        public void ClearBlockQueryHistory(string blockName)
            => (GetUnitOfWork(blockName) as IUnitofWorkHistory)?.ClearQueryHistory();

        #endregion

        #region Block Aggregates

        public decimal GetBlockSum(string blockName, string fieldName)
            => (GetUnitOfWork(blockName) as IAggregatable)?.Sum(fieldName) ?? 0m;

        public decimal GetBlockAverage(string blockName, string fieldName)
            => (GetUnitOfWork(blockName) as IAggregatable)?.Average(fieldName) ?? 0m;

        public int GetBlockCount(string blockName, Func<object, bool> predicate = null)
            => (GetUnitOfWork(blockName) as IAggregatable)?.Count(predicate) ?? 0;

        #endregion

        #region Batch Commit

        public async Task<CommitBatchResult> CommitFormBatchAsync(
            int batchSize = 200,
            IProgress<CommitBatchProgress> progress = null,
            CancellationToken ct = default)
        {
            var combined = new CommitBatchResult { Success = true };
            foreach (var blockName in _blocks.Keys)
            {
                var partial = await CommitBlockBatchAsync(blockName, batchSize, progress, ct);
                combined.TotalCommitted += partial.TotalCommitted;
                if (!partial.Success) combined.Success = false;
                combined.Errors.AddRange(partial.Errors);
                if (ct.IsCancellationRequested) break;
            }
            return combined;
        }

        public async Task<CommitBatchResult> CommitBlockBatchAsync(
            string blockName,
            int batchSize = 200,
            IProgress<CommitBatchProgress> progress = null,
            CancellationToken ct = default)
        {
            var uow = GetUnitOfWork(blockName);
            if (uow is IBatchCommittable bc)
                return await bc.CommitBatchAsync(batchSize, progress, ct);
            return new CommitBatchResult { Success = false,
                Errors = { $"Block '{blockName}' does not support batch commit." } };
        }

        #endregion

        #region Block Export / Import

        public async Task ExportBlockToJsonAsync(string blockName, Stream stream,
            CancellationToken ct = default)
        {
            if (GetUnitOfWork(blockName) is IExportable exp)
                await exp.ToJsonAsync(stream, ct);
        }

        public async Task ExportBlockToCsvAsync(string blockName, Stream stream,
            char delimiter = ',', CancellationToken ct = default)
        {
            if (GetUnitOfWork(blockName) is IExportable exp)
                await exp.ToCsvAsync(stream, delimiter, ct);
        }

        public DataTable GetBlockAsDataTable(string blockName)
            => (GetUnitOfWork(blockName) as IExportable)?.ToDataTable() ?? new DataTable(blockName);

        public async Task<int> ImportBlockFromJsonAsync(string blockName, Stream stream,
            bool clearFirst = true, CancellationToken ct = default)
        {
            if (GetUnitOfWork(blockName) is IImportable imp)
                return await imp.LoadFromJsonAsync(stream, clearFirst, ct);
            return 0;
        }

        public async Task<int> ImportBlockFromCsvAsync(string blockName, Stream stream,
            char delimiter = ',', bool clearFirst = true, bool hasHeaderRow = true,
            CancellationToken ct = default)
        {
            if (GetUnitOfWork(blockName) is IImportable imp)
                return await imp.LoadFromCsvAsync(stream, delimiter, clearFirst, hasHeaderRow, ct);
            return 0;
        }

        #endregion

        #region Block Grouping

        public IReadOnlyList<ItemGroup<object>> GetBlockGroups(string blockName, string fieldName)
        {
            var uow = GetUnitOfWork(blockName);
            if (uow?.Units == null || string.IsNullOrEmpty(fieldName))
                return Array.Empty<ItemGroup<object>>();

            var prop = uow.Units.GetType()
                          .GetGenericArguments().FirstOrDefault()
                          ?.GetProperty(fieldName,
                             System.Reflection.BindingFlags.Public |
                             System.Reflection.BindingFlags.Instance |
                             System.Reflection.BindingFlags.IgnoreCase);

            if (prop == null) return Array.Empty<ItemGroup<object>>();

            return ((System.Collections.IEnumerable)uow.Units)
                      .Cast<object>()
                      .GroupBy(item => prop.GetValue(item))
                      .Select(g => new ItemGroup<object>
                      {
                          Key   = g.Key,
                          Items = g.ToList()
                      })
                      .ToList();
        }

        #endregion

        #region Phase 4-B – Form State Persistence

        public FormStateSnapshot SaveFormState()
        {
            var snap = new FormStateSnapshot
            {
                FormName     = _currentFormName,
                CurrentBlock = _currentBlockName
            };

            foreach (var kv in _blocks)
            {
                var uow = kv.Value.UnitOfWork;
                snap.BlockStates[kv.Key] = new BlockStateSnapshot
                {
                    BlockName      = kv.Key,
                    CursorPosition = uow?.Units?.CurrentIndex ?? 0,
                    IsDirty        = uow?.IsDirty ?? false,
                    RecordCount    = uow?.Units?.Count ?? 0,
                    Mode           = _systemVariablesManager?.GetSystemVariables(kv.Key)?.MODE ?? "Normal"
                };
            }
            return snap;
        }

        public async Task<bool> RestoreFormStateAsync(FormStateSnapshot snapshot,
            CancellationToken ct = default)
        {
            if (snapshot == null) return false;
            try
            {
                if (!string.IsNullOrEmpty(snapshot.CurrentBlock))
                    await SwitchToBlockAsync(snapshot.CurrentBlock);

                foreach (var kv in snapshot.BlockStates)
                {
                    if (ct.IsCancellationRequested) break;
                    if (!_blocks.ContainsKey(kv.Key)) continue;
                    var idx = kv.Value.CursorPosition;
                    await NavigateToRecordAsync(kv.Key, idx);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Phase 4-C – Cross-Block Validation

        public void RegisterCrossBlockRule(CrossBlockValidationRule rule)
            => _crossBlockValidation.Register(rule);

        public bool UnregisterCrossBlockRule(string ruleName)
            => _crossBlockValidation.Unregister(ruleName);

        public IReadOnlyList<string> ValidateCrossBlock()
            => _crossBlockValidation.Validate();

        #endregion

        #region Phase 4-D – Navigation History

        public async Task<bool> NavigateBackAsync(string blockName)
        {
            var uow = GetUnitOfWork(blockName);
            if (uow == null || !_navHistoryManager.CanGoBack(blockName)) return false;
            var current = uow.Units?.CurrentIndex ?? 0;
            var target  = _navHistoryManager.Back(blockName, current);
            if (target < 0) return false;
            await NavigateToRecordAsync(blockName, target);
            return true;
        }

        public async Task<bool> NavigateForwardAsync(string blockName)
        {
            var uow = GetUnitOfWork(blockName);
            if (uow == null || !_navHistoryManager.CanGoForward(blockName)) return false;
            var current = uow.Units?.CurrentIndex ?? 0;
            var target  = _navHistoryManager.Forward(blockName, current);
            if (target < 0) return false;
            await NavigateToRecordAsync(blockName, target);
            return true;
        }

        public bool CanNavigateBack(string blockName)    => _navHistoryManager.CanGoBack(blockName);
        public bool CanNavigateForward(string blockName) => _navHistoryManager.CanGoForward(blockName);

        public IReadOnlyList<NavigationHistoryEntry> GetNavigationHistory(string blockName)
            => _navHistoryManager.GetHistory(blockName);

        public void ClearNavigationHistory(string blockName)
            => _navHistoryManager.Clear(blockName);

        #endregion

        #region Phase 4-E – Block Clone / Duplicate

        public async Task<bool> CloneBlockDataAsync(string sourceBlockName, string destBlockName,
            CancellationToken ct = default)
        {
            var srcUow  = GetUnitOfWork(sourceBlockName);
            var destUow = GetUnitOfWork(destBlockName);
            if (srcUow == null || destUow == null) return false;
            try
            {
                var items = srcUow.Units == null
                    ? null
                    : ((System.Collections.IEnumerable)srcUow.Units).Cast<object>().ToList();
                if (items == null) return false;
                var srcHistory = srcUow as IUnitofWorkHistory;
                var clones = items.Select(item => srcHistory?.CloneItem(item)).Where(c => c != null).ToList();
                foreach (var clone in clones)
                {
                    if (ct.IsCancellationRequested) break;
                    if (clone is Entity entityClone)
                        destUow.Units?.Add(entityClone);
                }
                return true;
            }
            catch { return false; }
        }

        public async Task<bool> DuplicateCurrentRecordAsync(string blockName,
            CancellationToken ct = default)
        {
            var uow = GetUnitOfWork(blockName);
            if (uow?.Units?.Current == null) return false;
            try
            {
                var history = uow as IUnitofWorkHistory;
                var clone = history?.CloneItem(uow.Units.Current);
                if (clone == null) return false;
                await InsertRecordAsync(blockName, clone);
                return true;
            }
            catch { return false; }
        }

        #endregion
    }
}
