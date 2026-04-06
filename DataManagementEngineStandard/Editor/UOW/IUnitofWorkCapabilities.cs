using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.UOW.Models;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Editor.UOW
{
    /// <summary>Marks a UOW that supports single-item revert.</summary>
    public interface IRevertable
    {
        bool RevertItem(object item);
        Task<bool> RevertItemAsync(object item, CancellationToken ct = default);
    }

    /// <summary>Marks a UOW that supports batch commit with progress.</summary>
    public interface IBatchCommittable
    {
        Task<CommitBatchResult> CommitBatchAsync(
            int batchSize = 200,
            IProgress<CommitBatchProgress> progress = null,
            CancellationToken ct = default);
    }

    /// <summary>Marks a UOW that supports data export.</summary>
    public interface IExportable
    {
        DataTable ToDataTable();
        Task ToJsonAsync(Stream stream, CancellationToken ct = default);
        Task ToCsvAsync(Stream stream, char delimiter = ',', CancellationToken ct = default);
    }

    /// <summary>Marks a UOW that supports data import.</summary>
    public interface IImportable
    {
        Task<int> LoadFromJsonAsync(Stream stream, bool clearFirst = true, CancellationToken ct = default);
        Task<int> LoadFromCsvAsync(Stream stream, char delimiter = ',', bool clearFirst = true,
                                   bool hasHeaderRow = true, CancellationToken ct = default);
    }

    /// <summary>Marks a UOW that exposes aggregate calculations.</summary>
    public interface IAggregatable
    {
        decimal Sum(string numericFieldName);
        decimal Average(string numericFieldName);
        int     Count(Func<object, bool> predicate = null);
    }

    /// <summary>Marks a UOW that supports undo/redo.</summary>
    public interface IUndoable
    {
        bool CanUndo { get; }
        bool CanRedo { get; }
        bool UndoLastAction();
        bool RedoLastAction();
        void EnableUndo(bool enable, int maxDepth = 50);
    }

    /// <summary>Marks a UOW with server-merge capability.</summary>
    public interface IMergeable
    {
        Task<bool> RefreshAsync(
            List<AppFilter> filters = null,
            ConflictMode conflictMode = ConflictMode.ServerWins,
            CancellationToken ct = default);
    }

    /// <summary>Marks a UOW that exposes query history and change summary (Phase 3 audit).</summary>
    public interface IUnitofWorkHistory
    {
        ChangeSummary GetChangeSummary();
        IReadOnlyList<QueryHistoryEntry> GetQueryHistory();
        void ClearQueryHistory();
        object CloneItem(object item, bool deep = false);
    }
}
