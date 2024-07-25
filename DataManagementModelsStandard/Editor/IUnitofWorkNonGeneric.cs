using System;
using System.Collections.Generic;
using System.Text;
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
        Task<IErrorsInfo> Commit(IProgress<PassedArgs> progress, CancellationToken token);
        Task<IErrorsInfo> Commit();
        Task<IErrorsInfo> Rollback();
        ErrorsInfo Delete(string id);
        void UndoLastChange();
        Task<ObservableBindingList<Entity>> GetQuery(string query);
        Task<ObservableBindingList<Entity>> Get();
        Task<ObservableBindingList<Entity>> Get(List<AppFilter> filters);
        Dictionary<DateTime, EntityUpdateInsertLog> UpdateLog { get; set; }
        bool SaveLog(string pathandname);
        // Add other non-generic methods and properties
    }

}
