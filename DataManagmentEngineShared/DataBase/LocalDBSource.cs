using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.DataBase
{
   

    public class LocalDBSource : RDBSource, ILocalDB 
    {
        public LocalDBSource(string datasourcename, IDMLogger logger, IDMEEditor DMEEditor, DataSourceType databasetype, IErrorsInfo per) : base(datasourcename, logger, DMEEditor, databasetype, per)
        {


        }

        public bool CanCreateLocal { get; set; }

        public virtual IErrorsInfo CloseConnection()
        {
            throw new NotImplementedException();
        }

        public virtual bool CopyDB(string DestDbName, string DesPath)
        {
            throw new NotImplementedException();
        }

        public virtual bool CreateDB()
        {
            throw new NotImplementedException();
        }

        public virtual bool DeleteDB()
        {
            throw new NotImplementedException();
        }

        public virtual IErrorsInfo DropEntity(string EntityName)
        {
            throw new NotImplementedException();
        }
    }
}
