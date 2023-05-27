using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.Util;

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
