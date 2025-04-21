using System;
using System.Collections.Generic;
using System.Threading;
using TheTechIdea;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.DataBase
{
    public interface IInMemoryDB
    {
        bool IsCreated { get; set; }
        bool IsLoaded { get; set; }
        bool IsSaved { get; set; }
        bool IsSynced { get; set; }
    
      
        ETLScriptHDR CreateScript { get; set; }
         event EventHandler<PassedArgs> OnLoadData;
         event EventHandler<PassedArgs> OnLoadStructure;
         event EventHandler<PassedArgs> OnSaveStructure;
        event EventHandler<PassedArgs> OnCreateStructure;
        event EventHandler<PassedArgs> OnRefreshData;
        event EventHandler<PassedArgs> OnRefreshDataEntity;
         
         event EventHandler<PassedArgs> OnSyncData;
        IErrorsInfo OpenDatabaseInMemory(string databasename);
        string GetConnectionString();
        IErrorsInfo SaveStructure();
        IErrorsInfo LoadStructure(IProgress<PassedArgs> progress, CancellationToken token, bool copydata = false);
        bool IsStructureCreated { get; set; }
        IErrorsInfo CreateStructure(IProgress<PassedArgs> progress, CancellationToken token);
        IErrorsInfo LoadData(IProgress<PassedArgs> progress,CancellationToken token);
        IErrorsInfo SyncData(IProgress<PassedArgs> progress, CancellationToken token);
        IErrorsInfo SyncData(string entityname, IProgress<PassedArgs> progress, CancellationToken token);
        IErrorsInfo RefreshData(IProgress<PassedArgs> progress, CancellationToken token);
        IErrorsInfo RefreshData(string entityname,IProgress<PassedArgs> progress, CancellationToken token);
        List<EntityStructure> InMemoryStructures { get; set; }

    }
}
