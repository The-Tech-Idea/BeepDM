using DataManagementModels.Editor;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Report;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.Editor
{
    public interface IUnitofWork<T>: IDisposable where T : Entity
    {
        void Clear();
        bool IsInListMode { get; set; }
        bool IsDirty { get; }
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
        void Create(T entity);
        ErrorsInfo Delete(string id);
        ErrorsInfo Delete(T doc);
        ErrorsInfo Update(T entity);
        ErrorsInfo Update(string id, T entity);
        
        int DocExist(T doc);
        int DocExistByKey(T doc);
        int FindDocIdx(T doc);
        T Get(string PrimaryKeyid);
        bool IsIdentity { get; set; }
        double GetLastIdentity();
        IEnumerable<int> GetAddedEntities();
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
       

        event EventHandler<UnitofWorkParams> PreInsert;
        event EventHandler<UnitofWorkParams> PreUpdate;
        event EventHandler<UnitofWorkParams> PreQuery;
        event EventHandler<UnitofWorkParams> PostQuery;
        event EventHandler<UnitofWorkParams> PostInsert;
        event EventHandler<UnitofWorkParams> PostUpdate;
        event EventHandler<UnitofWorkParams> PostEdit;
        event EventHandler<UnitofWorkParams> PreDelete;
        event EventHandler<UnitofWorkParams> PostCreate;

    }
    public class UnitofWorkParams : PassedArgs
    {
        public bool Cancel { get; set; } = false;
        public string PropertyName { get; set; }
        public string PropertyValue { get; set; }
        public string EntityName { get; set; }
        public object Record { get; set; }

    }
}