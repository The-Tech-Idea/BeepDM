using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace DataManagmentEngineShared.DataBase
{
    public class PostgreDataSource : RDBSource
    {
        public PostgreDataSource(string datasourcename, IDMLogger logger, IDMEEditor DME_Editor, DataSourceType databasetype, IErrorsInfo per) : base(datasourcename, logger, DME_Editor, databasetype, per)
        {

        }
    }
}
