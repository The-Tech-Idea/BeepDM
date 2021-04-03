using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.DataManagment_Engine.Workflow;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.DataBase
{
    public class SQLServerDataSource : RDBSource
    {
        
        public SQLServerDataSource(string datasourcename, IDMLogger logger, IDMEEditor DME_Editor, DataSourceType databasetype, IErrorsInfo per) : base(datasourcename, logger, DME_Editor, databasetype, per)
        {
        }
      

    }
}
