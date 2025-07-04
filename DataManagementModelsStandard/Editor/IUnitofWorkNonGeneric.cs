using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Report;


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
        string Sequencer { get; set; }
        Dictionary<int, string> DeletedKeys { get; set; }
        List<Entity> DeletedUnits { get; set; }
        Dictionary<int, string> InsertedKeys { get; set; }
        string PrimaryKey { get; set; }

        // Here is the Units property specifically for Entity.
        ObservableBindingList<Entity> Units { get; set; }

        Dictionary<int, string> UpdatedKeys { get; set; }
        // commit methods
        Task<IErrorsInfo> Commit(IProgress<PassedArgs> progress, CancellationToken token);
        Task<IErrorsInfo> Commit();
        Task<IErrorsInfo> Rollback();

        void Add(Entity entity);
        void New();
        ErrorsInfo Delete(string id);
        ErrorsInfo Delete();
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

  
}
