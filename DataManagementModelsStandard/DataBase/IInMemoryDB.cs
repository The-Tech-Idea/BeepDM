using System;
using System.Collections.Generic;
using System.Threading;
using TheTechIdea;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Util;

namespace DataManagementModels.DataBase
{
    public interface IInMemoryDB
    {
        bool IsCreated { get; set; }
        bool IsLoaded { get; set; }
        bool IsSaved { get; set; }
        bool IsSynced { get; set; }
        

       IErrorsInfo OpenDatabaseInMemory(string databasename);
       string GetConnectionString();
        IErrorsInfo SaveStructure();
        IErrorsInfo LoadStructure();
        IErrorsInfo LoadData(Progress<PassedArgs> progress,CancellationToken token);
        IErrorsInfo SyncData(Progress<PassedArgs> progress, CancellationToken token);
        List<EntityStructure> InMemoryStructures { get; set; }

    }
}
