using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;
using TheTechIdea.Util;

namespace DataManagementModels.Editor
{
    public interface IEntityUnitofWork
    {
        bool IsInListMode { get; set; }
        IDataSource DataSource { get; set; }
        string DatasourceName { get; set; }
        Dictionary<int, string> DeletedKeys { get; set; }
        List<Entity> DeletedUnits { get; set; }
        IDMEEditor DMEEditor { get; }
        string EntityName { get; set; }
        EntityStructure EntityStructure { get; set; }
        Type EntityType { get; set; }
        string GuidKey { get; set; }
        Dictionary<int, string> InsertedKeys { get; set; }
        string PrimaryKey { get; set; }
        string Sequencer { get; set; }
        ObservableCollection<Entity> Units { get; set; }
        Dictionary<int, string> UpdatedKeys { get; set; }

        Task<IErrorsInfo> Commit(IProgress<PassedArgs> progress, CancellationToken token);
        void Create(Entity entity);
        void Delete(string id);
        int DocExist(Entity doc);
        int DocExistByKey(Entity doc);
        int FindDocIdx(Entity doc);
        IEnumerable<int> GetAddedEntities();
        Task<ObservableCollection<Entity>> Get();
        Task<ObservableCollection<Entity>> Get(List<AppFilter> filters);
        IEnumerable<Entity> GetDeletedEntities();
        Entity GetDocFromList(KeyValuePair<int, int> key);
        object GetIDValue(Entity entity);
        int Getindex(string id);
        int Getindex(Entity entity);
        IEnumerable<int> GetModifiedEntities();
        int GetPrimaryKey(Entity doc);
        int GetSeq(string SeqName);
        Entity Read(string id);
        void Update(string id, Entity entity);
    }
}
