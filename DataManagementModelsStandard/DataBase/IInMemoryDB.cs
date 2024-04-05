using System;
using System.Collections.Generic;
using System.Threading;
using TheTechIdea;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Util;

namespace DataManagementModels.DataBase
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
         event EventHandler<PassedArgs> OnSyncData;
        IErrorsInfo OpenDatabaseInMemory(string databasename);
        string GetConnectionString();
        IErrorsInfo SaveStructure();
        IErrorsInfo LoadStructure(Progress<PassedArgs> progress, CancellationToken token, bool copydata = false);
        IErrorsInfo LoadData(Progress<PassedArgs> progress,CancellationToken token);
        IErrorsInfo SyncData(Progress<PassedArgs> progress, CancellationToken token);
        List<EntityStructure> InMemoryStructures { get; set; }

    }
}
