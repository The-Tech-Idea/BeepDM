using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace DataManagmentEngineShared.DataBase
{
    public class FireBirdDataSource : RDBSource
    {
        public FireBirdDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDME_Editor, DataSourceType databasetype, IErrorsInfo per) : base(datasourcename, logger, pDME_Editor, databasetype, per)
        {
        }
    }
}
