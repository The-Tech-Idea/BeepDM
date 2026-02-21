using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Addin;

namespace TheTechIdea.Beep.Editor.UOW
{
    /// <summary>
    /// Interface for Unit of Work wrapper operations
    /// Provides strongly-typed access to dynamic UnitOfWork functionality
    /// </summary>
    public interface IUnitOfWorkWrapper : IDisposable
    {
        // Properties
        bool IsInListMode { get; set; }
        bool IsDirty { get; }
        IDataSource DataSource { get; set; }
        string DatasourceName { get; set; }
        IDMEEditor DMEEditor { get; }
        string EntityName { get; set; }
        EntityStructure EntityStructure { get; set; }
        Type EntityType { get; set; }
        string GuidKey { get; set; }
        string Sequencer { get; set; }
        string PrimaryKey { get; set; }
        dynamic Units { get; set; }
        bool IsIdentity { get; set; }
        
        // Collections for tracking changes
        Dictionary<int, string> DeletedKeys { get; set; }
        List<dynamic> DeletedUnits { get; set; }
        Dictionary<int, string> InsertedKeys { get; set; }
        Dictionary<int, string> UpdatedKeys { get; set; }

        // Core operations
        void Clear();
        Task<dynamic> Get();
        Task<dynamic> Get(List<AppFilter> filters);
        Task<dynamic> GetQuery(string query);
        dynamic Get(int key);
        dynamic Get(string primaryKeyId);
        dynamic Read(string id);

        // CRUD operations (async)
        Task<IErrorsInfo> InsertAsync(dynamic doc);
        Task<IErrorsInfo> UpdateAsync(dynamic doc);
        Task<IErrorsInfo> DeleteAsync(dynamic doc);
        
        // Document operations
        Task<IErrorsInfo> InsertDoc(dynamic doc);
        Task<IErrorsInfo> UpdateDoc(dynamic doc);
        Task<IErrorsInfo> DeleteDoc(dynamic doc);
        int DocExist(dynamic doc);
        int DocExistByKey(dynamic doc);
        int FindDocIdx(dynamic doc);

        // Legacy CRUD operations (sync)
        void New();
        void Create(dynamic entity);
        ErrorsInfo Delete(string id);
        ErrorsInfo Delete(dynamic doc);
        ErrorsInfo Update(dynamic entity);
        ErrorsInfo Update(string id, dynamic entity);

        // Navigation
        void MoveFirst();
        void MoveNext();
        void MovePrevious();
        void MoveLast();
        void MoveTo(int index);

        // Transaction operations
        Task<IErrorsInfo> Commit(IProgress<PassedArgs> progress, CancellationToken token);
        Task<IErrorsInfo> Commit();
        Task<IErrorsInfo> Rollback();

        // Utility methods
        double GetLastIdentity();
        IEnumerable<int> GetAddedEntities();
        IEnumerable<dynamic> GetDeletedEntities();
        IEnumerable<int> GetModifiedEntities();
        dynamic GetIDValue(dynamic entity);
        int Getindex(string id);
        int Getindex(dynamic entity);
        int GetPrimaryKeySequence(dynamic doc);
        int GetSeq(string seqName);

        // Current record access
        dynamic CurrentItem { get; }
        int CurrentIndex { get; }
        int Count { get; }

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

        // Master-Detail (Phase 6)
        void UnregisterAllDetails();
        IReadOnlyList<object> DetailLists { get; }

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
        decimal Average(string propertyName);
        object Min(string propertyName);
        object Max(string propertyName);
        int CountWhere(Func<dynamic, bool> predicate);
        List<object> DistinctValues(string propertyName);

        // Navigation Enhancements (Phase 11)
        bool IsAtBOF { get; }
        bool IsAtEOF { get; }
        bool IsEmpty { get; }
        bool MoveToItem(dynamic item);
    }
}
