using DataManagmentEngineShared.WebAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.DataManagment_Engine.DataBase;

namespace TheTechIdea.DataManagment_Engine.NOSQL.CouchDB
{
    public class CouchDBReader : WebAPIReader
    {
        public CouchDBReader(string datasourcename, string databasename, IDMEEditor pDMEEditor, IDataConnection pConn, List<EntityField> pfields = null) : base(datasourcename, pDMEEditor, pConn, pfields)
        {
            
            
        }

    }
}
