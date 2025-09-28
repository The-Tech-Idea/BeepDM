using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Caching.DataSources
{
    /// <summary>
    /// Simple memory cache connection implementation
    /// </summary>
    public class MemoryCacheConnection : IDataConnection
    {
        public ConnectionDriversConfig DataSourceDriver { get; set; }
        public IConnectionProperties ConnectionProp { get; set; }
        public ConnectionState ConnectionStatus { get; set; } = ConnectionState.Closed;
        public IErrorsInfo ErrorObject { get; set; }
        public IDMLogger Logger { get; set; }
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
        public bool InMemory { get; set; } = true;
    
        public IDMEEditor DMEEditor { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int ID { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public MemoryCacheConnection(IDMEEditor dMEEditor)
        {
            ErrorObject = new ErrorsInfo();
        }

        public ConnectionState OpenConnection()
        {
            ConnectionStatus = ConnectionState.Open;
            return ConnectionStatus;
        }

        public ConnectionState OpenConnection(DataSourceType dbtype, string host, int port, string database, string userid, string password, string parameters)
        {
            return OpenConnection();
        }

        public ConnectionState OpenConnection(DataSourceType dbtype, string connectionstring)
        {
            return OpenConnection();
        }

        public ConnectionState CloseConn()
        {
            ConnectionStatus = ConnectionState.Closed;
            return ConnectionStatus;
        }

        public string ReplaceValueFromConnectionString()
        {
            throw new NotImplementedException();
        }

       
    }
}
