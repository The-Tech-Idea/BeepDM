using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.Util;

namespace DataManagementModels.DataBase
{
    public interface IInMemoryDB
    {
       IErrorsInfo OpenDatabaseInMemory(string databasename);
      
    }
}
