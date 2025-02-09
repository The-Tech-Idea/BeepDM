
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
        Task<IErrorsInfo> Commit(IProgress<PassedArgs> progress, CancellationToken token);
        Task<IErrorsInfo> Commit();
        Task<IErrorsInfo> Rollback();
        void Add(T entity);
        void New();
        T Read(Func<T, bool> predicate);
        Task<ObservableBindingList<T>> MultiRead(Func<T, bool> predicate);
        ErrorsInfo Update(Func<T, bool> predicate, T updatedEntity);
        ErrorsInfo Delete(Func<T, bool> predicate);
        void UndoLastChange();
        int DocExist(T doc);
        int DocExistByKey(T doc);
        int FindDocIdx(T doc);
        T Get(string PrimaryKeyid);
        bool IsIdentity { get; set; }
        double GetLastIdentity();
        IEnumerable<int> GetAddedEntities();
        Task<ObservableBindingList<T>> GetQuery(string query);
        Task<ObservableBindingList<T>> Get();
        Task<ObservableBindingList<T>> Get(List<AppFilter> filters);
        IEnumerable<T> GetDeletedEntities();
        T Get(int key);
        object GetIDValue(T entity);
        int Getindex(string id);
        int Getindex(T entity);
        IEnumerable<int> GetModifiedEntities();
        int GetPrimaryKeySequence(T doc);
        int GetSeq(string SeqName);
        T Read(string id);
        T GetItemFroCurrentList(int index);

        Tracking GetTrackingITem(T item);
        Dictionary<DateTime, EntityUpdateInsertLog> UpdateLog { get; set; }
        bool SaveLog(string pathandname);
        void MoveFirst();
         void MoveNext();
         void MovePrevious();
         void MoveLast();
         void MoveTo(int index);
        Task<IErrorsInfo> UpdateAsync(T doc);
        Task<IErrorsInfo> InsertAsync(T doc);
        Task<IErrorsInfo> DeleteAsync(T doc);

        IErrorsInfo InsertDoc(T doc);
        IErrorsInfo UpdateDoc(T doc);
        IErrorsInfo DeleteDoc(T doc);
        IErrorsInfo Delete(string id);
        IErrorsInfo Delete(T doc);
        IErrorsInfo Delete();
        IErrorsInfo Update(T entity);
        IErrorsInfo Update(string id, T entity);

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
        PreCommit

    }

}