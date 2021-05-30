using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.Hadoop
{
    public class HadoopDataConnection : IDataConnection
    {
        public IConnectionProperties ConnectionProp { get ; set ; }
        public ConnectionDriversConfig DataSourceDriver { get ; set ; }
        public ConnectionState ConnectionStatus { get ; set ; }
        public IDMEEditor DMEEditor { get ; set ; }
        public int ID { get ; set ; }
        public IDMLogger Logger { get ; set ; }
        public IErrorsInfo ErrorObject { get ; set ; }
        public IDbConnection DbConn { get ; set ; }

        public ConnectionState CloseConn()
        {
            throw new NotImplementedException();
        }

        public ConnectionState OpenConnection()
        {
            throw new NotImplementedException();
        }

        public ConnectionState OpenConnection(DataSourceType dbtype, string host, int port, string database, string userid, string password, string parameters)
        {
            throw new NotImplementedException();
        }

        public ConnectionState OpenConnection(DataSourceType dbtype, string connectionstring)
        {
            throw new NotImplementedException();
        }

        public string ReplaceValueFromConnectionString()
        {
            throw new NotImplementedException();
        }
    }
}
