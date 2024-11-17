using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.DataBase
{
    public interface ILocalDB
    {
        bool CanCreateLocal { get; set; }
        bool InMemory { get; set; }
        bool CreateDB();
        bool CreateDB(bool inMemory);
        bool CreateDB(string filepathandname);
        bool DeleteDB();
        IErrorsInfo DropEntity(string EntityName);
        bool CopyDB(string DestDbName,string DesPath);



    }
}
