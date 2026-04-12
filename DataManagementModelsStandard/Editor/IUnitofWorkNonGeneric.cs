using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;


namespace TheTechIdea.Beep.Editor
{
    public interface IUnitofWork : IDisposable
    {
        void Clear();
        bool IsInListMode { get; set; }
        bool IsDirty { get; }
        dynamic CurrentItem { get; }
        bool IsLogging { get; set; }
        IDataSource DataSource { get; set; }
        string DatasourceName { get; set; }
        string EntityName { get; set; }
        EntityStructure EntityStructure { get; set; }
        Type EntityType { get; set; }
        string Sequencer { get; set; }
        Dictionary<int, string> DeletedKeys { get; set; }
        List<dynamic> DeletedUnits { get; set; }
        Dictionary<int, string> InsertedKeys { get; set; }
        string PrimaryKey { get; set; }
        bool IsIdentity { get; set; }
        string GuidKey { get; set; }

        // Runtime UoW instances hold ObservableBindingList<T>; keep this dynamic to avoid generic invariance issues.
        dynamic Units { get; set; }

        Dictionary<int, string> UpdatedKeys { get; set; }

        // Paging
        int PageIndex { get; set; }
        int PageSize { get; set; }
        int TotalItemCount { get; }

        // commit methods
        Task<IErrorsInfo> Commit(IProgress<PassedArgs> progress, CancellationToken token);
        Task<IErrorsInfo> Commit();
        Task<IErrorsInfo> Rollback();

        void Add(dynamic entity);
        void New();
        ErrorsInfo Delete(string id);
        ErrorsInfo Delete();
        ErrorsInfo Update(string id, dynamic entity);
        ErrorsInfo Update(Func<dynamic, bool> predicate, dynamic updatedEntity);
        ErrorsInfo Delete(Func<dynamic, bool> predicate);

        dynamic Read(Func<dynamic, bool> predicate);
        Task<dynamic> MultiRead(Func<dynamic, bool> predicate);
        Task<dynamic> GetQuery(string query);
        Task<dynamic> Get();
        Task<dynamic> Get(List<AppFilter> filters);

        void UndoLastChange();
        int DocExist(dynamic doc);
        int DocExistByKey(dynamic doc);
        int FindDocIdx(dynamic doc);

        dynamic Get(string PrimaryKeyid);
        double GetLastIdentity();
        IEnumerable<int> GetAddedEntities();

        IEnumerable<dynamic> GetDeletedEntities();
        dynamic Get(int key);
        object GetIDValue(dynamic entity);

        int Getindex(string id);
        int Getindex(dynamic entity);
        IEnumerable<int> GetModifiedEntities();
        int GetPrimaryKeySequence(dynamic doc);
        int GetSeq(string SeqName);
        dynamic Read(string id);
        dynamic GetItemFromCurrentList(int index);
        void MoveFirst();
        void MoveNext();
        void MovePrevious();
        void MoveLast();
        Task<IErrorsInfo> UpdateAsync(dynamic doc);
        Task<IErrorsInfo> InsertAsync(dynamic doc);
        Task<IErrorsInfo> DeleteAsync(dynamic doc);

        Task<IErrorsInfo> InsertDoc(dynamic doc);
        Task<IErrorsInfo> UpdateDoc(dynamic doc);
        Task<IErrorsInfo> DeleteDoc(dynamic doc);
        void MoveTo(int index);
        Tracking GetTrackingItem(dynamic item);
        Dictionary<DateTime, EntityUpdateInsertLog> UpdateLog { get; set; }
        bool SaveLog(string pathandname);

        // Batch operations
        Task<IErrorsInfo> AddRange(IEnumerable<dynamic> entities);
        Task<IErrorsInfo> UpdateRange(IEnumerable<dynamic> entities);
        Task<IErrorsInfo> DeleteRange(IEnumerable<dynamic> entities);

        // Change audit
        List<ChangeRecord> GetChangeLog();

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
        /// <summary>Fires when the current record changes (current-record pointer moved).</summary>
        event EventHandler CurrentChanged;
        /// <summary>Fires when a field changes on a tracked item.</summary>
        event EventHandler<ItemChangedEventArgs<Entity>> ItemChanged;

        // Validation (Phase 3)
        bool IsAutoValidateEnabled { get; set; }
        bool BlockCommitOnValidationError { get; set; }
        ValidationResult ValidateItem(dynamic item);
        ValidationResult ValidateAll();
        List<ValidationError> GetErrors(dynamic item);
        List<dynamic> GetInvalidItems();

        // Undo/Redo (Phase 4)
        bool IsUndoEnabled { get; set; }
        int MaxUndoDepth { get; set; }
        bool CanUndo { get; }
        bool CanRedo { get; }
        bool Undo();
        bool Redo();
        void ClearUndoHistory();

        // Virtual/Lazy Loading (Phase 5)
        bool IsVirtualMode { get; }
        int PageCacheSize { get; set; }
        int VirtualTotalPages { get; }
        void EnableVirtualMode(int totalCount);
        void DisableVirtualMode();
        Task GoToPageAsync(int pageNumber);
        Task PrefetchAdjacentPagesAsync();
        void InvalidatePageCache();

        // Computed Columns (Phase 7)
        void RegisterComputed(string name, Func<dynamic, object> computation);
        void UnregisterComputed(string name);
        object GetComputed(dynamic item, string name);
        Dictionary<string, object> GetAllComputed(dynamic item);
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
        decimal SumWhere(string propertyName, Func<dynamic, bool> predicate);
        decimal Average(string propertyName);
        decimal AverageWhere(string propertyName, Func<dynamic, bool> predicate);
        object Min(string propertyName);
        object Max(string propertyName);
        int CountWhere(Func<dynamic, bool> predicate);
        Dictionary<object, List<dynamic>> GroupBy(string propertyName);
        List<object> DistinctValues(string propertyName);

        // Navigation Enhancements (Phase 11)
        bool IsAtBOF { get; }
        bool IsAtEOF { get; }
        bool IsEmpty { get; }
        bool MoveToItem(dynamic item);

        // Commit Order
        CommitOrder CommitOrder { get; set; }
    }
}
