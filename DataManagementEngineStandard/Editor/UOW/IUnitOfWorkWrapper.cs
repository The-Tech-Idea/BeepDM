using System;
using System.Collections.Generic;
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
    }
}
