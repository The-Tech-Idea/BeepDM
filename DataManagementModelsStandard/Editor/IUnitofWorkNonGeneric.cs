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
        Entity CurrentItem { get; }
        bool IsLogging { get; set; }
        IDataSource DataSource { get; set; }
        string DatasourceName { get; set; }
        string EntityName { get; set; }
        EntityStructure EntityStructure { get; set; }
        Type EntityType { get; set; }
        string Sequencer { get; set; }
        Dictionary<int, string> DeletedKeys { get; set; }
        List<Entity> DeletedUnits { get; set; }
        Dictionary<int, string> InsertedKeys { get; set; }
        string PrimaryKey { get; set; }
        bool IsIdentity { get; set; }
        string GuidKey { get; set; }

        // Here is the Units property specifically for Entity.
        ObservableBindingList<Entity> Units { get; set; }

        Dictionary<int, string> UpdatedKeys { get; set; }

        // Paging
        int PageIndex { get; set; }
        int PageSize { get; set; }
        int TotalItemCount { get; }

        // commit methods
        Task<IErrorsInfo> Commit(IProgress<PassedArgs> progress, CancellationToken token);
        Task<IErrorsInfo> Commit();
        Task<IErrorsInfo> Rollback();

        void Add(Entity entity);
        void New();
        ErrorsInfo Delete(string id);
        ErrorsInfo Delete();
        ErrorsInfo Update(string id, Entity entity);
        ErrorsInfo Update(Func<Entity, bool> predicate, Entity updatedEntity);
        ErrorsInfo Delete(Func<Entity, bool> predicate);

        Entity Read(Func<Entity, bool> predicate);
        Task<ObservableBindingList<Entity>> MultiRead(Func<Entity, bool> predicate);
        Task<ObservableBindingList<Entity>> GetQuery(string query);
        Task<ObservableBindingList<Entity>> Get();
        Task<ObservableBindingList<Entity>> Get(List<AppFilter> filters);

        void UndoLastChange();
        int DocExist(Entity doc);
        int DocExistByKey(Entity doc);
        int FindDocIdx(Entity doc);

        Entity Get(string PrimaryKeyid);
        double GetLastIdentity();
        IEnumerable<int> GetAddedEntities();

        IEnumerable<Entity> GetDeletedEntities();
        Entity Get(int key);
        object GetIDValue(Entity entity);

        int Getindex(string id);
        int Getindex(Entity entity);
        IEnumerable<int> GetModifiedEntities();
        int GetPrimaryKeySequence(Entity doc);
        int GetSeq(string SeqName);
        Entity Read(string id);
        Entity GetItemFromCurrentList(int index);
        void MoveFirst();
        void MoveNext();
        void MovePrevious();
        void MoveLast();
        Task<IErrorsInfo> UpdateAsync(Entity doc);
        Task<IErrorsInfo> InsertAsync(Entity doc);
        Task<IErrorsInfo> DeleteAsync(Entity doc);

        Task<IErrorsInfo> InsertDoc(Entity doc);
        Task<IErrorsInfo> UpdateDoc(Entity doc);
        Task<IErrorsInfo> DeleteDoc(Entity doc);
        void MoveTo(int index);
        Tracking GetTrackingItem(Entity item);
        Dictionary<DateTime, EntityUpdateInsertLog> UpdateLog { get; set; }
        bool SaveLog(string pathandname);

        // Batch operations
        Task<IErrorsInfo> AddRange(IEnumerable<Entity> entities);
        Task<IErrorsInfo> UpdateRange(IEnumerable<Entity> entities);
        Task<IErrorsInfo> DeleteRange(IEnumerable<Entity> entities);

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

        // Validation (Phase 3)
        bool IsAutoValidateEnabled { get; set; }
        bool BlockCommitOnValidationError { get; set; }
        ValidationResult ValidateItem(Entity item);
        ValidationResult ValidateAll();
        List<ValidationError> GetErrors(Entity item);
        List<Entity> GetInvalidItems();

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

        // Master-Detail (Phase 6)
        void RegisterDetail<TChild>(ObservableBindingList<TChild> childList,
            string foreignKeyProperty, string masterKeyProperty)
            where TChild : class, INotifyPropertyChanged, new();
        void UnregisterDetail<TChild>(ObservableBindingList<TChild> childList)
            where TChild : class, INotifyPropertyChanged, new();
        void UnregisterAllDetails();
        IReadOnlyList<object> DetailLists { get; }

        // Computed Columns (Phase 7)
        void RegisterComputed(string name, Func<Entity, object> computation);
        void UnregisterComputed(string name);
        object GetComputed(Entity item, string name);
        Dictionary<string, object> GetAllComputed(Entity item);
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
        decimal SumWhere(string propertyName, Func<Entity, bool> predicate);
        decimal Average(string propertyName);
        decimal AverageWhere(string propertyName, Func<Entity, bool> predicate);
        object Min(string propertyName);
        object Max(string propertyName);
        int CountWhere(Func<Entity, bool> predicate);
        Dictionary<object, List<Entity>> GroupBy(string propertyName);
        List<object> DistinctValues(string propertyName);

        // Navigation Enhancements (Phase 11)
        bool IsAtBOF { get; }
        bool IsAtEOF { get; }
        bool IsEmpty { get; }
        bool MoveToItem(Entity item);

        // Commit Order
        CommitOrder CommitOrder { get; set; }
    }

  
}
