using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Util;

namespace DataManagementModels.DataBase
{
    public interface IInMemoryDB
    {
       IErrorsInfo OpenDatabaseInMemory(string databasename);
       string GetConnectionString();
        IErrorsInfo SaveStructure();
        List<EntityStructure> InMemoryStructures { get; set; }

    }
}
