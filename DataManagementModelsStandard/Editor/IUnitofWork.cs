
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
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
        Task<IErrorsInfo> Commit(IProgress<PassedArgs> progress, CancellationToken token);
        Task<IErrorsInfo> Commit();
        Task<IErrorsInfo> Rollback();
     
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

        // Utility / Tracking
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

        // Events
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
