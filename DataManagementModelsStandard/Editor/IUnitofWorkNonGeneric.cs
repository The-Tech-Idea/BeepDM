﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;
using TheTechIdea.Util;

namespace DataManagementModels.Editor
{
    public interface IUnitofWork : IDisposable
    {
        void Clear();
        bool IsInListMode { get; set; }
        bool IsDirty { get; }
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

        Task<IErrorsInfo> Commit(IProgress<PassedArgs> progress, CancellationToken token);
        Task<IErrorsInfo> Commit();
        Task<IErrorsInfo> Rollback();

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

        Tracking GetTrackingItem(Entity item);
        Dictionary<DateTime, EntityUpdateInsertLog> UpdateLog { get; set; }
        bool SaveLog(string pathandname);

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
