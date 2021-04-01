using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Logger;
using TheTechIdea.Util;


namespace TheTechIdea.DataManagment_Engine.DataBase
{
    public class DatabaseMethods
    {

        public string DatasourceName { get; set; }
        public IRDBDataConnection Dataconnection { get; set; }
        public DataSourceType DatasourceType { get; set; }
        public DatasourceCategory Category { get; set; } = DatasourceCategory.RDBMS;
        public IErrorsInfo ErrorObject { get; set; }
        public IDMLogger Logger { get; set; }
        public IUtil util { get; set; }
        public DatabaseMethods(IRDBDataConnection pDataconnection, string datasourcename, IDMLogger logger, IUtil putil, DataSourceType databasetype, IErrorsInfo per)
        {
            DatasourceName = datasourcename;
            Logger = logger;
            ErrorObject = per;
            util = putil;
            DatasourceType = databasetype;
            Dataconnection =pDataconnection;



        }


        //----------------------------------------------------------
      
    }
}
