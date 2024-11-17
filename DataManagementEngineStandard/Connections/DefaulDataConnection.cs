using TheTechIdea.Beep.DriversConfigurations;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Connections
{
    public class DefaulDataConnection : IDataConnection
    {
        public DefaulDataConnection()
        {
          
            ConnectionProp = new ConnectionProperties();
            DataSourceDriver = new ConnectionDriversConfig();
            GuidID = Guid.NewGuid().ToString();
        }
        public DefaulDataConnection(IDMEEditor pDMEEditor)
        {
            DMEEditor = pDMEEditor;
            ConnectionProp = new ConnectionProperties();
            DataSourceDriver = new ConnectionDriversConfig();
            GuidID=Guid.NewGuid().ToString();
        }
        public IConnectionProperties ConnectionProp { get  ; set  ; }
        public ConnectionDriversConfig DataSourceDriver { get  ; set  ; }
        public ConnectionState ConnectionStatus { get  ; set  ; }
        public IDMEEditor DMEEditor { get  ; set  ; }
        public int ID { get  ; set  ; }
        public string GuidID { get  ; set  ; }
        public IDMLogger Logger { get  ; set  ; }
        public IErrorsInfo ErrorObject { get  ; set  ; }
        public bool InMemory { get  ; set  ; }

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
