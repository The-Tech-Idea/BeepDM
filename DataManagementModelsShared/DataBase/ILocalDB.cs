using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.DataBase
{
    public interface ILocalDB
    {
        bool CanCreateLocal { get; set; }
     
       
        bool CreateDB();
        bool DeleteDB();
        IErrorsInfo DropEntity(string EntityName);
        bool CopyDB(string DestDbName,string DesPath);
        IErrorsInfo CloseConnection();


    }
}
