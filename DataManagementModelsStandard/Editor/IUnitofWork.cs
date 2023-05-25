using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Report;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.Editor
{
    public interface IUnitofWork<T> where T : class, INotifyPropertyChanged, new()
    {
        IDataSource DataSource { get; set; }
        string DatasourceName { get; set; }
        Dictionary<int, double> DeletedKeys { get; set; }
        List<T> DeletedUnits { get; set; }
        IDMEEditor DMEEditor { get; }
        string EntityName { get; set; }
        EntityStructure EntityStructure { get; set; }
        Type EntityType { get; set; }
        string GuidKey { get; set; }
        Dictionary<int, double> InsertedKeys { get; set; }
        string PrimaryKey { get; set; }
        ObservableCollection<T> Units { get; set; }
        Dictionary<int, double> UpdatedKeys { get; set; }

        Task<IErrorsInfo> Commit(IProgress<PassedArgs> progress, CancellationToken token);
        void Create(T entity);
        void Delete(string id);
        int DocExist(T doc);
        int DocExistByKey(T doc);
        int FindDocIdx(T doc);
        IEnumerable<int> GetAddedEntities();
        Task<ObservableCollection<T>> GetAllFromSource();
        Task<ObservableCollection<T>> GetByConditionFromSource(List<AppFilter> filters);
        IEnumerable<T> GetDeletedEntities();
        T GetDocFromList(KeyValuePair<int, int> key);
        object GetIDValue(T entity);
        int Getindex(string id);
        int Getindex(T entity);
        IEnumerable<int> GetModifiedEntities();
        int GetPrimaryKey(T doc);
        int GetSeq(string SeqName);
        T Read(string id);
        void Update(string id, T entity);
    }
}