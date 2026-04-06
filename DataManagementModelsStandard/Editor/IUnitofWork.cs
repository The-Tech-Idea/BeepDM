
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.UOW.Models;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor
{
    public interface IUnitofWork<T>: IDisposable where T : Entity,new()
    {
        void Clear();
        bool IsInListMode { get; set; }
        bool IsDirty { get; }
        bool IsLogging { get; set; }
        T CurrentItem { get; }
        IDataSource DataSource { get; set; }
        string DatasourceName { get; set; }
        Dictionary<int, string> DeletedKeys { get; set; }
        List<T> DeletedUnits { get; set; }
        IDMEEditor DMEEditor { get; }
        string EntityName { get; set; }
        EntityStructure EntityStructure { get; set; }
        Type EntityType { get; set; }
        string GuidKey { get; set; }
        string Sequencer { get; set; }
        Dictionary<int, string> InsertedKeys { get; set; }
        string PrimaryKey { get; set; }
        ObservableBindingList<T> Units { get; set; }
        Dictionary<int, string> UpdatedKeys { get; set; }
        bool IsIdentity { get; set; }

        // Paging and filtering
        int PageIndex { get; set; }
        int PageSize { get; set; }
        int TotalItemCount { get; }
        ObservableBindingList<T> FilteredUnits { get; set; }
        string FilterExpression { get; set; }

        // Commit / Rollback
        CommitOrder CommitOrder { get; set; }
        Task<IErrorsInfo> Commit(IProgress<PassedArgs> progress, CancellationToken token);
        Task<IErrorsInfo> Commit();
        Task<IErrorsInfo> Rollback();

        // Validation
        bool IsAutoValidateEnabled { get; set; }
        bool BlockCommitOnValidationError { get; set; }
        ValidationResult ValidateItem(T item);
        ValidationResult ValidateAll();
        List<ValidationError> GetErrors(T item);
        List<T> GetInvalidItems();
     
        // Read operations
        T Read(Func<T, bool> predicate);
        Task<ObservableBindingList<T>> MultiRead(Func<T, bool> predicate);
        T Read(string id);
        T Get(string PrimaryKeyid);
        T Get(int key);
        Task<ObservableBindingList<T>> GetQuery(string query);
        Task<ObservableBindingList<T>> Get();
        Task<ObservableBindingList<T>> Get(List<AppFilter> filters);

        // Create operations
        void New();
        void Add(T entity);

        // Update operations
        ErrorsInfo Update(Func<T, bool> predicate, T updatedEntity);
        IErrorsInfo Update(T entity);
        IErrorsInfo Update(string id, T entity);
        Task<IErrorsInfo> UpdateAsync(T doc);
        IErrorsInfo UpdateDoc(T doc);

        // Delete operations
        ErrorsInfo Delete(Func<T, bool> predicate);
        IErrorsInfo Delete(T doc);
        IErrorsInfo Delete(string id);
        IErrorsInfo Delete();
        Task<IErrorsInfo> DeleteAsync(T doc);
        IErrorsInfo DeleteDoc(T doc);

        // Insert operations
        Task<IErrorsInfo> InsertAsync(T doc);
        IErrorsInfo InsertDoc(T doc);

        // Batch operations
        Task<IErrorsInfo> AddRange(IEnumerable<T> entities);
        Task<IErrorsInfo> UpdateRange(IEnumerable<T> entities);
        Task<IErrorsInfo> DeleteRange(IEnumerable<T> entities);

        // Navigation
        void MoveFirst();
        void MoveNext();
        void MovePrevious();
        void MoveLast();
        void MoveTo(int index);

        // Undo/Redo (Phase 4)
        bool IsUndoEnabled { get; set; }
        int MaxUndoDepth { get; set; }
        bool CanUndo { get; }
        bool CanRedo { get; }
        bool Undo();
        bool Redo();
        void ClearUndoHistory();

        // Computed Columns (Phase 7)
        void RegisterComputed(string name, Func<T, object> computation);
        void UnregisterComputed(string name);
        object GetComputed(T item, string name);
        Dictionary<string, object> GetAllComputed(T item);
        IReadOnlyCollection<string> ComputedColumnNames { get; }

        // Bookmarks (Phase 8)
        void SetBookmark(string name);
        bool GoToBookmark(string name);
        void RemoveBookmark(string name);
        void ClearBookmarks();

        // Thread Safety, Freeze, Batch Update (Phase 9)
        bool IsThreadSafe { get; set; }
        bool IsFrozen { get; }
        void Freeze();
        void Unfreeze();
        IDisposable BeginBatchUpdate();

        // Aggregates (Phase 10)
        decimal Sum(string propertyName);
        decimal SumWhere(string propertyName, Func<T, bool> predicate);
        decimal Average(string propertyName);
        decimal AverageWhere(string propertyName, Func<T, bool> predicate);
        object Min(string propertyName);
        object Max(string propertyName);
        int CountWhere(Func<T, bool> predicate);
        Dictionary<object, List<T>> GroupBy(string propertyName);
        List<object> DistinctValues(string propertyName);

        // Navigation Enhancements (Phase 11)
        bool IsAtBOF { get; }
        bool IsAtEOF { get; }
        bool IsEmpty { get; }
        bool MoveToItem(T item);

        // Virtual/Lazy Loading (Phase 5)
        bool IsVirtualMode { get; }
        int PageCacheSize { get; set; }
        int VirtualTotalPages { get; }
        void EnableVirtualMode(int totalCount);
        void DisableVirtualMode();
        Task GoToPageAsync(int pageNumber);
        Task PrefetchAdjacentPagesAsync();
        void InvalidatePageCache();

        // Utility / Tracking
        [Obsolete("Use Undo() instead")]
        void UndoLastChange();
        int DocExist(T doc);
        int DocExistByKey(T doc);
        int FindDocIdx(T doc);
        double GetLastIdentity();
        IEnumerable<int> GetAddedEntities();
        IEnumerable<T> GetDeletedEntities();
        object GetIDValue(T entity);
        int Getindex(string id);
        int Getindex(T entity);
        IEnumerable<int> GetModifiedEntities();
        int GetPrimaryKeySequence(T doc);
        int GetSeq(string SeqName);
        T GetItemFromCurrentList(int index);
        Tracking GetTrackingItem(T item);

        // Logging
        Dictionary<DateTime, EntityUpdateInsertLog> UpdateLog { get; set; }
        bool SaveLog(string pathandname);

        // Change audit
        List<ChangeRecord> GetChangeLog();

        // Phase 2 — Change Summary (2-A)
        ChangeSummary GetChangeSummary();
        IReadOnlyList<T> GetInsertedItems();
        IReadOnlyList<T> GetUpdatedItems();
        IReadOnlyList<T> GetDeletedItems();

        // Phase 2 — Refresh / Batch Commit (2-B, 2-D)
        Task<IErrorsInfo> RefreshAsync(List<AppFilter> filters = null, ConflictMode conflictMode = ConflictMode.ServerWins, CancellationToken ct = default);
        Task<CommitBatchResult> CommitBatchAsync(int batchSize = 100, IProgress<CommitBatchProgress> progress = null, CancellationToken ct = default);

        // Phase 2 — Revert (2-C)
        bool RevertItem(T item);
        Task<bool> RevertItemAsync(T item, CancellationToken ct = default);

        // Phase 2 — Query History (2-E)
        IReadOnlyList<QueryHistoryEntry> QueryHistory { get; }
        void ClearQueryHistory();
        int MaxQueryHistorySize { get; set; }

        // Phase 2 — Export / Import (2-F, 2-G)
        DataTable ToDataTable();
        Task ToJsonAsync(Stream stream, CancellationToken ct = default);
        Task ToCsvAsync(Stream stream, char delimiter = ',', CancellationToken ct = default);
        Task<int> LoadFromJsonAsync(Stream stream, bool clearFirst = true, CancellationToken ct = default);
        Task<int> LoadFromCsvAsync(Stream stream, char delimiter = ',', bool clearFirst = true, bool hasHeaderRow = true, CancellationToken ct = default);

        // Phase 2 — Find / Clone (2-H)
        Task<T> FindAsync(Func<T, bool> predicate, CancellationToken ct = default);
        Task<List<T>> FindManyAsync(Func<T, bool> predicate, CancellationToken ct = default);
        T CloneItem(T item, bool deepCopy = false);

        // Phase 2 — Count predicate (2-I)
        int Count(Func<T, bool> predicate);

        // Phase 2 — Undo helpers (2-J)
        void EnableUndo(bool enable, int maxDepth = 100);
        bool UndoLastAction();
        bool RedoLastAction();

        // Events
        /// <summary>Fires when the current record changes (current-record pointer moved).</summary>
        event EventHandler CurrentChanged;
        /// <summary>Fires when a field changes on a tracked item.</summary>
        event EventHandler<ItemChangedEventArgs<T>> ItemChanged;
        event EventHandler<UnitofWorkParams> PreDelete;
        event EventHandler<UnitofWorkParams> PreInsert;
        event EventHandler<UnitofWorkParams> PreCreate;
        event EventHandler<UnitofWorkParams> PreUpdate;
        event EventHandler<UnitofWorkParams> PreQuery;

        event EventHandler<UnitofWorkParams> PostQuery;
        event EventHandler<UnitofWorkParams> PostInsert;
        event EventHandler<UnitofWorkParams> PostCreate;
        event EventHandler<UnitofWorkParams> PostUpdate;
        event EventHandler<UnitofWorkParams> PostEdit;
        event EventHandler<UnitofWorkParams> PostDelete;
        event EventHandler<UnitofWorkParams> PostCommit;
        event EventHandler<UnitofWorkParams> PreCommit;
        event EventHandler<UnitofWorkParams> OnItemReverted;
    }

    public class UnitofWorkParams : PassedArgs
    {
        public EventAction EventAction { get; set; }
        public bool Cancel { get; set; } = false;
        public string PropertyName { get; set; }
        public string PropertyValue { get; set; }
        public string EntityName { get; set; }
        public object Record { get; set; }
    }

    public enum EventAction
    {
        PreInsert,
        PreCreate,
        PreUpdate,
        PreDelete,
        PostEdit,
        PreQuery,
        PostInsert,
        PostUpdate,
        PostDelete,
        PostQuery,
        PostCreate,
        PostCommit,
        PreCommit,
        PreBatchInsert,
        PostBatchInsert,
        PreBatchUpdate,
        PostBatchUpdate,
        PreBatchDelete,
        PostBatchDelete,
        PreRollback,
        PostRollback
    }

    /// <summary>
    /// Concurrency mode for optimistic concurrency control
    /// </summary>
    public enum ConcurrencyMode
    {
        /// <summary>No concurrency checking</summary>
        None,
        /// <summary>Last write wins - overwrites without checking</summary>
        LastWriteWins,
        /// <summary>Throws exception if entity was modified by another user</summary>
        ThrowOnConflict
    }

    /// <summary>
    /// Represents a single change record for audit trail purposes
    /// </summary>
    public class ChangeRecord
    {
        public object Entity { get; set; }
        public string PropertyName { get; set; }
        public object OldValue { get; set; }
        public object NewValue { get; set; }
        public DateTime Timestamp { get; set; }
        public EntityState Action { get; set; }
        public string EntityName { get; set; }
    }
}
